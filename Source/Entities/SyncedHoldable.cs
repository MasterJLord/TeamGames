using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using Celeste.Mod.CelesteNet.Client;
using Celeste.Mod.CelesteNet.Client.Entities;

namespace Celeste.Mod.practiceMod.Entities;

public abstract class SyncedHoldable : Actor
{
	protected static CelesteNetClientContext clientContext;
	protected static uint? localPlayerID 
	{
		get {
			return clientContext?.Client.PlayerInfo.ID;
		}
	}
	protected static Dictionary<int, uint> owners = new();

	protected const float UPDATE_INTERVAL = 0.05f;
	protected const float STAY_DEAD_TIME = 0.2f;

	public bool IsHeldRemote;
	public Vector2 Speed;
	public Holdable Hold;
	public Vector2 SpawnPosition;
	protected Sprite sprite;
	protected Collision onCollideH;
	protected Collision onCollideV;
	protected float deadTimer;
	protected Level level;
	protected float noGravityTimer;
	protected Vector2 prevLiftSpeed;
	protected float hardVerticalHitSoundCooldown;
	protected ParticleType P_Impact;

	[MethodImpl(MethodImplOptions.NoInlining)]
	public SyncedHoldable(EntityData data, Vector2 offset)
		: base(data.Position + offset)
	{
		onCollideH = OnCollideH;
		onCollideV = OnCollideV;
		SpawnPosition = Position;
		base.Collider = new Hitbox(8f, 10f, -4f, -10f);

		Add(Hold = new Holdable(0.1f));
		Hold.PickupCollider = new Hitbox(16f, 22f, -8f, -16f);
		Hold.SlowFall = false;
		Hold.SlowRun = true;
		Hold.OnPickup = OnPickup;
		Hold.OnRelease = OnRelease;
		Hold.DangerousCheck = Dangerous;
		Hold.OnHitSeeker = HitSeeker;
		Hold.OnSwat = Swat;
		Hold.OnHitSpring = HitSpring;
		Hold.OnHitSpinner = HitSpinner;
		Hold.SpeedGetter = () => Speed;
		Hold.SpeedSetter = delegate(Vector2 speed)
		{
			Speed = speed;
		};
		P_Impact = new ParticleType
		{
			Color = Calc.HexToColor("cbdbfc"),
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

	public virtual void Swat(HoldableCollider hc, int dir) {
	}

	public virtual void HitSeeker(Seeker seeker) {
	}

	public override void Added(Scene scene) 
	{
		base.Added(scene);
		level = SceneAs<Level>();
		if (clientContext == null) 
		{
			return;
		}
		if (!owners.ContainsKey(base.SourceId.ID)) 
		{
			DataPlayerInfo[] playerList = clientContext.Client.Data.GetRefs<DataPlayerInfo>();
			uint minID = playerList[0].ID;
			foreach (DataPlayerInfo player in playerList) 
			{
				if (player.ID < minID) 
				{
					minID = player.ID;
				}
			}
			owners[base.SourceId.ID] = minID;
		}

		DataContext data = clientContext.Client.Data;
		// data.RegisterHandlersIn(this);
		data.RegisterHandler<DataHoldableUpdate>(Handle);
		data.RegisterHandler<DataSession>(Handle);
	}

	public override void Removed(Scene scene) 
	{
		DataContext data = clientContext.Client.Data;
		data.UnregisterHandlersIn(this);

		// Relinquishes control of the ball before it is reset, so that it is not reset across all players' games (assuming there is another player eligible to gain control of it)

		if (owners[base.SourceId.ID] != localPlayerID)
		{
			return;
		}
		DataPlayerInfo[] playerList = clientContext.Client.Data.GetRefs<DataPlayerInfo>();
		uint minOtherID = playerList[0].ID;
		foreach (DataPlayerInfo player in playerList) 
		{
			if (player.ID < minOtherID || minOtherID == localPlayerID) 
			{
				minOtherID = player.ID;
			}
		}
		owners[base.SourceId.ID] = minOtherID;
		if (minOtherID == localPlayerID) {
			return;
		}
		DataHoldableUpdate dataPacket = new DataHoldableUpdate {
			SenderID = minOtherID,
			EntityID = base.SourceId.ID,
			SentTime = Scene.TimeActive,
			IsHeld = false,
			Position = Position,
			Velocity = Speed
		};
		clientContext.Client.Send(dataPacket);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void Update()
	{
		base.Update();
		updateGivenTime(Engine.DeltaTime);
		if (base.Scene.OnInterval(0.1f)) 
		{
			if (Hold.IsHeld) 
			{
				owners[base.SourceId.ID] = (uint) localPlayerID;
			}
			SendUpdate();
		}
	}

	protected virtual void updateGivenTime(float time, bool isCatchup = false) 
	{
		if (deadTimer > 0f)
		{
			deadTimer -= time;
			if (deadTimer <= 0) 
			{
				Respawn();
				updateGivenTime (deadTimer * -1);
			}
			return;
		}
		if (!isCatchup) 
		{
			hardVerticalHitSoundCooldown -= time;
		}
		base.Depth = 100;
		if (IsHeldRemote) 
		{
			foreach (Ghost ghost in Scene.Tracker.GetEntities<Ghost>())
			{
				uint? id = ghost.PlayerInfo?.ID;
				if (id != owners[base.SourceId.ID]) 
				{
					continue;
				}
				Player player = Scene.Tracker.GetEntity<Player>();
				if (player != null && ghost != null)
				{
					Position = (Vector2) (ghost?.Position + player?.carryOffset + Vector2.UnitY * ghost?.Sprite.CarryYOffset);
				}
				Hold.CheckAgainstColliders();
				return;
			}
		}
		if (Hold.IsHeld)
		{
			prevLiftSpeed = Vector2.Zero;
		} else {
			if (OnGround()) 
			{
				float target = ((!OnGround(Position + Vector2.UnitX * 3f)) ? 20f : (OnGround(Position - Vector2.UnitX * 3f) ? 0f : (-20f)));
				Speed.X = Calc.Approach(Speed.X, target, 800f * time);
				Vector2 liftSpeed = base.LiftSpeed;
				if (liftSpeed == Vector2.Zero && prevLiftSpeed != Vector2.Zero) {
					Speed = prevLiftSpeed;
					prevLiftSpeed = Vector2.Zero;
					Speed.Y = Math.Min(Speed.Y * 0.6f, 0f);
					if (Speed.X != 0f && Speed.Y == 0f)
					{
						Speed.Y = -60f;
					}
					if (Speed.Y < 0f)
					{
						noGravityTimer = 0.15f;
					}
				} else {
					prevLiftSpeed = liftSpeed;
					if (liftSpeed.Y < 0f && Speed.Y < 0f)
					{
						Speed.Y = 0f;
					}
				}
			} else if (Hold.ShouldHaveGravity) {
				float num = 800f;
				if (Math.Abs(Speed.Y) <= 30f)
				{
					num *= 0.5f;
				}
				float num2 = 350f;
				if (Speed.Y < 0f)
				{
					num2 *= 0.5f;
				}
				Speed.X = Calc.Approach(Speed.X, 0f, num2 * time);
				if (noGravityTimer > 0f)
				{
					noGravityTimer -= time;
				} else {
					Speed.Y = Calc.Approach(Speed.Y, 200f, num * time);
				}
			}
			MoveH(Speed.X * time, onCollideH);
			MoveV(Speed.Y * time, onCollideV);
			if (base.Center.X > (float)level.Bounds.Right)
			{
				MoveH(32f * time);
				if (base.Left - 8f > (float)level.Bounds.Right)
				{
					RemoveSelf();
				}
			} else if (base.Left < (float)level.Bounds.Left) {
				Die();
			} else if (base.Top < (float)(level.Bounds.Top - 4)) {
				Die();
			} else if (base.Top > (float)level.Bounds.Bottom) {
				Die();
			}
		}
		Hold.CheckAgainstColliders();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public void ExplodeLaunch(Vector2 from)
	{
		if (!Hold.IsHeld)
		{
			Speed = (base.Center - from).SafeNormalize(120f);
			SlashFx.Burst(base.Center, Monocle.Calc.Angle(Speed));
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public bool Dangerous(HoldableCollider holdableCollider)
	{
		return false;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public void HitSpinner(Entity spinner)
	{
		if (!Hold.IsHeld && !IsHeldRemote && Speed.Length() < 0.01f && base.LiftSpeed.Length() < 0.01f && OnGround())
		{
			int num = Math.Sign(base.X - spinner.X);
			if (num == 0)
			{
				num = 1;
			}
			Speed.X = (float)num * 120f;
			Speed.Y = -30f;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public bool HitSpring(Spring spring)
	{
		if (!Hold.IsHeld)
		{
			if (spring.Orientation == Spring.Orientations.Floor && Speed.Y >= 0f)
			{
				Speed.X *= 0.5f;
				Speed.Y = -160f;
				noGravityTimer = 0.15f;
				return true;
			}
			if (spring.Orientation == Spring.Orientations.WallLeft && Speed.X <= 0f)
			{
				MoveTowardsY(spring.CenterY + 5f, 4f);
				Speed.X = 220f;
				Speed.Y = -80f;
				noGravityTimer = 0.1f;
				return true;
			}
			if (spring.Orientation == Spring.Orientations.WallRight && Speed.X >= 0f)
			{
				MoveTowardsY(spring.CenterY + 5f, 4f);
				Speed.X = -220f;
				Speed.Y = -80f;
				noGravityTimer = 0.1f;
				return true;
			}
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	protected virtual void OnCollideH(CollisionData data)
	{
		if (data.Hit is DashSwitch)
		{
			(data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitX * Math.Sign(Speed.X));
		}
		Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_side", Position);
		if (Math.Abs(Speed.X) > 100f)
		{
			ImpactParticles(data.Direction);
		}
		Speed.X *= -0.4f;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	protected virtual void OnCollideV(CollisionData data)
	{
		if (data.Hit is DashSwitch)
		{
			(data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitY * Math.Sign(Speed.Y));
		}
		if (Speed.Y > 0f)
		{
			if (hardVerticalHitSoundCooldown <= 0f)
			{
				Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_ground", Position, "crystal_velocity", Calc.ClampedMap(Speed.Y, 0f, 200f));
				hardVerticalHitSoundCooldown = 0.5f;
			}
			else
			{
				Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_ground", Position, "crystal_velocity", 0f);
			}
		}
		if (Speed.Y > 160f)
		{
			ImpactParticles(data.Direction);
		}
		if (Speed.Y > 140f && !(data.Hit is SwapBlock) && !(data.Hit is DashSwitch))
		{
			Speed.Y *= -0.6f;
		}
		else
		{
			Speed.Y = 0f;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void ImpactParticles(Vector2 dir)
	{
		float direction;
		Vector2 position;
		Vector2 positionRange;
		if (dir.X > 0f)
		{
			direction = (float)Math.PI;
			position = new Vector2(base.Right, base.Y - 4f);
			positionRange = Vector2.UnitY * 6f;
		} else if (dir.X < 0f) {
			direction = 0f;
			position = new Vector2(base.Left, base.Y - 4f);
			positionRange = Vector2.UnitY * 6f;
		} else if (dir.Y > 0f) {
			direction = -(float)Math.PI / 2f;
			position = new Vector2(base.X, base.Bottom);
			positionRange = Vector2.UnitX * 6f;
		} else {
			direction = (float)Math.PI / 2f;
			position = new Vector2(base.X, base.Top);
			positionRange = Vector2.UnitX * 6f;
		}
		level.Particles.Emit(P_Impact, 12, position, positionRange, direction);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override bool IsRiding(Solid solid)
	{
		if (Speed.Y == 0f)
		{
			return base.IsRiding(solid);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void OnSquish(CollisionData data)
	{
		if (!TrySquishWiggle(data, 3, 3) && !SaveData.Instance.Assists.Invincible)
		{
			Die();
		}
	}

	protected virtual void Die() 
	{
		sprite.Visible = false;
		deadTimer = STAY_DEAD_TIME; 
		Hold.cannotHoldTimer = Single.PositiveInfinity;
	}

	protected virtual void Respawn() 
	{
		sprite.Visible = true;
		Position = SpawnPosition;
		Hold.cannotHoldTimer = 0;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	protected virtual void OnPickup()
	{
		Speed = Vector2.Zero;
		AddTag(Tags.Persistent);
		owners[base.SourceId.ID] = (uint) localPlayerID;
		SendUpdate();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	protected virtual void OnRelease(Vector2 force)
	{
		RemoveTag(Tags.Persistent);
		if (force.X != 0f && force.Y == 0f)
		{
			force.Y = -0.4f;
		}
		Speed = force * 200f;
		if (Speed != Vector2.Zero)
		{
			noGravityTimer = 0.1f;
		}
		owners[base.SourceId.ID] = (uint) localPlayerID;
		SendUpdate();
	}

	private void SendUpdate() {
		if (clientContext == null)
		{
			return;
		}
		if (owners[base.SourceId.ID] != localPlayerID) 
		{

			return;
		}
		DataHoldableUpdate data = new DataHoldableUpdate {
			SenderID = (uint) localPlayerID,
			EntityID = base.SourceId.ID,
			SentTime = Scene.TimeActive,
			IsHeld = Hold.IsHeld,
			Position = Position,
			Velocity = Speed
		};
		clientContext.Client.Send(data);
	}

	// Function used to get access to the client context
	public static void GetClientContext(CelesteNetClientContext context) {
		clientContext = context;
		Logger.Log(LogLevel.Debug, "practiceMod/SyncedHoldable", "Got client context");
	}

	protected virtual void Handle(CelesteNetConnection con, DataSession session) {
		Console.WriteLine(Scene.TimeActive);
		Console.WriteLine(session.InSession);
		Console.WriteLine(session.Time);
	}

	protected virtual void Handle(CelesteNetConnection con, DataHoldableUpdate data) {
		if (base.SourceId.ID != data.EntityID) {
			return;
		}

		owners[data.EntityID] = data.SenderID;
		// Prevents the holdable from being held by two players at once
		if (data.IsHeld) 
		{
			Hold.cannotHoldTimer = Single.PositiveInfinity;
		} else if (IsHeldRemote) {
			Hold.cannotHoldTimer = 0.1f;
		}
		IsHeldRemote = data.IsHeld;
		Position = data.Position;
		Speed = data.Velocity;
		// updateGivenTime(Scene.TimeActive - data.SentTime, true);
	}
}
