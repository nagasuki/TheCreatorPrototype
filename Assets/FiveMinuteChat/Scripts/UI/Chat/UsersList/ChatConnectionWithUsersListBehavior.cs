namespace FiveMinuteChat.UI
{
    public class ChatConnectionWithUsersListBehavior : ChatConnectionBehavior
    {
        protected override void InitConnectorWithMessageHandler() 
            => Connector.InitWithMessageHandler( new ChatWithUsersListMessageHandler() );
    }
}
