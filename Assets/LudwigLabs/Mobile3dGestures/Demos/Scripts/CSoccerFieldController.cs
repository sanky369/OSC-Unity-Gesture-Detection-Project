using UnityEngine;
using System.Collections;
using Gestures;

namespace GesturesDemo
{
	public class CSoccerFieldController : MonoBehaviour
	{
		private CMobile3dGestures	m_gestures;
		private Rigidbody			m_ball;
		private Transform			m_rotateCamera;
		private bool				m_touching;
		private Vector2				m_touchStartPos;
		private Vector3				m_touchStartCamRot;
		private GameObject			m_instructions;

		private void Start()
		{
			m_gestures		= GetComponent<CMobile3dGestures>();
			m_ball			= GameObject.Find("Ball").GetComponent<Rigidbody>();
			m_rotateCamera	= GameObject.Find("RotateCamera").transform;
			m_instructions	= GameObject.Find("Canvas/Instructions");

			// Set faster update interval for the gyro.  This should make the gesture detection more accurate.
			m_gestures.HandleGesture += ProcessGesture;
			m_gestures.Begin();
		}

		public void GoBack()
		{
			Application.LoadLevel("DemoMenu");
		}

		public void Restart()
		{
			Application.LoadLevel("SoccerField");
		}

		// This gets called in response to a gesture event.
		private void ProcessGesture(Gesture gesture)
		{
			if (m_instructions.activeSelf)
				m_instructions.SetActive(false);

			float camAngleY = m_rotateCamera.localEulerAngles.y;

			Vector3 v = gesture.m_dirDevice * 3f;
			v = Quaternion.Euler(0, camAngleY, 0) * v;

			Debug.DrawRay(m_ball.position, v, Color.red, 1.0f);
			m_ball.AddForce(v, ForceMode.Impulse);
		}

		private void Update()
		{
			// Rotate the camera on mouse/touch input.
			Vector2 screenPos	= Vector2.zero;
			bool touching		= false;

			if (Input.touchCount > 0)
			{
				touching = true;
				screenPos = Input.touches[0].position;
			}
			else if (Input.GetMouseButton(0))	// Left click
			{
				touching = true;
				screenPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
			}

			if (touching)
			{
				if (m_instructions.activeSelf)
					m_instructions.SetActive(false);

				if (!m_touching)
				{
					m_touchStartPos = screenPos;
					m_touchStartCamRot = m_rotateCamera.localEulerAngles;
				}
				else
				{
					Vector3 delta = screenPos - m_touchStartPos;

					Vector3 rotateBy = Vector3.zero;
					rotateBy.x = -delta.y / Screen.width;
					rotateBy.y = delta.x / Screen.height;
					rotateBy *= 100f;

					Vector3 newAngles = CUtil.MakeAnglesSigned(m_touchStartCamRot + rotateBy);
					newAngles.x = Mathf.Clamp(newAngles.x, 15, 80);
					m_rotateCamera.localEulerAngles = newAngles;
				}
			}
			
			m_touching = touching;
		}
	}
}
