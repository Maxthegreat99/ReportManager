using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;

namespace ReportManager
{
    static class Extensions
    {
        public static bool ParsePlayer(TSPlayer player, string input, out TSPlayer result, bool dosenderror = true)
        {
            var players = TSPlayer.FindByNameOrID(input);
            if (players.Count == 0)
            {
                if (dosenderror)
                    player.SendErrorMessage("Invalid player!");
                result = null; return false;
            }
            else if (players.Count > 1)
            {
                if (dosenderror)
                    player.SendMultipleMatchError(players.Select(p => p.Name));
                result = null; return false;
            }
            else
            {
                result = players.FirstOrDefault(); return true;
            }
        }
    }
}
