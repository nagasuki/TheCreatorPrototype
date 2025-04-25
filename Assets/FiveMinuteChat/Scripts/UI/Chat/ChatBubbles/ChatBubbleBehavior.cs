using System;
using System.Collections;
using FiveMinutes.Model.Enums;
using FiveMinutes.Model.Messages.Server;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FiveMinuteChat.UI.ChatBubbles
{
#if UNITY_EDITOR
    [ExecuteAlways]
#endif
    public class ChatBubbleBehavior : MonoBehaviour
    {
        private enum Alignment
        {
            Left, 
            Right
        }

        public DateTime Timestamp;
        
        private Alignment _alignment;
        private bool _lastIsSelfAuthored;
        public bool IsSelfAuthored;
        private UserType _lastIsFromUserType;
        public UserType IsFromUserType;
        private bool _needsUpdate;
        public bool CanBeReported = true;
        public bool IsWhisper;
        
        private ScrollRect _parentScrollRect => transform.GetComponentInParent<ScrollRect>();
        private VerticalLayoutGroup _parentLayoutGroup => transform.GetComponentInParent<VerticalLayoutGroup>();
        private RectTransform _selfRect => transform.GetComponent<RectTransform>();
        private RectTransform _outerContainerRect => transform.Find( "OuterContainer" ).GetComponent<RectTransform>();
        private RectTransform _innerContainerRect => transform.Find( "OuterContainer/InnerContainer" ).GetComponent<RectTransform>();
        private RectTransform _headerRect => _outerContainerRect.Find( "InnerContainer/Header" ).GetComponent<RectTransform>();
        private TextMeshProUGUI _headerText =>  _headerRect.GetComponentInChildren<TextMeshProUGUI>();
        private RectTransform _contentRect => _outerContainerRect.Find( "InnerContainer/Content" ).GetComponent<RectTransform>();
        private TextMeshProUGUI _contentText => _contentRect.GetComponentInChildren<TextMeshProUGUI>();
        private Image _contentImage => _contentRect.GetComponent<Image>();
        private Button _reportButton => _headerRect.Find( "ReportButton" )?.GetComponent<Button>();
        private Button _respondToModButton => _contentRect.Find( "RespondToModeratorButton" )?.GetComponent<Button>();
        private ReportOverlayBehavior _reportOverlayRect => transform.GetComponentInParent<ChatLogBehavior>().transform.parent.GetComponentInChildren<ReportOverlayBehavior>( true );
        private InputField _inputField => transform.GetComponentInParent<ChatLogBehavior>().transform.parent.GetComponentInChildren<ChatInputFieldBehavior>( true ).GetComponent<InputField>();
        private ConnectionBehaviorBase _connection => transform.GetComponentInParent<ConnectionBehaviorBase>();
        
        void Start()
        {
            _alignment = IsSelfAuthored ? Alignment.Right : Alignment.Left;
            _lastIsSelfAuthored = IsSelfAuthored;
            SetAlignment( _alignment );
        }

        public ChatBubbleBehavior SetCanBeReported( bool value )
        {
            CanBeReported = value;
            return this;
        }

        public void SetMessage( ServerWhisperMessage message )
            => SetMessage( message.MessageId, message.SentAt, message.FromUser.Name, message.FromUser.UserType, message.ToUser.Name, message.Content, message.FromUser.DisplayId == _connection.OwnDisplayId, true );

        public void SetMessage( ServerChatMessage message )
            => SetMessage( message.MessageId, message.SentAt, message.FromUser.Name, message.FromUser.UserType, null, message.Content, message.FromUser.DisplayId == _connection.OwnDisplayId, false );

        private void SetMessage( Guid messageId, DateTime sentAt, string fromUsername, UserType fromUserType, string toUsername, string content, bool isSelfAuthored, bool isWhisper )
        {
            Timestamp = sentAt;
            IsWhisper = isWhisper;
            
            var headerText = $"{sentAt:yyyy-MM-dd HH:mm} | " + (fromUserType == UserType.Standard && !string.IsNullOrEmpty(toUsername) && isSelfAuthored ? $"to {toUsername}" : $"{fromUsername}");
            _headerText.text = headerText;
            _contentText.text = isWhisper && fromUserType == UserType.Standard ? $"Whisper> {content}" : content;
            IsSelfAuthored = isSelfAuthored;
            IsFromUserType = fromUserType;
            if( _reportButton )
            {
                if( CanBeReported && 
                    !isSelfAuthored && 
                    fromUserType == UserType.Standard )
                {
                    _reportButton.gameObject.SetActive( true );
                    _reportButton.onClick.RemoveAllListeners();
                    _reportButton.onClick.AddListener( () => ShowReportWindow( messageId, fromUsername, content ));
                    _reportButton.enabled = true;
                    _reportButton.GetComponent<Image>().enabled = true;
                }
                else
                {
                    _reportButton.gameObject.SetActive( false );
                }
            }

            if( _respondToModButton )
            {
                if( isWhisper && 
                    fromUserType == UserType.Moderator )
                {
                    _respondToModButton.gameObject.SetActive( true );
                    _respondToModButton.onClick.RemoveAllListeners();
                    _respondToModButton.onClick.AddListener( () =>
                    {
                        _inputField.text = $"/whisper-back {messageId} ";
                    });
                    _respondToModButton.enabled = true;
                    _respondToModButton.GetComponent<Image>().enabled = true;
                    
                }
                else
                {
                    _respondToModButton.gameObject.SetActive( false );
                }
            }

            UpdatePosition();
            _needsUpdate = true;
        }

        private void UpdatePosition()
        {
            if( transform.parent.childCount <= 1 )
            {
                // No sorting needed
                return;
            }
            
            transform.SetAsLastSibling();
            if( transform.parent.GetChild( transform.parent.childCount - 1 ).GetComponent<ChatBubbleBehavior>().Timestamp < Timestamp )
            {
                // This is the newest message, leave it where it is.
                return;
            }

            for( var i = transform.parent.childCount - 1; i >= 0; i-- )
            {
                // Loop until we find an older message
                if( transform.parent.GetChild( i ).GetComponent<ChatBubbleBehavior>().Timestamp < Timestamp )
                {
                    transform.SetSiblingIndex( i + 1 );
                    break;
                }
            }
        }

        private void Update()
        {
            if( _lastIsSelfAuthored != IsSelfAuthored )
            {
                SwapAlignment();
                _needsUpdate = true;
            }
            if( _lastIsFromUserType != IsFromUserType )
            {
                _lastIsFromUserType = IsFromUserType;
                _needsUpdate = true;
            }

            if( _needsUpdate )
            {
                StartCoroutine( UpdateRectSizes() );
            }
        }

        private IEnumerator UpdateRectSizes()
        {
            _needsUpdate = false;
            
            _contentText.ForceMeshUpdate();
            yield return null;
            var preferredHeaderValues =_headerText.GetPreferredValues( _headerText.text );
            var totalHeaderWidth = preferredHeaderValues.x - _headerText.rectTransform.offsetMax.x + _headerText.rectTransform.offsetMin.x;
            _headerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, totalHeaderWidth );
            _headerRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, _headerRect.rect.height );

            var maxWidth = _selfRect.rect.width - 150;
            var preferredContentValues =_contentText.GetPreferredValues( _contentText.text, maxWidth, 0 );

            if( preferredContentValues.x >= maxWidth )
            {
                _outerContainerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxWidth);
                _contentText.ForceMeshUpdate( forceTextReparsing: true );

                var renderedValues = _contentText.GetRenderedValues();
                _outerContainerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, renderedValues.y + 45 );
                _selfRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, renderedValues.y + 45 );
            }
            else
            {
                var preferredContentWidth = preferredContentValues.x + 
                    _contentText.rectTransform.offsetMin.x - _contentText.rectTransform.offsetMax.x +
                    _innerContainerRect.offsetMin.x - _innerContainerRect.offsetMax.x;
                var totalHeaderWidthIncMargins = totalHeaderWidth +
                    _contentText.rectTransform.offsetMin.x - _contentText.rectTransform.offsetMax.x;
                var totalContentWidth = Math.Max( totalHeaderWidthIncMargins, preferredContentWidth );
                
                _outerContainerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, totalContentWidth );
                _contentText.ForceMeshUpdate();

                var renderedValues = _contentText.GetRenderedValues();
                _outerContainerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, renderedValues.y + 45 );
                _selfRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, renderedValues.y + 45 );
            }

            yield return null;
            if( _parentLayoutGroup.spacing > 0.01f )
            {
                _parentLayoutGroup.spacing = 0f;
            }
            else
            {
                _parentLayoutGroup.spacing = 0.02f;
            }

            yield return null;
            _parentScrollRect.verticalNormalizedPosition = 0;
        }

        private void SwapAlignment()
        {
            SetAlignment( _alignment == Alignment.Left ? Alignment.Right : Alignment.Left );
            _lastIsSelfAuthored = IsSelfAuthored;
        }

        private void SetAlignment( Alignment alignment )
        {
            if( alignment == Alignment.Left )
            {
                _outerContainerRect.pivot = Vector2.up;
                _outerContainerRect.anchorMin = Vector2.up;
                _outerContainerRect.anchorMax = Vector2.up;
                _outerContainerRect.anchoredPosition = Vector2.right * 10;
                
                _headerRect.pivot = Vector2.up;
                _headerRect.anchorMin = Vector2.up;
                _headerRect.anchorMax = Vector2.up;
                _headerRect.anchoredPosition = Vector2.down * 10;

                _headerText.alignment = TextAlignmentOptions.Left;

                switch( IsFromUserType )
                {
                    case UserType.Standard:
                        _contentImage.color = IsWhisper 
                            ? new Color(0.4575472f, 1f, 6758742f) 
                            : new Color( 1f, 0.9825011f, 0.9294118f );
                        break;
                    case UserType.System:
                        _contentImage.color = new Color( 0.8962264f, 0.8425564f, 0.4777056f );
                        break;
                    case UserType.Moderator:
                        _contentImage.color = new Color( 0.495372f, 0.5494857f, 0.8679245f );
                        break;
                }

                if( _reportButton )
                {
                    var reportButtonRect = _reportButton.GetComponent<RectTransform>();
                    reportButtonRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, -30, reportButtonRect.rect.width);
                }
            }
            else
            {
                _outerContainerRect.pivot = Vector2.one;
                _outerContainerRect.anchorMin = Vector2.one;
                _outerContainerRect.anchorMax = Vector2.one;
                _outerContainerRect.anchoredPosition = Vector2.left * 10;
                
                _headerRect.pivot = Vector2.one;
                _headerRect.anchorMin = Vector2.one;
                _headerRect.anchorMax = Vector2.one;
                _headerRect.anchoredPosition = Vector2.down * 10;

                _headerText.alignment = TextAlignmentOptions.Right;

                if(IsWhisper)
                    _contentImage.color = new Color(0.5f, 0.9f, 0.8f);
                else
                    _contentImage.color = new Color(0.759434f, 0.8501809f, 1);
                

                if( _reportButton )
                {
                    var reportButtonRect = _reportButton.GetComponent<RectTransform>();
                    reportButtonRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, -30, reportButtonRect.rect.width);
                }                    
            }

            _alignment = alignment;
            _needsUpdate = true;
        }

        private void ShowReportWindow( Guid messageId, string fromUsername, string content )
        {
            _reportOverlayRect.ShowOverlay( messageId, fromUsername, content );            
        }
    }
}
