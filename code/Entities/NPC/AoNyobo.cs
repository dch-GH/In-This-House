﻿namespace BrickJam;

public partial class AoNyobo : NPC
{
	public override string ModelPath { get; set; } = "models/nyobo/nyobo.vmdl";
	public override float WalkSpeed { get; set; } = 80f;
	public override float RunSpeed { get; set; } = 300f;

	public AoNyobo() { }
	public AoNyobo( Level level ) : base( level ) { }

	public override void Spawn()
	{
		base.Spawn();
		Scale = 1.1f;

		//PlaySound( "sounds/wega.sound" );
	}


	[ConCmd.Server( "SpawnNPC" )]
	public static void SpawnNPC()
	{
		var caller = ConsoleSystem.Caller.Pawn;

		var npc = new AoNyobo( MansionGame.Instance.CurrentLevel );
		npc.Position = caller.Position;
	}
}
