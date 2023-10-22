﻿using Sandbox.Utility;

namespace BrickJam;

public partial class Specter : NPC
{
	public override string ModelPath { get; set; } = "models/specter/specter.vmdl";
	public override float WalkSpeed { get; set; } = 100f;
	public override float RunSpeed { get; set; } = 320f;
	public float TimeToTeleport => 3f; // 4f = 2 seconds to go into ground, (1 second always added to stay underground) 2 seconds to rise up
	public bool IsLowering => LastTeleport <= TimeToTeleport / 2f + 0.5f;
	public bool IsRising => LastTeleport <= TimeToTeleport + 1f && LastTeleport > TimeToTeleport / 2f + 0.5f;
	public bool IsTeleporting => IsLowering || IsRising;
	[Net] public TimeSince LastTeleport { get; set; } = 999f; // screw it
	internal CapsuleLightEntity lampLight { get; set; }

	public Specter() { }
	public Specter( Level level ) : base( level ) { }

	public override void ClientSpawn()
	{
		base.Spawn();

		//PlaySound( "sounds/wega.sound" );

		lampLight = new CapsuleLightEntity();
		lampLight.CapsuleLength = 12f;
		lampLight.LightSize = 12f;
		lampLight.Enabled = true;
		lampLight.Color = new Color( 0.5f, 0.4f, 0.2f );
		lampLight.Range = 512f;
		lampLight.Brightness = 0.4f;
		lampLight.Position = GetAttachment( "lamp" ).Value.Position;
		lampLight.SetParent( this, "lamp" );
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		lampLight?.Delete();
	}

	[GameEvent.Tick.Client]
	void flickerLight()
	{
		if ( lampLight != null )
		{
			if ( !IsTeleporting )
				lampLight.Brightness = Noise.Perlin( Time.Now * 200f, 100f, 1000f );
			else
			{
				if ( IsLowering )
					lampLight.Brightness = Math.Max( MathX.Remap( LastTeleport, 0f, TimeToTeleport / 2f, 0f, -2f ) + Noise.Perlin( Time.Now * 200f, 100f, 1000f ), 0f );

				if ( IsRising )
					lampLight.Brightness = Math.Max( MathX.Remap( LastTeleport, TimeToTeleport / 2f + 1f, TimeToTeleport + 1f, -2f, 0f ) + Noise.Perlin( Time.Now * 200f, 100f, 1000f ), 0f );
			}
		}
	}

	public override void ComputeMotion()
	{
		if ( !IsTeleporting )
			base.ComputeMotion();
		else
		{
			if ( IsLowering )
				Position += Vector3.Down * CollisionHeight * Time.Delta / (TimeToTeleport / 2f);

			if ( IsRising )
				Position += Vector3.Up * CollisionHeight * Time.Delta / (TimeToTeleport / 2f);
		}
	}

	public async void Teleport( Vector3 position )
	{
		LastTeleport = 0;

		await GameTask.Delay( (int)( TimeToTeleport * 500 + 500 ));

		Position = position + Vector3.Down * CollisionHeight + Vector3.Down * CollisionHeight * Time.Delta * 0.5f;

		ResetInterpolation();
	}


	[ConCmd.Server( "Specter" )]
	public static void SpawnNPC()
	{
		var caller = ConsoleSystem.Caller.Pawn;

		var npc = new Specter( MansionGame.Instance.CurrentLevel );
		npc.Position = caller.Position + caller.Rotation.Forward * 300f;
		npc.Rotation = caller.Rotation;
	}

	[ConCmd.Server( "TpToMe" )]
	public static void TeleportSpecter()
	{
		var caller = ConsoleSystem.Caller.Pawn;

		foreach ( var specter in Entity.All.OfType<Specter>().ToList() )
			specter.Teleport( caller.Position);
	}
}
