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
    // Required to start the game
    public string jsonLevelPath;
    private Level micziLevel;
    private RhythmPatternEvent[] leftNotes;
    private RhythmPatternEvent[] rightNotes;
    public AnimationCurve curve;
    private int count = -1;

    private RhythmTracker.TriggerTiming triggerTiming;

    private void Awake()
    {
        string jsonString = File.ReadAllText(jsonLevelPath);
        micziLevel = JsonUtility.FromJson<Level>(jsonString);

        // wszystkie beaty są nullami
        Debug.Log("Start wypelniania nullami");
        leftNotes = new RhythmPatternEvent[100000];
        rightNotes = new RhythmPatternEvent[100000];
        for (int i = 0; i < 100000; i++)
        {
            leftNotes[i] = null;
            rightNotes[i] = null;
        }
        Debug.Log("Koniec wypelniania nullami");
        Debug.Log("Start sortowania nut");
        // wypelniamy tylko indeksy gdzie powinny przypadac nuty
        foreach (Note note in micziLevel._notes)
        {
            int micziHitIndex = (int)(4 * note._time);
            Debug.Log("Nutkaa");
            RhythmPatternEvent pattern = new RhythmPatternEvent();
            pattern.side = RhythmPatternEvent.Side.Any;


            float noteY = -1f;
            float noteX = -1f;
            if (note._lineLayer == 0)
            {
                noteY = -0.5f;
            }
            else if (note._lineLayer == 1)
            {
                noteY = -0.25f;
            }
            else if (note._lineLayer == 2)
            {
                noteY = 0.0f;
            }

            if (note._lineIndex == 0)
            {
                noteX = -0.5f;
            }
            else if (note._lineIndex == 1)
            {
                noteX = -0.25f;
            }
            else if (note._lineIndex == 2)
            {
                noteX = 0.0f;
            }
            else if (note._lineIndex == 3)
            {
                noteX = 0.25f;
            }
            else if (note._lineIndex == 4)
            {
                noteX = 0.5f;
            }
       
            pattern.position = new Vector2(noteX, noteY);
            // Rece sa na odwrot chyba
            pattern.hand = (RhythmPatternEvent.Hand)note._type;
            if (pattern.hand == RhythmPatternEvent.Hand.Left)
            {
                leftNotes[micziHitIndex] = pattern;
            }
            else if (pattern.hand == RhythmPatternEvent.Hand.Right)
            {
                rightNotes[micziHitIndex] = pattern;
            }

        }
        Debug.Log("Koniec sortowania nut");
    }
    void Start ()
    {
        

        triggerTiming = pattern.timing;
        RhythmTracker.instance.Subscribe(Spawn, triggerTiming, true);

        
    }
    
    private void Spawn(int beatIndex)
    {
        count++;
        if (leftNotes[count] != null)
            StartCoroutine(SpawnAndMoveAndDestroy(leftNotes[count], count));
        if (rightNotes[count] != null)
            StartCoroutine(SpawnAndMoveAndDestroy(rightNotes[count], count));

        //foreach (RhythmPatternEvent e in pattern.events)
        //{
        //    if (e.hitIndex == count % pattern.steps && e.side != RhythmPatternEvent.Side.None)

        //        StartCoroutine(SpawnAndMoveAndDestroy(e));
        //}
    }

    private IEnumerator SpawnAndMoveAndDestroy(RhythmPatternEvent e, int micziHitIndex)
    {
        
        float x = Mathf.Lerp(-1.5f, 1.5f, e.position.x);
        float y = Mathf.Lerp(2.5f, 0, e.position.y);
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
        Vector3 targetPos = Vector3.zero  + Vector3.right * x;
        float offset = RhythmTracker.instance.GetOffset();
        float elapsedTime = 0;
        while (elapsedTime <= offset)
        {
            float t = Mathf.InverseLerp(0, offset, elapsedTime);
            float inverseT = Mathf.InverseLerp(offset, 0, elapsedTime);
            Vector3 currentPos = targetPos + Vector3.up * y * curve.Evaluate(t) + 
                Vector3.forward * 30 * inverseT;
            go.transform.position = currentPos;
            elapsedTime += Time.unscaledDeltaTime;
            yield return new WaitForEndOfFrame();
        }
        // zle dziala
        float cwiercnutaWTakcie = micziHitIndex;
        Debug.Log(cwiercnutaWTakcie);
        while (elapsedTime < offset + 1)
        {
            go.transform.position = Vector3.Lerp(targetPos + Vector3.up * y, targetPos - Vector3.forward * 10, Mathf.InverseLerp(offset, offset + 1, elapsedTime));
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
