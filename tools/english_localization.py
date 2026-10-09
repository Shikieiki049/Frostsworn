"""English source text. Native terminology checked against installed game 0.111.0.

Custom mechanics: Frost, Freeze, Shatter, Cold Storage, Thaw, Ice Armor,
Self-Frost, Snowfall, Ice Crystal. Keep these identical across all tables.
"""
import re
from collections import Counter

# Entries use the same identifiers as cards_data.py; formatting variables are
# preserved verbatim. Native card keywords are appended by CardModel itself.
CARD_ROWS = r'''
FROST_STRIKE|Strike|Deal {Damage:diff()} damage.
FROST_DEFEND|Defend|Gain {Block:diff()} Block.
CRYSTAL_RITE|Deep Burial|Gain {Block:diff()} Block. Put up to 1 card from your Draw Pile into Cold Storage.
ICE_CRYSTAL|Ice Crystal|Deal {Damage:diff()} damage. Apply {Amount:diff()} Frost.
FROST_SHATTER|Frost Fracture|If the enemy has at least 3 Frost, Shatter 3, gain {Amount:diff()} Strength, and deal {Damage:diff()} damage.
FREEZE_RAY|Freezing Ray|Deal {Damage:diff()} damage. Apply {Amount:diff()} Frost.
RIME_COAT|Rime Coat|Gain {Block:diff()} Block. Gain {Amount:diff()} Ice Armor.
CRYSTALLIZE|Prismatic Cleave|Deal {Damage:diff()} damage twice. If this card was Thawed, apply {Amount:diff()} Vulnerable and gain 1 Self-Frost.
CRYSTAL_CLUSTER|Crystal Cluster|Add {Amount:diff()} Ice Crystals to your Hand.
SNOW_CACHE|Snow Cache|Put 1 card into Cold Storage. Gain Snowfall equal to {Amount:diff()} times that card's base cost.
MELT_SEAL|Melt the Seal|Gain {Block:diff()} Block. Thaw 1 card.
WAKE_ICE|Icebreaker|Thaw 2 random cards. Gain 2 Self-Frost.
OVERDRAW_WARMTH|Thermal Cycle|Gain {Amount:energyIcons()}. Gain 3 Self-Frost.
RAPID_CALCULATION|Rapid Calculation|Draw {Amount:diff()} cards. Gain 1 Self-Frost.
REWARM|Rewarm|Gain {Block:diff()} Block. Lose {Amount:diff()} Self-Frost.
EXPAND_COLD_STORE|Expanded Cold Storage|Increase Cold Storage capacity by {Amount:diff()}.
SLOW_RELEASE|Quick Release|At the start of each turn, Thaw {Amount:diff()} additional cards.
SHIELD_COUNTER|Ice Shield Riposte|Deal {CalculatedDamage:diff()} damage, including bonus damage equal to your current Ice Armor.
ICE_HARVEST|Ice Harvest|Deal {Damage:diff()} damage. Add {Amount:diff()} Ice Crystals to your Hand.
SNOW_ROLL|Snowdrift|Deal {Damage:diff()} damage to ALL enemies. Gain {Amount:diff()} Snowfall.
AVALANCHE|Avalanche|Deal {Damage:diff()} damage. Apply {Amount:diff()} Frost. Gain 5 Self-Frost.
SNOW_SWEEP|Snow Sweep|Deal {Damage:diff()} damage to ALL enemies. Shatter 3. Deal additional damage equal to {Amount:diff()} times the number of successful Shatters.
TWIN_BLADES|Iceblade Flurry|Deal {Damage:diff()} damage twice. Shatter 1. Deal damage {Amount:diff()} additional times.
COLD_HAMMER|Cold Storage Maul|Deal {CalculatedDamage:diff()} damage, including {Amount:diff()} bonus damage for each point of total base cost of cards in Cold Storage.
MELT_SLASH|Thawing Slash|Deal {Damage:diff()} damage. Thaw up to {Amount:diff()} cards.
WHITE_MIST|Frostmist Blade|Deal {Damage:diff()} damage. Apply 4 Frost and {Amount:diff()} Weak.
ICE_BIND|Ice Bind|Apply {Amount:diff()} Frost.\nYou may put 1 card into Cold Storage.
ABSOLUTE_ZERO|Absolute Zero|Apply {Amount:diff()} Frost to ALL enemies.
SHELL_RECYCLE|Ice Shell|Consume all Ice Armor. Gain Block equal to twice the Ice Armor consumed.
SEAL_SPELL|Sealing Spell|Draw {Amount:diff()} cards. Put 1 card into Cold Storage.
BLIZZARD_COMING|Gathering Blizzard|Gain {Amount:diff()} Snowfall. Prevent the next {Extra:diff()} instances of Self-Frost you would gain.
SNOWLINE|Advancing Snowline|Gain 2 Self-Frost. Deal {Damage:diff()} damage to ALL enemies. Gain Snowfall equal to half the total damage dealt.
THAW_FROST|Melting Rime|Gain {Amount:diff()} Ice Armor. Shatter {Extra:diff()}. Gain additional Ice Armor equal to twice the Frost removed.
COLD_RETURN|Returning Cold Front|Gain {Amount:diff()} Snowfall. Gain Ice Armor equal to {Extra:diff()} times your current Snowfall.
STEP_SNOW|Snowbound Pursuit|Deal {Damage:diff()} damage. Draw 1 card for each card in Cold Storage, up to {Amount:diff()}.
THIN_ICE|Thin Ice Thrust|Deal {Damage:diff()} damage. If the target already has Frost, gain Ice Armor equal to the damage dealt.
SPRING_FLOOD|Spring Flood|Play ALL cards in your Cold Storage.
DEEP_CACHE|Hidden Edge|Deal {Damage:diff()} damage. Put up to 1 card into Cold Storage. If you did, draw {Amount:diff()} cards.
GLACIER_BODY|Glacier Form|Gain {Amount:diff()} Ice Armor. Gain 1 Ice Armor each turn. Your Ice Armor no longer melts naturally.
STORM_CORE|Storm Core|Gain {Amount:diff()} Snowfall each turn.
SNOW_EYE|Eye of the Snow|Whenever you play an Ice Crystal, gain {Amount:diff()} Snowfall.
ICE_MIRROR|Ice Mirror Sanctuary|Whenever you Thaw a card, gain {Amount:diff()} Ice Armor.
GLACIAL_CORE|Glacial Core|Ice Crystals apply {Amount:diff()} additional Frost.
PRISM_BLAST|Prismatic Smelting|Deal {Damage:diff()} damage. Exhaust up to {Amount:diff()} cards from your Hand. Gain {Extra:diff()} Snowfall for each card Exhausted.
ICE_CHISEL|Ice Chisel|Deal {Damage:diff()} damage. If the enemy has at least 2 Frost, apply {Amount:diff()} Vulnerable.
FROSTFIRE|Frostfire Recoil|Deal {Damage:diff()} damage. Gain 1 Self-Frost.
CRYSTAL_VOLLEY|Crystal Volley|Deal {Damage:diff()} damage. Fill your Hand with Ice Crystals.
ICE_DUST|Ice Dust|Gain {Amount:diff()} Ice Armor and {Extra:diff()} Snowfall. Exhaust 1 card.
PERMAFROST_WARD|Permafrost Ward|Gain {Amount:diff()} Ice Armor. You may put 1 card into Cold Storage.
WARMTH_RECOVERY|Residual Warmth|Exhaust up to {Amount:diff()} cards from your Hand. Add that many Ice Crystals to your Hand.
THAW_ENERGY|Cold Power|Thaw up to {Amount:diff()} cards. Gain {energyPrefix:energyIcons(1)} for each card Thawed. Gain 1 Self-Frost.
CRYSTAL_FURNACE|Crystal Recycling|Exhaust up to 2 Ice Crystals from your Hand. Gain {energyPrefix:energyIcons(1)} and 1 Self-Frost for each.
CRYSTAL_EDGE|Crystal Edge|Ice Crystals deal {Amount:diff()} additional damage and ignore Block.
CRYSTAL_AMULET|Crystal Amulet|Whenever you Exhaust an Ice Crystal, gain {Amount:diff()} Ice Armor.
ICE_CELLAR|Ice Cellar|Whenever you Thaw a card, draw {Amount:diff()} cards.
DELAYED_CHILL|Delayed Chill|Deal {Damage:diff()} damage. Apply {Amount:diff()} Frost, or {Extra:diff()} if this card was Thawed.
PIERCING_COLD|Bonechilling Gleam|Deal {Damage:diff()} damage. Put up to {Amount:diff()} cards into Cold Storage. Deal damage 1 additional time for each card stored.
SNOW_FINALE|Snowbound Finale|Deal {Damage:diff()} damage. Heal 1 HP for each HP you have lost to Self-Frost this combat.
SPREAD_FROST|Creeping Frost|Apply 5 Frost to ALL enemies. Gain {Amount:diff()} Snowfall for every 5 Frost on enemies.
FROST_CARVING|Frost Etching|Put 1 card into Cold Storage. The next time you Thaw it, add {Amount:diff()} Ice Crystals to your Hand. Thaw 1 additional card next turn.
FROZEN_MOMENT|Frozen Moment|You may put up to {Amount:diff()} cards from your Hand, {Amount:diff()} from your Discard Pile, and {Amount:diff()} from your Draw Pile into Cold Storage.
PHASE_SHIFT|Phase Change|Shatter 10. Gain Ice Armor equal to {Amount:diff()} times the Frost removed.
COLD_SEARCH|Flash Freeze Search|Gain {Block:diff()} Block. Put up to {Amount:diff()} Attacks from your Draw Pile into Cold Storage.
BLOOD_WINTER|Winter Sealed in Blood|Gain 3 Self-Frost. Gain {energyPrefix:energyIcons(1)}. Draw {Amount:diff()} cards. Put up to {Extra:diff()} cards from your Hand into Cold Storage.
DEBT_SETTLEMENT|Frost Debt|Remove all Self-Frost. Draw 1 card for each Self-Frost removed.
QUIET_MEDITATION|Silent Snow Meditation|Next turn, gain {Amount:energyIcons()} and {Extra:diff()} Ice Armor.
CRYSTAL_RESONANCE|Crystal Resonance|Whenever you create Ice Crystals, create {Amount:diff()} additional copies.
WINTER_ARCHIVE|Lingering Winter|At the end of your turn, put up to 1 card from your Hand into Cold Storage.
ENDLESS_STORM|Endless Nightstorm|At the end of your turn, Snowfall decreases by only {Amount:diff()}.
SIXFOLD_SNOW|Snowpack|The first time you Thaw a card each turn, gain {Amount:diff()} Ice Armor.
HIDDEN_BLADE|Secret of the Hidden Blade|Thawed Attacks deal {Amount:diff()} additional damage the next time they are played.
COLD_BLOOD_ECHO|Coldblood Echo|Whenever you lose HP to Self-Frost, gain Ice Armor equal to {Amount:diff()} times the HP lost.
HEAT_EXCHANGE|Heat Exchange|The first time you Thaw a card each turn, gain {Amount:energyIcons()}.
SNOW_BLOOM|Frost in Full Bloom|Put up to 3 cards from your Exhaust Pile into Cold Storage.
DISSOLVE|Dissolve|Choose any number of cards in Cold Storage to Thaw.
SILVER_FROST|Silver Frost|Gain {Amount:diff()} Snowfall.
FROZEN_WISH|Frozen Wish|Put {Amount:diff()} cards from your Draw Pile into Cold Storage. Thaw {Amount:diff()} additional cards next turn.
GLITTER|Glitter|Add 2 Sharp Ice Crystals to your Hand.
BLESSING_WIND|Blessing Wind|Dexterity now also affects Ice Armor you gain. Gain 1 Dexterity.
HEART_INSCRIPTION|Inscribed on the Heart|Strength now also affects Snowfall you gain. Gain 1 Strength.
FLOWING_POWER|Power Overflowing|Whenever you create an Ice Crystal, Upgrade it.
FROST_BITE|Frostbite|Apply 2 Frost. Deal {CalculatedDamage:diff()} damage, including {Extra:diff()} additional damage for each time you have applied Frost this combat.
ICE_BREAK|Icebreak|Deal {Damage:diff()} damage to ALL enemies. Apply {Amount:diff()} Frost. Consume all Frost on enemies. This card deals {Extra:diff()} additional damage for each Frost consumed.
SNOW_UNSHEATHED|Snow, Unsheathed|Deal {Damage:diff()} damage. Apply {Amount:diff()} Frost for each stack of debuff you have applied to enemies this turn.
HYPOTHERMIA_STRIKE|Hypothermic Strike|Deal {Damage:diff()} damage. Apply Frost equal to the damage dealt.
WHITE_NIGHT_RUSH|White Night Rush|Deal {Damage:diff()} damage to ALL enemies. Put this card into Cold Storage. Each time this card enters Cold Storage this combat, reduce its cost by {energyPrefix:energyIcons(1)}.
PIERCE|Pierce|Deal {Damage:diff()} damage to ALL enemies {Amount:diff()} times. Add a Depleted to both your Draw Pile and your Discard Pile.
DEPLETED|Depleted|After this card enters your Hand, you cannot draw any more cards this turn.
SNOW_CHARGE|Snowbound Charge|Deal {Damage:diff()} damage. Double your Snowfall.
FATED_STORY|Fated Story|Thawed cards gain Replay 1 the next time they are played.
ABSOLUTE_BEAM|Absolute Freeze Beam|Deal {Damage:diff()} damage. Apply Frost equal to half the enemy's Freeze threshold.
GUARD_ADVANCE|Guard, Advance|Gain {Amount:diff()} Ice Armor X times. Gain {Extra:diff()} Snowfall X times.
ICE_RELEASE|Ice Release|The next X Thawed cards with a non-X cost cost {energyPrefix:energyIcons(0)} the next time they are played.
'''

POWER_ROWS = '''
GLACIER_BODY_POWER|Glacier Form|Gain {Amount} Ice Armor each turn. Your Ice Armor no longer melts naturally.
STORM_CORE_POWER|Storm Core|Gain {Amount} Snowfall each turn.
SNOW_EYE_POWER|Eye of the Snow|Whenever you play an Ice Crystal, gain {Amount} Snowfall.
ICE_MIRROR_POWER|Ice Mirror Sanctuary|Whenever you Thaw a card, gain {Amount} Ice Armor.
GLACIAL_CORE_POWER|Glacial Core|Ice Crystals apply {Amount} additional Frost.
CRYSTAL_EDGE_POWER|Crystal Edge|Ice Crystals deal {Amount} additional damage and ignore Block.
CRYSTAL_AMULET_POWER|Crystal Amulet|Whenever you Exhaust an Ice Crystal, gain {Amount} Ice Armor.
ICE_CELLAR_POWER|Ice Cellar|Whenever you Thaw a card, draw {Amount} cards.
CRYSTAL_RESONANCE_POWER|Crystal Resonance|Whenever you create Ice Crystals, create {Amount} additional copies.
WINTER_ARCHIVE_POWER|Lingering Winter|At the end of your turn, put up to {Amount} cards from your Hand into Cold Storage.
ENDLESS_STORM_POWER|Endless Nightstorm|At the end of your turn, Snowfall decreases by only a fixed amount.
SIXFOLD_SNOW_POWER|Snowpack|The first time you Thaw a card each turn, gain {Amount} Ice Armor.
HIDDEN_BLADE_POWER|Secret of the Hidden Blade|Thawed Attacks deal {Amount} additional damage the next time they are played.
COLD_BLOOD_ECHO_POWER|Coldblood Echo|Whenever you lose HP to Self-Frost, gain Ice Armor equal to {Amount} times the HP lost.
HEAT_EXCHANGE_POWER|Heat Exchange|The first time you Thaw a card each turn, gain {Amount:energyIcons()}.
DRAW_NEXT_POWER|Winter Preparations|Draw {Amount} additional cards next turn.
FROST_POWER|Frost|When the threshold is reached, Freeze the enemy, making it skip an action. Artifact can negate Frost.
ICE_ARMOR_POWER|Ice Armor|After Block, each Ice Armor prevents 1 damage and is consumed. Prevents damage, but not HP loss. Before you draw each turn, halve your remaining Ice Armor, rounded up.
SELF_FROST_POWER|Self-Frost|Before you draw next turn, lose {Amount} HP, then remove this debuff.
SNOW_POWER|Snowfall|At the end of your turn, deal {Amount} non-Attack damage to ALL enemies, then halve your Snowfall, rounded down.
COLD_EXPANSION_POWER|Expanded Cold Storage|Increase Cold Storage capacity by {Amount}, up to 10.
SLOW_RELEASE_POWER|Quick Release|At the start of your turn, Thaw {Amount} additional cards.
COLD_STORAGE_POWER|Cold Storage|Thaw 1 card at the start of your turn. Thawed cards cost 1 less the next time they are played.
ARMOR_NEXT_POWER|Ice Armor Next Turn|Gain {Amount} Ice Armor next turn.
THAW_NEXT_POWER|Thaw Next Turn|Thaw {Amount} additional cards next turn.
BLESSING_WIND_POWER|Blessing Wind|Dexterity now also affects Ice Armor you gain.
HEART_INSCRIPTION_POWER|Inscribed on the Heart|Strength now also affects Snowfall you gain.
FLOWING_POWER_POWER|Power Overflowing|Ice Crystals you create are Upgraded.
SELF_FROST_GUARD_POWER|Blizzard Shelter|Prevent the next {Amount} instances of Self-Frost you would gain.
FATED_STORY_POWER|Fated Story|Thawed cards gain Replay {Amount} the next time they are played.
ICE_RELEASE_POWER|Ice Release|The next {Amount} Thawed cards with a non-X cost are free the next time they are played.
FRACTAL_SNOW_POWER|Fractal Snowflake|Ice Crystals created this combat gain Sharp.
ECLIPSE_STRENGTH_POWER|Eclipse: Temporary Strength|Gain {Amount} Strength this turn. Removed at the end of the turn. Cannot be negated by Artifact.
'''

RELIC_ROWS = r'''
WINTER_CROWN|Core of Deep Winter|At the start of each combat, apply 5 Frost to ALL enemies.\nEach turn, apply 2 Frost to ALL enemies.
SNOW_BOOKMARK|Snowflake Bookmark|At the end of your turn, gain 1 Ice Armor for each card in Cold Storage.
POLAR_GLOBE|Polar Orrery|Every time you put 3 cards into Cold Storage, you may choose up to 1 card to Thaw.
WINTER_BOTTLE|Bottle of Lingering Winter|Before your first draw each combat, choose 1 card from your Draw Pile to put into Cold Storage.
ICE_KEY|Ice Key|The first time you Freeze an enemy each turn, deal 12 damage to that enemy.
FROZEN_SOIL|Frozen Soil|After Ice Armor melts for the first time each combat, gain 7 Ice Armor.
SNOW_PRIMER|The Super-Easy Beginner's Guide to Snow Magic!|Double the first Snowfall you gain each turn.
FROST_DIPLOMA|Advanced Cryomancer's Certificate|Whenever you deal damage to an enemy, apply 1 Frost to it.
WINTER_CORE|Winterbound Core|At the start of each combat, apply 3 Frost to ALL enemies.
'''

def rows(text):
    return [line.split('|', 2) for line in text.strip().splitlines()]

def gold(text):
    # Native sentences keep ordinary "damage" lowercase and unhighlighted.
    terms = r'Ice Crystals?\+?|Self-Frost|Cold Storage|Ice Armor|Exhaust Pile|Discard Pile|Draw Pile|Snowfall|Vulnerable|Dexterity|Strength|Artifact|Exhaust(?:ed)?|Thawed|Shatters?|Freeze|Frost|Block|Weak|Replay|Sharp|Thaw|Hand|Upgrade(?:d)?'
    return ''.join(part if part.startswith('{') else re.sub(r'(?<![A-Za-z-])(?:'+terms+r')(?![A-Za-z-])', lambda m: '[gold]'+m.group(0)+'[/gold]', part) for part in re.split(r'(\{[^{}]+\})', text))

def build_english(chinese):
    result = {name: {} for name in chinese}
    for category, prefix, source in [('cards','CARD',CARD_ROWS),('powers','POWER',POWER_ROWS),('relics','RELIC',RELIC_ROWS)]:
        table = result[category]
        for ident, title, description in rows(source):
            key = f'FROSTSWORN_{prefix}_{ident}'
            table[key+'.title'] = title
            table[key+'.description'] = gold(description.replace('\\n','\n'))
            if category == 'cards':
                table[key+'.selectionScreenPrompt'] = 'Choose a card.'
                table[key+'.upgradeDescription'] = table[key+'.description']
            elif category == 'powers':
                table[key+'.smartDescription'] = table[key+'.description']
            else:
                table[key+'.flavor'] = 'A relic left behind by winter.'
    c = result['cards']
    for ident, text in {
        'WAKE_ICE': 'Choose 2 cards to Thaw. Gain 2 Self-Frost.',
        'CRYSTAL_VOLLEY': 'Deal {Damage:diff()} damage. Fill your Hand with Ice Crystals+.',
        'GLITTER': 'Add 2 Sharp Ice Crystals+ to your Hand.',
        'ABSOLUTE_BEAM': "Deal {Damage:diff()} damage. Apply Frost equal to the enemy's entire Freeze threshold.",
        'ICE_RELEASE': 'The next X+1 Thawed cards with a non-X cost cost {energyPrefix:energyIcons(0)} the next time they are played.',
    }.items(): c[f'FROSTSWORN_CARD_{ident}.upgradeDescription'] = gold(text)
    for ident, line in {
        'SHELL_RECYCLE': 'Currently grants {LiveAmount:diff()} Block.',
        'SNOW_FINALE': 'Currently heals {LiveAmount:diff()} HP.',
        'SNOW_UNSHEATHED': 'Currently applies {LiveAmount:diff()} Frost.',
        'COLD_RETURN': 'Currently grants {LiveAmount:diff()} Ice Armor.',
        'SNOW_CHARGE': 'Snowfall after doubling: {LiveAmount:diff()}.',
        'PHASE_SHIFT': 'This target currently provides {LiveAmount:diff()} Ice Armor.',
    }.items():
        for suffix in ('.description','.upgradeDescription'):
            key = 'FROSTSWORN_CARD_'+ident+suffix
            c[key+'.combat'] = c[key]+'\n'+gold(line)
    for suffix in ('.description','.upgradeDescription'):
        c['FROSTSWORN_CARD_ABSOLUTE_BEAM'+suffix+'.combat'] = gold('Deal {Damage:diff()} damage. Apply {BeamFrost:diff()} Frost.')
    for ident in ('SNOW_EYE_POWER','CRYSTAL_AMULET_POWER'):
        key = 'FROSTSWORN_POWER_'+ident+'.smartDescription'
        result['powers'][key] = result['powers'][key].replace('{Amount}','{EffectiveAmount}')
    result['powers']['FROSTSWORN_POWER_ENDLESS_STORM_POWER.smartDescription'] = gold('At the end of your turn, Snowfall decreases by only {Decay}.')
    result['relics']['FROSTSWORN_RELIC_WINTER_CORE.flavor'] = "It holds a city's last remaining warmth."
    char = 'FROSTSWORN_CHARACTER_FROSTSWORN_CHARACTER'
    for suffix, text in {
        'title':'Frostsworn','titleObject':'Frostsworn',
        'description':'An exiled mage who sealed a city in ice.\nDelay enemy actions with Frost and save opportunities for your next turn in Cold Storage.',
        'pronounObject':'him','possessiveAdjective':'his','pronounPossessive':'his','pronounSubject':'he',
        'cardsModifierTitle':'Frostsworn Cards','cardsModifierDescription':'Use the Frostsworn card pool for this run.',
        'unlockText':'The frost oath is sworn.','bestiaryQuote':'Let this winter linger a little longer.',
        'bestiaryKillQuote':'Even ice will melt.','eventDeathPrevention':'It is not yet time to thaw.',
        'banter.alive.endTurnPing':'Give me a little more time.','banter.dead.endTurnPing':"The rest is up to you.",
    }.items(): result['characters'][char+'.'+suffix] = text
    for suffix, text in {'title':'Frost Oath','description':'Seal away the past. Enter the Spire.', 'unlockInfo':"The Frostsworn's journey.",'unlockText':'Frostsworn','storyTitle':'Frost Oath'}.items():
        result['epochs'][char+'_EPOCH.'+suffix] = text
    result['epochs']['frostsworn_character_frostsworn_character.title'] = 'Frost Oath'
    for suffix, text in {'title':'Sharp','description':'This card applies 1 Vulnerable.','extraCardText':'Apply 1 Vulnerable.'}.items():
        result['enchantments']['FROSTSWORN_ENCHANTMENT_SHARP_ENCHANTMENT.'+suffix] = gold(text) if suffix!='title' else text
    for ident, name, text in [('FROST_BOTTLE','Chill Potion','Apply {Amount} Frost.'),('LIQUID_NITROGEN','Liquid Nitrogen','Put up to 3 cards from your Hand into Cold Storage.'),('FRACTAL_SNOWFLAKE','Fractal Snowflake','Ice Crystals created this combat gain Sharp.')]:
        key = 'FROSTSWORN_POTION_'+ident
        result['potions'].update({key+'.title':name,key+'.description':gold(text),key+'.selectionScreenPrompt':'Choose a card to put into Cold Storage.'})
    result['static_hover_tips'] = STATIC_EN.copy()
    for ancient, lines in DIALOGUES.items():
        for index, (speaker, line) in enumerate(zip(('ancient','char','ancient'), lines)):
            key = f'{ancient}.talk.{char}.0-{index}r'
            result['ancients'][key+'.'+speaker] = line
            if index < 2: result['ancients'][key+'.next'] = 'Continue'
    for suffix in ('startattack','endattack'):
        result['ancients'][f'THE_ARCHITECT.talk.{char}.0-'+suffix] = 'None'
    validate(chinese, result)
    return result

STATIC_EN = {
    'FROSTSWORN_COLD_STATUS.description':'Currently stored: {Count}/{Capacity} cards\nCurrent capacity: {Capacity} cards (base 4, maximum 10).\nAfter your normal draw each turn, choose {Thaw} cards to Thaw.\nThawed cards cost 1 less only the next time they are played; reductions do not stack. Cards stay in Cold Storage if your Hand is full.',
    'FROSTSWORN_ARMOR_STATUS.description':'Current Ice Armor: {Amount}\n{Description}',
    'FROSTSWORN_ECLIPSE.title':'Eclipse {Level}',
    'FROSTSWORN_ECLIPSE_REST.description':'Heal {Heal} HP.{ExtraText}',
    'FROSTSWORN_ECLIPSE_MEND.description':'Heal {HasTarget:{Name}|someone else} for {Percent}% of their Max HP{HasTarget: ({Heal})|}.',
    'FROSTSWORN_FATED.cardDescription':'[gold][Story] Replay {FatedReplays} the next time this card is played.[/gold]',
    'FROSTSWORN_CARDPILE_COLD_STORAGE.title':'Cold Storage',
    'FROSTSWORN_CARDPILE_COLD_STORAGE.description':'Base capacity: 4, expandable to 10. After your normal draw each turn, choose 1 card to Thaw. Thawed cards cost 1 less the next time they are played.',
    'FROSTSWORN_CARDPILE_COLD_STORAGE.empty':'Cold Storage is empty. Use Snow Cache to store cards from your Hand.',
    'FROSTSWORN_STORE.prompt':'Choose other cards from your Hand to put into Cold Storage. You may skip.',
    'FROSTSWORN_STORE_REQUIRED.prompt':'Choose other cards from your Hand to put into Cold Storage.',
    'FROSTSWORN_SEARCH.prompt':'Choose cards to put into Cold Storage.',
    'FROSTSWORN_THAW.prompt':'Choose 1 card to Thaw. It costs 1 less the next time it is played.',
    'FROSTSWORN_THAW.cardDescription':'\n[blue]Thawed: costs 1 less the next time it is played.[/blue]',
    'FROSTSWORN_SHATTER.title':'Shatter',
    'FROSTSWORN_SHATTER.description':'Shatter X: Remove up to X Frost from the target. At least 1 Frost must be removed for Shatter to succeed. Benefits depend on the amount actually removed.',
    'FROSTSWORN_FROST.title':'Frost / Freeze',
    'FROSTSWORN_FROST.description':"After a card resolves, if Frost reaches 12% of the enemy's Max HP (rounded up, initially between 12 and 54), remove all Frost and make it skip its next action, keeping its original Intent. Each Freeze raises the threshold by 12% of Max HP (rounded up, between 4 and 12). The enemy must take a normal action before it can be Frozen again. Uninterruptible actions cannot be Frozen.",
    'FROSTSWORN_COLD.title':'Cold Storage / Thaw',
    'FROSTSWORN_COLD.description':'Base capacity: 4, maximum: 10. Stored cards leave your Hand. After your normal draw next turn, choose 1 card to Thaw. If your Hand is full, it stays in Cold Storage. Thawed cards cost 1 less only the next time they are played. This reduction does not stack and does not affect X-cost cards.',
    'FROSTSWORN_ARMOR.title':'Ice Armor',
    'FROSTSWORN_ARMOR.description':'After Block, each Ice Armor prevents 1 damage and is consumed. Prevents damage, but not HP loss. Before you draw each turn, halve your remaining Ice Armor, rounded up.',
    'FROSTSWORN_SELF.title':'Self-Frost',
    'FROSTSWORN_SELF.description':'Before you draw next turn, lose 1 HP for each Self-Frost, then remove all Self-Frost. Ignores Block and Ice Armor, cannot be negated by Artifact, and can kill you.',
    'FROSTSWORN_SNOW.title':'Snowfall',
    'FROSTSWORN_SNOW.description':'At the end of your turn, deal non-Attack damage to ALL enemies equal to your Snowfall, then halve it, rounded down.',
    'FROSTSWORN_ECLIPSE_UNRECORDED.title':'Eclipse: Not Recorded',
    'FROSTSWORN_ECLIPSE_QUOTE.description':'"You may celebrate only in the light... because I allow you to."',
    'FROSTSWORN_ECLIPSE_0.description':'No special effects.',
    'FROSTSWORN_ECLIPSE_1.description':'Ancients restore only 70% of your missing HP.',
    'FROSTSWORN_ECLIPSE_2.description':'The three floors before the first Boss of each Act are hidden.',
    'FROSTSWORN_ECLIPSE_3.description':'Start with 20 less Gold.',
    'FROSTSWORN_ECLIPSE_4.description':'At the start of even-numbered turns, enemies gain 3 Block.',
    'FROSTSWORN_ECLIPSE_5.description':'Healing from sources other than Ancients is reduced by one third.',
    'FROSTSWORN_ECLIPSE_6.description':'Damage taken from events is increased by 50%.',
    'FROSTSWORN_ECLIPSE_7.description':'At the start of odd-numbered turns, enemies gain 1 temporary Strength. Cannot be negated by Artifact.',
    'FROSTSWORN_ECLIPSE_8.description':'Allies suffer permanent damage (up to 50%), restored at the start of the next Act.',
    'FROSTSWORN_FROST_FROZEN.description':'Frozen: skips its next action.',
    'FROSTSWORN_FROST_RECOVERY.description':'Recovering: must take a normal action before it can be Frozen again.',
    'FROSTSWORN_FROST_UNINTERRUPTIBLE.description':'The current action cannot be interrupted.',
    'FROSTSWORN_FROST_READY.description':'Freezes after a card resolves if the threshold is reached.',
    'FROSTSWORN_FROST_STATUS.description':'Current Frost: {Frost}\nCurrent Freeze threshold: {Threshold}\n{Status}\nFrozen {Count} times this combat; each Freeze raises the threshold by 12.',
}

DIALOGUES = {
    'NEOW':["The chill you bring... cannot stop this place.","Then let me keep climbing. Some things are worth preserving.","Take my gift. Try again."],
    'OROBAS':["You hide the past in ice and think it will never change?","I just don't want to lose it again.","Then let us see what you are willing to pay."],
    'PAEL':["Your hands are cold. How long have you been walking?","Long enough that I can no longer remember what spring looks like.","Take this for now. Your road is not over."],
    'TEZCATARA':["Even ice can burn, if you are willing to pay the price.","I know. It is often the caster who burns away.","Then make your choice."],
    'NONUPEIPE':["The blizzard behind you is still chasing you.","It isn't chasing me. It follows me.","I hope you still remember how to make it stop."],
    'TANX':["Give something frozen a knock and you'll know how sturdy it is.","Some things can never be put back together once they break.","Then choose something that can stay with you a little longer."],
    'VAKUU':["You have preserved so much, yet left yourself in winter.","At least I can still choose where to go.","Then continue. Let me see what you choose."],
    'DARV':["There are cracks in your ice.","Cracks can become blades, too.","Good. Don't let them cut you first."],
    'THE_ARCHITECT':["You think freezing this place will make everything stop?","I no longer want to stop.","Then reach the end."],
}

def validate(chinese, english):
    for table, source in chinese.items():
        assert source.keys() == english[table].keys(), (table, source.keys() ^ english[table].keys())
        for key, original in source.items():
            translated = english[table][key]
            assert not re.search(r'[\u3400-\u9fff]', translated), (key, translated)
            assert Counter(re.findall(r'\{[^{}]+\}', original)) == Counter(re.findall(r'\{[^{}]+\}', translated)), ('variables',key)
            stack = []
            for closing, tag in re.findall(r'\[(/?)(gold|blue|red|green)\]', translated):
                if closing: assert stack and stack.pop() == tag, ('tags',key)
                else: stack.append(tag)
            assert not stack, ('tags',key)
