using NoodledEvents;
using System;
using System.Collections.Generic;
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
    public static PersistentArgumentType FGetType(this PersistentArgument arg)
        => (PersistentArgumentType)s_PersistentArgumentTypeGetSet.GetValue(arg);
    public static PersistentArgument FSetString(this PersistentArgument arg, string s)
    { s_PersistentArgumentStringGetSet.SetValue(arg, s); return arg; }
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
    public static SerializedNode MatchDef(this SerializedNode nod, CookBook.NodeDef def)
    {
        foreach (var @in in def.Inputs)
        {
            if (@in.Flow) nod.AddFlowIn(@in.Name);
            else nod.AddDataIn(@in.Name, @in.Type);
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
