using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Text.Json;
using System.Threading.Tasks;
using Frostsworn;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
namespace Frostsworn.Tests;
public static partial class Suite
{
    static Task CheckAudio0826()
    {
        var profiles=FrostCardAudio.Profiles;
        Assert(profiles.Count==52,"52 intentionally selected cards receive added sound");
        Assert(!profiles.ContainsKey(typeof(FrostStrike)) && !profiles.ContainsKey(typeof(FrostDefend)) && !profiles.ContainsKey(typeof(Depleted)),"basic cards and unplayable status retain existing audio");
        using var verified=JsonDocument.Parse(System.IO.File.ReadAllText(ProjectSettings.GlobalizePath("res://../native-audio-0826.json")));
        foreach(var path in FrostCardAudio.EventPaths)
            Assert(verified.RootElement.TryGetProperty(path,out var native) && native.GetProperty("oneshot").GetBoolean(),"original FMOD event resolves and is one-shot: "+path);
        foreach(var profile in profiles.Values)
            Assert(profile.Cast.Length<=2 && profile.Hit.Length<=2 && profile.Cast.Concat(profile.Hit).All(s=>s.Volume>0 && s.Volume<=.6f),"each sound phase has at most two low-volume layers");
        var owner=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,1);
        var run=MegaCrit.Sts2.Core.Runs.RunState.CreateForTest([owner]);
        foreach(var type in typeof(FrostCard).Assembly.GetTypes().Where(t=>!t.IsAbstract && typeof(FrostCard).IsAssignableFrom(t)))
        {
            var card=(FrostCard)ModelDb.GetById<CardModel>(ModelDb.GetId(type)).ToMutable();card.Owner=owner;
            var command=DamageCmd.Attack(1);FrostCardAudio.ConfigureAttack(card,command);
            int Count(string field)=>((ICollection)command.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(command)!).Count;
            profiles.TryGetValue(type,out var profile);
            bool timed=type==typeof(IceCrystal);
            Assert(Count("_customAttackerVfxNodes")==(!timed && (profile?.Cast.Length??0)>0?1:0) && Count("_customHitVfxNodes")==(!timed && (profile?.Hit.Length??0)>0?1:0),"native attack and hit audio callbacks configured: "+type.Name);
            FrostCardAudio.Cast(card); // TestMode must not require an audio manager.
        }
        Assert(FrostCardAudio.ShouldPlay(100,null) && !FrostCardAudio.ShouldPlay(100,100) && !FrostCardAudio.ShouldPlay(169,100) && FrostCardAudio.ShouldPlay(170,100),"area hits coalesce while subsequent hits can sound");
        FrostCardAudio.CrystalSummon();FrostCardAudio.CrystalImpact();
        Assert(true,"visual-timed crystal sounds are safe in headless/test sessions");
        return Task.CompletedTask;
    }
}
