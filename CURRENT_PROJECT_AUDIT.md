# Current Project Audit

Date: 2026-07-04
Project: EnderBroadcast
Game working title: 《末日播报（暂）》
Branch observed: `shiyuan`

This round was limited to project understanding, project rules, and reporting. No scene, script, or prefab files were edited.

## 1. Directory Structure Overview

Repository root contains:

- `Assets`
- `Packages`
- `ProjectSettings`
- `Library`
- `Logs`
- `Temp`
- `UserSettings`
- `Locus`
- `obj`
- `.gitattributes`
- `.gitignore`
- `.vsconfig`
- `Assembly-CSharp.csproj`
- `EnderBroadcast.sln`
- `Locus.Editor.csproj`

Important `Assets` folders:

- `Assets/Scenes`: contains `SampleScene.unity` and `copy shiyuan.unity`.
- `Assets/Scripts`: contains all current gameplay scripts.
- `Assets/Prefabs`: exists, no files found in this scan.
- `Assets/Art`: exists, no files found in this scan.
- `Assets/Resources`: not present.
- `Assets/ScriptableObjects`: not present.
- `Assets/Data`: not present.
- `Assets/Audio`: not present.

Package notes:

- `Packages/manifest.json` includes `com.gamebooom.unity.mcp` from `https://github.com/FunplayAI/funplay-unity-mcp.git`.
- `Packages/com.farlocus.locus` exists and is Git LFS-related in this working tree.

## 2. Scene List And Roles

Current scenes:

- `Assets/Scenes/SampleScene.unity`: team/base scene. Do not directly modify.
- `Assets/Scenes/copy shiyuan.unity`: personal development scene. Future feature work should happen here.

Observed via Unity MCP:

- Active Unity scene: `copy shiyuan`
- Active scene path: `Assets/Scenes/copy shiyuan.unity`
- Active scene dirty state: `False`

File comparison:

- `SampleScene.unity` and `copy shiyuan.unity` currently have the same SHA256 hash.
- This suggests `copy shiyuan` is currently a full copy of `SampleScene`.

Git status note:

- `copy shiyuan.unity` and `copy shiyuan.unity.meta` are currently untracked.
- `SampleScene.unity` did not appear as modified in the LFS-disabled status check.

## 3. Current `copy shiyuan` Scene Structure

Root objects in `copy shiyuan`:

- `Main Camera`
- `Directional Light`
- `GameFlowController`
- `GameFlowCanvas`
- `EventSystem`

Scene object count observed through Unity MCP:

- GameObjects: 86
- EventSystem count: 1
- Missing Script count: 0

Key UI panels and objects found:

- `StartPanel`
- `InfoCollectionPanel`
- `BroadcastPanel`
- `EndingPanel`
- `SettingsPanel`
- `StudioPanel`
- `InfoOrganizationPanel`
- `DayTransitionPanel`
- `EventSystem`
- `DocumentShelf`
- `DocumentListRoot`
- `DocumentButtonTemplate`
- `DocumentViewPanel`
- `DocumentTitleText`
- `DocumentBodyText`
- `DocumentSegmentRoot`
- `DocumentSegmentTemplate`
- `InfoHotspotRoot`
- `InfoHotspotTemplate`
- `CollectedInfoPanel`
- `CollectedInfoRoot`
- `CollectedInfoTemplate`
- `DocumentTooltipText`

## 4. Script Inventory

Current scripts under `Assets/Scripts`:

- `BroadcastCoreData.cs`
- `GameFlowController.cs`
- `InfoCollectionController.cs`

No ScriptableObject asset files were found in `Assets`.

## 5. Key Script Responsibility Assessment

### `GameFlowController.cs`

Current responsibilities:

- Holds serialized references to major UI panels.
- Shows the start screen on `Awake`.
- Switches between start, studio, information collection, information organization, broadcast, settings, and next-day transition.
- Controls simple day counter.
- Runs transition coroutine through `DayTransitionPanel`.
- Handles settings UI callbacks for volume and resolution.
- Handles quit behavior.

Current flow methods:

- `ShowStartScreen`
- `StartGame`
- `EnterInfoCollection`
- `CompleteCollection`
- `StartBroadcast`
- `CompleteBroadcast`
- `EnterNextDay`
- `OpenSettings`
- `ReturnToMainMenu`

Assessment:

- Useful as a temporary P0 flow controller.
- It does not currently define a formal flow-state enum.
- As the project grows, day progression, settlement, ending selection, and broadcast result processing should move out of direct UI panel switching.

Serialized references observed:

- All key `GameFlowController` serialized references are currently bound in `copy shiyuan`.

### `BroadcastCoreData.cs`

Current responsibilities:

- Defines serializable data classes matching much of the planning draft.
- Includes `BroadcastDayData`, `EnvelopeData`, `DocumentData`, `InfoNodeData`, `InfoNodeConnectionData`, `AudioTrackData`, `AudioNodeData`, `NodeEffectData`, and `BroadcastResultData`.
- Defines `InfoNodeType`.

Assessment:

- This file is the strongest match to the proposed data system.
- It is plain serializable data, not ScriptableObject-backed yet.
- It already includes Trust and Chaos effect fields through `NodeEffectData`.
- It can be retained and later adapted into ScriptableObject or external data loading.

### `InfoCollectionController.cs`

Current responsibilities:

- Owns current day sample data.
- Builds document buttons.
- Displays selected document title and body segments.
- Splits text into clickable highlighted segments based on `InfoNodeData.displayText`.
- Collects clicked info nodes into a collected-info list.
- Updates `BroadcastResultData.totalEffects`.
- Defines helper event handlers: `InfoTextSegmentHandler`, `InfoHotspotHandler`, and `DocumentHoverHandler`.

Assessment:

- It already provides an early information extraction prototype.
- It currently mixes UI construction, sample data creation, interaction handling, and scoring/data mutation.
- `EnsureSampleData` uses hardcoded temporary content.
- `HoverInfo` and `ClearHoveredInfo` are currently empty.
- Audio collection/editing is not implemented beyond data type placeholders.

Serialized references observed:

- `InfoCollectionController` is on `GameFlowCanvas/InfoCollectionPanel`.
- Its key serialized references are currently bound.

## 6. Current Framework Versus Planning Draft

Planning draft layer mapping:

- Flow system: partially present through `GameFlowController`.
- Data system: partially present through `BroadcastCoreData`.
- Interaction system: partially present inside `InfoCollectionController` and its pointer handlers.
- UI system: partially present through scene panels and UGUI components.
- Broadcast system: not meaningfully implemented yet.

Data draft mapping:

- `Day`: represented by `BroadcastDayData`.
- `Envelope`: represented by `EnvelopeData`.
- `Document`: represented by `DocumentData`.
- `InfoNode`: represented by `InfoNodeData`.
- `Connection`: represented by `InfoNodeConnectionData`.
- `Audio`: represented by `AudioTrackData`.
- `AudioNode`: represented by `AudioNodeData`.
- `BroadcastResult`: represented by `BroadcastResultData`.
- `Trust` and `Chaos`: represented by `NodeEffectData.trust` and `NodeEffectData.chaos`.

The current code closely matches the proposed data shape, but it is still prototype-level and scene-driven.

## 7. Features Already Present

- Start screen panel exists.
- Settings panel exists.
- Studio panel exists.
- Information collection panel exists.
- Information organization panel exists.
- Broadcast panel exists.
- Ending panel exists.
- Day transition panel exists.
- Main flow buttons are wired for start, settings, quit, enter collection, complete collection, start broadcast, complete broadcast, next day, and return main menu.
- Basic volume slider and resolution dropdown callbacks are wired.
- Basic document list generation exists.
- Basic clickable text extraction exists.
- Collected text snippets can appear in the collected-info board.
- Trust/Chaos effects can be accumulated when collecting info nodes.
- Unity compilation currently reports no errors or warnings.
- No Missing Script components were found in the active scene.
- Exactly one EventSystem was found.

## 8. UI Shells And Not-Yet-Implemented Features

Currently shell-level or incomplete:

- Information organization is only a panel plus a start-broadcast button; free ordering, max 3 articles, max 3 inserts per article, and mixed text/audio insertion are not implemented.
- Broadcast screen exists, but does not generate or play actual daily broadcast subtitles from organized content.
- Settlement logic is not implemented.
- Ending selection is not implemented.
- Bad/Normal ending framework is not implemented beyond an `EndingPanel` shell.
- Audio information collection is not implemented beyond `AudioTrackData` and `AudioNodeData` structures.
- `AudioEditButton` has no OnClick binding.
- Emergency news mechanism is not implemented.
- Broadcast CG/animation playback is not implemented.
- Persistent day data assets are not present.
- ScriptableObject data pipeline is not present.
- Current document data is hardcoded in `InfoCollectionController.EnsureSampleData`.

## 9. Recommended P0 Minimal Loop

Best next P0 target:

Start game -> day intro -> studio -> information collection -> collect text snippets -> information organization -> manually confirm broadcast -> broadcast subtitles -> settlement -> next day or ending.

Smallest practical implementation sequence:

1. Introduce a clear flow-state enum and a light `GameFlowState` transition method.
2. Move hardcoded sample day data out of `InfoCollectionController` into a data provider or asset-like structure.
3. Keep `BroadcastCoreData` as the shared data contract.
4. Add an organization model that stores selected snippets in ordered broadcast slots.
5. Add manual confirmation from organization to broadcast.
6. Generate simple subtitle text from organized snippets.
7. Add a simple settlement result that applies Trust and Chaos totals.
8. Add a day limit or threshold-based branch to Normal/Bad ending.

Important boundary:

- Do not put scoring, ending rules, day progression, or broadcast generation directly into UI button scripts.

## 10. Risks And Issues

Scene and Git risks:

- `copy shiyuan.unity` and `.meta` are currently untracked, so the personal development scene may be lost or absent for teammates unless added intentionally.
- `git status` initially failed because the Git LFS clean filter hit an access-denied error on `Packages/com.farlocus.locus/Editor/Roslyn/Locus.Roslyn.dll`.
- A second status check with LFS filters disabled showed modified package/project files: `Packages/com.farlocus.locus/Editor/Roslyn/Locus.Roslyn.dll`, `Packages/manifest.json`, `Packages/packages-lock.json`, and `ProjectSettings/ProjectSettings.asset`.
- `.vsconfig` is untracked.
- `SampleScene.unity` did not appear modified in the LFS-disabled status check.

UI and scene risks:

- `GameFlowCanvas/DayTransitionPanel` has `CanvasGroup.alpha = 0` and `blocksRaycasts = true`. This can create transparent UI blocking if the GameObject is active while hidden.
- `AudioEditButton` has an empty OnClick binding.
- `DocumentButtonTemplate` and `InfoNodeButtonTemplate` have empty OnClick lists. This is probably acceptable for templates, but should be documented as intentional.
- No null button OnClick targets were found in the active scene.
- No Missing Script components were found in the active scene.
- No multiple EventSystem issue was found.
- No obvious lost prefab instance source was found in the scanned scene YAML.

Architecture risks:

- `InfoCollectionController` currently mixes UI rendering, interaction, hardcoded data, and result mutation.
- `GameFlowController` currently uses direct panel activation instead of a formal state model.
- No shared runtime data/session object exists yet to carry collected and organized choices between collection, organization, broadcast, settlement, and ending.
- No actual `ScriptableObject` data assets exist yet.
- No audio asset folder or audio data content exists yet.
- Some source text displayed through PowerShell appears mojibake, though Unity compilation reports no errors. Confirm file encoding and intended Chinese text inside the Unity editor before content work.

## 11. Next Step Suggestions

Recommended next round:

1. First commit or otherwise intentionally track `Assets/Scenes/copy shiyuan.unity` and its `.meta` so the personal scene is safe.
2. Fix or consciously defer the `DayTransitionPanel` CanvasGroup blocking risk.
3. Establish Shiyuan-owned folders before adding personal assets.
4. Implement P0 around existing structures instead of replacing them:
   - keep `BroadcastCoreData` as the data contract,
   - split sample data loading away from `InfoCollectionController`,
   - add organization and broadcast result flow in small steps.
5. Add a minimal test/playthrough checklist for one-day flow.

## Files Modified In This Round

- `AGENTS.md`
- `CURRENT_PROJECT_AUDIT.md`
