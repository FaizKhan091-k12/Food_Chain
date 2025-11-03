using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using Unity.VisualScripting;
using System.Net.NetworkInformation;

[RequireComponent(typeof(RectTransform))]
public class DraggableUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    [Header("Drag settings")]
    public bool clampToCanvas = true;
    public float returnDuration = 0.15f; // if no snap, how fast to move back
    public bool allowSnapOnRelease = true;

    // NEW: if true, when returning to origin it will set localPosition = (0,0,0)
    public bool returnToOriginOnRelease = true;

    RectTransform rect;
    Canvas canvas;
    CanvasGroup canvasGroup;
    Vector2 startAnchoredPos;
    Transform originalParent;

    // Optional snap target set by manager (GamePlay)
    public RectTransform snapTarget;
    public bool canDrag;


    void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null) Debug.LogWarning("DraggableUI needs to be inside a Canvas.", this);

        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void Start()
    {
        originalParent = rect.parent;
        startAnchoredPos = rect.anchoredPosition;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        rect.SetAsLastSibling();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!canDrag) return;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.95f;
        // reparent to canvas root so it draws above everything (optional)
        if (canvas != null)
            rect.SetParent(canvas.transform, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!canDrag) return;
        if (canvas == null) return;

        // Convert screen point to local point in the parent RectTransform
        RectTransform parentRect = rect.parent as RectTransform;
        Vector2 anchored;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out anchored))
        {
            rect.anchoredPosition = anchored;
        }

        if (clampToCanvas)
        {
            KeepInsideParent(rect);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!canDrag) return;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // Decide whether we should snap (distance threshold)
        bool willSnap = false;
        float snapDistance = 120f; // pixels, tweak as needed

        if (allowSnapOnRelease && snapTarget != null)
        {
            Vector2 currentScreen = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, rect.position);
            Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, snapTarget.position);
            float dist = Vector2.Distance(currentScreen, targetScreen);
            if (dist <= snapDistance) willSnap = true;


                
        }

        if (willSnap)
        {
            // Animate visually to the snapTarget position while still parented to canvas,
            // then reparent into snapTarget and zero the anchored/local position.
            StartCoroutine(SnapThenParent(snapTarget, 0.12f));
            canDrag = false;
            UIHoverClickEffect uIHoverClickEffect = gameObject.GetComponent<UIHoverClickEffect>();
            if (uIHoverClickEffect != null)
            {
                uIHoverClickEffect.enabled = false;
            }
        }
        else
        {
            // Not snapping: reparent back to original parent (so anchored coords match startAnchoredPos)
            if (originalParent != null && rect.parent != originalParent)
            {
                // Reparent first — this ensures local space matches the original coordinate system
                rect.SetParent(originalParent, false);
               rect.anchoredPosition = Vector3.zero;

                // // Reset anchored and local position cleanly in the original parent's space
                // // rect.anchoredPosition = startAnchoredPos;
                // rect.localPosition = Vector3.zero;
            }


            // Animate back to stored start anchored pos (in original parent's space)
           // ResetToStart(true);

           
        }

        // Note: do not call SetParent(originalParent) here for the snapping case;
        // SnapThenParent will reparent after animation finishes.
    }
    // Replace your existing SnapThenParent with this version
    IEnumerator SnapThenParent(RectTransform targetSlot, float dur)
    {
        if (targetSlot == null)
        {
            if (originalParent != null && rect.parent != originalParent)
                rect.SetParent(originalParent, true);
            ResetToStart(true);
            yield break;
        }

        // 1) Compute the anchored position of the target in the current parent (canvas) space
        RectTransform currentParentRect = rect.parent as RectTransform;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, targetSlot.position);
        Vector2 canvasAnchored;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(currentParentRect, screenPoint, canvas.worldCamera, out canvasAnchored);

        // 2) Animate to that anchored position while staying under Canvas parent
        yield return MoveToAnchoredPositionCo(rect, canvasAnchored, dur);

        // 3) Reparent into the target slot (false = keep the visual position initially)
        rect.SetParent(targetSlot, false);

        // 4) Now fix the child's transform so it centers and matches slot size:
        //    - set anchors to center, pivot to center
        //    - reset scale to 1
        //    - optionally match the slot's size (or set a desired sizeDelta)
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;

        // Option A (recommended): snap to center and make the draggable fill the slot
        rect.anchoredPosition = Vector2.zero;

        // If you want the draggable to match the slot's size exactly, use:
        // rect.sizeDelta = targetSlot.rect.size; // copy slot size in pixels
        // Or use a specific padding:
        // rect.sizeDelta = targetSlot.rect.size * 0.9f; // slightly smaller than slot

        // 5) Final safety: snap localPosition to zero to avoid sub-pixel drift
        rect.anchoredPosition = Vector3.zero;

        yield break;
    }


IEnumerator ReturnToOriginAfterReparent(float dur)
{
    // ensure parent is original
    if (originalParent != null && rect.parent != originalParent)
        rect.SetParent(originalParent, true);

    // animate to startAnchoredPos in original parent's space
    yield return MoveToAnchoredPositionCo(rect, startAnchoredPos, dur);

    // final snap
    if (returnToOriginOnRelease)
        rect.localPosition = Vector3.zero;
}



    IEnumerator SnapThenCorrect()
    {
        // Run your existing animation coroutine
        yield return MoveToAnchoredPosition(rect, GetAnchoredPositionForTarget(snapTarget), 0.12f);

        // 👇 After it finishes, manually place it exactly on the target
        rect.position = snapTarget.position;
        rect.anchoredPosition = GetAnchoredPositionForTarget(snapTarget);
    }


    Vector2 GetAnchoredPositionForTarget(RectTransform target)
    {
        RectTransform parentRect = rect.parent as RectTransform;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, target.position);
        Vector2 anchored;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, canvas.worldCamera, out anchored);
        return anchored;
    }

    IEnumerator MoveToAnchoredPosition(RectTransform r, Vector2 targetAnchored, float dur)
    {
        Vector2 from = r.position;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            r.anchoredPosition = Vector2.Lerp(from, Vector3.zero, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur)));
            yield return null;
        }
        r.anchoredPosition = Vector3.zero;

        // Guarantee exact localPosition if requested and target is the origin
        if (returnToOriginOnRelease && targetAnchored == startAnchoredPos)
        {
            // Force exact localPosition zero to avoid tiny offsets
            r.localPosition = Vector3.zero;
        }
    }

    void KeepInsideParent(RectTransform r)
    {
        RectTransform parentRect = r.parent as RectTransform;
        if (parentRect == null) return;

        Vector3[] parentCorners = new Vector3[4];
        parentRect.GetWorldCorners(parentCorners);

        Vector3[] rectCorners = new Vector3[4];
        r.GetWorldCorners(rectCorners);

        Vector3 offset = Vector3.zero;

        if (rectCorners[0].x < parentCorners[0].x) offset.x = parentCorners[0].x - rectCorners[0].x;
        if (rectCorners[2].x > parentCorners[2].x) offset.x = parentCorners[2].x - rectCorners[2].x;
        if (rectCorners[0].y < parentCorners[0].y) offset.y = parentCorners[0].y - rectCorners[0].y;
        if (rectCorners[2].y > parentCorners[2].y) offset.y = parentCorners[2].y - rectCorners[2].y;

        if (offset != Vector3.zero)
        {
            r.position += offset;
        }
    }

    public void SetSnapTarget(RectTransform target)
    {
        snapTarget = target;
    }

    /// <summary>
    /// Reset this draggable to its original position.
    /// If animate == true it will animate and then force localPosition = Vector3.zero (if returnToOriginOnRelease is true).
    /// </summary>
    public void ResetToStart(bool animate = true)
    {
        if (!animate)
        {
            rect.anchoredPosition = startAnchoredPos;
            if (returnToOriginOnRelease)
            {
                rect.localPosition = Vector3.zero;
               
            }
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(ResetCoroutine());
        }
    }

    IEnumerator ResetCoroutine()
    {
        // Animate to startAnchoredPos
        yield return MoveToAnchoredPositionCo(rect, startAnchoredPos, returnDuration);

        // After animation, force exact zero localPosition if requested
        if (returnToOriginOnRelease)
            rect.anchoredPosition = Vector3.zero;
    }

    // Helper coroutine wrapper so MoveToAnchoredPosition (which returns IEnumerator) can be used inside another coroutine
    IEnumerator MoveToAnchoredPositionCo(RectTransform r, Vector2 targetAnchored, float dur)
    {
        Vector2 from = r.anchoredPosition;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            r.anchoredPosition = Vector2.Lerp(from, targetAnchored, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur)));
            yield return null;
        }
        r.anchoredPosition = Vector3.zero; 
    }
   
}
