#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UltEvents;
using UnityEditor;
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
            
            EditorUtility.DisplayProgressBar("Finding Types...", "", 0);
            var assembs = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.FullName.StartsWith("UnityEditor"));
            float n = (float)assembs.Count();
            int i = 0;
            var types = assembs
                .SelectMany(assembly =>
                {
                    EditorUtility.DisplayProgressBar("Finding Types...", assembly.FullName, (++i)/n);
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