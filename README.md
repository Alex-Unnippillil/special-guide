# Special Guide

Special Guide replaces the middle-mouse button with a radial AI assistant. When triggered it captures the current screen, asks OpenAI for context-aware prompts, and lets the user pick one to copy (and optionally paste) into the active application. A voice button records audio and sends it to OpenAI Whisper for transcription.

## Features
- Global middle-click hook with suppression
- Radial overlay rendered at the cursor
- Screenshot capture and OpenAI suggestion generation
- Optional auto-paste after selecting a prompt
- Voice recording and transcription
- Settings stored in `%AppData%/SpecialGuide`

## Setup
1. Install [.NET 8 SDK](https://dotnet.microsoft.com/download).
2. `cp .env/appsettings.sample .env/appsettings` and set `OPENAI_API_KEY`.
3. Build and run:
   ```bash
   dotnet restore
   dotnet build -c Release
   dotnet run --project src/SpecialGuide.App
   ```

## Testing
```bash
dotnet test
```

## Packaging
```bash
dotnet publish src/SpecialGuide.App/SpecialGuide.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Privacy
Screenshots and audio are sent only to OpenAI and are not stored unless logging is enabled. Window titles can be redacted via settings.

## License
MIT
