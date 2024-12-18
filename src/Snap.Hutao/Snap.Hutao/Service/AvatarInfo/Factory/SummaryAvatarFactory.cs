// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.ExceptionService;
using Snap.Hutao.Model;
using Snap.Hutao.Model.Intrinsic;
using Snap.Hutao.Model.Metadata.Avatar;
using Snap.Hutao.Model.Metadata.Converter;
using Snap.Hutao.Model.Primitive;
using Snap.Hutao.Service.AvatarInfo.Factory.Builder;
using Snap.Hutao.Service.Metadata.ContextAbstraction;
using Snap.Hutao.ViewModel.AvatarProperty;
using Snap.Hutao.ViewModel.Wiki;
using Snap.Hutao.Web.Hoyolab.Takumi.GameRecord.Avatar;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Globalization;
using EntityAvatarInfo = Snap.Hutao.Model.Entity.AvatarInfo;
using MetadataAvatar = Snap.Hutao.Model.Metadata.Avatar.Avatar;
using MetadataWeapon = Snap.Hutao.Model.Metadata.Weapon.Weapon;

namespace Snap.Hutao.Service.AvatarInfo.Factory;

internal sealed class SummaryAvatarFactory
{
    private readonly DetailedCharacter character;
    private readonly DateTimeOffset refreshTime;
    private readonly SummaryFactoryMetadataContext context;

    public SummaryAvatarFactory(SummaryFactoryMetadataContext context, EntityAvatarInfo avatarInfo)
    {
        ArgumentNullException.ThrowIfNull(avatarInfo.Info2);

        this.context = context;
        character = avatarInfo.Info2;
        refreshTime = avatarInfo.RefreshTime;
    }

    public static AvatarView Create(SummaryFactoryMetadataContext context, EntityAvatarInfo avatarInfo)
    {
        return new SummaryAvatarFactory(context, avatarInfo).Create();
    }

    public AvatarView Create()
    {
        MetadataAvatar avatar = context.GetAvatar(character.Base.Id);

        ProcessConstellations(
            avatar.SkillDepot,
            character.Constellations,
            out FrozenSet<SkillId> activatedConstellations,
            out FrozenDictionary<SkillId, SkillLevel> extraLevels);

        AvatarView propertyAvatar = new AvatarViewBuilder()
            .SetId(avatar.Id)
            .SetName(avatar.Name)
            .SetQuality(avatar.Quality)
            .SetNameCard(AvatarNameCardPicConverter.IconNameToUri(avatar.NameCard.PicturePrefix))
            .SetElement(ElementNameIconConverter.ElementNameToElementType(avatar.FetterInfo.VisionBefore))
            .SetConstellations(avatar.SkillDepot.Talents, activatedConstellations)
            .SetSkills(avatar.SkillDepot.CompositeSkillsNoInherents, character.Skills.ToFrozenDictionary(s => s.SkillId, s => s.Level), extraLevels)
            .SetFetterLevel(character.Base.Fetter)
            .SetProperties([.. character.SelectedProperties.OrderBy(p => p.PropertyType, InGameFightPropertyComparer.Shared).Select(FightPropertyFormat.ToAvatarProperty)])
            .SetLevelNumber(character.Base.Level)
            .SetWeapon(CreateWeapon(character.Weapon))
            .SetRecommendedProperties(character.RecommendRelicProperty.RecommendProperties)
            .SetReliquaries(character.Relics.SelectAsArray(relic => SummaryReliquaryFactory.Create(context, relic)))
            .SetRefreshTimeFormat(refreshTime, obj => string.Format(CultureInfo.CurrentCulture, "{0:MM-dd HH:mm}", obj), SH.ServiceAvatarInfoSummaryNotRefreshed)
            .SetCostumeIconOrDefault(character, avatar)
            .View;

        return propertyAvatar;
    }

    private static void ProcessConstellations(
        SkillDepot depot,
        ImmutableArray<Constellation> constellations,
        out FrozenSet<SkillId> activatedConstellationIds,
        out FrozenDictionary<SkillId, SkillLevel> extraLevels)
    {
        HashSet<SkillId> constellationIds = [];
        Dictionary<SkillId, SkillLevel> levels = [];

        foreach ((Model.Metadata.Avatar.Skill metaConstellation, Constellation dataConstellation) in depot.Talents.Zip(constellations))
        {
            // Constellations are activated in order, so if the current constellation is
            // not activated, all the subsequent constellations will not be activated.
            if (!dataConstellation.IsActived)
            {
                break;
            }

            constellationIds.Add(dataConstellation.Id);

            if (metaConstellation.ExtraLevel is { } extraLevel)
            {
                int index = extraLevel.Index switch
                {
                    ExtraLevelIndexKind.NormalAttack => 0,
                    ExtraLevelIndexKind.ElementalSkill => 1,
                    ExtraLevelIndexKind.ElementalBurst => 2,
                    _ => throw HutaoException.NotSupported("Unexpected extra level index."),
                };

                levels.Add(depot.CompositeSkillsNoInherents[index].Id, extraLevel.Level);
            }
        }

        activatedConstellationIds = constellationIds.ToFrozenSet();
        extraLevels = levels.ToFrozenDictionary();
    }

    private WeaponView CreateWeapon(DetailedWeapon detailedWeapon)
    {
        MetadataWeapon metadataWeapon = context.GetWeapon(detailedWeapon.Id);

        ImmutableArray<NameValue<string>> baseValues = metadataWeapon.GrowCurves.SelectAsArray(growCurve => BaseValueInfoFormat.ToNameValue(
            PropertyCurveValue.From(growCurve),
            detailedWeapon.Level,
            detailedWeapon.PromoteLevel,
            context.LevelDictionaryWeaponGrowCurveMap,
            context.IdDictionaryWeaponLevelPromoteMap[metadataWeapon.PromoteId]));

        return new WeaponViewBuilder()
            .SetName(metadataWeapon.Name)
            .SetIcon(EquipIconConverter.IconNameToUri(metadataWeapon.Icon))
            .SetDescription(metadataWeapon.Description)
            .SetLevel(LevelFormat.Format(detailedWeapon.Level))
            .SetQuality(metadataWeapon.Quality)
            .SetEquipType(EquipType.EQUIP_WEAPON)
            .SetId(metadataWeapon.Id)
            .SetLevelNumber(detailedWeapon.Level)
            .SetMainProperty(baseValues.ElementAtOrDefault(0))
            .SetSubProperty(baseValues.ElementAtOrDefault(1))
            .SetAffixLevelNumber(detailedWeapon.AffixLevel)
            .SetAffixName(metadataWeapon.Affix?.Name)
            .SetAffixDescription(metadataWeapon.Affix?.Descriptions.Single(a => a.Level == (detailedWeapon.AffixLevel - 1)).Description)
            .SetWeaponType(metadataWeapon.WeaponType)
            .View;
    }
}