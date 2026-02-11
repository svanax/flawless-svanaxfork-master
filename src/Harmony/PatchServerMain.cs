#nullable enable

using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.Server;

namespace flawlesssvanaxfork;

[HarmonyPatchCategory("flawlesssvanaxfork-server")]
[HarmonyPatch(typeof(ServerMain), "CreatePacketIdentification")]
public class PatchServerMain
{
    public static void Postfix(ServerMain __instance, ref Packet_Server __result)
    {
        var flawless = __instance.Api.ModLoader.GetModSystem<FlawlessModSystem>();
        if (flawless == null) return;

        var mods = __instance.Api.ModLoader.Mods;
        var list = (from mod in mods
            where mod.Info.Side.IsUniversal()
            select new Packet_ModId
            {
                Modid = mod.Info.ModID,
                Name = mod.Info.Name,
                Networkversion = mod.Info.NetworkVersion,
                Version = mod.Info.Version,
                RequiredOnClient = mod.Info.RequiredOnClient
            }).ToList();

        list.RemoveAll(m => flawless.ServerModNotPublic.Contains(m.Modid));
        __result.Identification.SetMods(list.ToArray());
    }
}