using Celeste.Mod.Entities;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.CelesteNet.Client.Entities;

namespace Celeste.Mod.TeamGames.Entities;

[CustomEntity("TeamGames/TeamControlledZone")]
public class TeamControlledZone : Trigger {
	public TeamManager.Team MyTeam;

	private bool isSafe;
	
	[MethodImpl(MethodImplOptions.NoInlining)]
	public TeamControlledZone(EntityData data, Vector2 offset) 
		: base(data, offset) {
		MyTeam = (TeamManager.Team) data.Float("Team");
		Visible = true;
	}

	// Keeps the boolean representing whether the local player can be killed in this zone and the list of remote players who can kill them up to date
	
	public override void Awake(Scene scene) {
		base.Awake(scene);
		isSafe = TeamManager.GetTeam(Scene.Tracker.GetEntity<Player>()) == MyTeam;
		TeamManager.LocalPlayerSwitched += localPlayerSwitched;
	}
	public override void Removed(Scene scene) {
		base.Removed(scene);
		TeamManager.LocalPlayerSwitched -= localPlayerSwitched;
	}

	private void localPlayerSwitched(uint playerID, TeamManager.Team newTeam) {
		isSafe = newTeam == MyTeam;
	}
	
	// Handles collisions between local player and killing player

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void OnStay(Player player) {
		if (isSafe) {
			return;
		}
		foreach (Ghost ghost in Scene.Tracker.GetEntities<Ghost>()) {
			if (TeamManager.GetTeam(ghost) != MyTeam) {
				continue;
			}
			if (player.CollideCheck(ghost)) {
				OnContact(player, ghost);
				break;
			}
		}
	}

	public void OnContact(Player dier, Ghost killer) {
		if (TeamManager.GetTeam(dier) == MyTeam) {
			return;
		}
		if (TeamManager.GetTeam(killer) != MyTeam) {
			return;
		}
		dier.Die(dier.Position - killer.Position);
	}

	// Renders the zone
	
	public override void Render() {
		Draw.Rect(Collider, TeamManager.TeamColors[MyTeam] * 0.075f);
	}
}

