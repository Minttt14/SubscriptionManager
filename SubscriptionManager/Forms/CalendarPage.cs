using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SubscriptionManager.Data;
using SubscriptionManager.Helpers;
using SubscriptionManager.Models;

namespace SubscriptionManager.Forms
{
    /// <summary>
    /// 頁面：月曆提醒與歷史紀錄
    /// - 左：自繪月曆（提醒日藍底、扣款日紅底）
    /// - 右：當日明細卡片（含滾軸，避免內容被截斷）
    /// - 下：歷史扣款 DataGridView
    /// </summary>
    public class CalendarPage : UserControl
    {
        // ── 月曆 ──────────────────────────────────────────────────────────────
        private Panel    pnlCalendar;
        private Button   btnPrevMonth, btnNextMonth;
        private Label    lblMonthYear;
        private DateTime _displayMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        private HashSet<int>       _dueDays      = new HashSet<int>();
        private HashSet<int>       _reminderDays = new HashSet<int>();
        private List<Subscription> _activeSubs   = new List<Subscription>();

        // ── 右側明細（有滾軸的 Panel）─────────────────────────────────────────
        private Panel           pnlDetailOuter;   // 固定大小的容器（帶邊框）
        private Panel           pnlDetailInner;   // 內部可捲動內容
        private Label           lblDetailDate;
        private Label           lblDetailWeek;
        private FlowLayoutPanel pnlRemindItems;
        private FlowLayoutPanel pnlDueItems;
        private Label           lblRemindSection;
        private Label           lblDueSection;

        // ── 歷史紀錄 ──────────────────────────────────────────────────────────
        private DataGridView dgvHistory;
        private Label        lblHistoryTitle;

        public CalendarPage() { InitializeComponent(); }

        private void InitializeComponent()
        {
            this.Size       = new Size(860, 640);
            this.BackColor  = Color.FromArgb(245, 245, 247);
            this.AutoScroll = true;

            // ── 月份導覽 ────────────────────────────────────────────────
            btnPrevMonth = MakeNavBtn("‹", new Point(16, 16));
            btnPrevMonth.Click += (s, e) => { _displayMonth = _displayMonth.AddMonths(-1); LoadData(); };

            lblMonthYear = new Label
            {
                Location  = new Point(60, 18),
                Size      = new Size(180, 28),
                TextAlign = ContentAlignment.MiddleCenter,
                Font      = new Font("Microsoft JhengHei UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40)
            };

            btnNextMonth = MakeNavBtn("›", new Point(248, 16));
            btnNextMonth.Click += (s, e) => { _displayMonth = _displayMonth.AddMonths(1); LoadData(); };

            // ── 月曆 ────────────────────────────────────────────────────
            pnlCalendar = new Panel
            {
                Location    = new Point(16, 58),
                Size        = new Size(300, 256),
                BackColor   = Color.White,
                BorderStyle = BorderStyle.None
            };
            pnlCalendar.Paint      += PnlCalendar_Paint;
            pnlCalendar.MouseClick += PnlCalendar_MouseClick;

            // 圖例
            var (box1, lbl1) = MakeLegend("提醒日", Color.FromArgb(210, 225, 248), new Point(16, 322));
            var (box2, lbl2) = MakeLegend("扣款日", Color.FromArgb(250, 210, 210), new Point(88, 322));
            var (box3, lbl3) = MakeLegendBorder("今天", new Point(160, 322));

            // ── 右側明細外框（固定大小，帶滾軸）───────────────────────
            pnlDetailOuter = new Panel
            {
                Location    = new Point(330, 16),
                Size        = new Size(500, 300),
                BackColor   = Color.White,
                BorderStyle = BorderStyle.None,
                AutoScroll  = false   // 外層不捲，由內層 Panel 控制
            };
            pnlDetailOuter.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(228, 228, 228), 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlDetailOuter.Width - 1, pnlDetailOuter.Height - 1);
            };

            // 內層可捲動 Panel
            pnlDetailInner = new Panel
            {
                Location   = new Point(0, 0),
                Width      = pnlDetailOuter.Width - SystemInformation.VerticalScrollBarWidth - 2,
                AutoSize   = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor  = Color.White
            };

            // 捲軸（垂直，貼在右側）
            var scrollBar = new VScrollBar
            {
                Dock    = DockStyle.Right,
                Visible = false
            };
            scrollBar.Scroll += (s, e) =>
            {
                pnlDetailInner.Top = -scrollBar.Value;
            };

            // 當內層高度超過外層時顯示捲軸
            pnlDetailInner.Resize += (s, e) =>
            {
                bool needScroll = pnlDetailInner.Height > pnlDetailOuter.Height;
                scrollBar.Visible = needScroll;
                if (needScroll)
                {
                    scrollBar.Maximum  = pnlDetailInner.Height - pnlDetailOuter.Height + 20;
                    scrollBar.LargeChange = pnlDetailOuter.Height / 2;
                    pnlDetailInner.Width = pnlDetailOuter.Width - SystemInformation.VerticalScrollBarWidth - 2;
                }
                else
                {
                    pnlDetailInner.Width = pnlDetailOuter.Width - 2;
                    pnlDetailInner.Top   = 0;
                    scrollBar.Value      = 0;
                }
            };

            // ── 滑鼠滾輪也可捲動 ──────────────────────────────────────────
            pnlDetailOuter.MouseWheel += (s, e) =>
            {
                if (!scrollBar.Visible) return;

                // 計算新的捲軸位置 (e.Delta 是滾輪移動量)
                int step = 40; // 設定每次滾動的步長
                int newVal = scrollBar.Value - (e.Delta > 0 ? step : -step);

                // 確保數值在有效範圍內
                newVal = Math.Max(0, Math.Min(scrollBar.Maximum, newVal));

                scrollBar.Value = newVal;
                pnlDetailInner.Top = -newVal;
            };

            // ── 明細內容控制項（放在 pnlDetailInner 裡）──────────────
            var lblIcon = new Label
            {
                Text     = "📅",
                Location = new Point(14, 14),
                AutoSize = true,
                Font     = new Font("Segoe UI Emoji", 12F)
            };
            lblDetailDate = new Label
            {
                Text      = "點擊日期查看明細",
                Location  = new Point(46, 16),
                AutoSize  = true,
                Font      = new Font("Microsoft JhengHei UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            lblDetailWeek = new Label
            {
                Text      = "",
                Location  = new Point(16, 48),
                AutoSize  = true,
                Font      = new Font("Microsoft JhengHei UI", 9F),
                ForeColor = Color.FromArgb(140, 140, 140)
            };
            var sepLine = new Label
            {
                Location  = new Point(16, 72),
                Size      = new Size(460, 1),
                BackColor = Color.FromArgb(235, 235, 235)
            };

            lblRemindSection = new Label
            {
                Text      = "提醒項目",
                Location  = new Point(16, 82),
                AutoSize  = true,
                Font      = new Font("Microsoft JhengHei UI", 9F),
                ForeColor = Color.FromArgb(140, 140, 140)
            };
            pnlRemindItems = new FlowLayoutPanel
            {
                Location      = new Point(16, 104),
                Width         = 462,
                AutoSize      = true,
                AutoSizeMode  = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                BackColor     = Color.Transparent
            };

            lblDueSection = new Label
            {
                Text      = "扣款項目",
                Location  = new Point(16, 200),   // 動態調整
                AutoSize  = true,
                Font      = new Font("Microsoft JhengHei UI", 9F),
                ForeColor = Color.FromArgb(140, 140, 140)
            };
            pnlDueItems = new FlowLayoutPanel
            {
                Location      = new Point(16, 222),  // 動態調整
                Width         = 462,
                AutoSize      = true,
                AutoSizeMode  = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                BackColor     = Color.Transparent
            };

            pnlDetailInner.Controls.AddRange(new Control[]
            {
                lblIcon, lblDetailDate, lblDetailWeek, sepLine,
                lblRemindSection, pnlRemindItems,
                lblDueSection, pnlDueItems
            });
            pnlDetailOuter.Controls.AddRange(new Control[] { pnlDetailInner, scrollBar });

            // ── 歷史紀錄 ────────────────────────────────────────────────
            lblHistoryTitle = new Label
            {
                Text      = "歷史扣款紀錄",
                Location  = new Point(16, 348),
                AutoSize  = true,
                Font      = new Font("Microsoft JhengHei UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50)
            };

            dgvHistory = new DataGridView
            {
                Location              = new Point(16, 376),
                Size                  = new Size(820, 290),
                Anchor                = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackgroundColor       = Color.White,
                BorderStyle           = BorderStyle.FixedSingle,
                RowHeadersVisible     = false,
                AllowUserToAddRows    = false,
                ReadOnly              = true,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                Font                  = new Font("Microsoft JhengHei UI", 9.5F),
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor             = Color.FromArgb(230, 230, 230),
                RowTemplate           = { Height = 34 }
            };
            dgvHistory.ColumnHeadersDefaultCellStyle.Font      = new Font("Microsoft JhengHei UI", 9.5F, FontStyle.Bold);
            dgvHistory.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 245);
            dgvHistory.EnableHeadersVisualStyles  = false;
            dgvHistory.DataBindingComplete += (s, e) => dgvHistory.ClearSelection();
            dgvHistory.CellFormatting      += DgvHistory_CellFormatting;

            this.Controls.AddRange(new Control[]
            {
                btnPrevMonth, lblMonthYear, btnNextMonth,
                pnlCalendar, box1, lbl1, box2, lbl2, box3, lbl3,
                pnlDetailOuter,
                lblHistoryTitle, dgvHistory
            });

            ShowDayDetail(DateTime.Today);
        }

        // ── 公開：載入 ────────────────────────────────────────────────────────
        public void LoadData()
        {
            lblMonthYear.Text = _displayMonth.ToString("yyyy 年 M 月");
            int userId = Session.CurrentUser.UserID;
            DateTime today = DateTime.Today;

            var dt = DatabaseHelper.ExecuteQuery(
                "SELECT * FROM Subscriptions WHERE UserID=@uid AND IsActive=1",
                new[] { new SqlParameter("@uid", userId) });

            _activeSubs.Clear(); _dueDays.Clear(); _reminderDays.Clear();

            foreach (DataRow row in dt.Rows)
            {
                int subId = Convert.ToInt32(row["SubID"]);
                decimal cost = Convert.ToDecimal(row["Cost"]);
                var sub = new Subscription
                {
                    Name         = row["Name"].ToString(),
                    Cost         = cost,
                    Period       = row["Period"].ToString(),
                    StartDate    = Convert.ToDateTime(row["StartDate"]),
                    ReminderDays = Convert.ToInt32(row["ReminderDays"])
                };
                _activeSubs.Add(sub);

                // 自動補歷史紀錄
                for (DateTime d = new DateTime(sub.StartDate.Year, sub.StartDate.Month, 1);
                     d <= new DateTime(today.Year, today.Month, 1); d = d.AddMonths(1))
                {
                    DateTime? due = sub.GetDueDateInMonth(d.Year, d.Month);
                    if (!due.HasValue || due.Value.Date > today) continue;
                    var ck = DatabaseHelper.ExecuteQuery(
                        "SELECT COUNT(*) FROM PaymentHistory WHERE SubID=@sid AND YEAR(PaidDate)=@yr AND MONTH(PaidDate)=@mo",
                        new[] { new SqlParameter("@sid", subId), new SqlParameter("@yr", due.Value.Year), new SqlParameter("@mo", due.Value.Month) });
                    if (Convert.ToInt32(ck.Rows[0][0]) == 0)
                        DatabaseHelper.ExecuteNonQuery(
                            "INSERT INTO PaymentHistory(SubID,UserID,PaidDate,Amount,Status) VALUES(@sid,@uid,@pd,@amt,N'已繳')",
                            new[] { new SqlParameter("@sid", subId), new SqlParameter("@uid", userId), new SqlParameter("@pd", due.Value.Date), new SqlParameter("@amt", cost) });
                }

                // 月曆標記
                var dueDisplay = sub.GetDueDateInMonth(_displayMonth.Year, _displayMonth.Month);
                if (dueDisplay.HasValue)
                {
                    _dueDays.Add(dueDisplay.Value.Day);
                    if (sub.ReminderDays > 0)
                    {
                        var remind = dueDisplay.Value.AddDays(-sub.ReminderDays);
                        if (remind.Year == _displayMonth.Year && remind.Month == _displayMonth.Month)
                            _reminderDays.Add(remind.Day);
                    }
                }
            }

            pnlCalendar.Invalidate();

            // 歷史紀錄
            dgvHistory.DataSource = DatabaseHelper.ExecuteQuery(@"
                SELECT h.PaidDate AS '扣款日期',
                       s.Name     AS '訂閱名稱',
                       s.Cost     AS '金額（NT$）',
                       CASE WHEN s.IsActive=1 THEN N'續訂中' ELSE N'已退訂' END AS '狀態'
                FROM PaymentHistory h
                JOIN Subscriptions s ON h.SubID=s.SubID
                WHERE h.UserID=@uid
                ORDER BY h.PaidDate DESC",
                new[] { new SqlParameter("@uid", userId) });
        }

        // ── 自繪月曆 ──────────────────────────────────────────────────────────
        private void PnlCalendar_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);

            int cellW   = pnlCalendar.Width / 7;
            int cellH   = 34;
            int headerH = 28;
            string[] wd = { "日", "一", "二", "三", "四", "五", "六" };

            using (var hf = new Font("Microsoft JhengHei UI", 9F))
            using (var hb = new SolidBrush(Color.FromArgb(160, 160, 160)))
                for (int i = 0; i < 7; i++)
                    g.DrawString(wd[i], hf, hb,
                        new RectangleF(i * cellW, 2, cellW, headerH),
                        new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

            int daysInMonth = DateTime.DaysInMonth(_displayMonth.Year, _displayMonth.Month);
            int startDow    = (int)new DateTime(_displayMonth.Year, _displayMonth.Month, 1).DayOfWeek;
            int todayDay    = (DateTime.Today.Year == _displayMonth.Year && DateTime.Today.Month == _displayMonth.Month)
                              ? DateTime.Today.Day : -1;

            for (int day = 1; day <= daysInMonth; day++)
            {
                int slot   = startDow + day - 1;
                int col    = slot % 7;
                int rowIdx = slot / 7;
                var rect   = new Rectangle(col * cellW + 2, headerH + rowIdx * cellH + 2, cellW - 4, cellH - 4);

                bool isDue    = _dueDays.Contains(day);
                bool isRemind = _reminderDays.Contains(day);
                bool isToday  = day == todayDay;

                if (isDue)
                    using (var path = RR(rect, 8))
                    using (var br = new SolidBrush(Color.FromArgb(252, 210, 210)))
                        g.FillPath(br, path);
                else if (isRemind)
                    using (var path = RR(rect, 8))
                    using (var br = new SolidBrush(Color.FromArgb(210, 225, 248)))
                        g.FillPath(br, path);

                if (isToday)
                    using (var path = RR(rect, 8))
                    using (var pen = new Pen(Color.FromArgb(24, 95, 165), 2))
                        g.DrawPath(pen, path);

                // 小圓點
                if (isDue || isRemind)
                {
                    var dot = new Rectangle(rect.X + rect.Width / 2 - 2, rect.Bottom - 6, 4, 4);
                    using (var br = new SolidBrush(isDue ? Color.FromArgb(180, 60, 60) : Color.FromArgb(24, 95, 165)))
                        g.FillEllipse(br, dot);
                }

                var numColor = isDue    ? Color.FromArgb(160, 40, 40)
                             : isRemind ? Color.FromArgb(24, 95, 165)
                             : isToday  ? Color.FromArgb(24, 95, 165)
                             : Color.FromArgb(60, 60, 60);
                var numFont  = (isDue || isRemind || isToday)
                    ? new Font("Microsoft JhengHei UI", 9.5F, FontStyle.Bold)
                    : new Font("Microsoft JhengHei UI", 9.5F);
                g.DrawString(day.ToString(), numFont, new SolidBrush(numColor),
                    new RectangleF(rect.X, rect.Y, rect.Width, rect.Height - 6),
                    new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }
        }

        // ── 點擊月曆 ──────────────────────────────────────────────────────────
        private void PnlCalendar_MouseClick(object sender, MouseEventArgs e)
        {
            int cellW   = pnlCalendar.Width / 7;
            int headerH = 28;
            int cellH   = 34;
            if (e.Y < headerH) return;

            int col     = e.X / cellW;
            int rowIdx  = (e.Y - headerH) / cellH;
            int startDow = (int)new DateTime(_displayMonth.Year, _displayMonth.Month, 1).DayOfWeek;
            int day      = rowIdx * 7 + col - startDow + 1;
            int maxDay   = DateTime.DaysInMonth(_displayMonth.Year, _displayMonth.Month);
            if (day < 1 || day > maxDay) return;

            ShowDayDetail(new DateTime(_displayMonth.Year, _displayMonth.Month, day));
        }

        // ── 右側明細（含滾軸）────────────────────────────────────────────────
        private void ShowDayDetail(DateTime date)
        {
            string[] wn = { "日", "一", "二", "三", "四", "五", "六" };
            lblDetailDate.Text = $"{date:MM/dd} 當日明細";
            lblDetailWeek.Text = $"{date:yyyy 年 M 月 d 日}（{wn[(int)date.DayOfWeek]}）";

            pnlRemindItems.Controls.Clear();
            pnlDueItems.Controls.Clear();

            bool hasRemind = false, hasDue = false;

            foreach (var sub in _activeSubs)
            {
                var due = sub.GetDueDateInMonth(date.Year, date.Month);
                if (!due.HasValue) continue;

                if (sub.ReminderDays > 0 && due.Value.AddDays(-sub.ReminderDays).Date == date.Date)
                {
                    pnlRemindItems.Controls.Add(MakeDetailCard(sub.Name,
                        $"NT$ {sub.Cost:N0} · {sub.ReminderDays} 天後 {due.Value:MM/dd} 實際扣款",
                        "🔔", Color.FromArgb(230, 240, 255)));
                    hasRemind = true;
                }
                if (due.Value.Date == date.Date)
                {
                    pnlDueItems.Controls.Add(MakeDetailCard(sub.Name,
                        $"NT$ {sub.Cost:N0}",
                        "💳", Color.FromArgb(240, 248, 255)));
                    hasDue = true;
                }
            }

            if (!hasRemind) pnlRemindItems.Controls.Add(MakeEmptyCard("本日無提醒項目"));
            if (!hasDue)    pnlDueItems.Controls.Add(MakeEmptyCard("本日無實際扣款項目"));

            // 動態更新 DueSection 位置
            pnlDetailInner.SuspendLayout();
            int remindBottom = pnlRemindItems.Bottom + 12;
            lblDueSection.Top  = remindBottom;
            pnlDueItems.Top    = remindBottom + 22;
            pnlDetailInner.ResumeLayout();
            pnlDetailInner.PerformLayout();

            // 重置捲動位置
            pnlDetailInner.Top = 0;
        }

        // ── 明細卡片 ──────────────────────────────────────────────────────────
        private Panel MakeDetailCard(string title, string sub, string icon, Color bg)
        {
            var card = new Panel { Size = new Size(462, 54), BackColor = bg, Margin = new Padding(0, 0, 0, 6) };
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = RR(card.ClientRectangle, 8))
                using (var br = new SolidBrush(bg))
                    e.Graphics.FillPath(br, path);
            };
            card.Controls.Add(new Label { Text = icon, Location = new Point(10, 14), AutoSize = true, Font = new Font("Segoe UI Emoji", 11F) });
            card.Controls.Add(new Label { Text = title, Location = new Point(38, 8),  AutoSize = true, Font = new Font("Microsoft JhengHei UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(40, 40, 40) });
            card.Controls.Add(new Label { Text = sub,   Location = new Point(38, 30), AutoSize = true, Font = new Font("Microsoft JhengHei UI", 8.5F), ForeColor = Color.FromArgb(110, 110, 110) });
            return card;
        }

        // ── 空狀態卡片 ────────────────────────────────────────────────────────
        private Panel MakeEmptyCard(string msg)
        {
            var card = new Panel { Size = new Size(462, 40), BackColor = Color.FromArgb(248, 248, 248), Margin = new Padding(0, 0, 0, 4) };
            card.Controls.Add(new Label { Text = $"ⓘ  {msg}", Location = new Point(12, 10), AutoSize = true, Font = new Font("Microsoft JhengHei UI", 9F), ForeColor = Color.FromArgb(160, 160, 160) });
            return card;
        }

        // ── 歷史紀錄著色 ──────────────────────────────────────────────────────
        private void DgvHistory_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || !dgvHistory.Columns.Contains("狀態")) return;
            if (dgvHistory.Rows[e.RowIndex].Cells["狀態"].Value?.ToString() == "已退訂")
            {
                dgvHistory.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248);
                dgvHistory.Rows[e.RowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(170, 170, 170);
            }
        }

        // ── 輔助 ──────────────────────────────────────────────────────────────
        private GraphicsPath RR(Rectangle b, int r)
        {
            var p = new GraphicsPath(); int d = r * 2;
            p.AddArc(b.X, b.Y, d, d, 180, 90);
            p.AddArc(b.Right - d, b.Y, d, d, 270, 90);
            p.AddArc(b.Right - d, b.Bottom - d, d, d, 0, 90);
            p.AddArc(b.X, b.Bottom - d, d, d, 90, 90);
            p.CloseFigure(); return p;
        }

        private Button MakeNavBtn(string text, Point loc)
        {
            var b = new Button { Text = text, Location = loc, Size = new Size(36, 36), FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 14F), ForeColor = Color.FromArgb(80, 80, 80), Cursor = Cursors.Hand, BackColor = Color.White };
            b.FlatAppearance.BorderColor = Color.FromArgb(220, 220, 220);
            b.FlatAppearance.BorderSize  = 1;
            return b;
        }

        private (Panel, Label) MakeLegend(string text, Color color, Point loc)
        {
            var box = new Panel { Location = loc, Size = new Size(16, 16), BackColor = color };
            box.Paint += (s, e) => { using (var pen = new Pen(Color.FromArgb(180, 180, 180))) e.Graphics.DrawRectangle(pen, 0, 0, 15, 15); };
            var lbl = new Label { Text = text, Location = new Point(loc.X + 20, loc.Y), AutoSize = true, Font = new Font("Microsoft JhengHei UI", 9F), ForeColor = Color.FromArgb(100, 100, 100) };
            return (box, lbl);
        }

        private (Panel, Label) MakeLegendBorder(string text, Point loc)
        {
            var box = new Panel { Location = loc, Size = new Size(16, 16), BackColor = Color.White };
            box.Paint += (s, e) => { using (var pen = new Pen(Color.FromArgb(24, 95, 165), 2)) e.Graphics.DrawRectangle(pen, 1, 1, 13, 13); };
            var lbl = new Label { Text = text, Location = new Point(loc.X + 20, loc.Y), AutoSize = true, Font = new Font("Microsoft JhengHei UI", 9F), ForeColor = Color.FromArgb(100, 100, 100) };
            return (box, lbl);
        }
    }
}
