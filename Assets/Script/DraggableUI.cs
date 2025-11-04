using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.UI.ProceduralImage;
using JetBrains.Annotations;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class DraggableUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    public GamePlay gameplay;
    
    [Header("Shake Settings")]
    [Tooltip("Optional panel to shake when dropped on a wrong slot.")]
    public RectTransform shakeTargetPanel;
    [Header("Drag settings")]
    public bool clampToCanvas = true;
    public float returnDuration = 0.15f;
    public bool allowSnapOnRelease = true;
    public bool returnToOriginOnRelease = true;

    RectTransform rect;
    Canvas canvas;
    CanvasGroup canvasGroup;
    Vector2 startAnchoredPos;
    Transform originalParent;

    [Header("Snap Settings")]
    [Tooltip("Single snap target used for green (correct) feedback and snapping.")]
    public RectTransform correctSnapTarget;

    [Tooltip("Multiple wrong targets that should blink red if the draggable is released near any of them.")]
    public List<RectTransform> wrongSnapTargets = new List<RectTransform>();

    [Tooltip("Whether this draggable can be dragged.")]
    public bool canDrag;

    [Header("Proximity / Feedback")]
    [Tooltip("How close (in screen pixels) the draggable must be to a slot to be considered 'near'.")]
    public float snapDistance = 120f;

    [Tooltip("How long red/green feedback should last.")]
    public float feedbackDuration = 0.4f;
    [Tooltip("Red color shown when dropped on a wrong slot.")]
    public Color wrongTint = new Color(1f, 0.3f, 0.3f, 0.8f);
    [Tooltip("Green color shown when dropped on the correct slot.")]
    public Color correctTint = new Color(0.4f, 1f, 0.4f, 0.8f);
    public GameObject dialogueBox;

    public bool one, two, three;
    public AudioClip audioClip;

    // internal cache for ongoing blink coroutines
    private Dictionary<ProceduralImage, Coroutine> feedbackCoroutines = new Dictionary<ProceduralImage, Coroutine>();

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            Debug.LogWarning("DraggableUI needs to be inside a Canvas.", this);

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void Start()
    {
        originalParent = rect.parent;
        startAnchoredPos = rect.anchoredPosition;
    }

    void OnEnable()
    {
        // Reset draggable state
        canDrag = true;

        // Reset position and parent to original setup
        if (rect != null)
        {
            // if we’ve stored originalParent and startAnchoredPos already
            if (originalParent != null)
                rect.SetParent(originalParent, false);

            rect.anchoredPosition = startAnchoredPos;
            //rect.localPosition = Vector3.zero;
            rect.localScale = Vector3.one;
        }

        // Reset visual transparency and raycast blocking
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        // Reset hover effect scale
        UIHoverClickEffect hover = GetComponent<UIHoverClickEffect>();
        if (hover != null)
        {
            hover.hoverScale = 1.08f; // ✅ Set this to your initial scale
        }

        // Reset flags in gameplay (if you want to re-try fresh round)
        if (gameplay != null)
        {
            if (one) gameplay.one = false;
            if (two) gameplay.two = false;
            if (three) gameplay.three = false;
        }

        // Hide dialogue box at start
        if (dialogueBox != null)
            dialogueBox.SetActive(false);

      
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

        // temporarily move to canvas root so it draws above everything
        if (canvas != null)
            rect.SetParent(canvas.transform, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!canDrag || canvas == null) return;

        RectTransform parentRect = rect.parent as RectTransform;
        Vector2 anchored;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, eventData.position, eventData.pressEventCamera, out anchored))
        {
            rect.anchoredPosition = anchored;
        }

        if (clampToCanvas)
            KeepInsideParent(rect);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!canDrag) return;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // Find nearest target among correct and wrong lists
        RectTransform nearest = null;
        bool nearestIsCorrect = false;
        float bestDist = float.MaxValue;

        Vector2 currentScreen = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, rect.position);

        // Check correct slot
        if (correctSnapTarget != null)
        {
            Vector2 correctScreen = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, correctSnapTarget.position);
            float d = Vector2.Distance(currentScreen, correctScreen);
            if (d <= snapDistance && d < bestDist)
            {
                nearest = correctSnapTarget;
                nearestIsCorrect = true;
                bestDist = d;
                if (one)
                {

                    gameplay.one = true;
                }
                else if (two)
                {

                    gameplay.two = true;
                }
                else if (three)
                {

                    gameplay.three = true;
                }
            }
        }

        // Check wrong slots
        if (wrongSnapTargets != null)
        {
            foreach (var w in wrongSnapTargets)
            {
                if (w == null) continue;
                Vector2 wScreen = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, w.position);
                float d = Vector2.Distance(currentScreen, wScreen);
                if (d <= snapDistance && d < bestDist)
                {
                    nearest = w;
                    nearestIsCorrect = false;
                    bestDist = d;
                }
            }
        }

        // Now act based on nearest found
        if (nearest != null)
        {
            // Show only that single slot feedback
            ShowSnapFeedback(nearest, nearestIsCorrect);

            if (nearestIsCorrect)
            {
                // Snap into correct target
                StartCoroutine(SnapThenParent(nearest, 0.12f));
                canDrag = false;

                UIHoverClickEffect hover = GetComponent<UIHoverClickEffect>();
                if (hover != null) hover.hoverScale = 1.2f;
            }
            else
            {
                // Wrong slot — blink red then return to origin
                StartCoroutine(ReturnToOriginAfterReparent(returnDuration));
            }
        }
        else
        {
            // Not near any slot — return without any blink
            if (originalParent != null && rect.parent != originalParent)
            {
                rect.SetParent(originalParent, false);
                rect.anchoredPosition = startAnchoredPos;
            }
            ResetToStart(true);
        }
    }

    // 🔸 Show feedback (blink) only on that single slot
    void ShowSnapFeedback(RectTransform slot, bool isCorrect)
    {
        if (slot == null) return;

        var proc = slot.GetComponent<ProceduralImage>();
        if (proc == null)
            proc = slot.GetComponentInChildren<ProceduralImage>();

        if (proc == null) return;

        // cancel existing blink if active
        if (feedbackCoroutines.TryGetValue(proc, out Coroutine running))
        {
            if (running != null) StopCoroutine(running);
            feedbackCoroutines.Remove(proc);
        }

        // Start new blink coroutine using crossfade which is robust for UI graphics
        Coroutine c = StartCoroutine(BlinkColorCrossFade(proc, isCorrect ? correctTint : wrongTint));
        feedbackCoroutines[proc] = c;


        // 💥 Add this line — shake canvas only when wrong
        if (!isCorrect)
            StartCoroutine(ShakeCanvas(0.25f, .1f));
    }

    IEnumerator BlinkColorCrossFade(ProceduralImage img, Color blinkColor)
    {
        if (img == null) yield break;

        Color original = img.color;
        float halfDur = feedbackDuration * 0.5f;

        // Instantly crossfade to blinkColor (duration 0 so it's immediate)
        img.CrossFadeColor(blinkColor, 0f, true, true);

        // Keep it for half the duration, then fade back during the remaining half
        yield return new WaitForSecondsRealtime(halfDur);

        // Smoothly return to original color over halfDur
        img.CrossFadeColor(original, halfDur, true, true);
        yield return new WaitForSecondsRealtime(halfDur);

        // Ensure exact original value at the end
        img.color = original;

        if (feedbackCoroutines.ContainsKey(img))
            feedbackCoroutines.Remove(img);
    }

    // --- SNAP + RESET LOGIC (unchanged) ---
    IEnumerator SnapThenParent(RectTransform targetSlot, float dur)
    {
        if (targetSlot == null)
        {
            if (originalParent != null && rect.parent != originalParent)
                rect.SetParent(originalParent, true);
            ResetToStart(true);
            yield break;
        }

        RectTransform currentParentRect = rect.parent as RectTransform;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, targetSlot.position);
        Vector2 canvasAnchored;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(currentParentRect, screenPoint, canvas.worldCamera, out canvasAnchored);

        yield return MoveToAnchoredPositionCo(rect, canvasAnchored, dur);

        rect.SetParent(targetSlot, false);
        rect.pivot = rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.anchoredPosition = Vector2.zero;
        rect.localPosition = Vector3.zero;
    }

    IEnumerator ReturnToOriginAfterReparent(float dur)
    {
        if (originalParent != null && rect.parent != originalParent)
            rect.SetParent(originalParent, true);

        yield return MoveToAnchoredPositionCo(rect, startAnchoredPos, dur);

        if (returnToOriginOnRelease)
            rect.anchoredPosition = Vector3.zero;
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

        if (returnToOriginOnRelease && targetAnchored == startAnchoredPos)
            r.localPosition = Vector3.zero;
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
            r.position += offset;
    }

    public void SetSnapTarget(RectTransform target)
    {
        // kept for backwards compatibility if you were using this previously
        // (it sets the single snapTarget - not used in the new multiple-wrong-target flow)
        // If you want to still use snapTarget, set correctSnapTarget or wrongSnapTargets accordingly.
        correctSnapTarget = target;
    }

    public void ResetToStart(bool animate = true)
    {
        if (!animate)
        {
            rect.anchoredPosition = startAnchoredPos;
            if (returnToOriginOnRelease)
                rect.localPosition = Vector3.zero;
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(ResetCoroutine());
        }
    }

    IEnumerator ResetCoroutine()
    {
        yield return MoveToAnchoredPositionCo(rect, startAnchoredPos, returnDuration);
        if (returnToOriginOnRelease)
            rect.anchoredPosition = Vector3.zero;
    }

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
        r.anchoredPosition = targetAnchored;
    }
    IEnumerator ShakeCanvas(float duration = 0.25f, float strength = 20f)
    {
        RectTransform targetRect = shakeTargetPanel != null ? shakeTargetPanel : canvas?.GetComponent<RectTransform>();
        if (targetRect == null)
        {
            Debug.LogWarning("ShakeCanvas: No valid RectTransform to shake (assign shakeTargetPanel in inspector).");
            yield break;
        }

        Debug.Log("ShakeCanvas started on " + targetRect.name);

        Vector2 originalAnchored = targetRect.anchoredPosition;
        Vector3 originalLocalPos = targetRect.localPosition;
        Vector3 originalWorldPos = targetRect.position;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float damper = 1f - Mathf.Clamp01(elapsed / duration);
            Vector2 offset2D = Random.insideUnitCircle * strength * damper;

            // Apply shake in multiple coordinate spaces so it’s always visible
            targetRect.anchoredPosition = originalAnchored + offset2D;
            targetRect.localPosition = originalLocalPos + (Vector3)offset2D;
            targetRect.position = originalWorldPos + (Vector3)offset2D;

            yield return null;
        }

        // Restore original position
        targetRect.anchoredPosition = originalAnchored;
        targetRect.localPosition = originalLocalPos;
        targetRect.position = originalWorldPos;

        Debug.Log("ShakeCanvas finished on " + targetRect.name);
        dialogueBox.SetActive(true);
        Invoke(nameof(TurbOffBox), 2f);
       
            AudioManager.Instance.audioSource_Click.Stop();
            AudioManager.Instance.PlaySpecificClip(audioClip);
        
    }

    public void TurbOffBox()
    {
        dialogueBox.SetActive(false);
    }


}
