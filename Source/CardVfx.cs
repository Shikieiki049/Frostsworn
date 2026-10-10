using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.TestSupport;
namespace Frostsworn;

[Flags]
public enum FrostMotif
{
    None=0, Snow=1, Armor=2, Shards=4, Slash=8, Pierce=16, Impact=32,
    Beam=64, Crystal=128, Store=256, Thaw=512, Rune=1024, Focus=2048,
    Energy=4096, Warmth=8192, Wind=16384, Mist=32768, Bind=65536,
    Prism=131072, Blood=262144, Echo=524288, Storm=1048576
}
public sealed record FrostVfxProfile(FrostMotif Caster,FrostMotif Target,string Intent,float Strength=1f);

// Explicit, language-independent choices. No card text parsing or gameplay RNG.
public static class FrostCardVfx
{
    private const string Vfx="res://scenes/vfx/";
    public static readonly Color Ice=new("B8F4FF");
    public static readonly Color Blue=new("7797F7");
    private static readonly Color Warm=new("FFE3AB");
    private static readonly Color Blood=new("EC7296");
    public static readonly IReadOnlyDictionary<Type,FrostVfxProfile> Profiles=new Dictionary<Type,FrostVfxProfile>
    {
        [typeof(CrystalRite)]=new(FrostMotif.Store,FrostMotif.None,"封入冷库的冰环"),
        [typeof(IceCrystal)]=new(FrostMotif.None,FrostMotif.Crystal | FrostMotif.Shards,"冰晶飞刺与碎晶",0.65f),
        [typeof(FrostShatter)]=new(FrostMotif.None,FrostMotif.Shards | FrostMotif.Slash,"碎冰后裂斩"),
        [typeof(FreezeRay)]=new(FrostMotif.None,FrostMotif.Beam | FrostMotif.Mist,"细寒光束与凝霜"),
        [typeof(RimeCoat)]=new(FrostMotif.Armor,FrostMotif.None,"覆体冰甲"),
        [typeof(Crystallize)]=new(FrostMotif.None,FrostMotif.Slash | FrostMotif.Prism,"每次棱镜裂斩分别闪光"),
        [typeof(CrystalCluster)]=new(FrostMotif.Crystal,FrostMotif.None,"多枚冰晶凝结"),
        [typeof(SnowCache)]=new(FrostMotif.Store | FrostMotif.Snow,FrostMotif.None,"封存并积聚雪势"),
        [typeof(MeltSeal)]=new(FrostMotif.Thaw,FrostMotif.None,"封印融解"),
        [typeof(WakeIce)]=new(FrostMotif.Thaw | FrostMotif.Shards,FrostMotif.None,"破开冰封唤醒"),
        [typeof(OverdrawWarmth)]=new(FrostMotif.Energy | FrostMotif.Mist,FrostMotif.None,"能量回流与降温"),
        [typeof(RapidCalculation)]=new(FrostMotif.Focus,FrostMotif.None,"紧凑演算光环",0.8f),
        [typeof(Rewarm)]=new(FrostMotif.Warmth,FrostMotif.None,"淡金暖意驱散寒冷"),
        [typeof(ExpandColdStore)]=new(FrostMotif.Store | FrostMotif.Rune,FrostMotif.None,"冷库边界展开"),
        [typeof(SlowRelease)]=new(FrostMotif.Thaw | FrostMotif.Rune,FrostMotif.None,"解封装置启动"),
        [typeof(ShieldCounter)]=new(FrostMotif.None,FrostMotif.Armor | FrostMotif.Impact,"冰盾撞击"),
        [typeof(IceHarvest)]=new(FrostMotif.None,FrostMotif.Slash | FrostMotif.Crystal,"采冰切割与晶屑"),
        [typeof(SnowRoll)]=new(FrostMotif.None,FrostMotif.Wind | FrostMotif.Snow,"横向卷雪"),
        [typeof(Avalanche)]=new(FrostMotif.None,FrostMotif.Storm | FrostMotif.Shards,"厚重落雪与冰块爆裂",1.2f),
        [typeof(SnowSweep)]=new(FrostMotif.None,FrostMotif.Slash | FrostMotif.Snow,"扫雪横斩"),
        [typeof(TwinBlades)]=new(FrostMotif.None,FrostMotif.Slash | FrostMotif.Shards,"每次连斩各有冰屑"),
        [typeof(ColdHammer)]=new(FrostMotif.None,FrostMotif.Impact | FrostMotif.Shards,"重锤冰屑冲击",1.15f),
        [typeof(MeltSlash)]=new(FrostMotif.Thaw,FrostMotif.Slash,"融封后斩击"),
        [typeof(WhiteMist)]=new(FrostMotif.None,FrostMotif.Mist | FrostMotif.Slash,"雾中冰刃"),
        [typeof(IceBind)]=new(FrostMotif.None,FrostMotif.Bind | FrostMotif.Mist,"寒霜缠缚"),
        [typeof(AbsoluteZero)]=new(FrostMotif.None,FrostMotif.Storm | FrostMotif.Bind,"全体深冻",1.1f),
        [typeof(ShellRecycle)]=new(FrostMotif.Shards | FrostMotif.Armor,FrostMotif.None,"碎壳重结屏障"),
        [typeof(SealSpell)]=new(FrostMotif.Store | FrostMotif.Rune,FrostMotif.None,"封存咒环"),
        [typeof(BlizzardComing)]=new(FrostMotif.Storm | FrostMotif.Rune,FrostMotif.None,"暴雪蓄势与守护"),
        [typeof(Snowline)]=new(FrostMotif.None,FrostMotif.Wind | FrostMotif.Snow,"雪线横向推进"),
        [typeof(ThawFrost)]=new(FrostMotif.Armor,FrostMotif.Shards,"碎霜转化冰甲"),
        [typeof(ColdReturn)]=new(FrostMotif.Snow | FrostMotif.Armor,FrostMotif.None,"寒潮回流凝成护甲"),
        [typeof(StepSnow)]=new(FrostMotif.None,FrostMotif.Slash | FrostMotif.Wind,"疾风踏雪追斩"),
        [typeof(ThinIce)]=new(FrostMotif.None,FrostMotif.Pierce | FrostMotif.Mist,"薄冰穿刺",0.8f),
        [typeof(SpringFlood)]=new(FrostMotif.Thaw | FrostMotif.Wind,FrostMotif.None,"冷库解封与春汛"),
        [typeof(DeepCache)]=new(FrostMotif.Store,FrostMotif.Slash,"藏刃斩击并封存"),
        [typeof(GlacierBody)]=new(FrostMotif.Armor | FrostMotif.Rune,FrostMotif.None,"冰川护甲形态",1.1f),
        [typeof(StormCore)]=new(FrostMotif.Storm | FrostMotif.Rune,FrostMotif.None,"风暴核心聚流"),
        [typeof(SnowEye)]=new(FrostMotif.Focus | FrostMotif.Snow,FrostMotif.None,"雪眼凝聚"),
        [typeof(IceMirror)]=new(FrostMotif.Armor | FrostMotif.Prism,FrostMotif.None,"冰镜反光与守护"),
        [typeof(GlacialCore)]=new(FrostMotif.Crystal | FrostMotif.Rune,FrostMotif.None,"极寒晶核聚合"),
        [typeof(PrismBlast)]=new(FrostMotif.None,FrostMotif.Beam | FrostMotif.Prism,"棱光熔铸闪击",1.05f),
        [typeof(IceChisel)]=new(FrostMotif.None,FrostMotif.Pierce | FrostMotif.Shards,"冰凿穿入裂冰"),
        [typeof(Frostfire)]=new(FrostMotif.None,FrostMotif.Warmth | FrostMotif.Mist,"霜火双色反冲"),
        [typeof(CrystalVolley)]=new(FrostMotif.Crystal,FrostMotif.Crystal | FrostMotif.Prism,"晶簇齐射与棱光"),
        [typeof(IceDust)]=new(FrostMotif.Armor | FrostMotif.Snow,FrostMotif.None,"冰尘落于护甲"),
        [typeof(PermafrostWard)]=new(FrostMotif.Armor | FrostMotif.Store,FrostMotif.None,"冻土结界封存"),
        [typeof(WarmthRecovery)]=new(FrostMotif.Warmth | FrostMotif.Crystal,FrostMotif.None,"余热重新凝晶"),
        [typeof(ThawEnergy)]=new(FrostMotif.Thaw | FrostMotif.Energy,FrostMotif.None,"解冻转化能量"),
        [typeof(CrystalFurnace)]=new(FrostMotif.Crystal | FrostMotif.Energy,FrostMotif.None,"冰晶回收转能量"),
        [typeof(CrystalEdge)]=new(FrostMotif.Crystal | FrostMotif.Rune,FrostMotif.None,"晶锋强化"),
        [typeof(CrystalAmulet)]=new(FrostMotif.Crystal | FrostMotif.Armor,FrostMotif.None,"护符凝晶与守护"),
        [typeof(IceCellar)]=new(FrostMotif.Store | FrostMotif.Focus,FrostMotif.None,"冰窖检索"),
        [typeof(DelayedChill)]=new(FrostMotif.None,FrostMotif.Pierce | FrostMotif.Mist,"寒芒与残留寒雾"),
        [typeof(PiercingCold)]=new(FrostMotif.None,FrostMotif.Pierce | FrostMotif.Shards,"每次追加攻击均穿刺"),
        [typeof(SnowFinale)]=new(FrostMotif.None,FrostMotif.Slash | FrostMotif.Storm,"雪蚀终章；回血沿用原版回血特效"),
        [typeof(SpreadFrost)]=new(FrostMotif.Snow,FrostMotif.Mist | FrostMotif.Bind,"寒霜蔓延全体"),
        [typeof(FrostCarving)]=new(FrostMotif.Store | FrostMotif.Crystal,FrostMotif.None,"霜刻封存晶印"),
        [typeof(FrozenMoment)]=new(FrostMotif.Store | FrostMotif.Echo,FrostMotif.None,"冻结时刻的层叠冰环"),
        [typeof(PhaseShift)]=new(FrostMotif.Armor,FrostMotif.Shards | FrostMotif.Prism,"碎冰相变为护甲"),
        [typeof(ColdSearch)]=new(FrostMotif.Store | FrostMotif.Focus,FrostMotif.None,"检索急冻"),
        [typeof(BloodWinter)]=new(FrostMotif.Blood | FrostMotif.Store,FrostMotif.None,"血色封冬咒环"),
        [typeof(DebtSettlement)]=new(FrostMotif.Thaw | FrostMotif.Focus,FrostMotif.None,"霜债解除与清算"),
        [typeof(QuietMeditation)]=new(FrostMotif.Focus | FrostMotif.Snow,FrostMotif.None,"静雪冥想",0.75f),
        [typeof(CrystalResonance)]=new(FrostMotif.Crystal | FrostMotif.Echo,FrostMotif.None,"晶群共鸣环"),
        [typeof(WinterArchive)]=new(FrostMotif.Store | FrostMotif.Snow,FrostMotif.None,"漫冬封存"),
        [typeof(EndlessStorm)]=new(FrostMotif.Storm | FrostMotif.Rune,FrostMotif.None,"永夜风暴旋流"),
        [typeof(SixfoldSnow)]=new(FrostMotif.Snow | FrostMotif.Armor,FrostMotif.None,"积雪护甲"),
        [typeof(HiddenBlade)]=new(FrostMotif.Crystal | FrostMotif.Rune,FrostMotif.None,"封藏晶刃秘术"),
        [typeof(ColdBloodEcho)]=new(FrostMotif.Blood | FrostMotif.Armor,FrostMotif.None,"寒血回响凝甲"),
        [typeof(HeatExchange)]=new(FrostMotif.Energy | FrostMotif.Warmth,FrostMotif.None,"热交换能流"),
        [typeof(SnowBloom)]=new(FrostMotif.Store | FrostMotif.Crystal,FrostMotif.None,"霜华晶花与封存"),
        [typeof(Dissolve)]=new(FrostMotif.Thaw | FrostMotif.Warmth,FrostMotif.None,"暖光消融"),
        [typeof(SilverFrost)]=new(FrostMotif.Storm | FrostMotif.Wind,FrostMotif.None,"银白风霜"),
        [typeof(FrozenWish)]=new(FrostMotif.Store | FrostMotif.Rune,FrostMotif.None,"心愿凝成冰印"),
        [typeof(Glitter)]=new(FrostMotif.Crystal | FrostMotif.Prism,FrostMotif.None,"晶莹锐晶闪光"),
        [typeof(BlessingWind)]=new(FrostMotif.Wind | FrostMotif.Armor,FrostMotif.None,"祝福之风守护"),
        [typeof(HeartInscription)]=new(FrostMotif.Rune | FrostMotif.Energy,FrostMotif.None,"力量铭刻"),
        [typeof(FlowingPower)]=new(FrostMotif.Crystal | FrostMotif.Rune,FrostMotif.None,"流动晶核强化"),
        [typeof(FrostBite)]=new(FrostMotif.None,FrostMotif.Mist | FrostMotif.Shards,"寒霜侵蚀爆裂"),
        [typeof(IceBreak)]=new(FrostMotif.None,FrostMotif.Shards | FrostMotif.Storm,"全体冰断",1.1f),
        [typeof(SnowUnsheathed)]=new(FrostMotif.None,FrostMotif.Slash | FrostMotif.Mist,"冰冷出鞘"),
        [typeof(HypothermiaStrike)]=new(FrostMotif.None,FrostMotif.Impact | FrostMotif.Mist,"失温冲击"),
        [typeof(WhiteNightRush)]=new(FrostMotif.Store,FrostMotif.Slash | FrostMotif.Wind,"白夜横袭与再封存"),
        [typeof(Pierce)]=new(FrostMotif.None,FrostMotif.Pierce | FrostMotif.Crystal,"逐段贯穿所有敌人",0.85f),
        [typeof(SnowCharge)]=new(FrostMotif.Snow,FrostMotif.Impact | FrostMotif.Wind,"踏雪重击破阵"),
        [typeof(FatedStory)]=new(FrostMotif.Echo | FrostMotif.Rune,FrostMotif.None,"命定重放的双重咒环"),
        [typeof(AbsoluteBeam)]=new(FrostMotif.None,FrostMotif.Beam | FrostMotif.Bind,"极冻光束命中后冰封",1.15f),
        [typeof(GuardAdvance)]=new(FrostMotif.Armor | FrostMotif.Snow,FrostMotif.None,"冰甲雪势守护"),
        [typeof(IceRelease)]=new(FrostMotif.Thaw | FrostMotif.Energy,FrostMotif.None,"冰释释放能流"),
    };

    public static readonly string[] NativeScenes={
        "orbs/frost/vfx_frost_orb_snow", "orbs/frost/vfx_frost_orb_evoke_shield",
        "orbs/frost/vfx_frost_orb_passive_shield", "orbs/glass/vfx_glass_orb_evoke_impact",
        "common/vfx_common_specks", "common/vfx_common_clouds", "common/vfx_common_ring_polar_a",
        "common/vfx_common_hit_flare", "slash/vfx_slash_core", "vfx_rock_shatter"
    };
    public static IEnumerable<string> AssetPaths=>NativeScenes.Select(p=>Vfx+p+".tscn").Concat(new[]{
        "res://images/vfx/orbs/frost_orb_particle.png"});

    public static void ConfigureAttack(FrostCard card,AttackCommand command)
    {
        if(Profiles.TryGetValue(card.GetType(),out var profile) && profile.Target!=FrostMotif.None)
            command.WithHitVfxNode(target=>Impact(card,target,profile));
    }
    private static Node2D? Impact(FrostCard card,Creature target,FrostVfxProfile profile)
    {
        if(TestMode.IsOn || target.IsDead || target.GetCreatureNode() is not {} victim || target.GetVfxContainer()==null)return null;
        var caster=card.Owner.Creature.GetCreatureNode();
        var source=caster?.VfxSpawnPosition ?? victim.VfxSpawnPosition;
        if(profile.Target.HasFlag(FrostMotif.Beam) && caster!=null)source=BeamOrigin(caster.Visuals,source);
        if(card is IceCrystal && caster!=null)source=CrystalOrigin(caster.Visuals,source);
        if(card is IceCrystal)return CreateCrystalFlight(source,victim.VfxSpawnPosition,profile.Strength);
        return CreateAt(profile.Target,source,victim.VfxSpawnPosition,profile.Strength);
    }
    public static Vector2 BeamOrigin(Node2D visuals,Vector2 fallback)
    {
        // Approved idle mesh coordinates: the crystal in the staff head.
        // Transform through the rig so each player's position/scale is respected.
        return visuals.FindChild("IdleLoop",true,false) is Node2D idle
            ?idle.ToGlobal(new Vector2(510,-1055)):fallback;
    }
    public static Node2D CreateCrystalFlight(Vector2 source,Vector2 target,float strength=.65f)
    {
        var root=new Node2D {Name="FrostCrystalFlight",Position=target};
        var from=source-target;
        var summon=CreateCrystalSigil(from,-from);
        summon.TreeEntered+=()=>{
            var fade=summon.CreateTween();fade.TweenInterval(.18);fade.TweenProperty(summon,"modulate:a",0f,.25);
        };
        root.AddChild(summon);
        var projectile=CreateFacetedCrystal();projectile.Name="FlyingCrystal";
        projectile.Position=from;projectile.Rotation=(-from).Angle();
        var crystalScale=Vector2.One*Math.Clamp(strength/.65f,.8f,1.2f);
        projectile.Scale=crystalScale;
        root.AddChild(projectile);
        root.TreeEntered+=()=>{
            var flight=root.CreateTween();
            root.SetMeta("flight_tween",flight);
            flight.TweenProperty(projectile,"modulate:a",1f,.14).From(0f);
            flight.Parallel().TweenProperty(projectile,"scale",crystalScale,.14).From(crystalScale*.35f);
            flight.TweenProperty(projectile,"position",Vector2.Zero,.22);
            flight.TweenCallback(Callable.From(()=>{
                projectile.QueueFree();
                root.AddChild(CreateAt(FrostMotif.Crystal|FrostMotif.Shards,Vector2.Zero,Vector2.Zero,strength));
            }));
            flight.TweenInterval(3.5);
            flight.TweenCallback(Callable.From(()=>root.QueueFree()));
        };
        return root;
    }
    public static Node2D CreateFacetedCrystal()
    {
        // Thick asymmetric crystal with separately lit facets, not a particle dart.
        var crystal=new Node2D {Name="FacetedIceCrystal"};
        Vector2[] rim={new(18,0),new(5,-12),new(-12,-8),new(-18,3),new(-4,12),new(9,8)};
        var ridge=new Vector2(-1,-1);
        string[] colors={"E6FBFF","A4E3FF","638FDB","477AC0","87CDF2","C7F4FF"};
        for(int i=0;i<rim.Length;i++)crystal.AddChild(new Polygon2D {
            Polygon=new[]{ridge,rim[i],rim[(i+1)%rim.Length]},Color=new Color(colors[i]),Antialiased=true});
        crystal.AddChild(new Line2D {Points=rim,Closed=true,Width=.9f,DefaultColor=new Color("86CDEB"),Antialiased=true});
        crystal.AddChild(new Line2D {Points=new[]{rim[2],ridge,rim[0]},Width=.8f,DefaultColor=new Color(Ice,.8f),Antialiased=true});
        return crystal;
    }
    public static Node2D CreateCrystalSigil(Vector2 position,Vector2? direction=null)
    {
        var sigil=new Node2D {Name="SummonCircle",Position=position,Rotation=(direction??Vector2.Right).Angle()};
        // A vertical portal, its plane perpendicular to the outgoing crystal.
        Vector2 Tilt(Vector2 p)=>new(p.X*.55f,p.Y);
        var strokes=new List<(Line2D Line,Vector2[] Points)>();
        void Ink(IEnumerable<Vector2> points,Color color,float width=1,bool closed=false)
        {
            var path=points.Select(Tilt).ToList();if(closed)path.Add(path[0]);
            var line=new Line2D {Points=new[]{path[0],path[0]},DefaultColor=color,Width=width,Antialiased=true,
                BeginCapMode=Line2D.LineCapMode.Round,EndCapMode=Line2D.LineCapMode.Round};
            strokes.Add((line,path.ToArray()));sigil.AddChild(line);
        }
        Ink(Enumerable.Range(0,64).Select(i=>Vector2.FromAngle(i*Mathf.Tau/64)*32),new Color(Blue,.8f),1.2f,true);
        Ink(Enumerable.Range(0,64).Select(i=>Vector2.FromAngle(i*Mathf.Tau/64)*25),new Color(Ice,.6f),.8f,true);
        Ink(Enumerable.Range(0,6).Select(i=>Vector2.FromAngle(i*Mathf.Tau/6)*5),Ice,1,true);
        for(int i=0;i<6;i++)
        {
            var axis=Vector2.FromAngle(-Mathf.Pi/2+i*Mathf.Tau/6);var side=axis.Orthogonal();
            Ink(new[]{axis*6,axis*21},Ice,1.2f);
            Ink(new[]{axis*12+side*5,axis*16,axis*12-side*5},new Color(Ice,.85f),1);
            Ink(new[]{axis*28,axis*30+side*2,axis*32,axis*30-side*2},Ice,1,true);
        }
        sigil.TreeEntered+=()=>{
            var draw=sigil.CreateTween();sigil.SetMeta("draw_tween",draw);
            draw.TweenMethod(Callable.From<float>(progress=>{
                foreach(var (line,path) in strokes)
                {
                    float cursor=progress*(path.Length-1);int end=Math.Min((int)cursor,path.Length-1);
                    var visible=path.Take(end+1).ToList();
                    if(end<path.Length-1)visible.Add(path[end].Lerp(path[end+1],cursor-end));
                    line.Points=visible.ToArray();
                }
            }),0f,1f,.12);
        };
        return sigil;
    }
    public static Vector2 CrystalOrigin(Node2D visuals,Vector2 fallback)
    {
        // Cosmetic randomness only: never use the run/combat RNG streams.
        float x=-260+System.Random.Shared.NextSingle()*910;
        float y=-1040+System.Random.Shared.NextSingle()*300;
        return visuals.FindChild("IdleLoop",true,false) is Node2D idle
            ?idle.ToGlobal(new Vector2(x,y))
            :fallback+new Vector2(x*.15f,(y+800)*.15f);
    }
    public static void Cast(FrostCard card,CardPlay play)
    {
        if(TestMode.IsOn || !Profiles.TryGetValue(card.GetType(),out var profile))return;
        if(card is GuardAdvance && card.ResolveEnergyXValue()==0)return;
        var owner=card.Owner.Creature;
        if(owner.GetCreatureNode() is not {} caster || owner.GetVfxContainer() is not {} container)return;
        if(profile.Caster!=FrostMotif.None)
        {
            var node=CreateAt(profile.Caster,caster.VfxSpawnPosition,caster.VfxSpawnPosition,profile.Strength);
            container.AddChild(node);node.GlobalPosition=caster.VfxSpawnPosition;
        }
        // Attack target FX belong to actual hits, including extra hits, not casts.
        if(card.Spec.Type==CardType.Attack || profile.Target==FrostMotif.None)return;
        IEnumerable<Creature> targets=card.Spec.Target==TargetType.AllEnemies
            ?owner.CombatState!.HittableEnemies:play.Target is {} t?new[]{t}:Array.Empty<Creature>();
        foreach(var target in targets)
            if(Impact(card,target,profile) is {} node && target.GetVfxContainer() is {} targetContainer)
            {targetContainer.AddChild(node);node.GlobalPosition=target.GetCreatureNode()!.VfxSpawnPosition;}
    }
    // Also used by the engine render checks. Coordinates are combat VFX centers,
    // never portrait texture dimensions or a player-specific screen position.
    public static Node2D CreateAt(FrostMotif motifs,Vector2 source,Vector2 target,float strength=1f)
    {
        var root=new Node2D {Name="FrostCardFx",Position=target};
        float size=Math.Clamp(strength,.6f,1.2f);
        void Part(string scene,float scale=1f,Color? color=null,Vector2? offset=null,float angle=0)
        {
            var node=ResourceLoader.Load<PackedScene>(Vfx+scene+".tscn").Instantiate<Node2D>();
            node.Scale*=scale*size;node.Position=offset??Vector2.Zero;node.Rotation+=angle;
            Prepare(node,color??Ice);root.AddChild(node);
        }
        if(motifs.HasFlag(FrostMotif.Snow) || motifs.HasFlag(FrostMotif.Storm))
        {
            bool storm=motifs.HasFlag(FrostMotif.Storm);
            var snow=ResourceLoader.Load<PackedScene>(Vfx+NativeScenes[0]+".tscn").Instantiate<GpuParticles2D>();
            Prepare(snow,Ice);snow.OneShot=true;snow.Amount=storm?64:28;snow.Lifetime=1.15;
            snow.Explosiveness=.75f;snow.Position=new(0,-85);snow.Scale=new(size,size);
            float dotSize=Math.Max(1,snow.Texture.GetWidth());
            snow.ProcessMaterial=new ParticleProcessMaterial {
                EmissionShape=ParticleProcessMaterial.EmissionShapeEnum.Box,EmissionBoxExtents=new(storm?125:90,40,0),
                Direction=new(0,1,0),Spread=25,Gravity=new(0,75,0),InitialVelocityMin=25,InitialVelocityMax=75,
                ScaleMin=3f/dotSize,ScaleMax=6f/dotSize,Color=new Color(Ice,.75f)
            };
            root.AddChild(snow);
            if(storm)Part(NativeScenes[5],.45f,new Color(Ice,.3f));
        }
        if(motifs.HasFlag(FrostMotif.Armor))Part(NativeScenes[1],.85f);
        if(motifs.HasFlag(FrostMotif.Shards))
        {
            // The original rock-shatter motion with ice pieces instead of stones
            // and no dark rock dust. Crystal sprites make the debris readable.
            Part(NativeScenes[9],.4f);
            ScatterCrystals(root,9,size,Ice);
        }
        if(motifs.HasFlag(FrostMotif.Slash))
        {
            Part(NativeScenes[8],.7f,Ice,angle:-.35f);
            Part(NativeScenes[4],.35f);
        }
        if(motifs.HasFlag(FrostMotif.Pierce))
        {
            Streak(root,new(-95,-12),new(95,12),Ice,7*size,.23f);
            Part(NativeScenes[6],.3f);
        }
        if(motifs.HasFlag(FrostMotif.Impact))
        {Part(NativeScenes[7],.75f);Part(NativeScenes[6],.6f);Part(NativeScenes[4],.6f);}
        if(motifs.HasFlag(FrostMotif.Beam))
        {
            Vector2 from=source-target;
            if(from.Length()<20)from=new(-240,0);
            Streak(root,from,Vector2.Zero,new Color(Blue,.55f),24*size,.38f);
            Streak(root,from,Vector2.Zero,Ice,8*size,.32f);
            Part(NativeScenes[7],.5f);
        }
        if(motifs.HasFlag(FrostMotif.Crystal))
        {
            var texture=ResourceLoader.Load<Texture2D>("res://images/vfx/orbs/frost_orb_particle.png");
            for(int i=0;i<3;i++)
            {
                float a=-Mathf.Pi/2+i*Mathf.Tau/3;
                var crystal=new Sprite2D {Texture=texture,Position=Vector2.FromAngle(a)*38*size,Scale=Vector2.One*(26*size/Math.Max(texture.GetWidth(),texture.GetHeight())),Rotation=a+Mathf.Pi/2};
                root.AddChild(crystal);
                crystal.TreeEntered+=()=>{var tween=crystal.CreateTween();tween.SetParallel();tween.TweenProperty(crystal,"position",crystal.Position*1.8f,.42);tween.TweenProperty(crystal,"modulate:a",0f,.52);};
            }
            Part(NativeScenes[1],.45f);
        }
        if(motifs.HasFlag(FrostMotif.Store))
        {
            Part(NativeScenes[2],.55f);
            Ring(root,Ice,true,size);
        }
        if(motifs.HasFlag(FrostMotif.Thaw))
        {Ring(root,new Color(Ice,.65f),false,size);Part(NativeScenes[5],.28f,new Color(Ice,.3f));}
        if(motifs.HasFlag(FrostMotif.Rune))Ring(root,Blue,false,size);
        if(motifs.HasFlag(FrostMotif.Echo))
        {Ring(root,Ice,false,size);Ring(root,new Color(Blue,.45f),false,size*.7f);}
        if(motifs.HasFlag(FrostMotif.Focus))
        {Part(NativeScenes[4],.3f);Ring(root,new Color(Ice,.55f),true,size*.6f);}
        if(motifs.HasFlag(FrostMotif.Energy))Part(NativeScenes[4],.55f,Warm);
        if(motifs.HasFlag(FrostMotif.Warmth))
        {Part(NativeScenes[5],.3f,new Color(Warm,.35f));Part(NativeScenes[4],.25f,Warm);}
        if(motifs.HasFlag(FrostMotif.Wind))
        {
            Part(NativeScenes[5],.45f,new Color(Ice,.3f));
            Streak(root,new(-100,20),new(110,-10),new Color(Ice,.5f),4*size,.35f);
        }
        if(motifs.HasFlag(FrostMotif.Mist))Part(NativeScenes[2],.65f);
        if(motifs.HasFlag(FrostMotif.Bind))
        {Part(NativeScenes[1],.6f,offset:new(0,-35));Part(NativeScenes[2],.6f,offset:new(0,35));Ring(root,Blue,true,size*.7f);}
        if(motifs.HasFlag(FrostMotif.Prism))Part(NativeScenes[3],.65f,Blue);
        if(motifs.HasFlag(FrostMotif.Blood))Ring(root,Blood,true,size*.65f);
        // Local tween only. No Cmd.Wait, synchronized tasks or unbounded emitters.
        root.TreeEntered+=()=>root.CreateTween().TweenInterval(3.4).Finished+=()=>{if(GodotObject.IsInstanceValid(root))root.QueueFree();};
        return root;
    }
    private static void ScatterCrystals(Node2D root,int count,float size,Color tint)
    {
        var texture=ResourceLoader.Load<Texture2D>("res://images/vfx/orbs/frost_orb_particle.png");
        for(int i=0;i<count;i++)
        {
            float a=-Mathf.Pi/2+i*Mathf.Tau/count;
            float radius=(45+(i%3)*17)*size;
            var shard=new Sprite2D {Texture=texture,Modulate=tint,Rotation=a,
                Scale=new Vector2(.65f,1.2f)*(12+(i%3)*3)*size/Math.Max(texture.GetWidth(),texture.GetHeight())};
            root.AddChild(shard);
            shard.TreeEntered+=()=>{var tween=shard.CreateTween();tween.SetParallel();tween.TweenProperty(shard,"position",Vector2.FromAngle(a)*radius+new Vector2(0,30),.42).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);tween.TweenProperty(shard,"rotation",a+.9f,.42);tween.TweenProperty(shard,"modulate:a",0f,.55);};
        }
    }
    private static void Ring(Node2D root,Color color,bool inward,float size)
    {
        var node=ResourceLoader.Load<PackedScene>(Vfx+NativeScenes[6]+".tscn").Instantiate<Node2D>();
        Prepare(node,color);node.Scale=new(size*.75f,size*.45f);root.AddChild(node);
        if(inward)node.TreeEntered+=()=>node.CreateTween().TweenProperty(node,"scale",node.Scale*.35f,.35);
    }
    private static void Streak(Node2D root,Vector2 from,Vector2 to,Color color,float width,float duration)
    {
        // Native Line2D geometry gives a correctly directed, bounded beam. The
        // hit flare and cold particles remain the original game VFX resources.
        var line=new Line2D {Points=new[]{from,to},Width=width,DefaultColor=color,
            Antialiased=true,BeginCapMode=Line2D.LineCapMode.Round,EndCapMode=Line2D.LineCapMode.Round};
        root.AddChild(line);
        line.TreeEntered+=()=>line.CreateTween().TweenProperty(line,"modulate:a",0f,duration);
    }
    private static void Prepare(Node node,Color color)
    {
        // Remove fullscreen distortion and dark rock dust from native scenes.
        foreach(var child in node.GetChildren().ToArray())
            if(child is BackBufferCopy || child.Name=="smoke"){node.RemoveChild(child);child.Free();}
            else Prepare(child,color);
        if(node is not GpuParticles2D particles)return;
        particles.OneShot=true;particles.Emitting=true;particles.LocalCoords=true;
        particles.TreeEntered+=()=>particles.Restart();
        // Native shared material resources must never be recolored in place.
        if(particles.ProcessMaterial is ParticleProcessMaterial original)
        {
            var material=(ParticleProcessMaterial)original.Duplicate(true);
            material.Color=color;
            if(material.ColorRamp is GradientTexture1D ramp && ramp.Gradient is {} gradient)
            {
                var colors=gradient.Colors;
                for(int i=0;i<colors.Length;i++){float light=Math.Max(colors[i].R,Math.Max(colors[i].G,colors[i].B));colors[i]=new(light,light,light,colors[i].A);}
                gradient.Colors=colors;
            }
            particles.ProcessMaterial=material;
        }
        if(particles.Name=="rock")
        {
            particles.Texture=ResourceLoader.Load<Texture2D>("res://images/vfx/orbs/frost_orb_particle.png");
            // The original rock texture is large. Keep replacement shards small.
            if(particles.ProcessMaterial is ParticleProcessMaterial material)
            {material.ScaleMin=.08f;material.ScaleMax=.16f;}
            particles.Lifetime=1.1;
        }
    }
}
