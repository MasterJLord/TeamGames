using Monocle;
using System;
using Celeste.Mod.TeamGames.Entities;

namespace Celeste.Mod.TeamGames 
{
    public static class DebugCommands 
    {

        [Command("newteam", "changes one's team")]
        public static void NewTeam(string newTeam) 
	{
		int teamNum = 0;
		if (!Int32.TryParse(newTeam, out teamNum))
		{
			return;
		}
		TeamManager.SetTeam((TeamManager.Team) teamNum);
        }

        [Command("balltoggle", "toggles the lethality of TeamBalls")]
        public static void ballToggle() 
	{
		TeamBall.ToggleLethal();
        }

    }
}
