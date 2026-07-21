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

public class DataHoldableUpdate : DataType<DataHoldableUpdate> {
	
	static DataHoldableUpdate() {
		DataID = "TeamHoldable";
	}

	public DataPlayerInfo Player;
	public uint SenderID;
	public int EntityID;
	public long SentTime;
	public bool IsHeld;
	public Vector2 Position;
	public Vector2 Velocity;

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
		SenderID = (uint) reader.ReadInt32();
		EntityID = reader.ReadInt32();
		SentTime = reader.ReadInt64();
		Position = reader.ReadVector2();
		Velocity = reader.ReadVector2();
		IsHeld = reader.ReadByte() > 0;
	}

	protected override void Write(CelesteNetBinaryWriter writer) {
		writer.Write(SenderID);
		writer.Write(EntityID);
		writer.Write(SentTime);
		writer.Write(Position);
		writer.Write(Velocity);
		writer.Write((byte) (IsHeld ? 1 : 0));
	}

}
