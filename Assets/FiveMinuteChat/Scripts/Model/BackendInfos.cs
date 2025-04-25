using System;

namespace FiveMinuteChat.Model
{
    [Serializable]
    public class BackendInfos
    {
        public string ApiEndpoint;
        public BackendInfo[] Backends = new BackendInfo[0];
    }
}
