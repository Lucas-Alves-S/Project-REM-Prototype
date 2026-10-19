using Godot;

namespace ProjectREM.UI;

// Inventory screen (docs/ui-mockups/Inventory.dc.html), toggled with the "inventory" action (E).
// Layout only for now: there is no item system yet, so every slot shows as empty.
// Runs with ProcessMode = Always because it pauses the tree while open, like DialogueBox.
public partial class Inventory : CanvasLayer
{
	private const int StorageSize = 24;
	private const int HotbarSize = 8;
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

		foreach (Node node in GetTree().GetNodesInGroup("equip_slots"))
		{
			var slot = (Button)node;
			RegisterSlot(slot, $"EQUIPMENT · {slot.GetMeta("slot_name", "").AsString()}");
		}

		for (int i = 0; i < StorageSize; i++)
		{
			Button slot = CreateSlot();
			_storageGrid.AddChild(slot);
			RegisterSlot(slot, $"STORAGE · SLOT {i + 1}");
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
			RegisterSlot(slot, $"QUICK SLOT · {i + 1}");
		}

		GetNode<Label>("%StorageLabel").Text = $"STORAGE · 0/{StorageSize}";
		_root.Visible = false;
	}

	private static Button CreateSlot() => new()
	{
		ThemeTypeVariation = "Slot",
		CustomMinimumSize = SlotSize,
	};

	private void RegisterSlot(Button slot, string kind)
	{
		slot.FocusEntered += () => ShowTooltip(kind);
		slot.MouseEntered += () => slot.GrabFocus();
	}

	// Placeholder until items exist; this is where an item's name, rarity, stats and lore go.
	private void ShowTooltip(string kind)
	{
		_tipName.Text = "EMPTY";
		_tipKind.Text = kind;
		_tipLore.Text = "Nothing here yet.";
		_tipUse.Text = "—";
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
