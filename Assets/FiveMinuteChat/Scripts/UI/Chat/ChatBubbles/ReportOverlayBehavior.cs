using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FiveMinuteChat.UI.ChatBubbles
{
    public class ReportOverlayBehavior : MonoBehaviour
    {
        private Guid _currentMessageId;
        private TextMeshProUGUI _message => transform.Find( "Container/Message" ).GetComponent<TextMeshProUGUI>();
        private TextMeshProUGUI _author => transform.Find( "Container/Author" ).GetComponent<TextMeshProUGUI>();
        private TMP_InputField _commentTextArea => transform.Find( "Container/InputField (TMP)" ).GetComponent<TMP_InputField>();

        private Button _sendButton => transform.Find( "Container/Send Button" ).GetComponent<Button>();
        private Button _cancelButton => transform.Find( "Container/Cancel Button" ).GetComponent<Button>();

        private ChatConnectionBehavior _chatConnection => transform.GetComponentInParent<ChatConnectionBehavior>();
        private ChatInputFieldBehavior _inputFieldBehavior => transform.parent.GetComponentInChildren<ChatInputFieldBehavior>( true );

        private void Start()
        {
            _sendButton.onClick.AddListener(Send);
            _cancelButton.onClick.AddListener(CloseView);
        }

        public void ShowOverlay( Guid messageId, string fromUsername, string content )
        {
            _inputFieldBehavior.SetAutoSubmit( false );
            _currentMessageId = messageId;
            _author.text = fromUsername;
            _message.text = content;
            gameObject.SetActive( true );
            _commentTextArea.ActivateInputField();
            _commentTextArea.Select();
        }

        private void Send()
        {
            _chatConnection.SendMessageReport( _currentMessageId, _commentTextArea.text);
            CloseView();
        }

        private void CloseView()
        {
            _inputFieldBehavior.SetAutoSubmit( true );
            gameObject.SetActive( false );
            _commentTextArea.text = string.Empty;
        }
    }
}
