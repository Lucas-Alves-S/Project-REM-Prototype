using Godot;
using System;

// Anything the player can interact with. The node needs an InteractionArea child
// so the player can detect it.
public interface IInteractable
{
	void Interact(Player player);
}
