#if UNITY_EDITOR
using NoodledEvents;
using NoodledEvents.Assets.Noodled_Events;
using System;
using System.Collections.Generic;
using System.Linq;
using UltEvents;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;


public class UltNoodleBowlUI : VisualElement
{
    public new class UxmlFactory : UxmlFactory<UltNoodleBowlUI, UxmlTraits> { }

    public static UltNoodleBowlUI CurrentBowlUI;
    public static bool EvtIsExecRn;

    // when a connection drag is happening, the source is here and not null
    public NoodleFlowInput CurHoveredFlowInput;
    public NoodleFlowOutput CurHoveredFlowOutput;
    public NoodleDataInput CurHoveredDataInput;
    public NoodleDataOutput CurHoveredDataOutput;
    VisualElement ConnectionLine;

    public VarMan[] VarMans = new VarMan[0];
    public NoodleDataInput[] VarManVars = new NoodleDataInput[0];
    public void ConnectNodes() // looks at the above sources, finds the not null and connects it with the target
    {
        EditorApplication.delayCall += Validate;

        // on drag release, search for the targ
        if (CurHoveredFlowInput != null)
        {
            foreach (var nd in SerializedData.NodeDatas)
                foreach (var fo in nd.FlowOutputs)
                {
                    if (!fo.HasMouse) continue;
                    fo.Connect(CurHoveredFlowInput); // this is the one
                    return;
                }

            Editor.ResetSearchFilter();
            Editor.OpenSearchMenu();
        }
        else if (CurHoveredFlowOutput != null)
        {
            foreach (var nd in SerializedData.NodeDatas)
                foreach (var fi in nd.FlowInputs)
                {
                    if (!fi.HasMouse) continue;
                    fi.Connect(CurHoveredFlowOutput); // this is the one
                    return;
                }
            Editor.ResetSearchFilter();
            Editor.OpenSearchMenu();
        }
        else if (CurHoveredDataInput != null)
        {
            foreach (var nd in SerializedData.NodeDatas)
                foreach (var @do in nd.DataOutputs)
                {
                    if (!@do.HasMouse) continue;
                    @do.Connect(CurHoveredDataInput); // this is the one
                    return;
                }

            if (CurHoveredDataInput.Type.IsValid())
            {
                Editor.SetSearchFilter(pinIn: false, CurHoveredDataInput.Type);
                Editor.OpenSearchMenu();
            }
        }
        else if (CurHoveredDataOutput != null)
        {
            foreach (var nd in SerializedData.NodeDatas)
                foreach (var di in nd.DataInputs)
                {
                    if (!di.HasMouse) continue;
                    di.Connect(CurHoveredDataOutput); // this is the one
                    return;
                }

            if (CurHoveredDataOutput.Type.IsValid())
            {
                Editor.SetSearchFilter(pinIn: true, CurHoveredDataOutput.Type);
                Editor.OpenSearchMenu();
            }
        }

        // ho ho ho :)
    }
    // Ctor :)
    public static UltNoodleBowlUI New(UltNoodleEditor editor, VisualElement parent, UnityEngine.Component eventComponent, SerializedType fieldType, string eventField)
    {
        var existing = editor.BowlUIs.FirstOrDefault(bui => bui.Component == eventComponent && bui._eventFieldPath == eventField);
        if (existing != null) return existing;
        var o = editor.UltNoodleBowlUI_UXML.Instantiate().Q<UltNoodleBowlUI>();
        o.setupInternal(editor, parent, eventComponent, fieldType, eventField);
        return o;
    }
    private SerializedType _fieldType;
    private void setupInternal(UltNoodleEditor editor, VisualElement parent, UnityEngine.Component eventComponent, SerializedType fieldType, string eventField)
    {
        Editor = editor; _fieldType = fieldType; _eventFieldPath = eventField; Component = eventComponent;

        // i'm visual
        Visual = this;
        Visual.RegisterCallback<AttachToPanelEvent>(OnEnabled);

        PathLabel = Visual.Q<Label>("PathLabel");
        PathLabel.text = "";
        NameField = Visual.Q<TextField>("BowlNameField");
        NameField.value = "";

        NodeBG = this;

        parent.Add(Visual);
        Visual.RegisterCallback<DetachFromPanelEvent>(OnDisabled);

        Visual.RegisterCallback<MouseOverEvent>(e => CurrentBowlUI = this);
        Visual.RegisterCallback<MouseOutEvent>(e => { if (CurrentBowlUI == this) CurrentBowlUI = null; });

        Visual.RegisterCallback<DragEnterEvent>((a) => { DragAndDrop.visualMode = DragAndDropVisualMode.Move; });
        Visual.RegisterCallback<DragUpdatedEvent>((a) => { DragAndDrop.visualMode = DragAndDropVisualMode.Move; });
        Visual.RegisterCallback<DragPerformEvent>((a) => 
        {
            // PLUHHHH
            Vector2 mp = a.localMousePosition;
            UnityEngine.Object targ = DragAndDrop.objectReferences[0];
            void fin(UnityEngine.Object selectee)
            {
                Type t = selectee.GetType();
                var node = AddNode(t.Name, editor.ObjectCookBook);
                node.AddDataIn(t.Name, t, selectee);
                var m = new SerializedMethod();
                m.Method = selectee.GetType().GetMethods(UltEventUtils.AnyAccessBindings).FirstOrDefault()
                ?? selectee.GetType().BaseType.GetMethods(UltEventUtils.AnyAccessBindings).First(); // not setting up a loop rn todo
                node.BookTag = JsonUtility.ToJson(m);
                node.Position = mp;
                
                Validate();
            }
            if (targ is GameObject gobj)
            {
                GenericMenu selor = new GenericMenu();
                
                selor.AddDisabledItem(new GUIContent("Select Target"), false);
                selor.AddSeparator("");
                selor.AddItem(new GUIContent("GameObject"), false, () => fin(targ));
                foreach (var comp in gobj.GetComponents<Component>())
                {
                    var c = comp;
                    selor.AddItem(new GUIContent(comp.GetType().Name), false, () => fin(c));
                }
                selor.ShowAsContext();
            }
            else fin(targ);
        });
        
        NodeBG.Q("Nodes").RegisterCallback<MouseMoveEvent>(e => MousePos = e.localMousePosition);
        ConnectionLine = this.Q("ConnectionLine");

        this.Q<Button>("BowlDeleteBT").clicked += () =>
        {
            this.parent.Remove(this);
            Editor.BowlUIs.Remove(this);
            if (_sb.LastGenerated != null)
                UnityEngine.Object.DestroyImmediate(_sb.LastGenerated);
            UnityEngine.Object.DestroyImmediate(_sb);
        };

        #region Drag Size Logic
        var lowerRightPull = Visual.Q<VisualElement>("LowerRightPull");
        bool draggingScale = false;
        var upperLeftPull = Visual.Q<VisualElement>("UpperLeftPull");
        bool draggingPos = false;

        // no clue why this fixes scaling issues
        NodeBG.style.height = NodeBG.style.left = new(new Length(-1, LengthUnit.Pixel));
        SerializedData.SizeChanged.Invoke(SerializedData.Size);
        SerializedData.PositionChanged.Invoke(SerializedData.Position);
        upperLeftPull.RegisterCallback<MouseDownEvent>((evt) =>
        {
            if (evt.button != 0) return;
            NodeBG.CaptureMouse();
            draggingPos = true;
        });
        lowerRightPull.RegisterCallback<MouseDownEvent>((evt) =>
        {
            if (evt.button != 0) return;
            NodeBG.CaptureMouse();
            draggingScale = true;
        });
        NodeBG.RegisterCallback<MouseMoveEvent>(evt =>
        {
            if (draggingScale)
                SerializedData.Size = NodeBG.parent.WorldToLocal(evt.mousePosition) - SerializedData.Position;
            if (draggingPos)
                SerializedData.Position = NodeBG.parent.WorldToLocal(evt.mousePosition);
        });
        NodeBG.RegisterCallback<MouseUpEvent>((evt) =>
        {
            if (evt.button != 0) return;
            NodeBG.ReleaseMouse();
            draggingScale = false;
            draggingPos = false;
        });
        #endregion
    }

    public SerializedNode AddNode(string name, CookBook book) 
    {
        var nod = new SerializedNode()
        {
            Bowl = SerializedData,
            NoadType = SerializedNode.NodeType.Normal,
            Book = book,
            Name = name
        };
        SerializedData.NodeDatas.Add(nod);

        // also close search
        Editor.CloseSearchMenu();
        return nod;
    }

    public Vector2 MousePos;

    private void OnEnabled(AttachToPanelEvent evt) 
    {
        // path syncup
        PathLabel.text = SerializedData.Path;
        SerializedData.PathChange += OnPathChange;

        // name syncup
        NameField.value = SerializedData.BowlName;
        NameField.RegisterValueChangedCallback(evt =>
        {
            if (SerializedData.BowlName != evt.newValue)
                SerializedData.BowlName = evt.newValue;
        });
        SerializedData.BowlNameChange += OnBowlNameChange;
        Editor.BowlUIs.Add(this);

        // Size Syncup
        SerializedData.SizeChanged += OnSizeChange;
        SerializedData.PositionChanged += OnPositionChange;

        if (SerializedData.Size == Vector2.zero)
            SerializedData.Size = new Vector2(1000, 800);
        SerializedData.OnUpdate += OnSceneUpdate;

        Validate();
        EditorApplication.update += OnEditorUpdate;
    }
    private void OnPathChange(string path) => PathLabel.text = path;
    private void OnBowlNameChange(string name) 
    {
        if (NameField.value != name)
            NameField.value = name;
    }
    private void OnSizeChange(Vector2 size)
    {
        NodeBG.style.minWidth = size.x;
        NodeBG.style.minHeight = size.y;

        foreach (var nd in NodeUIs)
        {
            nd.Node.Position = new Vector2(MathF.Min(nd.Node.Position.x, size.x), MathF.Min(nd.Node.Position.y, size.y));
            nd.UpdateLineUIs();
        }
    }
    private void OnPositionChange(Vector2 pos)
    {
        NodeBG.style.left = pos.x;
        NodeBG.style.top = pos.y;
    }
    private void OnDisabled(DetachFromPanelEvent evt) 
    {
        if (SerializedData != null)
        {
            SerializedData.PathChange -= OnPathChange;
            SerializedData.BowlNameChange -= OnBowlNameChange;
            SerializedData.SizeChanged -= OnSizeChange;
            SerializedData.PositionChanged -= OnPositionChange;
            SerializedData.OnUpdate -= OnSceneUpdate;
        }
        Editor.BowlUIs.Remove(this);
        EditorApplication.update -= OnEditorUpdate;
    }

    public List<UltNoodleNodeUI> NodeUIs = new();
    public VisualElement NodeBG;
    public Label PathLabel;
    public TextField NameField;

    private string _eventFieldPath;
    private SerializedBowl _sb;
    public SerializedBowl SerializedData
    { 
        get
        {
            if (_sb == null && Component != null)
            {
                _sb = Component.GetBowlData(_fieldType, _eventFieldPath);
                PathLabel.text = _sb.Path;
                _sb.PathChange += (newPath) => PathLabel.text = newPath;
            }
            if (_sb != null) _sb.BowlEvtHolderType = _fieldType; // backwards compat for pre BowlEvtHolderType bowlz
            return _sb;
        }
    }
    public UltNoodleEditor Editor;
    public void Validate() // validate this bowl UI and its nodes
    {
        if (Component == null //kms if no evt
        || (Selection.activeGameObject != SerializedData.gameObject && EditorPrefs.GetBool("SelectedBowlsOnly", true)) // kms if unselected
        || !(PrefabStageUtility.GetCurrentPrefabStage()?.IsPartOfPrefabContents(SerializedData.gameObject) ?? true) ) // kms if not in a prefab when prefab mode is active
        {
            Visual?.parent?.Remove(Visual);
            Editor.BowlUIs.Remove(this);
        } else
        {
            VarMans = SerializedData.GetComponentsInParent<VarMan>(true);
            if (VarMans.Any(v => v.HideBowls))
            {
                Visual?.parent?.Remove(Visual);
                Editor.BowlUIs.Remove(this);
            }
            VarManVars = VarMans.SelectMany(vm => vm.Vars).ToArray();
        }
        foreach (var nodeUI in NodeUIs.ToArray()) // validate my nodeUIs
            nodeUI.Validate();
        foreach (SerializedNode node in SerializedData.NodeDatas)
            if (!NodeUIs.Any(ui => ui.Node == node)) // if a node doesn't have a ui, create the ui
                UltNoodleNodeUI.New(this, node);
    }
    public void OnSceneUpdate()
    {
        foreach (var node in NodeUIs.ToArray())
            node.Validate();
    }
    public void OnEditorUpdate()
    {
        if (ConnectionLine != null)
        {
            // draglining
            ConnectionLine.visible = false;
            foreach (var ui in new[] { CurHoveredFlowInput?.UI, CurHoveredFlowOutput?.UI, CurHoveredDataInput?.UI, CurHoveredDataOutput?.UI })
                if (ui != null)
                {
                    ConnectionLine.visible = true;
                    Vector2 sourceLoc = ConnectionLine.parent.WorldToLocal(ui.LocalToWorld(Vector2.zero));
                    //ConnectionLine.style.transformOrigin = new TransformOrigin(0, new Length(.5f, LengthUnit.Percent));
                    ConnectionLine.style.left = sourceLoc.x + 8;
                    ConnectionLine.style.top = sourceLoc.y + 7f;
                    float x = MousePos.x - sourceLoc.x - 8;
                    float y = MousePos.y - sourceLoc.y - 7f;

                    ConnectionLine.style.width = Mathf.Sqrt((x*x) + (y*y));
                    ConnectionLine.style.rotate = new Rotate(Mathf.Rad2Deg * Mathf.Atan2(y, x));

                    // also move type text, just in case
                    UltNoodleEditor.TypeHinter.style.left = this.LocalToWorld(MousePos).x;
                    UltNoodleEditor.TypeHinter.style.top = this.LocalToWorld(MousePos).y;
                    break;
                }
        }
    }

    public UltEventBase Event => SerializedData.Event;
    public UnityEngine.Component Component;
    public VisualElement Visual;
}
#endif

