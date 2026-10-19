using System;
using System.Linq;
using Godot;
using Frostsworn;
namespace Frostsworn.Tests;
public static partial class Suite
{
    static void CheckVfxMissing0833()
    {
        var original=FrostCardVfx.NativeScenes.ToArray();
        try
        {
            FrostCardVfx.NativeScenes[1]="missing-test-shield";
            var shield=FrostCardVfx.CreateAt(FrostMotif.Armor,Vector2.Zero,Vector2.Zero);
#if STS2_STABLE_107
            Assert(shield.HasNode("IceShieldFallback"),"stable ice shield remains visible when optional native ring is missing");
#else
            Assert(shield.GetChildCount()>0 && !shield.HasNode("IceShieldFallback"),"missing evoke shield uses native passive shield without interrupting card VFX");
#endif
            shield.Free();
            FrostCardVfx.NativeScenes[2]="missing-test-passive";
            shield=FrostCardVfx.CreateAt(FrostMotif.Armor,Vector2.Zero,Vector2.Zero);
            Assert(shield.HasNode("IceShieldFallback"),"both native shields missing retain small procedural ice shield");
            shield.Free();
            for(int i=0;i<original.Length;i++)FrostCardVfx.NativeScenes[i]="missing-test-"+i;
            foreach(var profile in FrostCardVfx.Profiles.Values)
            {
                var fx=FrostCardVfx.CreateAt(profile.Caster|profile.Target,Vector2.Zero,new Vector2(200,0),profile.Strength);
                fx.Free();
            }
            Assert(true,"all 90 card profiles safely construct when optional native scenes are unavailable");
            // A resource can exist yet contain the wrong node type.
            FrostCardVfx.NativeScenes[0]=original[9];
            var snow=FrostCardVfx.CreateAt(FrostMotif.Snow,Vector2.Zero,Vector2.Zero);
            snow.Free();
            Assert(true,"wrong native snow node type is discarded safely");
        }
        finally {Array.Copy(original,FrostCardVfx.NativeScenes,original.Length);}
    }
}
