using Mirror;
using UnityEngine;

public class CreatorNetworkManager : NetworkManager
{
    public GameObject localPlayer;

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        int index = conn.authenticationData != null ? (int)conn.authenticationData : 0;

        Transform start = GetStartPosition();
        GameObject player = Instantiate(playerPrefab, start.position, start.rotation);
        player.GetComponent<PlayerController>().SetBodyIndex(index);

        NetworkServer.AddPlayerForConnection(conn, player);
        //base.OnServerAddPlayer(conn);
    }
}
