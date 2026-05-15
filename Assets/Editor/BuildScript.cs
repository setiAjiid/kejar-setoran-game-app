using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    private const string OutputDir = @"D:\Games\KejarSetoran";
    private const string ExeName = "KejarSetoran.exe";

    [MenuItem("Build/Windows 64-bit")]
    public static void BuildWindows()
    {
        Run(BuildTarget.StandaloneWindows64);
    }

    private static void Run(BuildTarget target)
    {
        if (!Directory.Exists(OutputDir))
            Directory.CreateDirectory(OutputDir);

        string exePath = Path.Combine(OutputDir, ExeName);

        var scenes = new[] { "Assets/Scenes/SampleScene.unity" };

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = exePath,
            target = target,
            options = BuildOptions.None
        };

        Debug.Log($"[BuildScript] Building {target} -> {exePath}");
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] SUCCESS: {summary.totalSize / (1024 * 1024)} MB at {exePath}");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError($"[BuildScript] FAILED: {summary.result} ({summary.totalErrors} errors)");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
