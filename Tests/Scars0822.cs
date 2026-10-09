using System;
using System.Linq;
using System.Threading.Tasks;
using Frostsworn;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.Context;
namespace Frostsworn.Tests;
public static partial class Suite
{
    static async Task CheckScars0822()
    {
        // Includes native serialization setup and the complete Ritsu load path.
        await CheckLoad0820();
        foreach(int count in new[]{2,4})
        foreach(int localSlot in Enumerable.Range(0,count))
        {
            var players=Enumerable.Range(0,count).Select(i=>Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,76561198363859616UL+(ulong)(i*1234567))).ToArray();
            var run=RunState.CreateForTest(players,seed:$"SCARS0822-{count}-{localSlot}");
            Eclipse.Data.Set(run,new EclipseRunData {Level=8});Eclipse.InitializeRun(run);
            LocalContext.NetId=players[localSlot].NetId;
            // One player has a genuine six-point max HP loss. It must persist.
            players[1].Creature.SetMaxHpInternal(64);
            for(int i=0;i<count;i++)
            {
                players[i].Creature.SetCurrentHpInternal(40-i);
                for(int n=0;n<6+i;n++)Eclipse.Scar(players[i].Creature,1);
            }
            // Genuine max HP gains after scarring must survive SL and restoration.
            players[0].Creature.SetMaxHpInternal(players[0].Creature.MaxHp+8);
            // Dead allies must also regain their maximum without reviving.
            players.Last().Creature.SetCurrentHpInternal(0);
            var native=SaveNative(run);
            for(int sl=0;sl<3;sl++)
            {
                run=RunState.FromSerializable(native);PublishLoaded(run);
                Assert(run.Players.All(p=>Eclipse.Data.Get(run).MaxHpLost[p.NetId]==6+run.GetPlayerSlotIndex(p)),"SL restores all players' scar debts, including remote Steam IDs");
            }
            int[] hp=run.Players.Select(p=>p.Creature.CurrentHp).ToArray();
            int[] updates=new int[count];
            for(int i=0;i<count;i++){int slot=i;run.Players[i].Creature.MaxHpChanged+=(_,_)=>updates[slot]++;}
            run.CurrentActIndex=1;
            Assert(run.Players.Select(p=>p.Creature.MaxHp).SequenceEqual(Enumerable.Range(0,count).Select(i=>i==0?78:i==1?64:70)),"native act setter restores each ally and preserves genuine maximum HP loss");
            Assert(run.Players.Select(p=>p.Creature.CurrentHp).SequenceEqual(hp),"restoration neither heals nor revives allies");
            Assert(updates.All(n=>n==1) && Eclipse.Data.Get(run).MaxHpLost.Count==0,"every ally emits one maximum HP UI notification and clears debt");
            run.CurrentActIndex=1;
            Assert(updates.All(n=>n==1),"repeated act initialization cannot restore maximum HP twice");
            var nextSave=SaveNative(run);var next=RunState.FromSerializable(nextSave);PublishLoaded(next);
            Assert(Eclipse.Data.Get(next).MaxHpLost.Count==0,"SL in the next act cannot recover the previous act's debt");
            next.Players[0].Creature.SetCurrentHpInternal(40);Eclipse.Scar(next.Players[0].Creature,1);
            next.CurrentActIndex=0;
            Assert(next.Players[0].Creature.MaxHp==77 && Eclipse.Data.Get(next).MaxHpLost.Count==1,"backward act assignment cannot award a restoration");
        }
        LocalContext.NetId=null;
    }
}
