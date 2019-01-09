using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

[Serializable]
public class Level {    
    public string _version;
    public int _beatsPerMinute;
    public int _beatsPerBar;
    public int _noteJumpSpeed;
    public int _shuffle;
    public float _shufflePeriod;
    public List<Note> _notes;

    // Ignore these, just for now
    private object _events;
    private object _obstacles;
}
