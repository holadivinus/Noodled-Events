using UnityEngine;

namespace NoodledEvents.Assets.Noodled_Events
{
    public class VarMan : MonoBehaviour
    {
#if UNITY_EDITOR
        public NoodleDataInput[] Vars = new NoodleDataInput[0];
#endif
    }
}
