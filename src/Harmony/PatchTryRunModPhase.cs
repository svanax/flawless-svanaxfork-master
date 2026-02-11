#nullable enable

using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.Common;

namespace flawlesssvanaxfork;

[HarmonyPatch(typeof(ModLoader), "TryRunModPhase")]
public class PatchTryRunModPhase
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result, Mod mod, ModSystem system, ICoreAPI api, ModRunPhase phase)
    {
        if (phase == ModRunPhase.Pre)
        {
            __result = true;
            if (system is FlawlessModSystem)
                return ClientModState.ClientPreloadPhase2 == false;
            else
                return ClientModState.ClientPreloadPhase2;
        }
        return true;
    }
}