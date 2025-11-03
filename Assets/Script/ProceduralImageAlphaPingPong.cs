using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage; // Make sure you have the ProceduralImage namespace

[RequireComponent(typeof(ProceduralImage))]
public class ProceduralImageAlphaPingPong : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Time (in seconds) for one full fade in-out cycle.")]
    public float pingPongDuration = 2f;

    [Tooltip("Minimum alpha value.")]
    [Range(0f, 1f)] public float minAlpha = 0f;

    [Tooltip("Maximum alpha value.")]
    [Range(0f, 1f)] public float maxAlpha = 1f;

    [Tooltip("If true, animation will play automatically on Start.")]
    public bool playOnStart = true;

    [Header("Runtime State")]
    public bool isPlaying = false;

    private ProceduralImage proceduralImage;
    private Color originalColor;
    private float timer = 0f;

    void Awake()
    {
        proceduralImage = GetComponent<ProceduralImage>();
        originalColor = proceduralImage.color;
    }

    void Start()
    {
        if (playOnStart) StartPingPong();
    }

    void Update()
    {
        if (!isPlaying) return;

        timer += Time.deltaTime;
        float t = Mathf.PingPong(timer / pingPongDuration, 1f);
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

        Color c = originalColor;
        c.a = alpha;
        proceduralImage.color = c;
    }

    /// <summary>
    /// Starts the ping-pong alpha animation.
    /// </summary>
    public void StartPingPong()
    {
        isPlaying = true;
        timer = 0f;
        proceduralImage.color = new Color(1, 1, 0, 1);
    }

    /// <summary>
    /// Stops the ping-pong alpha animation and restores original color.
    /// </summary>
    public void StopPingPong()
    {
        isPlaying = false;
        proceduralImage.color = originalColor;
        proceduralImage.color = new Color(1, 1, 0, 0);

    }

    /// <summary>
    /// Set a new duration at runtime.
    /// </summary>
    public void SetDuration(float seconds)
    {
        pingPongDuration = Mathf.Max(0.1f, seconds);
    }
}
