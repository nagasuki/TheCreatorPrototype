using Mirror;
using UnityEngine;

public class CreatorNetworkManager : NetworkManager
{
    public GameObject localPlayer;

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        int selectedIndex = SaveCharacterSelected.Instance.CharacterSelectedIndex;

        Debug.Log($"Character index: {selectedIndex}");

        playerPrefab.GetComponent<PlayerController>().SetBody(selectedIndex);

        Transform start = GetStartPosition();
        GameObject player = Instantiate(playerPrefab, start.position, start.rotation);

        localPlayer = player;

        NetworkServer.AddPlayerForConnection(conn, player);
        //base.OnServerAddPlayer(conn);
    }
}
