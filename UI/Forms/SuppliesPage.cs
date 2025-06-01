using LibraryApp.Data.Models;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace LibraryApp.UI.Forms;

public sealed class SuppliesPage : Form
{
    private readonly BindingSource _bs = new();
    private DataGridView _grid = null!;
    private string _sortColumn = nameof(Supply.SupplyId);
    private SortOrder _sortOrder = SortOrder.Ascending;

    public SuppliesPage()
    {
        BuildUI();
    }

    private TextBox _txtSearchSupplies = null!;

    private void BuildUI()
    {
        Text = "Поставки / Списание / Долги";
        MinimumSize = new Size(1000, 800);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(24, 24, 28);
        Font = new Font("Segoe UI", 10);
        ForeColor = Color.Gainsboro;

        var header = new Label
        {
            Text = Text,
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Left = 20,
            Top = 20
        };
        Controls.Add(header);

        // ── Строка поиска для Supplies ──
        _txtSearchSupplies = new TextBox
        {
            PlaceholderText = "  Поиск по книге или типу операции…",
            Left = 20,
            Top = header.Bottom + 35,
            Width = ClientSize.Width - 40,  // можно подогнать
            Height = 30
        };
        Controls.Add(_txtSearchSupplies);
        _txtSearchSupplies.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _txtSearchSupplies.TextChanged += async (_, __) => await RefreshGrid();

        var clickToSortLabel = new Label
        {
            Text = "Нажмите на заголовок колонки для сортировки",
            Left = 20,
            Top = _txtSearchSupplies.Bottom + 10,
            AutoSize = true
        };
        Controls.Add(clickToSortLabel);

        // ── Сам грид ──
        _grid = new DataGridView
        {
            Left = 20,
            Top = clickToSortLabel.Bottom + 10,
            Width = ClientSize.Width - 40,
            Height = ClientSize.Height - header.Bottom - 200,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackgroundColor = Color.FromArgb(40, 40, 46),
            ForeColor = Color.Gainsboro,
            GridColor = Color.DimGray,
            AutoGenerateColumns = true,
            ReadOnly = true,
            DataSource = _bs
        };
        _grid.ColumnHeaderMouseClick += Grid_ColumnHeaderMouseClick!;

        var darkCell = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(40, 40, 46),
            ForeColor = Color.Gainsboro,
            SelectionBackColor = Color.FromArgb(98, 0, 238),
            SelectionForeColor = Color.White
        };

        _grid.DefaultCellStyle = darkCell;
        _grid.RowsDefaultCellStyle = darkCell;
        _grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle(darkCell)
        {
            BackColor = Color.FromArgb(32, 32, 38)
        };

        _grid.RowHeadersDefaultCellStyle = new DataGridViewCellStyle(darkCell)
        {
            BackColor = Color.FromArgb(40, 40, 46)
        };

        _grid.BorderStyle = BorderStyle.None;
        _grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

        var btnAdd = MakeButton("Добавить", _grid.Left, _grid.Bottom + 15,
                                async (_, __) => { if (AddSupply()) await RefreshGrid(); });
        var btnDel = MakeButton("Удалить", btnAdd.Right + 10, _grid.Bottom + 15,
                                async (_, __) => { if (await DeleteSupply()) await RefreshGrid(); });

        Controls.AddRange(new Control[] { header, _txtSearchSupplies, _grid, btnAdd, btnDel });

        Shown += async (_, __) => await RefreshGrid();
    }

    private async void Grid_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
    {
        var column = _grid.Columns[e.ColumnIndex];
        var propName = column.DataPropertyName;

        if (_sortColumn == propName)
            _sortOrder = (_sortOrder == SortOrder.Ascending)
                            ? SortOrder.Descending
                            : SortOrder.Ascending;
        else
        {
            _sortColumn = propName;
            _sortOrder = SortOrder.Ascending;
        }

        await RefreshGrid();
    }



    private Button MakeButton(string txt, int l, int t, EventHandler onClick)
    {
        var button = new Button
        {
            Text = txt,
            Left = l,
            Top = t,
            Width = 110,
            Height = 36,
            BackColor = Color.FromArgb(98, 0, 238),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Cursor = Cursors.Hand,
        };
        button.Click += onClick;

        return button;
    }


    private async Task RefreshGrid()
    {
        var all = await Supplies.GetAllAsync(null, 0);
        string filter = _txtSearchSupplies.Text.Trim().ToLower();
        if (!string.IsNullOrEmpty(filter))
        {
            all = all
                .Where(s =>
                    (s.Book?.Title?.ToLower().Contains(filter) ?? false)
                    || (s.OperationType.ToLower().Contains(filter)))
                .ToList();
        }

        // ── Блок сортировки ──
        if (!string.IsNullOrEmpty(_sortColumn) && _sortOrder != SortOrder.None)
        {
            switch (_sortColumn)
            {
                case nameof(Supply.SupplyId):
                    all = (_sortOrder == SortOrder.Ascending)
                        ? all.OrderBy(s => s.SupplyId).ToList()
                        : all.OrderByDescending(s => s.SupplyId).ToList();
                    break;
                case "Book": // потому что в анонимном объекте свойство называется именно Book
                    all = (_sortOrder == SortOrder.Ascending)
                        ? all.OrderBy(s => s.Book!.Title).ToList()
                        : all.OrderByDescending(s => s.Book!.Title).ToList();
                    break;
                case nameof(Supply.Date):
                    all = (_sortOrder == SortOrder.Ascending)
                        ? all.OrderBy(s => s.Date).ToList()
                        : all.OrderByDescending(s => s.Date).ToList();
                    break;
                case "Type": // в проекции Type = s.OperationType
                    all = (_sortOrder == SortOrder.Ascending)
                        ? all.OrderBy(s => s.OperationType).ToList()
                        : all.OrderByDescending(s => s.OperationType).ToList();
                    break;
                case nameof(Supply.Amount):
                    all = (_sortOrder == SortOrder.Ascending)
                        ? all.OrderBy(s => s.Amount).ToList()
                        : all.OrderByDescending(s => s.Amount).ToList();
                    break;
            }
        }
        // ── Конец блока сортировки ──

        _bs.DataSource = all
            .Select(s => new
            {
                s.SupplyId,
                Book = s.Book?.Title ?? $"#{s.BookId}",
                s.Date,
                Type = s.OperationType,
                s.Amount
            })
            .ToList();
    }


    private async Task<bool> DeleteSupply()
    {
        if (_grid.CurrentRow?.Cells["SupplyId"].Value is not long id) return false;

        if (MessageBox.Show("Удалить запись?", "Подтверждение",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question) != DialogResult.Yes) return false;

        return await Supplies.DeleteAsync(id);
    }

    private bool AddSupply()
    {
        using var dlg = new SupplyAddDialog();
        return dlg.ShowDialog(this) == DialogResult.OK;
    }
}

internal sealed class SupplyAddDialog : Form
{
    /* ────────── общие элементы ────────── */
    private readonly CheckBox _chkNew = new() { Text = "Новая книга?", AutoSize = true };
    private readonly TextBox _txtSearch = new() { PlaceholderText = "  Поиск книги..." };
    private readonly ListBox _lstBooks = new() { Height = 90 };
    private readonly ComboBox _cbType = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _numAmt = new() { Minimum = -1000, Maximum = 9999, Value = 1 };
    private readonly DateTimePicker _dt = new() { Value = DateTime.Today };

    /* ────────── поля новой книги ───────── */
    private readonly TextBox _tTitle = new();
    private readonly TextBox _tAuthor = new();
    private readonly TextBox _tIsbn = new();
    private readonly NumericUpDown _tYear = new() { Minimum = 0, Maximum = (uint)DateTime.Now.Year, Value = (uint)DateTime.Now.Year };
    private readonly NumericUpDown _tPages = new() { Minimum = 1, Maximum = 10000 };
    private readonly TextBox _tGenre = new() { Text = "Не указано" };
    private readonly TextBox _tPublisher = new() { Text = "Не указано" };
    private readonly TextBox _tLanguage = new() { Text = "ru" };
    private readonly TextBox _tLocation = new() { Text = "Основной зал" };
    private readonly TextBox _tDescription = new() { Multiline = true, Height = 60, ScrollBars = ScrollBars.Vertical };
    private readonly TextBox _tCoverPath = new() { ReadOnly = true, BackColor = SystemColors.Window };
    private readonly Button _btnBrowse = new() { Text = "Файл…", Height = 30 };

    private Book? _selectedBook;

    public SupplyAddDialog() => BuildUI();

    /*──────────────────── UI ────────────────────*/
    private async void BuildUI()
    {
        Text = "Новая операция";
        Size = new Size(560, 1000);
        MinimumSize = Size;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(40, 40, 46);
        ForeColor = Color.Gainsboro;
        Font = new Font("Segoe UI", 10);

        _cbType.Items.AddRange(Enum.GetValues(typeof(OperationType)).Cast<object>().ToArray());

        int y = 20;
        Controls.AddRange([
            _chkNew  .At(20, y),

            new Label{Text="Книга", AutoSize=true}.At(20, y+=40),
            _txtSearch.At(150, y-3, 380),
            _lstBooks .At(150, y+=30, 380),

            new Label{Text="Операция", AutoSize=true}.At(20, y+=_lstBooks.Height+10),
            _cbType   .At(150, y-3, 200),

            new Label{Text="Количество", AutoSize=true}.At(20, y+=40),
            _numAmt   .At(150, y-3, 100),

            new Label{Text="Дата", AutoSize=true}.At(20, y+=40),
            _dt       .At(150, y-3, 200)
        ]);

        /* ── панель новой книги ── */
        Panel p = new() { Left = 20, Top = y += 50, Width = 510, Height = 500, Visible = false };
        int y2 = 0;
        p.Controls.AddRange([
            lbl("Название").At(0,y2),          _tTitle     .At(140,y2-3,350),
            lbl("Автор").At(0,y2+=35),         _tAuthor    .At(140,y2-3,350),
            lbl("ISBN").At(0,y2+=35),          _tIsbn      .At(140,y2-3,350),
            lbl("Год").At(0,y2+=35),           _tYear      .At(140,y2-3,80),
            lbl("Страниц").At(250,y2),         _tPages     .At(330,y2-3,80),
            lbl("Жанр").At(0,y2+=35),          _tGenre     .At(140,y2-3,350),
            lbl("Издатель").At(0,y2+=35),      _tPublisher .At(140,y2-3,350),
            lbl("Язык").At(0,y2+=35),          _tLanguage  .At(140,y2-3,150),
            lbl("Локация").At(0,y2+=35),       _tLocation  .At(140,y2-3,350),
            lbl("Описание").At(0,y2+=35),      _tDescription.At(140,y2-3,350),
            lbl("Обложка").At(0,y2+=_tDescription.Height+10),
            _tCoverPath.At(140,y2-3,260),      _btnBrowse  .At(410,y2-4,80)
        ]);
        Controls.Add(p);

        /* ── кнопка OK ── */
        var btnOk = new Button
        {
            Text = "Сохранить",
            DialogResult = DialogResult.OK,
            Left = Width / 2 - 80,
            Top = p.Bottom + 15,
            Width = 160,
            Height = 54,
            BackColor = Color.FromArgb(98, 0, 238),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        Controls.Add(btnOk);

        /* ── события ── */
        _chkNew.CheckedChanged += (_, __) => SwitchMode(p);
        _txtSearch.TextChanged += async (_, __) => await UpdateSearchAsync();
        _lstBooks.SelectedIndexChanged += (_, __) => _selectedBook = _lstBooks.SelectedItem as Book;
        _btnBrowse.Click += ChooseCover;

        _lstBooks.DataSource = (await Books.GetAllAsync(null, 0)).ToList();
        _lstBooks.DisplayMember = "Title";
        _cbType.SelectedIndex = 0;

        btnOk.Click += async (_, __) =>
        {
            try
            {
                if (_chkNew.Checked) await SaveNewBookAsync();
                else await SaveExistingBookAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.None;
            }
        };
    }

    /*──────────── helpers UI ────────────*/
    private void SwitchMode(Control pane)
    {
        bool n = _chkNew.Checked;
        pane.Visible = n;
        _txtSearch.Visible = _lstBooks.Visible = !n;
        _cbType.Enabled = !n;
        if (n) _cbType.SelectedItem = OperationType.Приход;
    }

    private async Task UpdateSearchAsync()
    {
        var list = await Books.FullTextAsync(_txtSearch.Text);
        _lstBooks.DataSource = list;
        _lstBooks.DisplayMember = "Title";
        _selectedBook = null;
    }

    private void ChooseCover(object? s, EventArgs e)
    {
        using var od = new OpenFileDialog
        {
            Title = "Выберите изображение обложки",
            Filter = "Картинки|*.jpg;*.jpeg;*.png;*.bmp|Все файлы|*.*"
        };
        if (od.ShowDialog(this) == DialogResult.OK)
            _tCoverPath.Text = od.FileName;
    }

    /*──────────── сохранение ────────────*/
    private async Task SaveExistingBookAsync()
    {
        if (_selectedBook is null)
            throw new InvalidOperationException("Выберите книгу.");

        var s = new Supply
        {
            BookId = _selectedBook.BookId,
            OperationType = ((OperationType)_cbType.SelectedItem!).Text(),
            Amount = (int)_numAmt.Value,
            Date = DateOnly.FromDateTime(_dt.Value.Date)
        };
        await Supplies.AddAsync(s);           // проверка и коррекция внутри
    }

    private async Task SaveNewBookAsync()
    {
        /* минимальная валидация */
        if (string.IsNullOrWhiteSpace(_tTitle.Text) ||
            string.IsNullOrWhiteSpace(_tAuthor.Text) ||
            string.IsNullOrWhiteSpace(_tIsbn.Text) ||
            string.IsNullOrWhiteSpace(_tLanguage.Text) ||
            string.IsNullOrWhiteSpace(_tPublisher.Text) ||
            string.IsNullOrWhiteSpace(_tGenre.Text) ||
            string.IsNullOrWhiteSpace(_tLocation.Text) ||
            string.IsNullOrWhiteSpace(_tYear.Value.ToString()))
            throw new InvalidOperationException("Заполните поля книги!");

        /* — создаём книгу с Amount = 0, дальше Supply.AddAsync корректирует — */
        var book = new Book
        {
            Title = _tTitle.Text,
            Author = _tAuthor.Text,
            ISBN = _tIsbn.Text,
            PublishYear = (int)_tYear.Value,
            Pages = (int)_tPages.Value,
            Genre = _tGenre.Text,
            Publisher = _tPublisher.Text,
            Language = _tLanguage.Text,
            Location = _tLocation.Text,
            Description = _tDescription.Text,
            CoverUrl = "no_cover.png",
            Amount = 0
        };
        await Books.AddAsync(book);                       // BookId теперь известен

        /* — обрабатываем обложку (если выбрана) — */
        if (!string.IsNullOrWhiteSpace(_tCoverPath.Text) && File.Exists(_tCoverPath.Text))
        {
            string slug = MakeSlug(_tTitle.Text);
            string fileNm = $"{book.BookId}_{slug}.jpg";          // CoverUrl
            string coversDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                            "AppData", "Media", "Covers");
            Directory.CreateDirectory(coversDir);
            string dest = Path.Combine(coversDir, fileNm);

            ResizeCrop260x320(_tCoverPath.Text, dest);
            book.CoverUrl = fileNm;
            await Books.UpdateAsync(book);                        // сохранить CoverUrl
        }

        /* — создаём запись поставки — */
        var s = new Supply
        {
            BookId = book.BookId,
            OperationType = OperationType.Приход.Text(),
            Amount = (int)_numAmt.Value,
            Date = DateOnly.FromDateTime(_dt.Value.Date)
        };
        await Supplies.AddAsync(s);                               // обновит Amount книги
    }

    /*──────────── графика (System.Drawing) ────────────*/
    private static void ResizeCrop260x320(string src, string dest)
    {
        using var img = Image.FromFile(src);

        // 1) масштабируем по ширине 260
        int newW = 260;
        int newH = (int)(img.Height * (newW / (double)img.Width));

        using var bmpW = new Bitmap(newW, newH);
        using (var g = Graphics.FromImage(bmpW))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(img, 0, 0, newW, newH);
        }

        // 2) приводим к высоте 320: crop или pad
        using var bmpFinal = new Bitmap(260, 320);
        using (var g = Graphics.FromImage(bmpFinal))
        {
            g.Clear(Color.White);
            if (newH >= 320)                                     // crop
            {
                int yOff = (newH - 320) / 2;
                g.DrawImage(bmpW, new Rectangle(0, 0, 260, 320),
                                   new Rectangle(0, yOff, 260, 320),
                                   GraphicsUnit.Pixel);
            }
            else                                                 // pad
            {
                int yPad = (320 - newH) / 2;
                g.DrawImage(bmpW, 0, yPad);
            }
        }

        bmpFinal.Save(dest, ImageFormat.Jpeg);
    }

    /* — простой slug (буквы/цифры и '_') — */
    private static string MakeSlug(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (char c in s)
            sb.Append(char.IsLetterOrDigit(c) ? c : '_');
        return sb.ToString();
    }

    /*──────────── helper-расширение ────────────*/
    private static Label lbl(string t) => new() { Text = t, AutoSize = true };
}

file static class CtlExt
{
    public static T At<T>(this T c, int x, int y, int? w = null) where T : Control
    { c.Left = x; c.Top = y; if (w.HasValue) c.Width = w.Value; return c; }
}
