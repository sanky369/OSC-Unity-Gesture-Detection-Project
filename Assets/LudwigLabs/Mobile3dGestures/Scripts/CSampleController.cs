using UnityEngine;
using System.Collections;

namespace Gestures
{
	public class CSampleController : MonoBehaviour
	{
		private CMobile3dGestures		m_gestures;

		public void Start()
		{
			// This sample assumes the C3dGestures script component is also attached to this game object.
			m_gestures = GetComponent<CMobile3dGestures>();

			// Register for gesture events.
			m_gestures.HandleGesture += ProcessGesture;
		}

		private void ProcessGesture(Gesture gesture)
		{
			// This gets called in response to a gesture event.

			Vector3 v = gesture.m_dirDevice;
			string msg = string.Format("Gesture!  dirDevice=", v.x, v.y, v.z);
			Debug.Log(msg);

			// TODO: Add your code here!
			//			:
			//			:
			//			V
		}
	}
}
