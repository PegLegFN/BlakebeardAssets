using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace BanjoBotAssets
{
    internal static class PegLegPostProcessor
    {
        public enum ImageMode
        {
            Ignore,
            Move,
            Copy
        }
        public static void PostProcessBanjoAssets(string banjoBotOutputPath, string destinationPath, ImageMode imageMode)
        {
            DirectoryInfo banjoDir = new(banjoBotOutputPath);
            DirectoryInfo destinationDir = new(destinationPath);

            if (!banjoDir.Exists || !destinationDir.Exists)
            {
                Console.WriteLine("no folders");
                return;
            }

            FileInfo banjoFile = new(banjoDir.FullName + "/assets.json");

            if (!banjoFile.Exists)
                return;

            JsonObject resultDatabase = banjoFile.ReadJSON().AsObject();

            KeyValuePair<string, JsonNode?>[] items = [.. (resultDatabase["NamedItems"]?.AsObject() ?? [])];

            object indexLock = new();
            int total = items.Length;
            int nextIndex = 0;

            ConcurrentDictionary<string, ConcurrentDictionary<string, JsonNode?>> splitItems = [];
            ConcurrentDictionary<string, JsonNode> splitNonItems = [];

            void ProcessItem()
            {
                while (nextIndex < total)
                {
                    int currentIndex;
                    lock (indexLock)
                    {
                        currentIndex = nextIndex;
                        nextIndex++;
                    }
                    if (currentIndex >= total)
                        break;
                    JsonObject? itemValue = JsonNode.Parse(items[currentIndex].Value?.ToString() ?? "{}")?.AsObject();

                    if (itemValue is null)
                        continue;
                    itemValue ??= [];

                    string itemType = itemValue["Type"]!.ToString();
                    string itemKey = $"{itemType}:{itemValue["Name"]!.ToString().ToLower(CultureInfo.InvariantCulture)}";

                    if (itemType is null)
                        continue;

                    if (itemValue["AssetPath"]?.ToString() is string assetPath)
                        itemValue["AssetPath"] = assetPath.ToLowerInvariant();

                    splitItems.TryAdd(itemType, []);
                    splitItems[itemType].TryAdd(itemKey, itemValue);
                    Console.WriteLine($"ported \"{itemValue?["DisplayName"]?.ToString()}\" to {itemType} object");
                }
            }

            Thread[] threads = new Thread[8];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new(new ThreadStart(ProcessItem));
                threads[i].Start();
            }
            ProcessItem();

            foreach (var remainingData in resultDatabase)
            {
                if (remainingData.Key == "NamedItems")
                    continue;
                if (remainingData.Value is JsonObject || remainingData.Value is JsonArray)
                    splitNonItems.TryAdd(remainingData.Key, remainingData.Value);
            }

            Directory.CreateDirectory($"{destinationDir.FullName}/NamedItems");
            foreach (var item in splitItems)
            {
                if (item.Key.Contains('/'))
                {
                    Console.WriteLine("oops: " + item.Key);
                    continue;
                }
                FileInfo splitItemFile = new($"{destinationDir.FullName}/NamedItems/{item.Key}.json");
                using var writer = splitItemFile.CreateText();
                writer.WriteLine(new JsonObject(item.Value.OrderBy(kvp=>kvp.Key).AsEnumerable()).ToString());
                writer.Flush();
                Console.WriteLine("saved json: " + item.Key);
            }

            foreach (var item in splitNonItems)
            {
                if (item.Key.Contains('/'))
                {
                    Console.WriteLine("oops: " + item.Key);
                    continue;
                }
                FileInfo splitItemFile = new($"{destinationDir.FullName}/{item.Key}.json");
                using var writer = splitItemFile.CreateText();
                writer.WriteLine(item.Value.ToString());
                writer.Flush();
                Console.WriteLine("saved json: " + item.Key);
            }

            if (imageMode!=ImageMode.Ignore)
            {
                //Console.WriteLine("press enter to copy images");
                //Console.ReadLine();
                DirectoryInfo imagesSourceFolder = new(banjoDir.FullName + "/ExportedImages");
                DirectoryInfo imagesDestFolder = new(destinationDir.FullName + "/ExportedImages");
                if (!imagesSourceFolder.Exists)
                    return;
                if (!imagesDestFolder.Exists)
                    imagesDestFolder.Create();
                foreach (var oldFile in imagesSourceFolder.GetFiles())
                {
                    FileInfo newFile = new(imagesDestFolder.FullName + "/" + oldFile.Name);
                    if (newFile.Exists)
                    {
                        if (newFile.CreationTime.CompareTo(oldFile.CreationTime) < 0)
                            newFile.Delete();
                        else
                            continue;
                    }
                    if (imageMode == ImageMode.Copy)
                    {
                        Console.WriteLine("copied image: " + oldFile.Name);
                        oldFile.CopyTo(newFile.FullName);
                    }
                    else
                    {
                        Console.WriteLine("moved image: " + oldFile.Name);
                        oldFile.MoveTo(newFile.FullName);
                    }
                }
            }
            Console.WriteLine("all done");
        }

        static JsonNode ReadJSON(this FileInfo file, bool skipSafety = true)
        {
            // if we can guarentee there are no errors
            if (skipSafety)
            {
                using var textStream = file.OpenText();
                return JsonNode.Parse(textStream.ReadToEnd()) ?? new JsonObject();
            }

            string fileContent = "";
            string lastKey = "";
            ///this ugly mess is a workaround for an error in
            ///FModel that causes it to produce multiple key-value
            ///pairs with identical keys in the same context
            using (var textStream = file.OpenText())
            {
                string? line;
                while ((line = textStream.ReadLine()) != null)
                {
                    string[] splitLine = line.Split(':');
                    if (splitLine.Length > 1)
                    {
                        string trimmedKey = splitLine[0].Trim();
                        if (trimmedKey == lastKey)
                            continue;
                        lastKey = trimmedKey;
                    }
                    fileContent += line;
                }
            }
            return JsonNode.Parse(fileContent) ?? new JsonObject();
        }
    }
}
