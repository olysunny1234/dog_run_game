# Dog Agility Park

A 3D dog agility game built in Unity where you control a German Shepherd through an obstacle course.

## How to Play

| Control | Action |
|---------|--------|
| W / A / S / D | Move (camera-relative 8-directional) |
| Shift | Run (gallop) |
| Space | Jump |
| B | Bark |
| Mouse Scroll | Zoom in / out |
| Middle Mouse + Drag | Orbit camera |

## Features

- **German Shepherd 3D model** with textured skin and skeletal animations (idle, walk, run, jump)
- **Dog agility obstacles** - jump hurdles, weave poles, A-frame ramp, hollow tunnel, tire jump
- **Realistic physics** based on real dog biomechanics:
  - Walk: 3.2 m/s, Run: 7.0 m/s (actual Labrador/Golden Retriever speeds)
  - Jump height: ~1.27m (medium dog standing jump)
  - Smooth acceleration (5 m/s²) and deceleration (8 m/s²)
  - Speed-dependent turning radius (tighter turns at walk, wider at run)
- **Animation blend trees** for natural walk/run transitions with directional leaning
- **Kenney Nature Kit** low-poly trees, grass, bushes, and flowers (CC0)
- **Fenced yard** with bushes along the perimeter
- **Camera system** with zoom and orbit controls
- **Puppy bark** sound effect on B key

## Setup

1. Clone this repo
2. Open the project in **Unity 6** (or Unity 2022.3+)
3. Wait for Unity to import all assets (may take a minute on first open)
4. Open the scene: `Assets/Scenes/DogGame.unity`
5. Go to **Tools > Build GS Controller** in the menu bar (one-time setup for animations)
6. Press Play

All assets including the German Shepherd model are included in the repo - no additional downloads needed.

## Project Structure

```
Assets/
  Scenes/          - DogGame scene
  Scripts/         - All game scripts
    DogGameSetup.cs          - Builds the entire scene at runtime
    PlayerDogController.cs   - Player input, movement, animation
    CameraFollow.cs          - Camera zoom/orbit system
    Dog.cs                   - AI dog behavior
    BallController.cs        - Ball physics tag
  Resources/
    Audio/           - Puppy bark sound (CC-BY 4.0)
    Models/
      Dog/           - German Shepherd model, animations, controller
      Trees/         - Kenney Nature Kit tree models (CC0)
      Grass/         - Kenney Nature Kit grass models (CC0)
      Bushes/        - Kenney Nature Kit bush models (CC0)
      Flowers/       - Kenney Nature Kit flower models (CC0)
  RSG_DogsPack/    - German Shepherd source assets
  Editor/          - Editor-only scripts for animation setup
```

## Credits

- **German Shepherd 3D Model** - RetroStyle Games (Unity Asset Store)
- **Nature Kit** - Kenney.nl (CC0 License)
- **Puppy bark sound** - Orange Free Sounds (CC-BY 4.0)
- Built with assistance from Claude Code by Anthropic
