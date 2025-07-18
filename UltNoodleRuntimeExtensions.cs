#if UNITY_EDITOR
using Newtonsoft.Json;
using NoodledEvents;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using UltEvents;
using UnityEngine;

    
public static class UltNoodleRuntimeExtensions
{
    private static FieldInfo s_methodGetSet = typeof(PersistentCall).GetField("_Method", UltEventUtils.AnyAccessBindings);
    private static FieldInfo s_methodNameGetSet = typeof(PersistentCall).GetField("_MethodName", UltEventUtils.AnyAccessBindings);
    private static FieldInfo s_targetGetSet = typeof(PersistentCall).GetField("_Target", UltEventUtils.AnyAccessBindings);
    private static FieldInfo s_listGetSet = typeof(UltEventBase).GetField("_PersistentCalls", UltEventUtils.AnyAccessBindings);
    private static FieldInfo s_PersistentArgumentsGetSet = typeof(PersistentCall).GetField("_PersistentArguments", UltEventUtils.AnyAccessBindings);
    private static FieldInfo s_PersistentArgumentTypeGetSet = typeof(PersistentArgument).GetField("_Type", UltEventUtils.AnyAccessBindings);
    private static FieldInfo s_PersistentArgumentStringGetSet = typeof(PersistentArgument).GetField("_String", UltEventUtils.AnyAccessBindings);
    private static FieldInfo s_PersistentArgumentIntGetSet = typeof(PersistentArgument).GetField("_Int", UltEventUtils.AnyAccessBindings);
    public static UnityEngine.Object FGetTarget(this PersistentCall call)
        => (UnityEngine.Object)s_targetGetSet.GetValue(call);
    public static void FSetTarget(this PersistentCall call, UnityEngine.Object target)
        => s_targetGetSet.SetValue(call, target);
    public static MethodBase FGetMethod(this PersistentCall call)
        => (MethodBase)s_methodGetSet.GetValue(call);
    public static void FSetMethod(this PersistentCall call, MethodBase method)
        => s_methodGetSet.SetValue(call, method);
    public static void FSetMethodName(this PersistentCall call, string name)
        => s_methodNameGetSet.SetValue(call, name);
    public static void FSetArguments(this PersistentCall call, params PersistentArgument[] args)
        => s_PersistentArgumentsGetSet.SetValue(call, args);
    public static PersistentArgument FSetType(this PersistentArgument arg, PersistentArgumentType t)
    { s_PersistentArgumentTypeGetSet.SetValue(arg, t); return arg; }
    public static PersistentArgument SafeSetValue(this PersistentArgument arg, object val)
    {

        if (val == null || val.ToString() == "null")// kms
        {

            if (Type.GetType(arg.FGetString()) != typeof(Type))
                if (arg.Type == PersistentArgumentType.ReturnValue || arg.Type == PersistentArgumentType.None || arg.Type == PersistentArgumentType.Object)
                    if (string.IsNullOrWhiteSpace(arg.FGetString()))
                        arg.FSetType(PersistentArgumentType.Object).FSetString(typeof(object).AssemblyQualifiedName);

            return arg;
        }
        if (val.GetType().IsEnum)
        {
            arg.Value = val;
            arg.FSetString(val.GetType().AssemblyQualifiedName);
        }
        else arg.Value = val;
        return arg;
    }
    public static PersistentArgumentType FGetType(this PersistentArgument arg)
        => (PersistentArgumentType)s_PersistentArgumentTypeGetSet.GetValue(arg);
    public static PersistentArgument FSetString(this PersistentArgument arg, string s)
    { s_PersistentArgumentStringGetSet.SetValue(arg, s); return arg; }
    public static string FGetString(this PersistentArgument arg) => (string)s_PersistentArgumentStringGetSet.GetValue(arg);
    public static PersistentArgument FSetInt(this PersistentArgument arg, int i)
    {
        s_PersistentArgumentIntGetSet.SetValue(arg, i);
        return arg;
    }
    public static void FSetPCalls(this UltEventBase evt, List<PersistentCall> list)
    {
        s_listGetSet.SetValue(evt, list);
    }
    public static int FGetInt(this PersistentArgument arg)
        => (int)s_PersistentArgumentIntGetSet.GetValue(arg);

    public static void SafeInsert(this List<PersistentCall> list, int idx, PersistentCall call)
    {
        list.Insert(idx, call);
        foreach (var pc in list)
            foreach (var ar in pc.PersistentArguments)
                if (ar.Type == PersistentArgumentType.ReturnValue && ar.FGetInt() >= idx)
                    ar.FSetInt(ar.FGetInt() + 1);

    }
    public static void EnsurePCallList(this UltEventBase evt)
    {
        if (evt.PersistentCallsList == null)
            evt.FSetPCalls(new List<PersistentCall>()); // bruh
    }
    public static PersistentArgument ToRetVal(this PersistentArgument arg, int idx, Type t)
    {
        arg.FSetType(PersistentArgumentType.ReturnValue);
        arg.FSetInt(idx);
        arg.FSetString(t.AssemblyQualifiedName);
        return arg;
    }
    public static PersistentArgument ToParamVal(this PersistentArgument arg, int argIdx, Type t)
    {
        arg.FSetType(PersistentArgumentType.Parameter);
        arg.FSetInt(argIdx);
        arg.FSetString(t.AssemblyQualifiedName);
        return arg;
    }
    public static T StoreComp<T>(this Transform dataStore, string name = null) where T : Component
    {
        var gobj = new GameObject(name ?? (typeof(T).Name + " store"), typeof(T));
        gobj.SetActive(false);
        gobj.transform.parent = dataStore.transform;
        return gobj.GetComponent<T>();
    }
    public static Component StoreComp(this Transform dataStore, Type compType, string name = null)
    {
        var gobj = new GameObject(name ?? (compType.Name + " store"), compType);
        gobj.SetActive(false);
        gobj.transform.parent = dataStore.transform;
        return gobj.GetComponent(compType);
    }
    public static Transform StoreTransform(this Transform dataStore, string name = null)
    {
        var gobj = new GameObject(name ?? "Transform store");
        gobj.SetActive(false);
        gobj.transform.parent = dataStore.transform;
        return gobj.transform;
    }
    public static Type GetRetType(this MethodBase method)
    {
        switch (method)
        {
            case MethodInfo mi:
                return mi.ReturnType;
            case ConstructorInfo:
                return method.DeclaringType;
            default:
                throw new Exception("MethodBase has no retval: " + method.DeclaringType.Name + "." + method.Name);
        }
    }
    public static List<SerializedNode> GatherDescendants(this SerializedNode node, List<SerializedNode> list = null)
    {
        if (list == null) list = new List<SerializedNode>();
        foreach (var o in node.FlowOutputs)
        {
            if (o.Target != null)
            {
                list.Add(o.Target.Node);
                GatherDescendants(o.Target.Node, list);
            }
        }
        return list;
    }
    public static SerializedNode MatchDef(this SerializedNode nod, CookBook.NodeDef def)
    {
        foreach (var @in in def.Inputs)
        {
            if (@in.Flow) nod.AddFlowIn(@in.Name);
            else nod.AddDataIn(@in.Name, @in.Type, null, @in.Const);
        }
        foreach (var @out in def.Outputs)
        {
            if (@out.Flow) nod.AddFlowOut(@out.Name);
            else nod.AddDataOut(@out.Name, @out.Type);
        }
        return nod;
    }
    public static PersistentArgumentType GetArgType(this Type type)
    {
        if (type == typeof(bool)) return PersistentArgumentType.Bool;
        if (type == typeof(int)) return PersistentArgumentType.Int;
        if (type == typeof(string)) return PersistentArgumentType.String;
        if (type.IsEnum) return PersistentArgumentType.Enum;
        if (type == typeof(float)) return PersistentArgumentType.Float;
        if (type == typeof(Vector2)) return PersistentArgumentType.Vector2;
        if (type == typeof(Vector3)) return PersistentArgumentType.Vector3;
        if (type == typeof(Vector4)) return PersistentArgumentType.Vector4;
        if (type == typeof(Quaternion)) return PersistentArgumentType.Quaternion;
        if (type == typeof(Color)) return PersistentArgumentType.Color;
        if (type == typeof(Color32)) return PersistentArgumentType.Color32;
        if (type == typeof(Rect)) return PersistentArgumentType.Rect;
        if (type.IsSubclassOf(typeof(UnityEngine.Object)) || type == typeof(UnityEngine.Object)) return PersistentArgumentType.Object;
        return PersistentArgumentType.None;
    }
    public static MethodBase ToMethod(this string serializedMethodName, Type[] paramz)
    {
        Type declaringType = null;
        string methodName = "";
        var lastDot = serializedMethodName.LastIndexOf('.');
        if (lastDot < 0)
            return null;
        else
        {
            declaringType = Type.GetType(serializedMethodName.Substring(0, lastDot));
            lastDot++;
            methodName = serializedMethodName.Substring(lastDot, serializedMethodName.Length - lastDot);
            if (methodName == "ctor") return declaringType.GetConstructor(UltEventUtils.AnyAccessBindings, null, paramz, null); ;
            return declaringType.GetMethod(methodName, UltEventUtils.AnyAccessBindings, null, paramz, null);
        }
    }
    public static Type ToType(this string serializedMethodName)
    {
        var lastDot = serializedMethodName.LastIndexOf('.');
        if (lastDot < 0)
            return null;
        else return Type.GetType(serializedMethodName.Substring(0, lastDot));
    }

    public static void ArrayItemSetter1(Array array, int idx, object obj) => array.SetValue(obj, idx);

    private static MethodInfo[] GetTypeMethods = {
        typeof(Type).GetMethod("GetType", new[] { typeof(string), typeof(bool), typeof(bool) }),
        typeof(Type).GetMethod("GetType", new[] { typeof(string), typeof(bool) }),
        typeof(Type).GetMethod("GetType", new[] { typeof(string) })
    };
    public static int FindOrAddGetTyper<T>(this List<PersistentCall> list) => list.FindOrAddGetTyper(typeof(T));
    public static int FindOrAddGetTyper(this List<PersistentCall> list, Type t)
    {
        return list.FindOrAddGetTyper(t.AssemblyQualifiedName);
    }
    public static int FindOrAddGetTyper(this List<PersistentCall> list, string t)
    {
        for (int i = 0; i < list.Count; i++)
        {
            PersistentCall call = list[i];
            if (GetTypeMethods.Contains(call.Method)
             && call.PersistentArguments[0].Type == PersistentArgumentType.String
             && call.PersistentArguments[0].String == t) // if is GetType call
            {
                return i;
            }
        }
        // none found, add
        var newCall = new PersistentCall(GetTypeMethods[0], null);
        newCall.PersistentArguments[0].String = t;
        newCall.PersistentArguments[1].Bool = true;
        newCall.PersistentArguments[2].Bool = true;
        list.Add(newCall);
        return list.Count - 1;
    }
    public static MethodInfo ArrayCreateMethod = typeof(Array).GetMethod("CreateInstance", UltEventUtils.AnyAccessBindings, null,
                new Type[] { typeof(Type), typeof(int) }, null);
    public static MethodInfo JsonDeserializeMethod = typeof(JsonConvert).GetMethod("DeserializeObject", UltEventUtils.AnyAccessBindings, null,
                new Type[] { typeof(string), typeof(Type) }, null);
    public static MethodInfo JsonSeserializeMethod = typeof(JsonConvert).GetMethod("SerializeObject", UltEventUtils.AnyAccessBindings, null,
                new Type[] { typeof(object) }, null);
    public static int FindOrAddJsonDeserialize(this List<PersistentCall> list, string jsonString, Type targType)
    {
        int typeGet = list.FindOrAddGetTyper(targType);
        for (int i = 0; i < list.Count; i++)
        {
            PersistentCall pcall = list[i];
            if (pcall.Method == JsonDeserializeMethod
             && pcall.PersistentArguments[0].FGetString() == jsonString
             && pcall.PersistentArguments[1].FGetInt() == typeGet)
            {
                return i;
            }
        }
        var newCall = new PersistentCall(JsonDeserializeMethod, null);
        newCall.PersistentArguments[0].FSetString(jsonString);
        newCall.PersistentArguments[1].ToRetVal(typeGet, typeof(Type));
        list.Add(newCall);
        return list.Count - 1;
    }
    public static int CreateArray(this List<PersistentCall> list, Type arrayType, int length, bool @new = false)
    {
        int typeGet = list.FindOrAddGetTyper(arrayType);
        if (!@new)
        {
            for (int i = 0; i < list.Count; i++)
            {
                PersistentCall pcall = list[i];
                if (pcall.Method == ArrayCreateMethod
                 && pcall.PersistentArguments[0].FGetInt() == typeGet
                 && pcall.PersistentArguments[1].FGetInt() == length)
                {
                    return i;
                }
            }
        }

        var arrCreateCall = new PersistentCall(ArrayCreateMethod, null);
        arrCreateCall.PersistentArguments[0].ToRetVal(typeGet, typeof(Type));
        arrCreateCall.PersistentArguments[1].FSetInt(length);
        list.Add(arrCreateCall);
        return list.Count - 1;
    }
    public static int FindOrAddGetTypeArr(this List<PersistentCall> list, params Type[] ts)
    {
        if (ts.Length == 0)
            return list.CreateArray(typeof(Type), 0, @new: false);

        string jsonStr = "[";
        foreach (var jT in ts)
            jsonStr += $"\"{jT.AssemblyQualifiedName}\",";
        jsonStr = jsonStr[..^1] + "]";

        return list.FindOrAddJsonDeserialize(jsonStr, typeof(Type[]));
    }
    public static MethodInfo FindMethod = typeof(System.ComponentModel.MemberDescriptor).GetMethod("FindMethod", UltEventUtils.AnyAccessBindings, null,
                new Type[] { typeof(Type), typeof(string), typeof(Type[]), typeof(Type), typeof(bool) }, null);
    public static MethodInfo GetMethod = typeof(Type).GetMethod("GetMethod", new Type[] { typeof(string), typeof(BindingFlags), typeof(Binder), typeof(Type[]), typeof(ParameterModifier[]) });
    public static int FindOrAddGetMethodInfo(this List<PersistentCall> list, MethodInfo m)
    {
        int declaringType = list.FindOrAddGetTyper(m.DeclaringType);
        return FindOrAddGetMethodInfo(list, declaringType, m.Name, m.GetParameters().Select(p => p.ParameterType).ToArray(), m.DeclaringType.GenericTypeArguments, list.FindOrAddGetTyper(m.ReturnType));
    }
    public static int FindOrAddGetMethodInfo(this List<PersistentCall> list, int declaringType, string methodName, Type[] @params, Type[] typeGenerics, int retVal)
    {
        // first get the declaring type
        int paramArr = list.FindOrAddGetTypeArr(@params);
        if (typeGenerics.Length > 0)
        {
            // for generics, we gotta run Type.GetMethod on declaringType
            int typeBindingFlag = list.FindOrAddJsonDeserialize("60", typeof(BindingFlags));

            return list.AddRunMethod(GetMethod, declaringType, new PersistentArgument(typeof(string)).FSetString(methodName), typeBindingFlag, null, paramArr, null);
        }

        for (int i = 0; i < list.Count; i++)
        {
            PersistentCall pcall = list[i];
            if (pcall.Method == FindMethod
             && pcall.PersistentArguments[0].FGetInt() == declaringType // type
             && pcall.PersistentArguments[1].FGetString() == methodName     // method name
             && pcall.PersistentArguments[2].FGetInt() == paramArr      // method params
             && pcall.PersistentArguments[3].FGetInt() == retVal)   // return type
            {
                return i;
            }
        }

        var newGetMethodInfoCall = new PersistentCall(FindMethod, null);
        newGetMethodInfoCall.PersistentArguments[0].ToRetVal(declaringType, typeof(Type));
        newGetMethodInfoCall.PersistentArguments[1].FSetString(methodName);
        newGetMethodInfoCall.PersistentArguments[2].ToRetVal(paramArr, typeof(Type[]));
        newGetMethodInfoCall.PersistentArguments[3].ToRetVal(retVal, typeof(Type));
        list.Add(newGetMethodInfoCall);
        return list.Count - 1;
    }
    public static MethodInfo GetField = typeof(Type).GetMethod("GetField", new Type[] { typeof(string), typeof(BindingFlags) });
    public static int AddGetFieldInfo(this List<PersistentCall> list, FieldInfo f)
        => list.AddGetFieldInfo(f.DeclaringType, f.Name);
    public static int AddGetFieldInfo(this List<PersistentCall> list, Type declarer, string fieldName)
    {
        int declaringType = list.FindOrAddGetTyper(declarer);
        return list.AddGetFieldInfo(declaringType, fieldName);
    }
    public static int AddGetFieldInfo(this List<PersistentCall> list, int declarer, string fieldName)
    {
        // Todo: make this have find functionality,
        // so we don't re-lookup FieldInfos

        // first get the declaring type

        int typeBindingFlag = list.FindOrAddJsonDeserialize("60", typeof(BindingFlags));

        return list.AddRunMethod(GetField, declarer, new PersistentArgument(typeof(string)).FSetString(fieldName), typeBindingFlag);
    }
    public static void AddArraySet(this List<PersistentCall> list, int array, int obj, int idx)
    {
        var editorSetCall = new PersistentCall(typeof(UltNoodleRuntimeExtensions).GetMethod("ArrayItemSetter1", UltEventUtils.AnyAccessBindings), null);
        editorSetCall.PersistentArguments[0].ToRetVal(array, typeof(Array));
        editorSetCall.PersistentArguments[1].Int = idx;
        editorSetCall.PersistentArguments[2].ToRetVal(obj, typeof(object));
        list.Add(editorSetCall);

        /*var ingameSetCall = new PersistentCall();
        ingameSetCall.CopyFrom(editorSetCall);
        ingameSetCall.FSetMethodName("System.Linq.Expressions.Interpreter.CallInstruction, System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e.ArrayItemSetter1");
        ingameSetCall.FSetMethod(null);
        list.Add(ingameSetCall);*/
    }
    public static void AddArraySet(this List<PersistentCall> list, int array, PersistentArgument @const, int idx)
    {
        var editorSetCall = new PersistentCall(typeof(UltNoodleRuntimeExtensions).GetMethod("ArrayItemSetter1", UltEventUtils.AnyAccessBindings), null);
        editorSetCall.PersistentArguments[0].ToRetVal(array, typeof(Array));
        editorSetCall.PersistentArguments[1].Int = idx;
        editorSetCall.PersistentArguments[2] = @const;
        list.Add(editorSetCall);

        /*var ingameSetCall = new PersistentCall();
        ingameSetCall.CopyFrom(editorSetCall);
        ingameSetCall.FSetMethodName("System.Linq.Expressions.Interpreter.CallInstruction, System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e.ArrayItemSetter1");
        ingameSetCall.FSetMethod(null);
        list.Add(ingameSetCall);*/
    }
    public const bool DEBUG_IN_COMP = false;
    public static void AddDebugLog(this List<PersistentCall> list, int retVal, bool useJson = false)
    {
        if (!DEBUG_IN_COMP) return;
        if (useJson)
        {
            var jsonSerialize = new PersistentCall(JsonSeserializeMethod, null);
            jsonSerialize.PersistentArguments[0].ToRetVal(retVal, typeof(object));
            list.Add(jsonSerialize);
            var dbg2 = new PersistentCall(typeof(Debug).GetMethod("Log", new Type[] { typeof(object) }), null);
            dbg2.PersistentArguments[0].ToRetVal(list.Count - 1, typeof(object));
            list.Add(dbg2);
            return;
        }

        var dbg = new PersistentCall(typeof(Debug).GetMethod("Log", new Type[] { typeof(object) }), null);
        dbg.PersistentArguments[0].ToRetVal(retVal, typeof(object));
        list.Add(dbg);
    }
    public static void AddDebugLog(this List<PersistentCall> list, string str)
    {
        if (!DEBUG_IN_COMP) return;
        var dbg = new PersistentCall(typeof(Debug).GetMethod("Log", new Type[] { typeof(object) }), null);
        dbg.PersistentArguments[0].FSetType(PersistentArgumentType.String).FSetString(str);
        list.Add(dbg);
    }
    public static MethodInfo MethodInfoInvoke = Type.GetType("System.SecurityUtils, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", true, true).GetMethod("MethodInfoInvoke", UltEventUtils.AnyAccessBindings, null,
                new Type[] { typeof(MethodInfo), typeof(object), typeof(object[]) }, null);
    public static int AddRunMethod(this List<PersistentCall> list, int methodIdx, int objIdx, params object[] @params)
        => list.AddRunDbgMethod(methodIdx, objIdx, false, @params);
    public static int AddRunDbgMethod(this List<PersistentCall> list, int methodIdx, int objIdx, bool debug, object[] @params)
    {
        @params ??= new object[0];
        int paramArr = list.CreateArray(typeof(object), @params.Length, @new: true);
        // setup paramz
        for (int i = 0; i < @params.Length; i++)
        {
            var curParam = @params[i];
            if (curParam == null)
                continue; 
            else if (curParam is int retVal)
                list.AddArraySet(paramArr, retVal, i);
            else if (curParam is PersistentArgument pa)
            {
                // const usually.
                list.AddArraySet(paramArr, pa, i);
            }
        }

        if (debug)
        {
            list.AddDebugLog("Targ MethodInfo:");
            list.AddDebugLog(methodIdx);
            list.AddDebugLog("Filled a Param Array for Run:");
            for (int i = 0; i < @params.Length; i++)
            {
                if (@params[i] == null)
                {
                    list.AddDebugLog("null for " + i);
                }
                else if (@params[i] is int iii)
                {
                    list.AddDebugLog(i + " is retval:");
                    list.AddDebugLog((int)@params[i]);
                }
                else
                {
                    list.AddDebugLog("advanced for " + i);
                }
            }
        }
            

        var invokeCall = new PersistentCall(MethodInfoInvoke, null);
        invokeCall.PersistentArguments[0].ToRetVal(methodIdx, typeof(MethodInfo));
        if (objIdx < 0)
            invokeCall.PersistentArguments[1].FSetString(typeof(object).AssemblyQualifiedName)
                .FSetType(PersistentArgumentType.Object).FSetInt(0);
        else
            invokeCall.PersistentArguments[1].ToRetVal(objIdx, typeof(object));
        invokeCall.PersistentArguments[2].ToRetVal(paramArr, typeof(object[]));
        list.Add(invokeCall);
        return list.Count - 1;
    }
    public static int AddRunMethod(this List<PersistentCall> list, MethodInfo method, int objIdx, params object[] @params)
    {
        int m = list.FindOrAddGetMethodInfo(method);
        return list.AddRunMethod(m, objIdx, @params);
    }
    public static MethodInfo GetFieldValue = typeof(FieldInfo).GetMethod("GetValue");
    public static int AddGetFieldValue(this List<PersistentCall> list, FieldInfo field, object obj)
    {
        int fieldIdx = list.AddGetFieldInfo(field);
        return list.AddRunMethod(GetFieldValue, fieldIdx, obj);
    }
    public static int AddGetFieldValue(this List<PersistentCall> list, int fieldIdx, object obj)
    {
        return list.AddRunMethod(GetFieldValue, fieldIdx, obj);
    }
    public static MethodInfo SetFieldValue = typeof(FieldInfo).GetMethod("SetValue", new Type[] { typeof(object), typeof(object) });
    public static int AddSetFieldValue(this List<PersistentCall> list, FieldInfo field, int objIdx, object value)
    {
        int fieldIdx = list.AddGetFieldInfo(field);
        return list.AddSetFieldValue(fieldIdx, objIdx, value);
    }
    public static int AddSetFieldValue(this List<PersistentCall> list, int field, int objIdx, object value)
    {
        return list.AddRunMethod(SetFieldValue, field, objIdx, value);
    }
    public static int AddCreateInstance<T>(this List<PersistentCall> list)
    {
        int t = list.FindOrAddGetTyper<T>();

        var makeDict = new PersistentCall(typeof(Activator).GetMethod("CreateInstance", new Type[] { typeof(Type) }), null);
        makeDict.PersistentArguments[0].ToRetVal(t, typeof(Type));
        list.Add(makeDict);
        return list.Count - 1;
    }

    public static Dictionary<string, object> TestDict = new Dictionary<string, object>() { { "hi", "hello" } };
}
public static class TypeTranslator
{
    private static Dictionary<Type, string> _defaultDictionary = new Dictionary<System.Type, string>
    {
        {typeof(int), "int"},
        {typeof(uint), "uint"},
        {typeof(long), "long"},
        {typeof(ulong), "ulong"},
        {typeof(short), "short"},
        {typeof(ushort), "ushort"},
        {typeof(byte), "byte"},
        {typeof(sbyte), "sbyte"},
        {typeof(bool), "bool"},
        {typeof(float), "float"},
        {typeof(double), "double"},
        {typeof(decimal), "decimal"},
        {typeof(char), "char"},
        {typeof(string), "string"},
        {typeof(object), "object"},
        {typeof(void), "void"}
    };
    public static Dictionary<string, Type> SimpleNames2Types = _defaultDictionary.ToDictionary(x => x.Value, x => x.Key);

    public static string GetFriendlyName(this Type type, Dictionary<Type, string> translations)
    {
        if (translations.ContainsKey(type))
            return translations[type];
        else if (type.IsArray)
        {
            var rank = type.GetArrayRank();
            var commas = rank > 1
                ? new string(',', rank - 1)
                : "";
            return GetFriendlyName(type.GetElementType(), translations) + $"[{commas}]";
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            return type.GetGenericArguments()[0].GetFriendlyName() + "?";
        else if (type.IsGenericType)
            return type.Name.Split('`')[0] + "<" + string.Join(", ", type.GetGenericArguments().Select(x => GetFriendlyName(x)).ToArray()) + ">";
        else
            return type.Name;
    }

    public static string GetFriendlyName(this Type type)
    {
        return type.GetFriendlyName(_defaultDictionary); 
    }
}
#endif