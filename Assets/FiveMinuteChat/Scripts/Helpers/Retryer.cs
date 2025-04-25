using System;
using System.Threading.Tasks;
using UnityEngine;

namespace FiveMinuteChat.Helpers
{
    public static class Retryer
    {
        public static async Task RetryUntilAsync( Func<Task<bool>> predicate, int intervalInMs, int maxAttempts = -1 )
        {
            var attempts = 0;
            while( attempts++ < maxAttempts && 
                   !await predicate.Invoke() )
            {
                await AsyncHelper.Delay( intervalInMs );
            }
        }
        
        public static async Task RetryUntilAsync<T>( Task action, int intervalInMs, int maxAttempts = -1 ) where T : Exception
        {
            var attempts = 0;
            while( attempts++ < maxAttempts )
            {
                try
                {
                    await action;
                }
                catch( T e )
                {
                    Logger.Log(e.GetType().Name);
                    // ignore, this exception is ok per definition
                }
                await AsyncHelper.Delay( intervalInMs );
            }
        }
    }
}
