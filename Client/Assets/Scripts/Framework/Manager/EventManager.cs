using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EventManager : MonoBehaviour
{
	public delegate void EventHandler(object args);

	private Dictionary<int, EventHandler> EventCaches = new Dictionary<int, EventHandler>();

	private void OnApplicationQuit()
	{
		// 先把所有 ID 取出来，避免在循环中修改字典
		int[] IDs = EventCaches.Keys.ToArray();
		for (int index = 0; index < IDs.Length; index++)
		{
			int id = IDs[index];
			var handler = EventCaches[id];
			if (handler == null)
				continue;

			// 用 GetInvocationList() 拿到订阅者数组
			Delegate[] list = handler.GetInvocationList();
			// 再用 for 循环逐一解绑
			for (int handler_index = 0; handler_index < list.Length; handler_index++)
			{
				EventCaches[id] -= (EventHandler)list[handler_index];
			}
		}

		// 最后清空字典
		EventCaches.Clear();
	}

	public void Subscribe(int ID, EventHandler @EventHandler)
	{
		if (EventCaches.ContainsKey(ID))
		{
			EventCaches[ID] += @EventHandler;
		}
		else
		{
			EventCaches.Add(ID, @EventHandler);
		}
	}
	public void Unsubscribe(int ID, EventHandler @EventHandler)
	{
		if (EventCaches.ContainsKey(ID))
		{
			if (EventCaches[ID] != null)
				EventCaches[ID] -= @EventHandler;

			if (EventCaches[ID] == null)
				EventCaches.Remove(ID);
		}
	}

	public void Invoke(int ID, object args = null)
	{
		if (EventCaches.TryGetValue(ID, out EventHandler handler))
		{
			handler?.Invoke(args);
		}
	}
}
