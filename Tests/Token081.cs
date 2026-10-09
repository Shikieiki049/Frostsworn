using System.Linq;
using Godot;
using Frostsworn;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Entities.Cards;
namespace Frostsworn.Tests;
public static partial class Suite
{
    private static void CheckToken081()
    {
        var pool=ModelDb.CardPool<FrostCardPool>();
        foreach(var card in new FrostCard[]{ModelDb.Card<IceCrystal>(),ModelDb.Card<Depleted>()})
        {
            Assert(card.Pool is TokenCardPool,card.GetType().Name+" belongs to native token pool");
            Assert(!pool.AllCardIds.Contains(card.Id) && ModelDb.CardPool<TokenCardPool>().AllCardIds.Contains(card.Id),card.GetType().Name+" removed from character pool and registered in token pool");
            Assert(card.VisualCardPool==pool && card.Portrait!=null,card.GetType().Name+" keeps character visuals and portrait");
            Assert(card.ToMutable().Id==card.Id,card.GetType().Name+" remains available for generation by existing model ID");
        }
        Assert(ModelDb.Card<IceCrystal>().Rarity==CardRarity.Token && ModelDb.Card<Depleted>().Type==CardType.Status,"token rarity and status card behavior preserved");
        Assert(pool.AllCardIds.Count()==91,"character pool now contains 91 cards");
        GD.Print("TOKEN081_COMPLETE");
    }
}
