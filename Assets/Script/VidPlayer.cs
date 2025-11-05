using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using System;

[RequireComponent(typeof(VideoPlayer))]
public class VidPlayer : MonoBehaviour
{

    [SerializeField] string videoFileName;


    void Start()
    {
        PlayVideo();
    }

    private void PlayVideo()
    {
        VideoPlayer videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer)
        {
            string videoURl = Path.Combine(Application.streamingAssetsPath, videoFileName);
            videoPlayer.url = videoURl;
            videoPlayer.Prepare();
            videoPlayer.Play();
        }
    }
}