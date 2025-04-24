using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSelection : MonoBehaviour
{
    [SerializeField] private List<GameObject> characterPrefabs = new();
    [SerializeField] private Transform spawnPoint = default!;

    private GameObject selectedCharacter;

    private IEnumerator Start()
    {
        selectedCharacter = Instantiate(characterPrefabs[0], spawnPoint.position, spawnPoint.rotation);
        yield return new WaitUntil(() => SaveCharacterSelected.Instance != null);
        SaveCharacterSelected.Instance.SetCharacter(0);
    }

    public void SelectCharacter(int index)
    {
        Destroy(selectedCharacter);
        SaveCharacterSelected.Instance.SetCharacter(index);
        var newCharacter = Instantiate(characterPrefabs[index], spawnPoint.position, spawnPoint.rotation);
        selectedCharacter = newCharacter;
    }
}
