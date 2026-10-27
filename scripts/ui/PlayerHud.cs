using Godot;
using ProjectREM.Characters;
using ProjectREM.Combat;

namespace ProjectREM.UI;

// Player status in the bottom-left corner (docs/ui-mockups/BossHUD.dc.html): diamond portrait,
// skewed HP bar and a stats line. The mockup's MAGIC bar is left out until there is a magic stat.
public partial class PlayerHud : CanvasLayer
{
    // The HP bar eases to its new value like the mockup's `transition: width .12s linear`.
    private const double BarTime = 0.12;

    [Export]
    public CharacterData Character { get; set; }

    [Export]
    public string PortraitKey { get; set; } = "Calm";

    private ProgressBar _hpBar;
    private Label _stats;
    private Player _player;
    private Tween _tween;

    public override void _Ready()
    {
        _hpBar = GetNode<ProgressBar>("%HpBar");
        _stats = GetNode<Label>("%Stats");

        if (
            Character != null
            && Character.Portraits.TryGetValue(PortraitKey, out Texture2D portrait)
        )
        {
            GetNode<TextureRect>("%Face").Texture = portrait;
        }

        _player = GetTree().GetFirstNodeInGroup("player") as Player;
        if (_player == null)
        {
            GD.PushError($"{Name}: no node in group 'player'.");
            return;
        }

        HealthComponent health = _player.Health;
        _hpBar.MaxValue = health.MaxHealth;
        _hpBar.Value = health.CurrentHealth;

        health.HealthChanged += OnHealthChanged;
        _player.EquipmentChanged += UpdateStats;
        UpdateStats();
    }

    public override void _ExitTree()
    {
        if (!IsInstanceValid(_player))
            return;
        _player.Health.HealthChanged -= OnHealthChanged;
        _player.EquipmentChanged -= UpdateStats;
    }

    private void OnHealthChanged(int current, int max)
    {
        _hpBar.MaxValue = max;
        _tween?.Kill();
        _tween = CreateTween();
        _tween.TweenProperty(_hpBar, "value", (double)current, BarTime);
        UpdateStats();
    }

    // Armor is shown while the damage formula is being tuned; the mockup has MAGIC here.
    private void UpdateStats()
    {
        HealthComponent health = _player.Health;
        int armor = _player.TotalArmor;
        float reduction = DamageCalculator.ArmorReduction(armor, _player.ArmorConstant);
        _stats.Text =
            $"HP {health.CurrentHealth}/{health.MaxHealth} · ARMOR {armor} (−{reduction:P0})";
    }
}
