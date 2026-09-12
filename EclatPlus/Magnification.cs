using System.Runtime.InteropServices;

namespace EclatPlus;

internal static class Magnification
{
    [StructLayout(LayoutKind.Sequential)]
    private struct MagColorEffect
    {
        public float M00, M01, M02, M03, M04;
        public float M10, M11, M12, M13, M14;
        public float M20, M21, M22, M23, M24;
        public float M30, M31, M32, M33, M34;
        public float M40, M41, M42, M43, M44;

        public static MagColorEffect FromRowMajor(float[] m) => new()
        {
            M00 = m[0],  M01 = m[1],  M02 = m[2],  M03 = m[3],  M04 = m[4],
            M10 = m[5],  M11 = m[6],  M12 = m[7],  M13 = m[8],  M14 = m[9],
            M20 = m[10], M21 = m[11], M22 = m[12], M23 = m[13], M24 = m[14],
            M30 = m[15], M31 = m[16], M32 = m[17], M33 = m[18], M34 = m[19],
            M40 = m[20], M41 = m[21], M42 = m[22], M43 = m[23], M44 = m[24]
        };
    }

    [DllImport("Magnification.dll", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool MagInitialize();

    [DllImport("Magnification.dll", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool MagUninitialize();

    [DllImport("Magnification.dll", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool MagSetFullscreenColorEffect(ref MagColorEffect effect);

    public static bool Available { get; private set; }

    public static bool Initialize()
    {
        Available = MagInitialize();
        return Available;
    }

    public static void Shutdown()
    {
        if (!Available)
        {
            return;
        }

        Apply(ColorScience.Identity());
        MagUninitialize();
        Available = false;
    }

    public static bool Apply(float[] matrix)
    {
        if (!Available)
        {
            return false;
        }

        var effect = MagColorEffect.FromRowMajor(matrix);
        return MagSetFullscreenColorEffect(ref effect);
    }
}
