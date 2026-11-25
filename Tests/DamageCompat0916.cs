using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.ValueProps;
namespace Frostsworn.Tests;
public static class DamageCompat0916
{
    public static void LoseTestHp0916(this Creature creature, decimal amount, ValueProp props)
    {
#if STS2_BETA_114
        creature.LoseHpInternal(amount, false);
#else
        creature.LoseHpInternal(amount, props);
#endif
    }
}
