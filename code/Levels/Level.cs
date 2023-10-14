﻿namespace BrickJam;

public enum LevelType
{
	None,
	Mansion,
	Dungeon,
	Office,
	Library
}

public abstract partial class Level : Entity // Easy replication to client
{
	public Level() => Transmit = TransmitType.Always;
	[Net] public Trapdoor Trapdoor { get; set; } = null;
	[Net] public TimeSince SinceStarted { get; set; } = 0f;

	[GameEvent.Tick.Server]
	public virtual void Compute() { }

	public virtual async Task Start()
	{
		await GameTask.NextPhysicsFrame();
		return;
	}
	public virtual async Task End()
	{
		await GameTask.NextPhysicsFrame();
		return;
	}
}

public partial class Mansion
{
	[Net, Change] public Level CurrentLevel { get; set; }

	public static async void SetLevel<T>() where T : Level
	{
		if ( !Game.IsServer ) return;

		if ( Instance.CurrentLevel != null )
		{
			await Instance.CurrentLevel?.End();
			Instance.CurrentLevel?.Delete();
		}

		Instance.CurrentLevel = Activator.CreateInstance<T>();
		await Instance.CurrentLevel.Start();
	}

	public static void NextLevel()
	{
		if ( Instance.CurrentLevel is MansionLevel )
			SetLevel<DungeonLevel>();
		else if ( Instance.CurrentLevel is DungeonLevel )
			SetLevel<OfficeLevel>();
		else if ( Instance.CurrentLevel is OfficeLevel )
			SetLevel<LibraryLevel>();
	}

	public void OnCurrentLevelChanged()
	{
	}

	[GameEvent.Entity.PostSpawn]
	static void startLevels()
	{
		SetLevel<MansionLevel>();
	}
}
