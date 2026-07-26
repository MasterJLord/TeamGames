
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
public class DataZipTrigger : DataType<DataZipTrigger> {
	
	static DataZipTrigger() {
		DataID = "ZipTrigger";
	}

	public DataPlayerInfo Player;
	public long SentTime;
	public int MoveGroup;
	public bool Toggled;

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
		SentTime = reader.ReadInt64();
		MoveGroup = reader.ReadInt32();
		Toggled = reader.ReadByte() > 0;
	}

	protected override void Write(CelesteNetBinaryWriter writer) {
		writer.Write(SentTime);
		writer.Write(MoveGroup);
		writer.Write((byte) (Toggled ? 1 : 0));
	}
}
