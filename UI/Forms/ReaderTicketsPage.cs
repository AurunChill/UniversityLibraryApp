using LibraryApp.Data;

namespace LibraryApp.UI.Forms;

public sealed class ReaderTicketsPage : Form
{
    private readonly BindingSource _bs = new();
    private DataGridView _grid = null!;

    public ReaderTicketsPage() => BuildUI();

    /* ───────── UI ───────── */
   private TextBox _txtSearchTickets = null!;

    private void BuildUI()
    {
        Text = "Читательские билеты";
        MinimumSize = new Size(900, 650);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(24, 24, 28);
        ForeColor = Color.Gainsboro;
        Font = new Font("Segoe UI", 10);

        // Заголовок
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

        // ── Строка поиска
        _txtSearchTickets = new TextBox
        {
            PlaceholderText = "  Поиск по ФИО, e-mail или телефону…",
            Left = 20,
            Top = header.Bottom + 15,
            Width = 800,
            Height = 30
        };
        Controls.Add(_txtSearchTickets);
        _txtSearchTickets.TextChanged += async (_, __) => await RefreshGrid();

        // ── Сам грид
        _grid = CreateDarkGrid();
        _grid.Top = _txtSearchTickets.Bottom + 10;
        Controls.Add(_grid);

        // ── Кнопки
        var btnAdd = MakeButton("Добавить");
        btnAdd.Left = _grid.Left;
        btnAdd.Top = _grid.Bottom + 15;
        btnAdd.Click += async (_, __) =>
        {
            using var dlg = new ReaderTicketDialog();
            if (dlg.ShowDialog(this) == DialogResult.OK)
                await RefreshGrid();
        };

        var btnDel = MakeButton("Удалить");
        btnDel.Left = btnAdd.Right + 10;
        btnDel.Top = btnAdd.Top;
        btnDel.Click += async (_, __) =>
        {
            if (await DeleteTicket())
                await RefreshGrid();
        };

        Controls.AddRange([btnAdd, btnDel]);

        Shown += async (_, __) => await RefreshGrid();
    }


   private async Task RefreshGrid()
    {
        var all = await ReaderTickets.GetAllAsync(null, 0);
        string filter = _txtSearchTickets.Text.Trim().ToLower();

        if (!string.IsNullOrEmpty(filter))
        {
            all = all
                .Where(r =>
                    (r.FullName?.ToLower().Contains(filter) ?? false)
                    || (r.Email?.ToLower().Contains(filter) ?? false)
                    || (r.PhoneNumber?.ToLower().Contains(filter) ?? false)
                    || (r.ExtraPhoneNumber?.ToLower().Contains(filter) ?? false))
                .ToList();
        }

        _bs.DataSource = all
                        .Select(r => new
                        {
                            r.ReaderTicketId,
                            r.FullName,
                            r.Email,
                            r.PhoneNumber,
                            r.ExtraPhoneNumber
                        })
                        .ToList();
    }


    private async Task<bool> DeleteTicket()
    {
        if (_grid.CurrentRow?.Cells["ReaderTicketId"].Value is not long id) return false;

        var res = MessageBox.Show("Удалить билет?", "Подтверждение",
                                  MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (res != DialogResult.Yes) return false;

        return await ReaderTickets.DeleteAsync(id);
    }

    private DataGridView CreateDarkGrid()
    {
        var gv = new DataGridView
        {
            Left = 20,
            Width = 800,
            Height = 350,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                     AnchorStyles.Left | AnchorStyles.Right,

            AutoGenerateColumns = true,
            ReadOnly = true,
            DataSource = _bs,

            BackgroundColor = Color.FromArgb(40, 40, 46),
            ForeColor = Color.Gainsboro,

            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        };

        /* базовые стили */
        var baseStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(40, 40, 46),
            ForeColor = Color.Gainsboro,
            SelectionBackColor = Color.FromArgb(98, 0, 238),
            SelectionForeColor = Color.White
        };
        gv.DefaultCellStyle = baseStyle;
        gv.RowsDefaultCellStyle = baseStyle;
        gv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle(baseStyle)
        {
            BackColor = Color.FromArgb(32, 32, 38)
        };
        gv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(55, 55, 60),
            ForeColor = Color.White
        };
        gv.EnableHeadersVisualStyles = false;

        return gv;
    }

    private static Button MakeButton(string text) => new()
    {
        Text = text,
        Width = 110,
        Height = 36,
        BackColor = Color.FromArgb(98, 0, 238),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Cursor = Cursors.Hand
    };
}

/* ───────── диалог создания билета ───────── */
internal sealed class ReaderTicketDialog : Form
{
    private readonly TextBox tName = new();
    private readonly TextBox tEmail = new();
    private readonly TextBox tPhone = new();
    private readonly TextBox tExtra = new();

    public ReaderTicketDialog()
    {
        Text = "Новый билет";
        Size = new Size(420, 465);
        BackColor = Color.FromArgb(40, 40, 46);
        ForeColor = Color.Gainsboro;
        Font = new Font("Segoe UI", 10);
        StartPosition = FormStartPosition.CenterParent;

        int y = 20;
        Controls.AddRange(
        [
            Label("ФИО", 20, y),
            TextBox(tName, 150, y - 3, 240),

            Label("E-mail", 20, y += 35),
            TextBox(tEmail, 150, y - 3, 240),

            Label("Телефон", 20, y += 35),
            TextBox(tPhone, 150, y - 3, 240),

            Label("Доп.тел", 20, y += 35),
            TextBox(tExtra, 150, y - 3, 240)
        ]);

        var ok = new Button
        {
            Text = "Сохранить",
            DialogResult = DialogResult.OK,
            Left = Width / 2 - 70,
            Top = 230,
            Width = 140,
            Height = 55,
            BackColor = Color.FromArgb(98, 0, 238),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        Controls.Add(ok);

        ok.Click += async (_, __) =>
        {
            if (string.IsNullOrWhiteSpace(tName.Text) ||
                string.IsNullOrWhiteSpace(tEmail.Text))
            {
                MessageBox.Show("ФИО и E-mail обязательны");
                DialogResult = DialogResult.None;
                return;
            }

            await ReaderTickets.AddAsync(new ReaderTicket
            {
                FullName = tName.Text,
                Email = tEmail.Text,
                PhoneNumber = tPhone.Text,
                ExtraPhoneNumber = tExtra.Text
            });
        };
    }

    /* мелкие фабрики */
    private static Label Label(string text, int x, int y) => new()
    {
        Text = text,
        AutoSize = true,
        Left = x,
        Top = y
    };

    private static TextBox TextBox(TextBox tb, int x, int y, int width)
    {
        tb.Left = x; tb.Top = y; tb.Width = width; return tb;
    }
}
