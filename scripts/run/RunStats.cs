using Godot;
using ProjectREM.Characters;

namespace ProjectREM.Run;

public partial class RunStats : Node
{
    private Player _player;
    private double _timeDreaming;

    public override void _Ready()
    {
        _player = GetTree().GetFirstNodeInGroup("player") as Player;
    }

    // Pausable like the world, so menus, dialogue and the death screen don't count as dreaming.
    public override void _Process(double delta)
    {
        _timeDreaming += delta;
    }

    // Seconds spent in this run.
    public double TimeDreaming() => _timeDreaming;

    // TODO: count runs across sessions once there is a save file.
    public int DreamNumber() => 1;

    // TODO: floor generation.
    public string FloorReached() => "1-1";

    // TODO: rooms.
    public int RoomsCleared() => 0;

    // TODO: enemies.
    public int NightmaresDefeated() => 0;

    // TODO: player attacks.
    public int DamageDealt() => 0;

    // TODO: resonances.
    public int Resonances() => 0;

    public string WokenBy() => _player?.LastDamageSource ?? "THE NIGHTMARE";

    // TODO: meta-progression currencies.
    public int DreamDust() => 0;

    public int Memories() => 0;
}
