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
    /// <summary>
    /// 頁面：財務分析儀表板
    /// - 上方月份導覽（可切換查看歷史月份）
    /// - 兩張統計卡（本月支出 + 較上月差異、訂閱數量 + 週期分布）
    /// - 圓餅圖（本月支出比例）
    /// </summary>
    public class DashboardPage : UserControl
    {
        // ── 月份導覽 ──────────────────────────────────────────────────────────
        private Button  btnPrevMonth, btnNextMonth;
        private Label   lblMonthYear;
        private DateTime _viewMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        // ── 統計卡 ────────────────────────────────────────────────────────────
        private Label lblTotalCostVal;   // NT$ 1,450
        private Label lblCostDiff;       // 較上月 +NT$ 90
        private Label lblTotalCountVal;  // 3 個
        private Label lblCountDetail;    // 月繳2 季繳1 年繳0

        // ── 圓餅圖 ────────────────────────────────────────────────────────────
        private Chart chartPie;

        public DashboardPage() { InitializeComponent(); }

        private void InitializeComponent()
        {
            this.Size      = new Size(800, 600);
            this.BackColor = Color.FromArgb(245, 245, 247);
            this.Padding   = new Padding(20);

            // ── 月份導覽列 ────────────────────────────────────────────────
            btnPrevMonth = MakeNavBtn("‹", new Point(16, 16));
            btnPrevMonth.Click += (s, e) => { _viewMonth = _viewMonth.AddMonths(-1); LoadData(); };

            lblMonthYear = new Label
            {
                Location  = new Point(56, 18),
                Size      = new Size(200, 28),
                TextAlign = ContentAlignment.MiddleCenter,
                Font      = new Font("Microsoft JhengHei UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40)
            };

            btnNextMonth = MakeNavBtn("›", new Point(264, 16));
            btnNextMonth.Click += (s, e) => { _viewMonth = _viewMonth.AddMonths(1); LoadData(); };

            // ── 卡片 1：本月支出 ──────────────────────────────────────────
            lblTotalCostVal = new Label
            {
                Font      = new Font("Microsoft JhengHei UI", 20F, FontStyle.Bold),
                Location  = new Point(14, 36),
                AutoSize  = true,
                ForeColor = Color.FromArgb(30, 30, 30)
            };
            lblCostDiff = new Label
            {
                Location  = new Point(14, 72),
                AutoSize  = true,
                Font      = new Font("Microsoft JhengHei UI", 8.5F),
                ForeColor = Color.FromArgb(140, 140, 140)
            };
            var card1 = BuildCard("本月支出", new Point(16, 80), new Size(390, 100),
                lblTotalCostVal, lblCostDiff);

            // ── 卡片 2：訂閱項目 ──────────────────────────────────────────
            lblTotalCountVal = new Label
            {
                Font      = new Font("Microsoft JhengHei UI", 20F, FontStyle.Bold),
                Location  = new Point(14, 36),
                AutoSize  = true,
                ForeColor = Color.FromArgb(30, 30, 30)
            };
            lblCountDetail = new Label
            {
                Location  = new Point(14, 72),
                AutoSize  = true,
                Font      = new Font("Microsoft JhengHei UI", 8.5F),
                ForeColor = Color.FromArgb(140, 140, 140)
            };
            var card2 = BuildCard("當前訂閱項目", new Point(426, 80), new Size(390, 100),
                lblTotalCountVal, lblCountDetail);

            // ── 圓餅圖 ────────────────────────────────────────────────────
            chartPie = new Chart
            {
                Location  = new Point(16, 200),
                Size      = new Size(800, 460),
                BackColor = Color.White
            };
            chartPie.Titles.Add(new Title("支出比例分析")
            {
                Font      = new Font("Microsoft JhengHei UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 80, 80)
            });
            var ca = new ChartArea("main");
            chartPie.ChartAreas.Add(ca);
            var series = new Series("支出")
            {
                ChartType           = SeriesChartType.Pie,
                IsValueShownAsLabel = true,
                LabelFormat         = "#PERCENT{P0}"
            };
            chartPie.Series.Add(series);

            this.Controls.AddRange(new Control[]
            {
                btnPrevMonth, lblMonthYear, btnNextMonth,
                card1, card2, chartPie
            });
        }

        // ── 公開：載入 ────────────────────────────────────────────────────────
        public void LoadData()
        {
            int userId = Session.CurrentUser.UserID;
            lblMonthYear.Text = _viewMonth.ToString("yyyy 年 M 月");

            // ── 1. 本月所有啟用訂閱 ──────────────────────────────────────
            var dtSubs = DatabaseHelper.ExecuteQuery(
                "SELECT * FROM Subscriptions WHERE UserID=@uid AND IsActive=1",
                new[] { new SqlParameter("@uid", userId) });

            // ── 2. 自動結算（僅針對「今天」所在月份，歷史月份不重複插入）──
            DateTime today = DateTime.Today;
            if (_viewMonth.Year == today.Year && _viewMonth.Month == today.Month)
            {
                foreach (DataRow row in dtSubs.Rows)
                {
                    int subId = Convert.ToInt32(row["SubID"]);
                    decimal cost = Convert.ToDecimal(row["Cost"]);
                    var sub = new Subscription
                    {
                        Period    = row["Period"].ToString(),
                        StartDate = Convert.ToDateTime(row["StartDate"])
                    };
                    DateTime? due = sub.GetDueDateInMonth(today.Year, today.Month);
                    if (!due.HasValue || due.Value.Date > today) continue;
                    var ck = DatabaseHelper.ExecuteQuery(
                        "SELECT COUNT(*) FROM PaymentHistory WHERE SubID=@sid AND YEAR(PaidDate)=@yr AND MONTH(PaidDate)=@mo",
                        new[] { new SqlParameter("@sid", subId), new SqlParameter("@yr", due.Value.Year), new SqlParameter("@mo", due.Value.Month) });
                    if (Convert.ToInt32(ck.Rows[0][0]) == 0)
                        DatabaseHelper.ExecuteNonQuery(
                            "INSERT INTO PaymentHistory(SubID,UserID,PaidDate,Amount,Status) VALUES(@sid,@uid,@pd,@amt,N'已繳')",
                            new[] { new SqlParameter("@sid", subId), new SqlParameter("@uid", userId), new SqlParameter("@pd", due.Value.Date), new SqlParameter("@amt", cost) });
                }
            }

            // ── 3. 本月支出合計 ───────────────────────────────────────────
            var dtThisMonth = DatabaseHelper.ExecuteQuery(
                "SELECT ISNULL(SUM(Amount),0) AS Total FROM PaymentHistory WHERE UserID=@uid AND YEAR(PaidDate)=@yr AND MONTH(PaidDate)=@mo",
                new[] { new SqlParameter("@uid", userId), new SqlParameter("@yr", _viewMonth.Year), new SqlParameter("@mo", _viewMonth.Month) });
            decimal thisTotal = Convert.ToDecimal(dtThisMonth.Rows[0]["Total"]);
            lblTotalCostVal.Text = $"NT$ {thisTotal:N0}";

            // ── 4. 上月支出合計（計算差異）───────────────────────────────
            var prevMonth = _viewMonth.AddMonths(-1);
            var dtPrevMonth = DatabaseHelper.ExecuteQuery(
                "SELECT ISNULL(SUM(Amount),0) AS Total FROM PaymentHistory WHERE UserID=@uid AND YEAR(PaidDate)=@yr AND MONTH(PaidDate)=@mo",
                new[] { new SqlParameter("@uid", userId), new SqlParameter("@yr", prevMonth.Year), new SqlParameter("@mo", prevMonth.Month) });
            decimal prevTotal = Convert.ToDecimal(dtPrevMonth.Rows[0]["Total"]);

            decimal diff = thisTotal - prevTotal;
            if (prevTotal == 0)
                lblCostDiff.Text = "上月無紀錄";
            else if (diff > 0)
            {
                lblCostDiff.Text      = $"較上月 ▲ NT$ {diff:N0}";
                lblCostDiff.ForeColor = Color.FromArgb(180, 50, 50);
            }
            else if (diff < 0)
            {
                lblCostDiff.Text      = $"較上月 ▼ NT$ {Math.Abs(diff):N0}";
                lblCostDiff.ForeColor = Color.FromArgb(50, 140, 70);
            }
            else
            {
                lblCostDiff.Text      = "與上月持平";
                lblCostDiff.ForeColor = Color.FromArgb(140, 140, 140);
            }

            // ── 5. 訂閱數量 + 週期分布 ────────────────────────────────────
            int monthly  = dtSubs.AsEnumerable().Count(r => r["Period"].ToString() == "每月");
            int quarterly = dtSubs.AsEnumerable().Count(r => r["Period"].ToString() == "每季");
            int yearly   = dtSubs.AsEnumerable().Count(r => r["Period"].ToString() == "每年");
            lblTotalCountVal.Text = $"{dtSubs.Rows.Count} 個";
            lblCountDetail.Text   = $"月繳 {monthly}  季繳 {quarterly}  年繳 {yearly}";

            // ── 6. 圓餅圖 ────────────────────────────────────────────────
            chartPie.Series["支出"].Points.Clear();
            var dtPie = DatabaseHelper.ExecuteQuery(@"
                SELECT s.Name, SUM(h.Amount) AS TotalCost
                FROM PaymentHistory h
                JOIN Subscriptions s ON h.SubID = s.SubID
                WHERE h.UserID=@uid AND YEAR(h.PaidDate)=@yr AND MONTH(h.PaidDate)=@mo
                GROUP BY s.Name",
                new[] { new SqlParameter("@uid", userId), new SqlParameter("@yr", _viewMonth.Year), new SqlParameter("@mo", _viewMonth.Month) });

            Color[] pieColors = {
                ColorTranslator.FromHtml("#E3ECF3"), 
                ColorTranslator.FromHtml("#E9EDC9"), 
                ColorTranslator.FromHtml("#A7C2E0"), 
                ColorTranslator.FromHtml("#FAE1DD"), 
                ColorTranslator.FromHtml("#DCD7ED"),
                ColorTranslator.FromHtml("#FFF8E7")
            };
            int ci = 0;
            foreach (DataRow row in dtPie.Rows)
            {
                int idx = chartPie.Series["支出"].Points.AddXY(row["Name"].ToString(), Convert.ToDouble(row["TotalCost"]));
                chartPie.Series["支出"].Points[idx].Color = pieColors[ci % pieColors.Length];
                chartPie.Series["支出"].Points[idx].Label = $"{row["Name"]}\n#PERCENT{{P0}}";
                ci++;
            }
            if (dtPie.Rows.Count == 0)
                chartPie.Titles[0].Text = "本月尚無支出紀錄";
            else
                chartPie.Titles[0].Text = "支出比例分析";
        }

        // ── 輔助：建立白色統計卡 ──────────────────────────────────────────────
        private Panel BuildCard(string title, Point loc, Size size, params Label[] valueLabels)
        {
            var panel = new Panel { BackColor = Color.White, Location = loc, Size = size };
            panel.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(225, 225, 225), 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            };
            panel.Controls.Add(new Label
            {
                Text      = title,
                Location  = new Point(14, 10),
                AutoSize  = true,
                Font      = new Font("Microsoft JhengHei UI", 8.5F),
                ForeColor = Color.FromArgb(130, 130, 130)
            });
            foreach (var lbl in valueLabels)
                panel.Controls.Add(lbl);
            return panel;
        }

        // ── 輔助：月份導覽按鈕 ───────────────────────────────────────────────
        private Button MakeNavBtn(string text, Point loc)
        {
            var btn = new Button
            {
                Text      = text,
                Location  = loc,
                Size      = new Size(36, 36),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Arial", 14F),
                ForeColor = Color.FromArgb(80, 80, 80),
                Cursor    = Cursors.Hand,
                BackColor = Color.White
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(220, 220, 220);
            btn.FlatAppearance.BorderSize  = 1;
            return btn;
        }
    }
}
