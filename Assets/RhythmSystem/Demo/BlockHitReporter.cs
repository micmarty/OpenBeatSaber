using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockHitReporter : MonoBehaviour {
    public BlockSliceDetection blockSliceDetector;
    public bool isFinalCollision;
    private AudioSource noteHit;
    private void Awake()
    {
        noteHit = (AudioSource)GameObject.Find("NoteHitAudioSource").GetComponent<AudioSource>(); ;
    }
    void OnTriggerEnter(Collider col)
    {
        if (col.GetComponent<LightSaber>())
        {
            if (isFinalCollision)
            {
                noteHit.PlayOneShot(noteHit.clip, 1.5f);
                blockSliceDetector.RegisterCollision2();
            }
            else
            {
                blockSliceDetector.RegisterCollision1();
            }
        }
    }
}
