using System.Drawing;
using System.Windows.Forms;

namespace DBComparer
{
    internal static class BlueTheme
    {
        static readonly Color Background = Color.FromArgb(228, 230, 227);
        static readonly Color Surface = Color.FromArgb(247, 247, 242);
        static readonly Color Text = Color.FromArgb(40, 49, 49);
        static readonly Color Border = Color.FromArgb(143, 155, 151);
        static readonly Color Accent = Color.FromArgb(36, 101, 94);
        static readonly Color Header = Color.FromArgb(211, 222, 216);
        static readonly Color Selected = Color.FromArgb(220, 221, 217);
        static readonly Color OnAccent = Color.FromArgb(250, 250, 245);

        public static void ResultSummary(Control control)
        {
            control.BackColor = Header;
            control.ForeColor = Text;
        }

        public static Color MutedText
        {
            get
            {
                return Color.FromArgb(79, 95, 89);
            }
        }

        public static void Apply(Control control)
        {
            control.ForeColor = Text;
            control.BackColor = Background;
            var button = control as Button;
            var table = control as DataGridView;
            if (button != null)
            {
                button.UseVisualStyleBackColor = false;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = Border;
                button.MinimumSize = new Size(0, 34);
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(211, 222, 216);
                button.FlatAppearance.MouseDownBackColor = Color.FromArgb(192, 209, 199);
                button.BackColor = Color.FromArgb(237, 239, 233);
            }
            else if (table != null)
            {
                table.BackgroundColor = Surface;
                table.BorderStyle = BorderStyle.FixedSingle;
                table.GridColor = Color.FromArgb(211, 217, 210);
                table.EnableHeadersVisualStyles = false;
                table.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
                table.ColumnHeadersDefaultCellStyle.BackColor = Header;
                table.ColumnHeadersDefaultCellStyle.ForeColor = Text;
                table.ColumnHeadersDefaultCellStyle.SelectionBackColor = Header;
                table.ColumnHeadersDefaultCellStyle.SelectionForeColor = Text;
                table.ColumnHeadersDefaultCellStyle.Padding = new Padding(3);
                table.ColumnHeadersDefaultCellStyle.Font = new Font(table.Font, FontStyle.Bold);
                table.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
                table.DefaultCellStyle.BackColor = Surface;
                table.DefaultCellStyle.ForeColor = Text;
                table.DefaultCellStyle.SelectionBackColor = Selected;
                table.DefaultCellStyle.SelectionForeColor = Text;
                table.AlternatingRowsDefaultCellStyle.BackColor = Surface;
                table.RowHeadersDefaultCellStyle.BackColor = Surface;
                table.RowHeadersDefaultCellStyle.ForeColor = Text;
                table.RowHeadersDefaultCellStyle.SelectionBackColor = Selected;
                table.RowHeadersDefaultCellStyle.SelectionForeColor = Text;
                table.RowTemplate.Height = 34;
                table.DefaultCellStyle.Padding = new Padding(5, 2, 5, 2);
                // DataGridView creates its own editing controls and scroll bars.
                return;
            }
            else if (control is TextBoxBase || control is ComboBox || control is ListBox || control is NumericUpDown)
            {
                control.BackColor = Surface;
                var combo = control as ComboBox;
                if (combo != null)
                    combo.FlatStyle = FlatStyle.Flat;
                var textBox = control as TextBoxBase;
                if (textBox != null)
                    textBox.BorderStyle = BorderStyle.Fixed3D;
                var list = control as ListBox;
                if (list != null)
                    list.BorderStyle = BorderStyle.Fixed3D;
                return;
            }
            foreach (Control child in control.Controls)
                Apply(child);
        }

        public static void Primary(Button button)
        {
            button.BackColor = Accent;
            button.ForeColor = OnAccent;
            button.FlatAppearance.BorderColor = Accent;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(29, 86, 80);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(23, 72, 67);
        }
    }
}
