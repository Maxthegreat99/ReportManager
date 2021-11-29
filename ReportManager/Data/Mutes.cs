using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using TShockAPI;
using TShockAPI.DB;
using System.Linq;

namespace ReportManager.Data
{
    public class Mute
    {
        /// <summary>
        /// The ID of this mute.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// The IP of the target of this mute.
        /// </summary>
        public string IP { get; set; }

        /// <summary>
        /// The UUID of the target of this mute.
        /// </summary>
        public string UUID { get; set; }

        /// <summary>
        /// The username of this mute.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Reason of the mute.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// The moderator that muted this user.
        /// </summary>
        public int ModID { get; set; }

        /// <summary>
        /// The expiration moment of this mute.
        /// </summary>
        public DateTime Expiration { get; set; }

        /// <summary>
        /// The active state of this mute, true or false.
        /// </summary>
        public bool Active { get; set; }
    }

    public static class Mutes
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
                    db = new SqliteConnection($"uri=file://{Path.Combine(TShock.SavePath, "Mutes.sqlite")},Version=3");
                    break;

                default:
                    throw new ArgumentException("Invalid storage type in config.json!");
            }
            SqlTableCreator creator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            creator.EnsureTableStructure(new SqlTable("rmmutes",
                new SqlColumn("id", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                new SqlColumn("ip", MySqlDbType.Text),
                new SqlColumn("uuid", MySqlDbType.Text),
                new SqlColumn("username", MySqlDbType.Text),
                new SqlColumn("reason", MySqlDbType.Text),
                new SqlColumn("moderator", MySqlDbType.Int32),
                new SqlColumn("expiration", MySqlDbType.Text),
                new SqlColumn("active", MySqlDbType.Int32)));
        }

        public static Mute Get(int id)
        {
            using (var result = db.QueryReader("SELECT * FROM rmmutes WHERE id = @0 AND active = 1;", id))
            {
                if (result.Read())
                {
                    var mute = new Mute()
                    {
                        ID = result.Get<int>("id"),
                        IP = result.Get<string>("ip"),
                        UUID = result.Get<string>("uuid"),
                        Username = result.Get<string>("username"),
                        Reason = result.Get<string>("reason"),
                        ModID = result.Get<int>("moderator"),
                        Expiration = DateTime.Parse(result.Get<string>("expiration")),
                        Active = result.Get<int>("active") != 0
                    };
                    return mute;
                }
            }
            return null;
        }

        public static IEnumerable<Mute> GetAll()
        {
            using (var result = db.QueryReader("SELECT * FROM rmmutes WHERE active = 1;"))
            {
                while (result.Read())
                {
                    var mute = new Mute()
                    {
                        ID = result.Get<int>("id"),
                        IP = result.Get<string>("ip"),
                        UUID = result.Get<string>("uuid"),
                        Username = result.Get<string>("username"),
                        Reason = result.Get<string>("reason"),
                        ModID = result.Get<int>("moderator"),
                        Expiration = DateTime.Parse(result.Get<string>("expiration")),
                        Active = result.Get<int>("active") != 0
                    };
                    yield return mute;
                }
            }
        }

        public static IEnumerable<Mute> GetAll(string uuid)
        {
            using (var result = db.QueryReader("SELECT * FROM rmmutes WHERE uuid = @0 AND active = 1;", uuid))
            {
                while (result.Read())
                {
                    var mute = new Mute()
                    {
                        ID = result.Get<int>("id"),
                        IP = result.Get<string>("ip"),
                        UUID = result.Get<string>("uuid"),
                        Username = result.Get<string>("username"),
                        Reason = result.Get<string>("reason"),
                        ModID = result.Get<int>("moderator"),
                        Expiration = DateTime.Parse(result.Get<string>("expiration")),
                        Active = result.Get<int>("active") != 0
                    };
                    yield return mute;
                }
            }
        }

        public static void Insert(TSPlayer player, string reason, int modid, bool doip = false, DateTime? time = null)
        {
            Remove(player);
            db.Query("INSERT INTO rmmutes (ip, uuid, username, reason, moderator, expiration, active) VALUES (@0, @1, @2, @3, @4, @5, 1);", 
                doip ? player.IP : "", 
                player.UUID, 
                player.Name, 
                reason, 
                modid, 
                (time ?? DateTime.MaxValue).ToString());
        }

        public static void Insert(UserAccount account, string reason, int modid, DateTime? time = null)
        {
            Remove(account);
            db.Query("INSERT INTO rmmutes (ip, uuid, username, reason, moderator, expiration, active) VALUES (@0, @1, @2, @3, @4, @5, 1);", 
                "", 
                account.UUID, 
                account.Name, 
                reason, 
                modid, 
                (time ?? DateTime.MaxValue).ToString()); 
        }

        public static void Remove(int id)
            => db.Query("UPDATE rmmutes SET active = 0 WHERE id = @0;", id);

        public static void Remove(TSPlayer player)
            => db.Query("UPDATE rmmutes SET active = 0 WHERE uuid = @0 AND active = 1;", player.UUID);

        public static void Remove(UserAccount account)
            => db.Query("UPDATE rmmutes SET active = 0 WHERE uuid = @0 AND active = 1;", account.UUID);
    }
}
