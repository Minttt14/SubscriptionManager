using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using SubscriptionManager.Data;
using SubscriptionManager.Helpers;

namespace SubscriptionManager.Forms
{
    public class SubscriptionPage : UserControl
    {
        private TextBox txtSearch;
        private Button btnSearch;
        private Button btnAdd;
        private Button btnEdit;
        private Button btnDelete;
        private DataGridView dgvSubscriptions;
        private Label lblCount;

        private int _selectedSubID = -1;

        public SubscriptionPage() { InitializeComponent(); }

        private void InitializeComponent()
        {
            this.Size = new Size(800, 600);
            this.BackColor = Color.FromArgb(245, 245, 247);
            this.Padding = new Padding(16);

            int ty = 16;
            var lblSearch = new Label { Text = "搜尋：", Location = new Point(16, ty + 6), AutoSize = true };

            txtSearch = new TextBox { Location = new Point(66, ty), Size = new Size(200, 28) };
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LoadData(txtSearch.Text.Trim()); };

            btnSearch = CreateButton("查詢", 280, ty, 70, false);
            btnSearch.Click += (s, e) => LoadData(txtSearch.Text.Trim());

            btnAdd = CreateButton("＋ 新增", 380, ty, 90, true);
            btnEdit = CreateButton("✎ 編輯", 484, ty, 80, false);
            btnDelete = CreateButton("✕ 刪除", 576, ty, 80, false);

            btnAdd.Click += BtnAdd_Click;
            btnEdit.Click += BtnEdit_Click;
            btnDelete.Click += BtnDelete_Click;

            dgvSubscriptions = new DataGridView
            {
                Location = new Point(16, 58),
                Size = new Size(this.Width - 32, this.Height - 110),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Microsoft JhengHei UI", 9.5F),
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(230, 230, 230),
                RowTemplate = { Height = 36 }
            };
            dgvSubscriptions.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft JhengHei UI", 9.5F, FontStyle.Bold);
            dgvSubscriptions.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 245);
            dgvSubscriptions.EnableHeadersVisualStyles = false;

            // ── 關鍵修復：資料綁定完成後自動清除預設的藍底選取 ──
            dgvSubscriptions.DataBindingComplete += (s, e) => dgvSubscriptions.ClearSelection();

            dgvSubscriptions.SelectionChanged += (s, e) => {
                if (dgvSubscriptions.SelectedRows.Count > 0)
                    _selectedSubID = dgvSubscriptions.SelectedRows[0].Cells["SubID"].Value != null
                        ? Convert.ToInt32(dgvSubscriptions.SelectedRows[0].Cells["SubID"].Value) : -1;
            };

            dgvSubscriptions.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) BtnEdit_Click(s, e); };

            lblCount = new Label
            {
                Text = "",
                Location = new Point(16, this.Height - 40),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                AutoSize = true,
                ForeColor = Color.FromArgb(140, 140, 140),
                Font = new Font("Microsoft JhengHei UI", 8.5F)
            };

            this.Controls.AddRange(new Control[] { lblSearch, txtSearch, btnSearch, btnAdd, btnEdit, btnDelete, dgvSubscriptions, lblCount });
        }

        public void LoadData(string keyword = "")
        {
            int userId = Session.CurrentUser.UserID;

            string sql = @"
                SELECT
                    SubID,
                    Name        AS '名稱',
                    Cost        AS '金額（NT$）',
                    Period      AS '週期',
                    CAST(StartDate AS DATE) AS '開始訂閱日',
                    ReminderDays AS '提前提醒(天)',
                    Notes       AS '備註'
                FROM Subscriptions
                WHERE UserID = @uid AND IsActive = 1
                  AND (@kw = '' OR Name LIKE '%' + @kw + '%')
                ORDER BY StartDate DESC";

            var dt = DatabaseHelper.ExecuteQuery(sql, new[] {
                new SqlParameter("@uid", userId),
                new SqlParameter("@kw",  keyword)
            });

            dgvSubscriptions.DataSource = dt;

            if (dgvSubscriptions.Columns["SubID"] != null) dgvSubscriptions.Columns["SubID"].Visible = false;

            lblCount.Text = $"共 {dt.Rows.Count} 筆";
            _selectedSubID = -1;
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            var form = new AddEditForm();
            if (form.ShowDialog(this) == DialogResult.OK) LoadData();
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (_selectedSubID < 0) { MessageBox.Show("請先選取一筆訂閱。"); return; }
            var form = new AddEditForm(_selectedSubID);
            if (form.ShowDialog(this) == DialogResult.OK) LoadData();
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedSubID < 0) { MessageBox.Show("請先選取一筆訂閱。"); return; }
            if (MessageBox.Show("確定要刪除這筆訂閱嗎？", "確認", MessageBoxButtons.YesNo) != DialogResult.Yes) return;

            int affected = DatabaseHelper.ExecuteNonQuery(
                "UPDATE Subscriptions SET IsActive = 0 WHERE SubID = @id AND UserID = @uid",
                new[] { new SqlParameter("@id", _selectedSubID), new SqlParameter("@uid", Session.CurrentUser.UserID) });

            if (affected > 0) LoadData();
        }

        private Button CreateButton(string text, int x, int y, int width, bool isPrimary)
        {
            var btn = new Button { Text = text, Location = new Point(x, y), Size = new Size(width, 30), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Microsoft JhengHei UI", 9.5F) };
            btn.FlatAppearance.BorderSize = 1;
            if (isPrimary)
            {
                btn.BackColor = Color.FromArgb(24, 95, 165);
                btn.ForeColor = Color.White;
                btn.FlatAppearance.BorderColor = Color.FromArgb(24, 95, 165);
            }
            return btn;
        }
    }
}