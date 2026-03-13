# WPF Thumb Primitive Parity Integration

## Context
The directive states:
"Identify a missing WPF control paradigm ... with a proposed structural mapping to the character buffer constraints."
"We will discard this ambiguity. The framework must provide a 1:1 behavioral mapping to WPF structural concepts."
"Replicate the Routed Event infrastructure. You must implement both tunneling (Preview) and bubbling event routing strategies."

From memory:
`In Tedd.TUI, the Thumb primitive control inherits from Control and provides standard WPF-equivalent bubbling routed events for drag lifecycles: DragStarted, DragDelta, and DragCompleted.`

Currently, the framework uses ad-hoc logic in `ScrollBar` for thumb dragging. Implementing `Thumb` as a primitive control creates structural parity and is a prerequisite for correctly building templates for `ScrollBar` and `Slider`.

## Parity Deficit
- The `Thumb` primitive control is entirely missing from `Tedd.TUI`.
- `ScrollBar` and `Slider` currently hardcode drag mechanics and rendering, violating WPF architectural norms where templates house a `Thumb` and `RepeatButton`s.
- There are no routed events for drag lifecycles (`DragStarted`, `DragDelta`, `DragCompleted`).

## Plan
1.  **Create `Thumb` Primitive (`src/Tedd.TUI/Thumb.cs`)**:
    - Inherits from `Control`.
    - Implements WPF drag-related routed events: `DragStartedEvent`, `DragDeltaEvent`, `DragCompletedEvent` (bubbling).
    - Event argument classes: `DragStartedEventArgs`, `DragDeltaEventArgs`, `DragCompletedEventArgs` inheriting from `RoutedEventArgs`.
    - Handles internal state for drag initiation (`OnMouseDown`), delta updates (`OnMouseMove`), and completion (`OnMouseUp`).
    - Uses `TuiWindow.CaptureMouse(this)` and `ReleaseMouseCapture()` to lock focus during drags, identical to the hardcoded logic currently in `ScrollBar`.

2.  **Add `Thumb` telemetry to `.jules/forge.md`**.
