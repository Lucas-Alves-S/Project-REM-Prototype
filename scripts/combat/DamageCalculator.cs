using System;

namespace ProjectREM.Combat;

public static class DamageCalculator
{
    // Fraction of damage blocked: armor / (armor + k). Never reaches 1, so stacking armor has
    // diminishing returns; armor == k blocks exactly half.
    public static float ArmorReduction(int armor, float k)
    {
        if (armor <= 0)
            return 0.0f;
        return armor / (armor + k);
    }

    // Rounds half away from zero (2.5 -> 3); Mathf.RoundToInt rounds half to even (2.5 -> 2).
    // Any hit that does damage deals at least 1, so armor can't make you immune.
    public static int ApplyArmor(int rawDamage, int armor, float k)
    {
        if (rawDamage <= 0)
            return 0;

        float reduced = rawDamage * (1.0f - ArmorReduction(armor, k));
        int damage = (int)Math.Round(reduced, MidpointRounding.AwayFromZero);
        return Math.Max(damage, 1);
    }
}
