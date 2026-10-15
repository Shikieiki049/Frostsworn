using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.HoverTips;
namespace Frostsworn;

[RegisterCard(typeof(CurseCardPool))]
public sealed class VoidInfection:ModCardTemplate
{
    public VoidInfection():base(1,CardType.Curse,CardRarity.Curse,TargetType.Self){}
    public override int MaxUpgradeLevel=>0;
    public override string CustomPortraitPath=>"res://Frostsworn/void/VoidInfection.png";
    public override IEnumerable<CardKeyword> CanonicalKeywords=>new[]{CardKeyword.Exhaust,CardKeyword.Eternal};
    protected override Task OnPlay(PlayerChoiceContext context,CardPlay play)=>Task.CompletedTask;
    public override async Task AfterCardExhausted(PlayerChoiceContext context,CardModel card,bool causedByEthereal)
    {
        if(card==this && !Owner.Creature.IsDead)
            await CreatureCmd.Damage(context,Owner.Creature,3,ValueProp.Unpowered|ValueProp.Unblockable,Owner.Creature);
    }
}

[RegisterSharedEvent]
public sealed class VoidCradle:ModEventTemplate
{
    public override string CustomInitialPortraitPath=>"res://Frostsworn/void/VoidCradle.png";
    protected override IReadOnlyList<EventOption> GenerateInitialOptions()=>new[]{
        new EventOption(this,VoidRelics.NewCandidates(Owner!).Any()?Take:null,InitialOptionKey("TAKE"),HoverTipFactory.FromCard<VoidInfection>()),
        new EventOption(this,VoidRelics.Candidates(Owner!).Any()?Corrupt:null,InitialOptionKey("CORRUPT"),HoverTipFactory.FromCard<VoidInfection>()),
        new EventOption(this,Leave,InitialOptionKey("LEAVE"))};
    public async Task AddCurse()
    {var c=Owner!.RunState.CreateCard<VoidInfection>(Owner);CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(c,PileType.Deck));}
    public async Task Take()
    {
        var eligible=VoidRelics.NewCandidates(Owner!).ToArray();if(eligible.Length==0)return;
        var relic=Rng.NextItem(eligible)!.ToMutable();
        await AddCurse();await RelicCmd.Obtain(relic,Owner!);SetEventFinished(PageDescription("TAKE"));
    }
    public async Task Corrupt()
    {
        var eligible=VoidRelics.Candidates(Owner!).ToArray();if(eligible.Length==0)return;
        var original=Rng.NextItem(eligible)!;int i=Array.IndexOf(VoidRelics.Originals,original.GetType());
        await AddCurse();await RelicCmd.Replace(original,VoidRelics.Canonical(VoidRelics.Types[i]).ToMutable());SetEventFinished(PageDescription("CORRUPT"));
    }
    Task Leave(){SetEventFinished(PageDescription("LEAVE"));return Task.CompletedTask;}
}
