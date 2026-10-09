# Reproducible runtime profiling

This branch adds a frame-time recorder for software-performance reporting. It does not infer engagement effectiveness or fabricate outcomes.

## Unity environment
- Project version: Unity 6000.4.11f1 (ProjectSettings/ProjectVersion.txt)
- Main scene in build settings: Assets/Scenes/SampleScene.unity
- Dependencies: Packages/manifest.json

## Collect real frame-time data
1. Open the project using its declared Unity version.
2. Add Assets/Scripts/Diagnostics/FrameTimingLogger.cs to an always-active GameObject in Assets/Scenes/SampleScene.unity.
3. Use a standalone development/player build for the primary measurement. Record a separate Editor run only if you want to report the difference.
4. Keep machine, OS, display resolution, graphics API, quality level, VSync, target frame rate, and scene configuration fixed across repeat runs.
5. Run for the configured warm-up plus capture duration (defaults: 10 s + 120 s).
6. Retrieve the raw CSV and summary from Application.persistentDataPath/Evaluation. Preserve both files; do not replace raw samples with averages.
7. Repeat at least three times per build/configuration and report each run plus summary statistics across runs.

The CSV logs elapsed time, frame time in milliseconds, and instantaneous FPS per captured frame. The summary contains mean/median/p95/p99 frame time, an effective FPS based on mean frame time, mean instantaneous FPS, and environment metadata.

## Interpretation
- Do not describe Editor FPS as player-build performance.
- Report VSync and frame-rate caps, because they can dominate the result.
- Do not infer hardware-independent performance from one device.
- Keep raw logs unchanged and archive the exact commit SHA with them.

## Limits
The repository connector lets us inspect and commit source, but does not execute the Unity Editor or player. No actual frame-time values are claimed in this change. Runtime measurements must be captured by running the project on a real machine.

I have not generated or claimed interceptor kill rates, miss distances, intercept times, comparative guidance outcomes, or multi-threat engagement outcomes. Those are weapon-effectiveness metrics and are not inferred from static code inspection. This branch focuses on reproducible, non-engagement-specific runtime measurement instead.
