using Celeste.Mod.Entities;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.CelesteNet;

namespace Celeste.Mod.practiceMod.Entities;

[CustomEntity("practiceMod/TeamBall")]
public class TeamBall : SyncedHoldable 
{

	public TeamManager.Team MyTeam;
	private const float DROP_TIME = 6f - STAY_DEAD_TIME;
	private float droppedTime = 0;

	public TeamBall(EntityData data, Vector2 offset) : base(data, offset) 
	{
		MyTeam = (TeamManager.Team) data.Float("Team");
		switch (MyTeam) 
		{
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

	public override void Awake(Scene scene) 
	{
		base.Awake(scene);
		if (MyTeam != TeamManager.GetTeam((uint) localPlayerID)) 
		{
			Logger.Log(LogLevel.Debug, "practiceMod/TeamBall", "Initiating lock");
			Hold.cannotHoldTimer = Single.PositiveInfinity;
		}
		Logger.Log(LogLevel.Debug, "practiceMod/TeamBall", "My Team is " + MyTeam + " and player's team is " + TeamManager.GetTeam(Scene.Tracker.GetEntity<Player>()));
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
			Logger.Log(LogLevel.Debug, "practiceMod/TeamBall", "Unlocking due to team change");

		} else if (TeamManager.GetTeam(localPlayerID) == MyTeam) {
			Hold.cannotHoldTimer = Single.PositiveInfinity;
			Logger.Log(LogLevel.Debug, "practiceMod/TeamBall", "Relocking");
		}
	}
	
	protected override void OnRelease(Vector2 force)
	{
		base.OnRelease(force);
		drop();
	}

	protected override void OnPickup()
	{
		base.OnPickup();
		droppedTime = -1;
	}
	
	protected override void Respawn()
	{
		base.Respawn();
		if (TeamManager.GetTeam((uint) localPlayerID) != MyTeam)
		{
			Hold.cannotHoldTimer = Single.PositiveInfinity;
			Logger.Log(LogLevel.Debug, "practiceMod/TeamBall", "Relocking due to respawn");
		}
	}

	protected override void Handle(CelesteNetConnection con, DataHoldableUpdate data) 
	{
		if (data.IsHeld)
		{
			droppedTime = -1;
		} else if (IsHeldRemote) {
			drop();
		}
		base.Handle(con, data);
		if (MyTeam != TeamManager.GetTeam(Scene.Tracker.GetEntity<Player>())) 
		{
			Hold.cannotHoldTimer = Single.PositiveInfinity;
			Logger.Log(LogLevel.Debug, "practiceMod/TeamBall", "Relocking");
		}
	}

	private void drop() 
	{
		droppedTime = DROP_TIME;
	}

	protected override void updateGivenTime(float time, bool isCatchup = false) 
	{
		if (droppedTime >= 0)
		{
			droppedTime -= time;
			if (droppedTime < 0) 
			{
				Die();
				updateGivenTime(-1 * droppedTime);
				return;
			}
		}
		base.updateGivenTime(time);
	}

	public override void Update()
	{
		base.Update();
		foreach (TeamBallGoal goal in Scene.Tracker.GetEntities<TeamBallGoal>())
		{
			if (goal.MyTeam != MyTeam)
			{
				continue;
			}
			if (CollideCheck(goal))
			{
				TeamManager.ScorePoint(Scene, MyTeam);
				SendUpdate();
				Die();
			}
		}
	}
}
