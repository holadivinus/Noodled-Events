using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UltEvents;
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
