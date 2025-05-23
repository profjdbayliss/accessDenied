using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class RGNetworkLoginUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] internal TMP_InputField playernameInput;
    [SerializeField] internal Button hostButton;
    [SerializeField] internal Button clientButton;
    public GameObject messageBox;
    public GameObject StartScreen;
    public Toggle TutorialToggle;
    public TextMeshProUGUI messageBoxText;

    public static RGNetworkLoginUI s_instance;
    public static bool HasLoadedScene = false;
    
    void Awake()
    {
        s_instance = this;
    }

    public void Update()
    {
        if (MessageInfo.ShouldDisplayMessage && !messageBox.activeSelf)
        {
            Debug.Log("displaying message");
            DisplayMessageBox(MessageInfo.Message);
        }
    }
    // Called by UI element UsernameInput.OnValueChanged
    public void ToggleButtons(string username)
    {
        hostButton.interactable = !string.IsNullOrWhiteSpace(username);
        clientButton.interactable = !string.IsNullOrWhiteSpace(username);
    }

    public void LoadScene(int index)
    {
        if (!HasLoadedScene)
        {
            HasLoadedScene = true;
            Debug.Log("loading scene in rg login ui " + index);
            SceneManager.LoadScene(index);
        }
    }

    public void DisplayMessageBox(string msg)
    {
        messageBoxText.text = msg;
        messageBox.SetActive(true);
        Debug.Log("showing message box");
    }

    public void HideMessageBox()
    {
        messageBox.SetActive(false);
        Debug.Log("hiding message box");
        MessageInfo.ShouldDisplayMessage = false;
    }

    public  void HideStartScreen()
    {
        StartScreen.SetActive(false);
    }

    public void ShowStartScreen()
    {
        StartScreen.SetActive(true );
    }

    public void ToggleTutorial()
    {
        MessageInfo.ShowTutorial = TutorialToggle.isOn;
        Debug.Log("tutorial is set to: " + TutorialToggle.isOn);
    }

   
}
