using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using RedLoader.IL2CPP.RuntimeFixes;
using RedLoader.Preloader.Core;
using RedLoader.Preloader.Core.Patching;
using RedLoader.Preloader.RuntimeFixes;
using RedLoader.Unity.Common;
using RedLoader.Unity.IL2CPP.UnityEngine;
using MonoMod.Utils;
using RedLoader;
using RedLoader.Unity.IL2CPP.Utils;
using RedLoader.Utils;

namespace RedLoader.Unity.IL2CPP;

public static class Preloader
{
    // private static PreloaderConsoleListener PreloaderLog { get; set; }

    // internal static ManualLogSource Log => PreloaderLogger.Log;

    // TODO: This is not needed, maybe remove? (Instance is saved in IL2CPPChainloader itself)
    private static IL2CPPChainloader Chainloader { get; set; }

    public static void Run()
    {
        try
        {
            HarmonyBackendFix.Initialize();
            ConsoleSetOutFix.Apply();
            UnityInfo.Initialize(LoaderEnvironment.GameExecutablePath, LoaderEnvironment.UnityGameDataDirectory);

            ConsoleManager.Initialize(false, true);

            // PreloaderLog = new PreloaderConsoleListener();
            // Logger.Listeners.Add(PreloaderLog);
            
            if (ConsoleManager.ConsoleEnabled)
            {
                ConsoleManager.CreateConsole();
                if(!LoaderEnvironment.IsDedicatedServer)
                    ConsoleManager.SetConsoleRect(CorePreferences.ConsoleRect.Value);
                // Logger.Listeners.Add(new ConsoleLogListener());
            }

            if (!LoaderEnvironment.IsDedicatedServer)
            {
                SplashWindow.CreateWindow();
                SplashWindow.HookLog();
                SplashWindow.TotalProgressSteps = 5;
                GlobalEvents.OnApplicationLateStart.Subscribe(() =>
                {
                    SplashWindow.UnhookLog();
                    SplashWindow.CloseWindow();
                });
            }

            RedirectStdErrFix.Apply();

            // ChainloaderLogHelper.PrintLogInfo(Log);

            // Logger.Log(LogLevel.Info, $"Running under Unity {UnityInfo.Version}");
            // Logger.Log(LogLevel.Info, $"Runtime version: {Environment.Version}");
            // Logger.Log(LogLevel.Info, $"Runtime information: {RuntimeInformation.FrameworkDescription}");
            //
            // Logger.Log(LogLevel.Debug, $"Game executable path: {Paths.ExecutablePath}");
            // Logger.Log(LogLevel.Debug, $"Interop assembly directory: {Il2CppInteropManager.IL2CPPInteropAssemblyPath}");
            // Logger.Log(LogLevel.Debug, $"BepInEx root path: {Paths.BepInExRootPath}");

            if (File.Exists(LoaderEnvironment.VortexFilePath))
            {
                RLog.Msg($"Mods are managed using Vortex Mod Manager.");
            }
            
            RLog.Msg($"Running under Unity {UnityInfo.Version}");
            RLog.Msg($"Runtime version: {Environment.Version}");
            RLog.Msg($"Runtime information: {RuntimeInformation.FrameworkDescription}");
            
            RLog.Msg($"Game executable path: {LoaderEnvironment.GameExecutablePath}");
            RLog.Msg($"Interop assembly directory: {Il2CppInteropManager.IL2CPPInteropAssemblyPath}");
            RLog.Msg($"Redloader root path: {LoaderEnvironment.LoaderDirectory}");

            if (PlatformHelper.Is(Platform.Wine) && !Environment.Is64BitProcess)
            {
                if (!NativeLibrary.TryGetExport(NativeLibrary.Load("ntdll"), "RtlRestoreContext", out var _))
                {
                    RLog.Warning("Your wine version doesn't support CoreCLR properly, expect crashes! Upgrade to wine 7.16 or higher.");
                }
            }

            NativeLibrary.SetDllImportResolver(typeof(Il2CppInterop.Runtime.IL2CPP).Assembly, DllImportResolver);

            Il2CppInteropManager.Initialize();
            SplashWindow.SetProgressSteps(1);
            
            using (var assemblyPatcher = new AssemblyPatcher((data, _) => Assembly.Load(data)))
            {
                assemblyPatcher.AddPatchersFromDirectory(LoaderEnvironment.PatcherPluginPath);

                RLog.Msg($"{assemblyPatcher.PatcherContext.PatcherPlugins.Count} patcher{(assemblyPatcher.PatcherContext.PatcherPlugins.Count == 1 ? "" : "s")} loaded");

                assemblyPatcher.LoadAssemblyDirectories(Il2CppInteropManager.IL2CPPInteropAssemblyPath);

                RLog.Msg($"{assemblyPatcher.PatcherContext.PatcherPlugins.Count} assemblies discovered");

                assemblyPatcher.PatchAndLoad();
            }
            
            SplashWindow.SetProgressSteps(2);


            // Logger.Listeners.Remove(PreloaderLog);


            Chainloader = new IL2CPPChainloader();

            Chainloader.Initialize();
        }
        catch (Exception ex)
        {
            // Log.Log(LogLevel.Fatal, ex);
            RLog.Error(ex);

            throw;
        }
    }

    private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName == "GameAssembly")
        {
            return NativeLibrary.Load(Il2CppInteropManager.GameAssemblyPath, assembly, searchPath);
        }

        return IntPtr.Zero;
    }
}
