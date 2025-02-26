using System;
using System.IO;
using System.Linq;
using System.Reflection;
using RedLoader;
using MonoMod.Utils;
using Version = SemanticVersioning.Version;

namespace RedLoader.Utils;

/// <summary>
///     Loader environment information
/// </summary>
public static class LoaderEnvironment
{
    public static bool IsDedicatedServer { get; internal set; }

    /// <summary>
    /// Path of {gameroot}/{loader}/
    /// </summary>
    // public static string LoaderDirectory { get; internal set; }
    public static string LoaderDirectory { get; private set; }

    /// <summary>
    /// Path of the directory where the game executable is located
    /// </summary>
    // public static string GameRootDirectory { get; internal set; }
    public static string GameRootDirectory { get; private set; }
    
    public static string GameExecutablePath { get; private set; }
    public static string ModsDirectory { get; private set; }
    public static string BlueprintDirectory => Path.Combine(GameRootDirectory, "Mods", "Blueprints");
    public static string LibsDirectory => Path.Combine(GameRootDirectory, "Libs");
    public static string UserDataDirectory { get; private set; }
    public static string VortexFilePath => Path.Combine(GameRootDirectory, "vortex.deployment.json")

    /// <summary>
    /// Name of the executable without the extension
    /// </summary>
    public static string GameExecutableName { get; private set; }
    public static string UnityGameDataDirectory { get; private set; }
    public static string Il2CppDataDirectory => Path.Combine(UnityGameDataDirectory, "il2cpp_data");
    
    /// <summary>
    /// Directory of the core mods
    /// </summary>
    internal static string CoreModDirectory => Path.Combine(LoaderDirectory, "CoreMods");
    
    public static string PendingDirectory => Path.Combine(LoaderDirectory, "Pending");

    //public static string MelonManagedDirectory => Path.Combine(LoaderDirectory, "Managed");
    public static string Il2CppAssembliesDirectory => Path.Combine(LoaderDirectory, "Game");
    
    public static string LoaderAssemblyDirectory { get; private set; }
    public static string LoaderAssemblyPath { get; private set; }
    
    /// <summary>
    /// The folder name of the loader
    /// </summary>
    public static string LoaderFolderName => Path.GetFileName(LoaderDirectory);
    
    public static string ScriptDirectory => Path.Combine(GameRootDirectory, "Scripts");
    
    /// <summary>
    ///    Redloader version.
    /// </summary>
    public static Version RedloaderVersion { get; } =
        Version.Parse(MetadataHelper.GetAttributes<AssemblyInformationalVersionAttribute>(typeof(LoaderEnvironment).Assembly)[0]
                                    .InformationalVersion);
    
    public static string RedloaderVersionString { get; private set; }

    /// <summary>
    ///     The path to temporary cache files.
    /// </summary>
    public static string CachePath { get; private set; }

    /// <summary>
    ///     The path to the patcher plugin folder which resides in the Redloader folder.
    /// </summary>
    public static string PatcherPluginPath { get; private set; }

    /// <summary>
    ///     List of directories from where Mono will search assemblies before assembly resolving is invoked.
    /// </summary>
    public static string[] DllSearchPaths { get; private set; }

    public static void SetExecutablePath(string executablePath, string[] dllSearchPath = null)
    {
        RedloaderVersionString = $"{RedloaderVersion.Major}.{RedloaderVersion.Minor}.{RedloaderVersion.Patch}";
        
        GameExecutablePath = executablePath;
        GameExecutableName = Path.GetFileNameWithoutExtension(executablePath);
        IsDedicatedServer = GameExecutablePath.EndsWith("DS.exe");

        GameRootDirectory = PlatformHelper.Is(Platform.MacOS)
                           ? Utility.ParentDirectory(executablePath, 4)
                           : Path.GetDirectoryName(executablePath);

        UnityGameDataDirectory = Path.Combine(GameRootDirectory, $"{GameExecutableName}_Data");
        
        // if (string.IsNullOrEmpty(GameDataPath) || !Directory.Exists(GameDataPath))
        //     throw new DirectoryNotFoundException("Failed to extract valid GameDataPath from executablePath: " + executablePath);

        LoaderDirectory = Path.Combine(GameRootDirectory, "_Redloader");
        UserDataDirectory = Path.Combine(GameRootDirectory, "UserData");
        ModsDirectory = Path.Combine(GameRootDirectory, "Mods");
        PatcherPluginPath = Path.Combine(LoaderDirectory, "Patchers");
        LoaderAssemblyDirectory = Path.Combine(LoaderDirectory, "net6");
        LoaderAssemblyPath = Path.Combine(LoaderAssemblyDirectory,
                                          $"{Assembly.GetExecutingAssembly().GetName().Name}.dll");
        CachePath = Path.Combine(LoaderDirectory, "cache");
        // DllSearchPaths = (dllSearchPath ?? new string[0]).Concat(new[] { ManagedPath }).Distinct().ToArray();
        
        Directory.CreateDirectory(ModsDirectory);
        Directory.CreateDirectory(PatcherPluginPath);
        Directory.CreateDirectory(UserDataDirectory);
        Directory.CreateDirectory(BlueprintDirectory);
    }

    public static PathObject GetModDataPath(Assembly assembly)
    {
        return new PathObject(Path.Combine(Path.GetDirectoryName(assembly.Location)!, assembly.GetName().Name!));
        // return Path.Combine(ModsDirectory, assembly.GetName().Name!);
    }
        
    public static PathObject GetModDataPath(ModBase mod)
    {
        return GetModDataPath(mod.ModAssembly);
    }
        
    public static PathObject GetMetadataPath(Assembly assembly)
    {
        return GetModDataPath(assembly) / "manifest.json";
    }

    public static PathObject GetMetadataPath(string assemblyPath)
    {
        var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
        return new(Path.Combine(ModsDirectory, assemblyName, "manifest.json"));
    }
}
