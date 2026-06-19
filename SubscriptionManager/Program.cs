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

            // 直接呼叫建表邏輯，因為檔案我們已經在 VS 裡建好了
            DatabaseHelper.InitializeDatabase();

            Application.Run(new LoginForm());
        }
    }
}
