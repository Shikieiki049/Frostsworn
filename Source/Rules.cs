namespace Frostsworn;

// Pure rules shared with the boundary tests; no game state is mutated here.
public static class Rules
{
    public static int FreezeThreshold(int maxHp, int freezes)
    {
        int portion=(int)Math.Ceiling(maxHp*0.12m);
        return Math.Clamp(portion,12,54)+Math.Clamp(portion,4,12)*freezes;
    }
    public static int Capacity(int expansion) => Math.Clamp(4 + expansion, 4, 10);
    public static int Melt(int armor) => Math.Max(0,armor) / 2;
    public static int Absorb(int armor, decimal incoming) => Math.Min(Math.Max(armor, 0), (int)Math.Ceiling(Math.Max(incoming, 0)));
    public static int ThawedCost(int cost, bool pending, bool costsX) => pending && !costsX && cost >= 0 ? Math.Max(0, cost - 1) : cost;
}

