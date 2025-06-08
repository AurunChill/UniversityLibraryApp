using LibraryApp.Data.Models;
using LibraryApp.Data.Services;

namespace LibraryApp.UI.Forms;

public sealed class DebtsPage : TablePageBase
{
    private readonly BindingSource _bs = new();
    private DataGridView _grid = null!;
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

        var btnAdd = MakeButton("Добавить", _grid.Left, _grid.Bottom + 15, (_, __) => Add());
        var btnDel = MakeButton("Удалить", btnAdd.Right + 10, _grid.Bottom + 15, async (_, __) => { if (await Delete()) await LoadAsync(); });
        Controls.AddRange(new Control[] { btnAdd, btnDel });

        Shown += async (_, __) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var list = await _debts.GetAllAsync(null, 0);
        _bs.DataSource = list.Select(d => new
        {
            d.DebtId,
            Book = d.BookId,
            Reader = d.ReaderTicketId,
            d.StartTime,
            d.EndTime
        }).ToList();
    }

    private void Add()
    {
        // simplified add dialog omitted
        MessageBox.Show("Добавление долга не реализовано");
    }

    private async Task<bool> Delete()
    {
        if (_grid.CurrentRow?.Cells["DebtId"].Value is not long id) return false;
        return await _debts.DeleteAsync(id);
    }
}
