using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace BloomMod;

[BepInAutoPlugin]
[BepInProcess("Among Us.exe")]
public partial class BloomModPlugin : BasePlugin
{
    public Harmony Harmony { get; } = new(Id);
    
    public static ManualLogSource Logger { get; private set; }
    
    public static Shader BloomShader { get; private set; }

    private static string BloomBundleName =>
        OperatingSystem.IsAndroid() ? "BloomMod.Resources.bloom-assets-android"
        : OperatingSystem.IsWindows() ? "BloomMod.Resources.bloom-assets-win" : throw new NotImplementedException();

    public BloomModPlugin()
    {
        Logger = Log;
    }

    public static void SetBloom(bool enabled)
    {
        if (!HudManager.InstanceExists)
        {
            Logger.LogInfo("No HudManager, skipping bloom!");
            return;
        }

        var bloom = HudManager.Instance.PlayerCam.GetComponent<Bloom>();
        if (!bloom)
        {
            bloom = HudManager.Instance.PlayerCam.gameObject.AddComponent<Bloom>();
            Logger.LogInfo("No Bloom, adding bloom!");
        }

        bloom.enabled = enabled;
        bloom.SetBloomByMap();
        Logger.LogInfo($"Set Bloom {enabled}!");
    }

    public override void Load()
    {
        // Load bloom shader
        unsafe {
            using var bloomStream = typeof(BloomModPlugin).Assembly.GetManifestResourceStream(BloomBundleName);
            if (bloomStream is null)
            {
                throw new Exception("Could not find bloom assets bundle in assembly resources!");
            }

            var length = (int) bloomStream.Length;
            var bytes = new Il2CppStructArray<byte>(length);
            var span = new Span<byte>(IntPtr.Add(bytes.Pointer, IntPtr.Size * 4).ToPointer(), bytes.Length);
            if (bloomStream.Read(span) < length) throw new IOException("Failed to read entire bundle");

            var assetBundle = AssetBundle.LoadFromMemory(bytes);
            if (!assetBundle)
            {
                throw new Exception("Could not load bloom assets bundle from bytes!");
            }

            var asset = assetBundle.LoadAsset("bloom.shader");
            if (!asset)
            {
                throw new Exception("Failed to load shader from bundle.");
            }

            BloomShader = asset.TryCast<Shader>();

            if (!BloomShader)
            {
                throw new Exception("Could not load bloom shader from bundle.");
            }
            UnityEngine.Object.DontDestroyOnLoad(BloomShader);
            BloomShader.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
        }

        // Register Bloom class
        ClassInjector.RegisterTypeInIl2Cpp<Bloom>();

        // Apply Harmony patches
        Harmony.PatchAll(typeof(BloomModPlugin).Assembly);
    }
}