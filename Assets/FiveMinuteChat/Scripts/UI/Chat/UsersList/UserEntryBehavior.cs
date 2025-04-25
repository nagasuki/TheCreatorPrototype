using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FiveMinutes.Model.Messages.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

namespace FiveMinuteChat.UI.UsersList
{
#if UNITY_EDITOR
    [ExecuteAlways]
#endif
    public class UserEntryBehavior : MonoBehaviour
    {
        private bool _needsUpdate;
        private ScrollRect _parentScrollRect => transform.GetComponentInParent<ScrollRect>();
        private VerticalLayoutGroup _parentLayoutGroup => transform.GetComponentInParent<VerticalLayoutGroup>();
        private RectTransform _selfRect => transform.GetComponent<RectTransform>();
        private RectTransform _outerContainerRect => transform.Find( "OuterContainer" ).GetComponent<RectTransform>();
        private RectTransform _innerContainerRect => transform.Find( "OuterContainer/InnerContainer" ).GetComponent<RectTransform>();
        private RectTransform _headerRect => _outerContainerRect.Find( "InnerContainer/Header" ).GetComponent<RectTransform>();
        private TextMeshProUGUI _headerText =>  _headerRect.GetComponentInChildren<TextMeshProUGUI>();
        private Image _statusIndicator =>  _headerRect.Find( "StatusIndicator" ).GetComponent<Image>();
        private RectTransform _statusRect => _outerContainerRect.Find( "InnerContainer/Status" ).GetComponent<RectTransform>();
        private TextMeshProUGUI _statusText =>  _statusRect.GetComponentInChildren<TextMeshProUGUI>();
        private RectTransform _contentRect => _outerContainerRect.Find( "InnerContainer/Content" ).GetComponent<RectTransform>();
        private TextMeshProUGUI _usernameText => _contentRect.GetComponentInChildren<TextMeshProUGUI>();
        private Button _sendWhisperButton => transform.Find( "SendWhisperButton" ).GetComponent<Button>();
        private ConnectionBehaviorBase _connection => transform.GetComponentInParent<ConnectionBehaviorBase>();

        private InputField _inputField( string channelName ) => GetComponentInParent<ChatConnectionWithUsersListBehavior>().transform.Find($"ChannelContainer/{channelName}/Chat Bottom Bar/Input Field").GetComponent<InputField>();
        
        public bool TriggerUpdate;
        
        public UserEntryBehavior SetUserInfo( 
            DateTime lastSeen,
            string channelName,
            string username,
            string userDisplayId,
            List<MetadataEntry> metadata )
        {
            _headerText.text = GetLastSeenAsString( lastSeen );
            _statusIndicator.color = GetOnlineIndicatorColor( lastSeen );
            _usernameText.text = username;
            // For demonstration purposes, we use the CustomStatus metadata to display a status message.
            // In the demo asset, that would mostly be empty, so we added a list of random statuses to populate the UI when it's not available
            // The Key for the metadata entry is arbitrary and can be chosen by the developer
            // Feel free to try it out by typing '/status This is my new status' in the chat and restart the session
            var statusText = metadata.FirstOrDefault( m => m.Key == "CustomStatus" )?.Value ?? GetRandomStatus();
            if( !string.IsNullOrWhiteSpace( statusText ) )
                _statusText.text = $"* {statusText} *";
            else
                _statusText.text = string.Empty;
            
            if(_connection.OwnDisplayId == userDisplayId)
            {
                _sendWhisperButton.gameObject.SetActive(false);
            }
            else
            {
                _sendWhisperButton.gameObject.SetActive(true);
                _sendWhisperButton.onClick.AddListener( () =>
                {
                    _inputField(channelName).text = $"/whisper {userDisplayId} ";
                } );
            }

            // If sorting is wanted, probably do so here
            _needsUpdate = true;
            
            return this;
        }

        private List<string> _randomStatuses = new (){"Out spelunking!", "Raid time!", "Victory dance!", "GG, EZ", "Noob slayer", "Loot hunting", "Boss fight!", "On a quest", "PvP battle", "Dungeon crawling", "Grinding XP", "AFK farming", "Top fragging", "Guild meeting", "Speedrun mode", "Co-op adventure", "Building base", "Crafting gear", "LFG!", "AFK, BRB..."};

        private string GetRandomStatus()
        {
            var rand = new Random();
            var id = rand.Next( -5, _randomStatuses.Count );
            if( id >= 0 )
            {
                return _randomStatuses[id];
            }

            return string.Empty;
        }

        private string GetLastSeenAsString( DateTime lastSeen )
        {
            var now = DateTime.UtcNow;
            if( now - lastSeen < TimeSpan.FromMinutes( 5 ) )
                return "Online";
            if( now - lastSeen < TimeSpan.FromMinutes( 15 ) )
                return "A few minutes back";
            if( now - lastSeen < TimeSpan.FromMinutes( 40 ) )
                return "Half an hour ago";
            if( now - lastSeen < TimeSpan.FromMinutes( 75 ) )
                return "About 1h ago";
            if( now - lastSeen < TimeSpan.FromHours( 3 ) )
                return "A couple of hours ago";
            if( now - lastSeen < TimeSpan.FromHours( 16 ) )
                return "Hours ago";
            if( now - lastSeen < TimeSpan.FromHours( 32 ) )
                return "About 1 day ago";
            if( now - lastSeen < TimeSpan.FromDays( 6 ) )
                return "About 1 week ago";
            if( now - lastSeen < TimeSpan.FromDays( 25 ) )
                return "About 1 month ago";
            return "A long time ago";
        }

        private Color GetOnlineIndicatorColor( DateTime lastSeen )
        {
            var now = DateTime.UtcNow;
            if( now - lastSeen < TimeSpan.FromMinutes( 5 ) )
                return new Color(0.3137255f,0.6784314f,0.4235294f);
            if( now - lastSeen < TimeSpan.FromHours( 1 ) )
                return new Color(0.8396226f,0.6118621f,0.2019847f);
            return new Color(0.3207547f,0.255696f,0.255696f);
        }

        private void Update()
        {
            if(TriggerUpdate != _needsUpdate || _needsUpdate )
            {
                StartCoroutine( UpdateRectSizes() );
            }
        }

        private IEnumerator UpdateRectSizes()
        {
            _needsUpdate = TriggerUpdate = false;
            
            _usernameText.ForceMeshUpdate();
            _statusText.ForceMeshUpdate();
            yield return null;
            var preferredHeaderValues =_headerText.GetPreferredValues( _headerText.text );
            var totalHeaderWidth = preferredHeaderValues.x - _headerText.rectTransform.offsetMax.x + _headerText.rectTransform.offsetMin.x + 15;
            _headerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, totalHeaderWidth );
            _headerRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, _headerRect.rect.height );

            if( !string.IsNullOrWhiteSpace( _statusText.text ) )
            {
                var preferredStatusValues = _statusText.GetPreferredValues( _statusText.text );
                var totalStatusWidth = preferredStatusValues.x - _statusText.rectTransform.offsetMax.x + _statusText.rectTransform.offsetMin.x;
                _statusRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, totalStatusWidth );
                _statusRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 55, _statusRect.rect.height );
                _selfRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100 );
            }
            else
            {
                _statusRect.gameObject.SetActive(false);
                _selfRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 75 );
            }

            var maxWidth = _selfRect.rect.width - 30;
            var preferredContentValues =_usernameText.GetPreferredValues( _usernameText.text, maxWidth, 0 );

            var preferredContentWidth = Math.Min( preferredContentValues.x, maxWidth ) + 
                _usernameText.rectTransform.offsetMin.x - _usernameText.rectTransform.offsetMax.x +
                _innerContainerRect.offsetMin.x - _innerContainerRect.offsetMax.x;
            var totalContentWidth = preferredContentWidth;
            
            _outerContainerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, totalContentWidth );
            _outerContainerRect.SetInsetAndSizeFromParentEdge( RectTransform.Edge.Right,_sendWhisperButton.gameObject.activeInHierarchy ? 42 : 2, totalContentWidth );
            _usernameText.ForceMeshUpdate();
            _statusText.ForceMeshUpdate();

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
    }
}
