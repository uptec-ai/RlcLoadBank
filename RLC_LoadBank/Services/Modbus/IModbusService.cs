using RLC_LoadBank.Models.Modbus;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RLC_LoadBank.Services.Modbus
{
    public interface IModbusService : IDisposable
    {
        ModbusEndpointOptions Endpoint { get; }
        ModbusProtocolDefinition Protocol { get; }
        bool IsConnected { get; }

        Task ConnectAsync(CancellationToken cancellationToken = default);
        Task DisconnectAsync();

        Task<bool[]> ReadCoilsAsync(ushort startAddress, ushort count, CancellationToken cancellationToken = default);
        Task<bool[]> ReadDiscreteInputsAsync(ushort startAddress, ushort count, CancellationToken cancellationToken = default);
        Task<ushort[]> ReadHoldingRegistersAsync(ushort startAddress, ushort count, CancellationToken cancellationToken = default);
        Task<ushort[]> ReadInputRegistersAsync(ushort startAddress, ushort count, CancellationToken cancellationToken = default);

        Task WriteSingleCoilAsync(ushort address, bool value, CancellationToken cancellationToken = default);
        Task WriteSingleRegisterAsync(ushort address, ushort value, CancellationToken cancellationToken = default);
        Task WriteMultipleCoilsAsync(ushort startAddress, bool[] values, CancellationToken cancellationToken = default);
        Task WriteMultipleRegistersAsync(ushort startAddress, ushort[] values, CancellationToken cancellationToken = default);

        Task<ModbusPointValue> ReadPointAsync(ModbusPointDefinition point, CancellationToken cancellationToken = default);
        Task WritePointAsync(ModbusPointDefinition point, object value, CancellationToken cancellationToken = default);
    }
}
