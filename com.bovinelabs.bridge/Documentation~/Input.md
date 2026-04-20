# Input

## Summary
Input provides source generation for Unity Input System integration with ECS. Define input components with attributes and the system automatically generates the required systems and bindings.

## Key Features
- `[InputAction]`, `[InputActionDelta]`, `[InputActionDown]`, and `[InputActionUp]` attributes for automatic binding.
- `InputCommon` singleton with common input utilities.
- `InputAPI` and `InputActionMapEnable` for enabling and disabling action maps at runtime.
- Settings-driven binding via `InputCommonSettings`.
- Menu world support via the `MenuEnabled` action map list.

## Defining Input Components
Create input components using the input attributes:

```csharp
public partial struct PlayerInput : IComponentData
{
    [InputAction]
    public float SingleAxis;

    [InputAction]
    public float2 Axis; // Mouse or stick

    [InputActionDelta]
    public float2 AxisDelta; // Modified by delta time

    [InputAction]
    public bool Button; // Pressed

    [InputAction]
    public ButtonState ButtonEvents; // Down, Pressed, Up events

    [InputActionDown]
    public bool ButtonDown;

    [InputActionUp]
    public bool ButtonUp;
}
```

## Setup
1. Open Input Common settings via `BovineLabs -> Settings -> Bridge -> Input Common`.
2. Assign the `InputActionAsset`.
3. Click `Find all Input` to discover new input components.
4. Assign `InputActionReference` assets to each field.
5. Configure `Default Enabled` and optional `Menu Enabled` action maps.

## Assembly Dependencies

Your assembly must reference:
- `BovineLabs.Bridge.Input` (runtime)
- `Unity.InputSystem`

For authoring components:
- `BovineLabs.Bridge.Input.Authoring`

## InputCommon
`InputCommon` provides common input utilities updated every frame:

```csharp
var inputCommon = SystemAPI.GetSingleton<InputCommon>();

inputCommon.CursorScreenPoint;    // Screen space cursor position
inputCommon.CursorViewPoint;      // Viewport space cursor position
inputCommon.CursorInViewPort;     // Is cursor in viewport
inputCommon.InputOverUI;          // Is cursor over UI
inputCommon.CameraRay;            // Ray from camera through cursor
inputCommon.AnyButtonPress;       // Any button pressed this frame
```

## InputAPI
Enable or disable action maps at runtime. Calls are queued and applied by `InputActionMapSystem`.

```csharp
public partial struct PlayerInputSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        InputAPI.InputEnable(ref state, "PlayerInput");
        InputAPI.InputDisable(ref state, "Menu");
    }
}
```

## Custom Input Settings
Implement `IInputSettings` and add the settings object to `InputCommonSettings` to bake custom input data via `InputSettingsBaker`.

## Common Issues
Source generator not running:
- Missing `partial` keyword on the component struct.
- Component does not implement `IComponentData`.
- Missing asmdef references to `BovineLabs.Bridge.Input`.

Input not responding:
- `InputActionAsset` not assigned in `InputCommonSettings`.
- Action maps not enabled in `Default Enabled` or `Menu Enabled`.
- `InputActionReference` assets not assigned to component fields.

Setup issues:
- `Find all Input` not run after creating new components.
- `BovineLabs.Bridge.Input` asmdef not referenced.
