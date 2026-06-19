using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using SubscriptionManager.Data;
using SubscriptionManager.Helpers;

namespace SubscriptionManager.Forms
{
    /// <summary>
    /// 頁面 1-B：註冊新帳號（從登入頁 [立即註冊] 彈出）
    /// </summary>
    public class RegisterForm : Form
    {
        private TextBox txtUsername;
        private TextBox txtPassword;
        private TextBox txtConfirm;
        private Button  btnSave;
        private Button  btnCancel;

        public RegisterForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text            = "註冊新帳號";
            this.Size            = new Size(360, 280);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.Font            = new Font("Microsoft JhengHei UI", 10F);
            this.BackColor       = Color.White;

            int labelX = 20, inputX = 105, y = 30, gap = 48;

            AddRow("帳號",    labelX, inputX, y,        false, out txtUsername);
            AddRow("密碼",    labelX, inputX, y + gap,  true,  out txtPassword);
            AddRow("確認密碼", labelX, inputX, y + gap*2,true,  out txtConfirm);

            btnSave = new Button
            {
                Text      = "建立帳號",
                Location  = new Point(inputX, y + gap * 3),
                Size      = new Size(100, 34),
                BackColor = Color.FromArgb(24, 95, 165),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += btnSave_Click;

            btnCancel = new Button
            {
                Text      = "取消",
                Location  = new Point(inputX + 115, y + gap * 3),
                Size      = new Size(80, 34),
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            btnCancel.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] { btnSave, btnCancel });
        }

        // ── 輔助：快速建立 Label + TextBox 一列 ─────────────────────────────
        private void AddRow(string labelText, int lx, int tx, int y,
                            bool isPassword, out TextBox textBox)
        {
            var lbl = new Label
            {
                Text     = labelText,
                Location = new Point(lx, y + 4),
                AutoSize = true
            };
            var txt = new TextBox
            {
                Location     = new Point(tx, y),
                Size         = new Size(210, 26),
                PasswordChar = isPassword ? '*' : '\0'
            };
            this.Controls.AddRange(new Control[] { lbl, txt });
            textBox = txt;
        }

        // ── 註冊邏輯 ──────────────────────────────────────────────────────────
        private void btnSave_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;
            string confirm  = txtConfirm.Text;

            // ── 輸入驗證 ──────────────────────────────────────────────────
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("帳號與密碼不可空白。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (password != confirm)
            {
                MessageBox.Show("兩次密碼輸入不一致。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (password.Length < 6)
            {
                MessageBox.Show("密碼長度至少 6 個字元。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ── 檢查帳號是否已存在 ────────────────────────────────────────
            var check = DatabaseHelper.ExecuteQuery(
                "SELECT COUNT(*) AS cnt FROM Users WHERE Username = @u",
                new[] { new SqlParameter("@u", username) });

            if (Convert.ToInt32(check.Rows[0]["cnt"]) > 0)
            {
                MessageBox.Show("此帳號已被使用，請換一個。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ── 寫入資料庫 ────────────────────────────────────────────────
            int newId = DatabaseHelper.ExecuteInsertGetId(
                "INSERT INTO Users (Username, Password) VALUES (@u, @p)",
                new[]
                {
                    new SqlParameter("@u", username),
                    new SqlParameter("@p", PasswordHelper.HashPassword(password))
                });

            if (newId > 0)
            {
                MessageBox.Show("帳號建立成功！請回到登入頁面登入。", "成功",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
        }
    }
}
