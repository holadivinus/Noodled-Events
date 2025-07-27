using UnityEngine;

namespace NoodledEvents.Assets.Noodled_Events
{
    public class VarMan : MonoBehaviour
    {
#if UNITY_EDITOR
        public string CustomTitle = "epic sigma Var Man";
        public Color VarsBGColor = new Color(1, 0.7960784f, 0, 0.0627451f);
        public Color TextFillColor = new Color(0.8235294f, 0.8235294f, 0.8235294f);
        public Color TextOutlineColor = Color.black;
        public float TextFontSize = 20;
        public bool AutoEnforce;
        public bool HideBowls;
        public bool TurnOnHideBowls = true;
        public NoodleDataInput[] Vars = new NoodleDataInput[0];
#endif
    }
}
