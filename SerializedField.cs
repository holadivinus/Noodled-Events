#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UltEvents;
using UnityEngine;

namespace NoodledEvents
{
    [Serializable]
    public class SerializedField
    {
        public FieldInfo Field 
        {
            get => f ??= Type.GetType(_assemblyTypeName).GetField(_fieldName, UltEventUtils.AnyAccessBindings);
            set
            {
                f = value;
                _assemblyTypeName = f.DeclaringType.AssemblyQualifiedName;
                _fieldName = f.Name;
            }
        }
        private FieldInfo f;
        [SerializeField] string _assemblyTypeName;
        [SerializeField] string _fieldName;
    }
}
#endif