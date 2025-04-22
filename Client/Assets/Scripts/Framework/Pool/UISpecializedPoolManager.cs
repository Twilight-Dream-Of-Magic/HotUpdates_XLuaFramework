using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 专门管理多个 UISpecializedPool 的管理器，负责按 UI 类型 key 建池和调度。
/// Manages multiple UISpecializedPool instances, creating and dispatching by UI type key.
/// </summary>
public class UISpecializedPoolManager : MonoBehaviour
{
	// key -> 单 UI 类型池
	// Mapping from UI type key to its specialized pool
	private Dictionary<string, UISpecializedPool> pools;

	/// <summary>
	/// 初始化管理器，创建内部字典。
	/// Initialize the manager by creating the internal dictionary.
	/// </summary>
	public void Initialize()
	{
		pools = new Dictionary<string, UISpecializedPool>();
	}

	/// <summary>
	/// 取消初始化，销毁所有池并清空。
	/// Uninitialize: destroy all pools and clear the dictionary.
	/// </summary>
	public void Uninitialize()
	{
		if (pools == null) return;
		foreach (var pool in pools.Values)
			pool.Uninitialize();
		pools.Clear();
		pools = null;
	}

	/// <summary>
	/// 获取指定 key 的 UISpecializedPool，如果不存在则返回 null。  
	/// Get the UISpecializedPool for the specified key; returns null if none exists.
	/// </summary>
	public UISpecializedPool GetPool(string key)
	{
		if (pools != null && pools.TryGetValue(key, out var pool))
			return pool;
		return null;
	}

	/// <summary>
	/// 创建并初始化指定 key 的 UISpecializedPool；如果已存在则直接返回现有实例。  
	/// Create (and initialize) the UISpecializedPool for the specified key; if it already exists, return the existing instance.
	/// </summary>
	public UISpecializedPool CreatePool(string key)
	{
		if (pools == null)
			Initialize();

		if (!pools.TryGetValue(key, out var pool))
		{
			pool = new UISpecializedPool();
			pool.Initialize();
			pools[key] = pool;
		}
		return pool;
	}

	/// <summary>
	/// 从指定 key 的池中取出一个 UI；若无可用，则返回 null。  
	/// 如果池不存在，则先创建再取。  
	/// Spawn a UI from the pool for the specified key; returns null if none available.  
	/// If the pool does not exist, it will be created first.
	/// </summary>
	public GameObject Spawn(string key)
	{
		// 先尝试拿已有池
		var pool = GetPool(key);
		// 池不存在则创建
		if (System.Object.ReferenceEquals(pool, null))
			pool = CreatePool(key);
		// 从池里取（队列空则返回 null）
		return pool.Spawn();
	}

	/// <summary>
	/// 将已关闭的 UI 返回到对应 key 的池中。  
	/// 如果池不存在，则先创建再放回。  
	/// Unspawn (close) a UI and return it to its pool for the specified key.  
	/// If the pool does not exist, it will be created first.
	/// </summary>
	public void Unspawn(string key, GameObject go)
	{
		// 先尝试拿已有池
		var pool = GetPool(key);
		// 池不存在则创建
		if (System.Object.ReferenceEquals(pool, null))
			pool = CreatePool(key);
		// 隐藏并入队
		pool.Unspawn(go);
	}

	/// <summary>
	/// 移除并销毁指定 key 池内所有缓存。
	/// Remove and destroy all cached UI for the specified key.
	/// </summary>
	public void RemovePool(string key)
	{
		if (pools == null || !pools.TryGetValue(key, out var pool))
			return;
		pool.ReleaseAll();
		pools.Remove(key);
	}

	/// <summary>
	/// 释放并销毁所有池内缓存的 UI。
	/// Clear and destroy all cached UI across all pools.
	/// </summary>
	public void ClearAllPools()
	{
		if (pools == null) return;
		foreach (var pool in pools.Values)
			pool.Uninitialize();
		pools.Clear();
	}
}