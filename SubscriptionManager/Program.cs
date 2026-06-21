using System;
using System.IO;
using System.Windows.Forms;
using SubscriptionManager.Data;
using SubscriptionManager.Forms;

namespace SubscriptionManager
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 設定資料庫路徑
            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SubscriptionManager");
            Directory.CreateDirectory(dbPath);
            AppDomain.CurrentDomain.SetData("DataDirectory", dbPath);

            DatabaseHelper.InitializeDatabase();
            Application.Run(new LoginForm());
        }
    }
}