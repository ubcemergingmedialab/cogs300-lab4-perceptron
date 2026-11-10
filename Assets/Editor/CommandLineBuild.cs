// Command line build script for Unity 2017.4 to 2022 (projects without Build Profiles).
// Builds the scenes ticked in File > Build Settings for one target and exits with
// code 0 on success or 1 on failure, so Jenkins can tell whether the build worked.
//
// Written in C# 4 (no string interpolation or ?.) so it compiles on Unity 2017.x
// with the legacy .NET 3.5 scripting runtime.
//
// CLI usage example (one line):
//   Unity -batchmode -quit -projectPath . -buildTarget Linux64 -buildType Release
//         -executeMethod CommandLineBuild.Build -logFile build.log
//
// Arguments:
//   -buildTarget Linux64|Win64|WebGL   required; also read by Unity itself to pick the platform
//   -buildType Release|Development     optional, default Release
//   -devBuild                          optional, same as -buildType Development
//   -outputPath path                   optional, default Builds/{target}/{buildType}/
//   -noMonoFallback                    optional, fail instead of building with Mono when IL2CPP is unavailable
//   -failOnBuildErrors                 optional, Unity 2017.x only: fail on any error Unity reports,
//                                      even when the player was written
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build.Reporting;
#endif

public static class CommandLineBuild
{
  // The only folder (relative to the project root) inside which the script may empty a directory.
  private const string WipeRootName = "Builds";

  /// <summary>
  /// Builds the enabled scenes for -buildTarget (Linux64, Win64 or WebGL) into Builds/{target}/{buildType}/.
  /// Optional arguments: -buildType Release|Development, -devBuild, -outputPath path,
  /// -noMonoFallback, -failOnBuildErrors.
  /// </summary>
  public static void Build()
  {
    try
    {
      Environment.ExitCode = BuildInternal() ? 0 : 1;
    }
    catch (Exception e)
    {
      Debug.LogError("[CLI Build] FAILED with exception: " + e);
      Environment.ExitCode = 1;
    }
    EditorApplication.Exit(Environment.ExitCode);
  }

  private static bool BuildInternal()
  {
    string targetArgument = GetCommandLineArgument("-buildTarget");
    if (string.IsNullOrEmpty(targetArgument))
    {
      Debug.LogError("[CLI Build] Error: '-buildTarget' argument is missing (use Linux64, Win64 or WebGL).");
      return false;
    }

    // targetName is the canonical spelling (Linux64, Win64, WebGL), whatever was typed
    BuildTarget target;
    BuildTargetGroup group;
    string targetName;
    if (!TryGetTarget(targetArgument, out target, out group, out targetName))
    {
      Debug.LogError("[CLI Build] Error: unsupported -buildTarget '" + targetArgument + "' (use Linux64, Win64 or WebGL).");
      return false;
    }

    bool devFlag = HasCommandLineFlag("-devBuild");
    string buildTypeArgument = GetCommandLineArgument("-buildType");
    bool development;
    if (string.IsNullOrEmpty(buildTypeArgument))
    {
      development = devFlag;
    }
    else if (buildTypeArgument.Equals("Development", StringComparison.OrdinalIgnoreCase))
    {
      development = true;
    }
    else if (buildTypeArgument.Equals("Release", StringComparison.OrdinalIgnoreCase))
    {
      if (devFlag)
      {
        Debug.LogError("[CLI Build] Error: '-devBuild' and '-buildType Release' contradict each other. Pass only one of them.");
        return false;
      }
      development = false;
    }
    else
    {
      Debug.LogError("[CLI Build] Error: unsupported -buildType '" + buildTypeArgument + "' (use Release or Development).");
      return false;
    }
    string buildType = development ? "Development" : "Release";

    // 1. Scenes: every scene ticked in File > Build Settings
    List<string> scenes = new List<string>();
    foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
    {
      if (scene.enabled && !string.IsNullOrEmpty(scene.path))
      {
        scenes.Add(scene.path);
      }
    }
    if (scenes.Count == 0)
    {
      Debug.LogError("[CLI Build] Error: no scenes are enabled in File > Build Settings. Add your scenes there and commit ProjectSettings/EditorBuildSettings.asset.");
      return false;
    }
    Debug.Log("[CLI Build] Scenes: " + string.Join(", ", scenes.ToArray()));

    // 2. Platform: Unity switches to -buildTarget at startup. Switching from here is not
    //    supported in batch mode (it needs a script recompile that cannot happen while this
    //    method runs), so a mismatch is an error.
    if (EditorUserBuildSettings.activeBuildTarget != target)
    {
      Debug.LogError("[CLI Build] Error: the active build target is " + EditorUserBuildSettings.activeBuildTarget + ", not " + target
        + ". Unity did not switch to it at startup: check that the " + targetName + " platform module is installed for this Unity version, and pass '-buildTarget "
        + targetName + "' (this Unity version may not understand '" + targetArgument + "').");
      return false;
    }

    // 3. Output path: Builds/{target}/{buildType}/
    string outputPath = GetCommandLineArgument("-outputPath");
    if (string.IsNullOrEmpty(outputPath))
    {
      string baseDir = Path.Combine(Path.Combine("Builds", targetName), buildType);
      outputPath = GetDefaultBinaryPath(baseDir, target, GetSafeFileName(Application.productName));
    }
    // Start from an empty folder, so an old player can't be mistaken for this build's output
    PrepareOutputFolder(target, outputPath);
    Debug.Log("[CLI Build] Target: " + target + ", type: " + buildType + ", output path: '" + outputPath + "'");

    BuildPlayerOptions options = new BuildPlayerOptions();
    options.scenes = scenes.ToArray();
    options.target = target;
    options.locationPathName = outputPath;
    options.options = BuildOptions.None;
    if (development)
    {
      Debug.Log("[CLI Build] Enabling Development Build flag.");
      options.options |= BuildOptions.Development;
    }
#if UNITY_2018_1_OR_NEWER
    options.targetGroup = group;
#endif

    // 4. Use Mono where IL2CPP cannot build on this machine, then put the setting back
    ScriptingImplementation originalBackend = PlayerSettings.GetScriptingBackend(group);
    bool backendChanged = false;
    if (originalBackend == ScriptingImplementation.IL2CPP)
    {
      string problem = GetIl2CppProblem(target);
      if (problem != null)
      {
        if (HasCommandLineFlag("-noMonoFallback"))
        {
          Debug.LogError("[CLI Build] FAILED: the project uses IL2CPP but " + problem + ", and -noMonoFallback was passed.");
          return false;
        }
        Debug.LogWarning("[CLI Build] The project uses IL2CPP but " + problem + ". Building with MONO instead (pass -noMonoFallback to fail here).");
        PlayerSettings.SetScriptingBackend(group, ScriptingImplementation.Mono2x);
        backendChanged = true;
      }
    }
    // Shown on the SUCCESS line, so a Mono fallback is visible without reading the whole log
    string summary = target + " " + buildType + (backendChanged ? " (MONO FALLBACK, project is set to IL2CPP)" : "");

    try
    {
      // 5. Build and check the result
#if UNITY_2018_1_OR_NEWER
      BuildReport report = BuildPipeline.BuildPlayer(options);
      if (report.summary.result == BuildResult.Succeeded)
      {
        Debug.Log("[CLI Build] SUCCESS: " + summary + " build completed. Output: " + report.summary.outputPath);
        return true;
      }
      Debug.LogError("[CLI Build] FAILED: " + summary + " build result " + report.summary.result + " with " + report.summary.totalErrors + " error(s).");
      return false;
#else
      // Unity 2017.x returns a string instead of a BuildReport: empty on success, otherwise
      // an error message. It also returns "N errors" when a build finished but logged errors
      // along the way (for example "RenderTexture.Create failed" under -nographics), so a
      // build that wrote a complete player (binary plus data files) is still treated as a
      // success, unless -failOnBuildErrors was passed.
      string error = BuildPipeline.BuildPlayer(options);
      if (string.IsNullOrEmpty(error))
      {
        Debug.Log("[CLI Build] SUCCESS: " + summary + " build completed. Output: " + outputPath);
        return true;
      }
      if (HasCommandLineFlag("-failOnBuildErrors"))
      {
        Debug.LogError("[CLI Build] FAILED: " + summary + " build: " + error + " (-failOnBuildErrors was passed).");
        return false;
      }
      if (PlayerLooksComplete(target, outputPath))
      {
        Debug.LogWarning("[CLI Build] Unity reported '" + error + "' but the player and its data files were written. Check the errors above (pass -failOnBuildErrors to fail here).");
        Debug.Log("[CLI Build] SUCCESS: " + summary + " build completed. Output: " + outputPath);
        return true;
      }
      Debug.LogError("[CLI Build] FAILED: " + summary + " build: " + error);
      return false;
#endif
    }
    finally
    {
      if (backendChanged)
      {
        // Save, so ProjectSettings.asset on disk does not stay on Mono if the build wrote it there
        PlayerSettings.SetScriptingBackend(group, originalBackend);
        AssetDatabase.SaveAssets();
      }
    }
  }

  // Empties the folder the player is built into, but only when that folder is strictly inside
  // {project}/Builds/ and no symlink or junction is involved. Anywhere else (the drive root, the
  // project folder, a home directory, a path reached through "..", a folder outside the project)
  // nothing is deleted recursively: only the old player file is removed, so the Unity 2017.x
  // "was the player written?" check still cannot be fooled by a previous build.
  private static void PrepareOutputFolder(BuildTarget target, string outputPath)
  {
    string directory = target == BuildTarget.WebGL ? outputPath : Path.GetDirectoryName(outputPath);
    string fullDirectory = string.IsNullOrEmpty(directory) ? null : NormalizePath(directory);

    string reason = GetWipeRefusalReason(fullDirectory);
    if (reason == null)
    {
      if (Directory.Exists(fullDirectory))
      {
        Debug.Log("[CLI Build] Emptying output folder '" + fullDirectory + "'.");
        Directory.Delete(fullDirectory, true);
      }
    }
    else
    {
      Debug.LogWarning("[CLI Build] Not emptying the output folder: " + reason + ". Only the old player file is removed; other files in that folder are left alone.");
      string player = target == BuildTarget.WebGL ? Path.Combine(outputPath, "index.html") : outputPath;
      if (File.Exists(player))
      {
        File.Delete(player);
      }
    }

    if (fullDirectory != null)
    {
      Directory.CreateDirectory(fullDirectory);
    }
  }

  // Returns null when fullDirectory may be deleted recursively, otherwise the reason it may not.
  private static string GetWipeRefusalReason(string fullDirectory)
  {
    if (fullDirectory == null)
    {
      return "the output path has no folder part, so it is the working directory";
    }
    if (PathsEqual(fullDirectory, Path.GetPathRoot(fullDirectory)))
    {
      return "'" + fullDirectory + "' is a filesystem root";
    }

    string wipeRoot = Path.Combine(GetProjectRoot(), WipeRootName);
    if (!IsStrictlyInside(wipeRoot, fullDirectory))
    {
      return "'" + fullDirectory + "' is not a subfolder of '" + wipeRoot + "'";
    }

    // GetFullPath does not resolve links, so a link on the way down could point anywhere
    string current = fullDirectory;
    while (!string.IsNullOrEmpty(current))
    {
      if (Directory.Exists(current) && IsLink(current))
      {
        return "'" + current + "' is a symlink or junction";
      }
      if (PathsEqual(current, wipeRoot))
      {
        break;
      }
      current = Path.GetDirectoryName(current);
    }

    // Some Mono versions follow directory links during a recursive delete
    if (Directory.Exists(fullDirectory))
    {
      string link = FindLinkedDirectory(fullDirectory);
      if (link != null)
      {
        return "it contains the symlink or junction '" + link + "'";
      }
    }
    return null;
  }

  private static string FindLinkedDirectory(string directory)
  {
    foreach (string sub in Directory.GetDirectories(directory))
    {
      if (IsLink(sub))
      {
        return sub;
      }
      string nested = FindLinkedDirectory(sub);
      if (nested != null)
      {
        return nested;
      }
    }
    return null;
  }

  private static bool IsLink(string path)
  {
    return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
  }

  // Application.dataPath is {project}/Assets in the editor
  private static string GetProjectRoot()
  {
    return NormalizePath(Path.GetDirectoryName(Application.dataPath));
  }

  // Absolute path with "." and ".." resolved and no trailing separator (except on a root)
  private static string NormalizePath(string path)
  {
    string full = Path.GetFullPath(path);
    string root = Path.GetPathRoot(full);
    if (full.Length > root.Length)
    {
      full = full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
    return full;
  }

  private static StringComparison PathComparison
  {
    get { return Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal; }
  }

  private static bool PathsEqual(string a, string b)
  {
    return string.Equals(a, b, PathComparison);
  }

  private static bool IsStrictlyInside(string parent, string child)
  {
    string prefix = parent + Path.DirectorySeparatorChar;
    return child.Length > prefix.Length && child.StartsWith(prefix, PathComparison);
  }

  private static bool TryGetTarget(string name, out BuildTarget target, out BuildTargetGroup group, out string canonicalName)
  {
    switch (name.ToLowerInvariant())
    {
      case "win64":
      case "standalonewindows64":
        target = BuildTarget.StandaloneWindows64;
        group = BuildTargetGroup.Standalone;
        canonicalName = "Win64";
        return true;
      case "linux64":
      case "standalonelinux64":
        target = BuildTarget.StandaloneLinux64;
        group = BuildTargetGroup.Standalone;
        canonicalName = "Linux64";
        return true;
      case "webgl":
        target = BuildTarget.WebGL;
        group = BuildTargetGroup.WebGL;
        canonicalName = "WebGL";
        return true;
      default:
        target = BuildTarget.NoTarget;
        group = BuildTargetGroup.Unknown;
        canonicalName = null;
        return false;
    }
  }

  // Returns null when IL2CPP can build this target here, otherwise the reason it cannot
  // (worded to follow "the project uses IL2CPP but ..."). WebGL is always IL2CPP.
  private static string GetIl2CppProblem(BuildTarget target)
  {
    switch (target)
    {
      case BuildTarget.StandaloneWindows64:
        if (Application.platform != RuntimePlatform.WindowsEditor)
        {
          return "IL2CPP Windows players can only be built on a Windows machine";
        }
        break;
      case BuildTarget.StandaloneLinux64:
#if !UNITY_2019_3_OR_NEWER
        return "IL2CPP Linux players need Unity 2019.3 or newer";
#else
        if (Application.platform != RuntimePlatform.LinuxEditor)
        {
#if UNITY_2021_2_OR_NEWER
          // Cross-compiling from Windows or macOS needs one of the com.unity.toolchain.* packages
          if (!HasLinuxToolchainPackage())
          {
            return "building IL2CPP Linux players on this machine needs a com.unity.toolchain package in Packages/manifest.json";
          }
#else
          return "before Unity 2021.2, IL2CPP Linux players can only be built on a Linux machine";
#endif
        }
        break;
#endif
      default:
        return null;
    }

    if (Il2CppSupportLooksMissing(target))
    {
      return "the IL2CPP build support for " + target + " is not installed for this Unity version";
    }
    return null;
  }

  private static bool HasLinuxToolchainPackage()
  {
    string manifest = Path.Combine(Path.Combine(GetProjectRoot(), "Packages"), "manifest.json");
    return File.Exists(manifest) && File.ReadAllText(manifest).IndexOf("com.unity.toolchain.", StringComparison.Ordinal) >= 0;
  }

  // True only when the platform's player files are installed and none of them is an IL2CPP
  // variation. When anything about the lookup is unclear this returns false, so the build
  // goes ahead with IL2CPP and Unity reports the real problem itself.
  private static bool Il2CppSupportLooksMissing(BuildTarget target)
  {
    try
    {
      // Called through reflection so the script compiles on every Unity version
      MethodInfo method = typeof(BuildPipeline).GetMethod("GetPlaybackEngineDirectory",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null,
        new Type[] { typeof(BuildTarget), typeof(BuildOptions) }, null);
      if (method == null)
      {
        return false;
      }
      string engineDirectory = method.Invoke(null, new object[] { target, BuildOptions.None }) as string;
      if (string.IsNullOrEmpty(engineDirectory))
      {
        return false;
      }
      string variations = Path.Combine(engineDirectory, "Variations");
      if (!Directory.Exists(variations))
      {
        return false;
      }
      foreach (string variation in Directory.GetDirectories(variations))
      {
        if (Path.GetFileName(variation).ToLowerInvariant().Contains("il2cpp"))
        {
          return false;
        }
      }
      return true;
    }
    catch (Exception)
    {
      return false;
    }
  }

  // Used on Unity 2017.x only: the player binary is not empty and its data files are there.
  private static bool PlayerLooksComplete(BuildTarget target, string outputPath)
  {
    if (target == BuildTarget.WebGL)
    {
      return File.Exists(Path.Combine(outputPath, "index.html")) && HasFiles(Path.Combine(outputPath, "Build"));
    }
    FileInfo player = new FileInfo(outputPath);
    if (!player.Exists || player.Length == 0)
    {
      return false;
    }
    string directory = Path.GetDirectoryName(outputPath);
    if (directory == null)
    {
      directory = "";
    }
    return HasFiles(Path.Combine(directory, Path.GetFileNameWithoutExtension(outputPath) + "_Data"));
  }

  private static bool HasFiles(string directory)
  {
    return Directory.Exists(directory) && Directory.GetFiles(directory).Length > 0;
  }

  private static string GetDefaultBinaryPath(string baseDir, BuildTarget target, string productName)
  {
    switch (target)
    {
      case BuildTarget.StandaloneWindows64:
        return Path.Combine(baseDir, productName + ".exe");
      case BuildTarget.StandaloneLinux64:
        return Path.Combine(baseDir, productName + ".x86_64");
      case BuildTarget.WebGL:
        return baseDir; // WebGL outputs a folder containing index.html and Build files
      default:
        return Path.Combine(baseDir, productName);
    }
  }

  // Product names can hold characters that are not allowed in file names on Windows or Linux
  // (for example "Game: Part 2" or "A/B"). Those become '_' so the path stays where it should.
  private static string GetSafeFileName(string name)
  {
    const string invalid = "\\/:*?\"<>|";
    StringBuilder builder = new StringBuilder();
    foreach (char c in name ?? "")
    {
      builder.Append(c < 32 || invalid.IndexOf(c) >= 0 ? '_' : c);
    }
    string result = builder.ToString().Trim(' ', '.');
    return result.Length == 0 ? "Game" : result;
  }

  // Returns the value after the flag, or null when the flag was not passed. A flag that is
  // there without a value (end of the command line, or followed by another -flag) is an error.
  private static string GetCommandLineArgument(string flag)
  {
    string[] args = Environment.GetCommandLineArgs();
    for (int i = 0; i < args.Length; i++)
    {
      if (args[i].Equals(flag, StringComparison.OrdinalIgnoreCase))
      {
        if (i + 1 >= args.Length || args[i + 1].StartsWith("-"))
        {
          throw new ArgumentException("'" + flag + "' was passed without a value.");
        }
        return args[i + 1];
      }
    }
    return null;
  }

  private static bool HasCommandLineFlag(string flag)
  {
    string[] args = Environment.GetCommandLineArgs();
    foreach (string arg in args)
    {
      if (arg.Equals(flag, StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }
    }
    return false;
  }
}
