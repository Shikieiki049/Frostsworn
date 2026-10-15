using System;
using MegaCrit.Sts2.Core.TestSupport;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using Frostsworn;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Frostsworn.Tests;
public static partial class Suite
{
 private static async Task CheckPowerIcons094()
 {
  // Includes the real native Exhaust command and the revised 3 HP Curse assertion.
  await CheckVoid090();
  var tree=(SceneTree)Engine.GetMainLoop();
  tree.Root.Size=new Vector2I(1100,460);
  var owner=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,94);
  _=RunState.CreateForTest([owner]);TestMode.IsOn=false;
  
  var bg=new ColorRect {Color=new Color("17202d"),Size=new Vector2(1100,460)};tree.Root.AddChild(bg);
  var models=new PowerModel[]{ModelDb.Power<DexterityPower>(),ModelDb.Power<VoidFadingDexterityPower>(),ModelDb.Power<StrengthPower>(),ModelDb.Power<VoidFadingStrengthPower>()};
  var names=new[]{"Native Dexterity","Fading Dexterity","Native Strength","Fading Strength"};
  for(int i=0;i<models.Length;i++){
   var model=models[i].ToMutable();AccessTools.Property(typeof(PowerModel),"Owner").SetValue(model,owner.Creature);model.SetAmount(3);
   var x=45+i*260;
   var label=new Label {Text=names[i],Position=new Vector2(x,35)};bg.AddChild(label);
   var node=NPower.Create(model);bg.AddChild(node);node.Position=new Vector2(x+75,120);
   Assert(node.GetNode<TextureRect>("%Icon").Texture==model.Icon,"native status widget resolves "+names[i]);
   bg.AddChild(new TextureRect{Texture=model.BigIcon,ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,Position=new Vector2(x+30,210),Size=new Vector2(128,128)});
   if(model is VoidFadingPower){
    Assert(model.Icon.GetSize()==new Vector2(128,128) && model.BigIcon.GetSize()==new Vector2(128,128),names[i]+" uses native-size status and particle textures");
    var image=model.Icon.GetImage();Assert(image.GetPixel(0,0).A<.01f && image.GetPixel(127,127).A<.01f,names[i]+" preserves transparent corners");
    int left=128,top=128,right=-1,bottom=-1;
    for(int y=0;y<128;y++)for(int xx=0;xx<128;xx++)if(image.GetPixel(xx,y).A>.1f){left=Math.Min(left,xx);right=Math.Max(right,xx);top=Math.Min(top,y);bottom=Math.Max(bottom,y);}
    Assert(Math.Max(right-left+1,bottom-top+1)>=112 && Math.Max(right-left+1,bottom-top+1)<=117 && Math.Abs(left+right-127)<=2 && Math.Abs(top+bottom-127)<=2,names[i]+" centered footprint matches the existing 90 percent buff art scale");
   }
  }
  if(DisplayServer.GetName()!="headless"){
   await tree.ToSignal(tree.CreateTimer(.65),SceneTreeTimer.SignalName.Timeout);await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
   tree.Root.GetTexture().GetImage().SavePng("C:/fz/sts2mod/outputs/fading-power-icons-094.png");
  }
  bg.Free();TestMode.IsOn=true;
 }
}

