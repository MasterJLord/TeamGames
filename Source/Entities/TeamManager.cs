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

public static class TeamManager 
{

	public enum Team 
	{
		UNSET = -1,
		NONE = 0,
		RED = 1,
		YELLOW = 2,
		GREEN = 3,
		BLUE = 4,
	}

	public static Dictionary<Team, Color> TeamColors = new Dictionary<Team, Color>() 
	{
		[Team.RED] = Color.Red,
		[Team.GREEN] = Color.Green,
		[Team.BLUE] = Color.Blue,
		[Team.YELLOW] = Color.Yellow,
		[Team.NONE] = Color.White
	};

	private static Dictionary<uint, Team> playerTeamAssignments = new();
	private static uint? localPlayerID 
	{
		get {
			return clientContext?.Client.PlayerInfo.ID;
		}
	}
	private static CelesteNetClientContext clientContext;
	private static List<uint> missingPlayerIDs = new();
	private static List<uint> missingPlayerTimes = new();

	public delegate void TeamSwitchHandler(uint playerID, Team newTeam);
	public static event TeamSwitchHandler LocalPlayerSwitched;
	public static event TeamSwitchHandler RemotePlayerSwitched;
	

	public static Team GetTeam(Actor player, Team defaultTeam = Team.UNSET) {
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

	public static Team GetTeam(uint playerID, Team defaultTeam = Team.UNSET) {
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
			Player = clientContext.Client.PlayerInfo,
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
		DataContext data = context.Client.Data;
		data.RegisterHandler<DataTeamSwitchEvent>(Handle);
		data.RegisterHandler<DataTeamsRequest>(Handle);
		data.RegisterHandler<DataTeamsList>(Handle);
		clientContext.Client.Send(new DataTeamsRequest() {Player = clientContext.Client.PlayerInfo});
		Logger.Log(LogLevel.Debug, "practiceMod/TeamManager", "Got client context");
	}

	// Updates the remote players' teams locally
	private static void Handle(CelesteNetConnection con, DataTeamSwitchEvent data) {
		SetTeamRemote(data.SwitchingPlayerID, data.NewTeam);
	}

	private static void SetTeamRemote(uint id, Team newTeam) {
		if (GetTeam(id) == newTeam) {
			Logger.Log(LogLevel.Debug, "practiceMod/TeamManager", "Remote player's team is not being switched to their current team (" + id + " is still " + newTeam + ")");
			return;
		}
		Logger.Log(LogLevel.Debug, "practiceMod/TeamManager", "Switching remote player's team (" + id + " is now " + newTeam + ")");
		OnRemotePlayerSwitched(id, newTeam);
		playerTeamAssignments[id] = newTeam;
	}

	// Syncs all team data with other players in the server when a player joins a server
	private static void Handle(CelesteNetConnection con, DataTeamsRequest data) {
		// Only respond to the request if I have the lowest ID, so that multiple clients are not sending redundant information
		DataPlayerInfo[] playerList = clientContext.Client.Data.GetRefs<DataPlayerInfo>();
		foreach (DataPlayerInfo player in playerList) {
			if (player.ID < localPlayerID) {
				return;
			}
		}
		// Build a response data packet with the team data of all players who are still in the server is contained
		DataTeamsList packet = new() {Player = clientContext.Client.PlayerInfo};
		foreach (DataPlayerInfo player in playerList) {
			if (GetTeam(player.ID) == Team.UNSET) {
				continue;
			}
			packet.PlayerAssignments[player.ID] = GetTeam(player.ID);
		}
		clientContext.Client.Send(packet);

	}

	private static void Handle(CelesteNetConnection con, DataTeamsList data) {
		Dictionary<uint, Team>.KeyCollection ids = data.PlayerAssignments.Keys;
		foreach (uint playerID in ids) {
			Logger.Log(LogLevel.Debug, "practiceMod/TeamManager", "Receiving teams list: player " + playerID + " is " + data.PlayerAssignments[playerID]);
			SetTeamRemote(playerID, data.PlayerAssignments[playerID]);
		}
	}
}
