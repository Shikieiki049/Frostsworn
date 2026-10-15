using System;
using MegaCrit.Sts2.Core.Localization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using Godot;
using Frostsworn;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Monsters.Mocks;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Nodes.Screens.InspectScreens;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.addons.mega_text;
using HarmonyLib;
namespace Frostsworn.Tests;
public static partial class Suite
{
 static bool HideVoidTestVotes(ref bool __result){__result=false;return false;}
 static async Task CheckVoid090()
 {
  var initialTree=(SceneTree)Engine.GetMainLoop();await initialTree.ToSignal(initialTree,SceneTree.SignalName.ProcessFrame);
  var all=ModelDb.AllRelics.OfType<VoidRelic>().ToArray();
  GD.Print("VOID_ENUM="+VoidRelics.Rarity);
  Assert(all.Length==16,"all 16 Void relics are registered in the native model database");
  Assert(!Enum.GetValues<RelicRarity>().Contains(VoidRelics.Rarity),"Void rarity is a registered custom value, not Event rarity");
  Assert(all.All(r=>r.Rarity==VoidRelics.Rarity && r.Icon.GetSize()==new Vector2(64,64) && r.BigIcon.GetSize()==new Vector2(256,256)),"all Void icons load at native 64px and detail 256px sizes");
  Assert(all.All(r=>UnlockState.all.Relics.Contains(r)),"Void relics are unlocked and discoverable in compendium");
  Assert(all.All(r=>!r.Title.GetFormattedText().Contains(".title") && !r.DynamicDescription.GetFormattedText().Contains(".description")),"all Void relic names and descriptions resolve");
  var player=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,1);
  var other=Player.CreateForNewRun<FrostswornCharacter>(UnlockState.all,2);
  var run=RunState.CreateForTest([player,other]);player.ResetCombatState();other.ResetCombatState();
  foreach(var p in run.Players)foreach(var r in p.Relics.ToArray())p.RemoveRelicInternal(r);
  Assert(all.All(r=>!r.IsAllowed(run) && !r.IsAllowedInShops),"all Void relics reject ordinary rewards and shops");
  var bag=new RelicGrabBag();bag.Populate(all,run.Rng.UnknownMapPoint);
  Assert(bag.PullFromFront(VoidRelics.Rarity,run)==null,"even a populated reward grab bag cannot roll Void relics");
  var combat=new CombatState(runState:run);combat.AddPlayer(player);combat.AddPlayer(other);
  var monster=(MockAttackMonster)ModelDb.Monster<MockAttackMonster>().ToMutable();
  var enemy=combat.CreateCreature(monster,CombatSide.Enemy,"void-test");combat.AddCreature(enemy);
  CombatManager.Instance.SetUpCombat(combat);
  var turn=typeof(CombatManager).GetField("_turnState",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(CombatManager.Instance)!;
  turn.GetType().GetProperty("IsInProgress")!.SetValue(turn,true);
  if(monster.MoveStateMachine==null)monster.SetUpForCombat();monster.RollMove(combat.PlayerCreatures);
  var ctx=new ThrowingPlayerChoiceContext();var selector=new TestCardSelector();using var selection=CardSelectCmd.UseSelector(selector);
  MegaCrit.Sts2.Core.Context.LocalContext.NetId=null;
  T Relic<T>() where T:RelicModel {var r=(T)ModelDb.Relic<T>().ToMutable();player.AddRelicInternal(r);return r;}
  void Turn(int n){typeof(PlayerCombatState).GetField("<TurnNumber>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(player.PlayerCombatState,n);}
  async Task Clear(){selector.Cleanup();foreach(var c in player.PlayerCombatState!.AllPiles.SelectMany(p=>p.Cards).ToArray())await CardPileCmd.RemoveFromCombat(c);foreach(var r in player.Relics.ToArray())player.RemoveRelicInternal(r);foreach(var p in player.Creature.Powers.ToArray())await PowerCmd.Remove(p);await CreatureCmd.SetMaxAndCurrentHp(player.Creature,100);if(player.Creature.Block>0)await CreatureCmd.LoseBlock(ctx,player.Creature,player.Creature.Block,null);await CreatureCmd.SetMaxAndCurrentHp(enemy,1000);}
  async Task<CardModel> Card<T>(PileType p)where T:CardModel {var c=combat.CreateCard<T>(player);await CardPileCmd.Add(c,p);return c;}
  var origin=Relic<BloodVial>();var cord=Relic<BloodlessCord>();
  await cord.AfterObtained();
  Assert(VoidRelics.Suppressed(origin) && origin.Status==RelicStatus.Disabled && !combat.IterateHookListeners().Contains(origin) && !run.IterateHookListeners(null).Contains(origin),"original relic remains visible but is removed from both combat and run hook dispatch");
  Assert(!origin.IsAllowed(run),"original counterpart cannot be rewarded again after obtaining its Void version");
  player.RemoveRelicInternal(cord);await cord.AfterRemoved();Assert(!VoidRelics.Suppressed(origin) && combat.IterateHookListeners().Contains(origin),"removing Void relic restores the original's hook eligibility");
  Assert(!origin.IsAllowed(run),"original remains permanently excluded from this run rewards even after Void relic removal");
  foreach(var type in VoidRelics.Types){var v=(VoidRelic)VoidRelics.Canonical(type).ToMutable();player.AddRelicInternal(v);Assert(!VoidRelics.Canonical(v.OriginalType).IsAllowed(run),"counterpart reward veto covers overridden native IsAllowed: "+v.OriginalType.Name);player.RemoveRelicInternal(v);}
  MegaCrit.Sts2.Core.Modding.AssemblyInfo.Init();
  using(var stream=System.IO.File.OpenRead(ProjectSettings.GlobalizePath("res://../../dist/Frostsworn/mod_manifest.json")))MegaCrit.Sts2.Core.Modding.AssemblyInfo.ModMap![typeof(Entry).Assembly]=new MegaCrit.Sts2.Core.Modding.Mod {path="test/Frostsworn",state=MegaCrit.Sts2.Core.Modding.ModLoadState.Loaded,manifest=MegaCrit.Sts2.Core.Modding.ModManifest.ReadFromStream(stream,out _)};
  MegaCrit.Sts2.Core.Multiplayer.Serialization.ModelIdSerializationCache.Init();
  var savedVoid=(OverdriveGearbox)ModelDb.Relic<OverdriveGearbox>().ToMutable();savedVoid.VoidCount=6;savedVoid.VoidFloor=18;savedVoid.VoidUsed=true;
  var restoredVoid=(OverdriveGearbox)RelicModel.FromSerializable(savedVoid.ToSerializable());Assert(restoredVoid.VoidCount==6 && restoredVoid.VoidFloor==18 && restoredVoid.VoidUsed && restoredVoid.Rarity==VoidRelics.Rarity,"Void identity and saved counters survive native relic serialization");
  var registry=typeof(STS2RitsuLib.RitsuLibFramework).Assembly.GetType("STS2RitsuLib.RunData.RunSavedDataRegistry")!;
  var nativeSave=SaveNative(run);var payload=(string)registry.GetMethod("BuildPayload")!.Invoke(null,new object[]{run})!;registry.GetMethod("AttachDocumentFromJson")!.Invoke(null,new object[]{nativeSave,payload});
  var loadedRun=RunState.FromSerializable(nativeSave);Assert(VoidRelics.OriginalBlocked(typeof(BloodVial),loadedRun),"run-level original reward exclusions survive native save/load");
  await Clear();var armor=await Card<VoidInfection>(PileType.Hand);int hp=player.Creature.CurrentHp;
  Assert(armor.Type==CardType.Curse && armor.EnergyCost.GetWithModifiers(CostModifiers.None)==1 && armor.Keywords.Contains(CardKeyword.Exhaust) && armor.Keywords.Contains(CardKeyword.Eternal),"Putrefaction is a playable 1-energy Exhaust/Eternal Curse");
  await CardCmd.Exhaust(ctx,armor);Assert(player.Creature.CurrentHp==hp-3,"exhausting Putrefaction through a real native command loses exactly 3 HP");
  await Clear();var f=Relic<DrownedFloat>();
  Assert(f.ModifyHpLostAfterOstyLate(player.Creature,12,ValueProp.Move,enemy,null)==0,"Drowner's Float absorbs up to 15 damage after Block");await f.AfterModifyingHpLostAfterOsty();
  Assert(f.ModifyHpLostAfterOstyLate(player.Creature,12,ValueProp.Move,enemy,null)==12,"Drowner's Float only absorbs once per combat");
  await Clear();f=Relic<DrownedFloat>();await CreatureCmd.Damage(ctx,player.Creature,8,ValueProp.Move,enemy);await CreatureCmd.Damage(ctx,player.Creature,8,ValueProp.Move,enemy);Assert(player.Creature.CurrentHp==92 && f.VoidUsed,"native damage pipeline consumes Float on the first damaging attack, including fully absorbed HP loss");
  await Clear();var sac=Relic<RuminationSac>();for(int i=0;i<6;i++)await Card<FrostStrike>(PileType.Draw);
  await sac.AfterShuffle(ctx,player);await sac.AfterShuffle(ctx,player);Assert(player.PlayerCombatState!.Hand.Cards.Count==3,"Ruminating Sac draws only on the first shuffle");
  await Clear();var moth=Relic<LightMothCocoon>();var h=await Card<DeepCache>(PileType.Hand);Turn(1);await moth.AfterPlayerTurnStartLate(ctx,player);
  Assert(h.EnergyCost.GetWithModifiers(CostModifiers.All)==0 && h.Keywords.Contains(CardKeyword.Exhaust),"Light-Eater Cocoon reduces chosen Hand cost and grants Exhaust for this turn");
  Turn(2);Assert(!h.Keywords.Contains(CardKeyword.Exhaust),"Cocoon's granted Exhaust expires next turn");
  await Clear();var wax=Relic<TriocularWax>();for(int i=0;i<4;i++)await Card<DeepCache>(PileType.Hand);Turn(2);await wax.AfterPlayerTurnStartLate(ctx,player);
  Assert(player.PlayerCombatState.Hand.Cards.Count(c=>c.EnergyCost.GetWithModifiers(CostModifiers.All)==0)==3,"Three-Eyed Wax discounts three distinct random cards");
  await Clear();var star=Relic<HangingStarSac>();await Card<DeepCache>(PileType.Hand);Turn(4);await star.AfterPlayerTurnStartLate(ctx,player);
  Assert(player.PlayerCombatState.Hand.Cards.All(c=>c.EnergyCost.GetWithModifiers(CostModifiers.All)==0),"Inverted Star Sac makes Hand free on turn four");
  await Clear();var face=Relic<FaceborrowMembrane>();Turn(1);await face.AfterPlayerTurnStartLate(ctx,player);
  Assert(player.Creature.GetPowerAmount<StrengthPower>()==3 && player.Creature.GetPowerAmount<DexterityPower>()==3,"Borrowed-Face Membrane grants 3 Strength and Dexterity");
  await player.Creature.GetPower<VoidFadingStrengthPower>()!.AfterSideTurnEnd(ctx,CombatSide.Player,[player.Creature]);
  await player.Creature.GetPower<VoidFadingDexterityPower>()!.AfterSideTurnEnd(ctx,CombatSide.Player,[player.Creature]);
  Assert(player.Creature.GetPowerAmount<StrengthPower>()==2 && player.Creature.GetPowerAmount<DexterityPower>()==2,"temporary stat bonuses decay by one each own turn");
  await Clear();var teeth=Relic<MeristemTeeth>();var a=await Card<FrostStrike>(PileType.Draw);var b=await Card<FrostDefend>(PileType.Draw);selector.PrepareToSelect(new[]{a,b});selector.PrepareToSelect(new[]{a});await teeth.BeforeHandDraw(player,ctx,combat);
  Assert(player.PlayerCombatState!.DrawPile.Cards.Concat(player.PlayerCombatState.DiscardPile.Cards).All(c=>c.IsUpgraded) && player.PlayerCombatState.DiscardPile.Cards.Count==1,"Budding Incisors upgrades two combat copies and discards one choice");
  await Clear();var gearbox=Relic<OverdriveGearbox>();var strike=await Card<FrostStrike>(PileType.Hand);Turn(14);
  for(int i=0;i<7;i++)await gearbox.AfterCardPlayed(ctx,Play(strike));
  Assert(player.Creature.GetPowerAmount<VigorPower>()==20 && gearbox.VoidCount==0,"Gearbox triggers every seven cards and caps Vigor at 20");
  var cookie=Relic<MoldyCookie>();await cookie.AfterCombatVictory(null!);Assert(cookie.DisplayAmount==1 && cookie.ModifyCardRewardUpgradeOdds(player,strike,0)==0,"Cookie does not upgrade first combat rewards");await cookie.AfterCombatVictory(null!);Assert(cookie.DisplayAmount==0 && cookie.ModifyCardRewardUpgradeOdds(player,strike,0)==1,"Cookie upgrades every second combat reward set");
  await Clear();var jelly=Relic<LighthouseJelly>();await CreatureCmd.SetMaxAndCurrentHp(player.Creature,100);
  Assert(!jelly.ShouldDieLate(player.Creature),"Lighthouse Jellyfish prevents lethal HP loss above 10 Max HP");await CreatureCmd.SetCurrentHp(player.Creature,0);
  GD.Print($"VOID_JELLY hp={player.Creature.CurrentHp}, max={player.Creature.MaxHp}, block={player.Creature.Block}");
  Assert(player.Creature.CurrentHp==15 && player.Creature.MaxHp==89 && player.Creature.Block==16,"Jellyfish heals 15 percent, gains 16 Block and spends 11 Max HP");
  await CreatureCmd.SetMaxAndCurrentHp(player.Creature,10);Assert(jelly.ShouldDieLate(player.Creature),"Jellyfish cannot revive at 10 Max HP");
  await Clear();var pulse=Relic<BloodlessCord>();await CreatureCmd.SetCurrentHp(player.Creature,90);pulse.VoidFloor=run.TotalFloor-1;
  await pulse.AfterRoomEntered(null!);await pulse.AfterRoomEntered(null!);Assert(player.Creature.CurrentHp==91,"Bloodless Cord heals once per climbed floor, not once per room re-entry");
  await Clear();var mirror=Relic<MirrorParasite>();await CreatureCmd.GainBlock(player.Creature,30,ValueProp.Unpowered,null);int enemyHp=enemy.CurrentHp;
  await CreatureCmd.Damage(ctx,player.Creature,8,ValueProp.Move,enemy);await CreatureCmd.Damage(ctx,player.Creature,8,ValueProp.Move,enemy);
  Assert(enemy.CurrentHp==enemyHp-8,"Mirror-Shell retaliates only once per turn for actual Block spent");
  await Clear();var memory=Relic<MemorySlough>();var m1=await Card<FrostStrike>(PileType.Draw);var m2=await Card<FrostDefend>(PileType.Draw);var m3=await Card<DeepCache>(PileType.Draw);selector.PrepareToSelect(new[]{m2});
  await CreatureCmd.Damage(ctx,player.Creature,1,ValueProp.Unpowered|ValueProp.Unblockable,enemy);
  Assert(player.PlayerCombatState!.Hand.Cards.Contains(m2) && player.PlayerCombatState.ExhaustPile.Cards.Contains(m1) && player.PlayerCombatState.ExhaustPile.Cards.Contains(m3),"Memory Slough keeps the chosen top-three card and Exhausts the other two");
  await Clear();var polyp=Relic<CurledPolyp>();await CreatureCmd.SetCurrentHp(player.Creature,80);await polyp.AfterSideTurnEnd(ctx,CombatSide.Enemy,[enemy]);
  Assert(player.Creature.CurrentHp==84,"Curled Polyp heals four at the end of an undamaged enemy turn");await CreatureCmd.Damage(ctx,player.Creature,1,ValueProp.Move,enemy);int damaged=player.Creature.CurrentHp;await polyp.AfterSideTurnEnd(ctx,CombatSide.Enemy,[enemy]);
  Assert(polyp.VoidDisabled && player.Creature.CurrentHp==damaged,"Curled Polyp permanently stops healing for that combat after enemy HP damage");
  await Clear();var marrow=Relic<MarrowTouch>();await CreatureCmd.SetCurrentHp(player.Creature,50);await marrow.AfterCombatVictory(null!);Assert(player.Creature.MaxHp==105,"Marrow Touch grants five Max HP at exactly half health");
  await Clear();marrow=Relic<MarrowTouch>();await CreatureCmd.SetCurrentHp(player.Creature,49);await marrow.AfterCombatVictory(null!);Assert(player.Creature.MaxHp==100,"Marrow Touch does not trigger below half health");
  await Clear();var pendant=Relic<GymnospermPendant>();var potion=ModelDb.AllPotions.First().ToMutable();potion.Owner=player;await pendant.AfterPotionUsed(potion,null);
  Assert(player.Creature.GetPowerAmount<DexterityPower>()==2 && player.Creature.GetPowerAmount<VoidFadingDexterityPower>()==2,"Gymnosperm Pendant grants two decaying Dexterity after a Potion");
  await Clear();var ev=(VoidCradle)ModelDb.Event<VoidCradle>().ToMutable();await ev.BeginEvent(player,null,false);
  Assert(ev.Title.GetFormattedText()=="虚空摇篮" && ev.CurrentOptions[1].Description.GetFormattedText().Contains("朽败"),"event title and both Curse-priced option descriptions resolve");
  Assert(ev.CurrentOptions.Count==3 && ev.CurrentOptions[1].IsLocked,"Cradle always has three options and locks corruption without an eligible relic");
  var blood=Relic<BloodVial>();var ev2=(VoidCradle)ModelDb.Event<VoidCradle>().ToMutable();await ev2.BeginEvent(player,null,false);await ev2.Corrupt();
  Assert(player.Relics.OfType<BloodlessCord>().Any() && !player.Relics.OfType<BloodVial>().Any() && player.Deck.Cards.OfType<VoidInfection>().Count()==1,"random corruption transforms only an eligible owned relic and adds the Curse");
  var ev3=(VoidCradle)ModelDb.Event<VoidCradle>().ToMutable();await ev3.BeginEvent(player,null,false);await ev3.Take();Assert(player.Relics.OfType<VoidRelic>().Count()==2 && player.Deck.Cards.OfType<VoidInfection>().Count()==2,"taking a new Void relic also adds the Curse");
  Assert(ev.CreateInitialPortrait().GetWidth()>1000,"event uses supplied Void Cradle artwork");
  for(int i=0;i<4;i++){await cookie.AfterCombatVictory(null!);Assert(cookie.DisplayAmount==(i+1)%2,"Cookie counter stays in the 0/1 cycle across repeated combats");}
  Assert(ModelDb.Relic<FaceborrowMembrane>().DynamicDescription.GetRawText().Contains("[gold]力量[/gold]") && ModelDb.Relic<FaceborrowMembrane>().HoverTipsExcludingRelic.Count()==2,"Void relics highlight and expose native Strength/Dexterity tips");
  Assert(!ModelDb.Card<VoidInfection>().Description.GetRawText().Contains("永恒") && ModelDb.Card<VoidInfection>().Description.GetRawText().Contains("[gold]消耗[/gold]"),"Curse highlights Exhaust without manually duplicating automatic keywords");
  Assert(all.All(r=>r.DynamicDescription.GetRawText().Contains("\n使")),"counterpart suppression is always on a new line");
  Assert(ModelDb.Relic<LighthouseJelly>().DynamicDescription.GetRawText().Contains("[green]15%[/green]") && ModelDb.Relic<LighthouseJelly>().DynamicDescription.GetRawText().Contains("[blue]16[/blue]") && ModelDb.Relic<LighthouseJelly>().DynamicDescription.GetRawText().Contains("[green]11[/green]"),"healing and Max HP values are green while Block remains blue");
  Assert(ModelDb.Relic<IceKey>().DynamicDescription.GetRawText().Contains("[blue]12[/blue]"),"original character relic damage values use native blue");
  var intro=new LocString("events","FROSTSWORN_EVENT_VOID_CRADLE.pages.INITIAL.description").GetFormattedText();
  Assert(intro.Contains("[purple][jitter]虚空摇篮[/jitter][/purple]") && intro.Contains("[red][jitter]虫子[/jitter][/red]"),"event narrative uses requested native purple/red jitter effects");
  Assert(ev.CurrentOptions[0].Title.GetFormattedText()=="触碰" && ev.CurrentOptions[2].Title.GetFormattedText()=="无视并离开","new touch and leave option titles resolve");
  var curseText=ModelDb.Card<VoidInfection>().GetDescriptionForPile(PileType.None);
  Assert(System.Text.RegularExpressions.Regex.Matches(curseText,"永恒").Count==1 && System.Text.RegularExpressions.Regex.Matches(curseText,"消耗").Count==2,"native final Curse description includes one Exhaust rule and one Eternal rule");
  var tree=(SceneTree)Engine.GetMainLoop();
  typeof(Node).Assembly.GetType("Godot.Bridge.ScriptManagerBridge")!.GetMethod("LookupScriptsInAssembly",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)!.Invoke(null,new object[]{typeof(NRelicCollection).Assembly});
  TestMode.IsOn=false;
  tree.Root.Size=new Vector2I(1920,1080);
  var category=ResourceLoader.Load<PackedScene>(NRelicCollectionCategory.scenePath).Instantiate<NRelicCollectionCategory>();tree.Root.AddChild(category);
  category.Position=new Vector2(40,30);category.Size=new Vector2(1800,950);
  var collection=new NRelicCollection();
  AccessTools.Method(typeof(VoidCollectionPatch),"Postfix").Invoke(null,new object[]{category,RelicRarity.Event,collection,all.Cast<RelicModel>().ToHashSet(),UnlockState.all,UnlockState.all.Relics.ToHashSet()});
  var section=category.GetNode<NRelicCollectionCategory>("FrostswornVoidRelics");
  Assert(section.GetGridItems().Sum(row=>row.Count)==16 && section.GetNode<MegaRichTextLabel>("%Header").Text.Contains("[font_size=28][b]虚空：") && section.GetNode<MegaRichTextLabel>("%Header").Text.Contains("只能通过特殊途径获得的腐化遗物。"),"actual native compendium category displays all 16 Void entries with purple header");
  var inspect=ResourceLoader.Load<PackedScene>("res://scenes/screens/inspect_relic_screen/inspect_relic_screen.tscn").Instantiate<NInspectRelicScreen>();tree.Root.AddChild(inspect);
  AccessTools.Field(typeof(NInspectRelicScreen),"_relics").SetValue(inspect,all);
  ((HashSet<RelicModel>)AccessTools.Field(typeof(NInspectRelicScreen),"_allUnlockedRelics").GetValue(inspect)!).UnionWith(all);
  foreach(var relic in all){SaveManager.Instance.MarkRelicAsSeen(relic);AccessTools.Method(typeof(NInspectRelicScreen),"SetRelic").Invoke(inspect,new object[]{Array.IndexOf(all,relic)});var label=(MegaLabel)AccessTools.Field(typeof(NInspectRelicScreen),"_rarityLabel").GetValue(inspect)!;Assert(label.Text.Contains("虚空") && label.GetThemeColor("font_color")==VoidRelics.Color,"native inspection opens custom rarity: "+relic.GetType().Name);}
  AccessTools.Method(typeof(NInspectRelicScreen),"SetRarityVisuals").Invoke(inspect,new object[]{RelicRarity.Event});
  Assert(!((MegaLabel)AccessTools.Field(typeof(NInspectRelicScreen),"_rarityLabel").GetValue(inspect)!).HasThemeColorOverride("font_color"),"returning to native rarities removes the custom purple override");
  var lastName=(MegaLabel)AccessTools.Field(typeof(NInspectRelicScreen),"_nameLabel").GetValue(inspect)!;
  Assert(!lastName.HasThemeColorOverride("font_color"),"relic names retain native default color");
  AccessTools.Method(typeof(NInspectRelicScreen),"SetRelic").Invoke(inspect,new object[]{Array.FindIndex(all,r=>r is FaceborrowMembrane)});
  inspect.Visible=false;
  if(DisplayServer.GetName()!="headless"){
   await tree.ToSignal(tree.CreateTimer(.25),SceneTreeTimer.SignalName.Timeout);await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
   tree.Root.GetTexture().GetImage().SavePng("C:/fz/sts2mod/outputs/void-collection-093.png");
  }
  inspect.Visible=true;inspect.GetNode<Control>("%Popup").Modulate=Colors.White;
  if(DisplayServer.GetName()!="headless"){
   await tree.ToSignal(tree.CreateTimer(.25),SceneTreeTimer.SignalName.Timeout);await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
   tree.Root.GetTexture().GetImage().SavePng("C:/fz/sts2mod/outputs/void-compendium-093.png");
  }
  if(DisplayServer.GetName()!="headless"){
   inspect.Visible=false;category.Visible=false;
   // Move native visual nodes into a neutral canvas: the test host has no room synchronizer.
   var nativeLayout=ResourceLoader.Load<PackedScene>("res://scenes/events/default_event_layout.tscn").Instantiate<Control>();
   var canvas=new Control {Size=new Vector2(1920,1080)};tree.Root.AddChild(canvas);
   foreach(var child in nativeLayout.GetChildren().ToArray()){nativeLayout.RemoveChild(child);canvas.AddChild(child);}
   var portrait=canvas.GetNode<TextureRect>("Portrait");portrait.Texture=ev.CreateInitialPortrait();
   Assert(portrait.Size.X==2560 && portrait.StretchMode==TextureRect.StretchModeEnum.KeepAspectCentered,"event keeps the full native 2560px backdrop without custom panel scaling");
   var title=canvas.GetNode<MegaLabel>("VBoxContainer/Title");title.SetTextAutoSize(ev.Title.GetFormattedText());title.Modulate=Colors.White;
   var desc=canvas.GetNode<MegaRichTextLabel>("VBoxContainer/EventDescription");desc.SetTextAutoSize(new LocString("events","FROSTSWORN_EVENT_VOID_CRADLE.pages.INITIAL.description").GetFormattedText());desc.Modulate=Colors.White;
   canvas.GetNode<Control>("VBoxContainer/SharedEventLabel").Visible=false;
   var options=canvas.GetNode<VBoxContainer>("VBoxContainer/OptionsContainer");
   var votesMethod=AccessTools.Method(typeof(MegaCrit.Sts2.Core.Nodes.Events.NEventOptionButton),"ShouldDisplayPlayerVote");
   var uiTestHarmony=new Harmony("Frostsworn.tests.event-render");
   uiTestHarmony.Patch(votesMethod,prefix:new HarmonyMethod(typeof(Suite),nameof(HideVoidTestVotes)));
   for(int i=0;i<ev.CurrentOptions.Count;i++){
    var button=MegaCrit.Sts2.Core.Nodes.Events.NEventOptionButton.Create(ev,ev.CurrentOptions[i],i);options.AddChild(button);button.Modulate=Colors.White;
   }

   await tree.ToSignal(tree.CreateTimer(.25),SceneTreeTimer.SignalName.Timeout);await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
   Assert(options.GetGlobalRect().End.Y<=1080,"all three native event option buttons fit on screen with the new narrative");
   tree.Root.GetTexture().GetImage().SavePng("C:/fz/sts2mod/outputs/void-event-093.png");
   options.Visible=false;
   foreach(var page in new[]{"TAKE","CORRUPT","LEAVE"}){
    desc.SetTextAutoSize(new LocString("events",$"FROSTSWORN_EVENT_VOID_CRADLE.pages.{page}.description").GetFormattedText());
    await tree.ToSignal(tree.CreateTimer(.15),SceneTreeTimer.SignalName.Timeout);await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
    tree.Root.GetTexture().GetImage().SavePng($"C:/fz/sts2mod/outputs/void-event-{page.ToLowerInvariant()}-093.png");
   }
   canvas.Free();nativeLayout.Free();uiTestHarmony.UnpatchAll("Frostsworn.tests.event-render");
  }
  if(DisplayServer.GetName()!="headless"){
   category.Visible=true;inspect.Visible=true;
   AccessTools.Method(typeof(NInspectRelicScreen),"SetRelic").Invoke(inspect,new object[]{Array.FindIndex(all,r=>r is LighthouseJelly)});
   await tree.ToSignal(tree.CreateTimer(.2),SceneTreeTimer.SignalName.Timeout);await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
   tree.Root.GetTexture().GetImage().SavePng("C:/fz/sts2mod/outputs/void-relic-health-093.png");
  }
  var locTables=(Dictionary<string,MegaCrit.Sts2.Core.Localization.LocTable>)typeof(MegaCrit.Sts2.Core.Localization.LocManager).GetField("_tables",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(MegaCrit.Sts2.Core.Localization.LocManager.Instance)!;
  foreach(var table in new[]{"cards","relics","powers","events","static_hover_tips"}){
   var text=Godot.FileAccess.GetFileAsString($"res://Frostsworn/localization/eng/{table}.json");locTables[table]=new MegaCrit.Sts2.Core.Localization.LocTable(table,System.Text.Json.JsonSerializer.Deserialize<Dictionary<string,string>>(text)!,locTables.GetValueOrDefault(table));
  }
  Assert(all.All(r=>!System.Text.RegularExpressions.Regex.IsMatch(r.Title.GetFormattedText()+r.DynamicDescription.GetFormattedText(),"[\\u4e00-\\u9fff]")),"all 16 Void relics have complete English names and rules");
  Assert(!ModelDb.AllCards.Any(c=>c is not VoidInfection && c.Title=="Putrefaction"),"new English Curse title does not duplicate another registered card");
  Assert(ModelDb.Card<VoidInfection>().Title=="Putrefaction" && ev.Title.GetFormattedText()=="Void Cradle","new Curse and event have English localization");
  if(DisplayServer.GetName()!="headless"){
   inspect.Visible=false;category.Visible=false;
   // Move native visual nodes into a neutral canvas: the test host has no room synchronizer.
   var nativeLayout=ResourceLoader.Load<PackedScene>("res://scenes/events/default_event_layout.tscn").Instantiate<Control>();
   var canvas=new Control {Size=new Vector2(1920,1080)};tree.Root.AddChild(canvas);
   foreach(var child in nativeLayout.GetChildren().ToArray()){nativeLayout.RemoveChild(child);canvas.AddChild(child);}
   var portrait=canvas.GetNode<TextureRect>("Portrait");portrait.Texture=ev.CreateInitialPortrait();
   Assert(portrait.Size.X==2560 && portrait.StretchMode==TextureRect.StretchModeEnum.KeepAspectCentered,"event keeps the full native 2560px backdrop without custom panel scaling");
   var title=canvas.GetNode<MegaLabel>("VBoxContainer/Title");title.SetTextAutoSize(ev.Title.GetFormattedText());title.Modulate=Colors.White;
   var desc=canvas.GetNode<MegaRichTextLabel>("VBoxContainer/EventDescription");desc.SetTextAutoSize(new LocString("events","FROSTSWORN_EVENT_VOID_CRADLE.pages.INITIAL.description").GetFormattedText());desc.Modulate=Colors.White;
   canvas.GetNode<Control>("VBoxContainer/SharedEventLabel").Visible=false;
   var options=canvas.GetNode<VBoxContainer>("VBoxContainer/OptionsContainer");
   var votesMethod=AccessTools.Method(typeof(MegaCrit.Sts2.Core.Nodes.Events.NEventOptionButton),"ShouldDisplayPlayerVote");
   var uiTestHarmony=new Harmony("Frostsworn.tests.event-render");
   uiTestHarmony.Patch(votesMethod,prefix:new HarmonyMethod(typeof(Suite),nameof(HideVoidTestVotes)));
   for(int i=0;i<ev.CurrentOptions.Count;i++){
    var button=MegaCrit.Sts2.Core.Nodes.Events.NEventOptionButton.Create(ev,ev.CurrentOptions[i],i);options.AddChild(button);button.Modulate=Colors.White;
   }

   await tree.ToSignal(tree.CreateTimer(.25),SceneTreeTimer.SignalName.Timeout);await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
   Assert(options.GetGlobalRect().End.Y<=1080,"all three native event option buttons fit on screen with the new narrative");
   tree.Root.GetTexture().GetImage().SavePng("C:/fz/sts2mod/outputs/void-event-eng-093.png");
   options.Visible=false;
   foreach(var page in new[]{"TAKE","CORRUPT","LEAVE"}){
    desc.SetTextAutoSize(new LocString("events",$"FROSTSWORN_EVENT_VOID_CRADLE.pages.{page}.description").GetFormattedText());
    await tree.ToSignal(tree.CreateTimer(.15),SceneTreeTimer.SignalName.Timeout);await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
    tree.Root.GetTexture().GetImage().SavePng($"C:/fz/sts2mod/outputs/void-event-{page.ToLowerInvariant()}-eng-093.png");
   }
   canvas.Free();nativeLayout.Free();uiTestHarmony.UnpatchAll("Frostsworn.tests.event-render");
  }
  inspect.Free();category.Free();collection.Free();TestMode.IsOn=true;
 }
}













