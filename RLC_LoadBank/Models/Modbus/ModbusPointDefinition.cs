using System;

namespace RLC_LoadBank.Models.Modbus
{
    public sealed class ModbusPointDefinition
    {
        public ModbusPointDefinition(
            string key,
            string displayName,
            string description,
            string group,
            ModbusAddressSpace addressSpace,
            ushort startAddress,
            ushort length,
            ModbusValueType valueType,
            bool isWritable,
            double scaleFactor = 1.0d,
            string unit = "",
            ModbusWordOrder wordOrder = ModbusWordOrder.HighLow)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key is required.", nameof(key));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("DisplayName is required.", nameof(displayName));
            }

            if (length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be greater than zero.");
            }

            Key = key;
            DisplayName = displayName;
            Description = description ?? string.Empty;
            Group = group ?? string.Empty;
            AddressSpace = addressSpace;
            StartAddress = startAddress;
            Length = length;
            ValueType = valueType;
            IsWritable = isWritable;
            ScaleFactor = scaleFactor;
            Unit = unit ?? string.Empty;
            WordOrder = wordOrder;
        }

        public string Key { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string Group { get; }
        public ModbusAddressSpace AddressSpace { get; }
        public ushort StartAddress { get; }
        public ushort Length { get; }
        public ModbusValueType ValueType { get; }
        public bool IsWritable { get; }
        public double ScaleFactor { get; }
        public string Unit { get; }
        public ModbusWordOrder WordOrder { get; }

        public ushort ReferenceAddress
        {
            get
            {
                return (ushort)(StartAddress + 1);
            }
        }
    }
}
