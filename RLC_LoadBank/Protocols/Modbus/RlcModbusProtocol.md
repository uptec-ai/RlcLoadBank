# RLC Load Bank Modbus TCP Protocol

## 목적
- HMI는 설계서 기준으로 `WAGO PLC`의 정규화된 Modbus TCP 맵만 접근한다.
- `GIMAC1000` 계측기 3대와 `PT100 Gateway` 7채널의 실제 벤더 레지스터는 PLC 내부에서 흡수한다.
- UI/상위 서비스는 `R/L/C`, `Main`, `Alarm`, `Temperature`, `Auto Mode` 기준 논리 주소만 사용한다.

## 설계 근거
- WAGO PLC `DI 112`, `DO 96`
- MC 상태 `80점`, MC 제어 `80점`
- R 3상 제어 `2식`, R 단상 제어 `1식`
- L 3상 제어 `2식`, L 단상 제어 `1식`
- C 부하 `130kvar x 2step`
- 계측기 `3EA`
- PT100 Gateway `7EA`

## 주소 체계
- 모든 주소는 코드에서 `0-based`로 정의한다.
- 현장 문서/시운전 시트에는 필요 시 `1-based reference`로 변환해서 표기한다.

## Coil (Write)
- `0-11`: 시스템 명령
- `12-27`: R 3상 제어 `16점`
- `28-51`: R 단상 제어 `24점`
- `52-67`: L 3상 제어 `16점`
- `68-91`: L 단상 제어 `24점`
- `92-95`: C Step1/Step2 투입/해제

## Discrete Input (Read)
- `0-79`: MC 상태 `80점`
- `80-81`: 메인 과전압/과전류
- `82-88`: 분기 과전류 `7점`
- `89-95`: 분기 과전압 `7점`
- `96-101`: 온도스위치 `6점`
- `102-111`: 비상정지, 차단기, 부저, Auto/Manual, 운전등, 알람등, Heartbeat, PLC Ready

## Holding Register (Write)
- `0-7`: 자동 운전 목표값
  - R kW, L kvar, C kvar, PF
- `8-9`: BUS OUT 선택, 제어 워드
- `10-13`: 단계 전환 지연, heartbeat 주기
- `14-19`: 알람 마스크, 온도 경고/정지 기준

## Input Register (Read)
- `0-5`: 상태 워드, 알람 워드, 활성 알람 수, 현재 모드, heartbeat counter
- `10-19`: 메인 계측
- `20-33`: 자동 모드 실제값/사용률
- `34-46`: PT100 정규화 온도값
- `50-72`: R/L/C 계측기 정규화 값

## 워드 정의
### Status Word
- bit0: PLC Ready
- bit1: Remote Connected
- bit2: Auto Mode
- bit3: Manual Mode
- bit4: Main Breaker Closed
- bit5: Emergency Active
- bit6: Buzzer Active
- bit7: Any Trip
- bit8: Any Alarm
- bit9: Bus Out 1
- bit10: Bus Out 2
- bit11: Bus Out 3
- bit12: Heartbeat Alive

### Alarm Word
- bit0: Main Over Voltage
- bit1: Main Over Current
- bit2: R High Temperature
- bit3: L High Temperature
- bit4: C High Temperature
- bit5: Branch Protection Active
- bit6: Meter Communication Fault
- bit7: PT100 Gateway Fault

## 구현 메모
- `Models/Modbus`: 주소 공간, 데이터 타입, 포인트 정의
- `Services/Modbus/ModbusTcpService.cs`: 기본 Modbus TCP read/write 서비스
- `Protocols/Modbus/RlcModbusProtocolDefinition.cs`: 설계서 기반 RLC 논리 포인트 정의

## 주의
- `GIMAC1000` 실 레지스터 주소는 본 문서에서 직접 노출하지 않는다.
- 현장 계기/게이트웨이 모델 확정 후 PLC 매핑만 변경하면 HMI 프로토콜은 유지된다.
