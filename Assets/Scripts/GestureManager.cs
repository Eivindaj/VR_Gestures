using PDollarGestureRecognizer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.IO;

[System.Serializable]
public struct HandGesture
{
	public string name;
	public List<Vector3> fingerDatas;
	public UnityEvent onRecognized;
}

public class GestureManager : MonoBehaviour
{
	public OVRSkeleton skeleton;
	public List<HandGesture> gestures;
	private IList<OVRBone> handBones;
	public bool debug = true;
	public GameObject cubePrefab;
	public GameObject bulletPrefab;
	public GameObject circle;
	private HandGesture lastGesture = new HandGesture();


	public Camera cam;
	public bool creationMode = false;
	public string newGestureName;
	private List<Gesture> trainingSet = new List<Gesture>();

	private bool trigger;
	private bool moving = false;

	public float threshold = 0.05f;

	private List<Vector3> Vecpositions;
	private HandGesture previousGesture;

	// Start is called before the first frame update
	void Start()
    {
		handBones = new List<OVRBone>(skeleton.Bones);
		Debug.Log(handBones.Count);
		previousGesture = new HandGesture();

		string[] gestureFiles = Directory.GetFiles(Application.persistentDataPath + "/", "*.xml");
		foreach (var item in gestureFiles)
		{
			trainingSet.Add(GestureIO.ReadGestureFromFile(item));
			Debug.Log("item");
		}
		Vecpositions = new List<Vector3>();
		previousGesture = gestures[0];
		//Debug.Log("First gesture: " + previousGesture.name);
	}

    // Update is called once per frame
    void Update()
    {
        /*if (debug && Input.GetKeyDown(KeyCode.Space))
		{
			Debug.Log("This happens?");
			Save();
		}*/
		if (handBones.Count == 0)
		{
			handBones = new List<OVRBone>(skeleton.Bones);
		}

		HandGesture currentGesture = Recognize();
		bool hasRecognized = !currentGesture.Equals(new HandGesture());

		if(hasRecognized && !currentGesture.Equals(previousGesture))
		{
			Debug.Log("Found new gesture: " + currentGesture.name);
			previousGesture = currentGesture;
			currentGesture.onRecognized.Invoke();

			//trigger = false;

			trigger = currentGesture.name == "Pointing";
		}

		if (trigger)
		{
			update();
		}
		
		/*else if (!trigger && moving)
		{
			onEnd();
		}*/
	}

	void Save()
	{
		HandGesture g = new HandGesture();
		g.name = "New gesture";
		List<Vector3> data = new List<Vector3>();
		foreach (var bone in handBones)
		{
			data.Add(skeleton.transform.InverseTransformPoint(bone.Transform.position));
		}

		g.fingerDatas = data;
		gestures.Add(g);
	}

	HandGesture Recognize()
	{
		HandGesture currentGesture = new HandGesture();
		float currentMin = Mathf.Infinity;

		foreach (var gesture in gestures)
		{
			float sumDistance = 0;
			bool isDiscarded = false;
			for (int i = 0; i < handBones.Count; i++)
			{
				Vector3 currentData = skeleton.transform.InverseTransformPoint(handBones[i].Transform.position);
				float distance = Vector3.Distance(currentData, gesture.fingerDatas[i]);

				if (distance > threshold)
				{
					isDiscarded = true;
					break;
				}

				sumDistance += distance;
			}

			if (!isDiscarded && sumDistance < currentMin)
			{
				currentMin = sumDistance;
				currentGesture = gesture;
			}
		}

		return currentGesture;
	}

	public void TriggerTrue()
	{
		trigger = true;
		
	}

	public void TriggerFalse()
	{
		trigger = false;
	}

	public void onEnd()
	{
		trigger = false;
		Debug.Log("Position count on end: " + Vecpositions.Count);
		if (Vecpositions.Count == 0)
		{
			return;
		}

		moving = false;
		Debug.Log("end");

		Point[] pointArray = new Point[Vecpositions.Count];
		for (int i = 0; i < Vecpositions.Count; i++)
		{
			Vector2 screenPoint = Camera.main.WorldToScreenPoint(Vecpositions[i]);
			pointArray[i] = new Point(screenPoint.x, screenPoint.y, 0);
		}

		Gesture newGesture = new Gesture(pointArray);

		if (creationMode)
		{
			newGesture.Name = newGestureName;
			trainingSet.Add(newGesture);

			string fileName = Application.persistentDataPath + "/" + newGesture.Name + ".xml";
			GestureIO.WriteGesture(pointArray, newGestureName, fileName);
		} else
		{
			Result result = PointCloudRecognizer.Classify(newGesture, trainingSet.ToArray());
			Debug.Log(result.GestureClass + result.Score);
			if(result.Score > 0.5)
			{
				if (result.GestureClass == "circle")
				{
					Instantiate(circle, (cam.transform.position + cam.transform.rotation * Vector3.forward * 10f), Quaternion.identity);
				}
			}
		}

		Vecpositions.Clear();
	}

	void update()
	{
		Vecpositions.Add(skeleton.Bones[8].Transform.position);
		Destroy(Instantiate(cubePrefab, skeleton.Bones[8].Transform.position, Quaternion.identity), 3);
		Debug.Log("update");
	}

	public void spawnBullet()
	{
		Destroy(Instantiate(bulletPrefab, skeleton.Bones[8].Transform.position, cam.transform.rotation), 3);
	}
}
