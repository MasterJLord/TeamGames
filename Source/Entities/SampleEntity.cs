using Celeste.Mod.Entities;
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
	private Vector2 anchor;
	private Vector2 hitSpeed;
	private States state;
	private Curve returnCurve = new SimpleCurve(Position, startPosition, control);
	private bool Collidable = false;
	private float goneTimer = 2.5f;
	private Vector2 lastHitSpeedPosition;
	private ParticleType P_Ambience;
	private ParticleType P_Launch;

	public SampleEntity(EntityData data, Vector2 offset) : base(data.Position + offset) {
		base.Collider = new Circle(12f);
		Add(new PlayerCollider(OnPlayer));
		Add(sprite = GFX.SpriteBank.Create("bumper"));
		Add(light = new VertexLight(Color.Teal, 1f, 16, 32));
		Add(bloom = new BloomPoint(0.5f, 16f));
		anchor = Position;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public Puffer(EntityData data, Vector2 offset)
		: this(data.Position + offset, data.Bool("right"))
	{
	}

	public override bool IsRiding(JumpThru jumpThru)
	{
		return false;
	}

	public override bool IsRiding(Solid solid)
	{
		return false;
	}

	protected override void OnSquish(CollisionData data)
	{
		GotoGone();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void OnCollideH(CollisionData data)
	{
		hitSpeed.X *= -0.8f;
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
		hitSpeed.Y *= -0.8f;
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
		}
		else
		{
			playerAliveFade = Calc.Approach(playerAliveFade, 1f, 1f * Engine.DeltaTime);
			lastPlayerPos = entity.Center;
		}
		switch (state)
		{
		case States.Idle:
			foreach (PufferCollider component in base.Scene.Tracker.GetComponents<PufferCollider>())
			{
				component.Check(this);
			}
			break;
		case States.Hit:
			lastSpeedPosition = Position;
			MoveH(hitSpeed.X * Engine.DeltaTime, onCollideH);
			MoveV(hitSpeed.Y * Engine.DeltaTime, OnCollideV);
			anchorPosition = Position;
			hitSpeed.X = Calc.Approach(hitSpeed.X, 0f, 150f * Engine.DeltaTime);
			hitSpeed = Calc.Approach(hitSpeed, Vector2.Zero, 320f * Engine.DeltaTime);
			if (base.Top >= (float)(SceneAs<Level>().Bounds.Bottom + 5))
			{
				sprite.Play("hidden");
				GotoGone();
				break;
			}
			foreach (PufferCollider component2 in base.Scene.Tracker.GetComponents<PufferCollider>())
			{
				component2.Check(this);
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
				facing.X = 1f;
				GotoHitSpeed(280f * Vector2.UnitX);
				MoveTowardsY(spring.CenterY, 4f);
				return true;
			}
			return false;
		case Spring.Orientations.WallRight:
			if (hitSpeed.X >= -60f)
			{
				facing.X = -1f;
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

	public override void OnPlayer(Player player) {
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
			GotoHit(vector2 * 300f);
		}
	}
}
