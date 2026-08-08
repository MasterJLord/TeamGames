
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.Client;

namespace Celeste.Mod.TeamGames.Entities;

[CustomEntity("TeamGames/SyncedZipMover")]
public class SyncedZipMover : ZipMover
{
	public enum MyThemes
	{
		Normal,
		Moon,
		Brass
	}

	private static Dictionary<MyThemes, Themes> enumReduction = new() {
		[MyThemes.Normal] = Themes.Normal,
		[MyThemes.Brass] = Themes.Normal,
		[MyThemes.Moon] = Themes.Moon
	};

	private MyThemes myTheme;

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

	protected long triggerTime = 0;

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

	private static Dictionary<int, bool> toggledGroups = new();

	public SyncedZipMover(Vector2 position, int width, int height, Vector2 target, MyThemes theme, bool toggle) : base(position, width, height, target, enumReduction[theme])
	{
		this.myTheme = theme;
		Toggle = toggle;
		// Replace base initialization with this
		Components.RemoveAll<Coroutine>();
		Add(new Coroutine(SyncedSequence()));
		string path;
		string id;
		string key;
		switch (theme)
		{
			case MyThemes.Moon:
				path = "objects/zipmover/moon/light";
				id = "objects/zipmover/moon/block";
				key = "objects/zipmover/moon/innercog";
				drawBlackBorder = false;
				break;
			case MyThemes.Brass:
				path = "objects/TeamGames/brassZip/light";
				id = "objects/TeamGames/brassZip/block";
				key = "objects/TeamGames/brassZip/innercog";
				drawBlackBorder = true;
				break;
			default:
				path = "objects/zipmover/light";
				id = "objects/zipmover/block";
				key = "objects/zipmover/innercog";
				drawBlackBorder = true;
				break;
		}
		innerCogs = GFX.Game.GetAtlasSubtextures(key);
		Remove(streetlight);
		Add(streetlight = new Sprite(GFX.Game, path));
		streetlight.Add("frames", "", 1f);
		streetlight.Play("frames");
		streetlight.Active = false;
		streetlight.SetAnimationFrame(1);
		streetlight.Position = new Vector2(base.Width / 2f - streetlight.Width / 2f, 0f);
		Add(bloom = new BloomPoint(1f, 6f));
		bloom.Position = new Vector2(base.Width / 2f, 4f);
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				edges[i, j] = GFX.Game[id].GetSubtexture(i * 8, j * 8, 8, 8);
			}
		}
		SurfaceSoundIndex = 7;
		sfx.Position = new Vector2(base.Width, base.Height) / 2f;
		Add(sfx);
	}


	public SyncedZipMover(EntityData data, Vector2 offset) : this(data.Position + offset, data.Width, data.Height, data.Nodes[0] + offset, data.Enum("theme", MyThemes.Normal), data.Bool("toggle"))
	{
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
		// Remain toggled after dying
		if (Toggle && toggledGroups.ContainsKey(moveGroup) && toggledGroups[moveGroup])
		{
			toggleAnchors();
			Position = start;
		}
		// SyncedHoldable.ClientContext?.Client.Data.RegisterHandlersIn(this);
	}


	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void Removed(Scene scene)
	{
		base.Removed(scene);
		// SyncedHoldable.ClientContext?.Client.Data.UnregisterHandlersIn(this);
		if (moveGroup < 0)
		{
			moveGroups[moveGroup].Remove(this);
		}
	}

	/*
	public void Handle(CelesteNetConnection con, DataZipTrigger data)
	{
		if (moveGroup != data.MoveGroup)
		{
			return;
		}
		if (SyncedHoldable.ServerTime < data.SentTime)
		{
			Logger.Log(LogLevel.Warn, "TeamGames/SyncedZipMover", "Received message from the future; time paradox imminent! (sent at T=" + data.SentTime + "; received at T=" + SyncedHoldable.ServerTime);
			triggerTime = data.SentTime;
		} else {
			triggerTime = data.SentTime;
		}
	}
	*/

	public static void GetClientContext(CelesteNetClientContext context)
	{
		context.Client.Data.RegisterHandler<DataZipTrigger>(Handle);
	}
 	
	public static void OnExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow)
	{
		toggledGroups = new();
	}

	public static void Handle(CelesteNetConnection con, DataZipTrigger data)
	{
		if ((toggledGroups.ContainsKey(data.MoveGroup) && toggledGroups[data.MoveGroup]) == data.Toggled && moveGroups.ContainsKey(data.MoveGroup))
		{
			foreach(SyncedZipMover mover in moveGroups[data.MoveGroup])
			{
				if (mover.Toggle)
				{
					return;
				}
			}
		}
		toggledGroups[data.MoveGroup] = data.Toggled;
		foreach (SyncedZipMover mover in moveGroups[data.MoveGroup])
		{
			mover.triggerTime = data.SentTime;
		}
	}


	private IEnumerator SyncedSequence()
	{
		while (true)
		{
			if (!HasPlayerRider() && triggerTime == 0)
			{
				yield return null;
				continue;
			}
			// Update everyone else about the trigger if this zip mover was not itself being triggered remotely
			float remoteTriggerCatchupTime = 0;
			if (triggerTime == 0)
			{
				toggledGroups[moveGroup] = !(toggledGroups.ContainsKey(moveGroup) && toggledGroups[moveGroup]);
			Logger.Log(LogLevel.Debug, "TeamGames/ZipMover", "" + moveGroup + " is " + toggledGroups[moveGroup]);
				// Trigger other zip movers remotely
				DataZipTrigger data = new DataZipTrigger {
					SentTime = SyncedHoldable.ServerTime,
					MoveGroup = moveGroup,
					Toggled = toggledGroups[moveGroup]
				};
				SyncedHoldable.ClientContext?.Client.Send(data);

				// Trigger other zip movers locally
				if (moveGroup < 0)
				{
					foreach (SyncedZipMover other in moveGroups[moveGroup])
					{
						if (other == this)
						{
							continue;
						}
						other.triggerTime = SyncedHoldable.ServerTime;
					}
				}
			} else {
				// Divides by 1e7 to convert from hundred-nanoseconds to seconds
				remoteTriggerCatchupTime = (SyncedHoldable.ServerTime - triggerTime) / 1e7f;
			}
			triggerTime = 0;

			if (remoteTriggerCatchupTime < 0.1f)
			{
				sfx.Play((theme == Themes.Normal) ? "event:/game/01_forsaken_city/zip_mover" : "event:/new_content/game/10_farewell/zip_mover");
				Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
				StartShaking(0.1f - Math.Max(remoteTriggerCatchupTime, 0f));
				yield return 0.1f - Math.Max(remoteTriggerCatchupTime, 0f);
			}
			Calc.Approach(remoteTriggerCatchupTime, 0f, 0.1f);
			
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

			if (Toggle)
			{
				toggleAnchors();
			} else {
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
		}
	}

	private void toggleAnchors()
	{
		Vector2 swap = start;
		start = target;
		target = swap;
	}

}
