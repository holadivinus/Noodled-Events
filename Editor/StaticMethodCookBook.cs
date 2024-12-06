#if UNITY_EDITOR
using NoodledEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UltEvents;
using UnityEngine;
using UnityEngine.UIElements;
using static NoodledEvents.CookBook.NodeDef;


public class StaticMethodCookBook : CookBook
{
    public override void CollectDefs(List<NodeDef> allDefs)
    {
        foreach (var t in UltNoodleEditor.SearchableTypes)
        {
            MethodInfo[] methods = null;
            try
            {
                methods = t.GetMethods(UltEventUtils.AnyAccessBindings);
            } catch(TypeLoadException) { continue; }

            foreach (var meth in methods)
            {
                if (meth.DeclaringType != t || !meth.IsStatic) continue;
                allDefs.Add(new NodeDef(t.Name + "." + meth.Name, 
                    inputs:() => 
                    {
                        var @params = meth.GetParameters();
                        if (@params == null || @params.Length == 0) return new Pin[] { new NodeDef.Pin("Exec") };
                        return @params.Select(p => new Pin(p.Name, p.ParameterType)).Prepend(new NodeDef.Pin("Exec")).ToArray(); 
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
                            var nod = UltNoodleEditor.NewNodeBowl.AddNode("static " + meth.Name, this).MatchDef(def);

                            nod.BookTag = JsonUtility.ToJson(new SerializedMethod() { Method = meth });

                            nod.Position = UltNoodleEditor.NewNodePos;
                            UltNoodleEditor.NewNodeBowl.Validate(); // update ui 
                        });
                        o.text = "static " + meth.DeclaringType.GetFriendlyName() + "." + meth.Name;
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

        // foreach input

        PersistentCall myCall = new PersistentCall((MethodInfo)meth.Method, null); // make my PCall
        if (node.DataInputs.Length > 0)
            myCall.FSetArguments(new PersistentArgument[node.DataInputs.Length]);


        for (int j = 0; j < node.DataInputs.Length; j++) // foreach node input
        {

            NoodleDataInput @in = node.DataInputs[j];


            if (@in.Source != null) // is connected
            {
                // ensure it's in the same evt
                if (@in.Source.CompEvt == evt)
                {
                    new PendingConnection(evt, @in.Source.CompCall, myCall, j).Connect(dataRoot);
                    continue;
                } else
                {
                    new PendingConnection(@in.Source.CompEvt, @in.Source.CompCall, evt, myCall, j).Connect(dataRoot);
                    continue;
                }
            } else
            {
                // this input uses a default value!
                myCall.PersistentArguments[j] = new PersistentArgument(meth.Parameters[j]);

                switch (myCall.PersistentArguments[j].Type)
                {
                    case PersistentArgumentType.Bool:
                        myCall.PersistentArguments[j].Bool = node.DataInputs[j].DefaultBoolValue;
                        break;
                    case PersistentArgumentType.String:
                        myCall.PersistentArguments[j].String = node.DataInputs[j].DefaultStringValue;
                        break;
                    case PersistentArgumentType.Int:
                        myCall.PersistentArguments[j].Int = node.DataInputs[j].DefaultIntValue;
                        break;
                    case PersistentArgumentType.Enum:
                        myCall.PersistentArguments[j].Enum = node.DataInputs[j].DefaultIntValue;
                        break;
                    case PersistentArgumentType.Float:
                        myCall.PersistentArguments[j].Float = node.DataInputs[j].DefaultFloatValue;
                        break;
                    case PersistentArgumentType.Vector2:
                        myCall.PersistentArguments[j].Vector2 = node.DataInputs[j].DefaultVector2Value;
                        break;
                    case PersistentArgumentType.Vector3:
                        myCall.PersistentArguments[j].Vector3 = node.DataInputs[j].DefaultVector3Value;
                        break;
                    case PersistentArgumentType.Vector4:
                        myCall.PersistentArguments[j].Vector4 = node.DataInputs[j].DefaultVector4Value;
                        break;
                    case PersistentArgumentType.Quaternion:
                        myCall.PersistentArguments[j].Quaternion = node.DataInputs[j].DefaultQuaternionValue;
                        break;
                    case PersistentArgumentType.Color:
                        myCall.PersistentArguments[j].Color = node.DataInputs[j].DefaultColorValue;
                        break;
                    case PersistentArgumentType.Color32:
                        myCall.PersistentArguments[j].Color32 = node.DataInputs[j].DefaultColorValue; // hope this implicitly casts well
                        break;
                    case PersistentArgumentType.Rect:
                        Vector4 v = node.DataInputs[j].DefaultVector4Value;
                        myCall.PersistentArguments[j].Rect = new Rect(v.x, v.y, v.z, v.w);
                        break;
                    case PersistentArgumentType.Object:
                        myCall.PersistentArguments[j].Object = node.DataInputs[j].DefaultObject;
                        break;
                }
            }
        }
        
        if (evt._PersistentCalls == null) evt._PersistentCalls = new List<PersistentCall>();
        evt.PersistentCallsList.Add(myCall);
        // calls have been added/linked;
        // set compcall and compile next node.
        if (node.DataOutputs.Length > 0)
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