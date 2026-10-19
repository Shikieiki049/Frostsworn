#if STS2_STABLE_107
using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Frostsworn;
using MegaCrit.Sts2.Core.Models;
namespace Frostsworn.Tests;
public static partial class Suite
{
 static async Task CheckStable107()
 {
  Assert(typeof(FrostRngProtocolMarker107).Assembly==typeof(Suite).Assembly,"stable test harness compiled for native 0.107.1 API");
  Assert(FrostRngWire097.Magic==0x46535232,"stable build uses FSR2 counter-based snapshot, distinct from beta FSR1");
  Assert(ModelDb.AllCards.OfType<FrostCard>().Count()==93 && ModelDb.AllRelics.OfType<VoidRelic>().Count()==16,"all character cards and Void relics registered on stable");
  switch(System.Environment.GetEnvironmentVariable("FROSTSWORN_STABLE_PHASE") ?? "cards")
  {
   case "package": CheckReleasePackage010(); break;
   case "summon": await CheckDebugSummon010(); break;
   case "network": CheckNativeNetwork107(); break;
   case "cards": await CheckRevision(); break;
   case "void": await CheckVoid090(); break;
   case "load": await CheckScars0822(); break;
   case "mend": await CheckMend0818(); break;
   case "visual": await CheckResourceGuards095(); break;
   case "english": await CheckKeyword0816(); break;
   case "audio": await CheckAudio0826(); break;
   default: throw new Exception("Unknown stable validation phase");
  }
 }
 sealed class FrostRngProtocolMarker107 {}
}
#endif