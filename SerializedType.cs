#if UNITY_EDITOR
using System;
using UnityEngine;

namespace NoodledEvents
{
    [Serializable]
    public class SerializedType
    {
        public SerializedType(Type type) => Type = type;
        public Type Type 
        {
            get => t ??= Type.GetType(_assemblyTypeName, true, true);
            set
            {
                t = value;
                _assemblyTypeName = value.AssemblyQualifiedName;
            }
        }
        private Type t;
        [SerializeField] string _assemblyTypeName;

        public static implicit operator Type(SerializedType st) => st.Type;
        public static implicit operator SerializedType(Type t) => new SerializedType(t);
    }
}
#endif