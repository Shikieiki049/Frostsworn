using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Frostsworn;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Unlocks;
namespace Frostsworn.Tests;
public static partial class Suite
{
    static async Task CheckVfx0823()
    {
        typeof(Node).Assembly.GetType("Godot.Bridge.ScriptManagerBridge")!.GetMethod("LookupScriptsInAssembly",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)!.Invoke(null,new object[]{typeof(NMerchantRoom).Assembly});
        var tree=(SceneTree)Engine.GetMainLoop();await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        var cards=typeof(FrostCard).Assembly.GetTypes().Where(t=>!t.IsAbstract && typeof(FrostCard).IsAssignableFrom(t)).ToArray();
        var expected=cards.Where(t=>t!=typeof(FrostStrike) && t!=typeof(FrostDefend) && t!=typeof(Depleted)).ToArray();
        Assert(expected.Length==90 && expected.All(FrostCardVfx.Profiles.ContainsKey) && FrostCardVfx.Profiles.Count==90,"all 90 playable non-basic cards have explicit intentional profiles");
        Assert(!FrostCardVfx.Profiles.ContainsKey(typeof(FrostStrike)) && !FrostCardVfx.Profiles.ContainsKey(typeof(FrostDefend)),"Strike and Defend VFX remain unchanged");
        foreach(var path in FrostCardVfx.AssetPaths)Assert(ResourceLoader.Exists(path),"native VFX resource exists: "+path);
        var owner=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,1);
        var run=MegaCrit.Sts2.Core.Runs.RunState.CreateForTest([owner]);
        foreach(var type in cards)
        {
            var card=(FrostCard)ModelDb.GetById<CardModel>(ModelDb.GetId(type)).ToMutable();card.Owner=owner;
            var command=DamageCmd.Attack(1);FrostCardVfx.ConfigureAttack(card,command);
            var hitCallbacks=(System.Collections.ICollection)command.GetType().GetField("_customHitVfxNodes",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(command)!;
            Assert(hitCallbacks.Count==(FrostCardVfx.Profiles.TryGetValue(type,out var profile) && profile.Target!=FrostMotif.None?1:0),"native per-hit callback configured for "+type.Name);
        }
        var container=new Node2D();tree.Root.AddChild(container);
        foreach(var profile in FrostCardVfx.Profiles.Values)
        {
            var fx=FrostCardVfx.CreateAt(profile.Caster|profile.Target,new Vector2(100,300),new Vector2(400,300),profile.Strength);
            container.AddChild(fx);
            void Check(Node node){if(node is GpuParticles2D p)Assert(p.OneShot,"no unbounded particle emitters");foreach(var child in node.GetChildren())Check(child);}
            Check(fx);fx.QueueFree();
        }
        await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        // Keep all motifs alive through their normal timed cleanup once.
        var all=Enum.GetValues<FrostMotif>().Aggregate(FrostMotif.None,(a,b)=>a|b);
        var alive=FrostCardVfx.CreateAt(all,new Vector2(100,300),new Vector2(400,300));container.AddChild(alive);
        await tree.ToSignal(tree.CreateTimer(3.7),SceneTreeTimer.SignalName.Timeout);
        Assert(!GodotObject.IsInstanceValid(alive),"visual root and particle children clean themselves up");
        if(DisplayServer.GetName()!="headless")
        {
            FrostMotif[] motifs={FrostMotif.Beam|FrostMotif.Mist,FrostMotif.Slash|FrostMotif.Shards,FrostMotif.Armor,FrostMotif.Storm|FrostMotif.Shards,FrostMotif.Store,FrostMotif.Crystal|FrostMotif.Prism,FrostMotif.Warmth,FrostMotif.Energy,FrostMotif.Bind,FrostMotif.Thaw,FrostMotif.Focus,FrostMotif.Impact};
            for(int page=0;page<2;page++)
            {
                for(int i=0;i<6;i++)
                {
                    int x=i%3,y=i/3;var target=new Vector2(240+x*420,230+y*350);
                    var label=new Label {Text=motifs[page*6+i].ToString(),Position=target-new Vector2(170,170)};container.AddChild(label);
                    var fx=FrostCardVfx.CreateAt(motifs[page*6+i],target-new Vector2(150,0),target);container.AddChild(fx);
                }
                await tree.ToSignal(tree.CreateTimer(.17),SceneTreeTimer.SignalName.Timeout);
                await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                tree.Root.GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath($"res://../../../outputs/card-vfx-0823-{page}.png"));
                foreach(var c in container.GetChildren())c.QueueFree();
                await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            }
        }
        container.QueueFree();
    }
}

