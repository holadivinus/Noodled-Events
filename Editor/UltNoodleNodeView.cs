using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using NoodledEvents;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UltEvents;
using System.Linq;
using UnityEditor;

public class UltNoodleNodeView : Node
{
    public SerializedNode Node { get; }
    public Action<UltNoodleNodeView> OnNodeSelected;

    private readonly Dictionary<string, Port> _flowInputs = new();
    private readonly Dictionary<string, Port> _flowOutputs = new();
    private readonly Dictionary<string, Port> _dataInputs = new();
    private readonly Dictionary<string, Port> _dataOutputs = new();

    public UltNoodleNodeView(SerializedNode node)
    {
        this.Node = node;
        this.title = node.Name;
        this.viewDataKey = node.ID;

        style.left = node.Position.x;
        style.top = node.Position.y;

        if (node.NoadType == SerializedNode.NodeType.BowlInOut)
        {
            this.capabilities &= ~Capabilities.Deletable; // cannot delete the primary node
            this.capabilities &= ~Capabilities.Copiable;  // cannot copy the primary node
        }

        CreateFlowPorts();
        CreateDataPorts();

        RegisterCallback<MouseDownEvent>(OnMouseDown);
    }

    private void OnMouseDown(MouseDownEvent evt)
    {
        // workaround for OnGraphViewChanged calling drags many times during a move
        if (evt.button == 0)
            Undo.RegisterCompleteObjectUndo(Node.Bowl, "Move Node");
    }

    private void CreateFlowPorts()
    {
        foreach (var fi in Node.FlowInputs)
        {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
            port.portName = string.IsNullOrEmpty(fi.Name) ? "Flow In" : fi.Name;
            port.userData = fi;
            _flowInputs[fi.ID] = port;
            inputContainer.Add(port);
        }

        foreach (var fo in Node.FlowOutputs)
        {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
            port.portName = string.IsNullOrEmpty(fo.Name) ? "Flow Out" : fo.Name;
            port.userData = fo;

            var listener = new UltNoodleEdgeConnectorListener(UltNoodleEditor.Editor.TreeView);
            var connector = new EdgeConnector<Edge>(listener);
            port.AddManipulator(connector);

            _flowOutputs[fo.ID] = port;
            outputContainer.Add(port);
        }
    }

    private void CreateDataPorts()
    {
        foreach (var di in Node.DataInputs)
        {
            var port = InstantiatePort(
                Orientation.Horizontal,
                Direction.Input,
                Port.Capacity.Single,
                di.Type?.Type ?? typeof(object)
            );
            port.portName = string.IsNullOrEmpty(di.Name) ? di.Type?.Type.Name ?? "In" : di.Name;
            port.userData = di;
            _dataInputs[di.ID] = port;

            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.Add(port);

            VisualElement field = CreateFieldForType(di);
            if (field != null)
            {
                field.name = "ConstantField";
                field.SetEnabled(!port.connected);
                container.Add(field);
            }

            inputContainer.Add(container);
        }

        foreach (var dout in Node.DataOutputs)
        {
            var port = InstantiatePort(
                Orientation.Horizontal,
                Direction.Output,
                Port.Capacity.Multi,
                dout.Type?.Type ?? typeof(object)
            );
            port.portName = string.IsNullOrEmpty(dout.Name) ? dout.Type?.Type.Name ?? "Out" : dout.Name;
            port.userData = dout;

            var listener = new UltNoodleEdgeConnectorListener(UltNoodleEditor.Editor.TreeView);
            var connector = new EdgeConnector<Edge>(listener);
            port.AddManipulator(connector);

            _dataOutputs[dout.ID] = port;
            outputContainer.Add(port);
        }
    }

    private VisualElement CreateFieldForType(NoodleDataInput input)
    {
        Type type = input.Type?.Type ?? typeof(object);
        PersistentArgumentType fauxType = input.ConstInput;
        VisualElement field = null;

        if (type == typeof(float) || fauxType == PersistentArgumentType.Float)
        {
            var floatField = new FloatField();
            floatField.value = input.DefaultFloatValue;
            floatField.RegisterValueChangedCallback(evt => input.DefaultFloatValue = evt.newValue);
            field = floatField;
        }
        else if (type == typeof(int) || fauxType == PersistentArgumentType.Int)
        {
            var intField = new IntegerField();
            intField.value = input.DefaultIntValue;
            intField.RegisterValueChangedCallback(evt => input.DefaultIntValue = evt.newValue);
            field = intField;
        }
        else if (type == typeof(bool) || fauxType == PersistentArgumentType.Bool)
        {
            var toggle = new Toggle();
            toggle.value = input.DefaultBoolValue;
            toggle.RegisterValueChangedCallback(evt => input.DefaultBoolValue = evt.newValue);
            field = toggle;
        }
        else if (type == typeof(string) || fauxType == PersistentArgumentType.String)
        {
            var textField = new TextField();
            textField.value = input.DefaultStringValue ?? "";
            textField.RegisterValueChangedCallback(evt => input.DefaultStringValue = evt.newValue);
            field = textField;
        }
        else if (type == typeof(Vector2) || fauxType == PersistentArgumentType.Vector2)
        {
            var vectorField = new Vector2Field();
            vectorField.value = input.DefaultVector2Value;
            vectorField.RegisterValueChangedCallback(evt => input.DefaultVector2Value = evt.newValue);
            field = vectorField;
        }
        else if (type == typeof(Vector3) || fauxType == PersistentArgumentType.Vector3)
        {
            var vectorField = new Vector3Field();
            vectorField.value = input.DefaultVector3Value;
            vectorField.RegisterValueChangedCallback(evt => input.DefaultVector3Value = evt.newValue);
            field = vectorField;
        }
        else if (type == typeof(Vector4) || fauxType == PersistentArgumentType.Vector4)
        {
            var vectorField = new Vector4Field();
            vectorField.value = input.DefaultVector4Value;
            vectorField.RegisterValueChangedCallback(evt => input.DefaultVector4Value = evt.newValue);
            field = vectorField;
        }
        else if (type == typeof(Quaternion) || fauxType == PersistentArgumentType.Quaternion)
        {
            var vectorField = new Vector4Field();
            vectorField.value = input.DefaultQuaternionValue is Quaternion q ? new Vector4(q.x, q.y, q.z, q.w) : Vector4.zero;
            vectorField.RegisterValueChangedCallback(evt =>
            {
                var v = evt.newValue;
                input.DefaultQuaternionValue = new Quaternion(v.x, v.y, v.z, v.w);
            });
            field = vectorField;
        }
        else if (type == typeof(Color) || type == typeof(Color32) || fauxType == PersistentArgumentType.Color || fauxType == PersistentArgumentType.Color32)
        {
            var colorField = new ColorField();
            colorField.value = input.DefaultColorValue;
            colorField.RegisterValueChangedCallback(evt => input.DefaultColorValue = evt.newValue);
            field = colorField;
        }
        else if (type.IsEnum) // enums are not supported by faux types, at least by SerializedNode
        {
            var enumField = new EnumField();
            enumField.Init((Enum)Enum.ToObject(type, input.DefaultIntValue));
            enumField.RegisterValueChangedCallback(evt => input.DefaultIntValue = Convert.ToInt32(evt.newValue));
            field = enumField;
        }
        else if (typeof(UnityEngine.Object).IsAssignableFrom(type) || fauxType == PersistentArgumentType.Object)
        {
            var objField = new ObjectField();
            objField.objectType = type;
            objField.value = input.DefaultObject;
            objField.RegisterValueChangedCallback(evt => input.DefaultObject = evt.newValue);
            field = objField;
        }

        if (field != null)
            field.style.flexGrow = 1;

        return field;
    }

    public IEnumerable<Port> GetAllPorts()
    {
        foreach (var port in _flowInputs.Values) yield return port;
        foreach (var port in _flowOutputs.Values) yield return port;
        foreach (var port in _dataInputs.Values) yield return port;
        foreach (var port in _dataOutputs.Values) yield return port;
    }

    public Port GetPortByName(string name, Direction dir)
    {
        var search = dir == Direction.Input ? _flowInputs.Values.Concat(_dataInputs.Values) : _flowOutputs.Values.Concat(_dataOutputs.Values);
        return search.FirstOrDefault(p => p.portName == name);
    }

    public void RebuildConstantField(NoodleDataInput input)
    {
        if (!_dataInputs.TryGetValue(input.ID, out var port))
            return;

        var container = port.parent;
        if (container == null)
            return;

        var oldField = container.Q<VisualElement>("ConstantField");
        if (oldField != null)
        {
            container.Remove(oldField);
        }

        var newField = CreateFieldForType(input);
        if (newField != null)
        {
            newField.name = "ConstantField";
            newField.SetEnabled(port.connected == false);
            container.Add(newField);
        }

        RefreshExpandedState();
        RefreshPorts();
    }

    public Port GetPortForFlowInput(NoodleFlowInput input) =>
        _flowInputs.TryGetValue(input.ID, out var port) ? port : null;

    public Port GetPortForFlowOutput(NoodleFlowOutput output) =>
        _flowOutputs.TryGetValue(output.ID, out var port) ? port : null;

    public Port GetPortForDataInput(NoodleDataInput input) =>
        _dataInputs.TryGetValue(input.ID, out var port) ? port : null;

    public Port GetPortForDataOutput(NoodleDataOutput output) =>
        _dataOutputs.TryGetValue(output.ID, out var port) ? port : null;

    public override void SetPosition(Rect newPos)
    {
        base.SetPosition(newPos);
        Node.Position = newPos.position;
    }

    public override void OnSelected()
    {
        base.OnSelected();
        OnNodeSelected?.Invoke(this);
    }
}
