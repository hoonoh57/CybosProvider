# Repository Guidelines

## Project Structure & Module Organization
CybosProvider is a VB.NET Windows Forms app targeting .NET Framework 4.8.1. Core data providers live in `Cybos.vb`, screeners and indicators in `Screener.vb`, `IndicatorManager.vb`, and `IndicatorInstance.vb`. The chart control resides under `chart/HighPerformanceChartControl.vb`. Designer partials and resources (`Form1.Designer.vb`, `Form1.resx`, `frmChartDialog.*`) define UI layouts. `My Project/` retains application settings, resources, and assembly metadata, while `App.config` holds runtime configuration. Build outputs appear under `bin/`, with intermediates in `obj/`.

## Build, Test, and Development Commands
Use Visual Studio 2022 (Developer Command Prompt) or standalone MSBuild. Typical commands:
- `msbuild .\CybosProvider.vbproj /t:Build /p:Configuration=Debug` to compile into `bin\Debug\`.
- `msbuild .\CybosProvider.vbproj /t:Clean` before switching branches or packaging.
Launch the app from Visual Studio (F5) to run `Form1`. Ensure the Cybos Plus COM components (CPSYSDIBLib, CPUTILLib, DSCBO1Lib) are registered locally before building.

## Coding Style & Naming Conventions
Follow the existing 4-space indentation and Option Explicit defaults. Keep classes, modules, and public members in PascalCase; local variables and private fields in camelCase. Group related helper methods and favor short, synchronous routines; extract to dedicated modules (e.g., `IndicatorManager.vb`) when logic grows. Store UI strings and assets in the corresponding `.resx` files rather than inline literals. Use single-quote comments to document intent above non-obvious logic or external API calls.

## Testing Guidelines
Automated tests are not yet present. When adding them, prefer MSTest or NUnit targeting .NET Framework 4.8.1, and place projects under `tests\`. Name test classes `{Subject}Tests` and individual cases `MethodName_State_ExpectedOutcome`. New features should include regression coverage for data providers and chart rendering helpers. Record manual verification steps for UI flows in the pull request until automated coverage exists.

## Commit & Pull Request Guidelines
Write commit messages in the present tense with a concise summary (<=72 characters) and optional detail in the body. Reference related issues using `#ID` when applicable. Pull requests should describe the change, note any COM registration or configuration prerequisites, list build/test commands executed, and include before/after screenshots for UI changes. Request a teammate review when altering shared providers or chart controls.
