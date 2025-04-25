using System.Collections.Generic;
using System.Linq;
using FiveMinutes.Model;
using UnityEngine;
using UnityEngine.UI;

namespace FiveMinuteChat.UI
{
    public class TabbedChatBehavior : MonoBehaviour
    {
        public ChatConnectionBehavior Connection;
        public Transform TabButtonList;
        public Transform TabContainer;
        public List<Color> ActiveTabBackgroundColors;
        public List<Color> InactiveTabBackgroundColors;
        public Color ActiveTabTextColor;
        public Color InactiveTabTextColor;

        public ColorBlock ActiveTabCloseButtonColors;
        public ColorBlock InactiveTabCloseButtonColors;
        
        private int _currentColorIndex;
        private int NextColorIndex
        {
            get
            {
                var retVal = _currentColorIndex++;
                _currentColorIndex = _currentColorIndex >= ActiveTabBackgroundColors.Count ? 0 : _currentColorIndex;
                return retVal;
            }
        }
        private GameObject _minimizeButton;
        private GameObject _buttonTemplate;
        private GameObject _tabTemplate;
        private GameObject _loadingMessage;

        public string CurrentChannelName;
        
        private class ChatTab
        {
            public int Index;
            public GameObject Button;
            public GameObject Tab;
        }

        private readonly Dictionary<string,ChatTab> _joinedChannels = new();

        void Awake()
        {
            if( !Connection )
            {
                Connection = GetComponentInParent<ChatConnectionBehavior>();
            }
        }

        void Start()
        {
            if (!TabButtonList)
            {
                throw new MissingComponentException($"{nameof(TabbedChatBehavior)} requires the TabButtonList ({nameof(GameObject)}) field to be set.");
            }
            if (!TabContainer)
            {
                throw new MissingComponentException($"{nameof(TabbedChatBehavior)} requires the TabContainer ({nameof(GameObject)}) field to be set.");
            }
            if (Connection)
            {
                Connection.ChannelJoined += OnChannelJoined;
                Connection.ChannelLeft += OnChannelLeft;
            }

            _minimizeButton = GameObject.Find( "MinimizeButton");
            if( _minimizeButton )
            {
                _minimizeButton.SetActive(false);
            }
            _buttonTemplate = TabButtonList.GetChild(0).gameObject;
            _buttonTemplate.SetActive(false);
            _tabTemplate = TabContainer.GetChild(0).gameObject;
            _tabTemplate.SetActive(false);
            _loadingMessage = TabContainer.GetChild(1).gameObject;
            _loadingMessage.SetActive(true);
        }

        private void OnApplicationQuit()
        {
            if (Connection)
            {
                Connection.ChannelJoined -= OnChannelJoined;
                Connection.ChannelLeft -= OnChannelLeft;
            }
        }

        private void OnChannelJoined(ChannelInfo channelInfo, bool canLeave, bool isSilenced)
        {
            if (_joinedChannels.ContainsKey(channelInfo.Name))
            {
                Logger.Log($"FiveMinuteChat: Got {nameof(Connection.ChannelJoined)} event for channel {channelInfo.Name} but already joined it previously. Ignoring...");
                return;
            }
           
            var ci = NextColorIndex;

            _loadingMessage.SetActive(false);
            
            var newTabButton = Instantiate(_buttonTemplate,TabButtonList);
            newTabButton.SetActive(true);
            newTabButton.GetComponentInChildren<Text>().text = channelInfo.Name;
            newTabButton.GetComponentInChildren<Text>().color = ActiveTabTextColor;
            var closeButton = newTabButton.transform.Find( "Close Button" ).GetComponent<Button>();
            if( canLeave )
            {
                closeButton.colors = ActiveTabCloseButtonColors;
                closeButton.onClick.AddListener( () => { Connection.Send($"/leave {channelInfo.Name}", string.Empty); } );
            }
            else
            {
                closeButton.gameObject.SetActive( false );
            }

            newTabButton.GetComponent<Button>().onClick.AddListener(() => ActivateChannel(channelInfo.Name));
            newTabButton.GetComponent<Image>().color = ActiveTabBackgroundColors[ci];
            
            var newTab = Instantiate(_tabTemplate,TabContainer);
            newTab.SetActive(true);
            newTab.name = channelInfo.Name;
            var inputField = newTab.GetComponentInChildren<ChatInputFieldBehavior>();
            if(inputField)
                newTab.GetComponentInChildren<ChatInputFieldBehavior>().ChannelName = channelInfo.Name;
            newTab.GetComponentInChildren<IChatLogBehavior>().Init( channelInfo.Name, isSilenced );
            
            foreach (var channelKv in _joinedChannels)
            {
                channelKv.Value.Tab.SetActive(false);
                channelKv.Value.Button.GetComponentInChildren<Text>().color = InactiveTabTextColor;
                channelKv.Value.Button.transform.Find("Close Button").GetComponent<Button>().colors = InactiveTabCloseButtonColors;
                channelKv.Value.Button.GetComponent<Image>().color = InactiveTabBackgroundColors[channelKv.Value.Index];
            }

            if( _minimizeButton )
            {
                _minimizeButton.SetActive( true );
            }
            
            _joinedChannels.Add(channelInfo.Name, new ChatTab
            {
                Index = ci,
                Button = newTabButton,
                Tab = newTab
            });
            CurrentChannelName = channelInfo.Name;
        }

        private void OnChannelLeft(string channelName)
        {
            if (_joinedChannels.ContainsKey(channelName))
            {
                Destroy(_joinedChannels[channelName].Button);
                Destroy(_joinedChannels[channelName].Tab);
                _joinedChannels.Remove(channelName);
            }

            if (_joinedChannels.Any())
            {
                var kv = _joinedChannels.First();
                kv.Value.Tab.SetActive(true);
            }
        }

        private void ActivateChannel( string channelName )
        {
            foreach (var channel in _joinedChannels)
            {
                if (channel.Key == channelName)
                {
                    CurrentChannelName = channelName;
                    channel.Value.Tab.SetActive(true);
                    channel.Value.Button.GetComponent<Image>().color = ActiveTabBackgroundColors[channel.Value.Index];
                    channel.Value.Button.transform.Find("Close Button").GetComponent<Button>().colors = ActiveTabCloseButtonColors;
                    channel.Value.Button.GetComponentInChildren<Text>().color = ActiveTabTextColor;
                }
                else
                {
                    channel.Value.Tab.SetActive(false);
                    channel.Value.Button.GetComponent<Image>().color = InactiveTabBackgroundColors[channel.Value.Index];
                    channel.Value.Button.transform.Find("Close Button").GetComponent<Button>().colors = InactiveTabCloseButtonColors;
                    channel.Value.Button.GetComponentInChildren<Text>().color = InactiveTabTextColor;
                }
            }
        }
    }
}
