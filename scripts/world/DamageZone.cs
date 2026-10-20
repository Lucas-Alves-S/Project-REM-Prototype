using System.Collections.Generic;
using Godot;
using ProjectREM.Combat;

namespace ProjectREM.World;

// Hurts every IDamageable standing inside it: one hit on entering, then one every HitInterval.
// Damage is raw; the target applies its own armor. Test hazard for tuning the armor formula.
public partial class DamageZone : Area3D
{
    [Export]
    public string ZoneName { get; set; } = "HAZARD";

    [Export]
    public int DamagePerHit { get; set; } = 10;

    [Export(PropertyHint.Range, "0.05,10.0,0.05,suffix:s")]
    public float HitInterval { get; set; } = 1.0f;

    [Export]
    public Color ZoneColor { get; set; } = new(1.0f, 0.3f, 0.2f, 0.35f);

    private readonly List<IDamageable> _targets = new();
    private Timer _timer;

    public override void _Ready()
    {
        _timer = GetNode<Timer>("HitTimer");
        _timer.WaitTime = HitInterval;
        _timer.Timeout += HitAll;

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;

        // Each instance gets its own material so zones can have different colors.
        GetNode<MeshInstance3D>("Mesh").MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = ZoneColor,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };

        float dps = DamagePerHit / HitInterval;
        GetNode<Label3D>("Label").Text =
            $"{ZoneName}\n{DamagePerHit} every {HitInterval}s ({dps:0.#} DPS)";
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is not IDamageable target)
            return;

        _targets.Add(target);
        target.TakeDamage(DamagePerHit, ZoneName);
        if (_timer.IsStopped())
            _timer.Start();
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is not IDamageable target)
            return;

        _targets.Remove(target);
        if (_targets.Count == 0)
            _timer.Stop();
    }

    private void HitAll()
    {
        foreach (IDamageable target in _targets)
        {
            target.TakeDamage(DamagePerHit, ZoneName);
        }
    }
}
