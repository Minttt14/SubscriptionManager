using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using SubscriptionManager.Data;
using SubscriptionManager.Helpers;
using SubscriptionManager.Models;

namespace SubscriptionManager.Forms
{
    /// <summary>
    /// 頁面 1：登入與註冊
    /// </summary>
    public class LoginForm : Form
    {
        // ── 控制項宣告（對應設計稿命名）────────────────────────────────────
        private Label      lblTitle;
        private Label      lblUsername;
        private Label      lblPassword;
        private TextBox    txtUsername;
        private TextBox    txtPassword;
        private Button     btnLogin;
        private Button     btnCancel;
        private LinkLabel  lnkRegister;
        private Panel      pnlCard;

        public LoginForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Form 本體
            this.Text            = "系統登入 — 訂閱管理小幫手";
            this.Size            = new Size(420, 340);
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.BackColor       = Color.FromArgb(245, 245, 247);
            this.Font            = new Font("Microsoft JhengHei UI", 10F);

            // 白色卡片底板
            pnlCard = new Panel
            {
                Size      = new Size(340, 250),
                Location  = new Point(32, 26),
                BackColor = Color.White,
                Padding   = new Padding(20)
            };
            pnlCard.Paint += (s, e) =>
            {
                // 畫細邊框（模擬 0.5px border）
                using (var pen = new Pen(Color.FromArgb(220, 220, 220), 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
            };

            // 標題
            lblTitle = new Label
            {
                Text      = "帳號登入",
                Font      = new Font("Microsoft JhengHei UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                Location  = new Point(20, 16),
                AutoSize  = true
            };

            // 帳號 Label + TextBox
            lblUsername = new Label { Text = "帳號", Location = new Point(20, 70), AutoSize = true };
            txtUsername = new TextBox
            {
                Name     = "txtUsername",
                Location = new Point(80, 66),
                Size     = new Size(220, 26),
                Font     = new Font("Microsoft JhengHei UI", 10F)
            };

            // 密碼 Label + TextBox
            lblPassword = new Label { Text = "密碼", Location = new Point(20, 112), AutoSize = true };
            txtPassword = new TextBox
            {
                Name         = "txtPassword",
                Location     = new Point(80, 108),
                Size         = new Size(220, 26),
                PasswordChar = '*',               // ← 星號隱藏字元
                Font         = new Font("Microsoft JhengHei UI", 10F)
            };
            // 按 Enter 直接觸發登入
            txtPassword.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) btnLogin_Click(s, e);
            };

            // 登入按鈕
            btnLogin = new Button
            {
                Name      = "btnLogin",
                Text      = "登入",
                Location  = new Point(80, 155),
                Size      = new Size(100, 34),
                BackColor = Color.FromArgb(24, 95, 165),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Microsoft JhengHei UI", 10F, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += btnLogin_Click;

            // 取消按鈕
            btnCancel = new Button
            {
                Name      = "btnCancel",
                Text      = "取消",
                Location  = new Point(195, 155),
                Size      = new Size(100, 34),
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            btnCancel.Click += (s, e) => Application.Exit();

            // 立即註冊連結
            lnkRegister = new LinkLabel
            {
                Name      = "lnkRegister",
                Text      = "沒有帳號？立即註冊",
                Location  = new Point(122, 202),
                AutoSize  = true,
                LinkColor = Color.FromArgb(24, 95, 165)
            };
            lnkRegister.LinkClicked += lnkRegister_LinkClicked;

            // 加入控制項
            pnlCard.Controls.AddRange(new Control[]
            {
                lblTitle, lblUsername, txtUsername,
                lblPassword, txtPassword,
                btnLogin, btnCancel, lnkRegister
            });
            this.Controls.Add(pnlCard);

            // 確保使用者點擊右上角 X 關閉登入畫面時，徹底結束程式
            this.FormClosed += (s, e) => Application.Exit();
        }

        // ── 登入邏輯 ──────────────────────────────────────────────────────────
        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("請輸入帳號與密碼。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string hashedPwd = PasswordHelper.HashPassword(password);

            var dt = DatabaseHelper.ExecuteQuery(
                "SELECT UserID, Username FROM Users WHERE Username = @u AND Password = @p",
                new[]
                {
                    new SqlParameter("@u", username),
                    new SqlParameter("@p", hashedPwd)
                });

            if (dt.Rows.Count > 0)
            {
                // 登入成功：寫入 Session，跳轉主畫面
                Session.CurrentUser = new User
                {
                    UserID   = (int)dt.Rows[0]["UserID"],
                    Username = dt.Rows[0]["Username"].ToString()
                };

                var dashboard = new MainForm();
                dashboard.Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show("帳號或密碼錯誤，請重新輸入。", "登入失敗",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Clear();
                txtPassword.Focus();
            }
        }

        // ── 開啟註冊視窗 ──────────────────────────────────────────────────────
        private void lnkRegister_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var registerForm = new RegisterForm();
            registerForm.ShowDialog(this);   // Modal 方式開啟
        }
    }
}
