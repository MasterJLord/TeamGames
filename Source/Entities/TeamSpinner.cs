using Celeste.Mod.Entities;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;


namespace Celeste.Mod.TeamGames.Entities;

[CustomEntity("TeamGames/TeamSpinner")]
[Tracked(false)]
public class TeamSpinner : Entity
{

	private class Border : Entity
	{
		private Entity[] drawing = new Entity[2];

		[MethodImpl(MethodImplOptions.NoInlining)]
		public Border(Entity parent, Entity filler)
		{
			drawing[0] = parent;
			drawing[1] = filler;
			base.Depth = parent.Depth + 2;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public override void Render()
		{
			if (drawing[0].Visible)
			{
				DrawBorder(drawing[0]);
				DrawBorder(drawing[1]);
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void DrawBorder(Entity entity)
		{
			if (entity == null)
			{
				return;
			}
			foreach (Component component in entity.Components)
			{
				if (component is Image { Color: var color, Position: var position } image)
				{
					image.Color = Color.Black;
					image.Position = position + new Vector2(0f, -1f);
					image.Render();
					image.Position = position + new Vector2(0f, 1f);
					image.Render();
					image.Position = position + new Vector2(-1f, 0f);
					image.Render();
					image.Position = position + new Vector2(1f, 0f);
					image.Render();
					image.Color = color;
					image.Position = position;
				}
			}
		}
	}

	public static ParticleType P_Move;

	public const float ParticleInterval = 0.02f;

	public bool AttachToSolid;
	
	public TeamManager.Team MyTeam;
	private bool isAlignedWithPlayer;

	private Entity filler;

	private Border border;

	private float offset;

	private bool expanded;

	private int randomSeed;

	private Color color;

	private int ID;

	[MethodImpl(MethodImplOptions.NoInlining)]
	public TeamSpinner(EntityData data, Vector2 offset) : base(data.Position + offset) 
	{
		ID = data.ID;
		MyTeam = (TeamManager.Team) data.Float("Team");
		this.offset = Calc.Random.NextFloat();
		this.color = TeamManager.TeamColors[MyTeam];
		base.Tag = Tags.TransitionUpdate;
		base.Collider = new ColliderList(new Circle(6f), new Hitbox(16f, 4f, -8f, -3f));
		Visible = false;
		Add(new PlayerCollider(OnPlayer));
		Add(new HoldableCollider(OnHoldable));
		Add(new LedgeBlocker());
		base.Depth = -8500;
		// TODO: make AttachToSolid a useful bool
		AttachToSolid = true;
		if (AttachToSolid)
		{
			Add(new StaticMover
			{
				OnShake = OnShake,
				SolidChecker = IsRiding,
				OnDestroy = base.RemoveSelf
			});
		}
		randomSeed = Calc.Random.Next();
	}


	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void Awake(Scene scene)
	{
		base.Awake(scene);
		TeamManager.LocalPlayerSwitched += onLocalPlayerSwitched;
		Player player = base.Scene.Tracker.GetEntity<Player>();
		isAlignedWithPlayer = TeamManager.GetTeam(player) == MyTeam;
	}


	public void ForceInstantiate()
	{
		CreateSprites();
		Visible = !isAlignedWithPlayer;
		filler.Visible = !isAlignedWithPlayer;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void Update()
	{
		if (!Visible)
		{
			Collidable = false;
			if (InView())
			{
				Visible = !isAlignedWithPlayer;
				if (filler != null)
				{
					filler.Visible = !isAlignedWithPlayer;
				}
				if (!expanded)
				{
					CreateSprites();
				}
			}
		}
		else
		{
			base.Update();
			if (base.Scene.OnInterval(0.25f, offset) && !InView())
			{
				Visible = false;
				if (filler != null)
				{
					filler.Visible = false;
				}
			}
			if (base.Scene.OnInterval(0.05f, offset))
			{
				Player entity = base.Scene.Tracker.GetEntity<Player>();
				if (entity != null)
				{
					Collidable = !isAlignedWithPlayer && Math.Abs(entity.X - base.X) < 128f && Math.Abs(entity.Y - base.Y) < 128f;
				}
			}
		}
		if (filler != null)
		{
			filler.Position = Position;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private bool InView()
	{
		Camera camera = (base.Scene as Level).Camera;
		if (base.X > camera.X - 16f && base.Y > camera.Y - 16f && base.X < camera.X + 320f + 16f)
		{
			return base.Y < camera.Y + 180f + 16f;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void CreateSprites()
	{
		if (expanded)
		{
			return;
		}
		Calc.PushRandom(randomSeed);
		List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures("danger/crystal/fg_white");
		MTexture mTexture = Calc.Random.Choose(atlasSubtextures);
		if (!SolidCheck(new Vector2(base.X - 4f, base.Y - 4f)))
		{
			Add(new Image(mTexture.GetSubtexture(0, 0, 14, 14)).SetOrigin(12f, 12f).SetColor(color));
		}
		if (!SolidCheck(new Vector2(base.X + 4f, base.Y - 4f)))
		{
			Add(new Image(mTexture.GetSubtexture(10, 0, 14, 14)).SetOrigin(2f, 12f).SetColor(color));
		}
		if (!SolidCheck(new Vector2(base.X + 4f, base.Y + 4f)))
		{
			Add(new Image(mTexture.GetSubtexture(10, 10, 14, 14)).SetOrigin(2f, 2f).SetColor(color));
		}
		if (!SolidCheck(new Vector2(base.X - 4f, base.Y + 4f)))
		{
			Add(new Image(mTexture.GetSubtexture(0, 10, 14, 14)).SetOrigin(12f, 2f).SetColor(color));
		}
		foreach (TeamSpinner entity in base.Scene.Tracker.GetEntities<TeamSpinner>())
		{
			if (entity.ID > ID && entity.AttachToSolid == AttachToSolid && (entity.Position - Position).LengthSquared() < 576f)
			{
				AddSprite((Position + entity.Position) / 2f - Position);
			}
		}
		base.Scene.Add(border = new Border(this, filler));
		expanded = true;
		Calc.PopRandom();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void AddSprite(Vector2 offset)
	{
		if (filler == null)
		{
			base.Scene.Add(filler = new Entity(Position));
			filler.Depth = base.Depth + 1;
		}
		List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures("danger/crystal/bg_white");
		Image image = new Image(Calc.Random.Choose(atlasSubtextures));
		image.Position = offset;
		image.Rotation = (float)Calc.Random.Choose(0, 1, 2, 3) * ((float)Math.PI / 2f);
		image.CenterOrigin();
		image.Color = color;
		filler.Add(image);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private bool SolidCheck(Vector2 position)
	{
		if (AttachToSolid)
		{
			return false;
		}
		foreach (Solid item in base.Scene.CollideAll<Solid>(position))
		{
			if (item is SolidTiles)
			{
				return true;
			}
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void ClearSprites()
	{
		if (filler != null)
		{
			filler.RemoveSelf();
		}
		filler = null;
		if (border != null)
		{
			border.RemoveSelf();
		}
		border = null;
		foreach (Image item in base.Components.GetAll<Image>())
		{
			item.RemoveSelf();
		}
		expanded = false;
	}

	private void OnShake(Vector2 pos)
	{
		foreach (Component component in base.Components)
		{
			if (component is Image image)
			{
				image.Position += pos;
			}
		}
		if (filler == null)
		{
			return;
		}
		foreach (Component component2 in filler.Components)
		{
			if (component2 is Image image2)
			{
				image2.Position += pos;
			}
		}
	}

	private bool IsRiding(Solid solid)
	{
		return CollideCheck(solid);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void OnPlayer(Player player)
	{
		player.Die((player.Position - Position).SafeNormalize());
	}

	private void OnHoldable(Holdable h)
	{
		h.HitSpinner(this);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void Removed(Scene scene)
	{
		if (filler != null && filler.Scene == scene)
		{
			filler.RemoveSelf();
		}
		if (border != null && border.Scene == scene)
		{
			border.RemoveSelf();
		}
		TeamManager.LocalPlayerSwitched -= onLocalPlayerSwitched;
		base.Removed(scene);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public void Destroy(bool boss = false)
	{
		if (InView())
		{
			Audio.Play("event:/game/06_reflection/fall_spike_smash", Position);
			CrystalDebris.Burst(Position, color, boss, 8);
		}
		RemoveSelf();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public void orig_Awake(Scene scene)
	{
		Logger.Log(LogLevel.Debug, "TeamGames/TeamSpinner", "orig being called");
		base.Awake(scene);
		if (InView())
		{
			CreateSprites();
		}
	}

	private void onLocalPlayerSwitched(uint playerID, TeamManager.Team newTeam)
	{
		isAlignedWithPlayer = newTeam == MyTeam;
		if (isAlignedWithPlayer)
		{
			Visible = false;
			if (filler != null)
			{
				filler.Visible = false;
			}
			Collidable = false;
		} else {
			Visible = InView();
			if (filler != null)
			{
				filler.Visible = InView();
			}
			Player player = base.Scene.Tracker.GetEntity<Player>();
			isAlignedWithPlayer = TeamManager.GetTeam(player) == MyTeam;
			Collidable = InView();
		}
		
	}
}
