using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using TShockAPI;
using TShockAPI.DB;

namespace ReportManager.Data
{
    public class Warning
    {
        /// <summary>
        /// The ID of this warning.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// The target of this warning.
        /// </summary>
        public string UUID { get; set; }

        /// <summary>
        /// The base username for this warning.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Reason of the warning.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// The moderator that warned.
        /// </summary>
        public int ModID { get; set; }
    }

    public static class Warnings
    {
        private static IDbConnection db;

        /// <summary>
        /// Initializes the DB connection.
        /// </summary>
        public static void Initialize()
        {
            switch (TShock.Config.Settings.StorageType.ToLower())
            {
                case "mysql":
                    var dbHost = TShock.Config.Settings.MySqlHost.Split(':');

                    db = new MySqlConnection($"Server={dbHost[0]};" +
                                                $"Port={(dbHost.Length == 1 ? "3306" : dbHost[1])};" +
                                                $"Database={TShock.Config.Settings.MySqlDbName};" +
                                                $"Uid={TShock.Config.Settings.MySqlUsername};" +
                                                $"Pwd={TShock.Config.Settings.MySqlPassword};");

                    break;

                case "sqlite":
                    db = new SqliteConnection($"Data Source={Path.Combine(TShock.SavePath, "Warnings.sqlite")}");
                    break;

                default:
                    throw new ArgumentException("Invalid storage type in config.json!");
            }
            SqlTableCreator creator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            creator.EnsureTableStructure(new SqlTable("rmwarnings",
                new SqlColumn("id", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                new SqlColumn("uuid", MySqlDbType.Text),
                new SqlColumn("username", MySqlDbType.Text),
                new SqlColumn("reason", MySqlDbType.Text),
                new SqlColumn("moderator", MySqlDbType.Text)));
        }

        public static Warning Get(int id)
        {
            using (var result = db.QueryReader("SELECT * FROM rmwarnings WHERE id = @0;", id))
            {
                if (result.Read())
                {
                    var warn = new Warning()
                    {
                        ID = result.Get<int>("id"),
                        UUID = result.Get<string>("uuid"),
                        Username = result.Get<string>("username"),
                        Reason = result.Get<string>("reason"),
                        ModID = result.Get<int>("moderator")
                    };
                    return warn;
                }
            }
            return null;
        }

        public static IEnumerable<Warning> GetAll()
        {
            using (var result = db.QueryReader("SELECT * FROM rmwarnings;"))
            {
                while (result.Read())
                {
                    var warning = new Warning()
                    {
                        ID = result.Get<int>("id"),
                        UUID = result.Get<string>("uuid"),
                        Username = result.Get<string>("username"),
                        ModID = result.Get<int>("moderator"),
                        Reason = result.Get<string>("reason")
                    };

                    yield return warning;
                }
            }
        }

        public static IEnumerable<Warning> GetAll(string uuid)
        {
            using (var result = db.QueryReader("SELECT * FROM rmwarnings WHERE uuid = @0;", uuid))
            {
                while (result.Read())
                {
                    var warning = new Warning()
                    {
                        ID = result.Get<int>("id"),
                        UUID = result.Get<string>("uuid"),
                        Username = result.Get<string>("username"),
                        ModID = result.Get<int>("moderator"),
                        Reason = result.Get<string>("reason")
                    };

                    yield return warning;
                }
            }
        }

        public static void Insert(string uuid, string username, string reason, int modId)
            => db.Query("INSERT INTO rmwarnings (uuid, username, reason, moderator) VALUES (@0, @1, @2, @3);", uuid, username, reason, modId);

        public static void Remove(int id)
            => db.Query("DELETE FROM rmwarnings WHERE id = @0;", id);
    }
}

