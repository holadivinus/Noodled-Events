#if UNITY_EDITOR
using NoodledEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UltEvents;
using UnityEngine;
using static NoodledEvents.CookBook.NodeDef;


public class CommonsCookBook : CookBook
{
    public override void CollectDefs(Action<IEnumerable<NodeDef>, float> progressCallback, Action completedCallback)
    {
        List<NodeDef> allDefs = new();

        #region FLOW
        // flow.if
        allDefs.Add(new NodeDef(this, "flow.if",
            inputs: () => new[] { new Pin("Exec"), new Pin("condition", typeof(bool)) },
            outputs: () => new[] { new Pin("true"), new Pin("false") },
            bookTag: "if"));

        // flow.redirect
        allDefs.Add(new NodeDef(this, "flow.redirect",
            inputs: () => new[] { new Pin("") },
            outputs: () => new[] { new Pin("") },
            bookTag: "flow_redirect"));

        // data.redirect
        allDefs.Add(new NodeDef(this, "data.redirect",
            inputs: () => new[] { new Pin("", typeof(object)) },
            outputs: () => new[] { new Pin("", typeof(object)) },
            bookTag: "data_redirect"));
        #endregion

        #region FLOAT MATH
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

        allDefs.Add(new NodeDef(this, "math.greater_floats",
            inputs: () => new[] { new Pin("Exec"), new Pin("a", typeof(float)), new Pin("b", typeof(float)) },
            outputs: () => new[] { new Pin("done"), new Pin("a > b", typeof(bool)) },
            bookTag: "greater"));

        allDefs.Add(new NodeDef(this, "math.lesser_floats",
            inputs: () => new[] { new Pin("Exec"), new Pin("a", typeof(float)), new Pin("b", typeof(float)) },
            outputs: () => new[] { new Pin("done"), new Pin("a < b", typeof(bool)) },
            bookTag: "lesser"));
        #endregion

        #region INT MATH
        allDefs.Add(new NodeDef(this, "math.add_ints",
            inputs: () => new[] { new Pin("Exec"), new Pin("a", typeof(int)), new Pin("b", typeof(int)) },
            outputs: () => new[] { new Pin("done"), new Pin("a+b", typeof(int)) },
            bookTag: "add_ints"));

        allDefs.Add(new NodeDef(this, "math.sub_ints",
            inputs: () => new[] { new Pin("Exec"), new Pin("a", typeof(int)), new Pin("b", typeof(int)) },
            outputs: () => new[] { new Pin("done"), new Pin("a-b", typeof(int)) },
            bookTag: "sub_ints"));

        allDefs.Add(new NodeDef(this, "math.mul_ints",
            inputs: () => new[] { new Pin("Exec"), new Pin("a", typeof(int)), new Pin("b", typeof(int)) },
            outputs: () => new[] { new Pin("done"), new Pin("a*b", typeof(int)) },
            bookTag: "mul_ints"));

        allDefs.Add(new NodeDef(this, "math.div_ints",
            inputs: () => new[] { new Pin("Exec"), new Pin("a", typeof(int)), new Pin("b", typeof(int)) },
            outputs: () => new[] { new Pin("done"), new Pin("a/b", typeof(int)) },
            bookTag: "div_ints"));

        allDefs.Add(new NodeDef(this, "math.greater_ints",
            inputs: () => new[] { new Pin("Exec"), new Pin("a", typeof(int)), new Pin("b", typeof(int)) },
            outputs: () => new[] { new Pin("done"), new Pin("a > b", typeof(bool)) },
            bookTag: "greater_ints"));

        allDefs.Add(new NodeDef(this, "math.lesser_ints",
            inputs: () => new[] { new Pin("Exec"), new Pin("a", typeof(int)), new Pin("b", typeof(int)) },
            outputs: () => new[] { new Pin("done"), new Pin("a < b", typeof(bool)) },
            bookTag: "lesser_ints"));
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
                inputs: () => new[] { new Pin("Exec"), new Pin("name", typeof(string), @const: true) },
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

        // Global Sys.Object vars
        allDefs.Add(new NodeDef(this, "vars.set_global_SystemObject_var",
            inputs: () => new[] { new Pin("Exec"), new Pin("Var Name", typeof(string), @const: true), new Pin("System.Object", typeof(object)) },
            outputs: () => new[] { new Pin("done") },
            bookTag: "set_global_SystemObject_var"
            ));
        allDefs.Add(new NodeDef(this, "vars.get_global_SystemObject_var",
                inputs: () => new[] { new Pin("Exec"), new Pin("Var Name", typeof(string), @const: true) },
                outputs: () => new[] { new Pin("done"), new Pin("System.Object", typeof(object)) },
                bookTag: "get_global_SystemObject_var"));

        // Saved Sys.String vars
        allDefs.Add(new NodeDef(this, "vars.set_saved_string_var",
            inputs: () => new[] { new Pin("Exec"), new Pin("Var Name", typeof(string), @const: true), new Pin("save data", typeof(string)) },
            outputs: () => new[] { new Pin("done") },
            bookTag: "set_saved_string_var"
            ));
        allDefs.Add(new NodeDef(this, "vars.get_saved_string_var",
                inputs: () => new[] { new Pin("Exec"), new Pin("Var Name", typeof(string), @const: true) },
                outputs: () => new[] { new Pin("done"), new Pin("save data", typeof(string)) },
                bookTag: "get_saved_string_var"));
        #endregion

        #region ASYNC
        // Async doesnt really need its own region but it looks cleaner if every catagory has a region
        allDefs.Add(new NodeDef(this, "async.Wait",
            inputs: () => new[] { new Pin("Start"), new Pin("seconds", typeof(float)),
            new Pin("embedded", typeof(bool), true)},
            outputs: () => new[] { new Pin("On Started"), new Pin("After \"seconds\"") },
            bookTag: "wait"));
        #endregion

        #region DELEGATES
        allDefs.Add(new NodeDef(this, "delegates.Create",
            inputs: () => new[] { new Pin("Create") },
            outputs: () => new[] { new Pin("On Created"), new Pin("DelegateID", typeof(string)), new Pin("Delegate", typeof(Delegate)), new Pin("On Triggered") },
            bookTag: "delegate0"));
        for (int i = 1; i < 5; i++)
        {
            List<Pin> outs = new();
            outs.AddRange(new[] { new Pin("On Created"), new Pin("DelegateID", typeof(string)), new Pin("Delegate", typeof(Delegate)), new Pin("On Triggered") });
            for (int i2 = 1; i2 <= i; i2++)
                outs.Add(new Pin($"Parameter {i2}", typeof(object)));

            List<Pin> ins = new() { new Pin("Create") };
            for (int i2 = 1; i2 <= i; i2++)
                ins.Add(new Pin($"Param Type {i2}", typeof(Type), true));

            allDefs.Add(new NodeDef(this, $"delegates.Create_with_{i}_parameter",
                inputs: ins.ToArray,
                outputs: outs.ToArray,
                bookTag: $"delegate{i}"));

        }
        allDefs.Add(new NodeDef(this, "delegates.Fetch",
            inputs: () => new[] { new Pin("Fetch"), new Pin("DelegateID", typeof(string)) },
            outputs: () => new[] { new Pin("Fetched"), new Pin("Delegate", typeof(Delegate)) },
            bookTag: "fetch_delegate"));
        #endregion

        progressCallback.Invoke(allDefs, 1);
        completedCallback.Invoke();
    }

    const string dictStoreTypeStr = "UnityEngine.Rendering.LensFlareCommonSRP, Unity.RenderPipelines.Core.Runtime, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
    const string dictStoreFieldStr = "m_Padlock";
    public override void CompileNode(UltEventBase evt, SerializedNode node, Transform dataRoot)
    {
        base.CompileNode(evt, node, dataRoot);

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
                }
                else
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
        void math_ints_op(string op_name, Type outType)
        {
            var SqlInt32 = typeof(System.Data.SqlTypes.SqlInt32);
            var SqlInt32Implicit = SqlInt32.GetMethod("op_Implicit", new Type[] { typeof(int) });
            var SqlInt32Operation = SqlInt32.GetMethod(op_name);

            var impCallA = new PersistentCall(SqlInt32Implicit, null);
            if (node.DataInputs[0].Source != null) new PendingConnection(node.DataInputs[0].Source, evt, impCallA, 0).Connect(dataRoot);
            else impCallA.PersistentArguments[0].Int = node.DataInputs[0].DefaultIntValue;
            var impCallA_idx = evt.PersistentCallsList.Count;
            evt.PersistentCallsList.Add(impCallA);

            var impCallB = new PersistentCall(SqlInt32Implicit, null);
            if (node.DataInputs[1].Source != null) new PendingConnection(node.DataInputs[1].Source, evt, impCallB, 0).Connect(dataRoot);
            else impCallB.PersistentArguments[0].Int = node.DataInputs[1].DefaultIntValue;
            var impCallB_idx = evt.PersistentCallsList.Count;
            evt.PersistentCallsList.Add(impCallB);

            var opCall = new PersistentCall(SqlInt32Operation, null);
            opCall.PersistentArguments[0].ToRetVal(impCallA_idx, SqlInt32);
            opCall.PersistentArguments[1].ToRetVal(impCallB_idx, SqlInt32);
            var opCall_idx = evt.PersistentCallsList.Count;
            evt.PersistentCallsList.Add(opCall);

            // string.Concat(object arg0) is stripped, using string.Concat(object arg0, object arg1)
            var stringConcatCall = new PersistentCall(typeof(string).GetMethod("Concat", new Type[] { typeof(object), typeof(object) }), null);
            stringConcatCall.PersistentArguments[0].ToRetVal(opCall_idx, typeof(object));
            stringConcatCall.PersistentArguments[1].ToObjVal(null, typeof(object)); // check if generates valid ult
            var concatCall_idx = evt.PersistentCallsList.Count;
            evt.PersistentCallsList.Add(stringConcatCall);

            var intConvertCall = new PersistentCall(outType.GetMethod("Parse", new Type[] { typeof(string) }), null);
            intConvertCall.PersistentArguments[0].ToRetVal(concatCall_idx, typeof(string));
            var intConvertCall_idx = evt.PersistentCallsList.Count;
            evt.PersistentCallsList.Add(intConvertCall);

            node.DataOutputs[0].CompEvt = evt;
            node.DataOutputs[0].CompCall = intConvertCall;
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
                    // embedded mode:
                    if (node.DataInputs.Length == 2 && node.DataInputs[1].DefaultBoolValue)
                    {
                        // is embedded - place at root

                        var asyncEvt = node.Bowl.LastGenerated.transform.StoreComp<DelayedUltEventHolder>("Embedded Async Event");
                        asyncEvt.gameObject.SetActive(true);

                        if (node.DataInputs[0].Source == null) asyncEvt.Delay = node.DataInputs[0].DefaultFloatValue;
                        else
                        {
                            var setTimer = new PersistentCall(typeof(DelayedUltEventHolder).GetProperty("Delay").SetMethod, asyncEvt);
                            new PendingConnection(node.DataInputs[0].Source, evt, setTimer, 0).Connect(dataRoot);
                            evt.PersistentCallsList.Add(setTimer);
                        }

                        var startDelay2 = new PersistentCall(typeof(DelayedUltEventHolder).GetMethod("Invoke", new Type[] { }), asyncEvt);
                        evt.PersistentCallsList.Add(startDelay2);

                        slowNext.Book.CompileNode(asyncEvt.Event, slowNext, asyncEvt.transform);
                        return;
                    }

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

                // oh if it's const just generate in the same event
                if (node.DataInputs[0].Source == null)
                {
                    // when hardcoded act hardcoded
                    var next = node.FlowOutputs[node.DataInputs[0].DefaultBoolValue ? 0 : 1];
                    if (next.Target != null)
                        next.Target.Node.Book.CompileNode(evt, next.Target.Node, dataRoot);
                    return;
                }
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


                if (node.DataInputs[0].Source != null) // condition varies
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
                    var makeAidx = evt.PersistentCallsList.Count;
                    evt.PersistentCallsList.Add(makeA);

                    var makeB = new PersistentCall(v3Mult, null);
                    makeB.PersistentArguments[0].Vector3 = new Vector3(0, 1, 0);
                    if (node.DataInputs[1].Source != null) new PendingConnection(node.DataInputs[1].Source, evt, makeB, 1).Connect(dataRoot);
                    else makeB.PersistentArguments[1].Float = node.DataInputs[1].DefaultFloatValue;
                    var makeBidx = evt.PersistentCallsList.Count;
                    evt.PersistentCallsList.Add(makeB);

                    MethodInfo v3Add = typeof(Vector3).GetMethod("op_Addition", UltEventUtils.AnyAccessBindings, null, new Type[] { typeof(Vector3), typeof(Vector3) }, null);
                    var subAB = new PersistentCall(v3Add, null);
                    subAB.PersistentArguments[0].ToRetVal(makeAidx, typeof(Vector3));
                    subAB.PersistentArguments[1].ToRetVal(makeBidx, typeof(Vector3));
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
                    var makeAidx = evt.PersistentCallsList.Count;
                    evt.PersistentCallsList.Add(makeA);

                    var makeB = new PersistentCall(v3Mult, null);
                    makeB.PersistentArguments[0].Vector3 = new Vector3(0, 1, 0);
                    if (node.DataInputs[1].Source != null) new PendingConnection(node.DataInputs[1].Source, evt, makeB, 1).Connect(dataRoot);
                    else makeB.PersistentArguments[1].Float = node.DataInputs[1].DefaultFloatValue;
                    var makeBidx = evt.PersistentCallsList.Count;
                    evt.PersistentCallsList.Add(makeB);

                    MethodInfo v3Sub = typeof(Vector3).GetMethod("op_Subtraction", UltEventUtils.AnyAccessBindings, null, new Type[] { typeof(Vector3), typeof(Vector3) }, null);
                    var subAB = new PersistentCall(v3Sub, null);
                    subAB.PersistentArguments[0].ToRetVal(makeAidx, typeof(Vector3));
                    subAB.PersistentArguments[1].ToRetVal(makeBidx, typeof(Vector3));
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
            case "add_ints":
                {
                    math_ints_op("op_Addition", typeof(int));

                    var nextNode = node.FlowOutputs[0].Target?.Node;
                    if (nextNode != null)
                        nextNode.Book.CompileNode(evt, nextNode, dataRoot);

                    return;
                }
            case "sub_ints":
                {
                    math_ints_op("op_Subtraction", typeof(int));

                    var nextNode = node.FlowOutputs[0].Target?.Node;
                    if (nextNode != null)
                        nextNode.Book.CompileNode(evt, nextNode, dataRoot);
                    return;
                }
            case "mul_ints":
                {
                    math_ints_op("op_Multiply", typeof(int));

                    var nextNode = node.FlowOutputs[0].Target?.Node;
                    if (nextNode != null)
                        nextNode.Book.CompileNode(evt, nextNode, dataRoot);
                    return;
                }
            case "div_ints":
                {
                    math_ints_op("op_Division", typeof(int));

                    var nextNode = node.FlowOutputs[0].Target?.Node;
                    if (nextNode != null)
                        nextNode.Book.CompileNode(evt, nextNode, dataRoot);
                    return;
                }
            case "greater_ints":
                {
                    math_ints_op("op_GreaterThan", typeof(bool));

                    var nextNode = node.FlowOutputs[0].Target?.Node;
                    if (nextNode != null)
                        nextNode.Book.CompileNode(evt, nextNode, dataRoot);
                    return;
                }
            case "lesser_ints":
                {
                    math_ints_op("op_LessThan", typeof(bool));

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
            case "fetch_delegate":
                {
                    // get dict
                    int dictField3 = evt.PersistentCallsList.AddGetFieldInfo(typeof(System.TimeZone).GetField("s_InternalSyncObject", (BindingFlags)60));
                    int gotDict4 = evt.PersistentCallsList.AddRunMethod(
                        UltNoodleRuntimeExtensions.GetFieldValue, dictField3, @params: new object[1]);

                    // for my sanity this is localized with a stupid format
                    var getGuidStr2 = MakeCall<string>("Format", typeof(string), typeof(object));
                    getGuidStr2.PersistentArguments[0].String = "{0}";
                    if (node.DataInputs[0].Source != null) new PendingConnection(node.DataInputs[0].Source, evt, getGuidStr2, 1).Connect(dataRoot);
                    else getGuidStr2.PersistentArguments[1].FSetType(PersistentArgumentType.String).FSetString(node.DataInputs[0].DefaultStringValue);
                    evt.PersistentCallsList.Add(getGuidStr2);

                    int gotValue = evt.PersistentCallsList.AddRunMethod(typeof(Dictionary<string, object>).GetMethod("TryGetValue"),
                            gotDict4, evt.PersistentCallsList.IndexOf(getGuidStr2), null);

                    int arrIdx = evt.PersistentCallsList.Count - 4;

                    var arrGetIdx = typeof(Array).GetMethod("GetValue", new Type[] { typeof(int) });
                    int getOut = evt.PersistentCallsList.AddRunMethod(arrGetIdx, arrIdx, new PersistentArgument(typeof(int)).FSetInt(1));

                    node.DataOutputs[0].CompEvt = evt;
                    node.DataOutputs[0].CompCall = evt.PersistentCallsList[getOut];

                    var evtNext2 = node.FlowOutputs[0].Target?.Node;
                    if (evtNext2 != null)
                        evtNext2.Book.CompileNode(evt, evtNext2, dataRoot);
                }
                return;
            case "set_global_SystemObject_var":
                {
                    // get dict
                    FieldInfo dictField = typeof(System.TimeZone).GetField("s_InternalSyncObject", (BindingFlags)60);
                    int gotDictA = evt.PersistentCallsList.AddGetFieldValue(dictField, null);

                    // if null, create dict...
                    var dictCreateEvt = dataRoot.StoreComp<LifeCycleEvents>("dict null check");
                    {
                        dictCreateEvt.EnableEvent = new UltEvent();
                        dictCreateEvt.gameObject.AddComponent<LifeCycleEvtEditorRunner>();
                        dictCreateEvt.EnableEvent.EnsurePCallList();

                        // Create Dict
                        int madeDictIdx = dictCreateEvt.EnableEvent.PersistentCallsList.AddCreateInstance<Dictionary<string, object>>();
                        int dictField4 = dictCreateEvt.EnableEvent.PersistentCallsList.AddSetFieldValue(dictField, -1, madeDictIdx);
                    }
                    var compareCall = MakeCall<object>("Equals", new Type[] { typeof(object), typeof(object) });
                    compareCall.PersistentArguments[0].ToRetVal(gotDictA, typeof(object));
                    compareCall.PersistentArguments[1].FSetType(PersistentArgumentType.Object);
                    evt.PersistentCallsList.Add(compareCall);

                    var decideCallA = MakeCall<GameObject>("SetActive", dictCreateEvt.gameObject, typeof(bool));
                    decideCallA.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.IndexOf(compareCall), typeof(bool));
                    evt.PersistentCallsList.Add(decideCallA);

                    var decideCallB = MakeCall<GameObject>("SetActive", dictCreateEvt.gameObject, typeof(bool));
                    decideCallB.PersistentArguments[0].Bool = false;
                    evt.PersistentCallsList.Add(decideCallB);

                    // get dict again, since now it's garuanteed to exist
                    int gotDictB = evt.PersistentCallsList.AddGetFieldValue(dictField, null);

                    MethodInfo setMethod = typeof(Dictionary<string, object>).GetMethod("set_Item", (BindingFlags)60);
                    var nameArg = new PersistentArgument(typeof(string)); nameArg.String = node.DataInputs[0].DefaultStringValue;
                    if (node.DataInputs[1].Source == null)
                    {
                        var constVal = node.DataInputs[1].GetDefault();
                        var arg = new PersistentArgument(constVal.GetType());
                        arg.Value = constVal;
                        evt.PersistentCallsList.AddRunMethod(setMethod, gotDictB, nameArg, arg);
                    }
                    else
                    {
                        if (node.DataInputs[1].Source.CompEvt != evt) throw new Exception("Can't transfer data for global vars! (todo)");
                        if (node.DataInputs[1].Source.UseCompAsParam) throw new Exception("Can't use event params for global vars! (todo)");
                        evt.PersistentCallsList.AddRunMethod(setMethod, gotDictB, nameArg, evt.PersistentCallsList.IndexOf(node.DataInputs[1].Source.CompCall));
                    }

                    evt.PersistentCallsList.AddDebugLog(gotDictB, true);

                    // compile next node.
                    var evtNext3 = node.FlowOutputs[0].Target?.Node;
                    if (evtNext3 != null)
                        evtNext3.Book.CompileNode(evt, evtNext3, dataRoot);
                }
                break;
            case "get_global_SystemObject_var":
                {
                    // get dict
                    FieldInfo dictField = typeof(System.TimeZone).GetField("s_InternalSyncObject", (BindingFlags)60);
                    int gotDictA = evt.PersistentCallsList.AddGetFieldValue(dictField, null);

                    // if null, create dict...
                    var dictCreateEvt = dataRoot.StoreComp<LifeCycleEvents>("dict null check");
                    {
                        dictCreateEvt.EnableEvent = new UltEvent();
                        dictCreateEvt.gameObject.AddComponent<LifeCycleEvtEditorRunner>();
                        dictCreateEvt.EnableEvent.EnsurePCallList();

                        // Create Dict
                        int madeDictIdx = dictCreateEvt.EnableEvent.PersistentCallsList.AddCreateInstance<Dictionary<string, object>>();
                        int dictField4 = dictCreateEvt.EnableEvent.PersistentCallsList.AddSetFieldValue(dictField, -1, madeDictIdx);
                    }
                    var compareCall = MakeCall<object>("Equals", new Type[] { typeof(object), typeof(object) });
                    compareCall.PersistentArguments[0].ToRetVal(gotDictA, typeof(object));
                    compareCall.PersistentArguments[1].FSetType(PersistentArgumentType.Object);
                    evt.PersistentCallsList.Add(compareCall);

                    var decideCallA = MakeCall<GameObject>("SetActive", dictCreateEvt.gameObject, typeof(bool));
                    decideCallA.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.IndexOf(compareCall), typeof(bool));
                    evt.PersistentCallsList.Add(decideCallA);

                    var decideCallB = MakeCall<GameObject>("SetActive", dictCreateEvt.gameObject, typeof(bool));
                    decideCallB.PersistentArguments[0].Bool = false;
                    evt.PersistentCallsList.Add(decideCallB);

                    // get dict again, since now it's garuanteed to exist
                    int gotDictB = evt.PersistentCallsList.AddGetFieldValue(dictField, null);

                    MethodInfo getMethod = typeof(Dictionary<string, object>).GetMethod("get_Item", (BindingFlags)60);
                    var nameArg = new PersistentArgument(typeof(string)); nameArg.String = node.DataInputs[0].DefaultStringValue;
                    int gotValue = evt.PersistentCallsList.AddRunMethod(getMethod, gotDictB, nameArg);

                    node.DataOutputs[0].CompCall = evt.PersistentCallsList[gotValue];
                    node.DataOutputs[0].CompEvt = evt;

                    // compile next node.
                    var evtNext3 = node.FlowOutputs[0].Target?.Node;
                    if (evtNext3 != null)
                        evtNext3.Book.CompileNode(evt, evtNext3, dataRoot);
                }
                break;
            case "set_saved_string_var":
                {
                    // kys
                    evt.PersistentCallsList.Add(MakeCall("SLZ.Bonelab.SaveData.DataManager, Assembly-CSharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null.get_ActiveSave"));
                    int activeSave = evt.PersistentCallsList.Count - 1;
                    int saveT = evt.PersistentCallsList.FindOrAddGetTyper("SLZ.Bonelab.SaveData.Save, Assembly-CSharp");
                    int progT = evt.PersistentCallsList.FindOrAddGetTyper("SLZ.Bonelab.SaveData.PlayerProgression, Assembly-CSharp");
                    int getProgMeth = evt.PersistentCallsList.FindOrAddGetMethodInfo(saveT, "get_Progression", new Type[] { }, new Type[] { }, progT);
                    int progression = evt.PersistentCallsList.AddRunMethod(getProgMeth, activeSave);
                    int dictT = evt.PersistentCallsList.FindOrAddGetTyper<Dictionary<string, Dictionary<string, System.Object>>>();
                    int getLevelState = evt.PersistentCallsList.FindOrAddGetMethodInfo(progT, "get_LevelState", new Type[] { }, new Type[] { }, dictT);
                    int levelState = evt.PersistentCallsList.AddRunMethod(getLevelState, progression);
                    int savedDict = evt.PersistentCallsList.AddRunMethod(typeof(Dictionary<string, Dictionary<string, System.Object>>).GetMethod("get_Item", (BindingFlags)60), levelState, new PersistentArgument(typeof(string)) { String = "G114" });
                    // we now have the data dict

                    if (node.DataInputs[1].Source == null)
                        evt.PersistentCallsList.AddRunMethod(typeof(Dictionary<string, System.Object>).GetMethod("set_Item"), savedDict, new PersistentArgument(typeof(string)) { String = node.DataInputs[0].DefaultStringValue }, new PersistentArgument(typeof(string)) { String = node.DataInputs[1].DefaultStringValue });
                    else
                    {
                        if (node.DataInputs[1].Source.CompEvt != evt) throw new Exception("Can't transfer data for saved vars! (todo)");
                        if (node.DataInputs[1].Source.UseCompAsParam) throw new Exception("Can't use event params for saved vars! (todo)");
                        evt.PersistentCallsList.AddRunMethod(typeof(Dictionary<string, System.Object>).GetMethod("set_Item"), savedDict, new PersistentArgument(typeof(string)) { String = node.DataInputs[0].DefaultStringValue }, evt.PersistentCallsList.IndexOf(node.DataInputs[1].Source.CompCall));
                    }


                    int saveFlag = evt.PersistentCallsList.FindOrAddGetTyper("SLZ.Marrow.SaveData.SaveFlags, SLZ.Marrow");

                    var getEnum = MakeCall<Enum>("Parse", new Type[] { typeof(Type), typeof(string) });
                    getEnum.PersistentArguments[0].ToRetVal(saveFlag, typeof(Type));
                    getEnum.PersistentArguments[1].FSetType(PersistentArgumentType.String).FSetString("Complete");
                    evt.PersistentCallsList.Add(getEnum);

                    var saveIt = MakeCall("SLZ.Bonelab.SaveData.DataManager, Assembly-CSharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null.TrySaveActiveSave");
                    saveIt.FSetArguments(new PersistentArgument().FSetType(PersistentArgumentType.ReturnValue).FSetInt(evt.PersistentCallsList.IndexOf(getEnum)).FSetString("SLZ.Marrow.SaveData.SaveFlags, SLZ.Marrow"));
                    evt.PersistentCallsList.Add(saveIt);

                    // compile next node.
                    var evtNext3 = node.FlowOutputs[0].Target?.Node;
                    if (evtNext3 != null)
                        evtNext3.Book.CompileNode(evt, evtNext3, dataRoot);
                }
                break;
            case "get_saved_string_var":
                {
                    // kys
                    evt.PersistentCallsList.Add(MakeCall("SLZ.Bonelab.SaveData.DataManager, Assembly-CSharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null.get_ActiveSave"));
                    int activeSave = evt.PersistentCallsList.Count - 1;
                    int saveT = evt.PersistentCallsList.FindOrAddGetTyper("SLZ.Bonelab.SaveData.Save, Assembly-CSharp");
                    int progT = evt.PersistentCallsList.FindOrAddGetTyper("SLZ.Bonelab.SaveData.PlayerProgression, Assembly-CSharp");
                    int getProgMeth = evt.PersistentCallsList.FindOrAddGetMethodInfo(saveT, "get_Progression", new Type[] { }, new Type[] { }, progT);
                    int progression = evt.PersistentCallsList.AddRunMethod(getProgMeth, activeSave);
                    int dictT = evt.PersistentCallsList.FindOrAddGetTyper<Dictionary<string, Dictionary<string, System.Object>>>();
                    int getLevelState = evt.PersistentCallsList.FindOrAddGetMethodInfo(progT, "get_LevelState", new Type[] { }, new Type[] { }, dictT);
                    int levelState = evt.PersistentCallsList.AddRunMethod(getLevelState, progression);
                    int savedDict = evt.PersistentCallsList.AddRunMethod(typeof(Dictionary<string, Dictionary<string, System.Object>>).GetMethod("get_Item", (BindingFlags)60), levelState, new PersistentArgument(typeof(string)) { String = "G114" });
                    // we now have the data dict

                    int getter = evt.PersistentCallsList.AddRunMethod(typeof(Dictionary<string, System.Object>).GetMethod("get_Item"), savedDict, new PersistentArgument(typeof(string)) { String = node.DataInputs[0].DefaultStringValue });

                    evt.PersistentCallsList.AddDebugLog(getter);

                    node.DataOutputs[0].CompEvt = evt;
                    node.DataOutputs[0].CompCall = evt.PersistentCallsList[getter]; //todo

                    // compile next node.
                    var evtNext3 = node.FlowOutputs[0].Target?.Node;
                    if (evtNext3 != null)
                        evtNext3.Book.CompileNode(evt, evtNext3, dataRoot);
                }
                break;
            case "flow_redirect": // basically a no-op, just continue
                {
                    var nextNode = node.FlowOutputs.FirstOrDefault()?.Target?.Node;
                    if (nextNode != null)
                        nextNode.Book.CompileNode(evt, nextNode, dataRoot);
                }
                break;
            default:
                if (node.BookTag.Contains("_scene_") && node.BookTag.EndsWith("_var"))
                {
                    if (node.BookTag.StartsWith("get_"))
                        GetSceneVar(null);
                    else SetSceneVar(null);
                    return;
                }
                else if (node.BookTag.Contains("_gobj_") && node.BookTag.EndsWith("_var"))
                {
                    if (node.BookTag.StartsWith("get_"))
                        GetSceneVar((GameObject)node.DataInputs[1].DefaultObject);
                    else SetSceneVar((GameObject)node.DataInputs[2].DefaultObject);
                    return;
                }
                else if (node.BookTag.StartsWith("delegate"))
                {
                    // added tons of awesome utility functions, 
                    // which should make this so much easier
                    // pt1: creating the evt
                    int ultType = -1;
                    Type evtType = null;
                    Type actionType = null;
                    // i cannot be bothered
                    switch (node.BookTag)
                    {
                        case "delegate0":
                            evtType = typeof(UltEvent);
                            actionType = typeof(Action);
                            break;
                        case "delegate1":
                            evtType = typeof(UltEvent<object>);
                            actionType = typeof(Action<>);
                            break;
                        case "delegate2":
                            evtType = typeof(UltEvent<object, object>);
                            actionType = typeof(Action<,>);
                            break;
                        case "delegate3":
                            evtType = typeof(UltEvent<object, object, object>);
                            actionType = typeof(Action<,,>);
                            break;
                        case "delegate4":
                            evtType = typeof(UltEvent<object, object, object, object>);
                            actionType = typeof(Action<,,,>);
                            break;
                    }

                    evt.PersistentCallsList.AddDebugLog("Getting type:");
                    ultType = evt.PersistentCallsList.FindOrAddGetTyper(evtType);
                    evt.PersistentCallsList.AddDebugLog("got!");
                    evt.PersistentCallsList.AddDebugLog(ultType);

                    if (node.DataInputs.Length > 0)
                        actionType = actionType.MakeGenericType(node.DataInputs.Select(di => Type.GetType(di.DefaultStringValue)).ToArray());

                    evt.PersistentCallsList.AddDebugLog("creating floater:");
                    // create the floating delegate target
                    var evtInstance = MakeCall<Activator>("CreateInstance", typeof(Type));
                    evtInstance.PersistentArguments[0].ToRetVal(ultType, typeof(Type));
                    evt.PersistentCallsList.Add(evtInstance);
                    int floatedIdx = evt.PersistentCallsList.Count - 1;
                    evt.PersistentCallsList.AddDebugLog("created! (cant tostring tho)");

                    // make a non-floater for compilation
                    var evtBase = dataRoot.StoreComp<UltEventHolder>("baseDelEvent");
                    evtBase.Event.FSetPCalls(new());
                    evtBase.gameObject.SetActive(true);

                    foreach (var o in node.DataOutputs)
                    {
                        if (o.Name.StartsWith("Parameter "))
                        {
                            o.CompEvt = evtBase.Event;
                            o.CompAsParam = int.Parse(o.Name.Replace("Parameter ", "")) - 1;
                            o.UseCompAsParam = true;
                        }
                    }

                    // compile the non-floater
                    var delNext = node.FlowOutputs[1].Target?.Node;
                    if (delNext != null)
                        delNext.Book.CompileNode(evtBase.Event, delNext, evtBase.transform);

                    evt.PersistentCallsList.AddDebugLog("copying from template to floater");
                    // copy PersistentCalls list from non-floater 2 floater
                    var getBaseEvent = MakeCall<UltEventHolder>("get_Event", evtBase);
                    evt.PersistentCallsList.Add(getBaseEvent);
                    var getPcallMethod = typeof(UltEventBase).GetMethod("get_PersistentCallsList", (BindingFlags)60);
                    int basePcalls = evt.PersistentCallsList.AddRunMethod(getPcallMethod, evt.PersistentCallsList.Count - 1);
                    evt.PersistentCallsList.AddDebugLog("got pcalls");


                    var pCallField = typeof(UltEventBase).GetField("_PersistentCalls", (BindingFlags)60);
                    evt.PersistentCallsList.AddSetFieldValue(pCallField, floatedIdx, basePcalls);
                    evt.PersistentCallsList.AddDebugLog("set pcalls");
                    evt.PersistentCallsList.AddDebugLog("floater is informed:");
                    evt.PersistentCallsList.AddDebugLog(floatedIdx);

                    evt.PersistentCallsList.AddDebugLog("forming delegate...");
                    // alr, the floater is informed!
                    // now, Make a Delegate from Invoke(...)
                    int invokeMethod = evt.PersistentCallsList.FindOrAddGetMethodInfo(evtType.GetMethod("Invoke", evtType.GenericTypeArguments));
                    int actionTypeIdx = evt.PersistentCallsList.FindOrAddGetTyper(actionType);

                    var createDel = typeof(Delegate).GetMethod("CreateDelegate", new Type[] { typeof(Type), typeof(object), typeof(MethodInfo) });
                    var makeDel = new PersistentCall(createDel, null);
                    makeDel.PersistentArguments[0].ToRetVal(actionTypeIdx, typeof(Type));
                    makeDel.PersistentArguments[1].ToRetVal(floatedIdx, typeof(object));
                    makeDel.PersistentArguments[2].ToRetVal(invokeMethod, typeof(MethodInfo));
                    evt.PersistentCallsList.Add(makeDel);
                    int delMade = evt.PersistentCallsList.Count - 1;
                    evt.PersistentCallsList.AddDebugLog("made delegate:");
                    evt.PersistentCallsList.AddDebugLog(delMade);

                    evt.PersistentCallsList.AddDebugLog("ensuring dict");
                    // now ensure the dict exists omfg

                    int dictStoreType = evt.PersistentCallsList.FindOrAddGetTyper(dictStoreTypeStr);
                    int dictField = evt.PersistentCallsList.AddGetFieldInfo(dictStoreType, dictStoreFieldStr);
                    int gotDict1 = evt.PersistentCallsList.AddGetFieldValue(dictField, null);

                    var nullCheck = MakeCall<object>("Equals", new Type[] { typeof(object), typeof(object) });
                    nullCheck.PersistentArguments[0].ToRetVal(gotDict1, typeof(object));
                    nullCheck.PersistentArguments[1].FSetType(PersistentArgumentType.Object).FSetString(typeof(object).AssemblyQualifiedName).FSetInt(0);
                    evt.PersistentCallsList.Add(nullCheck);

                    var ifNull = dataRoot.StoreComp<LifeCycleEvents>("dict null check");
                    ifNull.gameObject.AddComponent<LifeCycleEvtEditorRunner>();

                    var triggerNullCheck = MakeCall<GameObject>("SetActive", ifNull.gameObject);
                    triggerNullCheck.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.Count - 1, typeof(bool));
                    evt.PersistentCallsList.Add(triggerNullCheck);

                    // Dict is null, create new one
                    ifNull.EnableEvent.FSetPCalls(new());
                    int dictType = ifNull.EnableEvent.PersistentCallsList.FindOrAddGetTyper<Dictionary<string, object>>();

                    var makeDict = MakeCall<Activator>("CreateInstance", typeof(Type));
                    makeDict.PersistentArguments[0].ToRetVal(dictType, typeof(Type));
                    ifNull.EnableEvent.PersistentCallsList.Add(makeDict);
                    int madeDictIdx = ifNull.EnableEvent.PersistentCallsList.IndexOf(makeDict);

                    ifNull.EnableEvent.PersistentCallsList.AddDebugLog("dict was null, made new:");
                    ifNull.EnableEvent.PersistentCallsList.AddDebugLog(ifNull.EnableEvent.PersistentCallsList.IndexOf(makeDict));

                    // set dict
                    int dictStoreType2 = ifNull.EnableEvent.PersistentCallsList.FindOrAddGetTyper(dictStoreTypeStr);
                    int dictStoreField2 = ifNull.EnableEvent.PersistentCallsList.AddGetFieldInfo(dictStoreType2, dictStoreFieldStr);
                    ifNull.EnableEvent.PersistentCallsList.AddSetFieldValue(dictStoreField2, -1, madeDictIdx);

                    /*
                    int dictField2 = ifNull.EnableEvent.PersistentCallsList.AddGetFieldInfo(, "");
                    ifNull.EnableEvent.PersistentCallsList.AddDebugLog(dictField2);

                    int gotDict2 = ifNull.EnableEvent.PersistentCallsList.AddRunMethod(
                        UltNoodleRuntimeExtensions.SetFieldValue, dictField2, null,
                        ifNull.EnableEvent.PersistentCallsList.IndexOf(makeDict));*/
                    //

                    evt.PersistentCallsList.AddDebugLog("making guid");
                    // awesome, at this point we have a dict, delegate, floater
                    // with custom scripting within it
                    // now we just gotta generate & output a guid + store in dict w/ guid
                    var getGuid = MakeCall<Guid>("NewGuid");
                    evt.PersistentCallsList.Add(getGuid);

                    var getGuidStr = MakeCall<string>("Format", typeof(string), typeof(object));
                    getGuidStr.PersistentArguments[0].String = "{0}";
                    getGuidStr.PersistentArguments[1].ToRetVal(evt.PersistentCallsList.Count - 1, typeof(object));
                    evt.PersistentCallsList.Add(getGuidStr);

                    evt.PersistentCallsList.AddDebugLog("got guid:");
                    evt.PersistentCallsList.AddDebugLog(evt.PersistentCallsList.IndexOf(getGuidStr));


                    int gotDict3 = evt.PersistentCallsList.AddGetFieldValue(dictField, null);

                    evt.PersistentCallsList.AddDebugLog("got dict:");
                    evt.PersistentCallsList.AddDebugLog(gotDict3);

                    // put {GUID:Delegate} kvp into dict :)
                    evt.PersistentCallsList.AddRunMethod(typeof(Dictionary<string, object>).GetMethod("Add"),
                        gotDict3, new object[] { evt.PersistentCallsList.IndexOf(getGuidStr), delMade });

                    evt.PersistentCallsList.AddDebugLog("added kvp to dict");


                    // now:
                    // - also add a node to fetch from assembly_resolve_in_progress

                    // compile the remaining evt, post-del
                    node.DataOutputs[0].CompCall = getGuidStr; // delegate GUID
                    node.DataOutputs[0].CompEvt = evt;
                    node.DataOutputs[1].CompCall = evt.PersistentCallsList[delMade]; // delegate itself
                    node.DataOutputs[1].CompEvt = evt;

                    var evtNext = node.FlowOutputs[0].Target?.Node;
                    if (evtNext != null)
                        evtNext.Book.CompileNode(evt, evtNext, dataRoot);

                    /*// create & remember a delegate via string :3
                    // at this point in time, we create an event with the require obj signature,
                    // copy a sacrificial evts PersistentCalls list over,
                    // form the delegate and output it as a node item
                    bool hasParams = !node.BookTag.EndsWith("0");
                    int paramCount = int.Parse(node.BookTag.Replace("delegate", ""));
                    string m = "";
                    if (hasParams)
                        m = "`" + node.BookTag.Replace("delegate", "");
                    var getEvtTypeCall = new PersistentCall(typeof(Type).GetMethod("GetType", new Type[] { typeof(string) }), null);
                    getEvtTypeCall.PersistentArguments[0].FSetString($"UltEvents.UltEvent{m}, UltEvents");
                    evt.PersistentCallsList.Add(getEvtTypeCall);

                    int evtTypeIdx = evt.PersistentCallsList.Count - 1;
                    int objArrT = evt.PersistentCallsList.FindOrAddGetTyper<Type[]>();
                    int targDelegateTIdx = -1;
                    int invParamsTArrIdx = -1;

                    if (hasParams)
                    {
                        var makeObjTArr = new PersistentCall(typeof(JsonConvert).GetMethod("DeserializeObject", new Type[] { typeof(string), typeof(Type) }), null);
                        string objListMaker = "[";
                        for (int i = 0; i < paramCount; i++)
                            objListMaker += "\"System.Object, mscorlib\",";
                        objListMaker = objListMaker[..^1] + "]";
                        makeObjTArr.PersistentArguments[0].FSetString(objListMaker);
                        makeObjTArr.PersistentArguments[1].ToRetVal(objArrT, typeof(Type));
                        evt.PersistentCallsList.Add(makeObjTArr);
                        invParamsTArrIdx = evt.PersistentCallsList.Count - 1;

                        var makeGenericEvtT = new PersistentCall();
                        makeGenericEvtT.FSetMethodName("System.RuntimeType, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089.MakeGenericType");
                        makeGenericEvtT.FSetArguments(
                            new PersistentArgument(typeof(Type)).ToRetVal(evt.PersistentCallsList.IndexOf(getEvtTypeCall), typeof(Type)),
                            new PersistentArgument(typeof(Type)).ToRetVal(evt.PersistentCallsList.Count - 1, typeof(Type[])));
                        evt.PersistentCallsList.Add(makeGenericEvtT);
                        evtTypeIdx = evt.PersistentCallsList.Count - 1;

                        // also, delegate needs cooler type!

                        var dType = evt.PersistentCallsList.FindOrAddGetTyper(Type.GetType($"System.Action{m}, mscorlib"));
                        var makeGenericDelT = new PersistentCall();
                        makeGenericDelT.FSetMethodName("System.RuntimeType, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089.MakeGenericType");
                        makeGenericDelT.FSetArguments(
                            new PersistentArgument(typeof(Type)).ToRetVal(dType, typeof(Type)),
                            new PersistentArgument(typeof(Type)).ToRetVal(evt.PersistentCallsList.IndexOf(makeObjTArr), typeof(Type[])));
                        evt.PersistentCallsList.Add(makeGenericDelT);
                        targDelegateTIdx = evt.PersistentCallsList.Count - 1;
                    }
                    else
                    {
                        targDelegateTIdx = evt.PersistentCallsList.FindOrAddGetTyper<Action>();
                        int objT = evt.PersistentCallsList.FindOrAddGetTyper<object>();
                        var emptyTArr = MakeCall<Array>("CreateInstance", typeof(Type), typeof(int));
                        emptyTArr.PersistentArguments[0].ToRetVal(objT, typeof(Type));
                        evt.PersistentCallsList.Add(emptyTArr);
                        invParamsTArrIdx = evt.PersistentCallsList.Count - 1;
                    }

                    var createEvt = new PersistentCall(typeof(Activator).GetMethod("CreateInstance", new Type[] { typeof(Type) }), null);
                    createEvt.PersistentArguments[0].ToRetVal(evtTypeIdx, typeof(Type));
                    evt.PersistentCallsList.Add(createEvt);

                    var onTrigEvt = dataRoot.StoreComp<UltEventHolder>("OnTrigger");
                    onTrigEvt.Event.FSetPCalls(new()); 
                    onTrigEvt.gameObject.SetActive(true);
                    


                    // uahghhg
                    // gotta get the method
                    int voidT = evt.PersistentCallsList.FindOrAddGetTyper(typeof(void));
                    var getInv = new PersistentCall();
                    getInv.FSetMethodName("System.ComponentModel.MemberDescriptor, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089.FindMethod");
                    getInv.FSetArguments(
                        new PersistentArgument().ToRetVal(evtTypeIdx, typeof(Type)),
                        new PersistentArgument().FSetType(PersistentArgumentType.String).FSetString("Invoke"),
                        new PersistentArgument().ToRetVal(invParamsTArrIdx, typeof(Type[])),
                        new PersistentArgument().ToRetVal(voidT, typeof(Type)),
                        new PersistentArgument().FSetType(PersistentArgumentType.Bool)
                        );
                    evt.PersistentCallsList.Add(getInv);

                    // make the delegate
                    var delCreate = MakeCall<Delegate>("CreateDelegate", typeof(Type), typeof(object), typeof(MethodInfo));
                    delCreate.PersistentArguments[0].ToRetVal(targDelegateTIdx, typeof(Type));
                    delCreate.PersistentArguments[1].ToRetVal(evt.PersistentCallsList.IndexOf(createEvt), typeof(object));
                    delCreate.PersistentArguments[2].ToRetVal(evt.PersistentCallsList.IndexOf(getInv), typeof(MethodInfo));
                    evt.PersistentCallsList.Add(delCreate);

                    //var dbg = new PersistentCall(typeof(Debug).GetMethod("Log", new Type[] { typeof(object) }), null);
                    //dbg.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.Count - 1, typeof(object));
                    //evt.PersistentCallsList.Add(dbg);

                    // then we compile the remaining stuff for "On Created"
                    node.DataOutputs[0].CompCall = delCreate;
                    node.DataOutputs[0].CompEvt = evt;

                    var nextNode1 = node.FlowOutputs[0].Target?.Node;
                    if (nextNode1 != null)
                        nextNode1.Book.CompileNode(evt, nextNode1, dataRoot);

                    var nextNode2 = node.FlowOutputs[1].Target?.Node;
                    if (nextNode2 != null)
                        nextNode2.Book.CompileNode(onTrigEvt.Event, nextNode2, onTrigEvt.transform);

                    // alr we gyatta copy onTrigEvt to createEvt

                    // get the onTrigEvt
                    var getJevt = MakeCall<UltEventHolder>("get_Event", onTrigEvt);
                    evt.PersistentCallsList.Add(getJevt);
                    // get the _PersistentCalls list

                    // first get Type[0]
                    int tT = evt.PersistentCallsList.FindOrAddGetTyper<Type>();
                    var emptyTArr2 = MakeCall<Array>("CreateInstance", typeof(Type), typeof(int));
                    emptyTArr2.PersistentArguments[0].ToRetVal(tT, typeof(Type));
                    evt.PersistentCallsList.Add(emptyTArr2);

                    int methUB = evt.PersistentCallsList.FindOrAddGetTyper(typeof(UltEventBase));
                    int listT = evt.PersistentCallsList.FindOrAddGetTyper(typeof(List<PersistentCall>));
                    var getPCallGetter = new PersistentCall();
                    getPCallGetter.FSetMethodName("System.ComponentModel.MemberDescriptor, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089.FindMethod");
                    getPCallGetter.FSetArguments(
                        new PersistentArgument().ToRetVal(methUB, typeof(Type)),
                        new PersistentArgument().FSetType(PersistentArgumentType.String).FSetString("get_PersistentCallsList"),
                        new PersistentArgument().ToRetVal(evt.PersistentCallsList.IndexOf(emptyTArr2), typeof(Type[])),
                        new PersistentArgument().ToRetVal(listT, typeof(Type)),
                        new PersistentArgument().FSetType(PersistentArgumentType.Bool)
                        );
                    evt.PersistentCallsList.Add(getPCallGetter);

                    
                    int objT2 = evt.PersistentCallsList.FindOrAddGetTyper<object>();
                    var emptyObjArr = MakeCall<Array>("CreateInstance", typeof(Type), typeof(int));
                    emptyObjArr.PersistentArguments[0].ToRetVal(objT2, typeof(Type));
                    evt.PersistentCallsList.Add(emptyObjArr);
                    invParamsTArrIdx = evt.PersistentCallsList.Count - 1;


                    var getPCallInvoke = new PersistentCall();
                    getPCallInvoke.FSetMethodName("System.SecurityUtils, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089.MethodInfoInvoke");
                    getPCallInvoke.FSetArguments(
                        new PersistentArgument().ToRetVal(evt.PersistentCallsList.IndexOf(getPCallGetter), typeof(MethodInfo)),
                        new PersistentArgument().ToRetVal(evt.PersistentCallsList.IndexOf(getJevt), typeof(object)),
                        new PersistentArgument().ToRetVal(evt.PersistentCallsList.Count-1, typeof(object[]))
                        );
                    evt.PersistentCallsList.Add(getPCallInvoke);

                    // setter for field UltEventBase._PersistentCalls
                    ///////////////////////////////////////////////////////////////////////////////////////////
                    {
                        //evt.PersistentCallsList.FindOrAddGetTyper
                    }
                    ///////////////////////////////////////////////////////////////////////////////////////////

                    var dbg = new PersistentCall(typeof(Debug).GetMethod("Log", new Type[] { typeof(object) }), null);
                    dbg.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.IndexOf(createEvt), typeof(object));
                    evt.PersistentCallsList.Add(dbg);

                    //?
                    //nvm, _PersistentCalls is field for set :(
                    
                    */
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
                }
                else
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