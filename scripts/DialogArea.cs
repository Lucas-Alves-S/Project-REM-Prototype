using Godot;
using System;

public partial class DialogArea : Interactable
{
	[Export] public Json DialogueFile { get; set; }
	[Export] public string ConversationId { get; set; } = "";
}
