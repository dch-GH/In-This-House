﻿using GridAStar;
using Sandbox;

namespace BrickJam;

public partial class NPC : AnimatedEntity
{
	public Level Level { get; private set; }
	internal TimeUntil nextIdle { get; set; } = 0f;
	public virtual string ModelPath { get; set; } = "models/citizen/citizen.vmdl";
	public virtual float CollisionRadius { get; set; } = 8f;
	public virtual float CollisionHeight { get; set; } = 72f;
	public Capsule CollisionCapsule => new Capsule( Vector3.Up * CollisionRadius, Vector3.Up * (CollisionHeight - CollisionRadius), CollisionRadius );
	public virtual float MaxVisionRange { get; set; } = 1024f;
	public virtual float MaxVisionAngle { get; set; } = 120f; // Degrees
	public virtual float MaxVisionAngleWhenChasing { get; set; } = 180f;
	public virtual float MaxVisionRangeWhenChasing { get; set; } = 2048f;
	public virtual float MaxRememberTime { get; set; } = 2f; // How long will it keep tracking your position when you're out of sight (This lets them get into rooms or corridors you hide in)
	public virtual float AttackAnimationDuration { get; set; } = 1.5f;
	public virtual float KillAfterAttackTime { get; set; } = 1f;
	public virtual float SetDistanceWhenAttacking { get; set; } = 50f;
	public Dictionary<Player, TimeSince> PlayersInVision { get; private set; } = new();
	public Player Target { get; set; } = null;
	public Player LastTarget { get; set; } = null;
	public Player CurrentlyMurdering { get; set; } = null;

	public NPC() { }
	public NPC( Level level ) : base()
	{
		Level = level;
	}

	public override void Spawn()
	{
		base.Spawn();

		SetModel( ModelPath );
		SetupPhysicsFromOrientedCapsule( PhysicsMotionType.Keyframed, CollisionCapsule );
		Tags.Add( "npc" );
	}

	[GameEvent.Tick.Server]
	public virtual void Think()
	{
		FindTargets();

		if ( PlayersInVision.Count > 0 )
		{
			Target = PlayersInVision.OrderBy( x => x.Key.Position.Distance( Position ) )
				.FirstOrDefault().Key;
			LastTarget = Target;

			nextIdle = MansionGame.Random.Float( 3f, 6f );
		}
		else
			Target = null;

		if ( Target == null )
		{
			if ( nextIdle && !IsFollowingPath )
			{
				var randomPosition = Level.WorldBox.RandomPointInside;
				var targetCell = Level.Grid?.GetCell( randomPosition, false ) ?? null;

				if ( targetCell != null )
				{
					NavigateTo( targetCell );
					nextIdle = MansionGame.Random.Float( 3f, 6f );
				}

				LastTarget = null;
			}
		}

		if ( Target != null ) // Kill player is in range
			if ( Target.Position.Distance( Position ) <= 40f )
				CatchPlayer( Target );

		if ( IsFollowingPath )
			foreach ( var door in Entity.All.OfType<Door>() )
			{
				if ( door.Position.Distance( Position ) <= 60f )
				if ( door.State == DoorState.Closed )
					door.State = DoorState.Open;
			}

		ComputeOpenDoors();
		ComputeNavigation();
		ComputeMotion();
	}

	public virtual void ComputeOpenDoors()
	{
		var startPos = Position + Vector3.Up * CollisionCapsule.CenterB.z;
		var endPos = startPos + Rotation.Forward * 60;
		var doorTrace = Trace.Ray( startPos, endPos )
			.Size( 20f )
			.DynamicOnly()
			.WithAnyTags( "door" )
			.Ignore( this )
			.Run();

		if ( doorTrace.Hit )
			if ( doorTrace.Entity is Door door )
			{
				if ( door.State == DoorState.Closed || door.State == DoorState.Closing )
				{
					door.Open( this );

					GameTask.RunInThreadAsync( async () =>
					{
						await GameTask.Delay( 3000 );
						door.Close();
					} );
				}
			}
	}

	public async virtual void CatchPlayer( Player player )
	{
		SetAnimParameter( "attack", true );

		var currentDirection = (player.Position - Position).Normal;
		player.Position = Position + currentDirection * SetDistanceWhenAttacking;
		player.CameraTarget = this;
		player.Blocked = true;

		CurrentlyMurdering = player;
		Blocked = true;

		await GameTask.DelaySeconds( KillAfterAttackTime );

		player.Kill();

		await GameTask.DelaySeconds( AttackAnimationDuration );

		player.CameraTarget = null;
		player.Blocked = false;

		CurrentlyMurdering = null;
		Blocked = false;
	}

	[GameEvent.Tick]
	public virtual void ComputeAnimations()
	{
		SetAnimParameter( "move_x", MathX.Remap( Velocity.Length, 0f, RunSpeed, 0, 3 ) );
	}

	public virtual void FindTargets()
	{
		// Remove all the invalid players
		foreach (var player in PlayersInVision.Where( x => !x.Key.IsValid()).ToList()) // Make a copy of PlayersInVision
			PlayersInVision.Remove( player.Key );

		// Remove all dead players
		foreach ( var player in PlayersInVision.Where( x => !x.Key.IsAlive ).ToList() ) // Make a copy of PlayersInVision
			PlayersInVision.Remove( player.Key );

		foreach ( var player in Entity.All.OfType<Player>() )
		{
			if ( IsPlayerInVision( player ) )
			{
				if ( !PlayersInVision.ContainsKey( player ) )
					PlayersInVision.Add( player, 0f );
				else
					PlayersInVision[ player ] = 0f;
			}
			else
				if ( PlayersInVision.ContainsKey( player ) )
					if ( PlayersInVision[player] >= MaxRememberTime ) // Stops following if lost sight after 1 second (This means it will follow you into rooms or behind corridors)
						PlayersInVision.Remove( player );
		}
	}

	public virtual bool IsPlayerInVision( Player player )
	{
		var visionRange = player == Target ? MaxVisionRangeWhenChasing : MaxVisionRange;
		var visionAngle = player == Target ? MaxVisionAngleWhenChasing : MaxVisionAngle;

		if ( player.Position.Distance( Position ) >= visionRange ) return false;

		var relativePosition = Transform.PointToLocal( player.Position );
		var angle = Vector3.GetAngle( relativePosition, Vector3.Forward );

		if ( angle > visionAngle / 2f || angle < -visionAngle / 2f ) return false;

		var losTrace = Trace.Ray( Position + Vector3.Up * CollisionCapsule.CenterB.z, player.EyePosition )
			.Ignore( this )
			.Ignore( player )
			.Run();

		if ( losTrace.Hit ) return false;

		return true;
	}
}
