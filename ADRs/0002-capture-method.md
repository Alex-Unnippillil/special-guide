# ADR 0002: Screen Capture Method

## Context
We must capture the active window screenshot for AI analysis.

## Decision
Use **Windows.Graphics.Capture** when available; fallback to `Graphics.CopyFromScreen`.

## Rationale
- Windows.Graphics.Capture offers high performance and captures without window chrome.
- CopyFromScreen works on older systems or when capture API is unavailable.

## Consequences
- Requires Win10 1903+ for best results.
- Fallback path may capture entire screen with decorations.
