using Godot;
using System;

public partial class DialogArea : Node3D, IInteractable
{
	[Export] public Json DialogueFile { get; set; }
	[Export] public string ConversationId { get; set; } = "";

	public void Interact(Player player)
	{
		GetNode<DialogueBox>("/root/DialogueBox").StartDialogue(DialogueFile, ConversationId);
	}
}
