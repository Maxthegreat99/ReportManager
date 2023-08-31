using System;
using System.Collections.Generic;
using System.Linq;
using TShockAPI;
using ReportManager.Data;
using Discord.Webhook;
using Discord;


namespace ReportManager.Subcommands
{
    class Report
    {
        public static Dictionary<string, int> currentReportsPerPlayer = new Dictionary<string, int>();
        public static void Add(CommandArgs args)
        {
            if (!currentReportsPerPlayer.ContainsKey(args.Player.Name))
                currentReportsPerPlayer.Add(args.Player.Name, 0);

            else if (!args.Player.HasPermission(ReportManager.Permissions.ignoreReportLimit) 
                     && currentReportsPerPlayer[args.Player.Name] >= Config.Settings.MaxReportsPerMinute)
            {
                args.Player.SendErrorMessage("You have already reached your reports per minute limit!");
                return;
            } 

            ReportType type;
            switch(args.Parameters[0])
            {
                case "grief":
                    Reports.Insert(args.Player.Name, args.Player.X, args.Player.Y, ReportType.Grief);
                    type = ReportType.Grief;
                    break;
                case "tunnel":
                    Reports.Insert(args.Player.Name, args.Player.X, args.Player.Y, ReportType.Tunnel);
                    type = ReportType.Tunnel;
                    break;
                case "transfer":
                    Reports.Insert(args.Player.Name, args.Player.X, args.Player.Y, ReportType.Transfer);
                    type = ReportType.Transfer;
                    break;
                case "other":
                    if (args.Parameters.Count < 2 && args.Parameters.Count > 3)
                    {
                        args.Player.SendErrorMessage("Invalid syntax. Valid syntax: '/report other \"<reason>\"");
                        return;

                    }
                    Reports.Insert(args.Player.Name, args.Player.X, args.Player.Y, ReportType.Other, string.Join(" ", args.Parameters.Skip(1)));
                    type = ReportType.Other;
                    break;
                case "user":
                    if (args.Parameters.Count != 3)
                    {
                        args.Player.SendErrorMessage("Invalid syntax. Valid syntax: '/report user <player> \"<reason>\"'");
                        return;
                    }
                    Reports.Insert(args.Player.Name, args.Player.X, args.Player.Y, ReportType.User, string.Join(" ", args.Parameters.Skip(2)), args.Parameters[1]);
                    type = ReportType.User;
                    break;
                default:
                    Report.Help(args);
                    return;
            }

            if (Config.Settings.WebHook != "")
            {
                var client = new DiscordWebhookClient(Config.Settings.WebHook);
                var builder = new EmbedBuilder()
                    .WithTitle($"{type} report")
                    .AddField($"User", args.Player.Name)
                    .WithColor(Color.Purple)
                    .WithAuthor($"{TShock.Config.Settings.ServerName.Split(' ').Last()}")
                    .WithFooter($"{DateTime.UtcNow}");

                if (type == ReportType.Grief)
                    builder.AddField($"Position", $"{Math.Floor(args.Player.X / 16)}, {Math.Floor(args.Player.Y / 16)}");

                if (type == ReportType.Other)
                    builder.AddField($"Reason", args.Parameters[1]);

                if (type == ReportType.User)
                    builder.AddField("Target", args.Parameters[1])
                        .AddField("Reason", string.Join(" ", args.Parameters.Skip(2)));
                client.SendMessageAsync("", false, new[] { builder.Build() });
            }

            foreach(var player in TShock.Players)
            {
                if (player == null)
                    continue;
                if (player.HasPermission(ReportManager.Permissions.receiveReportNotif))
                    player.SendMessage(string.Format("[Report Manager] Player {0} has created a Report of type [{1}]!", args.Player.Name, args.Parameters[0]), Microsoft.Xna.Framework.Color.Magenta);
            }
            args.Player.SendSuccessMessage("Succesfully reported!");
            currentReportsPerPlayer[args.Player.Name]++;
        }

        public static void MuteAdd(CommandArgs args)
        {
            if (args.Parameters.Count != 2 && args.Parameters.Count != 3)
            {
                args.Player.SendErrorMessage("Invalid syntax. Valid syntax: '/report mute <player> <0d0h0m0s>'");
                return;
            }

            TSPlayer player;

            if (Extensions.ParsePlayer(args.Player, args.Parameters[1], out player, false) 
                || TShock.UserAccounts.GetUserAccountByName(args.Parameters[1]) != null )
            {
                DateTime duration = DateTime.MaxValue;
                int seconds = 0;

                if (args.Parameters.Count == 3 && TShock.Utils.TryParseTime(args.Parameters[2], out seconds))
                    duration = DateTime.UtcNow.AddSeconds(seconds);

                var username = (TSPlayer.FindByNameOrID(args.Parameters[1]).FirstOrDefault() != null ?
                                TSPlayer.FindByNameOrID(args.Parameters[1]).FirstOrDefault().Name :
                                TShock.UserAccounts.GetUserAccountByName(args.Parameters[1]).Name);

                Reports.InsertMute(username, duration);
                args.Player.SendSuccessMessage("Prohibited {0} from using /report {1}!", username, (seconds == 0 ? "permanently" : $"for {(seconds / 60)} minutes"));


                if (TSPlayer.FindByNameOrID(args.Parameters[1]).FirstOrDefault() != null)
                    TSPlayer.FindByNameOrID(args.Parameters[1]).FirstOrDefault().
                    SendErrorMessage("{0} prohibited you from using /report {1}!", args.Player.Name, (seconds == 0 ? "permanently" : $"for {(seconds / 60)} minutes"));

                return;
            }

            args.Player.SendErrorMessage("Could not find player or account!");
            

        }
        public static void MuteDel(CommandArgs args)
        {
            int index;

            if (args.Parameters.Count != 3)
            {
                args.Player.SendErrorMessage("Invalid syntax. Valid syntax: '/report mute del <index>'");
                return;
            }

            if (int.TryParse(args.Parameters[2], out index) && Reports.GetMutedUser(index) != null)
            {
                var muted = Reports.GetMutedUser(index);
                Reports.RemoveMute(index);
                args.Player.SendSuccessMessage("Successfully deleted report mute: {0}", index.ToString());

                if (TSPlayer.FindByNameOrID(muted.User).FirstOrDefault() != null)
                    TSPlayer.FindByNameOrID(muted.User).FirstOrDefault().SendSuccessMessage("You may now again use /report!");

                return;
            }

            args.Player.SendErrorMessage("Invalid ID or syntax. Valid syntax: '/report mute delete <id>'");
        }

        public static void ListMute(CommandArgs args)
        {
            if (args.Parameters.Count > 3)
            {
                args.Player.SendErrorMessage("Invalid syntax. Valid syntax: '/report mute list (page)'");
                return;
            }
            var reportMutes = Reports.GetAllMutes();

            if (!PaginationTools.TryParsePageNumber(args.Parameters, 2, args.Player, out int pageNumber))
                return;

            List<string> rlist = new List<string>();
            foreach (var m in reportMutes)
                rlist.Add($"{m.ID} | {m.User}, expires on: {m.Expiration}");

            PaginationTools.SendPage(args.Player, pageNumber, rlist,
                new PaginationTools.Settings
                {
                    HeaderFormat = "Reports Mutes ({0}/{1}):",
                    FooterFormat = "Type {0}report mute list {{0}} for more.".SFormat(Commands.Specifier),
                    NothingToDisplayString = "There are currently no report mutes."
                });
        }

        public static void Teleport(CommandArgs args)
        {
            if (args.Parameters.Count != 2)
            {
                args.Player.SendErrorMessage("Invalid syntax. Valid syntax: '/report teleport <id>'");
                return;
            }
            if (int.TryParse(args.Parameters[1], out int result))
            {
                var report = Reports.Get(result);
                if (report != null)
                {
                    args.Player.Teleport(report.X, report.Y);
                    args.Player.SendSuccessMessage($"Teleported to report: {result}");
                    args.Player.SendInfoMessage("Do not forget to delete the report after handling it!");
                }
                else args.Player.SendErrorMessage("Report not found. Are you sure the ID is correct?");
            }
            else args.Player.SendErrorMessage("Invalid syntax. Valid syntax: '/report teleport <id>'");
        }

        public static void Delete(CommandArgs args)
        {
            if (args.Parameters.Count != 2)
            {
                args.Player.SendErrorMessage("Invalid syntax. Valid syntax: '/report delete <id>'");
                return;
            }
            if (int.TryParse(args.Parameters[1], out int result))
            {
                var report = Reports.Get(result);
                if (report != null)
                {
                    Reports.Remove(result);
                    args.Player.SendSuccessMessage($"Deleted report: {result}");
                }
                else args.Player.SendErrorMessage("Report not found. Are you sure the ID is correct?");
            }
            else args.Player.SendErrorMessage("Invalid syntax. Valid syntax: '/report delete <id>'");
        }

        public static void Info(CommandArgs args)
        {
            if (args.Parameters.Count != 2)
            {
                args.Player.SendErrorMessage("Invalid syntax. Valid syntax: '/report info <id>'");
                return;
            }
            if (int.TryParse(args.Parameters[1], out int result))
            {
                var report = Reports.Get(result);
                string str = "";
                switch(report.Type)
                {
                    case ReportType.User:
                        str = $"Type: {report.Type}, Target: {report.Target}, \nReason: {report.Reason}, \nPosition: X/{Math.Floor(report.X / 16)} - Y/{Math.Floor(report.Y / 16)}";
                        break;
                    case ReportType.Grief:
                    case ReportType.Transfer:
                    case ReportType.Tunnel:
                        str = $"Type: {report.Type}, \nPosition: X/{Math.Floor(report.X / 16)} - Y/{Math.Floor(report.Y / 16)}";
                        break;
                    case ReportType.Other:
                        str = $"Type: {report.Type}, \nReason: {report.Reason}, \nPosition: X/{report.X / 16} - Y/{report.Y / 16}";
                        break;
                }
                    
                args.Player.SendInfoMessage("Viewing report: " + report.ID + "\n" + str);
            }
            else args.Player.SendErrorMessage("Invalid syntax. Valid syntax: '/report info <id>'");
        }

        public static void List(CommandArgs args)
        {
            if (args.Parameters.Count > 2)
            {
                args.Player.SendErrorMessage("Invalid syntax. Valid syntax: '/report list (page)'");
                return;
            }
            var reports = Reports.GetAll();

            if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out int pageNumber))
                return;

            List<string> rlist = new List<string>();
            foreach (var r in reports)
                rlist.Add($"{r.ID} | {r.Type}, {r.User} {((r.Reason != null) ? "" : $"- {r.Reason}")}");

            PaginationTools.SendPage(args.Player, pageNumber, rlist,
                new PaginationTools.Settings
                {
                    HeaderFormat = "Reports ({0}/{1}):",
                    FooterFormat = "Type {0}report list {{0}} for more.".SFormat(Commands.Specifier),
                    NothingToDisplayString = "There are currently no reports."
                });
        }

        public static void Help(CommandArgs args)
        {
            if (args.Parameters.Count > 2)
            {
                args.Player.SendErrorMessage($"Invalid syntax. Valid syntax: '/report help{(args.Player.HasPermission("reportmanager.staff") ? " (page)'" : "'")}");
                return;
            }

            if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out int pageNumber))
                return;

            var lines = new List<string>
            {
                "<type> (reason) (target) - Creates a report with provided type.",
                "| ^  Valid types are: 'grief', 'tunnel', 'transfer', 'user', 'other'. Examples:",
                "| '/report grief' *No other info required. Staff will teleport to the location you report at.",
                "| '/report user Rozen \"being a twat\".",
            };
            if (args.Player.HasPermission(ReportManager.Permissions.staff))
                lines.AddRange(new List<string>
                {
                            "teleport <id> - Teleports to a report with provided ID.",
                            "info <id> - Gets all info on a report.",
                            "list - Lists all reports.",
                            "delete <name> - Deletes a report.",
                            "mute <name> - Prohibits someone from using /report.",
                            "mute list - Lists all /report muted users.",
                            "mute del <id> - Deletes a report mute."
                });

            PaginationTools.SendPage(args.Player, pageNumber, lines,
                    new PaginationTools.Settings
                    {
                        HeaderFormat = "Report Sub-Commands ({0}/{1}):",
                        FooterFormat = "Type {0}report help {{0}} for more sub-commands.".SFormat(Commands.Specifier)
                    }
                );
        }

    }
}
