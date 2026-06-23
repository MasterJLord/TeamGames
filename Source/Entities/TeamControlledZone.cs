using Celeste.Mod.Entities;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.practiceMod.Entities;

[CustomEntity("practiceMod/TeamControlledZone")]
public class TeamControlledZone : Trigger {
	public TeamManager.Team MyTeam;
	private List<Player> myPlayersWithin = new List<Player>();
	private List<Player> enemyPlayersWithin = new List<Player>();
	
	[MethodImpl(MethodImplOptions.NoInlining)]
	public TeamControlledZone(EntityData data, Vector2 offset) 
		: base(data, offset) {
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void OnEnter(Player player) {
		if (TeamManager.GetTeam(player) == MyTeam) {
			myPlayersWithin.Add(player);
		} else {
			enemyPlayersWithin.Add(player);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void OnLeave(Player player) {
		myPlayersWithin.Remove(player);
		enemyPlayersWithin.Remove(player);
	}

	public void OnContact(Player player1, Player player2) {
		if (myPlayersWithin.Contains(player1)) {
			if (enemyPlayersWithin.Contains(player2)) {
				player2.Die(player2.Position - player1.Position);
			}
		} else if (enemyPlayersWithin.Contains(player1)) {
			if (myPlayersWithin.Contains(player2)) {
				player1.Die(player1.Position - player2.Position);
			}
		}
	}
}

