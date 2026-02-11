#nullable enable

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client;
using Vintagestory.Server;

namespace flawlesssvanaxfork;

internal class ServerModCollector
{
    private readonly FlawlessModSystem modSystem;
    private readonly ICoreAPI api;

    public ServerModCollector(FlawlessModSystem modSystem, ICoreAPI api)
    {
        this.modSystem = modSystem;
        this.api = api;
    }

    public async Task<List<ServerModInfo>> CollectServerModsNeededByClient()
    {
        var modsNeeded = new List<ServerModInfo>();
        var modPaths = GetServerModfiles();

        api.Logger.Notification($"[flawlesssvanaxfork] Found {modPaths.Count} server mods zip paths:");
        foreach (var mod in modPaths.Values)
            api.Logger.Notification($"[flawlesssvanaxfork] {mod.Modid} => {mod.ZipPath}");

        var modidNeededByClient = new List<string>();
        foreach (Mod mod in api.ModLoader.Mods)
        {
            if (mod.Info.ModID == "game" ||
                mod.Info.ModID == "creative" ||
                mod.Info.ModID == "survival" ||
                mod.Info.ModID == "flawlesssvanaxfork")
                continue;
            if (mod.Info.RequiredOnClient)
                modidNeededByClient.Add(mod.Info.ModID);
        }

        using var client = new HttpClient();
        var tasks = modidNeededByClient.Select(modid => FetchIsModPublic(modid, client)).ToList();
        await Task.WhenAll(tasks);

        for (int i = 0; i < modidNeededByClient.Count; i++)
        {
            string modid = modidNeededByClient[i];
            bool isPublic = tasks[i].Result;

            if (modPaths.TryGetValue(modid, out var modfile))
            {
                modsNeeded.Add(new ServerModInfo
                {
                    Modid = modid,
                    ZipFilepath = modfile.ZipPath,
                    Filename = modfile.ZipPath.Substring(modfile.ZipPath.LastIndexOf(Path.DirectorySeparatorChar) + 1),
                    Version = modfile.Version,
                    Description = modfile.Description,
                    Public = isPublic
                });
            }
            else
            {
                modSystem.Api?.Logger.Warning($"Mod {modid} not found in server mod paths, cannot send to client");
            }
        }

        return modsNeeded;
    }

    internal Dictionary<string, ServerModfile> GetServerModfiles()
    {
        api.Logger.Notification("[flawlesssvanaxfork] Gathering server mod paths...");
        var modPaths = new Dictionary<string, ServerModfile>();

        var modInfoFiles = Directory.EnumerateFiles(GamePaths.Cache, "modinfo.json", SearchOption.AllDirectories)
            .Where(file => file.EndsWith("modinfo.json", StringComparison.OrdinalIgnoreCase));

        api.Logger.Notification($"[flawlesssvanaxfork] Processing {modInfoFiles.Count()} modinfo.json files in game cache.");

        foreach (string file in modInfoFiles)
        {
            string json = File.ReadAllText(file);
            var modInfo = JsonConvert.DeserializeObject<ModinfoJson>(json);
            if (modInfo?.Modid == null)
            {
                api.Logger.Warning($"[flawlesssvanaxfork] Modinfo.json file {file} does not contain valid modid, skipping.");
                continue;
            }

            string parentDirectory = Path.GetDirectoryName(file)!;
            string zipName = Path.GetFileName(parentDirectory);
            if (zipName.Contains(".zip"))
                zipName = zipName[..(zipName.IndexOf(".zip") + 4)];

            string zipPath1 = Path.Combine(GamePaths.DataPathMods, zipName);
            string zipPath2 = Path.Combine(GamePaths.BinariesMods, zipName);
            string zipPath;
            if (File.Exists(zipPath1))
                zipPath = zipPath1;
            else if (File.Exists(zipPath2))
                zipPath = zipPath2;
            else
            {
                api.Logger.Warning($"[flawlesssvanaxfork] Mod zip file for modid {modInfo.Modid} not found at '{zipPath1}' or '{zipPath2}', skipping.");
                continue;
            }

            modPaths[modInfo.Modid] = new ServerModfile
            {
                Modid = modInfo.Modid,
                Version = modInfo.Version,
                Description = modInfo.Description,
                ZipPath = zipPath,
            };
        }

        return modPaths;
    }

    private async Task<bool> FetchIsModPublic(string modId, HttpClient client)
    {
        if (modId == "flawlesssvanaxfork") return true;

        string uri = $"https://mods.vintagestory.at/api/mod/{modId}";
        var res = await client.PostAsync(uri, null);
        if (!res.IsSuccessStatusCode) return false;

        string responseBody = await res.Content.ReadAsStringAsync();
        try
        {
            var responseJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseBody);
            if (responseJson != null && responseJson.TryGetValue("statuscode", out var statuscode) && statuscode.ToString() == "404")
            {
                api.Logger.Notification($"[flawlesssvanaxfork] Mod '{modId}' not found in VS ModDb database (404) => private mod");
                return false;
            }
        }
        catch (JsonException)
        {
            api.Logger.Warning($"[flawlesssvanaxfork] Failed to parse JSON response for mod {modId}");
            return false;
        }
        return true;
    }
}