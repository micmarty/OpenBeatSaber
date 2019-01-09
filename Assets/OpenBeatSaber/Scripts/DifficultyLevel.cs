using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

[Serializable]
public class DifficultyLevel
{
    public string audioPath;
    public string jsonPath;

    // Ignored fields
    public string difficulty;
    public int difficultyRank;
}