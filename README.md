# Dynamic Walk Speeds

A RimWorld 1.6 mod. The ground your pawns walk on decides how fast they cross it.

[Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3775191933) · MIT licensed

**Current release: 0.2**

## What it does

Five modifiers are read for each cell a pawn steps into and multiplied together.
Above `1.00x` is faster than vanilla, below is slower. Everything is a slider and
every modifier can be switched off on its own.

- **Weather** — per weather multiplier, seeded from each weather's own rain and snow rate.
- **Floors** — per terrain multiplier. Manufactured ground starts at `1.15x`, natural at `1.00x`.
- **Creature traction** — how much of the floor bonus a creature can actually use.
  A guinea pig on concrete comes out at `0.89x`: paving actively hurts it.
- **Snow and filth** — scales the game's own snow penalty from 0% to 1000%; filth is
  charged by the unit of thickness.
- **Hostile territory** — slower on enemy ground, unless the pawn is fleeing.

Plus footwear traction scaled by quality.

**Experimental, shipped disabled:** barefoot terrain penalties and three
consequences of going barefoot — a mood penalty, a stacking sore-feet memory, and
a rare foot injury. These are the only parts of the mod that add content and
damage pawns, and they have not been through real play, so they are opt-in until
1.0.

## Creature classification

Creatures are grouped from the game's own race data rather than a hand-maintained
species list, on two axes: **body type** (`race.body`) and **size band**
(`baseBodySize`). With all DLCs loaded that is **20 populated groups over 312
races**, and only 4.2% fall through to an untreated `Other` group. Modded animals
classify themselves; those shipping a custom body are classified by its parts.

The size axis is what earns its keep: the game calls a 0.2 guinea pig and a 4.0
megasloth both "paws", and they come out with traction of opposite sign.

## Compatibility

- **Combat Extended** — yes. CE changes the move speed *stat*; this changes what a
  *cell* costs to enter. Separate layers, no shared patch target.
- **Vehicle Framework** — detected by type, no assembly reference, no dependency
  either way.
- **Safe to add or remove mid-save.** Nothing is written to pawns or maps.

## Performance

One Harmony postfix on `Pawn_PathFollower.CostToMoveIntoCell`. Measured at roughly
**0.001% of the tick budget**: the postfix fires 0.4–0.6 times per tick at about
400 ns a call. It is not in the pathfinding loop.

See the [wiki](https://github.com/archdukejim/dynamic-walk-speed/wiki) for the
measurements and how to reproduce them.

## Documentation

Everything lives on the [wiki](https://github.com/archdukejim/dynamic-walk-speed/wiki)
rather than in the shipped mod folder, so players can read it without digging
through game files:

- **[Speed Table](https://github.com/archdukejim/dynamic-walk-speed/wiki/Speed-Table)**
  — every creature on every floor, measured in game
- **[Test Results](https://github.com/archdukejim/dynamic-walk-speed/wiki/Test-Results)**
  — the 48-case verification run and the performance figures
- **[Testing Methodology](https://github.com/archdukejim/dynamic-walk-speed/wiki/Testing-Methodology)**
  — how to reproduce any of it

## Known limits

- **Pawns do not prefer faster routes.** This mod changes how fast a cell is
  crossed, not which cell is chosen, so a colonist will still cut across mud on the
  shorter line. Pathfinding preference is planned for 1.0.
- Ideology nudity precepts are not handled; only the `Nudist` trait is exempt from
  the barefoot mood penalty.
- Vanilla ships no footwear, so the footwear tab is idle until a mod adds some.

## Building

```
dotnet build Source/DynamicWalkSpeeds.csproj -c Debug
```

Output goes to `1.6/Assemblies/`. The csproj expects RimWorld at the default Steam
path; edit `RimWorldPath` if yours differs.
