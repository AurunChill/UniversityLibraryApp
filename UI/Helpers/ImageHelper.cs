using System.Drawing;

namespace LibraryApp.UI.Helpers;

public static class ImageHelper
{
    public static string SaveCover(string sourcePath)
    {
        string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "AppData", "Media", "Covers");
        Directory.CreateDirectory(dir);
        string fileName = Guid.NewGuid() + Path.GetExtension(sourcePath);
        using var original = Image.FromFile(sourcePath);
        const int width = 260;
        int newHeight = (int)(original.Height * width / (float)original.Width);
        using var resized = new Bitmap(original, new Size(width, newHeight));
        using var finalImg = new Bitmap(width, 420);
        using var g = Graphics.FromImage(finalImg);
        g.Clear(Color.White);
        if (newHeight > 420)
        {
            int crop = (newHeight - 420) / 2;
            g.DrawImage(resized, new Rectangle(0, 0, width, 420),
                new Rectangle(0, crop, width, 420), GraphicsUnit.Pixel);
        }
        else
        {
            int offset = (420 - newHeight) / 2;
            g.DrawImage(resized, 0, offset);
        }
        finalImg.Save(Path.Combine(dir, fileName));
        return fileName;
    }
}
