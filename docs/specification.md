# The Cursed Labyrinth

>Project will be stored inside a public GitHub repository
[hyperlink to the repo](https://github.com/aliekins/The_Cursed_Labyrinth.git)
_(it would probably be a nicer read over there)_

## Project Overview  
A 2D, top-down dungeon crawler with light puzzle elements and a (somewhat) dark fantasy narrative. Players embody a lone knight lost in an ever-shifting maze, guided (and tested) by the ghost of the creature he slew.

- **Procedural Generation**
  - BSP-based rooms + corridor carve
- **Atmosphere & Theme** 
  - Journey, discovery, betrayal, bittersweet resolution 
  - Pixel art (in case I do not have time to create my own, sources will of course be listed)
  - Moody lighting, ambient SFX, minimal UI chrome 
- **Modular Puzzles** 
  - Lever-activated doors 
  - Pressure plates & weighted stones 
  - Sliding-block puzzles 
  - Ghost-driven hints & reveals 

Each region hides lore fragments that deepen the knight–ghost relationship and foreshadow the gods’ curse.

## Narrative & Ghost Integration  
1. **Inciting Incident** 
   - Knight awakens alone
2. **Ghost-Guide Arc** 
   - **Refusal**: initial combat stubs ignore “whispers” 
   - **Narrative Beats**: diary-style fragments unlock on puzzle solves 
   - **Abilities**: 
     - `GhostHintRoutine`: phases through walls after 30s of inactivity 
     - `SwitchPulse`: flickers levers to indicate hidden switches 
   - **Acceptance**: a mid-game corridor blocks until the player “listens” (dialogue flag + delegate `OnDialogueChoice`) 
3. **Biomes & Ghost Evolution** 

   | Biome               | Puzzles                          | Ghost Trait Unlocked              |
   |---------------------|----------------------------------|-----------------------------------|
   | Entry Halls         | Simple block pushes              | “Echo Vision” (reveals hidden tiles) |
   | Collapsed Quarry    | Pressure plates under rubble     | “Stone Touch” (shatter walls briefly) |
   | Grove of Whispers   | Sliding blocks in fungus groves  | “Mire Step” (phase through one obstacle) |

4. **Resolution** 
   - Final doors swing open; ghost fades, revealing the gods’ betrayal 
   - Knight returns home, haunted by the curse he helped enact 

5. **Clarification on Procedural Dungeon Generation and Biomes**
  - BSP-based rooms 
  - Corridor carve via A* between room centers 
  - Post-processing the graph: BFS from spawn - assigning biome zones by distance thresholds 

## Alignment with NPRG035  
- **Functionality** 
  - Movement, combat stub, puzzles 
- **Efficiency** 
  - All file I/O via `StreamReader` / `Read()` patterns (map prefabs, lore JSON) 
- **Maintainability** 
  - Clear layering: 
    1. **Data** (tiles, map, puzzle definitions) 
    2. **Generation** (IDungeonGenerator, BSP) 
    3. **Visualization** (TilemapVisualizer, SpriteAtlas) 
    4. **Gameplay** (PlayerController, PuzzleManager) 
- **C# Fundamentals** 
  - Value vs. reference: `struct Vector2Int` vs. `class Entity` 
  - DTOs: records for lore fragments, puzzle configs (nullable where appropriate) 
  - Exception safety: robust parsing of external configs 

## Alignment with NPRG038  
- **Generics & Constraints** 
- **Delegates & Events**
- **LINQ Queries**
- **Reflection & Attributes**
- (most likely as well) **Async/Threading** - Unity coroutines for time-based effects (torch flicker, ghost fades)

## Functional Requirements
- Character
    - 8-directional, grid-based movement
    - sprite flipping / orientation animations

- Dungeon Generation
    - choose BSP rooms + A* corridors

- Puzzle Framework
    - pluggable via IPuzzleElement
    - lever doors: toggle wall tiles
    - pressure plates: detect player / block weights
    - sliding blocks: grid-aligned moves with collision checks
    - ghost hints: trigger via event delegate
    
## Non-Functional Requirements
- Performance
    - prefab pooling (avoid GC spikes)
    - IReadOnlyList<T> for query results
    - sprite atlas batching

- Extensibility
    - auto-register new puzzles via reflection scan
    - biome modules pluggable in generator pipeline

- Testing & Documentation
    - xUnit tests
    - doc comments on all public APIs
    - README: setup, run instructions, art attribution list

