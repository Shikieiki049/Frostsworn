using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.CardSelection;
using STS2RitsuLib.Models.Capabilities;

namespace Frostsworn;

public sealed class ThawState
{
    public bool Pending;
    public int CarvedCrystals;
    public CardPlay? ActivePlay;
    public int AttackBonus;
}

public static class ColdStorage
{
    // Keyed by physical card: clones/transforms cannot inherit the temporary discount.
    private static readonly ConditionalWeakTable<CardModel, ThawState> States = new();
    public static ThawState State(CardModel card) => States.GetOrCreateValue(card);
    public static CardPile Pile(Player p) => CardPile.Get(Entry.ColdPileType, p)!;
    public static int Capacity(Player p) => Rules.Capacity(p.Creature.GetPowerAmount<ColdExpansionPower>());

    public static async Task<List<CardModel>> Store(PlayerChoiceContext context, Player p, AbstractModel source, int max, bool required = false)
    {
        var stored=new List<CardModel>();
        var pile = Pile(p);
        max = Math.Min(max, Capacity(p) - pile.Cards.Count);
        max = Math.Min(max, p.PlayerCombatState!.Hand.Cards.Count(c => c != source));
        if (max <= 0 || p.Creature.IsDead) return stored;
        var prefs = new CardSelectorPrefs(new LocString("static_hover_tips", required ? "FROSTSWORN_STORE_REQUIRED.prompt" : "FROSTSWORN_STORE.prompt"), required ? max : 0, max);
        var selected = await CardSelectCmd.FromHand(context, p, prefs, c => c != source, source);
        foreach (var card in selected.ToArray())
        {
            if (pile.Cards.Count >= Capacity(p)) break;
            if (card.Pile == p.PlayerCombatState!.Hand)
            {
                await CardPileCmd.Add(card, Entry.ColdPileType);
                if (card.Pile == pile) { stored.Add(card); await ExpansionEvents.Stored(context, p); }
            }
        }
        return stored;
    }

    public static async Task<int> Thaw(PlayerChoiceContext context, Player p, int count, bool random = false)
    {
        int done = 0;
        for (int i = 0; i < count && !p.Creature.IsDead; i++)
        {
            var combat = p.PlayerCombatState!;
            if (combat.Hand.Cards.Count >= CardPile.MaxCardsInHand) break;
            var eligible = Pile(p).Cards.ToArray();
            if (eligible.Length == 0) break;
            CardModel? card;
            if (random) card = p.RunState.Rng.CombatCardSelection.NextItem(eligible);
            else
            {
                var prefs = new CardSelectorPrefs(new LocString("static_hover_tips", "FROSTSWORN_THAW.prompt"), 1);
                card = (await CardSelectCmd.FromSimpleGrid(context, eligible, p, prefs)).FirstOrDefault();
            }
            if (card == null || card.Pile != Pile(p)) break;
            if(await ThawCard(context,p,card)) done++;
        }
        return done;
    }
    public static async Task<bool> ThawCard(PlayerChoiceContext context,Player p,CardModel card)
    {
        var combat=p.PlayerCombatState!;
        if(p.Creature.IsDead || card.Pile!=Pile(p) || combat.Hand.Cards.Count>=CardPile.MaxCardsInHand)return false;
        await CardPileCmd.Add(card,PileType.Hand);
        if(card.Pile!=combat.Hand)return false;
        var state=State(card);state.Pending=true;
        ModelCapabilities.Get(card).GetOrCreate<ThawDiscount>().Refresh();
        await FinalEvents.Thawed(context,p,card);
        await ExpansionEvents.Thawed(context,p);
        return true;
    }
    public static async Task<int> SelectThaw(PlayerChoiceContext context,Player p,int maximum)
    {
        var eligible=Pile(p).Cards.ToArray();
        int max=Math.Min(maximum,Math.Min(eligible.Length,CardPile.MaxCardsInHand-p.PlayerCombatState!.Hand.Cards.Count));
        if(max<=0)return 0;
        var selected=(await CardSelectCmd.FromSimpleGrid(context,eligible,p,new CardSelectorPrefs(new LocString("static_hover_tips","FROSTSWORN_THAW.prompt"),0,max))).ToArray();
        int done=0;
        foreach(var card in selected) if(await ThawCard(context,p,card))done++;
        return done;
    }
    public static async Task<int> StoreFromPile(PlayerChoiceContext context,Player p,CardPile from,int maximum,bool attacksOnly=false,bool required=false)
    {
        int count=Math.Min(maximum,Math.Min(Capacity(p)-Pile(p).Cards.Count,from.Cards.Count(c=>!attacksOnly || c.Type==CardType.Attack)));
        if(count<=0)return 0;
        var selected=(await CardSelectCmd.FromCombatPile(context,from,p,new CardSelectorPrefs(new LocString("static_hover_tips","FROSTSWORN_SEARCH.prompt"),required?count:0,count),c=>!attacksOnly || c.Type==CardType.Attack)).ToArray();
        int done=0;
        foreach(var card in selected)
        {
            if(p.Creature.IsDead || Pile(p).Cards.Count>=Capacity(p))break;
            if(card.Pile!=from)continue;
            await CardPileCmd.Add(card,Entry.ColdPileType);
            if(card.Pile==Pile(p)){done++;await ExpansionEvents.Stored(context,p);}
        }
        return done;
    }
    public static async Task PlayStored(PlayerChoiceContext context,Player p)
    {
        // Snapshot: newly stored cards are not recursively included in this play.
        foreach(var card in Pile(p).Cards.ToArray())
        {
            if(p.Creature.IsDead || CombatManager.Instance.IsOverOrEnding)break;
            if(card.Pile==Pile(p))await CardCmd.AutoPlay(context,card,null);
        }
    }

}

[RegisterModelCapability]
public sealed class ThawDiscount : CardCapability, ICardEnergyCostContributor, ICardDescriptionContributor
{
    public void Refresh() => MarkDirty();
    public int ModifyEnergyCost(CardModel card, int currentCost, CostModifiers modifiers)
        => Rules.ThawedCost(currentCost, ColdStorage.State(card).Pending, card.EnergyCost.CostsX);

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card == Owner)
        {
            var state=ColdStorage.State(cardPlay.Card);
            if(cardPlay.PlayIndex>0)
            {
                if(state.ActivePlay!=null)state.ActivePlay=cardPlay;
                return;
            }
            bool thawed=state.Pending;
            if(thawed && !cardPlay.Card.EnergyCost.CostsX && cardPlay.Player.Creature.GetPower<IceReleasePower>() is {} release)await PowerCmd.Decrement(release);
            state.Pending = false;
            state.ActivePlay=null;
            state.AttackBonus=0;
            MarkDirty();
            if(thawed) await FinalEvents.PlayedThawed(cardPlay);
        }
    }
    public IEnumerable<CardDescriptionFragment> GetDescriptionFragments(CardDescriptionContext context)
    {
        if(!ColdStorage.State(context.Card).Pending)yield break;
        yield return new(new LocString("static_hover_tips", "FROSTSWORN_THAW.cardDescription"));
        int replays=FatedStoryPower.PendingReplays(context.Card);
        if(replays>0)
        {
            var marker=new LocString("static_hover_tips", "FROSTSWORN_FATED.cardDescription");
            marker.Add("FatedReplays",replays);
            yield return new(marker,CardDescriptionFragmentPlacement.BeforeBase);
        }
    }
}

