using Godot;
using ProjectREM.Characters;

namespace ProjectREM.Items;

// Anything worn in an equipment slot. Using it from the inventory equips it.
[GlobalClass]
public abstract partial class EquipmentItem : Item
{
	[Export] public EquipmentSlot Slot { get; set; }

	public override bool Use(Player player) => player.Equip(this);
}
