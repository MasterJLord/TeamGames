using Celeste.Mod.Entities;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.CelesteNet.Client.Entities;

namespace Celeste.Mod.practiceMod.Entities;

[CustomEntity("practiceMod/TeamControlledZone")]
public class TeamControlledZone : Trigger {
	public TeamManager.Team MyTeam;

	private bool isSafe;
	private List<Ghost> myRemotePlayers = new();
	
	[MethodImpl(MethodImplOptions.NoInlining)]
	public TeamControlledZone(EntityData data, Vector2 offset) 
		: base(data, offset) {
		MyTeam = (TeamManager.Team) data.Float("Team");
		Logger.Log(LogLevel.Debug, "practiceMod/TeamManager", "Zone loaded team " + data.Float("Team") + " (" + MyTeam + ")");
	}

	// Keeps the boolean representing whether the local player can be killed in this zone and the list of remote players who can kill them up to date
	
	public override void Awake(Scene scene) {
		base.Awake(scene);
		isSafe = TeamManager.GetTeam(Scene.Tracker.GetEntity<Player>()) == MyTeam;
		foreach (Ghost ghost in Scene.Tracker.GetEntities<Ghost>()) {
			if (TeamManager.GetTeam(ghost) == MyTeam) {
				myRemotePlayers.Add(ghost);
			}
		}
		TeamManager.LocalPlayerSwitched += localPlayerSwitched;
		TeamManager.RemotePlayerSwitched += remotePlayerSwitched;

	}
	public override void Removed(Scene scene) {
		base.Removed(scene);
		TeamManager.LocalPlayerSwitched -= localPlayerSwitched;
		TeamManager.RemotePlayerSwitched -= remotePlayerSwitched;
	}

	private void localPlayerSwitched(uint playerID, TeamManager.Team newTeam) {
		isSafe = newTeam == MyTeam;
		Logger.Log(LogLevel.Debug, "practiceMod/TeamControlledZone", "toggled safe to " + isSafe);
	}
	
	private void remotePlayerSwitched(uint playerID, TeamManager.Team newTeam) {
		foreach (Ghost ghost in Scene.Tracker.GetEntities<Ghost>()) {
			if (ghost.PlayerInfo?.ID == playerID) {
				if (newTeam == MyTeam) {
					myRemotePlayers.Add(ghost);
				} else {
					myRemotePlayers.Remove(ghost);
				}
				break;
			}
		}
		Logger.Log(LogLevel.Debug, "practiceMod/TeamControlledZone", "There are now " + myRemotePlayers.Count + " in my list");
		// TODO: handle remote players respawning and their ghosts being recreated
	}

	// Handles collisions between local player and killing player

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void OnStay(Player player) {
		if (isSafe) {
			return;
		}
		foreach (Ghost ghost in myRemotePlayers) {
			if (player.CollideCheck(ghost)) {
				Logger.Log(LogLevel.Debug, "practiceMod/TeamControlledZone", "Collision detected");
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
		Draw.Rect(Collider, TeamManager.TeamColors[MyTeam] * 0.15f);
	}
}

