#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UltEvents;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NoodledEvents
{
    /// <summary>
    /// A "Bowl" latches onto a component with an Event.
    /// The bowl holds Nodes, which edit the event's calls.
    /// </summary>
    [ExecuteAlways]
    public partial class SerializedBowl : MonoBehaviour, ISerializationCallbackReceiver
    {
        public static SerializedBowl Create(Component target, SerializedType fieldType, string eventFieldPath)
        {
            var o = target.gameObject.AddComponent<SerializedBowl>();
            o.EventFieldPath = eventFieldPath;
            o.EventHolder = target;
            o.BowlName = "Bowl";
            o.BowlEvtHolderType = fieldType;

            o.NodeDatas.Add(new SerializedNode(o));



            return o;
        }
        public void Awake()
        {
            //this.hideFlags = HideFlags.HideInInspector;
        }

        private string _lastBowlName;
        public string BowlName;
        public Action<string> BowlNameChange = delegate { };
        [HideInInspector] public Component EventHolder;
        public string EventFieldPath;
        private FieldInfo _getter;
        [SerializeField] public SerializedType BowlEvtHolderType;
        public UltEventBase Event
        {
            get => (_getter ??= BowlEvtHolderType.Type.GetField(EventFieldPath, UltEventUtils.AnyAccessBindings))?.GetValue(EventHolder) as UltEventBase;
            set => (_getter ??= BowlEvtHolderType.Type.GetField(EventFieldPath, UltEventUtils.AnyAccessBindings))?.SetValue(EventHolder, value);
        }
        
        public Action<string> PathChange = delegate { };
        public string Path => gameObject.name + "." + EventHolder.GetType().Name + "." + EventFieldPath + ".";
        private string _lastGobjName;

        Vector2 _lastPos;
        [SerializeField] Vector2 _pos;
        public Vector2 Position
        {
            get => _pos;
            set
            {
                if (_pos == value) return;
                PositionChanged.Invoke(_pos = value);
            }
        }
        public Action<Vector2> PositionChanged = delegate { };
        Vector2 _lastSize;
        [SerializeField] Vector2 _size;
        public Vector2 Size 
        {
            get => _size;
            set
            {
                if (_size == value) return;
                SizeChanged.Invoke(_size = value);
            }
        }
        public Action<Vector2> SizeChanged = delegate { };
        public void Update()
        {
            if (EventHolder == null && this != null)
            {
                DestroyImmediate(this); // destroy if comp gone
                return;
            }
            if (_lastGobjName != gameObject.name) // track path changes & Event
            {
                _lastGobjName = gameObject.name;
                PathChange.Invoke(Path);
            }
            if (_lastBowlName != BowlName) BowlNameChange.Invoke(_lastBowlName = BowlName); // track name changes & Event
            if (_lastSize != Size) SizeChanged.Invoke(_lastSize = Size); // track size changes & Event
            if (_lastPos != Position) PositionChanged.Invoke(_lastPos = Position); // track pos changes & Event

            foreach (var node in NodeDatas)
                if (node.LastPosition != node.Position) node.PositionChanged.Invoke(node.LastPosition = node.Position);

            OnUpdate.Invoke();

            foreach (var node in NodeDatas)
                node.Update();
        }
        public Action OnUpdate = delegate { };
        public SerializedNode EntryNode => NodeDatas[0];
        [SerializeField] public List<SerializedNode> NodeDatas = new(); // this list is "compiled" into the targ event.
        [SerializeField] public GameObject LastGenerated;

        /// <summary>
        /// Compiles this bowl into their target event.
        /// </summary>
        public void Compile()
        {
            if (Event == null) Event = new UltEvent();
            Event.Clear();

            // remove old "bowl_generated" for pre 1.2.0 users
            if (LastGenerated == null)
                LastGenerated = EventHolder.transform.Find("bowl_generated")?.gameObject;
            if (LastGenerated != null) 
                UnityEngine.Object.DestroyImmediate(LastGenerated);
            
            LastGenerated = new GameObject(BowlName + "_generated");
            LastGenerated.transform.parent = EventHolder.transform;

            foreach (var n in NodeDatas)
            {
                foreach (var d in n.DataOutputs)
                {
                    d.CompCall = null;
                    d.CompEvt = null;
                }
                foreach (var d in n.DataInputs)
                {
                    d.CompArg = null;
                    d.CompEvt = null;
                    d.CompCall = null;
                }
            }

            EntryNode.Compile(LastGenerated.transform);

            EditorSceneManager.MarkSceneDirty(this.gameObject.scene);

            //Debug.Log(BowlName + ": Compile!");
        }

        public void OnBeforeSerialize()
        {
            NodeDatas = NodeDatas.Where(nd => nd != null).ToList();
            foreach (var node in NodeDatas)
                node.OnBeforeSerialize();
        }

        public void OnAfterDeserialize()
        {
            foreach (var node in NodeDatas)
                node.Bowl = this;
            foreach (var node in NodeDatas)
                node.OnAfterDeserialize();
        }

    }
}
#endif