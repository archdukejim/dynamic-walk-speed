# Dynamic Walk Speeds — Test Results

Date: 2026-08-02
Build under test: rebuilt clean, 0 warnings / 0 errors
Game: RimWorld 1.6.4871 rev590, Core + Royalty + Ideology + Biotech + Anomaly + Odyssey
Harness: `-quicktest -dws-test -savedatafolder=<isolated>`, three runs

## Result: 46 passed, 0 failed

**These tests were executed against the running game.** RimWorld was launched
headlessly with `-quicktest`, an in-game component ran the suite, results went
to `Player.log`, and the game shut itself down. Clean exit, code 0, 66 seconds,
no exceptions in the log.

Three real defects were found. Two were found only by running — a static model
could not have caught them.

---

## Method

The mod gained an auto-test entry point (`DWSAutoTest`, a `GameComponent`) that
arms only when RimWorld is launched with `-dws-test`. It waits 30 frames for
startup to settle, runs the suite, emits `[SYNAPSE-TEST] PASS/FAIL` lines in the
format `readlog.ps1` already parses, then calls `Root.Shutdown()` so the
launcher sees a clean exit instead of a timeout. This mirrors the RimSynapse
TestRunner convention.

`-quicktest` boots straight into a generated map with no menus, so the whole
loop is unattended. A dedicated `savedatafolder` was used with only Core, the
five DLCs and this mod active, so the real config and the 275-mod list were
never touched.

Run sequence:

| Run | Result | Change |
|---|---|---|
| 1 | 41 passed, **1 failed** | baseline — found the `generated` bug |
| 2 | 42 passed, 0 failed | after the manufactured fix; CSV then revealed the mechanoid bug |
| 3 | **46 passed, 0 failed** | after the barefoot gate fix, with 4 new regression cases |

---

## Defects found

### 1. Rough natural stone was treated as a built floor — FIXED

`FAIL terrain.Sandstone_Rough | manufactured expected False got True; floorMult expected 1.000 got 1.150`

`ResolveManufactured` treated `TerrainDef.generated` as evidence of a built
floor. `TerrainDefGenerator_Stone` sets that flag on **every** runtime-created
stone terrain, including `_Rough` and `_RoughHewn`. So natural rough-hewn rock
was getting the 1.15x manufactured floor bonus, and creature traction was being
applied to it as though someone had paved it.

**Only findable by running.** The `generated` field does not exist in the def
XML — it is set by the generator at load — so the offline model predicted
"not manufactured" and the running game disagreed. That disagreement was the
whole point of writing the prediction down first.

Fixed by dropping the `generated` check. `designationCategory`, research
prerequisites and the name patterns already classify every real floor
correctly, which run 2 and run 3 confirm across all 384 terrains.

### 2. Mechanoids took a barefoot penalty — FIXED

Not a failed assertion — spotted in the generated CSV:

```
Mech_Centurion  Gravel           barefoot 0.880   ratio 1.136
Mech_Centurion  Sandstone_Rough  barefoot 0.850   ratio 1.176
```

`GetBarefootMultiplier` gated on `pawn.apparel != null`. Mechanoids carry an
apparel tracker, so they read as permanently barefoot and were slowed on gravel
and rough stone for having no boots on feet they do not have.

The mood and injury paths were already correct — `CaresAboutBareFeet` checks
`RaceProps.Humanlike`. The speed path did not, so the three consequences of
going barefoot disagreed with each other about who could be barefoot.

Fixed by gating the speed penalty on `Humanlike` too. Four regression cases
added (`Mech_Centurion`, `Mech_Agrihand`, `Muffalo`, `GuineaPig`), all now
returning exactly 1.000 on rough stone.

### 3. The shipped group count was wrong twice — FIXED

`About.xml` and the Steam description originally claimed **18** populated
traction groups. Static analysis corrected that to **21**. The running game
says **20, over 312 races**.

Both earlier figures were computed from def XML. At runtime `DefDatabase`
resolves inheritance, so 312 ThingDefs have race properties versus the 154 that
declare a `<race>` block in their own XML. The docs now carry the measured
figure.

### 4. Open, no behavioural impact

- Pinnipeds (`Seal`, `SeaLion`, `Walrus`) group under `Shelled` by body name.
  Defensible for flippers, misleading as a label.
- `Nociosphere` lands in `Shelled_Large` because its body is `NociosphereShell`.

Both are grouping cosmetics. Traction is unaffected, so no speed changes.

---

## Measured results

### Suite — 46 cases

| Group | Cases | Result |
|---|---|---|
| Race classification | 12 | all pass |
| Terrain manufactured + floor multiplier | 12 | all pass |
| Barefoot penalty per terrain | 6 | all pass |
| Predicted cross values | 6 | all pass |
| Barefoot applies to humanlike only | 4 | all pass |
| Flat table vs fallback consistency | 3 | all pass |
| Census, CSV dump, benchmark | 3 | all pass |

### Flat lookup tables verified against the fallback path

The `Def.index` tables were the riskiest optimisation in the mod: a wrong row is
a silent gameplay bug, not a crash. Every def was checked against the resolve
path it replaced.

```
PASS tables.terrain | 384 terrains checked, 0 mismatched
PASS tables.weather |  24 weathers checked, 0 mismatched
PASS tables.race    | 312 races    checked, 0 mismatched
```

720 defs, zero disagreements. The optimisation is sound.

### Speed table — 700 rows, 28 races x 25 terrains

Ratio is `moddedTicks / baseTicks`. Below 1.0 is faster than vanilla.

| Race | Terrain | Total mult | Base ticks | Modded | Ratio |
|---|---|---|---|---|---|
| Mech_Centurion | Bridge | 1.188 | 37.5 | 31.6 | **0.842** |
| Mech_Agrihand | SterileTile | 1.188 | — | — | 0.842 |
| Human | Concrete | 1.150 | 13.0 | 11.3 | 0.870 |
| Megasloth | Concrete | 1.038 | — | — | 0.964 |
| Muffalo | Concrete | 0.910 | 13.3 | 14.7 | 1.099 |
| **GuineaPig** | **Concrete** | **0.888** | **12.0** | **13.5** | **1.127** |
| Human (barefoot) | Gravel | 0.880 | 13.0 | 14.8 | 1.136 |
| Human (barefoot) | Sandstone_Rough | 0.850 | 13.0 | 15.3 | **1.176** |

- **Range: 0.842 to 1.176.** Worst case ±18%, nothing runs away.
- **387 of 700 rows (55%) are exactly 1.000** — the mod changes nothing there.
  That is natural terrain for animals, and it is correct.

The guinea pig result is the one this feature was built for, and the game
produced it: **12.0 ticks on soil, 13.5 on concrete.** Paving the floor makes
your guinea pig measurably slower.

The design argument holds where it matters. `GuineaPig` (0.2) and `Megasloth`
(4.0) share the body def `QuadrupedAnimalWithPawsAndTail` — the game calls both
"paws" — and came out with traction of opposite sign, 0.888 against 1.038 on the
same floor.

### Group census — measured

312 races across **20 populated groups**:

| Group | Count | | Group | Count |
|---|---|---|---|---|
| Padded_Medium | 62 | | Shelled_Medium | 14 |
| Padded_Small | 40 | | Taloned_Small | 14 |
| Hoofed_Large | 34 | | Other_Medium | 7 |
| Mechanoid_Medium | 30 | | Insectoid_Medium | 6 |
| Taloned_Medium | 22 | | Other_Large | 6 |
| Hoofed_Medium | 20 | | Shelled_Small | 6 |
| Padded_Large | 18 | | Humanlike_Medium | 4 |
| Mechanoid_Large | 16 | | Insectoid_Small | 4 |
| | | | Shelled_Large | 3 |
| | | | Insectoid_Large, Mechanoid_Small, Serpentine_Small | 2 each |

**Only 13 of 312 races (4.2%) fall into an `Other` group** and get no traction
treatment. The def-driven classifier covers 95.8% of the game unattended, which
was the case for not maintaining a species list by hand.

### Benchmark — modifier chain

200,000 iterations after a 10,000 iteration warmup, three runs:

| Run | Terrain under the pawn | ns/call |
|---|---|---|
| 1 | Soil | 400.5 |
| 2 | Granite_Rough | 384.7 |
| 3 | MossyTerrain | 415.3 |

**Roughly 400 ns per call**, stable within about 8% across runs and terrains.

Context: at 60 TPS the budget is 16.7 ms per tick. At 400 ns a call, the mod
costs 1% of a tick at 400 calls per tick, or about 4% at 1,600 calls. Whether a
real colony reaches those call counts is still unmeasured — see below.

This is considerably more than the ~120 ns figure used as an illustration in
`TESTING.md`; that was a made-up example, and 400 ns is the measured value.

---

## Performance impact versus vanilla — measured

Four timed runs, 4,000 game ticks each at Ultrafast on `-quicktest` maps.

| Run | Modifiers | Pawns | TPS | Calls/tick |
|---|---|---|---|---|
| A | ON | 42 | 567.1 | 0.4 |
| B | **OFF** | 49 | **437.6** | 0.4 |
| C | ON | 77 | 372.5 | 0.6 |
| D | ON | 54 | 481.4 | 0.6 |

### The A/B cannot resolve the mod, and that is the answer

The modifiers-**off** run was **slower** than the modifiers-on run. Identical
on-configurations ranged 372.5 to 567.1 TPS — a **52% spread** — and the off
run sits comfortably inside that band. TPS tracks pawn count (77 pawns gave
372 TPS, 42 pawns gave 567), which is what actually drives the tick.

The mod's cost is **below the noise floor** of an A/B at this scale. Anyone
reporting a TPS delta from a single pair of runs is reporting map generation
variance, not a mod.

### The direct measurement

| Quantity | Value |
|---|---|
| Postfix calls per tick | **0.4 – 0.6** (measured) |
| Cost per call | **~400 ns** (measured) |
| Cost per tick | **0.00016 – 0.00024 ms** |
| Tick budget at 60 TPS | 16.67 ms |
| **Share of the tick budget** | **~0.001%** |

### Why the call rate is so low

`Pawn_PathFollower.CostToMoveIntoCell` fires **once per pawn per cell
entered**, and a pawn spends roughly 13 ticks crossing a cell. So the call rate
is bounded by *actively moving pawns ÷ ~13*, not by pawn count, colony size or
map size. With 42 pawns mostly idle, 0.4 calls per tick is exactly that
arithmetic.

**The mod is not in the pathfinding loop.** RimWorld's `PathFinder` uses its
own `CalculatedCostAt`, a different method this mod does not touch. If path
computation went through the patched method, a single 100-cell route would
generate hundreds of calls and the measured rate would be orders of magnitude
higher. It is not.

Scaling that structurally: even 200 pawns all moving at once — a mega-colony
mid-raid — gives about 15 calls per tick, or **0.006 ms**, roughly **0.04%** of
a 60 TPS budget.

### Gameplay consequence worth knowing

Because the mod changes movement cost and not pathfinding cost, pawns
**traverse** a paved corridor faster but do not **prefer** it when choosing a
route. They will still cut across the mud if it is geometrically shorter. Making
them route around it would mean patching `PathFinder`, which is a far hotter
path and is not something this mod does.

### Honest limits on these numbers

- `-quicktest` maps, 42 to 77 pawns. A mature colony has more pawns but not
  proportionally more *moving* pawns.
- The benchmark pawn stood on clean, snow-free ground, so the filth scan and
  the snow read were both on their cheap paths.
- The hostile-pawn scan was never under raid pressure, though it is cached per
  map and faction specifically so a raid cannot make it expensive.
- 400 ns/call is a tight-loop figure with everything warm in cache. In the real
  tick the cache lines are colder, so treat it as a floor.

## Still not measured

| Test | Why not |
|---|---|
| Calls per tick on a mature colony | Measured at 0.4-0.6 on quicktest maps; a lived-in save would confirm the scaling |
| Weather, filth, snow, territory multipliers | Depend on live map state; the suite covers the deterministic per-def values only |
| Footwear traction and quality scaling | Needs real apparel instances with quality; `-quicktest` colonists were stripped |
| Mood, sore feet, foot injury | Need hours of game time |
| Vehicle traction | Still unknown whether Vehicle Framework routes through the vanilla move cost |
| Snow tick scaling | `-quicktest` map had no snow |

The benchmark ran on a `-quicktest` map, so the hostile pawn scan and the filth
scan were both on their cheap paths. A benchmark taken during a raid, standing
on filth, would be higher.

---

## Reproducing

```
dotnet build Source/DynamicWalkSpeeds.csproj -c Debug
RimWorldWin64.exe -quicktest -dws-test -savedatafolder="<folder>"
```

The savedatafolder needs `Config/ModsConfig.xml` listing Core, the DLCs and
`archdukejim.dynamicwalkspeeds`. Results land in `Player.log` as
`[SYNAPSE-TEST]` lines; the speed table CSV lands in that folder's `Config`.

Exit code 0 with `SUMMARY failed=0` is a pass. A timeout means the component
never armed or `Root.Shutdown()` did not take.
