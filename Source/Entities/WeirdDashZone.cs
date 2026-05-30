
using Celeste.Mod.Entities;
using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.practiceMod.Entities;

[CustomEntity("practiceMod/WeirdDashZone")]
public class WeirdDashZone : Trigger {
	public int EntryDashes = 1;
	public int ExitDashes = 0;
	private static int withinZones = 0;
	public Color MyColor;
	
	[MethodImpl(MethodImplOptions.NoInlining)]
	public WeirdDashZone(EntityData data, Vector2 offset)
		: base(data, offset)
	{
		MyColor = new Color(40, 200, 100, 100);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void OnEnter(Player player)
	{
		++withinZones;
		if (withinZones == 1) {
			player.Dashes = EntryDashes;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void OnLeave(Player player) {
		--withinZones;
		if (withinZones == 0) {
			player.Dashes = ExitDashes;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void Render() {
		// Collider.Render(Scene.Camera, MyColor);
		// Draw.Rect(Collider.AbsoluteX, Collider.AbsoluteY, Width, Height, MyColor);
		Collider.Render(null, MyColor);
	}
}
