using Celeste.Mod.Entities;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.Client;

namespace Celeste.Mod.TeamGames.Entities;

[CustomEntity("TeamGames/TeamBall")]
public class TeamBall : SyncedHoldable 
{

	private static bool isLethal = false;

	public TeamManager.Team MyTeam;
	private const float DROP_TIME = 6f - STAY_DEAD_TIME;
	private float droppedTime = 0;
	private bool doPhysics = false;
	private float spawnSafety;

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
		Add(new PlayerCollider(onPlayer));
		spawnSafety = 3f;
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

	public override void Handle(CelesteNetConnection con, DataHoldableUpdate data) 
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
		// Same as in Update() but without sending an update, in order to prevent a death loop
		if (checkForGoal())
		{
			TeamManager.ScorePoint(Scene, MyTeam, true);
			Position = SpawnPosition;
		}
	}

	private void drop() 
	{
		droppedTime = DROP_TIME;
	}

	protected override void updateGivenTime(float time, bool isCatchup = false) 
	{
		if (!isCatchup && droppedTime >= 0)
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

	private bool checkForGoal()
	{
		foreach (TeamBallGoal goal in Scene.Tracker.GetEntities<TeamBallGoal>())
		{
			if (goal.MyTeam != MyTeam)
			{
				continue;
			}
			if (CollideCheck(goal))
			{
				return true;
			}
		}
		return false;
	}

	public override void Update()
	{
		base.Update();
		if (checkForGoal())
		{
			TeamManager.ScorePoint(Scene, MyTeam, true);
			SendUpdate();
			Position = SpawnPosition;
		}
		spawnSafety -= Engine.DeltaTime;
	}

	private void onPlayer(Player player)
	{
		if (!isLethal || spawnSafety > 0)
		{
			return;
		}
		if (TeamManager.GetTeam(player, MyTeam) != MyTeam)
		{
			player.Die((player.Position - Position).SafeNormalize());
		}
	}

	public static void ToggleLethal()
	{
		isLethal = !isLethal;
		if (ClientContext == null)
		{
			return;
		}
		DataMatchInfo data = new DataMatchInfo {
			TeamBallsAreDeadly = isLethal
		};
		ClientContext.Client.Send(data);
	}

	new public static void GetClientContext(CelesteNetClientContext context)
	{
		context.Client.Data.RegisterHandler<DataMatchInfo>(Handle);
	}

	public static void Handle(CelesteNetConnection con, DataMatchInfo data)
	{
		isLethal = data.TeamBallsAreDeadly;
	}
}
