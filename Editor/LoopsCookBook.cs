#if UNITY_EDITOR
using NoodledEvents;
using System;
using System.Collections.Generic;
using System.Reflection;
using UltEvents;
using UnityEngine;
using static NoodledEvents.CookBook.NodeDef;


public class LoopsCookBook : CookBook
{
    public override void CollectDefs(List<NodeDef> allDefs)
    {
        // flow.if
        allDefs.Add(new NodeDef(this, "loops.while",
            inputs: () => new[] { new Pin("Exec") },
            outputs: () => new[] { new Pin("Loop"), new Pin("Done") },
            bookTag: "while"));
        allDefs.Add(new NodeDef(this, "loops.for",
            inputs: () => new[] { new Pin("Exec"), new("from (Inclusive)", typeof(float)), new("to (Exclusive)", typeof(float)) },
            outputs: () => new[] { new Pin("Loop"), new Pin("i", typeof(float)), new Pin("Done") },
            bookTag: "for"));
        allDefs.Add(new NodeDef(this, "loops.continue",
            inputs: () => new[] { new Pin("Continue") },
            outputs: () => new Pin[0],
            bookTag: "continue"));
        allDefs.Add(new NodeDef(this, "loops.break",
            inputs: () => new[] { new Pin("break") },
            outputs: () => new Pin[0],
            bookTag: "break"));

    }
    private static MethodInfo SetActive = typeof(GameObject).GetMethod("SetActive");
    private static PropertyInfo GetSetLocPos = typeof(Transform).GetProperty("localPosition");
    private static MethodInfo Translate = typeof(Transform).GetMethod("Translate", new Type[] { typeof(float), typeof(float), typeof(float) });
    public override void CompileNode(UltEventBase evt, SerializedNode node, Transform dataRoot)
    {
        if (evt.PersistentCallsList == null) evt.FSetPCalls(new());

        // Loops take this structure:
        // root/LoopType/Loop)#)#)
        // root/LoopType/Done
        // basically it's imperitive that a loop's sibling of idx 1 is the "done" evt.
        // continue nodes will find Loop)#)#) and call it;
        // break nodes will find Loop)#)#).parent.GetChild(1) and call it.
        // this way breaking actually escapes the loop, and we can have nested loops without issue.
        switch (node.BookTag)
        {
            case "while":
            { 
                // this is just a point in execution that gets called by the continue node.
                // so just a seperate ult-event
                var loopRoot = dataRoot.StoreTransform("While");
                loopRoot.gameObject.SetActive(true);
                var loopEvt = loopRoot.StoreComp<UltEventHolder>("Loop)#)#)");
                loopEvt.gameObject.SetActive(true);
                loopEvt.Event.FSetPCalls(new());
                var doneEvt = loopRoot.StoreComp<UltEventHolder>("Done");
                doneEvt.gameObject.SetActive(true);
                doneEvt.Event.FSetPCalls(new());
                evt.PersistentCallsList.Add(new PersistentCall(typeof(UltEventHolder).GetMethod("Invoke", new Type[0]), loopEvt));
                if (node.FlowOutputs[0].Target != null)
                    node.FlowOutputs[0].Target.Node.Book.CompileNode(loopEvt.Event, node.FlowOutputs[0].Target.Node, loopEvt.transform);
                if (node.FlowOutputs[1].Target != null)
                    node.FlowOutputs[1].Target.Node.Book.CompileNode(loopEvt.Event, node.FlowOutputs[1].Target.Node, loopEvt.transform);
                break;
            }
            case "for":
                {
                    // this is just a point in execution that gets called by the continue node;
                    // but with bonus bs for an incrementing output.

                    var loopRoot = dataRoot.StoreTransform("For");
                    loopRoot.gameObject.SetActive(true);

                    var loopEvt = loopRoot.StoreComp<UltEventHolder>("Loop)#)#)");
                    loopEvt.gameObject.SetActive(true);
                    loopEvt.Event.FSetPCalls(new());
                    var doneEvt = loopRoot.StoreComp<UltEventHolder>("Done");
                    doneEvt.gameObject.SetActive(true);
                    doneEvt.Event.FSetPCalls(new());


                    // PRE LOOP:
                    //  - set i_transf pos (0,-1,0)
                    var iTransf = loopRoot.StoreTransform("i");
                    var lowerPosCall = new PersistentCall(GetSetLocPos.SetMethod, iTransf);
                    lowerPosCall.PersistentArguments[0].Vector3 = Vector3.down;
                    evt.PersistentCallsList.Add(lowerPosCall);
                    //  - translate up by input 1
                    var startCall = new PersistentCall(Translate, iTransf);
                    if (node.DataInputs[0].Source != null)
                        new PendingConnection(node.DataInputs[0].Source, evt, startCall, 1);
                    else startCall.PersistentArguments[1].Float = node.DataInputs[0].DefaultFloatValue;
                    evt.PersistentCallsList.Add(startCall);
                    //  - start loop
                    evt.PersistentCallsList.Add(new PersistentCall(typeof(UltEventHolder).GetMethod("Invoke", new Type[0]), loopEvt));
                    if (node.FlowOutputs[0].Target != null)
                    {
                        // IN LOOP:
                        //  - inc i_transf by 1
                        var incCall = new PersistentCall(Translate, iTransf);
                        incCall.PersistentArguments[1].Float = 1;
                        loopEvt.Event.PersistentCallsList.Add(incCall);
                        //  - measure i_transf
                        var getPosCall = new PersistentCall(GetSetLocPos.GetMethod, iTransf);
                        loopEvt.Event.PersistentCallsList.Add(getPosCall);
                        var dotter = new PersistentCall(typeof(Vector3).GetMethod("Dot"), null);
                        dotter.PersistentArguments[0].ToRetVal(loopEvt.Event.PersistentCallsList.Count - 1, typeof(Vector3));
                        dotter.PersistentArguments[1].Vector3 = Vector3.up;
                        loopEvt.Event.PersistentCallsList.Add(dotter);
                        //  - (make this measure into compcall/evt for data_out 1)
                        node.DataOutputs[0].CompCall = dotter;
                        node.DataOutputs[0].CompEvt = loopEvt.Event;
                        //  - i_transf >= input 2 ? Done : user code
                        var greater = new PersistentCall();
                        greater.FSetTarget(null);
                        greater.FSetMethodName("SLZ.Bonelab.VoidLogic.MathUtilities, Assembly-CSharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null.IsApproximatelyEqualToOrGreaterThan");
                        greater.FSetArguments(new PersistentArgument(typeof(float)), new PersistentArgument(typeof(float)));
                        greater.PersistentArguments[0].ToRetVal(loopEvt.Event.PersistentCallsList.Count - 1, typeof(float));
                        if (node.DataInputs[1].Source != null)
                            new PendingConnection(node.DataInputs[1].Source, loopEvt.Event, greater, 1);
                        else greater.PersistentArguments[1].Float = node.DataInputs[1].DefaultFloatValue;
                        loopEvt.Event.PersistentCallsList.Add(greater);

                        // the if part
                        var gateObj = loopEvt.transform.StoreTransform("Conditional");
                        var nextLoopEvt = gateObj.transform.StoreComp<LifeCycleEvents>("i < max");
                        nextLoopEvt.gameObject.AddComponent<LifeCycleEvtEditorRunner>();
                        nextLoopEvt.EnableEvent.FSetPCalls(new());
                        nextLoopEvt.EnableEvent.PersistentCallsList.Add(new PersistentCall(SetActive, gateObj.gameObject));
                        nextLoopEvt.EnableEvent.PersistentCallsList[0].PersistentArguments[0].Bool = false;
                        nextLoopEvt.EnableEvent.PersistentCallsList.Add(new PersistentCall(SetActive, nextLoopEvt.gameObject));
                        nextLoopEvt.EnableEvent.PersistentCallsList[1].PersistentArguments[0].Bool = false;
                        var stablerDataRoot = loopEvt.transform.StoreTransform("i < max Dataroot");
                        stablerDataRoot.gameObject.SetActive(true);
                        node.FlowOutputs[0].Target.Node.Book.CompileNode(nextLoopEvt.EnableEvent, node.FlowOutputs[0].Target.Node, stablerDataRoot);

                        var loopOverEvt = gateObj.transform.StoreComp<LifeCycleEvents>("i >= max");
                        loopOverEvt.gameObject.AddComponent<LifeCycleEvtEditorRunner>();
                        loopOverEvt.EnableEvent.FSetPCalls(new());
                        loopOverEvt.EnableEvent.PersistentCallsList.Add(new PersistentCall(SetActive, gateObj.gameObject));
                        loopOverEvt.EnableEvent.PersistentCallsList[0].PersistentArguments[0].Bool = false;
                        loopOverEvt.EnableEvent.PersistentCallsList.Add(new PersistentCall(SetActive, loopOverEvt.gameObject));
                        loopOverEvt.EnableEvent.PersistentCallsList[1].PersistentArguments[0].Bool = false;
                        loopOverEvt.EnableEvent.PersistentCallsList.Add(new PersistentCall(typeof(UltEventHolder).GetMethod("Invoke", new Type[0]), doneEvt));

                        var failInvCall = new PersistentCall(SetActive, loopOverEvt.gameObject);
                        failInvCall.PersistentArguments[0].ToRetVal(loopEvt.Event.PersistentCallsList.Count - 1, typeof(bool));
                        loopEvt.Event.PersistentCallsList.Add(failInvCall);

                        // bool flip
                        var invCall = new PersistentCall();
                        invCall.FSetMethodName("System.Object, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089.Equals");
                        invCall.FSetArguments(new PersistentArgument().ToRetVal(loopEvt.Event.PersistentCallsList.Count - 2, typeof(bool)), new PersistentArgument(typeof(bool)));
                        loopEvt.Event.PersistentCallsList.Add(invCall);

                        var successInvCall = new PersistentCall(SetActive, nextLoopEvt.gameObject);
                        successInvCall.PersistentArguments[0].ToRetVal(loopEvt.Event.PersistentCallsList.Count - 1, typeof(bool));
                        loopEvt.Event.PersistentCallsList.Add(successInvCall);

                        // enable gate as final call;
                        // this is so looping does, in fact, work! :)
                        var gateOpenCall = new PersistentCall(SetActive, gateObj.gameObject);
                        gateOpenCall.PersistentArguments[0].Bool = true;
                        loopEvt.Event.PersistentCallsList.Add(gateOpenCall);
                    }
                    if (node.FlowOutputs[1].Target != null)
                        node.FlowOutputs[1].Target.Node.Book.CompileNode(doneEvt.Event, node.FlowOutputs[1].Target.Node, doneEvt.transform);
                    break;
                }
            case "continue":
            { 
                // find containing loop
                Transform p = dataRoot.transform;
                while (p != null && p.gameObject.name != "Loop)#)#)") // need a better way of doing this
                    p = p.parent;
                if (p == null) return; // user error lol

                evt.PersistentCallsList.Add(new PersistentCall(typeof(UltEventHolder).GetMethod("Invoke", new Type[0]), p.GetComponent<UltEventHolder>()));
                break;
            }
            case "break":
            {
                // find containing loop
                Transform p = dataRoot.transform;
                while (p != null && !p.gameObject.name.EndsWith("Loop)#)#)")) // need a better way of doing this
                    p = p.parent;
                if (p == null) return; // user error lol

                evt.PersistentCallsList.Add(new PersistentCall(typeof(UltEventHolder).GetMethod("Invoke", new Type[0]), p.parent.GetChild(1).GetComponent<UltEventHolder>()));
                break;
            }
        }
    }

    public override void PostCompile(SerializedBowl bowl)
    {
        
    }
}
#endif