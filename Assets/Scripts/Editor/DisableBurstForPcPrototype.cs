#if UNITY_EDITOR
using Unity.Burst;
using UnityEditor;

[InitializeOnLoad]
public static class DisableBurstForPcPrototype
{
    static DisableBurstForPcPrototype()
    {
        EditorPrefs.SetBool("BurstCompilation", false);
        BurstCompiler.Options.EnableBurstCompilation = false;
    }
}
#endif
