using UnityEngine;

public class TraitDebugPanel : MonoBehaviour
{
    private PlayerTraitController traitController;

    private void Awake()
    {
        traitController = GetComponent<PlayerTraitController>();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void OnGUI()
    {
        if (traitController == null)
            return;

        GUI.Label(new Rect(12f, 12f, 900f, 24f), traitController.GetSummary());
    }
#endif
}
