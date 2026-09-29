using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace BloomMod;

[HarmonyPatch]
public static class BloomPatch
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(FollowerCamera), nameof(FollowerCamera.SetTarget));
        yield return AccessTools.Method(typeof(FollowerCamera), nameof(FollowerCamera.SnapToTarget));
        yield return AccessTools.Method(typeof(SpawnInMinigame), nameof(SpawnInMinigame.SpawnAt));
    }

    public static void Postfix()
    {
        BloomModPlugin.SetBloom(true);
    }
}