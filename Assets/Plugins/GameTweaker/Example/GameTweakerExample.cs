using UnityEngine;
using System.Collections;

public class GameTweakerExample : MonoBehaviour {
    [TweakableField]
    public string fooString;
    [TweakableField(true)]
    public GameObject somePrefab;
	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
