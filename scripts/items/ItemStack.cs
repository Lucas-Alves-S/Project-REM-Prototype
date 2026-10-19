using Godot;

namespace ProjectREM.Items;

// An item and how many of it a slot holds. A Resource so stacks can be set up in the inspector
// (e.g. Player.StartingItems).
[GlobalClass]
public partial class ItemStack : Resource
{
	[Export] public Item Item { get; set; }
	[Export] public int Quantity { get; set; } = 1;

	public ItemStack() { }

	public ItemStack(Item item, int quantity)
	{
		Item = item;
		Quantity = quantity;
	}

	public bool IsFull() => Item != null && Quantity >= Item.MaxStack;
}
