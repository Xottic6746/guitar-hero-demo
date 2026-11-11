# Neon Riff — Unity Rhythm Game

A complete, original rhythm-game prototype built in Unity. It creates its neon stage and original synthesized demo soundtrack at runtime, so there are no external art or music assets to install.

## Open and play
1. Install Unity Hub and Unity **2022.3 LTS** or later.
2. Clone/download this repository and open its folder in Unity Hub.
3. Wait for Unity to import and compile the scripts.
4. From the Unity menu choose **Neon Riff → Create Playable Demo Scene**.
5. Press **Play**. Choose a difficulty and click **START THE SHOW**.

The setup command creates `Assets/Scenes/NeonRiff.unity` and adds it to Build Settings. Run it once per fresh checkout; the scene can then be committed if you want to keep it in your local project.

## Play
- Hit the colored notes when they reach the glowing line: **A**, **S**, **D**, **F** for the four lanes.
- **Esc** pauses; Esc again resumes.
- Build combo streaks to raise the score multiplier, keep health above zero, and aim for an S rating.
- Replay from the results screen with **Enter**, **Space**, or the button.

## Included
- Three difficulty levels: Chill, Voltage, and Overdrive
- Procedural 3D neon stage and four color-coded lanes
- Timing windows, score, combo multiplier, health, accuracy/rating, and results screen
- Procedurally synthesized original backing track generated in code
- Editor menu command that creates a playable scene and enables it in Build Settings

The game uses original names, visuals, and generated audio; it is a genre-inspired project, not a copy of Guitar Hero assets or songs.
