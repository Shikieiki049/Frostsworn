#if STS2_STABLE_107
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Rooms;
namespace Frostsworn;

// Loadout 0.5.8 catches AfterAddedToRoom failures but can leave the partially
// added creature in combat. These two crab arms require the dedicated boss
// background for spawn, damage, death and moves. Refuse the unsupported debug
// summon before CreatureCmd mutates combat; do not swallow gameplay exceptions.
[HarmonyPatch(typeof(CreatureCmd),nameof(CreatureCmd.Add),typeof(MonsterModel),typeof(ICombatState),typeof(CombatSide),typeof(string))]
public static class FrostDebugSummonGuard010
{
    public static bool Unsupported(MonsterModel monster,bool loadoutCall,bool hasCrabBackground)
        => loadoutCall && !hasCrabBackground && monster is Crusher or Rocket;
    static bool Prefix(MonsterModel monster,ref Task<Creature> __result)
    {
        if(monster is not (Crusher or Rocket))return true;
        bool loadout=new System.Diagnostics.StackTrace().GetFrames().Any(f=>f.GetMethod()?.DeclaringType?.FullName?.StartsWith("Loadout.Services.Actions.LoadoutSummonMonsterService",StringComparison.Ordinal)==true);
        bool hasBackground=NCombatRoom.Instance?.Background?.GetNodeOrNull<Godot.Node>("%KaiserCrab")!=null;
        if(!Unsupported(monster,loadout,hasBackground))return true;
        __result=Task.FromException<Creature>(new InvalidOperationException("Frostsworn: Loadout cannot summon Crusher/Rocket without the Kaiser Crab boss background. No creature was added; use the complete boss encounter instead."));
        return false;
    }
}
#endif
