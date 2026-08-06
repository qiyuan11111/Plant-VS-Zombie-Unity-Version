# PvZ placement, card, and shadow fidelity checklist

## Audit boundary

The reference is the unpacked Plants vs. Zombies 0.9.9.1029 decompilation at
`D:\老D盘\Unity\PlantsVsZombies-decompilation`. The checklist covers every branch and
caller of these source entry points:

- `Lawn/CursorObject.cpp`: cursor-held plant and snapped cursor preview.
- `Lawn/Board.cpp`: planting hit conversion, grid/pixel conversion, and planting effects.
- `Lawn/Plant.cpp`: `PlantFlowerPotHeightOffset`, `PlantDrawHeightOffset`, and `DrawShadow`.
- `Lawn/SeedPacket.cpp`: packet portrait atlas selection, scale, and offsets.
- `Sexy.TodLib/TodCommon.cpp`: the shadow image's center-preserving scale behavior.

All branches in those functions are represented below. “Deferred” means catalogued but
outside the current three-plant/front-yard implementation; it does not mean undiscovered.

## Board and cursor

| Reference behavior | Status | Unity implementation |
|---|---|---|
| 80-pixel columns and 100-pixel front-yard rows | Implemented | `BoardGeometry` |
| Grid-local conversion uses the scene pivot at original screen `(480,300)` | Implemented and tested | `BoardGeometry` / `SampleScene` |
| Six pool rows at 85 pixels | Implemented geometry; no pool scene yet | `BoardGeometry` |
| Roof 85-pixel rows and first-five-column slope | Implemented geometry; no roof scene yet | `BoardGeometry` |
| 30-pixel high-ground adjustment | Implemented configuration | `GridManager.highGroundCells` |
| Exact cell hit rectangles | Implemented | `Grid.Contains` |
| Plant logical origin and 80x80 logical box | Logical origin stored; existing artwork remains ground-anchor driven | `Grid.LogicalOrigin` |
| Plant visual ground at logical `(40,74)` | Implemented | `Grid.GroundPosition` |
| Planting particle/effect point `(41,74)` | Deferred; planting particles are not implemented | — |
| Ordinary cursor hotspot at plant-local `(35,60)` | Implemented | `BoardGeometry.CursorToGroundOffset` |
| Snapped translucent preview uses grid origin plus draw-height offset | Ground alignment implemented; draw-height profiles deferred | `PlantingPreview` |
| Flying/grave-buster cursor hit Y `+15` | Deferred until those plant types exist | — |
| Spikeweed/spikerock cursor hit Y `-15` | Deferred until those plant types exist | — |
| Greenhouse cursor hit Y `-25` | Deferred with Zen Garden | — |
| Coffee bean searches current/down/up rows for a sleeping plant | Deferred until coffee bean exists | — |
| Column challenge previews all valid rows at 85-pixel spacing | Deferred with challenge mode | — |
| Zen Garden uses its own grid conversion | Deferred with Zen Garden | — |

## Plant draw height

| Reference behavior | Status |
|---|---|
| Pool floating sine wave of ±2 pixels | Deferred |
| Flower-pot stack height and scale correction table | Deferred |
| Flowerpot +26, lilypad +25, starfruit +10, tangle kelp +24, sea-shroom +28 | Deferred by plant type |
| Coffee bean -20, pumpkin +15, puff-shroom +5, scaredy-shroom -14, grave-buster -40 | Deferred by plant type |
| Spikeweed/spikerock roof, pool, bottom-row, pot, and six-row branches | Deferred by plant type |
| Current pea shooter, sunflower, and front-yard sun-shroom static offset 0 | Implemented |

## Seed cards

| Reference behavior | Status | Unity implementation |
|---|---|---|
| 50x70 packet | Implemented | `Card.prefab` |
| Default portrait scale 0.5 and origin offset `(5,9)` | Implemented through ground anchor `(0,-11)` | `PlantDefinition` / `EntityPresentation` |
| Per-plant portrait scale and offset | Implemented as data fields | `PlantDefinition` |
| Potato/chomper/etc. precomposed `packet_plants` atlas | Deferred until those plant types exist |
| Imitater washed-out filters and special less-washed-out list | Deferred with Imitater |
| Upgrade/background packet variants | Deferred with upgrade plants and challenge packets |
| Big Time and giant-wallnut overrides | Deferred with challenge mode |

## Plant shadows

| Reference behavior | Status | Unity implementation |
|---|---|---|
| Full 86x36 day texture, center pivot, Full Rect, no compression | Implemented | `plantshadow.png` and `.meta` |
| Full 86x36 night texture and stage switch | Implemented | `plantshadow2.png`, `Shadow.SetNight`, `GridManager.isNight` |
| Ordinary scale 1 and center 5 pixels above ground | Implemented | pea shooter and sunflower profiles |
| Small sun-shroom scale 0.5 and center 14 pixels above ground | Implemented for current small state | sun-shroom prefab |
| Sun-shroom growth interpolation from 0.5 to 1.0 | Deferred until growth state exists |
| Per-plant X/Y and scale table | Configuration path exists; values deferred with each plant type |
| No-shadow plant list and bungee/Zen Garden suppression | `drawsShadow` path exists; values deferred with those modes/types |
| Flying plant +10 Y and suppression over another plant/pumpkin | Deferred with flying plants and stack priorities |
| Squash correction while displaced from its grid | Deferred with squash state machine |

## Verification policy

The audit coverage for the stated boundary is complete: every reference branch is either
implemented or explicitly listed as deferred. Runtime parity is only claimed for the
current three plants on the normal front-yard board. Unity compilation and EditMode tests
must still be run in Unity 2022.3.57f1c2 on a machine with that editor installed.
