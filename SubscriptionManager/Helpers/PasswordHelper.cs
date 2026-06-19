using System;
using System.Security.Cryptography;
using System.Text;

namespace SubscriptionManager.Helpers
{
    /// <summary>
    /// 密碼雜湊工具（SHA-256）
    /// 不可逆雜湊，避免明文儲存密碼
    /// </summary>
    public static class PasswordHelper
    {
        public static string HashPassword(string plainText)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(plainText));
                var sb = new StringBuilder();
                foreach (byte b in bytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        public static bool VerifyPassword(string plainText, string hash)
        {
            return HashPassword(plainText) == hash;
        }
    }
}
