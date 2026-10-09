using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Frostsworn;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Unlocks;
namespace Frostsworn.Tests;
public static partial class Suite
{
    static async Task CheckShop0821()
    {
        typeof(Node).Assembly.GetType("Godot.Bridge.ScriptManagerBridge")!.GetMethod("LookupScriptsInAssembly",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)!.Invoke(null,new object[]{typeof(NMerchantRoom).Assembly});
        var tree=(SceneTree)Engine.GetMainLoop();
        await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        foreach(int count in new[]{1,2,4})
        {
            var room=new NMerchantRoom();var container=new Control();tree.Root.AddChild(container);
            var players=(System.Collections.Generic.List<Player>)typeof(NMerchantRoom).GetField("_players",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(room)!;
            players.AddRange(Enumerable.Range(1,count).Select(i=>Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,(ulong)i)));
            typeof(NMerchantRoom).GetField("_characterContainer",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(room,container);
            LocalContext.NetId=(ulong)count;
            typeof(NMerchantRoom).GetMethod("AfterRoomIsLoaded",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(room,null);
            Assert(room.PlayerVisuals.Count==count,"native shop builds all player visuals: "+count);
            foreach(var merchant in room.PlayerVisuals)
            {
                Assert(merchant.FindChild("IdleLoop",true,false)!=null,"every player has shop idle after actual room initialization");
                var body=merchant.FindChild("Visuals",true,false) as Sprite2D;
                if(body!=null){merchant.RemoveChild(body);body.Free();}
                FrostIdleVisuals.EnsureMerchant(merchant);FrostIdleVisuals.EnsureMerchant(merchant);
                var fallback=merchant.GetNode<Sprite2D>("FrostIdleBody");
                Assert(fallback.IsVisibleInTree() && fallback.GetChildCount()==1,"replacement sprite recovers visible model without duplicate idle");
                Assert(Math.Abs(fallback.Scale.X*0.275f/0.31f-0.4125f)<0.0001f,"multiplayer shop preserves approved character scale");
            }
            if(DisplayServer.GetName()!="headless" && count==2)
            {
                container.Position=new Vector2(850,610);
                await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                tree.Root.GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../../outputs/shop-0.8.21.png"));
            }
            container.Free();room.Free();
        }
        LocalContext.NetId=null;
    }
}
