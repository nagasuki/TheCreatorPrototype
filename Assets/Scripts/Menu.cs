using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private Button exitButton;
    [SerializeField] private TMP_Dropdown dropdown;

    void Start()
    {
        NetworkManager.singleton.onlineScene = dropdown.options[0].text;
        hostButton.onClick.AddListener(() => NetworkManager.singleton.StartHost());
        joinButton.onClick.AddListener(() => NetworkManager.singleton.StartClient());
        exitButton.onClick.AddListener(() => Application.Quit());
    }

    public void OnInputFieldChanged(string text)
    {
        NetworkManager.singleton.networkAddress = text;
    }

    public void OnDropdownValueChanged(int value)
    {
        NetworkManager.singleton.onlineScene = dropdown.options[value].text;
    }
}
