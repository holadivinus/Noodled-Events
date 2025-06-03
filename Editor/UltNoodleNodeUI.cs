#if UNITY_EDITOR
using NoodledEvents;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static NoodledEvents.CookBook;

/// <summary>
/// Class the puppeteers a VisualElement to represent and edit a Node.
/// Visual is only setup once; If the Node's method changes, the UI is deleted
/// and replaced with a new one.
/// </summary>
public class UltNoodleNodeUI : VisualElement
{
    public new class UxmlFactory : UxmlFactory<UltNoodleNodeUI, UxmlTraits> { }

    public static UltNoodleNodeUI New(UltNoodleBowlUI bowl, SerializedNode serializedNode)
    {
        var o = bowl.Editor.UltNoodleNodeUI_UXML.Instantiate().Q<UltNoodleNodeUI>();
        o.setupInternal(bowl, serializedNode);
        return o;
    }
    private void setupInternal(UltNoodleBowlUI bowl, SerializedNode serializedNode)
    {
        Bowl = bowl; Node = serializedNode;

        // create normal visual for normal nodes
        Visual = this;
        Bowl.Visual.Q("Nodes").Add(Visual);
        

        // hide Delete BT only if it's the "InOut node
        this.Q<Button>("DeleteBT").visible = serializedNode.NoadType != SerializedNode.NodeType.BowlInOut;
        this.Q<Button>("DuplicateBT").visible = serializedNode.NoadType != SerializedNode.NodeType.BowlInOut;

        // Create Invoke BT only if that's possible (aka the evt doesn't need inputs.)
        if (serializedNode.NoadType == SerializedNode.NodeType.BowlInOut && bowl.Event.GetType().GetEvtGenerics().Length == 0)
        {
            var inv = this.Q<Button>("Invoke");
            inv.visible = true;
            inv.clicked += () =>
            {
                //compile and run evt
                bowl.SerializedData.Compile();

                UltNoodleBowlUI.EvtIsExecRn = true;
                bowl.SerializedData.Event.DynamicInvoke();
                UltNoodleBowlUI.EvtIsExecRn = false;

                // recompile to reset state
                bowl.SerializedData.Compile();
            };
        }

        Bowl.NodeUIs.Add(this);
        NodeBG = Visual.Q("BG");


        //UIMethod = Node.Method;
        if (serializedNode.NoadType == SerializedNode.NodeType.Normal)
            (Title = Visual.Q<Label>("TypeTitle")).text = serializedNode.Name;
        InputsElement = Visual.Q<VisualElement>("Inputs");
        OutputsElement = Visual.Q<VisualElement>("Outputs");

        GenerateInputs();


        Node.PositionChanged += PositionChanged;
        Node.PositionChanged.Invoke(Node.Position);
        Visual.RegisterCallback<AttachToPanelEvent>(OnEnable);
        Visual.RegisterCallback<DetachFromPanelEvent>(OnDisable);

        var topLeftPuller = Visual.Q("UpperLeftPull");
        bool drag = false;
        topLeftPuller.RegisterCallback<MouseDownEvent>((evt) =>
        {
            if (evt.button != 0) return;
            NodeBG.CaptureMouse();
            drag = true;
        });
        NodeBG.RegisterCallback<MouseMoveEvent>((evt) =>
        {
            if (drag)
            {
                Node.Position = NodeBG.parent.WorldToLocal(evt.mousePosition);
                UpdateLineUIs();
            }
        });
        NodeBG.RegisterCallback<MouseUpEvent>((evt) =>
        {
            if (evt.button != 0) return;
            drag = false;
            NodeBG.ReleaseMouse();
        });

        this.Q<Button>("DeleteBT").clicked += () =>
        {
            Bowl.SerializedData.NodeDatas.Remove(Node);
            Bowl.Validate();
        };
        this.Q<Button>("DuplicateBT").clicked += () =>
        {
            var newnode = new SerializedNode().CopyFrom(serializedNode);
            serializedNode.Bowl.NodeDatas.Add(newnode);
            newnode.Position = serializedNode.Position + (Vector2.up * (this.resolvedStyle.height+10));
            Bowl.Validate();
        };
        
        if (Node.Book != null) // ignore special IO node
        {
            // ON DROPDOWN MOUSEOVER,
            // we'll ask each availiable book if they'd like to make an entry for this node!

            var dropper = new DropdownField("");
            dropper[0].style.maxWidth = 15;
            dropper[0].style.minWidth = 15;
            dropper.style.marginLeft = 0;
            this.Q("TypeTitle").parent.Add(dropper);

            bool setup = false;
            dropper.RegisterCallback<MouseOverEvent>((evt) =>
            {
                if (setup) return;    
                setup = true;

                foreach (CookBook opsgjd in UltNoodleEditor.AllBooks)
                {
                    CookBook curBook = opsgjd;
                    Dictionary<string, NodeDef> entries = curBook.GetAlternatives(Node);
                    if (entries == null || entries.Count == 0) continue;

                    foreach (var item in entries)
                    {
                        dropper.choices.Add(item.Key);
                    }
                    dropper.RegisterValueChangedCallback((evt) => 
                    {
                        if (evt.newValue == null) return;
                        dropper.index = -1;
                        if (entries.TryGetValue(evt.newValue, out NodeDef def))
                        {
                            //Debug.Log(evt.newValue);
                            var newNod = bowl.AddNode(def.Name, curBook).MatchDef(def);
                            newNod.BookTag = def.BookTag != string.Empty ? def.BookTag : def.Name;

                            newNod.Position = Node.Position;
                            Bowl.SerializedData.NodeDatas.Remove(Node);
                            bowl.Validate();
                            curBook.SwapConnections(Node, newNod);
                            // this should probably be handled elsewhere, TODO
                            foreach (var ip in bowl.NodeUIs.First(nui => nui.Node == newNod).InPoints)
                                if (ip.MyField is ObjectField of)
                                    of.value = ip.SData.DefaultObject;
                        }
                    });
                }
            });
        }
    }
    private void OnEnable(AttachToPanelEvent evt)
    {
        Node.PositionChanged += PositionChanged;
    }
    private void PositionChanged(Vector2 position)
    {
        NodeBG.style.left = position.x;
        NodeBG.style.top = position.y;
    }
    private void OnDisable(DetachFromPanelEvent evt)
    {
        Node.PositionChanged -= PositionChanged;
    }
    public UltNoodleBowlUI Bowl;
    public SerializedNode Node;

    public Label Title;
    private VisualElement NodeBG;
    public VisualElement InputsElement;
    public VisualElement OutputsElement;

    public List<UltNoodleFlowOutPoint> OutPoints = new();
    public List<UltNoodleDataInPoint> InPoints = new();


    public VisualElement Visual;
    private void GenerateInputs()
    {
        // Nodes should handle their "n" connectors themselves
        // simply because a custom class for this is bull
        // additionally todo, add an ordering system so data and flows arent grouped

        foreach (NoodleFlowInput input in Node.FlowInputs)
        {
            var disp = new VisualElement();
            disp.style.height = 27;
            disp.style.borderBottomColor = new Color(0.2627451f, 0.2627451f, 0.2627451f, 1f);
            disp.style.borderBottomWidth = 1;
            disp.pickingMode = PickingMode.Ignore;


            var col = new VisualElement();
            col.style.position = Position.Absolute;
            //arrow.style.backgroundImage = Bowl.Editor.ArrowPng;
            col.style.width = 20;
            col.style.height = 20;
            col.style.top = 4;
            col.style.left = -10;
            disp.Add(col);

            var ico = new VisualElement();
            ico.style.position = Position.Absolute;
            ico.style.backgroundImage = Bowl.Editor.ArrowPng;
            ico.pickingMode = PickingMode.Ignore;
            ico.style.width = 10;
            ico.style.height = 10;
            ico.style.top = 5;
            ico.style.left = 5;
            col.Add(ico);

            input.UI = col;

            // Inform the bowl about drags
            input.UI.RegisterCallback<MouseEnterEvent>(e => input.HasMouse = true); input.UI.RegisterCallback<MouseLeaveEvent>(e => input.HasMouse = false);
            input.UI.RegisterCallback<MouseDownEvent>(e => { if (e.button == 0) (Bowl.CurHoveredFlowInput = input).UI.CaptureMouse(); });
            input.UI.RegisterCallback<MouseMoveEvent>(e => Bowl.MousePos = Bowl.NodeBG.WorldToLocal(e.mousePosition));
            input.UI.RegisterCallback<MouseUpEvent>(e =>
            { if (input.UI.HasMouseCapture()) { Bowl.ConnectNodes(); input.UI.ReleaseMouse(); Bowl.CurHoveredFlowInput = null; } });
            // bowl's been informed :)

            var label = new Label(input.Name);
            label.style.paddingTop = 6;
            label.style.left = 5;
            label.style.height = 22;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            disp.Add(label);

            InputsElement.Add(disp);
        }

        foreach (NoodleFlowOutput output in Node.FlowOutputs)
            UltNoodleFlowOutPoint.New(this, output);

        foreach (NoodleDataInput input in Node.DataInputs)
            UltNoodleDataInPoint.New(this, input);

        foreach (NoodleDataOutput output in Node.DataOutputs)
        {
            var disp = new VisualElement();
            disp.style.height = 27;
            disp.style.borderBottomColor = new Color(0.2627451f, 0.2627451f, 0.2627451f, 1f);
            disp.style.borderBottomWidth = 1;

            var col = new VisualElement();
            col.style.position = Position.Absolute;
            col.style.width = 20;
            col.style.height = 20;
            col.style.top = 4;
            col.style.right = -10;
            disp.Add(col);

            var circ = new VisualElement();
            circ.style.position = Position.Absolute;
            circ.pickingMode = PickingMode.Ignore;
            circ.style.backgroundColor = new Color(0.5176471f, 0.5176471f, 0.5176471f, 1f);
            circ.style.borderRightColor = circ.style.borderLeftColor = circ.style.borderTopColor = circ.style.borderBottomColor = Color.black;
            circ.style.borderBottomLeftRadius = circ.style.borderBottomRightRadius = circ.style.borderTopRightRadius = circ.style.borderTopLeftRadius = 4;
            circ.style.borderRightWidth = circ.style.borderLeftWidth = circ.style.borderTopWidth = circ.style.borderBottomWidth = 3;
            circ.style.width = 10;
            circ.style.height = 10;
            circ.style.top = 5;
            circ.style.right = 5;
            col.Add(circ);


            var label = new Label(output.Name);
            label.style.paddingTop = 6;
            label.style.height = 22;
            label.style.unityTextAlign = TextAnchor.MiddleRight;
            label.style.right = 7;
            disp.Add(label);

            output.UI = col;

            // Inform the bowl about drags
            output.UI.RegisterCallback<MouseEnterEvent>(e => { output.HasMouse = true; UltNoodleEditor.TypeHinter.visible = true; UltNoodleEditor.TypeHinter.text = output.Type.Type.GetFriendlyName(); });
            output.UI.RegisterCallback<MouseLeaveEvent>(e => { output.HasMouse = false; UltNoodleEditor.TypeHinter.visible = false; });
            output.UI.RegisterCallback<MouseDownEvent>(e => { if (e.button == 0) (Bowl.CurHoveredDataOutput = output).UI.CaptureMouse(); });
            output.UI.RegisterCallback<MouseMoveEvent>(e => Bowl.MousePos = Bowl.NodeBG.WorldToLocal(e.mousePosition));
            output.UI.RegisterCallback<MouseUpEvent>(e =>
            { if (output.UI.HasMouseCapture()) { Bowl.ConnectNodes(); output.UI.ReleaseMouse(); Bowl.CurHoveredDataOutput = null; } });
            // bowl's been informed :)

            OutputsElement.Add(disp);
        }

    }

    public bool Dead;
    private int _lastIdx;
    private MethodBase UIMethod; // Method represented by the ui
    public void Validate() // this gets called each scene update
    {
        if (Dead) return;
        if (Bowl.Component == null)
        {
            Dead = true;
            Visual.parent.Remove(Visual);
            Bowl.NodeUIs.Remove(this);
            Bowl.Validate();
            return;
        }

       
        if (!Bowl.SerializedData.NodeDatas.Contains(Node)) // method changed, delete so new one is made with correct method
        {
            Dead = true;
            Visual.parent.Remove(Visual);
            Bowl.NodeUIs.Remove(this);
            Bowl.Validate();
            return;
        }
        Node.OnBeforeSerialize(); // called so that the connections are good 
        foreach (var ip in InPoints)
            ip.Validate();
        UpdateLineUIs();
    }

    public void UpdateLineUIs()
    {
        foreach (var fi in Node.FlowInputs)
            foreach (var s in fi.Sources)
                ((UltNoodleFlowOutPoint)s.UI.parent).UpdateLine();

        foreach (var fi in Node.DataOutputs)
            foreach (var t in fi.Targets)
                ((UltNoodleDataInPoint)t.UI.parent).UpdateLine();

        foreach (var op in OutPoints)
            op.UpdateLine();
        foreach (var ip in InPoints)
            ip.UpdateLine();
    }
}
#endif