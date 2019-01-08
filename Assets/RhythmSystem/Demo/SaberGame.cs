using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

[RequireComponent(typeof(AudioSource))]
public class SaberGame : MonoBehaviour {
    public GameObject rightPrefab;
    public GameObject leftPrefab;
    public RhythmPattern pattern;
    //public AnimationCurve curve;
    private int count = -1;
    private RhythmTracker.TriggerTiming triggerTiming;

    /* 
     Patch which allows loading custom songs from beatsaver.com
     It does not support events and obstacles
    */
    // Full path to JSON file containing notes, like: Normal.json, Hard.json, Expert.json
    public string jsonLevelPath;
    private Level level;
    private RhythmPatternEvent[] leftHandNotes;
    private RhythmPatternEvent[] rightHandNotes;

    private void Awake()
    {
        string jsonString = File.ReadAllText(jsonLevelPath);
        level = JsonUtility.FromJson<Level>(jsonString);
        PreloadLevelNotes();
    }
    private void PreloadLevelNotes()
    {
        // Instead of looping through all of the level notes (like in default RhythmSystem demo
        // just insert Note object in their proper place, so we have theoretical O(1) access time from Spawn()
        // Notice: This is may be extremely memory-ineffective, but it's a proof of concept
        int patternsCapacity = 100000;
        leftHandNotes = new RhythmPatternEvent[patternsCapacity];
        rightHandNotes = new RhythmPatternEvent[patternsCapacity];
        for (int i = 0; i < patternsCapacity; i++)
        {
            leftHandNotes[i] = null;
            rightHandNotes[i] = null;
        }

        foreach (Note note in level._notes)
        {   
            RhythmPatternEvent pattern = ConvertNoteToRhythmPatterEvent(note);
            

            // Adjust note timing to match reality (may not work)
            int hitIndex = (int)(4 * note._time);
            if (pattern.hand == RhythmPatternEvent.Hand.Left)
            {
                leftHandNotes[hitIndex] = pattern;
            }
            else if (pattern.hand == RhythmPatternEvent.Hand.Right)
            {
                rightHandNotes[hitIndex] = pattern;
            }
        }
    }

    // It a bridge between default RhythmSystem demo and this patch
    private RhythmPatternEvent ConvertNoteToRhythmPatterEvent(Note note)
    {
        RhythmPatternEvent pattern = new RhythmPatternEvent();

        // BeatSaber grid is 3 rows x 5 columns
        float[] yPositions = new float[3] { 0f, 1.25f, 2.5f };
        float[] xPositions = new float[5] { -1.5f, -0.75f, 0f, 0.75f, 1.5f };
        float x = xPositions[note._lineIndex];
        float y = yPositions[note._lineLayer];
        Debug.Log(string.Format("x: {0} y: {1}", x, y));
        pattern.position = new Vector2(x, y);
        pattern.hand = (RhythmPatternEvent.Hand)note._type;
        pattern.side = MapCutDirectionIntoSide(note);
        return pattern;
    }

    private RhythmPatternEvent.Side MapCutDirectionIntoSide(Note note)
    {
        switch (note._cutDirection)
        {
            // RhythmSystem Side uses the opposite directions, AFAIK!
            case 0:
                return RhythmPatternEvent.Side.Bottom;
            case 1:
                return RhythmPatternEvent.Side.Top;
            case 2:
                return RhythmPatternEvent.Side.Right;
            case 3:
                return RhythmPatternEvent.Side.Left;
            case 8:
                return RhythmPatternEvent.Side.Any;
            default:
                // Original: 4 is cut up left, 5 is cut up right, 6 is cut down left, 7 is cut down right
                // Not Supported yet
                return RhythmPatternEvent.Side.Any;
        }
    }

    void Start ()
    {
        triggerTiming = pattern.timing;
        RhythmTracker.instance.Subscribe(Spawn, triggerTiming, true);
    }
    
    private void Spawn(int beatIndex)
    {
        count++;
        if (leftHandNotes[count] != null)
            StartCoroutine(SpawnAndMoveAndDestroy(leftHandNotes[count]));
        if (rightHandNotes[count] != null)
            StartCoroutine(SpawnAndMoveAndDestroy(rightHandNotes[count]));

        //foreach (RhythmPatternEvent e in pattern.events)
        //{
        //    if (e.hitIndex == count % pattern.steps && e.side != RhythmPatternEvent.Side.None)

        //        StartCoroutine(SpawnAndMoveAndDestroy(e));
        //}
    }

    private IEnumerator SpawnAndMoveAndDestroy(RhythmPatternEvent e)
    {

        float x = Mathf.Lerp(-1f, 1f, e.position.x);
        float y = Mathf.Lerp(1.5f, 2.5f, e.position.y);
        GameObject instantiatePrefab = e.hand == RhythmPatternEvent.Hand.Right ? rightPrefab : leftPrefab;
        GameObject go = Instantiate(instantiatePrefab);
        go.transform.parent = transform;
        switch (e.side)
        {
            case RhythmPatternEvent.Side.Any:
                int RandomDir = UnityEngine.Random.Range(0, 2);
                float rotation = -90;
                if (RandomDir == 1)
                    rotation = 90;
                go.transform.Rotate(go.transform.forward, rotation);
                break;
            case RhythmPatternEvent.Side.Right:
                go.transform.Rotate(go.transform.forward, -90);
                break;
            case RhythmPatternEvent.Side.Bottom:
                go.transform.Rotate(go.transform.forward, 180);
                break;
            case RhythmPatternEvent.Side.Left:
                go.transform.Rotate(go.transform.forward, 90);
                break;
        }
        Vector3 targetPos = Vector3.zero + Vector3.right * x;
        float offset = RhythmTracker.instance.GetOffset();
        float elapsedTime = 0;
        while (elapsedTime <= offset)
        {
            float t = Mathf.InverseLerp(0, offset, elapsedTime);
            float inverseT = Mathf.InverseLerp(offset, 0, elapsedTime);
            Vector3 currentPos = targetPos + Vector3.up * y + //* curve.Evaluate(t)
                Vector3.forward * 30 * inverseT;
            go.transform.position = currentPos;
            elapsedTime += Time.unscaledDeltaTime;
            yield return new WaitForEndOfFrame();
        }
        while (elapsedTime < offset + 1)
        {
            go.transform.position = Vector3.Lerp(
                targetPos + Vector3.up * y, 
                targetPos - Vector3.forward * 10, 
                Mathf.InverseLerp(offset, offset + 1, elapsedTime)
            );
            elapsedTime += Time.unscaledDeltaTime;
            yield return new WaitForEndOfFrame();
        }
        Destroy(go);
    }

    private void OnDisable()
    {
        RhythmTracker.instance.Unsubscribe(Spawn, triggerTiming, true);
    }
}
