
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;
using Celeste.Mod.CelesteNet;

namespace Celeste.Mod.TeamGames.Entities;

[CustomEntity("TeamGames/SyncedZipMover")]
public class SyncedZipMover : ZipMover
{

	/*
	public static ParticleType P_Scrape;

	public static ParticleType P_Sparks;

	private Themes theme;

	private MTexture[,] edges = new MTexture[3, 3];

	private Sprite streetlight;

	private BloomPoint bloom;

	private ZipMoverPathRenderer pathRenderer;

	private List<MTexture> innerCogs;

	private MTexture temp = new MTexture();

	private bool drawBlackBorder;

	private Vector2 start;

	private Vector2 target;

	private float percent;

	private static Color ropeColor = Calc.HexToColor("663931");

	private static Color ropeLightColor = Calc.HexToColor("9b6157");

	private SoundSource sfx = new SoundSource();
	*/

	public bool Toggle = false;

	protected float remoteTriggerCatchupTime = -10f;

	protected int moveGroup
	{
		get
		{
			if (_moveGroup == 0)
			{
				return base.SourceId.ID;
			}
			return -1 * _moveGroup;

		}
		set
		{
			_moveGroup = value;
		}
	}
	private int _moveGroup;

	private static Dictionary<int, List<SyncedZipMover>> moveGroups = new();


	public SyncedZipMover(EntityData data, Vector2 offset) : base(data, offset)
	{
		Components.RemoveAll<Coroutine>();
		Add(new Coroutine(Sequence()));
		moveGroup = (int) data.Float("group");
		if ((int) data.Float("group") < 0)
		{
			Logger.Log(LogLevel.Warn, "TeamGames/ZipMover", "Group is " + (int) data.Float("group") + " -- this value should only ever be a positive number or 0");
		}
		if (moveGroup < 0)
		{
			if (!moveGroups.ContainsKey(moveGroup))
			{
				moveGroups[moveGroup] = new List<SyncedZipMover>();
			}
			moveGroups[moveGroup].Add(this);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void Added(Scene scene)
	{
		base.Added(scene);
		SyncedHoldable.ClientContext?.Client.Data.RegisterHandlersIn(this);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void Removed(Scene scene)
	{
		base.Removed(scene);
		SyncedHoldable.ClientContext?.Client.Data.UnregisterHandlersIn(this);
		if (moveGroup < 0)
		{
			moveGroups[moveGroup].Remove(this);
		}
	}

	public void Handle(CelesteNetConnection con, DataZipTrigger data)
	{
		if (moveGroup != data.MoveGroup)
		{
			return;
		}
		if (SyncedHoldable.ServerTime < data.SentTime)
		{
			Logger.Log(LogLevel.Warn, "TeamGames/SyncedZipMover", "Received message from the future; time paradox imminent! (sent at T=" + data.SentTime + "; received at T=" + SyncedHoldable.ServerTime);
		} else {
			// Divides by 1e7 to convert from hundred-nanoseconds to seconds
			Logger.Log(LogLevel.Debug, "TeamGames/ZipMover", "Received message from " + (SyncedHoldable.ServerTime - data.SentTime) / 1e7f + " seconds ago");
			remoteTriggerCatchupTime = (SyncedHoldable.ServerTime - data.SentTime) / 1e7f;
		}
	}


	private IEnumerator Sequence()
	{
		Vector2 start = Position;
		while (true)
		{
			if (!HasPlayerRider() && remoteTriggerCatchupTime < 0)
			{
				yield return null;
				continue;
			}
			Logger.Log(LogLevel.Debug, "TeamGames/ZipMover", "Zip mover triggered");
			// Update everyone else about the trigger if this zip mover was not itself being triggered remotely
			if (remoteTriggerCatchupTime < 0)
			{
				// Trigger other zip movers remotely
				Logger.Log(LogLevel.Debug, "TeamGames/ZipMover", "Sending message");
				DataZipTrigger data = new DataZipTrigger {
					SentTime = SyncedHoldable.ServerTime,
					MoveGroup = moveGroup
				};
				SyncedHoldable.ClientContext?.Client.Send(data);

				// Trigger other zip movers locally
				if (moveGroup < 0)
				{
					foreach (SyncedZipMover other in moveGroups[moveGroup])
					{
						other.remoteTriggerCatchupTime = 0f;
					}
				}
			}

			if (remoteTriggerCatchupTime < 0.1f)
			{
				Logger.Log(LogLevel.Debug, "TeamGames/ZipMover", "Warming up for " + remoteTriggerCatchupTime);
				sfx.Play((theme == Themes.Normal) ? "event:/game/01_forsaken_city/zip_mover" : "event:/new_content/game/10_farewell/zip_mover");
				Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
				StartShaking(0.1f - Math.Max(remoteTriggerCatchupTime, 0f));
				yield return 0.1f - Math.Max(remoteTriggerCatchupTime, 0f);
			}
			Calc.Approach(remoteTriggerCatchupTime, 0f, 0.1f);
			Logger.Log(LogLevel.Debug, "TeamGames/ZipMover", "Catchup time remaining: " + remoteTriggerCatchupTime);
			
			streetlight.SetAnimationFrame(3);
			StopPlayerRunIntoAnimation = false;
			float at = Math.Clamp(remoteTriggerCatchupTime, 0f, 1f);
			do
			{
				yield return null;
				at = Calc.Approach(at, 1f, 2f * Engine.DeltaTime);
				percent = Ease.SineIn(at);
				Vector2 vector = Vector2.Lerp(start, target, percent);
				ScrapeParticlesCheck(vector);
				if (Scene.OnInterval(0.1f))
				{
					pathRenderer.CreateSparks();
				}
				MoveTo(vector);
			} while (at < 1f);
			Calc.Approach(remoteTriggerCatchupTime, 0f, 1f);

			if (remoteTriggerCatchupTime < 0.2f)
			{
				StartShaking(0.2f - Math.Max(remoteTriggerCatchupTime, 0f));
				Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
				SceneAs<Level>().Shake();
				yield return 0.2f - Math.Max(remoteTriggerCatchupTime, 0f);
			}
			Calc.Approach(remoteTriggerCatchupTime, 0f, 0.2f);

			if (remoteTriggerCatchupTime < 0.3f)
			{
				StopPlayerRunIntoAnimation = true;
				yield return 0.3f - Math.Max(remoteTriggerCatchupTime, 0f);
				StopPlayerRunIntoAnimation = false;
			}
			Calc.Approach(remoteTriggerCatchupTime, 0f, 0.3f);

			if (!Toggle)
			{
				streetlight.SetAnimationFrame(2);
				at = Math.Clamp(remoteTriggerCatchupTime, 0f, 1f);
				do
				{
					yield return null;
					at = Calc.Approach(at, 1f, 0.5f * Engine.DeltaTime);
					percent = 1f - Ease.SineIn(at);
					Vector2 position = Vector2.Lerp(target, start, Ease.SineIn(at));
					MoveTo(position);
				}
				while (at < 1f);
				StopPlayerRunIntoAnimation = true;
				StartShaking(0.2f);
				streetlight.SetAnimationFrame(1);
			}
			Calc.Approach(remoteTriggerCatchupTime, 0f, 1f);

			yield return Math.Max(0.5f - Math.Max(remoteTriggerCatchupTime, 0), 0f);
			remoteTriggerCatchupTime = -10f;
		}
	}

}
