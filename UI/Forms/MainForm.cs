using System.Drawing.Drawing2D;
using LibraryApp.Data.Models;
using LibraryApp.Services.Interfaces;

namespace LibraryApp.UI.Forms;

public sealed class MainForm : Form
{
    private readonly IBookService _books;
    private readonly Rectangle _screenBounds;
    private FlowLayoutPanel? _cardPanel;
    private Label? _countLabel;

    public MainForm(IBookService bookService)
    {
        _books = bookService;
        _screenBounds = Screen.PrimaryScreen!.Bounds;
        BuildUI();
    }

    private void BuildUI()
    {
        Text = "Библиотека+";
        MinimumSize = new Size(1280, 800);
        WindowState = FormWindowState.Maximized;
        BackColor = Color.FromArgb(24, 24, 28);

        var navigation = GetNavigationRow();
        var mainLabel = GetMainLabel(marginTopStart: navigation.Top + navigation.Height);
        var search = GetSearchBox(marginTopStart: mainLabel.Height + mainLabel.Top);
        _countLabel = GetCountLabel(marginTopStart: search.Top + search.Height);
        _cardPanel = GetCardPanel(margintopStart: _countLabel.Top + _countLabel.Height);

        Controls.Add(navigation);
        Controls.Add(mainLabel);
        Controls.Add(search);
        Controls.Add(_countLabel);
        Controls.Add(_cardPanel);
        ActiveControl = mainLabel;

        Shown += async (_, __) => await GetCards();
    }

    private TextBox GetSearchBox(int marginTopStart = 0)
    {
        TextBox searchBox = new TextBox
        {
            Top = marginTopStart + 18,
            Left = (_screenBounds.Width - (int)(_screenBounds.Width * 0.45)) / 2,
            Width = (int)(_screenBounds.Width * 0.45),
            Height = 45,
            AutoSize = false,
            Multiline = false,
            BorderStyle = BorderStyle.None,
            BackColor   = Color.FromArgb(55, 55, 60),
            ForeColor   = Color.White,
            MaxLength = 75,
            PlaceholderText = "  Поиск...",
            Font = new Font("Segoe UI", 14, FontStyle.Regular)
        };
        Round(searchBox, 10);

        searchBox.KeyDown += async (sender, e) =>
        {
            if (e.KeyCode == Keys.Enter) {
                await GetCards(query: searchBox.Text);
            }
        };

        return searchBox;
    }

    private Label GetMainLabel(int marginTopStart = 0)
    {
        var label = new Label
        {
            Top = marginTopStart + 20,
            Width = _screenBounds.Width,
            Text = "Главная",
            Height = 60,
            Font = new Font("Segoe UI", 30, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter
        };

        return label;
    }

    private FlowLayoutPanel GetNavigationRow(int marginTopStart = 0)
    {
        var tabs = new FlowLayoutPanel
        {
            Top = marginTopStart + 8,
            Width = _screenBounds.Width,
            Height = 40,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent
        };
        Color accent = Color.FromArgb(98, 0, 238);  
        string[] navPages = ["Главная", "Поставки", "Должники", "Читательские билеты"];
        foreach (string navPage in navPages)
        {
            bool isSelected = navPage == "Главная";

            var l = new Label
            {
                Text = navPage,
                AutoSize = true,
                Font = new Font("Segoe UI", 11, isSelected ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = isSelected ? accent : Color.Gainsboro,
                Cursor = Cursors.Hand
            };
            l.Click += (_, __) => MessageBox.Show($"Переход на «{navPage}» (заглушка)");
            tabs.Controls.Add(l);
        }

        return tabs;
    }

    private Label GetCountLabel(int marginTopStart = 0) {
        var countLabel = new Label {
            Top = marginTopStart + 8,
            Left = 12,
            AutoSize = true,
            Font = new Font("Segoe UI", 9, FontStyle.Regular),
            ForeColor = Color.Gainsboro,
            Text = string.Empty
        };

        return countLabel;
    }

    private FlowLayoutPanel GetCardPanel(int margintopStart = 0) {
        var cards = new FlowLayoutPanel {
            Top = margintopStart + 18,
            Left = 75,
            Width = _screenBounds.Width,
            Height = _screenBounds.Height - (margintopStart + 18),
            AutoScroll = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent
        };

        return cards;
    }

    private async Task GetCards(string? query = null) {
        _cardPanel!.SuspendLayout();
        _cardPanel.Controls.Clear();

        var books = string.IsNullOrWhiteSpace(query)
                   ? await _books.GetAllAsync(null, 0)
                   : await _books.FullTextAsync(query);

        foreach (var book in books) {
            var card = GetCard(book: book);
            _cardPanel.Controls.Add(card);
        }

        _countLabel!.Text = $"Найдено: {_cardPanel.Controls.Count}шт.";

        _cardPanel.ResumeLayout();
    }

    private Panel GetCard(Book book) {
        var card = new Panel
        {
            Size = new Size(260, 420),
            Margin = new Padding(15),
            BackColor= Color.FromArgb(40, 40, 46), 
            Cursor = Cursors.Hand,
            Tag = book
        };
        Round(card, 12);

        string imgPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "AppData", "Media", "Covers", book.CoverUrl
        );

        var pic = new PictureBox {
            Dock = DockStyle.Top,
            Height = 320,
            Width = 260,
            SizeMode = PictureBoxSizeMode.AutoSize,
            BackColor = Color.FromArgb(40, 40, 46),
            Image = File.Exists(imgPath) ? Image.FromFile(imgPath) : null
        };
        Round(pic, 12, topOnly: true);

        string title = book.Title.Length > 25 ? book.Title[..22] + "..." : book.Title;
        string author = book.Author.Length > 25 ? book.Author[..22] + "..." : book.Author;

        var text = new Panel { 
            Dock = DockStyle.Fill, Padding = new Padding(10, 8, 6, 0) 
        };
        text.Controls.Add(
            new Label { 
                Text = book.PublishYear.ToString(), Dock = DockStyle.Top, Height = 18, 
                Font = new Font("Segoe UI", 9), ForeColor = Color.Silver 
            }
        );
        text.Controls.Add(
            new Label { 
                Text = author, Dock = DockStyle.Top, Height = 22, 
                Font = new Font("Segoe UI", 10), ForeColor = Color.Gainsboro
            }
        );
        text.Controls.Add(
            new Label { 
                Text = title, Dock = DockStyle.Top, Height = 32, 
                Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = Color.White
            }
        );

        card.Controls.AddRange([text, pic]);
        return card;
    }

    private static void Round(Control c, int r, bool topOnly = false)
    {
        var p = new GraphicsPath();
        var rc = c.ClientRectangle;
        if (topOnly)
        {
            p.AddArc(rc.X, rc.Y, r, r, 180, 90);
            p.AddArc(rc.Right - r, rc.Y, r, r, 270, 90);
            p.AddLine(rc.Right, rc.Y + r, rc.Right, rc.Bottom);
            p.AddLine(rc.Right, rc.Bottom, rc.X, rc.Bottom);
            p.AddLine(rc.X, rc.Bottom, rc.X, rc.Y + r);
        }
        else
        {
            p.AddArc(rc.X, rc.Y, r, r, 180, 90);
            p.AddArc(rc.Right - r, rc.Y, r, r, 270, 90);
            p.AddArc(rc.Right - r, rc.Bottom - r, r, r, 0, 90);
            p.AddArc(rc.X, rc.Bottom - r, r, r, 90, 90);
        }
        p.CloseFigure();
        c.Region = new Region(p);
    }
}
