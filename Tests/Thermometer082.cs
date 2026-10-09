using Godot;
using Frostsworn;
namespace Frostsworn.Tests;
public static partial class Suite
{
    static async System.Threading.Tasks.Task CheckThermometer082()
    {
        var canvas=new Control();
        var ratios=new[]{0.0,0.25,0.65,0.9,1.0};
        for(int i=0;i<ratios.Length;i++)
        {
            var row=new Control { Position=new Vector2(32,24+i*60),Size=new Vector2(240,32),Scale=new Vector2(2,2) };
            row.Position=new Vector2(32,24+i*76);canvas.AddChild(row);
            var bar=FrostThermometer.Create(row);bar.MaxValue=40;bar.Value=ratios[i]*40;FrostThermometer.Refresh(bar);
            Assert(row.GetNode<Node2D>("ThermometerFrost").Visible==(ratios[i]>=0.75),"frost crystal visibility at "+ratios[i]);
            Assert(bar.MouseFilter==Control.MouseFilterEnum.Ignore,"thermometer does not intercept parent hover");
            Assert(bar.GetNode("ThermometerTicks").GetChildCount()==9,"thermometer ticks preserved");
            row.AddChild(new Label {Text=$"{bar.Value:0} / 40",Position=new Vector2(170,4)});
        }
        var tree=(SceneTree)Engine.GetMainLoop();
        await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);tree.Root.AddChild(canvas);
        await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        foreach(var row in canvas.GetChildren()){var bar=row.GetNode<ProgressBar>("FrostswornFrostBar");bar.Size=new Vector2(146,12);Assert(bar.Size.Y==12,"thermometer stem fits twelve-pixel height after theme settles");}
        void Own(Node n){foreach(var c in n.GetChildren()){c.Owner=canvas;Own(c);}}
        Own(canvas);var packed=new PackedScene();Assert(packed.Pack(canvas)==Error.Ok,"pack thermometer preview");
        Assert(ResourceSaver.Save(packed,ProjectSettings.GlobalizePath("res://../../../work/thermometer082.tscn"))==Error.Ok,"save thermometer preview");
        canvas.Free();GD.Print("THERMOMETER082_COMPLETE");
    }
}
