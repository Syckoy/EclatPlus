using System.Runtime.InteropServices;

namespace EclatPlus;

/// <summary>
/// Détecte un jeu plein écran / bordless (sans barre de titre). Pas de timer :
/// uniquement quand la fenêtre au premier plan change.
/// </summary>
internal sealed class FullscreenGuard : IDisposable
{
    private const uint EventSystemForeground = 0x0003;
    private const uint WineventOutOfContext = 0;
    private const int GwlStyle = -16;
    private const int WsCaption = 0x00C00000;

    private delegate void WinEventDelegate(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(
        uint eventMin,
        uint eventMax,
        IntPtr hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc,
        uint idProcess,
        uint idThread,
        uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left, Top, Right, Bottom;
    }

    private readonly WinEventDelegate _callback;
    private readonly IntPtr _hook;
    private readonly IntPtr _ownHandle;
    private bool _fullscreen;

    public event Action<bool>? FullscreenChanged;

    public bool IsFullscreenGame => _fullscreen;

    public FullscreenGuard(IntPtr ownWindow)
    {
        _ownHandle = ownWindow;
        _callback = OnEvent;
        _hook = SetWinEventHook(
            EventSystemForeground,
            EventSystemForeground,
            IntPtr.Zero,
            _callback,
            0,
            0,
            WineventOutOfContext);
        Refresh();
    }

    public void Refresh() => Update(GetForegroundWindow());

    private void OnEvent(IntPtr _, uint __, IntPtr hwnd, int ___, int ____, uint _____, uint ______)
    {
        if (hwnd == IntPtr.Zero)
        {
            hwnd = GetForegroundWindow();
        }

        Update(hwnd);
    }

    private void Update(IntPtr hwnd)
    {
        bool next = hwnd != IntPtr.Zero && hwnd != _ownHandle && LooksLikeFullscreenGame(hwnd);
        if (next == _fullscreen)
        {
            return;
        }

        _fullscreen = next;
        FullscreenChanged?.Invoke(_fullscreen);
    }

    private static bool LooksLikeFullscreenGame(IntPtr hwnd)
    {
        int style = (int)GetWindowLongPtr(hwnd, GwlStyle).ToInt64();
        if ((style & WsCaption) != 0)
        {
            return false;
        }

        if (!GetWindowRect(hwnd, out var rect))
        {
            return false;
        }

        var screen = Screen.FromHandle(hwnd).Bounds;
        return rect.Left <= screen.Left + 2
               && rect.Top <= screen.Top + 2
               && rect.Right >= screen.Right - 2
               && rect.Bottom >= screen.Bottom - 2;
    }

    public void Dispose()
    {
        if (_hook != IntPtr.Zero)
        {
            UnhookWinEvent(_hook);
        }

        GC.KeepAlive(_callback);
    }
}
