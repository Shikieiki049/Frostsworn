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
            string game = "C:/fz/steam/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64";
            string ritsu = Path.GetFullPath(Path.Combine(root, "../work/ritsulib-package/lib/net9.0"));
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
            string tests = Path.GetFullPath(Path.Combine(root, "Tests/Frostsworn.Tests.dll"));
            var asm = engineContext.LoadFromAssemblyPath(tests);
            var task = (Task)asm.GetType("Frostsworn.Tests.Suite")!.GetMethod("Run")!.Invoke(null, null)!;
            await task;
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
