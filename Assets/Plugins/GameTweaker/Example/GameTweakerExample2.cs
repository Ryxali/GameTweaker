using UnityEngine;
using System.Collections;

public class GameTweakerExample2 : MonoBehaviour {
    [TweakableField]
    public int foo;
    [TweakableField]
    public int[] foo2;
    [TweakableField(true)]
    public int sharedFoo;
	// Use this for initialization
	void Start () {
	    
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
