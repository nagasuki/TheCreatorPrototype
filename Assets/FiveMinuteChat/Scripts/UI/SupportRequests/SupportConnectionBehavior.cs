using FiveMinuteChat.UI.SupportRequests;
using FiveMinutes.Model.Messages.Server;

namespace FiveMinuteChat
{
    public partial class SupportConnectionBehavior : ConnectionBehaviorBase
    {
        public string CurrentSupportTicketId { get; private set; }
        
        protected override void InitConnectorWithMessageHandler() 
            => Connector.InitWithMessageHandler( new SupportMessageHandler() );

        protected override void OnStart()
        {
            Subscribe<ServerCreateSupportTicketResponse>( response =>
            {
                if( response.Success && !string.IsNullOrEmpty( response.SupportTicketId ) )
                {
                    CurrentSupportTicketId = response.SupportTicketId;
                }
            } );
            Subscribe<ServerGetSupportTicketResponse>( response =>
            {
                if( response.Success && !string.IsNullOrEmpty( response.SupportTicketId ) )
                {
                    CurrentSupportTicketId = response.SupportTicketId;
                }
            } );
            
            if( AutoConnect )
            {
                Connect();
            }
        }
    }
}
