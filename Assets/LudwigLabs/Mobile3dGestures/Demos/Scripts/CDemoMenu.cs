using UnityEngine;
using System.Collections;

namespace GesturesDemo
{
	public class CDemoMenu : MonoBehaviour
	{
		public void OpenScene(string sceneName)
		{
			Application.LoadLevel(sceneName);
		}
	}
}
