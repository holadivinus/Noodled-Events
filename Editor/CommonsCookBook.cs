#if UNITY_EDITOR
using NoodledEvents;
using System;
using System.Collections.Generic;
using System.Reflection;
using UltEvents;
using UnityEngine;
using UnityEngine.UIElements;
using static NoodledEvents.CookBook.NodeDef;


public class CommonsCookBook : CookBook
{
    public override void CollectDefs(List<NodeDef> allDefs)
    {
        allDefs.Add(new NodeDef("flow.if", 
            inputs:() => new[] { new Pin("Exec"), new Pin("condition", typeof(bool)) },
            outputs:() => new[] { new Pin("true"), new Pin("false") },
            searchItem:(def) => 
            {
                var o = new Button(() =>
                {
                    // create serialized node.

                    if (UltNoodleEditor.NewNodeBowl == null) return;
                    var nod = UltNoodleEditor.NewNodeBowl.AddNode(def.Name, this).MatchDef(def);

                    nod.BookTag = "if";

                    nod.Position = UltNoodleEditor.NewNodePos;
                    UltNoodleEditor.NewNodeBowl.Validate(); // update ui 
                });
                o.text = def.Name;
                return o;
            }));

        allDefs.Add(new NodeDef("math.add_floats",
            inputs: () => new[] { new Pin("Exec"), new Pin("a", typeof(float)), new Pin("b", typeof(float)) },
            outputs: () => new[] { new Pin("done"), new Pin("a+b", typeof(float)) },
            searchItem: (def) =>
            {
                var o = new Button(() =>
                {
                    // create serialized node.

                    if (UltNoodleEditor.NewNodeBowl == null) return;
                    var nod = UltNoodleEditor.NewNodeBowl.AddNode(def.Name, this).MatchDef(def);

                    nod.BookTag = "add_floats";

                    nod.Position = UltNoodleEditor.NewNodePos;
                    UltNoodleEditor.NewNodeBowl.Validate(); // update ui 
                });
                o.text = def.Name;
                return o;
            }));
    }
    private static MethodInfo SetActive = typeof(GameObject).GetMethod("SetActive");
    private static PropertyInfo GetSetLocPos = typeof(Transform).GetProperty("localPosition");
    private static MethodInfo Translate = typeof(Transform).GetMethod("Translate", new Type[] { typeof(float), typeof(float), typeof(float) });
    public override void CompileNode(UltEventBase evt, SerializedNode node, Transform dataRoot)
    {
        switch (node.BookTag)
        {
            case "if":
                // if statementtt :/
                // requires two evts, one for true 1 for false
                var onTrue = new GameObject("if True", typeof(LifeCycleEvents)).GetComponent<LifeCycleEvents>();
                onTrue.transform.parent = dataRoot;
                onTrue.gameObject.SetActive(false);
                var onFalse = new GameObject("if False", typeof(LifeCycleEvents)).GetComponent<LifeCycleEvents>();
                onFalse.transform.parent = dataRoot;
                onFalse.gameObject.SetActive(false);

                // inject the "figuring" of the conditional:
                if (node.DataInputs[0].Source == null)
                {
                    // when hardcoded act hardcoded
                    var call = (new PersistentCall(SetActive, (node.DataInputs[0].DefaultBoolValue ? onTrue : onFalse).gameObject));
                    call.FSetArguments(new PersistentArgument(typeof(bool)) { Bool = true });
                    evt.PersistentCallsList.Add(call);
                    UnityEngine.Object.DestroyImmediate((!node.DataInputs[0].DefaultBoolValue ? onTrue : onFalse).gameObject);

                    var next = node.FlowOutputs[node.DataInputs[0].DefaultBoolValue ? 0 : 1];
                    if (next.Target != null)
                    {
                        next.Target.Node.Book.CompileNode((node.DataInputs[0].DefaultBoolValue ? onTrue : onFalse).EnableEvent, next.Target.Node, dataRoot);
                    }
                }
                else // condition varies
                {
                    var rs1 = new PersistentCall(SetActive, onTrue.gameObject);  // reset onTrue
                    rs1.FSetArguments(new PersistentArgument(typeof(bool)));
                    evt.PersistentCallsList.Add(rs1);
                    var rs2 = new PersistentCall(SetActive, onFalse.gameObject); // reset onFalse
                    rs2.FSetArguments(new PersistentArgument(typeof(bool)));
                    evt.PersistentCallsList.Add(rs2);

                    int conditionSource = evt.PersistentCallsList.IndexOf(node.DataInputs[0].Source.CompCall);

                    //pcall for setting the "true" state
                    var truCall = new PersistentCall(SetActive, onTrue.gameObject);
                    truCall.FSetArguments(new PersistentArgument().ToRetVal(conditionSource, typeof(bool)));
                    evt.PersistentCallsList.Add(truCall);

                    //pcall to invert the state for the falser
                    var invCall = new PersistentCall();
                    invCall.FSetMethodName("System.Object, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089.Equals");
                    invCall.FSetArguments(new PersistentArgument().ToRetVal(conditionSource, typeof(bool)), new PersistentArgument(typeof(bool)));
                    evt.PersistentCallsList.Add(invCall);

                    //pcall for setting the "false" state
                    var falCall = new PersistentCall(SetActive, onFalse.gameObject);
                    falCall.FSetArguments(new PersistentArgument().ToRetVal(evt.PersistentCallsList.IndexOf(invCall), typeof(bool)));
                    evt.PersistentCallsList.Add(falCall);

                    // compile true, false evts
                    var next = node.FlowOutputs[0];
                    if (next.Target != null)
                        next.Target.Node.Book.CompileNode(onTrue.EnableEvent, next.Target.Node, dataRoot);
                    next = node.FlowOutputs[1];
                    if (next.Target != null)
                        next.Target.Node.Book.CompileNode(onFalse.EnableEvent, next.Target.Node, dataRoot);
                }
                return;
            case "add_floats":
                var c = dataRoot.StoreTransform("add_floats counter");
                var resetTransf = new PersistentCall(GetSetLocPos.SetMethod, c);
                evt.PersistentCallsList.Add(resetTransf);

                // add values
                var addA = new PersistentCall(Translate, c);
                if (node.DataInputs[0].Source != null)
                {
                    new PendingConnection(node.DataInputs[0].Source, evt, addA, 1).Connect(dataRoot);
                } else addA.PersistentArguments[1].Float = node.DataInputs[0].DefaultFloatValue;
                evt.PersistentCallsList.Add(addA);

                var addB = new PersistentCall(Translate, c);
                if (node.DataInputs[1].Source != null)
                {
                    new PendingConnection(node.DataInputs[1].Source, evt, addB, 1).Connect(dataRoot);
                } else addB.PersistentArguments[1].Float = node.DataInputs[1].DefaultFloatValue;
                evt.PersistentCallsList.Add(addB);

                // get result
                var getO = new PersistentCall(GetSetLocPos.GetMethod, c);
                var toFloat = new PersistentCall(typeof(Vector3).GetMethod("Dot", UltEventUtils.AnyAccessBindings), null);
                toFloat.PersistentArguments[0].Vector3 = Vector3.up;
                toFloat.PersistentArguments[1].ToRetVal(evt.PersistentCallsList.Count, typeof(Vector3));
                evt.PersistentCallsList.Add(getO);
                evt.PersistentCallsList.Add(toFloat);
                
                node.DataOutputs[0].CompEvt = evt;
                node.DataOutputs[0].CompCall = toFloat;

                var nextNode = node.FlowOutputs[0].Target?.Node;
                if (nextNode != null)
                    nextNode.Book.CompileNode(evt, nextNode, dataRoot);
                return;
        }
    }
}
#endif