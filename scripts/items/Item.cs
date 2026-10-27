using Godot;
using ProjectREM.Characters;

namespace ProjectREM.Items;

// Base for everything that can sit in an inventory slot. Items are Resources, so each item is a
// .tres file under data/items/ and every stack holding it shares the same instance.
[GlobalClass]
public abstract partial class Item : Resource
{
    [Export]
    public string Id { get; set; } = "";

    [Export]
    public string DisplayName { get; set; } = "";

    [Export(PropertyHint.MultilineText)]
    public string Description { get; set; } = "";

    [Export]
    public Texture2D Icon { get; set; }

    [Export]
    public int MaxStack { get; set; } = 1;

    // Returns true if the item was consumed (or moved out of the inventory, like equipment).
    public virtual bool Use(Player player) => false;
}
