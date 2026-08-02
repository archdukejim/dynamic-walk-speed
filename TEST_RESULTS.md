# Dynamic Walk Speeds — Test Results

Date: 2026-08-02
Build under test: commit `65e4953`, rebuilt clean, 0 warnings / 0 errors
Game: RimWorld 1.6, Core + Royalty + Ideology + Biotech + Anomaly + Odyssey
Modlist context: 275 subscribed Workshop mods

---

## Status: static verification complete, in-game runs NOT executed

**Nothing in this document was produced by running RimWorld.** The six dev-mode
actions shipped in this mod require the game launched, a colony loaded and the
debug menu driven by hand. That has not happened.

What *was* executed is a static verification pass against the game's actual def
XML on this machine. That is a genuine test with genuine results — it found two
real defects, documented below — but it verifies **inputs and predicted
outputs**, not the running code.

Sections marked **PREDICTED** are computed by a PowerShell reimplementation of
the mod's classification and multiplier rules, run against the real def data.
They are the values the in-game speed table *should* reproduce. If the in-game
CSV disagrees with them, one of the two implementations is wrong and that
disagreement is the finding.

Sections marked **PENDING** need you at the keyboard.

---

## Methodology

### What was run

1. **Def resolution.** Every terrain and race defName in the test subject lists
   was resolved against all XML under the game's `Data` folder, across Core and
   all five DLCs.
2. **Runtime-generated terrain check.** The stone terrains are not in XML. The
   naming pattern was verified by locating the literal string `Vacstone_RoughHewn`
   inside `Assembly-CSharp.dll` and confirming `TerrainDefGenerator_Stone` and
   `ImpliedTerrainDefs` are present.
3. **Inheritance-aware classification.** RimWorld defs inherit scalar fields
   through `ParentName`. The verifier walks that chain, because
   `designationCategory` and `fleshType` are usually inherited from an abstract
   parent rather than declared on the def. A naive read misclassifies almost
   every floor.
4. **Group census.** All 154 races with a body were classified using the same
   cascade the C# uses: `fleshType` → `intelligence` → body defName pattern →
   body part scan.
5. **Cross table.** 25 races × 25 terrains = 625 predicted rows.

### API verification

Each RimWorld API the mod depends on was confirmed by probe-compiling against
the installed `Assembly-CSharp.dll` rather than assumed:

| API | Result |
|---|---|
| `Def.index`, `DefDatabase<T>.DefCount` | present — flat lookup tables are valid |
| `WeatherBuildupUtility.MovementTicksAddOn` | present |
| `snowGrid.GetCategory` / `.GetDepth` / `.SetDepth` | present |
| `Filth.thickness` | present |
| `JobDefOf.Flee`, `MentalStateDefOf.PanicFlee` | present |
| `LudeonTK.DebugAction`, `AllowedGameStates` | present |
| `ThingRequestGroup.Plant/.Filth/.HaulableEver/.BuildingArtificial` | present |
| `SnowUtility`, `SnowCategory` | **absent in 1.6** — renamed, see finding 3 |

---

## Findings

### 1. Doc error: the group count was wrong (fixed)

Shipped `About.xml` and the Steam description both claimed **18 populated
groups**. The correct figure is **21**.

The 18 came from an early survey whose insectoid and mechanoid detection used
a body-name regex, where the shipped code checks `race.FleshType`. The two
disagree on the Anomaly fleshbeasts and on several mechs. Recomputed with the
code-accurate cascade over all 154 races, the answer is 21.

Both files corrected.

### 2. Test list error: `Toughspike` is not an insectoid (fixed)

`Toughspike` was listed as the Insectoid_Medium representative. It classifies as
**`Other_Medium`**: it is an Anomaly fleshbeast whose `fleshType` is not
Insectoid and whose body parts are `FleshbeastLeg` / `FleshbeastHead`, matching
no traction pattern.

The behaviour is correct — a fleshbeast should not be treated as an insect — but
the test list mislabelled it, so the Insectoid group had **no real coverage at
all**. Replaced with `Megaspider` and `Spelopede`, and `Toughspike` plus
`StoneCrab` retained as deliberate Other-fallback coverage.

### 3. 1.6 renamed the snow API

`SnowUtility` and `SnowCategory` do not exist in 1.6. Odyssey generalised them
into `WeatherBuildupUtility` and `WeatherBuildupCategory`, covering sand as well
as snow. This was caught during implementation, not after. Any 1.5-era guide
referencing `SnowUtility` is stale.

### 4. Open: crabs fall through to `Other`

`StoneCrab` and `HermitCrab` classify as **`Other_Small`**. Their body def is
`Crab`, which matches no name pattern, and its parts are `Arm`, `InsectLeg`,
`InsectHeart`, `InsectMouth` — no paw, claw, hoof or foot. Their `fleshType` is
blank, inheriting from `AnimalThingBase`, so the insectoid check misses too.

**No behavioural impact:** Insectoid and Other both default to 1.00 traction, so
speeds are identical either way. It matters only for which settings slider
governs them — someone tuning "Insectoid" would reasonably expect crabs included.

Suggested fix if you want it: add an `InsectLeg` / `Insect*` tier to the part
scan. Not applied, because it changes grouping without changing behaviour and
you asked for testing rather than redesign.

### 5. Open: `Pinniped` is grouped as `Shelled`

`Seal`, `SeaLion` and `Walrus` classify as Shelled via the `Pinniped` body-name
pattern. Defensible for flippers, and the 0.25 traction is reasonable, but the
group *label* is misleading. `Nociosphere` also lands in Shelled_Large because
its body is `NociosphereShell`. Cosmetic only.

---

## PREDICTED results

### Terrain classification — 25/25 resolved

22 resolved directly from XML. The three stone terrains are generated at
runtime and are expected to resolve in game; `Sandstone` exists as a natural
rock ThingDef, and the `_Rough` / `_RoughHewn` / `_Smooth` suffix pattern is
confirmed in the assembly.

| Terrain | Manufactured | Floor | Barefoot |
|---|---|---|---|
| Soil, SoilRich, MossyTerrain, Sand, SoftSand, Mud, MarshyTerrain, Riverbank, PackedDirt | no | 1.00 | 1.00 |
| Gravel | no | 1.00 | **0.88** |
| Ice | no | 1.00 | **0.90** |
| BrokenAsphalt | no | 1.00 | **0.92** |
| Sandstone_Rough, Sandstone_RoughHewn | no | 1.00 | **0.85** |
| StrawMatting, WoodPlankFloor, Concrete, PavedTile, TileSandstone, FlagstoneSandstone, MetalTile, SilverTile, SterileTile, Bridge, Sandstone_Smooth | **yes** | **1.15** | 1.00 |

Manufactured detection behaved correctly on every floor, including `Bridge` and
`StrawMatting`, which have no research prerequisite and are caught by
`designationCategory` inherited from `FloorBase`.

### Race classification — 25/25 resolved

| Race | Body | Size | Group | Traction |
|---|---|---|---|---|
| Human | Human | 1.0 | Humanlike_Medium | 1.00 |
| GuineaPig | QuadrupedAnimalWithPawsAndTail | 0.2 | Padded_Small | −0.75 |
| Rat | QuadrupedAnimalWithPaws | 0.2 | Padded_Small | −0.75 |
| Cat | QuadrupedAnimalWithPawsAndTail | 0.32 | Padded_Small | −0.75 |
| Squirrel | QuadrupedAnimalWithPawsAndTail | 0.2 | Padded_Small | −0.75 |
| LabradorRetriever | QuadrupedAnimalWithPawsAndTail | 0.75 | Padded_Medium | −0.25 |
| Lynx | QuadrupedAnimalWithPawsAndTail | 0.6 | Padded_Medium | −0.25 |
| Megasloth | QuadrupedAnimalWithPawsAndTail | 4.0 | Padded_Large | +0.25 |
| Goat | QuadrupedAnimalWithHooves | 0.75 | Hoofed_Medium | −0.40 |
| Caribou | QuadrupedAnimalWithHooves | 1.0 | Hoofed_Medium | −0.40 |
| Muffalo, Cow, Bison | QuadrupedAnimalWithHooves | 2.4 | Hoofed_Large | −0.60 |
| Elephant | QuadrupedAnimalWithHoovesTusksAndTrunk | 4.0 | Hoofed_Large | −0.60 |
| Chicken, Duck | Bird | 0.3 | Taloned_Small | −0.60 |
| Goose | Bird | 0.6 | Taloned_Medium | −0.40 |
| Megascarab | BeetleLike | 0.2 | Insectoid_Small | 1.00 |
| Tortoise | TurtleLike | 0.5 | Shelled_Medium | +0.25 |
| Seal | Pinniped | 0.8 | Shelled_Medium | +0.25 |
| Walrus | PinnipedWithTusks | 2.0 | Shelled_Large | +0.25 |
| Cobra | Snake | 0.25 | Serpentine_Small | −0.50 |
| Mech_Agrihand | Mech_Agrihand | 0.7 | Mechanoid_Medium | +1.25 |
| Mech_Centurion | Mech_Centurion | 3.6 | Mechanoid_Large | +1.25 |
| Toughspike | Toughspike | 1.0 | Other_Medium | 1.00 |

**The size axis does the work it was added for.** `GuineaPig` at 0.2 and
`Megasloth` at 4.0 share the body def `QuadrupedAnimalWithPawsAndTail` — the
game calls both "paws" — and land in different groups with traction of opposite
sign. That was the entire argument for the two-axis design and it holds.

### Full group census — 21 populated groups, 154 races

| Group | Count | Examples |
|---|---|---|
| Padded_Medium | 33 | Cougar, Husky, Dryad_Barkskin |
| Padded_Small | 19 | Rat, Otter, Armadillo |
| Hoofed_Large | 17 | Moose, Yak, Hippo |
| Mechanoid_Medium | 13 | Mech_Lancer, Mech_Pikeman |
| Taloned_Medium | 11 | Cassowary, Peacock, Goose |
| Padded_Large | 9 | Chimera, Alligator, Gorilla |
| Hoofed_Medium | 9 | Gazelle, WildBoar, Caribou |
| Mechanoid_Large | 8 | Mech_CentipedeGunner, Mech_Tunneler |
| Taloned_Small | 7 | Bluebird, Crow, Quail |
| Shelled_Medium | 6 | Seal, SeaLion, Tortoise |
| Other_Medium | 4 | Toughspike, Trispike, Fingerspike |
| Insectoid_Medium | 3 | Locust, Megaspider, Spelopede |
| Other_Large | 3 | Devourer, Bulbfreak, Dreadmeld |
| Insectoid_Small, Humanlike_Medium, Shelled_Large, Other_Small | 2 each | |
| Insectoid_Large, Mechanoid_Small, Serpentine_Small, Shelled_Small | 1 each | |

**Only 9 of 154 races (5.8%) land in an `Other` group** and therefore get no
traction treatment. Given that the alternative was a hand-maintained species
list, the def-driven classifier covers 94% of the game's races unattended.

### Cross table — 625 rows

Tick ratio is `moddedTicks / baseTicks`. Below 1.00 is faster than vanilla.

| Race | Terrain | Effective floor | Tick ratio | Reading |
|---|---|---|---|---|
| Human | Concrete | 1.150 | **0.870** | 13% faster |
| Mech_Centurion | Concrete | 1.188 | **0.842** | fastest case measured |
| Megasloth | Concrete | 1.038 | 0.964 | mild benefit |
| Muffalo | Concrete | 0.910 | 1.099 | hooves lose on tile |
| Chicken | Concrete | 0.910 | 1.099 | |
| **GuineaPig** | **Concrete** | **0.888** | **1.127** | **paving hurts, as designed** |
| Cat | Concrete | 0.888 | 1.127 | identical to guinea pig |
| Human (barefoot) | Sandstone_Rough | 1.000 | **1.177** | slowest case measured |
| Human (barefoot) | Gravel | 1.000 | 1.136 | |
| any animal | any natural terrain | 1.000 | 1.000 | untouched |

Summary across all 625 rows:

- **Range:** 0.8421 to 1.1765. Nothing runs away; worst case is ±18%.
- **345 of 625 rows (55%) are exactly 1.000** — the mod changes nothing at all.
  That is natural terrain for every creature, and it is correct: traction only
  applies to manufactured ground, and animals take no barefoot penalty.

The `GuineaPig` on `Concrete` result of **0.8875 → 1.127 tick ratio** is the
specific outcome this feature was built for, arrived at from real def data.

### Confirmed coupling: Cat and GuineaPig are identical

Both are Padded_Small, both −0.75, both 1.127 on concrete. This was flagged when
the feature was designed and the cross table confirms it. If a cat should keep
its footing indoors, use the per-species override. Body type and size cannot
separate a retractable claw from a digging nail.

---

## PENDING — requires the game running

None of the following has been measured. Protocol is in `TESTING.md`.

| Test | Action | What it answers |
|---|---|---|
| Actual speed table | Dump speed table (CSV) | Does the running code reproduce the predicted table above? |
| Per-call cost | Benchmark modifier chain | Nanoseconds per postfix call |
| Calls per tick | Start 5 minute tick profile | How often the postfix actually runs |
| Sustained TPS | Tick profile, modifiers on vs off | Cost per tick = calls/tick × ns/call |
| Weather, filth, snow, territory | — | Excluded from prediction: they depend on live map state |
| Footwear traction and quality scaling | Manual, equip VAE boots | Needs real apparel instances with quality |
| Mood, sore feet, foot injury | Manual observation | Needs hours of game time |
| Vehicle traction | Manual, spawn a VF vehicle | Still unknown whether VF routes through the vanilla move cost |

### The first thing to check

Compare the in-game `DWS_SpeedTable.csv` against the cross table above.
`GuineaPig` on `Concrete` should read `tractionFloor` 0.8875 and `ratio` 1.127.
If it does, the classifier, the traction maths and the lookup tables are all
confirmed end to end in one comparison.

### Known determinism gap

Test pawn generation is seeded, hediffs are stripped and apparel destroyed, but
**biological age is not forced**, so an animal's life stage is whatever the seed
produced. Consistent between runs, not guaranteed adult. Life stage affects body
size and therefore `baseTicks`, so `baseTicks` may not match a hand-checked adult
of the same species. The `ratio` column is unaffected.

---

## Verification artifacts

Raw CSVs are in the session scratchpad, not committed:

- `predicted_terrain.csv` — 25 rows
- `predicted_races.csv` — 25 rows
- `predicted_cross.csv` — 625 rows
- `census.csv` — all 154 classified races
