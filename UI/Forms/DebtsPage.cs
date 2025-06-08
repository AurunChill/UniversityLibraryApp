using System.Drawing;
using System.Windows.Forms;
using LibraryApp.Data.Models;
using LibraryApp.Data.Services;
using LibraryApp.UI.Helpers;

namespace LibraryApp.UI.Forms;

public sealed class DebtsPage : TablePageBase
{
    private readonly BindingSource _bs = new();
    private DataGridView _grid = null!;
    private TextBox _search = null!;
    private string _sortColumn = nameof(Debt.DebtId);
    private SortOrder _sortOrder = SortOrder.Ascending;

    private readonly DebtService _debts;
    private readonly BookService _books;
    private readonly ReaderTicketService _tickets;

    public DebtsPage(DebtService debts, ReaderTicketService tickets, BookService books)
    {
        _debts = debts;
        _books = books;
        _tickets = tickets;
        BuildUI();
    }

    private void BuildUI()
    {
        Text = "Долги";
        MinimumSize = new Size(1000, 700);
        StartPosition = FormStartPosition.CenterParent;
        WindowState = FormWindowState.Maximized;

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

        _search = new TextBox
        {
            PlaceholderText = "  Поиск по книге или читателю…",
            Left = 20,
            Top = header.Bottom + 10,
            Width = 940,
            Height = 30
        };
        _search.TextChanged += async (_, __) => await LoadAsync();
        Controls.Add(_search);

        var hint = new Label
        {
            Text = "Сортировка при нажатии на названия полей таблицы",
            AutoSize = true,
            Left = 20,
            Top = _search.Bottom + 5
        };
        Controls.Add(hint);

        _grid = CreateGrid(hint.Bottom + 5, _bs);
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.ColumnHeaderMouseClick += async (s, e) =>
        {
            var col = _grid.Columns[e.ColumnIndex].DataPropertyName;
            if (_sortColumn == col)
                _sortOrder = _sortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            else
            {
                _sortColumn = col;
                _sortOrder = SortOrder.Ascending;
            }
            await LoadAsync();
        };
        Controls.Add(_grid);

        var btnAdd = MakeButton("Создать", _grid.Left, _grid.Bottom + 15, async (_, __) =>
        {
            using var dlg = new DebtDialog(_debts, _books, _tickets);
            if (dlg.ShowDialog(this) == DialogResult.OK)
                await LoadAsync();
        });
        var btnEdit = MakeButton("Обновить", btnAdd.Right + 10, btnAdd.Top, async (_, __) => await EditAsync());
        var btnDel = MakeButton("Удалить", btnEdit.Right + 10, btnAdd.Top, async (_, __) =>
        {
            if (await Delete()) await LoadAsync();
        });
        Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDel });
        Shown += async (_, __) =>
        {
            AdjustLayout();
            await LoadAsync();
        };
        Resize += (_, __) => AdjustLayout();

        void AdjustLayout()
        {
            int margin = 20;
            btnAdd.Top = ClientSize.Height - btnAdd.Height - margin;
            btnEdit.Top = btnAdd.Top;
            btnDel.Top = btnAdd.Top;

            _grid.Height = btnAdd.Top - _grid.Top - 10;
            _grid.Width = ClientSize.Width - 40;

        }
    }

    private async Task LoadAsync()
    {
        var list = await _debts.GetAllAsync();
        string filter = _search.Text.Trim().ToLower();
        if (!string.IsNullOrEmpty(filter))
        {
            list = list.Where(d =>
                d.Book!.Title.ToLower().Contains(filter) ||
                d.ReaderTicket!.Reader!.FullName.ToLower().Contains(filter))
            .ToList();
        }

        list = _sortColumn switch
        {
            "Книга" => _sortOrder == SortOrder.Ascending
                ? list.OrderBy(d => d.Book!.Title).ToList()
                : list.OrderByDescending(d => d.Book!.Title).ToList(),
            "Читатель" => _sortOrder == SortOrder.Ascending
                ? list.OrderBy(d => d.ReaderTicket!.Reader!.FullName).ToList()
                : list.OrderByDescending(d => d.ReaderTicket!.Reader!.FullName).ToList(),
            "Начало" => _sortOrder == SortOrder.Ascending
                ? list.OrderBy(d => d.StartTime).ToList()
                : list.OrderByDescending(d => d.StartTime).ToList(),
            "Окончание" => _sortOrder == SortOrder.Ascending
                ? list.OrderBy(d => d.EndTime).ToList()
                : list.OrderByDescending(d => d.EndTime).ToList(),
            _ => _sortOrder == SortOrder.Ascending
                ? list.OrderBy(d => d.DebtId).ToList()
                : list.OrderByDescending(d => d.DebtId).ToList()
        };

        var today = DateOnly.FromDateTime(DateTime.Today);
        _bs.DataSource = list.Select(d =>
        {
            string days;
            int penalty = 0;
            if (d.EndTime is DateOnly et)
            {
                int diff = (et.DayNumber - today.DayNumber);
                if (diff >= 0)
                {
                    days = diff.ToString();
                }
                else
                {
                    days = "Просрочено";
                    penalty = -diff * 30;
                }
            }
            else
            {
                days = "-";
            }

            return new
            {
                d.DebtId,
                Книга = d.Book!.Title,
                Читатель = d.ReaderTicket!.Reader!.FullName,
                Начало = d.StartTime,
                Окончание = d.EndTime,
                Дней_до_конца = days,
                Штраф = penalty
            };
        }).ToList();
    }

    private async Task EditAsync()
    {
        if (_grid.CurrentRow?.Cells["DebtId"].Value is not long id) return;
        var entity = await _debts.GetByIdAsync(id);
        if (entity is null) return;
        using var dlg = new DebtDialog(_debts, _books, _tickets, entity);
        if (dlg.ShowDialog(this) == DialogResult.OK)
            await LoadAsync();
    }

    private async Task<bool> Delete()
    {
        if (_grid.CurrentRow?.Cells["DebtId"].Value is not long id) return false;
        if (MessageBox.Show("Удалить запись?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return false;
        return await _debts.DeleteAsync(id);
    }
}

internal sealed class DebtDialog : Form
{
    private readonly ComboBox cbBook = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        AutoCompleteMode = AutoCompleteMode.SuggestAppend,
        AutoCompleteSource = AutoCompleteSource.ListItems
    };
    private readonly ComboBox cbTicket = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        AutoCompleteMode = AutoCompleteMode.SuggestAppend,
        AutoCompleteSource = AutoCompleteSource.ListItems
    };
    private readonly DateTimePicker dpStart = new() { Format = DateTimePickerFormat.Short };
    private readonly DateTimePicker dpEnd = new() { Format = DateTimePickerFormat.Short };
    private readonly DebtService _debts;
    private readonly BookService _books;
    private readonly ReaderTicketService _tickets;
    private readonly Debt? _orig;

    public DebtDialog(DebtService debts, BookService books, ReaderTicketService tickets, Debt? existing = null)
    {
        _debts = debts;
        _books = books;
        _tickets = tickets;
        _orig = existing;
        Text = existing is null ? "Новый долг" : "Редактирование";
        Size = new Size(420, 320);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(40, 40, 46);
        ForeColor = Color.Gainsboro;
        Font = new Font("Segoe UI", 10);

        int y = 20;
        Controls.AddRange(new Control[]
        {
            new Label { Text = "Книга", AutoSize = true, Left = 20, Top = y },
            cbBook.At(150, y - 3, 230),
            new Label { Text = "Билет", AutoSize = true, Left = 20, Top = y += 35 },
            cbTicket.At(150, y - 3, 230),
            new Label { Text = "Начало", AutoSize = true, Left = 20, Top = y += 35 },
            dpStart.At(150, y - 3),
            new Label { Text = "Окончание", AutoSize = true, Left = 20, Top = y += 35 },
            dpEnd.At(150, y - 3)
        });

        var ok = new Button
        {
            Text = "Сохранить",
            DialogResult = DialogResult.OK,
            Left = Width / 2 - 70,
            Top = 230,
            Width = 140,
            Height = 50,
            BackColor = Color.FromArgb(98, 0, 238),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        Controls.Add(ok);

        Load += async (_, __) => await InitAsync();
        ok.Click += async (_, __) => await SaveAsync();
    }

    private async Task InitAsync()
    {
        var books = await _books.GetAllAsync();
        cbBook.DataSource = books;
        cbBook.DisplayMember = nameof(Book.Title);
        cbBook.ValueMember = nameof(Book.BookId);

        var tickets = await _tickets.GetAllAsync();
        cbTicket.DataSource = tickets;
        cbTicket.DisplayMember = nameof(ReaderTicket.ReaderId); // will override below
        cbTicket.ValueMember = nameof(ReaderTicket.ReaderId);
        // replace display text with reader name
        cbTicket.Format += (s, e) =>
        {
            if (e.ListItem is ReaderTicket t)
                e.Value = t.Reader?.FullName ?? t.ReaderId.ToString();
        };

        if (_orig is not null)
        {
            cbBook.SelectedValue = _orig.BookId;
            cbTicket.SelectedValue = _orig.ReaderTicketId;
            dpStart.Value = DateTime.Parse(_orig.StartTime.ToString());
            if (_orig.EndTime is DateOnly et)
                dpEnd.Value = DateTime.Parse(et.ToString());
        }
    }

    private async Task SaveAsync()
    {
        if (cbBook.SelectedValue is not long bookId || cbTicket.SelectedValue is not long ticketId)
        {
            MessageBox.Show("Выберите книгу и билет");
            DialogResult = DialogResult.None;
            return;
        }

        if (_orig is null)
        {
            await _debts.AddAsync(new Debt
            {
                BookId = bookId,
                ReaderTicketId = ticketId,
                StartTime = DateOnly.FromDateTime(dpStart.Value),
                EndTime = DateOnly.FromDateTime(dpEnd.Value)
            });
        }
        else
        {
            _orig.BookId = bookId;
            _orig.ReaderTicketId = ticketId;
            _orig.StartTime = DateOnly.FromDateTime(dpStart.Value);
            _orig.EndTime = DateOnly.FromDateTime(dpEnd.Value);
            await _debts.UpdateAsync(_orig);
        }
    }
}
