#if UNITY_EDITOR
using NoodledEvents;
using SLZ.Marrow.Interaction;
using SLZ.Marrow.Utilities;
using SLZ.Marrow.Warehouse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UltEvents;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.UIElements;
using static NoodledEvents.CookBook.NodeDef;


public class ObjectFieldCookBook : CookBook
{
    public override void CollectDefs(List<NodeDef> allDefs)
    {
        foreach (var t in UltNoodleEditor.SearchableTypes)
        {
            try
            {
                if (typeof(UnityEngine.Object).IsAssignableFrom(t) && !typeof(MonoBehaviour).IsAssignableFrom(t) && !typeof(ScriptableObject).IsAssignableFrom(t))
                    continue;

                foreach (var field in t.GetFields(UltEventUtils.AnyAccessBindings))
                {
                    if (field.DeclaringType != t || field.IsStatic) continue;

                    if (!(typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType) || field.FieldType == typeof(string) || field.FieldType == typeof(int) || field.FieldType == typeof(float) || field.FieldType == typeof(bool))) continue;
                    if (field.GetCustomAttribute<NonSerializedAttribute>() != null) continue;
                    if (!(field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)) continue;

                    

                    allDefs.Add(new NodeDef(this, $"{t.GetFriendlyName()}.getf_{field.Name}",
                        inputs: () => new Pin[] { new Pin("Get"), new Pin(t.GetFriendlyName(), t) },
                        outputs: () => new[] { new NodeDef.Pin("got"), new NodeDef.Pin(field.Name, field.FieldType) },
                        bookTag: JsonUtility.ToJson(new SerializedField() { Field = field },
                        overrideTooltip: $"{t.Namespace}.{t.GetFriendlyName()}.getf_{field.Name}"))
                    ); 
                    allDefs.Add(new NodeDef(this, $"{t.GetFriendlyName()}.setf_{field.Name}",
                        inputs: () => new Pin[] { new Pin("Set"), new Pin(t.GetFriendlyName(), t), new NodeDef.Pin(field.Name, field.FieldType) },
                        outputs: () => new[] { new NodeDef.Pin("sot") },
                        bookTag: JsonUtility.ToJson(new SerializedField() { Field = field },
                        overrideTooltip: $"{t.Namespace}.{t.GetFriendlyName()}.setf_{field.Name}"))
                    );
                }
            } catch(TypeLoadException) { };
        }
    }
    
    public override void CompileNode(UltEventBase evt, SerializedNode node, Transform dataRoot)
    {
        // figure node method
        SerializedField field = JsonUtility.FromJson<SerializedField>(node.BookTag);
        evt.EnsurePCallList();

        if (node.DataInputs.Length == 1) // getter
        {
            var toJson = new PersistentCall(typeof(JsonUtility).GetMethod("ToJson", new Type[] { typeof(object) }), null);
            if (node.DataInputs[0].Source == null) toJson.PersistentArguments[0].FSetType(PersistentArgumentType.Object).Object = node.DataInputs[0].DefaultObject;
            else new PendingConnection(node.DataInputs[0].Source, evt, toJson, 0).Connect(dataRoot);
            evt.PersistentCallsList.Add(toJson);

            string startTidBit = "";
            if (field.Field.FieldType == typeof(string)) startTidBit += '\"';
            // regex cut everything b4 the fieldName
            var cutStart = new PersistentCall();
            cutStart.FSetMethodName("System.Text.RegularExpressions.Regex, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089.Replace");
            cutStart.FSetArguments(
                new PersistentArgument(typeof(string)).ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string)),
                new PersistentArgument(typeof(string)).FSetString($".*\"{field.Field.Name}\":" + startTidBit),
                new PersistentArgument(typeof(string)).FSetString("")
            );
            evt.PersistentCallsList.Add(cutStart);

            string endSnipper = "";
            if (typeof(UnityEngine.Object).IsAssignableFrom(field.Field.FieldType)) endSnipper = "(?<=}).*$";
            else if (field.Field.FieldType == typeof(string)) endSnipper = "\".*";
            else if (field.Field.FieldType == typeof(int) || field.Field.FieldType == typeof(float)) endSnipper = ".(?<=[^0-9\\.\\-]).*$"; 
            else if (field.Field.FieldType == typeof(bool)) endSnipper = "(?<=false|true).*$";
            // regex cut everything after the field data
            var cutEnd = new PersistentCall();
            cutEnd.FSetMethodName("System.Text.RegularExpressions.Regex, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089.Replace");
            cutEnd.FSetArguments(
                new PersistentArgument(typeof(string)).ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string)),
                new PersistentArgument(typeof(string)).FSetString(endSnipper),
                new PersistentArgument(typeof(string)).FSetString("")
            );
            evt.PersistentCallsList.Add(cutEnd);


            // at this point we have our field, as string!
            // destringify
            // varies by type :/
            if (typeof(UnityEngine.Object).IsAssignableFrom(field.Field.FieldType))
            {
                // XR COMP YASSSS
                var xrCompStorData = PendingConnection.CompStoragers[typeof(UnityEngine.Object)];
                Component xrComp = dataRoot.StoreComp(xrCompStorData.Item1, "got " + field.Field.Name);
                var jsForm = new PersistentCall(typeof(string).GetMethod(nameof(string.Concat), new Type[] { typeof(string), typeof(string), typeof(string) }), null);
                jsForm.PersistentArguments[0].String = "{\"m_InteractorSource\":";
                jsForm.PersistentArguments[1].ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string));
                jsForm.PersistentArguments[2].String = "}";
                evt.PersistentCallsList.Add(jsForm);

                var fromJson = new PersistentCall(typeof(JsonUtility).GetMethod("FromJsonOverwrite", new Type[] { typeof(string), typeof(object) }), null);
                fromJson.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string));
                fromJson.PersistentArguments[1].FSetType(PersistentArgumentType.Object).Object = xrComp;
                evt.PersistentCallsList.Add(fromJson);

                var getResult = new PersistentCall(xrCompStorData.Item2.GetMethod, xrComp);
                evt.PersistentCallsList.Add(getResult);

                node.DataOutputs[0].CompEvt = evt;
                node.DataOutputs[0].CompCall = getResult;
                if (node.FlowOutputs[0].Target != null)
                    node.FlowOutputs[0].Target.Node.Book.CompileNode(evt, node.FlowOutputs[0].Target.Node, dataRoot);
            }
            else if (field.Field.FieldType == typeof(string))
            {
                node.DataOutputs[0].CompEvt = evt;
                node.DataOutputs[0].CompCall = cutEnd;

                if (node.FlowOutputs[0].Target != null)
                    node.FlowOutputs[0].Target.Node.Book.CompileNode(evt, node.FlowOutputs[0].Target.Node, dataRoot);
            }
            else if (field.Field.FieldType == typeof(int))
            {
                var parser = new PersistentCall(typeof(int).GetMethod("Parse", new Type[] { typeof(string) }), null);
                parser.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string));
                evt.PersistentCallsList.Add(parser);
                node.DataOutputs[0].CompEvt = evt;
                node.DataOutputs[0].CompCall = parser;

                if (node.FlowOutputs[0].Target != null)
                    node.FlowOutputs[0].Target.Node.Book.CompileNode(evt, node.FlowOutputs[0].Target.Node, dataRoot);
            }
            else if (field.Field.FieldType == typeof(float))
            {
                var parser = new PersistentCall(typeof(float).GetMethod("Parse", new Type[] { typeof(string) }), null);
                parser.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string));
                evt.PersistentCallsList.Add(parser);
                node.DataOutputs[0].CompEvt = evt;
                node.DataOutputs[0].CompCall = parser;

                if (node.FlowOutputs[0].Target != null)
                    node.FlowOutputs[0].Target.Node.Book.CompileNode(evt, node.FlowOutputs[0].Target.Node, dataRoot);
            }
            else if (field.Field.FieldType == typeof(bool))
            {
                var parser = new PersistentCall(typeof(bool).GetMethod("Parse", new Type[] { typeof(string) }), null);
                parser.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string));
                evt.PersistentCallsList.Add(parser);
                node.DataOutputs[0].CompEvt = evt;
                node.DataOutputs[0].CompCall = parser;

                if (node.FlowOutputs[0].Target != null)
                    node.FlowOutputs[0].Target.Node.Book.CompileNode(evt, node.FlowOutputs[0].Target.Node, dataRoot);
            }
        }
        else
        {
            // SETTER :(

            // vary by type
            if (typeof(UnityEngine.Object).IsAssignableFrom(field.Field.FieldType))
            {
                //serialize the Obj first
                // set XRComp
                var xrCompStorData = PendingConnection.CompStoragers[typeof(UnityEngine.Object)];
                Component xrComp = dataRoot.StoreComp(xrCompStorData.Item1, "sot " + field.Field.Name);

                var setXR = new PersistentCall(xrCompStorData.Item2.SetMethod, xrComp);
                if (node.DataInputs[1].Source == null) setXR.PersistentArguments[0].Object = node.DataInputs[1].DefaultObject;
                else new PendingConnection(node.DataInputs[1].Source, evt, setXR, 0).Connect(dataRoot);
                evt.PersistentCallsList.Add(setXR);


                // json xrcomp
                var jsXR = new PersistentCall(typeof(JsonUtility).GetMethod("ToJson", new Type[] { typeof(object) }), null);
                jsXR.PersistentArguments[0].FSetType(PersistentArgumentType.Object).Object = xrComp;
                evt.PersistentCallsList.Add(jsXR);

                // cut out start
                var snipXR = new PersistentCall();
                snipXR.FSetMethodName("System.Text.RegularExpressions.Regex, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089.Replace");
                snipXR.FSetArguments(
                    new PersistentArgument(typeof(string)).ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string)),
                    new PersistentArgument(typeof(string)).FSetString(".*\"m_InteractorSource\":"),
                    new PersistentArgument(typeof(string)).FSetString("")
                );
                evt.PersistentCallsList.Add(snipXR);

                // cut out end
                var snipXR2 = new PersistentCall();
                snipXR2.FSetMethodName("System.Text.RegularExpressions.Regex, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089.Replace");
                snipXR2.FSetArguments(
                    new PersistentArgument(typeof(string)).ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string)),
                    new PersistentArgument(typeof(string)).FSetString("(?<=}).*$"),
                    new PersistentArgument(typeof(string)).FSetString("")
                );
                evt.PersistentCallsList.Add(snipXR2);

                // make deserz string
                var overwrStr = new PersistentCall(typeof(string).GetMethod(nameof(string.Concat), new Type[] { typeof(string), typeof(string), typeof(string) }), null);
                overwrStr.PersistentArguments[0].String = "{\"" + field.Field.Name + "\":";
                overwrStr.PersistentArguments[1].ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string));
                overwrStr.PersistentArguments[2].String = "}";
                evt.PersistentCallsList.Add(overwrStr);

                // do the deserz
                var fromJson = new PersistentCall(typeof(JsonUtility).GetMethod("FromJsonOverwrite", new Type[] { typeof(string), typeof(object) }), null);
                fromJson.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string));
                if (node.DataInputs[0].Source == null) fromJson.PersistentArguments[1].FSetType(PersistentArgumentType.Object).Object = node.DataInputs[0].DefaultObject;
                else new PendingConnection(node.DataInputs[0].Source, evt, fromJson, 1).Connect(dataRoot);
                evt.PersistentCallsList.Add(fromJson);
            }
            else if (field.Field.FieldType == typeof(string))
            {
                // make deserz string
                var overwrStr = new PersistentCall(typeof(string).GetMethod(nameof(string.Concat), new Type[] { typeof(string), typeof(string), typeof(string) }), null);
                overwrStr.PersistentArguments[0].String = "{\"" + field.Field.Name + "\":\"";
                if (node.DataInputs[1].Source == null) overwrStr.PersistentArguments[1].String = node.DataInputs[1].DefaultStringValue;
                else new PendingConnection(node.DataInputs[1].Source, evt, overwrStr, 1).Connect(dataRoot);
                overwrStr.PersistentArguments[2].String = "\"}";
                evt.PersistentCallsList.Add(overwrStr);

                // do the deserz
                var fromJson = new PersistentCall(typeof(JsonUtility).GetMethod("FromJsonOverwrite", new Type[] { typeof(string), typeof(object) }), null);
                fromJson.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string));
                if (node.DataInputs[0].Source == null) fromJson.PersistentArguments[1].FSetType(PersistentArgumentType.Object).Object = node.DataInputs[0].DefaultObject;
                else new PendingConnection(node.DataInputs[0].Source, evt, fromJson, 1).Connect(dataRoot);
                evt.PersistentCallsList.Add(fromJson);
            }
            else if (field.Field.FieldType == typeof(int))
            {
                var intStr = new PersistentCall(typeof(string).GetMethod(nameof(string.Format), new Type[] { typeof(string), typeof(object) }), null);
                intStr.PersistentArguments[0].String = "{0}";
                if (node.DataInputs[1].Source == null) intStr.PersistentArguments[1].FSetType(PersistentArgumentType.Int).FSetInt(node.DataInputs[1].DefaultIntValue);
                else new PendingConnection(node.DataInputs[1].Source, evt, intStr, 1).Connect(dataRoot);
                evt.PersistentCallsList.Add(intStr);

                var overwrStr = new PersistentCall(typeof(string).GetMethod(nameof(string.Concat), new Type[] { typeof(string), typeof(string), typeof(string) }), null);
                overwrStr.PersistentArguments[0].String = "{\"" + field.Field.Name + "\":";
                overwrStr.PersistentArguments[1].ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string));
                overwrStr.PersistentArguments[2].String = "}";
                evt.PersistentCallsList.Add(overwrStr);

                // do the deserz
                var fromJson = new PersistentCall(typeof(JsonUtility).GetMethod("FromJsonOverwrite", new Type[] { typeof(string), typeof(object) }), null);
                fromJson.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string));
                if (node.DataInputs[0].Source == null) fromJson.PersistentArguments[1].FSetType(PersistentArgumentType.Object).Object = node.DataInputs[0].DefaultObject;
                else new PendingConnection(node.DataInputs[0].Source, evt, fromJson, 1).Connect(dataRoot);
                evt.PersistentCallsList.Add(fromJson);
            }
            else if (field.Field.FieldType == typeof(float))
            {
                var fltStr = new PersistentCall(typeof(string).GetMethod(nameof(string.Format), new Type[] { typeof(string), typeof(object) }), null);
                fltStr.PersistentArguments[0].String = "{0}";
                if (node.DataInputs[1].Source == null) fltStr.PersistentArguments[1].FSetType(PersistentArgumentType.Float).Float = node.DataInputs[1].DefaultFloatValue;
                else new PendingConnection(node.DataInputs[1].Source, evt, fltStr, 1).Connect(dataRoot);
                evt.PersistentCallsList.Add(fltStr);

                var overwrStr = new PersistentCall(typeof(string).GetMethod(nameof(string.Concat), new Type[] { typeof(string), typeof(string), typeof(string) }), null);
                overwrStr.PersistentArguments[0].String = "{\"" + field.Field.Name + "\":";
                overwrStr.PersistentArguments[1].ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string));
                overwrStr.PersistentArguments[2].String = "}";
                evt.PersistentCallsList.Add(overwrStr);

                // do the deserz
                var fromJson = new PersistentCall(typeof(JsonUtility).GetMethod("FromJsonOverwrite", new Type[] { typeof(string), typeof(object) }), null);
                fromJson.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string));
                if (node.DataInputs[0].Source == null) fromJson.PersistentArguments[1].FSetType(PersistentArgumentType.Object).Object = node.DataInputs[0].DefaultObject;
                else new PendingConnection(node.DataInputs[0].Source, evt, fromJson, 1).Connect(dataRoot);
                evt.PersistentCallsList.Add(fromJson);
            }
            else if (field.Field.FieldType == typeof(bool))
            {
                var fltStr = new PersistentCall(typeof(string).GetMethod(nameof(string.Format), new Type[] { typeof(string), typeof(object) }), null);
                fltStr.PersistentArguments[0].String = "{0}";
                if (node.DataInputs[1].Source == null) fltStr.PersistentArguments[1].FSetType(PersistentArgumentType.Bool).Bool = node.DataInputs[1].DefaultBoolValue;
                else new PendingConnection(node.DataInputs[1].Source, evt, fltStr, 1).Connect(dataRoot);
                evt.PersistentCallsList.Add(fltStr);

                // ughhh we need to lowercase the True/False
                var Ttot = new PersistentCall();
                Ttot.FSetMethodName("System.Text.RegularExpressions.Regex, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089.Replace");
                Ttot.FSetArguments(
                    new PersistentArgument(typeof(string)).ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string)),
                    new PersistentArgument(typeof(string)).FSetString("T"),
                    new PersistentArgument(typeof(string)).FSetString("t")
                );
                evt.PersistentCallsList.Add(Ttot);
                var Ftof = new PersistentCall();
                Ftof.FSetMethodName("System.Text.RegularExpressions.Regex, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089.Replace");
                Ftof.FSetArguments(
                    new PersistentArgument(typeof(string)).ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string)),
                    new PersistentArgument(typeof(string)).FSetString("F"),
                    new PersistentArgument(typeof(string)).FSetString("f")
                );
                evt.PersistentCallsList.Add(Ftof);

                var overwrStr = new PersistentCall(typeof(string).GetMethod(nameof(string.Concat), new Type[] { typeof(string), typeof(string), typeof(string) }), null);
                overwrStr.PersistentArguments[0].String = "{\"" + field.Field.Name + "\":";
                overwrStr.PersistentArguments[1].ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string));
                overwrStr.PersistentArguments[2].String = "}";
                evt.PersistentCallsList.Add(overwrStr);

                // do the deserz
                var fromJson = new PersistentCall(typeof(JsonUtility).GetMethod("FromJsonOverwrite", new Type[] { typeof(string), typeof(object) }), null);
                fromJson.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.Count - 1, typeof(string));
                if (node.DataInputs[0].Source == null) fromJson.PersistentArguments[1].FSetType(PersistentArgumentType.Object).Object = node.DataInputs[0].DefaultObject;
                else new PendingConnection(node.DataInputs[0].Source, evt, fromJson, 1).Connect(dataRoot);
                evt.PersistentCallsList.Add(fromJson);
            }
            // comp next node
            if (node.FlowOutputs[0].Target != null)
                node.FlowOutputs[0].Target.Node.Book.CompileNode(evt, node.FlowOutputs[0].Target.Node, dataRoot);
        }
    }
    public override void PostCompile(SerializedBowl bowl) 
    {
    }
}
#endif