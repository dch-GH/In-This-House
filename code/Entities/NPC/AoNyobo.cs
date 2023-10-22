﻿namespace BrickJam;

public partial class AoNyobo : NPC
{
	public override string ModelPath { get; set; } = "models/nyobo/nyobo.vmdl";
	public override float WalkSpeed { get; set; } = 120f;
	public override float RunSpeed { get; set; } = 380f;

	public AoNyobo() { }
	public AoNyobo( Level level ) : base( level ) { }

	public override void Spawn()
	{
		base.Spawn();
		Scale = 1.1f;

		//PlaySound( "sounds/wega.sound" );
	}


	[ConCmd.Server( "AoNyobo" )]
	public static void SpawnNPC()
	{
		var caller = ConsoleSystem.Caller.Pawn;

		var npc = new AoNyobo( MansionGame.Instance.CurrentLevel );
		npc.Position = caller.Position + caller.Rotation.Forward * 300f;
		npc.Rotation = caller.Rotation;
	}
}
