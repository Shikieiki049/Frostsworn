using System;
using System.Threading.Tasks;
using Frostsworn;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using System.Reflection;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
namespace Frostsworn.Tests;
public static partial class Suite
{
    static async Task CheckRebuild0819()
    {
        var host=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,1);
        var guest=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,2);
        var run=RunState.CreateForTest([host,guest],seed:"REBUILD0819");
        Eclipse.Data.Set(run,new EclipseRunData {Level=8});Eclipse.InitializeRun(run);
        Assert(host.Gold==79 && guest.Gold==79,"initial Eclipse run applies gold penalty once");
        for(int n=0;n<3;n++)
        {
            var a=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,1);a.Gold=79;
            var b=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,2);b.Gold=79;
            var rebuilt=RunState.CreateForTest([a,b],seed:"REBUILD0819");
            // Recreate the native run without Ritsu's attached payload, as in
            // replay tools. The consumed begin-run packet cannot be read again.
            Eclipse.InitializeRun(rebuilt);
            Assert(Eclipse.Level(rebuilt)==8,"committed host difficulty survives repeated run reconstruction");
            Assert(a.Gold==79 && b.Gold==79,"reconstruction never deducts starting gold again");
        }
        var unrelated=RunState.CreateForTest([Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,1)],seed:"OTHER0819");
        Eclipse.InitializeRun(unrelated);Assert(Eclipse.Level(unrelated)==0,"different seed or party cannot inherit session difficulty");
        Eclipse.Data.Set(run,new EclipseRunData {Level=5,GoldApplied=true});Eclipse.InitializeRun(run);
        Assert(Eclipse.Level(run)==5,"complete saved difficulty takes precedence over recovery cache");
        var net=DispatchProxy.Create<INetGameService,EclipseNetProxy085>();
        var lobby=new StartRunLobby(GameMode.Standard,net,new NCharacterSelectScreen(),2);
        ((EclipseNetProxy085)(object)net).Kind=NetGameType.Host;
        EclipseNetwork.Poll(lobby);EclipseNetwork.Accept(lobby,0);
        var fresh=RunState.CreateForTest([Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,1),Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,2)],seed:"REBUILD0819");
        Eclipse.InitializeRun(fresh);
        Assert(Eclipse.Level(fresh)==0 && fresh.Players[0].Gold==99,"new lobby clears recovery even with identical seed and party");
        ((EclipseNetProxy085)(object)net).Kind=NetGameType.Client;
        EclipseNetwork.Accept(lobby,8);
        var clientRun=RunState.CreateForTest([Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,2)],seed:"CLIENT0819");
        Eclipse.InitializeRun(clientRun);
        Assert(Eclipse.Level(clientRun)==8 && clientRun.Players[0].Gold==79,"host-confirmed lobby level survives missing optional begin-run trailer");
        await CheckMultiplayer0817();
    }
}
