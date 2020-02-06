using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Gestures;

namespace GesturesDemo
{
	public class CVisualizer : MonoBehaviour
	{
		private CMobile3dGestures		m_gestures;

		private GameObject				m_instructions;
		private RectTransform			m_optionsPanel;
		private Button					m_coordSysDeviceBtn, m_coordSysWorldXZBtn, m_coordSysWorldXYZBtn;
		private Toggle					m_gesturesToggle;
		private Button					m_mag02Btn, m_mag04Btn, m_mag10Btn, m_mag20Btn;
		private Toggle					m_snapToAxisToggle;
		private Toggle					m_showAccelToggle;
		private GameObject				m_recordButton;

		private CSimpleTransformPool	m_accelBallPool;
		private CSimpleTransformPool	m_gestureRodPool;
		private Transform				m_smartphone;
		private Transform				m_rotateCamera;
		private int						m_coordSys;
		private ColorBlock				m_colorBlockUnselected, m_colorBlockSelected;

		private bool					m_touching;
		private Vector2					m_touchStartPos;
		private Vector3					m_touchStartCamRot;

		private void Start()
		{
			m_gestures					= GetComponent<CMobile3dGestures>();

			m_instructions				= GameObject.Find("Canvas/Instructions");
			m_optionsPanel				= GameObject.Find("Canvas/Options").GetComponent<RectTransform>();
			m_coordSysDeviceBtn			= GameObject.Find("Canvas/Options/CoordSys/Device").GetComponent<Button>();
			m_coordSysWorldXZBtn		= GameObject.Find("Canvas/Options/CoordSys/WorldXZ").GetComponent<Button>();
			m_coordSysWorldXYZBtn		= GameObject.Find("Canvas/Options/CoordSys/WorldXYZ").GetComponent<Button>();
			m_gesturesToggle			= GameObject.Find("Canvas/Options/Gestures/Toggle").GetComponent<Toggle>();
			m_mag02Btn					= GameObject.Find("Canvas/Options/Gestures/02g").GetComponent<Button>();
			m_mag04Btn					= GameObject.Find("Canvas/Options/Gestures/04g").GetComponent<Button>();
			m_mag10Btn					= GameObject.Find("Canvas/Options/Gestures/10g").GetComponent<Button>();
			m_mag20Btn					= GameObject.Find("Canvas/Options/Gestures/20g").GetComponent<Button>();
			m_snapToAxisToggle			= GameObject.Find("Canvas/Options/Gestures/SnapToAxis").GetComponent<Toggle>();
			m_showAccelToggle			= GameObject.Find("Canvas/Options/ShowAccel").GetComponent<Toggle>();
			m_smartphone				= GameObject.Find("DeviceOrientation/Smartphone").transform;
			m_rotateCamera				= GameObject.Find("RotateCamera").transform;

#if !DEBUG
			m_recordButton				= GameObject.Find("Canvas/Options/Record");
			m_recordButton.SetActive(false);
#endif

			m_colorBlockUnselected				= m_coordSysDeviceBtn.colors;
			m_colorBlockSelected				= m_colorBlockUnselected;
			m_colorBlockSelected.normalColor	= Color.cyan;
			ChangeCoordSys(0);
			ChangeMagnitude(0.4f);
		
			m_optionsPanel.gameObject.SetActive(false);

			Transform accelBall			= transform.Find("AccelBall");
			m_accelBallPool				= new CSimpleTransformPool(accelBall, 100);

			Transform gestureRod		= transform.Find("GestureRod");
			m_gestureRodPool			= new CSimpleTransformPool(gestureRod, 10);

			// Set faster update interval for the gyro.  This should make the gesture detection more accurate.
			m_gestures.HandleGesture += ProcessGesture;
			m_gestures.Begin();

			Button resetGyro			= GameObject.Find("Canvas/ResetGyro").GetComponent<Button>();
			resetGyro.interactable		= m_gestures.HasGyroscope;
		}

		public void GoBack()
		{
			Application.LoadLevel("DemoMenu");
		}

		public void ToggleOptionsPanel()
		{
			m_optionsPanel.gameObject.SetActive(!m_optionsPanel.gameObject.activeSelf);			
		}

		// This gets called in response to a gesture event.
		private void ProcessGesture(Gesture gesture)
		{
			if (m_instructions.activeSelf)
				m_instructions.SetActive(false);

			if (m_gesturesToggle.isOn)
				StartCoroutine(CoAnimateGesture(gesture, 0.5f));
		}

		private IEnumerator CoAnimateGesture(Gesture gesture, float timeSecs)
		{
			Vector3 v = Vector3.zero;
			switch (m_coordSys)
			{
				case 0:		v = gesture.m_dirDevice;		break;
				case 1:		v = gesture.m_dirWorldXZ;		break;
				case 2:		v = gesture.m_dirWorldXYZ;		break;
			}

			if (m_snapToAxisToggle.isOn)
				v = CUtil.ClosestAxis(v);

			Transform rod = m_gestureRodPool.Take();			
			rod.localRotation = Quaternion.LookRotation(v) * Quaternion.Euler(-90, 0, 0);

			float startTime		= Time.time;
			float elapsed		= 0f;
			while ((elapsed = Time.time - startTime) < timeSecs)
			{
				float pct = elapsed / timeSecs;

				float size = 1f;
				if (pct < 0.25f)			// Grow
					size = pct * 4f;
				else if (pct >= 0.75f)		// Shrink
					size = (1f - pct) * 4f;

				rod.localScale = new Vector3(0.1f, v.magnitude * size / 2f, 0.1f);
				rod.localPosition = v * size / 2f;

				yield return null;
			}

			rod.localPosition = Vector3.zero;
			rod.localScale = Vector3.zero;
			m_gestureRodPool.Return(rod);
		}

		public void ResetGyro()
		{
			m_gestures.CalibrateGyro();
		}

		public void ChangeCoordSys(int coordSys)
		{
			m_coordSys = coordSys;
			m_coordSysDeviceBtn.colors		= (coordSys == 0) ? m_colorBlockSelected : m_colorBlockUnselected;
			m_coordSysWorldXZBtn.colors		= (coordSys == 1) ? m_colorBlockSelected : m_colorBlockUnselected;
			m_coordSysWorldXYZBtn.colors	= (coordSys == 2) ? m_colorBlockSelected : m_colorBlockUnselected;
		}

		public void ChangeMagnitude(float mag)
		{
			m_mag02Btn.colors	= (mag == 0.2f)	? m_colorBlockSelected : m_colorBlockUnselected;
			m_mag04Btn.colors	= (mag == 0.4f) ? m_colorBlockSelected : m_colorBlockUnselected;
			m_mag10Btn.colors	= (mag == 1.0f)	? m_colorBlockSelected : m_colorBlockUnselected;
			m_mag20Btn.colors	= (mag == 2.0f)	? m_colorBlockSelected : m_colorBlockUnselected;

			m_gestures.MinGestureMagnitude = mag;
		}

		public void Record()
		{
			m_gestures.ToggleRecording();
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
				// Hide instructions
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
					newAngles.x = Mathf.Clamp(newAngles.x, -80, 80);
					m_rotateCamera.localEulerAngles = newAngles;
				}
			}
			
			m_touching = touching;
		}

		private void FixedUpdate()
		{
			m_smartphone.rotation = m_gestures.DeviceAttitudeXYZ * Quaternion.Euler(0, 0, -90);

			if (m_showAccelToggle.isOn)
			{
				Vector3 v = m_gestures.UserAccel;
				switch (m_coordSys)
				{
					case 1:		v = m_gestures.DeviceAttitudeXZ * v;		break;
					case 2:		v = m_gestures.DeviceAttitudeXYZ * v;		break;
				}

				Transform accelBall		= m_accelBallPool.Take();
				accelBall.localPosition	= v;
				accelBall.GetComponent<CColorFadeTimer>().Begin(1.0f, HandleTimerExpired);
			}
		}

		private void HandleTimerExpired(CColorFadeTimer timer)
		{
			Transform accelBall = timer.transform;
			accelBall.localPosition = Vector3.zero;
			m_accelBallPool.Return(accelBall);
		}
	}
}
