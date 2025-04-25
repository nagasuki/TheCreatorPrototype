using System;

namespace FiveMinuteChat.Model
{
    [Serializable]
    public class BackendInfo
    {
        public string ConnectorType;
        public string Name;
        public string Endpoint;
        public int Port;
    }
}