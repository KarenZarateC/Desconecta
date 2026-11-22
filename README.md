# Desconecta

Desconecta is a 3D educational game made with Unity for children ages 8–12. Set in a family home, it encourages responsible use of water, electricity, and gas. Players explore the house, identify everyday waste, and choose practical ways to address it. The project is based on the [EcoHogar game design document](https://miro.com/app/board/uXjVHnfIILI=/).

![Desconecta home screen](Documentation/Screenshots/Captura-Inicio.png)

## Get Started

**Requirements:** Unity **6000.3.17f1 LTS**. Install Windows Build Support (Mono) if you plan to create a Windows build. This repository uses Git LFS for large assets.

1. Clone the repository and fetch its LFS assets:

   ```bash
   git lfs install
   git lfs pull
   ```

2. Open the project folder from Unity Hub and wait for asset import to finish.
3. Open `Assets/Level/Scenes/Desconecta_Stage3.unity` and press **Play**.

`Desconecta_Stage3` is the only scene enabled in Build Settings and the entry point for this project version. `Nivel1_Recovered.unity` preserves the recovered house scene; earlier scenes are not the current entry point.

## Gameplay

Resolve seven household situations involving a kitchen faucet, bathroom faucet, living-room light, television, charger, lamp, and stove. Each correct answer awards **100 points**; an incorrect answer costs **25 points**, but the score cannot fall below zero. Players can retry, and each situation awards points only once. The ten-minute timer pauses on decision and pause screens; an optional unlimited-time setting is available.

Twenty-four optional 3D coins are distributed around the house. Walk into one to collect it for **10 points**. Coins spin while idle and play a collection animation; collecting all of them is not required to complete the mission. The final screen reports the collected count, percentage, coin bonus, and total score. With all seven situations solved without mistakes and all coins collected, the maximum score is **940**.

The stove and charger situations teach players to ask an adult for help. Coins are restored when a new run begins.

### Controls

| Action | Controls |
|---|---|
| Move | Arrow keys or WASD |
| Look | Mouse; Q/E to turn and R/F to look up or down |
| Inspect the focused target | Left mouse button, Enter, or Space |
| Choose an answer | 1 or 2, or the on-screen buttons |
| Pause / resume exploration | Esc |
| Request a hint | H |
| Navigate menus | Tab or arrow keys; Enter activates; arrow keys adjust sliders |

## Accessibility and Audio

The **A tu ritmo** menu provides large text, reduced motion, unlimited time, mute, separate music and effects volume, and mouse sensitivity settings. These preferences persist between sessions. Situations are identified with numbers and text, and sound effects have on-screen descriptions. Input supports Unity's Input System and the legacy input manager; keep **Active Input Handling** set to **Both** in Project Settings.

Known limitations: there is no screen reader support, voice-over narration, controller support, or complete key remapping. WCAG 2.2 and Universal Design for Learning are used as references; the project does not claim full compliance certification. See the [accessibility report](Documentation/Reporte-Accesibilidad-Desconecta.pdf).

## Project Layout

| Location | Purpose |
|---|---|
| `Assets/Code/Scripts` | Gameplay, session state, input, and interface |
| `Assets/Code/Editor` | Scene builders, verification tools, and optional local MCP connection |
| `Assets/Level/Scenes` | Playable Stage 3 and recovered source house |
| `Assets/Art/StageThree` | Stage materials and generated illustration |
| `Assets/Art/Coins` | Procedural coin mesh, materials, and animation clips |
| `Assets/Audio/Generated` | Original synthesized ambience and sound effects |
| `Assets/Audio/Camilo` | User-supplied background music and coin sound for this update |
| `Assets/Tests/Editor` | NUnit tests for mission and coin progress |
| `Documentation` | Reports, AI-use record, validation results, and screenshots |
| `Tools/create_audio.py` | Reproducible synthesis of the generated WAV files |
| `Packages/com.coplaydev.unity-mcp` | Unity MCP package used by the optional editor workflow |

## Tests and Builds

In Unity, open **Window > General > Test Runner** and run the EditMode tests in the `Desconecta.Tests` assembly. The checked-in test report records **11 passing tests**: six for mission progress and five for coin collection. Results are in [`Documentation/CoinTests.xml`](Documentation/CoinTests.xml).

The coin update also includes an editor integration check. Use **Desconecta > Coins > Run Runtime Verification** while in Play Mode; the check exercises collection, reset, pause, victory, and timeout behavior and returns the session to the home screen. Recorded results include **28 passing runtime checks** in [`Documentation/CoinValidation.json`](Documentation/CoinValidation.json). These are recorded project results, not a claim that the tests were run in the current Unity session.

To build for Windows x64, open **File > Build Profiles**, select Windows, and use the enabled scene. The documented setup uses the Mono backend and a 1600 × 1000 window. A running MCP server is not required to play. The packaged build is distributed separately from the source repository.

**Desconecta > Build Stage Three** regenerates the playable scene from `Nivel1_Recovered.unity` and replaces later manual changes to `Desconecta_Stage3`. Commit or otherwise preserve scene edits before running the builder.

## Optional Local MCP Workflow

The project includes the editor package for [MCP for Unity by CoplayDev](https://github.com/CoplayDev/unity-mcp). A local HTTP connection was verified at `http://127.0.0.1:8080/mcp` using server revision `81542a7e86292b5c4c21b1782c5af23b567f45ce`. To use MCP in another session, start the server following its documentation and select **Desconecta > Connect Local MCP**. The project does not start a server automatically and does not require a cloud account.

## Assets and AI Use

The house structure and furniture come from the existing project. The menu illustration was generated with OpenAI's image-generation tool integrated into Codex; the original prompt and post-processing details are recorded in [`Documentation/AI-Usage.json`](Documentation/AI-Usage.json). The exact image-model version was not exposed. The generated audio in `Assets/Audio/Generated` was synthesized with Python and uses no downloaded songs or recorded samples.

The coin update also uses two audio files supplied by the user. Preserve the appropriate attribution and confirm licensing before public distribution; the folder name alone does not establish authorship or usage rights. The coin model is procedural and does not use its visual reference as a texture. See the [coin update report](Documentation/Actualizacion-Monedas.md) and [gameplay screenshot](Documentation/Screenshots/Coin-Playing.png).

Codex assisted with recovery, implementation, automation, tests, and documentation. Team member names, individual contributions, roles, and delivery ownership should be confirmed by the team; this README does not infer them from commit metadata.
