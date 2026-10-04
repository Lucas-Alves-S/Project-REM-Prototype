using Godot;
using System;

public partial class MainMenu : Control
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GetNode<Button>("%PlayButton").Pressed += OnPlayPressed;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	[Export] public PackedScene GameScene;
	private void OnPlayPressed()
	{
		GetTree().ChangeSceneToPacked(GameScene);
	}
}
