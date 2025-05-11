#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UltEvents;
using UnityEngine;

namespace NoodledEvents
{
    public static class UltNoodleExtensions
    {
        public static SerializedBowl GetBowlData(this Component holder, SerializedType fieldType, string eventFieldPath)
        {
            foreach (var bowlData in holder.GetComponents<SerializedBowl>())
                if (bowlData.EventHolder == holder && bowlData.EventFieldPath == eventFieldPath)
                    return bowlData;
            
            // needs bowl
            return SerializedBowl.Create(holder, fieldType, eventFieldPath);
        }
        /*private static MethodInfo GetTyper = typeof(Type).GetMethod("GetType", new[] { typeof(string), typeof(bool), typeof(bool) });
        public static int FindOrAddTypePCall(this List<PersistentCall> calls)
        {
            var existing = calls.FirstOrDefault(c => c.Method == GetTyper && c.PersistentArguments[0].Type == PersistentArgumentType.String 
            && c.PersistentArguments[0].String );
            if (existing != nu)
        }*/

        public static Type[] GetEvtGenerics(this Type evtType)
        {
            if (!typeof(UltEventBase).IsAssignableFrom(evtType)) return new Type[0];
            List<Type> types = new List<Type>();
            while(evtType != typeof(UltEventBase))
            {
                //foreach (var item in evtType.GetGenericArguments())
                //{
                //    Debug.Log(item);
                //}
                types.InsertRange(0, evtType.GetGenericArguments());
                evtType = evtType.BaseType;
            }
            return types.ToArray();
        }

        public static PersistentArgumentType GetPCallType(this NoodleDataInput @in)
        {
            if (@in.Type == typeof(object)) 
                return @in.ConstInput;
            else
            {
                if (@in.Type == typeof(bool)) return PersistentArgumentType.Bool;
                if (@in.Type == typeof(int)) return PersistentArgumentType.Int;
                if (@in.Type.Type.IsEnum) return PersistentArgumentType.Enum;
                if (@in.Type == typeof(string)) return PersistentArgumentType.String;
                if (@in.Type == typeof(float)) return PersistentArgumentType.Float;
                if (@in.Type == typeof(Vector2)) return PersistentArgumentType.Vector2;
                if (@in.Type == typeof(Vector3)) return PersistentArgumentType.Vector3;
                if (@in.Type == typeof(Vector4)) return PersistentArgumentType.Vector4;
                if (@in.Type == typeof(Quaternion)) return PersistentArgumentType.Quaternion;
                if (@in.Type == typeof(Color)) return PersistentArgumentType.Color;
                if (@in.Type == typeof(Color32)) return PersistentArgumentType.Color32;
                if (@in.Type == typeof(Rect)) return PersistentArgumentType.Rect;
                if (typeof(UnityEngine.Object).IsAssignableFrom(@in.Type)) return PersistentArgumentType.Object;
                return PersistentArgumentType.None; // uhh
            }
        }

        public static Type[] GetAllTypes()
        {
            /*foreach (var assembl in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembl.FullName.StartsWith("UnityEditor"))
                    Debug.Log(assembl.FullName);
            }*/
            /*foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type t in ass.GetTypes())
                {
                    if (t.IsSubclassOf(typeof(Component)))
                    {
                        try
                        {
                            foreach (var prop in t.GetProperties())
                            {
                                if (prop.PropertyType == typeof(object))
                                {
                                    Log(t.Name + "." + prop.Name);
                                }
                            }
                        }
                        catch (TypeLoadException) { }
                    }
                }
            }*/
            
            //EditorUtility.DisplayProgressBar("Finding Types...", "", 0);
            var assembs = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.FullName.StartsWith("UnityEditor"));
            float n = (float)assembs.Count();
            int i = 0;
            var types = assembs
                .SelectMany(assembly =>
                {
                    //EditorUtility.DisplayProgressBar("Finding Types...", assembly.FullName, (++i)/n);
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        // Return the types that were successfully loaded
                        return ex.Types.Where(t => t != null);
                    }
                    catch (Exception)
                    {
                        // If we can't load types from this assembly, return empty
                        return Enumerable.Empty<Type>();
                    }
                });
            return types.ToArray();
        }
    }
}
#endif