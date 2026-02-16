#nullable enable

using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.Client;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace flawlesssvanaxfork;

[HarmonyPatchCategory("flawlesssvanaxfork-client")]
[HarmonyPatch(typeof(SystemModHandler), "StartMods")]
public class PatchSystemModHandler
{
    public static bool Prefix(SystemModHandler __instance)
    {
        var modLoader = __instance.GetField<ModLoader>("loader");
        var game = __instance.GetField<ClientMain>("game");

        game.Logger.Notification("[flawlesssvanaxfork] Injecting server mods into client!");

        var serverModFilenamesNeeded = ClientModState.ClientServerModsNeeded
            .Select(m => m.Filename)
            .ToHashSet();

        var allMods = modLoader.LoadModInfos();
        var addedServerMods = allMods
            .Where(m => serverModFilenamesNeeded.Contains(m.FileName))
            .ToList();

        foreach (var mod in addedServerMods)
            game.Logger.Notification($"[flawlesssvanaxfork] Adding server mod: {mod.FileName}");

        var prevEnabledSystems = modLoader.GetField<List<ModSystem>>("enabledSystems");
        var prevContentAssetOrigins = modLoader.GetField<OrderedDictionary<string, IAssetOrigin>>("contentAssetOrigins");
        var prevThemeAssetOrigins = modLoader.GetField<OrderedDictionary<string, IAssetOrigin>>("themeAssetOrigins");

        addedServerMods.CallMethod("CheckDuplicateModIDMods", addedServerMods);
        foreach (var mod in addedServerMods)
            if (mod.Enabled)
                mod.Unpack(modLoader.UnpackPath);

        modLoader.CallMethod("ClearCacheFolder", addedServerMods);
        var addedEnabledSystems = modLoader.CallMethod<List<ModSystem>>("instantiateMods", addedServerMods);
        var addedContentAssetOrigins = modLoader.GetField<OrderedDictionary<string, IAssetOrigin>>("contentAssetOrigins");
        var addedThemeAssetOrigins = modLoader.GetField<OrderedDictionary<string, IAssetOrigin>>("themeAssetOrigins");

        foreach (var kv in addedContentAssetOrigins)
            prevContentAssetOrigins.Add(kv.Key, kv.Value);
        modLoader.SetField("contentAssetOrigins", prevContentAssetOrigins);

        foreach (var kv in addedThemeAssetOrigins)
            prevThemeAssetOrigins.Add(kv.Key, kv.Value);
        modLoader.SetField("themeAssetOrigins", prevThemeAssetOrigins);

        prevEnabledSystems.AddRange(addedEnabledSystems);
        prevEnabledSystems.Sort((a, b) => a.ExecuteOrder().CompareTo(b.ExecuteOrder()));
        modLoader.SetField("enabledSystems", prevEnabledSystems);

        Vintagestory.ClientNative.CrashReporter.LoadedMods.AddRange(addedServerMods);
        game.textureSize = modLoader.TextureSize;


        ClientModState.ClientPreloadPhase2 = true;

        game.Logger.Notification("[flawlesssvanaxfork] Server mods injected. Phase 2 active. Game will now call StartPre on server mods.");
        
        return true;
    }
}