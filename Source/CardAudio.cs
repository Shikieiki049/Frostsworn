using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Helpers;
namespace Frostsworn;

public sealed record FrostSound(string Path,float Volume);
public sealed record FrostSoundProfile(FrostSound[] Cast,FrostSound[] Hit,string Intent);

// Native FMOD one-shots only; no loops, gameplay RNG or synchronized delays.
public static class FrostCardAudio
{
    public const string Frost="event:/sfx/characters/defect/defect_frost_channel";
    public const string Glass="event:/sfx/characters/defect/defect_glass_channel";
    public const string Beam="event:/sfx/characters/defect/defect_hyperbeam";
    public const string Slash="event:/sfx/characters/silent/silent_attack";
    public const string Magic="event:/sfx/enemy/enemy_impact_enemy_size/enemy_impact_magic";
    public const string Stone="event:/sfx/enemy/enemy_impact_enemy_size/enemy_impact_stone";
    public const string Break="event:/sfx/block_break";
    public const string Shimmer="event:/sfx/ui/enchant_shimmer";
    public const string Whoosh="event:/sfx/ui/enchant_whoosh";
    public static readonly IReadOnlyDictionary<Type,FrostSoundProfile> Profiles=BuildProfiles();
    private static readonly Dictionary<string,ulong> LastPlayed=new();
    private static Dictionary<Type,FrostSoundProfile> BuildProfiles()
    {
        var result=new Dictionary<Type,FrostSoundProfile>();
        void Add(Type[] cards,FrostSound[] cast,FrostSound[] hit,string intent)
        {foreach(var card in cards)result.Add(card,new(cast,hit,intent));}
        Add([typeof(IceCrystal)],[new(Frost,.35f),new(Glass,.35f)],[new(Break,.27f),new(Magic,.38f)],"冰晶凝结召唤；抵达目标时碎裂");
        Add([typeof(FreezeRay),typeof(AbsoluteBeam)],[new(Frost,.5f),new(Beam,.32f)],[new(Magic,.3f)],"寒霜充能与短光束；魔法命中");
        Add([typeof(PrismBlast)],[new(Glass,.45f),new(Beam,.32f)],[new(Glass,.3f),new(Magic,.3f)],"棱晶共振与光束冲击");
        Add([typeof(FrostShatter),typeof(IceBreak)],[],[new(Break,.4f),new(Glass,.3f)],"破冰脆裂与晶体共振");
        Add([typeof(IceChisel),typeof(PiercingCold),typeof(ThinIce)],[],[new(Slash,.38f),new(Glass,.23f)],"冰凿与冰刺穿入");
        Add([typeof(TwinBlades),typeof(Crystallize),typeof(IceHarvest),typeof(MeltSlash),typeof(StepSnow),typeof(WhiteMist),typeof(SnowUnsheathed),typeof(WhiteNightRush),typeof(DeepCache),typeof(SnowSweep)],[],[new(Slash,.42f),new(Glass,.2f)],"逐段冰刃斩击，附轻微晶响");
        Add([typeof(ColdHammer)],[],[new(Stone,.55f),new(Glass,.35f)],"重锤钝击与碎冰");
        Add([typeof(Avalanche),typeof(SnowFinale),typeof(SnowRoll),typeof(SnowCharge)],[new(Whoosh,.4f),new(Frost,.45f)],[new(Glass,.25f)],"卷雪冲击与冰屑");
        Add([typeof(IceBind),typeof(AbsoluteZero),typeof(SpreadFrost)],[new(Frost,.6f),new(Shimmer,.25f)],[],"寒霜凝结与冰封咒印");
        Add([typeof(RimeCoat),typeof(GlacierBody),typeof(IceMirror),typeof(PermafrostWard)],[new(Frost,.5f),new(Glass,.25f)],[],"覆体冰甲与结界凝结");
        Add([typeof(CrystalCluster),typeof(GlacialCore),typeof(CrystalEdge),typeof(CrystalAmulet),typeof(Glitter),typeof(CrystalVolley)],[new(Glass,.45f),new(Shimmer,.25f)],[],"凝晶与晶体强化");
        Add([typeof(ExpandColdStore),typeof(SealSpell),typeof(WinterArchive),typeof(FrozenMoment),typeof(CrystalRite)],[new(Frost,.5f)],[],"封存与冻结咒环");
        Add([typeof(MeltSeal),typeof(WakeIce),typeof(ThawEnergy),typeof(Dissolve),typeof(IceRelease)],[new(Whoosh,.4f),new(Shimmer,.25f)],[],"解封与能流释放");
        Add([typeof(FatedStory)],[new(Shimmer,.45f),new(Whoosh,.3f)],[],"命定咒环展开");
        Add([typeof(CrystalResonance)],[new(Glass,.45f),new(Shimmer,.25f)],[],"晶群共鸣");
        Add([typeof(StormCore),typeof(EndlessStorm),typeof(BlizzardComing)],[new(Whoosh,.45f),new(Frost,.55f)],[],"风暴聚流与寒潮蓄势");
        return result;
    }
    public static IEnumerable<string> EventPaths=>Profiles.Values.SelectMany(p=>p.Cast.Concat(p.Hit)).Select(s=>s.Path).Distinct();
    public static void Cast(FrostCard card)
    {
        // Attack casts are tied to a real attack command; ice crystals to their
        // actual summon/arrival animation. Failed attacks produce no added sound.
        if(card.Spec.Type==CardType.Attack || !Profiles.TryGetValue(card.GetType(),out var profile))return;
        Play(profile.Cast);
    }
    public static void ConfigureAttack(FrostCard card,AttackCommand command)
    {
        if(card is IceCrystal || !Profiles.TryGetValue(card.GetType(),out var profile))return;
        if(profile.Cast.Length>0)
        {
            bool castPlayed=false;
            command.WithAttackerFx(()=>{if(!castPlayed){castPlayed=true;Play(profile.Cast);}return null;});
        }
        if(profile.Hit.Length>0)command.WithHitVfxNode(_=>{Play(profile.Hit);return null;});
    }
    public static void CrystalSummon()=>Play(Profiles[typeof(IceCrystal)].Cast);
    public static void CrystalImpact()=>Play(Profiles[typeof(IceCrystal)].Hit);
    // This per-event local limiter folds simultaneous area hits into one cue.
    // Later hits may still sound; no combat state, counters or random streams change.
    public static bool ShouldPlay(ulong now,ulong? last)=>last is null || now-last.Value>=70;
    private static void Play(IEnumerable<FrostSound> sounds)
    {
        if(TestMode.IsOn || NonInteractiveMode.IsActive || NAudioManager.Instance==null)return;
        ulong now=Time.GetTicksMsec();
        foreach(var sound in sounds)
        {
            if(!ShouldPlay(now,LastPlayed.TryGetValue(sound.Path,out var last)?last:null))continue;
            LastPlayed[sound.Path]=now;
            SfxCmd.Play(sound.Path,sound.Volume);
        }
    }
}
