// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.IO.Ini;
using Snap.Hutao.Service.Game.Scheme;
using System.Collections.Immutable;
using System.IO;

namespace Snap.Hutao.Service.Game.Package.Advanced;

internal sealed class GameInstallPrerequisite
{
    private const string InstallingName = "snap_hutao_installing";
    private readonly IGameFileSystem gameFileSystem;

    private GameInstallPrerequisite(IGameFileSystem gameFileSystem)
    {
        this.gameFileSystem = gameFileSystem;
    }

    public static bool TryLock(IGameFileSystem gameFileSystem, string version, LaunchScheme launchScheme, [NotNullWhen(true)] out GameInstallPrerequisite? locker)
    {
        if (Directory.EnumerateFileSystemEntries(gameFileSystem.GetGameDirectory()).Any())
        {
            if (!File.Exists(gameFileSystem.GetGameConfigurationFilePath()))
            {
                locker = default;
                return false;
            }

            ImmutableArray<IniElement> ini = IniSerializer.DeserializeFromFile(gameFileSystem.GetGameConfigurationFilePath());
            if (!ini.Any(e => e is IniParameter { Key: InstallingName }))
            {
                locker = default;
                return false;
            }

            locker = new(gameFileSystem);
            return true;
        }

        gameFileSystem.GenerateConfigurationFile(version, launchScheme);
        {
            ImmutableArray<IniElement> ini = IniSerializer.DeserializeFromFile(gameFileSystem.GetGameConfigurationFilePath());
            IniSerializer.SerializeToFile(gameFileSystem.GetGameConfigurationFilePath(), ini.Add(new IniParameter(InstallingName, string.Empty)));
        }

        locker = new(gameFileSystem);
        return true;
    }

    public void Release()
    {
        ImmutableArray<IniElement> ini = IniSerializer.DeserializeFromFile(gameFileSystem.GetGameConfigurationFilePath());
        IniSerializer.SerializeToFile(gameFileSystem.GetGameConfigurationFilePath(), ini.Remove(ini.Single(e => e is IniParameter { Key: InstallingName })));
    }
}