using RLC_LoadBank.Models.Modbus;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RLC_LoadBank.Services.Modbus
{
    public sealed class ModbusTcpService : IModbusService
    {
        private readonly SemaphoreSlim _requestLock = new SemaphoreSlim(1, 1);
        private TcpClient _client;
        private NetworkStream _stream;
        private ushort _transactionId;
        private bool _disposed;

        public ModbusTcpService(ModbusProtocolDefinition protocol, ModbusEndpointOptions endpoint)
        {
            Protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        public ModbusEndpointOptions Endpoint { get; }
        public ModbusProtocolDefinition Protocol { get; }

        public bool IsConnected
        {
            get
            {
                return _client != null && _client.Connected && _stream != null;
            }
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (IsConnected)
            {
                return;
            }

            var client = new TcpClient
            {
                ReceiveTimeout = Endpoint.ReceiveTimeoutMilliseconds,
                SendTimeout = Endpoint.SendTimeoutMilliseconds
            };

            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkedSource.CancelAfter(Endpoint.ConnectTimeoutMilliseconds);

            await client.ConnectAsync(Endpoint.Host, Endpoint.Port, linkedSource.Token).ConfigureAwait(false);

            _client = client;
            _stream = client.GetStream();
        }

        public Task DisconnectAsync()
        {
            CloseConnection();
            return Task.CompletedTask;
        }

        public async Task<bool[]> ReadCoilsAsync(ushort startAddress, ushort count, CancellationToken cancellationToken = default)
        {
            return await ReadBitsAsync(0x01, startAddress, count, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool[]> ReadDiscreteInputsAsync(ushort startAddress, ushort count, CancellationToken cancellationToken = default)
        {
            return await ReadBitsAsync(0x02, startAddress, count, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ushort[]> ReadHoldingRegistersAsync(ushort startAddress, ushort count, CancellationToken cancellationToken = default)
        {
            return await ReadRegistersAsync(0x03, startAddress, count, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ushort[]> ReadInputRegistersAsync(ushort startAddress, ushort count, CancellationToken cancellationToken = default)
        {
            return await ReadRegistersAsync(0x04, startAddress, count, cancellationToken).ConfigureAwait(false);
        }

        public async Task WriteSingleCoilAsync(ushort address, bool value, CancellationToken cancellationToken = default)
        {
            var payload = new byte[4];
            BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(0, 2), address);
            BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(2, 2), value ? (ushort)0xFF00 : (ushort)0x0000);
            await SendRequestAsync(0x05, payload, cancellationToken).ConfigureAwait(false);
        }

        public async Task WriteSingleRegisterAsync(ushort address, ushort value, CancellationToken cancellationToken = default)
        {
            var payload = new byte[4];
            BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(0, 2), address);
            BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(2, 2), value);
            await SendRequestAsync(0x06, payload, cancellationToken).ConfigureAwait(false);
        }

        public async Task WriteMultipleCoilsAsync(ushort startAddress, bool[] values, CancellationToken cancellationToken = default)
        {
            if (values == null || values.Length == 0)
            {
                throw new ArgumentException("At least one coil value is required.", nameof(values));
            }

            var byteCount = (values.Length + 7) / 8;
            var payload = new byte[5 + byteCount];
            BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(0, 2), startAddress);
            BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(2, 2), (ushort)values.Length);
            payload[4] = (byte)byteCount;

            for (var index = 0; index < values.Length; index++)
            {
                if (values[index])
                {
                    payload[5 + (index / 8)] |= (byte)(1 << (index % 8));
                }
            }

            await SendRequestAsync(0x0F, payload, cancellationToken).ConfigureAwait(false);
        }

        public async Task WriteMultipleRegistersAsync(ushort startAddress, ushort[] values, CancellationToken cancellationToken = default)
        {
            if (values == null || values.Length == 0)
            {
                throw new ArgumentException("At least one register value is required.", nameof(values));
            }

            var payload = new byte[5 + (values.Length * 2)];
            BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(0, 2), startAddress);
            BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(2, 2), (ushort)values.Length);
            payload[4] = (byte)(values.Length * 2);

            for (var index = 0; index < values.Length; index++)
            {
                BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(5 + (index * 2), 2), values[index]);
            }

            await SendRequestAsync(0x10, payload, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ModbusPointValue> ReadPointAsync(ModbusPointDefinition point, CancellationToken cancellationToken = default)
        {
            if (point == null)
            {
                throw new ArgumentNullException(nameof(point));
            }

            switch (point.AddressSpace)
            {
                case ModbusAddressSpace.Coil:
                case ModbusAddressSpace.DiscreteInput:
                    var bits = point.AddressSpace == ModbusAddressSpace.Coil
                        ? await ReadCoilsAsync(point.StartAddress, point.Length, cancellationToken).ConfigureAwait(false)
                        : await ReadDiscreteInputsAsync(point.StartAddress, point.Length, cancellationToken).ConfigureAwait(false);

                    return new ModbusPointValue(point, DecodeBits(point, bits), bits, Array.Empty<ushort>(), DateTime.Now);

                case ModbusAddressSpace.HoldingRegister:
                case ModbusAddressSpace.InputRegister:
                    var registers = point.AddressSpace == ModbusAddressSpace.HoldingRegister
                        ? await ReadHoldingRegistersAsync(point.StartAddress, point.Length, cancellationToken).ConfigureAwait(false)
                        : await ReadInputRegistersAsync(point.StartAddress, point.Length, cancellationToken).ConfigureAwait(false);

                    return new ModbusPointValue(point, DecodeRegisters(point, registers), Array.Empty<bool>(), registers, DateTime.Now);

                default:
                    throw new NotSupportedException($"Unsupported Modbus address space: {point.AddressSpace}");
            }
        }

        public async Task WritePointAsync(ModbusPointDefinition point, object value, CancellationToken cancellationToken = default)
        {
            if (point == null)
            {
                throw new ArgumentNullException(nameof(point));
            }

            if (!point.IsWritable)
            {
                throw new InvalidOperationException($"Point '{point.Key}' is read only.");
            }

            switch (point.AddressSpace)
            {
                case ModbusAddressSpace.Coil:
                    await WriteSingleCoilAsync(point.StartAddress, Convert.ToBoolean(value), cancellationToken).ConfigureAwait(false);
                    break;

                case ModbusAddressSpace.HoldingRegister:
                    var registers = EncodeValue(point, value);
                    if (registers.Length == 1)
                    {
                        await WriteSingleRegisterAsync(point.StartAddress, registers[0], cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await WriteMultipleRegistersAsync(point.StartAddress, registers, cancellationToken).ConfigureAwait(false);
                    }
                    break;

                default:
                    throw new InvalidOperationException($"Point '{point.Key}' does not support writing in address space {point.AddressSpace}.");
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            CloseConnection();
            _requestLock.Dispose();
        }

        private async Task<bool[]> ReadBitsAsync(byte functionCode, ushort startAddress, ushort count, CancellationToken cancellationToken)
        {
            var payload = new byte[4];
            BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(0, 2), startAddress);
            BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(2, 2), count);

            var response = await SendRequestAsync(functionCode, payload, cancellationToken).ConfigureAwait(false);
            var byteCount = response[1];
            if (byteCount < (count + 7) / 8)
            {
                throw new IOException("Received invalid coil/discrete input response length.");
            }

            var bits = new bool[count];
            for (var index = 0; index < count; index++)
            {
                var responseByte = response[2 + (index / 8)];
                bits[index] = (responseByte & (1 << (index % 8))) != 0;
            }

            return bits;
        }

        private async Task<ushort[]> ReadRegistersAsync(byte functionCode, ushort startAddress, ushort count, CancellationToken cancellationToken)
        {
            var payload = new byte[4];
            BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(0, 2), startAddress);
            BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(2, 2), count);

            var response = await SendRequestAsync(functionCode, payload, cancellationToken).ConfigureAwait(false);
            var byteCount = response[1];
            if (byteCount != count * 2)
            {
                throw new IOException("Received invalid register response length.");
            }

            var registers = new ushort[count];
            for (var index = 0; index < count; index++)
            {
                registers[index] = BinaryPrimitives.ReadUInt16BigEndian(response.AsSpan(2 + (index * 2), 2));
            }

            return registers;
        }

        private async Task<byte[]> SendRequestAsync(byte functionCode, byte[] payload, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (!IsConnected)
            {
                if (!Endpoint.AutoConnect)
                {
                    throw new InvalidOperationException("Modbus client is not connected.");
                }

                await ConnectAsync(cancellationToken).ConfigureAwait(false);
            }

            await _requestLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var transactionId = unchecked(++_transactionId);
                var frameLength = (ushort)(payload.Length + 2);
                var request = new byte[8 + payload.Length];

                BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(0, 2), transactionId);
                BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(2, 2), 0);
                BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(4, 2), frameLength);
                request[6] = Endpoint.UnitIdentifier;
                request[7] = functionCode;
                Buffer.BlockCopy(payload, 0, request, 8, payload.Length);

                await _stream.WriteAsync(request.AsMemory(0, request.Length), cancellationToken).ConfigureAwait(false);

                var header = await ReadExactAsync(7, cancellationToken).ConfigureAwait(false);
                var responseTransactionId = BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(0, 2));
                var protocolId = BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(2, 2));
                var remainingLength = BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(4, 2));
                var unitId = header[6];

                if (responseTransactionId != transactionId)
                {
                    throw new IOException("Unexpected Modbus transaction identifier.");
                }

                if (protocolId != 0)
                {
                    throw new IOException("Unexpected Modbus protocol identifier.");
                }

                if (unitId != Endpoint.UnitIdentifier)
                {
                    throw new IOException("Unexpected Modbus unit identifier.");
                }

                var body = await ReadExactAsync(remainingLength - 1, cancellationToken).ConfigureAwait(false);
                if (body[0] == (functionCode | 0x80))
                {
                    throw new IOException($"Modbus exception response received. Function=0x{functionCode:X2}, Exception=0x{body[1]:X2}");
                }

                return body;
            }
            catch
            {
                CloseConnection();
                throw;
            }
            finally
            {
                _requestLock.Release();
            }
        }

        private async Task<byte[]> ReadExactAsync(int count, CancellationToken cancellationToken)
        {
            var buffer = new byte[count];
            var offset = 0;

            while (offset < count)
            {
                var bytesRead = await _stream.ReadAsync(buffer.AsMemory(offset, count - offset), cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    throw new IOException("Modbus server connection was closed.");
                }

                offset += bytesRead;
            }

            return buffer;
        }

        private static object DecodeBits(ModbusPointDefinition point, bool[] bits)
        {
            if (point.ValueType == ModbusValueType.Boolean && bits.Length == 1)
            {
                return bits[0];
            }

            return bits.ToArray();
        }

        private static object DecodeRegisters(ModbusPointDefinition point, ushort[] registers)
        {
            switch (point.ValueType)
            {
                case ModbusValueType.UInt16:
                    return registers[0] * point.ScaleFactor;

                case ModbusValueType.Int16:
                    return (short)registers[0] * point.ScaleFactor;

                case ModbusValueType.UInt32:
                    return ReadUInt32(registers, point.WordOrder) * point.ScaleFactor;

                case ModbusValueType.Int32:
                    return ReadInt32(registers, point.WordOrder) * point.ScaleFactor;

                case ModbusValueType.Float32:
                    return ReadFloat32(registers, point.WordOrder) * point.ScaleFactor;

                case ModbusValueType.BitField:
                    return registers[0];

                default:
                    return registers[0];
            }
        }

        private static ushort[] EncodeValue(ModbusPointDefinition point, object value)
        {
            switch (point.ValueType)
            {
                case ModbusValueType.UInt16:
                    return new[] { Convert.ToUInt16(Convert.ToDouble(value) / point.ScaleFactor) };

                case ModbusValueType.Int16:
                    return new[] { unchecked((ushort)Convert.ToInt16(Convert.ToDouble(value) / point.ScaleFactor)) };

                case ModbusValueType.UInt32:
                    return WriteUInt32(Convert.ToUInt32(Convert.ToDouble(value) / point.ScaleFactor), point.WordOrder);

                case ModbusValueType.Int32:
                    return WriteInt32(Convert.ToInt32(Convert.ToDouble(value) / point.ScaleFactor), point.WordOrder);

                case ModbusValueType.Float32:
                    return WriteFloat32(Convert.ToSingle(Convert.ToDouble(value) / point.ScaleFactor), point.WordOrder);

                case ModbusValueType.BitField:
                    return new[] { Convert.ToUInt16(value) };

                default:
                    return new[] { Convert.ToUInt16(value) };
            }
        }

        private static uint ReadUInt32(ushort[] registers, ModbusWordOrder wordOrder)
        {
            var buffer = ComposeRegisterBytes(registers, wordOrder);
            return BinaryPrimitives.ReadUInt32BigEndian(buffer);
        }

        private static int ReadInt32(ushort[] registers, ModbusWordOrder wordOrder)
        {
            var buffer = ComposeRegisterBytes(registers, wordOrder);
            return BinaryPrimitives.ReadInt32BigEndian(buffer);
        }

        private static float ReadFloat32(ushort[] registers, ModbusWordOrder wordOrder)
        {
            var buffer = ComposeRegisterBytes(registers, wordOrder);
            return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32BigEndian(buffer));
        }

        private static ushort[] WriteUInt32(uint value, ModbusWordOrder wordOrder)
        {
            var buffer = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
            return ComposeRegisters(buffer, wordOrder);
        }

        private static ushort[] WriteInt32(int value, ModbusWordOrder wordOrder)
        {
            var buffer = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(buffer, value);
            return ComposeRegisters(buffer, wordOrder);
        }

        private static ushort[] WriteFloat32(float value, ModbusWordOrder wordOrder)
        {
            var buffer = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(buffer, BitConverter.SingleToInt32Bits(value));
            return ComposeRegisters(buffer, wordOrder);
        }

        private static byte[] ComposeRegisterBytes(ushort[] registers, ModbusWordOrder wordOrder)
        {
            if (registers.Length < 2)
            {
                throw new InvalidOperationException("At least two registers are required.");
            }

            var first = wordOrder == ModbusWordOrder.HighLow ? registers[0] : registers[1];
            var second = wordOrder == ModbusWordOrder.HighLow ? registers[1] : registers[0];

            var buffer = new byte[4];
            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(0, 2), first);
            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(2, 2), second);
            return buffer;
        }

        private static ushort[] ComposeRegisters(byte[] buffer, ModbusWordOrder wordOrder)
        {
            var first = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(0, 2));
            var second = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(2, 2));

            return wordOrder == ModbusWordOrder.HighLow
                ? new[] { first, second }
                : new[] { second, first };
        }

        private void CloseConnection()
        {
            try
            {
                _stream?.Dispose();
            }
            catch
            {
            }

            try
            {
                _client?.Dispose();
            }
            catch
            {
            }

            _stream = null;
            _client = null;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ModbusTcpService));
            }
        }
    }
}
