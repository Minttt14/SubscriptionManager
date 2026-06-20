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
    /// <summary>
    /// 頁面：即將扣款通知
    /// 呈現未來 7 天內所有訂閱的下次扣款日，依天數排序，
    /// 以紅/橙/綠卡片呈現（仿截圖三風格）
    /// </summary>
    public class NotificationPage : UserControl
    {
        private Label           lblTitle;
        private Label           lblSubtitle;
        private FlowLayoutPanel pnlCards;
        private Panel           pnlEmpty;

        public NotificationPage() { InitializeComponent(); }

        private void InitializeComponent()
        {
            this.Size      = new Size(800, 600);
            this.BackColor = Color.FromArgb(245, 245, 247);
            this.Padding   = new Padding(24);

            lblTitle = new Label
            {
                Text      = "即將扣款通知",
                Location  = new Point(24, 20),
                AutoSize  = true,
                Font      = new Font("Microsoft JhengHei UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(35, 35, 35)
            };
            lblSubtitle = new Label
            {
                Text      = "未來 7 天內的扣款提醒",
                Location  = new Point(26, 52),
                AutoSize  = true,
                Font      = new Font("Microsoft JhengHei UI", 9.5F),
                ForeColor = Color.FromArgb(150, 150, 150)
            };

            // 卡片滾動容器
            pnlCards = new FlowLayoutPanel
            {
                Location      = new Point(24, 80),
                Size          = new Size(740, 480),
                Anchor        = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll    = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0, 4, 0, 0)
            };

            // 空狀態
            pnlEmpty = new Panel
            {
                Location  = new Point(24, 80),
                Size      = new Size(740, 200),
                BackColor = Color.White,
                Visible   = false
            };
            pnlEmpty.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(220, 220, 220)))
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlEmpty.Width - 1, pnlEmpty.Height - 1);
            };
            pnlEmpty.Controls.Add(new Label
            {
                Text      = "✓  未來 7 天內沒有即將到期的扣款",
                Location  = new Point(0, 80),
                Size      = new Size(740, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                Font      = new Font("Microsoft JhengHei UI", 11F),
                ForeColor = Color.FromArgb(100, 160, 80)
            });

            this.Controls.AddRange(new Control[] { lblTitle, lblSubtitle, pnlCards, pnlEmpty });
        }

        // ── 公開：載入 ────────────────────────────────────────────────────────
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

            // 如果沒有資料，顯示清空的面板
            if (list.Count == 0)
            {
                pnlEmpty.Visible = true;
                pnlCards.Visible = false;
                return;
            }

            pnlEmpty.Visible = false;
            pnlCards.Visible = true;

            // 依天數由近到遠排序
            foreach (var item in list.OrderBy(x => x.DaysLeft))
                pnlCards.Controls.Add(CreateCard(item.Name, item.Cost, item.NextDue, item.DaysLeft, item.ReminderDays));
        }

        // ── 通知卡片（對應截圖三風格）───────────────────────────────────────
        private Panel CreateCard(string name, decimal cost, DateTime dueDate, int daysLeft, int reminderDays)
        {
            // ── 色彩分級 ────────────────────────────────────────────────
            Color bgColor, nameColor, subColor, badgeBg, badgeFg;
            string iconText;

            if (daysLeft <= 3)
            {
                bgColor  = Color.FromArgb(255, 242, 242);
                nameColor = Color.FromArgb(160, 40, 40);
                subColor  = Color.FromArgb(190, 80, 80);
                badgeBg   = Color.FromArgb(245, 180, 180);
                badgeFg   = Color.FromArgb(120, 20, 20);
                iconText  = "!";
            }
            else if (daysLeft <= 7)
            {
                bgColor   = Color.FromArgb(255, 251, 230);
                nameColor = Color.FromArgb(140, 100, 20);
                subColor  = Color.FromArgb(170, 130, 40);
                badgeBg   = Color.FromArgb(250, 215, 120);
                badgeFg   = Color.FromArgb(110, 75, 10);
                iconText  = "○";
            }
            else
            {
                bgColor   = Color.FromArgb(242, 250, 238);
                nameColor = Color.FromArgb(50, 120, 40);
                subColor  = Color.FromArgb(80, 150, 60);
                badgeBg   = Color.FromArgb(185, 228, 158);
                badgeFg   = Color.FromArgb(30, 90, 20);
                iconText  = "✓";
            }

            // ── 外層卡片 Panel ──────────────────────────────────────────
            var card = new Panel
            {
                Size      = new Size(700, 76),
                BackColor = bgColor,
                Margin    = new Padding(0, 0, 0, 10)
            };

            // 圓角繪製
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = RoundedRect(card.ClientRectangle, 12))
                using (var brush = new SolidBrush(bgColor))
                    e.Graphics.FillPath(brush, path);
            };

            // 左側圖示
            var lblIcon = new Label
            {
                Text      = iconText,
                Location  = new Point(18, 24),
                Size      = new Size(26, 26),
                TextAlign = ContentAlignment.MiddleCenter,
                Font      = new Font("Arial", 11F, FontStyle.Bold),
                ForeColor = nameColor,
                BackColor = Color.Transparent
            };

            // 訂閱名稱
            var lblName = new Label
            {
                Text      = name,
                Location  = new Point(54, 14),
                AutoSize  = true,
                Font      = new Font("Microsoft JhengHei UI", 11F, FontStyle.Bold),
                ForeColor = nameColor,
                BackColor = Color.Transparent
            };

            // 扣款日 + 金額
            string reminderNote = reminderDays > 0 ? $"  （提醒日 {dueDate.AddDays(-reminderDays):MM/dd}）" : "";
            var lblSub = new Label
            {
                Text      = $"{dueDate:MM/dd} 扣款 · NT$ {cost:N0}{reminderNote}",
                Location  = new Point(54, 44),
                AutoSize  = true,
                Font      = new Font("Microsoft JhengHei UI", 9F),
                ForeColor = subColor,
                BackColor = Color.Transparent
            };

            // 右側 Badge「剩 N 天」
            var lblBadge = new Label
            {
                Text      = $"剩 {daysLeft} 天",
                Location  = new Point(590, 24),
                Size      = new Size(80, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                Font      = new Font("Microsoft JhengHei UI", 9.5F, FontStyle.Bold),
                ForeColor = badgeFg,
                BackColor = Color.Transparent
            };

            // Badge 圓角背景
            lblBadge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = RoundedRect(lblBadge.ClientRectangle, 14))
                using (var brush = new SolidBrush(badgeBg))
                {
                    e.Graphics.Clear(bgColor);
                    e.Graphics.FillPath(brush, path);
                }
                TextRenderer.DrawText(e.Graphics, lblBadge.Text, lblBadge.Font,
                    lblBadge.ClientRectangle, badgeFg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            card.Controls.AddRange(new Control[] { lblIcon, lblName, lblSub, lblBadge });
            return card;
        }

        // ── 圓角 Path ─────────────────────────────────────────────────────────
        private GraphicsPath RoundedRect(Rectangle b, int r)
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
