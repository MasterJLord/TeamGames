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

// Uses DataModRec as a template
public class DataSync : DataType<DataSync> {
	
	static DataSync() {
		DataID = "Sync";
	}

	public DataPlayerInfo Player;
	public Dictionary<uint, TeamManager.Team> PlayerAssignments = new();

	// Gives this data the MetaPlayerUpdate metadata, which tells the server to broadcast it to all other players when it is sent to the server
	
        public override MetaType[] GenerateMeta(DataContext ctx)
	{
		MetaType[] meta = new MetaType[] {
			new MetaPlayerUpdate(Player)
		};
		return meta;
	}

        public override void FixupMeta(DataContext ctx) {
            Player = Get<MetaPlayerUpdate>(ctx);
        }

	// Functions used to serialize and deserialize the object
	
	protected override void Read(CelesteNetBinaryReader reader) {
		int count = reader.ReadInt32();
		Logger.Log(LogLevel.Debug, "TeamGames/TeamsList", "Got a teams list with " + count + " players in it");
		for (int i = 0; i < count; ++i) {
			uint playerID = (uint) reader.ReadInt32();
			TeamManager.Team team = (TeamManager.Team) reader.ReadByte();
			PlayerAssignments[playerID] = team;
		}
	}

	protected override void Write(CelesteNetBinaryWriter writer) {
		Dictionary<uint, TeamManager.Team>.KeyCollection keys = PlayerAssignments.Keys;
		writer.Write(keys.Count);
		foreach (uint key in keys) {
			writer.Write(key);
			writer.Write((byte) PlayerAssignments[key]);
		}
	}
}
