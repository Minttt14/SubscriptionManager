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

        // ── 四個頁面 ──────────────────────────────────────────────────────────
        private DashboardPage    pageDashboard;
        private NotificationPage pageNotification;
        private SubscriptionPage pageSubscription;
        private CalendarPage     pageCalendar;

        // 目前選中的選單按鈕（用來高亮）
        private Button _activeMenuBtn;

        public MainForm()
        {
            InitializeComponent();
            ActivateMenu(_btnNotify);
            SwitchPage(pageNotification);
        }

        // 選單按鈕需要存 reference 才能高亮
        private Button _btnDash, _btnNotify, _btnSub, _btnCal;

        private void InitializeComponent()
        {
            this.Text          = "訂閱管理小幫手";
            this.Size          = new Size(1060, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize   = new Size(900, 600);
            this.Font          = new Font("Microsoft JhengHei UI", 10F);

            // ── 左側導覽列 ────────────────────────────────────────────────
            pnlSidebar = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = 210,
                BackColor = Color.FromArgb(30, 42, 56)
            };

            var lblLogo = new Label
            {
                Text      = "📋 訂閱小幫手",
                ForeColor = Color.White,
                Font      = new Font("Microsoft JhengHei UI", 13F, FontStyle.Bold),
                Location  = new Point(16, 22),
                AutoSize  = true
            };
            lblUser = new Label
            {
                Text      = $"👤 {Session.CurrentUser?.Username}",
                ForeColor = Color.FromArgb(160, 180, 200),
                Location  = new Point(18, 56),
                AutoSize  = true,
                Font      = new Font("Microsoft JhengHei UI", 9.5F)
            };

            // 分隔線
            var sep = new Label
            {
                Location  = new Point(16, 88),
                Size      = new Size(178, 1),
                BackColor = Color.FromArgb(55, 70, 88)
            };

            // 選單按鈕
            _btnNotify  = CreateMenuBtn("🔔  即將扣款通知", 105);
            _btnSub     = CreateMenuBtn("📝  訂閱管理", 155);
            _btnCal     = CreateMenuBtn("📅  月曆與紀錄", 205);
            _btnDash    = CreateMenuBtn("📊  財務分析", 255);

            _btnNotify.Click  += (s, e) => { ActivateMenu(_btnNotify); SwitchPage(pageNotification); };
            _btnSub.Click     += (s, e) => { ActivateMenu(_btnSub); SwitchPage(pageSubscription); };
            _btnCal.Click     += (s, e) => { ActivateMenu(_btnCal); SwitchPage(pageCalendar); };
            _btnDash.Click    += (s, e) => { ActivateMenu(_btnDash); SwitchPage(pageDashboard); };

            // 登出按鈕
            var btnLogout = new Button
            {
                Text      = "🔓  登出",
                Dock      = DockStyle.Bottom,
                Height    = 52,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(160, 50, 40),
                Cursor    = Cursors.Hand,
                Font      = new Font("Microsoft JhengHei UI", 10F)
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Click += BtnLogout_Click;

            pnlSidebar.Controls.AddRange(new Control[]
            {
                lblLogo, lblUser, sep,
                _btnNotify, _btnSub,  _btnCal, _btnDash,
                btnLogout
            });

            // ── 右側主畫面 ────────────────────────────────────────────────
            pnlContent = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 245, 247)
            };

            pageDashboard    = new DashboardPage    { Dock = DockStyle.Fill };
            pageNotification = new NotificationPage { Dock = DockStyle.Fill };
            pageSubscription = new SubscriptionPage { Dock = DockStyle.Fill };
            pageCalendar     = new CalendarPage     { Dock = DockStyle.Fill };

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlSidebar);

            this.FormClosed += (s, e) =>
            {
                if (Session.IsLoggedIn) Application.Exit();
            };
        }

        // ── 建立選單按鈕 ──────────────────────────────────────────────────────
        private Button CreateMenuBtn(string text, int y)
        {
            var btn = new Button
            {
                Text      = text,
                Location  = new Point(0, y),
                Size      = new Size(210, 48),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(190, 210, 230),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(22, 0, 0, 0),
                Cursor    = Cursors.Hand,
                Font      = new Font("Microsoft JhengHei UI", 10.5F)
            };
            btn.FlatAppearance.BorderSize  = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 60, 78);
            return btn;
        }

        // ── 高亮選中的選單按鈕 ────────────────────────────────────────────────
        private void ActivateMenu(Button btn)
        {
            // 取消上一個高亮
            if (_activeMenuBtn != null)
            {
                _activeMenuBtn.BackColor = Color.Transparent;
                _activeMenuBtn.ForeColor = Color.FromArgb(190, 210, 230);
                _activeMenuBtn.Font      = new Font("Microsoft JhengHei UI", 10.5F);
            }
            // 設定新高亮
            btn.BackColor = Color.FromArgb(24, 95, 165);
            btn.ForeColor = Color.White;
            btn.Font      = new Font("Microsoft JhengHei UI", 10.5F, FontStyle.Bold);
            _activeMenuBtn = btn;
        }

        // ── 切換頁面 ──────────────────────────────────────────────────────────
        private void SwitchPage(UserControl page)
        {
            pnlContent.Controls.Clear();
            pnlContent.Controls.Add(page);

            if (page is DashboardPage    d) d.LoadData();
            if (page is NotificationPage n) n.LoadData();
            if (page is SubscriptionPage s) s.LoadData();
            if (page is CalendarPage     c) c.LoadData();
        }

        // ── 登出 ──────────────────────────────────────────────────────────────
        private void BtnLogout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("確定要登出嗎？", "登出",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Session.Logout();
                new LoginForm().Show();
                this.Hide();
            }
        }
    }
}
