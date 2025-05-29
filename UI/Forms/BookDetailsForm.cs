using System.Drawing.Drawing2D;
using LibraryApp.Data.Models;

namespace LibraryApp.UI.Forms;

public sealed class BookDetailForm : Form
{
    private readonly Book _book;
    private readonly Rectangle _screen = Screen.PrimaryScreen!.Bounds;

    public BookDetailForm(Book book)
    {
        _book = book;
        BuildUI();
    }

    // ---------- UI ----------
    private void BuildUI()
    {
        Text = _book.Title;
        MinimumSize = new Size(900, 700);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(24, 24, 28);
        ForeColor = Color.Gainsboro;
        Font = new Font("Segoe UI", 10);

        // обложка
        string imgPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "AppData", "Media", "Covers", _book.CoverUrl
        );
        var cover = new PictureBox
        {
            Left = 40,
            Top = 40,
            Width = 260,
            Height = 420,
            BackColor = Color.FromArgb(40, 40, 46),
            SizeMode = PictureBoxSizeMode.Zoom,
            Image = File.Exists(imgPath) ? Image.FromFile(imgPath) : null
        };
        Round(cover, 12);

        // панель с текстом
        var details = new Panel
        {
            Left = cover.Right + 40,
            Top = 40,
            Width = 500,
            Height = _screen.Height,
            BackColor = Color.Transparent
        };

        int y = 0;
        details.Controls.Add(MakeLabel(_book.Title, 24, FontStyle.Bold, ref y, Color.White));
        details.Controls.Add(MakeLabel($"Автор: {_book.Author}", 12, FontStyle.Regular, ref y));
        details.Controls.Add(MakeLabel($"Год: {_book.PublishYear}", 12, FontStyle.Regular, ref y));
        details.Controls.Add(MakeLabel($"Издатель: {_book.Publisher}", 12, FontStyle.Regular, ref y));
        details.Controls.Add(MakeLabel($"Страниц: {_book.Pages}", 12, FontStyle.Regular, ref y));
        details.Controls.Add(MakeLabel($"ISBN: {_book.ISBN}", 12, FontStyle.Regular, ref y));
        details.Controls.Add(MakeLabel($"Язык: {_book.Language}", 12, FontStyle.Regular, ref y));
        details.Controls.Add(MakeLabel($"Жанр: {_book.Genre}", 12, FontStyle.Regular, ref y));
        details.Controls.Add(MakeLabel($"Находится: {_book.Location}", 12, FontStyle.Regular, ref y));

        var descBox = new Panel
        {
            Top = y + 10,
            Width = details.Width,
            Height = 240,
            AutoScroll = true,
            BackColor = Color.Transparent
        };

        var descLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(details.Width - 12, 0), // ширина – по панели, высота – сколько нужно
            Text = _book.Description ?? "Описание отсутствует",
            ForeColor = Color.Gainsboro,
            Font = new Font("Segoe UI", 10)
        };

        descBox.Controls.Add(descLabel);
        details.Controls.Add(descBox);

        Controls.AddRange([cover, details]);
    }

    // ---------- helpers ----------
    private Label MakeLabel(string text, int size, FontStyle style,
                            ref int y, Color? color = null)
    {
        var lbl = new Label
        {
            Text = text,
            AutoSize = true,
            Left = 0,
            Top = y,
            Font = new Font("Segoe UI", size, style),
            ForeColor = color ?? Color.Gainsboro
        };

        lbl.Height = lbl.PreferredHeight;
        y += lbl.Height + 8;

        return lbl;
    }

    private static void Round(Control c, int r)
    {
        var p = new GraphicsPath();
        var rc = c.ClientRectangle;
        p.AddArc(rc.X, rc.Y, r, r, 180, 90);
        p.AddArc(rc.Right - r, rc.Y, r, r, 270, 90);
        p.AddArc(rc.Right - r, rc.Bottom - r, r, r, 0, 90);
        p.AddArc(rc.X, rc.Bottom - r, r, r, 90, 90);
        p.CloseFigure();
        c.Region = new Region(p);
    }
}
