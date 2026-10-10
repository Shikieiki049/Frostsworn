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
        var visual=new Node2D {Position=new(300,500)};tree.Root.AddChild(visual);
        Assert(FrostCardVfx.BeamOrigin(visual,new(7,9))==new Vector2(7,9),"other characters retain their native beam origin");
        var rig=new Node2D {Name="IdleLoop",Scale=Vector2.One*.275f};visual.AddChild(rig);
        Assert(FrostCardVfx.BeamOrigin(visual,Vector2.Zero).DistanceTo(new Vector2(440.25f,209.875f))<.01f,"beam originates at elevated staff crystal rather than torso");
        visual.Position=new(700,500);visual.Scale=Vector2.One*.8f;
        Assert(FrostCardVfx.BeamOrigin(visual,Vector2.Zero).DistanceTo(new Vector2(812.2f,267.9f))<.01f,"beam origin follows each multiplayer model position and scale");
        var origins=Enumerable.Range(0,32).Select(_=>rig.ToLocal(FrostCardVfx.CrystalOrigin(visual,Vector2.Zero))).ToArray();
        Assert(origins.All(p=>p.X>=-260 && p.X<=650 && p.Y>=-1040 && p.Y<=-740) && origins.Distinct().Count()>1,"cosmetic summon positions vary within a bounded area around the caster");
        visual.QueueFree();
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
        var flight=FrostCardVfx.CreateCrystalFlight(new Vector2(180,290),new Vector2(900,290));container.AddChild(flight);
        var projectile=flight.GetNode<Node2D>("FlyingCrystal");
        var flightTween=(Tween)flight.GetMeta("flight_tween").AsGodotObject();flightTween.Pause();
        Assert(flight.GetNode<Node2D>("SummonCircle").Position==projectile.Position,"small magic circle marks the exact crystal summon position");
        Assert(projectile.Position==new Vector2(-720,0),"small crystal starts at caster instead of appearing on target");
        flightTween.CustomStep(.21);
        Assert(projectile.Position.X>-720 && projectile.Position.X<0,"ice crystal visibly travels toward target");
        if(DisplayServer.GetName()!="headless") {
            await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            tree.Root.GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../../outputs/crystal-flight-0824.png"));
        }
        flightTween.CustomStep(.12);
        await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        Assert(!GodotObject.IsInstanceValid(projectile) && flight.HasNode("FrostCardFx"),"crystal shatters only after flight reaches enemy");
        if(DisplayServer.GetName()!="headless") {
            await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            tree.Root.GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../../outputs/crystal-impact-0824.png"));
        }
        flightTween.Play();
        await tree.ToSignal(tree.CreateTimer(3.7),SceneTreeTimer.SignalName.Timeout);
        Assert(!GodotObject.IsInstanceValid(flight),"crystal flight and impact clean themselves up");
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

