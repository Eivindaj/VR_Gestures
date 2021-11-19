using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class moveForward : MonoBehaviour
{

    // Start is called before the first frame update
    void Start()
    {
		StartCoroutine(move());
    }

	private IEnumerator move()
	{
		while (true)
		{
			yield return new WaitForEndOfFrame();
			transform.position += transform.rotation * Vector3.forward * 0.01f;
		}
	}
}
