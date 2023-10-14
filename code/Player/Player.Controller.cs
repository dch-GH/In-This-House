﻿namespace BrickJam;

public partial class Player
{
	[Net] public float WalkSpeed { get; set; } = 200f;
	[Net] public float RunSpeed { get; set; } = 350f;
	public float StunSpeed => (float)(WalkSpeed + (RunSpeed - WalkSpeed) * Math.Sin( 45f.DegreeToRadian() ));

	/// <summary>
	/// Acceleration speed in units per second (Ex. 200f means that after 1 second you've reached 200f speed)
	/// </summary>
	[Net]
	public float AccelerationSpeed { get; set; } =
		400f;

	[Net, Predicted] public float WishSpeed { get; private set; } = 0f;

	public float StepSize => 16f;
	public float WalkAngle => 46f;
	public float StunBounceVelocity => 100f;

	protected void SimulateController()
	{
		WishSpeed = IsRunning ? RunSpeed : WalkSpeed;

		var oldVelocity = Velocity;
		var wishVelocity = (InputDirection.IsNearlyZero() || IsStunned
			? Vector3.Zero
			: InputDirection.Normal * Rotation.FromYaw( InputAngles.yaw )) * WishSpeed;

		var lerpVelocity = Vector3.Lerp( Velocity, wishVelocity,
				(wishVelocity.LengthSquared > Velocity.LengthSquared ? 15f : 5f) // Accelerate faster than decelerate
				* Time.Delta )
			.WithZ( Velocity.z );

		var helper = new MoveHelper( Position, lerpVelocity );
		helper.MaxStandableAngle = WalkAngle;

		helper.Trace = helper.Trace
			.Size( CollisionBox.Mins, CollisionBox.Maxs )
			.WithoutTags( "player" )
			.Ignore( this );

		helper.TryMoveWithStep( Time.Delta, StepSize );
		helper.TryUnstuck();

		Position = helper.Position;
		Velocity = helper.Velocity;

		// If:
		// - the player is running
		// - the velocity dropped from more than WalkSpeed to near zero
		// - there is a wall in the direction of movement
		// then the pawn has probably ran into a wall
		if ( !lerpVelocity.WithZ( 0 ).IsNearZeroLength
		     && IsRunning
		     && oldVelocity.WithZ( 0 ).Length > WalkSpeed
		     && helper.Velocity.WithZ( 0 ).Length < StunSpeed
		   )
		{
			// Making the collision box a little bit shorter to prevent small items from triggering a concussion
			var tr = helper.Trace
				.Size( CollisionBox.Mins + Vector3.Up * StepSize, CollisionBox.Maxs )
				.FromTo( helper.Position, helper.Position + lerpVelocity.WithZ( 0 ).Normal ).Run();

			if ( tr.Hit )
			{
				Stun();

				Velocity += tr.Normal * (CollisionBox.Size.WithZ( 0 ).Length + StunBounceVelocity);
			}
		}

		var traceDown = helper.TraceDirection( Vector3.Down );

		if ( traceDown.Entity != null )
		{
			GroundEntity = traceDown.Entity;
			Position = traceDown.EndPosition;
		}
		else
		{
			GroundEntity = null;
			Velocity -= Vector3.Down * Game.PhysicsWorld.Gravity * Time.Delta;
		}

		if ( Input.Down( "jump" ) && !IsStunned )
			if ( GroundEntity != null )
			{
				GroundEntity = null;
				Velocity += Vector3.Up * 300f;
			}
	}
}
