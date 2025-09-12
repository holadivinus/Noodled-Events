using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class UltNoodleEdgeConnectorListener : IEdgeConnectorListener
{
    private UltNoodleTreeView _graphView;

    public UltNoodleEdgeConnectorListener(UltNoodleTreeView graphView)
    {
        _graphView = graphView;
    }

    public void OnDropOutsidePort(Edge edge, Vector2 position)
    {
        Vector2 screenPos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);

        // nodeCreationRequest doesn't get called for this event, so we have to do it ourselves
        Vector2 graphPos = _graphView.contentViewContainer.WorldToLocal(Event.current.mousePosition);
        _graphView.PendingEdgeOriginPort = edge.output;
        _graphView.NewNodeSpawnPos = graphPos;

        UltNoodleSearchWindow.Open(_graphView, screenPos, edge);
    }

    public void OnDrop(GraphView graphView, Edge edge)
    {
        // from input to output, nothing special needed here
    }
}
