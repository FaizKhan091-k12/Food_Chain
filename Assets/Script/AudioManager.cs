using System.Collections;
using NUnit.Framework;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    [SerializeField] public AudioSource audioSource_Click;
    [SerializeField] public AudioSource audioSource_GameBG;
    [SerializeField] AudioClip click_Clip, pop_Sound, start_BG, InLevelBG;


    public bool isMute;

    void Awake()
    {
        Instance = this;
        audioSource_GameBG.resource = start_BG;
    }

    public void ClickSound()
    {

        audioSource_Click.PlayOneShot(click_Clip);
    }

    public void IsMute()
    {
        isMute = !isMute;
        if (isMute)
        {
          
            audioSource_GameBG.enabled = false;
        }
        else
        {
          
            audioSource_GameBG.enabled = true;
        }
    }

    public void PopSOund()
    {

    }

    public void IntoLevel()
    {
        StartCoroutine(FadeIntoLevel());
    }
    public void IntoMain()
    {
        StartCoroutine(FadeIntoMain());
    }
    IEnumerator FadeIntoLevel()
    {
        float t = 0f;
        while (t < 1)
        {
            t += Time.deltaTime;
            audioSource_GameBG.volume = Mathf.Lerp(.3f, 0, t);

            yield return null;
        }
        StartCoroutine(FadeIntoLevel1());
    }


    IEnumerator FadeIntoLevel1()
    {
        audioSource_GameBG.resource = InLevelBG;
        audioSource_GameBG.Play();
        float t = 0f;
        while (t < 1)
        {
            t += Time.deltaTime;
            audioSource_GameBG.volume = Mathf.Lerp(0, .3f, t);

            yield return null;
        }
    }
    IEnumerator FadeIntoMain()
    {
        float t = 0f;
        while (t < 1)
        {
            t += Time.deltaTime;
            audioSource_GameBG.volume = Mathf.Lerp(.3f, 0, t);

            yield return null;
        }
        StartCoroutine(FadeIntoMain1());
    }


    IEnumerator FadeIntoMain1()
    {
        audioSource_GameBG.resource = start_BG;
        audioSource_GameBG.Play();
        float t = 0f;
        while (t < 1)
        {
            t += Time.deltaTime;
            audioSource_GameBG.volume = Mathf.Lerp(0, .3f, t);

            yield return null;
        }
    }

    public void PlaySpecificClip(AudioClip audioClip)
    {
        audioSource_Click.PlayOneShot(audioClip);
    }
}
