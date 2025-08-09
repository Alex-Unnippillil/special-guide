# ADR 0003: Mouse Hook Library

## Context
The app must intercept the middle mouse button globally with minimal latency.

## Decision
Use **P/Invoke with `SetWindowsHookEx`** for a WH_MOUSE_LL hook.

## Rationale
- No external dependency; precise control over suppression behavior.
- Gma.System.MouseKeyHook or SharpHook are viable but add overhead.

## Consequences
- Requires careful unmanaged resource cleanup.
- Slightly more boilerplate but keeps binary small.
