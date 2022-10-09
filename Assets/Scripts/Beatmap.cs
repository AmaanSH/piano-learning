using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The lane the note is meant to fall into
/// </summary>
public enum Lane
{
    Lane1,
    Lane2,
    Lane3,
    Lane4
}

/// <summary>
/// Note information
/// </summary>
[System.Serializable]
public class NoteInfo
{
    public float beat;
    public float endBeat;

    public NoteType noteType;

    public Lane lane;

    public Note note;
}

/// <summary>
/// Responsible for creating beatmap structure
/// </summary>
[CreateAssetMenu(fileName = "Beatmap", menuName = "Beatmaps/ Create Beatmap", order = 0)]
public class Beatmap : ScriptableObject
{
    public string mapName;
    public string songArtist;

    public string mapArtist;
    public string mapDescription;
    public Sprite albumArt;

    public AudioClip songAudio;

    public float bpm;
    public float startOffset = 0;
    public List<NoteInfo> notes;
}