# Localization

The game selects `eng` or `zhs` from its language setting. English source is in
`tools/english_localization.py`; Chinese source remains in the existing card,
power, expansion, and dialogue modules. Run `tools/make_assets.py` to regenerate
the nine localization tables. Do not edit generated JSON alone.

Native terminology was checked against the installed game's English tables
(0.111.0): Block, Strength, Dexterity, Vulnerable, Weak, Artifact, Exhaust,
Retain, Innate, Ethereal, Unplayable, Replay, Sharp, HP, Max HP, Hand, Draw Pile,
Discard Pile, Exhaust Pile, Upgrade, Intent, and Ancients. Original game assets
are not distributed with this source.

| Custom mechanic | English |
|---|---|
| 寒霜 | Frost |
| 冰封 | Freeze / Frozen |
| 碎冰 | Shatter |
| 冷藏 | Cold Storage |
| 解冻 | Thaw / Thawed |
| 冰甲 | Ice Armor |
| 自霜 | Self-Frost |
| 雪势 | Snowfall |
| 冰晶 | Ice Crystal |
| 日食 | Eclipse |

Formatting variables and color tags must remain intact. Generation and source
validation enforce key parity, placeholder parity, balanced color tags, and no
Chinese fallback in English tables. `FrostText` localizes live UI and discovers
keyword hover tips in both languages. Engine test `0815` formats all 93 cards
and their upgrades using native CardModel, and checks difficulty and dialogue.
