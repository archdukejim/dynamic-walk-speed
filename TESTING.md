# Testing Dynamic Walk Speeds

Six dev-mode actions ship with the mod, under **Dynamic Walk Speeds** in the
debug actions menu (enable Development mode in Options first).

Two different questions, two different tools:

- **Is the maths right?** Dump the speed table. Per cell, exact, in ticks.
- **What does it cost?** Benchmark the chain for ns/call, and run a tick
  profile for calls per tick. Multiply.

Run **Clean map for testing** before either. Ambient fauna and flora skew the
numbers and blur the results — the wildlife count feeds directly into the
hostile pawn scan.

## Measure in ticks, not microseconds

RimWorld movement is tick-quantised. `Pawn_PathFollower.CostToMoveIntoCell`
returns the number of ticks a pawn spends entering a cell, and that is the
number this mod changes. It is an exact, reproducible integer.

Timing a pawn walking across a map with a stopwatch measures the tick
scheduler, the frame rate and the JIT, not the modifiers. Do not do it. The
speed table below reports ticks and the ratio against vanilla, which is the
thing you actually want to compare.

Microseconds are the right unit for exactly one question: how long the
modifier chain itself takes to execute. That is what the benchmark action
measures, separately.

## Dump speed table (CSV)

Writes `DWS_SpeedTable.csv` next to your mod config (the path is logged).
One row per race per terrain:

| column | meaning |
|---|---|
| `race`, `group` | the race and the traction group it classified into |
| `terrain`, `manufactured` | the terrain and whether traction applies to it |
| `floorMult` | the floor multiplier before traction |
| `tractionFloor` | the floor multiplier after that creature's traction |
| `creatureSpeed` | the flat per-group speed multiplier |
| `barefoot` | the barefoot terrain penalty for this pawn |
| `totalMult` | the product actually applied |
| `baseTicks` | vanilla ticks per cell for this pawn |
| `moddedTicks` | ticks after the modifiers |
| `ratio` | `moddedTicks / baseTicks`. Below 1.0 is faster than vanilla |

Weather, filth, snow and territory are deliberately excluded from this table:
they depend on live map state rather than on the race and terrain pair, so
including them would make the table non-reproducible. Sort by `ratio` to find
the extremes.

### Determinism

- Pawn generation is wrapped in `Rand.PushState(20260801)`, so the same pawns
  are produced on every run.
- Every hediff is removed, and all apparel and equipment destroyed, so no
  injury, prosthetic or gear weight leaks into `baseTicks`.
- Humans therefore come out **barefoot**, which is intentional: it exercises
  the barefoot penalty. To measure booted pawns, equip footwear and compare.
- Known gap: biological age is not forced, so an animal's life stage is
  whatever the seed produced. It is consistent between runs but is not
  guaranteed to be the adult stage.

## Benchmark modifier chain

Runs the full modifier chain 200,000 times against the first spawned colonist,
after a 10,000 iteration warmup for the JIT, and logs nanoseconds per call.
Results accumulate into a sink so the loop cannot be optimised away.

This measures the mod's own cost, not vanilla's. Run it twice: once with all
modifiers on, once with them all off, to see the early-out path.

Clean the map first. The chain includes the hostile pawn check, which walks
every spawned pawn on a cache miss, so wildlife inflates the figure and makes
runs incomparable.

Note it reads the pawn's current cell, so standing your colonist on a filthy
or snowy cell exercises the two scans that cannot be cached. Do that
deliberately if you want the worst case, not by accident.

## Paint terrain test strips

Paints a 20-cell strip of every terrain in the test list, two rows apart,
starting at the mouse cell. Useful for eyeballing behaviour and for watching
pawns cross boundaries.

Terrains that do not exist in your modlist are skipped and logged rather than
throwing, so the list is safe across different mod setups.

## Sustained performance over time

### Why the obvious A/B is weaker than it looks

Saving unmodded, then reloading the same save with the mod on and comparing
five minutes of ticks, does not compare the same simulation. This mod changes
move costs, so pawns take different paths, arrive at different times, pick
different jobs and pull different random numbers. Within a minute the two runs
are different worlds. Animal wandering and a single incident firing in one run
and not the other move the tick rate more than this mod does.

That comparison is still worth having, but read it as two samples from two
distributions, not as a paired measurement. Run each side several times.

### The direct measurement

You do not have to infer the cost. Take it in two pieces:

1. **How often the postfix runs.** The profiler counts calls per game tick.
   The counter is a single increment behind a static bool, so it does not
   meaningfully perturb what it measures.
2. **What one call costs.** That is the benchmark action, in nanoseconds.

Multiply them for the mod's cost per tick, with no divergence problem at all.
A colony averaging 400 postfix calls per tick at 120 ns a call is spending
about 0.05 ms per tick, against a 16.7 ms budget at 60 TPS.

Deliberately not a Stopwatch inside the postfix: `Stopwatch.GetTimestamp` costs
roughly what the measured work costs, so the instrument would dominate the
reading.

### Start 5 minute tick profile

Samples once per wall-clock second for five minutes and writes a CSV:

| column | meaning |
|---|---|
| `wallSeconds` | seconds since profiling started |
| `gameTicks` | game ticks in that second |
| `ticksPerSecond` | achieved TPS, the headline number |
| `postfixCalls` | postfix invocations in that second |
| `callsPerTick` | invocations per game tick |
| `spawnedPawns` | pawns across all maps, the main driver of both |

The filename records whether the mod's modifiers were enabled, so the two
sides of a comparison do not overwrite each other.

**Set a speed the CPU cannot keep up with.** At normal speed RimWorld caps at
60 TPS and both runs will read 60, which looks like no impact regardless of
the truth. Use 3x or dev-mode ultrafast so the tick rate is bound by
simulation cost. The profiler advances on game ticks, so it also stalls while
paused.

### A better A/B than mod-out versus mod-in

Keep the modlist identical and toggle this mod's own modifiers instead. Same
save, same load order, same everything: one run with the modifiers on, one
with all of them off so the postfix early-outs. That isolates this mod's
logic and removes modlist mismatch and load-order variance entirely.

The difference between the two designs is worth knowing:

- **Modifiers off** still pays the Harmony patch dispatch on every call, so it
  measures the mod's *logic*.
- **Mod uninstalled** removes the patch as well, so it measures logic *plus*
  the cost of being patched at all.

Run both if you want the full picture. Start with the toggle version: it is
the cleaner experiment.

## Clean map for testing (destructive)

Destroys every plant, wild and hostile pawn, item, corpse, chunk and filth
pile on the map, removes non-player buildings, clears all snow, and flushes
the mod's caches. Your own colonists, animals and buildings are kept, as is
natural rock. It asks for confirmation first, and it cannot be undone: use a
throwaway colony.

**This matters most for the benchmark.** `HasActiveHostilePawns` iterates
every spawned pawn on the map, so a map full of ambient wildlife inflates
exactly the scan the caching work targets, and any stray filth on the pawn's
cell exercises the one per-cell scan that cannot be cached. Benchmarking on a
lived-in map measures the wildlife, not the mod.

It matters much less for the speed table, which calls the modifiers directly
against a terrain and a race and never touches cell contents.

Snow is cleared for the same reason: with snow present the vanilla tick
addend is in play, and you want that isolated rather than mixed into a
baseline.

## Building a clean test map

1. Start a new colony on a flat, low-vegetation biome. Sea ice or extreme
   desert give you an almost empty map to begin with.
2. Enable Development mode and run **Clean map for testing**.
3. Use the terrain strip painter where you want the test lanes.
4. Spawn the animals you want with the pawn spawner. The race list below is
   one representative per populated traction group.

Re-run the cleaner between benchmark passes. Plants regrow, filth
accumulates, and wildlife wanders in, so a long session drifts away from the
baseline you started with.

## What is covered

**Terrains** — natural (`Soil`, `SoilRich`, `MossyTerrain`, `Gravel`, `Sand`,
`SoftSand`, `Mud`, `MarshyTerrain`, `Ice`, `Riverbank`), roads (`PackedDirt`,
`BrokenAsphalt`), stone (`Sandstone_Rough`, `Sandstone_RoughHewn`,
`Sandstone_Smooth`), and built floors (`StrawMatting`, `WoodPlankFloor`,
`Concrete`, `PavedTile`, `TileSandstone`, `FlagstoneSandstone`, `MetalTile`,
`SilverTile`, `SterileTile`, `Bridge`).

The stone entries are generated by the game per rock type rather than declared
in XML, so they are resolved by name at runtime and skipped if absent.

**Races** — one or more per populated traction group:

| group | subjects |
|---|---|
| Humanlike | `Human` |
| Padded small | `GuineaPig`, `Rat`, `Cat`, `Squirrel` |
| Padded medium | `LabradorRetriever`, `Lynx` |
| Padded large | `Megasloth` |
| Hoofed medium | `Goat`, `Caribou` |
| Hoofed large | `Muffalo`, `Cow`, `Bison`, `Elephant` |
| Taloned | `Chicken`, `Duck`, `Goose` |
| Insectoid | `Megascarab`, `Megaspider`, `Spelopede` |
| Shelled | `Tortoise`, `Seal`, `Walrus` |
| Serpentine | `Cobra` |
| Mechanoid | `Mech_Agrihand`, `Mech_Centurion` |
| Other (fallback) | `Toughspike`, `StoneCrab` |

`GuineaPig` and `Cat` are both Padded small and deliberately included together:
they share a group and a default traction, which is the case for the per
species override.

Both lists live in `Source/Debug/DWSTestSubjects.cs`.
