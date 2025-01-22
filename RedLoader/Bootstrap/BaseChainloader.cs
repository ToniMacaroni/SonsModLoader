﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using HarmonyLib;
using Mono.Cecil;
using RedLoader;
using RedLoader.Bootstrap;
using RedLoader.Utils;

namespace RedLoader.Bootstrap;

public abstract class BaseChainloader
{
    protected static readonly string CurrentAssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
    protected static readonly Version CurrentAssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;

    private static Regex allowedGuidRegex { get; } = new(@"^[a-zA-Z0-9\._\-]+$");

    // protected static bool PluginTargetsWrongBepin(PluginInfo pluginInfo)
    // {
    //     var pluginTarget = pluginInfo.TargettedBepInExVersion;
    //     // X.X.X.x - compare normally. x.x.x.X - nightly build number, ignore
    //     if (pluginTarget.Major != CurrentAssemblyVersion.Major) return true;
    //     if (pluginTarget.Minor > CurrentAssemblyVersion.Minor) return true;
    //     if (pluginTarget.Minor < CurrentAssemblyVersion.Minor) return false;
    //     return pluginTarget.Build > CurrentAssemblyVersion.Build;
    // }

    #region Contract

    protected virtual string ConsoleTitle => $"RedLoader {LoaderEnvironment.RedloaderVersionString} - {LoaderEnvironment.GameExecutableName}";

    private bool _initialized;

    /// <summary>
    ///     Occurs after all plugins are loaded.
    /// </summary>
    public event Action Finished;

    public virtual void Initialize(string gameExePath = null)
    {
        if (_initialized)
            throw new InvalidOperationException("Chainloader cannot be initialized multiple times");

        // Set vitals
        if (gameExePath != null)
            // Checking for null allows a more advanced initialization workflow, where the Paths class has been initialized before calling Chainloader.Initialize
            // This is used by Preloader to use environment variables, for example
            LoaderEnvironment.SetExecutablePath(gameExePath);

        InitializeLoggers();

        _initialized = true;

        // Logger.Log(LogLevel.Message, "Chainloader initialized");
        RLog.Msg("Chainloader initialized");
    }

    protected virtual void InitializeLoggers()
    {
        // TODO: Init loggers
        if (ConsoleManager.ConsoleEnabled && !ConsoleManager.ConsoleActive)
            ConsoleManager.CreateConsole();

        if (ConsoleManager.ConsoleActive)
        {
            // if (!Logger.Listeners.Any(x => x is ConsoleLogListener))
            //     Logger.Listeners.Add(new ConsoleLogListener());

            ConsoleManager.SetConsoleTitle(ConsoleTitle);
        }
    }

    /// <summary>
    /// Discovers all plugins in the plugin directory without loading them.
    /// </summary>
    /// <remarks>
    /// This is useful for discovering BepInEx plugin metadata.
    /// </remarks>
    /// <param name="path">Path from which to search the plugins.</param>
    /// <param name="cacheName">Cache name to use. If null, results are not cached.</param>
    /// <returns>List of discovered plugins and their metadata.</returns>
    // protected IList<PluginInfo> DiscoverPluginsFrom(string path, string cacheName = "chainloader")
    // {
    //     var pluginsToLoad =
    //         TypeLoader.FindPluginTypes(path, ToPluginInfo, HasBepinPlugins, cacheName);
    //     return pluginsToLoad.SelectMany(p => p.Value).ToList();
    // }

    /// <summary>
    /// Discovers plugins to load.
    /// </summary>
    /// <returns>List of plugins to be loaded.</returns>
    // protected virtual IList<PluginInfo> DiscoverPlugins()
    // {
    //     return DiscoverPluginsFrom(Paths.PluginPath);
    // }

    /// <summary>
    /// Preprocess the plugins and modify the load order.
    /// </summary>
    /// <remarks>Some plugins may be skipped if they cannot be loaded (wrong metadata, etc).</remarks>
    /// <param name="plugins">Plugins to process.</param>
    /// <returns>List of plugins to load in the correct load order.</returns>
    // protected virtual IList<PluginInfo> ModifyLoadOrder(IList<PluginInfo> plugins)
    // {
    //     // We use a sorted dictionary to ensure consistent load order
    //     var dependencyDict =
    //         new SortedDictionary<string, IEnumerable<string>>(StringComparer.InvariantCultureIgnoreCase);
    //     var pluginsByGuid = new Dictionary<string, PluginInfo>();
    //
    //     foreach (var pluginInfoGroup in plugins.GroupBy(info => info.Metadata.GUID))
    //     {
    //         if (Plugins.TryGetValue(pluginInfoGroup.Key, out var loadedPlugin))
    //         {
    //             // Logger.Log(LogLevel.Warning,
    //             //            $"Skipping [{pluginInfoGroup.Key}] because a plugin with a similar GUID ([{loadedPlugin}]) has been already loaded.");
    //             RLog.Warning($"Skipping [{pluginInfoGroup.Key}] because a plugin with a similar GUID ([{loadedPlugin}]) has been already loaded.");
    //             continue;
    //         }
    //
    //         PluginInfo loadedVersion = null;
    //         foreach (var pluginInfo in pluginInfoGroup.OrderByDescending(x => x.Metadata.Version))
    //         {
    //             if (loadedVersion != null)
    //             {
    //                 // Logger.Log(LogLevel.Warning,
    //                 //            $"Skipping [{pluginInfo}] because a newer version exists ({loadedVersion})");
    //                 RLog.Warning($"Skipping [{pluginInfo}] because a newer version exists ({loadedVersion})");
    //                 continue;
    //             }
    //
    //             // Perform checks that will prevent loading plugins in this run
    //             var filters = pluginInfo.Processes.ToList();
    //             var invalidProcessName = filters.Count != 0 &&
    //                                      filters.All(x => !string.Equals(x.ProcessName.Replace(".exe", ""),
    //                                                                      Paths.ProcessName,
    //                                                                      StringComparison
    //                                                                          .InvariantCultureIgnoreCase));
    //
    //             if (invalidProcessName)
    //             {
    //                 // Logger.Log(LogLevel.Warning,
    //                 //            $"Skipping [{pluginInfo}] because of process filters ({string.Join(", ", pluginInfo.Processes.Select(p => p.ProcessName).ToArray())})");
    //                 RLog.Warning($"Skipping [{pluginInfo}] because of process filters ({string.Join(", ", pluginInfo.Processes.Select(p => p.ProcessName).ToArray())})");
    //                 continue;
    //             }
    //
    //             loadedVersion = pluginInfo;
    //             dependencyDict[pluginInfo.Metadata.GUID] = pluginInfo.Dependencies.Select(d => d.DependencyGUID);
    //             pluginsByGuid[pluginInfo.Metadata.GUID] = pluginInfo;
    //         }
    //     }
    //
    //     foreach (var pluginInfo in pluginsByGuid.Values.ToList())
    //         if (pluginInfo.Incompatibilities.Any(incompatibility =>
    //                                                  pluginsByGuid.ContainsKey(incompatibility.IncompatibilityGUID)
    //                                               || Plugins.ContainsKey(incompatibility.IncompatibilityGUID))
    //            )
    //         {
    //             pluginsByGuid.Remove(pluginInfo.Metadata.GUID);
    //             dependencyDict.Remove(pluginInfo.Metadata.GUID);
    //
    //             var incompatiblePluginsNew = pluginInfo.Incompatibilities.Select(x => x.IncompatibilityGUID)
    //                                                    .Where(x => pluginsByGuid.ContainsKey(x));
    //             var incompatiblePluginsExisting = pluginInfo.Incompatibilities.Select(x => x.IncompatibilityGUID)
    //                                                         .Where(x => Plugins.ContainsKey(x));
    //             var incompatiblePlugins = incompatiblePluginsNew.Concat(incompatiblePluginsExisting).ToArray();
    //             var message =
    //                 $@"Could not load [{pluginInfo}] because it is incompatible with: {string.Join(", ", incompatiblePlugins)}";
    //             DependencyErrors.Add(message);
    //             // Logger.Log(LogLevel.Error, message);
    //             RLog.Error(message);
    //         }
    //         else if (PluginTargetsWrongBepin(pluginInfo))
    //         {
    //             var message =
    //                 $@"Plugin [{pluginInfo}] targets a wrong version of BepInEx ({pluginInfo.TargettedBepInExVersion}) and might not work until you update";
    //             DependencyErrors.Add(message);
    //             // Logger.Log(LogLevel.Warning, message);
    //             RLog.Warning(message);
    //         }
    //
    //     // We don't add already loaded plugins to the dependency graph as they are already loaded
    //
    //     var emptyDependencies = new string[0];
    //
    //     // Sort plugins by their dependencies.
    //     // Give missing dependencies no dependencies of its own, which will cause missing plugins to be first in the resulting list.
    //     var sortedPlugins = Utility.TopologicalSort(dependencyDict.Keys,
    //                                                 x =>
    //                                                     dependencyDict.TryGetValue(x, out var deps)
    //                                                         ? deps
    //                                                         : emptyDependencies).ToList();
    //
    //     return sortedPlugins.Where(pluginsByGuid.ContainsKey).Select(x => pluginsByGuid[x]).ToList();
    // }

    protected static IModProcessor ModProcessor;

    /// <summary>
    /// Run the chainloader and load all plugins from the plugins folder.
    /// </summary>
    public virtual void Execute()
    {
        try
        {
            var plugins = ModProcessor.LoadPlugins();
            foreach (var plugin in plugins)
            {
                RegisterTypeInIl2Cpp.RegisterAssembly(plugin.ModAssembly);
            }
            // Logger.Log(LogLevel.Info, $"{plugins.Count} plugin{(plugins.Count == 1 ? "" : "s")} to load");
            RLog.Msg($"{plugins.Count} plugin{(plugins.Count == 1 ? "" : "s")} to load");
            //LoadPlugins(plugins);
            Finished?.Invoke();
        }
        catch (Exception ex)
        {
            try
            {
                ConsoleManager.CreateConsole();
            }
            catch { }

            // Logger.Log(LogLevel.Error, $"Error occurred loading plugins: {ex}");
            RLog.Error($"Error occurred loading plugins: {ex}");
        }

        // Logger.Log(LogLevel.Message, "Chainloader startup complete");
        RLog.Msg("Chainloader startup complete");
    }

    // private IList<PluginInfo> LoadPlugins(IList<PluginInfo> plugins)
    // {
    //     var sortedPlugins = ModifyLoadOrder(plugins);
    //
    //     var invalidPlugins = new HashSet<string>();
    //     var processedPlugins = new Dictionary<string, SemanticVersioning.Version>();
    //     var loadedAssemblies = new Dictionary<string, Assembly>();
    //     var loadedPlugins = new List<PluginInfo>();
    //
    //     foreach (var plugin in sortedPlugins)
    //     {
    //         var dependsOnInvalidPlugin = false;
    //         var missingDependencies = new List<BepInDependency>();
    //         foreach (var dependency in plugin.Dependencies)
    //         {
    //             static bool IsHardDependency(BepInDependency dep) =>
    //                 (dep.Flags & BepInDependency.DependencyFlags.HardDependency) != 0;
    //
    //             // If the dependency wasn't already processed, it's missing altogether
    //             var dependencyExists =
    //                 processedPlugins.TryGetValue(dependency.DependencyGUID, out var pluginVersion);
    //             // Alternatively, if the dependency hasn't been loaded before, it's missing too
    //             if (!dependencyExists)
    //             {
    //                 dependencyExists = Plugins.TryGetValue(dependency.DependencyGUID, out var pluginInfo);
    //                 pluginVersion = pluginInfo?.Metadata.Version;
    //             }
    //
    //             if (!dependencyExists || dependency.VersionRange != null &&
    //                 !dependency.VersionRange.IsSatisfied(pluginVersion))
    //             {
    //                 // If the dependency is hard, collect it into a list to show
    //                 if (IsHardDependency(dependency))
    //                     missingDependencies.Add(dependency);
    //                 continue;
    //             }
    //
    //             // If the dependency is a hard and is invalid (e.g. has missing dependencies), report that to the user
    //             if (invalidPlugins.Contains(dependency.DependencyGUID) && IsHardDependency(dependency))
    //             {
    //                 dependsOnInvalidPlugin = true;
    //                 break;
    //             }
    //         }
    //
    //         processedPlugins.Add(plugin.Metadata.GUID, plugin.Metadata.Version);
    //
    //         if (dependsOnInvalidPlugin)
    //         {
    //             var message =
    //                 $"Skipping [{plugin}] because it has a dependency that was not loaded. See previous errors for details.";
    //             DependencyErrors.Add(message);
    //             // Logger.Log(LogLevel.Warning, message);
    //             RLog.Warning(message);
    //             continue;
    //         }
    //
    //         if (missingDependencies.Count != 0)
    //         {
    //             var message = $@"Could not load [{plugin}] because it has missing dependencies: {
    //                 string.Join(", ", missingDependencies.Select(s => s.VersionRange == null ? s.DependencyGUID : $"{s.DependencyGUID} ({s.VersionRange})").ToArray())
    //             }";
    //             DependencyErrors.Add(message);
    //             // Logger.Log(LogLevel.Error, message);
    //             RLog.Error(message);
    //
    //             invalidPlugins.Add(plugin.Metadata.GUID);
    //             continue;
    //         }
    //
    //         try
    //         {
    //             // Logger.Log(LogLevel.Info, $"Loading [{plugin}]");
    //             RLog.Msg($"Loading [{plugin}]");
    //
    //             if (!loadedAssemblies.TryGetValue(plugin.Location, out var ass))
    //                 loadedAssemblies[plugin.Location] = ass = Assembly.LoadFrom(plugin.Location);
    //
    //             Plugins[plugin.Metadata.GUID] = plugin;
    //             TryRunModuleCtor(plugin, ass);
    //             plugin.Instance = LoadPlugin(plugin, ass);
    //             loadedPlugins.Add(plugin);
    //
    //             PluginLoaded?.Invoke(plugin);
    //         }
    //         catch (Exception ex)
    //         {
    //             invalidPlugins.Add(plugin.Metadata.GUID);
    //             Plugins.Remove(plugin.Metadata.GUID);
    //
    //             // Logger.Log(LogLevel.Error,
    //             //            $"Error loading [{plugin}]: {(ex is ReflectionTypeLoadException re ? TypeLoader.TypeLoadExceptionToString(re) : ex.ToString())}");
    //             RLog.Error($"Error loading [{plugin}]: {(ex is ReflectionTypeLoadException re ? TypeLoader.TypeLoadExceptionToString(re) : ex.ToString())}");
    //         }
    //     }
    //
    //     return loadedPlugins;
    // }

    /// <summary>
    /// Detects and loads all plugins in the specified directories.
    /// </summary>
    /// <remarks>
    /// It is better to collect all paths at once and use a single call to LoadPlugins than multiple calls.
    /// This allows to run proper dependency resolving and to load all plugins in one go.
    /// </remarks>
    /// <param name="pluginsPaths">Directories to search the plugins from.</param>
    /// <returns>List of loaded plugin infos.</returns>
    // public IList<PluginInfo> LoadPlugins(params string[] pluginsPaths)
    // {
    //     // TODO: This is a temporary solution for 3rd party loaders. Instead, this should be done via metaplugins.
    //     var plugins = new List<PluginInfo>();
    //     foreach (var pluginsPath in pluginsPaths)
    //         plugins.AddRange(DiscoverPluginsFrom(pluginsPath));
    //     return LoadPlugins(plugins);
    // }

    private static void TryRunModuleCtor(ModBase mod, Assembly assembly)
    {
        try
        {
            RuntimeHelpers.RunModuleConstructor(mod.GetType().Module.ModuleHandle);
        }
        catch (Exception e)
        {
            // Logger.Log(LogLevel.Warning,
            //            $"Couldn't run Module constructor for {assembly.FullName}::{plugin.TypeName}: {e}");
            RLog.Warning($"Couldn't run Module constructor for {assembly.FullName}::{mod.ID}: {e}");
        }
    }

    //public abstract TPlugin LoadPlugin(PluginInfo pluginInfo, Assembly pluginAssembly);

    #endregion

    #region Config

    // private static readonly bool ConfigDiskAppend = false;
    //
    // private static readonly bool ConfigDiskLogging = true;

    // private static readonly ConfigEntry<LogLevel> ConfigDiskLoggingDisplayedLevel = ConfigFile.CoreConfig.Bind(
    //  "Logging.Disk", "LogLevels",
    //  LogLevel.Fatal | LogLevel.Error | LogLevel.Warning | LogLevel.Message | LogLevel.Info,
    //  "Only displays the specified log levels in the disk log output.");

    // private static readonly bool ConfigDiskLoggingInstantFlushing = false;
    //
    // private static readonly int ConfigDiskLoggingFileLimit = 5;

    #endregion
}
