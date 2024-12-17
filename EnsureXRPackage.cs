using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;

namespace NoodledEvents
{
    [InitializeOnLoad]
    static class EnsureXRPackage
    {
        static EnsureXRPackage() 
        {
            Client.Add("https://github.com/holadivinus/BLXRComp.git");
        }
    }
}
