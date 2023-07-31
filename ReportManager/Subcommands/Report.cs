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
        public static void Add(CommandArgs args)
        {
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

            args.Player.SendSuccessMessage("Succesfully reported!");
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
