using System;
using System.Linq;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using ReportManager.Data;
using System.Collections.Generic;
using System.Timers;

namespace ReportManager
{
    [ApiVersion(2, 1)]
    public class ReportManager : TerrariaPlugin
    {
        public override string Name
            => "ReportManager";

        public override Version Version
            => new Version(2, 2, 1);

        public override string Author
            => "Rozen4334, updated by csmir + RenderBr + Maxthegreat99";

        public override string Description
            => "A plugin that manages reports, warnings & mutes";

        public ReportManager(Main game) : base(game) 
            => Order = 1;

        public class Permissions
        {
            public const string report = "reportmanager.report";

            public const string staff = "reportmanager.staff";

            public const string receiveReportNotif = "reportmanager.notifreports";

            public const string ignoreReportLimit = "reportmanager.ignorereportlimit";
        }
        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command(Permissions.report, Report, "report", "rep"));
            Commands.ChatCommands.Add(new Command(Warn, "warning", "warn"));

            Config.Settings = Settings.Read();

            ServerApi.Hooks.GamePostInitialize.Register(this, PostInit);
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreet);

            Time = DateTime.UtcNow;
            
            reportLimitReset.Interval = 60000;
            reportLimitReset.Elapsed += new ElapsedEventHandler(ResetReportsLimit);


            ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
        }

        private System.Timers.Timer reportLimitReset = new System.Timers.Timer();
        private DateTime Time;


        internal static void ResetReportsLimit(object sender, ElapsedEventArgs args)
        {
            Subcommands.Report.currentReportsPerPlayer = new Dictionary<string, int>();
        }
        private void OnUpdate(EventArgs args)
        {
            if ((DateTime.UtcNow - Time).TotalSeconds > 5)
            {
                Time = DateTime.UtcNow;
                if (TShock.Utils.GetActivePlayerCount() > 0)
                {
                    foreach (var plr in TShock.Players.Where(p => p != null && p.Active))
                    {
                        var warns = Warnings.GetAll(plr.UUID);
                        if (warns != null && warns.Any())
                        {
                            plr.Disable("You have unread warnings!");
                            plr.SendInfoMessage($"You have {warns.Count()} warning(s). Type '/warn read' to read your warnings.");
                        }

                        var mutes = Mutes.GetAll(plr.UUID);
                        if (mutes != null && mutes.Any())
                        {
                            if (!plr.mute)
                            {
                                plr.SendErrorMessage("You have been muted!");
                                plr.mute = true;
                            }
                            if (mutes.Any(x => x.Expiration <= DateTime.UtcNow && x.UUID == plr.UUID))
                            {
                                Mutes.Remove(plr);
                                plr.SendSuccessMessage("You have been unmuted!");
                                plr.mute = false;
                            }
                        }
                        else if (mutes == null && plr.mute == true)
                        {
                            plr.mute = false;
                            plr.SendSuccessMessage("You have been unmuted!");
                        }

                        var reportMutes = Reports.GetAllMutes();
                        if(reportMutes != null 
                           && reportMutes.Any(i => i.User == plr.Name && i.Expiration <= DateTime.UtcNow))
                        {
                            var reportMute = reportMutes.First(i => i.User == plr.Name);
                            Reports.RemoveMute(reportMute.ID);
                            plr.SendSuccessMessage("You can now use /report!");
                        }
                        
                    }
                }
            }
        }

        private void OnGreet(GreetPlayerEventArgs args)
        {
            TSPlayer plr = TShock.Players[args.Who];
            var mutes = Mutes.GetAll(plr.UUID);
            if (mutes.Any())
            {
                plr.mute = true;
                plr.SendInfoMessage("You have been muted automatically.");
            }
        }

        private void PostInit(EventArgs args)
        {
            var muteCmd = new Command(Permissions.staff, Mute, "mute");
            Commands.ChatCommands.RemoveAll(cmd => cmd.Names.Exists(alias => muteCmd.Names.Contains(alias)));
            Commands.ChatCommands.Add(muteCmd);

            // The report manager.
            Reports.Initialize();

            // The warning manager.
            Warnings.Initialize();

            // The mutes manager.
            Mutes.Initialize();
        }

        private void Mute(CommandArgs args)
        {
            switch(args.Parameters.FirstOrDefault())
            {
                case "help":
                case "h":
                    Subcommands.Mute.Help(args);
                    break;
                case "delete":
                case "del":
                case "d":
                    Subcommands.Mute.Delete(args);
                    break;
                case "info":
                case "i":
                    Subcommands.Mute.Info(args);
                    break;
                case "list":
                case "l":
                    Subcommands.Mute.List(args);
                    break;
                default:
                    if (args.Parameters.Count == 0)
                        Subcommands.Mute.Help(args);
                    else Subcommands.Mute.Add(args);
                    return;
            }
        }

        private void Warn(CommandArgs args)
        {
            switch(args.Parameters.FirstOrDefault())
            {
                case "help":
                case "h":
                    Subcommands.Warning.Help(args);
                    break;
                case "delete":
                case "del":
                case "d":
                    if (!args.Player.HasPermission(Permissions.staff))
                    {
                        args.Player.SendErrorMessage("Invalid subcommand.");
                        return;
                    }
                    Subcommands.Warning.Delete(args);
                    break;
                case "read":
                case "r":
                    Subcommands.Warning.Read(args);
                    break;
                case "list":
                case "l":
                    if (!args.Player.HasPermission(Permissions.staff))
                    {
                        args.Player.SendErrorMessage("Invalid subcommand.");
                        return;
                    }
                    Subcommands.Warning.List(args);
                    break;
                default:
                    if (args.Parameters.Count == 0)
                        Subcommands.Warning.Help(args);
                    else if (!args.Player.HasPermission(Permissions.staff))
                    {
                        args.Player.SendErrorMessage("Invalid subcommand.");
                        return;
                    }
                    else Subcommands.Warning.Add(args);
                    break;
            }
        }

        private void Report(CommandArgs args)
        {
            if(Reports.GetAllMutes() != null && Reports.GetAllMutes().Any(i => i.User == args.Player.Name))
            {
                args.Player.SendErrorMessage("You have been prohibited from using /report");
                return;
            }

            switch(args.Parameters.FirstOrDefault())
            {
                case "help":
                case "h":
                    Subcommands.Report.Help(args);
                    break;
                case "delete":
                case "del":
                case "d":
                    if (!args.Player.HasPermission(Permissions.staff))
                    {
                        args.Player.SendErrorMessage("Invalid subcommand.");
                        return;
                    }
                    Subcommands.Report.Delete(args);
                    break;
                case "teleport":
                case "tp":
                    if (!args.Player.HasPermission(Permissions.staff))
                    {
                        args.Player.SendErrorMessage("Invalid subcommand.");
                        return;
                    }
                    Subcommands.Report.Teleport(args);
                    break;
                case "list":
                case "l":
                    if (!args.Player.HasPermission(Permissions.staff))
                    {
                        args.Player.SendErrorMessage("Invalid subcommand.");
                        return;
                    }
                    Subcommands.Report.List(args);
                    break;
                case "info":
                case "i":
                    if (!args.Player.HasPermission(Permissions.staff))
                    {
                        args.Player.SendErrorMessage("Invalid subcommand.");
                        return;
                    }
                    Subcommands.Report.Info(args);
                    break;
                case "mute":
                case "m":
                    if (!args.Player.HasPermission(Permissions.staff))
                    {
                        args.Player.SendErrorMessage("Invalid subcommand.");
                        return;
                    }

                    if (args.Parameters.Count < 2)
                    {
                        Subcommands.Report.Help(args);
                        return;
                    }

                    switch (args.Parameters[1])
                    {
                        case "del":
                        case "d":
                            Subcommands.Report.MuteDel(args);
                            break;
                        case "list":
                        case "l":
                            Subcommands.Report.ListMute(args);
                            break;
                        default:
                            Subcommands.Report.MuteAdd(args);
                            break;
                    }
                    break;
                default:
                    if (args.Parameters.Count == 0)
                        Subcommands.Report.Help(args);
                    else Subcommands.Report.Add(args);
                    break;
            }
        }
    }
}
