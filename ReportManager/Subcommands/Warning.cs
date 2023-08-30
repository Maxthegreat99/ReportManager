using System.Collections.Generic;
using System.Linq;
using TShockAPI;
using ReportManager.Data;

namespace ReportManager.Subcommands
{
    class Warning
    {
        public static void Add(CommandArgs args)
        {
            if (args.Parameters.Count != 2)
            {
                args.Player.SendErrorMessage("Invalid syntax. Valid syntax: '/warn <user> <reason>'");
                return;
            }
            var acc = TShock.UserAccounts.GetUserAccountByName(args.Parameters[0]);
            if (acc == null)
            {
                args.Player.SendErrorMessage("User not found! NOTE: If the target has not registered they cannot be warned as they don't exist in the database.");
                return;
            }
            
            var mod = args.Player.RealPlayer ? TShock.UserAccounts.GetUserAccountByName(args.Player.Name) : TSPlayer.Server.Account;
            Warnings.Insert(acc.UUID, acc.Name, args.Parameters[1], mod.ID);
            args.Player.SendSuccessMessage($"Succesfully warned '{acc.Name}' for: {args.Parameters[1]}");
        }

        public static void Read(CommandArgs args)
        {
            var acc = args.Player.Account;
            if (acc == null)
            {
                args.Player.SendErrorMessage("Unable to get your account! (Are you logged in?)");
                return;
            }
            var warnings = Warnings.GetAll(acc.UUID);
            if (warnings.FirstOrDefault() == null) {
                args.Player.SendErrorMessage("There are no warnings to read!");
                return;
            }

            var single = warnings.FirstOrDefault();
            var mod = TShock.UserAccounts.GetUserAccountByID(single.ModID);

            args.Player.SendSuccessMessage($"Reading warning made by: {((mod != null) ? mod.Name : "Unknown")}.");
            args.Player.SendInfoMessage($"{single.ID} | {single.Reason}");

            Warnings.Remove(single.ID);
        }

        public static void Delete(CommandArgs args)
        {
            if (args.Parameters.Count != 2)
            {
                args.Player.SendErrorMessage("Invalid syntax. Valid syntax: '/warn delete <id>'");
                return;
            }
            if (int.TryParse(args.Parameters[1], out int result))
            {
                Warnings.Remove(result);
                args.Player.SendSuccessMessage($"Succesfully deleted warning: {result}");
            }
            else args.Player.SendErrorMessage("Invalid ID, are you sure you specified a valid warning to delete?");
        }

        public static void List(CommandArgs args)
        {
            if (args.Parameters.Count > 2)
            {
                args.Player.SendErrorMessage("Invalid syntax. Valid syntax: '/warn list (page)'");
                return;
            }
            var warnings = Warnings.GetAll();

            if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out int pageNumber))
                return;

            List<string> wlist = new List<string>();
            foreach (var r in warnings)
            {
                var acc = TShock.UserAccounts.GetUserAccountByName(r.Username);
                var mod = TShock.UserAccounts.GetUserAccountByID(r.ModID);
                
                if (acc == null)
                {
                    Warnings.Remove(r.ID);
                    continue;
                }
                wlist.Add($"{r.ID} | {acc.Name}, by: {((mod != null) ? mod.Name : "Unknown")} - {r.Reason}");
            }

            PaginationTools.SendPage(args.Player, pageNumber, wlist,
                new PaginationTools.Settings
                {
                    HeaderFormat = "Warnings ({0}/{1}):",
                    FooterFormat = "Type {0}warn list {{0}} for more.".SFormat(Commands.Specifier),
                    NothingToDisplayString = "There are currently no warnings."
                });
        }

        public static void Help(CommandArgs args)
        {
            if (args.Parameters.Count > 2)
            {
                args.Player.SendErrorMessage($"Invalid syntax. Valid syntax: '/warn help (page)");
                return;
            }

            if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out int pageNumber))
                return;

            var lines = new List<string>
            {
                "read - Reads all your warnings in order.",
            };
            if (args.Player.HasPermission(ReportManager.Permissions.staff))
                lines.AddRange(new List<string>
                {
                    "<target> \"<reason>\" - Warns a user for specified reason.",
                    "list - Lists all warnings.",
                    "delete <id> - Deletes a warning.",
                });

            PaginationTools.SendPage(args.Player, pageNumber, lines,
                    new PaginationTools.Settings
                    {
                        HeaderFormat = "Warning Sub-Commands ({0}/{1}):",
                        FooterFormat = "Type {0}warn help {{0}} for more sub-commands.".SFormat(Commands.Specifier)
                    }
                );
        }
    }
}
