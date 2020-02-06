using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Gestures;

namespace GesturesDemo
{
	public class CSimpleDirectionsController : MonoBehaviour
	{
		private CMobile3dGestures	m_gestures;
		private GameObject			m_instructions;
		private CColorFadeTimer		m_left;
		private CColorFadeTimer		m_right;
		private CColorFadeTimer		m_back;
		private CColorFadeTimer		m_forward;
		private CColorFadeTimer		m_down;
		private CColorFadeTimer		m_up;

		void Start()
		{
			m_gestures		= GetComponent<CMobile3dGestures>();
			m_instructions	= GameObject.Find("Canvas/Instructions");
			m_left			= GameObject.Find("Canvas/Left").GetComponent<CColorFadeTimer>();
			m_right			= GameObject.Find("Canvas/Right").GetComponent<CColorFadeTimer>();
			m_back			= GameObject.Find("Canvas/Back").GetComponent<CColorFadeTimer>();
			m_forward		= GameObject.Find("Canvas/Forward").GetComponent<CColorFadeTimer>();
			m_down			= GameObject.Find("Canvas/Down").GetComponent<CColorFadeTimer>();
			m_up			= GameObject.Find("Canvas/Up").GetComponent<CColorFadeTimer>();

			m_gestures.HandleGesture += ProcessGesture;
			m_gestures.Begin();
		}

		// This gets called in response to a gesture event.
		private void ProcessGesture(Gesture gesture)
		{
			if (m_instructions.activeSelf)
				m_instructions.SetActive(false);

			Vector3 v = CUtil.ClosestAxis(gesture.m_dirDevice);
			float duration = 0.6f;

			if (v == Vector3.left)		m_left.Begin(duration);
			if (v == Vector3.right)		m_right.Begin(duration);
			if (v == Vector3.back)		m_back.Begin(duration);
			if (v == Vector3.forward)	m_forward.Begin(duration);
			if (v == Vector3.down)		m_down.Begin(duration);
			if (v == Vector3.up)		m_up.Begin(duration);			
		}

		public void GoBack()
		{
			Application.LoadLevel("DemoMenu");
		}
	}
}
