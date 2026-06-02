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
#pragma warning disable CA1852 // https://github.com/dotnet/roslyn-analyzers/issues/6141

using System.Reflection;
using System.Runtime.CompilerServices;
using BanjoBotAssets.Config;

[assembly: InternalsVisibleTo("BanjoBotAssets.SourceGenerators.Tests")]

namespace BanjoBotAssets;

internal class OldProgram
{
    internal async Task<int> Main(string[] args)
    {
// TODO: export per-difficulty stat clamp tables (GameDifficultyGrowthBounds, CombatStatClampsPerTheater)
// TODO: export collection book categories and recruitment/research/voucher options (CollectionBookSlots)
#if !RELEASE
Console.WriteLine("type \"skip\" to skip resource extraction");
if (Console.ReadLine() != "skip")
#endif
{
await Host.CreateDefaultBuilder(args)
#if DEBUG
    .UseEnvironment("Development")
#endif
    .UseContentRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException("Null content root"))
    .ConfigureLogging(logging =>
    {
        logging
            .ClearProviders()
            .AddSimpleConsole(console => console.SingleLine = true);
    })
    .ConfigureServices(services =>
    {
        services
            .AddHostedService<AssetExportService>()
            .AddHttpClient()
            .AddAesProviders()
            .AddMappingsProviders()
            .AddGameFileProvider()
            .AddAssetExporters();

        services
            .AddOptions<ScopeOptions>()
            .Configure<IConfiguration>((scope, config) => config.Bind(scope));
    })
    .RunConsoleAsync(o => o.SuppressStatusMessages = true);
}
string pegLegDestination = Path.GetFullPath("PegLegAssets", Environment.CurrentDirectory);
#if !RELEASE
Console.WriteLine("type \"skip\" to skip pegleg post processing");
pegLegDestination = @"C:\Users\Tomatech\Repositories\TomatechGames\Godot Projects\PegLegResourcePackager\PegLegResources\GameAssets";
if (Console.ReadLine() != "skip")
#endif
{
    if (!Directory.Exists(pegLegDestination))
    {
        if (pegLegDestination.StartsWith(@"C:\Users\Tomatech", StringComparison.OrdinalIgnoreCase))
            return Environment.ExitCode;
        else
            Directory.CreateDirectory(pegLegDestination);
    }
    PegLegPostProcessor.PostProcessBanjoAssets(Environment.CurrentDirectory, pegLegDestination, PegLegPostProcessor.ImageMode.Move);
}

return Environment.ExitCode;
    }
}
