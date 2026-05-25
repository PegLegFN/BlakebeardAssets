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

using CUE4Parse.UE4.Objects.Engine.Curves;

namespace BlakebeardAssets.Exporters
{
    internal sealed partial class HeroStatExporter(IExporterContext services) : BaseExporter(services)
    {
        protected override bool InterestedInAsset(string name) => name.EndsWith("AttributesHeroScaling.uasset", StringComparison.OrdinalIgnoreCase);
        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output, CancellationToken cancellationToken)
        {
            if (assetPaths.Count==0)
            {
                logger.LogError(Resources.Error_SpecificAssetNotFound, "BaseItemRating");
                return;
            }
            var file = provider[assetPaths[0]];

            Interlocked.Increment(ref assetsLoaded);
            var curveTable = await provider.LoadPackageObjectAsync<UCurveTable>(file.PathWithoutExtension);

            if (curveTable == null)
            {
                logger.LogError(Resources.Warning_CannotParseHeroName, assetPaths[0]);
                return;
            }

            HeroStatTable statTable = new() { Types = [] };
            foreach (var key in curveTable.RowMap.Keys)
            {
                var match = rowNameRegex.Match(key.Text);

                if (!match.Success)
                {
                    //Todo: create a localised version of this warning
                    logger.LogWarning("WARNING: Can&apos;t parse hero stat: {HeroStat}.", key);
                    continue;
                }

                string typeKey = match.Groups[1].Value + "_" + match.Groups[2].Value;
                string tierKey = match.Groups[3].Value + "_T0" + match.Groups[4].Value;
                string statKey = match.Groups[5].Value;

                if (!statTable.Types.ContainsKey(typeKey))
                    statTable.Types.Add(typeKey, []);
                var statTypeDict = statTable.Types[typeKey];
                if (!statTypeDict.ContainsKey(tierKey))
                    statTypeDict.Add(tierKey, []);
                var statTierDict = statTypeDict[tierKey];
                var statCurve = (FSimpleCurve?)curveTable.FindCurve(key);

                int tier = int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);
                int startingValue = (tier - 1) * 10;
                int sampleCount = tier == 5 ? 20 : 10;
                List<float> values = [];
                for (int i = startingValue; i <= startingValue+sampleCount; i++)
                {
                    values.Add(statCurve?.Eval(i) ?? 0);
                }
                statTierDict.Add(statKey, new() { FirstLevel = startingValue, Values = [.. values] });
            }
            output.AddHeroStats(statTable);
        }

        private static readonly Regex rowNameRegex = HeroStatRowRegex();

        [GeneratedRegex(@"^\w+\.([A-Z]+)_([A-Z]+)_(C|UC|R|VR|SR|UR)_T(\d+)\.(.+)$", RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex HeroStatRowRegex();
    }
}
