---
id: kd_1c04a581-7ea7-4416-ba13-39b1ed87c3cd
type: design
path: core-game-data.md
title: core-game-data
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1783136037598
updatedAt: 1783136074220
---

# core-game-data

## Summary
核心玩法以天为循环单位；每天一个信封，信封包含文件和音频，文件由信息节点和连接组成，音频由音频节点组成，播报结果记录收集内容和影响效果。

## Content
## 核心循环

游戏以“天”为单位推进。每一天提供一个信封，玩家在信息收集界面阅读文件、处理音频，并将收集到的线索用于后续播报。

## 数据层级

- `BroadcastDayData`：单日数据入口，包含天数、信封和当天播报结果。
- `EnvelopeData`：当天信封，包含多个文件和多个音频轨道。
- `DocumentData`：文件数据，包含完整文本、多个信息节点和节点连接。
- `InfoNodeData`：文件中的可提取信息节点，包含 ID、显示文本、节点类型、优先级、是否必选、影响效果、提取出的信息文本。
- `InfoNodeConnectionData`：信息节点之间的关联，记录起点节点 ID、终点节点 ID 和该连接带来的影响效果。
- `AudioTrackData`：一组音频内容，包含多个音频节点。
- `AudioNodeData`：音频节点，包含 ID、音频文件、字幕/内容文本和字幕显示时间。
- `NodeEffectData`：节点或连接对播报结果的数值影响，目前包含民众信任度 `trust` 和地区混乱度 `chaos`。
- `BroadcastResultData`：当天播报结果，记录已收集信息节点、已选择音频节点、总影响效果和最终播报文本。

## 当前实现

核心数据结构单独放在 `Assets/Scripts/BroadcastCoreData.cs`。
