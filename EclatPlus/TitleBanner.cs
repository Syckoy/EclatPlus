using System.Drawing.Drawing2D;

namespace EclatPlus;

internal sealed class TitleBanner : Control
{
    public TitleBanner()
    {
        DoubleBuffered = true;
        Height = 78;
        BackColor = Color.FromArgb(14, 12, 20);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(BackColor);

        LogoArt.DrawLatinCross(g, new RectangleF(16, 10, 22, 52), Color.FromArgb(255, 214, 90), Color.FromArgb(90, 55, 8));
        LogoArt.DrawHeart(g, new RectangleF(42, 22, 22, 22), Color.FromArgb(230, 45, 78), Color.FromArgb(110, 16, 32));

        using var nameFont = new Font("Segoe UI Semibold", 28f, FontStyle.Bold);
        using var nameBrush = new SolidBrush(Color.FromArgb(255, 236, 210));
        g.DrawString("Yeshua", nameFont, nameBrush, 70, 6);

        using var subFont = new Font("Segoe UI", 10f);
        using var subBrush = new SolidBrush(Color.FromArgb(210, 190, 140));
        g.DrawString("Éclat numérique  ·  couleurs plus riches, teinte intacte", subFont, subBrush, 72, 50);
    }
}
