#nullable enable

using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Config;
using Vintagestory.Client;
using Vintagestory.Client.NoObf;

namespace flawlesssvanaxfork;

public static class ClientModDownloader
{
    public static List<ServerModDownloadInfo> ModsNeeded(string installPath, ServerModDownloadInfo[] serverModsNeeded)
    {
        var modsNeeded = new List<ServerModDownloadInfo>();
        foreach (var mod in serverModsNeeded)
        {
            string defaultModsFilePath = Path.Combine(GamePaths.DataPathMods, mod.Filename);
            string serverModsFilePath = Path.Combine(installPath, mod.Filename);
            if (!File.Exists(defaultModsFilePath) && !File.Exists(serverModsFilePath))
            {
                modsNeeded.Add(mod);
                continue;
            }
            // TODO: if path exists, need to either open folder and unzip
            // and check if version in modinfo.json matches server
        }
        return modsNeeded;
    }

    public static void ClientHandleModDownloadPacket(ClientMain game, ModDataPacket p)
    {
        if (p.Data.Length == 0) return;

        if (!Directory.Exists(ClientModState.ModsInstallPath))
            Directory.CreateDirectory(ClientModState.ModsInstallPath);

        string filePath = Path.Combine(ClientModState.ModsInstallPath, p.FileName);
        if (!File.Exists(filePath))
        {
            game.Logger.Notification($"[flawlesssvanaxfork] Downloaded {filePath}");
            File.WriteAllBytes(filePath, p.Data);
        }
    }
}