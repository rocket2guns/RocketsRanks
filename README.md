# Rocket's Ranks

A RimWorld 1.6 mod that adds a military rank system for colonists. Promote pawns through enlisted and officer rank packs, optionally with a full Ideology-style ceremony, and they will automatically wear matching rank insignia apparel. Other mods can ship their own rank packs without writing any C#.

## What you get in-game

### Rank assignment

Every colonist gets a **Promote** gizmo on the command bar. Clicking it opens the promote dialog: a portrait of the pawn on the left, a tabbed rank list on the right (one tab per rank pack), a citation text box, and a "Hold promotion ceremony" checkbox.

- **No rank** is always the first option in the list, so you can demote a pawn back to civilian.
- Rank tabs are sorted by the lowest rank level in each pack, so enlisted packs come before officer packs.
- The dialog opens on the tab that contains the pawn's current rank.
- Tooltips on rank rows show the rank description.

When you confirm a promotion without a ceremony, the rank is applied immediately, the pawn's apparel is refreshed, and a colour-coded event message appears (positive for promotion, neutral for transfer/civilian, negative for demotion).

### Promotion ceremonies (Ideology DLC)

If Ideology is loaded and you tick **Hold promotion ceremony**, the pawn is not promoted right away. Instead the mod launches a ritual:

1. You pick a venue using the standard ritual targeter (any throne room, altar, or other ritual spot works; the mod also accepts an open chunk of floor).
2. The "Begin Ritual" dialog opens with the awardee role locked to the pawn being promoted, and the preferred presenter prefilled (your ideo leader if you have one, otherwise the highest-ranked colonist whose rank exceeds the awardee's).
3. Other colonists are recruited as spectators. The presenter walks to a spot in front of the awardee, gives a short speech using the promotion speech rule pack, and the rank is bestowed.
4. A letter is sent describing the ceremony, the attendees, the room quality, and the citation. A `ROCKET_PromotedTale` is recorded so the event can show up in colonist art.

The ceremony cannot be used for demotions. If you try to drop a pawn to a lower rank with the checkbox on, it silently falls back to the instant path.

#### Ceremony quality

Ceremony quality is computed from four factors and stored on the pawn's promotion record:

- **Attendance**: up to +40% based on the fraction of free colonists who attended.
- **Venue impressiveness**: up to +40% from the room's vanilla impressiveness stat.
- **Citation**: a flat +20% if the awardee was promoted with a written citation.
- **Preview**: shows the destination rank in the ritual dialog (no quality bonus, just a label).

If you confirmed the promotion without writing a citation, the ceremony can pop a dictation dialog mid-ritual asking the presenter to dictate one (toggleable in settings).

### Saluting

When enabled, soldiers occasionally salute officers when they pass them on the map. Salutes are driven by a regular RimWorld social interaction, so they fit into the play log and produce mood thoughts:

- The initiator gets a small "honored my superior" mood buff.
- The recipient (the officer) gets a small "honored by my troops" mood buff.
- Both thoughts stack up to three times with diminishing returns.

Salutes only fire when the recipient belongs to an officer pack (see `isOfficerPack` below). Officers can also salute more senior officers if you leave **Officers salute seniors** on.

### Rank-restricted apparel

The mod ships rank insignia apparel (shoulder pieces) for every bundled rank. The trick is the `RankExtension` mod extension:

```xml
<modExtensions>
    <li Class="RocketsRanks.RankExtension">
        <requiredRank>Rank_Sergeant</requiredRank>
    </li>
</modExtensions>
```

A pawn will only auto-equip a piece of apparel with this extension if their current rank's defName matches `requiredRank` exactly. This is enforced through a Harmony patch on the outfit/apparel selection path, so it works seamlessly with vanilla outfit policies: tag the insignia for whatever outfit you want and the right pawn will pick up the right rank automatically. The Soldier and Nudist outfit tags are applied by default.

The insignia draw on their own `ROCKET_RankLayer` (drawOrder 85) and are anchored to the shoulders, with per-body-type offsets and scales baked into the abstract `ROCKET_Ranks` parent so they sit cleanly on Male, Female, Thin, Fat, and Hulk frames.

### UI surfaces

Ranks show up in three places on the colonist UI, all individually toggleable:

- **Pawn label prefix**: the rank label is prepended to the floating name above the pawn.
- **Map icon**: the rank's icon is drawn next to the floating name. You can size and offset it, and optionally hide it when the pawn is undrafted.
- **Colonist bar badge**: the rank icon overlays the portrait on the colonist bar. Sizable and offsettable. The bundled ranks scale up automatically for generals (see `mapScale` / `portraitScale` on rank defs).

There is also a **Ranks** ITab on the pawn inspector that lists the full promotion history.

### Colonist bar tweaks

The mod includes optional colonist bar filters that piggyback on the rank UI work:

- Hide colonists currently in cryptosleep caskets.
- Hide colonists on a different map than the one you are viewing.
- Hide the bar entirely while you are looking at a map (handy if you also use rank icons over the pawns themselves).

The weapon icon under the portrait can also be repositioned and scaled.

## Settings

Settings are split across six tabs in the mod options:

| Tab | What's there |
|---|---|
| **Rank Packs** | A checkbox per `RankPackDef`. Disabling a pack hides its ranks from the promote menu and removes its apparel from crafting menus. Existing items and assigned ranks in the world are not retroactively removed. |
| **Pawn Labels** | Toggles for the rank prefix and the map icon. Slider controls for map icon size and X/Y offset. "Hide when not drafted" hides the map icon for civilians. |
| **Colonist Bar** | Cryptosleep / off-map / in-map visibility filters. Rank badge size and offset. Weapon icon offset and scale. |
| **Ceremony** | Default state of the "Hold ceremony" checkbox in the promote dialog. Whether to prompt for a citation mid-ceremony if you didn't write one up front. |
| **Salutes** | Master toggle for salutes. Sub-toggle for whether junior officers should salute senior officers. |
| **Debug** | Per-body-type insignia placement sliders (offset and scale, north/south and east/west). Most players can leave this off; it is intended for content creators tuning a new rank pack. |

## Bundled rank packs

| Pack defName | Contents |
|---|---|
| `CoreArmy` | US Army enlisted: Private through Sergeant Major of the Army (11 ranks, OR-2 to OR-9c). |
| `CoreOfficers` | US Army commissioned officers: Second Lieutenant through General (10 ranks). Marked as `isOfficerPack`. |

Each rank ships with matching shoulder insignia apparel. Officers have a silver cost; generals have a gold cost. Both apply small bonuses to suppression power and mental break threshold (with Ideology, the suppression stat also adjusts).

---

## For modders: building a rank pack

A rank pack is just a `RankPackDef` plus a set of `RankDef`s that point at it, plus (optionally) `ThingDef` apparel with the `RankExtension` mod extension. No C# required. You can ship a pack from any mod that loads after Rocket's Ranks; just declare a dependency in your About.xml so the player loads them in the right order.

### Step 1: declare the pack

```xml
<RocketsRanks.RankPackDef>
    <defName>MyMod_RoyalNavy</defName>
    <label>Royal Navy</label>
    <description>Officers of His Majesty's Royal Navy, from midshipman to admiral of the fleet.</description>
    <previewRank>MyMod_Rank_Captain</previewRank>
    <isOfficerPack>true</isOfficerPack>
</RocketsRanks.RankPackDef>
```

#### RankPackDef fields

| Field | Default | Meaning |
|---|---|---|
| `defName` | required | Unique identifier. Referenced from every `RankDef` in the pack and stored in the player's hidden-packs save. |
| `label` | required | Display name shown on the promote dialog tab and in the Rank Packs settings list. |
| `description` | optional | Shown as a tooltip in the Rank Packs settings list. |
| `previewRank` | `null` | A `RankDef` from this pack whose icon represents the pack in the settings list. If null, falls back to the icon of the middle-level rank in the pack. The picked rank must belong to this pack, or you'll get a ConfigErrors warning at startup. |
| `isOfficerPack` | `false` | If true, members of this pack receive salutes from enlisted ranks (and from junior officers, if Officers salute seniors is enabled). |

### Step 2: declare the ranks

```xml
<RocketsRanks.RankDef>
    <defName>MyMod_Rank_Midshipman</defName>
    <label>midshipman</label>
    <description>A junior officer under training. They wear the patch but have not yet earned the trust.</description>
    <rankLevel>1</rankLevel>
    <iconPath>UI/RoyalNavy/Midshipman</iconPath>
    <pack>MyMod_RoyalNavy</pack>
</RocketsRanks.RankDef>
```

#### RankDef fields

| Field | Default | Meaning |
|---|---|---|
| `defName` | required | Unique identifier. Referenced by the `requiredRank` field on rank insignia apparel. |
| `label` | required | The rank name. Title-cased automatically when displayed. |
| `description` | optional | Shown as a tooltip in the promote dialog and as the rank's apparel description hook. |
| `rankLevel` | `0` | Integer used for sorting the promote list and for comparing seniority (salutes, ceremony eligibility, demotion checks). Higher is more senior. Promotion is freeform: there is no enforced linear ladder, so it's fine to leave gaps between numbers. |
| `iconPath` | `null` | Path to the rank icon, relative to `Textures/`. Used on the promote dialog row, the map label, the colonist bar badge, and as the salute symbol. Square textures work best. |
| `pack` | required | The `RankPackDef` this rank belongs to. Validated at startup; a rank with no pack will fail config errors. |
| `mapScale` | `1.0` | Multiplier on the map icon size for this specific rank. The bundled generals use 1.1, 1.4, and 1.5 to make stars more visible at a glance. |
| `portraitScale` | `1.0` | Same idea, but for the colonist bar badge. |

### Step 3 (optional): rank-restricted apparel

The bundled abstract parents (`ROCKET_Ranks`, `ROCKET_Ranks_Enlisted`, `ROCKET_Ranks_Officer`, `ROCKET_Ranks_General`) already set up the apparel layer, body part group, render extension, and stat offsets. Inherit from one of them and add the `RankExtension`:

```xml
<ThingDef ParentName="ROCKET_Ranks_Officer">
    <defName>MyMod_Apparel_Midshipman</defName>
    <label>midshipman rank</label>
    <description>A small woven patch worn at the shoulder.</description>
    <graphicData>
        <texPath>Things/Apparel/RoyalNavy/Midshipman/Rank</texPath>
    </graphicData>
    <apparel>
        <wornGraphicPath>Things/Apparel/RoyalNavy/Midshipman/Rank</wornGraphicPath>
    </apparel>
    <modExtensions Inherit="True">
        <li Class="RocketsRanks.RankExtension">
            <requiredRank>MyMod_Rank_Midshipman</requiredRank>
        </li>
    </modExtensions>
    <uiOrder>40</uiOrder>
</ThingDef>
```

#### RankExtension fields

| Field | Meaning |
|---|---|
| `requiredRank` | The `RankDef` whose holder is allowed to equip this apparel. Match is exact on defName; promoting past this rank removes the apparel automatically when `RankApparelRefresh` runs. |

If you want a completely custom look (different layer, different body part group, different stat offsets), inherit straight from `ROCKET_Ranks` and override the apparel block.

### Per-body-type insignia placement

The bundled `RankRenderExtension` on the abstract `ROCKET_Ranks` parent positions the insignia for each body type. If you need to tune your own, copy the block from `1.6/Defs/Apparel_USArmy_Def.xml` and tweak the offsets and scales until things sit right. The Debug settings tab has live sliders for the bundled values so you can find the numbers visually before baking them into XML.

### What the player can override

- The Rank Packs settings tab lets the player tick your pack off. Hidden packs disappear from the promote menu and the crafting menus, but already-assigned ranks and already-spawned apparel are untouched.
- Salutes and ceremony toggles apply globally, including to your pack.
- UI scale and offset sliders apply globally; only `mapScale` / `portraitScale` on the rank def let you nudge a specific rank larger or smaller relative to those globals.

---

## Project layout

```
About/                      Mod metadata; depends on Harmony.
1.6/
  Assemblies/               Shipped DLL.
  Defs/
    Apparel_USArmy_Def.xml      Enlisted insignia apparel + abstract parents.
    Apparel_USOfficer_Def.xml   Officer / general insignia apparel.
    Category_Def.xml            Apparel category tree (Ranks > Enlisted/Officers).
    Interactions_Promote.xml    Promotion speech rule pack + ROCKET_PromotedTale.
    Interactions_Salute.xml     Salute interaction, rule pack, and mood thoughts.
    RankPack_Def.xml            CoreArmy and CoreOfficers pack defs.
    Rank_USArmy_Def.xml         Bundled enlisted RankDefs.
    Rank_USOfficer_Def.xml      Bundled officer/general RankDefs.
    Ritual_Def.xml              Promotion ritual pattern, behavior, and outcome.
    Thought_Def.xml             Awardee/witness ceremony thoughts.
Assemblies/                  Loose-folder DLL (mirrors 1.6/Assemblies for older loaders).
Source/                      C# source (csproj + .cs files).
Languages/English/           Translatable strings.
Textures/                    Rank icons, apparel textures, and UI assets.
```

## Building from source

1. Open `Source/RocketsRanks.csproj` in Rider or Visual Studio.
2. The csproj points at RimWorld via a property at the top; edit it if your install isn't at the default Steam location.
3. Build. Output goes to `Assemblies/RocketsRanks.dll` and `1.6/Assemblies/RocketsRanks.dll`.

Target framework `net48`, language version `latest`.

## Dependencies

- [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077)
- [Ideology DLC] (optional) - required only for promotion ceremonies. Without it the rest of the mod functions normally; the Ceremony settings tab is shown but greyed out.
