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

using BanjoBotAssets.Json;
using BanjoBotAssets.UExports;
using Org.BouncyCastle.Asn1.Cms;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

namespace BanjoBotAssets.Exporters
{
    internal sealed class AlterationLoadoutExporter(IExporterContext services) : BaseExporter(services)
    {
        private string? alterationGroupPath, slotDefsPath, slotLoadoutsPath;

        protected override bool InterestedInAsset(string name)
        {
            switch (Path.GetFileName(name))
            {
                case string s when s.Equals("AlterationGroups.uasset", StringComparison.OrdinalIgnoreCase):
                    alterationGroupPath = name;
                    return true;
                case string s when s.Equals("SlotDefs.uasset", StringComparison.OrdinalIgnoreCase):
                    slotDefsPath = name;
                    return true;
                case string s when s.Equals("SlotLoadouts.uasset", StringComparison.OrdinalIgnoreCase):
                    slotLoadoutsPath = name;
                    return true;
            }
            return false;
        }

        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output, CancellationToken cancellationToken)
        {
            var alterationGroupTask = TryLoadTableAsync(alterationGroupPath);
            var slotDefsTask = TryLoadTableAsync(slotDefsPath);
            var slotLoadoutsTask = TryLoadTableAsync(slotLoadoutsPath);

            var alterationGroupTable = (await alterationGroupTask)?.ToDictionary();
            var slotDefsTable = (await slotDefsTask)?.ToDictionary();
            var slotLoadoutsTable = (await slotLoadoutsTask)?.ToDictionary();

            if (alterationGroupTable is null)
                return;
            if (slotDefsTable is null)
                return;
            if (slotLoadoutsTable is null)
                return;

            Dictionary<string, AlterationSlot[]> slotDict = [];
            foreach (var kvp in slotLoadoutsTable)
            {
                logger.LogInformation($"Processing Alteration Loadout {kvp.Key}");
                var slots = kvp.Value.GetOrDefault<FStructFallback[]>("AlterationSlots").Select<FStructFallback, AlterationSlot>(slotData =>
                {
                    var slotDefRow = slotData.GetOrDefault<FName>("SlotDefinitionRow").Text;
                    var slotDef = slotDefsTable.TryGetValue(slotDefRow ?? "", out var d) ? d : null;

                    var baseCosts = slotDef?.GetOrDefault<FFortItemQuantityPair[]>("BaseRespecCosts")
                    .ToDictionary(
                        p => $"{p.ItemPrimaryAssetId.PrimaryAssetType.Name.Text}:{p.ItemPrimaryAssetId.PrimaryAssetName.Text}",
                        p => p.Quantity,
                        StringComparer.OrdinalIgnoreCase
                    );
                    if (baseCosts?.Count == 0)
                        baseCosts = null;

                    var altGroupRow = slotDef?.GetOrDefault<FName>("InitTierGroup").Text;
                    var altGroup = alterationGroupTable.TryGetValue(altGroupRow ?? "", out var g) ? g : null;

                    var mapping = altGroup?.GetOrDefault("RarityMapping", new UScriptMap());
                    RawAlteration[]? rawAlts = null;
                    if (mapping?.Properties.FirstOrDefault().Value?.GenericValue is FScriptStruct { StructType: FStructFallback weightedAlts })
                    {
                        rawAlts = weightedAlts.GetOrDefault<FStructFallback[]>("WeightData")
                            .Select<FStructFallback, RawAlteration>(wd => new()
                            {
                                AID=wd.GetOrDefault<string>("AID"),
                                ExclusionNames = wd.GetOrDefault<string[]>("ExclusionNames"),
                            })
                            .ToArray();
                    }

                    return new()
                    {
                        RequiredLevel = slotData.Get<int>("UnlockLevel"),
                        RequiredRarity = slotData.Get<EFortRarity>("UnlockRarity").GetNameText().Text,
                        BaseRespecCost = baseCosts,
                        RawAlterations = rawAlts ?? [],
                    };
                });
                slotDict.Add(kvp.Key, [..slots]);
            }
            //logger.LogInformation($"yooo: {JsonSerializer.Serialize(slotDict["SlotLoadout.Explicit.Ice.Sword"][2])}");
            output.AddAlterationLoadouts(slotDict);
        }
    }
}
