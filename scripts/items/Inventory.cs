using Godot;
using ProjectREM.Characters;

namespace ProjectREM.Items;

// The player's item storage: a fixed number of slots, each empty (null) or holding one stack.
// Data only; InventoryScreen draws it and listens to InventoryChanged.
[GlobalClass]
public partial class Inventory : Node
{
    [Signal]
    public delegate void InventoryChangedEventHandler();

    [Export]
    public int Capacity { get; set; } = 24;

    private ItemStack[] _slots;

    public override void _Ready()
    {
        _slots = new ItemStack[Capacity];
    }

    public ItemStack GetSlot(int index) => _slots[index];

    public int UsedSlots
    {
        get
        {
            int used = 0;
            foreach (ItemStack stack in _slots)
            {
                if (stack != null)
                    used++;
            }
            return used;
        }
    }

    // Tops up existing stacks first, then fills empty slots. All or nothing: returns false and
    // leaves the inventory untouched if the whole amount doesn't fit.
    public bool AddItem(Item item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;
        if (FreeSpaceFor(item) < amount)
            return false;

        for (int i = 0; i < _slots.Length && amount > 0; i++)
        {
            ItemStack stack = _slots[i];
            if (stack == null || stack.Item != item || stack.IsFull())
                continue;

            int moved = Mathf.Min(amount, item.MaxStack - stack.Quantity);
            stack.Quantity += moved;
            amount -= moved;
        }

        for (int i = 0; i < _slots.Length && amount > 0; i++)
        {
            if (_slots[i] != null)
                continue;

            int moved = Mathf.Min(amount, item.MaxStack);
            _slots[i] = new ItemStack(item, moved);
            amount -= moved;
        }

        EmitSignal(SignalName.InventoryChanged);
        return true;
    }

    // All or nothing, like AddItem.
    public bool RemoveItem(Item item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;
        if (CountOf(item) < amount)
            return false;

        for (int i = _slots.Length - 1; i >= 0 && amount > 0; i--)
        {
            ItemStack stack = _slots[i];
            if (stack == null || stack.Item != item)
                continue;

            int taken = Mathf.Min(amount, stack.Quantity);
            stack.Quantity -= taken;
            amount -= taken;
            if (stack.Quantity == 0)
                _slots[i] = null;
        }

        EmitSignal(SignalName.InventoryChanged);
        return true;
    }

    public bool HasItem(Item item) => CountOf(item) > 0;

    // Takes one item out of the slot before using it, so an item that swaps something back in
    // (equipping over an occupied slot) always has room. If Use fails the item goes back.
    public void UseItem(int index, Player player)
    {
        ItemStack stack = _slots[index];
        if (stack == null)
            return;

        Item item = stack.Item;
        stack.Quantity--;
        if (stack.Quantity == 0)
            _slots[index] = null;

        if (!item.Use(player))
        {
            if (_slots[index] == null)
                _slots[index] = new ItemStack(item, 1);
            else
                _slots[index].Quantity++;
        }

        EmitSignal(SignalName.InventoryChanged);
    }

    private int CountOf(Item item)
    {
        int count = 0;
        foreach (ItemStack stack in _slots)
        {
            if (stack != null && stack.Item == item)
                count += stack.Quantity;
        }
        return count;
    }

    private int FreeSpaceFor(Item item)
    {
        int space = 0;
        foreach (ItemStack stack in _slots)
        {
            if (stack == null)
                space += item.MaxStack;
            else if (stack.Item == item)
                space += item.MaxStack - stack.Quantity;
        }
        return space;
    }
}
