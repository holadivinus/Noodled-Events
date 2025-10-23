#if UNITY_EDITOR
using Newtonsoft.Json;
using NoodledEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UltEvents;
using UnityEditor;
using UnityEngine;
using static NoodledEvents.CookBook.NodeDef;


public class ObjectMethodCookBook : CookBook
{
    private Dictionary<MethodInfo, NodeDef> MyDefs = new();
    public override void CollectDefs(Action<IEnumerable<NodeDef>, float> progressCallback, Action completedCallback)
    {
        MyDefs.Clear();

        int i = 0;
        var p = Task.Run(() => Parallel.ForEach<Type>(UltNoodleEditor.SearchableTypes, (t) =>
        {
            try
            {
                List<NodeDef> newNodes = new();

                foreach (var meth in t.GetMethods(UltEventUtils.AnyAccessBindings))
                {
                    if (meth.DeclaringType != t || meth.IsStatic) continue;

                    string searchText = t.GetFriendlyName() + "." + meth.Name;
                    string descriptiveText = $"{t.Namespace}.{t.GetFriendlyName()}.{meth.Name}";

                    var parames = meth.GetParameters();
                    if (parames.Length == 0) { descriptiveText += "()"; searchText += "()"; }
                    else
                    {
                        descriptiveText += "(";
                        searchText += "(";
                        foreach (var param in parames)
                        {
                            searchText += $"{param.ParameterType.GetFriendlyName()}, ";
                            descriptiveText += $"{param.ParameterType.GetFriendlyName()} {param.Name}, ";
                        }
                        descriptiveText = descriptiveText.Substring(0, descriptiveText.Length - 2);
                        searchText = searchText.Substring(0, searchText.Length - 2);
                        descriptiveText += ")";
                        searchText += ")";
                    }
                    descriptiveText = $"{meth.ReturnType.GetFriendlyName()} {descriptiveText}";


                    string execPinMsg = NeedsReflection(meth) ? "Reflection Exec" : "Exec";
                    var newDef = new NodeDef(this, t.GetFriendlyName() + "." + meth.Name,
                        inputs: () =>
                        {
                            var @params = meth.GetParameters();
                            if (@params == null || @params.Length == 0) return new Pin[] { new NodeDef.Pin(execPinMsg), new Pin(meth.DeclaringType.Name, meth.DeclaringType) };
                            return @params.Select(p => new Pin(p.Name, p.ParameterType)).Prepend(new Pin(meth.DeclaringType.Name, meth.DeclaringType)).Prepend(new NodeDef.Pin(execPinMsg)).ToArray();
                        },
                        outputs: () =>
                        {
                            if (meth.ReturnType != typeof(void))
                                return new[] { new NodeDef.Pin("Done"), new NodeDef.Pin(meth.ReturnType.Name, meth.ReturnType) };
                            else return new[] { new NodeDef.Pin("Done") };
                        },
                        bookTag: JsonUtility.ToJson(new SerializedMethod() { Method = meth }),
                        searchTextOverride: searchText,
                        tooltipOverride: descriptiveText);
                    newNodes.Add(newDef);

                    UltNoodleEditor.MainThread.Enqueue(() => MyDefs.Add(meth, newDef));
                    
                }
                progressCallback.Invoke(newNodes, (++i / (float)UltNoodleEditor.SearchableTypes.Length));
            }
            catch (TypeLoadException) { } // bro
        }));

        List<NodeDef> lastDefs = new();
        lastDefs.Add(new NodeDef(this, "flow.ult_swap",
            inputs: () => new Pin[] { new("") },
            outputs: () => new Pin[] { new("On Cache"), new("Post Cache"), new("Cached", typeof(UnityEngine.Object)) },
            bookTag: "UltSwap-Head",
            tooltipOverride: "Cache and Use a found UnityObject."));

        lastDefs.Add(new NodeDef(this, "flow.ult_swap_end",
            inputs: () => new Pin[] { new("Finish Cache"), new("Cached", typeof(UnityEngine.Object)) },
            outputs: () => new Pin[] { },
            bookTag: "UltSwap-Foot",
            tooltipOverride: "Caches an UnityObject for an Ultswap"));

        lastDefs.Add(new NodeDef(this, "flow.ult_swap_reset",
            inputs: () => new Pin[] { new("Reset Cache"), new("Re-cache Immediately?", typeof(bool), @const:true) },
            outputs: () => new Pin[] { },
            bookTag: "UltSwap-Reset",
            tooltipOverride: "Resets an Ultswap, use in the Post Cache exec."));

        progressCallback.Invoke(lastDefs, 0);

        p.ContinueWith(t => completedCallback.Invoke());
    }

    private bool NeedsReflection(MethodBase meth) =>
        (!(typeof(UnityEngine.Object).IsAssignableFrom(meth.DeclaringType) && EditorPrefs.GetBool("InlineUltswaps")) // Not Targettable by Pcalls?
        || meth.DeclaringType.ContainsGenericParameters  // ex: List<T> vs List<bool>
        || meth.ContainsGenericParameters // yea
        || meth.GetParameters().Any(p => p.IsOut || p.ParameterType.IsByRef));

    public override void CompileNode(UltEventBase evt, SerializedNode node, Transform dataRoot)
    {
        base.CompileNode(evt, node, dataRoot);
        // sanity check (AddComponent() leaves this field empty)
        if (evt.PersistentCallsList == null) evt.FSetPCalls(new());

        if (!node.BookTag.StartsWith('{')) // UltSwap stuff
        {
            (Type, PropertyInfo) BLXRData = PendingConnection.CompStoragers[typeof(UnityEngine.Object)];
            switch (node.BookTag)
            {
                case "UltSwap-Head":
                    {
                        // Here's what the heirarchy will look like:
                        // bowl_generated/UltSwap/OnCache/OnEnable
                        // bowl_generated/UltSwap/PostCache/Safety/OnEnable
                        //
                        // on Invoke, we'll enable "bowl_generated/UltSwap/OnCache";
                        //    OnCache will run (finding the targ obj as the user specifies)
                        //    will "ultswap" "bowl_generated/UltSwap/PostCache"
                        //    will enable bowl_generated/UltSwap/PostCache/Safety
                        //    will kill itself (as an optimization)
                        //
                        // then we'll enable "bowl_generated/UltSwap/PostCache" (will only do final exec if OnCache succeeded! :>)

                        var swapRoot = dataRoot.StoreTransform("UltSwap");
                        swapRoot.gameObject.SetActive(true);
                        var onCache = swapRoot.StoreTransform("OnCache");
                        var onCacheEvt = onCache.StoreComp<LifeCycleEvents>("OnEnable");
                        onCacheEvt.gameObject.AddComponent<LifeCycleEvtEditorRunner>();
                        onCacheEvt.gameObject.SetActive(true);
                        var postCacheA = swapRoot.StoreTransform("PostCache");
                        var postCacheB = postCacheA.StoreTransform("Safety");
                        var postCache = postCacheB.StoreComp<LifeCycleEvents>("PostCache");
                        postCache.gameObject.SetActive(true);
                        postCache.gameObject.AddComponent<LifeCycleEvtEditorRunner>();
                        var BLXRStorager = swapRoot.StoreComp(BLXRData.Item1, "Obj Storage");
                        BLXRData.Item2.SetMethod.Invoke(BLXRStorager, new[] { node.Bowl.EventHolder });

                        // activate OnCache
                        var pcal1 = new PersistentCall(SetActive, onCache.gameObject);
                        pcal1.PersistentArguments[0].Bool = true;
                        evt.PersistentCallsList.Add(pcal1);
                        // reset
                        var pcala = new PersistentCall(SetActive, onCache.gameObject);
                        pcala.PersistentArguments[0].Bool = false;
                        evt.PersistentCallsList.Add(pcala);

                        // Try Invoke PostCache
                        var pcal2 = new PersistentCall(SetActive, postCacheA.gameObject);
                        pcal2.PersistentArguments[0].Bool = true;
                        evt.PersistentCallsList.Add(pcal2);
                        // reset
                        var pcal3 = new PersistentCall(SetActive, postCacheA.gameObject);
                        pcal3.PersistentArguments[0].Bool = false;
                        evt.PersistentCallsList.Add(pcal3);


                        // Compile PostCache, having the Cache Output being BLXR Getter
                        node.DataOutputs[0].CompEvt = postCache.EnableEvent;
                        postCache.EnableEvent.FSetPCalls(new List<PersistentCall>());
                        postCache.EnableEvent.PersistentCallsList.Add(node.DataOutputs[0].CompCall = new PersistentCall(BLXRData.Item2.GetMethod, BLXRStorager));

                        var postCacheNode = node.FlowOutputs[1].Target?.Node;
                        if (postCacheNode != null)
                            postCacheNode.Book.CompileNode(postCache.EnableEvent, postCacheNode, postCache.gameObject.transform);

                        // compile the footer
                        var onCacheNode = node.FlowOutputs[0].Target?.Node;
                        if (onCacheNode != null)
                        {
                            onCacheEvt.EnableEvent.FSetPCalls(new List<PersistentCall>());
                            onCacheNode.Book.CompileNode(onCacheEvt.EnableEvent, onCacheNode, onCacheEvt.gameObject.transform);
                        }
                        break;
                    }
                case "UltSwap-Foot":
                    {
                        // this node will seek up to its head;
                        // find the bowl_generated/UltSwap/PostCache event;
                        // generate ult-swapping code for all descending evts that ref Head's "Cached" output

                        // find first "OnCache" in parents!
                        Transform p = dataRoot.transform;
                        while (p != null && p.gameObject.name != "OnCache")
                            p = p.parent;
                        if (p == null) return; // user error lol

                        var BLXRComp = p.parent.GetChild(2).GetComponent(BLXRData.Item1);

                        // if a PCall has this as its source, it needs to be retargetted.
                        UnityEngine.Object SourceMarker = (UnityEngine.Object)BLXRData.Item2.GetMethod.Invoke(BLXRComp, null);


                        // ToJson the BLXRComp, and get the ID string to replace on the target events.
                        var blxrTojson = new PersistentCall(typeof(JsonUtility).GetMethod("ToJson", new Type[] { typeof(object) }), null);
                        blxrTojson.PersistentArguments[0].FSetType(PersistentArgumentType.Object).Object = BLXRComp;
                        evt.PersistentCallsList.Add(blxrTojson);

                        // cut off the start of this tojson 
                        var cutStart = new PersistentCall();
                        cutStart.FSetMethodName("System.Text.RegularExpressions.Regex, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089.Replace");
                        cutStart.FSetArguments(
                            new PersistentArgument().ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string)),
                            new PersistentArgument().FSetType(PersistentArgumentType.String).FSetString("^(.*\"m_InteractorSource\":)"),
                            new PersistentArgument().FSetType(PersistentArgumentType.String).FSetString("")
                            );
                        evt.PersistentCallsList.Add(cutStart);

                        // and the end
                        var cutEnd = new PersistentCall();
                        cutEnd.FSetMethodName("System.Text.RegularExpressions.Regex, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089.Replace");
                        cutEnd.FSetArguments(
                            new PersistentArgument().ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string)),
                            new PersistentArgument().FSetType(PersistentArgumentType.String).FSetString(",\"m_I(.)*"),
                            new PersistentArgument().FSetType(PersistentArgumentType.String).FSetString("")
                            );
                        evt.PersistentCallsList.Add(cutEnd); // Alr, this call's retval is the "to be replaced" ID!

                        var blxrChange = new PersistentCall(BLXRData.Item2.SetMethod, BLXRComp);
                        if (node.DataInputs[0].Source != null)
                            new PendingConnection(node.DataInputs[0].Source, evt, blxrChange, 0).Connect(dataRoot);
                        else blxrChange.PersistentArguments[0].FSetType(PersistentArgumentType.Object).Object = node.DataInputs[0].DefaultObject;
                        evt.PersistentCallsList.Add(blxrChange);

                        // ToJson the BLXRComp, and get the ID string to replace WITH on the target events.
                        var blxrTojson2 = new PersistentCall(typeof(JsonUtility).GetMethod("ToJson", new Type[] { typeof(object) }), null);
                        blxrTojson2.PersistentArguments[0].FSetType(PersistentArgumentType.Object).Object = BLXRComp;
                        evt.PersistentCallsList.Add(blxrTojson2);

                        // cut off the start of this tojson 
                        var cutStart2 = new PersistentCall();
                        cutStart2.FSetMethodName("System.Text.RegularExpressions.Regex, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089.Replace");
                        cutStart2.FSetArguments(
                            new PersistentArgument().ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string)),
                            new PersistentArgument().FSetType(PersistentArgumentType.String).FSetString("^(.*\"m_InteractorSource\":)"),
                            new PersistentArgument().FSetType(PersistentArgumentType.String).FSetString("")
                            );
                        evt.PersistentCallsList.Add(cutStart2);

                        // and the end
                        var cutEnd2 = new PersistentCall();
                        cutEnd2.FSetMethodName("System.Text.RegularExpressions.Regex, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089.Replace");
                        cutEnd2.FSetArguments(
                            new PersistentArgument().ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string)),
                            new PersistentArgument().FSetType(PersistentArgumentType.String).FSetString(",\"m_I(.)*"),
                            new PersistentArgument().FSetType(PersistentArgumentType.String).FSetString("")
                            );
                        evt.PersistentCallsList.Add(cutEnd2); // Alr, this call's retval is the "to be replaced WITH" ID!

                        int replacedID = evt.PersistentCallsList.IndexOf(cutEnd);
                        int replaceWithID = evt.PersistentCallsList.IndexOf(cutEnd2);

                        // now we'll tojson all descendent comps that ref SourceMarker.
                        foreach (Component comp in p.parent.GetChild(1).GetComponentsInChildren<Component>(true))
                        {
                            foreach (var compField in comp.GetType().GetFields(UltEventUtils.AnyAccessBindings))
                            {
                                if (typeof(UltEventBase).IsAssignableFrom(compField.FieldType)) // this field is an ultevent!
                                {
                                    // So, we'll do the whole shebang with this comp.

                                    // ToJson this comp:
                                    var toJson = new PersistentCall(typeof(JsonUtility).GetMethod("ToJson", new Type[] { typeof(object) }), null);
                                    toJson.PersistentArguments[0].FSetType(PersistentArgumentType.Object).Object = comp;
                                    evt.PersistentCallsList.Add(toJson);
                                    // Replace old ID with new 
                                    var replaceIDcall = new PersistentCall();
                                    replaceIDcall.FSetMethodName("System.Text.RegularExpressions.Regex, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089.Replace");
                                    replaceIDcall.FSetArguments(
                                        new PersistentArgument().ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string)),
                                        new PersistentArgument().ToRetVal(replacedID, typeof(string)),
                                        new PersistentArgument().ToRetVal(replaceWithID, typeof(string))
                                        );
                                    evt.PersistentCallsList.Add(replaceIDcall);
                                    // FromJson this comp:
                                    var fromJson = new PersistentCall(typeof(JsonUtility).GetMethod("FromJsonOverwrite", new Type[] { typeof(string), typeof(object) }), null);
                                    fromJson.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string));
                                    fromJson.PersistentArguments[1].FSetType(PersistentArgumentType.Object).Object = comp;
                                    evt.PersistentCallsList.Add(fromJson);

                                    break; // move to next comp
                                }
                            }
                        }
                        // enable PostCache's safety, so it'll get called by head in all future invokes:
                        var safetyOff = new PersistentCall(SetActive, p.parent.GetChild(1).GetChild(0).gameObject);
                        safetyOff.PersistentArguments[0].Bool = true;
                        evt.PersistentCallsList.Add(safetyOff);

                        // and finally delete ourself
                        // okay editor hates deleting for a multitude of reasons, so we'll just deactivate lol
                        var deactivation = new PersistentCall(SetActive, p.GetChild(0).gameObject);
                        deactivation.PersistentArguments[0].Bool = false;
                        evt.PersistentCallsList.Add(deactivation);

                        break;
                    }
                case "UltSwap-Reset":
                    {
                        // bowl_generated/UltSwap/OnCache/OnEnable
                        // bowl_generated/UltSwap/PostCache/Safety/OnEnable
                        // this one is stored somewhere in PostCache;
                        // We'll seek upwards untill we find Safety with PostCache above
                        
                        Transform p = dataRoot.transform;
                        while(p != null && p.name != "Safety") // again we need a more "secure" system for in-book tagging.
                            p = p.parent;
                        if (p == null) return; //user error lol 3000

                        // turn safety back on (aka disabled
                        var safeOff = new PersistentCall(SetActive, p.gameObject);
                        safeOff.PersistentArguments[0].Bool = false;
                        evt.PersistentCallsList.Add(safeOff);

                        // tell OnEnable that recaching is go. :)
                        var OnCache = p.parent.parent.GetChild(0).GetChild(0).gameObject;
                        var cacheOn = new PersistentCall(SetActive, OnCache);
                        cacheOn.PersistentArguments[0].Bool = true;
                        evt.PersistentCallsList.Add(cacheOn);

                        // turn off postcache preemtively (since root hasn't yet :/
                        var postcacheOff = new PersistentCall(SetActive, p.parent.gameObject);
                        postcacheOff.PersistentArguments[0].Bool = false;
                        evt.PersistentCallsList.Add(postcacheOff);

                        if (node.DataInputs[0].DefaultBoolValue)
                        {
                            var reset = dataRoot.StoreTransform("off").StoreComp<LifeCycleEvents>("Reset");
                            reset.gameObject.AddComponent<LifeCycleEvtEditorRunner>();
                            reset.EnableEvent.FSetPCalls(new());
                            reset.gameObject.SetActive(true);

                            //  real resetcall
                            var resetCall = new PersistentCall(typeof(UnityEngine.Object).GetMethod("Instantiate", new Type[] { typeof(UnityEngine.Object), typeof(Transform) }), reset);
                            resetCall.PersistentArguments[0].Object = reset.gameObject;
                            resetCall.PersistentArguments[1].Object = null;
                            resetCall.PersistentArguments[1].FSetString(typeof(Transform).AssemblyQualifiedName);
                            evt.PersistentCallsList.Add(resetCall);

                            // now we on-off cache;
                            var rs1 = new PersistentCall(SetActive, OnCache.transform.parent.gameObject);
                            rs1.PersistentArguments[0].Bool = true;
                            reset.EnableEvent.PersistentCallsList.Add(rs1);

                            var rs2 = new PersistentCall(SetActive, OnCache.transform.parent.gameObject);
                            rs2.PersistentArguments[0].Bool = false;
                            reset.EnableEvent.PersistentCallsList.Add(rs2);
                            // then on-off postcache.
                            var rs3 = new PersistentCall(SetActive, p.parent.gameObject);
                            rs3.PersistentArguments[0].Bool = true;
                            reset.EnableEvent.PersistentCallsList.Add(rs3);

                            var rs4 = new PersistentCall(SetActive, p.parent.gameObject);
                            rs4.PersistentArguments[0].Bool = false;
                            reset.EnableEvent.PersistentCallsList.Add(rs4);

                            //var dbg = new PersistentCall(typeof(Debug).GetMethod("Log", new Type[] { typeof(object) }), null);
                            //dbg.PersistentArguments[0].FSetType(PersistentArgumentType.String).FSetString("aaa");
                            //reset.EnableEvent.PersistentCallsList.Add(dbg);
                            var del = new PersistentCall(typeof(UnityEngine.Object).GetMethod("DestroyImmediate", new Type[] { typeof(UnityEngine.Object) }), null);
                            del.PersistentArguments[0].Object = reset.gameObject;
                            reset.EnableEvent.PersistentCallsList.Add(del);
                        }
                        // do note that sadly, this behavior cannot loop. use a while loop
                        break;
                    }
            }
            return;
        }

        // figure node method
        SerializedMethod meth = JsonUtility.FromJson<SerializedMethod>(node.BookTag);

        #region Reflection Based Method
        if (NeedsReflection(meth.Method) && node.DataInputs[0].DefaultObject == null) // bonus retvals!
        {
            // UAHGAHGAUGUAAAAAAS
            


            int typeArrType = evt.PersistentCallsList.FindOrAddGetTyper<Type[]>();
            int targType = evt.PersistentCallsList.FindOrAddGetTyper(meth.Method.DeclaringType);
            int retValType = evt.PersistentCallsList.FindOrAddGetTyper(meth.Method.GetReturnType());

            // get Type[] of params
            var paramTypeArr = new PersistentCall(typeof(JsonConvert).GetMethod(nameof(JsonConvert.DeserializeObject), new[] { typeof(string), typeof(Type) }), null);
            paramTypeArr.PersistentArguments[0].String = "[";
            ParameterInfo[] methodParams = meth.Method.GetParameters();
            foreach (var p in methodParams)
                paramTypeArr.PersistentArguments[0].String += "\"" + p.ParameterType.AssemblyQualifiedName + "\", ";
            if (paramTypeArr.PersistentArguments[0].String.EndsWith(", "))
                paramTypeArr.PersistentArguments[0].String = paramTypeArr.PersistentArguments[0].String.Substring(0, paramTypeArr.PersistentArguments[0].String.Length - 2);
            paramTypeArr.PersistentArguments[0].String += "]";
            paramTypeArr.PersistentArguments[1].ToRetVal(typeArrType, typeof(Type));
            evt.PersistentCallsList.Add(paramTypeArr);

            var getTargMethod = new PersistentCall(typeof(System.ComponentModel.MemberDescriptor).GetMethod("FindMethod", UltEventUtils.AnyAccessBindings, null,
                new Type[] { typeof(Type), typeof(string), typeof(Type[]), typeof(Type), typeof(bool) }, null), null);
            getTargMethod.PersistentArguments[0].ToRetVal(targType, typeof(Type));
            getTargMethod.PersistentArguments[1].String = meth.Method.Name;
            getTargMethod.PersistentArguments[2].ToRetVal(evt.PersistentCallsList.IndexOf(paramTypeArr), typeof(Type[]));
            getTargMethod.PersistentArguments[3].ToRetVal(retValType, typeof(Type));
            getTargMethod.PersistentArguments[4].Bool = false;
            evt.PersistentCallsList.Add(getTargMethod);

            // aight, we got the MethodInfo
            // just gotta compose the Param Array

            int objType = evt.PersistentCallsList.FindOrAddGetTyper<object>();
            var paramArr = new PersistentCall(typeof(Array).GetMethod("CreateInstance", new[] { typeof(Type), typeof(int) }), null);
            paramArr.PersistentArguments[0].ToRetVal(objType, typeof(Type));
            paramArr.PersistentArguments[1].Int = methodParams.Length;
            evt.PersistentCallsList.Add(paramArr);

            for (int i = 0; i < methodParams.Length; i++)
            {
                ParameterInfo p = methodParams[i];

                var editorSetCall = new PersistentCall(typeof(UltNoodleRuntimeExtensions).GetMethod("ArrayItemSetter1", UltEventUtils.AnyAccessBindings), null);
                editorSetCall.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.IndexOf(paramArr), typeof(Array));
                editorSetCall.PersistentArguments[1].Int = i;

                if (node.DataInputs[i + 1].Source != null)
                    new PendingConnection(node.DataInputs[i + 1].Source, evt, editorSetCall, 2).Connect(dataRoot);
                else
                {
                    editorSetCall.PersistentArguments[2].FSetType(node.DataInputs[i + 1].GetPCallType()).SafeSetValue(node.DataInputs[i + 1].GetDefault());
                    if (p.ParameterType == typeof(Type))
                    {
                        node.DataInputs[i + 1].CompEvt = evt;
                        node.DataInputs[i + 1].CompCall = editorSetCall;
                        node.DataInputs[i + 1].CompArg = editorSetCall.PersistentArguments[2];
                    }
                }

                evt.PersistentCallsList.Add(editorSetCall);

                /*var ingameSetCall = new PersistentCall();
                ingameSetCall.CopyFrom(editorSetCall);
                ingameSetCall.FSetMethodName("System.Linq.Expressions.Interpreter.CallInstruction, System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e.ArrayItemSetter1");
                ingameSetCall.FSetMethod(null);
                evt.PersistentCallsList.Add(ingameSetCall);*/
            }
            // paramArr is now full of data;
            // invoke the method upon the target.

            PersistentCall invokeMethod = new PersistentCall(Type.GetType("System.SecurityUtils, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", true, true).GetMethod("MethodInfoInvoke", UltEventUtils.AnyAccessBindings, null,
                new Type[] { typeof(MethodInfo), typeof(object), typeof(object[]) }, null), null);
            invokeMethod.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.IndexOf(getTargMethod), typeof(MethodInfo));

            if (node.DataInputs[0].Source != null)
                new PendingConnection(node.DataInputs[0].Source, evt, invokeMethod, 1).Connect(dataRoot);
            else
            {
                invokeMethod.PersistentArguments[1].FSetType(node.DataInputs[0].ConstInput).SafeSetValue(node.DataInputs[0].GetDefault());
                if (node.DataInputs[0].Type == typeof(Type))
                {
                    node.DataInputs[0].CompEvt = evt;
                    node.DataInputs[0].CompCall = invokeMethod;
                    node.DataInputs[0].CompArg = invokeMethod.PersistentArguments[1];
                }
            }

            invokeMethod.PersistentArguments[2].ToRetVal(evt.PersistentCallsList.IndexOf(paramArr), typeof(object[]));
            evt.PersistentCallsList.Add(invokeMethod);

            if (node.DataOutputs.Length > 0)
            {
                node.DataOutputs[0].CompCall = invokeMethod;
                node.DataOutputs[0].CompEvt = evt;
            }

            var nnextNode = node.FlowOutputs[0].Target?.Node;
            if (nnextNode != null)
                nnextNode.Book.CompileNode(evt, nnextNode, dataRoot);
            return;
        }
        #endregion

        #region PCall Based Method
        // make my PCall    
        PersistentCall myCall = new PersistentCall();
        myCall.SetMethod(meth.Method, node.DataInputs[0].DefaultObject);
        if (node.DataInputs.Length > 0)
            myCall.FSetArguments(new PersistentArgument[node.DataInputs.Length - 1]);

        UltEventHolder varyEvt = null;
        UltEventBase pre = evt;
        // if the source varies
        if (node.DataInputs[0].Source != null) // Okay, for Ult-Swap-Caching: uhhh
        {                                      // if the source node is an ult-swap head, we ref a template object (lets just use the src evt)
                                               // then, in OnCache, we get the temp obj ref as a string; tojson child evts; replace temp ref with real ref; fromJson.
            if (node.DataInputs[0].Source.Node.BookTag == "UltSwap-Head")
            {
                myCall.FSetTarget(node.DataInputs[0].Source.Node.Bowl.EventHolder);
            }
            else
            {
                // we need to json
                // make event for jsonning
                // TODO: UltSwap Cach chip
                varyEvt = dataRoot.StoreComp<UltEventHolder>("varyingEvt");
                varyEvt.Event = new UltEvent();
                varyEvt.Event.FSetPCalls(new());
                evt = varyEvt.Event; // move stuff to the targevt
                myCall.FSetTarget(null);
            }
        }
        
        

        // foreach input
        for (int j = 0; j < node.DataInputs.Length; j++) 
        {
            // first data-in is allways the targ obj;
            if (j == 0) continue;

            NoodleDataInput @in = node.DataInputs[j];
            @in.CompEvt = evt;
            @in.CompCall = myCall;

            if (@in.Source != null) // is connected
            {
                new PendingConnection(@in.Source, evt, myCall, j - 1).Connect(dataRoot);
            } else
            {
                @in.CompArg = myCall.PersistentArguments[j - 1] = new PersistentArgument(meth.Parameters[j - 1]);

                // this input uses a default value!
                PersistentArgumentType t = PersistentArgumentType.None;
                if (node.DataInputs[j - 1].ConstInput != PersistentArgumentType.None) // takes consts!
                {
                    t = node.DataInputs[j - 1].ConstInput;
                    // force this arg to be the const lol, ui todo
                    myCall.PersistentArguments[j - 1].FSetType(t);
                }
                else t = myCall.PersistentArguments[j - 1].Type;

                switch (t)
                {
                    case PersistentArgumentType.Bool:
                        myCall.PersistentArguments[j - 1].Bool = node.DataInputs[j].DefaultBoolValue;
                        break;
                    case PersistentArgumentType.String:
                        myCall.PersistentArguments[j - 1].String = node.DataInputs[j].DefaultStringValue;
                        break;
                    case PersistentArgumentType.Int:
                        myCall.PersistentArguments[j - 1].Int = node.DataInputs[j].DefaultIntValue;
                        break;
                    case PersistentArgumentType.Enum:
                        myCall.PersistentArguments[j - 1].Enum = node.DataInputs[j].DefaultIntValue;
                        break;
                    case PersistentArgumentType.Float:
                        myCall.PersistentArguments[j - 1].Float = node.DataInputs[j].DefaultFloatValue;
                        break;
                    case PersistentArgumentType.Vector2:
                        myCall.PersistentArguments[j - 1].Vector2 = node.DataInputs[j].DefaultVector2Value;
                        break;
                    case PersistentArgumentType.Vector3:
                        myCall.PersistentArguments[j - 1].Vector3 = node.DataInputs[j].DefaultVector3Value;
                        break;
                    case PersistentArgumentType.Vector4:
                        myCall.PersistentArguments[j - 1].Vector4 = node.DataInputs[j].DefaultVector4Value;
                        break;
                    case PersistentArgumentType.Quaternion:
                        myCall.PersistentArguments[j - 1].Quaternion = node.DataInputs[j].DefaultQuaternionValue;
                        break;
                    case PersistentArgumentType.Color:
                        myCall.PersistentArguments[j - 1].Color = node.DataInputs[j].DefaultColorValue;
                        break;
                    case PersistentArgumentType.Color32:
                        myCall.PersistentArguments[j - 1].Color32 = node.DataInputs[j].DefaultColorValue; // hope this implicitly casts well
                        break;
                    case PersistentArgumentType.Rect:
                        Vector4 v = node.DataInputs[j].DefaultVector4Value;
                        myCall.PersistentArguments[j - 1].Rect = new Rect(v.x, v.y, v.z, v.w);
                        break;
                    case PersistentArgumentType.Object:
                        myCall.PersistentArguments[j - 1].Object = node.DataInputs[j].DefaultObject;
                        myCall.PersistentArguments[j - 1].FSetString(node.DataInputs[j].Type.Type.AssemblyQualifiedName);
                        break;
                }
            }
        }

        evt.PersistentCallsList.Add(myCall);

        if (node.DataInputs[0].Source != null && myCall.Target == null)
        {
            // if evt had data output, get data
            Component retValStore = null;
            PropertyInfo retValProp = null;
            if (node.DataOutputs.Length > 0)
            {
                foreach (var kvp in PendingConnection.CompStoragers)
                {
                    if (node.DataOutputs[0].Type.Type.IsSubclassOf(kvp.Key) || node.DataOutputs[0].Type.Type == kvp.Key)
                    {
                        // add pcal saving the value
                        retValStore = dataRoot.StoreComp(kvp.Value.Item1, "retval");
                        retValProp = kvp.Value.Item2;
                        var savr = new PersistentCall(kvp.Value.Item2.SetMethod, retValStore);
                        savr.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.Count - 1, node.DataOutputs[0].Type);
                        evt.PersistentCallsList.Add(savr);
                        break;
                    }
                }
            }

            // swap back to reality
            evt = pre;

            // add pcalls to get the varying ID, insert it between the a & b json bits, deserialize into evt, run evt

            // to get the varying ID, we need to find a comp that has a get/set property of the target
            // found XRInteractorAffordanceStateProvider.interactorSource

            var idC = dataRoot.StoreComp(GetExtType("XRInteractorAffordanceStateProvider", XRAssembly));
            var setr = new PersistentCall(idC.GetType().GetMethod("set_interactorSource", UltEventUtils.AnyAccessBindings), idC);
            new PendingConnection(node.DataInputs[0].Source, evt, setr, 0).Connect(dataRoot);
            evt.PersistentCallsList.Add(setr);
            var serer = new PersistentCall(typeof(JsonUtility).GetMethod("ToJson", new[] { typeof(object) }), null);
            serer.PersistentArguments[0].FSetType(PersistentArgumentType.Object);
            serer.PersistentArguments[0].Object = idC;
            evt.PersistentCallsList.Add(serer);

            // we now have a string with the varying ID in it; extract it.
            var cutStart = new PersistentCall();
            cutStart.FSetMethodName("System.Text.RegularExpressions.Regex, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089.Replace");
            cutStart.FSetArguments(
                new PersistentArgument().ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string)),
                new PersistentArgument().FSetType(PersistentArgumentType.String).FSetString("^(.*\"m_InteractorSource\":)"),
                new PersistentArgument().FSetType(PersistentArgumentType.String).FSetString("")
                );
            evt.PersistentCallsList.Add(cutStart);

            var cutEnd = new PersistentCall();
            cutEnd.FSetMethodName("System.Text.RegularExpressions.Regex, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089.Replace");
            cutEnd.FSetArguments(
                new PersistentArgument().ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string)),
                new PersistentArgument().FSetType(PersistentArgumentType.String).FSetString(",\"m_I(.)*"),
                new PersistentArgument().FSetType(PersistentArgumentType.String).FSetString("")
                );
            evt.PersistentCallsList.Add(cutEnd);

            // at this point, the target serialized refference is the return val of "cutEnd"
            // we're gonna store a "template" version of the retarg evt, then we'll ToJson, insert serilized_ref, FromJson, invoke, (retval fetch?)
            var templateEvt = Instantiate(varyEvt.gameObject);
            templateEvt.name = "templateEvt";
            templateEvt.transform.parent = varyEvt.transform;

            var tmplSerz = new PersistentCall(typeof(JsonUtility).GetMethod("ToJson", new Type[] { typeof(object) }), null);
            tmplSerz.PersistentArguments[0].FSetType(PersistentArgumentType.Object);
            tmplSerz.PersistentArguments[0].Object = templateEvt.GetComponent<UltEventHolder>();
            evt.PersistentCallsList.Add(tmplSerz);
            // we now have the json of the template;
            // a pcall is this: {"_Target":{"instanceID":0},"_MethodName":"get_name","_PersistentArguments":[]}
            // we need to split the whole json in two while ommitting the serialized ref, and we only know the MethodName.
            //regex + string concat shenanigans
            string methName = meth.Method.Name;
            string removePreMethName = "^(.*),\"_MethodName\":\"" + methName + "\"";
            var getPostRef = new PersistentCall();
            getPostRef.FSetMethodName("System.Text.RegularExpressions.Regex, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089.Replace");
            getPostRef.FSetArguments(
                new PersistentArgument().ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string)),
                new PersistentArgument().FSetType(PersistentArgumentType.String).FSetString(removePreMethName),
                new PersistentArgument().FSetType(PersistentArgumentType.String).FSetString(",\"_MethodName\":\"" + methName + "\"")
                );
            evt.PersistentCallsList.Add(getPostRef);
            // this returns everything after the serz ref in the template evt

            string removePostRef = ",\"_MethodName\":\""+ methName + "\".*";
            var removePostRefPCall = new PersistentCall();
            removePostRefPCall.FSetMethodName("System.Text.RegularExpressions.Regex, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089.Replace");
            removePostRefPCall.FSetArguments(
                new PersistentArgument().ToRetVal(evt.PersistentCallsList.Count - 2, typeof(string)),
                new PersistentArgument().FSetType(PersistentArgumentType.String).FSetString(removePostRef),
                new PersistentArgument().FSetType(PersistentArgumentType.String).FSetString("")
                );
            evt.PersistentCallsList.Add(removePostRefPCall);
            // this returns everything until the serz ref (blablabla{ref})

            // we need to cut out the ref
            string removePostRefRef = ".[^{]+$";
            var removePostRefRefPCall = new PersistentCall();
            removePostRefRefPCall.FSetMethodName("System.Text.RegularExpressions.Regex, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089.Replace");
            removePostRefRefPCall.FSetArguments(
                new PersistentArgument().ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string)),
                new PersistentArgument().FSetType(PersistentArgumentType.String).FSetString(removePostRefRef),
                new PersistentArgument().FSetType(PersistentArgumentType.String).FSetString("")
                );
            evt.PersistentCallsList.Add(removePostRefRefPCall);
            // this returns everything before the serz ref in the template evt

            // now we just concat the correct retvals lol
            var concatPCall = new PersistentCall();
            concatPCall.FSetMethodName("System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089.Concat");
            concatPCall.FSetArguments(
                new PersistentArgument().FSetString(typeof(string).AssemblyQualifiedName).FSetType(PersistentArgumentType.ReturnValue).FSetInt(evt.PersistentCallsList.Count-1),
                new PersistentArgument().FSetString(typeof(string).AssemblyQualifiedName).FSetType(PersistentArgumentType.ReturnValue).FSetInt(evt.PersistentCallsList.Count - 5),
                new PersistentArgument().FSetString(typeof(string).AssemblyQualifiedName).FSetType(PersistentArgumentType.ReturnValue).FSetInt(evt.PersistentCallsList.Count - 3));
            evt.PersistentCallsList.Add(concatPCall);
            // this is the template, now reffing the our epic obj
            // just FromJson and Invoke!

            var froJs = new PersistentCall(typeof(JsonUtility).GetMethod("FromJsonOverwrite"), null);
            froJs.PersistentArguments[0].FSetType(PersistentArgumentType.ReturnValue).FSetString(typeof(string).AssemblyQualifiedName).FSetInt(evt.PersistentCallsList.Count-1);
            froJs.PersistentArguments[1].FSetType(PersistentArgumentType.Object);
            froJs.PersistentArguments[1].Object = varyEvt;
            froJs.PersistentArguments[1].FSetString(typeof(object).AssemblyQualifiedName);
            evt.PersistentCallsList.Add(froJs);

            var inv = new PersistentCall(typeof(UltEventHolder).GetMethod("Invoke", new Type[] { }), varyEvt);
            evt.PersistentCallsList.Add(inv);


            // if evt had data output, get data
            if (retValStore != null)
            {
                var getRet = new PersistentCall(retValProp.GetMethod, retValStore);
                evt.PersistentCallsList.Add(getRet);
                node.DataOutputs[0].CompEvt = evt;
                node.DataOutputs[0].CompCall = getRet;
            }
        }

        // calls have been added/linked;
        // set compcall and compile next node.
        if (node.DataOutputs.Length > 0 && node.DataOutputs[0].CompEvt == null)
        {
            node.DataOutputs[0].CompEvt = evt;
            node.DataOutputs[0].CompCall = myCall;
        }
        var nextNode = node.FlowOutputs[0].Target?.Node;
        if (nextNode != null)
            nextNode.Book.CompileNode(evt, nextNode, dataRoot);
        #endregion
    }

    public override Dictionary<string, NodeDef> GetAlternatives(SerializedNode node)
    {
        Type srcType = null;
        if (node.Book == this || node.Book.GetType() == typeof(StaticMethodCookBook))
        {
            SerializedMethod meth = JsonUtility.FromJson<SerializedMethod>(node.BookTag);
            srcType = meth.Method.DeclaringType;
        }
        else if (node.Book.GetType() == typeof(ObjectFieldCookBook))
        {
            SerializedField srcField = JsonUtility.FromJson<SerializedField>(node.BookTag);
            srcType = srcField.Field.DeclaringType;
        }
        else return null;
        Dictionary<string, NodeDef> o = new();
        // figure node method

        List<Type> relatives = new List<Type> { srcType };
        Type cur = srcType;
        while (cur != typeof(object)) {
            cur = cur.BaseType;
            relatives.Add(cur);
        }
        relatives.Add(cur);

        foreach (var t in relatives)
        {
            string tName = t.GetFriendlyName();
            PropertyInfo[] props = t.GetProperties(UltEventUtils.AnyAccessBindings);
            MethodInfo[] meths = t.GetMethods(UltEventUtils.AnyAccessBindings);
            // this code sucks BUT since it only runs on mouse hover, idc
            var lasts = meths.Where(m => m.Name.ToLower().StartsWith("internal_") || m.Name.EndsWith("_Injected"));
            var firsts = meths.Where(m => !lasts.Contains(m));
            foreach (var method in firsts.Concat(lasts))
            {
                if (props.Any(p => p.GetMethod == method || p.SetMethod == method)) continue;
                if (!MyDefs.TryGetValue(method, out NodeDef def)) continue;

                // lets collapse each overload into a submenu!
                // also collapse "Injected" methods
                string inj = method.Name.EndsWith("_Injected") ? "Injected/" : "";
                string inter = method.Name.ToLower().StartsWith("internal_") ? "Internal/" : "";
                if (meths.Any(m => (m.Name == method.Name && m != method) && m.DeclaringType == method.DeclaringType))
                {
                    o.TryAdd(tName + "/Methods/" + inj + inter + method.ReturnType.GetFriendlyName() + " " + def.SearchItem.text.Split('(').First() + "(...)/(" 
                        + string.Join(", ", method.GetParameters().Select(p => p.ParameterType.GetFriendlyName() + " " + p.Name)) +")", def);
                } 
                else
                    o.TryAdd(tName + "/Methods/"+ inj + inter + method.ReturnType.GetFriendlyName() + " " + def.SearchItem.text, def);
            }
            foreach (var prop in props)
            {
                if (prop.CanRead && MyDefs.TryGetValue(prop.GetMethod, out NodeDef getter))
                {
                    o.TryAdd(tName + "/Properties/" + getter.SearchItem.text.Replace(".get_", ".").Split('(').First() + "/Get", getter);
                }
                if (prop.CanWrite && MyDefs.TryGetValue(prop.SetMethod, out NodeDef setter))
                {
                    o.TryAdd(tName + "/Properties/" + setter.SearchItem.text.Replace(".set_", ".").Split('(').First() + "/Set", setter);
                }
            }
        }
        return o;
    }
    // called once on-swap, on the newNode's book
    public override void SwapConnections(SerializedNode oldNode, SerializedNode newNode)
    {
        base.SwapConnections(oldNode, newNode);

        if (oldNode.DataInputs.Length > 0)
        {
            if (oldNode.DataInputs[0].Source != null)
            {
                if (newNode.DataInputs[0].Type.Type.IsAssignableFrom(oldNode.DataInputs[0].Source.Type))
                    oldNode.DataInputs[0].Source.Connect(newNode.DataInputs[0]);
            }
            else
                newNode.DataInputs[0].DefaultObject = oldNode.DataInputs[0].DefaultObject;
        }
    }

    public override void PostCompile(SerializedBowl bowl) // post GetType injection
    {
        // GetType injection only happens to the varyingEvt, so lets copy to templateEvts
        foreach (var varyer in bowl.LastGenerated.GetComponentsInChildren<Transform>(true).Where(t => t.gameObject.name == "varyingEvt"))// find the varyingEvts
        {
            if (varyer.gameObject.name == "varyingEvt")
                varyer.GetChild(0).GetComponent<UltEventHolder>().Event = varyer.GetComponent<UltEventHolder>().Event;
        } 
    }

    
}
#endif
