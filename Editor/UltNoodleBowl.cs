using System.Linq;
using NoodledEvents;
using NoodledEvents.Assets.Noodled_Events;
using UltEvents;
using UnityEditor;
using UnityEngine;

public class UltNoodleBowl
{
    // TODO: assign this
    public static bool EvtIsExecRn;

    public VarMan[] VarMans = new VarMan[0];
    public NoodleDataInput[] VarManVars = new NoodleDataInput[0];


    // TODO: cleanup variables
    public UltNoodleEditor Editor;
    public UnityEngine.Component Component;
    private SerializedType _fieldType;
    public string _eventFieldPath;

    private SerializedBowl _sb;
    public SerializedBowl SerializedData
    {
        get
        {
            if (_sb == null && Component != null)
            {
                _sb = Component.GetBowlData(_fieldType, _eventFieldPath);
                // TODO: path label
                /* PathLabel.text = _sb.Path;
                _sb.PathChange += (newPath) => PathLabel.text = newPath; */
            }
            if (_sb != null) _sb.BowlEvtHolderType = _fieldType; // backwards compat for pre BowlEvtHolderType bowlz
            return _sb;
        }
    }

    public UltEventBase Event => SerializedData.Event;

    public UltNoodleBowl(UltNoodleEditor editor, UnityEngine.Component eventComponent, SerializedType fieldType, string eventField)
    {
        Editor = editor;
        Component = eventComponent;
        _fieldType = fieldType;
        _eventFieldPath = eventField;

        SerializedData.PathChange += OnPathChange;
        SerializedData.BowlNameChange += OnBowlNameChange;

        SerializedData.SizeChanged += OnSizeChange;
        SerializedData.PositionChanged += OnPositionChange;

        if (SerializedData.Size == Vector2.zero)
            SerializedData.Size = new Vector2(1000, 800);
        SerializedData.OnUpdate += OnSceneUpdate;

        Validate();
        EditorApplication.update += OnEditorUpdate;
    }

    public void Destroy()
    {
        if (SerializedData != null)
        {
            SerializedData.PathChange -= OnPathChange;
            SerializedData.BowlNameChange -= OnBowlNameChange;

            SerializedData.SizeChanged -= OnSizeChange;
            SerializedData.PositionChanged -= OnPositionChange;

            SerializedData.OnUpdate -= OnSceneUpdate;
        }
        EditorApplication.update -= OnEditorUpdate;
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

    private void OnPathChange(string path)
    {
        // TODO: OnPathChange
    }

    private void OnBowlNameChange(string name)
    {
        // TODO: OnBowlNameChange
    }

    private void OnSizeChange(Vector2 size)
    {
        // TODO: OnSizeChange
    }

    private void OnPositionChange(Vector2 pos)
    {
        // TODO: OnPositionChange
    }

    public void OnSceneUpdate()
    {
        // TODO: do i need anything here?
    }

    public void Validate()
    {
        if (Component == null //kms if no evt
            || (Selection.activeGameObject != SerializedData.gameObject && EditorPrefs.GetBool("SelectedBowlsOnly", true)) // kms if unselected
            || !(UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage()?.IsPartOfPrefabContents(SerializedData.gameObject) ?? true)) // kms if not in a prefab when prefab mode is active
        {
            Editor.Bowls.Remove(this);
        }
        else
        {
            VarMans = SerializedData.GetComponentsInParent<VarMan>(true);
            if (VarMans.Any(v => v.HideBowls))
            {
                Editor.Bowls.Remove(this);
            }
            VarManVars = VarMans.SelectMany(vm => vm.Vars).ToArray();
        }
    }

    public void OnEditorUpdate()
    {
        // TODO: do i need anything here?
    }
}