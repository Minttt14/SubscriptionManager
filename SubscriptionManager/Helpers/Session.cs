using SubscriptionManager.Models;

namespace SubscriptionManager.Helpers
{
    /// <summary>
    /// 全域 Session：儲存目前登入的使用者
    /// 靜態類別，整個 App 生命週期都可取用
    /// </summary>
    public static class Session
    {
        public static User CurrentUser { get; set; } = null;

        public static bool IsLoggedIn => CurrentUser != null;

        public static void Logout()
        {
            CurrentUser = null;
        }
    }
}
