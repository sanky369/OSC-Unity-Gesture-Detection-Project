using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace GesturesDemo
{
	public class CSimpleObjectPool<T> where T : new()
	{
		private List<T>		m_cache;

		public CSimpleObjectPool(int expectedMax = 20)
		{
			m_cache = new List<T>(expectedMax);
		}

		public T Take()
		{
			if (m_cache.Count == 0)
			{
				return new T();
			}
			else
			{
				object obj = m_cache[m_cache.Count - 1];
				m_cache.RemoveAt(m_cache.Count - 1);
				return (T)obj;
			}
		}

		public void Return(T obj)
		{
			m_cache.Add(obj);
		}
	}

	public class CSimpleTransformPool
	{
		private ArrayList		m_cache;
		private Transform		m_prototype;

		public CSimpleTransformPool(Transform prototype, int expectedMax = 20)
		{
			m_prototype		= prototype;
			m_cache			= new ArrayList(expectedMax);
		}

		public Transform Take()
		{
			if (m_cache.Count == 0)
			{
				Transform obj = GameObject.Instantiate<Transform>(m_prototype);
				Renderer r = obj.GetComponent<Renderer>();
				if (r != null) r.enabled = true;
				return obj;
			}
			else
			{
				Transform obj = (Transform)m_cache[m_cache.Count - 1];
				m_cache.RemoveAt(m_cache.Count - 1);
				return obj;
			}
		}

		public void Return(Transform obj)
		{
			m_cache.Add(obj);
		}
	}
}
