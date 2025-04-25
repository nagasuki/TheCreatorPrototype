using System;
using System.Collections.Generic;
using FiveMinutes.Model.Messages.Client;

namespace FiveMinuteChat.UI
{
    public class ChatConnectionWithUserGroupsBehavior : ChatConnectionBehavior
    {
        protected override void InitConnectorWithMessageHandler() 
            => Connector.InitWithMessageHandler( new ChatWithUserGroupsMessageHandler( this ) );
        
        protected override void Awake()
        {
            UserId = Guid.NewGuid().ToString();
            base.Awake();
        }

        public void SendRegisterGroup( string groupName, List<string> userIds )
        {
            if(userIds.Count == 0)
            {
                Logger.LogError( "FiveMinuteChat: No users to add to personal group, skipping..." );
                return;
            }
            var message = new ClientAddUsersToPersonalGroupRequest
            {
                GroupMoniker = groupName,
                OtherUserIds = userIds,
                IsAckRequested = false
            };
            Connector.Send(message);
        }
    }
}
