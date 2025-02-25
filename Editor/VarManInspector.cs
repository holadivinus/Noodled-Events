#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
using UltEvents;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NoodledEvents.Assets.Noodled_Events
{
    [CustomEditor(typeof(VarMan))]
    public class VarManInspector : Editor
    {
        [UnityEngine.SerializeField] public VisualTreeAsset UI;
        [UnityEngine.SerializeField] public VisualTreeAsset UI_ListElement;

        public VarMan myMan;

        // List of "UI_ListElement", which handles modifying & applying vars
        public ScrollView VarList;
        public override VisualElement CreateInspectorGUI()
        {
            myMan = (VarMan)target;

            // Load UI from File
            TemplateContainer myInspector = UI.Instantiate();
            VarList = myInspector.Q<ScrollView>(nameof(VarList));

            var varNamer = myInspector.Q<TextField>("NewVarName");

            myInspector.Q<Button>("bool").clicked += () =>
            {
                var @new = new NoodleDataInput();
                @new.Name = varNamer.value;
                @new.ConstInput = PersistentArgumentType.Bool;

                myMan.Vars = myMan.Vars.Append(@new).ToArray();
                EditorUtility.SetDirty(myMan); PrefabUtility.RecordPrefabInstancePropertyModifications(myMan);
                varNamer.value = "";
                RegenList();
            };
            myInspector.Q<Button>("string").clicked += () =>
            {
                var @new = new NoodleDataInput();
                @new.Name = varNamer.value;
                @new.ConstInput = PersistentArgumentType.String;

                myMan.Vars = myMan.Vars.Append(@new).ToArray();
                EditorUtility.SetDirty(myMan); PrefabUtility.RecordPrefabInstancePropertyModifications(myMan);
                varNamer.value = "";
                RegenList();
            };
            myInspector.Q<Button>("float").clicked += () =>
            {
                var @new = new NoodleDataInput();
                @new.Name = varNamer.value;
                @new.ConstInput = PersistentArgumentType.Float;

                myMan.Vars = myMan.Vars.Append(@new).ToArray();
                EditorUtility.SetDirty(myMan); PrefabUtility.RecordPrefabInstancePropertyModifications(myMan);
                varNamer.value = "";
                RegenList();
            };
            myInspector.Q<Button>("int").clicked += () =>
            {
                var @new = new NoodleDataInput();
                @new.Name = varNamer.value;
                @new.ConstInput = PersistentArgumentType.Int;

                myMan.Vars = myMan.Vars.Append(@new).ToArray();
                EditorUtility.SetDirty(myMan); PrefabUtility.RecordPrefabInstancePropertyModifications(myMan);
                varNamer.value = "";
                RegenList();
            };
            myInspector.Q<Button>("obj").clicked += () =>
            {
                var @new = new NoodleDataInput();
                @new.Name = varNamer.value;
                @new.ConstInput = PersistentArgumentType.Object;

                myMan.Vars = myMan.Vars.Append(@new).ToArray();
                EditorUtility.SetDirty(myMan); PrefabUtility.RecordPrefabInstancePropertyModifications(myMan);
                varNamer.value = "";
                RegenList();
            };

            RegenList();
            // Add a simple label.
            // Return the finished Inspector UI.
            return myInspector;
        }
        public void RegenList()
        {
            VarList.Clear();
            foreach (var varrr in myMan.Vars)
            {
                var SData = varrr;
                SData.Type = Typz[varrr.ConstInput];
                var entry = UI_ListElement.Instantiate();
                entry.Q<Label>("varname").text = $"<b>{varrr.Name}</b>:";
                var vfr = entry.Q("varfieldroot");
                switch (varrr.ConstInput)
                {
                    case UltEvents.PersistentArgumentType.Bool:
                        {
                            var t = new Toggle("");
                            vfr.Add(t);
                            t.value = SData.DefaultBoolValue;
                            t.RegisterValueChangedCallback((e) => SData.DefaultBoolValue = e.newValue);
                            break;
                        }
                    case UltEvents.PersistentArgumentType.String:
                        {
                            var t = new TextField("");
                            vfr.Add(t);
                            t.value = SData.DefaultStringValue;
                            t.RegisterValueChangedCallback((e) => SData.DefaultStringValue = e.newValue);
                            break;
                        }
                    case UltEvents.PersistentArgumentType.Int:
                        {
                            var t = new IntegerField("");
                            vfr.Add(t);
                            t.value = SData.DefaultIntValue;
                            t.RegisterValueChangedCallback((e) => SData.DefaultIntValue = e.newValue);
                            break;
                        }
                    case UltEvents.PersistentArgumentType.Float:
                        {
                            var t = new FloatField("");
                            vfr.Add(t);
                            t.value = SData.DefaultFloatValue;
                            t.RegisterValueChangedCallback((e) => SData.DefaultFloatValue = e.newValue);
                            break;
                        }
                    case UltEvents.PersistentArgumentType.Object:
                        {
                            var t = new ObjectField("");
                            t.objectType = typeof(UnityEngine.Object);
                            vfr.Add(t);
                            t.value = SData.DefaultObject;
                            t.RegisterValueChangedCallback((e) => SData.DefaultObject = e.newValue);
                            break;
                        }
                    default:
                        // uhhhh
                        break;
                }

                entry.Q<Button>("DelBT").clicked += () =>
                {
                    myMan.Vars = myMan.Vars.Where(v => v != SData).ToArray();
                    foreach (var bowl in myMan.GetComponentsInChildren<SerializedBowl>(true))
                        foreach (var node in bowl.NodeDatas)
                            foreach (var input in node.DataInputs)
                                if (input.EditorConstName == SData.Name)
                                {
                                    input.EditorConstName = "";
                                    EditorUtility.SetDirty(bowl);
                                    PrefabUtility.RecordPrefabInstancePropertyModifications(bowl);
                                    if (input.UI != null && !PrefabUtility.IsPartOfAnyPrefab(bowl))
                                        input.UI.GetFirstAncestorOfType<UltNoodleBowlUI>().Validate();
                                }
                    RegenList();
                };
                entry.Q<Button>("EnforceBT").clicked += () => 
                {
                    foreach (var bowl in myMan.GetComponentsInChildren<SerializedBowl>(true))
                    {
                        bool needsComp = false;
                        foreach (var node in bowl.NodeDatas)
                            foreach (var input in node.DataInputs)
                                if (input.Source == null && input.EditorConstName == SData.Name && input.Type.Type == SData.Type.Type)
                                {
                                    input.ValDefs = SData.ValDefs;
                                    needsComp = true;
                                    EditorUtility.SetDirty(bowl);
                                    PrefabUtility.RecordPrefabInstancePropertyModifications(bowl);
                                    if (input.UI != null && !PrefabUtility.IsPartOfAnyPrefab(bowl))
                                        input.UI.GetFirstAncestorOfType<UltNoodleBowlUI>().Validate();
                                }
                        if (needsComp)
                        bowl.Compile();
                    }
                };
                VarList.Add(entry);
            }
        }
        private Dictionary<PersistentArgumentType, Type> Typz = new Dictionary<PersistentArgumentType, Type>()
        {
            { PersistentArgumentType.Int, typeof(int)},
            { PersistentArgumentType.Float, typeof(float)},
            { PersistentArgumentType.Bool, typeof(bool) },
            { PersistentArgumentType.String, typeof(string) },
            { PersistentArgumentType.Object, typeof(UnityEngine.Object) }
        };
    }
}

#endif