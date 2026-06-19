using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using SubscriptionManager.Data;
using SubscriptionManager.Helpers;
using SubscriptionManager.Models;

namespace SubscriptionManager.Forms
{
    public class DashboardPage : UserControl
    {
        private Label lblTotalCostVal, lblTotalCountVal;
        private Chart chartPie;
        private FlowLayoutPanel pnlUrgentContainer;

        public DashboardPage() { InitializeComponent(); }

        private void InitializeComponent()
        {
            this.Size = new Size(800, 600);
            this.BackColor = Color.FromArgb(245, 245, 247);
            this.Padding = new Padding(16);

            // ── 數據卡片區 ──
            lblTotalCostVal = new Label
            {
                Font = new Font("Microsoft JhengHei UI", 18F, FontStyle.Bold),
                Location = new Point(20, 40),
                AutoSize = true,
                ForeColor = Color.FromArgb(30, 30, 30)
            };
            var p1 = CreateStatCard("本月支出", lblTotalCostVal, new Point(16, 16));

            lblTotalCountVal = new Label
            {
                Font = new Font("Microsoft JhengHei UI", 18F, FontStyle.Bold),
                Location = new Point(20, 40),
                AutoSize = true,
                ForeColor = Color.FromArgb(30, 30, 30)
            };
            var p2 = CreateStatCard("訂閱項目", lblTotalCountVal, new Point(232, 16));

            // ── 圓餅圖 ──
            chartPie = new Chart
            {
                Location = new Point(16, 124),
                Size = new Size(360, 280),
                BackColor = Color.White
            };
            chartPie.Titles.Add(new Title("支出比例分析")
            {
                Font = new Font("Microsoft JhengHei UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 80, 80)
            });
            chartPie.ChartAreas.Add(new ChartArea("main"));
            var series = new Series("支出")
            {
                ChartType = SeriesChartType.Pie,
                IsValueShownAsLabel = true,
                LabelFormat = "#PERCENT{P0}"
            };
            chartPie.Series.Add(series);

            // ── 急迫提醒標題 ──
            var lblUrgentTitle = new Label
            {
                Text = "繳費急迫提醒",
                Location = new Point(392, 124),
                Font = new Font("Microsoft JhengHei UI", 11F, FontStyle.Bold),
                AutoSize = true,
                ForeColor = Color.FromArgb(50, 50, 50)
            };
            var lblUrgentSub = new Label
            {
                Text = "未來 7 天",
                Location = new Point(500, 129),
                Font = new Font("Microsoft JhengHei UI", 9F),
                AutoSize = true,
                ForeColor = Color.FromArgb(140, 140, 140)
            };

            // ── 急迫提醒卡片容器 ──
            pnlUrgentContainer = new FlowLayoutPanel
            {
                Location = new Point(392, 152),
                Size = new Size(400, 380),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            this.Controls.AddRange(new Control[] { p1, p2, chartPie, lblUrgentTitle, lblUrgentSub, pnlUrgentContainer });
        }

        public void LoadData()
        {
            int userId = Session.CurrentUser.UserID;
            DateTime today = DateTime.Today;

            // 1. 讀取所有啟用中訂閱
            var dtSubs = DatabaseHelper.ExecuteQuery(
                "SELECT * FROM Subscriptions WHERE UserID=@uid AND IsActive=1",
                new[] { new SqlParameter("@uid", userId) });

            // 2. 自動結算本月已到期扣款 → 寫入 PaymentHistory
            foreach (DataRow row in dtSubs.Rows)
            {
                int subId = Convert.ToInt32(row["SubID"]);
                decimal cost = Convert.ToDecimal(row["Cost"]);
                var sub = new Subscription
                {
                    Period = row["Period"].ToString(),
                    StartDate = Convert.ToDateTime(row["StartDate"])
                };

                DateTime? dueThisMonth = sub.GetDueDateInMonth(today.Year, today.Month);
                if (dueThisMonth.HasValue && dueThisMonth.Value.Date <= today)
                {
                    var checkDt = DatabaseHelper.ExecuteQuery(
                        "SELECT COUNT(*) FROM PaymentHistory WHERE SubID=@sid AND YEAR(PaidDate)=@yr AND MONTH(PaidDate)=@mo",
                        new[] {
                            new SqlParameter("@sid", subId),
                            new SqlParameter("@yr",  dueThisMonth.Value.Year),
                            new SqlParameter("@mo",  dueThisMonth.Value.Month)
                        });
                    if (Convert.ToInt32(checkDt.Rows[0][0]) == 0)
                        DatabaseHelper.ExecuteNonQuery(
                            "INSERT INTO PaymentHistory(SubID,UserID,PaidDate,Amount,Status) VALUES(@sid,@uid,@pd,@amt,N'已繳')",
                            new[] {
                                new SqlParameter("@sid", subId),
                                new SqlParameter("@uid", userId),
                                new SqlParameter("@pd",  dueThisMonth.Value.Date),
                                new SqlParameter("@amt", cost)
                            });
                }
            }

            // 3. 圓餅圖：本月實際扣款統計
            chartPie.Series["支出"].Points.Clear();
            var dtPie = DatabaseHelper.ExecuteQuery(@"
                SELECT s.Name, SUM(h.Amount) AS TotalCost
                FROM PaymentHistory h
                JOIN Subscriptions s ON h.SubID = s.SubID
                WHERE h.UserID=@uid AND YEAR(h.PaidDate)=@yr AND MONTH(h.PaidDate)=@mo
                GROUP BY s.Name",
                new[] {
                    new SqlParameter("@uid", userId),
                    new SqlParameter("@yr",  today.Year),
                    new SqlParameter("@mo",  today.Month)
                });

            Color[] pieColors = {
                Color.FromArgb(24,  95, 165),
                Color.FromArgb(93, 202, 165),
                Color.FromArgb(250,199, 117),
                Color.FromArgb(212, 83, 126),
                Color.FromArgb(163, 45,  45)
            };
            decimal totalCost = 0;
            int ci = 0;
            foreach (DataRow row in dtPie.Rows)
            {
                decimal cost = Convert.ToDecimal(row["TotalCost"]);
                totalCost += cost;
                int idx = chartPie.Series["支出"].Points.AddXY(row["Name"].ToString(), (double)cost);
                chartPie.Series["支出"].Points[idx].Color = pieColors[ci % pieColors.Length];
                chartPie.Series["支出"].Points[idx].Label = $"{row["Name"]}\n#PERCENT{{P0}}";
                ci++;
            }
            lblTotalCostVal.Text = $"NT$ {totalCost:N0}";
            lblTotalCountVal.Text = $"{dtSubs.Rows.Count} 個";

            // 4. ── 急迫提醒卡片（未來 7 天）──
            //    核心修正：直接用 GetNextDueDate() 計算每筆的「下次扣款日」
            //    不再只看本月，確保跨月也能抓到
            pnlUrgentContainer.Controls.Clear();
            var urgentList = new List<(string Name, decimal Cost, DateTime NextDue, int DaysLeft)>();

            foreach (DataRow row in dtSubs.Rows)
            {
                var sub = new Subscription
                {
                    Name = row["Name"].ToString(),
                    Cost = Convert.ToDecimal(row["Cost"]),
                    Period = row["Period"].ToString(),
                    StartDate = Convert.ToDateTime(row["StartDate"]),
                    ReminderDays = Convert.ToInt32(row["ReminderDays"])
                };

                // GetNextDueDate 從今天往後找最近一次扣款日
                DateTime nextDue = sub.GetNextDueDate(today);
                int daysLeft = (nextDue.Date - today).Days;

                // 只顯示未來 0~7 天（含今天到期）
                if (daysLeft >= 0 && daysLeft <= 7)
                    urgentList.Add((sub.Name, sub.Cost, nextDue, daysLeft));
            }

            foreach (var item in urgentList.OrderBy(x => x.DaysLeft))
                pnlUrgentContainer.Controls.Add(CreateUrgentCard(item.Name, item.Cost, item.NextDue, item.DaysLeft));

            if (urgentList.Count == 0)
                pnlUrgentContainer.Controls.Add(new Label
                {
                    Text = "未來 7 天內沒有待扣款項目 ✓",
                    ForeColor = Color.FromArgb(100, 160, 80),
                    Font = new Font("Microsoft JhengHei UI", 9.5F),
                    AutoSize = true,
                    Margin = new Padding(8, 12, 0, 0)
                });
        }

        // ── 急迫提醒卡片（仿截圖風格）────────────────────────────────────────
        private Panel CreateUrgentCard(string name, decimal cost, DateTime date, int days)
        {
            // 顏色分級
            Color bgColor, textColor, badgeBg, badgeText, iconColor;
            string iconText;

            if (days <= 3)
            {
                bgColor = Color.FromArgb(255, 242, 242);
                textColor = Color.FromArgb(160, 40, 40);
                badgeBg = Color.FromArgb(248, 187, 187);
                badgeText = Color.FromArgb(120, 20, 20);
                iconColor = Color.FromArgb(200, 60, 60);
                iconText = "!";
            }
            else if (days <= 7)
            {
                bgColor = Color.FromArgb(255, 250, 225);
                textColor = Color.FromArgb(140, 100, 20);
                badgeBg = Color.FromArgb(250, 215, 130);
                badgeText = Color.FromArgb(110, 75, 10);
                iconColor = Color.FromArgb(200, 150, 30);
                iconText = "○";
            }
            else
            {
                bgColor = Color.FromArgb(240, 250, 235);
                textColor = Color.FromArgb(60, 120, 40);
                badgeBg = Color.FromArgb(190, 230, 160);
                badgeText = Color.FromArgb(40, 100, 20);
                iconColor = Color.FromArgb(80, 160, 60);
                iconText = "✓";
            }

            // 外層卡片
            var card = new Panel
            {
                Size = new Size(390, 72),
                BackColor = bgColor,
                Margin = new Padding(0, 0, 0, 10),
                Cursor = Cursors.Default
            };

            // 圓角效果（Paint 事件）
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = RoundedRect(card.ClientRectangle, 10))
                using (var brush = new SolidBrush(bgColor))
                    e.Graphics.FillPath(brush, path);
            };

            // 左側圖示圓圈
            var lblIcon = new Label
            {
                Text = iconText,
                Location = new Point(14, 22),
                Size = new Size(28, 28),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = iconColor,
                Font = new Font("Arial", 11F, FontStyle.Bold)
            };

            // 名稱
            var lblName = new Label
            {
                Text = name,
                Location = new Point(50, 12),
                AutoSize = true,
                Font = new Font("Microsoft JhengHei UI", 10F, FontStyle.Bold),
                ForeColor = textColor
            };

            // 日期與金額
            var lblSub = new Label
            {
                Text = $"{date:MM/dd} 扣款 · NT$ {cost:N0}",
                Location = new Point(50, 38),
                AutoSize = true,
                Font = new Font("Microsoft JhengHei UI", 8.5F),
                ForeColor = Color.FromArgb(textColor.R + 30, textColor.G + 30, textColor.B + 30)
            };

            // 右側「剩 N 天」Badge
            var lblBadge = new Label
            {
                Text = $"剩 {days} 天",
                Location = new Point(300, 24),
                Size = new Size(72, 26),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft JhengHei UI", 9F, FontStyle.Bold),
                ForeColor = badgeText,
                BackColor = badgeBg
            };
            // Badge 圓角
            lblBadge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = RoundedRect(lblBadge.ClientRectangle, 12))
                using (var brush = new SolidBrush(badgeBg))
                {
                    e.Graphics.Clear(bgColor);      // 先清成卡片背景色
                    e.Graphics.FillPath(brush, path);
                }
                TextRenderer.DrawText(e.Graphics, lblBadge.Text,
                    lblBadge.Font, lblBadge.ClientRectangle, badgeText,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            card.Controls.AddRange(new Control[] { lblIcon, lblName, lblSub, lblBadge });
            return card;
        }

        // ── 圓角 Path 輔助 ────────────────────────────────────────────────────
        private System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int d = radius * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ── 統計卡片 ──────────────────────────────────────────────────────────
        private Panel CreateStatCard(string title, Label valueLabel, Point location)
        {
            var p = new Panel { BackColor = Color.White, Location = location, Size = new Size(200, 90) };
            p.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(220, 220, 220), 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };
            p.Controls.Add(new Label
            {
                Text = title,
                Location = new Point(14, 12),
                AutoSize = true,
                ForeColor = Color.FromArgb(120, 120, 120),
                Font = new Font("Microsoft JhengHei UI", 8.5F)
            });
            valueLabel.Location = new Point(12, 36);
            p.Controls.Add(valueLabel);
            return p;
        }
    }
}