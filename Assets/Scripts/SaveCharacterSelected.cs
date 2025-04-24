using UnityEngine;

public class SaveCharacterSelected : MonoBehaviour
{
    public static SaveCharacterSelected Instance;
    public int CharacterSelectedIndex { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    public void SetCharacter(int index)
    {
        CharacterSelectedIndex = index;
    }
}
