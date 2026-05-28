#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;

namespace NoodledEvents
{
    /// <summary>
    /// Manages persistent caching of NodeDefs to improve loading speed.
    /// </summary>
    public static class NodeDefCacheManager
    {
        private const string CACHE_FILE_PATH = "Library/NoodleNodeDefCache.bin";
        private const string CACHE_VERSION = "2.0";

        public static bool TryLoadCache(CookBook[] allBooks, out List<CookBook.NodeDef> nodeDefs, bool rebuildOnAssemblyChange = false, 
            Action<int, int, float> progressCallback = null)
        {
            nodeDefs = new List<CookBook.NodeDef>();

            if (!File.Exists(CACHE_FILE_PATH))
            {
                Debug.Log("[NodeCache] No cache file found, will generate fresh.");
                return false;
            }

            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                TypeCache.Initialize();

                using (FileStream fileStream = new FileStream(CACHE_FILE_PATH, FileMode.Open, FileAccess.Read, FileShare.Read, 
                    bufferSize: 2 * 1024 * 1024)) // 2MB buffer for faster reads
                using (GZipStream gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
                using (BinaryReader reader = new BinaryReader(gzipStream))
                {
                    // Read header
                    string version = reader.ReadString();
                    string unityVersion = reader.ReadString();
                    long assemblyTimestamp = reader.ReadInt64();

                    // Validate cache
                    if (version != CACHE_VERSION)
                    {
                        Debug.Log($"[NodeCache] Cache version mismatch: {version} != {CACHE_VERSION}");
                        return false;
                    }

                    if (unityVersion != Application.unityVersion)
                    {
                        Debug.Log($"[NodeCache] Unity version changed: {unityVersion} -> {Application.unityVersion}");
                        return false;
                    }

                    long currentTimestamp = GetAssemblyTimestamp();
                    if (assemblyTimestamp != currentTimestamp)
                    {
                        if (rebuildOnAssemblyChange)
                        {     
                            Debug.Log($"[NodeCache] Assembly changed: {assemblyTimestamp} -> {currentTimestamp}");
                            return false;
                        }
                        else
                            Debug.LogWarning("[NodeCache] Assembly change detected but rebuildOnAssemblyChange is false. Attempting to load cache anyway...");
                    }

                    // Build cookbook lookup
                    var cookbookLookup = allBooks.ToDictionary(b => b.name, b => b);

                    // Read node count
                    int nodeCount = reader.ReadInt32();
                    nodeDefs = new List<CookBook.NodeDef>(nodeCount);

                    // Progress reporting setup
                    const int REPORT_INTERVAL = 10000; // Report every 10k nodes
                    int lastReportedCount = 0;
                    var progressStopwatch = System.Diagnostics.Stopwatch.StartNew();

                    // Stream nodes in batches for better performance
                    for (int i = 0; i < nodeCount; i++)
                    {
                        var cached = ReadCachedNodeDef(reader);
                        
                        if (cookbookLookup.TryGetValue(cached.CookBookName, out var cookbook))
                        {
                            var nodeDef = NodeDefCache.FromCached(cached, cookbook);
                            nodeDefs.Add(nodeDef);
                        }

                        // Report progress periodically
                        if (progressCallback != null && (i - lastReportedCount >= REPORT_INTERVAL || i == nodeCount - 1))
                        {
                            float percentage = (i + 1) / (float)nodeCount;
                            progressCallback?.Invoke(i + 1, nodeCount, percentage);
                            lastReportedCount = i;
                        }
                    }
                }

                stopwatch.Stop();
                float totalSeconds = stopwatch.ElapsedMilliseconds / 1000f;
                float nodesPerSec = nodeDefs.Count / totalSeconds;
                Debug.Log($"[NodeCache] Successfully loaded {nodeDefs.Count:N0} nodes from cache in {stopwatch.ElapsedMilliseconds}ms ({nodesPerSec:N0} nodes/sec)!");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NodeCache] Failed to load cache: {ex.Message}");
                return false;
            }
        }

        public static void SaveCache(List<CookBook.NodeDef> nodeDefs, Action<int, int, float> progressCallback = null)
        {
            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Phase 1: Convert to cached format
                Debug.Log($"[NodeCache] Converting {nodeDefs.Count:N0} nodes to cache format...");
                var conversionStopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                var cachedDefs = new NodeDefCache.CachedNodeDef[nodeDefs.Count];
                int processed = 0;
                
                for (int i = 0; i < nodeDefs.Count; i++)
                {
                    cachedDefs[i] = NodeDefCache.ToCached(nodeDefs[i]);
                    int current = processed++;
                    if (current % 10000 == 0 || current == nodeDefs.Count)
                    {
                        float percentage = current / (float)nodeDefs.Count;
                        progressCallback?.Invoke(current, nodeDefs.Count, percentage);
                    }
                }

                conversionStopwatch.Stop();
                Debug.Log($"[NodeCache] Conversion completed in {conversionStopwatch.ElapsedMilliseconds}ms ({nodeDefs.Count / (conversionStopwatch.ElapsedMilliseconds / 1000f):N0} nodes/sec)");

                // Phase 2: Write to disk with compression
                Debug.Log($"[NodeCache] Writing cache to disk...");
                var writeStopwatch = System.Diagnostics.Stopwatch.StartNew();

                using (FileStream fileStream = new FileStream(CACHE_FILE_PATH, FileMode.Create, FileAccess.Write, FileShare.None,
                    bufferSize: 1024 * 1024)) // 1MB buffer for faster writes
                using (GZipStream gzipStream = new GZipStream(fileStream, System.IO.Compression.CompressionLevel.Optimal))
                using (BinaryWriter writer = new BinaryWriter(gzipStream))
                {
                    // Write header
                    writer.Write(CACHE_VERSION);
                    writer.Write(Application.unityVersion);
                    writer.Write(GetAssemblyTimestamp());

                    // Write node count
                    writer.Write(cachedDefs.Length);

                    // Write each node with progress
                    int lastReported = 0;
                    for (int i = 0; i < cachedDefs.Length; i++)
                    {
                        WriteCachedNodeDef(writer, cachedDefs[i]);

                        if (progressCallback != null && (i - lastReported >= 10000 || i == cachedDefs.Length - 1))
                        {
                            float percentage = (i + 1) / (float)cachedDefs.Length;
                            progressCallback?.Invoke(i + 1, cachedDefs.Length, percentage);
                            lastReported = i;
                        }
                    }
                }

                writeStopwatch.Stop();
                stopwatch.Stop();

                var fileInfo = new FileInfo(CACHE_FILE_PATH);
                float totalSeconds = stopwatch.ElapsedMilliseconds / 1000f;
                float nodesPerSec = nodeDefs.Count / totalSeconds;
                float mbSize = fileInfo.Length / 1024f / 1024f;
                
                Debug.Log($"[NodeCache] Cache saved successfully:");
                Debug.Log($"  - Total time: {stopwatch.ElapsedMilliseconds}ms ({nodesPerSec:N0} nodes/sec)");
                Debug.Log($"  - Conversion: {conversionStopwatch.ElapsedMilliseconds}ms");
                Debug.Log($"  - Write+Compress: {writeStopwatch.ElapsedMilliseconds}ms");
                Debug.Log($"  - File size: {mbSize:F2}MB ({nodeDefs.Count / mbSize:N0} nodes/MB)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NodeCache] Failed to save cache: {ex.Message} {ex.StackTrace}");
            }
        }

        public static void ClearCache()
        {
            if (File.Exists(CACHE_FILE_PATH))
            {
                File.Delete(CACHE_FILE_PATH);
                Debug.Log("[NodeCache] Cache cleared.");
            }
        }

        private static long GetAssemblyTimestamp()
        {
            try
            {
                string[] assemblyPaths = new[]
                {
                    "Library/ScriptAssemblies/Assembly-CSharp.dll",
                    "Library/ScriptAssemblies/Assembly-CSharp-Editor.dll"
                };

                long maxTimestamp = 0;
                foreach (var path in assemblyPaths)
                {
                    if (File.Exists(path))
                    {
                        long timestamp = File.GetLastWriteTime(path).Ticks;
                        if (timestamp > maxTimestamp)
                            maxTimestamp = timestamp;
                    }
                }

                return maxTimestamp;
            }
            catch
            {
                return DateTime.Now.Ticks;
            }
        }

        private static void WriteCachedNodeDef(BinaryWriter writer, NodeDefCache.CachedNodeDef cached)
        {
            writer.Write(cached.Name ?? string.Empty);
            writer.Write(cached.CookBookName ?? string.Empty);
            writer.Write(cached.BookTag ?? string.Empty);
            writer.Write(cached.SearchTextOverride ?? string.Empty);
            writer.Write(cached.TooltipOverride ?? string.Empty);

            // Write inputs
            writer.Write(cached.Inputs?.Length ?? 0);
            if (cached.Inputs != null)
            {
                foreach (var pin in cached.Inputs)
                {
                    writer.Write(pin.Name ?? string.Empty);
                    writer.Write(pin.TypeFullName ?? string.Empty);
                    writer.Write(pin.Const);
                }
            }

            // Write outputs
            writer.Write(cached.Outputs?.Length ?? 0);
            if (cached.Outputs != null)
            {
                foreach (var pin in cached.Outputs)
                {
                    writer.Write(pin.Name ?? string.Empty);
                    writer.Write(pin.TypeFullName ?? string.Empty);
                    writer.Write(pin.Const);
                }
            }
        }

        private static NodeDefCache.CachedNodeDef ReadCachedNodeDef(BinaryReader reader)
        {
            var cached = new NodeDefCache.CachedNodeDef
            {
                Name = reader.ReadString(),
                CookBookName = reader.ReadString(),
                BookTag = reader.ReadString(),
                SearchTextOverride = reader.ReadString(),
                TooltipOverride = reader.ReadString()
            };

            // Read inputs
            int inputCount = reader.ReadInt32();
            cached.Inputs = new NodeDefCache.CachedPin[inputCount];
            for (int i = 0; i < inputCount; i++)
            {
                cached.Inputs[i] = new NodeDefCache.CachedPin
                {
                    Name = reader.ReadString(),
                    TypeFullName = reader.ReadString(),
                    Const = reader.ReadBoolean()
                };
            }

            // Read outputs
            int outputCount = reader.ReadInt32();
            cached.Outputs = new NodeDefCache.CachedPin[outputCount];
            for (int i = 0; i < outputCount; i++)
            {
                cached.Outputs[i] = new NodeDefCache.CachedPin
                {
                    Name = reader.ReadString(),
                    TypeFullName = reader.ReadString(),
                    Const = reader.ReadBoolean()
                };
            }

            return cached;
        }
    }
}
#endif