using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.XR.Interaction.Toolkit.AffordanceSystem.State
{
    public class XRInteractorAffordanceStateProvider : MonoBehaviour
    {
        [SerializeField] Object m_InteractorSource;

        public Object interactorSource
        {
            get => m_InteractorSource;
            set => m_InteractorSource = value;
        }
        [SerializeField] bool m_IgnoreHoverEvents;
    }
}
