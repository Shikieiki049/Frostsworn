namespace Frostsworn;
public static class FrostCompat
{
    public static Player PlayOwner(CardPlay play)
#if STS2_STABLE_107
        => play.Card.Owner;
#else
        => play.Player;
#endif
}