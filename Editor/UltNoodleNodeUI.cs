#if UNITY_EDITOR
using NoodledEvents;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

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
        var o = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ScriptPath.Replace(".cs", ".uxml")).Instantiate().Q<UltNoodleNodeUI>();
        o.setupInternal(bowl, serializedNode);
        return o;
    }
    private void setupInternal(UltNoodleBowlUI bowl, SerializedNode serializedNode)
    {
        Bowl = bowl; Node = serializedNode;

        // create normal visual for normal nodes
        Visual = this;
        Bowl.Visual.Q("Nodes").Add(Visual);
        if (serializedNode.NoadType == SerializedNode.NodeType.BowlInOut)
        {
            this.Q<Button>("DeleteBT").visible = false;
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
        Visual.RegisterCallback<DetachFromPanelEvent>(OnDisable);

        var topLeftPuller = Visual.Q("UpperLeftPull");
        bool drag = false;
        topLeftPuller.RegisterCallback<MouseDownEvent>((evt) =>
        {
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
            drag = false;
            NodeBG.ReleaseMouse();
        });

        this.Q<Button>("DeleteBT").clicked += () =>
        {
            Bowl.SerializedData.NodeDatas.Remove(Node);
            Bowl.Validate();
        };
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

    public static string ScriptPath
        => s_scriptPath ??= AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets($"t:Script {nameof(UltNoodleNodeUI)}")[0]);
    private static string s_scriptPath;

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


            var arrow = new VisualElement();
            arrow.style.position = Position.Absolute;
            arrow.style.backgroundImage = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Noodled-Events/Editor/UI Assets/arrow.png");
            arrow.style.width = 10;
            arrow.style.height = 10;
            arrow.style.top = 10;
            arrow.style.left = -5;
            disp.Add(arrow);

            input.UI = arrow;

            // Inform the bowl about drags
            input.UI.RegisterCallback<MouseEnterEvent>(e => input.HasMouse = true); input.UI.RegisterCallback<MouseLeaveEvent>(e => input.HasMouse = false);
            input.UI.RegisterCallback<MouseDownEvent>(e => { if (e.button == 0) (Bowl.CurHoveredFlowInput = input).UI.CaptureMouse(); });
            input.UI.RegisterCallback<MouseMoveEvent>(e => Bowl.MousePos = Bowl.NodeBG.WorldToLocal(e.mousePosition));
            input.UI.RegisterCallback<MouseUpEvent>(e =>
            { if (input.UI.HasMouseCapture()) { Bowl.ConnectNodes(); input.UI.ReleaseMouse(); Bowl.CurHoveredFlowInput = null; } });
            // bowl's been informed :)

            var label = new Label(input.Name);
            label.style.paddingLeft = 7;
            label.style.paddingTop = 6;
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

            var circle = new VisualElement();
            circle.style.position = Position.Absolute;
            circle.style.backgroundColor = new Color(0.5176471f, 0.5176471f, 0.5176471f, 1f);
            circle.style.borderRightColor = circle.style.borderLeftColor = circle.style.borderTopColor = circle.style.borderBottomColor = Color.black;
            circle.style.borderBottomLeftRadius = circle.style.borderBottomRightRadius = circle.style.borderTopRightRadius = circle.style.borderTopLeftRadius = 4;
            circle.style.borderRightWidth = circle.style.borderLeftWidth = circle.style.borderTopWidth = circle.style.borderBottomWidth = 3;
            circle.style.width = 10;
            circle.style.height = 10;
            circle.style.top = 10;
            circle.style.right = -5;
            disp.Add(circle);

            var label = new Label(output.Name);
            label.style.paddingTop = 6;
            label.style.height = 22;
            label.style.unityTextAlign = TextAnchor.MiddleRight;
            label.style.right = 7;
            disp.Add(label);

            output.UI = circle;

            // Inform the bowl about drags
            output.UI.RegisterCallback<MouseEnterEvent>(e => output.HasMouse = true); output.UI.RegisterCallback<MouseLeaveEvent>(e => output.HasMouse = false);
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