using Celeste.Mod.Entities;
using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.CelesteNet.Client;
using Celeste.Mod.CelesteNet.DataTypes;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.Client.Entities;

namespace Celeste.Mod.practiceMod.Entities;

[CustomEntity("practiceMod/TeamManager")]
public class TeamManager {

	public enum Team {
		RED,
		BLUE,
		GREEN,
		YELLOW,
		NONE
	}

	private static Dictionary<uint, Team> playerTeamAssignments = new();
	private static uint? localPlayerID {
		get {
			return clientContext.Client.PlayerInfo.ID;
		}
	}
	private static CelesteNetClientContext clientContext;

	public delegate void TeamSwitchHandler(uint playerID, Team newTeam);
	public static event TeamSwitchHandler LocalPlayerSwitched;
	public static event TeamSwitchHandler RemotePlayerSwitched;

	public static Team GetTeam(Actor player, Team defaultTeam = Team.NONE) {
		if (player is Player) {
			if (localPlayerID == null) {
				return defaultTeam;
			}
			return GetTeam((uint) localPlayerID, defaultTeam);
		}
		if (player is Ghost) {
			uint? id = ((Ghost) player).PlayerInfo?.ID;
			if (id == null) {
				return defaultTeam;
			}
			return GetTeam((uint) id);
		}
		return defaultTeam;
	}

	public static Team GetTeam(uint playerID, Team defaultTeam = Team.NONE) {
		if (!playerTeamAssignments.ContainsKey(playerID)) {
			return defaultTeam;
		}
		return playerTeamAssignments[playerID];
	}

	public static void SetTeam(Team newTeam = Team.NONE) {
		if (localPlayerID == null) {
			return;
		}
		if (GetTeam((uint) localPlayerID) == newTeam) {
			return;
		}
		OnLocalPlayerSwitched(newTeam);
		playerTeamAssignments[(uint) localPlayerID] = newTeam;
	}

	// Wrapper functions for the events
	
	private static void OnLocalPlayerSwitched(Team newTeam) {
		if (localPlayerID == null) {
			return;
		}
		LocalPlayerSwitched?.Invoke((uint) localPlayerID, newTeam);
	}

	private static void OnRemotePlayerSwitched(uint playerID, Team newTeam) {
		RemotePlayerSwitched?.Invoke(playerID, newTeam);
	}

	// Functions used to get access to the client context
	
	public void Load() {
		CelesteNetClientContext.OnCreate += GetClientContext;
	}

	private void GetClientContext(CelesteNetClientContext context) {
		clientContext = context;
		context.Client.Data.RegisterHandler<DataUnparsed>((DataHandler<DataUnparsed>) Handle);
		// TODO: fetch teams information from the server when joining
	}

	private static void Handle(CelesteNetConnection con, DataUnparsed data) {

	}
}
