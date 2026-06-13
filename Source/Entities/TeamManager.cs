using Celeste.Mod.Entities;
using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.practiceMod.Entities;

[CustomEntity("practiceMod/TeamManager")]
public class TeamManager : Component {

	public enum Teams {
		RED,
		BLUE,
		GREEN,
		YELLOW,
		NONE
	}

	public Teams Team;

	public TeamManager(Teams startingTeam = Teams.NONE) : base(active: false, visible: false) {
		Team = startingTeam;
	}

	public static Teams GetTeam(Actor player, Teams defaultTeam = Teams.NONE) {
		TeamManager playerManager = player.Get<TeamManager>();
		if (playerManager == null) {
			return Teams.NONE;
		} else {
			return playerManager.Team;
		}
	}
}
