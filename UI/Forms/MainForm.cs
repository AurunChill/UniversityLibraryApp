using System.Drawing.Drawing2D;
using LibraryApp.Data.Models;
using LibraryApp.Data.Services;
using LibraryApp.UI.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryApp.UI.Forms
{
    public sealed class MainForm : Form
    {
        private readonly Rectangle _screenBounds;
        private readonly IServiceProvider _provider;
        private readonly BookService _books;
        private readonly InventoryTransactionService _transactions;
        private readonly GenreService _genres;
        private readonly LanguageCodeService _languages;
        private FlowLayoutPanel? _cardPanel;
        private Label? _countLabel;
        private TextBox? _search;
        private ComboBox? _sortCombo;
        private ToolStripDropDown? _genreDropDown;
        private CheckedListBox? _genreList;
        private ToolStripDropDown? _langDropDown;
        private CheckedListBox? _langList;
        private Button? _clearButton;
        private readonly List<long> _selectedGenres = new();
        private readonly List<long> _selectedLangs = new();
        private bool _sortByTitleAscending = true;

        public MainForm(IServiceProvider provider, BookService books,
            InventoryTransactionService transactions,
            GenreService genres,
            LanguageCodeService languages)
        {
            _provider = provider;
            _books = books;
            _transactions = transactions;
            _genres = genres;
            _languages = languages;
            _screenBounds = Screen.PrimaryScreen!.Bounds;
            InitializeComponent();
        }

        #region Инициализация и Построение UI

        /// <summary>
        /// Настраивает основные свойства формы и добавляет все элементы управления.
        /// </summary>
        private void InitializeComponent()
        {
            Text = "Библиотека+";
            MinimumSize = new Size(1280, 800);
            WindowState = FormWindowState.Maximized;
            BackColor = Color.FromArgb(24, 24, 28);

            // Создаём строку навигации (табы вверху)
            var navigation = CreateNavigationRow();

            // Заголовок "Главная"
            var mainLabel = CreateMainLabel(marginTopStart: navigation.Bottom);

            // Поле поиска
            _search = CreateSearchBox(marginTopStart: mainLabel.Bottom);

            // Панель фильтров (жанры, языки, сортировка)
            var filterPanel = CreateFilterRow(marginTopStart: _search.Bottom);

            // Статус-панель (отображает количество найденных книг и «Обновить»)
            var statusPanel = CreateStatusPanel(marginTopStart: filterPanel.Bottom);

            // Панель карточек с обложками книг
            _cardPanel = CreateCardPanel(margintopStart: statusPanel.Bottom);

            // Добавляем всё на форму
            Controls.Add(navigation);
            Controls.Add(mainLabel);
            Controls.Add(_search);
            Controls.Add(filterPanel);
            Controls.Add(statusPanel);
            Controls.Add(_cardPanel);

            ActiveControl = mainLabel;

            // По показу формы загружаем карточки
            Shown += async (_, __) => await LoadCardsAsync();
            Activated += async (_, __) => await LoadCardsAsync(_search!.Text);
        }

        /// <summary>
        /// Создаёт FlowLayoutPanel с «табами» навигации (Главная, Поставки, Должники, Читательские билеты).
        /// </summary>
        private FlowLayoutPanel CreateNavigationRow(int marginTopStart = 0)
        {
            var panel = new FlowLayoutPanel
            {
                Top = marginTopStart + 8,
                Width = _screenBounds.Width,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent
            };

            Color accent = Color.FromArgb(98, 0, 238);
            string[] navPages = { "Инвентарь", "Долги", "Читатели" };

            foreach (string navPage in navPages)
            {
                bool isSelected = navPage == "Инвентарь";
                var lbl = new Label
                {
                    Text = navPage,
                    AutoSize = true,
                    Font = new Font("Segoe UI", 11, isSelected ? FontStyle.Bold : FontStyle.Regular),
                    ForeColor = isSelected ? accent : Color.Gainsboro,
                    Cursor = Cursors.Hand
                };

                lbl.Click += (_, __) =>
                {
                    switch (navPage)
                    {
                        case "Инвентарь":
                            using (var f = _provider.GetRequiredService<InventoryPage>())
                                f.ShowDialog(this);
                            break;
                        case "Долги":
                            using (var f = _provider.GetRequiredService<DebtsPage>())
                                f.ShowDialog(this);
                            break;
                        case "Читатели":
                            using (var f = _provider.GetRequiredService<ReadersPage>())
                                f.ShowDialog(this);
                            break;
                    }
                };

                panel.Controls.Add(lbl);
            }

            return panel;
        }

        /// <summary>
        /// Создаёт большой заголовок "Главная" по центру экрана.
        /// </summary>
        private Label CreateMainLabel(int marginTopStart = 0)
        {
            var label = new Label
            {
                Top = marginTopStart + 20,
                Width = _screenBounds.Width,
                Height = 60,
                Text = "Главная",
                Font = new Font("Segoe UI", 30, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter
            };
            return label;
        }

        /// <summary>
        /// Создаёт TextBox для поиска и задаёт ему события (Enter → поиск).
        /// </summary>
        private TextBox CreateSearchBox(int marginTopStart = 0)
        {
            var searchBox = new TextBox
            {
                Top = marginTopStart + 18,
                Left = (_screenBounds.Width - (int)(_screenBounds.Width * 0.45)) / 2,
                Width = (int)(_screenBounds.Width * 0.45),
                Height = 45,
                AutoSize = false,
                Multiline = false,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(55, 55, 60),
                ForeColor = Color.White,
                MaxLength = 75,
                PlaceholderText = "  Поиск...",
                Font = new Font("Segoe UI", 14, FontStyle.Regular)
            };
            searchBox.RoundCorners(10);

            // При нажатии Enter — перезагружаем карточки с новым запросом
            searchBox.KeyDown += async (sender, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                    await LoadCardsAsync(query: searchBox.Text);
            };

            return searchBox;
        }

        /// <summary>
        /// Создаёт панель фильтров: жанр, язык и сортировка.
        /// </summary>
        private Panel CreateFilterRow(int marginTopStart = 0)
        {
            var panel = new Panel
            {
                Top = marginTopStart + 8,
                Left = _search!.Left,
                Width = _search.Width,
                Height = 40,
                BackColor = Color.Transparent
            };

            var genreBtn = new Button
            {
                Text = "Жанры",
                Width = 120,
                Height = 32,
                Left = 0,
                Top = 4,
                BackColor = Color.FromArgb(55,55,60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            genreBtn.Click += async (_,__) => await ShowGenreDropDown(genreBtn);

            var langBtn = new Button
            {
                Text = "Языки",
                Width = 120,
                Height = 32,
                Left = genreBtn.Right + 10,
                Top = 4,
                BackColor = Color.FromArgb(55,55,60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            langBtn.Click += async (_,__) => await ShowLangDropDown(langBtn);

            _sortCombo = new ComboBox
            {
                Left = langBtn.Right + 10,
                Top = 4,
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(55,55,60),
                ForeColor = Color.White,
            };
            _sortCombo.Items.AddRange(new[] { "От A до Я", "От Я до А", "Дата добавления" });
            _sortCombo.SelectedIndex = 0;
            _sortCombo.SelectedIndexChanged += async (_,__) =>
                await LoadCardsAsync(_search!.Text);

            _clearButton = new Button
            {
                Text = "Очистить",
                Width = 100,
                Height = 32,
                Left = _sortCombo.Right + 10,
                Top = 4,
                BackColor = Color.FromArgb(98,0,238),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _clearButton.Click += async (_,__) =>
            {
                ClearFilters();
                await LoadCardsAsync(_search!.Text);
            };

            panel.Controls.AddRange(new Control[] { genreBtn, langBtn, _sortCombo, _clearButton });
            return panel;
        }

        /// <summary>
        /// Создаёт панель, в которой отображается число найденных книг, ссылка "Обновить список"
        /// и теперь место для кнопки сортировки.
        /// </summary>
        private Panel CreateStatusPanel(int marginTopStart = 0)
        {
            var panel = new Panel
            {
                Top = marginTopStart,
                Left = 0,
                Width = _screenBounds.Width,
                Height = 70, // Увеличили высоту, чтобы вместить кнопку
                BackColor = Color.Transparent
            };

            _countLabel = new Label
            {
                AutoSize = true,
                Left = 12,
                Top = 6,
                ForeColor = Color.Gainsboro,
                Font = new Font("Segoe UI", 9)
            };

            var refreshLink = new LinkLabel
            {
                Text = "Обновить список",
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Underline),
                LinkColor = Color.Gainsboro,
                ActiveLinkColor = Color.White
            };
            refreshLink.Left = panel.Width - refreshLink.PreferredWidth - 20;
            refreshLink.Top = 6;
            refreshLink.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            refreshLink.Click += async (_, __) => await LoadCardsAsync(query: _search!.Text);

            panel.Controls.AddRange(new Control[] { _countLabel, refreshLink });
            return panel;
        }

        /// <summary>
        /// Создаёт FlowLayoutPanel для карточек книг (с автопрокруткой).
        /// </summary>
        private FlowLayoutPanel CreateCardPanel(int margintopStart = 0)
        {
            var cards = new FlowLayoutPanel
            {
                Top = margintopStart + 18,
                Left = 75,
                Width = _screenBounds.Width,
                Height = _screenBounds.Height - (margintopStart + 150),
                AutoScroll = true,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent
            };
            return cards;
        }

        private async Task ShowGenreDropDown(Button anchor)
        {
            if (_genreDropDown == null)
            {
                _genreList = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, BackColor = Color.FromArgb(55,55,60), ForeColor = Color.White };
                var host = new ToolStripControlHost(_genreList) { AutoSize = false, Size = new Size(200, 200) };
                _genreDropDown = new ToolStripDropDown();
                _genreDropDown.Items.Add(host);
                _genreList.ItemCheck += async (_,__) =>
                {
                    await Task.Delay(10);
                    UpdateSelectedGenres();
                    await LoadCardsAsync(_search!.Text);
                };
                var all = await _genres.GetAllAsync();
                foreach (var g in all) _genreList.Items.Add(g, false);
            }
            _genreDropDown.Show(anchor, 0, anchor.Height);
        }

        private async Task ShowLangDropDown(Button anchor)
        {
            if (_langDropDown == null)
            {
                _langList = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, BackColor = Color.FromArgb(55,55,60), ForeColor = Color.White };
                var host = new ToolStripControlHost(_langList) { AutoSize = false, Size = new Size(200, 200) };
                _langDropDown = new ToolStripDropDown();
                _langDropDown.Items.Add(host);
                _langList.ItemCheck += async (_,__) =>
                {
                    await Task.Delay(10);
                    UpdateSelectedLangs();
                    await LoadCardsAsync(_search!.Text);
                };
                var all = await _languages.GetAllAsync();
                foreach (var l in all) _langList.Items.Add(l, false);
            }
            _langDropDown.Show(anchor, 0, anchor.Height);
        }

        private void UpdateSelectedGenres()
        {
            _selectedGenres.Clear();
            if (_genreList is null) return;
            foreach (var item in _genreList.CheckedItems)
                if (item is Genre g) _selectedGenres.Add(g.GenreId);
        }

        private void UpdateSelectedLangs()
        {
            _selectedLangs.Clear();
            if (_langList is null) return;
            foreach (var item in _langList.CheckedItems)
                if (item is LanguageCode l) _selectedLangs.Add(l.LangId);
        }

        private void ClearFilters()
        {
            _genreList?.ClearSelected();
            if (_genreList is not null)
                for (int i = 0; i < _genreList.Items.Count; i++)
                    _genreList.SetItemChecked(i, false);
            _langList?.ClearSelected();
            if (_langList is not null)
                for (int i = 0; i < _langList.Items.Count; i++)
                    _langList.SetItemChecked(i, false);
            _selectedGenres.Clear();
            _selectedLangs.Clear();
            if (_sortCombo is not null) _sortCombo.SelectedIndex = 0;
        }

        #endregion

        #region Загрузка карточек (Books)

        /// <summary>
        /// Загружает список книг из базы (с учётом full-text поиска) и отображает их карточки.
        /// </summary>
        private async Task LoadCardsAsync(string? query = null)
        {
            _cardPanel!.SuspendLayout();
            _cardPanel.Controls.Clear();

            var books = string.IsNullOrWhiteSpace(query)
                ? await _books.GetAllDetailedAsync()
                : await _books.FullTextAsync(query);

            if (_selectedGenres.Count > 0)
                books = books.Where(b => b.Genres.Any(g => _selectedGenres.Contains(g.GenreId))).ToList();

            if (_selectedLangs.Count > 0)
                books = books.Where(b => _selectedLangs.Contains(b.LangId)).ToList();

            if (_sortCombo is not null)
            {
                books = _sortCombo.SelectedIndex switch
                {
                    0 => books.OrderBy(b => b.Title, StringComparer.OrdinalIgnoreCase).ToList(),
                    1 => books.OrderByDescending(b => b.Title, StringComparer.OrdinalIgnoreCase).ToList(),
                    _ => books.OrderByDescending(b => b.BookId).ToList()
                };
            }

            foreach (var book in books)
            {
                var card = CreateBookCard(book);
                _cardPanel.Controls.Add(card);
            }

            _countLabel!.Text = $"Найдено: {_cardPanel.Controls.Count} шт.";
            _cardPanel.ResumeLayout();
        }

        /// <summary>
        /// Создаёт панель-карточку с обложкой, годом, автором и заголовком книги.
        /// </summary>
        private Panel CreateBookCard(Book book)
        {
            var card = new Panel
            {
                Size = new Size(260, 420),
                Margin = new Padding(15),
                BackColor = Color.FromArgb(40, 40, 46),
                Cursor = Cursors.Hand,
                Tag = book
            };
            card.RoundCorners(12);

            // Путь к изображению обложки (может отсутствовать)
            string? imgPath = null;
            if (!string.IsNullOrWhiteSpace(book.CoverUrl))
            {
                imgPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "AppData", "Media", "Covers",
                    book.CoverUrl
                );
            }

            var pic = new PictureBox
            {
                Dock = DockStyle.Top,
                Height = 320,
                Width = 260,
                SizeMode = PictureBoxSizeMode.AutoSize,
                BackColor = Color.FromArgb(40, 40, 46),
                Image = File.Exists(imgPath) ? Image.FromFile(imgPath) : null,
                Tag = book
            };
            pic.RoundCorners(12, topOnly: true);

            // Короткое отображение title/author
            string title = book.Title.Length > 15 ? book.Title[..12] + "..." : book.Title;
            string authorName = book.Authors?.FirstOrDefault()?.Author?.Name ?? "";
            string author = authorName.Length > 15 ? authorName[..12] + "..." : authorName;

            var textPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 8, 6, 0)
            };
            textPanel.Controls.Add(new Label
            {
                Text = book.PublishYear.ToString(),
                Dock = DockStyle.Top,
                Height = 18,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Silver
            });
            textPanel.Controls.Add(new Label
            {
                Text = author,
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.Gainsboro
            });
            textPanel.Controls.Add(new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 32,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.White
            });

            // Клик по любой части карточки открывает детали книги
            card.Click += OnCardClick;
            pic.Click += OnCardClick;
            textPanel.Click += OnCardClick;
            foreach (Control lbl in textPanel.Controls)
                lbl.Click += OnCardClick;

            card.Controls.AddRange(new Control[] { textPanel, pic });
            return card;
        }

        /// <summary>
        /// Обработчик клика по карточке или по её содержимому: открывает BookDetailForm.
        /// </summary>
        private void OnCardClick(object? sender, EventArgs e)
        {
            if (sender is Control c && c.Tag is Book b)
            {
                using var f = new BookDetailForm(b, _books, _transactions);
                f.ShowDialog(this);
            }
        }
        #endregion
    }
}


