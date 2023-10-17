﻿using BrickJam.UI;

namespace BrickJam;

public enum LevelType // We need this to categorize hammer entities
{
	None,
	Mansion,
	Dungeon,
	Bathrooms
}

public abstract partial class Level : Entity // Easy replication to client
{
	public virtual LevelType Type { get; set; } = LevelType.None;
	[Net] public Trapdoor Trapdoor { get; set; } = null;
	[Net] public NPC Monster { get; set; } = null;
	[Net] public TimeSince SinceStarted { get; set; } = 0f;

	public Level() => Transmit = TransmitType.Always;

	[GameEvent.Tick.Server]
	public virtual void Compute() { }

	public virtual async Task Start()
	{
		await GameTask.NextPhysicsFrame();

		foreach ( var player in Entity.All.OfType<Player>() )
			player.Respawn();

		var allValidTrapdoors = Entity.All.OfType<ValidTrapdoorPosition>()
			.Where( x => x.LevelType == Type )
			.ToList();
		var randomValidTrapdoor = Game.Random.FromList( allValidTrapdoors );

		Trapdoor = new Trapdoor();
		Trapdoor.Position = randomValidTrapdoor.Position;

		foreach ( var spawner in Entity.All.OfType<LootSpawner>().Where( x => x.Level == Type ).ToList() )
		{
			spawner.SpawnLoot();
		}

		await GenerateGrid();

		return;
	}

	public virtual async Task End()
	{
		await GameTask.NextPhysicsFrame();

		BlackScreen.Start( To.Everyone, 2f, 1f, 1f );
		await GameTask.DelaySeconds( 1f ); // Wait for the black screen to be fully black

		Trapdoor?.Delete();
		Monster?.Delete();

		foreach ( var spawner in Entity.All.OfType<LootSpawner>().Where( x => x.Level == Type ) )
			spawner.DeleteLoot();

		return;
	}

	public static Type GetType( LevelType type )
	{
		switch ( type )
		{
			case LevelType.None:
				return null;
			case LevelType.Mansion:
				return typeof(MansionLevel);
			case LevelType.Dungeon:
				return typeof( DungeonLevel );
			case LevelType.Bathrooms:
				return typeof( BathroomsLevel );
			default:
				return null;
		}
	}

	public static Type GetNextType( LevelType type ) => GetType( type + 1 );
	public static Type GetPreviousType( LevelType type ) => GetType( type - 1 );
}

public partial class MansionGame
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

		Instance.TimerStart();
	}

	public static void NextLevel()
	{
		if ( Instance.CurrentLevel is MansionLevel )
			SetLevel<DungeonLevel>();
		else if ( Instance.CurrentLevel is DungeonLevel )
			SetLevel<BathroomsLevel>();
	}

	// rndtrash: oh my god i hate it so much
	public static void RestartLevel()
	{
		if ( Instance.CurrentLevel is MansionLevel )
			SetLevel<MansionLevel>();
		else if ( Instance.CurrentLevel is DungeonLevel )
			SetLevel<DungeonLevel>();
		else if (Instance.CurrentLevel is BathroomsLevel)
			SetLevel<BathroomsLevel>();
		else
			Log.Error( $"Don't know how to restart the level {Instance.CurrentLevel.GetType()}" );
	}

	// For client callback
	public void OnCurrentLevelChanged()
	{
	}

	[GameEvent.Entity.PostSpawn]
	static void startLevels()
	{
		SetLevel<MansionLevel>();
	}
}
