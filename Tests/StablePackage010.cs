#if STS2_STABLE_107
using System.IO;
using System.Linq;
using MegaCrit.Sts2.Core.Modding;
namespace Frostsworn.Tests;
public static partial class Suite
{
 static void CheckReleasePackage010()
 {
  foreach(var target in new[]{"stable-01071","beta-01110"})
  {
   string root=Path.Combine("C:/fz/sts2mod/work/workshop-release",target,"content");
   var files=Directory.GetFiles(root,"*.json",SearchOption.AllDirectories);
   Assert(files.Length==1 && Path.GetFileName(files[0])=="mod_manifest.json",target+" recursive content scan contains only one manifest JSON");
   foreach(string file in files)
   {
    using var stream=File.OpenRead(file);var manifest=ModManifest.ReadFromStream(stream,out var errors);
    Assert(manifest!=null && (errors?.Count ?? 0)==0 && manifest.id=="Frostsworn",target+" every JSON accepts native manifest reader without phantom mods");
   }
   Assert(new FileInfo(Path.Combine(root,"Frostsworn.dll")).Length>0 && new FileInfo(Path.Combine(root,"Frostsworn.pck")).Length>0,target+" has nonempty DLL and PCK");
  }
 }
}
#endif
