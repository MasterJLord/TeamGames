using Celeste.Mod.Entities;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.CelesteNet;

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
		P_Impact = new ParticleType
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

	public override void Added(Scene scene) 
	{
		base.Added(scene);
		if (MyTeam != TeamManager.GetTeam(Scene.Tracker.GetEntity<Player>())) 
		{
			Hold.cannotHoldTimer = Single.PositiveInfinity;
		}
		TeamManager.LocalPlayerSwitched += handleSwitch;
	}

	public override void Removed(Scene scene)
	{
		base.Removed(scene);
		TeamManager.LocalPlayerSwitched -= handleSwitch;
	}

	private void handleSwitch(uint localPlayerID, TeamManager.Team newTeam) 
	{
		if (MyTeam == newTeam) 
		{
			Hold.cannotHoldTimer = 0;
		} else if (TeamManager.GetTeam(localPlayerID) == MyTeam) {
			Hold.cannotHoldTimer = Single.PositiveInfinity;
		}
	}

	protected override void Handle(CelesteNetConnection con, DataHoldableUpdate data) 
	{
		base.Handle(con, data);
		if (MyTeam != TeamManager.GetTeam(Scene.Tracker.GetEntity<Player>())) 
		{
			Hold.cannotHoldTimer = Single.PositiveInfinity;
		}
	}

}
