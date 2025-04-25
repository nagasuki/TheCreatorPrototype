using FiveMinuteChat.Helpers;
using FiveMinutes.Model.Messages.Server;

namespace FiveMinuteChat.UI.SupportRequests
{
    public class SupportMessageHandler : MessageHandlerBase
    {
        protected override void RegisterHandlers()
        {
            Handlers.Add( typeof(ServerCreateSupportTicketResponse),( _, _ ) => { } );
            Handlers.Add( typeof(ServerGetSupportTicketResponse), ( _, _ ) => { } );
            Handlers.Add( typeof(ServerSupportTicketMessage),( _, _ ) => { } );
            Handlers.Add( typeof(ServerWhisperMessage), ( _, _ ) => { } );
        }
    }
}
