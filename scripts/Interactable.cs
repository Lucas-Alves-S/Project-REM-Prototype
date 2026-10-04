using Godot;
using System;

public partial class Interactable : Area3D
{
	// Optional Label3D child named "Prompt", shown while this is the player's interaction target.
	private Label3D _prompt;

	public override void _Ready()
	{
		_prompt = GetNodeOrNull<Label3D>("Prompt");
		SetFocused(false);
	}

	public void SetFocused(bool focused)
	{
		if (_prompt != null) _prompt.Visible = focused;
	}

	public virtual void Interact(Player player) {}
}
