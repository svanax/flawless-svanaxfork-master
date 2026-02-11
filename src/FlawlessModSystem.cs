#nullable enable

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client;
using Vintagestory.Client.NoObf;
using Vintagestory.Server;

namespace flawlesssvanaxfork;

public class FlawlessModSystem : ModSystem
{
    // global
    public ICoreAPI? Api { get; set; }
    public Harmony? Harmony { get; private set; }

    // magnificent mare in the mirror's next magic trick!!
    public const int TRICKY_CANNON = int.MaxValue;

    // message ids
    public const int MSG_REQUEST_SERVER_MODS = int.MaxValue;
    public const int MSG_MOD_LIST = 0;
    public const int MSG_MOD_DATA = 1;

    public override double ExecuteOrder() => -double.MaxValue;

    // SERVER
    public List<ServerModInfo> ServerModsNeededByClient { get; private set; } = new();
    public HashSet<string> ServerModNotPublic { get; private set; } = new();

    public override void Start(ICoreAPI api)
    {
        if (api.Side != EnumAppSide.Server) return;
        if ((api.World as ServerMain)?.IsDedicatedServer != true) return;

        var collector = new ServerModCollector(this, api);
        ServerModsNeededByClient = collector.CollectServerModsNeededByClient().GetAwaiter().GetResult();
        ServerModsNeededByClient = ServerModsNeededByClient.Where(m => !m.Public).ToList();

        foreach (var mod in ServerModsNeededByClient)
            if (!mod.Public) ServerModNotPublic.Add(mod.Modid);
    }

    public override void StartPre(ICoreAPI api)
    {
        Api = api;
        api.Logger.Notification("[flawlesssvanaxfork] StartPre");

        if (api.World is ClientMain mainClient && mainClient.IsSingleplayer) return;
        if (api.World is ServerMain main && !main.IsDedicatedServer) return;

        Harmony ??= new Harmony("flawlesssvanaxfork");

        if (api.Side == EnumAppSide.Server)
        {
            Harmony.PatchCategory("flawlesssvanaxfork-server");
        }
        else if (api.Side == EnumAppSide.Client)
        {
            Harmony.PatchAllUncategorized();

            ClientModState.ClientPreloadPhase2 = false;

            var customPacket = new Packet_CustomPacket
            {
                ChannelId = TRICKY_CANNON,
                MessageId = MSG_REQUEST_SERVER_MODS,
            };

            var clientMain = api.World as ClientMain;
            if (clientMain == null)
            {
                api.Logger.Error("[flawlesssvanaxfork] ClientMain instance not found, cannot request server mods info.");
                return;
            }

            clientMain.SendPacketClient(new Packet_Client
            {
                Id = 23,
                CustomPacket = customPacket
            });

            api.Logger.Notification("[flawlesssvanaxfork] Sent mods info request to server, waiting for ServerModInfoResponse...");

            ClientModState.ClientServerModsRecieved = false;
            Task.Run(async () =>
            {
                while (ClientModState.ClientServerModsRecieved == false)
                    await Task.Delay(100);
            }).Wait();

            var connectdata = clientMain.Connectdata;
            string installPath = Path.Combine(GamePaths.DataPathServerMods,
                GamePaths.ReplaceInvalidChars(connectdata.Host + "-" + connectdata.Port.ToString()));
            ClientModState.ModsInstallPath = installPath;

            var serverModsNeeded = ClientModDownloader.ModsNeeded(installPath, ClientModState.ClientServerModsNeeded);
            if (serverModsNeeded.Count > 0)
            {
                var screenManager = ClientProgram.screenManager;
                var mainScreen = screenManager.GetField<GuiScreenMainRight>("mainScreen");

                screenManager.LoadScreen(new GuiScreenDownloadServerMods(
                    this,
                    clientMain,
                    connectdata,
                    serverModsNeeded,
                    ClientProgram.screenManager,
                    mainScreen
                ));
            }
        }

        api.Logger.Notification("[flawlesssvanaxfork] Finished StartPre");
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        Harmony?.UnpatchAll(Harmony.Id);
    }

    public override void Dispose()
    {
        if (Harmony != null)
        {
            Harmony.UnpatchAll(Harmony.Id);
            Harmony = null;
        }
    }
}