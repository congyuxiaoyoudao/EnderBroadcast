# EnderBroadcast Project Rules

## Project Identity

- Project name: EnderBroadcast
- Working title: 《末日播报（暂）》
- Development branch: `shiyuan`
- Personal development scene: `Assets/Scenes/shiyuan.unity`

## Scene And Branch Rules

- All future feature development must happen in `shiyuan`.
- Do not directly modify `Assets/Scenes/SampleScene.unity`.
- Do not develop directly on `main`.
- Team synchronization workflow: merge `main` into the working branch.
- Do not use rebase for team synchronization.
- Do not force push.
- New personal assets should be placed under Shiyuan-specific directories first, for example `Assets/Shiyuan/...`, unless the team agrees on a shared location.

## File Deletion Safety

- Do not batch-delete files or directories.
- Do not use `del /s`, `rd /s`, `rmdir /s`, `Remove-Item -Recurse`, or `rm -rf`.
- If a file must be deleted, delete exactly one explicit file path at a time.
- If batch deletion seems necessary, stop and ask the user to delete files manually.

## System Layering Principles

- Flow system: owns game-state transitions such as start, collection, organization, broadcast, settlement, next day, and ending.
- Data system: owns news, envelopes, documents, audio, days, broadcast results, Trust, Chaos, and other game data. It must not depend on concrete UI objects.
- Interaction system: owns player operations such as click, select, drag, reorder, audio edit gestures, and confirmation actions.
- UI system: displays state and receives input. UI scripts should forward player intent and should not own core business rules, scoring, day progression, or data generation.
- Broadcast system: generates daily broadcast output, subtitles, settlement, and ending presentation from organized results. Ending playback should reuse broadcast or settlement presentation where practical.

## Implementation Boundaries

- Keep feature work scoped to the personal scene and Shiyuan-owned assets unless explicitly requested otherwise.
- Preserve existing scene and prefab references when extending UI.
- Prefer adding reusable data and flow classes over embedding game logic in button handlers.
- Before feature edits, inspect the existing scene wiring and serialized references.
- When creating new data assets, prefer clear folders such as `Assets/Shiyuan/Data`, `Assets/Shiyuan/ScriptableObjects`, `Assets/Shiyuan/Prefabs`, `Assets/Shiyuan/Art`, and `Assets/Shiyuan/Audio`.
