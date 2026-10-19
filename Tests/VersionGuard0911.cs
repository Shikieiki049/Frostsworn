using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Frostsworn;
using MegaCrit.Sts2.Core.Localization;
namespace Frostsworn.Tests;
public static partial class Suite
{
 static async Task CheckVersionGuard0911()
 {
  foreach(var required in new[]{"0.107.1","0.111.0"})
  {
   Assert(!FrostVersionGuard.IsMismatch(required,required) && !FrostVersionGuard.IsMismatch(" v"+required+" ",required),"matching release prefixes never cause warning for "+required);
   Assert(FrostVersionGuard.IsMismatch(required=="0.107.1"?"0.111.0":"0.107.1",required),"opposite stable/beta build is rejected for "+required);
   Assert(FrostVersionGuard.IsMismatch("0.112.0",required),"unverified newer game is rejected for "+required);
  }
  foreach(string? unknown in new string?[]{null,"","nonsense"})Assert(!FrostVersionGuard.IsMismatch(unknown,"0.111.0"),"unreadable metadata does not invent a mismatch");
  string file=Path.Combine(OS.GetUserDataDir(),"version-guard-test.json");
  try
  {
   File.WriteAllText(file,"{bad json");Assert(FrostVersionGuard.ReadVersion(file)==null,"malformed release file fails safely");
   File.WriteAllText(file,"{\"version\":42}");Assert(FrostVersionGuard.ReadVersion(file)==null,"non-string release version fails safely");
   File.WriteAllText(file,"{\"version\":\"v"+FrostVersionGuard.RequiredGameVersion+"\"}");Assert(FrostVersionGuard.CheckStartup(file),"actual startup check permits matching JSON release");
   var tree=(SceneTree)Engine.GetMainLoop();await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
   foreach(bool english in new[]{false,true})
   {
    var dialog=FrostVersionGuard.CreateDialog("0.107.1","0.111.0",english);tree.Root.AddChild(dialog);dialog.PopupCentered(new Vector2I(680,340));
    await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
    Assert(dialog.Visible && dialog.DialogText.Contains("0.107.1") && dialog.DialogText.Contains("0.111.0"),"real popup shows actual and required game versions "+english);
    Assert(dialog.DialogText.Contains(english?"subscribe again":"重新订阅"),"popup explains Workshop resubscription "+english);
    if(DisplayServer.GetName()!="headless")
    {
     await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
     dialog.GetViewport().GetTexture().GetImage().SavePng("C:/fz/sts2mod/outputs/version-mismatch-"+(english?"en":"zh")+".png");
    }
    dialog.EmitSignal(AcceptDialog.SignalName.Confirmed);await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
    Assert(!GodotObject.IsInstanceValid(dialog),"acknowledging popup frees it without changing game state "+english);
   }
   LocManager.Instance.SetLanguage("zhs");
   string other=FrostVersionGuard.RequiredGameVersion=="0.111.0"?"0.107.1":"0.111.0";
   File.WriteAllText(file,"{\"version\":\"v"+other+"\"}");
   Assert(!FrostVersionGuard.CheckStartup(file) && !FrostVersionGuard.CheckStartup(file),"actual mismatch startup path blocks gameplay initialization on repeat calls");
   await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
   var warnings=tree.Root.GetChildren().OfType<AcceptDialog>().Where(d=>d.Name=="FrostswornVersionMismatch").ToArray();
   Assert(warnings.Length==1 && warnings[0].Visible && warnings[0].DialogText.Contains("重新订阅"),"deferred startup queues exactly one real localized warning");warnings[0].Free();
  }
  finally {File.Delete(file);}
 }
}
