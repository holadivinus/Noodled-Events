#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using System.Text;
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

        public static string GetBookTag(MethodInfo method)
        {
            StringBuilder sb = new();
            // method name and type
            sb.Append($"{{\"_assemblyMethodName\":\"{UltEventUtils.GetFullyQualifiedName(method)}\",\"Parameters\":");

            // build parameters
            sb.Append("[");
            var parameters = method.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                sb.Append($"{{\"_assemblyTypeName\":\"{parameters[i].ParameterType.AssemblyQualifiedName}\"}}");
                if (i < parameters.Length - 1) sb.Append(",");
            }
            sb.Append("]");

            // done
            sb.Append("}");

            return sb.ToString();
        }
    }
}
#endif