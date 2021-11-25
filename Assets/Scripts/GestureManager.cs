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
	public List<HandGesture> gestures;

	public OVRSkeleton leftSkeleton;
	public OVRSkeleton rightSkeleton;

	private IList<OVRBone> leftHandBones;
	private IList<OVRBone> rightHandBones;

	private HandGesture leftPreviousGesture;
	private HandGesture rightPreviousGesture;

	private List<Vector3> Vecpositions;

	public Camera cam;
	public GameObject cubePrefab;
	public GameObject bulletPrefab;
	public GameObject circle;
	public GameObject triangle;
	public GameObject star;


	private List<Gesture> trainingSet = new List<Gesture>();
	public string newGestureName;
	private bool leftTrigger;
	private bool rightTrigger;


	public float threshold = 0.05f;

	public bool creationMode = false;
	public bool debug = true;

	// Start is called before the first frame update
	void Start()
	{
		leftHandBones = new List<OVRBone>(leftSkeleton.Bones);
		rightHandBones = new List<OVRBone>(rightSkeleton.Bones);

		leftPreviousGesture = new HandGesture();
		rightPreviousGesture = new HandGesture();

		string[] gestureFiles = Directory.GetFiles(Application.persistentDataPath + "/", "*.xml");
		foreach (var item in gestureFiles)
		{
			trainingSet.Add(GestureIO.ReadGestureFromFile(item));
			Debug.Log("item");
		}
		Vecpositions = new List<Vector3>();
		leftPreviousGesture = gestures[1];
		rightPreviousGesture = gestures[1];
		//Debug.Log("First gesture: " + rightPreviousGesture.name);
	}

	// Update is called once per frame
	void Update()
	{

		if (debug && Input.GetKeyDown(KeyCode.Space))
		{
			Debug.Log("This happens?");
			Save();
		}
		if (leftHandBones.Count == 0)
		{
			leftHandBones = new List<OVRBone>(leftSkeleton.Bones);
		}

		if (rightHandBones.Count == 0)
		{
			rightHandBones = new List<OVRBone>(rightSkeleton.Bones);
		}

		HandGesture currentLeftGesture = Recognize(true);
		HandGesture currentRightGesture = Recognize(false);

		bool hasRecognizedLeft = !currentLeftGesture.Equals(new HandGesture());
		bool hasRecognizedRight = !currentRightGesture.Equals(new HandGesture());

		if (hasRecognizedLeft && !currentLeftGesture.Equals(leftPreviousGesture))
		{
			Debug.Log("Found new gesture left hand: " + currentLeftGesture.name);
			leftPreviousGesture = currentLeftGesture;
			currentLeftGesture.onRecognized.Invoke();

			if (currentLeftGesture.name == "Right finger guns")
			{
				currentLeftGesture.name = "Left finger guns";
			}

			//trigger = false;

			leftTrigger = currentLeftGesture.name == "Left pointing";
		}

		if (hasRecognizedRight && !currentRightGesture.Equals(rightPreviousGesture))
		{
			Debug.Log("Found new gesture right hand: " + currentRightGesture.name);
			rightPreviousGesture = currentRightGesture;
			currentRightGesture.onRecognized.Invoke();

			if (currentRightGesture.name == "Left finger guns")
			{
				currentRightGesture.name = "Right finger guns";
			}
			//trigger = false;

			rightTrigger = currentRightGesture.name == "Right pointing";
		}


		// fix etterpå
		if (leftTrigger || rightTrigger)
		{
			update(leftTrigger, rightTrigger);
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
		foreach (var bone in leftHandBones)
		{
			data.Add(leftSkeleton.transform.InverseTransformPoint(bone.Transform.position));
		}

		g.fingerDatas = data;
		gestures.Add(g);
	}

	HandGesture Recognize(bool leftHand)
	{


		HandGesture currentGesture = new HandGesture();
		float currentMin = Mathf.Infinity;

		foreach (var gesture in gestures)
		{
			float sumDistance = 0;
			bool isDiscarded = false;
			if (leftHand)
			{
				for (int i = 0; i < leftHandBones.Count; i++)
				{
					Vector3 currentData = leftSkeleton.transform.InverseTransformPoint(leftHandBones[i].Transform.position);
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
			else
			{
				for (int i = 0; i < rightHandBones.Count; i++)
				{
					Vector3 currentData = rightSkeleton.transform.InverseTransformPoint(rightHandBones[i].Transform.position);
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

		}

		return currentGesture;
	}

	public void onEnd()
	{
		//trigger = false;
		Debug.Log("Position count on end: " + Vecpositions.Count);
		if (Vecpositions.Count < 5)
		{
			return;
		}

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
		}
		else
		{
			Result result = PointCloudRecognizer.Classify(newGesture, trainingSet.ToArray());
			Debug.Log(result.GestureClass + result.Score);
			if (result.Score > 0.5)
			{
				if (result.GestureClass == "circle")
				{
					Instantiate(circle, (cam.transform.position + cam.transform.rotation * Vector3.forward * 10f), Quaternion.identity);
				}
				if (result.GestureClass == "triangle")
				{
					Instantiate(triangle, (cam.transform.position + cam.transform.rotation * Vector3.forward * 10f), Quaternion.identity);
				}
				if (result.GestureClass == "star")
				{
					Instantiate(star, (cam.transform.position + cam.transform.rotation * Vector3.forward * 10f), Quaternion.identity);
				}
			}
		}

		Vecpositions.Clear();
	}

	void update(bool left, bool right)
	{
		if (left)
		{
			Vecpositions.Add(leftSkeleton.Bones[8].Transform.position);
			Destroy(Instantiate(cubePrefab, leftSkeleton.Bones[8].Transform.position, Quaternion.identity), 3);
			Debug.Log("update");
		}
		if (right)
		{
			Vecpositions.Add(rightSkeleton.Bones[8].Transform.position);
			Destroy(Instantiate(cubePrefab, rightSkeleton.Bones[8].Transform.position, Quaternion.identity), 3);
			Debug.Log("update");
		}

	}

	public void leftSpawnBullet()
	{
		Destroy(Instantiate(bulletPrefab, leftSkeleton.Bones[8].Transform.position, cam.transform.rotation), 3);
	}

	public void rightSpawnBullet()
	{

		Destroy(Instantiate(bulletPrefab, rightSkeleton.Bones[8].Transform.position, cam.transform.rotation), 3);
	}
}
