#if UNITY_EDITOR
using System;
using System.Globalization;
using System.Threading.Tasks;
using FiveMinuteChat.Enums;
using FiveMinuteChat.Helpers;
using FiveMinuteChat.Model;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace FiveMinuteChat
{
    public static class GUILayoutUtils
    {
        public static bool LinkLabel( string labelText, Color labelColor, Vector2 contentOffset, int fontSize )
        {
            var style = EditorStyles.label;
            var originalColor = style.normal.textColor;
            var originalContentOffset = style.contentOffset;
            var originalSize = style.fontSize;

            style.normal.textColor = labelColor;
            style.contentOffset = contentOffset;
            style.fontSize = fontSize;

            var rect = new Rect( 15, 20, 200, 30 );
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
            try
            {
                return GUI.Button( rect, labelText, style );
            }
            finally
            {
                style.normal.textColor = originalColor;
                style.contentOffset = originalContentOffset;
                style.fontSize = originalSize;
            }
        }

        public static bool LinkLabel( string labelText, Color labelColor, Vector2 contentOffset, int fontSize, string webAddress )
        {
            if( LinkLabel( labelText, labelColor, contentOffset, fontSize ) )
            {
                try
                {
                    Application.OpenURL( @webAddress );
                    return true;
                }
                catch
                {
                    Logger.LogError( "FiveMinuteChat: Could not open URL. Please check your network connection and ensure the web address is correct." );
                    EditorApplication.Beep();
                }
            }

            return false;
        }
    }

    [CustomEditor( typeof(ConnectionBehaviorBase), true)]
    public class ConnectionBehaviorBaseEditor : Editor
    {
        private class AcceptAllCertificateHandler : CertificateHandler
        {
            protected override bool ValidateCertificate( byte[] certificateData ) => true;
        }

        private static string _generatedMessage = "-- use device id --";
        private static string _trialRequestedKeyName = "TrialRequestedAt";
        private bool _isRunningRequests;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var targetBehavior = (ConnectionBehaviorBase)target;

            GUILayout.Label("Have questions? Need a feature?", new GUIStyle(){fontSize = 16, normal = {textColor = Color.white}});
            GUILayoutUtils.LinkLabel( "Join us on Discord!", new Color( 0.2f, 0.5f, 1 ), Vector2.zero, 20, "http://discord.gg/2GjjCyNtns" );

            GUILayout.Space( 25 );
            if( GUILayout.Button( "Request application id" ) )
            {
                if( EditorPrefs.HasKey( _trialRequestedKeyName ) &&
                    DateTime.TryParse( EditorPrefs.GetString( _trialRequestedKeyName ), CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind, out var dt ) &&
                    DateTime.UtcNow < dt.AddHours( 24 ) )
                {
                    EditorUtility.DisplayDialog( "Please wait...",
                        "A new application cannot be created before 24 hours have passed since your last application was created. This is in order to reduce spam requests to our services.",
                        "Ok" );
                }
                else
                {
                    RequestTrialApplication( targetBehavior );
                }
            }

            if( GUILayout.Button( "Restore last application id" ) )
            {
                if( EditorPrefs.HasKey( $"FiveMinuteChat:{nameof(targetBehavior.ApplicationName)}" ) &&
                    EditorPrefs.HasKey( $"FiveMinuteChat:{nameof(targetBehavior.ApplicationSecret)}" ) )
                {
                    targetBehavior.ApplicationName =
                        EditorPrefs.GetString( $"FiveMinuteChat:{nameof(targetBehavior.ApplicationName)}" );
                    targetBehavior.ApplicationSecret =
                        EditorPrefs.GetString( $"FiveMinuteChat:{nameof(targetBehavior.ApplicationSecret)}" );
                }
                else
                {
                    EditorUtility.DisplayDialog( "No credentials found!",
                        "No saved credentials could be found.\nHave you requested an Application Id before? !", "Ok" );
                }
            }
            
            var dontDestroyOnLoad = targetBehavior.DontDestroyOnLoadOverride;
            targetBehavior.DontDestroyOnLoadOverride = EditorGUILayout.Toggle( "Don't destroy on load", dontDestroyOnLoad );

            var previousApplicationName = targetBehavior.ApplicationName;
            targetBehavior.ApplicationName = EditorGUILayout.TextField( "Application id", previousApplicationName );
            var previousApplicationSecret = targetBehavior.ApplicationSecret;
            targetBehavior.ApplicationSecret =
                EditorGUILayout.TextField( "Application secret", previousApplicationSecret );

            var originalUserId = targetBehavior.UserId;
            var userId = string.IsNullOrWhiteSpace( targetBehavior.UserId ) ? _generatedMessage : targetBehavior.UserId;
            targetBehavior.UserId = EditorGUILayout.TextField( "Unique user id", userId );
            if( targetBehavior.UserId == _generatedMessage )
            {
                targetBehavior.UserId = string.Empty;
            }

            GUILayout.Label("Connection Preferences", new GUIStyle(){fontSize = 14, normal = {textColor = Color.white}});

            var previousPreferredTransport = targetBehavior.PreferredTransport;
            targetBehavior.PreferredTransport = (ConnectorType)EditorGUILayout.EnumPopup( "Preferred transport", previousPreferredTransport );

            var autoConnect = targetBehavior.AutoConnect;
            targetBehavior.AutoConnect = EditorGUILayout.Toggle( "Auto-connect", autoConnect );
            
            var previousLogLevel = targetBehavior.LogLevel;
            targetBehavior.LogLevel = (LogLevel)EditorGUILayout.EnumPopup( "Log level", previousLogLevel );

            if( targetBehavior.ApplicationName != previousApplicationName ||
                targetBehavior.ApplicationSecret != previousApplicationSecret ||
                targetBehavior.UserId != originalUserId ||
                targetBehavior.AutoConnect != autoConnect ||
                targetBehavior.PreferredTransport != previousPreferredTransport ||
                targetBehavior.DontDestroyOnLoadOverride != dontDestroyOnLoad ||
                targetBehavior.LogLevel != previousLogLevel )
            {
                EditorUtility.SetDirty( targetBehavior );
            }

            serializedObject.ApplyModifiedProperties();
        }

        public async void RequestTrialApplication( ConnectionBehaviorBase targetBehavior )
        {
            if( _isRunningRequests )
            {
                Logger.LogWarning( "FiveMinuteChat: Already waiting for requests to finish." );
                return;
            }

            var progressId = Progress.Start( "Generating application credentials...", "Running server discovery...", Progress.Options.Indefinite );
            await AsyncHelper.Delay( 2000 );
            try
            {
                _isRunningRequests = true;
                if( await targetBehavior.DiscoverServers() )
                {
                    Progress.Start( "Generating application credentials...", "Requesting new application...",
                        Progress.Options.Indefinite, progressId );
                    if( await DoRequestTrialApplicationRequest( targetBehavior ) )
                    {
                        Logger.Log( "FiveMinuteChat: Application credentials were successfully generated." );

                        Progress.Finish( progressId );
                        EditorUtility.DisplayDialog( "Application generated.",
                            $"A new application was generated:\nApplication id: {targetBehavior.ApplicationName}\nApplication secret: {targetBehavior.ApplicationSecret}\n\nThese credentials have been filled in automatically on the behavior.\nPlease take note of these and keep them safe. A new application cannot be generated for the next 24 hours.",
                            "Ok" );
                    }
                }
                else
                {
                    var failMessage =
                        "Unable to look up API server endpoint. Trial application credentials could not be acquired. ";
                    EditorUtility.DisplayDialog( "Application generation failed!", failMessage, "Ok" );
                    Logger.LogError( $"FiveMinuteChat: {failMessage}" );
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                _isRunningRequests = false;
            }
        }

        private async Task<bool> DoRequestTrialApplicationRequest( ConnectionBehaviorBase targetBehavior )
        {
            using var req =
#if UNITY_2022_2_OR_NEWER
                UnityWebRequest.PostWwwForm(
#else
                UnityWebRequest.Post(
#endif
                    $"{targetBehavior.AvailableBackends.ApiEndpoint}/api/ext/applications/request-trial?accessToken=779c5553-a0fe-48d5-8afc-f3fd503ce208",
                    string.Empty );
            req.SetRequestHeader( "Content-Type", "application/json" );
            req.certificateHandler = new AcceptAllCertificateHandler();
            var operation = req.SendWebRequest();
            while( !operation.isDone )
            {
                await AsyncHelper.Delay( 100 );
            }

            return RequestTrialApplicationResult( req, targetBehavior );
        }

        private bool RequestTrialApplicationResult( UnityWebRequest req, ConnectionBehaviorBase targetBehavior )
        {
            if( req.result == UnityWebRequest.Result.Success )
            {
                var applicationInfo =
                    JsonUtility.FromJson<ApplicationInfo>(
                        req.downloadHandler.text.Replace( "application", "Application" ) );
                targetBehavior.ApplicationName = applicationInfo.ApplicationId;
                targetBehavior.ApplicationSecret = applicationInfo.ApplicationSecret;
                if( !Application.isPlaying )
                {
                    EditorPrefs.SetString( $"FiveMinuteChat:{nameof(targetBehavior.ApplicationName)}",
                        applicationInfo.ApplicationId );
                    EditorPrefs.SetString( $"FiveMinuteChat:{nameof(targetBehavior.ApplicationSecret)}",
                        applicationInfo.ApplicationSecret );
                    EditorPrefs.SetString( _trialRequestedKeyName, DateTime.UtcNow.ToString( "u" ) );
                }

                return true;
            }

            Logger.LogError( $"FiveMinuteChat: Trial application credentials could not be acquired: {req.error}" );
            return false;
        }
    }
}
#endif
