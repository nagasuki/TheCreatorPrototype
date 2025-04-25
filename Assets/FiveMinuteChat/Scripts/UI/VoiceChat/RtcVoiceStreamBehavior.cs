using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FiveMinutes.Model.Messages.Server;
using TMPro;
using UnityEngine;
#if !UNITY_WEBGL 
using Unity.WebRTC;
#endif
using UnityEngine.UI;

namespace FiveMinuteChat
{
    public class RtcVoiceStreamBehavior : MonoBehaviour
    {
#if !UNITY_WEBGL 
        private RTCPeerConnection _peerConnection;
        private MediaStream _sendStream;
        private MediaStream _receiveStream;
#endif

        public AudioClip AudioClipToStream;
        private AudioClip _clipInput;
#if !UNITY_WEBGL 
        private AudioStreamTrack _audioTrack;
        private readonly List<RTCRtpCodecCapability> _availableCodecs = new();
        private string _selectedMicrophone;
#endif

        private const int SamplingFrequency = 48000;
        private const int LengthSeconds = 1;
        private AudioSource _inputAudioSource;
        private AudioSource _outputAudioSource;

        private const int BufferSize = 256;

        public VoiceChatConnectionBehavior Connection;
        private TMP_Dropdown _microphoneDropdown => gameObject.GetComponentInChildren<TMP_Dropdown>();
        private TMP_Text _statisticsText => transform.Find("StatisticsPanel/StatisticsText")?.GetComponent<TMP_Text>();
        private Button _streamMicButton => transform.Find("StreamMicButton")?.GetComponent<Button>();
        private Button _streamFileButton => transform.Find("StreamFileButton")?.GetComponent<Button>();
        private Button _disconnectButton => transform.Find("DisconnectButton")?.GetComponent<Button>();
        void Start()
        {
            if( !Connection )
            {
                Connection = GetComponentInParent<VoiceChatConnectionBehavior>(false);
            }
            
            if( !Connection )
            {
                throw new MissingComponentException($"FiveMinuteChat: {nameof(VoiceChatConnectionBehavior)} is missing from {gameObject.name}");
            }
            if( !Connection.isActiveAndEnabled )
            {
                Logger.LogWarning($"FiveMinuteChat: Attached {nameof(VoiceChatConnectionBehavior)} is not enabled and will not start by itself");
                return;
            }
#if !UNITY_WEBGL 
            Connection.Subscribe<ServerP2PStreamNegotiationRequest>( OnP2PStreamInitRequest );
            Connection.Subscribe<ServerP2PStreamEndpointInfoRequest>( OnP2PStreamEndpointRequest );
            
            StartCoroutine(WebRTC.Update());
            StartCoroutine(LoopStatsCoroutine());
            
            if(_streamMicButton)
                _streamMicButton.interactable = false;
            if(_streamFileButton)
                _streamFileButton.interactable = false;
            if( _microphoneDropdown )
                _microphoneDropdown.interactable = false;
            if( _disconnectButton )
            {
                _disconnectButton.onClick.AddListener( Stop );
                _disconnectButton.gameObject.SetActive(false);
            }

            Connection.ConnectionAccepted += _ =>
            {
                if(_streamMicButton)
                    _streamMicButton.interactable = true;
                if( _microphoneDropdown )
                {
                    _microphoneDropdown.interactable = true;
                    _microphoneDropdown.options = Microphone.devices
                        .Select( d => new TMP_Dropdown.OptionData
                        {
                            text = d
                        } )
                        .ToList();
                }
                if( AudioClipToStream )
                {
                    if(_streamFileButton)
                        _streamFileButton.interactable = true;
                }
            };
            
            // best latency is default
            var audioConf = AudioSettings.GetConfiguration();
            audioConf.dspBufferSize = BufferSize;
            if (!AudioSettings.Reset(audioConf))
            {
                Logger.LogError("FiveMinuteChat: Failed updating audio settings with new buffer size");
            }

            var codecs = RTCRtpSender.GetCapabilities(TrackKind.Audio)
                .codecs
                .Where( c => c.mimeType != "audio/telephone-event" && c.mimeType != "audio/CN" );
            _availableCodecs.AddRange( codecs );
#endif
        }
        
#if !UNITY_WEBGL 
        private void OnP2PStreamInitRequest( ServerP2PStreamNegotiationRequest req )
        {
            var desc = new RTCSessionDescription()
            {
                type = Enum.Parse<RTCSdpType>(req.Type),
                sdp = req.ConnectionInfo
            };
            switch( desc.type )
            {
                case RTCSdpType.Offer:
                    PrepareListening();
                    OnOffer( desc );
                    break;
                case RTCSdpType.Answer:
                    OnAnswer( desc );
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnP2PStreamEndpointRequest( ServerP2PStreamEndpointInfoRequest req )
        {
            _peerConnection.AddIceCandidate( new RTCIceCandidate( new RTCIceCandidateInit
            {
                candidate = req.Candidate,
                sdpMid = req.SdpMid,
                sdpMLineIndex = req.SdpMLineIndex
            } ) );
        }
        
        public void StartStreamingMicrophone()
        {
            _sendStream = new MediaStream();
            
            _selectedMicrophone = _microphoneDropdown.options[_microphoneDropdown.value].text;
            Logger.Log($"FiveMinuteChat: Using microphone {_selectedMicrophone}");
            _clipInput = Microphone.Start(_selectedMicrophone, true, LengthSeconds, SamplingFrequency);
            // set the latency to “0” samples before the audio starts to play.
            while (!(Microphone.GetPosition(_selectedMicrophone) > 0)) { }

            if( _microphoneDropdown )
            {
                _microphoneDropdown.interactable = false;
            }
            
            var go = new GameObject { name = "input", transform = { parent = transform } };
            _inputAudioSource = go.AddComponent<AudioSource>();
            _inputAudioSource.loop = true;
            _inputAudioSource.clip = _clipInput;
            _inputAudioSource.Play();

            CreateSourceConnection();
        }
        
        public void StartStreamingFile()
        {
            _sendStream = new MediaStream();

            var go = new GameObject { name = "input", transform = { parent = transform } };
            _inputAudioSource = go.AddComponent<AudioSource>();
            _inputAudioSource.loop = true;
            _inputAudioSource.clip = AudioClipToStream;
            _inputAudioSource.Play();

            CreateSourceConnection();
        }

        private void CreateSourceConnection()
        {
            var configuration = GetSelectedSdpSemantics();
            _peerConnection = new RTCPeerConnection(ref configuration)
            {
                OnIceCandidate = candidate => Connection.SendCandidate( candidate ),
                OnNegotiationNeeded = () => StartCoroutine(InitializePeerToPeerConnection())
            };

            _audioTrack = new AudioStreamTrack(_inputAudioSource);
            _audioTrack.Loopback = true;
            _peerConnection.AddTrack(_audioTrack, _sendStream);
            _peerConnection.OnConnectionStateChange = state =>
            {
                Logger.Log($"FiveMinuteChat: Connection state changed to {state}");
                switch( state )
                {
                    case RTCPeerConnectionState.Disconnected:
                    case RTCPeerConnectionState.Failed:
                    case RTCPeerConnectionState.Closed:
                        Stop();
                        break;
                }
            };
            
            var transceiver = _peerConnection.GetTransceivers().First();
            
            var error = transceiver.SetCodecPreferences(_availableCodecs.ToArray());
            if( error != RTCErrorType.None )
                Logger.LogError( error.ToString() );
        }

        private void PrepareListening()
        {
            var go = new GameObject { name = "output", transform = { parent = transform } };
            _outputAudioSource = go.AddComponent<AudioSource>();
            
            _receiveStream = new MediaStream();
            _receiveStream.OnAddTrack += OnAddTrack;

            CreateListeningConnection();
        }

        private void CreateListeningConnection()
        {
            var configuration = GetSelectedSdpSemantics();
            _peerConnection = new RTCPeerConnection(ref configuration)
            {
                OnIceCandidate = candidate => Connection.SendCandidate(candidate),
                OnTrack = e => _receiveStream.AddTrack(e.Track),
            };

            var transceiver = _peerConnection.AddTransceiver(TrackKind.Audio);
            transceiver.Direction = RTCRtpTransceiverDirection.RecvOnly;
        }
        
        private IEnumerator InitializePeerToPeerConnection()
        {
            Logger.Log( "FiveMinuteChat: Initializing peer to peer connection..." );
            var offer = _peerConnection.CreateOffer();
            yield return offer;
            
            if (!offer.IsError)
            {
                StartCoroutine( OnOfferCreated( offer.Desc ) );
            }
            else
            {
                var error = offer.Error;
                OnSetSessionDescriptionError(ref error);
            }
        }

        private IEnumerator OnOfferCreated(RTCSessionDescription desc)
        {
            var op = _peerConnection.SetLocalDescription(ref desc);
            yield return op;

            if( op.IsError )
            {
                var error = op.Error;
                OnSetSessionDescriptionError( ref error );
                yield break;
            }

            Logger.Log($"FiveMinuteChat: Sent {desc.type}");
            Connection.SendNegotiationRequest( desc.type.ToString(), desc.sdp );
        }

        public void OnOffer(RTCSessionDescription desc) 
            => StartCoroutine(OnOfferCoroutine(desc));

        private IEnumerator OnOfferCoroutine(RTCSessionDescription desc)
        {
            Logger.Log($"FiveMinuteChat: Received {desc.type}");
            var op =_peerConnection.SetRemoteDescription( ref desc );
            yield return op;
            
            if (op.IsError)
            {
                var error = op.Error;
                OnSetSessionDescriptionError(ref error);
                yield break;
            }

            var answerOp = _peerConnection.CreateAnswer();
            yield return answerOp;
            
            if (answerOp.IsError)
            {
                var error = answerOp.Error;
                OnSetSessionDescriptionError( ref error );
                yield break;
            }

            StartCoroutine( OnAnswerCreated( answerOp.Desc ) );
        }

        private IEnumerator OnAnswerCreated( RTCSessionDescription desc )
        {
            var rdOp =_peerConnection.SetLocalDescription( ref desc );
            yield return rdOp;
            
            Logger.Log($"FiveMinuteChat: Sent {desc.type}");
            Connection.SendNegotiationRequest( desc.type.ToString(), desc.sdp );
            
            if( _disconnectButton )
                _disconnectButton.gameObject.SetActive(true);
        }

        public void OnAnswer(RTCSessionDescription desc)
            => StartCoroutine(OnAnswerCoroutine(desc));

        private IEnumerator OnAnswerCoroutine( RTCSessionDescription desc )
        {
            Logger.Log($"FiveMinuteChat: Received {desc.type}");
            var op =_peerConnection.SetRemoteDescription( ref desc );
            yield return op;
            
            if (op.IsError)
            {
                var error = op.Error;
                OnSetSessionDescriptionError( ref error );
            }
        }

        void OnAddTrack(MediaStreamTrackEvent e)
        {
            var track = e.Track as AudioStreamTrack;
            _outputAudioSource.SetTrack(track);
            _outputAudioSource.loop = true;
            _outputAudioSource.Play();
        }

        private void Stop()
        {
            Microphone.End(_selectedMicrophone);
            _clipInput = null;

            _audioTrack?.Dispose();
            _receiveStream?.Dispose();
            _sendStream?.Dispose();
            _peerConnection?.Dispose();
            _peerConnection = null;

            if(_inputAudioSource)
                _inputAudioSource.Stop();
            if(_outputAudioSource)
                _outputAudioSource.Stop();
        }

        private static RTCConfiguration GetSelectedSdpSemantics()
        {
            RTCConfiguration config = default;
            config.iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } };

            return config;
        }

        static void OnSetSessionDescriptionError(ref RTCError error)
        {
            Logger.LogError($"FiveMinuteChat: Error detail {error.message}");
        }

        private IEnumerator LoopStatsCoroutine()
        {
            while (true)
            {
                yield return StartCoroutine(UpdateStatsCoroutine());
                yield return new WaitForSeconds(1f);
            }
        }

        private IEnumerator UpdateStatsCoroutine()
        {
            RTCStatsReportAsyncOperation op = default;
            if( _inputAudioSource )
            {
                op = _peerConnection?.GetSenders().FirstOrDefault()?.GetStats();
            }
            else if( _outputAudioSource )
            {
                op = _peerConnection?.GetReceivers().FirstOrDefault()?.GetStats();
            }
            
            if( op is null )
            {
                yield break;
            }
            
            yield return op;
            if (op.IsError)
            {
                Logger.LogError( $"RTCRtpSender.GetStats() is failed {op.Error.errorType}" );
            }
            else
            {
                UpdateStatsPacketSize(op.Value);
            }
        }

        private RTCStatsReport lastOutboundReport;
        private RTCStatsReport lastInboundReport;
        private void UpdateStatsPacketSize(RTCStatsReport res)
        {
            foreach( var stats in res.Stats.Values )
            {
                if( stats is RTCOutboundRTPStreamStats currentOutbound )
                {
                    if( lastOutboundReport is not null &&
                        lastOutboundReport.TryGetValue( currentOutbound.Id, out RTCStats lastOutboundStats ) &&
                        lastOutboundStats is RTCOutboundRTPStreamStats lastOutbound )
                    {
                        var duration = (double)(currentOutbound.Timestamp - lastOutbound.Timestamp) / 1000000;
                        ulong bitrate = (ulong)(8 * (currentOutbound.bytesSent - lastOutbound.bytesSent) / duration);
                        _statisticsText.text = $"{bitrate / 1000.0f:f2} bps";
                    }
                    
                    this.lastOutboundReport = res;
                }
                else if( stats is RTCInboundRTPStreamStats currentInbound )
                {
                    if( lastInboundReport is not null &&
                        lastInboundReport.TryGetValue( currentInbound.Id, out RTCStats lastInboundStats ) &&
                        lastInboundStats is RTCInboundRTPStreamStats lastInbound )
                    {
                        var duration = (double)(currentInbound.Timestamp - lastInbound.Timestamp) / 1000000;
                        ulong bitrate = (ulong)(8 * (lastInbound.bytesReceived - lastInbound.bytesReceived) / duration);
                        _statisticsText.text = $"{bitrate / 1000.0f:f2} bps";
                    }

                    this.lastInboundReport = res;
                }
            }
        }
#endif
    }
}
