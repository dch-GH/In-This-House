﻿global using Sandbox;
global using System.Linq;
global using System.Threading.Tasks;
global using System.Collections.Generic;
global using System;
global using Sandbox.UI.Construct;
global using System.IO;
using BrickJam.UI;
using BrickJam.VoiceLines;
using Sandbox.Component;
using Sandbox.UI;

namespace BrickJam;

public partial class MansionGame : GameManager
{
	public static MansionGame Instance => (MansionGame)_instance?.Target;
	private static WeakReference _instance;

	[Net] public TimeUntil TimeOut { get; set; }
	[Net] public bool TimerActive { get; set; }

	public float TimePerLevel => 180.0f;

	public MansionGame()
	{
		_instance = new WeakReference( this );

		if ( Game.IsClient )
		{
			InitializeEffects();
			_ = new Hud();
			_ = new VoiceLinePlayer();
		}
	}

	public Transform GetSpawnPoint() => GetSpawnPoint( CurrentLevel.Type );

	public Transform GetSpawnPoint( LevelType level )
	{
		var spawnPoints = Entity.All.OfType<PlayerSpawn>()
			.Where( x => x.LevelType == level )
			.ToList();

		var randomSpawnPoint = Game.Random.FromList( spawnPoints );

		if ( randomSpawnPoint == null )
			return new Transform( Vector3.Zero, Rotation.Identity, 1 );

		return randomSpawnPoint.Transform;
	}

	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );

		var pawn = new Player();
		client.Pawn = pawn;
		pawn.Respawn();
		// TODO: Have pawn dead for now
	}

	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );

		if ( !IsAuthority )
			return;

		SimulateTimer();
	}

	public void TimerStart()
	{
		TimerActive = true;
		TimeOut = TimePerLevel;
	}

	public void TimerStop()
	{
		TimerActive = false;
		TimeOut = 0;
	}

	private void SimulateTimer()
	{
		if ( TimerActive && TimeOut )
		{
			TimerStop();
			RestartLevel();
		}
	}

	public override void FrameSimulate( IClient cl )
	{
		base.FrameSimulate( cl );

		BugBug.Here( v =>
		{
			v.Text( "small fish jam game" );
			v.Value( "time", DateTime.Now );
			v.Space();

			v.Group( "local camera", () =>
			{
				v.Value( "pos", Camera.Position );
				v.Value( "ang", Camera.Rotation.Angles() );
			} );
		} );

		foreach ( var player in All.Where( ent => ent is Player player && player.IsValid() ) )
		{
			var glow = player.Components.GetOrCreate<Glow>();
			glow.Color = Color.Green;
			glow.Enabled = Game.LocalPawn is Spectator;
		}
	}

	public override void RenderHud()
	{
		base.RenderHud();
		Event.Run( "render" );
	}
}
