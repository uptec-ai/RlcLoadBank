using System;

namespace RLC_LoadBank.Models.Modbus
{
    public sealed class ModbusPointValue
    {
        public ModbusPointValue(
            ModbusPointDefinition definition,
            object value,
            bool[] bitValues,
            ushort[] registerValues,
            DateTime timestamp)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Value = value;
            BitValues = bitValues ?? Array.Empty<bool>();
            RegisterValues = registerValues ?? Array.Empty<ushort>();
            Timestamp = timestamp;
        }

        public ModbusPointDefinition Definition { get; }
        public object Value { get; }
        public bool[] BitValues { get; }
        public ushort[] RegisterValues { get; }
        public DateTime Timestamp { get; }
    }
}
