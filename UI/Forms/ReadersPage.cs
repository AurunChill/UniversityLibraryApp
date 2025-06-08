using LibraryApp.Data.Models;
using LibraryApp.Data.Services;

namespace LibraryApp.UI.Forms;

public sealed class ReadersPage : TablePageBase
{
    private readonly BindingSource _bs = new();
    private DataGridView _grid = null!;
    private TextBox _search = null!;
    private string _sortColumn = nameof(ReaderTicket.ReaderId);
    private SortOrder _sortOrder = SortOrder.Ascending;
    private readonly ReaderTicketService _tickets;
    private readonly ReaderService _readers;

    public ReadersPage(ReaderTicketService tickets, ReaderService readers)
    {
        _tickets = tickets;
        _readers = readers;
        BuildUI();
    }

    private void BuildUI()
    {
        Text = "Читатели";
        MinimumSize = new Size(900, 650);
        StartPosition = FormStartPosition.CenterParent;

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
            PlaceholderText = "  Поиск по ФИО или e-mail…",
            Left = 20,
            Top = header.Bottom + 10,
            Width = 840,
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
            using var dlg = new ReaderDialog(_tickets, _readers);
            if (dlg.ShowDialog(this) == DialogResult.OK)
                await LoadAsync();
        });
        var btnEdit = MakeButton("Обновить", btnAdd.Right + 10, btnAdd.Top, async (_, __) => await EditAsync());
        var btnDel = MakeButton("Удалить", btnEdit.Right + 10, btnAdd.Top, async (_, __) =>
        {
            if (await DeleteAsync()) await LoadAsync();
        });
        Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDel });
        Shown += async (_, __) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var list = await _tickets.GetAllAsync();
        string filter = _search.Text.Trim().ToLower();
        if (!string.IsNullOrEmpty(filter))
            list = list.Where(t => t.Reader!.FullName.ToLower().Contains(filter) || t.Reader.Email.ToLower().Contains(filter)).ToList();

        list = _sortColumn switch
        {
            nameof(Reader.FullName) => _sortOrder == SortOrder.Ascending ? list.OrderBy(t => t.Reader!.FullName).ToList() : list.OrderByDescending(t => t.Reader!.FullName).ToList(),
            nameof(Reader.Email) => _sortOrder == SortOrder.Ascending ? list.OrderBy(t => t.Reader!.Email).ToList() : list.OrderByDescending(t => t.Reader!.Email).ToList(),
            nameof(ReaderTicket.RegistrationDate) => _sortOrder == SortOrder.Ascending ? list.OrderBy(t => t.RegistrationDate).ToList() : list.OrderByDescending(t => t.RegistrationDate).ToList(),
            _ => _sortOrder == SortOrder.Ascending ? list.OrderBy(t => t.ReaderId).ToList() : list.OrderByDescending(t => t.ReaderId).ToList()
        };

        _bs.DataSource = list.Select(t => new
        {
            t.ReaderId,
            ФИО = t.Reader!.FullName,
            Email = t.Reader.Email,
            Телефон = t.Reader.Phone,
            Дата_регистрации = t.RegistrationDate,
            Дата_окончания = t.EndTime
        }).ToList();
    }

    private async Task EditAsync()
    {
        if (_grid.CurrentRow?.Cells["ReaderId"].Value is not long id) return;
        var entity = await _tickets.GetByIdAsync(id);
        if (entity is null) return;
        using var dlg = new ReaderDialog(_tickets, _readers, entity);
        if (dlg.ShowDialog(this) == DialogResult.OK)
            await LoadAsync();
    }

    private async Task<bool> DeleteAsync()
    {
        if (_grid.CurrentRow?.Cells["ReaderId"].Value is not long id) return false;
        if (MessageBox.Show("Удалить читателя?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return false;
        return await _tickets.DeleteAsync(id);
    }
}

internal sealed class ReaderDialog : Form
{
    private readonly DateTimePicker dpReg = new() { Format = DateTimePickerFormat.Short };
    private readonly DateTimePicker dpEnd = new() { Format = DateTimePickerFormat.Short };
    private readonly TextBox tName = new();
    private readonly TextBox tEmail = new();
    private readonly TextBox tPhone = new();
    private readonly ReaderTicketService _tickets;
    private readonly ReaderService _readers;
    private readonly ReaderTicket? _orig;

    public ReaderDialog(ReaderTicketService tickets, ReaderService readers, ReaderTicket? existing = null)
    {
        _tickets = tickets;
        _readers = readers;
        _orig = existing;
        Text = existing is null ? "Новый читатель" : "Редактирование";
        Size = new Size(420, 330);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(40, 40, 46);
        ForeColor = Color.Gainsboro;
        Font = new Font("Segoe UI", 10);

        int y = 20;
        Controls.AddRange(new Control[]
        {
            new Label { Text = "ФИО", AutoSize = true, Left = 20, Top = y },
            tName.At(150, y - 3, 240),
            new Label { Text = "E-mail", AutoSize = true, Left = 20, Top = y += 35 },
            tEmail.At(150, y - 3, 240),
            new Label { Text = "Телефон", AutoSize = true, Left = 20, Top = y += 35 },
            tPhone.At(150, y - 3, 240),
            new Label { Text = "Дата регистрации", AutoSize = true, Left = 20, Top = y += 35 },
            dpReg.At(150, y - 3),
            new Label { Text = "Окончание", AutoSize = true, Left = 20, Top = y += 35 },
            dpEnd.At(150, y - 3)
        });

        var ok = new Button
        {
            Text = "Сохранить",
            DialogResult = DialogResult.OK,
            Left = Width / 2 - 70,
            Top = 240,
            Width = 140,
            Height = 50,
            BackColor = Color.FromArgb(98, 0, 238),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        Controls.Add(ok);

        if (_orig is not null)
        {
            tName.Text = _orig.Reader!.FullName;
            tEmail.Text = _orig.Reader.Email;
            tPhone.Text = _orig.Reader.Phone;
            dpReg.Value = DateTime.Parse(_orig.RegistrationDate.ToString());
            if (_orig.EndTime is DateOnly et)
                dpEnd.Value = DateTime.Parse(et.ToString());
        }

        ok.Click += async (_, __) => await SaveAsync();
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(tName.Text) || string.IsNullOrWhiteSpace(tEmail.Text))
        {
            MessageBox.Show("ФИО и E-mail обязательны");
            DialogResult = DialogResult.None;
            return;
        }

        if (_orig is null)
        {
            var reader = await _readers.AddAsync(new Reader
            {
                FullName = tName.Text,
                Email = tEmail.Text,
                Phone = tPhone.Text
            });
            await _tickets.AddAsync(new ReaderTicket
            {
                ReaderId = reader.ReaderId,
                RegistrationDate = DateOnly.FromDateTime(dpReg.Value),
                EndTime = DateOnly.FromDateTime(dpEnd.Value)
            });
        }
        else
        {
            _orig.Reader!.FullName = tName.Text;
            _orig.Reader.Email = tEmail.Text;
            _orig.Reader.Phone = tPhone.Text;
            await _readers.UpdateAsync(_orig.Reader);

            _orig.RegistrationDate = DateOnly.FromDateTime(dpReg.Value);
            _orig.EndTime = DateOnly.FromDateTime(dpEnd.Value);
            await _tickets.UpdateAsync(_orig);
        }
    }
}
