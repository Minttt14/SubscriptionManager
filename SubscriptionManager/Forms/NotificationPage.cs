using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using SubscriptionManager.Data;
using SubscriptionManager.Helpers;
using SubscriptionManager.Models;

namespace SubscriptionManager.Forms
{
    public class NotificationPage : UserControl
    {
        private Label lblTitle;
        private Label lblSubtitle;
        private FlowLayoutPanel pnlCards;
        private Panel pnlEmpty;

        public NotificationPage() { InitializeComponent(); }

        private void InitializeComponent()
        {
            this.Size = new Size(800, 600);
            this.BackColor = Color.FromArgb(245, 245, 247);

            lblTitle = new Label
            {
                Text = "近期扣款提醒",
                Location = new Point(24, 24),
                AutoSize = true,
                Font = new Font("Microsoft JhengHei UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(35, 35, 35)
            };
            lblSubtitle = new Label
            {
                Text = "未來 7 天",
                Location = new Point(148, 31),
                AutoSize = true,
                Font = new Font("Microsoft JhengHei UI", 10F),
                ForeColor = Color.FromArgb(170, 170, 170)
            };

            pnlCards = new FlowLayoutPanel
            {
                Location = new Point(24, 68),
                Size = new Size(740, 500),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 0, 4)
            };

            pnlEmpty = new Panel
            {
                Location = new Point(24, 68),
                Size = new Size(720, 120),
                BackColor = Color.White,
                Visible = false
            };
            pnlEmpty.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = RR(pnlEmpty.ClientRectangle, 12))
                using (var pen = new Pen(Color.FromArgb(220, 220, 220), 1))
                    e.Graphics.DrawPath(pen, path);
            };
            pnlEmpty.Controls.Add(new Label
            {
                Text = "✓  未來 7 天內沒有即將到期的扣款",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft JhengHei UI", 11F),
                ForeColor = Color.FromArgb(100, 160, 80)
            });

            this.Controls.AddRange(new Control[] { lblTitle, lblSubtitle, pnlCards, pnlEmpty });
        }

        public void LoadData()
        {
            int userId = Session.CurrentUser.UserID;
            DateTime today = DateTime.Today;

            var dtSubs = DatabaseHelper.ExecuteQuery(
                "SELECT * FROM Subscriptions WHERE UserID=@uid AND IsActive=1",
                new[] { new SqlParameter("@uid", userId) });

            var list = new List<(string Name, decimal Cost, DateTime NextDue, int DaysLeft, int ReminderDays)>();

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

                DateTime nextDue = sub.GetNextDueDate(today);
                int daysLeft = (nextDue.Date - today).Days;

                if (daysLeft >= 0 && daysLeft <= 7)
                    list.Add((sub.Name, sub.Cost, nextDue, daysLeft, sub.ReminderDays));
            }

            pnlCards.Controls.Clear();

            if (list.Count == 0)
            {
                pnlEmpty.Visible = true;
                pnlCards.Visible = false;
                return;
            }

            pnlEmpty.Visible = false;
            pnlCards.Visible = true;

            foreach (var item in list.OrderBy(x => x.DaysLeft))
                pnlCards.Controls.Add(CreateCard(item.Name, item.Cost, item.NextDue, item.DaysLeft, item.ReminderDays));
        }

        // ── 卡片：完全用 Paint 事件自繪，避免子控制項背景蓋掉圓角 ──────────
        private Panel CreateCard(string name, decimal cost, DateTime dueDate, int daysLeft, int reminderDays)
        {
            // ── 色彩分級 ─────────────────────────────────────────────────
            Color bgColor, nameColor, subColor, badgeBg, badgeFg;
            string iconText;

            if (daysLeft <= 3)
            {
                bgColor = Color.FromArgb(255, 242, 242);
                nameColor = Color.FromArgb(155, 40, 40);
                subColor = Color.FromArgb(185, 85, 85);
                badgeBg = Color.FromArgb(243, 178, 178);
                badgeFg = Color.FromArgb(115, 18, 18);
                iconText = "!";
            }
            else if (daysLeft > 3 && daysLeft <= 5)
            {
                bgColor = Color.FromArgb(255, 251, 230);
                nameColor = Color.FromArgb(135, 98, 18);
                subColor = Color.FromArgb(165, 128, 38);
                badgeBg = Color.FromArgb(248, 213, 116);
                badgeFg = Color.FromArgb(108, 72, 8);
                iconText = "○";
            }
            else
            {
                bgColor = Color.FromArgb(242, 250, 238);
                nameColor = Color.FromArgb(48, 118, 38);
                subColor = Color.FromArgb(78, 148, 58);
                badgeBg = Color.FromArgb(183, 226, 155);
                badgeFg = Color.FromArgb(28, 88, 18);
                iconText = "✓";
            }

            // ── 組裝子控制項（背景設成 Transparent）─────────────────────
            var lblIcon = new Label
            {
                Text = iconText,
                Size = new Size(28, 28),
                Location = new Point(18, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 12F, FontStyle.Bold),
                ForeColor = nameColor,
                BackColor = Color.Transparent
            };

            var lblName = new Label
            {
                Text = name,
                Location = new Point(56, 13),
                AutoSize = true,
                Font = new Font("Microsoft JhengHei UI", 11F, FontStyle.Bold),
                ForeColor = nameColor,
                BackColor = Color.Transparent
            };

            string reminderNote = reminderDays > 0
                ? $"  （提醒日 {dueDate.AddDays(-reminderDays):MM/dd}）" : "";
            var lblSub = new Label
            {
                Text = $"{dueDate:MM/dd} 扣款 · NT$ {cost:N0}{reminderNote}",
                Location = new Point(56, 43),
                AutoSize = true,
                Font = new Font("Microsoft JhengHei UI", 9F),
                ForeColor = subColor,
                BackColor = Color.Transparent
            };

            // Badge 尺寸與位置
            const int badgeW = 80, badgeH = 30, badgeR = 15;
            string badgeText = $"剩 {daysLeft} 天";

            // ── 外層卡片：完整 Paint 自繪（圓角背景 + Badge）────────────
            var card = new Panel
            {
                Size = new Size(720, 76),
                BackColor = Color.Transparent,   // 讓父容器背景透出
                Margin = new Padding(0, 0, 0, 10)
            };

            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // 1. 畫圓角卡片背景
                var cardRect = new Rectangle(0, 0, card.Width, card.Height);
                using (var path = RR(cardRect, 12))
                using (var brush = new SolidBrush(bgColor))
                    g.FillPath(brush, path);

                // 2. 畫 Badge 圓角背景
                int badgeX = card.Width - badgeW - 20;
                int badgeY = (card.Height - badgeH) / 2;
                var badgeRect = new Rectangle(badgeX, badgeY, badgeW, badgeH);
                using (var path = RR(badgeRect, badgeR))
                using (var brush = new SolidBrush(badgeBg))
                    g.FillPath(brush, path);

                // 3. 畫 Badge 文字
                TextRenderer.DrawText(g, badgeText,
                    new Font("Microsoft JhengHei UI", 9.5F, FontStyle.Bold),
                    badgeRect, badgeFg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            card.Controls.AddRange(new Control[] { lblIcon, lblName, lblSub });
            return card;
        }

        // ── 圓角 Path 輔助 ────────────────────────────────────────────────────
        private GraphicsPath RR(Rectangle b, int r)
        {
            var p = new GraphicsPath();
            int d = r * 2;
            p.AddArc(b.X, b.Y, d, d, 180, 90);
            p.AddArc(b.Right - d, b.Y, d, d, 270, 90);
            p.AddArc(b.Right - d, b.Bottom - d, d, d, 0, 90);
            p.AddArc(b.X, b.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}