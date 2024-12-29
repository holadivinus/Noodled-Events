#if UNITY_EDITOR
using Codice.CM.SEIDInfo;
using Codice.CM.WorkspaceServer;
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
        allDefs.Add(new NodeDef(this, "flow.if", 
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
        #region MATH
        allDefs.Add(new NodeDef(this, "math.add_floats",
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

        allDefs.Add(new NodeDef(this, "math.sub_floats",
            inputs: () => new[] { new Pin("Exec"), new Pin("a", typeof(float)), new Pin("b", typeof(float)) },
            outputs: () => new[] { new Pin("done"), new Pin("a-b", typeof(float)) },
            searchItem: (def) =>
            {
                var o = new Button(() =>
                {
                    // create serialized node.

                    if (UltNoodleEditor.NewNodeBowl == null) return;
                    var nod = UltNoodleEditor.NewNodeBowl.AddNode(def.Name, this).MatchDef(def);

                    nod.BookTag = "sub_floats";

                    nod.Position = UltNoodleEditor.NewNodePos;
                    UltNoodleEditor.NewNodeBowl.Validate(); // update ui 
                });
                o.text = def.Name;
                return o;
            }));

        allDefs.Add(new NodeDef(this, "math.mul_floats",
            inputs: () => new[] { new Pin("Exec"), new Pin("a", typeof(float)), new Pin("b", typeof(float)) },
            outputs: () => new[] { new Pin("done"), new Pin("a*b", typeof(float)) },
            searchItem: (def) =>
            {
                var o = new Button(() =>
                {
                    // create serialized node.

                    if (UltNoodleEditor.NewNodeBowl == null) return;
                    var nod = UltNoodleEditor.NewNodeBowl.AddNode(def.Name, this).MatchDef(def);

                    nod.BookTag = "mul_floats";

                    nod.Position = UltNoodleEditor.NewNodePos;
                    UltNoodleEditor.NewNodeBowl.Validate(); // update ui 
                });
                o.text = def.Name;
                return o;
            }));

        allDefs.Add(new NodeDef(this, "math.div_floats",
            inputs: () => new[] { new Pin("Exec"), new Pin("a", typeof(float)), new Pin("b", typeof(float)) },
            outputs: () => new[] { new Pin("done"), new Pin("a/b", typeof(float)) },
            searchItem: (def) =>
            {
                var o = new Button(() =>
                {
                    // create serialized node.

                    if (UltNoodleEditor.NewNodeBowl == null) return;
                    var nod = UltNoodleEditor.NewNodeBowl.AddNode(def.Name, this).MatchDef(def);

                    nod.BookTag = "div_floats";

                    nod.Position = UltNoodleEditor.NewNodePos;
                    UltNoodleEditor.NewNodeBowl.Validate(); // update ui 
                });
                o.text = def.Name;
                return o;
            }));

        allDefs.Add(new NodeDef(this, "math.greater",
            inputs: () => new[] { new Pin("Exec"), new Pin("a", typeof(float)), new Pin("b", typeof(float)) },
            outputs: () => new[] { new Pin("done"), new Pin("a > b", typeof(bool)) },
            searchItem: (def) =>
            {
                var o = new Button(() =>
                {
                    // create serialized node.

                    if (UltNoodleEditor.NewNodeBowl == null) return;
                    var nod = UltNoodleEditor.NewNodeBowl.AddNode(def.Name, this).MatchDef(def);

                    nod.BookTag = "greater";

                    nod.Position = UltNoodleEditor.NewNodePos;
                    UltNoodleEditor.NewNodeBowl.Validate(); // update ui 
                });
                o.text = def.Name;
                return o;
            }));

        allDefs.Add(new NodeDef(this, "math.lesser",
            inputs: () => new[] { new Pin("Exec"), new Pin("a", typeof(float)), new Pin("b", typeof(float)) },
            outputs: () => new[] { new Pin("done"), new Pin("a < b", typeof(bool)) },
            searchItem: (def) =>
            {
                var o = new Button(() =>
                {
                    // create serialized node.

                    if (UltNoodleEditor.NewNodeBowl == null) return;
                    var nod = UltNoodleEditor.NewNodeBowl.AddNode(def.Name, this).MatchDef(def);

                    nod.BookTag = "lesser";

                    nod.Position = UltNoodleEditor.NewNodePos;
                    UltNoodleEditor.NewNodeBowl.Validate(); // update ui 
                });
                o.text = def.Name;
                return o;
            }));
        #endregion

        allDefs.Add(new NodeDef(this, "math.set_float_var",
            inputs: () => new[] { new Pin("Exec"), new Pin("name", typeof(string), @const: true), new Pin("value", typeof(float)) },
            outputs: () => new[] { new Pin("done") },
            searchItem: (def) =>
            {
                var o = new Button(() =>
                {
                    // create serialized node.

                    if (UltNoodleEditor.NewNodeBowl == null) return;
                    var nod = UltNoodleEditor.NewNodeBowl.AddNode(def.Name, this).MatchDef(def);

                    nod.BookTag = "set_float_var";

                    nod.Position = UltNoodleEditor.NewNodePos;
                    UltNoodleEditor.NewNodeBowl.Validate(); // update ui 
                });
                o.text = def.Name;
                return o;
            }));

        allDefs.Add(new NodeDef(this, "math.get_float_var",
                inputs: () => new[] { new Pin("Exec"), new Pin("name", typeof(string), @const:true) },
                outputs: () => new[] { new Pin("done"), new Pin("value", typeof(float)) },
                searchItem: (def) =>
                {
                    var o = new Button(() =>
                    {
                        // create serialized node.

                        if (UltNoodleEditor.NewNodeBowl == null) return;
                        var nod = UltNoodleEditor.NewNodeBowl.AddNode(def.Name, this).MatchDef(def);

                        nod.BookTag = "get_float_var";

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
                onTrue.gameObject.AddComponent<LifeCycleEvtEditorRunner>();
                var onFalse = new GameObject("if False", typeof(LifeCycleEvents)).GetComponent<LifeCycleEvents>();
                onFalse.transform.parent = dataRoot;
                onFalse.gameObject.SetActive(false);
                onFalse.gameObject.AddComponent<LifeCycleEvtEditorRunner>();

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
                {
                    MethodInfo v3Mult = typeof(Vector3).GetMethod("op_Multiply", UltEventUtils.AnyAccessBindings, null, new Type[] { typeof(Vector3), typeof(float) }, null);

                    var makeA = new PersistentCall(v3Mult, null);
                    makeA.PersistentArguments[0].Vector3 = new Vector3(0, 1, 0);
                    if (node.DataInputs[0].Source != null) new PendingConnection(node.DataInputs[0].Source, evt, makeA, 1).Connect(dataRoot);
                    else makeA.PersistentArguments[1].Float = node.DataInputs[0].DefaultFloatValue;
                    evt.PersistentCallsList.Add(makeA);

                    var makeB = new PersistentCall(v3Mult, null);
                    makeB.PersistentArguments[0].Vector3 = new Vector3(0, 1, 0);
                    if (node.DataInputs[1].Source != null) new PendingConnection(node.DataInputs[1].Source, evt, makeB, 1).Connect(dataRoot);
                    else makeB.PersistentArguments[1].Float = node.DataInputs[1].DefaultFloatValue;
                    evt.PersistentCallsList.Add(makeB);

                    MethodInfo v3Add = typeof(Vector3).GetMethod("op_Addition", UltEventUtils.AnyAccessBindings, null, new Type[] { typeof(Vector3), typeof(Vector3) }, null);
                    var subAB = new PersistentCall(v3Add, null);
                    subAB.PersistentArguments[0].FSetType(PersistentArgumentType.ReturnValue);
                    subAB.PersistentArguments[0].FSetString(typeof(Vector3).AssemblyQualifiedName);
                    subAB.PersistentArguments[0].FSetInt(evt.PersistentCallsList.Count - 2);
                    subAB.PersistentArguments[1].FSetString(typeof(Vector3).AssemblyQualifiedName);
                    subAB.PersistentArguments[1].FSetType(PersistentArgumentType.ReturnValue);
                    subAB.PersistentArguments[1].FSetInt(evt.PersistentCallsList.Count - 1);
                    evt.PersistentCallsList.Add(subAB);

                    // dot extract float
                    var dotter = new PersistentCall(typeof(Vector3).GetMethod("Dot"), null);
                    dotter.PersistentArguments[0].FSetType(PersistentArgumentType.ReturnValue);
                    dotter.PersistentArguments[0].FSetInt(evt.PersistentCallsList.Count - 1);
                    dotter.PersistentArguments[0].FSetString(typeof(Vector3).AssemblyQualifiedName);
                    dotter.PersistentArguments[1].FSetType(PersistentArgumentType.Vector3);
                    dotter.PersistentArguments[1].Vector3 = Vector3.up;
                    evt.PersistentCallsList.Add(dotter);

                    node.DataOutputs[0].CompEvt = evt;
                    node.DataOutputs[0].CompCall = dotter;

                    var nextNode = node.FlowOutputs[0].Target?.Node;
                    if (nextNode != null)
                        nextNode.Book.CompileNode(evt, nextNode, dataRoot);
                    return;
                }
            case "mul_floats":
                {
                    MethodInfo v3Mult = typeof(Vector3).GetMethod("op_Multiply", UltEventUtils.AnyAccessBindings, null, new Type[] { typeof(Vector3), typeof(float) }, null);

                    var makeA = new PersistentCall(v3Mult, null);
                    makeA.PersistentArguments[0].Vector3 = new Vector3(0, 1, 0);
                    if (node.DataInputs[0].Source != null) new PendingConnection(node.DataInputs[0].Source, evt, makeA, 1).Connect(dataRoot);
                    else makeA.PersistentArguments[1].Float = node.DataInputs[0].DefaultFloatValue;
                    evt.PersistentCallsList.Add(makeA);

                    var mul2 = new PersistentCall(v3Mult, null);
                    mul2.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.Count - 1, typeof(Vector3)); // vec = makeA
                    if (node.DataInputs[1].Source != null) new PendingConnection(node.DataInputs[1].Source, evt, mul2, 1).Connect(dataRoot);
                    else mul2.PersistentArguments[1].Float = node.DataInputs[1].DefaultFloatValue;
                    evt.PersistentCallsList.Add(mul2);

                    // dot extract float
                    var dotter = new PersistentCall(typeof(Vector3).GetMethod("Dot"), null);
                    dotter.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.Count - 1, typeof(Vector3));
                    dotter.PersistentArguments[1].FSetType(PersistentArgumentType.Vector3);
                    dotter.PersistentArguments[1].Vector3 = Vector3.up;
                    evt.PersistentCallsList.Add(dotter);

                    node.DataOutputs[0].CompEvt = evt;
                    node.DataOutputs[0].CompCall = dotter;

                    var nextNode = node.FlowOutputs[0].Target?.Node;
                    if (nextNode != null)
                        nextNode.Book.CompileNode(evt, nextNode, dataRoot);
                    return;
                }
            case "div_floats":
                {
                    MethodInfo v3Mult = typeof(Vector3).GetMethod("op_Multiply", UltEventUtils.AnyAccessBindings, null, new Type[] { typeof(Vector3), typeof(float) }, null);

                    var makeA = new PersistentCall(v3Mult, null);
                    makeA.PersistentArguments[0].Vector3 = new Vector3(0, 1, 0);
                    if (node.DataInputs[0].Source != null) new PendingConnection(node.DataInputs[0].Source, evt, makeA, 1).Connect(dataRoot);
                    else makeA.PersistentArguments[1].Float = node.DataInputs[0].DefaultFloatValue;
                    evt.PersistentCallsList.Add(makeA);

                    MethodInfo v3Div = typeof(Vector3).GetMethod("op_Division", UltEventUtils.AnyAccessBindings, null, new Type[] { typeof(Vector3), typeof(float) }, null);
                    var div2 = new PersistentCall(v3Div, null);
                    div2.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.Count - 1, typeof(Vector3)); // vec = makeA
                    if (node.DataInputs[1].Source != null) new PendingConnection(node.DataInputs[1].Source, evt, div2, 1).Connect(dataRoot);
                    else div2.PersistentArguments[1].Float = node.DataInputs[1].DefaultFloatValue;
                    evt.PersistentCallsList.Add(div2);

                    // dot extract float
                    var dotter = new PersistentCall(typeof(Vector3).GetMethod("Dot"), null);
                    dotter.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.Count - 1, typeof(Vector3));
                    dotter.PersistentArguments[1].FSetType(PersistentArgumentType.Vector3);
                    dotter.PersistentArguments[1].Vector3 = Vector3.up;
                    evt.PersistentCallsList.Add(dotter);

                    node.DataOutputs[0].CompEvt = evt;
                    node.DataOutputs[0].CompCall = dotter;

                    var nextNode = node.FlowOutputs[0].Target?.Node;
                    if (nextNode != null)
                        nextNode.Book.CompileNode(evt, nextNode, dataRoot);
                    return;
                }
            case "sub_floats":
                {
                    MethodInfo v3Mult = typeof(Vector3).GetMethod("op_Multiply", UltEventUtils.AnyAccessBindings, null, new Type[] { typeof(Vector3), typeof(float) }, null);

                    var makeA = new PersistentCall(v3Mult, null);
                    makeA.PersistentArguments[0].Vector3 = new Vector3(0, 1, 0);
                    if (node.DataInputs[0].Source != null) new PendingConnection(node.DataInputs[0].Source, evt, makeA, 1).Connect(dataRoot);
                    else makeA.PersistentArguments[1].Float = node.DataInputs[0].DefaultFloatValue;
                    evt.PersistentCallsList.Add(makeA);

                    var makeB = new PersistentCall(v3Mult, null);
                    makeB.PersistentArguments[0].Vector3 = new Vector3(0, 1, 0);
                    if (node.DataInputs[1].Source != null) new PendingConnection(node.DataInputs[1].Source, evt, makeB, 1).Connect(dataRoot);
                    else makeB.PersistentArguments[1].Float = node.DataInputs[1].DefaultFloatValue;
                    evt.PersistentCallsList.Add(makeB);

                    MethodInfo v3Sub = typeof(Vector3).GetMethod("op_Subtraction", UltEventUtils.AnyAccessBindings, null, new Type[] { typeof(Vector3), typeof(Vector3) }, null);
                    var subAB = new PersistentCall(v3Sub, null);
                    subAB.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.Count-2, typeof(Vector3));
                    subAB.PersistentArguments[1].ToRetVal(evt.PersistentCallsList.Count - 1, typeof(Vector3));
                    evt.PersistentCallsList.Add(subAB);

                    // dot extract float
                    var dotter = new PersistentCall(typeof(Vector3).GetMethod("Dot"), null);
                    dotter.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.Count - 1, typeof(Vector3));
                    dotter.PersistentArguments[1].FSetType(PersistentArgumentType.Vector3);
                    dotter.PersistentArguments[1].Vector3 = Vector3.up;
                    evt.PersistentCallsList.Add(dotter);

                    node.DataOutputs[0].CompEvt = evt;
                    node.DataOutputs[0].CompCall = dotter;

                    var nextNode = node.FlowOutputs[0].Target?.Node;
                    if (nextNode != null)
                        nextNode.Book.CompileNode(evt, nextNode, dataRoot);
                    return;
                }
            case "greater":
                {
                    // given a, b
                    // a > b?

                    var clampr = new PersistentCall(typeof(UnityEngine.Mathf).GetMethod("Clamp", new Type[] { typeof(float), typeof(float), typeof(float) }), null);

                    // Connect a to Clamp.value
                    if (node.DataInputs[0].Source != null) new PendingConnection(node.DataInputs[0].Source, evt, clampr, 0).Connect(dataRoot);
                    else clampr.PersistentArguments[0].Float = node.DataInputs[0].DefaultFloatValue;

                    // Connect b to Clamp.max
                    if (node.DataInputs[1].Source != null) new PendingConnection(node.DataInputs[1].Source, evt, clampr, 2).Connect(dataRoot);
                    else clampr.PersistentArguments[2].Float = node.DataInputs[1].DefaultFloatValue;

                    evt.PersistentCallsList.Add(clampr);

                    // if clamp_out == b, a > b
                    var comper = new PersistentCall(typeof(object).GetMethod("Equals", new Type[] { typeof(object), typeof(object) }), null);
                    comper.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.Count-1, typeof(object));

                    // Connect b to comper.b
                    if (node.DataInputs[1].Source != null) new PendingConnection(node.DataInputs[1].Source, evt, comper, 1).Connect(dataRoot);
                    else
                        comper.PersistentArguments[1].FSetType(PersistentArgumentType.Float).Float = node.DataInputs[1].DefaultFloatValue;

                    evt.PersistentCallsList.Add(comper);


                    node.DataOutputs[0].CompEvt = evt;
                    node.DataOutputs[0].CompCall = comper;

                    var nextNode = node.FlowOutputs[0].Target?.Node;
                    if (nextNode != null)
                        nextNode.Book.CompileNode(evt, nextNode, dataRoot);
                    return;
                }
            case "lesser":
                {
                    // given a, b
                    // b > a?

                    var clampr = new PersistentCall(typeof(UnityEngine.Mathf).GetMethod("Clamp", new Type[] { typeof(float), typeof(float), typeof(float) }), null);

                    // Connect b to Clamp.value
                    if (node.DataInputs[1].Source != null) new PendingConnection(node.DataInputs[1].Source, evt, clampr, 0).Connect(dataRoot);
                    else clampr.PersistentArguments[0].Float = node.DataInputs[1].DefaultFloatValue;

                    // Connect a to Clamp.max
                    if (node.DataInputs[0].Source != null) new PendingConnection(node.DataInputs[0].Source, evt, clampr, 2).Connect(dataRoot);
                    else clampr.PersistentArguments[2].Float = node.DataInputs[0].DefaultFloatValue;

                    evt.PersistentCallsList.Add(clampr);

                    // if clamp_out == a, b > a
                    var comper = new PersistentCall(typeof(object).GetMethod("Equals", new Type[] { typeof(object), typeof(object) }), null);
                    comper.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.Count - 1, typeof(object));

                    // Connect a to comper.b
                    if (node.DataInputs[0].Source != null) new PendingConnection(node.DataInputs[0].Source, evt, comper, 1).Connect(dataRoot);
                    else
                        comper.PersistentArguments[1].FSetType(PersistentArgumentType.Float).Float = node.DataInputs[0].DefaultFloatValue;

                    evt.PersistentCallsList.Add(comper);


                    node.DataOutputs[0].CompEvt = evt;
                    node.DataOutputs[0].CompCall = comper;

                    var nextNode = node.FlowOutputs[0].Target?.Node;
                    if (nextNode != null)
                        nextNode.Book.CompileNode(evt, nextNode, dataRoot);
                    return;
                }
            case "set_float_var":
                {
                    // CONST ONLY
                    if (node.DataInputs[0].Source != null) node.DataInputs[0].Connect(null);

                    Transform counter = dataRoot.Find("float_var_" + node.DataInputs[0].DefaultStringValue);
                    counter ??= dataRoot.StoreTransform("float_var_" + node.DataInputs[0].DefaultStringValue);

                    MethodInfo v3Mult = typeof(Vector3).GetMethod("op_Multiply", UltEventUtils.AnyAccessBindings, null, new Type[] { typeof(Vector3), typeof(float) }, null);
                    var makeA = new PersistentCall(v3Mult, null);
                    makeA.PersistentArguments[0].Vector3 = new Vector3(0, 1, 0);
                    if (node.DataInputs[1].Source != null) new PendingConnection(node.DataInputs[1].Source, evt, makeA, 1).Connect(dataRoot);
                    else makeA.PersistentArguments[1].Float = node.DataInputs[1].DefaultFloatValue;
                    evt.PersistentCallsList.Add(makeA);

                    MethodInfo setLocalPos = typeof(Transform).GetMethod("set_localPosition", UltEventUtils.AnyAccessBindings);
                    var setVarPos = new PersistentCall(setLocalPos, counter);
                    setVarPos.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.Count - 1, typeof(Vector3));
                    evt.PersistentCallsList.Add(setVarPos);

                    var nextNode = node.FlowOutputs[0].Target?.Node;
                    if (nextNode != null)
                        nextNode.Book.CompileNode(evt, nextNode, dataRoot);
                    return;
                }
            case "get_float_var":
                {
                    // CONST ONLY
                    if (node.DataInputs[0].Source != null) node.DataInputs[0].Connect(null);

                    Transform counter = dataRoot.Find("float_var_" + node.DataInputs[0].DefaultStringValue);
                    counter ??= dataRoot.StoreTransform("float_var_" + node.DataInputs[0].DefaultStringValue);

                    MethodInfo getLocalPos = typeof(Transform).GetMethod("get_localPosition", UltEventUtils.AnyAccessBindings);
                    var setVarPos = new PersistentCall(getLocalPos, counter);
                    evt.PersistentCallsList.Add(setVarPos);

                    // dot extract float
                    var dotter = new PersistentCall(typeof(Vector3).GetMethod("Dot"), null);
                    dotter.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.Count - 1, typeof(Vector3));
                    dotter.PersistentArguments[1].FSetType(PersistentArgumentType.Vector3);
                    dotter.PersistentArguments[1].Vector3 = Vector3.up;
                    evt.PersistentCallsList.Add(dotter);

                    node.DataOutputs[0].CompCall = dotter;
                    node.DataOutputs[0].CompEvt = evt;

                    var nextNode = node.FlowOutputs[0].Target?.Node;
                    if (nextNode != null)
                        nextNode.Book.CompileNode(evt, nextNode, dataRoot);
                    return;
                }
        }
    }
}
#endif