#if UNITY_EDITOR
using Newtonsoft.Json;
using NoodledEvents;
using System;
using System.Collections.Generic;
using System.Reflection;
using UltEvents;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static NoodledEvents.CookBook.NodeDef;


public class ObjectFieldCookBook : CookBook
{
    private Dictionary<FieldInfo, (NodeDef, NodeDef)> MyDefs = new();
    public override void CollectDefs(List<NodeDef> allDefs) // why aren't these threaded
    {
        MyDefs.Clear();
        foreach (var t in UltNoodleEditor.SearchableTypes)
        {
            try
            {
                foreach (var field in t.GetFields(UltEventUtils.AnyAccessBindings))
                {
                    var getter =
                        (new NodeDef(this, $"{t.GetFriendlyName()}.getf_{field.Name}",
                            inputs: () => field.IsStatic ? new Pin[] { new Pin("Reflection Get") } : new Pin[] { new Pin("Reflection Get"), new Pin(t.GetFriendlyName(), t) },
                            outputs: () => new[] { new NodeDef.Pin("got"), new NodeDef.Pin(field.Name, field.FieldType) },
                            bookTag: JsonUtility.ToJson(new SerializedField() { Field = field }),
                            tooltipOverride: $"{t.Namespace}.{t.GetFriendlyName()}.getf_{field.Name} (Reflection)")
                        );
                    allDefs.Add(getter);
                    var setter =
                    (new NodeDef(this, $"{t.GetFriendlyName()}.setf_{field.Name}",
                        inputs: () => field.IsStatic ? new Pin[] { new Pin("Reflection Set"), new NodeDef.Pin(field.Name, field.FieldType) } : new Pin[] { new Pin("Reflection Set"), new Pin(t.GetFriendlyName(), t), new NodeDef.Pin(field.Name, field.FieldType) },
                        outputs: () => new[] { new NodeDef.Pin("sot") },
                        bookTag: JsonUtility.ToJson(new SerializedField() { Field = field }),
                        tooltipOverride: $"{t.Namespace}.{t.GetFriendlyName()}.setf_{field.Name} (Reflection)")
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

        
        // So only 1 pcall needs to be changed for get/set
        // still will be 25~+ nodes tho :/

        int typeStringArr = evt.PersistentCallsList.FindOrAddGetTyper<string[]>();

        PersistentCall name2arr = new PersistentCall(typeof(JsonConvert).GetMethod("DeserializeObject", new Type[] { typeof(string), typeof(Type) }), null);
        name2arr.PersistentArguments[0].String = $"[\"{field.Field.Name}\"]";
        name2arr.PersistentArguments[1].ToRetVal(typeStringArr, typeof(Type));
        evt.PersistentCallsList.Add(name2arr);

        int typeBindingFlagArr = evt.PersistentCallsList.FindOrAddGetTyper<BindingFlags[]>();

        PersistentCall getBindingFlag = new PersistentCall(typeof(JsonConvert).GetMethod("DeserializeObject", new Type[] { typeof(string), typeof(Type) }), null);
        getBindingFlag.PersistentArguments[0].String = "[60]";
        getBindingFlag.PersistentArguments[1].ToRetVal(typeBindingFlagArr, typeof(Type));
        evt.PersistentCallsList.Add(getBindingFlag);

        int typeSysObj = evt.PersistentCallsList.FindOrAddGetTyper<object>();

        int typeType = evt.PersistentCallsList.FindOrAddGetTyper<Type>();

        PersistentCall paramSysObjArr = new PersistentCall(typeof(Array).GetMethod("CreateInstance", new Type[] { typeof(Type), typeof(int) }), null);
        paramSysObjArr.PersistentArguments[0].ToRetVal(typeSysObj, typeof(Type));
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

        int typeTypeArr = evt.PersistentCallsList.FindOrAddGetTyper<Type[]>();

        PersistentCall getParamTypes = new PersistentCall(typeof(JsonConvert).GetMethod("DeserializeObject", new Type[] { typeof(string), typeof(Type) }), null);
        getParamTypes.PersistentArguments[0].String = "[\"System.String, mscorlib\", \"System.Reflection.BindingFlags, mscorlib\"]";
        getParamTypes.PersistentArguments[1].ToRetVal(typeTypeArr, typeof(Type));
        evt.PersistentCallsList.Add(getParamTypes);

        int typeFieldInfo = evt.PersistentCallsList.FindOrAddGetTyper<FieldInfo>();

        PersistentCall getGetFieldMethod = new PersistentCall(typeof(System.ComponentModel.MemberDescriptor).GetMethod("FindMethod", UltEventUtils.AnyAccessBindings, null,
            new Type[] { typeof(Type), typeof(string), typeof(Type[]), typeof(Type), typeof(bool) }, null), null);
        getGetFieldMethod.PersistentArguments[0].ToRetVal(typeType, typeof(Type));
        getGetFieldMethod.PersistentArguments[1].String = "GetField";
        getGetFieldMethod.PersistentArguments[2].ToRetVal(evt.PersistentCallsList.IndexOf(getParamTypes), typeof(Type[]));
        getGetFieldMethod.PersistentArguments[3].ToRetVal(typeFieldInfo, typeof(Type));
        getGetFieldMethod.PersistentArguments[4].Bool = false;
        evt.PersistentCallsList.Add(getGetFieldMethod);

        int typeTargType = evt.PersistentCallsList.FindOrAddGetTyper(field.Field.DeclaringType);

        PersistentCall getFieldInfo = new PersistentCall(Type.GetType("System.SecurityUtils, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", true, true).GetMethod("MethodInfoInvoke", UltEventUtils.AnyAccessBindings, null,
            new Type[] { typeof(MethodInfo), typeof(object), typeof(object[]) }, null), null);
        getFieldInfo.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.IndexOf(getGetFieldMethod), typeof(MethodInfo));
        getFieldInfo.PersistentArguments[1].ToRetVal(typeTargType, typeof(object));
        getFieldInfo.PersistentArguments[2].ToRetVal(evt.PersistentCallsList.IndexOf(paramSysObjArr), typeof(object[]));
        evt.PersistentCallsList.Add(getFieldInfo);

        // GRAHHH Getter/Setter needs shitass rewrite :/
        //oki
        //System.ComponentModel.MemberDescriptor.FindMethod(typeof(FieldInfo), "FieldInfo.SetValue", ["obj", "obj"], null, false):
        //Array.CreateInstance<object>(2)
        //Array[0] = targ obj
        //Array[1] = targ val
        //System.SecurityUtils.MethodInfoInvoke(set, fieldinfo, arr)
        //System.ComponentModel.MemberDescriptor.FindMethod(typeof(FieldInfo), "FieldInfo.GetValue", ["obj"], typof(object), false):
        //Array.CreateInstance<object>(1)
        //Array[0] = targ obj
        //return System.SecurityUtils.MethodInfoInvoke(get, fieldinfo, arr)

        bool isGetter = field.Field.IsStatic ? (node.DataInputs.Length == 0) : (node.DataInputs.Length == 1);

        if (isGetter) // getter 
        {
            PersistentCall objObjArr1 = new PersistentCall(typeof(JsonConvert).GetMethod("DeserializeObject", new Type[] { typeof(string), typeof(Type) }), null);
            objObjArr1.PersistentArguments[0].String = "[\"System.Object, mscorlib\"]";
            objObjArr1.PersistentArguments[1].ToRetVal(typeTypeArr, typeof(Type));
            evt.PersistentCallsList.Add(objObjArr1);

            PersistentCall getGetValueMethod = new PersistentCall(typeof(System.ComponentModel.MemberDescriptor).GetMethod("FindMethod", UltEventUtils.AnyAccessBindings, null,
            new Type[] { typeof(Type), typeof(string), typeof(Type[]), typeof(Type), typeof(bool) }, null), null);
            getGetValueMethod.PersistentArguments[0].ToRetVal(typeFieldInfo, typeof(Type));
            getGetValueMethod.PersistentArguments[1].String = "GetValue";
            getGetValueMethod.PersistentArguments[2].ToRetVal(evt.PersistentCallsList.IndexOf(objObjArr1), typeof(Type[]));
            getGetValueMethod.PersistentArguments[3].ToRetVal(typeSysObj, typeof(Type));
            getGetValueMethod.PersistentArguments[4].Bool = false;
            evt.PersistentCallsList.Add(getGetValueMethod);

            PersistentCall smallTargArr = new PersistentCall(typeof(Array).GetMethod("CreateInstance", new Type[] { typeof(Type), typeof(int) }), null);
            smallTargArr.PersistentArguments[0].ToRetVal(typeSysObj, typeof(Type));
            smallTargArr.PersistentArguments[1].Int = 1;
            evt.PersistentCallsList.Add(smallTargArr);

            var editorSetCall = new PersistentCall(typeof(UltNoodleRuntimeExtensions).GetMethod("ArrayItemSetter1", UltEventUtils.AnyAccessBindings), null);
            editorSetCall.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.IndexOf(smallTargArr), typeof(Array));
            editorSetCall.PersistentArguments[1].Int = 0;
            if (!field.Field.IsStatic)
            {
                if (node.DataInputs[0].Source != null)
                    new PendingConnection(node.DataInputs[0].Source, evt, editorSetCall, 2).Connect(dataRoot);
                else editorSetCall.PersistentArguments[2].FSetType(node.DataInputs[0].GetPCallType()).Value = node.DataInputs[0].GetDefault();
            }
            else editorSetCall.PersistentArguments[2].FSetType(PersistentArgumentType.Object);
            evt.PersistentCallsList.Add(editorSetCall);

            var ingameSetCall = new PersistentCall();
            ingameSetCall.CopyFrom(editorSetCall);
            ingameSetCall.FSetMethodName("System.Linq.Expressions.Interpreter.CallInstruction, System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e.ArrayItemSetter1");
            ingameSetCall.FSetMethod(null);
            evt.PersistentCallsList.Add(ingameSetCall);

            PersistentCall getValue = new PersistentCall(Type.GetType("System.SecurityUtils, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", true, true).GetMethod("MethodInfoInvoke", UltEventUtils.AnyAccessBindings, null,
            new Type[] { typeof(MethodInfo), typeof(object), typeof(object[]) }, null), null);
            getValue.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.IndexOf(getGetValueMethod), typeof(MethodInfo));

            getValue.PersistentArguments[1].ToRetVal(evt.PersistentCallsList.IndexOf(getFieldInfo), typeof(object));

            getValue.PersistentArguments[2].ToRetVal(evt.PersistentCallsList.IndexOf(smallTargArr), typeof(object[]));
            evt.PersistentCallsList.Add(getValue);

            node.DataOutputs[0].CompEvt = evt;
            node.DataOutputs[0].CompCall = getValue;

            if (node.FlowOutputs[0].Target != null)
                node.FlowOutputs[0].Target.Node.Book.CompileNode(evt, node.FlowOutputs[0].Target.Node, dataRoot);
        }
        else // setter
        {
            PersistentCall objObjArr2 = new PersistentCall(typeof(JsonConvert).GetMethod("DeserializeObject", new Type[] { typeof(string), typeof(Type) }), null);
            objObjArr2.PersistentArguments[0].String = "[\"System.Object, mscorlib\", \"System.Object, mscorlib\"]";
            objObjArr2.PersistentArguments[1].ToRetVal(typeTypeArr, typeof(Type));
            evt.PersistentCallsList.Add(objObjArr2);

            PersistentCall getSetValueMethod = new PersistentCall(typeof(System.ComponentModel.MemberDescriptor).GetMethod("FindMethod", UltEventUtils.AnyAccessBindings, null,
            new Type[] { typeof(Type), typeof(string), typeof(Type[]), typeof(Type), typeof(bool) }, null), null);
            getSetValueMethod.PersistentArguments[0].ToRetVal(typeFieldInfo, typeof(Type));
            getSetValueMethod.PersistentArguments[1].String = "SetValue";
            getSetValueMethod.PersistentArguments[2].ToRetVal(evt.PersistentCallsList.IndexOf(objObjArr2), typeof(Type[]));
            getSetValueMethod.PersistentArguments[3].ToRetVal(evt.PersistentCallsList.FindOrAddGetTyper(typeof(void)), typeof(Type));
            getSetValueMethod.PersistentArguments[4].Bool = false;
            evt.PersistentCallsList.Add(getSetValueMethod); 

            PersistentCall twoTargArr = new PersistentCall(typeof(Array).GetMethod("CreateInstance", new Type[] { typeof(Type), typeof(int) }), null);
            twoTargArr.PersistentArguments[0].ToRetVal(typeSysObj, typeof(Type));
            twoTargArr.PersistentArguments[1].Int = 2;
            evt.PersistentCallsList.Add(twoTargArr);

            var editorSetCall = new PersistentCall(typeof(UltNoodleRuntimeExtensions).GetMethod("ArrayItemSetter1", UltEventUtils.AnyAccessBindings), null);
            editorSetCall.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.IndexOf(twoTargArr), typeof(Array));
            editorSetCall.PersistentArguments[1].Int = 0;
            if (!field.Field.IsStatic)
            {
                if (node.DataInputs[0].Source != null)
                    new PendingConnection(node.DataInputs[0].Source, evt, editorSetCall, 2).Connect(dataRoot);
                else editorSetCall.PersistentArguments[2].FSetType(node.DataInputs[0].GetPCallType()).Value = node.DataInputs[0].GetDefault();
            }
            else editorSetCall.PersistentArguments[2].FSetType(PersistentArgumentType.Object);
            evt.PersistentCallsList.Add(editorSetCall);

            var ingameSetCall = new PersistentCall();
            ingameSetCall.CopyFrom(editorSetCall);
            ingameSetCall.FSetMethodName("System.Linq.Expressions.Interpreter.CallInstruction, System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e.ArrayItemSetter1");
            ingameSetCall.FSetMethod(null);
            evt.PersistentCallsList.Add(ingameSetCall);

            var editorSetCall2 = new PersistentCall(typeof(UltNoodleRuntimeExtensions).GetMethod("ArrayItemSetter1", UltEventUtils.AnyAccessBindings), null);
            editorSetCall2.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.IndexOf(twoTargArr), typeof(Array));
            editorSetCall2.PersistentArguments[1].Int = 1;
            int srcIdx = field.Field.IsStatic ? 0 : 1;
            if (node.DataInputs[srcIdx].Source != null)
                new PendingConnection(node.DataInputs[srcIdx].Source, evt, editorSetCall2, 2).Connect(dataRoot);
            else editorSetCall2.PersistentArguments[2].FSetType(node.DataInputs[srcIdx].GetPCallType()).Value = node.DataInputs[srcIdx].GetDefault();
            evt.PersistentCallsList.Add(editorSetCall2);

            var ingameSetCall2 = new PersistentCall();
            ingameSetCall2.CopyFrom(editorSetCall2);
            ingameSetCall2.FSetMethodName("System.Linq.Expressions.Interpreter.CallInstruction, System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e.ArrayItemSetter1");
            ingameSetCall2.FSetMethod(null);
            evt.PersistentCallsList.Add(ingameSetCall2);

            PersistentCall setValue = new PersistentCall(Type.GetType("System.SecurityUtils, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", true, true).GetMethod("MethodInfoInvoke", UltEventUtils.AnyAccessBindings, null,
            new Type[] { typeof(MethodInfo), typeof(object), typeof(object[]) }, null), null);
            setValue.PersistentArguments[0].ToRetVal(evt.PersistentCallsList.IndexOf(getSetValueMethod), typeof(MethodInfo));

            setValue.PersistentArguments[1].ToRetVal(evt.PersistentCallsList.IndexOf(getFieldInfo), typeof(object));

            setValue.PersistentArguments[2].ToRetVal(evt.PersistentCallsList.IndexOf(twoTargArr), typeof(object[]));
            evt.PersistentCallsList.Add(setValue);

            if (node.FlowOutputs[0].Target != null)
                node.FlowOutputs[0].Target.Node.Book.CompileNode(evt, node.FlowOutputs[0].Target.Node, dataRoot);
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
                if (field.DeclaringType != t) continue;

                // reflection :)
                //if (!(typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType) || field.FieldType == typeof(string) || field.FieldType == typeof(int) || field.FieldType == typeof(float) || field.FieldType == typeof(bool))) continue;
                //if (field.GetCustomAttribute<NonSerializedAttribute>() != null) continue;
                //if (!(field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)) continue;

                if (!MyDefs.TryGetValue(field, out (NodeDef, NodeDef) nodes)) continue;

                if (!field.IsStatic)
                {
                    o.TryAdd(tName + "/Fields/" + field.FieldType.GetFriendlyName() + " " + field.Name.Replace('_', ' ') + "/Get", nodes.Item1);
                    o.TryAdd(tName + "/Fields/" + field.FieldType.GetFriendlyName() + " " + field.Name.Replace('_', ' ') + "/Set", nodes.Item2);
                } else
                {
                    o.TryAdd(tName + "/Static/Fields/" + field.FieldType.GetFriendlyName() + " " + field.Name.Replace('_', ' ') + "/Get", nodes.Item1);
                    o.TryAdd(tName + "/Static/Fields/" + field.FieldType.GetFriendlyName() + " " + field.Name.Replace('_', ' ') + "/Set", nodes.Item2);
                }
            }
        }
        return o;
    }
    public override void SwapConnections(SerializedNode oldNode, SerializedNode newNode)
    {
        base.SwapConnections(oldNode, newNode);

        if (oldNode.DataInputs.Length > 0 && newNode.DataInputs.Length > 0)
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
