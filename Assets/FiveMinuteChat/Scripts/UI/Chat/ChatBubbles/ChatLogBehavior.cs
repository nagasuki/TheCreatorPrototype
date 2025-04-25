using System;
using System.Collections.Generic;
using FiveMinuteChat.Extensions;
using FiveMinutes.Model;
using FiveMinutes.Model.Messages.Server;
using UnityEngine;
using UnityEngine.UI;

namespace FiveMinuteChat.UI.ChatBubbles
{
    public class ChatLogBehavior : MonoBehaviour, IChatLogBehavior
    {
        public enum ChatEntryType
        {
            ChatMessage,
            WhisperMessage
        }

        private class ChatEntry
        {
            public Guid MessageId { get; set; }
            public ChatEntryType Type { get; set; }
            public DateTime SentAt { get; set; }
            public UserInfo FromUser { get; set; }
            public string Content { get; set; }
        }
        
        private readonly Dictionary<Guid, ChatEntry> _chatEntries = new();
        
        private RectTransform _contentContainer;
        public ChatConnectionBehavior Connection;
        public string ChannelName = "Global";
        public GameObject ChatBubblePrefab;
        private InputField _inputField;
        private Guid _serverWelcomeCallbackId;
        private Guid _serverChatMessageCallbackId;
        private Guid _serverWhisperMessageCallbackId;
        private Guid _userSilenceInfoCallbackId;
        private string _originalPlaceholderText;

        private void Awake()
        {
            _contentContainer = GetComponent<ScrollRect>().content;
            _contentContainer.transform.Clear();
        }

        private void Start()
        {
            if( !Connection )
            {
                Connection = GetComponentInParent<ChatConnectionBehavior>();
            }

            if( !_inputField )
            {
                _inputField = transform.parent.Find("Chat Bottom Bar").GetComponentInChildren<InputField>();
            }
            if( _inputField )
            {
                _originalPlaceholderText = _inputField.transform.Find( "Placeholder" ).GetComponent<Text>().text;
            }
        }

        public void Init( string channelName, bool isSilenced )
        {
            ChannelName = channelName;
            Activate();

            if(!_inputField)
            {
                _inputField = transform.parent.Find("Chat Bottom Bar").GetComponentInChildren<InputField>();
            }
            if( _inputField )
            {
                _originalPlaceholderText = _inputField.transform.Find( "Placeholder" ).GetComponent<Text>().text;
            }
            
            Logger.Log($"FiveMinuteChat: Initialized channel {channelName} with silenced = {isSilenced}");
            SetIsSilenced( isSilenced );
        }

        private void Activate()
        {
            if( !Connection )
            {
                Connection = GetComponentInParent<ChatConnectionBehavior>();
            }
            if( Connection )
            {
                _serverChatMessageCallbackId = Connection.Subscribe<ServerChatMessage>(OnChatMessageReceived);
                _serverWhisperMessageCallbackId = Connection.Subscribe<ServerWhisperMessage>(OnWhisperMessageReceived);
                _userSilenceInfoCallbackId = Connection.Subscribe<ServerUserSilenceInfoMessage>( OnUserSilenceInfoMessage );
            }
            else
            {
                Logger.LogError($"FiveMinuteChat: No {nameof(ChatConnectionBehavior)} has been assigned to the Connection field. It must either be found as a parent of this GameObject or set explicitly via the Connection field on this behavior.");
            }
        }

        private void Deactivate()
        {
            if( Connection )
            {
                Connection.Unsubscribe(_serverChatMessageCallbackId);
                Connection.Unsubscribe(_serverWhisperMessageCallbackId);
                Connection.Unsubscribe(_userSilenceInfoCallbackId);
            }
        }

        private void OnDestroy() => Deactivate();

        private void OnApplicationQuit() => Deactivate();

        private void OnChatMessageReceived( ServerChatMessage message )
        {
            if( message.ChannelName == ChannelName && 
                message.Content.Length > 1 )
            {
                if( !_chatEntries.ContainsKey( message.MessageId ) )
                {
                    _chatEntries.Add( message.MessageId, new ChatEntry
                    {
                        Type = ChatEntryType.ChatMessage,
                        MessageId = message.MessageId,
                        FromUser = message.FromUser,
                        SentAt = message.SentAt,
                        Content = message.Content
                    }  );
                    Instantiate( ChatBubblePrefab, _contentContainer )
                        .GetComponent<ChatBubbleBehavior>().SetMessage( message );
                }
            }
        }

        private void OnWhisperMessageReceived( ServerWhisperMessage message )
        {
            if( gameObject.activeInHierarchy && 
                message.Content.Length > 1 )
            {
                if( message.MessageId == Guid.Empty )
                {
                    // system messages use empty ids 
                    message.MessageId = Guid.NewGuid();
                }

                if( !_chatEntries.ContainsKey( message.MessageId ) )
                {
                    _chatEntries.Add( message.MessageId,
                        new ChatEntry
                        {
                            Type = ChatEntryType.WhisperMessage,
                            FromUser = message.FromUser,
                            SentAt = message.SentAt,
                            Content = message.Content
                        } );
                    Instantiate( ChatBubblePrefab, _contentContainer )
                        .GetComponent<ChatBubbleBehavior>().SetMessage( message );
                }
            }
        }

        private void OnUserSilenceInfoMessage( ServerUserSilenceInfoMessage message )
        {
            if( message.ChannelName == ChannelName )
            {
                SetIsSilenced( message.IsSilenced );
            }
        }

        private void SetIsSilenced( bool isSilenced )
        {
            if( !_inputField )
                return;
            _inputField.interactable = !isSilenced;
            _inputField.GetComponent<Image>().color = isSilenced ? new Color( 0.69f, 0.69f, 0.69f, 1 ) : Color.white;
            _inputField.transform.Find( "Placeholder" ).GetComponent<Text>().text = isSilenced ? "You have been silenced" : _originalPlaceholderText ?? "enter your message...";
            _inputField.transform.Find( "Placeholder" ).GetComponent<Text>().color = isSilenced ? Color.black : new Color(0.5f,0.5f,0.5f,1);
            _inputField.transform.parent.Find( "Send Button" ).GetComponent<Button>().interactable = !isSilenced;
        }
    }
}
