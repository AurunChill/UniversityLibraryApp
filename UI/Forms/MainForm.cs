using System.Drawing.Drawing2D;
using LibraryApp.Data.Models;
using LibraryApp.Data.Services;
using LibraryApp.UI.Helpers;

namespace LibraryApp.UI.Forms
{
    public sealed class MainForm : Form
    {
        private readonly Rectangle _screenBounds;
        private readonly IServiceProvider _provider;
        private readonly BookService _books;
        private readonly InventoryTransactionService _transactions;
        private FlowLayoutPanel? _cardPanel;
        private Label? _countLabel;
        private TextBox? _search;
        private Button? _sortButton;
        private bool _sortByTitleAscending = true;

        public MainForm(IServiceProvider provider, BookService books, InventoryTransactionService transactions)
        {
            _provider = provider;
            _books = books;
            _transactions = transactions;
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

            // Статус-панель (отображает количество найденных книг и «Обновить»)
            var statusPanel = CreateStatusPanel(marginTopStart: _search.Bottom);

            // Кнопка сортировки
            _sortButton = new Button
            {
                Text = "Сортировать",
                Width = 130,
                Height = 32,
                // После увеличения высоты statusPanel теперь можно поместить кнопку внутри
                Left = statusPanel.Width - 150,
                Top = 35,
                BackColor = Color.FromArgb(98, 0, 238),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _sortButton.Click += async (_, __) =>
            {
                // Инвертируем направление и перезагружаем карточки
                _sortByTitleAscending = !_sortByTitleAscending;
                await LoadCardsAsync(query: _search!.Text);
            };
            statusPanel.Controls.Add(_sortButton);

            // Панель карточек с обложками книг
            _cardPanel = CreateCardPanel(margintopStart: statusPanel.Bottom);

            // Добавляем всё на форму
            Controls.Add(navigation);
            Controls.Add(mainLabel);
            Controls.Add(_search);
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

            if (_sortButton is not null)
            {
                books = _sortByTitleAscending
                    ? books.OrderBy(b => b.Title, StringComparer.OrdinalIgnoreCase).ToList()
                    : books.OrderByDescending(b => b.Title, StringComparer.OrdinalIgnoreCase).ToList();
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

            // Путь к изображению обложки
            string imgPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "AppData", "Media", "Covers",
                book.CoverUrl
            );

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
                f.Show();
            }
        }

        #endregion

