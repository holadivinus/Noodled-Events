#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UltEvents;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace NoodledEvents.Assets.Noodled_Events
{
    [CustomEditor(typeof(VarMan))]
    public class VarManInspector : Editor
    {
        [UnityEngine.SerializeField] public VisualTreeAsset UI;
        [UnityEngine.SerializeField] public VisualTreeAsset UI_ListElement;

        public VarMan myMan;
        private bool _settings;
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

            var title = myInspector.Q<Label>("Title");
            myInspector.Q<ColorField>("FillSetting").RegisterValueChangedCallback(e => title.style.color = e.newValue);
            myInspector.Q<ColorField>("OutlineSetting").RegisterValueChangedCallback(e => title.style.unityTextOutlineColor = e.newValue);
            myInspector.Q<IntegerField>("SizeSetting").RegisterValueChangedCallback(e => title.style.fontSize = e.newValue);

            title.style.color = myMan.TextFillColor;
            title.style.unityTextOutlineColor = myMan.TextOutlineColor;
            title.style.fontSize = myMan.TextFontSize;

            myInspector.Q<ColorField>("VarsBGColor").RegisterValueChangedCallback(e => VarList.style.backgroundColor = e.newValue);
            VarList.style.backgroundColor = myMan.VarsBGColor;

            myInspector.Q<Button>("ConfigBT").clicked += () =>
            {
                _settings = !_settings;
                myInspector.Q("TitleSettings").style.display = _settings ? DisplayStyle.Flex : DisplayStyle.None;
                myInspector.Q("VarListSettings").style.display = _settings ? DisplayStyle.Flex : DisplayStyle.None;

                RegenList();
            };

            Action errorCheck = null;
            myInspector.RegisterCallback<MouseEnterEvent>(evt => (errorCheck = () =>
            {
                // Turns out we can compile without the editor. Cool!
                bool loaded = true;// ((UltNoodleEditor.AllBooks?.Length ?? 0) != 0 && (UltNoodleEditor.AllNodeDefs?.Count ?? 0) != 0);
                bool inPrefab = PrefabUtility.IsPartOfPrefabInstance(myMan);

                //myInspector.Q("ErrorPage").style.display = loaded ? DisplayStyle.None : DisplayStyle.Flex;
                myInspector.Q("ErrorPage2").style.display = inPrefab ? DisplayStyle.Flex : DisplayStyle.None;
                myInspector.Q("NormalStuff").style.display = (loaded && !inPrefab) ? DisplayStyle.Flex : DisplayStyle.None;
            }).Invoke());
            myInspector.Q<Button>("OpenEditorBT").clicked += () =>
            {
                UltNoodleEditor.OpenWindow();
            };
            myInspector.Q<Button>("UnpackBT").clicked += () =>
            {
                PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetNearestPrefabInstanceRoot(myMan), PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction);
                if (myMan.TurnOnHideBowls)
                    myMan.HideBowls = true;
                errorCheck.Invoke();
            };

            myInspector.Q<Toggle>("HideBowls").RegisterValueChangedCallback(e =>
            {
                UltNoodleEditor.Editor?.GetType().GetMethod("OnFocus", UltEventUtils.AnyAccessBindings).Invoke(UltNoodleEditor.Editor, new object[] { });
            });

            RegenList();
            // Add a simple label.
            // Return the finished Inspector UI.
            return myInspector;
        }
        private void AutoEnforce()
        {
            if (myMan.AutoEnforce)
                foreach (var act in _forceClickers.Values)
                    act.Invoke();
        }
        public void RegenList()
        {
            VarList.Clear(); _forceClickers.Clear();
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
                            t.RegisterValueChangedCallback((e) => { SData.DefaultBoolValue = e.newValue; AutoEnforce(); });
                            break;
                        }
                    case UltEvents.PersistentArgumentType.String:
                        {
                            var t = new TextField("");
                            vfr.Add(t);
                            t.value = SData.DefaultStringValue;
                            t.RegisterValueChangedCallback((e) => {SData.DefaultStringValue = e.newValue; AutoEnforce(); });
                            break;
                        }
                    case UltEvents.PersistentArgumentType.Int:
                        {
                            var t = new IntegerField("");
                            vfr.Add(t);
                            t.value = SData.DefaultIntValue;
                            t.RegisterValueChangedCallback((e) => {SData.DefaultIntValue = e.newValue; AutoEnforce(); });
                            break;
                        }
                    case UltEvents.PersistentArgumentType.Float:
                        {
                            var t = new FloatField("");
                            vfr.Add(t);
                            t.value = SData.DefaultFloatValue;
                            t.RegisterValueChangedCallback((e) => {SData.DefaultFloatValue = e.newValue; AutoEnforce(); });
                            break;
                        }
                    case UltEvents.PersistentArgumentType.Object:
                        {
                            var t = new ObjectField("");
                            t.objectType = typeof(UnityEngine.Object);
                            vfr.Add(t);
                            t.value = SData.DefaultObject;
                            t.RegisterValueChangedCallback((e) => {SData.DefaultObject = e.newValue; AutoEnforce(); });
                            break;
                        }
                    default:
                        // uhhhh
                        break;
                }

                if (_settings)
                    entry.Q<Button>("DelBT").style.display = DisplayStyle.Flex;

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
                                    if (!PrefabUtility.IsPartOfAnyPrefab(bowl) && UltNoodleEditor.Editor != null)
                                        UltNoodleEditor.Editor.Bowls.FirstOrDefault(b => b.SerializedData == bowl)?.Validate();
                                }
                    RegenList();
                };
                var bt = entry.Q<Button>("EnforceBT");
                if (myMan.AutoEnforce)
                    bt.style.display = DisplayStyle.None;

                Action btAct = () => 
                {
                    foreach (var bowl in myMan.GetComponentsInChildren<SerializedBowl>(true))
                    {
                        bool needsComp = false;
                        foreach (var node in bowl.NodeDatas)
                            foreach (var input in node.DataInputs)
                                if (input.Source == null && input.EditorConstName == SData.Name && input.Type.Type == SData.Type.Type)
                                {
                                    input.ValDefs = SData.ValDefs;
                                    input.DefaultStringValue = SData.DefaultStringValue;
                                    input.DefaultObject = SData.DefaultObject;
                                    needsComp = true;
                                    EditorUtility.SetDirty(bowl);
                                    PrefabUtility.RecordPrefabInstancePropertyModifications(bowl);
                                    if (!PrefabUtility.IsPartOfAnyPrefab(bowl) && UltNoodleEditor.Editor != null)
                                        UltNoodleEditor.Editor.Bowls.FirstOrDefault(b => b.SerializedData == bowl)?.Validate();
                                }
                        if (needsComp)
                        bowl.Compile();
                    }
                };
                entry.Q<Button>("EnforceBT").clicked += btAct;
                _forceClickers[bt] = btAct;
                VarList.Add(entry);
            }
        }
        private Dictionary<Button, Action> _forceClickers = new();
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