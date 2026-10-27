using Godot;

namespace ProjectREM.Combat;

// Hit points for anything that can die. A child node, so Player and later Enemy share it.
// Takes final damage only; armor is applied by the owner before calling TakeDamage.
[GlobalClass]
public partial class HealthComponent : Node
{
    [Signal]
    public delegate void HealthChangedEventHandler(int current, int max);

    [Signal]
    public delegate void DiedEventHandler();

    [Export]
    public int MaxHealth { get; set; } = 100;

    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
        if (IsDead)
            EmitSignal(SignalName.Died);
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
    }
}
