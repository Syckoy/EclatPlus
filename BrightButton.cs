using System.Drawing.Drawing2D;

namespace EclatPlus;

internal sealed class BrightButton : Button
{
    private bool _hover;
    private bool _press;

    public BrightButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        ForeColor = Color.FromArgb(28, 18, 8);
        BackColor = Color.FromArgb(255, 204, 70);
        Font = new Font("Segoe UI Semibold", 11f);
        Height = 46;
        Size = new Size(220, 46);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        _press = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        _press = true;
        Invalidate();
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _press = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? Color.FromArgb(14, 12, 20));

        var fill = _press
            ? Color.FromArgb(255, 230, 140)
            : _hover
                ? Color.FromArgb(255, 220, 100)
                : Color.FromArgb(255, 204, 70);

        var rect = new Rectangle(1, 1, Width - 3, Height - 3);
        using var path = Round(rect, 10);
        using var brush = new SolidBrush(fill);
        using var border = new Pen(Color.FromArgb(255, 248, 210), 2.4f);
        using var textBrush = new SolidBrush(Color.FromArgb(28, 18, 8));
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        e.Graphics.FillPath(brush, path);
        e.Graphics.DrawPath(border, path);
        e.Graphics.DrawString(Text, Font, textBrush, rect, format);
    }

    private static GraphicsPath Round(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
