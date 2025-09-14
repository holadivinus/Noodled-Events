#if UNITY_EDITOR
using System.Linq;
using NoodledEvents;
using NoodledEvents.Assets.Noodled_Events;
using UltEvents;
using UnityEditor;
using UnityEngine;

public class UltNoodleBowl
{
    public static bool EvtIsExecRn;

    public VarMan[] VarMans = new VarMan[0];
    public NoodleDataInput[] VarManVars = new NoodleDataInput[0];

    public Component Component => _component;
    public string EventFieldPath => _eventFieldPath;

    private UltNoodleEditor _editor;
    private Component _component;
    private SerializedType _fieldType;
    private string _eventFieldPath;

    private SerializedBowl _sb;
    public SerializedBowl SerializedData
    {
        get
        {
            if (_sb == null && _component != null)
                _sb = _component.GetBowlData(_fieldType, _eventFieldPath);
            if (_sb != null) _sb.BowlEvtHolderType = _fieldType; // backwards compat for pre BowlEvtHolderType bowlz
            return _sb;
        }
    }

    public UltEventBase Event => SerializedData.Event;

    public UltNoodleBowl(UltNoodleEditor editor, Component eventComponent, SerializedType fieldType, string eventField)
    {
        _editor = editor;
        _component = eventComponent;
        _fieldType = fieldType;
        _eventFieldPath = eventField;

        if (SerializedData.Size == Vector2.zero)
            SerializedData.Size = new Vector2(1000, 800);

        Validate();
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
        Undo.RecordObject(SerializedData, "Add Node");
        SerializedData.NodeDatas.Add(nod);
        return nod;
    }

    public void Validate()
    {
        if (_component == null //kms if no evt
            || (Selection.activeGameObject != SerializedData.gameObject && EditorPrefs.GetBool("SelectedBowlsOnly", true)) // kms if unselected
            || !(UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage()?.IsPartOfPrefabContents(SerializedData.gameObject) ?? true)) // kms if not in a prefab when prefab mode is active
        {
            _editor.Bowls.Remove(this);
        }
        else
        {
            VarMans = SerializedData.GetComponentsInParent<VarMan>(true);
            if (VarMans.Any(v => v.HideBowls))
            {
                _editor.Bowls.Remove(this);
            }
            VarManVars = VarMans.SelectMany(vm => vm.Vars).ToArray();
        }
    }
}
#endif