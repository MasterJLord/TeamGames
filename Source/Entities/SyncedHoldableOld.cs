/*
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using Celeste.Mod.CelesteNet.Client;

namespace Celeste.Mod.practiceMod.Entities;

[TrackedAs(typeof(Holdable))]
public class SyncedHoldableOld : Holdable
{
	protected static CelesteNetClientContext clientContext;
	private static uint? localPlayerID 
	{
		get {
			return clientContext?.Client.PlayerInfo.ID;
		}
	}
	private static Dictionary<int, uint> owners = new();
	public bool IsHeldRemote;

	[MethodImpl(MethodImplOptions.NoInlining)]
	public SyncedHoldableOld(float cannotHoldDelay = 0.1f)
		: base(cannotHoldDelay)
	{
	}

	public SyncedHoldableOld()
		: this(0.1f)
	{
	}

	
	public new bool Pickup(Player player) {
		if (IsHeldRemote) {
			return false;
		}
		bool basePickup = base.Pickup(player);
		if (!basePickup) { return false; }
		ownerID = clientContext?.Client.PlayerInfo.ID;
		SendUpdate();
		return true;
	}
	
	
	public override void Update() {
		if (base.Entity.Scene.OnInterval(0.05f)) {
			SendUpdate();
		}

	}

	public override void Added(Entity entity) {
		base.Added(entity);
		if (!owners.ContainsKey(base.Entity.SourceId.ID)) {
			DataPlayerInfo[] playerList = clientContext.Client.Data.GetRefs<DataPlayerInfo>();
			uint minID = playerList[0].ID;
			foreach (DataPlayerInfo player in playerList) {
				if (player.ID < minID) {
					minID = player.ID;
				}
			}
			owners[base.Entity.SourceId.ID] = minID;
		}
		DataContext data = clientContext.Client.Data;
		data.RegisterHandler<DataSession>(Handle);
	}

	private Vector2 getVelocity() {
		if (Entity is TeamBall) {
			return ((TeamBall) Entity).Speed;
		}
		return Vector2.Zero;
	}

	private void setVelocity(Vector2 velocity) {
		if (Entity is TeamBall) {
			((TeamBall) Entity).Speed = velocity;
		}
	}

	private void SendUpdate() {
		if (owners[base.Entity.SourceId.ID] != localPlayerID) {
			return;
		}
		DataHoldableUpdate data = new DataHoldableUpdate {
			SenderID = (uint) localPlayerID,
			EntityID = base.Entity.SourceId.ID,
			SentTime = Entity.Scene.TimeActive,
			IsHeld = IsHeld,
			Position = Entity.Position,
			Velocity = getVelocity()
		};
		clientContext.Client.Send(data);
	}

	// Function used to get access to the client context
	public static void GetClientContext(CelesteNetClientContext context) {
		clientContext = context;
		Logger.Log(LogLevel.Debug, "practiceMod/TeamManager", "Got client context");
	}

	private void Handle(CelesteNetConnection con, DataSession session) {
		Console.WriteLine(Entity.Scene.TimeActive);
		Console.WriteLine(session.InSession);
		Console.WriteLine(session.Time);
	}

	private void Handle(CelesteNetConnection con, DataHoldableUpdate data) {
		if (base.Entity.SourceId.ID != data.EntityID) {
			return;
		}
		owners[data.EntityID] = data.SenderID;
		IsHeldRemote = data.IsHeld;
		Entity.Position = data.Position;
		setVelocity(data.Velocity);
		((TeamBall) Entity).Move(Entity.Scene.TimeActive - data.SentTime);
		
	}
}
*/
