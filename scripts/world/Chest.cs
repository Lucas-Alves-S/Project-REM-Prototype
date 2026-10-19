using Godot;
using Godot.Collections;
using ProjectREM.Characters;
using ProjectREM.Interaction;

namespace ProjectREM.World;

// Opens once and hands its contents to a reward screen. Like DialogueArea, what it does comes from
// data: ContentsId picks an entry in ContentsFile, and that entry's "screen" field ("loot" or
// "buffs") decides which screen opens. One chest scene, configured per instance in the inspector.
public partial class Chest : Node3D, IInteractable
{
    [Export]
    public Json ContentsFile { get; set; }

    [Export]
    public string ContentsId { get; set; } = "";

    private static readonly System.Collections.Generic.Dictionary<string, string> Screens = new()
    {
        { "loot", "LootScreen" },
        { "buffs", "BuffScreen" },
    };

    public bool IsOpen { get; private set; }

    private AnimationPlayer _animationPlayer;
    private InteractionArea _interactArea;

    public override void _Ready()
    {
        _animationPlayer = GetNode<AnimationPlayer>("%AnimationPlayer");
        _interactArea = GetNode<InteractionArea>("InteractArea");
    }

    public async void Interact(Player player)
    {
        if (IsOpen)
            return;

        Dictionary contents = LoadContents();
        if (contents == null)
            return;

        IsOpen = true;
        _interactArea.SetDeferred(Area3D.PropertyName.Monitorable, false);

        _animationPlayer.Play("open");
        await ToSignal(_animationPlayer, AnimationMixer.SignalName.AnimationFinished);

        ShowScreen(contents);
    }

    private Dictionary LoadContents()
    {
        if (ContentsFile == null)
        {
            GD.PushError($"{Name}: no contents file given.");
            return null;
        }

        Dictionary chests = ContentsFile.Data.AsGodotDictionary();
        if (!chests.TryGetValue(ContentsId, out Variant contents))
        {
            GD.PushError(
                $"{Name}: contents '{ContentsId}' not found in {ContentsFile.ResourcePath}."
            );
            return null;
        }
        return contents.AsGodotDictionary();
    }

    private void ShowScreen(Dictionary contents)
    {
        string screen = contents["screen"].AsString();
        if (!Screens.TryGetValue(screen, out string autoload))
        {
            GD.PushError($"{Name}: unknown screen '{screen}' in '{ContentsId}'.");
            return;
        }

        Node target = GetNodeOrNull($"/root/{autoload}");
        if (target == null)
        {
            GD.PushWarning(
                $"{Name}: autoload '{autoload}' not found. Contents: {Json.Stringify(contents, "  ")}"
            );
            return;
        }
        target.Call("Open", contents);
    }
}
