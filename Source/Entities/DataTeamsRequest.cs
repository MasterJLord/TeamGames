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
public class DataTeamsRequest : DataType<DataTeamsRequest> {
	
	static DataTeamsRequest() {
		DataID = "TeamRequest";
	}

	public DataPlayerInfo Player;
	public uint? senderID;

	// Gives this data the MetaPlayerUpdate metadata, which tells the server to broadcast it to all other players when it is sent to the server
	
        public override MetaType[] GenerateMeta(DataContext ctx)
	{
		MetaType[] meta = new MetaType[] {
			new MetaPlayerUpdate(Player)
		};
		return meta;
	}

        public override void FixupMeta(DataContext ctx) 
	{
            Player = Get<MetaPlayerUpdate>(ctx);
        }

        protected override MetaType[] ReadMeta(CelesteNetBinaryReader reader) 
	{
            MetaType[] meta = new MetaType[reader.ReadByte()];
            for (int i = 0; i < meta.Length; i++)
                meta[i] = reader.Data.ReadMeta(reader);
            return meta;
        }

	protected override void Write(CelesteNetBinaryWriter writer) 
	{
		if (senderID == null)
		{
			Logger.Log(LogLevel.Error, "TeamGames/DataTeamsRequest", "Request missing sender id");
		}
		writer.Write((uint) senderID);
	}

	protected override void Read(CelesteNetBinaryReader reader) {
		senderID = (uint) reader.ReadInt32();
	}
}
