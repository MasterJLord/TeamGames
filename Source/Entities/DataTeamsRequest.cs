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
public class DataTeamsRequest : DataType<DataTeamsRequest> {
	
	static DataTeamsRequest() {
		DataID = "TeamSwitchEvent";
	}

	public DataPlayerInfo Player;

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
}
