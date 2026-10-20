using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Frostsworn;
using MegaCrit.Sts2.Core.Timeline;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
namespace Frostsworn.Tests;
public static partial class Suite
{
 static async Task CheckEpoch0912()
 {
  // Access through the native base class: this is the path that failed in the report.
  EpochModel epoch=EpochModel.Get<FrostEpoch>();
  var mod=(FrostEpoch)epoch;
  Assert(ResourceLoader.Exists(mod.CustomPackedPortraitPath) && ResourceLoader.Exists(mod.CustomBigPortraitPath),"epoch slot and detail paths exist in the published pack");
  Assert((bool)typeof(EpochModel).GetProperty("HasRealPortrait",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)!.GetValue(epoch)!,"native epoch recognizes the custom full-size portrait");
#if STS2_STABLE_107
  Assert(!epoch.IsArtPlaceholder,"native stable detail page does not mark custom art as placeholder");
#endif
  Assert(epoch.ResolvedPortraitPath==mod.CustomBigPortraitPath,"native resolver never falls back to the missing vanilla placeholder");
  for(int i=0;i<3;i++)
  {
   Assert(epoch.Portrait is Texture2D slot && slot.GetWidth()>0,"native timeline slot portrait loads "+i);
   Assert(epoch.RealPortrait is Texture2D full && full.GetWidth()>1000 && full.GetHeight()>0,"native inspect-screen full portrait loads "+i);
  }
  Assert(epoch.Id=="FROSTSWORN_CHARACTER_FROSTSWORN_CHARACTER_EPOCH" && typeof(FrostEpoch).GetCustomAttributes().Any(a=>a.GetType().Name.Contains("RegisterStoryEpoch")),"epoch identity and story registration remain unchanged for existing progress");
  var tree=(SceneTree)Engine.GetMainLoop();
  await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
  var nativePortrait=new TextureRect {Texture=epoch.RealPortrait,ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,Size=new Vector2(1000,600)};
  tree.Root.AddChild(nativePortrait);
  await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
  Assert(nativePortrait.Texture.GetSize()==epoch.RealPortrait.GetSize(),"native TextureRect displays full portrait at its original aspect ratio");
  if(DisplayServer.GetName()!="headless")
  {
   await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
   tree.Root.GetTexture().GetImage().SavePng("C:/fz/sts2mod/outputs/epoch-0912-"+FrostVersionGuard.RequiredGameVersion+".png");
  }
  nativePortrait.Free();
  // Both opposite-branch and unknown headers must fail atomically and leave useful diagnostics.
  foreach(int marker in new[]{FrostRngWire097.Magic==0x46535231?0x46535232:0x46535231,0x12345678})
  {
   var writer=new PacketWriter();writer.WriteInt(marker);
   var reader=new PacketReader();reader.Reset(writer.Buffer.Take(writer.BytePosition).ToArray());
   string? message=null;try {FrostRngWire097.Read(reader);}catch(InvalidDataException e){message=e.Message;}
   Assert(message!=null && message.Contains($"received=0x{marker:X8}") && message.Contains($"expected=0x{FrostRngWire097.Magic:X8}") && message.Contains("local_mod="+FrostVersion.Value) && message.Contains("target_game="+FrostVersionGuard.RequiredGameVersion),"incompatible packet rejected with exact build/header diagnostics "+marker);
  }
 }
}
