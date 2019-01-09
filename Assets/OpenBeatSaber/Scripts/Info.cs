using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

[Serializable]
public class Info {    
    // Important
    public List<DifficultyLevel> difficultyLevels;
    // This is not always correct (sometimes the precise bpm is inside level file, e.g. Normal.json etc.)
    public float beatsPerMinute;

    // Just informational
    public string songName;
    public string songSubName;
    public string authorName;
    public string environmentName;

    // Ignored fields
    public float previewStartTime;
    public float previewDuration;
    public string coverImagePath;
}
