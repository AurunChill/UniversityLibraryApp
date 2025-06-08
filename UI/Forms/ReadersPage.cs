using LibraryApp.Data.Models;
using LibraryApp.Data.Services;

namespace LibraryApp.UI.Forms;

public sealed class ReadersPage : TablePageBase
{
    private readonly BindingSource _bs = new();
    private DataGridView _grid = null!;
    private readonly ReaderService _readers;

    public ReadersPage(ReaderService readers)
    {
        _readers = readers;
        BuildUI();
    }

    private void BuildUI()
    {
        Text = "Читатели";
        MinimumSize = new Size(800, 600);
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

        _grid = CreateGrid(header.Bottom + 20, _bs);
        Controls.Add(_grid);

        Shown += async (_, __) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var list = await _readers.GetAllAsync(null, 0);
        _bs.DataSource = list.Select(r => new { r.ReaderId, r.FullName, r.Email, r.Phone }).ToList();
    }
}
