using FiveMinutes.Model.Messages.Client;

namespace FiveMinuteChat
{
    public partial class SupportConnectionBehavior
    {
        public void SendSupportRequest( string topic, string message )
        {
            Connector.Send( new ClientCreateSupportTicketRequest()
            {
                Topic = topic,
                Message = message
            } );
        }

        public void SendSupportMessage( string supportTicketId, string message )
        {
            Connector.Send( new ClientSendSupportTicketMessageRequest()
            {
                SupportTicketId = supportTicketId,
                Message = message, 
            } );
        }

        public void ResumeSupportTicket( string supportTicketId )
        {
            Connector.Send( new ClientGetSupportTicketRequest()
            {
                SupportTicketId = supportTicketId
            } );
        }
    }
}
