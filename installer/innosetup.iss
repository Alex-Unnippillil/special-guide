; Inno Setup script placeholder
[Setup]
AppName=Special Guide
AppVersion=1.0.0
DefaultDirName={autopf}\SpecialGuide
OutputDir=..

[Files]
Source: "..\publish\SpecialGuide.App.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\Special Guide"; Filename: "{app}\SpecialGuide.App.exe"
