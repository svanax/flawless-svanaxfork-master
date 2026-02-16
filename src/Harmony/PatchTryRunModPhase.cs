#nullable enable

using System.Linq;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.Common;

namespace flawlesssvanaxfork;

[HarmonyPatchCategory("flawlesssvanaxfork-client")]
[HarmonyPatch(typeof(ModLoader), "TryRunModPhase")]
public class PatchTryRunModPhase
{
    private static readonly string[] Phase1Mods = new[]
    {
        "combatoverhaul",
        "overhaullib",
    };
    
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result, Mod mod, ModSystem system, ICoreAPI api, ModRunPhase phase)
    {
        if (phase == ModRunPhase.Pre)
        {
            __result = true;
            
            string modId = mod.Info.ModID.ToLower();
            string systemName = system.GetType().Name;
            bool phase2Active = ClientModState.ClientPreloadPhase2;
            
            // Flawless runs in Phase 1, skips Phase 2
            if (system is FlawlessModSystem)
            {
                bool shouldRun = !phase2Active;
                api.Logger.Notification($"[PatchTryRunModPhase] Flawless/{systemName}: Phase2={phase2Active}, returning {shouldRun}");
                return shouldRun;
            }
            
            // Combat Overhaul and dependencies run in Phase 1, skip Phase 2
            bool isPhase1Mod = Phase1Mods.Contains(modId);
            if (isPhase1Mod)
            {
                bool shouldRun = !phase2Active;
                api.Logger.Notification($"[PatchTryRunModPhase] {modId}/{systemName}: Phase2={phase2Active}, returning {shouldRun} (Phase 1 mod)");
                return shouldRun;
            }
            
            // All other mods skip Phase 1, run in Phase 2
            bool shouldRunPhase2 = phase2Active;
            api.Logger.Debug($"[PatchTryRunModPhase] {modId}/{systemName}: Phase2={phase2Active}, returning {shouldRunPhase2} (Phase 2 mod)");
            return shouldRunPhase2;
        }
        return true;
    }
}