using System;
using System.Collections.Generic;
using TShockAPI;
using ReportManager.Data;

namespace ReportManager.Subcommands
{
    class Mute
    {
        public static void Add(CommandArgs args)
        {
            if (args.Parameters.Count < 1 || args.Parameters.Count > 4)
            {
                args.Player.SendErrorMessage("Invalid syntax. Valid syntax: '/mute <player> (time) (reason) (doIP <only works for online players>)");
                return;
            }

            var duration = DateTime.MaxValue;
            bool doip = false;

            if (args.Parameters.Count == 4)
                if (!bool.TryParse(args.Parameters[3], out doip))
                {
                    args.Player.SendErrorMessage("Invalid bool on parameter: (doip). Options are: 'true' or 'false'.\n " +
                        "Set to true in assumption that this parameter is only defined if it is required.");
                }
            TSPlayer player = args.Player;
            if (Extensions.ParsePlayer(args.Player, args.Parameters[0], out player, false))
            {
                player.mute = true;
                if (args.Parameters.Count > 1)
                {
                    if (TShock.Utils.TryParseTime(args.Parameters[1], out int seconds))
                    {
                        duration = DateTime.UtcNow.AddSeconds(seconds);
                        if (args.Parameters.Count > 2)
                        {
                            Mutes.Insert(player, args.Parameters[2], args.Player.Account.ID, doip, duration);
                            if (duration != DateTime.MaxValue)
                            {
                                args.Player.SendInfoMessage($"Muted: {player.Name} for {(seconds / 60)} minutes. (Until: {duration}) for: {args.Parameters[2]}.");
                                player.SendErrorMessage($"You were muted for {(seconds / 60)} minutes. For: {args.Parameters[2]}");
                            }
                        }
                        else
                        {
                            Mutes.Insert(player, "", args.Player.Account.ID, doip, duration);
                            args.Player.SendInfoMessage($"Muted: {player.Name} for {(seconds / 60)} minutes.");
                            player.SendErrorMessage($"You were muted for {(seconds / 60)}");
                        }
                    }
                    else if (args.Parameters[1] == "0")
                    {
                        if (args.Parameters.Count > 2)
                        {
                            Mutes.Insert(player, args.Parameters[2], args.Player.Account.ID, doip);
                            args.Player.SendInfoMessage($"Muted: {player.Name} permanently for: {args.Parameters[2]}.");
                            player.SendErrorMessage($"You were permanently muted by: {args.Player.Name}. For: {args.Parameters[2]}!");
                        }
                        else
                        {
                            Mutes.Insert(player, "", args.Player.Account.ID, doip);
                            args.Player.SendSuccessMessage($"Permanently muted: {player.Name}");
                            player.SendErrorMessage($"You were permanently muted by: {args.Player.Name}.");
                        }
                    }
                    else args.Player.SendErrorMessage("Invalid time string. Valid string: <0d0h0m0s>");
                }
                else
                {
                    Mutes.Insert(player, "", args.Player.Account.ID, doip);
                    args.Player.SendSuccessMessage($"Permanently muted: {player.Name}");
                    player.SendErrorMessage($"You were permanently muted by: {args.Player.Name}");
                }
            }
            else
            {
                var account = TShock.UserAccounts.GetUserAccountByName(args.Parameters[0]);
                if (account == null)
                    args.Player.SendErrorMessage("Player or account not found!");
                else
                {
                    if (args.Parameters.Count > 1)
                    {
                        if (TShock.Utils.TryParseTime(args.Parameters[1], out int seconds))
                        {
                            duration = DateTime.UtcNow.AddSeconds(seconds);
                            if (args.Parameters.Count > 2)
                            {
                                Mutes.Insert(account, args.Parameters[2], args.Player.Account.ID, duration);
                                if (duration != DateTime.MaxValue)
                                    args.Player.SendInfoMessage($"Muted: {account.Name} for {(seconds / 60)} minutes. (Until: {duration}) for: {args.Parameters[2]}.");
                            }
                            else
                            {
                                Mutes.Insert(account, "", args.Player.Account.ID, duration);
                                args.Player.SendInfoMessage($"Muted: {account.Name} for {(seconds / 60)} minutes.");
                            }
                        }
                        else if (args.Parameters[1] == "0")
                        {
                            if (args.Parameters.Count > 2)
                            {
                                Mutes.Insert(account, args.Parameters[2], args.Player.Account.ID);
                                args.Player.SendInfoMessage($"Muted: {account.Name} permanently for: {args.Parameters[2]}.");
                            }
                            else
                            {
                                Mutes.Insert(account, "", args.Player.Account.ID);
                                args.Player.SendSuccessMessage($"Permanently muted: {account.Name}");
                            }
                        }
                        else args.Player.SendErrorMessage("Invalid time string. Valid string: <0d0h0m0s>");
                    }
                    else
                    {
                        Mutes.Insert(account, "", args.Player.Account.ID);
                        args.Player.SendSuccessMessage($"Permanently muted: {account.Name}");
                    }
                }
            }
        }

        public static void Delete(CommandArgs args)
        {
            if (args.Parameters.Count != 2)
            {
                args.Player.SendErrorMessage("Invalid syntax. Valid syntax: '/mute delete <id>'");
                return;
            }
            if (int.TryParse(args.Parameters[1], out int result))
            {
                var mute = Mutes.Get(result);
                TSPlayer muted = TSPlayer.FindByNameOrID(mute.Username)[0];
                muted.mute = false;
                Mutes.Remove(result);
                args.Player.SendSuccessMessage($"Succesfully deleted mute: {result}");
            }
            else args.Player.SendErrorMessage("Invalid ID, are you sure you specified a valid mute to delete?");
        }

        public static void Info(CommandArgs args)
        {
            if (args.Parameters.Count != 2)
            {
                args.Player.SendErrorMessage("Invalid syntax. Valid syntax: '/mute info <id>'");
                return;
            }
            if (int.TryParse(args.Parameters[1], out int result))
            {
                var mute = Mutes.Get(result);
                args.Player.SendInfoMessage($"{mute.ID} | {mute.Username}, expires on: {mute.Expiration}\nReason: {mute.Reason}\nIs IP? {((mute.IP != "") ? "true" : "false")}");
            }
            else args.Player.SendErrorMessage("Invalid ID, are you sure you specified a valid mute to read?");
        }

        public static void List(CommandArgs args)
        {
            if (args.Parameters.Count < 1 || args.Parameters.Count > 2)
            {
                args.Player.SendErrorMessage("Invalid syntax. Valid syntax: '/mute list (page)'");
                return;
            }
            var mutes = Mutes.GetAll();

            if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out int pageNumber))
                return;

            List<string> wlist = new List<string>();
            foreach (var m in mutes)
            {
                var mod = TShock.UserAccounts.GetUserAccountByID(m.ModID);
                wlist.Add($"{m.ID} | {m.Username}, by: {mod.Name} - {m.Reason}");
            }

            PaginationTools.SendPage(args.Player, pageNumber, wlist,
                new PaginationTools.Settings
                {
                    HeaderFormat = "Mutes ({0}/{1}):",
                    FooterFormat = "Type {0}mute list {{0}} for more.".SFormat(Commands.Specifier),
                    NothingToDisplayString = "There are currently no mutes."
                });
        }

        public static void Help(CommandArgs args)
        {
            if (args.Parameters.Count > 2)
            {
                args.Player.SendErrorMessage($"Invalid syntax. Valid syntax: '/mute help (page)");
                return;
            }

            if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out int pageNumber))
                return;

            var lines = new List<string>
            {
                "<target> (length) \"(reason)\" (true/false) < to include IP - Mutes a user for a specific reason.",
                "list - Lists all mutes.",
                "info <id> - Gets all info on a specified mute.",
                "delete <id> - Deletes a mute.",
            };

            PaginationTools.SendPage(args.Player, pageNumber, lines,
                    new PaginationTools.Settings
                    {
                        HeaderFormat = "Mute Sub-Commands ({0}/{1}):",
                        FooterFormat = "Type {0}mute help {{0}} for more sub-commands.".SFormat(Commands.Specifier)
                    }
                );
        }
    }
}
