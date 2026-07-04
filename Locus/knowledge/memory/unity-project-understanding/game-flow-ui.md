---
id: kd_0482b108-1b90-4bc2-a2db-0a8685969cba
type: memory
path: unity-project-understanding/game-flow-ui.md
title: game-flow-ui
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1783134720737
updatedAt: 1783137100117
---

# game-flow-ui

## Summary
Current prototype flow uses UGUI in SampleScene with GameFlowController driving Start, Settings, Info Collection, Broadcast, and Ending panels.

<!-- locus:body:start -->
- `Assets/Scripts/GameFlowController.cs` controls the initial prototype UI flow in `Assets/Scenes/SampleScene.unity`.
- `Assets/Scripts/InfoCollectionController.cs` drives the prototype information collection interaction in `InfoCollectionPanel`: document list, document hover tooltip, document selection, colored info-node buttons by `InfoNodeType`, and collected clue display.
- Scene roots added for the flow: `GameFlowController`, `GameFlowCanvas`, and `EventSystem`.
- `GameFlowCanvas` contains five full-screen panels: `StartPanel`, `SettingsPanel`, `InfoCollectionPanel`, `BroadcastPanel`, and `EndingPanel`.
- `InfoCollectionPanel` layout has `DocumentShelf` on the left, `DocumentViewPanel` in the middle, `CollectedInfoPanel` on the right, an `AudioEditButton` placeholder on the lower left, and `CompleteCollectionButton` on the lower right.
- Button flow: StartGameButton -> info collection, SettingsButton -> settings, ReturnMainMenuButton -> start, CompleteCollectionButton -> broadcast, CompleteBroadcastButton -> ending. Settings includes volume slider and resolution dropdown (1280x720, 1600x900, 1920x1080). Quit exits play mode in Editor or quits the app in builds.
- Project uses Legacy Input Manager and UGUI (`Canvas`, `Button`, `Text`, `Slider`, `Dropdown`) for this flow.
<!-- locus:body:end -->
