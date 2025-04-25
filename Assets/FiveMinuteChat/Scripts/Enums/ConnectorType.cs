namespace FiveMinuteChat.Enums
{
    public enum ConnectorType
    {
#if !UNITY_WEBGL
        Tcp,
#endif
        SignalRCore,
#if FiveMinuteChat_BestHttpEnabled
        SignalRBestHttp2,
#endif
    }
}
