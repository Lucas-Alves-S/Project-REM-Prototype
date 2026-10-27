using System;
using Godot;
using ProjectREM.Characters;
using ProjectREM.Interaction;
using ProjectREM.UI;

namespace ProjectREM.World;

public partial class DialogueArea : Node3D, IInteractable
{
    [Export]
    public Json DialogueFile { get; set; }

    [Export]
    public string ConversationId { get; set; } = "";

    public void Interact(Player player)
    {
        GetNode<DialogueBox>("/root/DialogueBox").StartDialogue(DialogueFile, ConversationId);
    }
}
