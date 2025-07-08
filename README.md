# Generic PID Controller in C#

A comprehensive, production-ready PID (Proportional-Integral-Derivative) controller implementation in C# with advanced features for industrial control applications.

## Features

### 🎯 **Core PID Control**
- **Proportional (P)**: Responds to current error
- **Integral (I)**: Eliminates steady-state error
- **Derivative (D)**: Provides damping and stability
- **Thread-safe** implementation with proper locking

### 🛡️ **Advanced Features**
- **Anti-windup protection** for integral term
- **Output limiting** with configurable min/max values
- **Derivative filtering** to reduce noise sensitivity
- **Deadband** to prevent hunting around setpoint
- **Multiple operating modes** (Manual, Automatic, Cascade)
- **Direct/Reverse action** support

### 📊 **Monitoring & Events**
- **Real-time statistics** and status monitoring
- **Event-driven architecture** for output and status changes
- **Comprehensive tuning parameter management**
- **Performance metrics** and debugging information

### 🔧 **Tuning Utilities**
- **Ziegler-Nichols** tuning method
- **Cohen-Coon** tuning method
- **Auto-tuning** capabilities
- **Parameter validation** and optimization

## Quick Start

### Basic Usage

```csharp
using ControlSystems;

// Create a PID controller
var controller = new PIDController(kp: 2.0, ki: 0.5, kd: 0.1);

// Set target value
controller.Setpoint = 100.0;

// Configure limits
controller.SetOutputLimits(0, 100);

// Compute control output
double processVariable = 50.0;
double output = controller.Compute(processVariable);
```

### Temperature Control Example

```csharp
// Temperature control system
var tempController = new PIDController(kp: 2.0, ki: 0.5, kd: 0.1, sampleTime: 0.1);
tempController.Setpoint = 75.0; // Target 75°C
tempController.SetOutputLimits(0, 100); // Heater 0-100%
tempController.IntegralWindupLimit = 50.0; // Prevent windup

// Control loop
while (true)
{
    double currentTemp = ReadTemperature(); // Get sensor reading
    double heaterOutput = tempController.Compute(currentTemp);
    SetHeater(heaterOutput); // Apply to actuator
    Thread.Sleep(100); // 100ms cycle
}
```

### Motor Speed Control Example

```csharp
// Motor speed control
var motorController = new PIDController(kp: 1.5, ki: 0.3, kd: 0.05, sampleTime: 0.05);
motorController.Setpoint = 1000.0; // Target 1000 RPM
motorController.SetOutputLimits(-12, 12); // Voltage range
motorController.DerivativeFilterCoefficient = 0.2; // Filter noise

// Control loop
while (true)
{
    double currentSpeed = ReadMotorSpeed();
    double voltage = motorController.Compute(currentSpeed);
    SetMotorVoltage(voltage);
    Thread.Sleep(50); // 50ms cycle
}
```

## Advanced Configuration

### Anti-Windup Protection

```csharp
// Prevent integral windup when output is saturated
controller.IntegralWindupLimit = 25.0; // Limit integral term
```

### Derivative Filtering

```csharp
// Filter derivative term to reduce noise sensitivity
controller.DerivativeFilterCoefficient = 0.3; // 0 = no filtering, 1 = no filtering
```

### Deadband Configuration

```csharp
// Ignore small errors around setpoint
controller.Deadband = 0.5; // ±0.5 unit deadband
```

### Mode Switching

```csharp
// Manual mode
controller.SetManual(25.0); // Set manual output

// Automatic mode
controller.SetAutomatic(); // Return to automatic control
```

## Tuning Methods

### Ziegler-Nichols Tuning

```csharp
using PIDControllerExamples;

// Find ultimate gain (Ku) and period (Tu) through testing
double ku = 10.0; // Ultimate gain
double tu = 2.0;  // Ultimate period

// Get tuning parameters
var parameters = PIDTuningUtils.ZieglerNichols(ku, tu, ZNTuningType.PID);
controller.SetTuningParameters(parameters);
```

### Cohen-Coon Tuning

```csharp
// Process characteristics
double k = 2.0;    // Process gain
double tau = 5.0;  // Time constant
double theta = 1.0; // Dead time

// Get tuning parameters
var parameters = PIDTuningUtils.CohenCoon(k, tau, theta, CCTuningType.PID);
controller.SetTuningParameters(parameters);
```

## Event Handling

### Output Change Events

```csharp
controller.OutputChanged += (sender, e) =>
{
    Console.WriteLine($"Output changed to: {e.Output:F2}");
    // Log changes, update displays, etc.
};
```

### Status Change Events

```csharp
controller.StatusChanged += (sender, e) =>
{
    Console.WriteLine($"Controller status: {e.Status}");
    // Handle mode changes, errors, etc.
};
```

## Monitoring and Diagnostics

### Get Controller Statistics

```csharp
var stats = controller.GetStatistics();
Console.WriteLine($"Error: {stats.CurrentError:F2}");
Console.WriteLine($"Output: {stats.CurrentOutput:F2}");
Console.WriteLine($"P Term: {stats.ProportionalTerm:F2}");
Console.WriteLine($"I Term: {stats.IntegralTerm:F2}");
Console.WriteLine($"D Term: {stats.DerivativeTerm:F2}");
```

### Get Tuning Parameters

```csharp
var params = controller.GetTuningParameters();
Console.WriteLine($"Kp: {params.Kp:F2}");
Console.WriteLine($"Ki: {params.Ki:F2}");
Console.WriteLine($"Kd: {params.Kd:F2}");
```

## Performance Considerations

### Thread Safety
- All public methods are thread-safe
- Uses proper locking for concurrent access
- Safe for multi-threaded applications

### Memory Usage
- Minimal memory footprint
- No memory allocations during normal operation
- Efficient event handling

### Timing
- Configurable sample time
- Automatic time-based calculations
- Support for both fixed and variable sample times

## Best Practices

### 1. **Proper Tuning**
- Start with P-only control
- Add I term to eliminate steady-state error
- Add D term for stability if needed
- Use anti-windup for systems with output limits

### 2. **Sample Time Selection**
- Sample time should be 10-20x faster than process dynamics
- Too fast: computational overhead
- Too slow: poor control performance

### 3. **Output Limiting**
- Always set appropriate output limits
- Use anti-windup when limits are active
- Consider actuator saturation

### 4. **Noise Handling**
- Use derivative filtering for noisy signals
- Consider deadband for stable systems
- Filter process variable if necessary

### 5. **Mode Management**
- Implement proper manual/automatic switching
- Handle bumpless transfer between modes
- Monitor controller status

## Common Applications

### Industrial Control
- Temperature control systems
- Pressure control
- Flow control
- Level control
- Speed control

### Motion Control
- Position control
- Velocity control
- Torque control
- Servo systems

### Process Control
- Chemical processes
- HVAC systems
- Water treatment
- Power generation

### Robotics
- Joint position control
- End-effector control
- Force control
- Trajectory following

## Troubleshooting

### Oscillations
- Reduce proportional gain (Kp)
- Increase derivative gain (Kd)
- Check for noise in process variable

### Slow Response
- Increase proportional gain (Kp)
- Increase integral gain (Ki)
- Check sample time

### Steady-State Error
- Increase integral gain (Ki)
- Check for output limits
- Verify controller action (Direct/Reverse)

### Overshoot
- Reduce proportional gain (Kp)
- Increase derivative gain (Kd)
- Use setpoint weighting if available

## License

This PID controller implementation is provided as-is for educational and commercial use. No warranty is provided.

## Contributing

Contributions are welcome! Please feel free to submit issues, feature requests, or pull requests.

## Version History

- **v1.0**: Initial release with core PID functionality
- **v1.1**: Added anti-windup and output limiting
- **v1.2**: Added derivative filtering and deadband
- **v1.3**: Added tuning utilities and examples
- **v1.4**: Added event handling and monitoring 