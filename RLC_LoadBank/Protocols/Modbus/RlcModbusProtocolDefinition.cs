using RLC_LoadBank.Models.Modbus;
using System.Collections.Generic;
using System.Configuration;

namespace RLC_LoadBank.Protocols.Modbus
{
    public static class RlcModbusProtocolDefinition
    {
        private const string ProtocolDescription =
            "설계 요청서 기준 RLC Load Bank용 PLC 정규화 Modbus TCP 프로토콜. " +
            "HMI는 WAGO PLC의 단일 Modbus 맵만 조회하며, 개별 계측기와 PT100 게이트웨이 상세 주소는 PLC 내부에서 정규화한다.";

        public static ModbusProtocolDefinition CreateDefault()
        {
            var points = new List<ModbusPointDefinition>();

            BuildCommandCoils(points);
            BuildDiscreteInputs(points);
            BuildHoldingRegisters(points);
            BuildInputRegisters(points);

            return new ModbusProtocolDefinition(
                "RLC Load Bank Modbus TCP",
                ProtocolDescription,
                new ModbusEndpointOptions(ConfigurationManager.AppSettings["RlcHost"], int.Parse(ConfigurationManager.AppSettings["RlcPort"]), 1), 
                points
                );
        }

        private static void BuildCommandCoils(List<ModbusPointDefinition> points)
        {
            AddPoint(points, "cmd.system.auto_start", "자동 운전 시작", "자동 운전 시작 요청", "Command/System", ModbusAddressSpace.Coil, 0, 1, ModbusValueType.Boolean, true);
            AddPoint(points, "cmd.system.auto_stop", "자동 운전 정지", "자동 운전 정지 요청", "Command/System", ModbusAddressSpace.Coil, 1, 1, ModbusValueType.Boolean, true);
            AddPoint(points, "cmd.system.mode_auto", "자동 모드 선택", "자동 모드 전환 요청", "Command/System", ModbusAddressSpace.Coil, 2, 1, ModbusValueType.Boolean, true);
            AddPoint(points, "cmd.system.mode_manual", "수동 모드 선택", "수동 모드 전환 요청", "Command/System", ModbusAddressSpace.Coil, 3, 1, ModbusValueType.Boolean, true);
            AddPoint(points, "cmd.system.reset", "시스템 초기화", "알람/상태 초기화 요청", "Command/System", ModbusAddressSpace.Coil, 4, 1, ModbusValueType.Boolean, true);
            AddPoint(points, "cmd.system.bus_out_1", "BUS OUT 1", "BUS OUT 1 선택", "Command/System", ModbusAddressSpace.Coil, 5, 1, ModbusValueType.Boolean, true);
            AddPoint(points, "cmd.system.bus_out_2", "BUS OUT 2", "BUS OUT 2 선택", "Command/System", ModbusAddressSpace.Coil, 6, 1, ModbusValueType.Boolean, true);
            AddPoint(points, "cmd.system.bus_out_3", "BUS OUT 3", "BUS OUT 3 선택", "Command/System", ModbusAddressSpace.Coil, 7, 1, ModbusValueType.Boolean, true);
            AddPoint(points, "cmd.main.breaker_close", "메인 차단기 투입", "메인 차단기 Close 요청", "Command/Main", ModbusAddressSpace.Coil, 8, 1, ModbusValueType.Boolean, true);
            AddPoint(points, "cmd.main.breaker_open", "메인 차단기 차단", "메인 차단기 Open 요청", "Command/Main", ModbusAddressSpace.Coil, 9, 1, ModbusValueType.Boolean, true);
            AddPoint(points, "cmd.alarm.buzzer_stop", "부저 정지", "경보 부저 정지 요청", "Command/Alarm", ModbusAddressSpace.Coil, 10, 1, ModbusValueType.Boolean, true);
            AddPoint(points, "cmd.alarm.lamp_test", "램프 테스트", "램프 테스트 요청", "Command/Alarm", ModbusAddressSpace.Coil, 11, 1, ModbusValueType.Boolean, true);

            var currentAddress = 12;
            currentAddress = AddThreePhaseStepBlock(points, currentAddress, "cmd.r.3ph.bank1", "R 3상 제어 1식", "R3PH1");
            currentAddress = AddThreePhaseStepBlock(points, currentAddress, "cmd.r.3ph.bank2", "R 3상 제어 2식", "R3PH2");
            currentAddress = AddSinglePhaseStepBlock(points, currentAddress, "cmd.r.1ph", "R 단상 제어", "R1PH");
            currentAddress = AddThreePhaseStepBlock(points, currentAddress, "cmd.l.3ph.bank1", "L 3상 제어 1식", "L3PH1");
            currentAddress = AddThreePhaseStepBlock(points, currentAddress, "cmd.l.3ph.bank2", "L 3상 제어 2식", "L3PH2");
            currentAddress = AddSinglePhaseStepBlock(points, currentAddress, "cmd.l.1ph", "L 단상 제어", "L1PH");

            AddPoint(points, "cmd.c.step1.insert", "C Step 1 투입", "130kvar 1단 투입 요청", "Command/C", ModbusAddressSpace.Coil, (ushort)currentAddress++, 1, ModbusValueType.Boolean, true);
            AddPoint(points, "cmd.c.step1.release", "C Step 1 해제", "130kvar 1단 해제 요청", "Command/C", ModbusAddressSpace.Coil, (ushort)currentAddress++, 1, ModbusValueType.Boolean, true);
            AddPoint(points, "cmd.c.step2.insert", "C Step 2 투입", "130kvar 2단 투입 요청", "Command/C", ModbusAddressSpace.Coil, (ushort)currentAddress++, 1, ModbusValueType.Boolean, true);
            AddPoint(points, "cmd.c.step2.release", "C Step 2 해제", "130kvar 2단 해제 요청", "Command/C", ModbusAddressSpace.Coil, (ushort)currentAddress++, 1, ModbusValueType.Boolean, true);
        }

        private static void BuildDiscreteInputs(List<ModbusPointDefinition> points)
        {
            var currentAddress = 0;
            currentAddress = AddThreePhaseStepBlock(points, currentAddress, "sts.r.3ph.bank1", "R 3상 제어 1식 상태", "R3PH1", ModbusAddressSpace.DiscreteInput, false);
            currentAddress = AddThreePhaseStepBlock(points, currentAddress, "sts.r.3ph.bank2", "R 3상 제어 2식 상태", "R3PH2", ModbusAddressSpace.DiscreteInput, false);
            currentAddress = AddSinglePhaseStepBlock(points, currentAddress, "sts.r.1ph", "R 단상 제어 상태", "R1PH", ModbusAddressSpace.DiscreteInput, false);
            currentAddress = AddThreePhaseStepBlock(points, currentAddress, "sts.l.3ph.bank1", "L 3상 제어 1식 상태", "L3PH1", ModbusAddressSpace.DiscreteInput, false);
            currentAddress = AddThreePhaseStepBlock(points, currentAddress, "sts.l.3ph.bank2", "L 3상 제어 2식 상태", "L3PH2", ModbusAddressSpace.DiscreteInput, false);
            currentAddress = AddSinglePhaseStepBlock(points, currentAddress, "sts.l.1ph", "L 단상 제어 상태", "L1PH", ModbusAddressSpace.DiscreteInput, false);

            AddPoint(points, "sts.main.over_voltage", "메인 과전압", "메인 판넬 과전압 보호계전 상태", "Status/Protection", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);
            AddPoint(points, "sts.main.over_current", "메인 과전류", "메인 판넬 과전류 보호계전 상태", "Status/Protection", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);

            AddPoint(points, "sts.branch.r3ph1.over_current", "R3PH1 과전류", "R 3상 1식 분기 과전류", "Status/Protection", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);
            AddPoint(points, "sts.branch.r3ph2.over_current", "R3PH2 과전류", "R 3상 2식 분기 과전류", "Status/Protection", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);
            AddPoint(points, "sts.branch.r1ph.over_current", "R1PH 과전류", "R 단상 분기 과전류", "Status/Protection", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);
            AddPoint(points, "sts.branch.l3ph1.over_current", "L3PH1 과전류", "L 3상 1식 분기 과전류", "Status/Protection", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);
            AddPoint(points, "sts.branch.l3ph2.over_current", "L3PH2 과전류", "L 3상 2식 분기 과전류", "Status/Protection", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);
            AddPoint(points, "sts.branch.l1ph.over_current", "L1PH 과전류", "L 단상 분기 과전류", "Status/Protection", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);
            AddPoint(points, "sts.branch.c.over_current", "C 과전류", "C 부하 분기 과전류", "Status/Protection", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);

            AddPoint(points, "sts.branch.r3ph1.over_voltage", "R3PH1 과전압", "R 3상 1식 분기 과전압", "Status/Protection", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);
            AddPoint(points, "sts.branch.r3ph2.over_voltage", "R3PH2 과전압", "R 3상 2식 분기 과전압", "Status/Protection", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);
            AddPoint(points, "sts.branch.r1ph.over_voltage", "R1PH 과전압", "R 단상 분기 과전압", "Status/Protection", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);
            AddPoint(points, "sts.branch.l3ph1.over_voltage", "L3PH1 과전압", "L 3상 1식 분기 과전압", "Status/Protection", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);
            AddPoint(points, "sts.branch.l3ph2.over_voltage", "L3PH2 과전압", "L 3상 2식 분기 과전압", "Status/Protection", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);
            AddPoint(points, "sts.branch.l1ph.over_voltage", "L1PH 과전압", "L 단상 분기 과전압", "Status/Protection", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);
            AddPoint(points, "sts.branch.c.over_voltage", "C 과전압", "C 부하 분기 과전압", "Status/Protection", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);

            AddPoint(points, "sts.temp.r.panel_high", "R 판넬 고온", "R 판넬 바이메탈 온도스위치 상태", "Status/Temperature", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);
            AddPoint(points, "sts.temp.r.exhaust_high", "R 배기 고온", "R 배기부 고온 스위치 상태", "Status/Temperature", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);
            AddPoint(points, "sts.temp.l.panel_high", "L 판넬 고온", "L 판넬 바이메탈 온도스위치 상태", "Status/Temperature", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);
            AddPoint(points, "sts.temp.l.exhaust_high", "L 배기 고온", "L 배기부 고온 스위치 상태", "Status/Temperature", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);
            AddPoint(points, "sts.temp.c.panel_high", "C 판넬 고온", "C 판넬 바이메탈 온도스위치 상태", "Status/Temperature", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);
            AddPoint(points, "sts.temp.main.panel_high", "메인 판넬 고온", "메인 판넬 바이메탈 온도스위치 상태", "Status/Temperature", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);

            AddPoint(points, "sts.system.emergency", "비상정지", "이머전시 스위치 상태", "Status/System", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);
            AddPoint(points, "sts.main.breaker_closed", "메인 차단기 투입 상태", "메인 차단기 On 상태", "Status/Main", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);
            AddPoint(points, "sts.main.breaker_trip", "메인 차단기 트립", "메인 차단기 Trip 상태", "Status/Main", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);
            AddPoint(points, "sts.alarm.buzzer", "경보 부저", "부저 동작 상태", "Status/Alarm", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);
            AddPoint(points, "sts.system.auto_mode", "자동 모드 상태", "자동 모드 활성 상태", "Status/System", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);
            AddPoint(points, "sts.system.manual_mode", "수동 모드 상태", "수동 모드 활성 상태", "Status/System", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);
            AddPoint(points, "sts.system.run_lamp", "운전등", "동작 램프 상태", "Status/System", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);
            AddPoint(points, "sts.system.alarm_lamp", "알람등", "알람 램프 상태", "Status/System", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);
            AddPoint(points, "sts.system.heartbeat", "Heartbeat", "PLC heartbeat 상태", "Status/System", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);
            AddPoint(points, "sts.system.plc_ready", "PLC 준비 완료", "PLC 서비스 준비 상태", "Status/System", ModbusAddressSpace.DiscreteInput, (ushort)currentAddress++, 1, ModbusValueType.Boolean, false);
        }

        private static void BuildHoldingRegisters(List<ModbusPointDefinition> points)
        {
            AddPoint(points, "set.auto.target_r_kw", "자동 목표 R(kW)", "자동 모드 목표 저항부하", "Setpoint/Auto", ModbusAddressSpace.HoldingRegister, 0, 2, ModbusValueType.Float32, true, 1.0d, "kW");
            AddPoint(points, "set.auto.target_l_kvar", "자동 목표 L(kvar)", "자동 모드 목표 리액터부하", "Setpoint/Auto", ModbusAddressSpace.HoldingRegister, 2, 2, ModbusValueType.Float32, true, 1.0d, "kvar");
            AddPoint(points, "set.auto.target_c_kvar", "자동 목표 C(kvar)", "자동 모드 목표 콘덴서부하", "Setpoint/Auto", ModbusAddressSpace.HoldingRegister, 4, 2, ModbusValueType.Float32, true, 1.0d, "kvar");
            AddPoint(points, "set.auto.target_pf", "자동 목표 PF", "자동 모드 목표 역률", "Setpoint/Auto", ModbusAddressSpace.HoldingRegister, 6, 2, ModbusValueType.Float32, true);
            AddPoint(points, "set.auto.bus_out_selection", "자동 BUS OUT 선택", "자동 모드 BUS OUT 선택값", "Setpoint/Auto", ModbusAddressSpace.HoldingRegister, 8, 1, ModbusValueType.UInt16, true);
            AddPoint(points, "set.auto.control_word", "자동 제어 워드", "자동 운전 제어 플래그", "Setpoint/Auto", ModbusAddressSpace.HoldingRegister, 9, 1, ModbusValueType.BitField, true);
            AddPoint(points, "set.auto.transition_delay_ms", "자동 전환 지연(ms)", "단계 전환 지연시간", "Setpoint/Auto", ModbusAddressSpace.HoldingRegister, 10, 2, ModbusValueType.UInt32, true, 1.0d, "ms");
            AddPoint(points, "set.system.heartbeat_interval_ms", "Heartbeat 주기(ms)", "PLC-HMI heartbeat 주기", "Setpoint/System", ModbusAddressSpace.HoldingRegister, 12, 2, ModbusValueType.UInt32, true, 1.0d, "ms");
            AddPoint(points, "set.system.alarm_mask", "알람 마스크", "알람 Enable/Mask 비트필드", "Setpoint/System", ModbusAddressSpace.HoldingRegister, 14, 1, ModbusValueType.BitField, true);
            AddPoint(points, "set.system.temperature_limit_warning", "온도 경고 기준", "PT100 경고 기준 온도", "Setpoint/System", ModbusAddressSpace.HoldingRegister, 16, 2, ModbusValueType.Float32, true, 1.0d, "°C");
            AddPoint(points, "set.system.temperature_limit_trip", "온도 정지 기준", "PT100 운전 정지 기준 온도", "Setpoint/System", ModbusAddressSpace.HoldingRegister, 18, 2, ModbusValueType.Float32, true, 1.0d, "°C");
        }

        private static void BuildInputRegisters(List<ModbusPointDefinition> points)
        {
            AddPoint(points, "meas.system.status_word", "시스템 상태 워드", "PLC 정규화 시스템 상태 비트필드", "Measure/System", ModbusAddressSpace.InputRegister, 0, 1, ModbusValueType.BitField, false);
            AddPoint(points, "meas.system.alarm_word", "시스템 알람 워드", "PLC 정규화 시스템 알람 비트필드", "Measure/System", ModbusAddressSpace.InputRegister, 1, 1, ModbusValueType.BitField, false);
            AddPoint(points, "meas.system.active_alarm_count", "활성 알람 수", "현재 활성 알람 수", "Measure/System", ModbusAddressSpace.InputRegister, 2, 1, ModbusValueType.UInt16, false);
            AddPoint(points, "meas.system.active_mode", "현재 모드 코드", "0:정지, 1:자동, 2:수동", "Measure/System", ModbusAddressSpace.InputRegister, 3, 1, ModbusValueType.UInt16, false);
            AddPoint(points, "meas.system.heartbeat_counter", "Heartbeat 카운터", "PLC heartbeat 누적 카운터", "Measure/System", ModbusAddressSpace.InputRegister, 4, 2, ModbusValueType.UInt32, false);

            AddPoint(points, "meas.main.line_voltage_ll", "선간전압 평균", "메인 평균 선간전압", "Measure/Main", ModbusAddressSpace.InputRegister, 10, 2, ModbusValueType.Float32, false, 1.0d, "V");
            AddPoint(points, "meas.main.line_current", "선전류 평균", "메인 평균 선전류", "Measure/Main", ModbusAddressSpace.InputRegister, 12, 2, ModbusValueType.Float32, false, 1.0d, "A");
            AddPoint(points, "meas.main.frequency", "주파수", "메인 주파수", "Measure/Main", ModbusAddressSpace.InputRegister, 14, 2, ModbusValueType.Float32, false, 1.0d, "Hz");
            AddPoint(points, "meas.main.total_apparent_power", "총 피상전력", "R+L+C 합산 피상전력", "Measure/Main", ModbusAddressSpace.InputRegister, 16, 2, ModbusValueType.Float32, false, 1.0d, "kVA");
            AddPoint(points, "meas.main.total_power_factor", "총 역률", "시스템 총 역률", "Measure/Main", ModbusAddressSpace.InputRegister, 18, 2, ModbusValueType.Float32, false);

            AddPoint(points, "meas.auto.actual_r_kw", "실제 R(kW)", "현재 투입된 저항부하", "Measure/Auto", ModbusAddressSpace.InputRegister, 20, 2, ModbusValueType.Float32, false, 1.0d, "kW");
            AddPoint(points, "meas.auto.actual_l_kvar", "실제 L(kvar)", "현재 투입된 리액터부하", "Measure/Auto", ModbusAddressSpace.InputRegister, 22, 2, ModbusValueType.Float32, false, 1.0d, "kvar");
            AddPoint(points, "meas.auto.actual_c_kvar", "실제 C(kvar)", "현재 투입된 콘덴서부하", "Measure/Auto", ModbusAddressSpace.InputRegister, 24, 2, ModbusValueType.Float32, false, 1.0d, "kvar");
            AddPoint(points, "meas.auto.usage_r_percent", "R 사용률", "R 목표 대비 사용률", "Measure/Auto", ModbusAddressSpace.InputRegister, 26, 2, ModbusValueType.Float32, false, 1.0d, "%");
            AddPoint(points, "meas.auto.usage_l_percent", "L 사용률", "L 목표 대비 사용률", "Measure/Auto", ModbusAddressSpace.InputRegister, 28, 2, ModbusValueType.Float32, false, 1.0d, "%");
            AddPoint(points, "meas.auto.usage_c_percent", "C 사용률", "C 목표 대비 사용률", "Measure/Auto", ModbusAddressSpace.InputRegister, 30, 2, ModbusValueType.Float32, false, 1.0d, "%");
            AddPoint(points, "meas.auto.usage_total_percent", "총 사용률", "전체 목표 대비 사용률", "Measure/Auto", ModbusAddressSpace.InputRegister, 32, 2, ModbusValueType.Float32, false, 1.0d, "%");

            AddPoint(points, "meas.temp.r_panel", "R 판넬 온도", "R 판넬 PT100 측정값", "Measure/Temperature", ModbusAddressSpace.InputRegister, 34, 2, ModbusValueType.Float32, false, 1.0d, "°C");
            AddPoint(points, "meas.temp.l_panel", "L 판넬 온도", "L 판넬 PT100 측정값", "Measure/Temperature", ModbusAddressSpace.InputRegister, 36, 2, ModbusValueType.Float32, false, 1.0d, "°C");
            AddPoint(points, "meas.temp.c_panel", "C 판넬 온도", "C 판넬 PT100 측정값", "Measure/Temperature", ModbusAddressSpace.InputRegister, 38, 2, ModbusValueType.Float32, false, 1.0d, "°C");
            AddPoint(points, "meas.temp.main_panel", "메인 판넬 온도", "메인 판넬 PT100 측정값", "Measure/Temperature", ModbusAddressSpace.InputRegister, 40, 2, ModbusValueType.Float32, false, 1.0d, "°C");
            AddPoint(points, "meas.temp.r_exhaust", "R 배기 온도", "R 상부 배기 PT100 측정값", "Measure/Temperature", ModbusAddressSpace.InputRegister, 42, 2, ModbusValueType.Float32, false, 1.0d, "°C");
            AddPoint(points, "meas.temp.l_exhaust", "L 배기 온도", "L 상부 배기 PT100 측정값", "Measure/Temperature", ModbusAddressSpace.InputRegister, 44, 2, ModbusValueType.Float32, false, 1.0d, "°C");
            AddPoint(points, "meas.temp.ambient", "주변 온도", "판넬 주변부 기준 온도", "Measure/Temperature", ModbusAddressSpace.InputRegister, 46, 2, ModbusValueType.Float32, false, 1.0d, "°C");

            AddPoint(points, "meas.meter.r_voltage", "R 계기 전압", "R 계기 전압", "Measure/Meter/R", ModbusAddressSpace.InputRegister, 50, 2, ModbusValueType.Float32, false, 1.0d, "V");
            AddPoint(points, "meas.meter.r_current", "R 계기 전류", "R 계기 전류", "Measure/Meter/R", ModbusAddressSpace.InputRegister, 52, 2, ModbusValueType.Float32, false, 1.0d, "A");
            AddPoint(points, "meas.meter.r_active_power", "R 계기 유효전력", "R 계기 유효전력", "Measure/Meter/R", ModbusAddressSpace.InputRegister, 54, 2, ModbusValueType.Float32, false, 1.0d, "kW");
            AddPoint(points, "meas.meter.r_power_factor", "R 계기 역률", "R 계기 역률", "Measure/Meter/R", ModbusAddressSpace.InputRegister, 56, 2, ModbusValueType.Float32, false);

            AddPoint(points, "meas.meter.l_voltage", "L 계기 전압", "L 계기 전압", "Measure/Meter/L", ModbusAddressSpace.InputRegister, 58, 2, ModbusValueType.Float32, false, 1.0d, "V");
            AddPoint(points, "meas.meter.l_current", "L 계기 전류", "L 계기 전류", "Measure/Meter/L", ModbusAddressSpace.InputRegister, 60, 2, ModbusValueType.Float32, false, 1.0d, "A");
            AddPoint(points, "meas.meter.l_reactive_power", "L 계기 무효전력", "L 계기 무효전력", "Measure/Meter/L", ModbusAddressSpace.InputRegister, 62, 2, ModbusValueType.Float32, false, 1.0d, "kvar");
            AddPoint(points, "meas.meter.l_power_factor", "L 계기 역률", "L 계기 역률", "Measure/Meter/L", ModbusAddressSpace.InputRegister, 64, 2, ModbusValueType.Float32, false);

            AddPoint(points, "meas.meter.c_voltage", "C 계기 전압", "C 계기 전압", "Measure/Meter/C", ModbusAddressSpace.InputRegister, 66, 2, ModbusValueType.Float32, false, 1.0d, "V");
            AddPoint(points, "meas.meter.c_current", "C 계기 전류", "C 계기 전류", "Measure/Meter/C", ModbusAddressSpace.InputRegister, 68, 2, ModbusValueType.Float32, false, 1.0d, "A");
            AddPoint(points, "meas.meter.c_reactive_power", "C 계기 무효전력", "C 계기 무효전력", "Measure/Meter/C", ModbusAddressSpace.InputRegister, 70, 2, ModbusValueType.Float32, false, 1.0d, "kvar");
            AddPoint(points, "meas.meter.c_power_factor", "C 계기 역률", "C 계기 역률", "Measure/Meter/C", ModbusAddressSpace.InputRegister, 72, 2, ModbusValueType.Float32, false);
        }

        private static int AddThreePhaseStepBlock(
            List<ModbusPointDefinition> points,
            int startAddress,
            string keyPrefix,
            string displayPrefix,
            string equipmentCode,
            ModbusAddressSpace addressSpace = ModbusAddressSpace.Coil,
            bool isWritable = true)
        {
            var currentAddress = startAddress;
            var stepCapacities = new[] { "2.5", "2.5", "5", "10", "15", "15", "25", "30" };

            for (var step = 1; step <= 8; step++)
            {
                AddPoint(points, $"{keyPrefix}.step{step:00}", $"{displayPrefix} STEP {step:00}", $"{equipmentCode} {stepCapacities[step - 1]} 단계 제어/상태", displayPrefix, addressSpace, (ushort)currentAddress++, 1, ModbusValueType.Boolean, isWritable);
            }

            return currentAddress;
        }

        private static int AddSinglePhaseStepBlock(
            List<ModbusPointDefinition> points,
            int startAddress,
            string keyPrefix,
            string displayPrefix,
            string equipmentCode,
            ModbusAddressSpace addressSpace = ModbusAddressSpace.Coil,
            bool isWritable = true)
        {
            var currentAddress = startAddress;
            var phases = new[] { "L1", "L2", "L3" };
            var stepCapacities = new[] { "0.83", "0.83", "1.67", "3.33", "5", "5", "8.33", "10" };

            foreach (var phase in phases)
            {
                for (var step = 1; step <= 8; step++)
                {
                    AddPoint(points, $"{keyPrefix}.{phase.ToLowerInvariant()}.step{step:00}", $"{displayPrefix} {phase} STEP {step:00}", $"{equipmentCode} {phase} {stepCapacities[step - 1]} 단계 제어/상태", $"{displayPrefix}/{phase}", addressSpace, (ushort)currentAddress++, 1, ModbusValueType.Boolean, isWritable);
                }
            }

            return currentAddress;
        }

        private static void AddPoint(
            List<ModbusPointDefinition> points,
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
            points.Add(new ModbusPointDefinition(
                key,
                displayName,
                description,
                group,
                addressSpace,
                startAddress,
                length,
                valueType,
                isWritable,
                scaleFactor,
                unit,
                wordOrder));
        }
    }
}
