using Celeste.Mod.Entities;
using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.practiceMod.Entities;

[CustomEntity("practiceMod/SampleEntity")]
public class SampleEntity : Actor {
	private enum States
	{
		Idle,
		Hit,
		Gone
	}
	private VertexLight light;
	private BloomPoint bloom;
	private Vector2 startPosition;
	private Vector2 hitSpeed;
	private States state;
	private Sprite sprite;
	private SimpleCurve returnCurve;
	private float respawnTimer = 0;
	private float goneTimer = 2.5f;
	public static ParticleType P_Ambience;
	public static ParticleType P_Launch;
	private float playerAliveFade;
	private Collision onCollideV;
	private Collision onCollideH;
	private Collider moveCollider;
	private Collider normalCollider;
	private static float bounciness = 0.6f;

	public SampleEntity(EntityData data, Vector2 offset) : base(data.Position + offset) {
		moveCollider = new Hitbox(12f, 10f, -7f, 7f);
		base.Collider = (normalCollider = new Circle(12f));
		Add(new PlayerCollider(OnPlayer));
		Add(sprite = GFX.SpriteBank.Create("bumper"));
		Add(light = new VertexLight(Color.Teal, 1f, 16, 32));
		Add(bloom = new BloomPoint(0.5f, 16f));
		startPosition = Position;
		onCollideV = OnCollideV;
		onCollideH = OnCollideH;
		if (P_Ambience == null) {
			P_Ambience = new ParticleType
			{
				Source = GFX.Game["particles/rect"],
				Color = Calc.HexToColor("47b5cc"),
				Color2 = Calc.HexToColor("c4f4ff"),
				ColorMode = ParticleType.ColorModes.Blink,
				FadeMode = ParticleType.FadeModes.InAndOut,
				Size = 0.5f,
				SizeRange = 0.2f,
				RotationMode = ParticleType.RotationModes.SameAsDirection,
				LifeMin = 0.2f,
				LifeMax = 0.4f,
				SpeedMin = 10f,
				SpeedMax = 20f,
				DirectionRange = (float)Math.PI / 6f
			};
			P_Launch = new ParticleType
			{
				Source = GFX.Game["particles/rect"],
				Color = Calc.HexToColor("47b5cc"),
				Color2 = Calc.HexToColor("c4f4ff"),
				ColorMode = ParticleType.ColorModes.Blink,
				FadeMode = ParticleType.FadeModes.Late,
				Size = 0.5f,
				SizeRange = 0.2f,
				RotationMode = ParticleType.RotationModes.Random,
				LifeMin = 0.6f,
				LifeMax = 1.2f,
				SpeedMin = 40f,
				SpeedMax = 140f,
				SpeedMultiplier = 0.1f,
				Acceleration = new Vector2(0f, 10f),
				DirectionRange = 0.6981317f
			};
		}
	}

	public override bool IsRiding(JumpThru jumpThru)
	{
		return false;
	}

	public override bool IsRiding(Solid solid)
	{
		return false;
	}

	public override void OnSquish(CollisionData data)
	{
		GotoGone();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void OnCollideH(CollisionData data)
	{
		hitSpeed.X *= -1 * bounciness;
	}
	[MethodImpl(MethodImplOptions.NoInlining)]
	private void OnCollideV(CollisionData data)
	{
		// nudges the puffer horizontally to get up/down alongside ledges
		for (int i = -1; i <= 1; i += 2)
		{
			for (int j = 1; j <= 2; j++)
			{
				Vector2 vector = Position + Vector2.UnitX * j * i;
				if (!CollideCheck<Solid>(vector) && !OnGround(vector))
				{
					Position = vector;
					return;
				}
			}
		}
		// Bounces off of the floor/ceiling
		hitSpeed.Y *= -1 * bounciness;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void GotoIdle()
	{
		if (state == States.Gone)
		{
			Position = startPosition;
		}
		hitSpeed = Vector2.Zero;
		state = States.Idle;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void GotoHit(Vector2 from)
	{
		hitSpeed = Vector2.UnitY * 200f;
		state = States.Hit;
	}

	private void GotoHitSpeed(Vector2 speed)
	{
		hitSpeed = speed;
		state = States.Hit;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void GotoGone()
	{
		Vector2 control = Position + (startPosition - Position) * 0.5f;
		if ((startPosition - Position).LengthSquared() > 100f)
		{
			if (Math.Abs(Position.Y - startPosition.Y) > Math.Abs(Position.X - startPosition.X))
			{
				if (Position.X > startPosition.X)
				{
					control += Vector2.UnitX * -24f;
				}
				else
				{
					control += Vector2.UnitX * 24f;
				}
			}
			else if (Position.Y > startPosition.Y)
			{
				control += Vector2.UnitY * -24f;
			}
			else
			{
				control += Vector2.UnitY * 24f;
			}
		}
		returnCurve = new SimpleCurve(Position, startPosition, control);
		Collidable = false;
		goneTimer = 2.5f;
		state = States.Gone;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void Update()
	{
		base.Update();
		if (respawnTimer > 0f)
		{
			respawnTimer -= Engine.DeltaTime;
			if (respawnTimer <= 0f)
			{
				light.Visible = true;
				bloom.Visible = true;
				sprite.Play("on");
				Audio.Play("event:/game/06_reflection/pinballbumper_reset", Position);
			}
		}
		else if (base.Scene.OnInterval(0.05f))
		{
			float num = Calc.Random.NextAngle();
			ParticleType type = P_Ambience;
			float direction = num;
			float length = 8;
			SceneAs<Level>().Particles.Emit(type, 1, base.Center + Calc.AngleToVector(num, length), Vector2.One * 2f, direction);
		}
		Player entity = base.Scene.Tracker.GetEntity<Player>();
		if (entity == null)
		{
			playerAliveFade = Calc.Approach(playerAliveFade, 0f, 1f * Engine.DeltaTime);
		} else {
			playerAliveFade = Calc.Approach(playerAliveFade, 1f, 1f * Engine.DeltaTime);
		}
		switch (state)
		{
		case States.Idle:
			foreach (PufferCollider component in base.Scene.Tracker.GetComponents<PufferCollider>())
			{
				CheckPufferCollider(component);
			}
			break;
		case States.Hit:
			base.Collider = moveCollider;
			MoveH(hitSpeed.X * Engine.DeltaTime, onCollideH);
			MoveV(hitSpeed.Y * Engine.DeltaTime, onCollideV);
			hitSpeed.X = Calc.Approach(hitSpeed.X, 0f, 150f * Engine.DeltaTime);
			hitSpeed = Calc.Approach(hitSpeed, Vector2.Zero, 320f * Engine.DeltaTime);
			if (base.Top >= (float)(SceneAs<Level>().Bounds.Bottom + 5))
			{
				// sprite.Play("hidden");
				Visible = false;
				GotoGone();
				break;
			}
			foreach (PufferCollider component2 in base.Scene.Tracker.GetComponents<PufferCollider>())
			{
				CheckPufferCollider(component2);
			}
			if (hitSpeed == Vector2.Zero)
			{
				ZeroRemainderX();
				ZeroRemainderY();
				GotoIdle();
			}
			break;
			case States.Gone:
			{
				float num = goneTimer;
				goneTimer -= Engine.DeltaTime;
				if (goneTimer <= 0.5f)
				{
					if (num > 0.5f && returnCurve.GetLengthParametric(8) > 8f)
					{
						Audio.Play("event:/new_content/game/10_farewell/puffer_return", Position);
					}
					Position = returnCurve.GetPoint(Ease.CubeInOut(Calc.ClampedMap(goneTimer, 0.5f, 0f)));
				}
				if (goneTimer <= 0f)
				{
					Visible = (Collidable = true);
					GotoIdle();
				}
				break;
			}
		}
		base.Collider = normalCollider;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public bool HitSpring(Spring spring)
	{
		switch (spring.Orientation)
		{
		default:
			if (hitSpeed.Y >= 0f)
			{
				GotoHitSpeed(224f * -Vector2.UnitY);
				MoveTowardsX(spring.CenterX, 4f);
				return true;
			}
			return false;
		case Spring.Orientations.WallLeft:
			if (hitSpeed.X <= 60f)
			{
				GotoHitSpeed(280f * Vector2.UnitX);
				MoveTowardsY(spring.CenterY, 4f);
				return true;
			}
			return false;
		case Spring.Orientations.WallRight:
			if (hitSpeed.X >= -60f)
			{
				GotoHitSpeed(280f * -Vector2.UnitX);
				MoveTowardsY(spring.CenterY, 4f);
				return true;
			}
			return false;
		}
	}

	public override void Added(Scene scene)
	{
		base.Added(scene);
		if (base.Depth == 0 && ((AreaKey)(object)(scene as Level).Session.Area).LevelSet != "Celeste")
		{
			base.Depth = -1;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public void OnPlayer(Player player) {
		if (respawnTimer <= 0f)
		{
			if ((base.Scene as Level).Session.Area.ID == 9)
			{
				Audio.Play("event:/game/09_core/pinballbumper_hit", Position);
			}
			else
			{
				Audio.Play("event:/game/06_reflection/pinballbumper_hit", Position);
			}
			respawnTimer = 0.6f;
			Vector2 vector2 = player.ExplodeLaunch(Position, false, false);
			sprite.Play("hit", restart: true);
			light.Visible = false;
			bloom.Visible = false;
			SceneAs<Level>().DirectionalShake(vector2, 0.15f);
			SceneAs<Level>().Displacement.AddBurst(base.Center, 0.3f, 8f, 32f, 0.8f);
			SceneAs<Level>().Particles.Emit(P_Launch, 12, base.Center + vector2 * 12f, Vector2.One * 3f, vector2.Angle());
			// Moves the bumper in the opposite direction
			GotoHitSpeed(vector2 * -300f);
		}
	}

	private void CheckPufferCollider(PufferCollider pufferCollider) {
		
		Collider collider = pufferCollider.Entity.Collider;
		if (pufferCollider.Collider != null)
		{
			pufferCollider.Entity.Collider = pufferCollider.Collider;
		}
		if (CollideCheck(pufferCollider.Entity))
		{
			HitSpring((Spring)pufferCollider.Entity);
		}
		pufferCollider.Entity.Collider = collider;
	}
}
