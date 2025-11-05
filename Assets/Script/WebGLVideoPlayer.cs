using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using UnityEngine.UI;

[RequireComponent(typeof(VideoPlayer))]
public class WebGLVideoPlayer : MonoBehaviour
{
    [Tooltip("Can be a URL (https://...) or local relative path. For caching the full URL is recommended.")]
    [SerializeField] string videoFileName;

    [Tooltip("If true, downloaded videos will be cached to persistentDataPath (not used on WebGL).")]
    public bool enableCaching = true;

    [Tooltip("Global loading slider shared by all video players.")]
    public static Slider globalLoadingSlider;

    VideoPlayer videoPlayer;
    string cachedPath; // local file path if cached
    static int activeDownloads = 0; // how many videos currently downloading/preparing

    IEnumerator Start()
    {
        ClearCache();
        videoPlayer = GetComponent<VideoPlayer>();
        ConfigureVideoPlayer();

        if (enableCaching && Application.platform != RuntimePlatform.WebGLPlayer)
        {
            string fileName = Path.GetFileName(videoFileName);
            if (string.IsNullOrEmpty(fileName))
                fileName = "cachedVideo.mp4";

            cachedPath = Path.Combine(Application.persistentDataPath, fileName);

            if (File.Exists(cachedPath))
            {
                videoPlayer.url = cachedPath;
                yield return PrepareAndPlay();
                yield break;
            }
            else
            {
                yield return StartCoroutine(DownloadAndPlay(videoFileName, cachedPath));
                yield break;
            }
        }

        // WebGL or no caching: stream directly via URL
        videoPlayer.url = videoFileName;
        yield return PrepareAndPlay();
    }

    void ConfigureVideoPlayer()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.source = VideoSource.Url;
        videoPlayer.playOnAwake = false;
        videoPlayer.skipOnDrop = true;
        videoPlayer.waitForFirstFrame = false;
        videoPlayer.errorReceived += (vp, msg) => Debug.LogError("[VidPlayer] VideoPlayer error: " + msg);
        videoPlayer.prepareCompleted += (vp) => Debug.Log("[VidPlayer] prepareCompleted");
    }

    IEnumerator PrepareAndPlay()
    {
        activeDownloads++;
        UpdateSliderVisible(true);

        videoPlayer.Prepare();
        float timeout = 8f;
        float timer = 0f;

        while (!videoPlayer.isPrepared && timer < timeout)
        {
            timer += Time.unscaledDeltaTime;
            UpdateGlobalSlider(timer / timeout);
            yield return null;
        }

        UpdateGlobalSlider(1f);
        activeDownloads--;

        if (!videoPlayer.isPrepared)
        {
            Debug.LogWarning("[VidPlayer] prepare timeout - trying to Play anyway");
            videoPlayer.Play();
        }
        else
        {
            videoPlayer.Play();
        }

        if (activeDownloads <= 0)
            UpdateSliderVisible(false);
    }

    IEnumerator DownloadAndPlay(string url, string localPath)
    {
        activeDownloads++;
        UpdateSliderVisible(true);

        Debug.Log("[VidPlayer] Downloading video for caching: " + url);
        UnityWebRequest uwr = UnityWebRequest.Get(url);
        uwr.downloadHandler = new DownloadHandlerBuffer();
        uwr.SendWebRequest();

        while (!uwr.isDone)
        {
            if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogWarning("[VidPlayer] Download error: " + uwr.error);
                break;
            }

            UpdateGlobalSlider(uwr.downloadProgress * 0.9f);
            yield return null;
        }

        if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
        {
            videoPlayer.url = url;
            yield return PrepareAndPlay();
            yield break;
        }

        byte[] data = uwr.downloadHandler.data;
        try
        {
            File.WriteAllBytes(localPath, data);
            Debug.Log("[VidPlayer] Saved cached video to: " + localPath);
            videoPlayer.url = localPath;
        }
        catch
        {
            videoPlayer.url = url;
        }

        UpdateGlobalSlider(1f);
        activeDownloads--;

        yield return PrepareAndPlay();

        if (activeDownloads <= 0)
            UpdateSliderVisible(false);
    }

    // --- Shared UI helpers ---
    static void UpdateGlobalSlider(float progress)
    {
        if (globalLoadingSlider != null)
        {
            globalLoadingSlider.value = Mathf.Clamp01(progress);
        }
    }

    static void UpdateSliderVisible(bool visible)
    {
        if (globalLoadingSlider != null)
        {
            globalLoadingSlider.gameObject.SetActive(visible);
        }
    }

    // --- 🧹 CLEAR CACHE FEATURE ---
    [ContextMenu("Clear Cached Videos")]
    public  static void ClearCache()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            Debug.Log("[VidPlayer] WebGL does not support local caching, nothing to clear.");
            return;
        }

        string cachePath = Application.persistentDataPath;

        if (!Directory.Exists(cachePath))
        {
            Debug.Log("[VidPlayer] No cache directory found.");
            return;
        }

        string[] files = Directory.GetFiles(cachePath, "*.mp4");
        int count = 0;

        foreach (var file in files)
        {
            try
            {
                File.Delete(file);
                count++;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[VidPlayer] Failed to delete cache file: " + file + " (" + ex.Message + ")");
            }
        }

        Debug.Log($"[VidPlayer] Cleared {count} cached video(s) from {cachePath}");
    }

    // --- Optional Button Hook ---
    public void ClearCacheFromButton()
    {
        ClearCache();
    }
}
