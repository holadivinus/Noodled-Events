#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UltEvents;

public class UltNoodleInspectorView : VisualElement
{
    public new class UxmlFactory : UxmlFactory<UltNoodleInspectorView, VisualElement.UxmlTraits> { }

    public UltNoodleInspectorView() { }

    internal void UpdateSelection(UltNoodleNodeView nodeView)
    {
        Clear();

        if (nodeView == null || nodeView.Node == null)
            return;

        if (nodeView.Node.NoadType == NoodledEvents.SerializedNode.NodeType.Redirect)
        {
            var label = new Label("Redirect nodes have no editable properties.")
            {
                style = {
                    fontSize = 14,
                    marginTop = 12,
                    unityTextAlign = TextAnchor.MiddleCenter
                }
            };
            Add(label);
            return;
        }

        bool isInOut = nodeView.Node.NoadType == NoodledEvents.SerializedNode.NodeType.BowlInOut;
        string title = isInOut
                        ? "Bowl In/Out"
                        : nodeView.Node.Name;
        var nodeTitle = new Label(title)
        {
            style =
            {
                unityFontStyleAndWeight = FontStyle.Bold,
                fontSize = 16,
                marginBottom = 12,
                unityTextAlign = TextAnchor.MiddleCenter
            }
        };

        Add(nodeTitle);

        if (isInOut)
        {
            Button button = null;
            button = new Button(() =>
            {
                var bowl = nodeView.Node.Bowl;
                //compile and run evt
                bowl.Compile();

                System.Diagnostics.Stopwatch stopWatch = new();
                stopWatch.Start();


                UltNoodleBowl.EvtIsExecRn = true;
                bowl.Event.DynamicInvoke(new object[bowl.Event.ParameterCount]);
                UltNoodleBowl.EvtIsExecRn = false;


                stopWatch.Stop();
                button.text = $"Run -> {stopWatch.ElapsedTicks * 0.0001f}ms";


                // recompile to reset state
                bowl.Compile();
            })
            {
                text = "Run"
            };
            Add(button);
            return;
        }
        DrawDataInputs(nodeView);

        var separator = new VisualElement { style = { height = 12 } };
        Add(separator);
        DrawDataOutputs(nodeView);
    }

    private void DrawDataInputs(UltNoodleNodeView nodeView)
    {
        var titleLabel = new Label("Data Inputs")
        {
            style =
            {
                unityFontStyleAndWeight = FontStyle.Bold,
                fontSize = 14,
                marginBottom = 8,
                unityTextAlign = TextAnchor.MiddleCenter
            }
        };
        Add(titleLabel);

        foreach (var input in nodeView.Node.DataInputs)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 2;

            var label = new Label(input.Name)
            {
                style =
                {
                    flexGrow = 1,
                    unityTextAlign = TextAnchor.MiddleLeft
                }
            };
            row.Add(label);

            if (input.Type.Type == typeof(object))
            {
                var enumField = new EnumField((FauxArgumentType)input.ConstInput);
                enumField.style.flexBasis = 120;
                enumField.style.flexShrink = 0;
                enumField.RegisterValueChangedCallback(evt =>
                {
                    if (!Equals(input.ConstInput, (FauxArgumentType)evt.newValue))
                    {
                        input.ConstInput = (PersistentArgumentType)evt.newValue;
                        nodeView.RebuildConstantField(input);
                    }
                });
                row.Add(enumField);
            }
            else
            {
                var constLabel = new Label($"({input.Type.Type.Name})");
                constLabel.style.color = new StyleColor(Color.gray);
                row.Add(constLabel);
            }

            Add(row);
        }

        if (nodeView.Node.DataInputs.Length == 0)
        {
            var noInputsLabel = new Label("No data inputs");
            noInputsLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            noInputsLabel.style.color = new StyleColor(Color.gray);
            Add(noInputsLabel);
        }
    }

    private void DrawDataOutputs(UltNoodleNodeView nodeView)
    {
        var titleLabel = new Label("Data Outputs")
        {
            style =
            {
                unityFontStyleAndWeight = FontStyle.Bold,
                fontSize = 14,
                marginBottom = 8,
                unityTextAlign = TextAnchor.MiddleCenter
            }
        };
        Add(titleLabel);

        foreach (var output in nodeView.Node.DataOutputs)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 2;

            var label = new Label(output.Name)
            {
                style =
                {
                    flexGrow = 1,
                    unityTextAlign = TextAnchor.MiddleLeft
                }
            };
            row.Add(label);

            var typeLabel = new Label($"({output.Type.Type.Name})");
            typeLabel.style.color = new StyleColor(Color.gray);
            row.Add(typeLabel);

            Add(row);
        }

        if (nodeView.Node.DataOutputs.Length == 0)
        {
            var noOutputsLabel = new Label("No data outputs");
            noOutputsLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            noOutputsLabel.style.color = new StyleColor(Color.gray);
            Add(noOutputsLabel);
        }
    }

    // based on PersistentArgumentType from UltEvents, but removed some things that don't work with faux types
    private enum FauxArgumentType
    {
        None = 0,
        Bool = 1,
        String = 2,
        Int = 3,
        Float = 5,
        Vector2 = 6,
        Vector3 = 7,
        Vector4 = 8,
        Quaternion = 9,
        Color = 10,
        Object = 13
    }
}
#endif