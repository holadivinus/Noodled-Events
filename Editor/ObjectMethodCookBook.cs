#if UNITY_EDITOR
using NoodledEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UltEvents;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit.AffordanceSystem.State;
using static NoodledEvents.CookBook.NodeDef;


public class ObjectMethodCookBook : CookBook
{
    public override void CollectDefs(List<NodeDef> allDefs)
    {
        foreach (var t in UltNoodleEditor.SearchableTypes)
        {
            if (!(t.IsSubclassOf(typeof(UnityEngine.Object)) || t == typeof(UnityEngine.Object)))
                continue;

            
            foreach (var meth in t.GetMethods(UltEventUtils.AnyAccessBindings))
            {
                if (meth.DeclaringType != t || meth.IsStatic) continue;
                allDefs.Add(new NodeDef(meth.Name, 
                    inputs:() => 
                    {
                        var @params = meth.GetParameters();
                        if (@params == null || @params.Length == 0) return new Pin[] { new NodeDef.Pin("Exec"), new Pin(meth.DeclaringType.Name, meth.DeclaringType) };
                        return @params.Select(p => new Pin(p.Name, p.ParameterType)).Prepend(new Pin(meth.DeclaringType.Name, meth.DeclaringType)).Prepend(new NodeDef.Pin("Exec")).ToArray(); 
                    },
                    outputs:() => 
                    {
                        if (meth.ReturnType != typeof(void))
                            return new[] { new NodeDef.Pin("Done"), new NodeDef.Pin(meth.ReturnType.Name, meth.ReturnType) };
                        else return new[] { new NodeDef.Pin("Done") };
                    },
                    searchItem:(def) =>
                    {
                        var o = new Button(() =>
                        {
                            // create serialized node.
                            
                            if (UltNoodleEditor.NewNodeBowl == null) return;
                            var nod = UltNoodleEditor.NewNodeBowl.AddNode(meth.Name, this).MatchDef(def);

                            nod.BookTag = JsonUtility.ToJson(new SerializedMethod() { Method = meth });

                            nod.Position = UltNoodleEditor.NewNodePos;
                            UltNoodleEditor.NewNodeBowl.Validate(); // update ui 
                        });

                        // button text
                        o.text = meth.DeclaringType.GetFriendlyName() + "." + meth.Name;
                        var p = meth.GetParameters();
                        if (p.Length == 0) o.text += "()";
                        else
                        {
                            o.text += "(";
                            foreach (var paramType in p)
                            {
                                o.text += paramType.ParameterType.GetFriendlyName() + " " + paramType.Name + ", ";
                            }
                            o.text = o.text.Substring(0, o.text.Length - 2);
                            o.text += ")";
                        }

                        if (meth.ReturnType != typeof(void))
                            o.text += " -> " + meth.ReturnType.Name;


                        return o;
                    })
                );
            }
        }
    }
    
    public override void CompileNode(UltEventBase evt, SerializedNode node, Transform dataRoot)
    {
        // figure node method
        SerializedMethod meth = JsonUtility.FromJson<SerializedMethod>(node.BookTag);

        // sanity check (AddComponent() leaves this field empty)
        if (evt.PersistentCallsList == null) evt._PersistentCalls = new();

        // make my PCall    
        PersistentCall myCall = new PersistentCall((MethodInfo)meth.Method, node.DataInputs[0].DefaultObject); 
        if (node.DataInputs.Length > 0)
            myCall.FSetArguments(new PersistentArgument[node.DataInputs.Length - 1]);

        UltEventHolder varyEvt = null;
        UltEventBase pre = evt;
        // if the source varies
        if (node.DataInputs[0].Source != null)
        {
            // we need to json
            // make event for jsonning
            varyEvt = dataRoot.StoreComp<UltEventHolder>("varying");
            varyEvt.Event = new UltEvent();
            varyEvt.Event._PersistentCalls = new();
            evt = varyEvt.Event; // move stuff to the targevt
            myCall.FSetTarget(null);
        }
        
        

        // foreach input
        for (int j = 0; j < node.DataInputs.Length; j++) 
        {
            // first data-in is allways the targ obj;
            if (j == 0) continue;

            NoodleDataInput @in = node.DataInputs[j];

            if (@in.Source != null) // is connected
            {
                // ensure it's in the same evt
                if (@in.Source.CompEvt == evt)
                {
                    new PendingConnection(evt, @in.Source.CompCall, myCall, j - 1).Connect(dataRoot);
                    continue;
                } else
                {
                    new PendingConnection(@in.Source.CompEvt, @in.Source.CompCall, evt, myCall, j - 1).Connect(dataRoot);
                    continue;
                }
            } else
            {
                // this input uses a default value!
                myCall.PersistentArguments[j - 1] = new PersistentArgument(meth.Parameters[j - 1]);

                switch (myCall.PersistentArguments[j - 1].Type)
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
                        break;
                }
            }
        }

        evt.PersistentCallsList.Add(myCall);

        if (node.DataInputs[0].Source != null)
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

            var idC = dataRoot.StoreComp<XRInteractorAffordanceStateProvider>();
            var setr = new PersistentCall(idC.GetType().GetMethod("set_interactorSource", UltEventUtils.AnyAccessBindings), idC);
            new PendingConnection(node.DataInputs[0].Source, evt, setr, 0).Connect(dataRoot);
            evt._PersistentCalls.Add(setr);
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
            templateEvt.transform.parent = varyEvt.transform.parent;

            var tmplSerz = new PersistentCall(typeof(JsonUtility).GetMethod("ToJson", new Type[] { typeof(object) }), null);
            tmplSerz.PersistentArguments[0].FSetType(PersistentArgumentType.Object);
            tmplSerz.PersistentArguments[0].Object = templateEvt.GetComponent<UltEventHolder>();
            evt.PersistentCallsList.Add(tmplSerz);
            // we now have the json of the template;
            // a pcall is this: {"_Target":{"instanceID":0},"_MethodName":"get_name","_PersistentArguments":[]}
            // we need to split the whole json in two while ommitting the serialized ref, and we only know the MethodName.
            //regex + string concat shenanigans

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
    }
}
#endif