using LibraryApp.Data.Models;
using LibraryApp.Data.Services;
using LibraryApp.UI.Helpers;

namespace LibraryApp.UI.Forms;

public sealed class BookDetailForm : Form
{
    private Book _book;
    private readonly BookService _books;
    private readonly InventoryTransactionService _transactions;
    private PictureBox _cover = null!;

    public BookDetailForm(Book book, BookService books, InventoryTransactionService transactions)
    {
        _book = book;
        _books = books;
        _transactions = transactions;
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
