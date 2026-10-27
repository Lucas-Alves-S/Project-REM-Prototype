using Godot;

namespace ProjectREM.Items;

[GlobalClass]
public partial class Armor : EquipmentItem
{
    // Summed across all worn pieces, then turned into damage reduction by DamageCalculator.
    [Export]
    public int Defense { get; set; }
}
