using System;
using Celeste.Mod.TeamGames.Entities;
using Celeste.Mod.CelesteNet.Client;

namespace Celeste.Mod.TeamGames;

public class TeamGamesModule : EverestModule {
    public static TeamGamesModule Instance { get; private set; }

    public override Type SettingsType => typeof(TeamGamesModuleSettings);
    public static TeamGamesModuleSettings Settings => (TeamGamesModuleSettings) Instance._Settings;

    public override Type SessionType => typeof(TeamGamesModuleSession);
    public static TeamGamesModuleSession Session => (TeamGamesModuleSession) Instance._Session;

    public override Type SaveDataType => typeof(TeamGamesModuleSaveData);
    public static TeamGamesModuleSaveData SaveData => (TeamGamesModuleSaveData) Instance._SaveData;

    public TeamGamesModule() {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(TeamGamesModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(TeamGamesModule), LogLevel.Info);
#endif
    }

    public override void Load() {
	CelesteNetClientContext.OnInit += TeamManager.GetClientContext;
	CelesteNetClientContext.OnInit += SyncedHoldable.GetClientContext;
	CelesteNetClientContext.OnInit += TeamBall.GetClientContext;
	CelesteNetClientContext.OnInit += SyncedZipMover.GetClientContext;
	Everest.Events.Level.OnExit += SyncedZipMover.OnExit;
    }

    public override void Unload() {
	CelesteNetClientContext.OnInit -= TeamManager.GetClientContext;
	CelesteNetClientContext.OnInit -= SyncedHoldable.GetClientContext;
	CelesteNetClientContext.OnInit -= TeamBall.GetClientContext;
	CelesteNetClientContext.OnInit -= SyncedZipMover.GetClientContext;
	Everest.Events.Level.OnExit -= SyncedZipMover.OnExit;
    }
}
