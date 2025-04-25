using FiveMinutes.Model.Messages;

namespace FiveMinuteChat.Interfaces
{
    public interface IConnectorClient
    {
        bool Connected { get; }
        void Connect( string backendEndpoint, int backendPort );
        void Disconnect();
        bool Send( MessageBase message, bool shouldEncrypt = true );
        bool Send(byte[] data, bool shouldEncrypt = true);
    }
}
