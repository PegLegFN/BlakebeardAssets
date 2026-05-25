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
using System.Collections.Concurrent;
using System.Drawing;

namespace BlakebeardAssets.Exporters
{
    internal class HomebaseRatingExporter(IExporterContext services) : BaseExporter(services)
    {
        protected override bool InterestedInAsset(string name) => name.EndsWith("HomebaseRatingMapping.uasset", StringComparison.OrdinalIgnoreCase);

        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output, CancellationToken cancellationToken)
        {
            progress.Report(new ExportProgress { TotalSteps = 1, CompletedSteps = 0, AssetsLoaded = assetsLoaded, CurrentItem = Resources.Status_ExportedItemRatings });

            var ratings = await ExportHomebaseRatings();
            if (ratings is not null)
                output.AddHomebaseRatingRequirements(ratings);

            progress.Report(new ExportProgress { TotalSteps = 1, CompletedSteps = 1, AssetsLoaded = assetsLoaded, CurrentItem = Resources.Status_ExportedItemRatings });
        }

        private async Task<Dictionary<int, int>?> ExportHomebaseRatings()
        {
            var tablePath = assetPaths.Find(p => Path.GetFileNameWithoutExtension(p).Equals("HomebaseRatingMapping", StringComparison.OrdinalIgnoreCase));

            if (tablePath is null)
            {
                logger.LogError(Resources.Error_SpecificAssetNotFound, "HomebaseRatingMapping");
                return null;
            }

            var file = provider[tablePath];
            Interlocked.Increment(ref assetsLoaded);

            var tableData = await provider.LoadPackageObjectAsync<UCurveTable>(file.PathWithoutExtension);

            if (tableData is null)
            {
                logger.LogError(Resources.Error_ExceptionWhileProcessingAsset, "HomebaseRatingMapping");
                return null;
            }

            var rowName = tableData.RowMap.FirstOrDefault().Key;
            var rowKeys = ((FSimpleCurve?)tableData.FindCurve(rowName))?.Keys;
            if(rowKeys is null)
                return null;

            return rowKeys.ToDictionary(fk => (int)fk.Time, fk => (int)fk.Value);
            //int[] result = new int[rowKeys.Max(k => (int)k.Value) + 1];
            //result[0] = 0;
            //for (var i = 0; i < rowKeys.Length-1; i++)
            //{
            //    var fromKey = rowKeys[i];
            //    var toKey = rowKeys[i + 1];
            //    var startIndex = (int)fromKey.Value;
            //    var endIndex = (int)toKey.Value;
            //    for (int j = startIndex; j <= endIndex; j++)
            //    {
            //        float progress = (j - startIndex) / (float)(endIndex - startIndex);
            //        result[j] = (int)(fromKey.Time + ((toKey.Time - fromKey.Time) * progress));
            //    }
            //}
            //return result;
        }
    }
}
