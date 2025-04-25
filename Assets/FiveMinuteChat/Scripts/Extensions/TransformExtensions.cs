using UnityEngine;

namespace FiveMinuteChat.Extensions
{
    public static class TransformExtensions
    {
        public static void Clear( this Transform self )
        {
            foreach( Transform child in self )
            {
                Object.Destroy( child.gameObject );
            }
        }
    }
    public static class GameObjectExtensions
    {
        public static void Clear( this GameObject self )
        {
            self.transform.Clear();
        }
    }
}
