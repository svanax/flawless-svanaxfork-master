#nullable enable

using HarmonyLib;
using ProtoBuf;
using System;
using System.IO;
using Vintagestory.Client;
using Vintagestory.Client.NoObf;

namespace flawlesssvanaxfork;

[HarmonyPatch(typeof(SystemNetworkProcess), "TryReadPacket")]
public class PatchClientNetwork
{
    public static bool Prefix(SystemNetworkProcess __instance, byte[] data, int dataLength)
    {
        var packet = new Packet_Server();
        Packet_ServerSerializer.DeserializeBuffer(data, dataLength, packet);
        if (packet.Id != 55 || packet.CustomPacket == null)
            return true;

        var game = __instance.GetField<ClientMain>("game");

        if (packet.CustomPacket.ChannelId == FlawlessModSystem.TRICKY_CANNON)
        {
            if (packet.CustomPacket.MessageId == FlawlessModSystem.MSG_MOD_LIST)
            {
                game.Logger.Notification("[flawlesssvanaxfork] Processing server custom mods list...");

                ClientModState.ClientServerModsNeeded = System.Array.Empty<ServerModDownloadInfo>();

                if (packet.CustomPacket.Data?.Length > 0)
                {
                    try
                    {
                        using var ms = new MemoryStream(packet.CustomPacket.Data);
                        var response = Serializer.Deserialize<ServerModInfoResponse>(ms);
                        ClientModState.ClientServerModsNeeded = response.mods;

                        game.Logger.Notification($"[flawlesssvanaxfork] Server requires {response.mods.Length} mods:");
                        foreach (var mod in response.mods)
                        {
                            string version = string.IsNullOrEmpty(mod.Version) ? "unknown" : mod.Version;
                            game.Logger.Notification($"[flawlesssvanaxfork] - {mod.Modid} (v{version})");
                        }
                    }
                    catch (Exception ex)
                    {
                        game.Logger.Error($"[flawlesssvanaxfork] Failed to parse server mod info response: {ex.Message}");
                        ClientModState.ClientServerModsNeeded = System.Array.Empty<ServerModDownloadInfo>();
                    }
                }

                ClientModState.ClientServerModsRecieved = true;
            }
            else if (packet.CustomPacket.MessageId == FlawlessModSystem.MSG_MOD_DATA)
            {
                game.Logger.Notification("[flawlesssvanaxfork] Downloading mod...");
                try
                {
                    using var ms = new MemoryStream(packet.CustomPacket.Data);
                    var modDataPacket = Serializer.Deserialize<ModDataPacket>(ms);
                    ClientModDownloader.ClientHandleModDownloadPacket(game, modDataPacket);
                }
                catch (Exception ex)
                {
                    game.Logger.Error($"[flawlesssvanaxfork] Failed to parse server mod download data: {ex.Message}");
                }

                ClientModState.ModsDownloaded += 1;
                ClientModState.WaitingForDownload = false;
            }
            else
            {
                game.Logger.Error($"[flawlesssvanaxfork] Unknown server message ID: {packet.CustomPacket.MessageId}");
            }

            return false; // skip further processing
        }

        return true;
    }
}