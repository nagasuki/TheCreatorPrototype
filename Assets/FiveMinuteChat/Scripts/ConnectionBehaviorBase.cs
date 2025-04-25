using System;
using System.Linq;
using System.Threading.Tasks;
using FiveMinuteChat.Connectors;
using FiveMinuteChat.Enums;
using FiveMinuteChat.Helpers;
using FiveMinuteChat.Interfaces;
using FiveMinuteChat.Model;
using FiveMinutes.Model.Messages;
using FiveMinutes.Model.Messages.Server;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FiveMinuteChat
{
    public abstract partial class ConnectionBehaviorBase : MonoBehaviour
    {
        private class AcceptAllCertificateHandler : CertificateHandler
        {
            protected override bool ValidateCertificate(byte[] certificateData) => true;
        }
        
        public event OnConnectionAccepted ConnectionAccepted;

        private const string DiscoveryUrl = "https://api.fiveminutes.io/discover";

        [HideInInspector]
        public BackendInfos AvailableBackends = new ();
        
        [HideInInspector]
        [SerializeField] 
        public string SelectedServerName;
        [SerializeField] 
        public string ApplicationName;
        [SerializeField] 
        public string ApplicationSecret;
        [SerializeField] 
        public ConnectorType PreferredTransport = ConnectorType.SignalRCore;
        [SerializeField] 
        public string UserId;
        [SerializeField] 
        public bool AutoConnect;
        [SerializeField] 
        public bool DontDestroyOnLoadOverride = true;
        [SerializeField] 
        public LogLevel LogLevel = LogLevel.Debug;

        public string OwnDisplayId { get; private set; }
        
        protected IConnector Connector;

        protected virtual void Awake()
        {
            if( DontDestroyOnLoadOverride )
            {
                DontDestroyOnLoad( this );
            }
            Application.runInBackground = true;

            FiveMinutes.Telepathy.Logger.Log = Logger.Log;
            FiveMinutes.Telepathy.Logger.LogWarning = Logger.LogWarning;
            FiveMinutes.Telepathy.Logger.LogError = Logger.LogError;
            Logger.CurrentLogLevel = LogLevel;
            
#if UNITY_EDITOR
            if( Application.isEditor 
                && !Application.isPlaying &&
                string.IsNullOrEmpty(ApplicationName) && 
                string.IsNullOrEmpty(ApplicationSecret) &&
                EditorPrefs.HasKey($"FiveMinuteChat:{nameof(ApplicationName)}") &&
                EditorPrefs.HasKey($"FiveMinuteChat:{nameof(ApplicationSecret)}"))
            {
                ApplicationName = EditorPrefs.GetString(nameof(ApplicationName));
                ApplicationSecret = EditorPrefs.GetString(nameof(ApplicationSecret));
            }
#endif
            
#if UNITY_WEBGL
            PreferredTransport = ConnectorType.SignalRCore;

            var cl = GameObject.Find("WebGLCallbackListener");
            if( cl == null || cl.GetComponent<WebGLCallbackListener>() == null )
            {
                var go = new GameObject("WebGLCallbackListener");
                go.AddComponent<WebGLCallbackListener>();
            }
#else 
            if( (int)PreferredTransport > Enum.GetValues( typeof(ConnectorType) ).Cast<int>().Max() )
            {
                PreferredTransport = ConnectorType.Tcp;
            }
#endif
            switch( PreferredTransport )
            {
#if !UNITY_WEBGL                
                case ConnectorType.Tcp:
                    Connector = gameObject.AddComponent<TcpConnector>();
                    break;
#endif
                case ConnectorType.SignalRCore:
                    Connector = gameObject.AddComponent<SignalRCoreConnector>();
                    break;
#if FiveMinuteChat_BestHttpEnabled
                case ConnectorType.SignalRBestHttp2:
                    Connector = gameObject.AddComponent<BestHttpSignalRConnector>();
                    break;
#endif
                default:
                    throw new ArgumentOutOfRangeException($"Unknown value of parameter {nameof(PreferredTransport)}: {PreferredTransport}");
            }

            InitConnectorWithMessageHandler();
        }

        protected abstract void InitConnectorWithMessageHandler();

        private void Start()
        {
            var userId =
                string.IsNullOrWhiteSpace(UserId) ? 
                    SystemInfo.deviceUniqueIdentifier :
                    UserId;

            Connector.SetCredentials(ApplicationName, ApplicationSecret, userId); 
            Connector.ConnectionAccepted += message => ConnectionAccepted?.Invoke(message);

            Subscribe<ServerWelcome>( welcome =>
            {
                OwnDisplayId = welcome.DisplayId;
            } );
            Subscribe<ServerSetUsernameResponse>( setUsernameResponse =>
            {
                OwnDisplayId = setUsernameResponse.DisplayId;
            } );
            
            OnStart();
        }

        protected abstract void OnStart();

        public Guid Subscribe<T>( Action<T> callback ) where T : MessageBase
            => Connector.Subscribe( callback );

        public void Unsubscribe( Guid callbackId )
            => Connector?.Unsubscribe( callbackId );

        public async void Connect() 
            => await Retryer.RetryUntilAsync( () => RunServerDiscovery(), 500, 4 );

        public void Disconnect() 
            => Connector?.Disconnect( false );
        
        private async Task<bool> RunServerDiscovery()
        {
            Logger.Log("FiveMinuteChat: Discovering servers...");
            if (await DiscoverServers())
            {
                switch( PreferredTransport )
                {
#if !UNITY_WEBGL
                    case ConnectorType.Tcp:
                        SelectedServerName = AvailableBackends.Backends.Single( s => s.Name == "europe-tcp" ).Name;
                        break;
#endif
                    case ConnectorType.SignalRCore:
#if FiveMinuteChat_BestHttpEnabled
                    case ConnectorType.SignalRBestHttp2:
#endif
                        SelectedServerName = AvailableBackends.Backends.Single( s => s.Name == "europe-signalr" ).Name;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"Unknown value of parameter {nameof(PreferredTransport)}: {PreferredTransport}");
                }
                
                ConnectToSelectedServer();
                return true;
            }

            Logger.Log("FiveMinuteChat: Servers discovery failed!");
            return false;
        }

        private void ConnectToSelectedServer()
        {
            var backend = AvailableBackends.Backends.SingleOrDefault(b => b.Name == SelectedServerName);
            if (backend == default)
            {
                Logger.LogError($"FiveMinuteChat: List of available servers does not contain the selected server {SelectedServerName}!");
                return;
            }

            Connector.Connect(backend.Endpoint, backend.Port);
        }
        
        public async Task<bool> DiscoverServers()
        {
            using (var req = UnityWebRequest.Get( DiscoveryUrl ))
            {
                req.certificateHandler = new AcceptAllCertificateHandler();
                var operation = req.SendWebRequest();
                while (!operation.isDone)
                {
                    Logger.Log("FiveMinuteChat: Waiting for server discovery response...");
                    await AsyncHelper.Delay( 500 );
                }
                Logger.Log("FiveMinuteChat: Got server discovery response!");
                return ServerDiscoveryResult( req );
            }
        }
        
        private bool ServerDiscoveryResult(UnityWebRequest req)
        {
            if (req.result == UnityWebRequest.Result.Success)
            {
                AvailableBackends = JsonUtility.FromJson<BackendInfos>(req.downloadHandler.text);
                return true;
            }
            Logger.LogWarning($"FiveMinuteChat: Unable to discover remote servers: {req.error}");
            return false;
        }
        
        void OnApplicationQuit()
        {
            // the client/server threads won't receive the OnQuit info if we are
            // running them in the Editor. they would only quit when we press Play
            // again later. this is fine, but let's shut them down here for consistency
            Connector?.Disconnect( false);
        }
    }
}
