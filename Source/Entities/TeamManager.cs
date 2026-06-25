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

public static class TeamManager {

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
			return clientContext?.Client.PlayerInfo.ID;
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
		// Updates the local player's team locally
		if (localPlayerID == null) {
			return;
		}
		if (GetTeam((uint) localPlayerID) == newTeam) {
			return;
		}
		Logger.Log(LogLevel.Debug, "practiceMod/TeamManager", "Switching Local Player's Team (" + localPlayerID + " is now " + newTeam + ")");
		OnLocalPlayerSwitched(newTeam);
		playerTeamAssignments[(uint) localPlayerID] = newTeam;
		// Updates the local player's team remotely
		DataTeamSwitchEvent packet = new DataTeamSwitchEvent {
			SwitchingPlayerID = (uint) localPlayerID,
			NewTeam = newTeam
		};
		clientContext.Client.Send(packet);
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

	// Function used to get access to the client context
	public static void GetClientContext(CelesteNetClientContext context) {
		clientContext = context;
		CelesteNetClient client = context.Client;
		// TODO: data doesn't seem to exist when the client is created; fix this
		DataContext data = client.Data;
		data.RegisterHandler<DataTeamSwitchEvent>(Handle);
		data.RegisterHandler<DataUnparsed>(Handle);
		Logger.Log(LogLevel.Debug, "practiceMod/TeamManager", "Got client context");
		Logger.Log(LogLevel.Debug, "PracticeMod/TeamManager", "ID is " + data.DataTypeToID[typeof(DataTeamSwitchEvent)]);
		// TODO: fetch teams information from the server when joining
	}

	// Updates the remote players' teams locally
	private static void Handle(CelesteNetConnection con, DataTeamSwitchEvent data) {
		if (GetTeam(data.SwitchingPlayerID) == data.NewTeam) {
			return;
		}
		Logger.Log(LogLevel.Debug, "practiceMod/TeamManager", "Switching remote player's team (" + localPlayerID + " is now " + data.NewTeam + ")");
		OnRemotePlayerSwitched(data.SwitchingPlayerID, data.NewTeam);
		playerTeamAssignments[data.SwitchingPlayerID] = data.NewTeam;
	}

	private static void Handle(CelesteNetConnection con, DataUnparsed data) {
		Logger.Log(LogLevel.Debug, "practiceMod/TeamManager", "Unparsed data received");
	}
}
