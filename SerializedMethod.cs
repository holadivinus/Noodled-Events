#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UltEvents;
using UnityEngine;

namespace NoodledEvents
{
    [Serializable]
    public class SerializedMethod
    {
        public MethodBase Method 
        {
            get => m ??= _assemblyMethodName.ToMethod(Parameters.Select(p => p.Type).ToArray());
            set
            {
                m = value;
                _assemblyMethodName = UltEventUtils.GetFullyQualifiedName(value);
                Parameters = value.GetParameters().Select(p => new SerializedType(p.ParameterType)).ToArray();
            }
        }
        public MethodBase RawMethod => m;
        
        private MethodBase m;
        [SerializeField] string _assemblyMethodName;
        [SerializeField] public SerializedType[] Parameters;
    }
}
#endif