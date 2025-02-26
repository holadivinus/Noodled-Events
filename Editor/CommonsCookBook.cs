#if UNITY_EDITOR
using Codice.CM.SEIDInfo;
using Codice.CM.WorkspaceServer;
using NoodledEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UltEvents;
using UnityEngine;
using UnityEngine.UIElements;
using WebSocketSharp;
using static NoodledEvents.CookBook.NodeDef;


public class CommonsCookBook : CookBook
{
    public override void CollectDefs(List<NodeDef> allDefs)
    {
        // flow.if
        allDefs.Add(new NodeDef(this, "flow.if", 
            inputs:() => new[] { new Pin("Exec"), new Pin("condition", typeof(bool)) },
            outputs:() => new[] { new Pin("true"), new Pin("false") },
            bookTag: "if"));

        #region MATH
        allDefs.Add(new NodeDef(this, "math.add_floats",
            inputs: () => new[] { new Pin("Exec"), new Pin("a", typeof(float)), new Pin("b", typeof(float)) },
            outputs: () => new[] { new Pin("done"), new Pin("a+b", typeof(float)) },
            bookTag: "add_floats"));

        allDefs.Add(new NodeDef(this, "math.sub_floats",
            inputs: () => new[] { new Pin("Exec"), new Pin("a", typeof(float)), new Pin("b", typeof(float)) },
            outputs: () => new[] { new Pin("done"), new Pin("a-b", typeof(float)) },
            bookTag: "sub_floats"));

        allDefs.Add(new NodeDef(this, "math.mul_floats",
            inputs: () => new[] { new Pin("Exec"), new Pin("a", typeof(float)), new Pin("b", typeof(float)) },
            outputs: () => new[] { new Pin("done"), new Pin("a*b", typeof(float)) },
            bookTag: "mul_floats"));

        allDefs.Add(new NodeDef(this, "math.div_floats",
            inputs: () => new[] { new Pin("Exec"), new Pin("a", typeof(float)), new Pin("b", typeof(float)) },
            outputs: () => new[] { new Pin("done"), new Pin("a/b", typeof(float)) },
            bookTag: "div_floats"));

        allDefs.Add(new NodeDef(this, "math.greater",
            inputs: () => new[] { new Pin("Exec"), new Pin("a", typeof(float)), new Pin("b", typeof(float)) },
            outputs: () => new[] { new Pin("done"), new Pin("a > b", typeof(bool)) },
            bookTag: "greater"));

        allDefs.Add(new NodeDef(this, "math.lesser",
            inputs: () => new[] { new Pin("Exec"), new Pin("a", typeof(float)), new Pin("b", typeof(float)) },
            outputs: () => new[] { new Pin("done"), new Pin("a < b", typeof(bool)) },
            bookTag: "lesser"));
        #endregion

        #region VARIABLES
        // vars.set_bowl_float_var
        allDefs.Add(new NodeDef(this, "vars.set_bowl_float_var",
            inputs: () => new[] { new Pin("Exec"), new Pin("name", typeof(string), @const: true), new Pin("value", typeof(float)) },
            outputs: () => new[] { new Pin("done") },
            bookTag: "set_bowl_float_var"
            ));

        // vars.set_bowl_float_var
        allDefs.Add(new NodeDef(this, "vars.get_bowl_float_var", // Is this supposed to be SET? Book tag says Get
                inputs: () => new[] { new Pin("Exec"), new Pin("name", typeof(string), @const:true) },
                outputs: () => new[] { new Pin("done"), new Pin("value", typeof(float)) },
                bookTag: "get_bowl_float_var"));

        foreach (var storager in PendingConnection.CompStoragers)
        {
            // scene storagers
            allDefs.Add(new NodeDef(this, $"vars.set_scene_{storager.Key.GetFriendlyName()}_var",
                inputs: () => new[] { new Pin("Exec"), new Pin("name", typeof(string), @const: true), new Pin("value", storager.Key) },
                outputs: () => new[] { new Pin("done") },
                bookTag: $"set_scene_{storager.Key.GetFriendlyName()}_var"));
            allDefs.Add(new NodeDef(this, $"vars.get_scene_{storager.Key.GetFriendlyName()}_var",
                inputs: () => new[] { new Pin("Exec"), new Pin("name", typeof(string), @const: true) },
                outputs: () => new[] { new Pin("done"), new Pin("value", storager.Key) },
                bookTag: $"get_scene_{storager.Key.GetFriendlyName()}_var"));
            allDefs.Add(new NodeDef(this, $"vars.get_or_init_scene_{storager.Key.GetFriendlyName()}_var",
                inputs: () => new[] { new Pin("Exec"), new Pin("name", typeof(string), @const: true), new Pin("default/init value", storager.Key, @const: true) },
                outputs: () => new[] { new Pin("done"), new Pin("value", storager.Key) },
                bookTag: $"get_or_init_scene_{storager.Key.GetFriendlyName()}_var"));
            // gobj storagers
            allDefs.Add(new NodeDef(this, $"vars.set_gobj_{storager.Key.GetFriendlyName()}_var",
                inputs: () => new[] { new Pin("Exec"), new Pin("name", typeof(string), @const: true), new Pin("value", storager.Key), new Pin("gobj", typeof(GameObject), true) },
                outputs: () => new[] { new Pin("done") },
                bookTag: $"set_gobj_{storager.Key.GetFriendlyName()}_var"));
            allDefs.Add(new NodeDef(this, $"vars.get_gobj_{storager.Key.GetFriendlyName()}_var",
                inputs: () => new[] { new Pin("Exec"), new Pin("name", typeof(string), @const: true), new Pin("gobj", typeof(GameObject), true) },
                outputs: () => new[] { new Pin("done"), new Pin("value", storager.Key) },
                bookTag: $"get_gobj_{storager.Key.GetFriendlyName()}_var"));
            allDefs.Add(new NodeDef(this, $"vars.get_or_init_gobj_{storager.Key.GetFriendlyName()}_var",
                inputs: () => new[] { new Pin("Exec"), new Pin("name", typeof(string), @const: true), new Pin("gobj", typeof(GameObject), true), new Pin("default/init value", storager.Key, @const: true) },
                outputs: () => new[] { new Pin("done"), new Pin("value", storager.Key) },
                bookTag: $"get_or_init_gobj_{storager.Key.GetFriendlyName()}_var"));
        }
        #endregion

        // Async
        allDefs.Add(new NodeDef(this, "async.Wait",
            inputs: () => new[] { new Pin("Start"), new Pin("seconds", typeof(float)) },
            outputs: () => new[] { new Pin("On Started"), new Pin("After \"seconds\"") },
            bookTag: "wait"));
    }
    private static MethodInfo SetActive = typeof(GameObject).GetMethod("SetActive");
    private static PropertyInfo GetSetLocPos = typeof(Transform).GetProperty("localPosition");
    private static MethodInfo Translate = typeof(Transform).GetMethod("Translate", new Type[] { typeof(float), typeof(float), typeof(float) });
    public override void CompileNode(UltEventBase evt, SerializedNode node, Transform dataRoot)
    {
        void SetSceneVar(GameObject gobjRoot)
        {
            // Type Agnostic set var code

            // CONST ONLY
            if (node.DataInputs[0].Source != null) node.DataInputs[0].Connect(null); // disconnect var_name vary cables

            // scene vars are made to be retargetted!
            var storagerData = PendingConnection.CompStoragers[node.DataInputs[1].Type];
            string varName = $"{(gobjRoot ? "gobj_" : "TEMP_scene_")}{node.DataInputs[1].Type.Type.GetFriendlyName()}_var_" + node.DataInputs[0].DefaultStringValue;

            Component tempVarRef = (gobjRoot?.transform ?? dataRoot).Find(varName)?.GetComponent(storagerData.Item1);
            if (tempVarRef == null)
            {
                tempVarRef = (gobjRoot?.transform ?? dataRoot).StoreComp(storagerData.Item1, varName);
                if (storagerData.Item2.PropertyType.IsValueType)
                    storagerData.Item2.SetValue(tempVarRef, Activator.CreateInstance(storagerData.Item2.PropertyType));
                else storagerData.Item2.SetValue(tempVarRef, null);
            }
            

            var setCall = new PersistentCall(storagerData.Item2.SetMethod, tempVarRef);
            if (node.DataInputs[1].Source != null) new PendingConnection(node.DataInputs[1].Source, evt, setCall, 0).Connect(dataRoot); // retval
            else // const (todo: add more types)
            {
                if (node.DataInputs[1].Type.Type == typeof(float))
                    setCall.PersistentArguments[0].Float = node.DataInputs[1].DefaultFloatValue;
                else if (node.DataInputs[1].Type.Type == typeof(UnityEngine.Object))
                    setCall.PersistentArguments[0].Object = node.DataInputs[1].DefaultObject;
                else if (node.DataInputs[1].Type.Type == typeof(int))
                    setCall.PersistentArguments[0].Int = node.DataInputs[1].DefaultIntValue;
                else if (node.DataInputs[1].Type.Type == typeof(string))
                    setCall.PersistentArguments[0].String = node.DataInputs[1].DefaultStringValue;
                else if (node.DataInputs[1].Type.Type == typeof(bool))
                    setCall.PersistentArguments[0].Bool = node.DataInputs[1].DefaultBoolValue;
                else if (node.DataInputs[1].Type.Type == typeof(Vector3))
                    setCall.PersistentArguments[0].Vector3 = node.DataInputs[1].DefaultVector3Value;
                //else
            }
            evt.PersistentCallsList.Add(setCall);

            var nextNode = node.FlowOutputs[0].Target?.Node;
            if (nextNode != null)
                nextNode.Book.CompileNode(evt, nextNode, dataRoot);
            return;
        }
        void GetSceneVar(GameObject gobjRoot)
        {
            // Type Agnostic Get var code

            // CONST ONLY
            if (node.DataInputs[0].Source != null) node.DataInputs[0].Connect(null); // disconnect var_name vary cables

            // scene vars are made to be retargetted!
            var storagerData = PendingConnection.CompStoragers[node.DataOutputs[0].Type];
            string varName = $"{(gobjRoot ? "gobj_" : "TEMP_scene_")}{node.DataOutputs[0].Type.Type.GetFriendlyName()}_var_" + node.DataInputs[0].DefaultStringValue;

            Component tempVarRef = (gobjRoot?.transform ?? dataRoot).Find(varName)?.GetComponent(storagerData.Item1);
            if (tempVarRef == null)
            {
                tempVarRef = (gobjRoot?.transform ?? dataRoot).StoreComp(storagerData.Item1, varName);
                if (node.Name.StartsWith("vars.get_or_init_gobj_"))
                {
                    storagerData.Item2.SetValue(tempVarRef, node.DataInputs[2].GetDefault());
                } else
                {
                    // get before init??? we have to use type defaults then :/
                    if (storagerData.Item2.PropertyType.IsValueType)
                        storagerData.Item2.SetValue(tempVarRef, Activator.CreateInstance(storagerData.Item2.PropertyType));
                    else storagerData.Item2.SetValue(tempVarRef, null);
                }
            }

            var getCall = new PersistentCall(storagerData.Item2.GetMethod, tempVarRef);
            evt.PersistentCallsList.Add(getCall);
            node.DataOutputs[0].CompCall = getCall;
            node.DataOutputs[0].CompEvt = evt;

            var nextNode = node.FlowOutputs[0].Target?.Node;
            if (nextNode != null)
                nextNode.Book.CompileNode(evt, nextNode, dataRoot);
            return;
        }
        switch (node.BookTag)
        {
            case "wait":
                // first, we need to figure what data needs transfer
                // i mean do we? PendingConnection handles this shit
                /*
                List<NoodleDataOutput> transfz = new List<NoodleDataOutput>();
                bool IsBeforeWait(SerializedNode node)
                {
                    // if next node is Wait, ret true
                    foreach (var flow in node.FlowOutputs)
                    {
                        if (flow.Target != null && flow.Target.Node == node)
                            return true;
                    }
                    // else, run above again for next nodes
                    // if next node finds it, ret true
                    // else false
                    foreach (var flow in node.FlowOutputs)
                    {
                        if (flow.Target != null && IsBeforeWait(flow.Target.Node))
                            return true;
                    }
                    return false;
                }
                foreach (var descendent in node.GatherDescendants())
                    foreach (var postWaitInput in descendent.DataInputs)
                        if (postWaitInput.Source != null && IsBeforeWait(postWaitInput.Source.Node))
                            transfz.Add(postWaitInput.Source);

                transfz.Distinct();*/

                // just make the evts and proceed under the root.

                var immediateNext = node.FlowOutputs[0].Target?.Node;
                if (immediateNext != null)
                    immediateNext.Book.CompileNode(evt, immediateNext, dataRoot);

                var slowNext = node.FlowOutputs[1].Target?.Node;
                if (slowNext != null)
                {
                    Transform ats = dataRoot.Find("async templates");
                    if (!ats) ats = dataRoot.StoreTransform("async templates");

                    
                    // okay so like this is supposed to be async
                    // new, copied on run dataroot:
                    var slowDataRoot = ats.StoreComp<LifeCycleEvents>("Async DataRoot");
                    slowDataRoot.gameObject.SetActive(true); // so OnEnabled runs when cloned
                    slowDataRoot.EnableEvent.EnsurePCallList();
                    slowDataRoot.gameObject.AddComponent<LifeCycleEvtEditorRunner>();

                    var delayedEvt = slowDataRoot.gameObject.AddComponent<DelayedUltEventHolder>();
                    delayedEvt.Event.EnsurePCallList();

                    var startDelay = new PersistentCall(typeof(DelayedUltEventHolder).GetMethod("Invoke", new Type[] { }), delayedEvt);
                    slowDataRoot.EnableEvent.PersistentCallsList.Add(startDelay);

                    slowNext.Book.CompileNode(delayedEvt.Event, slowNext, slowDataRoot.transform);

                    // okay now we just need to insert cloning pcalls to original evt!
                    // since PendingConnection naturally put compstoragers under slowDataRoot.transform :3
                    
                    // delay pin in functionality
                    if (node.DataInputs[0].Source == null) delayedEvt.Delay = node.DataInputs[0].DefaultFloatValue;
                    else
                    {
                        var setTimer = new PersistentCall(typeof(DelayedUltEventHolder).GetProperty("Delay").SetMethod, delayedEvt);
                        new PendingConnection(node.DataInputs[0].Source, evt, setTimer, 0).Connect(dataRoot);
                        evt.PersistentCallsList.Add(setTimer);
                    }

                    // cloning bs
                    var cloneCall = new PersistentCall(typeof(UnityEngine.Object).GetMethod("Instantiate", new Type[] { typeof(UnityEngine.Object), typeof(Transform) }), null);
                    cloneCall.PersistentArguments[0].FSetString(typeof(UnityEngine.Object).AssemblyQualifiedName).FSetType(PersistentArgumentType.Object).Object = slowDataRoot.gameObject;
                    cloneCall.PersistentArguments[1].FSetString(typeof(UnityEngine.Transform).AssemblyQualifiedName).FSetType(PersistentArgumentType.Object);
                    evt.PersistentCallsList.Add(cloneCall);

                    // cleanup
                    var delCall = new PersistentCall(typeof(UnityEngine.Object).GetMethod("DestroyImmediate", new Type[] { typeof(UnityEngine.Object) }), null);
                    delCall.PersistentArguments[0].FSetType(PersistentArgumentType.Object).Object = slowDataRoot.gameObject;
                    delayedEvt.Event.PersistentCallsList.Add(delCall);

                    // i can't believe how easy this one was
                }
                return;
            case "if":
                // if statementtt :/
                // requires two evts, one for true 1 for false
                var onTrue = new GameObject("if True", typeof(LifeCycleEvents)).GetComponent<LifeCycleEvents>();
                onTrue.transform.parent = dataRoot;
                onTrue.gameObject.SetActive(false);
                onTrue.EnableEvent.EnsurePCallList();
                onTrue.gameObject.AddComponent<LifeCycleEvtEditorRunner>();
                var onFalse = new GameObject("if False", typeof(LifeCycleEvents)).GetComponent<LifeCycleEvents>();
                onFalse.transform.parent = dataRoot;
                onFalse.gameObject.SetActive(false);
                onFalse.EnableEvent.EnsurePCallList();
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

                    // reset to false so no accidental triggers happen
                    var rs11 = new PersistentCall(SetActive, onTrue.gameObject);  // reset onTrue
                    rs11.FSetArguments(new PersistentArgument(typeof(bool)));
                    evt.PersistentCallsList.Add(rs11);
                    var rs22 = new PersistentCall(SetActive, onFalse.gameObject); // reset onFalse
                    rs22.FSetArguments(new PersistentArgument(typeof(bool)));
                    evt.PersistentCallsList.Add(rs22);

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
                    
                    var greater = new PersistentCall();
                    greater.FSetTarget(null);
                    greater.FSetMethodName("SLZ.Bonelab.VoidLogic.MathUtilities, Assembly-CSharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null.IsApproximatelyEqualToOrGreaterThan");
                    greater.FSetArguments(new PersistentArgument(typeof(float)), new PersistentArgument(typeof(float)));

                    // Connect a to Clamp.value
                    if (node.DataInputs[0].Source != null) new PendingConnection(node.DataInputs[0].Source, evt, greater, 0).Connect(dataRoot);
                    else greater.PersistentArguments[0].Float = node.DataInputs[0].DefaultFloatValue;

                    // Connect b to Clamp.max
                    if (node.DataInputs[1].Source != null) new PendingConnection(node.DataInputs[1].Source, evt, greater, 1).Connect(dataRoot);
                    else greater.PersistentArguments[1].Float = node.DataInputs[1].DefaultFloatValue;
                    evt.PersistentCallsList.Add(greater);

                    node.DataOutputs[0].CompEvt = evt;
                    node.DataOutputs[0].CompCall = greater;

                    var nextNode = node.FlowOutputs[0].Target?.Node;
                    if (nextNode != null)
                        nextNode.Book.CompileNode(evt, nextNode, dataRoot);
                    return;
                }
            case "lesser":
                {
                    // given a, b
                    // a > b?

                    var lesser = new PersistentCall();
                    lesser.FSetTarget(null);
                    lesser.FSetMethodName("SLZ.Bonelab.VoidLogic.MathUtilities, Assembly-CSharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null.IsApproximatelyEqualToOrLessThan");
                    lesser.FSetArguments(new PersistentArgument(typeof(float)), new PersistentArgument(typeof(float)));

                    // Connect a to Clamp.value
                    if (node.DataInputs[0].Source != null) new PendingConnection(node.DataInputs[0].Source, evt, lesser, 0).Connect(dataRoot);
                    else lesser.PersistentArguments[0].Float = node.DataInputs[0].DefaultFloatValue;

                    // Connect b to Clamp.max
                    if (node.DataInputs[1].Source != null) new PendingConnection(node.DataInputs[1].Source, evt, lesser, 1).Connect(dataRoot);
                    else lesser.PersistentArguments[1].Float = node.DataInputs[1].DefaultFloatValue;
                    evt.PersistentCallsList.Add(lesser);

                    node.DataOutputs[0].CompEvt = evt;
                    node.DataOutputs[0].CompCall = lesser;

                    var nextNode = node.FlowOutputs[0].Target?.Node;
                    if (nextNode != null)
                        nextNode.Book.CompileNode(evt, nextNode, dataRoot);
                    return;
                }
            case "set_float_var": // old name support
            case "set_bowl_float_var":
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
            case "get_float_var": // old name support
            case "get_bowl_float_var":
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
            default:
                if (node.BookTag.Contains("_scene_") && node.BookTag.EndsWith("_var"))
                {
                    if (node.BookTag.StartsWith("get_"))
                        GetSceneVar(null);
                    else SetSceneVar(null); 
                    return;
                } else if (node.BookTag.Contains("_gobj_") && node.BookTag.EndsWith("_var"))
                {
                    if (node.BookTag.StartsWith("get_"))
                        GetSceneVar((GameObject)node.DataInputs[1].DefaultObject);
                    else SetSceneVar((GameObject)node.DataInputs[2].DefaultObject);
                    return;
                }
                return;
        }
    }

    public override void PostCompile(SerializedBowl bowl)
    {
        handleSceneVars(bowl);
    }
    private void handleSceneVars(SerializedBowl bowl)
    {
        // First, figure var gameobjs
        List<(GameObject, Type)> vars = new();
        foreach (var node in bowl.NodeDatas)
            if (node.Book == this && ((node.BookTag.StartsWith("get_scene_") || node.BookTag.StartsWith("set_scene_")) && node.BookTag.EndsWith("_var")))
            {
                // find the compstorager transf
                for (int i = 0; i < bowl.LastGenerated.transform.childCount; i++)
                {
                    GameObject c = bowl.LastGenerated.transform.GetChild(i).gameObject;
                    if (c.name.StartsWith("TEMP_scene_") && c.gameObject.name.EndsWith("_var_" + node.DataInputs[0].DefaultStringValue))
                    {
                        vars.Add((c, node.BookTag.StartsWith("set") ? node.DataInputs[1].Type : node.DataOutputs[0].Type));
                        break;
                    }
                }
            }
        vars = vars.Distinct().ToList();

        // early exit if none
        if (vars.Count == 0) return;

        // we have vars and need to do a ton of bs
        // move entry event to LastGenerated, so it can possibly be retargetted
        var movedEntryEvt = bowl.LastGenerated.transform.StoreComp<UltEventHolder>("Moved Entry Event");
        movedEntryEvt.Event.EnsurePCallList();
        movedEntryEvt.Event.CopyFrom(bowl.Event);

        bowl.Event.Clear(); // god

        // I FORGOR ABT PARAMS
        // we need to transfer params to the moved entry evt :(
        foreach (var pcall in movedEntryEvt.Event.PersistentCallsList.ToArray())
            for (int i = 0; i < pcall.PersistentArguments.Length; i++)
            {
                PersistentArgument argument = pcall.PersistentArguments[i];
                if (argument.Type == PersistentArgumentType.Parameter)
                {
                    new PendingConnection(bowl.EntryNode.DataOutputs[argument.FGetInt()], movedEntryEvt.Event, pcall, i).Connect(bowl.LastGenerated.transform);
                    // so this is awkward
                    // PendingConnection.Connect() is expected to run before a pcall is in the list, not after
                    var getr = movedEntryEvt.Event.PersistentCallsList[movedEntryEvt.Event.PersistentCallsList.Count - 1];
                    movedEntryEvt.Event.PersistentCallsList.Remove(getr);
                    movedEntryEvt.Event.PersistentCallsList.SafeInsert(movedEntryEvt.Event.PersistentCallsList.IndexOf(pcall), getr);
                    pcall.PersistentArguments[i].FSetInt(movedEntryEvt.Event.PersistentCallsList.IndexOf(pcall) - 1);
                }
            }


        var varEnsurementRoot = bowl.LastGenerated.transform.StoreTransform("Scene Var Ensurement");

        foreach ((GameObject, Type) varData in vars)
        {
            string varName = varData.Item1.name.Substring(5); // the scene_var name
            var varStoragerData = PendingConnection.CompStoragers[varData.Item2]; // this tells us what compstorager the scene_var uses
            var reffedTempStorager = varData.Item1.GetComponent(varStoragerData.Item1);

            // omfg im an NOT setting all this shii up manually
            // spawn et prefab with ensurement and targ compstorager fetching (made with noodled evts!!!! the tool is helping the tool develop lol)
            GameObject ensurer = GameObject.Instantiate(VarEnsurer, varEnsurementRoot);
            ensurer.SetActive(true);
            ensurer.name = varName.Split('_').Last() + " Ensurer";
            ensurer.transform.parent = varEnsurementRoot;
            ensurer.AddComponent<LifeCycleEvtEditorRunner>();

            // give ensurer the targ var comp store name
            ensurer.transform.GetChild(0).gameObject.name = varName;

            // place the compstorager on the scene_var template
            Component storger = ensurer.transform.GetChild(0).gameObject.AddComponent(varStoragerData.Item1);
            // we need to figure the default value for this scene var
            // crawl down the bowl untill a vars.get_or_init happens
            bool SearchForDef(SerializedNode curNode)
            {
                if (curNode.Book == this && curNode.Name.StartsWith("vars.get_or_init_scene_") && varName.EndsWith(curNode.DataInputs[0].DefaultStringValue))
                {
                    // found it!
                    varStoragerData.Item2.SetValue(storger, curNode.DataInputs[1].GetDefault());
                    return true;
                } else
                    foreach (var o in curNode.FlowOutputs)
                        if (o.Target != null && SearchForDef(o.Target.Node))
                            return true;
                return false;
            }
            if (!SearchForDef(bowl.EntryNode))
            {
                // didnt find initter, use default value
                // get before init??? we have to use type defaults then :/
                if (varStoragerData.Item2.PropertyType.IsValueType)
                    varStoragerData.Item2.SetValue(storger, Activator.CreateInstance(varStoragerData.Item2.PropertyType));
                else varStoragerData.Item2.SetValue(storger, null);
            }
            if (storger is TextMeshPro tmp)
            {
                tmp.enabled = false;
                tmp.GetComponent<Renderer>().enabled = false;
            }

            // give the ensurer the targ compstorager type
            ensurer.transform.GetChild(2).GetChild(2).GetComponent<UltEventHolder>().Event.PersistentCallsList[0].PersistentArguments[0].String = varStoragerData.Item1.AssemblyQualifiedName;
            ensurer.transform.GetChild(2).GetChild(2).GetChild(0).GetComponent<UltEventHolder>().Event.PersistentCallsList[0].PersistentArguments[0].String = varStoragerData.Item1.AssemblyQualifiedName;
            // god

            // okay, now that we've got the targ scene_var_storager in VarEnsurer.retval,
            // we just need to add some ultevent code to:
            // 1. Retarget evts that use this scene_var
            // 2. destroy this VarEnsurer instance
            // RAHHHHH

            var postEnsurementEvt = ensurer.transform.GetChild(2).GetChild(1).GetComponent<LifeCycleEvents>().EnableEvent;
            Type XRComp = GetExtType("XRInteractorAffordanceStateProvider", XRAssembly);
            var CompStoragerXRRef = ensurer.transform.GetChild(2).GetChild(4).GetComponent(XRComp);

            List<(Component, UltEventBase)> evtsThatUseVar = new();
            foreach (Component comp in bowl.EventHolder.gameObject.GetComponentsInChildren(typeof(Component), true)) // scan all comps for Ult fields :(
                foreach (var field in comp.GetType().GetFields(UltEventUtils.AnyAccessBindings))
                    if (typeof(UltEventBase).IsAssignableFrom(field.FieldType))
                    {
                        UltEventBase evt = field.GetValue(comp) as UltEventBase;
                        if (evt == null || evt.PersistentCallsList == null || !evt.PersistentCallsList.Any(pcall => pcall.Target == reffedTempStorager))
                            continue;
                        evtsThatUseVar.Add((comp, evt));
                    }

            // set the tempreffer's targ, to TempRef
            var tempRefRef = ensurer.transform.GetChild(3).GetComponent(XRComp);
            MethodInfo toJson = typeof(JsonUtility).GetMethod("ToJson", new Type[] { typeof(object) });
            MethodInfo regReplace = typeof(System.Text.RegularExpressions.Regex).GetMethod("Replace", new Type[] { typeof(string), typeof(string), typeof(string) });
            XRComp.GetField("m_InteractorSource", UltEventUtils.AnyAccessBindings).SetValue(tempRefRef, reffedTempStorager);
            {
                // post Ensurement, we'll get TempRef as a SerializedReference
                var jsoTemper = new PersistentCall(toJson, null);
                jsoTemper.PersistentArguments[0].FSetType(PersistentArgumentType.Object).Object = tempRefRef;
                postEnsurementEvt.PersistentCallsList.Add(jsoTemper);
                // now cut the pre-ref
                var prerefCut = new PersistentCall(regReplace, null);
                prerefCut.PersistentArguments[0].ToRetVal(postEnsurementEvt.PersistentCallsList.Count - 1, typeof(string));
                prerefCut.PersistentArguments[1].String = "^(.*\"m_InteractorSource\":)";
                prerefCut.PersistentArguments[2].String = "";
                postEnsurementEvt.PersistentCallsList.Add(prerefCut);
                // now cut the post-ref
                var postrefCut = new PersistentCall(regReplace, null);
                postrefCut.PersistentArguments[0].ToRetVal(postEnsurementEvt.PersistentCallsList.Count - 1, typeof(string));
                postrefCut.PersistentArguments[1].String = ",\"m_I(.)*";
                postrefCut.PersistentArguments[2].String = "";
                postEnsurementEvt.PersistentCallsList.Add(postrefCut);
            }

            // now, foreach event that refs this var, we'll retarget them from the above SerializedRef to the below SerializedRef
            //omfg ultevents suck
            {
                // jsonify the XRComp that's reffing our ensured targ scene compstorager
                var jsoTemper = new PersistentCall(toJson, null);
                jsoTemper.PersistentArguments[0].FSetType(PersistentArgumentType.Object).Object = ensurer.transform.GetChild(2).GetChild(3).GetComponent(XRComp);
                postEnsurementEvt.PersistentCallsList.Add(jsoTemper);
                // now cut the pre-ref
                var prerefCut = new PersistentCall(regReplace, null);
                prerefCut.PersistentArguments[0].ToRetVal(postEnsurementEvt.PersistentCallsList.Count - 1, typeof(string));
                prerefCut.PersistentArguments[1].String = "^(.*\"m_InteractorSource\":)";
                prerefCut.PersistentArguments[2].String = "";
                postEnsurementEvt.PersistentCallsList.Add(prerefCut);
                // now cut the post-ref
                var postrefCut = new PersistentCall(regReplace, null);
                postrefCut.PersistentArguments[0].ToRetVal(postEnsurementEvt.PersistentCallsList.Count - 1, typeof(string));
                postrefCut.PersistentArguments[1].String = ",\"m_I(.)*";
                postrefCut.PersistentArguments[2].String = "";
                postEnsurementEvt.PersistentCallsList.Add(postrefCut);
            }
            int tempRefIdx = postEnsurementEvt.PersistentCallsList.Count - 4;
            int sceneRefIdx = postEnsurementEvt.PersistentCallsList.Count - 1;

            /*var dbg = new PersistentCall(typeof(Debug).GetMethod("Log", new Type[] { typeof(object) }), null);
            dbg.PersistentArguments[0].ToRetVal(tempRefIdx, typeof(object));
            postEnsurementEvt.PersistentCallsList.Add(dbg);

            var dbg2 = new PersistentCall(typeof(Debug).GetMethod("Log", new Type[] { typeof(object) }), null);
            dbg2.PersistentArguments[0].ToRetVal(sceneRefIdx, typeof(object));
            postEnsurementEvt.PersistentCallsList.Add(dbg2);*/

            foreach (var evt in evtsThatUseVar)
            {
                // this event has pcalls where Target=reffedTempStorager
                // in postEnsurementEvt, we need to toJson evt, RegReplace

                var jsoTemper = new PersistentCall(toJson, null);
                jsoTemper.PersistentArguments[0].FSetType(PersistentArgumentType.Object).Object = evt.Item1;
                postEnsurementEvt.PersistentCallsList.Add(jsoTemper);
                // replace tempRef with sceneRef
                var temp2Scene = new PersistentCall(regReplace, null);
                temp2Scene.PersistentArguments[0].ToRetVal(postEnsurementEvt.PersistentCallsList.Count - 1, typeof(string));
                temp2Scene.PersistentArguments[1].ToRetVal(tempRefIdx, typeof(string));
                temp2Scene.PersistentArguments[2].ToRetVal(sceneRefIdx, typeof(string));
                postEnsurementEvt.PersistentCallsList.Add(temp2Scene);
                //fromjson overwrite
                var jsoOver = new PersistentCall(typeof(JsonUtility).GetMethod("FromJsonOverwrite"), null);
                jsoOver.PersistentArguments[0].ToRetVal(postEnsurementEvt.PersistentCallsList.Count - 1, typeof(string));
                jsoOver.PersistentArguments[1].FSetType(PersistentArgumentType.Object).Object = evt.Item1;
                postEnsurementEvt.PersistentCallsList.Add(jsoOver);
            }
            // temp is done for; Kill.
            var tdelCall = new PersistentCall(typeof(UnityEngine.Object).GetMethod("DestroyImmediate", new Type[] { typeof(UnityEngine.Object) }), null);
            tdelCall.PersistentArguments[0].FSetType(PersistentArgumentType.Object).Object = reffedTempStorager.gameObject;
            postEnsurementEvt.PersistentCallsList.Add(tdelCall);
            // finally, kill off this so it's done
            var delCall = new PersistentCall(typeof(UnityEngine.Object).GetMethod("DestroyImmediate", new Type[] { typeof(UnityEngine.Object) }), null);
            delCall.PersistentArguments[0].FSetType(PersistentArgumentType.Object).Object = ensurer;
            postEnsurementEvt.PersistentCallsList.Add(delCall);
        }
        // make entry event enable varEnsurementRoot, causing the ensurers to ensure & die
        var sact = new PersistentCall(typeof(GameObject).GetMethod("SetActive"), varEnsurementRoot.gameObject);
        sact.PersistentArguments[0].Bool = true;
        bowl.Event.PersistentCallsList.Add(sact);
        bowl.Event.PersistentCallsList.Add(new PersistentCall(typeof(UltEventHolder).GetMethod("Invoke", new Type[] { }), movedEntryEvt));
    }
    [SerializeField] GameObject VarEnsurer;
}
#endif