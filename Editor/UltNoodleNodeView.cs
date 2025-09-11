using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using NoodledEvents;
using UnityEditor.Experimental.GraphView;

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
            this.capabilities &= ~Capabilities.Deletable; // cannot delete the primary node

        CreateFlowPorts();
        CreateDataPorts();
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
            _flowOutputs[fo.ID] = port;
            outputContainer.Add(port);
        }
    }

    private void CreateDataPorts()
    {
        foreach (var di in Node.DataInputs)
        {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, di.Type?.Type ?? typeof(object));
            port.portName = string.IsNullOrEmpty(di.Name) ? di.Type?.Type.Name ?? "In" : di.Name;
            port.userData = di;
            _dataInputs[di.ID] = port;
            inputContainer.Add(port);
        }

        foreach (var dout in Node.DataOutputs)
        {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, dout.Type?.Type ?? typeof(object));
            port.portName = string.IsNullOrEmpty(dout.Name) ? dout.Type?.Type.Name ?? "Out" : dout.Name;
            port.userData = dout;
            _dataOutputs[dout.ID] = port;
            outputContainer.Add(port);
        }
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
