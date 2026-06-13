using Celeste.Mod.Entities;
using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.practiceMod.Entities;

[CustomEntity("practiceMod/TeamAssigner")]
public class TeamAssigner : Entity {
	public TeamManager.Teams myTeam;

	public TeamAssigner(EntityData data, Vector2 offset) : base(data.Position + offset) {
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private DashCollisionResults OnDashed(Player player, Vector2 direction) {
		TeamManager teamManager = player.Get<TeamManager>();
		if (teamManager == null) {
			player.Add(teamManager = new TeamManager(TeamManager.Teams.NONE));
		} else {
			teamManager.Team = myTeam;
		}
		return DashCollisionResults.NormalCollision;
	}
}
