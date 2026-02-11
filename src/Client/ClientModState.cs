#nullable enable

namespace flawlesssvanaxfork;

public static class ClientModState
{
    // flag indicating 2nd phase, after server mods identified
    internal static bool ClientPreloadPhase2 = false;

    // atomic flag for polling to wait on mod info from server
    internal static volatile bool ClientServerModsRecieved = false;

    // modids and filenames needed by client from server
    internal static volatile ServerModDownloadInfo[] ClientServerModsNeeded = System.Array.Empty<ServerModDownloadInfo>();

    // path to server specific mods folder
    internal static string ModsInstallPath = "";

    // flag while waiting for mod to download.
    internal static volatile bool WaitingForDownload = false;

    // indicate number of mods downloaded
    internal static volatile int ModsDownloaded = 0;
}