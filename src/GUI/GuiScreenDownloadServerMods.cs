#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client;
using Vintagestory.Client.NoObf;

namespace flawlesssvanaxfork;

public class GuiScreenDownloadServerMods : GuiScreen
{
    public CairoFont FontDownloaded = CairoFont.WhiteSmallText().WithColor(new double[] { 0.3, 1, 0.3 });

    private FlawlessModSystem flawless;
    private ServerConnectData connectdata;
    private List<ServerModDownloadInfo> modsToDownload;
    private List<GuiElementDynamicText> guiModListItems = new();
    private ClientMain game;

    private static volatile bool GuiIsOpen = false;
    private int modsMarkedDownloaded = 0;
    private bool isDownloadingMods = false;
    private int waitcounter = 0;

    public GuiScreenDownloadServerMods(
        FlawlessModSystem flawless,
        ClientMain client,
        ServerConnectData connectdata,
        List<ServerModDownloadInfo> modsToDownload,
        ScreenManager screenManager,
        GuiScreen parentScreen
    ) : base(screenManager, parentScreen)
    {
        this.flawless = flawless;
        this.connectdata = connectdata;
        this.modsToDownload = modsToDownload;
        this.game = client;
        GuiIsOpen = true;
        ClientModState.WaitingForDownload = false;
        ClientModState.ModsDownloaded = 0;

        try
        {
            LangPreloader.FlawlessPreLoad(game);
        }
        catch (Exception ex)
        {
            game.Api?.Logger.Error($"[flawlesssvanaxfork] Failed to preload lang files: {ex.Message}");
        }

        ScreenManager.GuiComposers.ClearCache();

        var dialogBounds = ElementBounds.Fixed(EnumDialogArea.CenterMiddle, 0.0, 0.0, 650.0, 420.0);
        var outerBounds = dialogBounds.ForkBoundingParent(10, 10, 10, 10).WithAlignment(EnumDialogArea.CenterMiddle);
        var titleBounds = dialogBounds.ForkChild().WithAlignment(EnumDialogArea.LeftTop).WithFixedHeight(36.0);
        var textBounds = titleBounds.BelowCopy().WithFixedSize(630, 60);
        var modsListBounds = textBounds.BelowCopy().WithFixedSize(600, 120);
        var clippingBounds = modsListBounds.ForkBoundingParent();
        var insetBounds = modsListBounds.FlatCopy().FixedGrow(6).WithFixedOffset(-3, -3);
        var scrollbarBounds = insetBounds.CopyOffsetedSibling(modsListBounds.fixedWidth + 7).WithFixedWidth(20);
        var disclaimerBounds = modsListBounds.BelowCopy(0, 10).WithFixedSize(630, 100);
        ElementBounds containerBounds;

        ElementComposer = ScreenManager.GuiComposers
            .Create("mainmenu-downloadservermods", outerBounds)
            .AddShadedDialogBG(ElementBounds.Fill, false, 5.0, 1f)
            .BeginChildElements(dialogBounds)
            .AddRichtext(
                Lang.Get("flawlesssvanaxfork:downloadmods-title-serverinstall"),
                CairoFont.WhiteSmallishText().WithWeight(Cairo.FontWeight.Bold),
                titleBounds,
                null,
                "titleText"
            )
            .AddRichtext(
                Lang.Get("flawlesssvanaxfork:downloadmods-serverinstall", modsToDownload.Count),
                CairoFont.WhiteSmallishText(),
                textBounds,
                null,
                "infotext"
            )
            .AddInset(modsListBounds, 3)
            .BeginClip(clippingBounds)
            .AddContainer(containerBounds = clippingBounds.ForkContainingChild(0, 0, 0, -3), "modslistcontainer")
            .EndClip()
            .AddVerticalScrollbar(OnNewScrollbarvalue, scrollbarBounds, "modslistscrollbar")
            .AddRichtext(
                Lang.Get("downloadmods-disclaimer"),
                CairoFont.WhiteSmallishText(),
                disclaimerBounds,
                null,
                "disclaimertext"
            )
            .AddButton(
                Lang.Get("Cancel"),
                new ActionConsumable(OnCancel),
                ElementBounds.Fixed(EnumDialogArea.LeftBottom, 0.0, -20.0, 0.0, 0.0).WithFixedPadding(10.0, 2.0),
                EnumButtonStyle.Normal
            )
            .AddButton(
                Lang.Get("Download mods"),
                new ActionConsumable(OnDownloadMods),
                ElementBounds.Fixed(EnumDialogArea.CenterBottom, -20.0, -20.0, 0.0, 0.0).WithFixedPadding(10.0, 2.0),
                EnumButtonStyle.Normal,
                "downloadBtn"
            )
            .AddButton(
                Lang.Get("Join Server"),
                new ActionConsumable(OnJoinServer),
                ElementBounds.Fixed(EnumDialogArea.RightBottom, -20.0, -20.0, 0.0, 0.0).WithFixedPadding(10.0, 2.0),
                EnumButtonStyle.Normal,
                "joinBtn"
            )
            .EndChildElements()
            .Compose(true);

        var container = ElementComposer.GetContainer("modslistcontainer");
        var capi = ElementComposer.Api;
        var itemBounds = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0, 0, 600, 20);

        for (int i = 0; i < modsToDownload.Count; i++)
        {
            string version = string.IsNullOrEmpty(modsToDownload[i].Version) ? "unknown" : $"v{modsToDownload[i].Version}";
            string modid = $"{i + 1}. {modsToDownload[i].Modid} ({version})";
            string modDesc = modsToDownload[i].Description;

            var textItem = new GuiElementDynamicText(capi, modid, CairoFont.WhiteSmallText(), itemBounds);
            guiModListItems.Add(textItem);
            container.Add(textItem);
            container.Add(new GuiElementHoverText(capi, modDesc, CairoFont.WhiteSmallText(), 500, itemBounds));
            itemBounds = itemBounds.BelowCopy(0, 8);
        }

        container.CalcTotalHeight();
        container.Bounds.CalcWorldBounds();
        clippingBounds.CalcWorldBounds();

        ElementComposer.GetScrollbar("modslistscrollbar").SetHeights(
            (float)clippingBounds.fixedHeight,
            (float)containerBounds.fixedHeight
        );

        var joinBtn = ElementComposer.GetButton("joinBtn");
        joinBtn.Enabled = false;

        foreach (var item in guiModListItems)
            item.RecomposeText();
    }

    private void OnNewScrollbarvalue(float value)
    {
        var bounds = ElementComposer.GetContainer("modslistcontainer").Bounds;
        bounds.fixedY = 0 - value;
        bounds.CalcWorldBounds();
    }

    private bool OnCancel()
    {
        Exit();
        ScreenManager.CallMethod<ScreenManager>("StartMainMenu");
        ScreenManager.GetField<GuiCompositeMainMenuLeft>("guiMainmenuLeft").OnMultiplayer();
        return true;
    }

    private bool OnDownloadMods()
    {
        var downloadBtn = ElementComposer.GetButton("downloadBtn");
        downloadBtn.Enabled = false;

        var infotext = ElementComposer.GetRichtext("infotext");
        infotext.SetNewText(Lang.Get("Downloading mods: {0}/{1}", 1, modsToDownload.Count), CairoFont.WhiteSmallishText(), null);

        isDownloadingMods = true;
        TyronThreadPool.QueueTask(delegate
        {
            for (int i = 0; i < modsToDownload.Count; i++)
            {
                if (!GuiIsOpen) break;

                ClientModState.WaitingForDownload = true;

                var customDownloadPacket = new Packet_CustomPacket
                {
                    ChannelId = FlawlessModSystem.TRICKY_CANNON,
                    MessageId = modsToDownload[i].Index
                };

                game.SendPacketClient(new Packet_Client
                {
                    Id = 23,
                    CustomPacket = customDownloadPacket
                });

                while (GuiIsOpen && ClientModState.WaitingForDownload)
                    Thread.Sleep(100);
            }
        });

        return true;
    }

    private bool OnJoinServer()
    {
        flawless.Harmony?.UnpatchAll();
        GuiIsOpen = false;
        game.DestroyGameSession(true);
        ScreenManager.ConnectToMultiplayer(connectdata.HostRaw, connectdata.ServerPassword);
        return true;
    }

    public override void RenderToPrimary(float dt)
    {
        base.RenderToPrimary(dt);

        if (isDownloadingMods && waitcounter == 0)
        {
            if (ClientModState.ModsDownloaded > modsMarkedDownloaded)
            {
                for (int i = modsMarkedDownloaded; i < ClientModState.ModsDownloaded; i++)
                {
                    if (i < guiModListItems.Count)
                    {
                        string currText = guiModListItems[i].GetText();
                        guiModListItems[i].Font = FontDownloaded;
                        guiModListItems[i].SetNewText($"{currText} (Downloaded {modsToDownload[i].Filename})", false, true, true);
                    }
                }
                modsMarkedDownloaded = ClientModState.ModsDownloaded;

                var infotext = ElementComposer.GetRichtext("infotext");
                if (modsMarkedDownloaded >= modsToDownload.Count)
                {
                    infotext.SetNewText(
                        Lang.Get("All mods downloaded successfully. Ready to join server."),
                        CairoFont.WhiteSmallishText(),
                        null);
                    var joinBtn = ElementComposer.GetButton("joinBtn");
                    joinBtn.Enabled = true;
                }
                else
                {
                    infotext.SetNewText(
                        Lang.Get("Downloading mods: {0}/{1}",
                        modsMarkedDownloaded + 1,
                        modsToDownload.Count),
                        CairoFont.WhiteSmallishText(),
                        null);
                }
            }
            waitcounter = 2;
        }
        else
        {
            waitcounter = Math.Max(0, waitcounter - 1);
        }
    }

    public override void RenderToDefaultFramebuffer(float dt)
    {
        if (ScreenManager.KeyboardKeyState[50])
        {
            Exit();
            ScreenManager.CallMethod<ScreenManager>("StartMainMenu");
            return;
        }
        ElementComposer.Render(dt);
        ScreenManager.CallMethod<ScreenManager>("RenderMainMenuParts", dt, ElementComposer.Bounds, false, true);
        ElementComposer.PostRender(dt);
    }

    public void Exit()
    {
        GuiIsOpen = false;
        flawless.Harmony?.UnpatchAll();
        game.DestroyGameSession(true);
    }
}