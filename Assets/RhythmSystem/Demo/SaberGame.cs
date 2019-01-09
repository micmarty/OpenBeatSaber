using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
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
        // Choose path to info.json
        string path = EditorUtility.OpenFilePanel("Select main level file info.json (BeatSaber)", "", "json");
        if (path.Length != 0)
        {
            try
            {
                // Parse info.json
                string infoFileJsonString = File.ReadAllText(path);
                Info levelInfo = JsonUtility.FromJson<Info>(infoFileJsonString);

                // Read specific level (difficulty)
                // Hardcoded - always read first-defined in array, e.g Easy.json
                int difficultyLevelId = 0;

                string infoDirectory = Path.GetDirectoryName(path);
                string jsonLevelFileName = levelInfo.difficultyLevels[difficultyLevelId].jsonPath;
                jsonLevelPath = Path.Combine(infoDirectory, jsonLevelFileName);
                string jsonLevelAsText = File.ReadAllText(jsonLevelPath);
                level = JsonUtility.FromJson<Level>(jsonLevelAsText);

                // Adjust RhythmTracker's bpm
                RhythmTracker.instance.SetTempo(level._beatsPerMinute);

                // Load song (based on given path)
                string jsonLevelAudioName = levelInfo.difficultyLevels[difficultyLevelId].audioPath;
                string audioPath = Path.Combine(infoDirectory, jsonLevelAudioName);
                StartCoroutine(LoadSongCoroutine(audioPath));

                // Load and order all the notes
                PreloadLevelNotes();

                // Notice!
                // Assuming that song is always 16 beats per bar...
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        else
        {
            Debug.LogError("Incorrect path!");
        }
    }

    IEnumerator LoadSongCoroutine(string path)
    {
        string url = string.Format("file://{0}", path);
        WWW www = new WWW(url);
        yield return www;

        // Replace audio clip with song which is located at the url
        RhythmTracker.instance.GetPlaybackAudioSource().clip = www.GetAudioClip(false, false);
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
        // Values are between 0 and 1 -> they will be interpolated by Math.Lerp()
        float[] yPositions = new float[3] { 0f, 0.5f, 1f };
        float[] xPositions = new float[5] { 0f, 0.25f, 0.5f, 0.75f, 1f };

        float x = xPositions[note._lineIndex];
        float y = yPositions[note._lineLayer];
        //Debug.Log(string.Format("x: {0} y: {1}", x, y));
        pattern.position = new Vector2(x, y);
        pattern.hand = (RhythmPatternEvent.Hand)note._type;
        pattern.side = MapCutDirectionIntoSide(note);
        return pattern;
    }

    private RhythmPatternEvent.Side MapCutDirectionIntoSide(Note note)
    {
        //Debug.Log(note._cutDirection);
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
                return RhythmPatternEvent.Side.Top;
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
        float y = Mathf.Lerp(-0.25f, 0.75f, e.position.y) + 0.05f;
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
        // We don't want notes to go away quickly, so we mimic the first loop
        // by giving it the same amount time (0->offset) for execution and same forward speed
        while (elapsedTime <= 2 * offset)
        {
            go.transform.position = Vector3.Lerp(
                targetPos + Vector3.up * y, 
                targetPos - Vector3.forward * 30, 
                Mathf.InverseLerp(offset, 2 * offset, elapsedTime)
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
