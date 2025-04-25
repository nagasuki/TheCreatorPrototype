using System;
using FiveMinuteChat.Extensions;
using FiveMinutes.Model;
using FiveMinutes.Model.Enums;
using FiveMinutes.Model.Messages.Server;
using UnityEngine;
using UnityEngine.UI;

namespace FiveMinuteChat.UI.ChatBubbles
{
    public class SupportLogBehavior : MonoBehaviour
    {
        private RectTransform _contentContainer;
        public SupportConnectionBehavior Connection;
        public GameObject ChatBubblePrefab;
        private InputField _inputField;
        private Button _sendButtonField;
        private Guid _serverWelcomeCallbackId;
        private Guid _serverChatMessageCallbackId;
        private Guid _serverWhisperMessageCallbackId;
        private Guid _supportMessageCallbackId;
        private Guid _supportTicketCreatedCallbackId;

        private void Awake()
        {
            if( !Connection )
            {
                Connection = GetComponentInParent<SupportConnectionBehavior>();
            }
            if( Connection )
            {
                _supportTicketCreatedCallbackId = Connection.Subscribe<ServerCreateSupportTicketResponse>( OnCreateSupportTicketResponse );
                _supportMessageCallbackId = Connection.Subscribe<ServerSupportTicketMessage>( OnSupportMessage );
            }
            else
            {
                Logger.LogError($"FiveMinuteChat: No {nameof(SupportConnectionBehavior)} has been assigned to the Connection field. It must either be found as a parent of this GameObject or set explicitly via the Connection field on this behavior.");
                return;
            }

            _inputField = transform.parent.parent.Find("Input Bottom Bar").GetComponentInChildren<InputField>();
            _sendButtonField = transform.parent.parent.Find("Input Bottom Bar").GetComponentInChildren<Button>();
            _sendButtonField.onClick.AddListener( SendMessage );
            
            _contentContainer = GetComponent<ScrollRect>().content;
            _contentContainer.transform.Clear();
        }

        private void SendMessage()
        {
            Connection.SendSupportMessage( Connection.CurrentSupportTicketId, _inputField.text );
            _inputField.text = string.Empty;
        }

        private void OnCreateSupportTicketResponse( ServerCreateSupportTicketResponse message )
        {
            var fakeId = Guid.NewGuid();
            var fakeMessage = $"Support ticket with ID {message.SupportTicketId} has been created. Please stand by, we will take care of you as soon as possible...";

            Instantiate( ChatBubblePrefab, _contentContainer )
                .GetComponent<ChatBubbleBehavior>()
                .SetCanBeReported( false )
                .SetMessage( new ServerChatMessage()
                {
                    MessageId = fakeId,
                    Content = fakeMessage,
                    FromUser = new UserInfo()
                    {
                        Name = "System",
                        UserType = UserType.Support
                    },
                    SentAt = DateTime.UtcNow
                } );
        }

        private void OnSupportMessage( ServerSupportTicketMessage message )
        {
            Logger.Log($"FiveMinuteChat: Received support message: {message.Message}");
            Instantiate( ChatBubblePrefab, _contentContainer )
                .GetComponent<ChatBubbleBehavior>()
                .SetCanBeReported( false )
                .SetMessage( new ServerChatMessage()
                {
                    Content = message.Message,
                    FromUser = new ()
                    {
                        Name = message.FromUser.Name,
                        UserType = message.FromUser.UserType,
                        DisplayId = message.FromUser.UserType == UserType.Standard 
                            ? Connection.OwnDisplayId
                            : message.FromUser.DisplayId
                    },
                    SentAt = DateTime.UtcNow,
                    MessageId = message.MessageId
                } );
        }

        private void Deactivate()
        {
            if( Connection )
            {
                Connection.Unsubscribe(_supportTicketCreatedCallbackId);
                Connection.Unsubscribe(_supportMessageCallbackId);
            }
        }

        private void OnDestroy() => Deactivate();

        private void OnApplicationQuit() => Deactivate();
    }
}
