﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.ExceptionService;
using Snap.Hutao.Factory.ContentDialog;
using Snap.Hutao.Service.Game;
using Snap.Hutao.Service.Game.Configuration;
using Snap.Hutao.Service.Game.Scheme;
using Snap.Hutao.Service.Navigation;
using Snap.Hutao.Service.Notification;
using Snap.Hutao.View.Dialog;
using Snap.Hutao.View.Page;
using System.IO;

namespace Snap.Hutao.ViewModel.Game;

[Injection(InjectAs.Transient)]
[ConstructorGenerated]
internal sealed partial class LaunchGameShared
{
    private readonly IContentDialogFactory contentDialogFactory;
    private readonly INavigationService navigationService;
    private readonly IGameServiceFacade gameService;
    private readonly IInfoBarService infoBarService;
    private readonly LaunchOptions launchOptions;
    private readonly ITaskContext taskContext;

    public LaunchScheme? GetCurrentLaunchSchemeFromConfigFile(IGameServiceFacade gameService, IInfoBarService infoBarService)
    {
        ChannelOptions options = gameService.GetChannelOptions();

        switch (options.ErrorKind)
        {
            case ChannelOptionsErrorKind.None:
                try
                {
                    return KnownLaunchSchemes.Get().Single(scheme => scheme.Equals(options));
                }
                catch (InvalidOperationException)
                {
                    if (!IgnoredInvalidChannelOptions.Contains(options))
                    {
                        // 后台收集
                        throw ThrowHelper.NotSupported($"不支持的 MultiChannel: {options}");
                    }
                }

                break;
            case ChannelOptionsErrorKind.ConfigurationFileNotFound:
                infoBarService.Warning($"{options.ErrorKind}", SH.FormatViewModelLaunchGameMultiChannelReadFail(options.FilePath), SH.ViewModelLaunchGameFixConfigurationFileButtonText, HandleConfigurationFileNotFoundCommand);
                break;
            case ChannelOptionsErrorKind.GamePathNullOrEmpty:
                infoBarService.Warning($"{options.ErrorKind}", SH.FormatViewModelLaunchGameMultiChannelReadFail(options.FilePath), SH.ViewModelLaunchGameSetGamePathButtonText, HandleGamePathNullOrEmptyCommand);
                break;
        }

        return default;
    }

    [Command("HandleConfigurationFileNotFoundCommand")]
    private async void HandleConfigurationFileNotFoundAsync()
    {
        launchOptions.TryGetGameFileSystem(out GameFileSystem? gameFileSystem);
        ArgumentNullException.ThrowIfNull(gameFileSystem);
        bool isOversea = LaunchScheme.ExecutableIsOversea(gameFileSystem.GameFileName);
        string version = await File.ReadAllTextAsync(Path.Combine(gameFileSystem.GameDirectory, isOversea ? GameConstants.GenshinImpactData : GameConstants.YuanShenData, "Persistent", "ScriptVersion")).ConfigureAwait(false);

        LaunchGameConfigurationFixDialog dialog = await contentDialogFactory.CreateInstanceAsync<LaunchGameConfigurationFixDialog>().ConfigureAwait(false);

        await taskContext.SwitchToMainThreadAsync();
        dialog.KnownSchemes = KnownLaunchSchemes.Get().Where(scheme => scheme.IsOversea == isOversea);
        dialog.SelectedScheme = dialog.KnownSchemes.First(scheme => scheme.IsNotCompatOnly);
        (bool isOk, LaunchScheme? launchScheme) = await dialog.GetLaunchSchemeAsync().ConfigureAwait(false);

        if (isOk)
        {
            ArgumentNullException.ThrowIfNull(launchScheme);

            string content = $"""
                [General]
                channel={(int)launchScheme.Channel}
                cps=mihoyo
                game_version={version}
                sub_channel={(int)launchScheme.SubChannel}
                sdk_version=
                game_biz=hk4e_{(launchScheme.IsOversea ? "global" : "cn")}
                """;

            await File.WriteAllTextAsync(gameFileSystem.GameConfigFilePath, content).ConfigureAwait(false);
            infoBarService.Success(SH.ViewModelLaunchGameFixConfigurationFileSuccess);
        }
    }

    [Command("HandleGamePathNullOrEmptyCommand")]
    private void HandleGamePathNullOrEmpty()
    {
        navigationService.Navigate<LaunchGamePage>(INavigationAwaiter.Default);
    }
}