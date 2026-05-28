#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NoodledEvents
{
    /// <summary>
    /// Serializable cache for NodeDef data. Stores everything except UI elements.
    /// </summary>
    [Serializable]
    public class NodeDefCache
    {
        [Serializable]
        public class CachedNodeDef
        {
            public string Name;
            public string CookBookName; // Name of the cookbook asset
            public CachedPin[] Inputs;
            public CachedPin[] Outputs;
            public string BookTag;
            public string SearchTextOverride;
            public string TooltipOverride;
        }

        [Serializable]
        public class CachedPin
        {
            public string Name;
            public string TypeFullName; // Store as string for serialization
            public bool Const;
            
            public Type GetPinType()
            {
                if (string.IsNullOrEmpty(TypeFullName)) return null;
                return TypeCache.GetType(TypeFullName);
            }
        }

        public static CachedNodeDef ToCached(CookBook.NodeDef nodeDef)
        {
            return new CachedNodeDef
            {
                Name = nodeDef.Name,
                CookBookName = nodeDef.CookBook?.name,
                BookTag = nodeDef.BookTag,
                SearchTextOverride = string.Empty, // Will be reconstructed from name
                TooltipOverride = string.Empty,
                Inputs = nodeDef.Inputs?.Select(p => new CachedPin
                {
                    Name = p.Name,
                    TypeFullName = p.Type?.AssemblyQualifiedName,
                    Const = p.Const
                }).ToArray() ?? Array.Empty<CachedPin>(),
                Outputs = nodeDef.Outputs?.Select(p => new CachedPin
                {
                    Name = p.Name,
                    TypeFullName = p.Type?.AssemblyQualifiedName,
                    Const = p.Const
                }).ToArray() ?? Array.Empty<CachedPin>()
            };
        }

        public static CookBook.NodeDef FromCached(CachedNodeDef cached, CookBook cookbook)
        {
            return new CookBook.NodeDef(
                book: cookbook,
                name: cached.Name,
                inputs: () => cached.Inputs?.Select(p => new CookBook.NodeDef.Pin(
                    p.Name,
                    p.GetPinType(),
                    p.Const
                )).ToArray() ?? Array.Empty<CookBook.NodeDef.Pin>(),
                outputs: () => cached.Outputs?.Select(p => new CookBook.NodeDef.Pin(
                    p.Name,
                    p.GetPinType(),
                    p.Const
                )).ToArray() ?? Array.Empty<CookBook.NodeDef.Pin>(),
                bookTag: cached.BookTag ?? "",
                searchTextOverride: cached.SearchTextOverride ?? "",
                tooltipOverride: cached.TooltipOverride ?? ""
            );
        }
    }
}
#endif