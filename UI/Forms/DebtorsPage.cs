using LibraryApp.Data;
using LibraryApp.Data.Models;

namespace LibraryApp.UI.Forms
{
    /// <summary>
    /// Страница управления должниками:
    /// – Отображение списка должников в виде таблицы.
    /// – Поиск по читателю или названию книги.
    /// – Сортировка по клику на заголовок колонки.
    /// – Добавление, редактирование и удаление записей должников.
    /// </summary>
    public sealed class DebtorsPage : Form
    {
        // ───────── Поля ─────────

        private readonly BindingSource _bs = new();
        private DarkGrid _grid = null!;
        private TextBox _txtSearchDebtors = null!;
        private string _sortColumn = nameof(Debtor.DebtorId);
        private SortOrder _sortOrder = SortOrder.Ascending;

        // ───────── Конструктор ─────────

        public DebtorsPage() => BuildUI();

        // ───────── UI ─────────

        /// <summary>
        /// Построить интерфейс формы: инициализировать форму, заголовок,
        /// поле поиска, метку сортировки, таблицу и кнопки.
        /// </summary>
        private void BuildUI()
        {
            InitializeFormProperties();

            // 1) Заголовок страницы
            var header = CreateHeaderLabel("Должники", top: 20);
            Controls.Add(header);

            // 2) Поле поиска должников
            _txtSearchDebtors = CreateSearchTextBox(
                placeholder: "  Поиск по читателю или книге…",
                top: header.Bottom + 15,
                width: 900);
            Controls.Add(_txtSearchDebtors);
            _txtSearchDebtors.TextChanged += async (_, __) => await RefreshGridAsync();

            // 3) Метка-подсказка о том, что клик по заголовку колонки меняет сортировку
            var sortHint = CreateInfoLabel(
                text: "Нажмите на заголовок колонки для сортировки",
                top: _txtSearchDebtors.Bottom + 10);
            Controls.Add(sortHint);

            // 4) Таблица должников (DarkGrid) сразу под sortHint
            _grid = new DarkGrid(top: sortHint.Bottom + 10, bs: _bs);
            _grid.ColumnHeaderMouseClick += Grid_ColumnHeaderMouseClick!;
            Controls.Add(_grid);

            // 5) Кнопки «Добавить», «Изменить», «Удалить» под таблицей
            var btnAdd = CreateActionButton("Добавить", left: _grid.Left, top: _grid.Bottom + 15);
            btnAdd.Click += async (_, __) =>
            {
                using var dlg = new DebtorDialog();
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    await RefreshGridAsync();
            };

            var btnEdit = CreateActionButton("Изменить", left: btnAdd.Right + 10, top: btnAdd.Top);
            btnEdit.Click += async (_, __) => await EditCurrentAsync();

            var btnDel = CreateActionButton("Удалить", left: btnEdit.Right + 10, top: btnEdit.Top);
            btnDel.Click += async (_, __) =>
            {
                if (await DeleteCurrentAsync())
                    await RefreshGridAsync();
            };

            Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDel });

            // 6) При показе формы – загружаем данные
            Shown += async (_, __) => await RefreshGridAsync();
        }

        /// <summary>
        /// Настроить основные свойства формы (размер, цвета, шрифт и т.д.).
        /// </summary>
        private void InitializeFormProperties()
        {
            Text = "Должники";
            MinimumSize = new Size(1400, 750);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(24, 24, 28);
            ForeColor = Color.Gainsboro;
            Font = new Font("Segoe UI", 10);
        }

        /// <summary>
        /// Создать Label-заголовок страницы с указанным текстом.
        /// </summary>
        private Label CreateHeaderLabel(string text, int top)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Left = 20,
                Top = top
            };
        }

        /// <summary>
        /// Создать TextBox для поиска должников, выставить его на указанную позицию Top,
        /// задать ширину и высоту, а также навесить обработчик TextChanged.
        /// </summary>
        private TextBox CreateSearchTextBox(string placeholder, int top, int width)
        {
            var txt = new TextBox
            {
                PlaceholderText = placeholder,
                Left = 20,
                Top = top,
                Width = width,
                Height = 30
            };
            // Обработчик обновления грида при изменении текста
            txt.TextChanged += async (_, __) => await RefreshGridAsync();
            return txt;
        }

        /// <summary>
        /// Создать Label-информацию (например, "Нажмите на заголовок колонки для сортировки").
        /// </summary>
        private Label CreateInfoLabel(string text, int top)
        {
            return new Label
            {
                Text = text,
                Left = 20,
                Top = top,
                AutoSize = true,
                ForeColor = Color.Gainsboro
            };
        }

        /// <summary>
        /// Создать кнопку действия (Добавить/Изменить/Удалить) с указанным текстом и позицией.
        /// </summary>
        private Button CreateActionButton(string text, int left, int top)
        {
            return new Button
            {
                Text = text,
                Left = left,
                Top = top,
                Width = 110,
                Height = 36,
                BackColor = Color.FromArgb(98, 0, 238),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
        }

        // ───────── Логика обновления/фильтрации/сортировки ─────────

        /// <summary>
        /// Обновляет содержимое DataGridView:
        /// 1) Получить всех должников из базы.
        /// 2) Отфильтровать по введенному тексту (ФИО читателя или название книги).
        /// 3) Отсортировать по _sortColumn и _sortOrder.
        /// 4) Передать проекцию в BindingSource.
        /// </summary>
        private async Task RefreshGridAsync()
        {
            var all = await Debtors.GetAllAsync(null, 0);

            // Фильтрация по строке поиска (reader или book)
            string filter = _txtSearchDebtors.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(filter))
            {
                all = all
                    .Where(d =>
                        (d.ReaderTicket?.FullName?.ToLower().Contains(filter) ?? false)
                        || (d.Book?.Title?.ToLower().Contains(filter) ?? false))
                    .ToList();
            }

            // Сортировка по выбранной колонке и направлению
            if (!string.IsNullOrEmpty(_sortColumn) && _sortOrder != SortOrder.None)
            {
                switch (_sortColumn)
                {
                    case nameof(Debtor.DebtorId):
                        all = _sortOrder == SortOrder.Ascending
                            ? all.OrderBy(d => d.DebtorId).ToList()
                            : all.OrderByDescending(d => d.DebtorId).ToList();
                        break;
                    case "Reader":
                        all = _sortOrder == SortOrder.Ascending
                            ? all.OrderBy(d => d.ReaderTicket!.FullName).ToList()
                            : all.OrderByDescending(d => d.ReaderTicket!.FullName).ToList();
                        break;
                    case "Book":
                        all = _sortOrder == SortOrder.Ascending
                            ? all.OrderBy(d => d.Book!.Title).ToList()
                            : all.OrderByDescending(d => d.Book!.Title).ToList();
                        break;
                    case nameof(Debtor.GetDate):
                        all = _sortOrder == SortOrder.Ascending
                            ? all.OrderBy(d => d.GetDate).ToList()
                            : all.OrderByDescending(d => d.GetDate).ToList();
                        break;
                    case nameof(Debtor.DebtDate):
                        all = _sortOrder == SortOrder.Ascending
                            ? all.OrderBy(d => d.DebtDate).ToList()
                            : all.OrderByDescending(d => d.DebtDate).ToList();
                        break;
                    case nameof(Debtor.ReturnDate):
                        all = _sortOrder == SortOrder.Ascending
                            ? all.OrderBy(d => d.ReturnDate).ToList()
                            : all.OrderByDescending(d => d.ReturnDate).ToList();
                        break;
                    case nameof(Debtor.DaysUntilDebt):
                        all = _sortOrder == SortOrder.Ascending
                            ? all.OrderBy(d => d.DaysUntilDebt).ToList()
                            : all.OrderByDescending(d => d.DaysUntilDebt).ToList();
                        break;
                    case "Status":
                        all = _sortOrder == SortOrder.Ascending
                            ? all.OrderBy(d => d.Status).ToList()
                            : all.OrderByDescending(d => d.Status).ToList();
                        break;
                    case "LatePenalty":
                        all = _sortOrder == SortOrder.Ascending
                            ? all.OrderBy(d => d.LatePenalty).ToList()
                            : all.OrderByDescending(d => d.LatePenalty).ToList();
                        break;
                }
            }

            // Проекция без ReaderTicketId и BookId
            _bs.DataSource = all
                .Select(d => new
                {
                    d.DebtorId,
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

        /// <summary>
        /// Обработчик клика по заголовку колонки:
        /// – Если клик по той же колонке, переключает _sortOrder.
        /// – Если новая колонка, сохраняет _sortColumn и ставит Ascending.
        /// – Вызывает RefreshGridAsync().
        /// </summary>
        private async void Grid_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            var column = _grid.Columns[e.ColumnIndex];
            var propName = column.DataPropertyName;

            if (_sortColumn == propName)
                _sortOrder = _sortOrder == SortOrder.Ascending
                                 ? SortOrder.Descending
                                 : SortOrder.Ascending;
            else
            {
                _sortColumn = propName;
                _sortOrder = SortOrder.Ascending;
            }

            await RefreshGridAsync();
        }

        /// <summary>
        /// Удаляет текущую выделенную запись должника:
        /// – Получает ID из текущей строки грида.
        /// – Спрашивает подтверждение через MessageBox.
        /// – Если подтверждено, вызывает Debtors.DeleteAsync(id).
        /// </summary>
        private async Task<bool> DeleteCurrentAsync()
        {
            if (_grid.CurrentRow?.Cells["DebtorId"].Value is not long id)
                return false;

            var res = MessageBox.Show(
                "Удалить запись?",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (res != DialogResult.Yes)
                return false;

            return await Debtors.DeleteAsync(id);
        }

        /// <summary>
        /// Редактирует текущую выделенную запись должника:
        /// – Получает ID из текущей строки грида.
        /// – Загружает сущность из базы через Debtors.GetByIdAsync(id).
        /// – Открывает DebtorDialog в режиме редактирования. 
        /// – При успешном сохранении снова вызывает RefreshGridAsync().
        /// </summary>
        private async Task EditCurrentAsync()
        {
            if (_grid.CurrentRow?.Cells["DebtorId"].Value is not long id)
                return;

            var entity = await Debtors.GetByIdAsync(id);
            if (entity is null)
                return;

            using var dlg = new DebtorDialog(entity);
            if (dlg.ShowDialog(this) == DialogResult.OK)
                await RefreshGridAsync();
        }

        // ───────── DarkGrid (тёмная таблица) ─────────

        /// <summary>
        /// Вложенный класс-наследник DataGridView с заранее настроенной тёмной темой.
        /// </summary>
        private sealed class DarkGrid : DataGridView
        {
            public DarkGrid(int top, BindingSource bs)
            {
                Left = 20;
                Top = top;
                Width = 1400;
                Height = 450;
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

                AutoGenerateColumns = true;
                ReadOnly = true;
                DataSource = bs;

                BackgroundColor = Color.FromArgb(40, 40, 46);
                ForeColor = Color.Gainsboro;

                DefaultCellStyle = GetDarkStyle();
                RowsDefaultCellStyle = GetDarkStyle();
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle(GetDarkStyle())
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

            private static DataGridViewCellStyle GetDarkStyle() => new()
            {
                BackColor = Color.FromArgb(40, 40, 46),
                ForeColor = Color.Gainsboro,
                SelectionBackColor = Color.FromArgb(98, 0, 238),
                SelectionForeColor = Color.White
            };
        }
    }

    // ───────── Диалог добавления/редактирования должника ─────────

    internal sealed class DebtorDialog : Form
    {
        private readonly Debtor _model;

        /* Поля для поиска читательских билетов */
        private readonly TextBox _tTicketSearch = new() { PlaceholderText = "  Поиск билета…" };
        private readonly ListBox _lstTickets = new() { Height = 100 };

        /* Поля для поиска книг */
        private readonly TextBox _tBookSearch = new() { PlaceholderText = "  Поиск книги…" };
        private readonly ListBox _lstBooks = new() { Height = 100 };

        /* Поля для дат, штрафа и статуса */
        private readonly DateTimePicker _dtGet = new() { Value = DateTime.Today };
        private readonly DateTimePicker _dtDebt = new() { Value = DateTime.Today.AddDays(14) };
        private readonly DateTimePicker _dtReturn = new(); // по умолчанию пустой
        private readonly ComboBox _cbStatus = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly NumericUpDown _numPenalty = new() { Minimum = 0, Maximum = 10000, DecimalPlaces = 2 };

        /* Поле для отображения дней до/после долга */
        private readonly Label _lblDays = new() { AutoSize = true };

        private ReaderTicket? _selTicket;
        private Book? _selBook;

        public DebtorDialog(Debtor? existing = null)
        {
            _model = existing ?? new Debtor { Status = DebtorStatus.В_Срок.Text() };
            BuildDialogUI();
        }

        /// <summary>
        /// Построить интерфейс диалога:
        /// – Поля для выбора читательского билета (поиск + список).
        /// – Поля для выбора книги (поиск + список).
        /// – Дата выдачи, дата должен вернуть, фактический возврат.
        /// – Штраф и статус.
        /// – Метка с калькуляцией дней до/после долга.
        /// – Кнопка «Сохранить».
        /// – Логика загрузки, поиска и заполнения в режиме редактирования.
        /// </summary>
        private async void BuildDialogUI()
        {
            Text = "Должник";
            Size = new Size(600, 720);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(40, 40, 46);
            ForeColor = Color.Gainsboro;
            Font = new Font("Segoe UI", 10);

            // Заполнить ComboBox статусами должника
            _cbStatus.Items.AddRange(Enum.GetValues(typeof(DebtorStatus)).Cast<object>().ToArray());
            _cbStatus.SelectedItem = Enum.GetValues(typeof(DebtorStatus))
                .Cast<DebtorStatus>()
                .FirstOrDefault(s => s.Text() == _model.Status);

            int y = 20;

            // Поле выбора ReaderTicket
            Controls.Add(CreateLabel("Билет", 20, y));
            PositionControl(_tTicketSearch, 140, y - 3, 420);
            Controls.Add(_tTicketSearch);
            PositionControl(_lstTickets, 140, y += 30, 420);
            Controls.Add(_lstTickets);

            // Поле выбора Book
            Controls.Add(CreateLabel("Книга", 20, y += _lstTickets.Height + 10));
            PositionControl(_tBookSearch, 140, y - 3, 420);
            Controls.Add(_tBookSearch);
            PositionControl(_lstBooks, 140, y += 30, 420);
            Controls.Add(_lstBooks);

            // Поле GetDate
            Controls.Add(CreateLabel("Дата выдачи", 20, y += _lstBooks.Height + 10));
            PositionControl(_dtGet, 140, y - 3, 200);
            Controls.Add(_dtGet);

            // Поле DebtDate
            Controls.Add(CreateLabel("Вернуть до", 20, y += 35));
            PositionControl(_dtDebt, 140, y - 3, 200);
            Controls.Add(_dtDebt);

            // Поле ReturnDate
            Controls.Add(CreateLabel("Фактический возврат", 20, y += 35));
            _dtReturn.CustomFormat = " ";
            _dtReturn.Format = DateTimePickerFormat.Custom;
            PositionControl(_dtReturn, 200, y - 3, 200);
            Controls.Add(_dtReturn);

            // Поле штрафа
            Controls.Add(CreateLabel("Штраф, ₽", 20, y += 35));
            PositionControl(_numPenalty, 140, y - 3, 100);
            Controls.Add(_numPenalty);

            // Поле статуса
            Controls.Add(CreateLabel("Статус", 20, y += 35));
            PositionControl(_cbStatus, 140, y - 3, 250);
            Controls.Add(_cbStatus);

            // Поле дней до/после долга
            Controls.Add(CreateLabel("Дней до / после долга:", 20, y += 35));
            PositionControl(_lblDays, 240, y);
            Controls.Add(_lblDays);

            // Кнопка «Сохранить»
            var btnOk = new Button
            {
                Text = "Сохранить",
                DialogResult = DialogResult.OK,
                Width = 160,
                Height = 46,
                BackColor = Color.FromArgb(98, 0, 238),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            PositionControl(btnOk, x: Width / 2 - 80, y: y + 45);
            Controls.Add(btnOk);

            // Загрузка списков ReaderTickets и Books
            _lstTickets.DataSource = (await ReaderTickets.GetAllAsync(null, 0)).ToList();
            _lstTickets.DisplayMember = "FullName";
            _lstBooks.DataSource = (await Books.GetAllAsync(null, 0)).ToList();
            _lstBooks.DisplayMember = "Title";

            // Поиск в списках
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

            // Если режим редактирования (_model.DebtorId != 0), заполняем контролы
            if (_model.DebtorId != 0)
            {
                _selTicket = _lstTickets.Items.Cast<ReaderTicket>()
                    .FirstOrDefault(t => t.ReaderTicketId == _model.ReaderTicketId);
                _lstTickets.SelectedItem = _selTicket;

                _selBook = _lstBooks.Items.Cast<Book>()
                    .FirstOrDefault(b => b.BookId == _model.BookId);
                _lstBooks.SelectedItem = _selBook;

                _dtGet.Value = _model.GetDate.ToDateTime(TimeOnly.MinValue);
                _dtDebt.Value = _model.DebtDate.ToDateTime(TimeOnly.MinValue);

                if (_model.ReturnDate is { } ret)
                {
                    _dtReturn.Format = DateTimePickerFormat.Long;
                    _dtReturn.Value = ret.ToDateTime(TimeOnly.MinValue);
                }

                _numPenalty.Value = (decimal)(_model.LatePenalty ?? 0);
            }

            // Пересчет дней
            void RecalcDays() => _lblDays.Text = CalculateDays().ToString();
            _dtDebt.ValueChanged += (_, __) => RecalcDays();
            _dtReturn.ValueChanged += (_, __) =>
            {
                _dtReturn.Format = DateTimePickerFormat.Long;
                RecalcDays();
            };
            RecalcDays();

            // Обработчик кнопки «Сохранить» – сохраняем новую или редактируем существующую запись
            btnOk.Click += async (_, __) =>
            {
                if (_selTicket is null || _selBook is null)
                {
                    MessageBox.Show("Выберите билет и книгу");
                    DialogResult = DialogResult.None;
                    return;
                }

                _model.ReaderTicketId = _selTicket.ReaderTicketId;
                _model.BookId = _selBook.BookId;
                _model.GetDate = DateOnly.FromDateTime(_dtGet.Value.Date);
                _model.DebtDate = DateOnly.FromDateTime(_dtDebt.Value.Date);
                _model.ReturnDate = _dtReturn.Format == DateTimePickerFormat.Long
                                      ? DateOnly.FromDateTime(_dtReturn.Value.Date)
                                      : null;
                _model.Status = ((DebtorStatus)_cbStatus.SelectedItem!).Text();
                _model.LatePenalty = (double)_numPenalty.Value;
                _model.DaysUntilDebt = CalculateDays();

                if (_model.DebtorId == 0)
                    await Debtors.AddAsync(_model);
                else
                    await Debtors.UpdateAsync(_model);
            };
        }

        // ───────── Логика и утилиты диалога ─────────

        /// <summary>
        /// Вычисляет дни до/после даты возврата (или до сегодня, если возврат не установлен).
        /// </summary>
        private int CalculateDays()
        {
            var refDate = _dtReturn.Format == DateTimePickerFormat.Long
                          ? _dtReturn.Value.Date
                          : DateTime.Today;
            return Math.Abs((refDate - _dtDebt.Value.Date).Days);
        }

        private static Label CreateLabel(string text, int x, int y) => new()
        {
            Text = text,
            AutoSize = true,
            Left = x,
            Top = y,
            ForeColor = Color.Gainsboro
        };

        private static void PositionControl(Control c, int x, int y, int? width = null)
        {
            c.Left = x;
            c.Top = y;
            if (width.HasValue)
                c.Width = width.Value;
        }
    }
}
