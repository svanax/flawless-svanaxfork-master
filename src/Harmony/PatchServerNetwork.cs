#nullable enable

using HarmonyLib;
using System.IO;
using System.Linq;
using Vintagestory.Server;

namespace flawlesssvanaxfork;

[HarmonyPatchCategory("flawlesssvanaxfork-server")]
[HarmonyPatch(typeof(Vintagestory.Server.NetworkAPI), "HandleCustomPacket")]
public class PatchServerNetwork
{
    public static bool Prefix(Vintagestory.Server.NetworkAPI __instance, Packet_Client packet, ConnectedClient client)
    {
        var p = packet.CustomPacket;
        if (p.ChannelId != FlawlessModSystem.TRICKY_CANNON)
            return true;

        var server = __instance.GetField<ServerMain>("server");
        if (server == null) return true;

        ServerMain.Logger.Notification("[flawlesssvanaxfork] Received custom packet with channelId FlawlessModSystem.TRICKY_CANNON, processing...");

        var flawless = server.Api.ModLoader.GetModSystem<FlawlessModSystem>();

        if (p.MessageId == FlawlessModSystem.MSG_REQUEST_SERVER_MODS)
        {
            ServerMain.Logger.Notification($"[flawlesssvanaxfork] Processing client {client.PlayerName} mod info request, sending ServerModInfoResponse");

            var modDownloadInfos = flawless.ServerModsNeededByClient
                .Select((mod, index) => new ServerModDownloadInfo
                {
                    Index = index,
                    Modid = mod.Modid,
                    Filename = mod.Filename,
                    Version = mod.Version,
                    Description = mod.Description
                })
                .ToArray();

            var modInfoResponse = new ServerModInfoResponse { mods = modDownloadInfos };

            var response = new Packet_CustomPacket
            {
                ChannelId = FlawlessModSystem.TRICKY_CANNON,
                MessageId = FlawlessModSystem.MSG_MOD_LIST
            };
            response.SetData(ByteSerializer.ToBytes(modInfoResponse));

            server.SendPacket(client.Player.ClientId, new Packet_Server
            {
                Id = 55,
                CustomPacket = response
            });
        }
        else // mod download request
        {
            int modIndex = p.MessageId;
            if (modIndex < 0 || modIndex >= flawless.ServerModsNeededByClient.Count)
            {
                ServerMain.Logger.Warning($"[flawlesssvanaxfork] Invalid mod index {modIndex} requested by client {client.PlayerName}");
                return false;
            }

            var modinfo = flawless.ServerModsNeededByClient[modIndex];
            if (!File.Exists(modinfo.ZipFilepath))
            {
                ServerMain.Logger.Warning($"[flawlesssvanaxfork] Mod file {modinfo.ZipFilepath} not found for modid {modinfo.Modid}, cannot send to client {client.PlayerName}");
                return false;
            }

            byte[] data = File.ReadAllBytes(modinfo.ZipFilepath);
            var modDataPacket = new ModDataPacket
            {
                FileName = modinfo.Filename,
                ModIndex = modIndex,
                Data = data
            };

            ServerMain.Logger.Notification($"[flawlesssvanaxfork] Sending mod {modinfo.Modid} ({modinfo.Filename}) to client {client.PlayerName}");

            var customPacket = new Packet_CustomPacket
            {
                ChannelId = FlawlessModSystem.TRICKY_CANNON,
                MessageId = FlawlessModSystem.MSG_MOD_DATA
            };
            customPacket.SetData(ByteSerializer.ToBytes(modDataPacket));

            server.SendPacket(client.Player.ClientId, new Packet_Server
            {
                Id = 55,
                CustomPacket = customPacket
            });
        }

        return false; // skip further processing
    }
}