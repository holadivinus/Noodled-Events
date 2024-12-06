#if UNITY_EDITOR
using NoodledEvents;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


public class UltNoodleFlowOutPoint : VisualElement
{
    public new class UxmlFactory : UxmlFactory<UltNoodleFlowOutPoint, UxmlTraits> { }

    public static UltNoodleFlowOutPoint New(UltNoodleNodeUI node, NoodleFlowOutput output)
    {
        var o = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ScriptPath.Replace(".cs", ".uxml")).Instantiate().Q<UltNoodleFlowOutPoint>();
        o.setupInternal(node, output);
        return o;
    }
    private void setupInternal(UltNoodleNodeUI node, NoodleFlowOutput output)
    {
        NodeUI = node; SData = output;
        SData.UI = this.Q("ConnectionPoint");
        // Inform the bowl about drags
        SData.UI.RegisterCallback<MouseEnterEvent>(e => SData.HasMouse = true); SData.UI.RegisterCallback<MouseLeaveEvent>(e => SData.HasMouse = false);
        SData.UI.RegisterCallback<MouseDownEvent>(e => { if (e.button == 0) (NodeUI.Bowl.CurHoveredFlowOutput = SData).UI.CaptureMouse(); });
        SData.UI.RegisterCallback<MouseMoveEvent>(e => node.Bowl.MousePos = node.Bowl.NodeBG.WorldToLocal(e.mousePosition));
        SData.UI.RegisterCallback<MouseUpEvent>(e =>
        { if (SData.UI.HasMouseCapture()) { node.Bowl.ConnectNodes(); SData.UI.ReleaseMouse(); NodeUI.Bowl.CurHoveredFlowOutput = null; } });
        // bowl's been informed :)
        Label = this.Q<Label>("OutputName");
        Label.text = output.Name;

        NodeUI.OutPoints.Add(this);
        Line = this.Q("Line");

        NodeUI.OutputsElement.Add(this);
    }
    public UltNoodleNodeUI NodeUI;
    public NoodleFlowOutput SData;

    Label Label;

    public static string ScriptPath
        => s_scriptPath ??= AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets($"t:Script {nameof(UltNoodleFlowOutPoint)}")[0]);
    private static string s_scriptPath;

    int i;
    VisualElement Line;
    public void UpdateLine() // draw connection line
    {
        Line.visible = false;
        if (SData.Target != null)
        {
            Line.visible = true;
            Vector2 start = Line.parent.WorldToLocal(SData.UI.LocalToWorld(Vector2.zero));
            Vector2 end = Line.parent.WorldToLocal(SData.Target.UI.LocalToWorld(Vector2.zero)) - new Vector2(7, 0);
            Line.style.left = start.x + 8;
            Line.style.top = start.y + 3;
            Line.style.minWidth = Vector2.Distance(start, end);

            //rotat...

            // figure angle, make start 0 0 0
            Vector2 a = end - start;
            Line.transform.rotation = Quaternion.Euler(0, 0, Mathf.Rad2Deg * Mathf.Atan2(a.y, a.x));
        }
    }
}
#endif