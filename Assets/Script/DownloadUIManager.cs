using System.Collections;
using UnityEngine;
using UnityEngine.UI;

using TMPro;


public class DownloadUIManager : MonoBehaviour
{
    [Header("UI References (drag one)")]

    public TextMeshProUGUI tmpText; // optional: drag your TMP text here
        // optional: legacy UI text

    [Header("Behavior")]
    public string baseMessage = "Downloading Content";
    public float dotInterval = 0.5f; // seconds per dot step (., .., ...)
    public int maxDots = 3;

    public GameObject textImage;
    static DownloadUIManager instance;
    Coroutine dotCoroutine;

    void Awake()
    {
        // singleton style: only one manager expected
        if (instance != null && instance != this) Destroy(this);
        else instance = this;

        // hide initially
        SetVisible(false);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // Static API
    public static void Show()
    {
        if (instance == null) return;
        instance.InternalShow();
    }

    public static void Hide()
    {
        if (instance == null) return;
        instance.InternalHide();
    }

    void InternalShow()
    {
        SetVisible(true);
        if (dotCoroutine != null) StopCoroutine(dotCoroutine);
        dotCoroutine = StartCoroutine(DotLoop());
    }

    void InternalHide()
    {
        if (dotCoroutine != null)
        {
            StopCoroutine(dotCoroutine);
            dotCoroutine = null;
        }
        SetVisible(false);
    }

    IEnumerator DotLoop()
    {
        int dots = 0;
        while (true)
        {
            dots = (dots + 1) % (maxDots + 1); // 0..maxDots
            string dotsStr = new string('.', dots);
            string text = baseMessage + (dots > 0 ? dotsStr : "");

            if (tmpText != null) tmpText.text = text;
         

          

            yield return new WaitForSecondsRealtime(dotInterval);
        }
    }

    void SetVisible(bool v)
    {

        if (tmpText != null) 
        {
            tmpText.gameObject.SetActive(v);
            textImage.SetActive(v);
        }

    }
}
