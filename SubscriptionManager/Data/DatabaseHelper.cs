using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows.Forms;

namespace SubscriptionManager.Data
{
    public static class DatabaseHelper
    {
        private static readonly string ConnectionString =
            @"Data Source=(LocalDB)\MSSQLLocalDB;" +
            @"Initial Catalog=SubManagerDB;" +
            @"Integrated Security=True;";

        public static SqlConnection GetConnection() => new SqlConnection(ConnectionString);

        public static void InitializeDatabase()
        {
            try
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SubscriptionManager");
                string dbPath = Path.Combine(folder, "SubManagerDB.mdf");
                string logPath = Path.Combine(folder, "SubManagerDB_log.ldf");

                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                using (var conn = new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;"))
                {
                    conn.Open();
                    // 檢查資料庫是否存在於 SQL Server 執行個體中
                    var checkCmd = new SqlCommand("SELECT count(*) FROM sys.databases WHERE name = 'SubManagerDB'", conn);
                    bool exists = (int)checkCmd.ExecuteScalar() > 0;

                    if (!exists)
                    {
                        // 如果 SQL Server 裡沒有這個資料庫，才執行建立或附加
                        // 如果檔案已經在硬碟上存在，使用 ATTACH
                        if (File.Exists(dbPath))
                        {
                            string attachSql = $@"CREATE DATABASE SubManagerDB ON (FILENAME = '{dbPath}'), (FILENAME = '{logPath}') FOR ATTACH";
                            new SqlCommand(attachSql, conn).ExecuteNonQuery();
                        }
                        else
                        {
                            // 如果檔案也不在硬碟上，才是真的第一次建立
                            string createSql = $@"CREATE DATABASE SubManagerDB ON PRIMARY (NAME=SubManagerDB, FILENAME='{dbPath}') LOG ON (NAME=SubManagerDB_log, FILENAME='{logPath}')";
                            new SqlCommand(createSql, conn).ExecuteNonQuery();
                        }
                    }
                }

                // 3. 連線到目標資料庫並初始化表格
                using (var conn = GetConnection())
                {
                    conn.Open();
                    ExecuteNonQuery(conn, @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='Users')
                        CREATE TABLE Users (UserID INT IDENTITY(1,1) PRIMARY KEY, Username NVARCHAR(50) NOT NULL UNIQUE, Password NVARCHAR(256) NOT NULL, CreatedAt DATETIME DEFAULT GETDATE())");

                    ExecuteNonQuery(conn, @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='Subscriptions')
                        CREATE TABLE Subscriptions (SubID INT IDENTITY(1,1) PRIMARY KEY, UserID INT NOT NULL, Name NVARCHAR(100) NOT NULL, Cost DECIMAL(10,2) NOT NULL, Period NVARCHAR(10) NOT NULL, StartDate DATE NOT NULL, ReminderDays INT NOT NULL, Notes NVARCHAR(200), IsActive BIT DEFAULT 1, FOREIGN KEY (UserID) REFERENCES Users(UserID))");

                    ExecuteNonQuery(conn, @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='PaymentHistory')
                        CREATE TABLE PaymentHistory (HistoryID INT IDENTITY(1,1) PRIMARY KEY, SubID INT NOT NULL, UserID INT NOT NULL, PaidDate DATE NOT NULL, Amount DECIMAL(10,2) NOT NULL, Status NVARCHAR(20) DEFAULT '已繳', FOREIGN KEY (SubID) REFERENCES Subscriptions(SubID), FOREIGN KEY (UserID) REFERENCES Users(UserID))");
                }
            }
            catch (Exception ex) { MessageBox.Show("資料庫初始化失敗: " + ex.Message); }
        }

        private static void ExecuteNonQuery(SqlConnection conn, string sql)
        {
            using (var cmd = new SqlCommand(sql, conn)) cmd.ExecuteNonQuery();
        }

        public static DataTable ExecuteQuery(string sql, SqlParameter[] parameters = null)
        {
            var dt = new DataTable();
            try
            {
                using (var conn = GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (parameters != null) cmd.Parameters.AddRange(parameters);
                    conn.Open();
                    using (var adapter = new SqlDataAdapter(cmd)) adapter.Fill(dt);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
            return dt;
        }

        public static int ExecuteNonQuery(string sql, SqlParameter[] parameters = null)
        {
            try
            {
                using (var conn = GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (parameters != null) cmd.Parameters.AddRange(parameters);
                    conn.Open();
                    return cmd.ExecuteNonQuery();
                }
            }
            catch { return -1; }
        }

        public static int ExecuteInsertGetId(string sql, SqlParameter[] parameters = null)
        {
            try
            {
                using (var conn = GetConnection())
                using (var cmd = new SqlCommand(sql + "; SELECT SCOPE_IDENTITY();", conn))
                {
                    if (parameters != null) cmd.Parameters.AddRange(parameters);
                    conn.Open();
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            catch { return -1; }
        }
    }
}