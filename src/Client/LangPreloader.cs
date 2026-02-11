#nullable enable

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client;
using Vintagestory.Client.NoObf;

namespace flawlesssvanaxfork;

public static class LangPreloader
{
    public static void PreLoad(TranslationService lang, string assetsPath)
    {
        var entryCache = lang.GetField<Dictionary<string, string>>("entryCache");
        var regexCache = lang.GetField<Dictionary<string, KeyValuePair<Regex, string>>>("regexCache");
        var wildcardCache = lang.GetField<Dictionary<string, string>>("wildcardCache");

        foreach (var file in new DirectoryInfo(Path.Combine(assetsPath, "lang")).EnumerateFiles(lang.LanguageCode + ".json", SearchOption.AllDirectories))
        {
            try
            {
                string json = File.ReadAllText(file.FullName);
                var jsonEntries = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (jsonEntries == null) continue;
                lang.CallMethod<TranslationService>("LoadEntries", entryCache, regexCache, wildcardCache, jsonEntries, "flawlesssvanaxfork");
            }
            catch (Exception ex)
            {
                lang.GetField<ILogger>("logger").Error("Failed to load language file: " + file.Name);
                lang.GetField<ILogger>("logger").Error(ex);
            }
        }

        lang.SetField("entryCache", entryCache);
        lang.SetField("regexCache", regexCache);
        lang.SetField("wildcardCache", wildcardCache);
    }

    public static void FlawlessPreLoad(ClientMain game)
    {
        string? flawlessAssetPath = null;

        var modinfoJsonFiles = Directory.EnumerateFiles(GamePaths.Cache, "modinfo.json", SearchOption.AllDirectories)
            .Where(file => file.EndsWith("modinfo.json", StringComparison.OrdinalIgnoreCase));

        foreach (string file in modinfoJsonFiles)
        {
            var modInfo = JsonConvert.DeserializeObject<ModinfoJson>(File.ReadAllText(file));
            if (modInfo?.Modid == "flawlesssvanaxfork")
            {
                flawlessAssetPath = Path.Combine(Path.GetDirectoryName(file)!, "assets", "flawlesssvanaxfork");
                break;
            }
        }

        if (flawlessAssetPath == null)
        {
            string localModsPath = Path.Combine(GamePaths.DataPathMods, "flawlesssvanaxfork_mod_manager", "assets", "flawlesssvanaxfork");
            if (Directory.Exists(localModsPath))
                flawlessAssetPath = localModsPath;
            else
            {
                foreach (var origin in ScreenManager.Platform.AssetManager.CustomAppOrigins)
                {
                    if (Directory.Exists(origin.OriginPath))
                    {
                        flawlessAssetPath = Directory.GetParent(origin.OriginPath)!.ToString();
                        break;
                    }
                }
            }
        }

        if (flawlessAssetPath == null)
        {
            game.Api.Logger.Error("[flawlesssvanaxfork] Failed to find flawless mod assets path, cannot load lang files.");
            return;
        }

        if (Lang.AvailableLanguages.TryGetValue(Lang.CurrentLocale, out var langWrapper) && langWrapper is TranslationService lang)
        {
            PreLoad(lang, flawlessAssetPath);
        }
    }
}