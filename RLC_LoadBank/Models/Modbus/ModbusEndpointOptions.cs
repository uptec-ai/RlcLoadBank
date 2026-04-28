using System;

namespace RLC_LoadBank.Models.Modbus
{
    public sealed class ModbusEndpointOptions
    {
        public ModbusEndpointOptions(
            string host,
            int port,
            byte unitIdentifier,
            int connectTimeoutMilliseconds = 3000,
            int receiveTimeoutMilliseconds = 3000,
            int sendTimeoutMilliseconds = 3000,
            bool autoConnect = true)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentException("Host is required.", nameof(host));
            }

            if (port <= 0 || port > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");
            }

            Host = host;
            Port = port;
            UnitIdentifier = unitIdentifier;
            ConnectTimeoutMilliseconds = connectTimeoutMilliseconds;
            ReceiveTimeoutMilliseconds = receiveTimeoutMilliseconds;
            SendTimeoutMilliseconds = sendTimeoutMilliseconds;
            AutoConnect = autoConnect;
        }

        public string Host { get; }
        public int Port { get; }
        public byte UnitIdentifier { get; }
        public int ConnectTimeoutMilliseconds { get; }
        public int ReceiveTimeoutMilliseconds { get; }
        public int SendTimeoutMilliseconds { get; }
        public bool AutoConnect { get; }
    }
}
