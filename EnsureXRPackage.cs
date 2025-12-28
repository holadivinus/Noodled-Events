#if UNITY_EDITOR
using NoodledEvents.Assets.Noodled_Events;
using System;
using System.Collections;
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
                                    ProcessEvent(script, evt);
                            }

                        }

                    }
                }));
            }
        }
        public static void ProcessEvent(MonoBehaviour script, UltEventBase evt)
        {
            for (int i = 0; i < evt.PersistentCallsList.Count; i++)
            {
                PersistentCall pcall = evt.PersistentCallsList[i];

                if (pcall.Method == SysObjStoreProp.GetMethod)
                {
                    GameObject tempObjStore = ((Component)pcall.Target).gameObject;
                    Component realObjStore = tempObjStore.GetComponent(PlateAnimatorType) ?? tempObjStore.AddComponent(PlateAnimatorType);

                    int threshhold = i;
                    // cut off next pcalls
                    var @base = evt.PersistentCallsList.ToArray()[..threshhold].ToList();
                    var remainings = evt.PersistentCallsList.ToArray()[(threshhold + 1)..];

                    PersistentArgument ros = new PersistentArgument();
                    ros.FSetType(PersistentArgumentType.Object);
                    ros.FSetObject(realObjStore);
                    int got = @base.AddGetFieldValue(PlateAnimatorType.GetField("mainSequence", UltEventUtils.AnyAccessBindings), ros);
                    got = @base.AddRunMethod(typeof(IEnumerator).GetProperty("Current", UltEventUtils.AnyAccessBindings).GetMethod, got);
                    int newLength = @base.Count;
                    foreach (var remPcall in remainings)
                        foreach (var pa in remPcall.PersistentArguments)
                            if (pa.Type == PersistentArgumentType.ReturnValue)
                            {
                                if (pa.FGetInt() == threshhold)
                                    pa.FSetInt(got);
                                else if (pa.FGetInt() > threshhold)
                                {
                                    pa.FSetInt(pa.FGetInt() + (newLength - threshhold) + -1);
                                }
                            }
                    @base.AddRange(remainings);
                    evt.FSetPCalls(@base);
                }
                else if (pcall.Method == SysObjStoreProp.SetMethod)
                {
                    GameObject tempObjStore = ((Component)pcall.Target).gameObject;
                    Component realObjStore = tempObjStore.GetComponent(PlateAnimatorType) ?? tempObjStore.AddComponent(PlateAnimatorType);

                    int threshhold = i;
                    // cut off next pcalls
                    var @base = evt.PersistentCallsList.ToArray()[..threshhold].ToList();
                    var remainings = evt.PersistentCallsList.ToArray()[(threshhold + 1)..];
                    PersistentArgument val = pcall.PersistentArguments[0];

                    int valArr = @base.CreateArray(typeof(object), 1);
                    @base.AddArraySet(valArr, val, 0);
                    int numer = @base.AddRunMethod(typeof(IEnumerable).GetMethod("GetEnumerator", UltEventUtils.AnyAccessBindings), valArr);
                    @base.AddRunMethod(typeof(IEnumerator).GetMethod("MoveNext", UltEventUtils.AnyAccessBindings), numer);

                    PersistentArgument ros = new PersistentArgument();
                    ros.FSetType(PersistentArgumentType.Object);
                    ros.FSetObject(realObjStore);

                    int got = @base.AddSetFieldValue(PlateAnimatorType.GetField("mainSequence", UltEventUtils.AnyAccessBindings), ros, numer);
                    int newLength = @base.Count;

                    foreach (var remPcall in remainings)
                        foreach (var pa in remPcall.PersistentArguments)
                            if (pa.Type == PersistentArgumentType.ReturnValue)
                                if (pa.FGetInt() > threshhold)
                                    pa.FSetInt(pa.FGetInt() + (newLength - threshhold) + -1);

                    @base.AddRange(remainings);
                    evt.FSetPCalls(@base);

                }


                if (pcall.MethodName == "UltNoodleRuntimeExtensions, Noodled-Events, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null.ArrayItemSetter1" && !Application.isPlaying)
                    pcall.FSetMethodName("System.Linq.Expressions.Interpreter.CallInstruction, System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e.ArrayItemSetter1");
            }
        }

        private static Type PlateAnimatorType = CookBook.GetExtType("PlateJointAnimator", CookBook.BLAssembly);
        private static PropertyInfo SysObjStoreProp = typeof(ObjectStore).GetProperty("Obj", UltEventUtils.AnyAccessBindings);
        const string fixedMathCS = "using System.Runtime.CompilerServices;\r\n\r\nnamespace SLZ.Bonelab.VoidLogic\r\n{\r\n\tinternal static class MathUtilities\r\n\t{\r\n\t\t[MethodImpl(256)]\r\n\t\tpublic static bool IsApproximatelyEqualToOrGreaterThan(this float num1, float num2)\r\n\t\t{\r\n\t\t\treturn num1 >= num2;\r\n\t\t}\r\n\r\n\t\t[MethodImpl(256)]\r\n\t\tpublic static bool IsApproximatelyEqualToOrLessThan(this float num1, float num2)\r\n\t\t{\r\n\t\t\treturn num1 <= num2;\r\n\t\t}\r\n\t}\r\n}\r\n";
    }
}
#endif
 