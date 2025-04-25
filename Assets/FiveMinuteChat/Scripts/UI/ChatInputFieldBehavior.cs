using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FiveMinuteChat.UI
{
    [RequireComponent(typeof(InputField))]
    public class ChatInputFieldBehavior : MonoBehaviour
    {
        private InputField _inputField;
        public ChatConnectionBehavior Connection;
        private bool _autoSubmit = true;
        public bool AutoSubmitOnFocusLost = true;
        public bool AutoRefocusOnFocusLost = true;
        
        public string ChannelName { get; set; }

        void Awake()
        {
            _inputField = GetComponentInChildren<InputField>();
            _inputField.onEndEdit.AddListener( str => FocusLost() );
        }

        void Start()
        {
            if( !Connection )
            {
                Connection = GetComponentInParent<ChatConnectionBehavior>();
            }
            
#if UNITY_2022_3_OR_NEWER
            if( !FindFirstObjectByType<EventSystem>() )
#else
            if( !FindObjectOfType<EventSystem>() )
#endif
            {
                Logger.LogWarning("FiveMinuteChat: No EventSystem detected! Please make sure your scene is correctly set up to handle UI input.");
            }
        }

        public void SetAutoSubmit( bool isEnabled )
        {
            if( isEnabled )
            {
                _inputField.ActivateInputField();
                _inputField.Select();
            }
            _autoSubmit = isEnabled;
        }

        public void FocusLost()
        {
            Submit();
        
            if( AutoRefocusOnFocusLost )
            {
                _inputField.ActivateInputField();
                _inputField.Select();
            }
        }

        public void Submit()
        {
            if( !_autoSubmit || !AutoSubmitOnFocusLost )
            {
                return;
            }
            
            var text = _inputField.text;

            if( Connection )
            {
                Connection.Send( text, ChannelName );
            }
            else
            {
                Logger.LogError($"FiveMinuteChat: No {nameof(ChatConnectionBehavior)} has been assigned. It must either be found as a parent of this GameObject or set explicitly via the Connection field on this behavior.");
            }
            
            _inputField.text = string.Empty;
        }
    }
}
