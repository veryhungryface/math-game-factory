// Diagnostic native bridge for Unity's single-threaded WebGL IL2CPP collector.
// GC_get_total_bytes is supplied by Unity's bundled Boehm GC (gc.h).
// Unlike GC.GetAllocatedBytesForCurrentThread, this counter is implemented on WebGL.
// It includes cumulative allocations, even objects already collected.
#include <stddef.h>
extern "C" size_t GC_get_total_bytes(void);
extern "C" double HYEOPGOK_TotalAllocatedBytes(void) {
    return static_cast<double>(GC_get_total_bytes());
}
