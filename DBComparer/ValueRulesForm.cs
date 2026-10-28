using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBComparer
{
    internal sealed class ValueRulesForm : Form
    {
        public List<ValueRule> Rules
        {
            get; private set;
        }
        public ValueRulesForm(string[] columns, IEnumerable<ValueRule> rules)
        {
            Text = "값 변환 규칙";
            Width = 760;
            Height = 420;
            MinimumSize = new Size(650, 300);
            Font = new Font("맑은 고딕", 10F);
            StartPosition = FormStartPosition.CenterParent;
            var layout = new BufferedLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), RowCount = 3, ColumnCount = 1 };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            Controls.Add(layout);
            layout.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "DB1의 값을 지정한 DB2 기대 값으로 비교합니다. 예: A / TEST / MIC\n문자열 전체가 정확히 일치할 때만 적용합니다. 빈 값은 빈 문자열이며, 규칙은 연쇄 적용하지 않습니다.\n비교 키에는 적용할 수 없습니다. 행을 선택하고 Delete를 누르면 규칙을 삭제합니다." }, 0, 0);
            var grid = new DataGridView { Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AllowUserToAddRows = true, AllowUserToDeleteRows = true };
            var column = new DataGridViewComboBoxColumn { Name = "Column", HeaderText = "컬럼", FlatStyle = FlatStyle.Flat };
            column.Items.AddRange(columns.Cast<object>().ToArray());
            grid.Columns.Add(column);
            grid.Columns.Add("Source", "DB1 값 (변환 전)");
            grid.Columns.Add("Target", "DB2 기대 값 (변환 후)");
            foreach (var rule in rules)
                grid.Rows.Add(rule.Column, rule.Source, rule.Target);
            layout.Controls.Add(grid, 0, 1);
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 6, 0, 0) };
            var cancel = new Button { Text = "취소", DialogResult = DialogResult.Cancel };
            var save = new Button { Text = "적용" };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(save);
            layout.Controls.Add(buttons, 0, 2);
            CancelButton = cancel;
            save.Click += (s, e) =>
            {
                if (!grid.EndEdit())
                    return;
                var result = new List<ValueRule>();
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.IsNewRow)
                        continue;
                    var name = Convert.ToString(row.Cells[0].Value);
                    if (string.IsNullOrEmpty(name))
                    {
                        MessageBox.Show(this, "각 규칙의 컬럼을 선택하세요.");
                        return;
                    }
                    var source = Convert.ToString(row.Cells[1].Value);
                    var target = Convert.ToString(row.Cells[2].Value);
                    if (result.Any(r => r.Column == name && r.Source == source))
                    {
                        MessageBox.Show(this, "같은 컬럼과 DB1 값은 한 번만 등록하세요.");
                        return;
                    }
                    result.Add(new ValueRule { Column = name, Source = source, Target = target });
                }
                Rules = result;
                DialogResult = DialogResult.OK;
            };
            BlueTheme.Apply(this);
            BlueTheme.Primary(save);
        }
    }
}
