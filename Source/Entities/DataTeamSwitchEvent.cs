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

// Uses DataModRec as a template
public class DataTeamSwitchEvent : DataType<DataTeamSwitchEvent> {
	
	static DataTeamSwitchEvent() {
		DataID = "TeamSwitchEvent";
	}

	public DataPlayerInfo Player;
	public uint SwitchingPlayerID;
	public TeamManager.Team NewTeam;

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
        protected override MetaType[] ReadMeta(CelesteNetBinaryReader reader) {
            MetaType[] meta = new MetaType[reader.ReadByte()];
            for (int i = 0; i < meta.Length; i++)
                meta[i] = reader.Data.ReadMeta(reader);
            return meta;
        }
	
	// Functions used to serialize and deserialize the object
	
	protected override void Read(CelesteNetBinaryReader reader) {
		SwitchingPlayerID = (uint) reader.ReadInt32();
		NewTeam = (TeamManager.Team) reader.ReadInt32();
	}

	protected override void Write(CelesteNetBinaryWriter writer) {
		writer.Write(SwitchingPlayerID);
		writer.Write((int) NewTeam);
	}

}
