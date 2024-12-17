#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UltEvents;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace NoodledEvents
{
    /// <summary>
    /// A Node refers to a CookBook for compilation;
    /// A CookBook provides a List of addable nodes, while handling the construction of these nodes (serialized side only)
    /// </summary>
    public class CookBook : ScriptableObject
    {
        private static Assembly _blAssmb;
        private static Assembly _xrAssmb;
        public static Assembly BLAssembly => _blAssmb ??= AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(ass => ass.FullName.StartsWith("Assembly-CSharp"));
        public static Assembly XRAssembly => _xrAssmb ??= AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(ass => ass.FullName.StartsWith("Unity.XR.Interaction.Toolkit"));
        public static Type GetExtType(string name, Assembly ass = null) 
        {
            ass ??= BLAssembly;
            if (ass == null)
                return null;
            foreach (Type t in ass.GetTypes())
                try
                {
                    if (t.Name == name) return t;
                }
                catch (TypeLoadException) { }
            return null;
        }
        public virtual void CollectDefs(List<NodeDef> allDefs) 
        {
            
        }
        public virtual void CompileNode(UltEventBase evt, SerializedNode node, Transform dataRoot)
        {
            // when a bowl is compiled, it puts forward an evt that is filled by compiled nodes.
        }
        public class PendingConnection // utility class to link pcalls, with support for cross-event data transfer
        { 
            /// <summary>
            /// Super generic NoodleOut -> PersistentCallArgIn
            /// </summary>
            /// <param name="o"></param>
            /// <param name="targEvt"></param>
            /// <param name="targCall"></param>
            /// <param name="argIdx"></param>
            public PendingConnection(NoodleDataOutput o, UltEventBase targEvt, PersistentCall targCall, int argIdx) 
            { 
                SourceEvent = o.CompEvt; SourceCall = o.CompCall;
                TargEvent = targEvt; TargCall = targCall;
                TargArgType = targCall.Method.GetParameters()[argIdx].ParameterType; TargInput = argIdx;
                if (o.Node.NoadType == SerializedNode.NodeType.BowlInOut)
                    ArgIsSource = Array.IndexOf(o.Node.DataOutputs, o);
                else ArgIsSource = -1;
            }

            // Comps used to transfer data between events
            public static Dictionary<Type, (Type, PropertyInfo)> CompStoragers = new Dictionary<Type, (Type, PropertyInfo)>()
            {
                { typeof(UnityEngine.Object), (GetExtType("XRInteractorAffordanceStateProvider", XRAssembly), GetExtType("XRInteractorAffordanceStateProvider", XRAssembly).GetProperty("interactorSource", UltEventUtils.AnyAccessBindings)) },
                { typeof(float), (typeof(SphereCollider), RadiusGetSet) },
                { typeof(bool), (typeof(Mask), typeof(Mask).GetProperty("enabled")) },
                { typeof(Vector3), (typeof(BoxCollider), typeof(BoxCollider).GetProperty(nameof(BoxCollider.center))) },
                { typeof(string), (typeof(TextMeshPro), typeof(TMP_Text).GetProperty("text", UltEventUtils.AnyAccessBindings)) },
                { typeof(int), (typeof(LineRenderer), typeof(LineRenderer).GetProperty("numCapVertices", UltEventUtils.AnyAccessBindings)) }
            };
        /* Todo types for CompStoragers
        {typeof(uint), "uint"},
        {typeof(long), "long"},
        { typeof(ulong), "ulong"},
        { typeof(short), "short"},
        { typeof(ushort), "ushort"},
        { typeof(byte), "byte"},
        { typeof(sbyte), "sbyte"},
        { typeof(double), "double"},
        { typeof(decimal), "decimal"},
        { typeof(char), "char"},
        // these remains are pretty uncommon, i'll implement them later
        */

            public int ArgIsSource; // if this is from an arg (-1 means no >= 0 gives arg idx)
            public UltEventBase SourceEvent;
            public PersistentCall SourceCall;

            public UltEventBase TargEvent;
            public PersistentCall TargCall;
            public Type TargArgType;
            public int TargInput; // the idx of the arg on the TargCall to set as Arg
            public void Connect(Transform dataRoot) // fyi this is called while the targcall is being constructed
            {
                if (SourceEvent == TargEvent) // same evt connection
                {
                    if (ArgIsSource > -1)
                        TargCall.PersistentArguments[TargInput] = new PersistentArgument().ToParamVal(ArgIsSource, TargArgType);
                    else
                        TargCall.PersistentArguments[TargInput] = new PersistentArgument().ToRetVal(SourceEvent.PersistentCallsList.IndexOf(SourceCall), TargArgType);
                }
                else
                {
                    // Source evt != targ event.
                    // to transfer data, we need a temp component to store data in

                    // for UnityEngine.Object, this is easy
                    // all the other types (int, float, color, bool) are todo.

                        
                    Type transferredType = TargArgType;
                    if (ArgIsSource == -1)
                    {
                        if (SourceCall.Method.GetReturnType().IsSubclassOf(TargArgType))
                            transferredType = SourceCall.Method.GetReturnType();
                    } else
                    {
                        Type evtT = SourceEvent.GetType().GetEvtGenerics()[ArgIsSource]; 
                        if (evtT.IsSubclassOf(TargArgType))
                            transferredType = evtT;
                    }

                    foreach (var kvp in CompStoragers)
                    {
                        if (!(transferredType == kvp.Key || transferredType.IsSubclassOf(kvp.Key)))
                            continue;

                        Type dataT = kvp.Key;
                        Type storageT = kvp.Value.Item1 ?? kvp.Value.Item2.DeclaringType;

                        var compVar = dataRoot.StoreComp(storageT);

                        // set compVar in Source Event
                        PersistentCall varSet = null;
                        if (ArgIsSource == -1)
                        {
                            int sourceIdx = SourceEvent.PersistentCallsList.IndexOf(SourceCall); // source PCall idx
                            varSet = new PersistentCall(kvp.Value.Item2.SetMethod, compVar); // compVar setter PCall
                            varSet.FSetArguments(new PersistentArgument().ToRetVal(sourceIdx, transferredType)); // arg for compVar setter PCall
                            SourceEvent.PersistentCallsList.SafeInsert(sourceIdx + 1, varSet); // add compVar setter PCall directly after source PCall
                        }
                        else
                        {
                            varSet = new PersistentCall(kvp.Value.Item2.SetMethod, compVar); // compVar setter PCall
                            varSet.FSetArguments(new PersistentArgument().ToParamVal(ArgIsSource, transferredType)); // arg for compVar setter PCall
                            SourceEvent.PersistentCallsList.SafeInsert(0, varSet); // add compVar setter PCall directly after source PCall
                        }

                        // make getter pcall for targ evt
                        var getPCall = new PersistentCall(kvp.Value.Item2.GetMethod, compVar);

                        // add the getter pcall
                        TargEvent.PersistentCallsList.Add(getPCall);

                        // make targcall ref the gotten value (remember, targcall is under construction rn so its gonna be added last)
                        TargCall.PersistentArguments[TargInput] = new PersistentArgument().ToRetVal(TargEvent.PersistentCallsList.Count - 1, TargArgType);

                        return;
                    }

                    // fail
                    Debug.Log("failed data transfer for " + TargArgType);
                    
                }
            }
            private static PropertyInfo RadiusGetSet = typeof(SphereCollider).GetProperty(nameof(SphereCollider.radius), UltEventUtils.AnyAccessBindings);
        }
        public class NodeDef
        {
            //public NodeDef() { }
            public NodeDef(CookBook book, string name, Func<Pin[]> inputs, Func<Pin[]> outputs, Func<NodeDef, VisualElement> searchItem) 
            {
                CookBook = book; Name = name; Inputs = inputs?.Invoke() ?? new Pin[0]; Outputs = outputs?.Invoke() ?? new Pin[0];
                createSearchItem = searchItem;
            }
            public string Name;
            public CookBook CookBook;
            public Pin[] Inputs;
            public Pin[] Outputs;
            public Func<SerializedNode> CreateNode;
            private Func<NodeDef, VisualElement> createSearchItem;
            public VisualElement SearchItem => _searchItem ??= createSearchItem.Invoke(this);
            private VisualElement _searchItem;

            public class Pin
            {
                public Pin(string name) { Name = name; }
                public Pin(string name, Type type, bool @const = false)
                {
                    Name = name; Type = type; Const = @const;
                }
                public string Name;
                public Type Type;
                public bool Const;
                public bool Flow => Type == null;
            }
        }
    }
}
#endif