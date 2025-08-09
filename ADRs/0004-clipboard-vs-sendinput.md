# ADR 0004: Clipboard vs SendInput

## Context
Selecting a radial slice should copy text and optionally paste into the focused app.

## Decision
Use **Clipboard.SetText** for copying and optional **SendInput** for simulated `Ctrl+V`.

## Rationale
- Clipboard API is reliable and respects user privacy toggles.
- SendInput enables auto-paste without requiring app-specific integrations.

## Consequences
- Auto-paste may fail in elevated or UWP apps; documented in README.
- Users can disable auto-paste to avoid unintended input.
