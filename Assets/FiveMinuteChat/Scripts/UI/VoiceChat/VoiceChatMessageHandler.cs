using FiveMinuteChat.Helpers;
using FiveMinutes.Model.Messages.Server;

namespace FiveMinuteChat.UI.VoiceChat
{
    public class VoiceChatMessageHandler : MessageHandlerBase
    {
        protected override void RegisterHandlers()
        {
            Handlers.Add( typeof(ServerWhisperMessage),( _, _ ) => { } );
            Handlers.Add( typeof(ServerP2PStreamNegotiationRequest),( _, _ ) => { } );
            Handlers.Add( typeof(ServerP2PStreamEndpointInfoRequest),( _, _ ) => { } );
        }
    }
}
