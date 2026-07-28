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
	}
	public float ScrollSpeed = 1.0f;
	public Vector2 StartPosition;
	private GearSize size;
	private Sprite sprite;
	private float cycleTime
	{
		get
		{
			CassetteBlockManager musicManager = Scene.Tracker.GetEntity<CassetteBlockManager>();
			if (musicManager != null)
			{
				return DynamicData.For(musicManager).Get<int>("beatsPerTick") / (6f * musicManager.tempoMult);
			}
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

	public BackgroundGear(EntityData data, Vector2 offset) : base(data.Position + offset) 
	{
		ScrollSpeed = data.Float("scrollspeed");
		base.Depth = 9998 - (int) (100 * ScrollSpeed);
		cycleTime  = data.Float("cycle");
		size = (GearSize) data.Float("size");
		switch (size)
		{
			default:
				Add(sprite = GFX.SpriteBank.Create("Gear2"));
				break;
		}
		sprite.SetColor(data.HexColor("color"));
		StartPosition = Position;
		
	}

	public override void Update()
	{
		Vector2 camera = (base.Scene as Level).Camera.Position;
		Position = StartPosition - camera * ScrollSpeed;
	}

}
