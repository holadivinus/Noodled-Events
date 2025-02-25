#if UNITY_EDITOR
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace NoodledEvents
{
    //[InitializeOnLoad]
    static class EnsureXRPackage
    {
        static EnsureXRPackage() 
        {
            Client.Add("https://github.com/holadivinus/BLXRComp.git");


            // also fix up MathUtilities.cs
           
            string[] existingMaths = AssetDatabase.FindAssets("MathUtilities t:MonoScript").Select(g => AssetDatabase.GUIDToAssetPath(g)).Where(p => !p.Contains("com.stresslevelzero.marrow.sdk.extended")).ToArray();
            if (existingMaths.Length > 0)
            { 
                string existingMathPath = Application.dataPath + existingMaths[0].Substring(6);
                if (File.ReadAllText(existingMathPath) != fixedMathCS)
                    File.WriteAllText(existingMathPath, fixedMathCS);
            } else
            {
                string scripPath = Application.dataPath + "\\Marrow-ExtendedSDK-MAINTAINED-main\\Scripts\\Assembly-CSharp\\SLZ\\Bonelab\\VoidLogic\\";
                if (!Directory.Exists(scripPath)) 
                    Directory.CreateDirectory(scripPath);
                scripPath += "MathUtilities.cs";
                File.WriteAllText(scripPath, fixedMathCS);
            }
        }
        const string fixedMathCS = "using System.Runtime.CompilerServices;\r\n\r\nnamespace SLZ.Bonelab.VoidLogic\r\n{\r\n\tinternal static class MathUtilities\r\n\t{\r\n\t\t[MethodImpl(256)]\r\n\t\tpublic static bool IsApproximatelyEqualToOrGreaterThan(this float num1, float num2)\r\n\t\t{\r\n\t\t\treturn num1 >= num2;\r\n\t\t}\r\n\r\n\t\t[MethodImpl(256)]\r\n\t\tpublic static bool IsApproximatelyEqualToOrLessThan(this float num1, float num2)\r\n\t\t{\r\n\t\t\treturn num1 <= num2;\r\n\t\t}\r\n\t}\r\n}\r\n";
    }
}
#endif
