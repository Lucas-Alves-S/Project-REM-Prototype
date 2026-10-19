using System;
using System.Collections.Generic;
using Godot;
using ProjectREM.Characters;
using ProjectREM.Items;

namespace ProjectREM.UI;

// Inventory screen (docs/ui-mockups/Inventory.dc.html), toggled with the "inventory" action (E).
// Draws the player's Inventory and equipment; pressing a storage slot uses its item (equipment
// gets equipped), pressing an equip slot unequips it. The hotbar is still layout only.
// Runs with ProcessMode = Always because it pauses the tree while open, like DialogueBox.
public partial class InventoryScreen : CanvasLayer
{
	private const int HotbarSize = 8;

	// Equip slot buttons carry a "slot_name" meta; CLOAK is where body armor goes.
	private static readonly Dictionary<string, EquipmentSlot> EquipSlotNames = new()
	{
		{ "HEAD", EquipmentSlot.Head },
		{ "CLOAK", EquipmentSlot.Body },
		{ "MAGIC TOOL", EquipmentSlot.MagicTool },
		{ "AMULET", EquipmentSlot.Amulet },
	};
	private static readonly Vector2 SlotSize = new(104, 104);

	// Panel sweep: 320ms cubic-bezier(.7,0,.2,1).
	private const double OpenTime = 0.32;
	private const double CloseTime = 0.16;
	private const float SlideDistance = 120.0f;

	private Control _root;
	private Control _sweep;
	private GridContainer _storageGrid;
	private HBoxContainer _hotbar;
	private Label _tipName;
	private Label _tipKind;
	private Label _tipLore;
	private Label _tipUse;

	private bool _open;
	private Tween _tween;

	private Player _player;
	private readonly List<Button> _storageSlots = new();
	private readonly Dictionary<EquipmentSlot, Button> _equipSlots = new();

	// What the tooltip is showing, so it can be redrawn when the focused slot's item changes.
	private string _tipSlotKind;
	private Func<Item> _tipItemAt;
	private string _tipAction;

	public override void _Ready()
	{
		_root = GetNode<Control>("%Root");
		_sweep = GetNode<Control>("%Sweep");
		_storageGrid = GetNode<GridContainer>("%StorageGrid");
		_hotbar = GetNode<HBoxContainer>("%Hotbar");
		_tipName = GetNode<Label>("%TipName");
		_tipKind = GetNode<Label>("%TipKind");
		_tipLore = GetNode<Label>("%TipLore");
		_tipUse = GetNode<Label>("%TipUse");

		_player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (_player == null)
		{
			GD.PushError($"{Name}: no node in group 'player'; the inventory will stay empty.");
		}

		foreach (Node node in GetTree().GetNodesInGroup("equip_slots"))
		{
			var slot = (Button)node;
			string slotName = slot.GetMeta("slot_name", "").AsString();
			if (!EquipSlotNames.TryGetValue(slotName, out EquipmentSlot equipSlot))
			{
				GD.PushError($"{Name}: unknown equip slot '{slotName}'.");
				continue;
			}

			_equipSlots[equipSlot] = slot;
			SetupItemSlot(slot);
			RegisterSlot(slot, $"EQUIPMENT · {slotName}", () => _player?.GetEquipped(equipSlot), "UNEQUIP");
			slot.Pressed += () => _player?.Unequip(equipSlot);
		}

		int storageSize = _player?.Inventory.Capacity ?? 24;
		for (int i = 0; i < storageSize; i++)
		{
			int index = i;
			Button slot = CreateSlot();
			SetupItemSlot(slot);
			_storageGrid.AddChild(slot);
			_storageSlots.Add(slot);
			RegisterSlot(slot, $"STORAGE · SLOT {i + 1}", () => _player?.Inventory.GetSlot(index)?.Item, "USE");
			slot.Pressed += () => _player?.Inventory.UseItem(index, _player);
		}

		for (int i = 0; i < HotbarSize; i++)
		{
			Button slot = CreateSlot();
			var key = new Label
			{
				Text = (i + 1).ToString(),
				ThemeTypeVariation = "TagLabel",
				Position = new Vector2(10, 4),
				MouseFilter = Control.MouseFilterEnum.Ignore,
			};
			slot.AddChild(key);
			_hotbar.AddChild(slot);
			RegisterSlot(slot, $"QUICK SLOT · {i + 1}", () => null, "");
		}

		if (_player != null)
		{
			_player.Inventory.InventoryChanged += Refresh;
			_player.EquipmentChanged += Refresh;
		}
		Refresh();
		_root.Visible = false;
	}

	public override void _ExitTree()
	{
		if (IsInstanceValid(_player))
		{
			_player.Inventory.InventoryChanged -= Refresh;
			_player.EquipmentChanged -= Refresh;
		}
	}

	private void Refresh()
	{
		if (_player == null) return;

		Inventory inventory = _player.Inventory;
		for (int i = 0; i < _storageSlots.Count; i++)
		{
			ItemStack stack = inventory.GetSlot(i);
			ShowItem(_storageSlots[i], stack?.Item, stack?.Quantity ?? 0);
		}

		foreach ((EquipmentSlot equipSlot, Button button) in _equipSlots)
		{
			ShowItem(button, _player.GetEquipped(equipSlot), 1);
		}

		GetNode<Label>("%StorageLabel").Text = $"STORAGE · {inventory.UsedSlots}/{inventory.Capacity}";

		if (_tipItemAt != null) ShowTooltip(_tipSlotKind, _tipItemAt, _tipAction);
	}

	// Icon if the item has one, otherwise its name, so test items without art are still readable.
	private static void ShowItem(Button slot, Item item, int quantity)
	{
		slot.Icon = item?.Icon;
		if (item == null) slot.Text = "";
		else if (item.Icon != null) slot.Text = quantity > 1 ? $"×{quantity}" : "";
		else slot.Text = quantity > 1 ? $"{item.DisplayName} ×{quantity}" : item.DisplayName;
	}

	private static void SetupItemSlot(Button slot)
	{
		slot.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		slot.ClipText = true;
		slot.ExpandIcon = true;
		slot.IconAlignment = HorizontalAlignment.Center;
	}

	private static Button CreateSlot() => new()
	{
		ThemeTypeVariation = "Slot",
		CustomMinimumSize = SlotSize,
	};

	private void RegisterSlot(Button slot, string slotKind, Func<Item> itemAt, string action)
	{
		slot.FocusEntered += () => ShowTooltip(slotKind, itemAt, action);
		slot.MouseEntered += () => slot.GrabFocus();
	}

	private void ShowTooltip(string slotKind, Func<Item> itemAt, string action)
	{
		_tipSlotKind = slotKind;
		_tipItemAt = itemAt;
		_tipAction = action;

		Item item = itemAt();
		if (item == null)
		{
			_tipName.Text = "EMPTY";
			_tipKind.Text = slotKind;
			_tipLore.Text = "Nothing here yet.";
			_tipUse.Text = "—";
			return;
		}

		_tipName.Text = item.DisplayName.ToUpper();
		_tipLore.Text = item.Description;
		if (item is Armor armor)
		{
			_tipKind.Text = $"ARMOR · {SlotLabel(armor.Slot)}";
			_tipUse.Text = $"+{armor.Defense} ARMOR · PRESS TO {(action == "USE" ? "EQUIP" : action)}";
		}
		else
		{
			_tipKind.Text = slotKind;
			_tipUse.Text = action == "" ? "—" : $"PRESS TO {action}";
		}
	}

	private static string SlotLabel(EquipmentSlot slot)
	{
		foreach ((string label, EquipmentSlot value) in EquipSlotNames)
		{
			if (value == slot) return label;
		}
		return slot.ToString().ToUpper();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("inventory"))
		{
			// The tree is also paused by dialogue; don't open on top of it.
			if (_open) Close();
			else if (!GetTree().Paused) Open();
			else return;
			GetViewport().SetInputAsHandled();
		}
		else if (_open && @event.IsActionPressed("ui_cancel"))
		{
			Close();
			GetViewport().SetInputAsHandled();
		}
	}

	public void Open()
	{
		_open = true;
		GetTree().Paused = true;
		_root.Visible = true;

		_tween?.Kill();
		_tween = CreateTween().SetParallel().SetTrans(Tween.TransitionType.Quint).SetEase(Tween.EaseType.InOut);
		_tween.TweenProperty(_root, "modulate:a", 1.0f, OpenTime).From(0.0f);
		_tween.TweenProperty(_root, "position:x", 0.0f, OpenTime).From(-SlideDistance);
		// A blue slash leads the panel across the screen.
		float width = _root.Size.X;
		_sweep.Visible = true;
		_tween.TweenProperty(_sweep, "position:x", width, OpenTime).From(-_sweep.Size.X);
		_tween.Chain().TweenCallback(Callable.From(() => _sweep.Visible = false));

		_storageGrid.GetChild<Button>(0).GrabFocus();
	}

	public void Close()
	{
		_open = false;
		GetTree().Paused = false;
		GetViewport().GuiReleaseFocus();

		_tween?.Kill();
		_sweep.Visible = false;
		_tween = CreateTween().SetParallel().SetTrans(Tween.TransitionType.Quint).SetEase(Tween.EaseType.In);
		_tween.TweenProperty(_root, "modulate:a", 0.0f, CloseTime);
		_tween.TweenProperty(_root, "position:x", -SlideDistance * 0.5f, CloseTime);
		_tween.Chain().TweenCallback(Callable.From(() => _root.Visible = false));
	}
}
