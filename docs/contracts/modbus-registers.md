# Contract: device Modbus registers

Source of truth in code: `EnergySim.Modbus.RegisterMap`, `RegisterCodec`, `DeviceProfiles`. This file explains them. Register map version: 1.

## Rules
- One Modbus TCP server per device process, library EasyModbusTCP.NETCore, host and port configurable (`Modbus:Host`, `Modbus:Port`, port 1502 or higher), `Modbus:UnitId` configurable.
- Addresses are client addresses, 0-based. The server library uses 1-based arrays, the +1 offset exists only inside the server class.
- Byte order: big-endian per register. Word order: high word first (ABCD).
- The server stays reachable while the simulation is paused. Registers keep the last computed state, sequence and virtual time stay constant.
- Power sign: generation positive, consumption negative.
- Read the whole header with one request (17 registers). No atomic guarantee across registers, the sequence number tells which state was read.
- The library cannot return Modbus exception 03. An invalid write is accepted by the library, then the server restores the old value and sets warn flag bit 2.

## Default ports
| Process | Port |
|---|---|
| Time service (master) | 1502 |
| Clock endpoints of devices | 1601 pv, 1602 battery, 1603 charger, 1604 wind |
| Device Modbus servers | 1511 pv, 1512 battery, 1513 charger, 1514 wind |
| Weather service (HTTP) | 5200 |

## Input registers (FC04, read only), common header
| Addr | Ref | Count | Type | Scale | Unit | Range | Meaning |
|---|---|---|---|---|---|---|---|
| 0 | 30001 | 1 | u16 | 1 | - | 1..4 | DeviceType: 1 Pv, 2 Battery, 3 Charger, 4 Wind |
| 1 | 30002 | 1 | u16 | 1 | - | 1.. | Register map version |
| 2 | 30003 | 1 | u16 | 1 | - | 0..4 | State: 0 Off, 1 Standby, 2 Running, 3 Limited, 4 Fault |
| 3 | 30004 | 1 | u16 | 1 | - | 0..65535 | Fault code, 0 = ok, stays until reset |
| 4 | 30005 | 1 | u16 | bits | - | b0..b2 | Warn flags: b0 NoMasterSync, b1 WeatherStale, b2 InvalidWrite |
| 5 | 30006 | 2 | s32 | 1 | W | device specific | Active power, 0 when off or in fault |
| 7 | 30008 | 2 | u32 | 1 | Wh | 0.. | Cumulative energy, reset on restart |
| 9 | 30010 | 4 | u64 | 1 | ms | unix ms UTC | Virtual time of this state, frozen while paused |
| 13 | 30014 | 2 | u32 | 1 | - | wraps | Sequence, +1 per new state, constant while paused |
| 15 | 30016 | 1 | u16 | 1 | - | 0..65535 | Generation, +1 per restart |
| 16 | 30017 | 1 | u16 | 1 | - | 0..3 | Clock state: 0 Stopped, 1 Running, 2 Paused, 3 Unsynced |

## Input registers, device block
Value i sits at address `100 + 2 * i`, type s32, 2 registers, raw = value * Scale. Order, names, units and scales per device type come from `DeviceProfiles`.

| Type | Values (index: name, unit, scale) |
|---|---|
| Pv | 0 Irradiance W/m2 x1, 1 ModuleTemperature degC x10, 2 SunElevation deg x10, 3 DailyEnergy Wh x1 |
| Battery | 0 StateOfCharge % x10, 1 CellTemperature degC x10, 2 ChargedEnergy Wh x1, 3 DischargedEnergy Wh x1 |
| Charger | 0 Port1Power W x1, 1 Port2Power W x1, 2 Port1VehicleSoc % x10, 3 Port2VehicleSoc % x10 |
| Wind | 0 WindSpeed m/s x10, 1 RotorSpeed rpm x10, 2 NacelleDirection deg x10 |

A device reports its values in `ISimulatedDevice.SpecificValues` in exactly this order. If the model has no real value for an entry, report 0 and say so in the device README.

## Holding registers (FC03 read, FC06 write)
| Addr | Ref | Count | Type | Scale | Unit | Range | Meaning and error behavior |
|---|---|---|---|---|---|---|---|
| 0 | 40001 | 1 | u16 | 1 | - | 0..1 | On/off. Outside range: old value restored, warn flag b2 |
| 1 | 40002 | 1 | u16 | 0.1 | % | 0..1000 | Power limit in permille of nominal power. Outside range: old value restored, warn flag b2 |
| 2 | 40003 | 1 | u16 | 1 | - | 0..1 | Fault reset. 1 resets the fault, server sets the register back to 0. Ignored without fault |

## Errors the client must show
- Connection refused or timeout: name host, port, and which device probably is not running.
- Value outside range: rejected by the client before sending, otherwise warn flag b2 after the write.
- Unknown device type or map version: stop with a clear message.
