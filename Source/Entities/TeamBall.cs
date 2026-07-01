using Celeste.Mod.Entities;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.practiceMod.Entities;

[CustomEntity("practiceMod/TeamBall")]
public class TeamBall : Actor {

	public TeamManager.Team MyTeam;
	public SyncedHoldable SyncedHoldableComponent;
	public Vector2 Speed;

	public TeamBall(EntityData data, Vector2 offset) : base(data.Position + offset) {
		MyTeam = (TeamManager.Team) data.Float("Team");
		Add(SyncedHoldableComponent = new());
	}

	public void Move(float deltaTime) {
	}
}
