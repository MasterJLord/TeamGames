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

	public uint SwitchingPlayerID;
	public TeamManager.Team NewTeam;

	protected override void Read(CelesteNetBinaryReader reader) {
		SwitchingPlayerID = reader.ReadByte();
		NewTeam = (TeamManager.Team) reader.ReadByte();
	}

	protected override void Write(CelesteNetBinaryWriter writer) {
		writer.Write(SwitchingPlayerID);
		writer.Write((int) NewTeam);
	}

	// looks like this is being handled already by CelesteNetClientModule.GetTypes()? Not sure if the order of operations shakes out right for that function to handle DataTypes which are loaded after the CelesteNet module itself
	/*
	// Ensures that the DataContext being used will be registered in the DataContext's IDToDataType dictionary 
	public void Load() {
		CelesteNetClientContext.OnCreate += GetClientContext;
	}

	private void GetClientContext(CelesteNetClientContext context) {
		context.Client.Data.RescanDataTypes(new Type[] {typeof(this)});
	}
	*/
}
