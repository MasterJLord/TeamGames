
using System;
using System.Runtime.CompilerServices;
using Monocle;

namespace Celeste.Mod.practiceMod.Entities;

[Tracked(false)]
public class BumperCollider : Component
{
	public Action<SampleEntity> OnCollide;

	public Collider Collider;

	[MethodImpl(MethodImplOptions.NoInlining)]
	public BumperCollider(Action<SampleEntity> onCollide, Collider collider = null)
		: base(active: false, visible: false)
	{
		OnCollide = onCollide;
		Collider = null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public void Check(SampleEntity bumper)
	{
		if (OnCollide != null)
		{
			Collider collider = base.Entity.Collider;
			if (Collider != null)
			{
				base.Entity.Collider = Collider;
			}
			if (bumper.CollideCheck(base.Entity))
			{
				OnCollide(bumper);
			}
			base.Entity.Collider = collider;
		}
	}
}
