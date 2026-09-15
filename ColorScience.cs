namespace EclatPlus;

internal static class ColorScience
{
    private const float Gray = 1f / 3f;

    public static float[] Identity()
    {
        return
        [
            1, 0, 0, 0, 0,
            0, 1, 0, 0, 0,
            0, 0, 1, 0, 0,
            0, 0, 0, 1, 0,
            0, 0, 0, 0, 1
        ];
    }

    /// <param name="amount">1 = naturel. 1.5 ≈ max NVIDIA. 2+ = plus pétillant, teinte inchangée.</param>
    public static float[] DigitalVibrance(float amount)
    {
        float inv = 1f - amount;
        float g = Gray * inv;
        float d = g + amount;

        return
        [
            d, g, g, 0, 0,
            g, d, g, 0, 0,
            g, g, d, 0, 0,
            0, 0, 0, 1, 0,
            0, 0, 0, 0, 1
        ];
    }
}
