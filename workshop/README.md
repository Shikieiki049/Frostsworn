# Steam Workshop

Published item: https://steamcommunity.com/sharedfiles/filedetails/?id=3816906546

First release: 0.8.31, published publicly on 2026-10-10. Steam API returned
result 1, visibility 0, content size 375657645 bytes. RitsuLib dependency:
3747602295.

`initial-workshop.json` records the first uploaded metadata. Do not use it for
routine updates: the author may change the title and description on Steam.
`workshop.json` deliberately omits title and description, preserving
those fields during future uploads, and explicitly keeps this release public.
`mod_id.txt` identifies the existing item;
always use it to avoid creating another listing.

`description.bbcode.txt` records the bilingual description revision requested
by the author. It uses Steam headings and lists. Only add it as the JSON
`description` field when an explicit description update is requested; ordinary
mod updates must leave description omitted. Keep `dependencies` and `tags` in
every upload configuration: the uploader treats omitted dependency lists as
empty and removes existing dependencies.

### Language-specific descriptions and cover

Steam stores localized descriptions separately. The first description revision
updated the English entry while the Simplified Chinese page retained its old
text; this was a language mismatch, not a browser cache issue.
Both English and Simplified Chinese entries now have the bilingual description.
For another explicit description change, update both `english` and `schinese`
using `SteamUGC.SetItemUpdateLanguage` before setting the description.
`uploader-language.patch` adds a `language` field to the official uploader's
config and fails if Steam rejects the requested language. Apply it to a clone
of https://github.com/megacrit/sts2-mod-uploader and build the uploader locally.
The local build is `../work/WorkshopUploaderLocalized`; executables and Steam
libraries are not committed here.

`cover.png` is the author's requested upper-body close-up of the character
artwork, formatted as a 512x512 PNG (592430 bytes), replacing the full-body
thumbnail. It was produced through an image edit of `art075/selection.png`, then
resized to meet the Workshop preview limit. Copy it to the uploader workspace
as `image.png`. The wide preview image is unchanged. The Chinese Workshop page
was visually verified with the new cover and description on 2026-10-10.

The local uploader workspace is `../work/WorkshopUploader/Frostsworn`. For an
update, copy the release's `Frostsworn.dll`, `Frostsworn.pck` and
`mod_manifest.json` into its `content` folder and verify their hashes against
`dist/Frostsworn`. Do not include PDBs, game libraries, logs or account data.
Copy this directory's `workshop.json` and `mod_id.txt` into the uploader
workspace, set the update's `changeNote`, then run the official uploader:

```powershell
./ModUploader.exe upload -w Frostsworn
```

Confirm the uploader reports success for 3816906546 and the RitsuLib dependency.
Preview artwork stays in the local uploader workspace; it is under Steam's
1 MiB limit. Official instructions:
https://github.com/megacrit/sts2-mod-uploader

## Release asset audit

The shipped 0.8.31 package passed focused game-engine checks for:

- 93 card portraits, including Strike, Defend, generated and Ancient cards.
- Nine relic inventory/detail textures, with 64px native trigger textures.
- Three potion textures and outlines; character selection, map and energy art.
- 32 supplied buff icons, transparency and native icon dimensions.
- Approved first idle loop, blinking, floor anchoring, shop and rest-site size.
- Two- and four-player merchant creation and character visibility.
- Four supplied treasure gestures on native 422x1200 canvases.
- Static death artwork for all death cues and restoration after revival.
- Small history portraits, deck summary icons and Eclipse badge alignment.

Focused suites: 080, 0811, 0813, 0814, 0821, 0828, 075 and 086.
Early 075 and 086 assertions were updated to the current card-art paths,
native energy scene/palette and approved Ping wording. The standalone energy
scene reports script-binding warnings for hidden stock particle nodes; resource
replacement checks passed. These are engine checks, not a claim of a live
multiplayer playthrough.

Attack animation prototypes remain excluded by the author's request.
Statuses without supplied replacement artwork retain their mod-specific icons.
Native effects and sounds are reused from the installed game.
