// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Core.Setting;
using Snap.Hutao.Model.Intrinsic;
using Snap.Hutao.Model.Metadata.Converter;
using Snap.Hutao.Model.Metadata.Weapon;
using Snap.Hutao.Model.Primitive;
using Snap.Hutao.ViewModel.AvatarProperty;

namespace Snap.Hutao.Model.Calculable;

internal sealed partial class CalculableWeapon : ObservableObject, ICalculableWeapon
{
    // Only persists current level for non-view weapons
    private readonly bool persistsLevel;

    private CalculableWeapon(Weapon weapon)
    {
        persistsLevel = true;

        WeaponId = weapon.Id;
        LevelMin = 1;
        LevelMax = weapon.MaxLevel;
        Name = weapon.Name;
        Icon = EquipIconConverter.IconNameToUri(weapon.Icon);
        Quality = weapon.RankLevel;

        LevelCurrent = LevelMin;
    }

    private CalculableWeapon(WeaponView weapon)
    {
        persistsLevel = false;

        WeaponId = weapon.Id;
        LevelMin = weapon.LevelNumber;
        LevelMax = weapon.MaxLevel;
        Name = weapon.Name;
        Icon = weapon.Icon;
        Quality = weapon.Quality;

        LevelCurrent = LevelMin;
    }

    public WeaponId WeaponId { get; }

    public uint LevelMin { get; }

    public uint LevelMax { get; }

    public string Name { get; }

    public Uri Icon { get; }

    public QualityType Quality { get; }

    public uint LevelCurrent
    {
        get => persistsLevel ? LocalSetting.Get(SettingKeyCurrentFromQualityType(Quality), LevelMin) : field;
        set => _ = persistsLevel ? SetProperty(LevelCurrent, value, v => LocalSetting.Set(SettingKeyCurrentFromQualityType(Quality), v)) : SetProperty(ref field, value);
    }

    public uint LevelTarget
    {
        get => LocalSetting.Get(SettingKeyTargetFromQualityType(Quality), LevelMax);
        set => SetProperty(LevelTarget, value, v => LocalSetting.Set(SettingKeyTargetFromQualityType(Quality), v));
    }

    public static CalculableWeapon From(Weapon source)
    {
        return new(source);
    }

    public static CalculableWeapon From(WeaponView source)
    {
        return new(source);
    }

    private static string SettingKeyCurrentFromQualityType(QualityType quality)
    {
        return quality >= QualityType.QUALITY_BLUE
            ? SettingKeys.CultivationWeapon90LevelCurrent
            : SettingKeys.CultivationWeapon70LevelCurrent;
    }

    private static string SettingKeyTargetFromQualityType(QualityType quality)
    {
        return quality >= QualityType.QUALITY_BLUE
            ? SettingKeys.CultivationWeapon90LevelTarget
            : SettingKeys.CultivationWeapon70LevelTarget;
    }
}