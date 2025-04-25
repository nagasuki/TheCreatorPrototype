using System;
using UnityEngine;

namespace FiveMinuteChat
{
    public enum LogLevel
    {
        Debug,
        Warning,
        Error,
        None
    }

    public static class Logger
    {
        public static LogLevel CurrentLogLevel { get; set; } = LogLevel.Debug;

        public static Action<string> Log = ( str ) =>
        {
            if( CurrentLogLevel <= LogLevel.Debug )
            {
                Debug.Log( str );
            }
        };

        public static Action<string> LogWarning = ( str ) =>
        {
            if( CurrentLogLevel <= LogLevel.Warning )
            {
                Debug.Log( str );
            }
        };

        public static Action<string> LogError = ( str ) =>
        {
            if( CurrentLogLevel <= LogLevel.Error )
            {
                Debug.Log( str );
            }
        };
    }
}
