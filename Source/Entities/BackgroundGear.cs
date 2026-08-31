using System;
using Celeste.Mod.Entities;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.TeamGames.Entities;

[CustomEntity("TeamGames/BackgroundGear")]
public class BackgroundGear : Entity
{
	public enum GearSize
	{
		small,
		medium,
		large
	}
	/*
	public delegate void CassetteSwitchHandler(int index);
	public static event CassetteSwitchHandler CassetteBlockSwitched;
	*/

	public float ScrollSpeed = 1.0f;
	public Vector2 StartPosition;
	private GearSize size;
	private Sprite sprite;
	private bool musicSynced = false;
	private float cycleTime
	{
		get
		{
			if (_cycleTime == 0f)
			{
				return _cycleTime;
			}
			return 1f;
		}
		set
		{
			_cycleTime = value;
		}
	}
	private float _cycleTime;


	/*
	public static void TriggerGears(On.Celeste.CassetteBlockManager.orig_SetActiveIndex orig, CassetteBlockManager self, int index)
	{
		orig(self, index);
		CassetteBlockSwitched?.Invoke(index);
	}
	*/

	public BackgroundGear(EntityData data, Vector2 offset) : base(data.Position + offset) 
	{
		ScrollSpeed = data.Float("scrollspeed");
		base.Depth = 9998 - (int) (100 * ScrollSpeed);
		cycleTime  = data.Float("cycle");
		size = data.Enum<GearSize> ("size");
		switch (size)
		{
			case GearSize.large:
				Add(sprite = GFX.SpriteBank.Create("Gear3"));
				break;
			case GearSize.medium:
				Add(sprite = GFX.SpriteBank.Create("Gear2"));
				break;
			default:
				Add(sprite = GFX.SpriteBank.Create("Gear1"));
				break;
		}
		sprite.SetColor(data.HexColor("color"));
		StartPosition = Position;
		
	}

	public override void Awake(Scene scene)
	{
		base.Awake(scene);
		CassetteBlockManager musicManager = Scene.Tracker.GetEntity<CassetteBlockManager>();
		if (musicManager != null)
		{
			int maxBeat = SceneAs<Level>().CassetteBlockBeats;
			CassetteListener listener = new CassetteListener((base.SourceId.ID / 2) % maxBeat);
			listener.OnActivated += Rotate;
			Add(listener);
			musicSynced = true;
			// CassetteBlockSwitched += OnCassetteSwitched;
		}
	}

	public override void Removed(Scene scene)
	{
		base.Removed(scene);
		if (musicSynced)
		{
			// CassetteBlockSwitched -= OnCassetteSwitched;
		}
	}


	public override void Update()
	{
		base.Update();
		Vector2 camera = (base.Scene as Level).Camera.Position;
		Position = StartPosition + camera * ScrollSpeed;
		if (musicSynced)
		{
			return;
		}
		if (base.Scene.OnInterval(cycleTime))
		{
			Rotate();
		}
	}

	public void Rotate()
	{
		if (base.SourceId.ID % 2 == 0)
		{
			sprite.Play("SpinClockwise");
		} else {
			sprite.Play("SpinCounterclockwise");
		}
	}

	private void OnCassetteSwitched(int index)
	{
		Rotate();
	}


}
