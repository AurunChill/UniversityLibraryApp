using System.Drawing;
using System.Windows.Forms;
using LibraryApp.UI.Helpers;

namespace LibraryApp.UI.Forms;

public abstract class TablePageBase : Form
{
    protected TablePageBase()
    {
        BackColor = Color.FromArgb(24, 24, 28);
        ForeColor = Color.Gainsboro;
        Font = new Font("Segoe UI", 10);
    }

    protected DataGridView CreateGrid(int top, BindingSource bs)
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
            DataSource = bs,
            BackgroundColor = Color.FromArgb(40, 40, 46),
            ForeColor = Color.Gainsboro,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        };
        var style = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(40, 40, 46),
            ForeColor = Color.Gainsboro,
            SelectionBackColor = Color.FromArgb(98, 0, 238),
            SelectionForeColor = Color.White
        };
        gv.DefaultCellStyle = style;
        gv.RowsDefaultCellStyle = style;
        gv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle(style)
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

    protected Button MakeButton(string text, int left, int top, EventHandler onClick)
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
}
