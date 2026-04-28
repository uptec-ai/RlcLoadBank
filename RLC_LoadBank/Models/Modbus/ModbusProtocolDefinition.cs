using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RLC_LoadBank.Models.Modbus
{
    public sealed class ModbusProtocolDefinition
    {
        private readonly IReadOnlyDictionary<string, ModbusPointDefinition> _points;

        public ModbusProtocolDefinition(
            string name,
            string description,
            ModbusEndpointOptions defaultEndpoint,
            IEnumerable<ModbusPointDefinition> points)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Protocol name is required.", nameof(name));
            }

            Name = name;
            Description = description ?? string.Empty;
            DefaultEndpoint = defaultEndpoint ?? throw new ArgumentNullException(nameof(defaultEndpoint));

            var pointDictionary = new Dictionary<string, ModbusPointDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var point in points ?? throw new ArgumentNullException(nameof(points)))
            {
                if (pointDictionary.ContainsKey(point.Key))
                {
                    throw new InvalidOperationException($"Duplicated Modbus point key: {point.Key}");
                }

                pointDictionary.Add(point.Key, point);
            }

            _points = new ReadOnlyDictionary<string, ModbusPointDefinition>(pointDictionary);
        }

        public string Name { get; }
        public string Description { get; }
        public ModbusEndpointOptions DefaultEndpoint { get; }
        public IReadOnlyDictionary<string, ModbusPointDefinition> Points
        {
            get
            {
                return _points;
            }
        }

        public ModbusPointDefinition GetPoint(string key)
        {
            if (!_points.TryGetValue(key, out var point))
            {
                throw new KeyNotFoundException($"Modbus point '{key}' is not defined.");
            }

            return point;
        }

        public bool TryGetPoint(string key, out ModbusPointDefinition point)
        {
            return _points.TryGetValue(key, out point);
        }

        public IReadOnlyList<ModbusPointDefinition> GetPointsByAddressSpace(ModbusAddressSpace addressSpace)
        {
            return _points.Values
                .Where(point => point.AddressSpace == addressSpace)
                .OrderBy(point => point.StartAddress)
                .ToList();
        }
    }
}
