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
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace BlakebeardAssets.Json
{
    public sealed class AlterationSlot
    {
        public int RequiredLevel { get; set; }
        [DisallowNull]
        public string? RequiredRarity { get; set; }
        [DisallowNull]
        public RawAlteration[]? RawAlterations { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, int>? BaseRespecCost { get; set; }
    }

    public sealed class RawAlteration
    {
        [DisallowNull]
        public string? AID { get; set; }
        [DisallowNull]
        public string[]? ExclusionNames { get; set; }
    }
}
