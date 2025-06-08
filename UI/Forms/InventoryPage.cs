using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibraryApp.Data;
using LibraryApp.Data.Models;
using LibraryApp.Data.Services;
using LibraryApp.UI.Helpers;
using Microsoft.EntityFrameworkCore;

// Псевдонимы для моделей, чтобы избежать конфликта с Form.Location (Point)
using ModelLocation              = LibraryApp.Data.Models.Location;
using ModelInventoryTransaction  = LibraryApp.Data.Models.InventoryTransaction;

namespace LibraryApp.UI.Forms
{
    public sealed class InventoryPage : TablePageBase
    {
        private readonly BindingSource _bsLocations = new();
        private readonly BindingSource _bsTrans     = new();
        private DataGridView     _gridLoc          = null!;
        private DataGridView     _gridTrans        = null!;
        private TextBox          _searchLoc        = null!;
        private TextBox          _searchTrans      = null!;
        private readonly LocationService                 _locations;
        private readonly InventoryTransactionService      _transactions;
        private readonly IDbContextFactory<LibraryContext> _db;
        private readonly PublisherService                _publishers;
        private readonly GenreService                   _genres;
        private readonly LanguageCodeService            _languages;

        public InventoryPage(
            LocationService locations,
            InventoryTransactionService transactions,
            IDbContextFactory<LibraryContext> db,
            PublisherService publishers,
            GenreService genres,
            LanguageCodeService languages)
        {
            _locations   = locations;
            _transactions = transactions;
            _db           = db;
            _publishers   = publishers;
            _genres       = genres;
            _languages    = languages;
            BuildUI();
        }

        private void BuildUI()
        {
            Text          = "Инвентарь";
            MinimumSize   = new Size(1100, 700);
            StartPosition = FormStartPosition.CenterParent;
            WindowState   = FormWindowState.Maximized;

            // --- Секция локаций ---
            var lblLoc = new Label
            {
                Text     = "Данные мест",
                Font     = new Font("Segoe UI", 14, FontStyle.Bold),
                AutoSize = true,
                Left     = 20,
                Top      = 20
            };
            Controls.Add(lblLoc);

            _searchLoc = new TextBox
            {
                PlaceholderText = "  Поиск по имени места…",
                Left            = lblLoc.Left,
                Top             = lblLoc.Bottom + 10,
                Width           = 480,
                Height          = 30
            };
            _searchLoc.TextChanged += async (_, __) => await LoadLocationsAsync();
            Controls.Add(_searchLoc);

            var hintLoc = new Label
            {
                Text     = "Для сортировки нажмите на заголовок колонки",
                AutoSize = true,
                Left     = lblLoc.Left,
                Top      = _searchLoc.Bottom + 5
            };
            Controls.Add(hintLoc);

            _gridLoc = CreateGrid(hintLoc.Bottom + 5, _bsLocations);
            _gridLoc.Width = 520;
            _gridLoc.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _gridLoc.ColumnHeaderMouseClick += async (s, e)
                => await LoadLocationsAsync(_gridLoc.Columns[e.ColumnIndex].DataPropertyName);
            Controls.Add(_gridLoc);

            var btnAdd = MakeButton("Создать", _gridLoc.Left, _gridLoc.Bottom + 10, async (_, __) =>
            {
                using var dlg = new LocationDialog(_locations);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    await LoadLocationsAsync();
            });
            var btnEdit = MakeButton("Обновить", btnAdd.Right + 10, btnAdd.Top, async (_, __) =>
            {
                if (_gridLoc.CurrentRow?.Cells["LocationId"].Value is not long id) return;
                var entity = await _locations.GetByIdAsync(id);
                if (entity is null) return;
                using var dlg = new LocationDialog(_locations, entity);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    await LoadLocationsAsync();
            });
            var btnDel = MakeButton("Удалить", btnEdit.Right + 10, btnAdd.Top, async (_, __) =>
            {
                if (_gridLoc.CurrentRow?.Cells["LocationId"].Value is not long id) return;
                if (MessageBox.Show("Удалить место?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                    != DialogResult.Yes) return;
                if (await _locations.DeleteAsync(id))
                    await LoadLocationsAsync();
            });
            Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDel });

            // --- Секция транзакций ---
            var offsetLeft = _gridLoc.Right + 40;
            int transShift = 350;
            var lblTrans = new Label
            {
                Text     = "Транзакции",
                Font     = new Font("Segoe UI", 14, FontStyle.Bold),
                AutoSize = true,
                Left     = offsetLeft + transShift,
                Top      = 20
            };
            Controls.Add(lblTrans);

            _searchTrans = new TextBox
            {
                PlaceholderText = "  Поиск по месту…",
                Left            = offsetLeft + transShift,
                Top             = lblTrans.Bottom + 10,
                Width           = 520,
                Height          = 30
            };
            _searchTrans.TextChanged += async (_, __) => await LoadTransactionsAsync();
            Controls.Add(_searchTrans);

            var hintTr = new Label
            {
                Text     = "Для сортировки нажмите на заголовок колонки",
                AutoSize = true,
                Left     = offsetLeft + transShift,
                Top      = _searchTrans.Bottom + 5
            };
            Controls.Add(hintTr);

            _gridTrans = CreateGrid(hintTr.Bottom + 5, _bsTrans);
            _gridTrans.Left = offsetLeft;
            _gridTrans.Width = 520;
            _gridTrans.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _gridTrans.ColumnHeaderMouseClick += async (s, e)
                => await LoadTransactionsAsync(_gridTrans.Columns[e.ColumnIndex].DataPropertyName);
            Controls.Add(_gridTrans);

            var btnAddTr = MakeButton("Создать", _gridTrans.Left + transShift, _gridTrans.Bottom + 10, async (_, __) =>
            {
                using var page = new TransactionAddPage(_transactions, _locations, _db, _publishers, _genres, _languages);
                if (page.ShowDialog(this) == DialogResult.OK)
                    await LoadTransactionsAsync();
            });
            Controls.Add(btnAddTr);

            Shown += async (_, __) =>
            {
                AdjustLayout();
                await LoadLocationsAsync();
                await LoadTransactionsAsync();
            };
            Resize += (_, __) => AdjustLayout();

            void AdjustLayout()
            {
                int margin = 20;
                btnAdd.Top = ClientSize.Height - btnAdd.Height - margin;
                btnEdit.Top = btnAdd.Top;
                btnDel.Top = btnAdd.Top;
                btnAddTr.Top  = ClientSize.Height - btnAddTr.Height - margin;

                int tableWidth = (ClientSize.Width - 80) / 2;
                _gridLoc.Width   = tableWidth;
                _gridTrans.Width = tableWidth;
                _gridTrans.Left  = _gridLoc.Right + 40;

                lblTrans.Left    = _gridTrans.Left + transShift;
                _searchTrans.Left = _gridTrans.Left + transShift;
                hintTr.Left      = _gridTrans.Left + transShift;
                btnAddTr.Left    = _gridTrans.Left + transShift;
                _gridLoc.Height = btnAdd.Top - _gridLoc.Top - 10;
                _gridTrans.Height = btnAddTr.Top - _gridTrans.Top - 10;
            }
        }

        // --- Сортировка ---
        private string    _locSort = nameof(ModelLocation.LocationId);
        private SortOrder _locOrder = SortOrder.Ascending;
        private string    _trSort  = nameof(ModelInventoryTransaction.InventoryTransactionId);
        private SortOrder _trOrder = SortOrder.Ascending;

        private async Task LoadLocationsAsync(string? sortColumn = null)
        {
            if (sortColumn is not null)
            {
                if (_locSort == sortColumn)
                    _locOrder = _locOrder == SortOrder.Ascending
                        ? SortOrder.Descending
                        : SortOrder.Ascending;
                else
                {
                    _locSort = sortColumn;
                    _locOrder = SortOrder.Ascending;
                }
            }

            var list = await _locations.GetAllAsync(null, 0);
            var filter = _searchLoc.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(filter))
                list = list.Where(l => l.LocationName.ToLower().Contains(filter)).ToList();

            list = _locSort switch
            {
                nameof(ModelLocation.LocationName)
                    => _locOrder == SortOrder.Ascending
                        ? list.OrderBy(l => l.LocationName).ToList()
                        : list.OrderByDescending(l => l.LocationName).ToList(),

                nameof(ModelLocation.Amount)
                    => _locOrder == SortOrder.Ascending
                        ? list.OrderBy(l => l.Amount).ToList()
                        : list.OrderByDescending(l => l.Amount).ToList(),

                _ => _locOrder == SortOrder.Ascending
                        ? list.OrderBy(l => l.LocationId).ToList()
                        : list.OrderByDescending(l => l.LocationId).ToList()
            };

            _bsLocations.DataSource = list
                .Select(l => new
                {
                    l.LocationId,
                    Имя_места   = l.LocationName,
                    Количество  = l.Amount
                })
                .ToList();
        }

        private async Task LoadTransactionsAsync(string? sortColumn = null)
        {
            if (sortColumn is not null)
            {
                if (_trSort == sortColumn)
                    _trOrder = _trOrder == SortOrder.Ascending
                        ? SortOrder.Descending
                        : SortOrder.Ascending;
                else
                {
                    _trSort = sortColumn;
                    _trOrder = SortOrder.Ascending;
                }
            }

            var list = await _transactions.GetAllAsync(null, 0);
            var filter = _searchTrans.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(filter))
                list = list.Where(t => t.Location!.LocationName
                                     .ToLower()
                                     .Contains(filter))
                           .ToList();

            list = _trSort switch
            {
                nameof(ModelInventoryTransaction.Date)
                    => _trOrder == SortOrder.Ascending
                        ? list.OrderBy(t => t.Date).ToList()
                        : list.OrderByDescending(t => t.Date).ToList(),

                nameof(ModelInventoryTransaction.Amount)
                    => _trOrder == SortOrder.Ascending
                        ? list.OrderBy(t => t.Amount).ToList()
                        : list.OrderByDescending(t => t.Amount).ToList(),

                _ => _trOrder == SortOrder.Ascending
                        ? list.OrderBy(t => t.InventoryTransactionId).ToList()
                        : list.OrderByDescending(t => t.InventoryTransactionId).ToList()
            };

            _bsTrans.DataSource = list
                .Select(t => new
                {
                    t.InventoryTransactionId,
                    Дата     = t.Date,
                    Количество = t.Amount,
                    Локация  = t.Location!.LocationName,
                    Откуда   = t.PrevLocation?.LocationName ?? "-"
                })
                .ToList();
        }
    }

    // ---------------------------------------------
    // Диалог создания/редактирования Location
    // ---------------------------------------------
    internal sealed class LocationDialog : Form
    {
        private readonly TextBox       tName = new();
        private readonly NumericUpDown tAmt  = new() { Minimum = 0, Maximum = 1000000 };
        private readonly LocationService  _service;
        private readonly ModelLocation?   _orig;

        public LocationDialog(LocationService service, ModelLocation? existing = null)
        {
            _service = service;
            _orig    = existing;
            Text     = existing is null ? "Новое место" : "Редактирование";
            Size     = new Size(400, existing is null ? 200 : 260);
            StartPosition = FormStartPosition.CenterParent;
            BackColor     = Color.FromArgb(40, 40, 46);
            ForeColor     = Color.Gainsboro;
            Font          = new Font("Segoe UI", 10);

            int y = 20;
            Controls.Add(new Label { Text = "Имя места", AutoSize = true, Left = 20, Top = y });
            tName.Left = 150; tName.Top = y - 3; tName.Width = 200;
            Controls.Add(tName);

            if (existing is not null)
            {
                Controls.Add(new Label { Text = "Количество", AutoSize = true, Left = 20, Top = y + 35 });
                tAmt.Left = 150; tAmt.Top = y + 32; tAmt.Width = 100;
                Controls.Add(tAmt);
            }

            var ok = new Button
            {
                Text         = "Сохранить",
                DialogResult = DialogResult.OK,
                Left         = Width / 2 - 70,
                Top          = existing is null ? 100 : 150,
                Width        = 140,
                Height       = 45,
                BackColor    = Color.FromArgb(98, 0, 238),
                ForeColor    = Color.White,
                FlatStyle    = FlatStyle.Flat
            };
            Controls.Add(ok);

            if (existing is not null)
            {
                tName .Text = existing.LocationName;
                tAmt  .Value = existing.Amount;
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
                    await _service.AddAsync(new ModelLocation
                    {
                        LocationName = tName.Text,
                        Amount       = 0
                    });
                }
                else
                {
                    _orig.LocationName = tName.Text;
                    _orig.Amount       = (int)tAmt.Value;
                    await _service.UpdateAsync(_orig);
                }
            };
        }
    }

    // ---------------------------------------------
    // Страница создания транзакции
    // ---------------------------------------------
    internal sealed class TransactionAddPage : Form
    {
        private readonly InventoryTransactionService _transactions;
        private readonly LocationService             _locations;
        private readonly IDbContextFactory<LibraryContext> _db;
        private readonly PublisherService            _publishers;
        private readonly GenreService               _genres;
        private readonly LanguageCodeService        _languages;
        private readonly ComboBox      cbTo     = new()
        {
            DropDownStyle     = ComboBoxStyle.DropDownList,
            AutoCompleteSource = AutoCompleteSource.ListItems,
            AutoCompleteMode  = AutoCompleteMode.SuggestAppend
        };
        private readonly ComboBox      cbFrom   = new()
        {
            DropDownStyle     = ComboBoxStyle.DropDownList,
            AutoCompleteSource = AutoCompleteSource.ListItems,
            AutoCompleteMode  = AutoCompleteMode.SuggestAppend
        };
        private readonly DateTimePicker dt      = new() { Value = DateTime.Today };
        private readonly NumericUpDown  num     = new() { Minimum = 1, Maximum = 1000, Value = 1 };
        private readonly CheckBox       chk     = new() { Text = "Сформировать книгу?", AutoSize = true };

        // Поля для новой книги
        private readonly TextBox       tIsbn   = new();
        private readonly TextBox       tTitle  = new();
        private readonly NumericUpDown tYear   = new() { Minimum = 0, Maximum = DateTime.Now.Year, Value = DateTime.Now.Year };
        private readonly ComboBox      cbPublisher = new()
        {
            DropDownStyle     = ComboBoxStyle.DropDownList,
            AutoCompleteSource = AutoCompleteSource.ListItems,
            AutoCompleteMode  = AutoCompleteMode.SuggestAppend
        };
        private readonly CheckedListBox clLanguages = new() { CheckOnClick = true, Height = 80 };
        private readonly TextBox       tDescription = new() { Multiline = true, Height = 60, ScrollBars = ScrollBars.Vertical };
        private readonly NumericUpDown tPages   = new() { Minimum = 1, Maximum = 10000 };
        private readonly CheckedListBox clGenres  = new() { CheckOnClick = true, Height = 80 };
        private readonly TextBox       tCover   = new() { ReadOnly = true, BackColor = SystemColors.Window };
        private readonly Button        btnBrowse = new() { Text = "Файл…", Height = 30 };

        public TransactionAddPage(
            InventoryTransactionService transactions,
            LocationService locations,
            IDbContextFactory<LibraryContext> db,
            PublisherService publishers,
            GenreService genres,
            LanguageCodeService languages)
        {
            _transactions = transactions;
            _locations    = locations;
            _db           = db;
            _publishers   = publishers;
            _genres       = genres;
            _languages    = languages;
            BuildUI();
        }

        private void BuildUI()
        {
            Text          = "Новая транзакция";
            Size          = new Size(1100, 760);
            StartPosition = FormStartPosition.CenterParent;
            BackColor     = Color.FromArgb(24, 24, 28);
            ForeColor     = Color.Gainsboro;
            Font          = new Font("Segoe UI", 10);

            int half = ClientSize.Width / 2 - 40;
            int y    = 20;

            // «Куда»
            Controls.Add(new Label { Text = "Куда", AutoSize = true, Left = 20, Top = y });
            cbTo.Left   = 150; cbTo.Top   = y - 3; cbTo.Width = half - 170;
            Controls.Add(cbTo);

            // «Откуда»
            Controls.Add(new Label { Text = "Откуда", AutoSize = true, Left = 20, Top = y + 35 });
            cbFrom.Left = 150; cbFrom.Top = y + 32; cbFrom.Width = half - 170;
            Controls.Add(cbFrom);

            // «Дата»
            Controls.Add(new Label { Text = "Дата", AutoSize = true, Left = 20, Top = y + 70 });
            dt.Left  = 150; dt.Top  = y + 67; dt.Width = 200;
            Controls.Add(dt);

            // «Количество»
            Controls.Add(new Label { Text = "Количество", AutoSize = true, Left = 20, Top = y + 105 });
            num.Left = 150; num.Top = y + 102; num.Width = 100;
            Controls.Add(num);

            // Чекбокс «сформировать книгу»
            chk.Left = 20; chk.Top = y + 150;
            Controls.Add(chk);

            // Панель деталей книги (скрыта по умолчанию)
            var bookPanel = new Panel
            {
                Left    = half + 40,
                Top     = 20,
                Width   = half,
                Height  = 500,
                Visible = false
            };
            int y2 = 0;
            bookPanel.Controls.AddRange(new Control[]
            {
                new Label{Text="ISBN", AutoSize=true, Left=0, Top=y2},
                tIsbn.At(120, y2-3, 320),

                new Label{Text="Издатель", AutoSize=true, Left=0, Top=y2+=35},
                cbPublisher.At(120, y2-3, 320),

                new Label{Text="Год", AutoSize=true, Left=0, Top=y2+=35},
                tYear.At(120, y2-3, 120),

                new Label{Text="Языки", AutoSize=true, Left=0, Top=y2+=35},
                clLanguages.At(120, y2-3, 200),
                new Label{Text="Название", AutoSize=true, Left=0, Top=y2+=35},
                tTitle.At(120, y2-3, 320),

                new Label{Text="Описание", AutoSize=true, Left=0, Top=y2+=35},
                tDescription.At(120, y2-3, 320),

                new Label{Text="Страниц", AutoSize=true, Left=0, Top=y2+=tDescription.Height+10},
                tPages.At(120, y2-3, 120),

                new Label{Text="Обложка", AutoSize=true, Left=0, Top=y2+=35},
                tCover.At(120, y2-3, 200),
                btnBrowse.At(330, y2-4, 80),
                new Label{Text="Жанры", AutoSize=true, Left=0, Top=y2+=35},
                clGenres.At(120, y2-3, 320)
            });
            Controls.Add(bookPanel);

            // Открываем панель деталей при чек-боксе
            chk.CheckedChanged += (_, __) => bookPanel.Visible = chk.Checked;

            btnBrowse.Click += ChooseCover;

            Shown += async (_, __) =>
            {
                await LoadLocationsAsync();
                await LoadLookupDataAsync();
            };

            var btnOk = new Button
            {
                Text         = "Сохранить",
                DialogResult = DialogResult.OK,
                Left         = Width - 180,
                Top          = Height - 90,
                Width        = 160,
                Height       = 54,
                BackColor    = Color.FromArgb(98,0,238),
                ForeColor    = Color.White,
                FlatStyle    = FlatStyle.Flat
            };
            Controls.Add(btnOk);
            btnOk.Click += async (_, __) => await SaveAsync();
        }

        private async Task LoadLocationsAsync()
        {
            var list = await _locations.GetAllAsync(null, 0);
            cbTo.DataSource       = list.ToList();
            cbTo.DisplayMember    = nameof(ModelLocation.LocationName);
            cbFrom.DataSource     = list.ToList();
            cbFrom.DisplayMember  = nameof(ModelLocation.LocationName);
        }

        private async Task LoadLookupDataAsync()
        {
            cbPublisher.DataSource = (await _publishers.GetAllAsync()).ToList();
            cbPublisher.DisplayMember = nameof(Publisher.Name);

            clLanguages.Items.Clear();
            foreach (var l in await _languages.GetAllAsync()) clLanguages.Items.Add(l);
            clLanguages.DisplayMember = nameof(LanguageCode.Code);

            clGenres.Items.Clear();
            foreach (var g in await _genres.GetAllAsync()) clGenres.Items.Add(g);
            clGenres.DisplayMember = nameof(Genre.Name);
        }

        private void ChooseCover(object? sender, EventArgs e)
        {
            using var od = new OpenFileDialog
            {
                Title  = "Выберите изображение обложки",
                Filter = "Картинки|*.jpg;*.jpeg;*.png;*.bmp|Все файлы|*.*"
            };
            if (od.ShowDialog(this) == DialogResult.OK)
                tCover.Text = od.FileName;
        }

        private async Task SaveAsync()
        {
            if (cbTo.SelectedItem is not ModelLocation toLoc ||
                cbFrom.SelectedItem is not ModelLocation fromLoc)
            {
                MessageBox.Show("Выберите места");
                DialogResult = DialogResult.None;
                return;
            }

            long bookId;
            if (chk.Checked)
            {
                var book = new Book
                {
                    ISBN        = tIsbn.Text,
                    Title       = tTitle.Text,
                    PublishYear = (int)tYear.Value,
                    Description = tDescription.Text,
                    Pages       = (int)tPages.Value,
                    CoverUrl    = "no_cover.png",
                    PublisherId = cbPublisher.SelectedItem is Publisher p ? p.PublisherId : null,
                };

                var langIds = clLanguages.CheckedItems.Cast<LanguageCode>().Select(l => l.LangId).ToList();
                if (langIds.Count > 0)
                    book.LangId = langIds[0];
                foreach (var id in langIds)
                    book.Languages.Add(new BookLanguage { LangId = id });

                foreach (var item in clGenres.CheckedItems)
                    if (item is Genre g)
                        book.Genres.Add(new GenreBook { GenreId = g.GenreId });
                await using var db = await _db.CreateDbContextAsync();
                db.Books.Add(book);
                await db.SaveChangesAsync();
                bookId = book.BookId;
            }
            else
            {
                // упрощённо
                bookId = 1;
            }

            var tran = new ModelInventoryTransaction
            {
                BookId         = bookId,
                LocationId     = toLoc.LocationId,
                PrevLocationId = fromLoc.LocationId,
                Date           = DateOnly.FromDateTime(dt.Value.Date),
                Amount         = (int)num.Value
            };
            await _transactions.AddAsync(tran);
        }
    }
}
