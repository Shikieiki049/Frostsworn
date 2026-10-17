using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using Frostsworn;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
namespace Frostsworn.Tests;
public static partial class Suite
{
 static int VfxLoadCalls096;
 static readonly HashSet<string> VfxPaths096=new();
 static void CountVfxLoad096(string path){if(VfxPaths096.Contains(path))VfxLoadCalls096++;}
 static async Task CheckVfxCache096()
 {
  var paths=FrostCardVfx.AssetPaths.ToHashSet();
  var getPaths=AccessTools.Method(typeof(PreloadManager),"GetRunAssetPaths");
  // Use the native common-run list plus the selected character's native card list.
  // CharacterModel.AssetPaths also loads Spine extensions unavailable in this isolated host.
  var runPaths=((IEnumerable<string>)getPaths.Invoke(null,new object[]{Array.Empty<CharacterModel>(),false})!)
   .Concat(ModelDb.Character<FrostswornCharacter>().CardPool.AllCards.SelectMany(c=>c.RunAssetPaths)).ToHashSet();
  Assert(paths.All(runPaths.Contains),"native run preload retains every Frostsworn VFX scene and particle texture");
  Assert(runPaths.Contains(ModelDb.CardPool<FrostCardPool>().TextEnergyIconPath),"text energy icon is retained in native run preload");
  var tree=(SceneTree)Engine.GetMainLoop();
  typeof(Node).Assembly.GetType("Godot.Bridge.ScriptManagerBridge")!.GetMethod("LookupScriptsInAssembly",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)!.Invoke(null,new object[]{typeof(NMerchantRoom).Assembly});
  await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
  // Seed the real native cache as the background preloader does; retain only IDs in the fixture.
  var ids=new Dictionary<string,ulong>();
  foreach(string path in paths)
  {
   var resource=ResourceLoader.Load(path);Assert(resource!=null,"preload VFX resource "+path);
   PreloadManager.Cache.SetAsset(path,resource!);ids[path]=resource!.GetInstanceId();
  }
  // Negative control: prove the native boundary really disposes assets omitted from run sets.
  var unretained=new GradientTexture2D();
  const string controlPath="res://Frostsworn/tests/unretained-cache-control";
  PreloadManager.Cache.SetAsset(controlPath,unretained);
  var loadSets=AccessTools.Method(typeof(PreloadManager),"LoadAssetSets");
  bool enabled=PreloadManager.Enabled;
  try
  {
   // Avoid NAssetLoader (not mounted in the harness), but execute native cache subtraction/unloading.
   PreloadManager.Enabled=false;
   for(int room=0;room<4;room++)
   {
    var task=(Task)loadSets.Invoke(null,new object[]{"Frostsworn-room-regression-"+room,new IEnumerable<string>[]{runPaths}})!;
    await task;await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
    GC.Collect();GC.WaitForPendingFinalizers();await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
    if(room==0)Assert(!PreloadManager.Cache.ContainsKey(controlPath) && !GodotObject.IsInstanceValid(unretained),"native transition releases an unretained control resource while keeping VFX");
    Assert(paths.All(p=>PreloadManager.Cache.ContainsKey(p) && GodotObject.IsInstanceValid(PreloadManager.Cache.GetAsset<Resource>(p)) && PreloadManager.Cache.GetAsset<Resource>(p).GetInstanceId()==ids[p]),"native room transition "+room+" keeps original VFX handles alive through GC");
   }
  }
  finally {PreloadManager.Enabled=enabled;}
  var loader=typeof(ResourceLoader).GetMethods(BindingFlags.Public|BindingFlags.Static).Single(m=>m.Name==nameof(ResourceLoader.Load) && !m.IsGenericMethod && m.GetParameters().Length==3);
  var harmony=new Harmony("Frostsworn.Tests.VfxCache096");
  VfxPaths096.UnionWith(paths);VfxLoadCalls096=0;
  harmony.Patch(loader,prefix:new HarmonyMethod(typeof(Suite),nameof(CountVfxLoad096)));
  try
  {
   foreach(var profile in FrostCardVfx.Profiles.Values)
   {
    var effect=FrostCardVfx.CreateAt(profile.Caster|profile.Target,Vector2.Zero,new Vector2(240,0),profile.Strength);
    effect.Free();
   }
   Assert(VfxLoadCalls096==0,"all 90 card VFX reuse native cached resources without re-entering ResourceLoader.Load");
  }
  finally {harmony.UnpatchAll(harmony.Id);VfxPaths096.Clear();}
  // Existing null-icon safeguards, all packaged textures, visual lifetime and missing-scene fallbacks.
  await CheckResourceGuards095(true);
 }
}

