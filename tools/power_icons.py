"""Original vector placeholders. Different silhouettes, motifs and colors remain readable at small sizes."""
from pathlib import Path
SHAPES = {
 'shield':'<path d="M64 25 L95 39 V67 Q91 87 64 104 Q37 87 33 67 V39Z"/><path d="M64 37V88M46 57H82"/>',
 'snow':'<path d="M64 26V102M31 45L97 83M31 83L97 45M53 32L64 44L75 32M53 96L64 84L75 96M32 58L46 54L43 39M85 89L82 74L97 70"/>',
 'drop':'<path d="M64 25 Q50 47 38 65 Q24 100 64 103 Q104 100 90 65Z"/><path d="M65 53L52 75H72L60 94"/>',
 'storm':'<path d="M29 47Q64 20 98 45Q109 68 71 70H38M28 82H87Q107 91 87 104M31 64H53"/><path d="M71 41L60 57H79L67 78"/>',
 'box':'<rect x="29" y="35" width="70" height="64" rx="7"/><path d="M29 54H99M64 35V99M38 70H54M74 70H90"/>',
 'sun':'<circle cx="64" cy="65" r="23"/><path d="M64 25V34M64 96V105M24 65H34M94 65H104M36 36L43 43M86 86L94 94M94 36L86 44M43 87L36 94"/>',
 'blade':'<path d="M82 26L99 32L74 78L54 76Z"/><path d="M40 73L78 94M52 81L39 103"/>',
 'eye':'<path d="M23 65Q64 20 105 65Q64 107 23 65Z"/><circle cx="64" cy="65" r="17"/>',
 'mirror':'<path d="M42 29H86L101 63L84 98H42L27 63Z"/><path d="M71 37L44 80M83 52L60 82"/>',
 'crystal':'<path d="M64 23L92 57L79 99H49L36 57Z"/><path d="M64 23V102M36 57H92M49 99L64 57L79 99"/>',
 'book':'<path d="M26 34Q45 24 64 39Q83 24 102 34V95Q82 85 64 101Q45 85 26 95Z"/><path d="M64 39V101M35 48L53 51M35 64L53 67M75 51L93 48"/>',
 'cycle':'<path d="M32 58A34 34 0 0 1 92 41L97 29M92 41L76 43M96 72A34 34 0 0 1 36 90L29 100M36 90L53 86"/>',
 'burst':'<path d="M64 24L73 49L99 35L83 62L106 73L79 78L82 105L61 86L37 103L45 78L23 65L47 58L38 33L59 48Z"/>',
 'hourglass':'<path d="M36 27H92M36 103H92M42 28Q40 47 64 65Q88 82 86 102M86 28Q88 47 64 65Q40 82 42 102M51 86H77"/>',
 'crown':'<path d="M29 45L48 62L64 30L80 62L100 45L91 94H38Z"/><path d="M40 81H88"/>',
}
MOTIFS = {
 'FatedStoryPower':('book','#FFE39B'), 'IceReleasePower':('cycle','#A5F8EF'), 'FractalSnowPower':('snow','#C89EFF'),
 'FlowingPowerPower':('crystal','#FFE878'), 'SelfFrostGuardPower':('shield','#98FFBC'),
 'FrostPower':('snow','#8ADFFC'), 'IceArmorPower':('shield','#5BD7EC'),
 'GlacialCorePower':('mirror','#DFEFFF'),
 'SelfFrostPower':('drop','#FF7796'), 'SnowPower':('storm','#B9A4FF'),
 'ColdExpansionPower':('box','#F4BE62'), 'SlowReleasePower':('hourglass','#87E6AC'),
 'ColdStoragePower':('box','#94CBE7'), 'GlacierBodyPower':('shield','#BCC9F1'),
 'StormCorePower':('storm','#D388FF'), 'SnowEyePower':('eye','#8CB6FF'),
 'RimeErosionPower':('burst','#D2EA7A'), 'IceMirrorPower':('mirror','#71EDDE'),
 'CrystalEdgePower':('blade','#FFD76D'), 'CrystalAmuletPower':('crystal','#69E6A7'),
 'IceCellarPower':('book','#ADBDFF'), 'WinterEdictPower':('crown','#E0C275'),
 'WinterStayPower':('hourglass','#9DE0C9'), 'ShardBurstPower':('burst','#F8B06A'),
 'CrystalResonancePower':('crystal','#FA9CD7'), 'WinterArchivePower':('book','#DC9AE9'),
 'EndlessStormPower':('storm','#78A8F9'), 'SixfoldSnowPower':('snow','#D9B3FF'),
 'PolarCyclePower':('cycle','#93F1C8'), 'HiddenBladePower':('blade','#CBADFF'),
 'ColdBloodEchoPower':('drop','#EFBA79'), 'HeatExchangePower':('sun','#FF9970'),
 'DrawNextPower':('book','#C9E5A0'), 'MeditationPower':('sun','#9DD6F3'),
}
MOTIFS.update({'ArmorNextPower':('shield','#86B9FF'),'ThawNextPower':('hourglass','#F0AE68'),'BlessingWindPower':('mirror','#82E7AE'),'HeartInscriptionPower':('sun','#FFA0B4')})

def write_icons(directory: Path, powers):
    directory.mkdir(parents=True, exist_ok=True)
    for index, name in enumerate(powers):
        shape, color = MOTIFS.get(name, ('snow','#8ADFFC'))
        # Dots distinguish same-motif powers without relying on color alone.
        dots=''.join(f'<circle cx="{49+i*15}" cy="114" r="3" fill="{color}"/>' for i in range(index%3+1))
        svg=f'<svg xmlns="http://www.w3.org/2000/svg" width="128" height="128" viewBox="0 0 128 128"><rect x="5" y="5" width="118" height="118" rx="25" fill="#112232" stroke="{color}" stroke-width="4"/><g fill="none" stroke="{color}" stroke-width="7" stroke-linecap="round" stroke-linejoin="round">{SHAPES[shape]}</g>{dots}</svg>'
        (directory/(name+'.svg')).write_text(svg,encoding='utf-8')
