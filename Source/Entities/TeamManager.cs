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

namespace Celeste.Mod.TeamGames.Entities;

public static class TeamManager 
{

	private class TeamSyncerDelayer : Entity
	{
		private const float attemptIntervals = 0.5f;
		public static Level Level = null;
		public static TeamSyncerDelayer Instance;

		public TeamSyncerDelayer() : base(Vector2.Zero)
		{
			Add(new Coroutine(Sequence()));
			Instance = this;
		}

		public override void Removed(Scene scene)
		{
			if (Instance == this)
			{
				Instance = null;
			}
		}

		public IEnumerator<float> Sequence()
		{
			while (true)
			{
				yield return attemptIntervals;
				TeamManager.TrySyncTeams();
			}
		}
	}
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
		[Team.RED] = Calc.HexToColor("cc3232"),
		[Team.GREEN] = Calc.HexToColor("64dc00"),
		[Team.BLUE] = Calc.HexToColor("5b6fe1"),
		[Team.YELLOW] = Calc.HexToColor("ffff00"),
		[Team.NONE] = Calc.HexToColor("ffffff"),
		[Team.UNSET] = Calc.HexToColor("ac3232")
	};

	public static Dictionary<Team, Color> TeamColorsAlternate = new Dictionary<Team, Color>() 
	{
		[Team.RED] = Calc.HexToColor("9a1f2a"),
		[Team.GREEN] = Calc.HexToColor("68a334"),
		[Team.BLUE] = Calc.HexToColor("3f3f74"),
		[Team.YELLOW] = Calc.HexToColor("baba30"),
		[Team.NONE] = Calc.HexToColor("000000"),
		[Team.UNSET] = Calc.HexToColor("44b7ff")
	};

	private static Dictionary<uint, Team> playerTeamAssignments = new();
	public static uint? localPlayerID 
	{
		get 
		{
			if (clientContext == null || clientContext.Client == null || clientContext.Client.PlayerInfo  == null)
			{
				return 0;
			}
			return clientContext.Client.PlayerInfo.ID;
		}
	}
	private static CelesteNetClientContext clientContext;
	private static List<uint> missingPlayerIDs = new();
	private static List<uint> missingPlayerTimes = new();

	public delegate void TeamSwitchHandler(uint playerID, Team newTeam);
	public static event TeamSwitchHandler LocalPlayerSwitched;
	public static event TeamSwitchHandler RemotePlayerSwitched;
	
	private static bool teamsSynced = false;
	private static long lastSyncRequest = 0;

	public static Team GetTeam(Actor player, Team defaultTeam = Team.UNSET) 
	{
		if (player is Player) 
		{
			if (localPlayerID == null) 
			{
				return defaultTeam;
			}
			return GetTeam(localPlayerID, defaultTeam);
		}
		if (player is Ghost) 
		{
			uint? id = ((Ghost) player).PlayerInfo?.ID;
			if (id == null) 
			{
				return defaultTeam;
			}
			return GetTeam(id, defaultTeam);
		}
		return defaultTeam;
	}

	public static Team GetTeam(uint? playerID, Team defaultTeam = Team.UNSET) 
	{
		if (playerID == null)
		{
			return defaultTeam;
		}
		if (!playerTeamAssignments.ContainsKey((uint) playerID)) 
		{
			return defaultTeam;
		}
		return playerTeamAssignments[(uint) playerID];
	}

	public static void SetTeam(Team newTeam = Team.NONE) 
	{
		// Updates the local player's team locally
		if (localPlayerID == null) 
		{
			return;
		}
		if (GetTeam(localPlayerID) == newTeam) 
		{
			return;
		}
		Logger.Log(LogLevel.Debug, "TeamGames/TeamManager", "Switching Local Player's Team (" + localPlayerID + " is now " + newTeam + ")");
		OnLocalPlayerSwitched(newTeam);
		playerTeamAssignments[(uint) localPlayerID] = newTeam;
		// Updates the local player's team remotely
		if (clientContext == null)
		{
			return;
		}
		DataTeamSwitchEvent packet = new DataTeamSwitchEvent {
			Player = clientContext.Client.PlayerInfo,
			SwitchingPlayerID = (uint) localPlayerID,
			NewTeam = newTeam
		};
		clientContext.Client.Send(packet);
	}

	// Wrapper functions for the events
	
	private static void OnLocalPlayerSwitched(Team newTeam) 
	{
		if (localPlayerID == null) 
		{
			return;
		}
		LocalPlayerSwitched?.Invoke((uint) localPlayerID, newTeam);
	}

	private static void OnRemotePlayerSwitched(uint playerID, Team newTeam) 
	{
		RemotePlayerSwitched?.Invoke(playerID, newTeam);
	}

	// Function used to get access to the client context
	public static void GetClientContext(CelesteNetClientContext context) 
	{
		clientContext = context;
		DataContext data = context.Client.Data;
		data.RegisterHandler<DataTeamSwitchEvent>(Handle);
		data.RegisterHandler<DataTeamsRequest>(Handle);
		data.RegisterHandler<DataSync>(Handle);
		data.RegisterHandler<DataChannelMove>(Handle);
		data.RegisterFilter<DataPlayerFrame>(RecolorHair);
		Logger.Log(LogLevel.Debug, "TeamGames/TeamManager", "Got client context");
	}

	/*
	public static void Handle(CelesteNetConnection con, DataReady data)
	{
		OnEnterLobby();
	}
	*/

	public static void Handle(CelesteNetConnection con, DataChannelMove data)
	{
		if (data.Player.ID == clientContext?.Client?.PlayerInfo?.ID) {
			Logger.Log(LogLevel.Debug, "TeamGames/TeamsList", "Switching lobby - resetting teams");
			ResetTeams();
			TrySyncTeams();
		}
	}

	public static void OnEnter(Session session, bool fromSaveData)
	{
		Logger.Log(LogLevel.Debug, "TeamGames/TeamsList", "Entering level - resetting teams");
		// TODO : is the TrySyncTeams in ResetTeams happening too soon here?
		ResetTeams();
	}

	private static void ResetTeams()
	{
		playerTeamAssignments = new();
		OnLocalPlayerSwitched(Team.UNSET);
		DataPlayerInfo[] playerList = clientContext?.Client?.Data?.GetRefs<DataPlayerInfo>();
		if (playerList != null && playerList.Length < 2)
		{
			return;
		}
		teamsSynced = false;
		lastSyncRequest = 0;
	}

	public static void OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader)
	{
		Logger.Log(LogLevel.Debug, "TeamGames/TeamsList", "Loaded level");
		TeamSyncerDelayer.Level = level;
		if (teamsSynced)
		{
			return;
		}
		TrySyncTeams();
	}

	public static void TrySyncTeams()
	{
		if (SyncedHoldable.ServerTime - lastSyncRequest < 9e6) // 0.9 seconds
		{
			Logger.Log(LogLevel.Debug, "TeamGames/TeamsList", "Sync request spam blocked");
			return;
		}
		Logger.Log(LogLevel.Debug, "TeamGames/TeamsList", "Attempting to sync teams");

		if (clientContext != null && clientContext.Client != null && clientContext.Client.PlayerInfo != null && clientContext.Client.IsReady)
		{
			SyncTeams();
			lastSyncRequest = SyncedHoldable.ServerTime;
			if (TeamSyncerDelayer.Instance != null)
			{
				TeamSyncerDelayer.Instance.RemoveSelf();
			}
			return;
		}
		Logger.Log(LogLevel.Debug, "TeamGames/TeamsList", "Failed to sync teams");
		if (TeamSyncerDelayer.Level == null)
		{
			Logger.Log(LogLevel.Debug, "TeamGames/TeamsList", "No level found");
			return;
		}
		if (TeamSyncerDelayer.Instance == null)
		{
			TeamSyncerDelayer.Level.Add(new TeamSyncerDelayer());
		}

	}

	public static void SyncTeams()
	{
		Logger.Log(LogLevel.Debug, "TeamGames/TeamsList", clientContext?.Client == null ? "Missing client" : "Client found");
		
		clientContext?.Client.Send(new DataTeamsRequest {
			Player = clientContext.Client.PlayerInfo,
			senderID = localPlayerID
			});
		Logger.Log(LogLevel.Debug, "TeamGames/TeamsList", "Requested teams info");
		// TODO: this is being called too early, and the request is not being received
	}


	// Updates the remote players' teams locally
	private static void Handle(CelesteNetConnection con, DataTeamSwitchEvent data) 
	{
		SetTeamRemote(data.SwitchingPlayerID, data.NewTeam);
	}

	private static void SetTeamRemote(uint id, Team newTeam) 
	{
		if (GetTeam(id) == newTeam) 
		{
			Logger.Log(LogLevel.Debug, "TeamGames/TeamManager", "Remote player's team is not being switched to their current team (" + id + " is still " + newTeam + ")");
			return;
		}
		Logger.Log(LogLevel.Debug, "TeamGames/TeamManager", "Switching remote player's team (" + id + " is now " + newTeam + ")");
		OnRemotePlayerSwitched(id, newTeam);
		playerTeamAssignments[id] = newTeam;
	}

	// Syncs all team data with other players in the server when a player joins a server
	private static void Handle(CelesteNetConnection con, DataTeamsRequest data)
	{
		Logger.Log(LogLevel.Debug, "TeamGames/TeamManager", "Received request for teams");
		// Only respond to the request if I have the lowest other ID, so that multiple clients are not sending redundant information
		DataPlayerInfo[] playerList = clientContext.Client.Data.GetRefs<DataPlayerInfo>();
		foreach (DataPlayerInfo player in playerList)
		{
			if (player.ID == data.senderID)
			{
				continue;
			}
			if (player.ID < localPlayerID)
			{
				return;
			}
		}
		Logger.Log(LogLevel.Debug, "TeamGames/TeamManager", "Responding to request for teams");
		// Build a response data packet with the team data of all players who are still in the server is contained
		DataSync packet = new DataSync {Player = clientContext.Client.PlayerInfo};
		foreach (DataPlayerInfo player in playerList)
		{
			if (GetTeam(player.ID) == Team.UNSET)
			{
				continue;
			}
			packet.PlayerAssignments[player.ID] = GetTeam(player.ID);
		}
		clientContext.Client.Send(packet);

	}

	private static void Handle(CelesteNetConnection con, DataSync data)
	{
		teamsSynced = true;
		Logger.Log(LogLevel.Debug, "TeamGames/TeamManager", "Received teams list");
		Dictionary<uint, Team>.KeyCollection ids = data.PlayerAssignments.Keys;
		foreach (uint playerID in ids)
		{
			Logger.Log(LogLevel.Debug, "TeamGames/TeamManager", "Receiving teams list: player " + playerID + " is " + data.PlayerAssignments[playerID]);
			SetTeamRemote(playerID, data.PlayerAssignments[playerID]);
		}
	}

	private static bool RecolorHair(CelesteNetConnection con, DataPlayerFrame data)
	{
		if (data.Player == null || GetTeam(data.Player.ID) == Team.UNSET)
		{
			return true;
		}
		data.HairColors = new Color[] {
			TeamColors[GetTeam(data.Player.ID)],
			TeamColorsAlternate[GetTeam(data.Player.ID)]
		};
		return true;
	}

	public static void ScorePoint(Scene scene, TeamManager.Team winningTeam, bool killPlayers = false, Vector2? position = null)
	{
		Logger.Log(LogLevel.Debug, "TeamGames/TeamManager", "Team " + winningTeam + " scored a point" + (killPlayers ? "; killing players" : ""));
		Player player = scene.Tracker.GetEntity<Player>();
		Vector2 playerPosition = player == null ? Vector2.Zero : player.Position;
		bool victory = GetTeam(localPlayerID) == winningTeam;
		if (victory)
		{
			Audio.Play("event:/game/general/strawberry_get", (position == null) ? playerPosition : (Vector2) position, "colour", 3, "count", 1); 
		} else {
			Audio.Play("event:/new_content/char/madeline/death_golden", (position == null) ? playerPosition : (Vector2) position);
		}
		if (killPlayers)
		{
			if (player == null)
			{
				Logger.Log(LogLevel.Warn, "TeamGames/TeamManager", "Losing player was not found");
				return;
			}
			player.Die(Vector2.UnitY * (victory ? -1 : 1));
		}
	}
}
