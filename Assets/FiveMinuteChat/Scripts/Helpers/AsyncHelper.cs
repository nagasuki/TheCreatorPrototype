using System.Threading.Tasks;
using UnityEngine;

namespace FiveMinuteChat.Helpers
{
    public static class AsyncHelper
    {
        public static async Task Delay( int ms)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            await DelayAsync(0.01f * ms);
#else
            await Task.Delay(ms);
#endif
        }
        
        private static async Task DelayAsync(float secondsDelay)
        {
            float startTime = Time.time;
            while (Time.time < startTime + secondsDelay) await Task.Yield();
        }
    }
}
