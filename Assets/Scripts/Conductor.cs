using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HitScore
{
    Perfect,
    Good,
    Okay,
    Miss
}

public enum ConductorStatus
{
    Countdown,
    Playing,
    Paused,
    Finished
}

public enum GameMode
{
    Standard,
    Arcade,
    Infinite
}

/// <summary>
/// Responsible for keeping track of timing and note spawning
/// </summary>
public class Conductor : MonoBehaviour
{
    public static Conductor instance;

    public GameMode gameMode;

    // do we want to do this?
    // dont need that...
    //blic GameBoardManifest boardManifest;

    public Transform boardTransform;

    public float BPM { get; protected set; }
    public float tempo { get; protected set; }
    public float songPosition { get; protected set; }

    // Current song position, in beats
    public float songPositionInBeats { get; protected set; }

    // how many seconds have passed since the start of the song
    private float dspSongTime;

    public AudioSource audioSource;

    private float firstBeatOffset;
    private int nextIndex = 0;
    private List<NoteInfo> notes;

    public int beatsShownInAdvance = 1;

    public Beatmap beatmap;

    public bool generateBeatmap = false;

    private ConductorStatus status;

    // store each note in its corressponding lane
    public Queue<NoteInfo> lane1 = new Queue<NoteInfo>();
    public Queue<NoteInfo> lane2 = new Queue<NoteInfo>();
    public Queue<NoteInfo> lane3 = new Queue<NoteInfo>();
    public Queue<NoteInfo> lane4 = new Queue<NoteInfo>();

    public float PerfectOffset;
    public float GoodOffset;
    public float OkayOffset;

    private int perfectHits = 0;
    private int goodHits = 0;
    private int okayHits = 0;
    private int missedHits = 0;

    //private GameBoard board;

    public void Start()
    {
        if (generateBeatmap)
        {
            BeatmapManager.GenerateNotes(beatmap.songAudio.length, 60f / beatmap.bpm, beatmap);
            return;
        }

        instance = this;

        BPM = beatmap.bpm;

        notes = beatmap.notes;
        tempo = 60f / BPM;
        firstBeatOffset = beatmap.startOffset;

        // load the correct gameboard
        GameBoardAsset asset = boardManifest.GetGameBoard(gameMode);
        if (asset)
        {
            GameObject gameBoard = Instantiate(asset.gameBoard, boardTransform);
            board = gameBoard.GetComponent<GameBoard>();
        }
        else
        {
            Debug.LogErrorFormat("No gameboard found for game mode {0}", gameMode);
            return;
        }

        // assign methods to input event
        InputController.userInput += HandleInput;

        UIController.SetupGameUI(beatmap.albumArt, beatmap.mapName, board.complimentText, board.endTargets);

        LevelManager.Init();

        StartCoroutine(CountdownRoutine());
    }

    IEnumerator CountdownRoutine()
    {
        int count = 3;
        status = ConductorStatus.Countdown;

        // set the audio source clip
        audioSource.clip = beatmap.songAudio;

        while (count != 0)
        {
            Debug.Log(count);

            count -= 1;
            yield return new WaitForSeconds(1f);
        }

        audioSource.Play();

        // record start of song
        dspSongTime = (float)AudioSettings.dspTime;

        StartCoroutine(Monitor());
    }

    private void OnDestroy()
    {
        InputController.userInput -= HandleInput;
    }

    private void End()
    {
        status = ConductorStatus.Finished;

        Debug.LogFormat("End. Final scores. Perfect {0} Good {1} Okay {2} Missed {3}", perfectHits, goodHits, okayHits, missedHits);

        StopCoroutine(Monitor());
    }

    private IEnumerator Monitor()
    {
        // TODO: handle changing BPMs


        status = ConductorStatus.Playing;

        while (audioSource.isPlaying && nextIndex <= notes.Count)
        {
            // determine how many seconds since the song started
            songPosition = (float)(AudioSettings.dspTime - dspSongTime - firstBeatOffset) * audioSource.pitch - firstBeatOffset;

            if (status == ConductorStatus.Paused)
            {
                audioSource.Pause();

                while (status == ConductorStatus.Paused)
                {
                    yield return null;
                }

                audioSource.Play();
                songPosition = (float)(AudioSettings.dspTime - dspSongTime - firstBeatOffset) * audioSource.pitch - firstBeatOffset;
            }

            // determine how many beats since song started
            songPositionInBeats = songPosition / tempo + beatsShownInAdvance;

            if (nextIndex < notes.Count && notes[nextIndex].beat < songPositionInBeats)
            {
                // is this a held note
                if (notes[nextIndex].endBeat > 0)
                {
                    InitaliseHeldNote(nextIndex);
                    nextIndex++;
                }
                else
                {
                    // we need to generate the note here
                    InitialiseNote(nextIndex);

                    if (nextIndex + 1 < notes.Count)
                    {
                        NoteInfo nextNote = notes[nextIndex + 1];
                        if (nextNote != null && nextNote.beat == notes[nextIndex].beat)
                        {
                            InitialiseNote(nextIndex + 1);
                            nextIndex += 2;
                        }
                        else
                        {
                            nextIndex++;
                        }
                    }
                    else
                    {
                        nextIndex++;
                    }
                }
            }

            // handles removing the note if it reaches the end, we don't care about it anymore
            ClearNoteFromLane(lane1, 0);
            ClearNoteFromLane(lane2, 1);
            ClearNoteFromLane(lane3, 2);
            ClearNoteFromLane(lane4, 3);

            yield return null;
        }

        End();
    }

    public void PauseGame()
    {
        switch (status)
        {
            case ConductorStatus.Playing:
                status = ConductorStatus.Paused;
                break;
            case ConductorStatus.Paused:
                status = ConductorStatus.Playing;
                break;
        }
    }

    void InitaliseHeldNote(int index)
    {
        HeldNote heldNote = Instantiate(GetHeldNoteFromLane(notes[index]), board.notePool);
        notes[index].note = heldNote;

        GameObject lane = GetLaneEnd(notes[index]);

        heldNote.name = nextIndex.ToString();
        heldNote.Init(notes[index].beat, lane, notes[index].endBeat);
    }

    void InitialiseNote(int nextIndex)
    {
        Note note = Instantiate(GetNoteFromLane(notes[nextIndex]), board.notePool);

        notes[nextIndex].note = note;

        GameObject lane = GetLaneEnd(notes[nextIndex]);

        note.name = nextIndex.ToString();
        note.Init(notes[nextIndex].beat, lane);

        Debug.LogFormat("Note Type {0}", notes[nextIndex].noteType);
    }

    bool NotesStillOnBoard()
    {
        if (lane1.Count > 0 || lane2.Count > 0 || lane3.Count > 0 || lane4.Count > 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    void ClearNoteFromLane(Queue<NoteInfo> lane, int position)
    {
        if (lane.Count > 0)
        {
            Note note = lane.Peek().note;

            if (note != null)
            {
                // is the note in the offscreen position?
                float currentPosition = 0f;
                float offscreenPosition = 0f;
                switch (gameMode)
                {
                    case GameMode.Standard:
                        currentPosition = note.transform.position.x;
                        offscreenPosition = board.offscreenPositions[position].position.x;
                        break;
                    case GameMode.Arcade:
                        currentPosition = note.transform.position.y;
                        offscreenPosition = board.offscreenPositions[position].position.y;
                        break;
                }

                if (currentPosition <= offscreenPosition)
                {
                    if (!note.Handled)
                    {
                        IncrementHitScore(HitScore.Miss);
                    }

                    Destroy(note.gameObject);

                    lane.Dequeue();
                }
            }
        }
    }
    Queue<NoteInfo> GetQueue(Lane lane)
    {
        switch (lane)
        {
            case Lane.Lane1:
                return lane1;
            case Lane.Lane2:
                return lane2;
            case Lane.Lane3:
                return lane3;
            case Lane.Lane4:
                return lane4;
        }

        return null;
    }

    void HandleInput(Lane lane)
    {
        //Debug.LogFormat("Handling input for {0} - {1}", key, lane);

        Queue<NoteInfo> noteQueue = GetQueue(lane);

        if (noteQueue.Count > 0)
        {
            NoteInfo noteToCheck = noteQueue.Peek();

            // check the distance between the note and the positions;
            float distanceToTarget = Mathf.Abs(songPositionInBeats - noteToCheck.beat - beatsShownInAdvance);

            if (noteToCheck.note != null)
            {
                noteToCheck.note.HandleInput(distanceToTarget);
            }

            noteQueue.Dequeue();
        }
    }

    public static void IncrementHitScore(HitScore score)
    {
        if (instance)
        {
            switch (score)
            {
                case HitScore.Perfect:
                    instance.perfectHits += 1;
                    break;
                case HitScore.Good:
                    instance.goodHits += 1;
                    break;
                case HitScore.Okay:
                    instance.okayHits += 1;
                    break;
                case HitScore.Miss:
                    instance.missedHits += 1;
                    break;

            }
        }

        UIController.UpdateGameUI(score);
    }

    // TODO: this shouldn't really live here, maybe make a music pool class???
    HeldNote GetHeldNoteFromLane(NoteInfo note)
    {
        if (note.lane == Lane.Lane1)
        {
            return board.heldNotePrefabs[0];
        }
        else if (note.lane == Lane.Lane2)
        {
            return board.heldNotePrefabs[1];
        }
        else if (note.lane == Lane.Lane3)
        {
            return board.heldNotePrefabs[2];
        }
        else if (note.lane == Lane.Lane4)
        {
            return board.heldNotePrefabs[3];
        }

        return null;
    }

    Note GetNoteFromLane(NoteInfo note)
    {
        if (note.lane == Lane.Lane1)
        {
            return board.notePrefabs[0];
        }
        else if (note.lane == Lane.Lane2)
        {
            return board.notePrefabs[1];
        }
        else if (note.lane == Lane.Lane3)
        {
            return board.notePrefabs[2];
        }
        else if (note.lane == Lane.Lane4)
        {
            return board.notePrefabs[3];
        }

        return null;
    }

    // TODO: this shouldn't really live here, maybe make a music node class?
    GameObject GetLaneEnd(NoteInfo note)
    {
        if (note.lane == Lane.Lane1)
        {
            EnqueueNote(note, Lane.Lane1);
            return board.lanePositions[0];
        }

        else if (note.lane == Lane.Lane2)
        {
            EnqueueNote(note, Lane.Lane2);
            return board.lanePositions[1];
        }

        else if (note.lane == Lane.Lane3)
        {
            EnqueueNote(note, Lane.Lane3);
            return board.lanePositions[2];
        }

        else if (note.lane == Lane.Lane4)
        {
            EnqueueNote(note, Lane.Lane4);
            return board.lanePositions[3];
        }
        else
            return null;
    }

    void EnqueueNote(NoteInfo note, Lane lane)
    {
        switch (lane)
        {
            case Lane.Lane1:
                lane1.Enqueue(note);
                return;
            case Lane.Lane2:
                lane2.Enqueue(note);
                return;
            case Lane.Lane3:
                lane3.Enqueue(note);
                return;
            case Lane.Lane4:
                lane4.Enqueue(note);
                return;
        }
    }
}