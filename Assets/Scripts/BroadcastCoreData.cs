using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BroadcastDayData
{
    public string id;
    public int dayIndex;
    public EnvelopeData envelope = new EnvelopeData();
    public BroadcastResultData broadcastResult = new BroadcastResultData();
}

[Serializable]
public class EnvelopeData
{
    public string id;
    public List<DocumentData> documents = new List<DocumentData>();
    public List<AudioTrackData> audioTracks = new List<AudioTrackData>();
}

[Serializable]
public class DocumentData
{
    public string id;
    public string displayName;
    [TextArea(3, 10)] public string fullText;
    public List<InfoNodeData> infoNodes = new List<InfoNodeData>();
    public List<InfoNodeConnectionData> connections = new List<InfoNodeConnectionData>();
}

[Serializable]
public class InfoNodeData
{
    public int id;
    [TextArea(2, 6)] public string displayText;
    public InfoNodeType type;
    public int priority;
    public bool isMandatory;
    public NodeEffectData effects = new NodeEffectData();
    [TextArea(2, 6)] public string extractedText;
}

[Serializable]
public class InfoNodeConnectionData
{
    public int fromNodeId;
    public int toNodeId;
    public NodeEffectData effects = new NodeEffectData();
}

[Serializable]
public class AudioTrackData
{
    public string id;
    public string displayName;
    public List<AudioNodeData> audioNodes = new List<AudioNodeData>();
}

[Serializable]
public class AudioNodeData
{
    public int id;
    public AudioClip audioFile;
    [TextArea(2, 6)] public string contentText;
    public float displayTime;
}

[Serializable]
public class NodeEffectData
{
    public int trust;
    public int chaos;
}

[Serializable]
public class BroadcastResultData
{
    public List<int> collectedInfoNodeIds = new List<int>();
    public List<int> selectedAudioNodeIds = new List<int>();
    public NodeEffectData totalEffects = new NodeEffectData();
    [TextArea(3, 10)] public string finalBroadcastText;
}

public enum InfoNodeType
{
    Normal,
    KeyClue,
    Character,
    Location,
    Evidence,
    Rumor
}
