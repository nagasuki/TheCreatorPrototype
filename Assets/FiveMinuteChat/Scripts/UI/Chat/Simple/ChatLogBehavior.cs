using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using FiveMinutes.Model;
using FiveMinutes.Model.Messages.Server;
using UnityEngine;
using UnityEngine.UI;

namespace FiveMinuteChat.UI.Simple
{
    [RequireComponent(typeof(Text))]
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
        
        private readonly Queue<ChatEntry> _chatEntries = new Queue<ChatEntry>();

        private Text _textfield;
        private bool _needsUpdate;
        public ChatConnectionBehavior Connection;
        public string ChannelName = "Global";
        private Guid _serverChatMessageCallbackId;
        private Guid _serverWhisperMessageCallbackId;
        
        void Start()
        {
            _textfield = GetComponentInChildren<Text>();
            if( !Connection )
            {
                Connection = GetComponentInParent<ChatConnectionBehavior>();
            }
            if( Connection )
            {
                _serverChatMessageCallbackId = Connection.Subscribe<ServerChatMessage>(OnChatMessageReceived);
                _serverWhisperMessageCallbackId = Connection.Subscribe<ServerWhisperMessage>(OnWhisperMessageReceived);
            }
            else
            {
                Logger.LogError($"FiveMinuteChat: No {nameof(ChatConnectionBehavior)} has been assigned to the Connection field. It must either be found as a parent of this GameObject or set explicitly via the Connection field on this behavior.");
            }
        }

        public void Init( string channelName, bool isSilenced )
        {
            ChannelName = channelName;
            if( !_textfield )
            {
                _textfield = GetComponentInChildren<Text>();
            }

            _textfield.text = string.Empty;
        }

        private void Update()
        {
            if(_needsUpdate)
            {
                _needsUpdate = false;
                UpdateChatLog();
            }
        }

        private void OnDestroy()
        {
            if( Connection )
            {
                Connection.Unsubscribe(_serverChatMessageCallbackId);
                Connection.Unsubscribe(_serverWhisperMessageCallbackId);
            }
        }

        private void OnApplicationQuit()
        {
            if( Connection )
            {
                Connection.Unsubscribe(_serverChatMessageCallbackId);
                Connection.Unsubscribe(_serverWhisperMessageCallbackId);
            }
        }

        private void OnChatMessageReceived( ServerChatMessage message )
        {
            if( message.ChannelName == ChannelName && 
                message.Content.Length > 1 )
            {
                if( _chatEntries.Any( ce => message.MessageId != Guid.Empty && ce.MessageId == message.MessageId ) )
                {
                    return;
                }
                _chatEntries.Enqueue( new ChatEntry
                {
                    Type = ChatEntryType.ChatMessage,
                    MessageId = message.MessageId,
                    FromUser = message.FromUser,
                    SentAt = message.SentAt,
                    Content = message.Content
                } );
            }
            _needsUpdate = true;
        }

        private void OnWhisperMessageReceived( ServerWhisperMessage message )
        {
            if( gameObject.activeInHierarchy && 
                message.Content.Length > 1 )
            {
                if( _chatEntries.Any( ce => message.MessageId != Guid.Empty && ce.MessageId == message.MessageId ) )
                {
                    return;
                }
                _chatEntries.Enqueue( new ChatEntry
                {
                    Type = ChatEntryType.WhisperMessage,
                    FromUser = message.FromUser,
                    SentAt = message.SentAt,
                    Content = message.Content
                } );
            }

            _needsUpdate = true;
        }

        private int _maxLines = 15;
        private int _charactersPerRowEstimate = 35;

        private void UpdateChatLog()
        {
            lock( _chatEntries )
            { 
                var messages = _chatEntries
                    .OrderBy( e => e.SentAt )
                    .ToList();
                while( CalculateLines(messages) > _maxLines )
                {
                    _chatEntries.Dequeue();
                    messages = _chatEntries.ToList();
                }

                RenderText( messages );
            }
        }

        private int CalculateLines( List<ChatEntry> messages )
        {
            var lines = 0;
            for( var i = 0; i < messages.Count; i++ )
            {
                var messagesParts = messages[i].Content.Split( '\n' );
                for( var j = 0; j < messagesParts.Length; j++ )
                {
                    var part = messagesParts[j];
                    lines += (int)Math.Ceiling( (float)part.Length / _charactersPerRowEstimate );
                }
            }

            return lines;
        }

        private void RenderText( List<ChatEntry> messages )
        {
            var sb = new StringBuilder();
            foreach( var message in messages )
            {
                switch( message.Type )
                {
                    case ChatEntryType.ChatMessage:
                        sb.Append($"\n<size=18>{message.SentAt.ToString("T", CultureInfo.CurrentCulture)}</size> <color=#FFF545>{message.FromUser.Name}</color> > {message.Content}");
                        break;
                    case ChatEntryType.WhisperMessage:
                        sb.Append($"\n<size=18>{message.SentAt.ToString("T", CultureInfo.CurrentCulture)}</size> <i>(whisper) <color=#FFF545>{message.FromUser.Name}</color> > {message.Content}</i>");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            _textfield.text = sb.ToString();
        }
    }
}
