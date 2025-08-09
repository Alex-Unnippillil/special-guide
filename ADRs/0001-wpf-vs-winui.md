# ADR 0001: WPF vs WinUI

## Context
We require a desktop UI framework that supports transparent, topmost overlays and integrates with low-level hooks.

## Decision
Use **WPF on .NET 8**.

## Rationale
- Mature support for transparent windows and per-pixel hit testing.
- Broad ecosystem and tooling; easier integration with existing libraries.
- WinUI 3 overlay support is evolving and adds complexity via WinAppSDK runtime.

## Consequences
- Windows-only build, but meets target platforms (Win10/11).
- Enables leveraging community radial menu controls and proven patterns.
