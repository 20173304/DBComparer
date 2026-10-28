using System;
using System.Data;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBComparer
{
    internal sealed partial class MainForm : Form
    {
        string connection;
        bool busy;
        List<ValueRule> valueRules = new List<ValueRule>();
        public MainForm()
        {
            InitializeWindow();
            BuildLayout();
            BindEvents();
            ApplyTheme();
        }

        private void BindEvents()
        {
            windows.CheckedChanged += (s, e) =>
            {
                user.Enabled = password.Enabled = !windows.Checked;
                InvalidateConnection();
            };
            server.TextChanged += (s, e) => InvalidateConnection();
            user.TextChanged += (s, e) => InvalidateConnection();
            password.TextChanged += (s, e) => InvalidateConnection();
            connect.Click += async (s, e) => await Execute(ConnectToServerAsync);
            leftDb.SelectedIndexChanged += async (s, e) => { if (!busy) await LoadTables(leftDb, leftTable); };
            rightDb.SelectedIndexChanged += async (s, e) => { if (!busy) await LoadTables(rightDb, rightTable); };
            leftTable.SelectedIndexChanged += async (s, e) => { if (!busy) await LoadColumns(); };
            rightTable.SelectedIndexChanged += async (s, e) => { if (!busy) await LoadColumns(); };
            leftWhere.TextChanged += (s, e) => ClearResult();
            rightWhere.TextChanged += (s, e) => ClearResult();
            columns.ItemCheck += (s, e) => ClearResult();
            keys.ItemCheck += (s, e) => ClearResult();
            limit.ValueChanged += (s, e) => ClearResult();
            compare.Click += async (s, e) => await Execute(Compare);
            refreshColumns.Click += async (s, e) => await LoadColumns();
            editRules.Click += (s, e) => EditValueRules();
            grid.DataBindingComplete += (s, e) =>
            {
                grid.CurrentCell = null;
                grid.ClearSelection();
            };
        }

        private async Task ConnectToServerAsync()
        {
            InvalidateConnection();
            try
            {
                var builder = new SqlConnectionStringBuilder { DataSource = server.Text.Trim(), InitialCatalog = "master", IntegratedSecurity = windows.Checked, ConnectTimeout = 10, ApplicationName = "DBComparer" };
                if (!windows.Checked)
                {
                    builder.UserID = user.Text;
                    builder.Password = password.Text;
                }
                var candidate = builder.ConnectionString;
                var names = await Database.Databases(candidate);
                connection = candidate;
                leftDb.Items.AddRange(names.Cast<object>().ToArray());
                rightDb.Items.AddRange(names.Cast<object>().ToArray());
            }
            catch (Exception ex)
            {
                InvalidateConnection();
                throw new InvalidOperationException("DB 연결에 실패했습니다.\n\n" + ex.Message, ex);
            }
            status.Text = "서버 연결 성공. 비교할 두 DB를 선택하세요.";
            MessageBox.Show(this, "DB에 연결되었습니다.\n비교할 DB1과 DB2를 선택하세요.", "DB 연결 성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void EditValueRules()
        {
            if (columns.Items.Count == 0)
            {
                MessageBox.Show(this, "먼저 테이블1과 테이블2를 선택하세요.");
                return;
            }
            using (var dialog = new ValueRulesForm(columns.Items.Cast<string>().ToArray(), valueRules))
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    valueRules = dialog.Rules;
                    editRules.Text = "값 변환 규칙 (" + valueRules.Count + ")";
                    ClearResult();
                }
        }

        private void ClearResult()
        {
            if (grid.DataSource != null)
                grid.DataSource = null;
            const string message = "설정 변경 후 데이터 비교를 실행하세요.";
            if (summary.Text != message)
                summary.Text = message;
        }

        private void ClearRules()
        {
            valueRules.Clear();
            editRules.Text = "값 변환 규칙 (0)";
        }

        private void InvalidateConnection()
        {
            connection = null;
            leftDb.Items.Clear();
            rightDb.Items.Clear();
            leftTable.SetTables(new TableName[0]);
            rightTable.SetTables(new TableName[0]);
            columns.Items.Clear();
            keys.Items.Clear();
            ClearResult();
            ClearRules();
            status.Text = "연결을 확인하세요.";
        }

        private async Task Execute(Func<Task> action)
        {
            if (busy)
                return;
            busy = true;
            var inputFilter = new BusyInputFilter(controls);
            Application.AddMessageFilter(inputFilter);
            UseWaitCursor = true;
            status.Text = "처리 중...";
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                ClearResult();
                status.Text = "실패: " + ex.Message;
                MessageBox.Show(this, ex.Message, "DBComparer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Application.RemoveMessageFilter(inputFilter);
                busy = false;
                if (!IsDisposed)
                {
                    UseWaitCursor = false;
                }
            }
        }

        private async Task LoadTables(ComboBox db, TablePicker table)
        {
            await Execute(async () =>
            {
                table.SetTables(new TableName[0]);
                columns.Items.Clear();
                keys.Items.Clear();
                ClearResult();
                ClearRules();
                if (db.SelectedItem == null || connection == null)
                    return;
                table.SetTables(await Database.Tables(Database.InDatabase(connection, db.Text)));
                status.Text = "테이블 목록 조회 완료.";
            });
        }

        private async Task LoadColumns()
        {
            await Execute(async () =>
            {
                columns.Items.Clear();
                keys.Items.Clear();
                ClearResult();
                ClearRules();
                if (leftTable.SelectedItem == null || rightTable.SelectedItem == null)
                {
                    status.Text = "테이블1과 테이블2을 모두 선택해야 공통 컬럼을 표시합니다.";
                    summary.Text = status.Text;
                    return;
                }
                var db1Columns = await Database.Columns(Database.InDatabase(connection, leftDb.Text), (TableName)leftTable.SelectedItem);
                var db2Columns = await Database.Columns(Database.InDatabase(connection, rightDb.Text), (TableName)rightTable.SelectedItem);
                columns.BeginUpdate();
                keys.BeginUpdate();
                try
                {
                    foreach (var name in db1Columns.Intersect(db2Columns, StringComparer.Ordinal))
                    {
                        columns.Items.Add(name, true);
                        keys.Items.Add(name, false);
                    }
                }
                finally { columns.EndUpdate(); keys.EndUpdate(); }
                if (columns.Items.Count == 0)
                {
                    var message = "이름이 정확히 일치하는 공통 컬럼이 없습니다.\nDB1: " + string.Join(", ", db1Columns) + "\nDB2: " + string.Join(", ", db2Columns);
                    status.Text = "공통 컬럼 없음 · 양쪽 컬럼 이름을 확인하세요.";
                    summary.Text = status.Text;
                    MessageBox.Show(this, message, "컬럼 조회 결과", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                status.Text = "공통 컬럼 " + columns.Items.Count + "개. 비교 키와 조회 조건을 지정하세요.";
                summary.Text = status.Text;
            });
        }

        private async Task Compare()
        {
            ClearResult();
            if (connection == null)
                throw new InvalidOperationException("먼저 연결 확인을 실행하세요.");
            if (leftTable.SelectedItem == null || rightTable.SelectedItem == null)
                throw new InvalidOperationException("양쪽 테이블을 선택하세요.");
            if (leftDb.Text == rightDb.Text)
                throw new InvalidOperationException("서로 다른 DB를 선택하세요.");
            var selectedColumns = columns.CheckedItems.Cast<string>().ToArray();
            var keyColumns = keys.CheckedItems.Cast<string>().ToArray();
            if (selectedColumns.Length == 0 || keyColumns.Length == 0)
                throw new InvalidOperationException("비교 컬럼과 비교 키를 각각 하나 이상 선택하세요.");
            var queryColumns = selectedColumns.Union(keyColumns).ToArray();
            int rowLimit = (int)limit.Value;
            var db1Connection = Database.InDatabase(connection, leftDb.Text);
            var db2Connection = Database.InDatabase(connection, rightDb.Text);
            var db1Table = (TableName)leftTable.SelectedItem;
            var db2Table = (TableName)rightTable.SelectedItem;
            var db1Filter = leftWhere.Text.Trim();
            var db2Filter = rightWhere.Text.Trim();
            var tables = await Task.WhenAll(Task.Run(() => Database.Read(db1Connection, db1Table, queryColumns, db1Filter, rowLimit)), Task.Run(() => Database.Read(db2Connection, db2Table, queryColumns, db2Filter, rowLimit)));
            if (tables.Any(t => t.Rows.Count > rowLimit))
                throw new InvalidOperationException("조회 결과가 최대 행 수를 초과했습니다. 조건을 좁히거나 한도를 늘리세요. 일부 데이터로 일치 판정하지 않습니다.");
            var result = await Task.Run(() => Comparison.Run(tables[0], tables[1], queryColumns, keyColumns, valueRules));
            ShowComparisonResult(result, tables);
        }

        private void ShowComparisonResult(ComparisonResult result, DataTable[] tables)
        {
            grid.DataSource = result.Details;
            if (grid.Columns.Contains("DB2 기대 값"))
                grid.Columns["DB2 기대 값"].Visible = valueRules.Count > 0;
            var verdict = result.Different + result.LeftOnly + result.RightOnly == 0 ? "일치" : "불일치";
            if (tables.All(t => t.Rows.Count == 0))
                verdict = "양쪽 조회 결과 없음";
            summary.Text = verdict + " | 일치 " + result.Equal + "행 / 값 불일치 " + result.Different + "행 (" + result.DifferentCells + "셀, " + result.DifferentColumns.Count + "종류의 컬럼) / DB1에만 " + result.LeftOnly + "행 / DB2에만 " + result.RightOnly + "행";
            status.Text = "비교 완료 · DB1 " + tables[0].Rows.Count + "행, DB2 " + tables[1].Rows.Count + "행 · 변환 규칙 " + valueRules.Count + "개 / 변환 기대 값 일치 " + result.ConvertedCells + "셀";
        }
    }
}
