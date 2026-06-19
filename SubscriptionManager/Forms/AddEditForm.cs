using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using SubscriptionManager.Data;
using SubscriptionManager.Helpers;

namespace SubscriptionManager.Forms
{
    public class AddEditForm : Form
    {
        private int _subId;
        private TextBox txtName, txtCost, txtNotes;
        private ComboBox cmbPeriod;
        private DateTimePicker dtpStartDate;
        private NumericUpDown nudReminderDays;

        public AddEditForm(int subId = -1)
        {
            _subId = subId;
            InitializeComponent();
            if (_subId > 0) LoadExisting();
        }

        private void InitializeComponent()
        {
            this.Text = _subId < 0 ? "新增訂閱項目" : "編輯訂閱項目";
            this.Size = new Size(400, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.BackColor = Color.White;
            this.Font = new Font("Microsoft JhengHei UI", 10F);

            int lx = 20, tx = 110, w = 240, y = 20, gap = 44;

            AddLabel("項目名稱", lx, y);
            txtName = AddTextBox(tx, y, w); y += gap;

            AddLabel("扣款金額", lx, y);
            txtCost = AddTextBox(tx, y, w); y += gap;

            AddLabel("扣款週期", lx, y);
            cmbPeriod = new ComboBox { Location = new Point(tx, y), Size = new Size(w, 28), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbPeriod.Items.AddRange(new object[] { "每月", "每季", "每年" });
            cmbPeriod.SelectedIndex = 0;
            this.Controls.Add(cmbPeriod); y += gap;

            var separator = new Label { Location = new Point(lx, y - 10), Size = new Size(340, 1), BackColor = Color.FromArgb(220, 220, 220) };
            this.Controls.Add(separator);

            AddLabel("開始訂閱日", lx, y);
            dtpStartDate = new DateTimePicker { Location = new Point(tx, y), Size = new Size(w, 28), Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            this.Controls.Add(dtpStartDate); y += gap;

            AddLabel("提前提醒(天)", lx, y);
            nudReminderDays = new NumericUpDown { Location = new Point(tx, y), Size = new Size(w, 28), Minimum = 0, Maximum = 30, Value = 3 };
            this.Controls.Add(nudReminderDays); y += gap;

            AddLabel("備註說明", lx, y);
            txtNotes = AddTextBox(tx, y, w); y += gap;

            var btnSave = new Button { Text = "儲存送出", Location = new Point(tx, y), Size = new Size(110, 34), BackColor = Color.FromArgb(24, 95, 165), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            var btnCancel = new Button { Text = "取消", Location = new Point(tx + 124, y), Size = new Size(80, 34), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel, Cursor = Cursors.Hand };

            this.Controls.AddRange(new Control[] { btnSave, btnCancel });
            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }

        private void LoadExisting()
        {
            var dt = DatabaseHelper.ExecuteQuery("SELECT * FROM Subscriptions WHERE SubID = @id", new[] { new SqlParameter("@id", _subId) });
            if (dt.Rows.Count > 0)
            {
                var row = dt.Rows[0];
                txtName.Text = row["Name"].ToString();
                txtCost.Text = row["Cost"].ToString();
                cmbPeriod.Text = row["Period"].ToString();
                dtpStartDate.Value = Convert.ToDateTime(row["StartDate"]);
                nudReminderDays.Value = Convert.ToInt32(row["ReminderDays"]);
                txtNotes.Text = row["Notes"].ToString();
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // 1. 基礎防呆：檢查名稱與金額
            if (string.IsNullOrWhiteSpace(txtName.Text) || !decimal.TryParse(txtCost.Text, out decimal cost) || cost <= 0)
            {
                MessageBox.Show("請確認名稱與有效的金額（金額必須大於 0）。", "輸入錯誤", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2. 關鍵防呆：新增訂閱日期不能在未來
            // 使用 dtpStartDate.Value.Date 與 DateTime.Today 比較
            if (dtpStartDate.Value.Date > DateTime.Today)
            {
                MessageBox.Show($"日期設定錯誤：\n開始訂閱日「{dtpStartDate.Value:yyyy/MM/dd}」不能超過今天 ({DateTime.Today:yyyy/MM/dd})。\n\n系統無法預測未來的扣款日期，請修正後再試。",
                                "日期超出範圍", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; // 直接擋下，不執行後續 SQL
            }

            // 3. 執行儲存
            string sql = _subId < 0 ?
                "INSERT INTO Subscriptions (UserID, Name, Cost, Period, StartDate, ReminderDays, Notes) VALUES (@uid, @n, @c, @p, @sd, @rd, @no)" :
                "UPDATE Subscriptions SET Name=@n, Cost=@c, Period=@p, StartDate=@sd, ReminderDays=@rd, Notes=@no WHERE SubID=@id AND UserID=@uid";

            int affected = _subId < 0
                ? DatabaseHelper.ExecuteInsertGetId(sql, GetParams(cost))
                : DatabaseHelper.ExecuteNonQuery(sql, GetParams(cost));

            if (affected > 0)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private SqlParameter[] GetParams(decimal cost)
        {
            return new[] {
                new SqlParameter("@uid", Session.CurrentUser.UserID),
                new SqlParameter("@n", txtName.Text.Trim()),
                new SqlParameter("@c", cost),
                new SqlParameter("@p", cmbPeriod.Text),
                new SqlParameter("@sd", dtpStartDate.Value.Date),
                new SqlParameter("@rd", (int)nudReminderDays.Value),
                new SqlParameter("@no", txtNotes.Text.Trim()),
                new SqlParameter("@id", _subId)
            };
        }

        private void AddLabel(string text, int x, int y) { this.Controls.Add(new Label { Text = text, Location = new Point(x, y + 5), AutoSize = true, ForeColor = Color.FromArgb(80, 80, 80) }); }
        private TextBox AddTextBox(int x, int y, int width) { var txt = new TextBox { Location = new Point(x, y), Size = new Size(width, 28) }; this.Controls.Add(txt); return txt; }
    }
}