using System;
using System.Collections.Generic;
using UnityEngine;

namespace FiveMinuteChat.Helpers
{
    public class WebGLCallbackListener : MonoBehaviour
    {
        private readonly Dictionary<string, List<Action<string>>> _registeredCallbacks = new ();

        public void Subscribe<T>( string eventName, Action<T> callback ) where  T : class
        {
            if (!_registeredCallbacks.ContainsKey(eventName))
            {
                _registeredCallbacks.Add( eventName, new List<Action<string>>());
            }

            if( typeof(T) == typeof(string) )
            {
                _registeredCallbacks[eventName].Add( o => callback( o as T ));
            }
            else
            {
                _registeredCallbacks[eventName].Add( o =>
                {
                    Logger.Log( $"FiveMinuteChat: Deserializing string '{o}' as {typeof(T).Name}" );
                    callback( System.Text.Json.JsonSerializer.Deserialize<T>( o ) );
                } );
            }
        }

        public void On(string data)
        {
            // Logger.Log( $"FiveMinuteChat: Received event from Javascript: {data}" );
            var firstDelimiterIndex = data.IndexOf("-", StringComparison.Ordinal);
            if (firstDelimiterIndex < 0)
            {
                Logger.LogError($"FiveMinuteChat: Unable to handle event from Javascript. Payload is bad: '{data}'");
                return;
            }

            var eventName = data.Substring(0, firstDelimiterIndex);
            var payload = data.Substring(firstDelimiterIndex + 1, data.Length - firstDelimiterIndex - 1);
            var strippedPayload = payload.Replace( "\"$type\":\"FiveMinutes.Model.Messages.MessageContainer, Gateway.Contracts\",", "" );
            if (_registeredCallbacks.TryGetValue(eventName, out var callback))
            {
                foreach (var ev in callback)
                {
                    ev(strippedPayload);
                }
            }
        }
    }
}
