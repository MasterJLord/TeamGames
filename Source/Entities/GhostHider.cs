using Celeste.Mod.Entities;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.CelesteNet.Client.Entities;

namespace Celeste.Mod.practiceMod.Entities;

[CustomEntity("practiceMod/GhostHider")]
[Tracked(false)]
public class GhostHider : Trigger {

	private static Dictionary<uint, int> ghostsInZones = new();

	private List<Ghost> internalGhosts = new();

	public GhostHider(EntityData data, Vector2 offset) : base(data, offset) 
	{
	}

	// This will get laggy if there are too many of them in the level.
	// TODO: switch to a better optimized method if I ever find one.
	
	public override void Update()
	{
		foreach (Ghost ghost in Scene.Tracker.GetEntities<Ghost>())
		{
			bool alreadyContained = internalGhosts.Contains(ghost);
			bool nowContains = CollideCheck(ghost);
			if (!alreadyContained && nowContains)
			{
				ghostEntered(ghost);
			} else if (alreadyContained && !nowContains) {
				ghostLeft(ghost);
			}

		}
	}

	public override void Removed(Scene scene)
	{
		while (internalGhosts.Count > 0)
		{
			ghostLeft(internalGhosts[0]);
		}
	}

	private void ghostEntered(Ghost ghost)
	{
		uint? ghostID = (uint) ghost.PlayerInfo?.ID;
		if (ghostID == null)
		{
			Logger.Log(LogLevel.Warn, "PracticeMod/GhostHider", "Entering ghost was missing an ID");
			return;
		}
		if (!ghostsInZones.ContainsKey((uint) ghostID))
		{
			ghostsInZones[(uint) ghostID] = 0;
		}
		ghostsInZones[(uint) ghostID]++;
		if (ghostsInZones[(uint) ghostID] == 1)
		{
			ghost.Visible = false;
		}
	}

	private void ghostLeft(Ghost ghost)
	{
		uint? ghostID = (uint) ghost.PlayerInfo?.ID;
		if (ghostID == null)
		{
			Logger.Log(LogLevel.Warn, "PracticeMod/GhostHider", "Entering ghost was missing an ID");
			return;
		}
		ghostsInZones[(uint) ghostID]--;
		if (ghostsInZones[(uint) ghostID] == 0)
		{
			ghost.Visible = true;
		}
	}


}
