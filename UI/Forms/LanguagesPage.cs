using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LibraryApp.Data.Models;
using LibraryApp.Data.Services;
using LibraryApp.UI.Helpers;

namespace LibraryApp.UI.Forms;

public sealed class LanguagesPage : TablePageBase
{
    private readonly BindingSource _bs = new();
    private DataGridView _grid = null!;
    private TextBox _search = null!;
    private string _sortColumn = nameof(LanguageCode.LangId);
    private SortOrder _sortOrder = SortOrder.Ascending;
    private readonly LanguageCodeService _languages;

    public LanguagesPage(LanguageCodeService languages)
    {
        _languages = languages;
        BuildUI();
    }

    private void BuildUI()
    {
        Text = "Языки";
        MinimumSize = new Size(700, 500);
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
            PlaceholderText = "  Поиск по имени…",
            Left = 20,
            Top = header.Bottom + 10,
            Width = 640,
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

        var (btnAdd, btnEdit, btnDel) = CreateActionButtons();
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
        }
    }

    private async Task LoadAsync()
    {
        var list = await _languages.GetAllAsync();
        string filter = _search.Text.Trim().ToLower();
        if (!string.IsNullOrEmpty(filter))
            list = list.Where(l => l.Code.ToLower().Contains(filter)).ToList();

        list = _sortColumn switch
        {
            nameof(LanguageCode.Code) => _sortOrder == SortOrder.Ascending ? list.OrderBy(l => l.Code).ToList() : list.OrderByDescending(l => l.Code).ToList(),
            _ => _sortOrder == SortOrder.Ascending ? list.OrderBy(l => l.LangId).ToList() : list.OrderByDescending(l => l.LangId).ToList()
        };

        _bs.DataSource = list.Select(l => new { l.LangId, Код = l.Code }).ToList();
    }

    private (Button, Button, Button) CreateActionButtons()
    {
        var btnAdd = MakeButton("Создать", _grid.Left, _grid.Bottom + 15, async (_, __) =>
        {
            using var dlg = new LanguageDialog(_languages);
            if (dlg.ShowDialog(this) == DialogResult.OK)
                await LoadAsync();
        });
        var btnEdit = MakeButton("Обновить", btnAdd.Right + 10, btnAdd.Top, async (_, __) => await EditAsync());
        var btnDel = MakeButton("Удалить", btnEdit.Right + 10, btnAdd.Top, async (_, __) =>
        {
            if (await DeleteAsync()) await LoadAsync();
        });
        return (btnAdd, btnEdit, btnDel);
    }

    private async Task EditAsync()
    {
        if (_grid.CurrentRow?.Cells["LangId"].Value is not long id) return;
        var entity = await _languages.GetByIdAsync(id);
        if (entity is null) return;
        using var dlg = new LanguageDialog(_languages, entity);
        if (dlg.ShowDialog(this) == DialogResult.OK)
            await LoadAsync();
    }

    private async Task<bool> DeleteAsync()
    {
        if (_grid.CurrentRow?.Cells["LangId"].Value is not long id) return false;
        if (MessageBox.Show("Удалить язык?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return false;
        return await _languages.DeleteAsync(id);
    }
}

internal sealed class LanguageDialog : Form
{
    private readonly TextBox tName = new();
    private readonly LanguageCodeService _service;
    private readonly LanguageCode? _orig;

    public LanguageDialog(LanguageCodeService service, LanguageCode? existing = null)
    {
        _service = service;
        _orig = existing;
        Text = existing is null ? "Новый язык" : "Редактирование";
        Size = new Size(400, 200);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(40, 40, 46);
        ForeColor = Color.Gainsboro;
        Font = new Font("Segoe UI", 10);

        int y = 20;
        Controls.Add(new Label { Text = "Имя", AutoSize = true, Left = 20, Top = y });
        tName.At(150, y - 3, 200); Controls.Add(tName);

        var ok = new Button
        {
            Text = "Сохранить",
            DialogResult = DialogResult.OK,
            Left = Width / 2 - 70,
            Top = 100,
            Width = 140,
            Height = 45,
            BackColor = Color.FromArgb(98, 0, 238),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        Controls.Add(ok);

        if (existing is not null)
            tName.Text = existing.Code;

        ok.Click += async (_, __) =>
        {
            if (string.IsNullOrWhiteSpace(tName.Text))
            {
                MessageBox.Show("Имя обязательно");
                DialogResult = DialogResult.None;
                return;
            }
            if (_orig is null)
                await _service.AddAsync(new LanguageCode { Code = tName.Text });
            else
            {
                _orig.Code = tName.Text;
                await _service.UpdateAsync(_orig);
            }
        };
    }
}
