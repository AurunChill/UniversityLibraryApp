using System.Drawing.Drawing2D;

namespace LibraryApp.UI.Helpers;

public static class ControlExtensions
{
    public static void RoundCorners(this Control c, int radius, bool topOnly = false)
    {
        var path = new GraphicsPath();
        var rect = c.ClientRectangle;
        if (topOnly)
        {
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddLine(rect.Right, rect.Y + radius, rect.Right, rect.Bottom);
            path.AddLine(rect.Right, rect.Bottom, rect.X, rect.Bottom);
            path.AddLine(rect.X, rect.Bottom, rect.X, rect.Y + radius);
        }
        else
        {
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
        }
        path.CloseFigure();
        c.Region = new Region(path);
    }

    public static void Round(this Control c, int radius)
    {
        var p = new GraphicsPath();
        var rc = c.ClientRectangle;
        p.AddArc(rc.X, rc.Y, radius, radius, 180, 90);
        p.AddArc(rc.Right - radius, rc.Y, radius, radius, 270, 90);
        p.AddArc(rc.Right - radius, rc.Bottom - radius, radius, radius, 0, 90);
        p.AddArc(rc.X, rc.Bottom - radius, radius, radius, 90, 90);
        p.CloseFigure();
        c.Region = new Region(p);
    }

    public static T At<T>(this T control, int x, int y, int? width = null) where T : Control
    {
        control.Left = x;
        control.Top = y;
        if (width.HasValue)
            control.Width = width.Value;
        return control;
    }
}
