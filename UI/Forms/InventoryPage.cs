using System.Drawing;
using System.Windows.Forms;
using LibraryApp.Data.Models;
using LibraryApp.Data.Services;
using LibraryApp.UI.Helpers;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.UI.Forms;

public sealed class InventoryPage : TablePageBase
{
    private readonly BindingSource _bsLocations = new();
    private readonly BindingSource _bsTrans = new();
    private DataGridView _gridLoc = null!;
    private DataGridView _gridTrans = null!;
    private TextBox _searchLoc = null!;
    private TextBox _searchTrans = null!;
    private readonly LocationService _locations;
    private readonly InventoryTransactionService _transactions;
    private readonly IDbContextFactory<LibraryContext> _db;

    public InventoryPage(LocationService locations, InventoryTransactionService transactions, IDbContextFactory<LibraryContext> db)
    {
        _locations = locations;
        _transactions = transactions;
        _db = db;
        BuildUI();
    }

    private void BuildUI()
    {
        Text = "Инвентарь";
        MinimumSize = new Size(1100, 700);
        StartPosition = FormStartPosition.CenterParent;

        var lblLoc = new Label
        {
            Text = "Данные мест",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            AutoSize = true,
            Left = 20,
            Top = 20
        };
        Controls.Add(lblLoc);

        _searchLoc = new TextBox
        {
            PlaceholderText = "  Поиск по имени места…",
            Left = lblLoc.Left,
            Top = lblLoc.Bottom + 10,
            Width = 480,
            Height = 30
        };
        _searchLoc.TextChanged += async (_, __) => await LoadLocationsAsync();
        Controls.Add(_searchLoc);

        var hintLoc = new Label
        {
            Text = "Для сортировки нажмите на заголовок колонки",
            AutoSize = true,
            Left = lblLoc.Left,
            Top = _searchLoc.Bottom + 5
        };
        Controls.Add(hintLoc);

        _gridLoc = CreateGrid(hintLoc.Bottom + 5, _bsLocations);
        _gridLoc.Width = 520;
        _gridLoc.ColumnHeaderMouseClick += async (s, e) => await LoadLocationsAsync(_gridLoc.Columns[e.ColumnIndex].DataPropertyName);
        Controls.Add(_gridLoc);

        var btnAdd = MakeButton("Создать", _gridLoc.Left, _gridLoc.Bottom + 10, async (_, __) =>
        {
            using var dlg = new LocationDialog(_locations);
            if (dlg.ShowDialog(this) == DialogResult.OK)
                await LoadLocationsAsync();
        });
        var btnEdit = MakeButton("Обновить", btnAdd.Right + 10, btnAdd.Top, async (_, __) =>
        {
            if (_gridLoc.CurrentRow?.Cells["LocationId"].Value is not long id)
                return;
            var entity = await _locations.GetByIdAsync(id);
            if (entity is null) return;
            using var dlg = new LocationDialog(_locations, entity);
            if (dlg.ShowDialog(this) == DialogResult.OK)
                await LoadLocationsAsync();
        });
        var btnDel = MakeButton("Удалить", btnEdit.Right + 10, btnAdd.Top, async (_, __) =>
        {
            if (_gridLoc.CurrentRow?.Cells["LocationId"].Value is not long id) return;
            if (MessageBox.Show("Удалить место?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            if (await _locations.DeleteAsync(id)) await LoadLocationsAsync();
        });
        Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDel });

        // Right side
        var offsetLeft = _gridLoc.Right + 40;
        var lblTrans = new Label
        {
            Text = "Транзакции",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            AutoSize = true,
            Left = offsetLeft,
            Top = 20
        };
        Controls.Add(lblTrans);

        _searchTrans = new TextBox
        {
            PlaceholderText = "  Поиск по месту…",
            Left = offsetLeft,
            Top = lblTrans.Bottom + 10,
            Width = 520,
            Height = 30
        };
        _searchTrans.TextChanged += async (_, __) => await LoadTransactionsAsync();
        Controls.Add(_searchTrans);

        var hintTr = new Label
        {
            Text = "Для сортировки нажмите на заголовок колонки",
            AutoSize = true,
            Left = offsetLeft,
            Top = _searchTrans.Bottom + 5
        };
        Controls.Add(hintTr);

        _gridTrans = CreateGrid(hintTr.Bottom + 5, _bsTrans);
        _gridTrans.Left = offsetLeft;
        _gridTrans.Width = 520;
        _gridTrans.ColumnHeaderMouseClick += async (s, e) => await LoadTransactionsAsync(_gridTrans.Columns[e.ColumnIndex].DataPropertyName);
        Controls.Add(_gridTrans);

        var btnAddTr = MakeButton("Создать", _gridTrans.Left, _gridTrans.Bottom + 10, async (_, __) =>
        {
            using var page = new TransactionAddPage(_transactions, _locations, _db);
            if (page.ShowDialog(this) == DialogResult.OK)
                await LoadTransactionsAsync();
        });
        Controls.Add(btnAddTr);

        Shown += async (_, __) => { await LoadLocationsAsync(); await LoadTransactionsAsync(); };
    }

    private string _locSort = nameof(Location.LocationId);
    private SortOrder _locOrder = SortOrder.Ascending;
    private string _trSort = nameof(InventoryTransaction.InventoryTransactionId);
    private SortOrder _trOrder = SortOrder.Ascending;

    private async Task LoadLocationsAsync(string? sortColumn = null)
    {
        if (sortColumn is not null)
        {
            if (_locSort == sortColumn)
                _locOrder = _locOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            else
            {
                _locSort = sortColumn;
                _locOrder = SortOrder.Ascending;
            }
        }

        var list = await _locations.GetAllAsync(null, 0);
        string filter = _searchLoc.Text.Trim().ToLower();
        if (!string.IsNullOrEmpty(filter))
            list = list.Where(l => l.LocationName.ToLower().Contains(filter)).ToList();

        list = _locSort switch
        {
            nameof(Location.LocationName) => _locOrder == SortOrder.Ascending ? list.OrderBy(l => l.LocationName).ToList() : list.OrderByDescending(l => l.LocationName).ToList(),
            nameof(Location.Amount) => _locOrder == SortOrder.Ascending ? list.OrderBy(l => l.Amount).ToList() : list.OrderByDescending(l => l.Amount).ToList(),
            _ => _locOrder == SortOrder.Ascending ? list.OrderBy(l => l.LocationId).ToList() : list.OrderByDescending(l => l.LocationId).ToList()
        };

        _bsLocations.DataSource = list.Select(l => new { l.LocationId, Имя_места = l.LocationName, Количество = l.Amount }).ToList();
    }

    private async Task LoadTransactionsAsync(string? sortColumn = null)
    {
        if (sortColumn is not null)
        {
            if (_trSort == sortColumn)
                _trOrder = _trOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            else
            {
                _trSort = sortColumn;
                _trOrder = SortOrder.Ascending;
            }
        }

        var list = await _transactions.GetAllAsync(null, 0);
        string filter = _searchTrans.Text.Trim().ToLower();
        if (!string.IsNullOrEmpty(filter))
            list = list.Where(t => t.Location!.LocationName.ToLower().Contains(filter)).ToList();

        list = _trSort switch
        {
            nameof(InventoryTransaction.Date) => _trOrder == SortOrder.Ascending ? list.OrderBy(t => t.Date).ToList() : list.OrderByDescending(t => t.Date).ToList(),
            nameof(InventoryTransaction.Amount) => _trOrder == SortOrder.Ascending ? list.OrderBy(t => t.Amount).ToList() : list.OrderByDescending(t => t.Amount).ToList(),
            _ => _trOrder == SortOrder.Ascending ? list.OrderBy(t => t.InventoryTransactionId).ToList() : list.OrderByDescending(t => t.InventoryTransactionId).ToList()
        };

        _bsTrans.DataSource = list.Select(t => new
        {
            t.InventoryTransactionId,
            Дата = t.Date,
            Количество = t.Amount,
            Локация = t.Location!.LocationName,
            Откуда = t.PrevLocation?.LocationName ?? "-"
        }).ToList();
    }
}

internal sealed class LocationDialog : Form
{
    private readonly TextBox tName = new();
    private readonly NumericUpDown tAmt = new() { Minimum = 0, Maximum = 1000000 };
    private readonly LocationService _service;
    private readonly Location? _orig;

    public LocationDialog(LocationService service, Location? existing = null)
    {
        _service = service;
        _orig = existing;
        Text = existing is null ? "Новое место" : "Редактирование";
        Size = new Size(400, existing is null ? 200 : 260);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(40, 40, 46);
        ForeColor = Color.Gainsboro;
        Font = new Font("Segoe UI", 10);

        int y = 20;
        Controls.Add(new Label { Text = "Имя места", AutoSize = true, Left = 20, Top = y });
        tName.Left = 150; tName.Top = y - 3; tName.Width = 200;
        Controls.Add(tName);
        if (existing is not null)
        {
            Controls.Add(new Label { Text = "Количество", AutoSize = true, Left = 20, Top = y += 35 });
            tAmt.Left = 150; tAmt.Top = y - 3; tAmt.Width = 100;
            Controls.Add(tAmt);
        }

        var ok = new Button
        {
            Text = "Сохранить",
            DialogResult = DialogResult.OK,
            Left = Width / 2 - 70,
            Top = existing is null ? 100 : 150,
            Width = 140,
            Height = 45,
            BackColor = Color.FromArgb(98, 0, 238),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        Controls.Add(ok);

        if (existing is not null)
        {
            tName.Text = existing.LocationName;
            tAmt.Value = existing.Amount;
        }

        ok.Click += async (_, __) =>
        {
            if (string.IsNullOrWhiteSpace(tName.Text))
            {
                MessageBox.Show("Имя обязательно");
                DialogResult = DialogResult.None;
                return;
            }
            if (_orig is null)
            {
                await _service.AddAsync(new Location { LocationName = tName.Text, Amount = 0 });
            }
            else
            {
                _orig.LocationName = tName.Text;
                _orig.Amount = (int)tAmt.Value;
                await _service.UpdateAsync(_orig);
            }
        };
    }
}

internal sealed class TransactionAddPage : Form
{
    private readonly InventoryTransactionService _transactions;
    private readonly LocationService _locations;
    private readonly IDbContextFactory<LibraryContext> _db;

    private readonly ComboBox cbTo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox cbFrom = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DateTimePicker dt = new() { Value = DateTime.Today };
    private readonly NumericUpDown num = new() { Minimum = 1, Maximum = 1000, Value = 1 };
    private readonly CheckBox chk = new() { Text = "Сформировать книгу?", AutoSize = true };

    // New book fields
    private readonly TextBox tIsbn = new();
    private readonly TextBox tTitle = new();
    private readonly NumericUpDown tYear = new() { Minimum = 0, Maximum = DateTime.Now.Year, Value = DateTime.Now.Year };
    private readonly TextBox tPublisher = new();
    private readonly TextBox tLanguage = new();
    private readonly TextBox tDescription = new() { Multiline = true, Height = 60, ScrollBars = ScrollBars.Vertical };
    private readonly NumericUpDown tPages = new() { Minimum = 1, Maximum = 10000 };
    private readonly TextBox tGenre = new();
    private readonly TextBox tCover = new() { ReadOnly = true, BackColor = SystemColors.Window };
    private readonly Button btnBrowse = new() { Text = "Файл…", Height = 30 };

    public TransactionAddPage(InventoryTransactionService transactions, LocationService locations, IDbContextFactory<LibraryContext> db)
    {
        _transactions = transactions;
        _locations = locations;
        _db = db;
        BuildUI();
    }

    private void BuildUI()
    {
        Text = "Новая транзакция";
        Size = new Size(1000, 700);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(24,24,28);
        ForeColor = Color.Gainsboro;
        Font = new Font("Segoe UI", 10);

        int half = ClientSize.Width/2 - 40;
        int y = 20;
        Controls.Add(new Label{Text="Куда", AutoSize=true}.At(20,y));
        cbTo.At(150, y-3, half-170);
        Controls.Add(cbTo);
        Controls.Add(new Label{Text="Откуда", AutoSize=true}.At(20,y+=35));
        cbFrom.At(150,y-3, half-170);
        Controls.Add(cbFrom);
        Controls.Add(new Label{Text="Дата", AutoSize=true}.At(20,y+=35));
        dt.At(150,y-3,200);
        Controls.Add(dt);
        Controls.Add(new Label{Text="Количество", AutoSize=true}.At(20,y+=35));
        num.At(150,y-3,100);
        Controls.Add(num);
        chk.At(20,y+=45);
        Controls.Add(chk);

        Panel bookPanel = new() { Left = half + 40, Top = 20, Width = half, Height = 500, Visible = false };
        int y2 = 0;
        bookPanel.Controls.AddRange(new Control[]
        {
            new Label{Text="ISBN", AutoSize=true}.At(0,y2), tIsbn.At(120,y2-3,320),
            new Label{Text="Издатель", AutoSize=true}.At(0,y2+=35), tPublisher.At(120,y2-3,320),
            new Label{Text="Год", AutoSize=true}.At(0,y2+=35), tYear.At(120,y2-3,120),
            new Label{Text="Язык", AutoSize=true}.At(0,y2+=35), tLanguage.At(120,y2-3,200),
            new Label{Text="Название", AutoSize=true}.At(0,y2+=35), tTitle.At(120,y2-3,320),
            new Label{Text="Описание", AutoSize=true}.At(0,y2+=35), tDescription.At(120,y2-3,320),
            new Label{Text="Страниц", AutoSize=true}.At(0,y2+=tDescription.Height+10), tPages.At(120,y2-3,120),
            new Label{Text="Обложка", AutoSize=true}.At(0,y2+=35), tCover.At(120,y2-3,200), btnBrowse.At(330,y2-4,80),
            new Label{Text="Жанр", AutoSize=true}.At(0,y2+=35), tGenre.At(120,y2-3,320)
        });
        Controls.Add(bookPanel);

        var btnOk = new Button
        {
            Text = "Сохранить",
            DialogResult = DialogResult.OK,
            Left = Width - 180,
            Top = Height - 90,
            Width = 160,
            Height = 54,
            BackColor = Color.FromArgb(98,0,238),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        Controls.Add(btnOk);

        chk.CheckedChanged += (_,__) => bookPanel.Visible = chk.Checked;
        btnBrowse.Click += ChooseCover;
        Shown += async (_,__) => await LoadLocationsAsync();
        btnOk.Click += async (_,__) => await SaveAsync();
    }

    private async Task LoadLocationsAsync()
    {
        var list = await _locations.GetAllAsync(null,0);
        cbTo.DataSource = list.ToList();
        cbTo.DisplayMember = nameof(Location.LocationName);
        cbFrom.DataSource = list.ToList();
        cbFrom.DisplayMember = nameof(Location.LocationName);
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
        if (cbTo.SelectedItem is not Location to || cbFrom.SelectedItem is not Location from)
        {
            MessageBox.Show("Выберите места");
            DialogResult = DialogResult.None;
            return;
        }

        long bookId = 0;
        if (chk.Checked)
        {
            var book = new Book
            {
                ISBN = tIsbn.Text,
                Title = tTitle.Text,
                PublishYear = (int)tYear.Value,
                Description = tDescription.Text,
                Pages = (int)tPages.Value,
                CoverUrl = "no_cover.png"
            };
            await using var db = await _db.CreateDbContextAsync();
            db.Books.Add(book);
            await db.SaveChangesAsync();
            bookId = book.BookId;
        }
        else
        {
            // simplified: use 1 as placeholder
            bookId = 1;
        }

        var tran = new InventoryTransaction
        {
            BookId = bookId,
            LocationId = to.LocationId,
            PrevLocationId = from.LocationId,
            Date = DateOnly.FromDateTime(dt.Value.Date),
            Amount = (int)num.Value
        };
        await _transactions.AddAsync(tran);
    }
}
