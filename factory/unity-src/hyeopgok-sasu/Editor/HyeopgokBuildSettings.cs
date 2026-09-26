using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Mgf.HyeopgokSasu.Editor
{
    // Game-local workaround: a code-bootstrap scene contains no serialized instanced
    // renderers, so Unity's StripUnused removes our required INSTANCING_ON variants.
    // The shared kit remains unchanged. Restore the previous setting after the build.
    public sealed class HyeopgokBuildSettings : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        static int before;
        public int callbackOrder=>-100;
        public void OnPreprocessBuild(BuildReport report){
            var obj=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var prop=obj.FindProperty("m_InstancingStripping");
            if(prop==null)throw new Exception("Cannot preserve required horde instancing variants");
            before=prop.intValue;
            // Unity InstancingStrippingMode: StripUnused=0, StripAll=1, KeepAll=2.
            prop.intValue=2;obj.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();
            Debug.Log("[HYEOPGOK] Instancing variants kept for runtime horde renderer");
        }
        public void OnPostprocessBuild(BuildReport report){
            var obj=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            obj.FindProperty("m_InstancingStripping").intValue=before;obj.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();
        }
    }
}
