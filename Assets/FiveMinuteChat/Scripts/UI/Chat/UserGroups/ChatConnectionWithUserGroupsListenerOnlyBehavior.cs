using System;

namespace FiveMinuteChat.UI
{
    public class ChatConnectionWithUserGroupsListenerOnlyBehavior : ChatConnectionBehavior
    {
        protected override void InitConnectorWithMessageHandler() 
            => Connector.InitWithMessageHandler( new ChatWithUserGroupsListenerOnlyMessageHandler( this ) );

        protected override void Awake()
        {
            UserId = Guid.NewGuid().ToString();
            base.Awake();
        }
    }
}
