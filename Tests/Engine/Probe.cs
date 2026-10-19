using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Godot;

public partial class Probe : Node
{
    public override async void _Ready()
    {
        try
        {
            string root = ProjectSettings.GlobalizePath("res://../..");
            string game = System.Environment.GetEnvironmentVariable("FROSTSWORN_TEST_GAME_DLLS") ?? "C:/fz/steam/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64";
            string ritsu = System.Environment.GetEnvironmentVariable("FROSTSWORN_TEST_RITSU_PATH") ?? Path.GetFullPath(Path.Combine(root, "../work/ritsulib-package/lib/net9.0"));
            var engineContext = AssemblyLoadContext.GetLoadContext(typeof(Probe).Assembly)!;
            engineContext.Resolving += (context, name) =>
            {
                foreach (string dir in new[] { game, ritsu, Path.Combine(root, "dist/Frostsworn") })
                {
                    string path = Path.Combine(dir, name.Name + ".dll");
                    if (File.Exists(path)) return context.LoadFromAssemblyPath(path);
                }
                return null;
            };
            string? crossDll=System.Environment.GetEnvironmentVariable("FROSTSWORN_CROSS_DLL");
            if(!string.IsNullOrEmpty(crossDll))
            {
                var mod=engineContext.LoadFromAssemblyPath(crossDll);
                if(mod.GetTypes().Length==0)throw new Exception("Wrong-target assembly types failed discovery");
                bool refused=false;
                try {mod.GetType("Frostsworn.Entry")!.GetMethod("Initialize")!.Invoke(null,null);}
                catch(TargetInvocationException e) when(e.InnerException is InvalidOperationException && e.InnerException.Message.Contains("Frostsworn version mismatch")){refused=true;}
                if(!refused)throw new Exception("Mismatched initializer did not signal controlled failure to the native mod loader");
                for(int i=0;i<3;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                AcceptDialog? warning=null;int count=0;
                foreach(var child in GetTree().Root.GetChildren())if(child is AcceptDialog d && d.Name=="FrostswornVersionMismatch"){warning=d;count++;}
                if(count!=1 || warning==null || !warning.Visible || !(warning.DialogText.Contains("subscribe again") || warning.DialogText.Contains("重新订阅")))throw new Exception("Wrong-target native startup did not display one resubscription warning");
                var pile=mod.GetType("Frostsworn.Entry")!.GetProperty("ColdPileType")!.GetValue(null);
                if(Convert.ToInt32(pile)!=0)throw new Exception("Wrong-target gameplay initializer ran");
                GD.Print("PASS: opposite-target assembly discovered, actual Entry.Initialize skips gameplay and shows one warning before Ritsu initialization");
                warning.Free();warning=null;
                for(int i=0;i<3;i++){GC.Collect();GC.WaitForPendingFinalizers();await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
                GD.Print("FROSTSWORN_TESTS_PASS");GetTree().Quit(0);return;
            }
            string tests = Path.GetFullPath(Path.Combine(root, "Tests/Frostsworn.Tests.dll"));
            var asm = engineContext.LoadFromAssemblyPath(tests);
            var task = (Task)asm.GetType("Frostsworn.Tests.Suite")!.GetMethod("Run")!.Invoke(null, null)!;
            await task;
            task=null!;
            // Let deferred frees and managed Godot wrappers finish before the
            // standalone engine shuts down, particularly card/tooltip fonts.
            foreach(var child in GetTree().Root.GetChildren())if(child!=this)child.QueueFree();
            for(int i=0;i<3;i++)
            {
                GC.Collect();GC.WaitForPendingFinalizers();
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            }
            GD.Print("FROSTSWORN_TESTS_PASS");
            GetTree().Quit(0);
        }
        catch (Exception e)
        {
            GD.PrintErr(e.ToString());
            GetTree().Quit(1);
        }
    }
}
