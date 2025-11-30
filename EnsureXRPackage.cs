#if UNITY_EDITOR
using NoodledEvents.Assets.Noodled_Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UltEvents;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace NoodledEvents
{
    [InitializeOnLoad]
    static class EnsureXRPackage
    {
        private static Dictionary<Type, List<FieldInfo>> TypeToUltFields = new();
        static EnsureXRPackage()
        {
            // Soft Dependancies
            Client.AddAndRemove(new string[] { "https://github.com/holadivinus/BLXRComp.git", "https://github.com/holadivinus/MarrowBuildHook.git" }, null);


            // also fix up MathUtilities.cs

            string[] existingMaths = AssetDatabase.FindAssets("MathUtilities t:MonoScript").Select(g => AssetDatabase.GUIDToAssetPath(g)).Where(p => !p.Contains("com.stresslevelzero.marrow.sdk.extended")).ToArray();
            if (existingMaths.Length > 0)
            {
                string existingMathPath = Application.dataPath + existingMaths[0].Substring(6);
                if (File.ReadAllText(existingMathPath) != fixedMathCS)
                    File.WriteAllText(existingMathPath, fixedMathCS);
            }
            else
            {
                string scripPath = Application.dataPath + "\\Marrow-ExtendedSDK-MAINTAINED-main\\Scripts\\Assembly-CSharp\\SLZ\\Bonelab\\VoidLogic\\";
                if (!Directory.Exists(scripPath))
                    Directory.CreateDirectory(scripPath);
                scripPath += "MathUtilities.cs";
                File.WriteAllText(scripPath, fixedMathCS);
            }

            // Build hooks!
            Type buildHookType = Type.GetType("MarrowBuildHook.MarrowBuildHook, MarrowBuildHook");
            if (buildHookType != null)
            {
                List<Action<IEnumerable<GameObject>>> softCallbacks = (List<Action<IEnumerable<GameObject>>>)buildHookType.GetField("ExternalGameObjectProcesses").GetValue(null);
                softCallbacks.Add((Action<IEnumerable<GameObject>>)((numer) =>
                {
                    foreach (var gameObject in numer)
                    {

                        foreach (var c in gameObject.GetComponentsInChildren<SerializedBowl>(true))
                            UnityEngine.Object.DestroyImmediate(c);
                        foreach (var c in gameObject.GetComponentsInChildren<LifeCycleEvtEditorRunner>(true))
                            UnityEngine.Object.DestroyImmediate(c);
                        foreach (var c in gameObject.GetComponentsInChildren<VarMan>(true))
                            UnityEngine.Object.DestroyImmediate(c);


                        foreach (var script in gameObject.GetComponentsInChildren<MonoBehaviour>(true))
                        {
                            if (script == null) continue;

                            List<FieldInfo> fields = null;
                            if (!TypeToUltFields.TryGetValue(script.GetType(), out fields))
                            {
                                fields = new List<FieldInfo>();
                                foreach (var f in script.GetType().GetFields((BindingFlags)60))
                                {
                                    if (typeof(IUltEventBase).IsAssignableFrom(f.FieldType))
                                        fields.Add(f);
                                }

                                Type @base = script.GetType();
                                while (@base != typeof(MonoBehaviour))
                                {
                                    @base = @base.BaseType;
                                    foreach (var f in @base.GetFields((BindingFlags)60))
                                    {
                                        if (typeof(IUltEventBase).IsAssignableFrom(f.FieldType))
                                            fields.Add(f);
                                    }
                                }

                                TypeToUltFields[script.GetType()] = fields;
                            }
                            
                            foreach (var ultField in fields)
                            {
                                var evt = (UltEventBase)ultField.GetValue(script);
                                if (evt?.PersistentCallsList != null)
                                    foreach (var pcall in evt.PersistentCallsList)
                                    {
                                        if (pcall.MethodName == "UltNoodleRuntimeExtensions, Noodled-Events, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null.ArrayItemSetter1" && !Application.isPlaying)
                                            pcall.FSetMethodName("System.Linq.Expressions.Interpreter.CallInstruction, System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e.ArrayItemSetter1");
                                    }
                            }

                        }

                    }
                }));
            }
        }
        const string fixedMathCS = "using System.Runtime.CompilerServices;\r\n\r\nnamespace SLZ.Bonelab.VoidLogic\r\n{\r\n\tinternal static class MathUtilities\r\n\t{\r\n\t\t[MethodImpl(256)]\r\n\t\tpublic static bool IsApproximatelyEqualToOrGreaterThan(this float num1, float num2)\r\n\t\t{\r\n\t\t\treturn num1 >= num2;\r\n\t\t}\r\n\r\n\t\t[MethodImpl(256)]\r\n\t\tpublic static bool IsApproximatelyEqualToOrLessThan(this float num1, float num2)\r\n\t\t{\r\n\t\t\treturn num1 <= num2;\r\n\t\t}\r\n\t}\r\n}\r\n";
    }
}
#endif
