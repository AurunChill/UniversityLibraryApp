using LibraryApp.Data;
using LibraryApp.Data.Models;

namespace LibraryApp.UI.Forms;

public sealed class DebtorsPage : Form
{
    private readonly BindingSource _bs = new();
    private DarkGrid _grid = null!;

    public DebtorsPage() => BuildUI();

    /* ───────── UI ───────── */
    private void BuildUI()
    {
        Text = "Должники";
        MinimumSize = new Size(1000, 650);
        BackColor = Color.FromArgb(24, 24, 28);
        ForeColor = Color.Gainsboro;
        Font = new Font("Segoe UI", 10);
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

        _grid = new DarkGrid(header.Bottom + 20, _bs);
        Controls.AddRange([header, _grid]);

        var btnAdd = Btn("Добавить", _grid.Left, _grid.Bottom + 15,
                          async (_, __) =>
                          {
                              using var d = new DebtorDialog();
                              if (d.ShowDialog(this) == DialogResult.OK)
                                  await RefreshGrid();
                          });

        var btnEdit = Btn("Изменить", btnAdd.Right + 10, _grid.Bottom + 15,
                          async (_, __) => await EditCurrent());

        var btnDel = Btn("Удалить", btnEdit.Right + 10, _grid.Bottom + 15,
                          async (_, __) => { if (await Delete()) await RefreshGrid(); });

        Controls.AddRange([btnAdd, btnEdit, btnDel]);

        Shown += async (_, __) => await RefreshGrid();
    }

    /* ───────── CRUD ───────── */
    private async Task RefreshGrid()
    {
        _bs.DataSource = (await Debtors.GetAllAsync(null, 0))
            .Select(d => new
            {
                d.DebtorId,
                d.ReaderTicketId,
                d.BookId,
                Reader = d.ReaderTicket!.FullName,
                Book = d.Book!.Title,
                d.GetDate,
                d.DebtDate,
                d.ReturnDate,
                d.DaysUntilDebt,
                Status = d.Status ?? "—",
                LatePenalty = d.LatePenalty?.ToString() ?? "—"
            })
            .ToList();
    }

    private async Task<bool> Delete()
    {
        if (_grid.CurrentRow?.Cells["DebtorId"].Value is not long id) return false;
        if (MessageBox.Show("Удалить запись?", "Подтверждение",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return false;

        return await Debtors.DeleteAsync(id);
    }

    private async Task EditCurrent()
    {
        if (_grid.CurrentRow?.Cells["DebtorId"].Value is not long id) return;

        var ent = await Debtors.GetByIdAsync(id);
        if (ent is null) return;

        using var dlg = new DebtorDialog(ent);
        if (dlg.ShowDialog(this) == DialogResult.OK)
            await RefreshGrid();
    }

    /* ───────── helpers ───────── */
    private Button Btn(string t, int l, int t2, EventHandler h)
    {
        var b = new Button
        {
            Text = t,
            Left = l,
            Top = t2,
            Width = 110,
            Height = 36,
            BackColor = Color.FromArgb(98, 0, 238),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        b.Click += h;
        return b;
    }

    /* ───── тёмная таблица ───── */
    private sealed class DarkGrid : DataGridView
    {
        public DarkGrid(int top, BindingSource bs)
        {
            Left = 20; Top = top; Width = 900; Height = 450;
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            AutoGenerateColumns = true;
            ReadOnly = true;
            DataSource = bs;

            BackgroundColor = Color.FromArgb(40, 40, 46);
            ForeColor = Color.Gainsboro;

            DefaultCellStyle = DarkStyle();
            RowsDefaultCellStyle = DarkStyle();
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle(DarkStyle())
            {
                BackColor = Color.FromArgb(32, 32, 38)
            };
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(55, 55, 60),
                ForeColor = Color.White
            };

            EnableHeadersVisualStyles = false;
            BorderStyle = BorderStyle.None;
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        }

        private static DataGridViewCellStyle DarkStyle() => new()
        {
            BackColor = Color.FromArgb(40, 40, 46),
            ForeColor = Color.Gainsboro,
            SelectionBackColor = Color.FromArgb(98, 0, 238),
            SelectionForeColor = Color.White
        };
    }
}

internal sealed class DebtorDialog : Form
{
    private readonly Debtor _model;

    /* ── поиск билета ── */
    private readonly TextBox _tTicketSearch = new() { PlaceholderText = "  Поиск билета…" };
    private readonly ListBox _lstTickets    = new() { Height = 100 };

    /* ── поиск книги ── */
    private readonly TextBox _tBookSearch = new() { PlaceholderText = "  Поиск книги…" };
    private readonly ListBox _lstBooks    = new() { Height = 100 };

    /* даты, статус, штраф */
    private readonly DateTimePicker _dtGet    = new() { Value = DateTime.Today };
    private readonly DateTimePicker _dtDebt   = new() { Value = DateTime.Today.AddDays(14) };
    private readonly DateTimePicker _dtReturn = new();                // может быть пустым
    private readonly ComboBox       _cbStatus = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown  _numPenalty = new() { Minimum = 0, Maximum = 10000, DecimalPlaces = 2 };

    /* отображаемые дни до/после долга */
    private readonly Label _lblDays = new() { AutoSize = true };

    private ReaderTicket? _selTicket;
    private Book?         _selBook;

    public DebtorDialog(Debtor? existing = null)
    {
        _model = existing ?? new Debtor { Status = DebtorStatus.В_Срок.Text() };
        BuildUI();
    }

    /* ───────── UI ───────── */
    private async void BuildUI()
    {
        Text          = "Должник";
        Size          = new Size(600, 720);
        StartPosition = FormStartPosition.CenterParent;
        BackColor     = Color.FromArgb(40, 40, 46);
        ForeColor     = Color.Gainsboro;
        Font          = new Font("Segoe UI", 10);

        _cbStatus.Items.AddRange(Enum.GetValues(typeof(DebtorStatus)).Cast<object>().ToArray());
        _cbStatus.SelectedItem = Enum.GetValues(typeof(DebtorStatus))
                                   .Cast<DebtorStatus>()
                                   .FirstOrDefault(s => s.Text() == _model.Status);

        int y = 20;
        Controls.Add(L("Билет", 20, y));
        Pos(_tTicketSearch, 140, y - 3, 420); Controls.Add(_tTicketSearch);
        Pos(_lstTickets, 140, y += 30, 420);  Controls.Add(_lstTickets);

        Controls.Add(L("Книга", 20, y += _lstTickets.Height + 10));
        Pos(_tBookSearch, 140, y - 3, 420); Controls.Add(_tBookSearch);
        Pos(_lstBooks, 140, y += 30, 420);   Controls.Add(_lstBooks);

        Controls.Add(L("Дата выдачи", 20, y += _lstBooks.Height + 10));
        Pos(_dtGet, 140, y - 3, 200); Controls.Add(_dtGet);

        Controls.Add(L("Вернуть до", 20, y += 35));
        Pos(_dtDebt, 140, y - 3, 200); Controls.Add(_dtDebt);

        Controls.Add(L("Фактический возврат", 20, y += 35));
        _dtReturn.CustomFormat = " "; _dtReturn.Format = DateTimePickerFormat.Custom;
        Pos(_dtReturn, 200, y - 3, 200); Controls.Add(_dtReturn);

        Controls.Add(L("Штраф, ₽", 20, y += 35));
        Pos(_numPenalty, 140, y - 3, 100); Controls.Add(_numPenalty);

        Controls.Add(L("Статус", 20, y += 35));
        Pos(_cbStatus, 140, y - 3, 250); Controls.Add(_cbStatus);

        Controls.Add(L("Дней до / после долга:", 20, y += 35));
        Pos(_lblDays, 240, y); Controls.Add(_lblDays);

        var btnOk = new Button
        {
            Text = "Сохранить",
            DialogResult = DialogResult.OK,
            Width  = 160,
            Height = 46,
            BackColor = Color.FromArgb(98, 0, 238),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        Pos(btnOk, Width / 2 - 80, y + 45); Controls.Add(btnOk);

        /* ── загрузка списков ── */
        _lstTickets.DataSource    = (await ReaderTickets.GetAllAsync(null, 0)).ToList();
        _lstTickets.DisplayMember = "FullName";
        _lstBooks.DataSource      = (await Books.GetAllAsync(null, 0)).ToList();
        _lstBooks.DisplayMember   = "Title";

        /* ── поиск ── */
        _tTicketSearch.TextChanged += async (_, __) =>
        {
            _lstTickets.DataSource = await ReaderTickets.FullTextAsync(_tTicketSearch.Text);
            _selTicket = null;
        };
        _lstTickets.SelectedIndexChanged += (_, __) =>
            _selTicket = _lstTickets.SelectedItem as ReaderTicket;

        _tBookSearch.TextChanged += async (_, __) =>
        {
            _lstBooks.DataSource = await Books.FullTextAsync(_tBookSearch.Text);
            _selBook = null;
        };
        _lstBooks.SelectedIndexChanged += (_, __) =>
            _selBook = _lstBooks.SelectedItem as Book;

        /* ── edit mode ── */
        if (_model.DebtorId != 0)
        {
            _selTicket = _lstTickets.Items.Cast<ReaderTicket>()
                          .FirstOrDefault(t => t.ReaderTicketId == _model.ReaderTicketId);
            _lstTickets.SelectedItem = _selTicket;

            _selBook = _lstBooks.Items.Cast<Book>()
                        .FirstOrDefault(b => b.BookId == _model.BookId);
            _lstBooks.SelectedItem = _selBook;

            _dtGet.Value  = _model.GetDate.ToDateTime(TimeOnly.MinValue);
            _dtDebt.Value = _model.DebtDate.ToDateTime(TimeOnly.MinValue);

            if (_model.ReturnDate is { } ret)
            {
                _dtReturn.Format = DateTimePickerFormat.Long;
                _dtReturn.Value  = ret.ToDateTime(TimeOnly.MinValue);
            }

            _numPenalty.Value = (decimal)(_model.LatePenalty ?? 0);
        }

        void RecalcDays() => _lblDays.Text = CalculateDays().ToString();
        _dtDebt.ValueChanged   += (_, __) => RecalcDays();
        _dtReturn.ValueChanged += (_, __) => { _dtReturn.Format = DateTimePickerFormat.Long; RecalcDays(); };
        RecalcDays();

        /* ── сохранение ── */
        btnOk.Click += async (_, __) =>
        {
            if (_selTicket is null || _selBook is null)
            {
                MessageBox.Show("Выберите билет и книгу");
                DialogResult = DialogResult.None;
                return;
            }

            _model.ReaderTicketId = _selTicket.ReaderTicketId;
            _model.BookId         = _selBook.BookId;
            _model.GetDate        = DateOnly.FromDateTime(_dtGet.Value.Date);
            _model.DebtDate       = DateOnly.FromDateTime(_dtDebt.Value.Date);
            _model.ReturnDate     = _dtReturn.Format == DateTimePickerFormat.Long
                                  ? DateOnly.FromDateTime(_dtReturn.Value.Date)
                                  : null;
            _model.Status         = ((DebtorStatus)_cbStatus.SelectedItem!).Text();
            _model.LatePenalty    = _numPenalty.Value > 0 ? (double)_numPenalty.Value : null;
            _model.DaysUntilDebt  = CalculateDays();

            if (_model.DebtorId == 0)
                await Debtors.AddAsync(_model);
            else
                await Debtors.UpdateAsync(_model);
        };
    }

    /* ───────── helpers ───────── */
    private int CalculateDays()
    {
        var refDate = _dtReturn.Format == DateTimePickerFormat.Long
                      ? _dtReturn.Value.Date
                      : DateTime.Today;
        return (refDate - _dtDebt.Value.Date).Days;
    }

    private static Label L(string t, int x, int y) => new() { Text = t, AutoSize = true, Left = x, Top = y };

    private static void Pos(Control c, int x, int y, int? w = null)
    { c.Left = x; c.Top = y; if (w.HasValue) c.Width = w.Value; }
}