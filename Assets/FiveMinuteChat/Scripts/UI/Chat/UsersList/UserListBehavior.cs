using System.Collections.Generic;
using System.Linq;
using FiveMinuteChat.Extensions;
using FiveMinutes.Model;
using FiveMinutes.Model.Messages.Server;
using UnityEngine;
using UnityEngine.UI;

namespace FiveMinuteChat.UI.UsersList
{
    public class UserListBehavior : MonoBehaviour
    {
        private ScrollRect _scrollRect => transform.Find( "Tab/UsersList" ).GetComponent<ScrollRect>();
        
        private ConnectionBehaviorBase _connection;
        private UserEntryBehavior _entryTemplate;

        private Dictionary<string, List<UserInfo>> _userInfos = new();
        private TabbedChatBehavior _chatLogBehavior;
        private string _lastChannel;
        private float _lastUpdate;
        
        private void Start()
        {
            _connection = GetComponentInParent<ConnectionBehaviorBase>();
            _connection.Subscribe<ServerChannelInfoResponse>( OnServerChannelInfoResponse );
            _connection.Subscribe<ServerUserInfoResponse>( OnServerUserInfoResponse );
            _connection.Subscribe<ServerJoinChannelResponse>( OnServerJoinChannelResponse );
            _connection.Connect();

            _chatLogBehavior = _connection.gameObject.GetComponentInChildren<TabbedChatBehavior>();
            
            _entryTemplate = _scrollRect.content.GetChild( 0 ).GetComponent<UserEntryBehavior>();
            _entryTemplate.transform.SetParent(gameObject.transform);
            _entryTemplate.gameObject.SetActive(false);
            _scrollRect.content.Clear();
            
            _lastUpdate = Time.time + 2;
        }

        private void Update()
        {
            if(_lastChannel != _chatLogBehavior.CurrentChannelName && 
               _lastUpdate + 1 < Time.time)
            {
                _lastChannel = _chatLogBehavior.CurrentChannelName;
                _lastUpdate = Time.time;
                UpdateList();
            }
        }

        private void UpdateList()
        {
            if( !_userInfos.ContainsKey( _chatLogBehavior.CurrentChannelName ) )
            {
                return;
            }
            _scrollRect.content.Clear();
            foreach( var user in _userInfos[_chatLogBehavior.CurrentChannelName].OrderByDescending( ui => ui.LastSeen) )
            {
                var entry = Instantiate( _entryTemplate, _scrollRect.content );
                entry.SetUserInfo( user.LastSeen, _chatLogBehavior.CurrentChannelName, user.Name, user.DisplayId, user.Metadata );
                entry.gameObject.SetActive(true);
            }
        }
        
        private void OnServerChannelInfoResponse( ServerChannelInfoResponse channelInfo )
        {
            if( !_userInfos.ContainsKey( channelInfo.ChannelName ) )
                _userInfos.Add( channelInfo.ChannelName, new List<UserInfo>() );
            _userInfos[channelInfo.ChannelName] = channelInfo.Users;
            UpdateList();
        }

        private void OnServerJoinChannelResponse( ServerJoinChannelResponse msg )
        {
            _scrollRect.content.Clear();
            if(_connection is ChatConnectionBehavior ccb)
                ccb.GetChannelInfo(msg.ChannelInfo.Name);
        }

        private void OnServerUserInfoResponse( ServerUserInfoResponse userInfo )
        {
            // Left as an exercise for the developer
            // to update the specific user info in the channel member list
        }
    }
}
