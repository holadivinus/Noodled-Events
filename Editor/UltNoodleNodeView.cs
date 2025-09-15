#if UNITY_EDITOR
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

    protected readonly Dictionary<string, Port> _flowInputs = new();
    protected readonly Dictionary<string, Port> _flowOutputs = new();
    protected readonly Dictionary<string, Port> _dataInputs = new();
    protected readonly Dictionary<string, Port> _dataOutputs = new();

    private Dictionary<Type, List<NoodleDataInput>> _varManVarOptions = new();

    public UltNoodleNodeView(SerializedNode node)
    {
        this.Node = node;
        this.title = node.Name;
        this.viewDataKey = node.ID;

        if (node.NoadType == SerializedNode.NodeType.Redirect)
            return; // let the subclass handle it

        style.left = node.Position.x;
        style.top = node.Position.y;

        if (node.NoadType == SerializedNode.NodeType.BowlInOut)
        {
            this.capabilities &= ~Capabilities.Deletable; // cannot delete the primary node
            this.capabilities &= ~Capabilities.Copiable;  // cannot copy the primary node
        }

        Validate();

        CreateFlowPorts();
        CreateDataPorts();

        RegisterCallback<MouseDownEvent>(OnMouseDown);

        titleButtonContainer.ElementAt(0).RemoveFromHierarchy(); // don't allow collapsing nodes, it doesn't work well with our UI
        titleButtonContainer.AddToClassList("ult-node-button-container");

        // TODO: this entire node-swapping system is very janky
        AddTitleButton("d_Assembly Icon", () =>
        {
            Dictionary<string, CookBook.NodeDef> entries = new();
            foreach (CookBook cb in UltNoodleEditor.AllBooks)
            {
                var alternatives = cb.GetAlternatives(Node);
                if (alternatives == null || alternatives.Count == 0) continue;
                foreach (var entry in alternatives)
                {
                    if (entries.ContainsKey(entry.Key)) continue;
                    entries[entry.Key] = entry.Value;
                }
            }

            var menu = new GenericMenu();
            foreach (var option in entries.Keys)
            {
                menu.AddItem(new GUIContent(option), false, () =>
                {
                    var nodeDef = entries[option];
                    var newNode = UltNoodleEditor.Editor.CurrentBowl.AddNode(nodeDef.Name, nodeDef.CookBook).MatchDef(nodeDef);
                    newNode.BookTag = nodeDef.BookTag != string.Empty ? nodeDef.BookTag : nodeDef.Name;
                    newNode.Position = Node.Position;
                    Node.Bowl.NodeDatas.Remove(Node);
                    UltNoodleEditor.Editor.CurrentBowl.Validate();
                    nodeDef.CookBook.SwapConnections(Node, newNode);
                    UltNoodleEditor.Editor.TreeView.PopulateView(UltNoodleEditor.Editor.CurrentBowl);
                });
            }
            menu.ShowAsContext();
        });

        AddTitleButton("winbtn_win_close", () =>
        {
            UltNoodleEditor.Editor.TreeView.DeleteElements(new[] { this });
        });
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
            port.tooltip = di.Type?.Type.Name ?? "Unknown";
            _dataInputs[di.ID] = port;

            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.Add(port);

            VisualElement varManDropdown = CreateVarManDropdown(di);

            VisualElement field = CreateFieldForType(di);
            if (field != null && (varManDropdown == null || string.IsNullOrWhiteSpace(di.EditorConstName)))
            {
                field.name = "ConstantField";
                if (port.connected)
                    field.style.display = DisplayStyle.None;
                container.Add(field);
            }

            if (varManDropdown != null)
            {
                varManDropdown.name = "VarManDropdown";
                if (port.connected)
                    varManDropdown.style.display = DisplayStyle.None;
                container.Add(varManDropdown);
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
            port.tooltip = dout.Type?.Type.Name ?? "Unknown";

            var listener = new UltNoodleEdgeConnectorListener(UltNoodleEditor.Editor.TreeView);
            var connector = new EdgeConnector<Edge>(listener);
            port.AddManipulator(connector);

            _dataOutputs[dout.ID] = port;
            outputContainer.Add(port);
        }
    }

    public void Validate(bool tryRebuildInputs = false)
    {
        if (UltNoodleEditor.Editor == null || UltNoodleEditor.Editor.CurrentBowl == null)
            return;

        _varManVarOptions.Clear();
        var bowl = UltNoodleEditor.Editor.CurrentBowl;
        foreach (var type in Node.DataInputs.Select(i => i.Type.Type).Distinct())
        {
            var applicables = bowl.VarManVars.Where(var => var.Type.Type.IsAssignableFrom(type)).ToList();
            applicables.Insert(0, new NoodleDataInput() { Name = "none" });
            _varManVarOptions[type] = applicables;
        }

        // TODO: rebuilding inputs is a bit janky, should be smarter about what changed or just entirely refactor how constants/varman inputs work
        if (tryRebuildInputs)
        {
            foreach (var input in Node.DataInputs)
            {
                RebuildConstantField(input);
            }
        }
    }

    private void AddTitleButton(string iconName, Action onClick)
    {
        if (Node.NoadType == SerializedNode.NodeType.BowlInOut)
            return;

        var button = new VisualElement();
        button.AddToClassList("ult-node-button");

        var icon = new Image()
        {
            image = EditorGUIUtility.IconContent(iconName).image as Texture2D,
            scaleMode = ScaleMode.ScaleToFit
        };
        icon.AddToClassList("ult-node-button-icon");
        button.Add(icon);

        button.RegisterCallback<MouseEnterEvent>(e => icon.AddToClassList("ult-node-button-icon-hover"));
        button.RegisterCallback<MouseLeaveEvent>(e => icon.RemoveFromClassList("ult-node-button-icon-hover"));
        button.AddManipulator(new Clickable(onClick));

        titleButtonContainer.Add(button);
    }

    private VisualElement CreateVarManDropdown(NoodleDataInput input)
    {
        if (!_varManVarOptions.TryGetValue(input.Type.Type, out var options))
            return null;
        if (options.Count <= 1) return null;

        var dropdown = new DropdownField();
        dropdown.choices = options.Select(v => v.Name).ToList();

        dropdown.RegisterValueChangedCallback(evt =>
        {
            string prev = input.EditorConstName;
            string now = evt.newValue == "none" ? "" : evt.newValue;
            if (evt.newValue == "none")
            {
                input.EditorConstName = "";
            }
            else
            {
                var selected = options.FirstOrDefault(v => v.Name == evt.newValue);
                if (selected == null) return;

                input.EditorConstName = evt.newValue;
                input.ValDefs = selected.ValDefs;
                input.DefaultObject = selected.DefaultObject;
                input.DefaultStringValue = selected.DefaultStringValue;
            }

            // only rebuild if we changed from const to varman or vice versa
            if ((prev == "" && now != "") || (prev != "" && now == ""))
                RebuildConstantField(input);
        });
        dropdown.value = string.IsNullOrEmpty(input.EditorConstName) ? "" : input.EditorConstName;
        dropdown.style.justifyContent = Justify.FlexEnd;
        dropdown.style.flexShrink = 1;
        dropdown.style.flexGrow = 0;
        dropdown.style.minWidth = 0;
        if (string.IsNullOrEmpty(input.EditorConstName))
            dropdown.style.width = 18; // shrink to only show dropdown arrow

        return dropdown;
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
            vectorField.style.minWidth = 100; // numbers act a bit weirdly in small widths
            vectorField.RegisterValueChangedCallback(evt => input.DefaultVector2Value = evt.newValue);
            field = vectorField;
        }
        else if (type == typeof(Vector3) || fauxType == PersistentArgumentType.Vector3)
        {
            var vectorField = new Vector3Field();
            vectorField.value = input.DefaultVector3Value;
            vectorField.style.minWidth = 140; // numbers act a bit weirdly in small widths
            vectorField.RegisterValueChangedCallback(evt => input.DefaultVector3Value = evt.newValue);
            field = vectorField;
        }
        else if (type == typeof(Vector4) || fauxType == PersistentArgumentType.Vector4)
        {
            var vectorField = new Vector4Field();
            vectorField.style.minWidth = 187; // numbers act a bit weirdly in small widths
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
        else if (type == typeof(Type))
        {
            // TODO: this entire UI is a bit clunky, it should probably get an actual search window
            var textField = new TextField();
            textField.value = input.DefaultStringValue ?? "";
            field = textField;

            // Manual "change output type for delegates.Create" node
            // This is a bodge, The correct course of action here was to make
            // & use a "dynamic NodeDef" api
            // TODO i supposed
            void UpdateDelegateOutput()
            {
                if (Node.Name.StartsWith("delegates.Create_with")
                 && input.Name.StartsWith("Param Type"))
                {
                    int paramNum = int.Parse(input.Name.Replace("Param Type ", ""));
                    Type t = null;
                    try
                    {
                        t = Type.GetType(input.DefaultStringValue, true, true);
                    }
                    catch (Exception) { t = typeof(object); }
                    Node.DataOutputs[paramNum + 1].Type = t;
                }
            }

            void TryUpdateType()
            {
                textField.value = textField.value.Trim();
                if (TypeTranslator.SimpleNames2Types.TryGetValue(textField.value.ToLower(), out Type v))
                {
                    textField.value = string.Join(',', v.AssemblyQualifiedName.Split(',').Take(2));
                    input.DefaultStringValue = textField.value;
                    UpdateDelegateOutput();
                    return;
                }
                foreach (Type t in UltNoodleEditor.SearchableTypes)
                {
                    if (string.Compare(t.Name, textField.value, StringComparison.CurrentCultureIgnoreCase) == 0)
                    {
                        textField.value = string.Join(',', t.AssemblyQualifiedName.Split(',').Take(2));
                        input.DefaultStringValue = textField.value;
                        UpdateDelegateOutput();
                        return;
                    }
                }

                // string doesn't match any type name, might be raw gettype input
                input.DefaultStringValue = textField.value;
            }

            textField.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Return)
                    TryUpdateType();
                input.DefaultStringValue = textField.value;
                UpdateDelegateOutput();
            });
            textField.RegisterCallback<FocusOutEvent>(e =>
            {
                TryUpdateType();
                input.DefaultStringValue = textField.value;
                UpdateDelegateOutput();
            });

            UpdateDelegateOutput();
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
        if (Node.NoadType == SerializedNode.NodeType.Redirect)
            return; // redirect nodes don't do constants

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

        var oldDropdown = container.Q<VisualElement>("VarManDropdown");
        if (oldDropdown != null)
        {
            container.Remove(oldDropdown);
        }

        var newDropdown = CreateVarManDropdown(input);

        var newField = CreateFieldForType(input);
        if (newField != null && (newDropdown == null || string.IsNullOrWhiteSpace(input.EditorConstName)))
        {
            newField.name = "ConstantField";
            if (port.connected)
                newField.style.display = DisplayStyle.None;
            container.Add(newField);
        }

        if (newDropdown != null)
        {
            newDropdown.name = "VarManDropdown";
            if (port.connected)
                newDropdown.style.display = DisplayStyle.None;
            container.Add(newDropdown);
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
#endif