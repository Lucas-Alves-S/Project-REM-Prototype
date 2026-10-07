using Godot;
using System;

// Detection area the player looks for. Forwards interaction to its parent IInteractable.
public partial class InteractionArea : Area3D
{
	public IInteractable Target { get; private set; }

	// Optional Label3D child named "Prompt", shown while this is the player's interaction target.
	private Label3D _prompt;

	public override void _Ready()
	{
		Target = GetParent() as IInteractable;
		if (Target == null)
			GD.PushWarning($"{GetPath()}: parent does not implement IInteractable.");

		_prompt = GetNodeOrNull<Label3D>("Prompt");
		SetFocused(false);
	}

	public void SetFocused(bool focused)
	{
		if (_prompt != null) _prompt.Visible = focused;
	}
}
