#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NoodledEvents
{
    public static class TypeCache
    {
        private static Dictionary<string, Type> _cache = new Dictionary<string, Type>();
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        public static void Initialize()
        {
            lock (_lock)
            {
                if (_initialized) return;

                var sw = System.Diagnostics.Stopwatch.StartNew();

                // Get all relevant assemblies
                var assemblies = new List<System.Reflection.Assembly>();

                // Unity assemblies
                assemblies.Add(typeof(UnityEngine.GameObject).Assembly); // UnityEngine
                assemblies.Add(typeof(UnityEditor.Editor).Assembly);      // UnityEditor

                // Game assemblies
                var gameAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => a.FullName.StartsWith("Assembly-CSharp"));
                assemblies.AddRange(gameAssemblies);

                // XR Toolkit if available
                var xrAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.FullName.StartsWith("Unity.XR.Interaction.Toolkit"));
                if (xrAssembly != null)
                    assemblies.Add(xrAssembly);

                // Other Unity assemblies that might be used
                var otherAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => a.FullName.StartsWith("UnityEngine.") || 
                                a.FullName.StartsWith("Unity.") ||
                                a.FullName.StartsWith("System"));
                assemblies.AddRange(otherAssemblies);

                // Pre-cache all types
                int typeCount = 0;
                foreach (var assembly in assemblies.Distinct())
                {
                    try
                    {
                        foreach (var type in assembly.GetTypes())
                        {
                            try
                            {
                                // Cache by AssemblyQualifiedName (full name)
                                if (!string.IsNullOrEmpty(type.AssemblyQualifiedName))
                                {
                                    _cache[type.AssemblyQualifiedName] = type;
                                    typeCount++;
                                }

                                // Also cache by FullName (for partial matches)
                                if (!string.IsNullOrEmpty(type.FullName))
                                {
                                    _cache[type.FullName] = type;
                                }

                                // Cache simple name for common types
                                if (!string.IsNullOrEmpty(type.Name))
                                {
                                    _cache[type.Name] = type;
                                }
                            }
                            catch (Exception ex)
                            {
                                // Some types can't be reflected (generic, nested, etc)
                                // Just skip them
                            }
                        }
                    }
                    catch (System.Reflection.ReflectionTypeLoadException)
                    {
                        // Assembly has types that can't be loaded, skip it
                    }
                }

                _initialized = true;
                sw.Stop();
                
                Debug.Log($"[TypeCache] Initialized with {typeCount:N0} types from {assemblies.Count} assemblies in {sw.ElapsedMilliseconds}ms");
            }
        }

        public static Type GetType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) 
                return null;

            // Ensure cache is initialized
            if (!_initialized)
                Initialize();

            // Try cache first (fast path)
            if (_cache.TryGetValue(typeName, out var type))
                return type;

            // Fallback to slow Type.GetType() and cache result
            type = Type.GetType(typeName);
            if (type != null)
            {
                lock (_lock)
                {
                    _cache[typeName] = type;
                }
            }

            return type;
        }

        public static void Clear()
        {
            lock (_lock)
            {
                _cache.Clear();
                _initialized = false;
            }
        }

        public static (int cachedTypes, bool initialized) GetStats()
        {
            return (_cache.Count, _initialized);
        }
    }
}
#endif
