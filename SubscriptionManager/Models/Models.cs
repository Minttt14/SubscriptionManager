using System;

namespace SubscriptionManager.Models
{
    public class Subscription
    {
        public int SubID { get; set; }
        public int UserID { get; set; }
        public string Name { get; set; }
        public decimal Cost { get; set; }
        public string Period { get; set; }   // "每月" / "每季" / "每年"
        public DateTime StartDate { get; set; }  // 開始訂閱日
        public int ReminderDays { get; set; }    // 提前幾天提醒
        public string Notes { get; set; }
        public bool IsActive { get; set; } = true;

        // ── 動態計算：取得特定月份的扣款日 ──────────────────────────────
        public DateTime? GetDueDateInMonth(int year, int month)
        {
            var targetMonth = new DateTime(year, month, 1);
            var startMonth = new DateTime(StartDate.Year, StartDate.Month, 1);

            if (targetMonth < startMonth) return null; // 尚未開始訂閱

            // 處理小月沒有 31 號的問題 (例如 2/31 自動變成 2/28)
            int dueDay = Math.Min(StartDate.Day, DateTime.DaysInMonth(year, month));

            if (Period == "每月") return new DateTime(year, month, dueDay);
            if (Period == "每年" && month == StartDate.Month) return new DateTime(year, month, dueDay);
            if (Period == "每季" && (month - StartDate.Month) % 3 == 0) return new DateTime(year, month, dueDay);

            return null;
        }

        // ── 動態計算：尋找從「某天」開始算起，最近一次的扣款日 ───────────
        public DateTime GetNextDueDate(DateTime fromDate)
        {
            for (int i = 0; i < 12; i++) // 往後找 12 個月內一定有
            {
                var checkMonth = fromDate.AddMonths(i);
                var dueDate = GetDueDateInMonth(checkMonth.Year, checkMonth.Month);
                if (dueDate.HasValue && dueDate.Value.Date >= fromDate.Date)
                    return dueDate.Value;
            }
            return StartDate;
        }
    }

    public class User
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}