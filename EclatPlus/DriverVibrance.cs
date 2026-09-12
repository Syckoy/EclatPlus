using System.Runtime.InteropServices;

namespace EclatPlus;

/// <summary>
/// Même réglage que le panneau NVIDIA (Éclat numérique) ou AMD (Saturation).
/// Pas d’overlay, pas d’injection, pas de lecture mémoire des jeux.
/// </summary>
internal sealed class DriverVibrance : IDisposable
{
    private readonly IGpuVibrance? _backend;
    private bool _disposed;

    public string BackendName => _backend?.Name ?? "Aucun";
    public bool IsAvailable => _backend is not null;

    public DriverVibrance()
    {
        if (NvidiaVibrance.TryCreate(out var nvidia))
        {
            _backend = nvidia;
            return;
        }

        if (AmdVibrance.TryCreate(out var amd))
        {
            _backend = amd;
        }
    }

    /// <param name="percent">0–100 comme le curseur NVIDIA. 50 = défaut, 100 = max officiel du pilote.</param>
    public bool ApplyPercent(int percent)
    {
        if (_backend is null)
        {
            return false;
        }

        percent = Math.Clamp(percent, 0, 100);
        return _backend.ApplyPercent(percent);
    }

    public void Restore() => _backend?.Restore();

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Restore();
        _backend?.Dispose();
        _disposed = true;
    }
}

internal interface IGpuVibrance : IDisposable
{
    string Name { get; }
    bool ApplyPercent(int percent);
    void Restore();
}

internal sealed class NvidiaVibrance : IGpuVibrance
{
    private const uint IdInitialize = 0x0150E828;
    private const uint IdUnload = 0xD22BDD7E;
    private const uint IdEnumDisplay = 0x9ABDD40D;
    private const uint IdGetDvc = 0x4085DE45;
    private const uint IdGetDvcEx = 0x0E45002D;
    private const uint IdSetDvc = 0x172409B4;
    private const uint IdSetDvcEx = 0x4A82C2B1;
    private const int Ok = 0;
    private const int EndEnumeration = -7;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvStatusFn();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvEnumFn(uint thisEnum, out IntPtr display);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvGetDvcFn(IntPtr display, uint outputId, ref DvcInfo info);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvGetDvcExFn(IntPtr display, uint outputId, ref DvcInfoEx info);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvSetDvcFn(IntPtr display, uint outputId, int level);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvSetDvcExFn(IntPtr display, uint outputId, ref DvcInfoEx info);

    [DllImport("nvapi64.dll", EntryPoint = "nvapi_QueryInterface", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr QueryInterface(uint id);

    [StructLayout(LayoutKind.Sequential)]
    private struct DvcInfo
    {
        public uint Version;
        public int CurrentLevel;
        public int MinLevel;
        public int MaxLevel;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DvcInfoEx
    {
        public uint Version;
        public int CurrentLevel;
        public int MinLevel;
        public int MaxLevel;
        public int DefaultLevel;
    }

    private readonly NvStatusFn? _unload;
    private readonly NvEnumFn _enumDisplay;
    private readonly NvGetDvcFn? _getDvc;
    private readonly NvGetDvcExFn? _getDvcEx;
    private readonly NvSetDvcFn? _setDvc;
    private readonly NvSetDvcExFn? _setDvcEx;
    private readonly List<(IntPtr Display, int Original)> _originals = [];
    private readonly bool _useEx;

    public string Name => "NVIDIA Éclat numérique";

    private NvidiaVibrance(
        NvStatusFn? unload,
        NvEnumFn enumDisplay,
        NvGetDvcFn? getDvc,
        NvGetDvcExFn? getDvcEx,
        NvSetDvcFn? setDvc,
        NvSetDvcExFn? setDvcEx,
        bool useEx)
    {
        _unload = unload;
        _enumDisplay = enumDisplay;
        _getDvc = getDvc;
        _getDvcEx = getDvcEx;
        _setDvc = setDvc;
        _setDvcEx = setDvcEx;
        _useEx = useEx;
        Snapshot();
    }

    public static bool TryCreate(out NvidiaVibrance? result)
    {
        result = null;
        try
        {
            var initPtr = QueryInterface(IdInitialize);
            if (initPtr == IntPtr.Zero)
            {
                return false;
            }

            var initialize = Marshal.GetDelegateForFunctionPointer<NvStatusFn>(initPtr);
            if (initialize() != Ok)
            {
                return false;
            }

            var enumPtr = QueryInterface(IdEnumDisplay);
            if (enumPtr == IntPtr.Zero)
            {
                return false;
            }

            var getEx = QueryInterface(IdGetDvcEx);
            var setEx = QueryInterface(IdSetDvcEx);
            var get = QueryInterface(IdGetDvc);
            var set = QueryInterface(IdSetDvc);
            bool useEx = getEx != IntPtr.Zero && setEx != IntPtr.Zero;

            if (!useEx && (get == IntPtr.Zero || set == IntPtr.Zero))
            {
                return false;
            }

            var unloadPtr = QueryInterface(IdUnload);
            result = new NvidiaVibrance(
                unloadPtr == IntPtr.Zero ? null : Marshal.GetDelegateForFunctionPointer<NvStatusFn>(unloadPtr),
                Marshal.GetDelegateForFunctionPointer<NvEnumFn>(enumPtr),
                get == IntPtr.Zero ? null : Marshal.GetDelegateForFunctionPointer<NvGetDvcFn>(get),
                getEx == IntPtr.Zero ? null : Marshal.GetDelegateForFunctionPointer<NvGetDvcExFn>(getEx),
                set == IntPtr.Zero ? null : Marshal.GetDelegateForFunctionPointer<NvSetDvcFn>(set),
                setEx == IntPtr.Zero ? null : Marshal.GetDelegateForFunctionPointer<NvSetDvcExFn>(setEx),
                useEx);
            return true;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    public bool ApplyPercent(int percent)
    {
        bool any = false;
        for (uint i = 0; ; i++)
        {
            int status = _enumDisplay(i, out var display);
            if (status == EndEnumeration || display == IntPtr.Zero)
            {
                break;
            }

            if (status != Ok)
            {
                continue;
            }

            if (TrySet(display, percent))
            {
                any = true;
            }
        }

        return any;
    }

    public void Restore()
    {
        foreach (var (display, original) in _originals)
        {
            TrySetLevel(display, original);
        }
    }

    public void Dispose() => _unload?.Invoke();

    private void Snapshot()
    {
        for (uint i = 0; ; i++)
        {
            int status = _enumDisplay(i, out var display);
            if (status == EndEnumeration || display == IntPtr.Zero)
            {
                break;
            }

            if (status != Ok)
            {
                continue;
            }

            if (TryRead(display, out _, out _, out _, out int current))
            {
                _originals.Add((display, current));
            }
        }
    }

    private bool TrySet(IntPtr display, int percent)
    {
        if (!TryRead(display, out int min, out int max, out int def, out _))
        {
            return false;
        }

        int level = MapPercent(percent, min, def, max);
        return TrySetLevel(display, level);
    }

    private bool TryRead(IntPtr display, out int min, out int max, out int def, out int current)
    {
        min = 0;
        max = 63;
        def = 0;
        current = 0;

        if (_useEx && _getDvcEx is not null)
        {
            var info = new DvcInfoEx { Version = (uint)Marshal.SizeOf<DvcInfoEx>() | 0x10000 };
            if (_getDvcEx(display, 0, ref info) == Ok)
            {
                min = info.MinLevel;
                max = info.MaxLevel;
                def = info.DefaultLevel;
                current = info.CurrentLevel;
                return true;
            }
        }

        if (_getDvc is not null)
        {
            var info = new DvcInfo { Version = (uint)Marshal.SizeOf<DvcInfo>() | 0x10000 };
            if (_getDvc(display, 0, ref info) == Ok)
            {
                min = info.MinLevel;
                max = info.MaxLevel;
                def = info.MinLevel;
                current = info.CurrentLevel;
                return true;
            }
        }

        return false;
    }

    private bool TrySetLevel(IntPtr display, int level)
    {
        if (_useEx && _setDvcEx is not null && _getDvcEx is not null)
        {
            var info = new DvcInfoEx { Version = (uint)Marshal.SizeOf<DvcInfoEx>() | 0x10000 };
            if (_getDvcEx(display, 0, ref info) != Ok)
            {
                return false;
            }

            info.CurrentLevel = Math.Clamp(level, info.MinLevel, info.MaxLevel);
            return _setDvcEx(display, 0, ref info) == Ok;
        }

        return _setDvc is not null && _setDvc(display, 0, level) == Ok;
    }

    private static int MapPercent(int percent, int min, int def, int max)
    {
        if (percent <= 50)
        {
            float t = percent / 50f;
            return (int)Math.Round(min + (def - min) * t);
        }

        float u = (percent - 50) / 50f;
        return (int)Math.Round(def + (max - def) * u);
    }
}

internal sealed class AmdVibrance : IGpuVibrance
{
    private const int AdlOk = 0;
    private const int ColorSaturation = 1 << 2;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr AllocFn(int size);

    [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int ADL2_Main_Control_Create(AllocFn callback, int enumConnected, out IntPtr context);

    [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int ADL2_Main_Control_Destroy(IntPtr context);

    [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int ADL2_Display_Color_Get(
        IntPtr context,
        int adapterIndex,
        int displayIndex,
        int colorType,
        out int current,
        out int def,
        out int min,
        out int max,
        out int step);

    [DllImport("atiadlxx.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int ADL2_Display_Color_Set(
        IntPtr context,
        int adapterIndex,
        int displayIndex,
        int colorType,
        int current);

    private static IntPtr Alloc(int size) => Marshal.AllocHGlobal(size);

    private readonly IntPtr _context;
    private readonly List<(int Adapter, int Display, int Original)> _targets = [];
    private readonly AllocFn _allocKeepAlive;

    public string Name => "AMD Saturation";

    private AmdVibrance(IntPtr context, AllocFn allocKeepAlive)
    {
        _context = context;
        _allocKeepAlive = allocKeepAlive;
        Discover();
    }

    public static bool TryCreate(out AmdVibrance? result)
    {
        result = null;
        try
        {
            AllocFn alloc = Alloc;
            if (ADL2_Main_Control_Create(alloc, 1, out var context) != AdlOk || context == IntPtr.Zero)
            {
                return false;
            }

            var amd = new AmdVibrance(context, alloc);
            if (amd._targets.Count == 0)
            {
                amd.Dispose();
                return false;
            }

            result = amd;
            return true;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    public bool ApplyPercent(int percent)
    {
        bool any = false;
        foreach (var (adapter, display, _) in _targets)
        {
            if (ADL2_Display_Color_Get(_context, adapter, display, ColorSaturation, out _, out int def, out int min, out int max, out _) != AdlOk)
            {
                continue;
            }

            int level = MapPercent(percent, min, def, max);
            if (ADL2_Display_Color_Set(_context, adapter, display, ColorSaturation, level) == AdlOk)
            {
                any = true;
            }
        }

        return any;
    }

    public void Restore()
    {
        foreach (var (adapter, display, original) in _targets)
        {
            ADL2_Display_Color_Set(_context, adapter, display, ColorSaturation, original);
        }
    }

    public void Dispose()
    {
        if (_context != IntPtr.Zero)
        {
            ADL2_Main_Control_Destroy(_context);
        }

        GC.KeepAlive(_allocKeepAlive);
    }

    private void Discover()
    {
        for (int adapter = 0; adapter < 16; adapter++)
        {
            for (int display = 0; display < 8; display++)
            {
                if (ADL2_Display_Color_Get(_context, adapter, display, ColorSaturation, out int current, out _, out _, out _, out _) == AdlOk)
                {
                    _targets.Add((adapter, display, current));
                }
            }
        }
    }

    private static int MapPercent(int percent, int min, int def, int max)
    {
        if (percent <= 50)
        {
            float t = percent / 50f;
            return (int)Math.Round(min + (def - min) * t);
        }

        float u = (percent - 50) / 50f;
        return (int)Math.Round(def + (max - def) * u);
    }
}
