using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UltEvents;
using UnityEngine;

namespace NoodledEvents
{
    [ExecuteAlways]
    public class LifeCycleEvtEditorRunner : MonoBehaviour
    {
        private LifeCycleEvents e;
        public LifeCycleEvents Evts => e ??= GetComponent<LifeCycleEvents>();
        private void OnEnable()
        {
            if (UltNoodleBowlUI.EvtIsExecRn)
                Evts.OnEnable();
        }
        private void OnDisable()
        {
            if (UltNoodleBowlUI.EvtIsExecRn)
                Evts.OnDisable();
        }
    }
}
