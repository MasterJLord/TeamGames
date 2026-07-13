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
	private bool doPhysics = false;

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
		if (MyTeam != TeamManager.GetTeam(localPlayerID)) 
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

	public override bool IsRiding(Solid solid)
	{
		if (!doPhysics)
		{
			return false;
		}
		return base.IsRiding(solid);
	}

	private void handleSwitch(uint localPlayerID, TeamManager.Team newTeam) 
	{
		if (MyTeam == newTeam && !IsHeldRemote) 
		{
			Hold.cannotHoldTimer = 0;

		} else if (TeamManager.GetTeam(localPlayerID) == MyTeam) {
			Hold.cannotHoldTimer = Single.PositiveInfinity;
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
		doPhysics = true;
	}
	
	protected override void Respawn()
	{
		base.Respawn();
		if (TeamManager.GetTeam(localPlayerID) != MyTeam)
		{
			Hold.cannotHoldTimer = Single.PositiveInfinity;
		}
		doPhysics = false;
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
		Vector2 prePosition = Position;
		base.updateGivenTime(time);
		if (!doPhysics)
		{
			Position = prePosition;
		}
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
