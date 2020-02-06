using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace GesturesDemo
{
	public delegate void TimerExpired(CColorFadeTimer timer);

	public class CColorFadeTimer : MonoBehaviour
	{
		public Color			m_startColor;
		public Color			m_endColor;
		public float			m_startScale;
		public float			m_endScale;

		private float			m_startTime;
		private float			m_timeSecs;
		private bool			m_started;
		private TimerExpired	m_timerExpiredFunc;

		// When attached to a typical object with a mesh renderer.
		private MeshRenderer	m_renderer;
		private Vector3			m_origScale;

		// When attached to a UI object.
		private Graphic			m_graphic;

		public void Awake()
		{
			m_startTime			= 0;
			m_timeSecs			= 0;
			m_started			= false;
			m_timerExpiredFunc	= null;

			m_renderer			= GetComponent<MeshRenderer>();
			m_graphic			= GetComponent<Graphic>();

			m_origScale			= transform.localScale;
		}

		public void Begin(float timeSecs, TimerExpired expiredFunc = null)
		{
			m_startTime			= Time.time;
			m_timeSecs			= timeSecs > 0f ? timeSecs : 0.001f;
			m_timerExpiredFunc	= expiredFunc;
			m_started			= true;
		}

		public void Update()
		{
			if (m_started)
			{
				float elapsed = Time.time - m_startTime;
				float elapsedPct = elapsed / m_timeSecs;
				float scalePct = Mathf.Lerp(m_startScale, m_endScale, elapsedPct);
				Color color = Color.Lerp(m_startColor, m_endColor, elapsedPct);

				if (m_renderer != null)
				{
					m_renderer.material.color = color;
					transform.localScale = m_origScale * scalePct;
				}
				else if (m_graphic != null)
				{
					m_graphic.color = color;
				}

				if (elapsed > m_timeSecs)
				{
					m_started = false;
					if (m_timerExpiredFunc != null)
						m_timerExpiredFunc(this);
				}
			}
		}
	}
}
