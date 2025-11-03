using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.UI.ProceduralImage;
using System.Collections;
using TMPro;

public class UIManager : MonoBehaviour
{

    public bool isTesting;
    [SerializeField] Button start_btn, instructions_btn;
    [SerializeField] Button[] sound_btn;
    [SerializeField] Sprite soundImage, muteImage;
    [SerializeField] RectTransform mainMenu, SubMenu, terrestial, desert, aquatic;
    [SerializeField] RectTransform[] subMenuButtons;
    [SerializeField] bool isMute;
    [SerializeField] Ease ease;
    [SerializeField] GameObject leafsParticles;


    public float pivotX, pivotY, duration;




    void Start()
    {
      

        leafsParticles.SetActive(true);
        if (!isTesting)
        {

            InitializeMainMenu();
        }

        start_btn.onClick.AddListener(delegate
        {
            // StartCoroutine(MovePivot(pivotX, pivotY, duration));
            leafsParticles.SetActive(false);
            AudioManager.Instance.ClickSound();
        });
        instructions_btn.onClick.AddListener(delegate { AudioManager.Instance.ClickSound(); });

    }

  

    private void ScaleZeroBtn()
    {
        start_btn.transform.localScale = Vector2.zero;
        instructions_btn.transform.localScale = Vector2.zero;
        sound_btn[0].transform.localScale = Vector2.zero;
    }

    public void InitializeMainMenu()
    {
        mainMenu.transform.localScale = Vector2.one;
        SubMenu.transform.localScale = Vector2.zero;
        terrestial.transform.localScale = Vector2.zero;
        desert.transform.localScale = Vector2.zero;
        aquatic.transform.localScale = Vector2.zero;

        terrestial.transform.localScale = Vector2.zero;
        desert.transform.localScale = Vector2.zero;
        aquatic.transform.localScale = Vector2.zero;

        foreach (var item in subMenuButtons)
        {
            item.transform.localScale = Vector2.zero;
        }

        ScaleZeroBtn();
        // mainMenu.pivot = new Vector2(0.5f, 0.5f);
       
        start_btn.transform.DOScale(Vector2.one, .2f).SetEase(ease);
        AudioManager.Instance.PopSOund();
        Invoke(nameof(InstructionBtn), .2f);
    }

    void InstructionBtn()
    {
        AudioManager.Instance.PopSOund();
        instructions_btn.transform.DOScale(Vector2.one, .2f).SetEase(ease);
        Invoke(nameof(SoundBtn), .2f);
    }

    void SoundBtn()
    {
        AudioManager.Instance.PopSOund();
        sound_btn[0].transform.DOScale(Vector2.one, .2f).SetEase(ease);
    }

    public void MuteButton()
    {
        isMute = !isMute;
        if (isMute)
        {
            AudioManager.Instance.IsMute();

            foreach (var item in sound_btn)
            {
                item.GetComponent<ProceduralImage>().sprite = muteImage;
            }
            AudioManager.Instance.ClickSound();

        }
        else
        {
            AudioManager.Instance.IsMute();
            foreach (var item in sound_btn)
            {
                item.GetComponent<ProceduralImage>().sprite = soundImage;
            }
            AudioManager.Instance.ClickSound();
        }
    }

    public void PlayButtonClicked()
    {
        SubMenu.transform.DOScale(Vector3.one, .2f).SetEase(ease);
    
        Invoke(nameof(SubMenuButtonsClicked), .2f);
    }

    public void SubMenuButtonsClicked()
    {
        foreach (var item in subMenuButtons)
        {
            item.gameObject.SetActive(true);
            item.transform.localScale = Vector2.zero;
        }
        subMenuButtons[0].transform.DOScale(Vector3.one, .2f).SetEase(ease);
        AudioManager.Instance.PopSOund();
        Invoke(nameof(SubButton1), .2f);
    }

    public void SubButton1()
    {
        subMenuButtons[1].transform.DOScale(Vector3.one, .2f).SetEase(ease);
        AudioManager.Instance.PopSOund();
        Invoke(nameof(SubButton2), .2f);
    }

    public void SubButton2()
    {
        AudioManager.Instance.PopSOund();
        subMenuButtons[2].transform.DOScale(Vector3.one, .2f).SetEase(ease);
    }

    public void BackButtonToMainMenu()
    {
     
        AudioManager.Instance.ClickSound();
        SubMenu.transform.DOScale(Vector2.zero, .2f).SetEase(ease);
        foreach (var item in subMenuButtons)
        {
            item.transform.localScale = Vector2.zero;
        }

    }

    public void TerrestialButtonClicked()
    {
        foreach (var item in subMenuButtons)
        {
            item.gameObject.SetActive(false);
            subMenuButtons[0].gameObject.SetActive(true);
        }
      
        AudioManager.Instance.ClickSound();
        Invoke(nameof(Terrestial), .3f);
    }

    void Terrestial()
    {
        terrestial.transform.DOScale(Vector2.one, .2f).SetEase(ease);
    }

    public void DesertButtonClicked()
    {
        foreach (var item in subMenuButtons)
        {
            item.gameObject.SetActive(false);
            subMenuButtons[1].gameObject.SetActive(true);
        }
       
        AudioManager.Instance.ClickSound();
        Invoke(nameof(Desert), .3f);
    }

    public void AquaticButtonClicked()
    {
        foreach (var item in subMenuButtons)
        {
            item.gameObject.SetActive(false);
            subMenuButtons[2].gameObject.SetActive(true);
        }
       
        AudioManager.Instance.ClickSound();
        Invoke(nameof(Aquatic), .3f);
    }

    public void Aquatic()
    {
        aquatic.transform.DOScale(Vector2.one, .2f).SetEase(ease);

    }

    public void Desert()
    {
        desert.transform.DOScale(Vector2.one, .2f).SetEase(ease);

    }
    public void MainToSubMenu()
    {
        AudioManager.Instance.ClickSound();
        terrestial.transform.DOScale(Vector2.zero, .2f).SetEase(ease);
        desert.transform.DOScale(Vector2.zero, .2f).SetEase(ease);
        aquatic.transform.DOScale(Vector2.zero, .2f).SetEase(ease);
        Invoke(nameof(SubMenuButtonsClicked), .1f);
    }



}
