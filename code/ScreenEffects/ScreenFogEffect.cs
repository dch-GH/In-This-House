﻿namespace BrickJam;

public class ScreenFogEffect : RenderHook
{
	public Color Color { get; set; } = new Color( 0.5f ).WithAlpha( 1f );
	public float MaximumDistance { get; set; } = 6000f;
	public float MinimumDistance { get; set; } = 50f;
	public float MaxOpacity { get; set; } = 0.7f;

	public override void OnStage( SceneCamera target, Stage stage )
	{
		if ( stage != Stage.AfterTransparent )
			return;

		var mat = Material.FromShader( "shaders/screenfog.shader" );

		var attributes = new RenderAttributes();
		attributes.Set( "Color", Color );
		attributes.Set( "MinDistance", MinimumDistance );
		attributes.Set( "MaxDistance", MaximumDistance );
		attributes.Set( "Opacity", MaxOpacity );

		Graphics.GrabFrameTexture( renderAttributes: attributes );
		Graphics.GrabDepthTexture( renderAttributes: attributes );

		Graphics.Blit( mat, attributes );
	}
}
