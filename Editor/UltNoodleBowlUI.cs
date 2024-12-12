#if UNITY_EDITOR
using NoodledEvents;
using System.Collections.Generic;
using System.Linq;
using UltEvents;
using UnityEditor;
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

            _editor.ResetSearchFilter();
            _editor.OpenSearchMenu();
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
            _editor.ResetSearchFilter();
            _editor.OpenSearchMenu();
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
            _editor.SetSearchFilter(pinIn: false, CurHoveredDataInput.Type);
            _editor.OpenSearchMenu();
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
            _editor.SetSearchFilter(pinIn: true, CurHoveredDataOutput.Type);
            _editor.OpenSearchMenu();
        }

        // ho ho ho :)
    }
    // Ctor :)
    public static UltNoodleBowlUI New(UltNoodleEditor editor, VisualElement parent, UnityEngine.Component eventComponent, SerializedType fieldType, string eventField)
    {
        var o = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ScriptPath.Replace(".cs", ".uxml")).Instantiate().Q<UltNoodleBowlUI>();
        o.setupInternal(editor, parent, eventComponent, fieldType, eventField);
        return o;
    }
    private SerializedType _fieldType;
    private void setupInternal(UltNoodleEditor editor, VisualElement parent, UnityEngine.Component eventComponent, SerializedType fieldType, string eventField)
    {
        _editor = editor; _fieldType = fieldType; _eventFieldPath = eventField; Component = eventComponent;

        // i'm visual
        Visual = this; //AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ScriptPath.Replace(".cs", ".uxml")).Instantiate();
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
            _editor.SetSearchFilter(true, DragAndDrop.objectReferences[0].GetType());
            MousePos = a.mousePosition;
            _editor.OpenSearchMenu(true); 
        });
        
        NodeBG.Q("Nodes").RegisterCallback<MouseMoveEvent>(e => MousePos = e.localMousePosition);
        ConnectionLine = this.Q("ConnectionLine");

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
            NodeBG.CaptureMouse();
            draggingPos = true;
        });
        lowerRightPull.RegisterCallback<MouseDownEvent>((evt) =>
        {
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
        _editor.BowlUIs.Add(this);

        // Size Syncup
        SerializedData.SizeChanged += OnSizeChange;
        SerializedData.PositionChanged += OnPositionChange;

        if (SerializedData.Size == Vector2.zero)
            SerializedData.Size = new Vector2(300, 100);
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
        _editor.BowlUIs.Remove(this);
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
    private UltNoodleEditor _editor;
    public void Validate() // validate this bowl UI and its nodes
    {
        if (Component == null) //kms if no evt
        {
            Visual?.parent?.Remove(Visual);
            _editor.BowlUIs.Remove(this);
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
                    ConnectionLine.style.left = MousePos.x;
                    ConnectionLine.style.top = MousePos.y;
                    break;
                }
        }
    }
    public static string ScriptPath
        => s_scriptPath ??= AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets($"t:Script {nameof(UltNoodleBowlUI)}")[0]);
    private static string s_scriptPath;

    public UltEventBase Event => SerializedData.Event;
    public UnityEngine.Component Component;
    public VisualElement Visual;
}
#endif

