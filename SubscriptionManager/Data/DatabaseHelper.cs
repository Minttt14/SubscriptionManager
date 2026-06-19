using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace SubscriptionManager.Data
{
    public static class DatabaseHelper
    {
        // ── 遵照 PPT 第 44 頁的標準連線字串 ─────────────────────────
        private static readonly string ConnectionString =
            @"Data Source=(LocalDB)\MSSQLLocalDB;" +
            @"AttachDbFilename=|DataDirectory|\SubscriptionDB.mdf;" +
            @"Integrated Security=True;";

        public static SqlConnection GetConnection() => new SqlConnection(ConnectionString);

        public static void InitializeDatabase()
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    // Users 資料表
                    ExecuteNonQuery(conn, @"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
                        CREATE TABLE Users (
                            UserID    INT IDENTITY(1,1) PRIMARY KEY,
                            Username  NVARCHAR(50) NOT NULL UNIQUE,
                            Password  NVARCHAR(256) NOT NULL,
                            CreatedAt DATETIME DEFAULT GETDATE()
                        )");

                    // Subscriptions 資料表 (使用 StartDate 與 ReminderDays)
                    ExecuteNonQuery(conn, @"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Subscriptions' AND xtype='U')
                        CREATE TABLE Subscriptions (
                            SubID        INT IDENTITY(1,1) PRIMARY KEY,
                            UserID       INT NOT NULL,
                            Name         NVARCHAR(100) NOT NULL,
                            Cost         DECIMAL(10,2) NOT NULL,
                            Period       NVARCHAR(10) NOT NULL,
                            StartDate    DATE NOT NULL,
                            ReminderDays INT NOT NULL,
                            Notes        NVARCHAR(200),
                            IsActive     BIT DEFAULT 1,
                            FOREIGN KEY (UserID) REFERENCES Users(UserID)
                        )");

                    // PaymentHistory 資料表
                    ExecuteNonQuery(conn, @"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='PaymentHistory' AND xtype='U')
                        CREATE TABLE PaymentHistory (
                            HistoryID   INT IDENTITY(1,1) PRIMARY KEY,
                            SubID       INT NOT NULL,
                            UserID      INT NOT NULL,
                            PaidDate    DATE NOT NULL,
                            Amount      DECIMAL(10,2) NOT NULL,
                            Status      NVARCHAR(20) DEFAULT '已繳',
                            FOREIGN KEY (SubID) REFERENCES Subscriptions(SubID),
                            FOREIGN KEY (UserID) REFERENCES Users(UserID)
                        )");
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
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
            catch (Exception ex) { return -1; }
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
            catch (Exception ex) { return -1; }
        }
    }
}