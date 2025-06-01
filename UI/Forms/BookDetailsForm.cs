using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using LibraryApp.Data.Models;

namespace LibraryApp.UI.Forms
{
    public sealed class BookDetailForm : Form
    {
        private Book _book;
        private PictureBox _cover = null!;
        private readonly Rectangle _screen = Screen.PrimaryScreen!.Bounds;

        public BookDetailForm(Book book)
        {
            _book = book;
            BuildUI();
        }

        private void BuildUI()
        {
            Text = _book.Title;
            MinimumSize = new Size(900, 700);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(24, 24, 28);
            ForeColor = Color.Gainsboro;
            Font = new Font("Segoe UI", 10);

            string imgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "AppData", "Media", "Covers", _book.CoverUrl);

            _cover = new PictureBox
            {
                Left = 40,
                Top = 40,
                Width = 260,
                Height = 420,
                BackColor = Color.FromArgb(40, 40, 46),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            if (File.Exists(imgPath)) _cover.Image = LoadImageUnlocked(imgPath);

            Round(_cover, 12);
            Controls.Add(_cover);

            var details = new Panel
            {
                Left = _cover.Right + 40,
                Top = 40,
                Width = 500,
                Height = _screen.Height,
                BackColor = Color.Transparent
            };
            Controls.Add(details);

            int y = 0;
            details.Controls.Add(Make(details, _book.Title, 24, FontStyle.Bold, ref y, Color.White));
            details.Controls.Add(Make(details, $"Автор: {_book.Author}", 12, 0, ref y));
            details.Controls.Add(Make(details, $"Год: {_book.PublishYear}", 12, 0, ref y));
            details.Controls.Add(Make(details, $"Издатель: {_book.Publisher}", 12, 0, ref y));
            details.Controls.Add(Make(details, $"Страниц: {_book.Pages}", 12, 0, ref y));
            details.Controls.Add(Make(details, $"ISBN: {_book.ISBN}", 12, 0, ref y));
            details.Controls.Add(Make(details, $"Язык: {_book.Language}", 12, 0, ref y));
            details.Controls.Add(Make(details, $"Жанр: {_book.Genre}", 12, 0, ref y));
            details.Controls.Add(Make(details, $"Находится: {_book.Location}", 12, 0, ref y));
            details.Controls.Add(Make(details, $"Количество: {_book.Amount}", 12, 0, ref y));

            var descBox = new Panel { Top = y + 10, Width = details.Width, Height = 240, AutoScroll = true };
            var descLbl = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(details.Width - 12, 0),
                Text = _book.Description ?? "Описание отсутствует",
                ForeColor = Color.Gainsboro,
                Font = new Font("Segoe UI", 10)
            };
            descBox.Controls.Add(descLbl);
            details.Controls.Add(descBox);

            var btnDel = Btn("Удалить", _cover.Left, _cover.Bottom + 15,
                             Color.FromArgb(232, 63, 63), async (_, __) => await DeleteBookAsync());

            var btnEdit = Btn("Обновить", btnDel.Right + 10, btnDel.Top,
                              Color.FromArgb(98, 0, 238), (_, __) => EditBook());

            Controls.AddRange(new Control[] { btnDel, btnEdit });
        }

        private static Button Btn(string t, int l, int t2, Color bg, EventHandler h)
        {
            var b = new Button
            {
                Text = t,
                Left = l,
                Top = t2,
                Width = 110,
                Height = 36,
                BackColor = bg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            b.Click += h;
            return b;
        }

        private static Label Make(Control parent, string txt, int sz, FontStyle st, ref int y, Color? c = null)
        {
            var lbl = new Label
            {
                Text = txt,
                AutoSize = true,
                Left = 0,
                Top = y,
                Font = new Font("Segoe UI", sz, st),
                ForeColor = c ?? Color.Gainsboro
            };
            y += lbl.PreferredHeight + 8;
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

        private static Image? LoadImageUnlocked(string path)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                using var img = Image.FromStream(fs);
                return (Image)img.Clone();
            }
            catch { return null; }
        }

        private async Task DeleteBookAsync()
        {
            if (MessageBox.Show("Удалить книгу?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            if (_cover.Image is not null) { _cover.Image.Dispose(); _cover.Image = null; }

            if (_book.CoverUrl != "no_cover.png")
            {
                var p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                      "AppData", "Media", "Covers", _book.CoverUrl);
                TryDeleteFile(p);
            }

            await Books.DeleteAsync(_book.BookId);
            Close();
        }

        private void EditBook()
        {
            using var dlg = new BookEditDialog(_book);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            _book = dlg.ResultBook!;
            Controls.Clear();
            BuildUI();
        }

        private static void TryDeleteFile(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }
    }

    internal sealed class BookEditDialog : Form
    {
        private readonly Book _orig;
        public Book? ResultBook { get; private set; }

        TextBox tTitle = new(), tAuthor = new(), tIsbn = new(), tDesc = new() { Multiline = true, Height = 60 };
        NumericUpDown nYear = new() { Minimum = 0, Maximum = 3000 }, nPages = new() { Maximum = 10000 };
        TextBox tGenre = new(), tPublisher = new(), tLang = new(), tLoc = new(), tCover = new() { ReadOnly = true };
        Button btnBrowse = new() { Text = "Файл...", Height = 45 };

        public BookEditDialog(Book b) { _orig = b; BuildUI(); }

        void BuildUI()
        {
            Text = "Редактирование";
            Size = new Size(520, 530);
            BackColor = Color.FromArgb(40, 40, 46);
            ForeColor = Color.Gainsboro;
            Font = new Font("Segoe UI", 10);
            StartPosition = FormStartPosition.CenterParent;

            int y = 20;
            Controls.AddRange(new Control[]
            {
                lbl("Название").At(20, y), tTitle.At(140, y - 3, 350),
                lbl("Автор").At(20, y += 35), tAuthor.At(140, y - 3, 350),
                lbl("ISBN").At(20, y += 35), tIsbn.At(140, y - 3, 350),
                lbl("Год").At(20, y += 35), nYear.At(140, y - 3, 80),
                lbl("Страниц").At(250, y), nPages.At(330, y - 3, 80),
                lbl("Жанр").At(20, y += 35), tGenre.At(140, y - 3, 350),
                lbl("Издатель").At(20, y += 35), tPublisher.At(140, y - 3, 350),
                lbl("Язык").At(20, y += 35), tLang.At(140, y - 3, 150),
                lbl("Локация").At(20, y += 35), tLoc.At(140, y - 3, 350),
                lbl("Описание").At(20, y += 35), tDesc.At(140, y - 3, 350),
                lbl("Обложка").At(20, y += tDesc.Height + 10), tCover.At(140, y - 3, 260), btnBrowse.At(410, y - 4, 80)
            });

            tTitle.Text = _orig.Title;
            tAuthor.Text = _orig.Author;
            tIsbn.Text = _orig.ISBN;
            nYear.Value = _orig.PublishYear;
            nPages.Value = _orig.Pages;
            tGenre.Text = _orig.Genre;
            tPublisher.Text = _orig.Publisher;
            tLang.Text = _orig.Language;
            tLoc.Text = _orig.Location;
            tDesc.Text = _orig.Description;

            btnBrowse.Click += (_, __) =>
            {
                using var od = new OpenFileDialog { Filter = "Images|*.jpg;*.png;*.jpeg;*.bmp" };
                if (od.ShowDialog(this) == DialogResult.OK)
                    tCover.Text = od.FileName;
            };

            var ok = new Button
            {
                Text = "Сохранить",
                Left = Width / 2 - 80,
                Top = tCover.Bottom + 15,
                Width = 160,
                Height = 50,
                DialogResult = DialogResult.OK,
                BackColor = Color.FromArgb(98, 0, 238),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            Controls.Add(ok);

            ok.Click += async (_, __) =>
            {
                if (string.IsNullOrWhiteSpace(tTitle.Text))
                {
                    MessageBox.Show("Название не может быть пустым");
                    DialogResult = DialogResult.None;
                    return;
                }
                var book = _orig;
                book.Title = tTitle.Text;
                book.Author = tAuthor.Text;
                book.ISBN = tIsbn.Text;
                book.PublishYear = (int)nYear.Value;
                book.Pages = (int)nPages.Value;
                book.Genre = tGenre.Text;
                book.Publisher = tPublisher.Text;
                book.Language = tLang.Text;
                book.Location = tLoc.Text;
                book.Description = tDesc.Text;

                string oldCover = book.CoverUrl;
                if (!string.IsNullOrWhiteSpace(tCover.Text) && File.Exists(tCover.Text))
                {
                    string slug = MakeSlug(book.Title);
                    string newName = $"{book.BookId}_{slug}.jpg";
                    string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppData", "Media", "Covers");
                    Directory.CreateDirectory(dir);
                    string dest = Path.Combine(dir, newName);

                    ResizeCrop260x320(tCover.Text, dest);
                    book.CoverUrl = newName;

                    if (oldCover != "no_cover.png" && oldCover != newName)
                    {
                        string oldPath = Path.Combine(dir, oldCover);
                        TryDeleteFile(oldPath);
                    }
                }
                await Books.UpdateAsync(book);
                ResultBook = book;
            };
        }

        private static Label lbl(string t) => new() { Text = t, AutoSize = true };

        private static string MakeSlug(string s) => new(s.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());

        private static void ResizeCrop260x320(string src, string dest)
        {
            using var img = Image.FromFile(src);
            int nw = 260;
            int nh = (int)(img.Height * (nw / (double)img.Width));
            using var bmp = new Bitmap(nw, nh);
            using (var g = Graphics.FromImage(bmp))
            { g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.DrawImage(img, 0, 0, nw, nh); }
            using var final = new Bitmap(260, 320);
            using (var g = Graphics.FromImage(final))
            {
                g.Clear(Color.White);
                if (nh >= 320)
                {
                    int yOff = (nh - 320) / 2;
                    g.DrawImage(bmp, new Rectangle(0, 0, 260, 320),
                                        new Rectangle(0, yOff, 260, 320),
                                        GraphicsUnit.Pixel);
                }
                else
                {
                    int yPad = (320 - nh) / 2;
                    g.DrawImage(bmp, 0, yPad);
                }
            }
            final.Save(dest, ImageFormat.Jpeg);
        }

        private static void TryDeleteFile(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }
    }

    file static class CtlExt
    {
        public static T At<T>(this T c, int x, int y, int? w = null) where T : Control
        {
            c.Left = x;
            c.Top = y;
            if (w.HasValue)
                c.Width = w.Value;
            return c;
        }
    }
}