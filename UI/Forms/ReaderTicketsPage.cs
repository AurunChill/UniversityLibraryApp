using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibraryApp.Data;
using LibraryApp.Data.Models;

namespace LibraryApp.UI.Forms
{
    /// <summary>
    /// Страница управления читательскими билетами:
    /// – Отображение списка билетов в виде таблицы.
    /// – Поиск по ФИО, e-mail или телефону.
    /// – Сортировка по клику на заголовок колонки.
    /// – Добавление и удаление билетов.
    /// </summary>
    public sealed class ReaderTicketsPage : Form
    {
        // ───────── Поля ─────────

        private readonly BindingSource _bs = new();
        private DataGridView _grid = null!;
        private TextBox _txtSearchTickets = null!;
        private string _sortColumn = nameof(ReaderTicket.ReaderTicketId);
        private SortOrder _sortOrder = SortOrder.Ascending;

        // ───────── Конструктор ─────────

        public ReaderTicketsPage() => BuildUI();

        // ───────── UI ─────────

        /// <summary>
        /// Построить интерфейс формы: инициализировать форму, заголовок,
        /// поле поиска, метку сортировки, таблицу и кнопки.
        /// </summary>
        private void BuildUI()
        {
            InitializeForm();

            // 1) Заголовок
            var header = CreateHeaderLabel();
            Controls.Add(header);

            // 2) Поле поиска
            _txtSearchTickets = CreateSearchTextBox(header.Bottom + 15);
            Controls.Add(_txtSearchTickets);

            // 3) Метка-подсказка о сортировке
            var sortHint = CreateSortHintLabel(_txtSearchTickets.Bottom + 10);
            Controls.Add(sortHint);

            // 4) Таблица (DataGridView) сразу под sortHint
            _grid = CreateDarkGrid(sortHint.Bottom + 10);
            _grid.ColumnHeaderMouseClick += Grid_ColumnHeaderMouseClick!;
            Controls.Add(_grid);

            // 5) Кнопки «Добавить» и «Удалить» ниже таблицы
            var (btnAdd, btnDel) = CreateActionButtons();
            Controls.AddRange(new Control[] { btnAdd, btnDel });

            // 6) При показе формы — загружаем данные
            Shown += async (_, __) => await RefreshGridAsync();
        }

        /// <summary>
        /// Настраивает основные свойства формы (размер, цвета, шрифт и т.д.).
        /// </summary>
        private void InitializeForm()
        {
            Text = "Читательские билеты";
            MinimumSize = new Size(900, 650);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(24, 24, 28);
            ForeColor = Color.Gainsboro;
            Font = new Font("Segoe UI", 10);
        }

        /// <summary>
        /// Создает Label-заголовок страницы.
        /// </summary>
        private Label CreateHeaderLabel()
        {
            return new Label
            {
                Text = Text,
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Left = 20,
                Top = 20
            };
        }

        /// <summary>
        /// Создает TextBox для поиска билетов, ставит его на указанную позицию Top
        /// и навешивает обработчик TextChanged (RefreshGridAsync).
        /// </summary>
        private TextBox CreateSearchTextBox(int top)
        {
            var txt = new TextBox
            {
                PlaceholderText = "  Поиск по ФИО, e-mail или телефону…",
                Left = 20,
                Top = top,
                Width = 800,
                Height = 30
            };
            txt.TextChanged += async (_, __) => await RefreshGridAsync();
            return txt;
        }

        /// <summary>
        /// Создает Label-подсказку о том, что клик по заголовку колонки меняет сортировку.
        /// </summary>
        private Label CreateSortHintLabel(int top)
        {
            return new Label
            {
                Text = "Нажмите на заголовок колонки для сортировки",
                Left = 20,
                Top = top,
                AutoSize = true,
                ForeColor = Color.Gainsboro
            };
        }

        /// <summary>
        /// Создает DataGridView с тёмной темой, привязывает BindingSource и
        /// выставляет его на указанную позицию Top.
        /// </summary>
        private DataGridView CreateDarkGrid(int top)
        {
            var gv = new DataGridView
            {
                Left = 20,
                Top = top,
                Width = 800,
                Height = 350,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoGenerateColumns = true,
                ReadOnly = true,
                DataSource = _bs,
                BackgroundColor = Color.FromArgb(40, 40, 46),
                ForeColor = Color.Gainsboro,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            };

            // Базовый тёмный стиль ячеек
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

        /// <summary>
        /// Создает кнопки «Добавить» и «Удалить», задает их позиции под таблицей,
        /// навешивает обработчики и возвращает их в кортеже.
        /// </summary>
        private (Button btnAdd, Button btnDel) CreateActionButtons()
        {
            var btnAdd = MakeButton("Добавить",
                                    left: _grid.Left,
                                    top: _grid.Bottom + 15,
                                    onClick: async (_, __) =>
                                    {
                                        using var dlg = new ReaderTicketDialog();
                                        if (dlg.ShowDialog(this) == DialogResult.OK)
                                            await RefreshGridAsync();
                                    });

            var btnDel = MakeButton("Удалить",
                                    left: btnAdd.Right + 10,
                                    top: btnAdd.Top,
                                    onClick: async (_, __) =>
                                    {
                                        if (await DeleteTicketAsync())
                                            await RefreshGridAsync();
                                    });

            return (btnAdd, btnDel);
        }

        /// <summary>
        /// Упрощенная фабрика для создания кнопки с текстом, позицией и обработчиком клика.
        /// </summary>
        private Button MakeButton(string text, int left, int top, EventHandler onClick)
        {
            var btn = new Button
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
            btn.Click += onClick;
            return btn;
        }

        // ───────── Логика загрузки/фильтрации/сортировки ─────────

        /// <summary>
        /// Обновляет содержимое DataGridView:
        /// 1) Получает все билеты из базы.
        /// 2) Фильтрует по тексту из _txtSearchTickets (ФИО, e-mail, телефон, доп.телефон).
        /// 3) Сортирует по _sortColumn и _sortOrder.
        /// 4) Передает результат в BindingSource.
        /// </summary>
        private async Task RefreshGridAsync()
        {
            var all = await ReaderTickets.GetAllAsync(null, 0);

            // Фильтрация
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

            // Сортировка
            if (!string.IsNullOrEmpty(_sortColumn) && _sortOrder != SortOrder.None)
            {
                switch (_sortColumn)
                {
                    case nameof(ReaderTicket.ReaderTicketId):
                        all = _sortOrder == SortOrder.Ascending
                            ? all.OrderBy(r => r.ReaderTicketId).ToList()
                            : all.OrderByDescending(r => r.ReaderTicketId).ToList();
                        break;

                    case nameof(ReaderTicket.FullName):
                        all = _sortOrder == SortOrder.Ascending
                            ? all.OrderBy(r => r.FullName).ToList()
                            : all.OrderByDescending(r => r.FullName).ToList();
                        break;

                    case nameof(ReaderTicket.Email):
                        all = _sortOrder == SortOrder.Ascending
                            ? all.OrderBy(r => r.Email).ToList()
                            : all.OrderByDescending(r => r.Email).ToList();
                        break;

                    case nameof(ReaderTicket.PhoneNumber):
                        all = _sortOrder == SortOrder.Ascending
                            ? all.OrderBy(r => r.PhoneNumber).ToList()
                            : all.OrderByDescending(r => r.PhoneNumber).ToList();
                        break;

                    case nameof(ReaderTicket.ExtraPhoneNumber):
                        all = _sortOrder == SortOrder.Ascending
                            ? all.OrderBy(r => r.ExtraPhoneNumber).ToList()
                            : all.OrderByDescending(r => r.ExtraPhoneNumber).ToList();
                        break;
                }
            }

            // Обновление BindingSource
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

        /// <summary>
        /// Обработчик клика по заголовку колонки DataGridView:
        /// – Если тот же столбец, меняет направление сортировки;
        /// – Если новый столбец, устанавливает сортировку Ascending;
        /// – Повторно вызывает RefreshGridAsync().
        /// </summary>
        private async void Grid_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            var column = _grid.Columns[e.ColumnIndex];
            var propName = column.DataPropertyName;

            if (_sortColumn == propName)
            {
                _sortOrder = _sortOrder == SortOrder.Ascending
                                 ? SortOrder.Descending
                                 : SortOrder.Ascending;
            }
            else
            {
                _sortColumn = propName;
                _sortOrder = SortOrder.Ascending;
            }

            await RefreshGridAsync();
        }

        /// <summary>
        /// Удаляет выбранный билет:
        /// – Получает ID из текущей строки таблицы;
        /// – Спрашивает подтверждение через MessageBox;
        /// – Если подтверждено, вызывает ReaderTickets.DeleteAsync(id).
        /// </summary>
        private async Task<bool> DeleteTicketAsync()
        {
            if (_grid.CurrentRow?.Cells["ReaderTicketId"].Value is not long id)
                return false;

            var res = MessageBox.Show(
                "Удалить билет?",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (res != DialogResult.Yes)
                return false;

            return await ReaderTickets.DeleteAsync(id);
        }
    }

    // ───────── Диалог создания нового читательского билета ─────────

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
            Controls.AddRange(new Control[]
            {
                CreateLabel("ФИО", 20, y),
                SetupTextBox(tName, 150, y - 3, 240),

                CreateLabel("E-mail", 20, y += 35),
                SetupTextBox(tEmail, 150, y - 3, 240),

                CreateLabel("Телефон", 20, y += 35),
                SetupTextBox(tPhone, 150, y - 3, 240),

                CreateLabel("Доп.тел", 20, y += 35),
                SetupTextBox(tExtra, 150, y - 3, 240)
            });

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

        private static Label CreateLabel(string text, int x, int y) => new()
        {
            Text = text,
            AutoSize = true,
            Left = x,
            Top = y
        };

        private static TextBox SetupTextBox(TextBox tb, int x, int y, int width)
        {
            tb.Left = x;
            tb.Top = y;
            tb.Width = width;
            return tb;
        }
    }
}
