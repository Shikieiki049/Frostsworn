using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Frostsworn;
namespace Frostsworn.Tests;
public static partial class Suite
{
 static async Task CheckResourceGuards095(bool scriptsRegistered=false)
 {
  var build=typeof(EclipseUi).GetMethod("BuildIcon",BindingFlags.NonPublic|BindingFlags.Static)!;
  Texture2D Build(Texture2D? source)=>(Texture2D)build.Invoke(null,new object?[]{source})!;
  void CheckFallback(Texture2D? source,string why)
  {
   using var result=Build(source);using var image=result.GetImage();
   Assert(result.GetSize()==new Vector2(64,64) && image.GetPixel(0,0).A==0 && image.GetPixel(6,32).A>.8f,"Eclipse fallback handles "+why);
  }
  CheckFallback(null,"a failed texture load without a NullReferenceException");
  using(var empty=Image.CreateEmpty(32,32,false,Image.Format.Rgba8))
  {empty.Fill(Colors.Transparent);using var source=ImageTexture.CreateFromImage(empty);CheckFallback(source,"fully transparent art");}
  var disposed=new GradientTexture2D();disposed.Dispose();CheckFallback(disposed,"a disposed cached source");
  using(var image=Image.CreateEmpty(32,32,false,Image.Format.Rgba8))
  {
   image.Fill(Colors.Transparent);image.SetPixel(10,12,Colors.White);
   using var source=ImageTexture.CreateFromImage(image);using var icon=Build(source);
   Assert(icon is AtlasTexture atlas && atlas.Region==new Rect2(10,12,1,1),"valid Eclipse art retains exact transparent-margin crop");
  }
  for(int i=0;i<=8;i++)Assert(EclipseUi.Icon(i) is AtlasTexture,"packaged Eclipse level "+i+" uses real art rather than fallback");
  var cached=EclipseUi.Icon(2);cached.Dispose();
  Assert(GodotObject.IsInstanceValid(EclipseUi.Icon(2)) && EclipseUi.Icon(2) is AtlasTexture,"disposed Eclipse cache entry reloads safely");
  Assert(ReferenceEquals(EclipseUi.Icon(2),EclipseUi.Icon(2)),"Eclipse timers reuse the cached texture");
  var optional=typeof(FrostCardVfx).GetMethod("OptionalScene",BindingFlags.NonPublic|BindingFlags.Static)!.MakeGenericMethod(typeof(Node2D));
  Node2D? Load(Func<Node?> factory)=>(Node2D?)optional.Invoke(null,new object[]{factory,"regression-test"});
  Assert(Load(()=>throw new InvalidOperationException("Handle is not initialized."))==null,"optional VFX handle failure does not cancel card construction");
  Assert(Load(()=>throw new ObjectDisposedException("native VFX"))==null,"optional VFX disposed handle is skipped");
  var wrong=new Control();Assert(Load(()=>wrong)==null && !GodotObject.IsInstanceValid(wrong),"wrong VFX type is released");
  var dead=new Node2D();dead.Free();Assert(Load(()=>dead)==null,"invalid native VFX result is rejected before property access");
  var healthy=new Node2D();try {Assert(ReferenceEquals(Load(()=>healthy),healthy),"healthy native VFX are preserved");}finally {healthy.Free();}
  try {Load(()=>throw new InvalidOperationException("unrelated programming error"));throw new Exception("unexpected VFX exception was swallowed");}
  catch(TargetInvocationException ex)when(ex.InnerException is InvalidOperationException){Assert(true,"unrelated programming errors are not hidden by optional VFX handling");}
  var failures=new List<string>();int count=0;
  void Scan(string path)
  {
   using var dir=DirAccess.Open(path);Assert(dir!=null,"open packaged asset directory "+path);
   foreach(var name in dir!.GetFiles())
   {
    if(!new[]{"png","svg","tres"}.Contains(name.GetExtension().ToLowerInvariant()))continue;
    string file=path+"/"+name;var texture=ResourceLoader.Load<Texture2D>(file);
    if(texture==null || !GodotObject.IsInstanceValid(texture)){failures.Add(file);continue;}
    using var image=texture.GetImage();if(image==null || image.IsEmpty())failures.Add(file);count++;
   }
   foreach(var child in dir.GetDirectories())Scan(path+"/"+child);
  }
  Scan("res://Frostsworn");
  Assert(count>=531 && failures.Count==0,"all "+count+" packaged textures and atlases load and decode in native engine; failures: "+string.Join(", ",failures));
  await CheckVfx0823(scriptsRegistered);CheckVfxMissing0833();
 }
}
