using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

[Serializable]
public class Level {
    string _version;
    int _beatsPerMinute;
    int _beatsPerBar;
    int _noteJumpSpeed;
    int _shuffle;
    float _shufflePeriod;
    List<Note> _notes;

    // Ignore these, just for now
    private object _events;
    private object _obstacles;
}
