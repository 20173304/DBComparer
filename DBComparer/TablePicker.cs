using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBComparer
{
    internal sealed class TablePicker : UserControl
    {
        readonly TextBox search = new TextBox { Dock = DockStyle.Fill };
        readonly ComboBox list = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, MaxDropDownItems = 20 };
        readonly Label count = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
        List<TableName> tables = new List<TableName>();
        bool updating;
        public event EventHandler SelectedIndexChanged;
        public object SelectedItem
        {
            get
            {
                return list.SelectedItem;
            }
        }

        public TablePicker()
        {
            DoubleBuffered = true;
            Dock = DockStyle.Fill;
            Margin = Padding.Empty;
            search.Margin = new Padding(0, 0, 8, 4);
            list.Margin = Padding.Empty;
            count.Margin = Padding.Empty;
            var layout = new BufferedLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2, Margin = Padding.Empty };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            layout.Controls.Add(new Label { Text = "검색", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            layout.Controls.Add(search, 1, 0);
            layout.Controls.Add(count, 2, 0);
            layout.Controls.Add(list, 0, 1);
            layout.SetColumnSpan(list, 3);
            Controls.Add(layout);
            search.TextChanged += (s, e) => { if (!updating) Filter(); };
            list.SelectedIndexChanged += (s, e) => { if (!updating) OnSelectionChanged(); };
            SetTables(new TableName[0]);
        }
        public void SetTables(IEnumerable<TableName> values)
        {
            updating = true;
            try
            {
                search.Clear();
                tables = values.ToList();
            }
            finally { updating = false; }
            Filter(true);
        }
        void Filter(bool resetSelection = false)
        {
            var previous = list.SelectedItem;
            var matches = tables.Where(t => t.ToString().IndexOf(search.Text.Trim(), StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            updating = true;
            list.BeginUpdate();
            try
            {
                list.Items.Clear();
                list.Items.AddRange(matches.Cast<object>().ToArray());
                if (!resetSelection && previous != null && matches.Contains(previous))
                    list.SelectedItem = previous;
                else
                    list.SelectedIndex = -1;
                count.Text = matches.Length + " / " + tables.Count + "개";
            }
            finally { list.EndUpdate(); updating = false; }
            if (!ReferenceEquals(previous, list.SelectedItem))
                OnSelectionChanged();
        }
        void OnSelectionChanged()
        {
            var handler = SelectedIndexChanged;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }
    }
}
