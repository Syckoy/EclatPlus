using System.Drawing.Drawing2D;

namespace EclatPlus;

internal static class LogoArt
{
    /// <summary>
    /// Croix latine du Christ : hampe plus longue, traverse haute. Pas un « plus », pas un X.
    /// </summary>
    public static void DrawLatinCross(Graphics g, RectangleF box, Color fill, Color edge)
    {
        float w = box.Width;
        float h = box.Height;
        float cx = box.X + w / 2f;
        float beam = Math.Max(2.2f, w * 0.16f);
        float arm = w * 0.46f;
        float top = box.Y;
        float bottom = box.Bottom;
        // Traverse dans le tiers supérieur, comme une croix de Calvaire.
        float crossY = box.Y + h * 0.28f;

        using var path = new GraphicsPath();
        path.AddRectangle(new RectangleF(cx - beam / 2f, top, beam, bottom - top));
        path.AddRectangle(new RectangleF(cx - arm, crossY - beam / 2f, arm * 2f, beam));

        using var brush = new SolidBrush(fill);
        using var pen = new Pen(edge, 1.1f) { LineJoin = LineJoin.Round };
        g.FillPath(brush, path);
        g.DrawPath(pen, path);
    }

    public static void DrawHeart(Graphics g, RectangleF box, Color fill, Color edge)
    {
        using var path = HeartPath(box);
        using var brush = new SolidBrush(fill);
        using var pen = new Pen(edge, Math.Max(1.2f, box.Width / 16f))
        {
            LineJoin = LineJoin.Round
        };
        g.FillPath(brush, path);
        g.DrawPath(pen, path);
    }

    public static GraphicsPath HeartPath(RectangleF r)
    {
        var path = new GraphicsPath();
        float x = r.X;
        float y = r.Y;
        float w = r.Width;
        float h = r.Height;
        path.AddBezier(
            x + w / 2f, y + h * 0.32f,
            x - w * 0.05f, y - h * 0.18f,
            x - w * 0.08f, y + h * 0.58f,
            x + w / 2f, y + h);
        path.AddBezier(
            x + w / 2f, y + h,
            x + w * 1.08f, y + h * 0.58f,
            x + w * 1.05f, y - h * 0.18f,
            x + w / 2f, y + h * 0.32f);
        path.CloseFigure();
        return path;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr handle);

    public static Icon CreateTrayIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            DrawLatinCross(g, new RectangleF(3, 3, 14, 26), Color.FromArgb(255, 214, 90), Color.FromArgb(80, 50, 10));
            DrawHeart(g, new RectangleF(16, 9, 13, 13), Color.FromArgb(220, 40, 70), Color.FromArgb(90, 10, 25));
        }

        var handle = bmp.GetHicon();
        using var temp = Icon.FromHandle(handle);
        var icon = (Icon)temp.Clone();
        DestroyIcon(handle);
        return icon;
    }
}
