using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单 UI 类型的专用对象池，只缓存已实例化并完成初始化的 UI GameObject。
/// A specialized pool for a single UI type, caching only instantiated and initialized UI GameObjects.
/// </summary>
public class UISpecializedPool
{
	// Queue 缓存同一类型的 UI 实例
	// Queue caches instances of the same UI type
	private Queue<GameObject> queue;

	/// <summary>
	/// 初始化池，创建队列。
	/// Initialize the pool by creating the queue.
	/// </summary>
	public void Initialize()
	{
		queue = new Queue<GameObject>();
	}

	/// <summary>
	/// 取消初始化，销毁所有缓存并清空队列。
	/// Uninitialize the pool: destroy all cached objects and clear the queue.
	/// </summary>
	public void Uninitialize()
	{
		ReleaseAll();
		queue = null;
	}

	/// <summary>
	/// 从池中取出一个已初始化的 UI；若无可用，则返回 null。
	/// Spawn an initialized UI from the pool; returns null if none available.
	/// </summary>
	/// <returns>已缓存的 UI 实例或 null。
	/// The cached UI instance or null.</returns>
	public GameObject Spawn()
	{
		if (queue != null && queue.Count > 0)
		{
			var go = queue.Dequeue();
			go.SetActive(true); // 激活 UI
			return go;
		}
		return null;
	}

	/// <summary>
	/// 将已关闭的 UI 放回池中，仅做隐藏和入队操作。
	/// Unspawn (close) a UI: deactivate and enqueue it into the pool.
	/// </summary>
	/// <param name="go">已初始化且准备缓存的 UI 实例。
	/// The initialized UI instance to cache.</param>
	public void Unspawn(GameObject go)
	{
		go.SetActive(false); // 隐藏 UI
		queue.Enqueue(go);
	}

	/// <summary>
	/// 清理并销毁池中所有缓存的 UI。
	/// Clear and destroy all cached UI in the pool.
	/// </summary>
	public void ReleaseAll()
	{
		if (queue == null) return;
		while (queue.Count > 0)
		{
			GameObject.Destroy(queue.Dequeue());
		}
	}
}