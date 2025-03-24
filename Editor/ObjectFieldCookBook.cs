#if UNITY_EDITOR
using Newtonsoft.Json;
using NoodledEvents;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UltEvents;
using UnityEngine;
using UnityEngine.UIElements;
using static NoodledEvents.CookBook.NodeDef;


public class ObjectFieldCookBook : CookBook
{
    private Dictionary<FieldInfo, (NodeDef, NodeDef)> MyDefs = new();
    public override void CollectDefs(List<NodeDef> allDefs)
    {
        MyDefs.Clear();
        foreach (var t in UltNoodleEditor.SearchableTypes)
        {
            try
            {
                if (typeof(UnityEngine.Object).IsAssignableFrom(t) && !typeof(MonoBehaviour).IsAssignableFrom(t) && !typeof(ScriptableObject).IsAssignableFrom(t))
                    continue;

                foreach (var field in t.GetFields(UltEventUtils.AnyAccessBindings))
                {
                    if (field.DeclaringType != t || field.IsStatic) continue;

                    // these could all be one if really needed
                    if (!(typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType) || field.FieldType == typeof(string) || field.FieldType == typeof(int) || field.FieldType == typeof(float) || field.FieldType == typeof(bool))) 
                    {
                        // Field can't be got via JsonUtility.ToJson due to type :(
                        ReflectionGetField();
                        continue; 
                    }
                    if (field.GetCustomAttribute<NonSerializedAttribute>() != null)
                    {
                        // Field can't be got via JsonUtility.ToJson due to NonSerializedAttribute
                        ReflectionGetField();
                        continue;
                    }
                    if (!(field.IsPublic || field.GetCustomAttribute<SerializeField>() != null))
                    {
                        // Field Is Private, & not a "SerializeField" - So no JsonUtility.ToJson
                        ReflectionGetField();
                        continue;
                    }
                    void ReflectionGetField()
                    {
                        var getter =
                        (new NodeDef(this, $"{t.GetFriendlyName()}.getff_{field.Name}",
                            inputs: () => new Pin[] { new Pin("Reflection Get"), new Pin(t.GetFriendlyName(), t) },
                            outputs: () => new[] { new NodeDef.Pin("got"), new NodeDef.Pin(field.Name, field.FieldType) },
                            bookTag: JsonUtility.ToJson(new SerializedField() { Field = field }),
                            tooltipOverride: $"{t.Namespace}.{t.GetFriendlyName()}.getf_{field.Name}")
                        );
                        allDefs.Add(getter);
                        var setter =
                        (new NodeDef(this, $"{t.GetFriendlyName()}.setff_{field.Name}",
                            inputs: () => new Pin[] { new Pin("Reflection Set"), new Pin(t.GetFriendlyName(), t), new NodeDef.Pin(field.Name, field.FieldType) },
                            outputs: () => new[] { new NodeDef.Pin("sot") },
                            bookTag: JsonUtility.ToJson(new SerializedField() { Field = field }),
                            tooltipOverride: $"{t.Namespace}.{t.GetFriendlyName()}.setf_{field.Name}")
                        );
                        allDefs.Add(setter);
                        MyDefs.Add(field, (getter, setter));
                    }
                    

                    

                    var getter = 
                    (new NodeDef(this, $"{t.GetFriendlyName()}.getf_{field.Name}",
                        inputs: () => new Pin[] { new Pin("Json Get"), new Pin(t.GetFriendlyName(), t) },
                        outputs: () => new[] { new NodeDef.Pin("got"), new NodeDef.Pin(field.Name, field.FieldType) },
                        bookTag: JsonUtility.ToJson(new SerializedField() { Field = field }),
                        tooltipOverride: $"{t.Namespace}.{t.GetFriendlyName()}.getf_{field.Name}")
                    );
                    allDefs.Add(getter);
                    var setter =
                    (new NodeDef(this, $"{t.GetFriendlyName()}.setf_{field.Name}",
                        inputs: () => new Pin[] { new Pin("Json Set"), new Pin(t.GetFriendlyName(), t), new NodeDef.Pin(field.Name, field.FieldType) },
                        outputs: () => new[] { new NodeDef.Pin("sot") },
                        bookTag: JsonUtility.ToJson(new SerializedField() { Field = field }),
                        tooltipOverride: $"{t.Namespace}.{t.GetFriendlyName()}.setf_{field.Name}")
                    );
                    allDefs.Add(setter);
                    MyDefs.Add(field, (getter, setter));
                }
            } catch(TypeLoadException) { };
        }
    }
    
    public override void CompileNode(UltEventBase evt, SerializedNode node, Transform dataRoot)
    {
        SerializedField field = JsonUtility.FromJson<SerializedField>(node.BookTag);
        evt.EnsurePCallList();


        if (!(typeof(UnityEngine.Object).IsAssignableFrom(field.Field.FieldType) || field.Field.FieldType == typeof(string) || field.Field.FieldType == typeof(int) || field.Field.FieldType == typeof(float) || field.Field.FieldType == typeof(bool))
        || (field.Field.GetCustomAttribute<NonSerializedAttribute>() != null)
        || (!(field.Field.IsPublic || field.Field.GetCustomAttribute<SerializeField>() != null)))
        {
            #region Reflection Type
            // So only 1 pcall needs to be changed for get/set
            // still will be 25~+ nodes tho :/

            PersistentCall typeStringArr = new PersistentCall(typeof(Type).GetMethod("GetType", new Type[] { typeof(string), typeof(bool), typeof(bool) }), null);
            typeStringArr.PersistentArguments[0].String = "System.String[], mscorlib";
            typeStringArr.PersistentArguments[1].Bool = true;
            typeStringArr.PersistentArguments[2].Bool = true;
            evt.PersistentCallsList.Add(typeStringArr);

            PersistentCall name2arr = new PersistentCall(typeof(JsonConvert).GetMethod("DeserializeObject", new Type[] { typeof(string), typeof(Type) }), null);
            name2arr.PersistentArguments[0].String = $"[\"{field.Field.Name}\"]";
            name2arr.PersistentArguments[1].ToRetVal(evt.PersistentCallsList.IndexOf(typeStringArr), typeof(Type));
            evt.PersistentCallsList.Add(name2arr);

            PersistentCall typeBindingFlagArr = new PersistentCall(typeof(Type).GetMethod("GetType", new Type[] { typeof(string), typeof(bool), typeof(bool) }), null);
            typeBindingFlagArr.PersistentArguments[0].String = "System.Reflection.BindingFlags[], mscorlib";
            typeBindingFlagArr.PersistentArguments[1].Bool = true;
            typeBindingFlagArr.PersistentArguments[2].Bool = true;
            evt.PersistentCallsList.Add(typeBindingFlagArr);

            PersistentCall getBindingFlag = new PersistentCall(typeof(JsonConvert).GetMethod("DeserializeObject", new Type[] { typeof(string), typeof(Type) }), null);
            getBindingFlag.PersistentArguments[0].String = "[60]";
            getBindingFlag.PersistentArguments[1].ToRetVal(evt.PersistentCallsList.IndexOf(typeBindingFlagArr), typeof(Type));
            evt.PersistentCallsList.Add(getBindingFlag);

            PersistentCall typeSysObj = new PersistentCall(typeof(Type).GetMethod("GetType", new Type[] { typeof(string), typeof(bool), typeof(bool) }), null);
            typeSysObj.PersistentArguments[0].String = typeof(object).AssemblyQualifiedName;
            typeSysObj.PersistentArguments[1].Bool = true;
            typeSysObj.PersistentArguments[2].Bool = true;
            evt.PersistentCallsList.Add(typeSysObj);

            PersistentCall typeType = new PersistentCall(typeof(Type).GetMethod("GetType", new Type[] { typeof(string), typeof(bool), typeof(bool) }), null);
            typeType.PersistentArguments[0].String = typeof(Type).AssemblyQualifiedName;
            typeType.PersistentArguments[1].Bool = true;
            typeType.PersistentArguments[2].Bool = true;
            evt.PersistentCallsList.Add(typeType);

            PersistentCall paramSysObjArr = new PersistentCall(typeof(Array).GetMethod("CreateInstance", new Type[] { typeof(Type), typeof(int) }), null);
            paramSysObjArr.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.IndexOf(typeSysObj), typeof(Type));
            paramSysObjArr.PersistentArguments[1].Int = 2;
            evt.PersistentCallsList.Add(paramSysObjArr);

            PersistentCall param1load = new PersistentCall(typeof(Array).GetMethod("Copy", new Type[]
            { typeof(Array), typeof(int), typeof(Array), typeof(int), typeof(int)}), null);
            //Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length
            param1load.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.IndexOf(name2arr), typeof(Array));
            param1load.PersistentArguments[1].Int = 0;
            param1load.PersistentArguments[2].ToRetVal(evt.PersistentCallsList.IndexOf(paramSysObjArr), typeof(Array));
            param1load.PersistentArguments[3].Int = 0;
            param1load.PersistentArguments[4].Int = 1;
            evt.PersistentCallsList.Add(param1load);

            PersistentCall param2load = new PersistentCall(typeof(Array).GetMethod("Copy", new Type[]
            { typeof(Array), typeof(int), typeof(Array), typeof(int), typeof(int)}), null);
            //Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length
            param2load.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.IndexOf(getBindingFlag), typeof(Array));
            param2load.PersistentArguments[1].Int = 0;
            param2load.PersistentArguments[2].ToRetVal(evt.PersistentCallsList.IndexOf(paramSysObjArr), typeof(Array));
            param2load.PersistentArguments[3].Int = 1;
            param2load.PersistentArguments[4].Int = 1;
            evt.PersistentCallsList.Add(param2load);

            PersistentCall typeTypeArr = new PersistentCall(typeof(Type).GetMethod("GetType", new Type[] { typeof(string), typeof(bool), typeof(bool) }), null);
            typeTypeArr.PersistentArguments[0].String = typeof(Type[]).AssemblyQualifiedName;
            typeTypeArr.PersistentArguments[1].Bool = true;
            typeTypeArr.PersistentArguments[2].Bool = true;
            evt.PersistentCallsList.Add(typeTypeArr);

            PersistentCall getParamTypes = new PersistentCall(typeof(JsonConvert).GetMethod("DeserializeObject", new Type[] { typeof(string), typeof(Type) }), null);
            getParamTypes.PersistentArguments[0].String = "[\"System.String, mscorlib\", \"System.Reflection.BindingFlags, mscorlib\"]";
            getParamTypes.PersistentArguments[1].ToRetVal(evt.PersistentCallsList.IndexOf(typeTypeArr), typeof(Type));
            evt.PersistentCallsList.Add(getParamTypes);

            PersistentCall typeFieldInfo = new PersistentCall(typeof(Type).GetMethod("GetType", new Type[] { typeof(string), typeof(bool), typeof(bool) }), null);
            typeFieldInfo.PersistentArguments[0].String = typeof(FieldInfo).AssemblyQualifiedName;
            typeFieldInfo.PersistentArguments[1].Bool = true;
            typeFieldInfo.PersistentArguments[2].Bool = true;
            evt.PersistentCallsList.Add(typeFieldInfo);

            PersistentCall getGetFieldMethod = new PersistentCall(typeof(System.ComponentModel.MemberDescriptor).GetMethod("FindMethod", UltEventUtils.AnyAccessBindings, null,
                new Type[] { typeof(Type), typeof(string), typeof(Type[]), typeof(Type), typeof(bool) }, null), null);
            getGetFieldMethod.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.IndexOf(typeType), typeof(Type));
            getGetFieldMethod.PersistentArguments[1].String = "GetField";
            getGetFieldMethod.PersistentArguments[2].ToRetVal(evt.PersistentCallsList.IndexOf(getParamTypes), typeof(Type[]));
            getGetFieldMethod.PersistentArguments[3].ToRetVal(evt.PersistentCallsList.IndexOf(typeFieldInfo), typeof(Type));
            getGetFieldMethod.PersistentArguments[4].Bool = false;
            evt.PersistentCallsList.Add(getGetFieldMethod);

            PersistentCall typeTargType = new PersistentCall(typeof(Type).GetMethod("GetType", new Type[] { typeof(string), typeof(bool), typeof(bool) }), null);
            typeTargType.PersistentArguments[0].String = field.Field.DeclaringType.AssemblyQualifiedName;
            typeTargType.PersistentArguments[1].Bool = true;
            typeTargType.PersistentArguments[2].Bool = true;
            evt.PersistentCallsList.Add(typeTargType);

            PersistentCall getFieldInfo = new PersistentCall(Type.GetType("System.SecurityUtils, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", true, true).GetMethod("MethodInfoInvoke", UltEventUtils.AnyAccessBindings, null,
                new Type[] { typeof(MethodInfo), typeof(object), typeof(object[]) }, null), null);
            getFieldInfo.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.IndexOf(getGetFieldMethod), typeof(MethodInfo));
            getFieldInfo.PersistentArguments[1].ToRetVal(evt.PersistentCallsList.IndexOf(typeTargType), typeof(object));
            getFieldInfo.PersistentArguments[2].ToRetVal(evt.PersistentCallsList.IndexOf(paramSysObjArr), typeof(object[]));
            evt.PersistentCallsList.Add(getFieldInfo);

            if (node.DataInputs.Length == 1) // getter 
            {
                PersistentCall getValue = new PersistentCall(Type.GetType("Newtonsoft.Json.Utilities.ReflectionUtils, Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed", true, true).GetMethod("GetMemberValue", UltEventUtils.AnyAccessBindings, null,
                    new Type[] { typeof(MemberInfo), typeof(object) }, null), null);
                getValue.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.IndexOf(getFieldInfo), typeof(MemberInfo));

                if (node.DataInputs[0].Source == null) getValue.PersistentArguments[1].FSetType(PersistentArgumentType.Object).Object = node.DataInputs[0].DefaultObject;
                else new PendingConnection(node.DataInputs[0].Source, evt, getValue, 1).Connect(dataRoot);
                evt.PersistentCallsList.Add(getValue);

                node.DataOutputs[0].CompEvt = evt;
                node.DataOutputs[0].CompCall = getValue;

                if (node.FlowOutputs[0].Target != null)
                    node.FlowOutputs[0].Target.Node.Book.CompileNode(evt, node.FlowOutputs[0].Target.Node, dataRoot);

            } else // setter
            {
                PersistentCall setValue = new PersistentCall(Type.GetType("Newtonsoft.Json.Utilities.ReflectionUtils, Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed", true, true).GetMethod("SetMemberValue", UltEventUtils.AnyAccessBindings, null,
                    new Type[] { typeof(MemberInfo), typeof(object), typeof(object) }, null), null);
                setValue.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.IndexOf(getFieldInfo), typeof(MemberInfo));

                if (node.DataInputs[0].Source == null) setValue.PersistentArguments[1].FSetType(PersistentArgumentType.Object).Object = node.DataInputs[0].DefaultObject;
                else new PendingConnection(node.DataInputs[0].Source, evt, setValue, 1).Connect(dataRoot);

                if (node.DataInputs[1].Source == null)
                {
                    switch (node.DataInputs[1].Type.Type.GetFriendlyName())
                    {
                        case "bool":
                            setValue.PersistentArguments[2].FSetType(PersistentArgumentType.Bool);
                            setValue.PersistentArguments[2].Bool = node.DataInputs[1].DefaultBoolValue;
                            break;
                        case "float":
                            setValue.PersistentArguments[2].FSetType(PersistentArgumentType.Float);
                            setValue.PersistentArguments[2].Float = node.DataInputs[1].DefaultFloatValue;
                            break;
                        case "int":
                            setValue.PersistentArguments[2].FSetType(PersistentArgumentType.Int);
                            setValue.PersistentArguments[2].Int = node.DataInputs[1].DefaultIntValue;
                            break;
                        case "Vector2":
                            setValue.PersistentArguments[2].FSetType(PersistentArgumentType.Vector2);
                            setValue.PersistentArguments[2].Vector2 = node.DataInputs[1].DefaultVector2Value;
                            break;
                        case "Vector3":
                            setValue.PersistentArguments[2].FSetType(PersistentArgumentType.Vector3);
                            setValue.PersistentArguments[2].Vector3 = node.DataInputs[1].DefaultVector3Value;
                            break;
                        case "Vector4":
                            setValue.PersistentArguments[2].FSetType(PersistentArgumentType.Vector4);
                            setValue.PersistentArguments[2].Vector4 = node.DataInputs[1].DefaultVector4Value;
                            break;
                        case "Quaternion":
                            setValue.PersistentArguments[2].FSetType(PersistentArgumentType.Quaternion);
                            setValue.PersistentArguments[2].Quaternion = node.DataInputs[1].DefaultQuaternionValue;
                            break;
                        case "string":
                            setValue.PersistentArguments[2].FSetType(PersistentArgumentType.String);
                            setValue.PersistentArguments[2].String = node.DataInputs[1].DefaultStringValue;
                            break;
                        default:
                            if (node.DataInputs[1].Type == typeof(UnityEngine.Object))
                                setValue.PersistentArguments[2].FSetType(PersistentArgumentType.Object).Object = node.DataInputs[2].DefaultObject;
                            break;
                    }
                }
                else new PendingConnection(node.DataInputs[1].Source, evt, setValue, 2).Connect(dataRoot);

                evt.PersistentCallsList.Add(setValue);

                if (node.FlowOutputs[0].Target != null)
                    node.FlowOutputs[0].Target.Node.Book.CompileNode(evt, node.FlowOutputs[0].Target.Node, dataRoot);
            }

            #endregion
        }
        else
        {
            #region Json Type

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
            #endregion
        }
    }

    public override Dictionary<string, NodeDef> GetAlternatives(SerializedNode node)
    {
        Type srcType = null;
        if (node.Book == this)
        {
            SerializedField srcField = JsonUtility.FromJson<SerializedField>(node.BookTag);
            srcType = srcField.Field.DeclaringType;
        }
        else if (node.Book.GetType() == typeof(ObjectMethodCookBook) || node.Book.GetType() == typeof(StaticMethodCookBook))
        {
            SerializedMethod meth = JsonUtility.FromJson<SerializedMethod>(node.BookTag);
            srcType = meth.Method.DeclaringType;
        }
        else return null;

        Dictionary<string, NodeDef> o = new();

        List<Type> relatives = new List<Type> { srcType };
        Type cur = srcType;
        while (cur != typeof(object))
        {
            cur = cur.BaseType;
            relatives.Add(cur);
        }
        relatives.Add(cur);

        foreach (var t in relatives)
        {
            if (typeof(UnityEngine.Object).IsAssignableFrom(t) && !typeof(MonoBehaviour).IsAssignableFrom(t) && !typeof(ScriptableObject).IsAssignableFrom(t))
                continue;

            string tName = t.GetFriendlyName();
            foreach (var field in t.GetFields(UltEventUtils.AnyAccessBindings))
            {
                if (field.DeclaringType != t || field.IsStatic) continue;

                // reflection :)
                //if (!(typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType) || field.FieldType == typeof(string) || field.FieldType == typeof(int) || field.FieldType == typeof(float) || field.FieldType == typeof(bool))) continue;
                //if (field.GetCustomAttribute<NonSerializedAttribute>() != null) continue;
                //if (!(field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)) continue;

                if (!MyDefs.TryGetValue(field, out (NodeDef, NodeDef) nodes)) continue;

                o.Add(tName + "/Fields/" + field.FieldType.GetFriendlyName() + " " + field.Name.Replace('_',' ') + "/Get", nodes.Item1);
                o.Add(tName + "/Fields/" + field.FieldType.GetFriendlyName() + " " + field.Name.Replace('_', ' ') + "/Set", nodes.Item2);
            }
        }
        return o;
    }
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
            else newNode.DataInputs[0].DefaultObject = oldNode.DataInputs[0].DefaultObject;
        }
    }
    public override void PostCompile(SerializedBowl bowl) 
    {
    }
}
#endif
