using System;

namespace Celeste.Mod.practiceMod;

public class practiceModModule : EverestModule {
    public static practiceModModule Instance { get; private set; }

    public override Type SettingsType => typeof(practiceModModuleSettings);
    public static practiceModModuleSettings Settings => (practiceModModuleSettings) Instance._Settings;

    public override Type SessionType => typeof(practiceModModuleSession);
    public static practiceModModuleSession Session => (practiceModModuleSession) Instance._Session;

    public override Type SaveDataType => typeof(practiceModModuleSaveData);
    public static practiceModModuleSaveData SaveData => (practiceModModuleSaveData) Instance._SaveData;

    public practiceModModule() {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(practiceModModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(practiceModModule), LogLevel.Info);
#endif
    }

    public override void Load() {
        // TODO: apply any hooks that should always be active
    }

    public override void Unload() {
        // TODO: unapply any hooks applied in Load()
    }
}