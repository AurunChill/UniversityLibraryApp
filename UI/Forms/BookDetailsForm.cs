using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using LibraryApp.Data.Models;
using LibraryApp.Data.Services;
using LibraryApp.UI.Helpers;

namespace LibraryApp.UI.Forms;

public sealed class BookDetailForm : Form
{
    private Book _book;
    private readonly BookService _books;
    private readonly InventoryTransactionService _transactions;
    private readonly PublisherService _publishers;
    private readonly GenreService _genres;
    private readonly LanguageCodeService _languages;
    private readonly AuthorService _authors;
    private readonly IDbContextFactory<LibraryContext> _db;
    private PictureBox _cover = null!;

    public BookDetailForm(Book book, BookService books, InventoryTransactionService transactions,
        PublisherService publishers, GenreService genres, LanguageCodeService languages,
        AuthorService authors, IDbContextFactory<LibraryContext> db)
    {
        _book = book;
        _books = books;
        _transactions = transactions;
        _publishers = publishers;
        _genres = genres;
        _languages = languages;
        _authors = authors;
        _db = db;
        BuildUI();
    }

    private void BuildUI()
    {
        Text = _book.Title;
        MinimumSize = new Size(800, 600);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(24,24,28);
        ForeColor = Color.Gainsboro;

        string imgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "AppData","Media","Covers", _book.CoverUrl ?? "no_cover.png");

        _cover = new PictureBox
        {
            Left = 40,
            Top = 40,
            Width = 260,
            Height = 420,
            BackColor = Color.FromArgb(40,40,46),
            SizeMode = PictureBoxSizeMode.Zoom
        };
        if (File.Exists(imgPath))
            _cover.Image = Image.FromFile(imgPath);
        _cover.Round(12);
        Controls.Add(_cover);

        var details = new Panel
        {
            Left = _cover.Right + 40,
            Top = 40,
            Width = 400,
            Height = 420,
            BackColor = Color.Transparent
        };
        Controls.Add(details);

        int y = 0;
        details.Controls.Add(Make(details, _book.Title, 20, FontStyle.Bold, ref y, Color.White));
        string authors = string.Join(", ", _book.Authors.Select(a => a.Author!.Name));
        if (!string.IsNullOrEmpty(authors))
            details.Controls.Add(Make(details, $"Автор(ы): {authors}", 12, 0, ref y));
        string genres = string.Join(", ", _book.Genres.Select(g => g.Genre!.Name));
        if (!string.IsNullOrEmpty(genres))
            details.Controls.Add(Make(details, $"Жанр: {genres}", 12, 0, ref y));
        if (_book.Language is not null)
            details.Controls.Add(Make(details, $"Язык: {_book.Language.Code}", 12, 0, ref y));
        if (_book.Publisher is not null)
            details.Controls.Add(Make(details, $"Издатель: {_book.Publisher.Name}", 12, 0, ref y));
        if (_book.PublishYear != null)
            details.Controls.Add(Make(details, $"Год издания: {_book.PublishYear}",12,0,ref y));
        if (_book.Pages != null)
            details.Controls.Add(Make(details, $"Страниц: {_book.Pages}",12,0,ref y));
        details.Controls.Add(Make(details, $"ISBN: {_book.ISBN}", 12, 0, ref y));
        details.Controls.Add(Make(details, $"Описание:",12,FontStyle.Bold, ref y));
        var descBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            Width = details.Width - 10,
            Height = 120,
            Text = _book.Description ?? "Описание отсутствует"
        };
        descBox.At(0,y); y += descBox.Height + 10;
        details.Controls.Add(descBox);

        var btnHistory = new Button
        {
            Text = "Посмотреть историю транзакций",
            Left = _cover.Left,
            Top = _cover.Bottom + 15,
            Width = 220,
            Height = 36,
            BackColor = Color.FromArgb(98,0,238),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnHistory.Click += (_,__) => ShowTransactions();
        Controls.Add(btnHistory);

        var btnEdit = new Button
        {
            Text = "Обновить",
            Left = btnHistory.Right + 10,
            Top = btnHistory.Top,
            Width = 110,
            Height = 36,
            BackColor = Color.FromArgb(98,0,238),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnEdit.Click += async (_,__) =>
        {
            using var dlg = new BookEditDialog(_books, _publishers, _authors,
                _genres, _languages, _db, _book);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                var fresh = await _books.GetDetailedByIdAsync(_book.BookId);
                if (fresh is not null) _book = fresh;
                Controls.Clear();
                BuildUI();
            }
        };
        Controls.Add(btnEdit);

        var btnDelete = new Button
        {
            Text = "Удалить",
            Left = btnEdit.Right + 10,
            Top = btnHistory.Top,
            Width = 110,
            Height = 36,
            BackColor = Color.FromArgb(98,0,238),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnDelete.Click += async (_,__) =>
        {
            if (MessageBox.Show("Удалить книгу?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            if (await _books.DeleteAsync(_book.BookId))
                Close();
        };
        Controls.Add(btnDelete);
    }

    private static Label Make(Control parent,string t,int sz,FontStyle fs,ref int y,Color? c=null)
    {
        var lbl = new Label
        {
            Text = t,
            AutoSize = true,
            MaximumSize = new Size(parent.Width - 10, 0),
            Left = 0,
            Top = y,
            Font = new Font("Segoe UI", sz, fs),
            ForeColor = c ?? Color.Gainsboro
        };
        // Предварительно вычисляем высоту с учётом переноса
        var preferred = lbl.GetPreferredSize(new Size(parent.Width - 10, 0));
        lbl.Size = preferred;
        y += lbl.Height + 6;
        return lbl;
    }

    private void ShowTransactions()
    {
        using var dlg = new TransactionsDialog(_book, _transactions);
        dlg.ShowDialog(this);
    }
}

internal sealed class TransactionsDialog : Form
{
    public TransactionsDialog(Book book, InventoryTransactionService service)
    {
        Text = "Транзакции";
        Size = new Size(600,400);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(24,24,28);
        ForeColor = Color.Gainsboro;

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AutoGenerateColumns = true,
            BackgroundColor = Color.FromArgb(40, 40, 46),
            ForeColor = Color.Gainsboro,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        };
        var style = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(40, 40, 46),
            ForeColor = Color.Gainsboro,
            SelectionBackColor = Color.FromArgb(98, 0, 238),
            SelectionForeColor = Color.White
        };
        grid.DefaultCellStyle = style;
        grid.RowsDefaultCellStyle = style;
        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle(style)
        {
            BackColor = Color.FromArgb(32, 32, 38)
        };
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(55, 55, 60),
            ForeColor = Color.White
        };
        grid.EnableHeadersVisualStyles = false;
        Controls.Add(grid);

        Shown += async (_,__) =>
        {
            var list = await service.GetForBookAsync(book.BookId);
            grid.DataSource = list.Select(t => new { t.InventoryTransactionId, t.Date, t.Amount, Location = t.Location!.LocationName }).ToList();
        };
    }
}

internal sealed class BookEditDialog : Form
{
    private readonly BookService _books;
    private readonly PublisherService _publishers;
    private readonly AuthorService _authors;
    private readonly GenreService _genres;
    private readonly LanguageCodeService _languages;
    private readonly IDbContextFactory<LibraryContext> _db;
    private readonly Book _book;
    private readonly TextBox tTitle = new();
    private readonly TextBox tIsbn = new();
    private readonly NumericUpDown numYear = new() { Minimum = 0, Maximum = DateTime.Now.Year };
    private readonly NumericUpDown numPages = new() { Minimum = 0, Maximum = 10000 };
    private readonly TextBox tDesc = new() { Multiline = true, Height = 80 };
    private readonly ComboBox cbPublisher = new()
    {
        DropDownStyle     = ComboBoxStyle.DropDownList,
        AutoCompleteSource = AutoCompleteSource.ListItems,
        AutoCompleteMode  = AutoCompleteMode.SuggestAppend
    };
    private readonly TextBox tCover = new() { ReadOnly = true, BackColor = SystemColors.Window };
    private readonly Button btnBrowse = new() { Text = "Файл…", Height = 30 };
    private readonly CheckedListBox clAuthors = new() { CheckOnClick = true, Height = 80 };
    private readonly CheckedListBox clGenres = new() { CheckOnClick = true, Height = 80 };
    private readonly CheckedListBox clLanguages = new() { CheckOnClick = true, Height = 80 };

    public BookEditDialog(BookService books, PublisherService publishers, AuthorService authors,
        GenreService genres, LanguageCodeService languages, IDbContextFactory<LibraryContext> db,
        Book book)
    {
        _books = books;
        _publishers = publishers;
        _authors = authors;
        _genres = genres;
        _languages = languages;
        _db = db;
        _book = book;

        Text = "Редактирование";
        Size = new Size(560, 640);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(40, 40, 46);
        ForeColor = Color.Gainsboro;
        Font = new Font("Segoe UI", 10);

        int y = 20;
        Controls.Add(new Label { Text = "Название", AutoSize = true, Left = 20, Top = y });
        tTitle.At(150, y - 3, 320); Controls.Add(tTitle);

        Controls.Add(new Label { Text = "ISBN", AutoSize = true, Left = 20, Top = y += 35 });
        tIsbn.At(150, y - 3, 200); Controls.Add(tIsbn);

        Controls.Add(new Label { Text = "Издатель", AutoSize = true, Left = 20, Top = y += 35 });
        cbPublisher.At(150, y - 3, 200); Controls.Add(cbPublisher);
        Controls.Add(new Label { Text = "Год", AutoSize = true, Left = 20, Top = y += 35 });
        numYear.At(150, y - 3); Controls.Add(numYear);

        Controls.Add(new Label { Text = "Страниц", AutoSize = true, Left = 20, Top = y += 35 });
        numPages.At(150, y - 3); Controls.Add(numPages);

        Controls.Add(new Label { Text = "Описание", AutoSize = true, Left = 20, Top = y += 35 });
        tDesc.At(150, y - 3, 320); Controls.Add(tDesc);

        Controls.Add(new Label { Text = "Обложка", AutoSize = true, Left = 20, Top = y += tDesc.Height + 10 });
        tCover.At(150, y - 3, 250); Controls.Add(tCover);
        btnBrowse.At(410, y - 4, 80); Controls.Add(btnBrowse);

        Controls.Add(new Label { Text = "Авторы", AutoSize = true, Left = 20, Top = y += 35 });
        clAuthors.At(150, y - 3, 320); Controls.Add(clAuthors);

        Controls.Add(new Label { Text = "Жанры", AutoSize = true, Left = 20, Top = y += clAuthors.Height + 10 });
        clGenres.At(150, y - 3, 320); Controls.Add(clGenres);

        Controls.Add(new Label { Text = "Языки", AutoSize = true, Left = 20, Top = y += clGenres.Height + 10 });
        clLanguages.At(150, y - 3, 200); Controls.Add(clLanguages);

        var ok = new Button
        {
            Text = "Сохранить",
            DialogResult = DialogResult.OK,
            Left = Width / 2 - 70,
            Top = 550,
            Width = 140,
            Height = 45,
            BackColor = Color.FromArgb(98, 0, 238),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        Controls.Add(ok);

        Load += async (_, __) =>
        {
            tTitle.Text = _book.Title;
            tIsbn.Text = _book.ISBN;
            if (_book.PublishYear is int yv) numYear.Value = yv;
            if (_book.Pages is int p) numPages.Value = p;
            tDesc.Text = _book.Description ?? string.Empty;
            cbPublisher.DataSource = (await _publishers.GetAllAsync()).ToList();
            cbPublisher.DisplayMember = nameof(Publisher.Name);
            cbPublisher.SelectedItem = cbPublisher.Items
                .Cast<Publisher?>()
                .FirstOrDefault(p => p?.PublisherId == _book.PublisherId);
            tCover.Text = _book.CoverUrl ?? string.Empty;

            clAuthors.Items.Clear();
            foreach (var a in await _authors.GetAllAsync())
            {
                int idx = clAuthors.Items.Add(a);
                if (_book.Authors.Any(ab => ab.AuthorId == a.AuthorId))
                    clAuthors.SetItemChecked(idx, true);
            }
            clAuthors.DisplayMember = nameof(Author.Name);

            clGenres.Items.Clear();
            foreach (var g in await _genres.GetAllAsync())
            {
                int idx = clGenres.Items.Add(g);
                if (_book.Genres.Any(bg => bg.GenreId == g.GenreId))
                    clGenres.SetItemChecked(idx, true);
            }
            clGenres.DisplayMember = nameof(Genre.Name);

            clLanguages.Items.Clear();
            foreach (var l in await _languages.GetAllAsync())
            {
                int idx = clLanguages.Items.Add(l);
                if (_book.Languages.Any(bl => bl.LangId == l.LangId))
                    clLanguages.SetItemChecked(idx, true);
            }
            clLanguages.DisplayMember = nameof(LanguageCode.Code);
        };

        btnBrowse.Click += ChooseCover;

        ok.Click += async (_, __) => await SaveAsync();
    }

    private void ChooseCover(object? sender, EventArgs e)
    {
        using var od = new OpenFileDialog
        {
            Title = "Выберите изображение обложки",
            Filter = "Картинки|*.jpg;*.jpeg;*.png;*.bmp|Все файлы|*.*"
        };
        if (od.ShowDialog(this) == DialogResult.OK)
            tCover.Text = od.FileName;
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(tTitle.Text) || string.IsNullOrWhiteSpace(tIsbn.Text))
        {
            MessageBox.Show("Название и ISBN обязательны");
            DialogResult = DialogResult.None;
            return;
        }

        _book.Title = tTitle.Text;
        _book.ISBN = tIsbn.Text;
        _book.PublishYear = (int)numYear.Value;
        _book.Pages = (int)numPages.Value;
        _book.PublisherId = cbPublisher.SelectedItem is Publisher p ? p.PublisherId : null;
        _book.Description = tDesc.Text;
        if (!string.IsNullOrWhiteSpace(tCover.Text) && File.Exists(tCover.Text))
        {
            _book.CoverUrl = ImageHelper.SaveCover(tCover.Text);
        }
        await using var db = await _db.CreateDbContextAsync();
        var entity = await db.Books
            .Include(b => b.Authors)
            .Include(b => b.Genres)
            .Include(b => b.Languages)
            .FirstAsync(b => b.BookId == _book.BookId);

        db.Entry(entity).CurrentValues.SetValues(_book);

        entity.Authors.Clear();
        foreach (var item in clAuthors.CheckedItems.OfType<Author>())
            entity.Authors.Add(new AuthorBook { AuthorId = item.AuthorId });

        entity.Genres.Clear();
        foreach (var item in clGenres.CheckedItems.OfType<Genre>())
            entity.Genres.Add(new GenreBook { GenreId = item.GenreId });

        entity.Languages.Clear();
        long firstLang = 0;
        foreach (var item in clLanguages.CheckedItems.OfType<LanguageCode>())
        {
            entity.Languages.Add(new BookLanguage { LangId = item.LangId });
            if (firstLang == 0) firstLang = item.LangId;
        }
        if (firstLang != 0)
            entity.LangId = firstLang;

        await db.SaveChangesAsync();
    }
}
