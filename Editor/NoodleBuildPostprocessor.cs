#if UNITY_EDITOR

using NoodledEvents;
using NoodledEvents.Assets.Noodled_Events;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class NoodleBuildPostprocessor
{
    private static Dictionary<string, string> StrAssets = new();
    [UnityEditor.Callbacks.PostProcessScene(0)]
    public static void BuildCallBack()
    {
        foreach (var t in new[] { typeof(SerializedBowl), typeof(LifeCycleEvtEditorRunner), typeof(VarMan) })
        {
            foreach (var comp in UnityEngine.Resources.FindObjectsOfTypeAll(t))
            {
                if (!PrefabUtility.IsPartOfAnyPrefab(comp))
                {
                    Object.DestroyImmediate(comp);
                }
                else 
                {
                    // this comp has to be FULLY destroyed on pack, due to bullshit.
                    // We'll find its asset, save as text, restore afterward.
                    string path = AssetDatabase.GetAssetPath(comp);
                    if (!StrAssets.ContainsKey(path)) StrAssets.Add(path, "");
                    if (StrAssets[path] == "")
                    {
                        StrAssets[path] = File.ReadAllText(path);
                        EditorApplication.delayCall += () =>
                        {
                            File.WriteAllText(path, StrAssets[path]);
                            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                            StrAssets[path] = "";
                        };
                        Object.DestroyImmediate(comp, true);
                    } else Object.DestroyImmediate(comp, true); // already cached, just kill
                }
            }
        }
    }
}
#endif