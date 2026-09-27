using System;
using System.Runtime.InteropServices;
using UnityEngine;
namespace Mgf.HyeopgokSasu {
    // Opt-in measurement only (?artprobe=1). Does not change the test bridge,
    // state, RNG, timing, input or rendering. Counts are per scoped game callback.
    // The unchanged engine/bridge callbacks are outside these scopes.
    static class HyeopgokArtProbe {
        static readonly bool enabled=Application.absoluteURL.Contains("artprobe=1");
        static byte[] calibration;
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void HYEOPGOK_ArtAllocation(int scope,double bytes);
        [DllImport("__Internal")] static extern double HYEOPGOK_TotalAllocatedBytes();
#else
        static void HYEOPGOK_ArtAllocation(int scope,double bytes) { }
        static double HYEOPGOK_TotalAllocatedBytes()=>GC.GetAllocatedBytesForCurrentThread();
#endif
        public static long Begin(){
            if(!enabled)return 0;
            if(calibration==null){
                double before=HYEOPGOK_TotalAllocatedBytes();
                calibration=new byte[257];
                calibration[0]=1;
                HYEOPGOK_ArtAllocation(3,HYEOPGOK_TotalAllocatedBytes()-before);
            }
            return (long)HYEOPGOK_TotalAllocatedBytes();
        }
        public static void End(int scope,long before) {
            if(enabled)HYEOPGOK_ArtAllocation(scope,HYEOPGOK_TotalAllocatedBytes()-before);
        }
    }
}
