using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGLBatchBuilder
{
    public static void Build()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
        {
            throw new InvalidOperationException(
                "WebGL Build Support is not available for this Unity Editor installation.");
        }

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes were found in Build Settings.");
        }

        string outputPath = GetCommandLineValue("-customBuildPath");
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = Path.GetFullPath(Path.Combine("Builds", "WebGL"));
        }

        Directory.CreateDirectory(outputPath);

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        Debug.Log($"Starting verified WebGL build at: {outputPath}");
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        Debug.Log(
            $"WebGL build result: {summary.result}; " +
            $"errors: {summary.totalErrors}; warnings: {summary.totalWarnings}; " +
            $"size: {summary.totalSize} bytes; duration: {summary.totalTime}");

        if (summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"WebGL build failed with {summary.totalErrors} error(s). See the batch build log.");
        }
    }

    private static string GetCommandLineValue(string argumentName)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], argumentName, StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetFullPath(args[i + 1]);
            }
        }

        return null;
    }
}
