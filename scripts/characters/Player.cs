using System.Collections.Generic;
using Godot;
using ProjectREM.Combat;
using ProjectREM.Combat.Rng;
using ProjectREM.Interaction;
using ProjectREM.Items;

namespace ProjectREM.Characters;

public partial class Player : CharacterBody3D, IDamageable
{
	[Signal] public delegate void EquipmentChangedEventHandler();

	[Export] public float Speed = 5.0f;
	[Export] public float Acceleration = 60.0f;
	[Export] public float Friction = 50.0f;

	[ExportGroup("Combat Stats")]
	[Export(PropertyHint.Range, "0.0,1.0,0.01")] public float BaseCritChance { get; set; } = 0.10f;
	[Export] public float CritMultiplier { get; set; } = 1.5f;
	[Export] public int CritBagCapacity { get; set; } = 20;

	// K in reduction = armor / (armor + K): the armor value that halves incoming damage.
	[Export] public float ArmorConstant { get; set; } = 100.0f;

	[ExportGroup("Items")]
	// Put into the inventory on spawn.
	[Export] public Godot.Collections.Array<ItemStack> StartingItems { get; set; } = new();

	public CritMarbleBag CritBag { get; private set; }
	public HealthComponent Health { get; private set; }
	public Inventory Inventory { get; private set; }

	private readonly Dictionary<EquipmentSlot, EquipmentItem> _equipment = new();

	// Sheet rows: 0 = facing the camera, 1 = side (left), 2 = up.
	private static readonly StringName[][] Anims =
	{
		new StringName[] { "idle_down", "idle_side", "idle_up" },
		new StringName[] { "walk_down", "walk_side", "walk_up" },
	};
	private enum MotionState { Idle, Walk }

	private AnimatedSprite3D _sprite;

	// Last non-zero move input on the ground plane. X = right, Y = toward the camera.
	private Vector2 _facing = Vector2.Down;

	private Area3D _interactionArea;
	private InteractionArea _focused;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite3D>("Sprite");
		_interactionArea = GetNode<Area3D>("%InteractionArea");
		CritBag = new CritMarbleBag(BaseCritChance, CritBagCapacity);
		Health = GetNode<HealthComponent>("%Health");
		Inventory = GetNode<Inventory>("%Inventory");

		foreach (ItemStack stack in StartingItems)
		{
			if (stack?.Item != null) Inventory.AddItem(stack.Item, stack.Quantity);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		Vector3 velocity = Velocity;
		MotionState state;

		Vector2 input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		Vector3 wish = new Vector3(input.X, 0, input.Y);
		
		if (wish != Vector3.Zero)
		{
			state = MotionState.Walk;
		}
		else
		{
			state = MotionState.Idle;
		}

		if (wish.LengthSquared() > 1.0f)
		{
			wish = wish.Normalized();
		}

		// Accelerate toward the wished velocity so diagonal input doesn't snap the sprite.
		Vector3 planar = new Vector3(velocity.X, 0, velocity.Z);
		planar = planar.MoveToward(wish * Speed, (wish == Vector3.Zero ? Friction : Acceleration) * dt);
		velocity.X = planar.X;
		velocity.Z = planar.Z;

		Velocity = velocity;
		MoveAndSlide();

		if (wish != Vector3.Zero)
		{
			_facing = new Vector2(wish.X, wish.Z);
		}
		UpdateAnimation(state);
		UpdateFocus();
	}

	private void UpdateAnimation(MotionState state)
	{
		int row;
		if (Mathf.Abs(_facing.X) > Mathf.Abs(_facing.Y))
		{
			row = 1;
			_sprite.FlipH = _facing.X > 0.0f;
		}
		else
		{
			row = _facing.Y >= 0.0f ? 0 : 2;
			_sprite.FlipH = false;
		}

		StringName anim = Anims[(int)state][row];
		if (_sprite.Animation != anim)
		{
			_sprite.Play(anim);
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!@event.IsActionPressed("interact")) return;

		if (_focused != null)
		{
			_focused.Target?.Interact(this);
			GetViewport().SetInputAsHandled();
		}
	}

	// Re-pick the nearest interactable every frame so only one prompt shows at a time.
	private void UpdateFocus()
	{
		InteractionArea nearest = FindNearestInteractable();
		if (nearest == _focused) return;

		if (IsInstanceValid(_focused)) _focused.SetFocused(false);
		nearest?.SetFocused(true);
		_focused = nearest;
	}

	private InteractionArea FindNearestInteractable()
	{
		InteractionArea nearest_interactable = null;
		float nearest_distance = float.PositiveInfinity;

		foreach(Area3D area in _interactionArea.GetOverlappingAreas()) {
			if (area is not InteractionArea candidate) continue;

			float distance = GlobalPosition.DistanceSquaredTo(candidate.GlobalPosition);
			if (distance < nearest_distance)
			{
				nearest_interactable = candidate;
				nearest_distance = distance;
			}
		}

		return nearest_interactable;
	}

	public EquipmentItem GetEquipped(EquipmentSlot slot) => _equipment.GetValueOrDefault(slot);

	public int TotalArmor
	{
		get
		{
			int total = 0;
			foreach (EquipmentItem item in _equipment.Values)
			{
				if (item is Armor armor) total += armor.Defense;
			}
			return total;
		}
	}

	// Called by EquipmentItem.Use after the inventory has already taken the item out.
	// A piece already in that slot goes back to the inventory.
	public bool Equip(EquipmentItem item)
	{
		if (_equipment.TryGetValue(item.Slot, out EquipmentItem current))
		{
			if (!Inventory.AddItem(current)) return false;
		}

		_equipment[item.Slot] = item;
		EmitSignal(SignalName.EquipmentChanged);
		return true;
	}

	public bool Unequip(EquipmentSlot slot)
	{
		if (!_equipment.TryGetValue(slot, out EquipmentItem item)) return false;
		if (!Inventory.AddItem(item)) return false;

		_equipment.Remove(slot);
		EmitSignal(SignalName.EquipmentChanged);
		return true;
	}

	public void TakeDamage(int rawDamage)
	{
		if (Health.IsDead) return;

		int armor = TotalArmor;
		int damage = DamageCalculator.ApplyArmor(rawDamage, armor, ArmorConstant);
		float reduction = DamageCalculator.ArmorReduction(armor, ArmorConstant);
		Health.TakeDamage(damage);
		GD.Print($"Hit: {rawDamage} -> {damage} (armor {armor}, -{reduction:P1}) | HP {Health.CurrentHealth}/{Health.MaxHealth}");
	}
}
