#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UltEvents;
using UnityEngine;
using UnityEngine.UIElements;

namespace NoodledEvents
{
    /// <summary>
    /// a Node in a Bowl. Compiles into persistent calls
    /// </summary>
    [Serializable]
    public class SerializedNode
    {
        public SerializedNode() { }
        public SerializedNode CopyFrom(SerializedNode other)
        {
            this.Book = other.Book;
            this.Name = other.Name;
            this.NoadType = other.NoadType;
            this.Bowl = other.Bowl;
            this.BookTag = other.BookTag;
            foreach (var din in other.DataInputs)
                this.AddDataIn(din.Name, din.Type, @const: din.UIConst);
            for (int i = 0; i < other.DataInputs.Length; i++)
            {
                this.DataInputs[i].ConstInput = other.DataInputs[i].ConstInput;
                this.DataInputs[i].DefaultObject = other.DataInputs[i].DefaultObject;
                this.DataInputs[i].DefaultStringValue = other.DataInputs[i].DefaultStringValue;
                this.DataInputs[i].ValDefs = other.DataInputs[i].ValDefs;
            }
            foreach (var dou in other.DataOutputs)
                this.AddDataOut(dou.Name, dou.Type);
            foreach (var fi in other.FlowInputs)
                this.AddFlowIn(fi.Name);
            foreach (var fo in other.FlowOutputs)
                this.AddFlowOut(fo.Name);
            return this;
        }
        public NoodleFlowInput AddFlowIn(string name)
        {
            var @in = new NoodleFlowInput(this) { Name = name };
            FlowInputs = FlowInputs.Append(@in).ToArray();
            return @in;
        }
        public NoodleDataInput AddDataIn(string name, Type t, object defaultValue = null, bool @const = false)
        {
            var @in = new NoodleDataInput(this, t, name, defaultValue) { UIConst = @const };
            DataInputs = DataInputs.Append(@in).ToArray();
            return @in;
        }
        public NoodleFlowOutput AddFlowOut(string name)
        {
            var o = new NoodleFlowOutput(this) { Name = name };
            FlowOutputs = FlowOutputs.Append(o).ToArray();
            return o;
        }
        public NoodleDataOutput AddDataOut(string name, Type t)
        {
            var o = new NoodleDataOutput(this, t);
            DataOutputs = DataOutputs.Append(o).ToArray();
            o.Name = name;
            return o;
        }
        public SerializedNode(SerializedBowl bowl) // inout node
        {
            Bowl = bowl;
            NoadType = SerializedNode.NodeType.BowlInOut;
            Position = new Vector2(-210, 0);

            FieldInfo evtField = bowl.BowlEvtHolderType.Type.GetField(bowl.EventFieldPath, UltEventUtils.AnyAccessBindings);
            
            FlowOutputs = new[] { new NoodleFlowOutput(this) };
            FlowInputs = new NoodleFlowInput[0];
            DataInputs = new NoodleDataInput[0];
            DataOutputs = evtField.FieldType.GetEvtGenerics().Select(t => new NoodleDataOutput(this, t)).ToArray();

            // move node foreach letter in dataoutputs
            if (DataOutputs.Length != 0)
                Position += new Vector2((DataOutputs.OrderBy(d => d.Name.Length).First().Name.Length - 4) * 10, 0);
        }
        [NonSerialized] public SerializedBowl Bowl;
        [SerializeField] public string Name;

        public void Update() // idk lol
        {

        }
        // we need to keep order for these, as currently it's FlowIn, DataIn, FlowOut, DataOut.
        // not rlly important but needed later

        public NoodleFlowInput[] FlowInputs = new NoodleFlowInput[0];
        public NoodleFlowOutput[] FlowOutputs = new NoodleFlowOutput[0];


        public NoodleDataInput[] DataInputs = new NoodleDataInput[0];
        public NoodleDataOutput[] DataOutputs = new NoodleDataOutput[0];

        public enum NodeType { BowlInOut, Normal }

        public NodeType NoadType;


        public Vector2 Position
        {
            get => _position;
            set
            {
                if (_position != value)
                {
                    if (NoadType != NodeType.BowlInOut)
                        value = new Vector2(Mathf.Clamp(value.x, 0, Bowl.Size.x-10), Mathf.Clamp(value.y, 0, Bowl.Size.y-30));
                    PositionChanged.Invoke(_position = value);
                }
            }
        }
        [SerializeField] private Vector2 _position;
        [HideInInspector] public Vector2 LastPosition;
        public Action<Vector2> PositionChanged = delegate { };



        public void Compile(Transform dataRoot)
        {
            // on compile, data outputs keep a nonserialized "compdata" obj that detail how to fetch them
            // for persistant calls it's just their idx,
            // event<t> values are that idx
            // since a data out knows if it's being used, it can fetch data preemptively for advanced/abstracted outputs
            // thing is data-ins might be within another event, in that case the data-in would need to inject pcalls that
            // save the data-out to some silly component. uahrh
            if (NoadType == NodeType.BowlInOut) 
            {
                foreach (var o in DataOutputs)
                    o.CompEvt = Bowl.Event;

                // bowl in out has no cookbook, so we comp it here
                if (Bowl.Event.PersistentCallsList == null) Bowl.Event.FSetPCalls(new List<PersistentCall>());
                Bowl.Event.PersistentCallsList.Clear();
                var targNode = FlowOutputs.FirstOrDefault()?.Target?.Node; // this also handles the descending nodes
                targNode?.Book.CompileNode(Bowl.Event, targNode, dataRoot);
                // all the comp happens through Book.CompileNode calling more of itself

                // we also need to account for nodes with inputs of "System.Type"
                foreach (var node in Bowl.NodeDatas)
                {
                    foreach (var din in node.DataInputs)
                    {
                        if (din.Source == null && din.Type == typeof(Type))
                        {
                            if (din.CompEvt != null)
                            {
                                // cookbooks gotta tell inputs about their compdata, otherwise no System.Type injection
                                int callIdx = din.CompEvt.PersistentCallsList.IndexOf(din.CompCall);
                                var getTypeCall = new PersistentCall(typeof(Type).GetMethod(nameof(Type.GetType), new Type[] { typeof(string), typeof(bool), typeof(bool) }), null);
                                getTypeCall.PersistentArguments[0].String = din.DefaultStringValue;
                                getTypeCall.PersistentArguments[1].Bool = true;
                                getTypeCall.PersistentArguments[2].Bool = true;

                                din.CompEvt.PersistentCallsList.SafeInsert(callIdx, getTypeCall);
                                din.CompArg.FSetType(PersistentArgumentType.ReturnValue);
                                din.CompArg.FSetInt(callIdx);
                                din.CompArg.FSetString(typeof(Type).AssemblyQualifiedName);
                            }
                        }
                    }
                }
            }
        }
        public CookBook Book;
        public string BookTag; // identifies the node within the book
        public void OnBeforeSerialize() // also called by validate for sanity lol
        {
            // save the connections GUIDs for load
            // also ignore deleted node guid
            if (FlowInputs != null)
                foreach (var fi in FlowInputs)
                    fi.SourcesIds = fi.Sources.Where(s => Bowl.NodeDatas.Contains(s.Node)).Select(s => s.ID).ToArray();

            if (DataOutputs != null)
                foreach (var dout in DataOutputs)
                    dout.TargetIds = dout.Targets.Where(t => Bowl.NodeDatas.Contains(t.Node)).Select(t => t.ID).ToArray();

            if (FlowOutputs != null)
                foreach (var fo in FlowOutputs)
                    if (fo.Target != null && !Bowl.NodeDatas.Contains(fo.Target.Node))
                        fo.Target = null;

            if (DataInputs != null)
                foreach (var di in DataInputs)
                    if (di.Source != null && !Bowl.NodeDatas.Contains(di.Source.Node))
                        di.Source = null;
        }

        public void OnAfterDeserialize() // we need to maintain refs to ourselves non cyclically
        {
            foreach (var fi in FlowInputs)
                fi.Node = this;
            foreach (var fo in FlowOutputs)
                fo.Node = this;
            foreach (var di in DataInputs)
                di.Node = this; 
            foreach (var @do in DataOutputs)
                @do.Node = this;

            // then resolve Node Connection refs
            foreach (var fi in FlowInputs)
            {
                // find outputs that touch this input
                fi.Sources = Bowl.NodeDatas.SelectMany(n => n.FlowOutputs).Where(fo => fi.SourcesIds.Contains(fo.ID)).ToList();

                // tell the output they're touching this input
                foreach (var output in fi.Sources)
                    output.Target = fi;
            }

            foreach (var dout in DataOutputs)
            {
                // find inputs this output touches
                dout.Targets = Bowl.NodeDatas.SelectMany(n => n.DataInputs).Where(di => dout.TargetIds.Contains(di.ID)).ToList();

                // tell the output they're touching this input
                foreach (var targ in dout.Targets)
                    targ.Source = dout;
            }
        }
    }

    [Serializable]
    public class NoodleDataInput // has 1 source
    {
        public NoodleDataInput() { }
        public NoodleDataInput(SerializedNode node, Type t, string paramName, object defaultValue) 
        {
            Node = node; Type = new SerializedType(t); Name = paramName;
            if (defaultValue is UnityEngine.Object obj) DefaultObject = obj;
            else
            {
                //call the executioner
                switch (defaultValue)
                {
                    case bool b:
                        DefaultBoolValue = b;
                        break;
                    case float f:
                        DefaultFloatValue = f;
                        break;
                    case int i:
                        DefaultIntValue = i;
                        break;
                    case Vector2 v2:
                        DefaultVector2Value = v2;
                        break;
                    case Vector3 v3:
                        DefaultVector3Value = v3;
                        break;
                    case Vector4 v4:
                        DefaultVector4Value = v4;
                        break;
                    case Quaternion q:
                        DefaultQuaternionValue = q;
                        break;
                    case string s:
                        DefaultStringValue = s;
                        break;
                }
            }
        }
        public string Name;
        [NonSerialized] public SerializedNode Node;
        [SerializeField] public SerializedType Type;
        [NonSerialized] public NoodleDataOutput Source;
        [NonSerialized] public VisualElement UI;
        [NonSerialized] public bool HasMouse;
        [SerializeField] public string ID = Guid.NewGuid().ToString();
        [SerializeField] public bool UIConst;

        public object GetDefault()
        {
            switch (Type.Type.GetFriendlyName())
            {
                case "bool":
                    return DefaultBoolValue;
                case "float":
                    return DefaultFloatValue;
                case "int":
                    return DefaultIntValue;
                case "Vector2":
                    return DefaultVector2Value;
                case "Vector3":
                    return DefaultVector3Value;
                case "Vector4":
                    return DefaultVector4Value;
                case "Quaternion":
                    return DefaultQuaternionValue;
                case "string":
                    return DefaultStringValue;
            }
            if (Type == typeof(object))
            {
                switch (ConstInput)
                {
                    case PersistentArgumentType.None:
                        return null;
                    case PersistentArgumentType.Bool:
                        return DefaultBoolValue;
                    case PersistentArgumentType.String:
                        return DefaultStringValue;
                    case PersistentArgumentType.Int:
                        return DefaultIntValue;
                    case PersistentArgumentType.Float:
                        return DefaultFloatValue;
                    case PersistentArgumentType.Vector2:
                        return DefaultVector2Value;
                    case PersistentArgumentType.Vector3:
                        return DefaultVector3Value;
                    case PersistentArgumentType.Vector4:
                        return DefaultVector4Value;
                    case PersistentArgumentType.Quaternion:
                        return DefaultQuaternionValue;
                    case PersistentArgumentType.Color:
                        return DefaultColorValue;
                    case PersistentArgumentType.Color32:
                        return DefaultColorValue;
                    case PersistentArgumentType.Object:
                        return DefaultObject;
                }
            }
            if (Type == typeof(UnityEngine.Object))
                return DefaultObject;
            return null;
        }

        [SerializeField] public string DefaultStringValue;
        [SerializeField] public Vector4 ValDefs;
        public bool DefaultBoolValue { get => ValDefs.x != 0; set => ValDefs.x = value ? 1 : 0; }
        public float DefaultFloatValue { get => ValDefs.x; set => ValDefs.x = value; }
        public int DefaultIntValue { get => (int)ValDefs.x; set => ValDefs.x = value; }
        public Vector2 DefaultVector2Value { get => new Vector2(ValDefs.x, ValDefs.y); set => ValDefs = new Vector4(value.x, value.y); }
        public Vector3 DefaultVector3Value { get => new Vector3(ValDefs.x, ValDefs.y, ValDefs.z); set => ValDefs = new Vector4(value.x, value.y, value.z); }
        public Vector4 DefaultVector4Value { get => ValDefs; set => ValDefs = value; }
        public Color DefaultColorValue { get => ValDefs; set => ValDefs = value; }
        public Quaternion DefaultQuaternionValue { get => new Quaternion(ValDefs.x, ValDefs.y, ValDefs.z, ValDefs.w); set => ValDefs = new Vector4(value.x, value.y, value.z, value.w); }
        [SerializeField] public UnityEngine.Object DefaultObject;
        [SerializeField] public PersistentArgumentType ConstInput = PersistentArgumentType.None;

        [NonSerialized] public UltEventBase CompEvt;
        [NonSerialized] public PersistentCall CompCall;
        [NonSerialized] public PersistentArgument CompArg;

        [SerializeField] public string EditorConstName;

        public void Connect(NoodleDataOutput output)
        {
            if (Source != null)
                Source.Targets.Remove(this);
            Source = output;
            if (Source != null && !Source.Targets.Contains(this))
                Source.Targets.Add(this);
        }
    }
    [Serializable]
    public class NoodleDataOutput // has n outputs
    {
        public NoodleDataOutput(SerializedNode node, Type t)
        {
            Node = node; Type = new SerializedType(t);
            Name = t.GetFriendlyName();
        }
        public string Name = "";
        [NonSerialized] public SerializedNode Node;
        [NonSerialized] public List<NoodleDataInput> Targets = new();
        [SerializeField] public string[] TargetIds;
        [SerializeField] public SerializedType Type;
        [NonSerialized] public bool HasMouse;
        [NonSerialized] public VisualElement UI;
        [SerializeField] public string ID = Guid.NewGuid().ToString();
        public void Connect(NoodleDataInput input) => input.Connect(this); //lol

        [NonSerialized] public UltEventBase CompEvt; // these only exist at compile time,
        [NonSerialized] public PersistentCall CompCall; // labelling where to find this output.
    }

    [Serializable]
    public class NoodleFlowInput // has n inputs
    {
        public NoodleFlowInput(SerializedNode node) => Node = node;
        public string Name = "";
        
        [NonSerialized] public SerializedNode Node;
        [NonSerialized] public List<NoodleFlowOutput> Sources = new();
        [SerializeField] public string[] SourcesIds;

        [NonSerialized] public bool HasMouse;
        [NonSerialized] public VisualElement UI;

        [SerializeField] public string ID = Guid.NewGuid().ToString();

        public void Connect(NoodleFlowOutput output) => output.Connect(this);
        
    }
    [Serializable]
    public class NoodleFlowOutput // has 1 output
    {
        public string Name = "";
        public NoodleFlowOutput(SerializedNode node) => Node = node;
        [NonSerialized] public SerializedNode Node;
        [NonSerialized] public NoodleFlowInput Target;
        [NonSerialized] public bool HasMouse;
        [NonSerialized] public VisualElement UI;
        [SerializeField] public string ID = Guid.NewGuid().ToString();

        public void Connect(NoodleFlowInput input)
        {
            if (Target != null)
                Target.Sources.Remove(this);
            Target = input;
            if (Target != null && !Target.Sources.Contains(this))
                Target.Sources.Add(this);

            // Connected; Now disconnect if something down the line refs back to me somehow.
            List<SerializedNode> valids = new List<SerializedNode>();
            void conLoop(NoodleFlowOutput o)
            {
                if (valids.Contains(o.Node)) return;
                valids.Add(Node);
                if (o.Target != null)
                    foreach (var fo in o.Target.Node.FlowOutputs)
                    {
                        if (fo.Target == null) continue;
                        if (fo.Target.Node == this.Node)
                        {
                            this.Connect(null);
                            return;
                        }
                        conLoop(fo);
                    }
            }
            conLoop(this);
        }
    }
}
#endif