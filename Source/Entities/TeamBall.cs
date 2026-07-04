using Celeste.Mod.Entities;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.practiceMod.Entities;

[CustomEntity("practiceMod/TeamBall")]
public class TeamBall : SyncedHoldable {

	public TeamManager.Team MyTeam;

	public TeamBall(EntityData data, Vector2 offset) : base(data, offset) {
		MyTeam = (TeamManager.Team) data.Float("Team");
		switch (MyTeam) {
			case TeamManager.Team.RED:
				Add(sprite = GFX.SpriteBank.Create("TeamBallRed"));
				break;
			case TeamManager.Team.GREEN:
				Add(sprite = GFX.SpriteBank.Create("TeamBallGreen"));
				break;
			case TeamManager.Team.YELLOW:
				Add(sprite = GFX.SpriteBank.Create("TeamBallYellow"));
				break;
			case TeamManager.Team.BLUE:
				Add(sprite = GFX.SpriteBank.Create("TeamBallBlue"));
				break;
		}
		base.Collider = new Hitbox(8f, 10f, -4f, -10f);
		TheoCrystal.P_Impact = new ParticleType
		{
			Color = TeamManager.TeamColors[MyTeam],
			Size = 1f,
			FadeMode = ParticleType.FadeModes.Late,
			DirectionRange = 1.7453293f,
			SpeedMin = 10f,
			SpeedMax = 20f,
			SpeedMultiplier = 0.1f,
			LifeMin = 0.3f,
			LifeMax = 0.8f
		};

	}
}
