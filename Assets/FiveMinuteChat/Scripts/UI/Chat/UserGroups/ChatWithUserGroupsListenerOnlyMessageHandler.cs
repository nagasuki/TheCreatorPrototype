namespace FiveMinuteChat.UI
{
    public class ChatWithUserGroupsListenerOnlyMessageHandler : ChatMessageHandler
    {
        private readonly ChatConnectionWithUserGroupsListenerOnlyBehavior _connectionBehavior;

        public ChatWithUserGroupsListenerOnlyMessageHandler( ChatConnectionWithUserGroupsListenerOnlyBehavior connectionBehavior )
        {
            _connectionBehavior = connectionBehavior;
        }
        
    }
}
