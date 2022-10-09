using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Note : MonoBehaviour
{
    public Lane lane;

    public float Beat
    {
        get;
        protected set;
    }

    public Vector3 StartPosition
    {
        get;
        protected set;
    }

    public Vector3 EndPosition
    {
        get;
        protected set;
    }

    public bool Held
    {
        get;
        protected set;
    }

    public bool Handled
    {
        get;
        protected set;
    }

    public virtual void Init(float beatInSong, GameObject lane)
    {
        Transform target = lane.transform;

        StartPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        EndPosition = new Vector3(target.position.x, target.position.y, transform.position.z);

        Beat = beatInSong;
        StartCoroutine(MoveNote());
    }

    public virtual void Init(float beatInSong, GameObject lane, float endBeat)
    {

    }

    public virtual void HandleInput(float distanceToTarget)
    {
        Handled = true;

        Destroy(gameObject);

        CalculateScore(distanceToTarget);

    }

    public HitScore CalculateScore(float distanceToTarget)
    {
        if (distanceToTarget <= Conductor.instance.PerfectOffset)
        {
            Conductor.IncrementHitScore(HitScore.Perfect);
            return HitScore.Perfect;
        }
        if (distanceToTarget <= Conductor.instance.GoodOffset)
        {
            Conductor.IncrementHitScore(HitScore.Good);
            return HitScore.Good;
        }
        if (distanceToTarget <= Conductor.instance.OkayOffset)
        {
            Conductor.IncrementHitScore(HitScore.Okay);
            return HitScore.Okay;
        }
        else
        {
            Conductor.IncrementHitScore(HitScore.Miss);
            return HitScore.Miss;
        }
    }

    public virtual IEnumerator MoveNote()
    {
        Conductor conductor = Conductor.instance;
        float position = 0f;
        float endPosition = 0f;

        switch (conductor.gameMode)
        {
            case GameMode.Standard:
                position = transform.position.x;
                endPosition = EndPosition.x;
                break;
            case GameMode.Arcade:
                position = transform.position.y;
                endPosition = EndPosition.y;
                break;
        }

        while (position != endPosition)
        {
            switch (conductor.gameMode)
            {
                case GameMode.Standard:
                    transform.position = new Vector3(StartPosition.x + (EndPosition.x - StartPosition.x) * (1f - (Beat - conductor.songPosition / conductor.tempo) / conductor.beatsShownInAdvance), StartPosition.y, StartPosition.z);
                    break;
                case GameMode.Arcade:
                    transform.position = new Vector3(StartPosition.x, StartPosition.y + (EndPosition.y - StartPosition.y) * (1f - (Beat - conductor.songPosition / conductor.tempo) / conductor.beatsShownInAdvance), StartPosition.z);
                    break;
            }

            yield return null;
        }
    }

    private void OnDestroy()
    {
        StopCoroutine(MoveNote());
    }
}
