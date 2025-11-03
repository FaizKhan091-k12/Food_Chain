using UnityEngine;

public class GamePlay : MonoBehaviour
{
    [Header("Draggables (assign the GameObjects with DraggableUI)")]
    [SerializeField] Transform one_Drag, two_Drag, three_Drag;

    [Header("Optional snap targets (assign RectTransforms where you want each draggable to snap)")]
    [SerializeField] RectTransform one_SnapTarget, two_SnapTarget, three_SnapTarget;

    DraggableUI d1, d2, d3;

    void Start()
    {
        // Try to get DraggableUI on each transform
        if (one_Drag != null) { d1 = one_Drag.GetComponent<DraggableUI>(); if (d1 == null) d1 = one_Drag.gameObject.AddComponent<DraggableUI>(); }
        if (two_Drag != null) { d2 = two_Drag.GetComponent<DraggableUI>(); if (d2 == null) d2 = two_Drag.gameObject.AddComponent<DraggableUI>(); }
        if (three_Drag != null) { d3 = three_Drag.GetComponent<DraggableUI>(); if (d3 == null) d3 = three_Drag.gameObject.AddComponent<DraggableUI>(); }

        // Assign snap targets if provided
        if (d1 != null) d1.SetSnapTarget(one_SnapTarget);
        if (d2 != null) d2.SetSnapTarget(two_SnapTarget);
        if (d3 != null) d3.SetSnapTarget(three_SnapTarget);
    }

    // Example: programmatically set a snap target at runtime (e.g. when showing a new panel)
    public void SetSnapTargetForDraggable(int index, RectTransform target)
    {
        switch (index)
        {
            case 0: if (d1 != null) d1.SetSnapTarget(target); break;
            case 1: if (d2 != null) d2.SetSnapTarget(target); break;
            case 2: if (d3 != null) d3.SetSnapTarget(target); break;
        }
    }

    // Example: reset all draggables
    public void ResetAllDraggables()
    {
        if (d1 != null) d1.ResetToStart();
        if (d2 != null) d2.ResetToStart();
        if (d3 != null) d3.ResetToStart();
    }
}
