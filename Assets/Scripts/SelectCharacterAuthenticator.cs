using Mirror;

public class SelectCharacterAuthenticator : NetworkAuthenticator
{
    public struct AuthRequest : NetworkMessage { public int index; }
    public struct AuthResponse : NetworkMessage { }

    public override void OnStartServer()
    {
        NetworkServer.RegisterHandler<AuthRequest>(OnAuthRequest, false);
    }

    public override void OnStartClient()
    {
        NetworkClient.RegisterHandler<AuthResponse>(OnAuthResponse, false);
    }

    public override void OnClientAuthenticate()
    {
        int index = SaveCharacterSelected.Instance.CharacterSelectedIndex;
        NetworkClient.Send(new AuthRequest { index = index });
    }

    public override void OnServerAuthenticate(NetworkConnectionToClient conn)
    {
        // รอ message มา -> ค่อย Accept
    }

    void OnAuthRequest(NetworkConnectionToClient conn, AuthRequest msg)
    {
        conn.authenticationData = msg.index;
        conn.isAuthenticated = true;
        ServerAccept(conn);
        conn.Send(new AuthResponse());
    }

    void OnAuthResponse(AuthResponse msg) => ClientAccept();
}
