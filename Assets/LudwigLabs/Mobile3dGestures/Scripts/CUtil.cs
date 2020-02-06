///////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2016, Ludwig Labs - All Rights Reserved
//
// Use of this code is governed by the terms of the Unity Asset Store EULA,
// which can be found here: https://unity3d.com/legal/as_terms

using UnityEngine;
using System;

namespace Gestures
{
	public class CUtil : MonoBehaviour
	{
		// This function will return the 90-degree world axis that is closest to the vector.
		public static Vector3 ClosestAxis(Vector3 v)
		{
			float dotX = Vector3.Dot(v, Vector3.right);
			float dotY = Vector3.Dot(v, Vector3.up);
			float dotZ = Vector3.Dot(v, Vector3.forward);

			float absX = Mathf.Abs(dotX);
			float absY = Mathf.Abs(dotY);
			float absZ = Mathf.Abs(dotZ);

			if (absX > absY && absX > absZ)
				return dotX > 0 ? Vector3.right : Vector3.left;
			else if (absY > absX && absY > absZ)
				return dotY > 0 ? Vector3.up : Vector3.down;
			else if (absZ > absX && absZ > absY)
				return dotZ > 0 ? Vector3.forward : Vector3.back;
		
			return Vector3.zero;
		}

		public static void Log(string format, params object[] args)
		{
			if (!Debug.isDebugBuild)
				return;

			string s = string.Format(format, args);
			string msg = string.Format("{0} {1}", DateTime.Now.ToString("HH:mm:ss.fff"), s);

			Debug.Log(msg);
		}

		public static void DebugDrawPoint(Vector3 pt, Color clr, float size = 0.05f, float duration = 0f)
		{
			size /= 2f;

			Debug.DrawLine(pt + Vector3.left * size,	pt + Vector3.right		* size,		clr,	duration);
			Debug.DrawLine(pt + Vector3.down * size,	pt + Vector3.up			* size,		clr,	duration);
			Debug.DrawLine(pt + Vector3.back * size,	pt + Vector3.forward	* size,		clr,	duration);
		}

		public static void DebugDrawRotation(Quaternion rot, Vector3 pt, float size = 1f, float duration = 0f)
		{
			Vector3 x = rot * Vector3.right;
			Vector3 y = rot * Vector3.up;
			Vector3 z = rot * Vector3.forward;

			Debug.DrawRay(pt, x * size,		Color.red,		duration);
			Debug.DrawRay(pt, y * size,		Color.green,	duration);
			Debug.DrawRay(pt, z * size,		Color.blue,		duration);
		}

		public static Vector3 MakeAnglesSigned(Vector3 a)
		{
			if (Mathf.Abs(a.x) >= 360f)		a.x = a.x % 360f;
			if (Mathf.Abs(a.y) >= 360f)		a.y = a.y % 360f;
			if (Mathf.Abs(a.z) >= 360f)		a.z = a.z % 360f;

			if (a.x > 180)		a.x -= 360;
			if (a.x <= -180)	a.x += 360;

			if (a.y > 180)		a.y -= 360;
			if (a.y <= -180)	a.y += 360;

			if (a.z > 180)		a.z -= 360;
			if (a.z <= -180)	a.z += 360;

			return a;
		}
	}
}
