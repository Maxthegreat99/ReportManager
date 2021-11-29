using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using TShockAPI;
using TShockAPI.DB;

namespace ReportManager.Data
{
    public enum ReportType
    {
        /// <summary>
        /// Grief report.
        /// </summary>
        Grief,

        /// <summary>
        /// Tunnel report.
        /// </summary>
        Tunnel,

        /// <summary>
        /// Targeted report, for a specified user.
        /// </summary>
        User,

        /// <summary>
        /// Transfer report.
        /// </summary>
        Transfer,

        /// <summary>
        /// Other reports.
        /// </summary>
        Other,
    }

    public class Report
    {
        /// <summary>
        /// ID of the report.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// ID of the user doing this report.
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// X pos of report.
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Y pos of report.
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// Type of the report.
        /// </summary>
        public ReportType Type { get; set; }

        /// <summary>
        /// Reason of the report.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// The time when this report was made.
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// The target of this report.
        /// </summary>
        public string Target { get; set; }
    }

    public static class Reports
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
                    db = new SqliteConnection($"uri=file://{Path.Combine(TShock.SavePath, "Reports.sqlite")},Version=3");
                    break;

                default:
                    throw new ArgumentException("Invalid storage type in config.json!");
            }
            SqlTableCreator creator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            creator.EnsureTableStructure(new SqlTable("rmreports",
                new SqlColumn("id", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                new SqlColumn("username", MySqlDbType.Text),
                new SqlColumn("x", MySqlDbType.Text),
                new SqlColumn("y", MySqlDbType.Text),
                new SqlColumn("type", MySqlDbType.Int32),
                new SqlColumn("reason", MySqlDbType.Text),
                new SqlColumn("time", MySqlDbType.Text),
                new SqlColumn("target", MySqlDbType.Text)));
        }

        public static void Insert(string username, float x, float y, ReportType type, string reason = null, string target = null)
        {
            string query = $"INSERT INTO rmreports (username, x, y, type, reason, time, target) VALUES ('{username}', '{x}', '{y}', {((int)type)}, '{reason}', '{(DateTime.UtcNow)}', '{target}');";
            db.Query(query);
        }

        public static void Remove(int id)
        {
            string query = $"DELETE FROM rmreports WHERE id = {id}";
            db.Query(query);
        }

        private static ReportType Parse(int i)
        {
            switch(i)
            {
                case 0:
                    return ReportType.Grief;
                case 1:
                    return ReportType.Tunnel;
                case 2:
                    return ReportType.User;
                case 3:
                    return ReportType.Transfer;
                default:
                    return ReportType.Other;
            }
        }

        public static Report Get(int id)
        {
            string query = $"SELECT * FROM rmreports WHERE id = {id};";
            using (var result = db.QueryReader(query))
            {
                if (result.Read())
                {
                    var report = new Report()
                    {
                        ID = result.Get<int>("id"),
                        User = result.Get<string>("username"),
                        Type = Parse(result.Get<int>("type")),
                        X = result.Get<float>("x"),
                        Y = result.Get<float>("y"),
                        Reason = result.Get<string>("reason"),
                        Target = result.Get<string>("target"),
                        Time = DateTime.Parse(result.Get<string>("time"))
                    };
                    return report;
                }
            }
            return null;
        }

        public static IEnumerable<Report> GetAll()
        {
            string query = $"SELECT * FROM rmreports;";
            using (var result = db.QueryReader(query))
            {
                while (result.Read())
                {
                    var report = new Report()
                    {
                        ID = result.Get<int>("id"),
                        User = result.Get<string>("username"),
                        Type = Parse(result.Get<int>("type")),
                        X = result.Get<float>("x"),
                        Y = result.Get<float>("y"),
                        Reason = result.Get<string>("reason"),
                        Target = result.Get<string>("target"),
                        Time = DateTime.Parse(result.Get<string>("time"))
                    };

                    yield return report;
                }
            }
        }
    }
}
