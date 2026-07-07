using Celeste.Mod.Entities;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.practiceMod.Entities;

[CustomEntity("practiceMod/TeamBallGoal")]
[Tracked(false)]
public class TeamBallGoal : Trigger {
	public TeamManager.Team MyTeam;

	public TeamBallGoal(EntityData data, Vector2 offset) : base(data, offset) 
	{
		MyTeam = (TeamManager.Team) data.Float("Team");
	}
	// Does nothing on its own; only exists to be referenced by TeamBall.Update()
}
