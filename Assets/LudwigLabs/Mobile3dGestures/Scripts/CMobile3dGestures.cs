///////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2016, Ludwig Labs - All Rights Reserved
//
// Use of this code is governed by Unity Asset Store EULA, which can be found here: https://unity3d.com/legal/as_terms.
// In short, you may use or modify this code/algorithm in your own projects, but you may NOT distribute it to others
// for use in their projects.

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Gestures
{
	public struct Gesture
	{
		public Vector3		m_dirDevice;			// Relative to device.
		public Vector3		m_dirWorldXZ;			// World space, except for Y-axis which is always 0.
		public Vector3		m_dirWorldXYZ;			// World space.
		public float		m_elapsed;				// How much time the gesture took (in seconds).

		public float		Magnitude				{ get { return m_dirDevice.magnitude; } }
	}

	public delegate void HandleGesture(Gesture gesture);

	public class CMobile3dGestures : MonoBehaviour
	{
		// Event that gets fired when a gesture occurs.
		public event HandleGesture	HandleGesture;

		// Basic settings. You can change them in the Unity Editor, but only if you really need to.
		// These defaults should be fine for most use cases.
		public float				MinGestureMagnitude		= 0.40f;		// Measured in g's.
		public float				MaxGestureAngle			= 60f;			// Measured in degrees.
		public float				MaxGestureAge			= 0.20f;		// Measured in seconds.
		public bool					IgnoreWindUps			= true;			// Ignore smaller gestures that immediately precede larger ones.
		public float				PostGestureCooldown		= 0.5f;			// Measured in seconds.

		public bool					HasAccelerometer		{ get { return m_hasAccel;									} }
		public bool					HasGyroscope			{ get { return m_hasGyro;									} }
		public bool					IsRunning				{ get { return m_running;									} }
		public Vector3				UserAccel				{ get { return new Vector3(m_user.x, m_user.y, -m_user.z);	} }	// Z-axis is backwards.
		public Quaternion			DeviceAttitudeXZ		{ get { return m_attitudeXZ;								} }
		public Quaternion			DeviceAttitudeXYZ		{ get { return m_attitudeXYZ;								} }
		public int					DebugSpikeCount			{ get { return m_spikes.Count;								} }

		// Advanced settings.  These shouldn't need to be changed. 
		private float				MinSpikeMagnitude		= 0.20f;		// Measured in g's.
		private float				MaxSpikeAngle			= 30f;			// Measured in degrees.
		private float				IgnoreAccelBelow		= 0.01f;

		// Properties whose values are established at Begin/Resume time.
		private bool				m_running;
		private bool				m_hasAccel;
		private bool				m_hasGyro;
		private Quaternion			m_rotationBase;
		private Vector3				m_calibrate;
		internal float				m_maxSpikeAngleDot;		// Dot product of angle.
		internal float				m_maxGestureAngleDot;	// Dot product of angle.

		// Properties that are constantly updated during operation.
		private Recording			m_recording;
		private Quaternion			m_attitudeXZ;			// Current attitude of device (except y-axis)
		private Quaternion			m_attitudeXYZ;			// Current attitude of device (all axes)
		public Vector3				m_current;				// Current acceleration with gravity, in device's space
		public Vector3				m_user;					// User acceleration without gravity, in device's space
		private Spike				m_cur;
		private Spikes				m_spikes;
		private bool				m_hasGestureCandidate;	// Used when IgnoreWindUps is true.
		private Gesture				m_gestureCandidate;	
		private float				m_lastGestureTime;
		private Vector3				m_gravity;				// Current gravity vector.

		// Low-pass filter for gravity vector.  Only used when gyroscope isn't available.
		private const float			c_gravityFilter			= 0.10f;

        public Vector3 oscAcceleration;

        public void Awake()
		{
			m_running				= false;
			m_hasAccel				= false;
			m_hasGyro				= false;

			m_rotationBase			= Quaternion.Euler(90, 0, 0);		
			m_calibrate				= Vector3.zero;
			m_gravity				= Vector3.zero;
			m_attitudeXZ			= Quaternion.identity;
			m_attitudeXYZ			= Quaternion.identity;
			m_current				= Vector3.zero;
			m_user					= Vector3.zero;

			m_cur					= new Spike();
			m_spikes				= new Spikes(this);
			m_hasGestureCandidate	= false;
			m_lastGestureTime		= 0;
			m_recording				= new Recording();
		}

		public bool Begin()
		{
			return Resume();
		}

		public void Suspend()
		{
			m_running = false;
		}

		public bool Resume()
		{
#if UNITY_EDITOR
			// SystemInfo doesn't seem to be correct when running in the Editor using Unity Remote,
			// so just tell the class that both accel and gyro are available.  If you are developing with
			// a device that doesn't have a gyroscope, just change m_hasGyro to false here, for when
			// you're running in the Editor.
			m_hasAccel	= true;
			m_hasGyro	= true;
#else
			m_hasAccel	= SystemInfo.supportsAccelerometer;
			m_hasGyro	= SystemInfo.supportsGyroscope;
#endif
			if (m_hasGyro)
				Input.gyro.enabled = true;

			m_running = m_hasAccel || m_hasGyro;
			if (m_running)
			{
				CalibrateGyro();

				if (m_hasGyro)
				{
                    m_gravity = oscAcceleration; //Input.gyro.gravity;
                    m_current = oscAcceleration; //Input.acceleration;
					m_user	  = oscAcceleration; //Input.gyro.userAcceleration;
				}
				else
				{
					m_gravity	= oscAcceleration; //Input.acceleration;
					m_current	= oscAcceleration; //Input.acceleration;
					m_user		= Vector3.zero;
				}
			}

			m_maxGestureAngleDot = Mathf.Cos(MaxGestureAngle * Mathf.Deg2Rad);
			m_maxSpikeAngleDot = Mathf.Cos(MaxSpikeAngle * Mathf.Deg2Rad);

			return m_running;
		}

		public void CalibrateGyro()
		{
			if (!m_running || !m_hasGyro) return;

			Quaternion rot = m_rotationBase * FlipHands(Input.gyro.attitude);
			m_calibrate = new Vector3(0, rot.eulerAngles.y, 0);
		}
	
		public void ToggleRecording()
		{
			m_recording.ToggleRecording();
		}

		private Quaternion FlipHands(Quaternion q)
		{
			return new Quaternion(q.x, q.y, -q.z, -q.w);
		}

		private void FixedUpdate()
		{
			if (!m_running) return;

			// Neutral device position is portrait or landscape, perpendicular to the ground, screen facing the m_user.
			// In this position, X runs left(-)/right(+), Y runs down(-)/up(+), and Z runs away(-)/towards(+).
			// Unity world space has the Z-axis running towards the back rather than the front, so that's why
			// some of the below code reverses the Z-axis.

			if (m_hasGyro)
			{
				m_gravity			= oscAcceleration; //Input.gyro.gravity;
				m_user				= oscAcceleration; //Input.gyro.userAcceleration;

				Vector3 g			= new Vector3(m_gravity.x, m_gravity.y, -m_gravity.z);
				m_attitudeXZ		= Quaternion.FromToRotation(g, Vector3.down);

				Quaternion rot		= m_rotationBase * FlipHands(Input.gyro.attitude);
				Vector3 angles		= rot.eulerAngles - m_calibrate;
				m_attitudeXYZ		= Quaternion.Euler(angles.x, angles.y, angles.z);
			}
			else
			{
				m_gravity			= Vector3.Lerp(m_gravity, oscAcceleration, c_gravityFilter);
                m_current           = oscAcceleration; //Input.acceleration;
				m_user				= m_current - m_gravity;

				Vector3 g			= new Vector3(m_gravity.x, m_gravity.y, -m_gravity.z);
				m_attitudeXZ		= Quaternion.FromToRotation(g, Vector3.down);
				m_attitudeXYZ		= m_attitudeXZ;
			}

			if (HandleGesture == null) return;

			bool aboveThreshold = m_user.magnitude >= IgnoreAccelBelow;
			if (!aboveThreshold)
				m_user = Vector3.zero;

			if (!m_cur.Started)
			{
				if (aboveThreshold)
					m_cur.Begin(m_user);
				m_recording.Record(m_user, 0, m_user.magnitude, m_attitudeXYZ);
				return;
			}

			float dot = Vector3.Dot(m_cur.m_dir.normalized, m_user.normalized);
			float spikeMag = 0f;

			if (aboveThreshold && (dot >= m_maxSpikeAngleDot))
			{
				// Continue current spike.
				m_cur.Update(m_user);
			}
			else
			{
				// End current spike and see if it completes a gesture.
				m_cur.End();

				if (m_cur.Magnitude < MinSpikeMagnitude)
				{
					// Not big enough to complete a spike.  But if we have a gesture queued up, fire it off.
					FireCandidateGesture();
				}
				else
				{
					// Big enough to be a spike, so add it to the array.
					spikeMag = m_cur.Magnitude;
					m_spikes.Add(m_cur);

#if DEBUG
					//Vector3 dir = new Vector3(m_cur.m_dir.x, m_cur.m_dir.y, -m_cur.m_dir.z);
					//Debug.DrawRay(transform.position, dir * 2f, Color.magenta, 0.5f);
#endif

					// Try to find a matching spike in the opposite direction to form a gesture.
					Spike match;
					bool foundMatch = m_spikes.FindMatch(m_cur, out match);
					if (!foundMatch)
					{
						// Didn't find one, but fire off any candidate that's queued up.
						FireCandidateGesture();
					}
					else
					{
						// Found a matching spike, so create a gesture from the two spikes.
						Gesture gest;
						gest.m_dirDevice	= m_cur.m_dir - match.m_dir;
						gest.m_dirDevice.z	= -gest.m_dirDevice.z;			// Accel/gyro z-axis points towards m_user, which is backwards.
						gest.m_dirWorldXZ	= m_attitudeXZ * gest.m_dirDevice;
						gest.m_dirWorldXYZ	= m_attitudeXYZ * gest.m_dirDevice;
						gest.m_elapsed		= m_cur.m_endTime - match.m_endTime;

						if (IgnoreWindUps)
						{
							// If we're ignoring wind-ups, we first save the gesture as a candidate (if we don't have one already).
							if (!m_hasGestureCandidate)
							{
								if (Time.time - m_lastGestureTime <= PostGestureCooldown)
								{
									// Ignore gesture since it's too soon since last gesture.
								}
								else
								{
									m_hasGestureCandidate	= true;
									m_gestureCandidate		= gest;
								}
							}
							else
							{
								// If we do already have a candidate, fire off whichever one is bigger.
								m_lastGestureTime		= Time.time;
								m_hasGestureCandidate	= false;

								// Take whichever one is bigger.
								if (gest.Magnitude >= m_gestureCandidate.Magnitude)
									HandleGesture(gest);
								else
									HandleGesture(m_gestureCandidate);
							}
						}
						else
						{
							m_lastGestureTime = Time.time;
							HandleGesture(gest);
						}
					}
				}

				if (aboveThreshold)
					m_cur.Begin(m_user);
				else
					m_cur.Clear();
			}

			m_recording.Record(m_user, dot, spikeMag, m_attitudeXYZ);
			m_spikes.PurgeOld();
		}

		private void FireCandidateGesture()
		{
			if (IgnoreWindUps && m_hasGestureCandidate)
			{
				m_lastGestureTime = Time.time;
				m_hasGestureCandidate = false;
				HandleGesture(m_gestureCandidate);
			}
		}
	}


	internal class Spikes
	{
		// This container class holds all spikes larger than MinSpikeMagnitude.

		private CMobile3dGestures	m_gestures;
		private List<Spike>			m_spikes;

		public int Count			{ get { return m_spikes.Count; } }

		public Spikes(CMobile3dGestures gestures)
		{
			m_gestures	= gestures;
			m_spikes	= new List<Spike>(50);
		}

		public void Add(Spike spike)
		{
			// Add the spike to the end.
			m_spikes.Add(spike);
		}

		public void PurgeOld()
		{
			// Remove any old spikes.
			for (int i = m_spikes.Count - 1; i >= 0; i--)
			{
				Spike oldSpike = (Spike)m_spikes[i];

				if (Time.time - oldSpike.m_endTime > m_gestures.MaxGestureAge)
				{
					// All remaining spikes are old, so remove them. (NOTE: they are at the beginning.)
					m_spikes.RemoveRange(0, i + 1);
					break;
				}
			}
		}

		public bool FindMatch(Spike newSpike, out Spike match)
		{
			// Start at the penultimate item since the last item is newSpike itself.
			for (int i = m_spikes.Count - 2; i >= 0; i--)
			{
				Spike oldSpike = (Spike)m_spikes[i];
				float dot = Vector3.Dot(oldSpike.m_dir.normalized, newSpike.m_dir.normalized);

				// If this old spike points mostly the opposite direction, and the two spikes combined
				// exceed the minimum gesture magnitude, it's a match.
				if ((dot < -m_gestures.m_maxGestureAngleDot) &&
					(newSpike.m_dir - oldSpike.m_dir).magnitude > m_gestures.MinGestureMagnitude)
				{
					match = oldSpike;
					return true;
				}
			}

			match = new Spike();
			return false;
		}
	}


	internal struct Spike
	{
		public Vector3		m_dir;
		public float		m_startTime;
		public float		m_endTime;
		public bool			Started				{ get { return m_startTime > 0f; } }
		public bool			Finished			{ get { return m_endTime > 0f; } }
		public float		Magnitude			{ get { return m_dir.magnitude; } }

		public void Clear()
		{
			m_dir				= Vector3.zero;
			m_startTime			= 0;
			m_endTime			= 0;

			//CUtil.Log("Spike clear.");
		}

		public void Begin(Vector3 end)
		{
			m_dir				= end;
			m_startTime			= Time.time;
			m_endTime			= Time.time;

			//CUtil.Log("Spike begin.");
		}

		public void Update(Vector3 accel)
		{
			m_endTime = Time.time;

			// Ignore readings that are smaller than our current vector.
			if (accel.magnitude <= m_dir.magnitude)
				return;

			// Combine the vectors based on relative magnitudes.  (Whichever vector has the greater magnitude
			// will affect the result the most.)
			float totalMag = m_dir.magnitude + accel.magnitude;
			if (totalMag == 0)		
				totalMag = 0.001f;		// Make sure it's never 0.

			float pct	= accel.magnitude / totalMag;
			m_dir		= Vector3.Lerp(m_dir, accel, pct);
		}

		public void End()
		{
			m_endTime	= Time.time;

			//CUtil.Log("Spike end:  dir=({0:F2},{1:F2},{2:F2})  elapsed={3:F3}s",
			//	m_dir.x, m_dir.y, m_dir.z, m_endTime - m_startTime);
		}
	}


	internal class Recording
	{
		private class Rec
		{
			public int			m_frame;
			public Vector3		m_user;
			public float		m_angle;
			public float		m_spike;
			public Vector3		m_attitude;
		}

		private bool			m_recording		= false;
		private List<Rec>		m_recs;

		public void ToggleRecording()
		{
			if (m_recording)
				Stop();
			else
				Start();
		}

		public void Record(Vector3 user, float angle, float spike, Quaternion attitude)
		{
			if (!m_recording) return;

			Rec rec			= new Rec();
			rec.m_frame		= Time.frameCount;
			rec.m_user		= user;
			rec.m_angle		= angle;
			rec.m_spike		= spike;
			rec.m_attitude	= attitude.eulerAngles;

			m_recs.Add(rec);
		}

		private void Start()
		{
			m_recording		= true;
			m_recs			= new List<Rec>();
		}

		private void Stop()
		{
			m_recording = false;

#if UNITY_EDITOR
			string dir = Directory.GetParent(Application.dataPath).FullName;
#else
			string dir = Application.persistentDataPath;
#endif
			if (!dir.EndsWith("/")) dir += "/";
			dir += "GestureLogs";
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);

			string fname = string.Format("{0}/Gest_{1}.csv", dir, DateTime.Now.ToString("MMdd_HHmmss"));

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("FRM,UX,UY,UZ,ANG,SPK,AX,AY,AZ");

			foreach (Rec rec in m_recs)
			{
				sb.AppendFormat(
					"{0},{1:F5},{2:F5},{3:F5},{4:F2},{5:F1},{6:F5},{7:F5},{8:F5}",
					rec.m_frame, rec.m_user.x, rec.m_user.y, rec.m_user.z, rec.m_angle, rec.m_spike,
					rec.m_attitude.x, rec.m_attitude.y, rec.m_attitude.z);
				sb.AppendLine();
			}

			File.WriteAllText(fname, sb.ToString());
		}
	}
}
