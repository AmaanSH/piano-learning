using System.Collections.Generic;
using UnityEngine;

public class LaneStatus
{
    public Lane lane;
    public bool canSpawnNotes;
    public float beatTillCanSpawn;
}

class LastHeldBeat
{
    public float beat;
    public Lane lane;
}

/// <summary>
/// The style of note this is
/// </summary>
public enum NoteType
{
    Normal,
    LongNote,
    Staircase,
    Jackhammer,
    Chord,
    Burst,
    Roll,
    Shield,
    ChordJack,
    Trill,
    Chrodtrill,
    JumpStream
}

/// <summary>
/// Map difficulty
/// </summary>
public enum Difficulty
{
    Easy,
    Medium,
    Hard
}

/// <summary>
/// Class responsible for creating new beatmaps, WIP for now
/// </summary>
public class BeatmapManager : MonoBehaviour
{
    public static List<LaneStatus> laneStatus = new List<LaneStatus>();

    public static void SetupLanes()
    {
        LaneStatus lane1 = new LaneStatus();
        LaneStatus lane2 = new LaneStatus();
        LaneStatus lane3 = new LaneStatus();
        LaneStatus lane4 = new LaneStatus();

        lane1.lane = Lane.Lane1;
        lane1.canSpawnNotes = true;
        laneStatus.Add(lane1);

        lane2.lane = Lane.Lane2;
        lane2.canSpawnNotes = true;
        laneStatus.Add(lane2);

        lane3.lane = Lane.Lane3;
        lane3.canSpawnNotes = true;
        laneStatus.Add(lane3);

        lane4.lane = Lane.Lane4;
        lane4.canSpawnNotes = true;
        laneStatus.Add(lane4);
    }

    public static void GenerateNotes(float songLength, float tempo, Beatmap beatmap)
    {
        // remove all the notes
        beatmap.notes.Clear();

        // generate lanes
        SetupLanes();

        int normalNotes = 0;
        int heldNotes = 0;
        int chord = 0;
        int jackhammer = 0;
        int chordJack = 0;

        // determine how many beats in whole of song
        float totalBeats = Mathf.Floor(songLength / tempo);

        // TODO: mp3 audio parser
        // On a high peak generate certain notes
        // on louder peaks walls, jackhammer etc
        // low peak single/chords?
        // scales for building up phases
        // hold note and scales next to it!!!!

        for (int i = 0; i < totalBeats; i++)
        {
            // Go through all the notes
            // pick a style
            // put it in the map

            // THIS IS FOR TESTING DO NOT KEEP THIS USE MP3 FILE
            float chordChance = Random.value;
            float jackhammerChance = Random.value;
            float chordjackChance = Random.value;

            if (jackhammerChance > 0.95f)
            {
                // jackhammer
                JackhammerNote jackhammerNote = new JackhammerNote();
                jackhammerNote.Generate(beatmap, i);

                jackhammer += 1;

                //Debug.LogFormat("Generating JackHammer on beat {0}", i);
            }
            else if (chordjackChance > 0.95f)
            {
                // chordjack
                ChordJackNote chordjackNote = new ChordJackNote();
                chordjackNote.Generate(beatmap, (float)i);

                chordJack += 1;

                //Debug.LogFormat("Generating ChordJackNote on beat {0}", i);
            }
            else if (chordChance > 0.7f)
            {
                // make a chord
                ChordNote chordNote = new ChordNote();
                chordNote.Generate(beatmap, i);

                chord += 1;

                //Debug.LogFormat("Generating ChordNote on beat {0}", i);
            }
            else
            {
                // normal
                BaseNote baseNote = new BaseNote();
                baseNote.Generate(beatmap, i);

                normalNotes += 1;

                //Debug.LogFormat("Generating BaseNote on beat {0}", i);
            }
        }

        Debug.LogFormat("{0} beats! Generated {1} Normal {2} Held {3} Chord {4} Jackhammer {5} ChordJack {6}", totalBeats, normalNotes + heldNotes + chord, normalNotes, heldNotes, chord, jackhammer, chordJack);
    }

    public static List<LaneStatus> GetLaneStatuses()
    {
        return laneStatus;
    }

    public static List<LaneStatus> GetAvailableLanes()
    {
        return laneStatus.FindAll(x => x.canSpawnNotes == true);
    }

    /// <summary>
    /// Update the lane status
    /// </summary>
    /// <param name="lane">The lane to update</param>
    /// <param name="canSpawn">If the lane can spawn notes</param>
    public static void UpdateLaneStatus(Lane lane, bool canSpawn)
    {
        LaneStatus foundLane = laneStatus.Find(x => x.lane == lane);
        if (foundLane != null)
        {
            foundLane.canSpawnNotes = canSpawn;
            //Debug.LogFormat("Lane {0} canSpawn {1}", lane, canSpawn);
        }
    }

    public static Lane GetOppositeLane(Lane lane)
    {
        switch (lane)
        {
            case Lane.Lane1:
                return Lane.Lane3;
            case Lane.Lane2:
                return Lane.Lane4;
            case Lane.Lane3:
                return Lane.Lane1;
            case Lane.Lane4:
                return Lane.Lane2;
        }

        return Lane.Lane1;
    }

    public static Lane GetLane(int number)
    {
        switch (number)
        {
            case 1:
                return Lane.Lane1;
            case 2:
                return Lane.Lane2;
            case 3:
                return Lane.Lane3;
            case 4:
                return Lane.Lane4;
        }

        return Lane.Lane1;
    }

    public static Lane GetRandomLane()
    {
        List<LaneStatus> lanesAvailable = laneStatus.FindAll(x => x.canSpawnNotes == true);

        if (lanesAvailable.Count == 0)
        {
            Debug.Log("No more lanes!");
            return lanesAvailable[0].lane;
        }

        int random = Random.Range(0, lanesAvailable.Count);
        return lanesAvailable[random].lane;
    }
}
