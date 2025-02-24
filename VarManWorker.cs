using System.Collections;
using System.Collections.Generic;
using UltEvents;
using UltEvents.Editor;
using UnityEditor;
using UnityEngine;

namespace NoodledEvents
{
    public class VarManWorker : MonoBehaviour
    {
        public UnityEngine.Object Target;
        public string Path;
        public string VarName;
        public PersistentArgumentType Type;
        public void Apply(object value)
        {
            new SerializedObject(Target).FindProperty(Path).SetValue(value);
        }
    }
}
