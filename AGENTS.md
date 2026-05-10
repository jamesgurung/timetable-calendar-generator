# AGENTS

## Instructions

- Make the smallest reasonable code change that fully solves the task, keeping the diff to a minimum.
- Prefer concise implementations over broad refactors.
- Do not edit or reformat unrelated code. Keep edits local to the relevant files unless a wider change is required.
- Avoid introducing new dependencies unless they are clearly necessary.
- Reuse existing code and patterns in the codebase where possible.
- To avoid interfering with active processes, run validation builds with a temporary artifacts directory outside the repo, then delete it afterwards. Example PowerShell flow: `$artifacts = Join-Path $env:TEMP ("dotnet-build-" + [guid]::NewGuid().ToString("N")); dotnet build --artifacts-path $artifacts; Remove-Item -LiteralPath $artifacts -Recurse -Force`
- Do not introduce any test projects.
- Do not access files named `appsettings.json`, `secrets.json`, or `local.settings.json` under any circumstances.

## Editing

- Avoid editing more than one file in a single tool call, as a context miss can lead the whole change to be rejected. If multiple files need to be edited, make the changes in separate tool calls.
- When the user sends a follow-up message, assume that they may have edited the codebase. Always check the latest code before making further edits.

## Code Style

- Match the existing codebase style, structure, naming, and patterns.
- Use 2-space indentation, spaces not tabs, CRLF line endings, and a final newline.
- Avoid adding small helper methods that are only called once. Prefer inline code when it is clear and concise.
- Use small defensive guards only when essential to prevent errors. Avoid over-engineering.
- When writing C#, prefer:
  - `var` for locals, including built-in and obvious types
  - early returns
  - compact methods
  - LINQ-heavy query composition
  - expression-bodied members when they fit on one line
  - omitting braces for single-line blocks
  - no nullable annotations (`string?`, null-forgiving `!`) as this C# feature is disabled
- When writing JavaScript, prefer:
  - modern ES2025+ syntax
  - semicolons
  - no trailing commas
  - `const` for variables that are not reassigned and `let` for those that are
  - DOM lookups, ideally in an `elements` object near the top
