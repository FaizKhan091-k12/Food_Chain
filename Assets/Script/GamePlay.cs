using System.Collections;
using UnityEngine;

public class GamePlay : MonoBehaviour
{
    public bool one, two, three;

    public bool OnBackBtn;
    public GameObject backButton, bottom_Layer;
    public AudioClip audioClip_Great;
    void Start()
    {
        OnBackBtn = true;

    }
    void OnEnable()
    {
        OnBackBtn = true;
        backButton.SetActive(false);
        bottom_Layer.SetActive(true);
        one = false;
        two = false;
        three = false;
    }
    void Update()
    {
        if (one && two && three)
        {

            if (OnBackBtn)
            {
                OnBackBtn = false;
                backButton.SetActive(true);
                bottom_Layer.SetActive(false);
                AudioManager.Instance.audioSource_Click.Stop();
                AudioManager.Instance.PlaySpecificClip(audioClip_Great);
            }
            else
            {
                return;
            }
        }
    }

 




}
