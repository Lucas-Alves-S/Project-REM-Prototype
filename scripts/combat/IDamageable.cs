namespace ProjectREM.Combat;

// Anything a hazard or attack can hit. Takes raw damage; the implementer applies its own armor.
public interface IDamageable
{
	void TakeDamage(int rawDamage);
}
