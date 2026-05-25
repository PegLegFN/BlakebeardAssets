/* Copyright 2023 Tara "Dino" Cassatt
 * 
 * This file is part of BanjoBotAssets.
 * 
 * BanjoBotAssets is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * BanjoBotAssets is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with BanjoBotAssets.  If not, see <http://www.gnu.org/licenses/>.
 */
using CUE4Parse.FN.Enums.FortniteGame;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.Utilities;

namespace BlakebeardAssets.Exporters.UObjects
{
    internal sealed partial class TeamPerkExporter(IExporterContext services) : UObjectExporter<UObject, TeamPerkItemData>(services)
    {
        protected override string Type => "TeamPerk";

        protected override bool InterestedInAsset(string name) => name.Contains("/TPID_", StringComparison.OrdinalIgnoreCase);

        protected override async Task<bool> ExportAssetAsync(UObject asset, TeamPerkItemData teamPerkItemData, Dictionary<ImageType, string> imagePaths)
        {
            Interlocked.Increment(ref assetsLoaded);
            var grantedAbilityKit = await asset.GetOrDefault<FSoftObjectPath>("GrantedAbilityKit").LoadAsync(provider);
            teamPerkItemData.Description = await abilityDescription.GetForPerkAbilityKitAsync(grantedAbilityKit, this) ?? $"<{Resources.Field_NoDescription}>";

            teamPerkItemData.CommanderRequirement = HeroExporter.GetHeroPerkRequirement(asset);

            string heroRequirementText = "";
            if(asset.GetOrDefaultFromDataList<FSoftObjectPath?>("TooltipClass") is FSoftObjectPath ttPath)
            {
                var tooltipCDO = await (await ttPath.LoadAsync<UBlueprintGeneratedClass>(provider)).ClassDefaultObject.LoadAsync();
                if(tooltipCDO?.GetOrDefault<FText>("Description").Text is string tooltip)
                {
                    heroRequirementText = TooltipMarkupTag().Replace(tooltip, "");
                }
            }

            var loadoutConditionStruct = asset.GetOrDefault<FStructFallback[]>("TeamPerkLoadoutConditions")[0];
            var supportRequirementTags = HeroExporter.GetHeroPerkRequirement(loadoutConditionStruct, "RequiredTagQuery");

            TeamPerkRequirement supportRequirements = new()
            {
                Description = heroRequirementText,
                HeroTags = supportRequirementTags?.CommanderTag,
                HeroSubType = supportRequirementTags?.CommanderSubType,
                MinimumQuantity = 1
            };

            if (loadoutConditionStruct.GetOrDefault<bool>("bConsiderMinimumRarity"))
            {
                supportRequirements.MinimumRarity = loadoutConditionStruct.GetOrDefault<EFortRarity>("MinimumHeroRarity").GetNameText().Text;
            }

            if (loadoutConditionStruct.GetOrDefault<bool>("bConsiderMinimumTier"))
            {
                supportRequirements.MinimumTier = (int)loadoutConditionStruct.GetOrDefault<EFortItemTier>("MinimumHeroTier");
            }

            teamPerkItemData.ProgressiveBonus = asset.GetOrDefault<bool>("bProgressiveBonus");
            if (teamPerkItemData.ProgressiveBonus != true)
                supportRequirements.MinimumQuantity = loadoutConditionStruct.GetOrDefault<int>("NumTimesSatisfiable");

            teamPerkItemData.SupportRequirements = supportRequirements;

            if (grantedAbilityKit.GetResourceObjectPath("IconBrush") is string path)
            {
                imagePaths.Add(ImageType.Icon, path);
            }

            return true;
        }

        [GeneratedRegex("<[^>]*>")]
        private static partial Regex TooltipMarkupTag();
    }
}
