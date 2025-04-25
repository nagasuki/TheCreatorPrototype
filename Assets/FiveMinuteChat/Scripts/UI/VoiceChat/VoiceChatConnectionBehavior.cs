using System;
using System.Linq;
using FiveMinuteChat.UI.VoiceChat;
using FiveMinutes.Model.Messages.Client;
#if !UNITY_WEBGL 
using Unity.WebRTC;
#endif
using UnityEngine;

namespace FiveMinuteChat
{
    public partial class VoiceChatConnectionBehavior : ConnectionBehaviorBase
    {
        protected override void InitConnectorWithMessageHandler() 
            => Connector.InitWithMessageHandler( new VoiceChatMessageHandler() );

        private string OtherUserId => 
#if UNITY_2022_3_OR_NEWER
            FindObjectsByType<VoiceChatConnectionBehavior>( FindObjectsInactive.Exclude, FindObjectsSortMode.None )
#else
            FindObjectsOfType<VoiceChatConnectionBehavior>( false )
#endif
                .First( c => c != this )
                .UserId;
        
        protected override void OnStart()
        {
            if( UserId == SystemInfo.deviceUniqueIdentifier )
            {
                UserId = Guid.NewGuid().ToString();
            }
            
            if( AutoConnect )
            {
                Connect();
            }
        }
#if !UNITY_WEBGL 

        public void SendNegotiationRequest( string type, string connectionInfo )
        {
            Connector.Send( new ClientP2PStreamNegotiationRequest
            {
                Type = type, 
                ConnectionInfo = connectionInfo,
                Recipient = OtherUserId
            } );
        }

        public void SendCandidate( RTCIceCandidate candidate )
        {
            Connector.Send( new ClientP2PStreamEndpointInfoRequest
            {
                Candidate = candidate.Candidate,
                SdpMid = candidate.SdpMid,
                SdpMLineIndex = candidate.SdpMLineIndex,
                Recipient = OtherUserId
            } );
        }
#endif
    }
}
