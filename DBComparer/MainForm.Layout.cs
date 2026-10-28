using System.Drawing;
using System.Windows.Forms;

namespace DBComparer
{
    // 화면 배치와 스타일은 이 파일에서 관리합니다.
    internal sealed partial class MainForm
    {
        readonly TextBox server = new TextBox { Width = 190, Text = "localhost" };
        readonly CheckBox windows = new CheckBox { Text = "Windows 인증", Checked = true, AutoSize = true };
        readonly TextBox user = new TextBox { Width = 110, Enabled = false };
        readonly TextBox password = new TextBox { Width = 110, UseSystemPasswordChar = true, Enabled = false };
        readonly ComboBox leftDb = Combo(), rightDb = Combo();
        readonly TablePicker leftTable = new TablePicker(), rightTable = new TablePicker();
        readonly TextBox leftWhere = new TextBox { Dock = DockStyle.Fill }, rightWhere = new TextBox { Dock = DockStyle.Fill };
        readonly CheckedListBox columns = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true };
        readonly CheckedListBox keys = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true };
        readonly NumericUpDown limit = new NumericUpDown { Minimum = 1, Maximum = 1000000, Value = 10000, Width = 100 };
        readonly Label status = new Label { AutoSize = true, Text = "서버 정보를 입력하고 연결 확인을 누르세요.", Padding = new Padding(6) };
        readonly Label summary = new Label { AutoSize = true, Text = "비교 결과가 여기에 표시됩니다.", Padding = new Padding(6) };
        readonly DataGridView grid = new BufferedGrid { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false };
        readonly Panel controls = new BufferedPanel { Dock = DockStyle.Fill };
        readonly Button editRules = new Button { Text = "값 변환 규칙 (0)", AutoSize = true };
        static ComboBox Combo()
        {
            return new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
        }
        readonly Button connect = new Button { Text = "연결 확인", AutoSize = false };
        readonly Button compare = new Button { Text = "데이터 비교", AutoSize = true };
        readonly Button refreshColumns = new Button
        {
            Text = "컬럼 다시 불러오기",
            AutoSize = false,
            Dock = DockStyle.Fill,
            Margin = new Padding(8, 0, 0, 4)
        };

        private void InitializeWindow()
        {
            DoubleBuffered = true;
            Text = "DBComparer · MSSQL 테이블 비교";
            Width = 1280;
            Height = 960;
            MinimumSize = new Size(1050, 860);
            Font = new Font("맑은 고딕", 10F);
            StartPosition = FormStartPosition.CenterScreen;
        }

        private void BuildLayout()
        {
            var root = new BufferedLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, Padding = new Padding(12) };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 560));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            Controls.Add(root);
            root.Controls.Add(controls, 0, 0);
            root.Controls.Add(summary, 0, 1);
            root.Controls.Add(grid, 0, 2);
            root.Controls.Add(status, 0, 3);
            var layout = new BufferedLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 6 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            foreach (int height in new[] { 80, 70, 110, 70, 160, 60 })
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            controls.Controls.Add(layout);
            var auth = CreateConnectionPanel();
            layout.Controls.Add(auth, 0, 0);
            layout.SetColumnSpan(auth, 2);
            layout.Controls.Add(Field("DB1", leftDb), 0, 1);
            layout.Controls.Add(Field("DB2", rightDb), 1, 1);
            layout.Controls.Add(Field("테이블1", leftTable), 0, 2);
            layout.Controls.Add(Field("테이블2", rightTable), 1, 2);
            layout.Controls.Add(Field("테이블1 조건 (WHERE 제외)", leftWhere), 0, 3);
            layout.Controls.Add(Field("테이블2 조건 (WHERE 제외)", rightWhere), 1, 3);

            layout.Controls.Add(Field("비교할 공통 컬럼 (체크)", columns), 0, 4);
            layout.Controls.Add(Field("비교 키", keys, refreshColumns), 1, 4);
            var actions = CreateActionsPanel();
            layout.Controls.Add(actions, 0, 5);
            layout.SetColumnSpan(actions, 2);
        }

        private TableLayoutPanel CreateConnectionPanel()
        {
            var auth = new BufferedLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1, Margin = Padding.Empty };
            auth.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
            auth.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 168));
            auth.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
            auth.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
            auth.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132));
            auth.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            server.Dock = user.Dock = password.Dock = DockStyle.Fill;
            windows.Dock = DockStyle.Fill;
            windows.AutoSize = false;
            connect.Dock = DockStyle.Fill;
            auth.Controls.Add(Field("서버 IP / 인스턴스", server), 0, 0);
            auth.Controls.Add(Field("인증 방식", windows), 1, 0);
            auth.Controls.Add(Field("계정", user), 2, 0);
            auth.Controls.Add(Field("암호", password), 3, 0);
            auth.Controls.Add(Field(" ", connect), 4, 0);
            return auth;
        }

        private TableLayoutPanel CreateActionsPanel()
        {
            var actions = new BufferedLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1, Padding = new Padding(12, 8, 12, 8), Margin = Padding.Empty };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 162));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 158));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
            actions.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var limitLabel = Label("DB별 최대 조회 행 수");
            limitLabel.AutoSize = false;
            limitLabel.Dock = DockStyle.Fill;
            limitLabel.TextAlign = ContentAlignment.MiddleLeft;
            limitLabel.Padding = Padding.Empty;
            limit.Anchor = AnchorStyles.Left;
            foreach (var button in new[] { editRules, compare })
            {
                button.AutoSize = false;
                button.Dock = DockStyle.Fill;
                button.Margin = new Padding(6, 0, 0, 0);
            }
            actions.Controls.Add(limitLabel, 0, 0);
            actions.Controls.Add(limit, 1, 0);
            actions.Controls.Add(editRules, 3, 0);
            actions.Controls.Add(compare, 4, 0);
            return actions;
        }

        private void ApplyTheme()
        {
            BlueTheme.Apply(this);
            BlueTheme.Primary(connect);
            BlueTheme.Primary(compare);
            summary.AutoSize = false;
            summary.Dock = DockStyle.Fill;
            summary.TextAlign = ContentAlignment.MiddleLeft;
            BlueTheme.ResultSummary(summary);
            status.ForeColor = BlueTheme.MutedText;
        }

        static Label Label(string text)
        {
            return new Label { Text = text, AutoSize = true, Padding = new Padding(0, 5, 0, 0) };
        }
        static Control Field(string title, Control input, Button headerAction = null)
        {
            var panel = new BufferedLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(8), Margin = new Padding(4, 0, 4, 4) };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, input is CheckedListBox ? 40 : 27));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var titleLabel = Label(title);
            titleLabel.Font = new Font("맑은 고딕", 10F, FontStyle.Bold);
            titleLabel.Padding = Padding.Empty;
            titleLabel.Margin = Padding.Empty;
            titleLabel.Dock = DockStyle.Fill;
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            input.Margin = Padding.Empty;
            if (headerAction == null)
                panel.Controls.Add(titleLabel, 0, 0);
            else
            {
                var header = new BufferedLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
                header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 172));
                header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                header.Controls.Add(titleLabel, 0, 0);
                header.Controls.Add(headerAction, 1, 0);
                panel.Controls.Add(header, 0, 0);
            }
            panel.Controls.Add(input, 0, 1);
            return panel;
        }
    }
}
