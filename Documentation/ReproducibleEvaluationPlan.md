# Reproducible evaluation harness plan

This branch is for adding reproducible evidence collection for the Unity simulation. Do not write results into the paper until they are produced by running the actual Unity project.

## Required environment
- Unity Editor 6000.4.11f1 (see ProjectSettings/ProjectVersion.txt)
- Open Assets/Scenes/SampleScene.unity
- Install dependencies from Packages/manifest.json
- Run in the Unity Editor or a standalone player on a named hardware/software configuration.

## Experiments to record
1. Per target class, at least 100 seeded trials: outcome, time-to-termination, intercept time for successful intercepts, minimum separation/miss distance, termination reason.
2. Guidance comparison: existing hybrid lead-pursuit/PN baseline versus pure-pursuit and lead-pursuit alternatives. Use paired initial conditions and identical seeds. Record controller version and prefab settings.
3. Navigation constant sensitivity: N = 3, 5, 7, paired seeds and initial conditions.
4. Saturation: 3, 4, 5, and 6 simultaneous threats. Record launches, reload-constrained queue delay, kills, misses, and threats left alive at scenario timeout.
5. Frame-time profiling: capture median, 95th percentile, and 99th percentile frame time plus FPS on the same machine and quality settings.
6. One representative engagement: save time-series CSV with timestamp and positions for target/interceptor; plot target and interceptor trajectories from the CSV.

## Raw log schema
One CSV row per trial:
experiment, seed, target_type, guidance, navigation_constant, threat_count, trial_index, start_time_utc, duration_s, outcome, termination_reason, intercept_time_s, miss_distance_m, frame_time_median_ms, frame_time_p95_ms, frame_time_p99_ms, fps_mean, initial_target_position, initial_target_velocity, initial_interceptor_position, build_commit

Write a second per-frame trajectory CSV with:
experiment, seed, time_s, target_x, target_y, target_z, interceptor_x, interceptor_y, interceptor_z, separation_m, target_alive, interceptor_alive

## Data-integrity rules
- Set and log the random seed before spawning targets. Current TargetSpawner uses UnityEngine.Random for spawn angle and altitude; verify whether all target motion uses UnityEngine.Random or any other nondeterministic source.
- A trial is a kill only when the existing target-destruction/proximity-fuze event occurs. Do not label timeout or unrelated collision as a kill.
- Use the existing scene, prefab parameters, and guidance code unless an experiment explicitly names the changed controller. Keep raw rows, including failures/timeouts.
- Include machine, operating system, graphics API, display resolution, Unity version, quality level, VSync, target frame-rate setting, fixed timestep, commit SHA, and all prefab parameter values in a run manifest.
- Do not infer FPS from Unity Editor impressions. Use a Profiler capture or a lightweight per-frame timing logger in a built player.
- Compare controllers on paired seeds; report denominators and confidence intervals with kill rates.
- Make claims only after the raw CSVs and manifest are checked into an artifact or otherwise archived.

## Existing implementation facts to validate against runs
- Fire control has a nominal 3 s reload interval and will only engage a new target when no interceptor is already assigned to it.
- Interceptor launch uses a vertical initial direction and a boost phase before homing.
- Current guidance uses lead pursuit outside terminal range and PN inside terminal range.
- Bird filtering is RCS-only below 0.0008 m^2; very-low-RCS non-bird contacts can therefore be classified as clutter.
