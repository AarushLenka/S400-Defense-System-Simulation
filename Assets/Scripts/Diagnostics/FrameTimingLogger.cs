using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Records frame-time measurements for the running Unity scene.
/// Independent of target, interceptor, and guidance state.
/// Attach to an always-active GameObject. Outputs are saved under
/// Application.persistentDataPath/Evaluation.
/// </summary>
public sealed class FrameTimingLogger : MonoBehaviour
{
    [Min(0f)] public float warmupSeconds = 10f;
    [Min(1f)] public float captureSeconds = 120f;
    public bool logToConsole = true;

    private readonly List<double> elapsedSeconds = new List<double>();
    private readonly List<double> frameMilliseconds = new List<double>();
    private double applicationStart;
    private double captureStart;
    private bool captureStarted;
    private bool saved;

    private void Awake()
    {
        applicationStart = Time.realtimeSinceStartupAsDouble;
        if (logToConsole)
            Debug.Log($"[FrameTimingLogger] Warm-up {warmupSeconds:F1}s; capture {captureSeconds:F1}s. Output: {Path.Combine(Application.persistentDataPath, "Evaluation")}");
    }

    private void Update()
    {
        if (saved) return;
        double now = Time.realtimeSinceStartupAsDouble;
        if (!captureStarted)
        {
            if (now - applicationStart < warmupSeconds) return;
            captureStarted = true;
            captureStart = now;
        }

        double elapsed = now - captureStart;
        elapsedSeconds.Add(elapsed);
        frameMilliseconds.Add(Math.Max(0.000001, Time.unscaledDeltaTime * 1000.0));

        if (elapsed >= captureSeconds)
            SaveResults("capture_complete");
    }

    private void OnApplicationQuit()
    {
        if (!saved) SaveResults("application_quit");
    }

    private void OnDestroy()
    {
        if (!saved) SaveResults("logger_destroyed");
    }

    private void SaveResults(string stopReason)
    {
        if (saved || frameMilliseconds.Count == 0) return;
        saved = true;

        try
        {
            string directory = Path.Combine(Application.persistentDataPath, "Evaluation");
            Directory.CreateDirectory(directory);
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
            string stem = $"frame_timing_{stamp}";
            string csvPath = Path.Combine(directory, stem + ".csv");
            string summaryPath = Path.Combine(directory, stem + "_summary.txt");

            var csv = new StringBuilder();
            csv.AppendLine("frame_index,elapsed_s,frame_time_ms,instantaneous_fps");
            for (int i = 0; i < frameMilliseconds.Count; i++)
            {
                double ms = frameMilliseconds[i];
                csv.Append(i.ToString(CultureInfo.InvariantCulture)).Append(',')
                   .Append(elapsedSeconds[i].ToString("F6", CultureInfo.InvariantCulture)).Append(',')
                   .Append(ms.ToString("F6", CultureInfo.InvariantCulture)).Append(',')
                   .Append((1000.0 / ms).ToString("F4", CultureInfo.InvariantCulture)).AppendLine();
            }
            File.WriteAllText(csvPath, csv.ToString());

            double[] sorted = frameMilliseconds.OrderBy(v => v).ToArray();
            double meanMs = sorted.Average();
            double meanInstantFps = sorted.Average(ms => 1000.0 / ms);
            double measuredDuration = elapsedSeconds.Count > 1
                ? elapsedSeconds[elapsedSeconds.Count - 1] - elapsedSeconds[0]
                : 0.0;

            var summary = new StringBuilder();
            summary.AppendLine("Unity frame-time capture summary");
            summary.AppendLine($"stop_reason={stopReason}");
            summary.AppendLine($"capture_saved_utc={DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
            summary.AppendLine($"sample_count={sorted.Length}");
            summary.AppendLine($"sampled_duration_s={measuredDuration.ToString("F4", CultureInfo.InvariantCulture)}");
            summary.AppendLine($"mean_frame_time_ms={meanMs.ToString("F4", CultureInfo.InvariantCulture)}");
            summary.AppendLine($"median_frame_time_ms={Percentile(sorted, 0.50).ToString("F4", CultureInfo.InvariantCulture)}");
            summary.AppendLine($"p95_frame_time_ms={Percentile(sorted, 0.95).ToString("F4", CultureInfo.InvariantCulture)}");
            summary.AppendLine($"p99_frame_time_ms={Percentile(sorted, 0.99).ToString("F4", CultureInfo.InvariantCulture)}");
            summary.AppendLine($"effective_fps_from_mean_frame_time={(1000.0 / meanMs).ToString("F2", CultureInfo.InvariantCulture)}");
            summary.AppendLine($"mean_instantaneous_fps={meanInstantFps.ToString("F2", CultureInfo.InvariantCulture)}");
            summary.AppendLine($"unity_version={Application.unityVersion}");
            summary.AppendLine($"application_version={Application.version}");
            summary.AppendLine($"is_editor={Application.isEditor}");
            summary.AppendLine($"scene={SceneManager.GetActiveScene().name}");
            summary.AppendLine($"platform={Application.platform}");
            summary.AppendLine($"device_model={SystemInfo.deviceModel}");
            summary.AppendLine($"device_name={SystemInfo.deviceName}");
            summary.AppendLine($"operating_system={SystemInfo.operatingSystem}");
            summary.AppendLine($"processor={SystemInfo.processorType}");
            summary.AppendLine($"processor_count={SystemInfo.processorCount}");
            summary.AppendLine($"system_memory_mb={SystemInfo.systemMemorySize}");
            summary.AppendLine($"graphics_device={SystemInfo.graphicsDeviceName}");
            summary.AppendLine($"graphics_device_version={SystemInfo.graphicsDeviceVersion}");
            summary.AppendLine($"screen_resolution={Screen.width}x{Screen.height}");
            summary.AppendLine($"quality_level={QualitySettings.names[QualitySettings.GetQualityLevel()]}");
            summary.AppendLine($"vsync_count={QualitySettings.vSyncCount}");
            summary.AppendLine($"target_frame_rate={Application.targetFrameRate}");
            summary.AppendLine($"fixed_timestep_s={Time.fixedDeltaTime.ToString("F6", CultureInfo.InvariantCulture)}");
            summary.AppendLine("Note: results depend on hardware, build/editor mode, quality settings, VSync, resolution, and background load.");
            File.WriteAllText(summaryPath, summary.ToString());

            if (logToConsole)
                Debug.Log($"[FrameTimingLogger] Saved {sorted.Length} frames. Mean={meanMs:F2}ms, median={Percentile(sorted, 0.50):F2}ms, p95={Percentile(sorted, 0.95):F2}ms, p99={Percentile(sorted, 0.99):F2}ms. CSV: {csvPath}; summary: {summaryPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FrameTimingLogger] Failed to save frame timing output: {ex}");
        }
    }

    private static double Percentile(double[] sortedAscending, double quantile)
    {
        if (sortedAscending == null || sortedAscending.Length == 0) return double.NaN;
        if (sortedAscending.Length == 1) return sortedAscending[0];

        double index = Math.Max(0.0, Math.Min(1.0, quantile)) * (sortedAscending.Length - 1);
        int lower = (int)Math.Floor(index);
        int upper = (int)Math.Ceiling(index);
        if (lower == upper) return sortedAscending[lower];

        double fraction = index - lower;
        return sortedAscending[lower] + (sortedAscending[upper] - sortedAscending[lower]) * fraction;
    }
}
