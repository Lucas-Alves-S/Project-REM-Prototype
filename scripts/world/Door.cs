using System;
using Godot;
using ProjectREM.Characters;
using ProjectREM.Interaction;

namespace ProjectREM.World;

public partial class Door : Node3D, IInteractable
{
	private AnimationPlayer animation_player { get; set; }
	private Boolean _is_open { get; set; }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		animation_player = GetNode<AnimationPlayer>("%AnimationPlayer");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) { }

	public void Interact(Player player)
	{
		if (!animation_player.IsPlaying())
		{
			if (!_is_open)
			{
				animation_player.Play("open");
				_is_open = true;
			}
			else
			{
				animation_player.PlayBackwards("open");
				_is_open = false;
			}
		}
	}
}
