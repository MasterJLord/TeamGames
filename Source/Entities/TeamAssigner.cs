using Celeste.Mod.Entities;
using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.practiceMod.Entities;

[CustomEntity("practiceMod/TeamAssigner")]
public class TeamAssigner : Solid {
	public TeamManager.Team MyTeam;
	private Sprite sprite;

	public TeamAssigner(EntityData data, Vector2 offset) : base(data.Position + offset, 16f, 16f, false) {
		MyTeam = (TeamManager.Team) data.Float("Team");
		Add(sprite = GFX.SpriteBank.Create("dashSwitch_default"));
		base.Collider.Width = 16f;
		base.Collider.Height = 8f;
		OnDashCollide = OnDashed;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private DashCollisionResults OnDashed(Player player, Vector2 direction) {
		TeamManager.SetTeam(MyTeam);
		return DashCollisionResults.NormalCollision;
	}
}
