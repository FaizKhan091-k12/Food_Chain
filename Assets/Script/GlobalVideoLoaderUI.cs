using UnityEngine;
using UnityEngine.UI;
public class GlobalVideoLoaderUI : MonoBehaviour
{
    public Slider globalSlider;
    void Awake()
    {
        WebGLVideoPlayer.globalLoadingSlider = globalSlider;
    }
}
