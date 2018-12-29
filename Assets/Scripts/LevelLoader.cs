using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class LevelLoader : MonoBehaviour {
    // TODO read from info.json etc.
    public string JsonPath = "D:\\Wolfer\\Downloads\\504-311\\Toto - Africa\\Normal.json";
    private Level Level;

    void Awake()
    {
        string jsonString = File.ReadAllText(JsonPath);
        Debug.Log(jsonString);

        this.Level = JsonUtility.FromJson<Level>(jsonString);
        Debug.Log(this.Level);
    }

	// Use this for initialization
	void Start () {

    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
