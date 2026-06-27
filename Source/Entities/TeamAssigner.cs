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
		switch (MyTeam) {
			case TeamManager.Team.RED:
				Add(sprite = GFX.SpriteBank.Create("TeamAssignerRed"));
				break;
			case TeamManager.Team.GREEN:
				Add(sprite = GFX.SpriteBank.Create("TeamAssignerGreen"));
				break;
			case TeamManager.Team.YELLOW:
				Add(sprite = GFX.SpriteBank.Create("TeamAssignerYellow"));
				break;
			case TeamManager.Team.BLUE:
				Add(sprite = GFX.SpriteBank.Create("TeamAssignerBlue"));
				break;
		}
		base.Collider.Width = 16f;
		base.Collider.Height = 16f;
		OnDashCollide = OnDashed;
	}

	// Makes the assigner change its sprite when the local player switches teams
	
	public override void Awake(Scene scene) {
		base.Awake(scene);
		if (sprite == null) {
			return;
		}
		if (TeamManager.GetTeam(scene.Tracker.GetEntity<Player>()) == MyTeam) {
			sprite.Play("idleIsMember");
		} else {
			sprite.Play("idleNonMember");
		}
		TeamManager.LocalPlayerSwitched += updateSprite;
	}

	public override void Removed(Scene scene) {
		base.Removed(scene);
		TeamManager.LocalPlayerSwitched -= updateSprite;
	}

	private void updateSprite(uint localPlayerID, TeamManager.Team newTeam) {
		if (newTeam == MyTeam) {
			sprite.Play("idleIsMember");
		} else {
			sprite.Play("idleNonMember");
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private DashCollisionResults OnDashed(Player player, Vector2 direction) {
		TeamManager.SetTeam(MyTeam);
		return DashCollisionResults.NormalCollision;
	}
}
