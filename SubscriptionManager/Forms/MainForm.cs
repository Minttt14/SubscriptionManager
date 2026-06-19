using System;
using System.Drawing;
using System.Windows.Forms;
using SubscriptionManager.Helpers;

namespace SubscriptionManager.Forms
{
    public class MainForm : Form
    {
        private Panel pnlSidebar;
        private Panel pnlContent;
        private Label lblUser;

        private DashboardPage pageDashboard;
        private SubscriptionPage pageSubscription;
        private CalendarPage pageCalendar;

        public MainForm()
        {
            InitializeComponent();
            SwitchPage(pageDashboard);
        }

        private void InitializeComponent()
        {
            this.Text = "訂閱管理小幫手";
            this.Size = new Size(1050, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Microsoft JhengHei UI", 10F);

            // ── 左側導覽列 ─────────────────────────────
            pnlSidebar = new Panel { Dock = DockStyle.Left, Width = 200, BackColor = Color.FromArgb(44, 62, 80) };

            var lblLogo = new Label { Text = "📋 訂閱小幫手", ForeColor = Color.White, Font = new Font("Microsoft JhengHei UI", 14F, FontStyle.Bold), Location = new Point(20, 20), AutoSize = true };
            lblUser = new Label { Text = $"👤 {Session.CurrentUser?.Username}", ForeColor = Color.LightGray, Location = new Point(20, 55), AutoSize = true };

            var btnDash = CreateMenuBtn("📊 財務分析", 100);
            btnDash.Click += (s, e) => SwitchPage(pageDashboard);

            var btnSub = CreateMenuBtn("📝 訂閱管理", 150);
            btnSub.Click += (s, e) => SwitchPage(pageSubscription);

            var btnCal = CreateMenuBtn("📅 月曆與紀錄", 200);
            btnCal.Click += (s, e) => SwitchPage(pageCalendar);

            var btnLogout = new Button { Text = "🔓 登出", Dock = DockStyle.Bottom, Height = 50, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.FromArgb(192, 57, 43), Cursor = Cursors.Hand };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Click += BtnLogout_Click;

            pnlSidebar.Controls.AddRange(new Control[] { lblLogo, lblUser, btnDash, btnSub, btnCal, btnLogout });

            // ── 右側主畫面 ─────────────────────────────
            pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(245, 245, 247) };

            pageDashboard = new DashboardPage { Dock = DockStyle.Fill };
            pageSubscription = new SubscriptionPage { Dock = DockStyle.Fill };
            pageCalendar = new CalendarPage { Dock = DockStyle.Fill };

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlSidebar);

            // 確保按 X 徹底關閉程式
            this.FormClosed += (s, e) => {
                if (Session.IsLoggedIn) Application.Exit();
            };
        }

        private Button CreateMenuBtn(string text, int y)
        {
            var btn = new Button { Text = text, Location = new Point(0, y), Size = new Size(200, 50), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(20, 0, 0, 0), Cursor = Cursors.Hand, Font = new Font("Microsoft JhengHei UI", 11F) };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void SwitchPage(UserControl page)
        {
            pnlContent.Controls.Clear();
            pnlContent.Controls.Add(page);
            if (page is DashboardPage d) d.LoadData();
            if (page is SubscriptionPage s) s.LoadData();
            if (page is CalendarPage c) c.LoadData();
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("確定要登出嗎？", "登出", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Session.Logout();
                new LoginForm().Show();
                this.Hide();
            }
        }
    }
}