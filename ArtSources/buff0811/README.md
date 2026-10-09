# Buff 图标来源与对应

用户提供 bufficon.zip 中的四张图，每张4列2行。文件名中的名称从左到右、从上到下对应，共32种能力。

使用 imagegen 去除深色背景及圆角底板，透明成品保存在 Assets/Frostsworn/buffs/sheet1.png 至 sheet4.png。图标仍保留自身深色描边。

tools/buff_atlases.py 只生成 Godot AtlasTexture 区域与 mapping.json，不修改图片。游戏小图标和悬浮说明使用同一高分辨率图集；界面大小沿用原版。新增图标不改变能力效果。
