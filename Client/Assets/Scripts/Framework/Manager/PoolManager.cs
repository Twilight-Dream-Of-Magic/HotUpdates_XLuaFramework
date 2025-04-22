using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
	[SerializeField]
	private Transform PoolParent;

	private Dictionary<string, PoolBase> Pools = new Dictionary<string, PoolBase>();

	private void Awake()
	{
		PoolParent = this.transform.parent.Find("RootPool");
	}

	private void OnApplicationQuit()
	{
		PoolParent = null;
	}

	private void CreatePool<T>(string PoolName, float ReleaseObjectTimeSpan) where T : PoolBase
	{
		if(!Pools.TryGetValue(PoolName, out PoolBase Pool))
		{
			GameObject gameObject = new GameObject(PoolName);
			gameObject.transform.SetParent(PoolParent);
			Pool = gameObject.AddComponent<T>();
			Pool.Initialize(ReleaseObjectTimeSpan);
			Pools.Add(PoolName, Pool);
		}
	}

	public void CreateGameObjectPool(string PoolName, float ReleaseObjectTimeSpan)
	{
		CreatePool<GameObjectPool>(PoolName, ReleaseObjectTimeSpan);
	}

	public void CreateAssetPool(string PoolName, float ReleaseObjectTimeSpan)
	{
		CreatePool<AssetPool>(PoolName, ReleaseObjectTimeSpan);
	}

	public PoolBase GetPool(string PoolName)
	{
		if (Pools.TryGetValue(PoolName, out PoolBase Pool))
		{
			return Pool;
		}
		return null;
	}

	public void RemovePool(string PoolName)
	{
		if (Pools.TryGetValue(PoolName, out PoolBase Pool))
		{
			Pool.Uninitialize();
			Destroy(Pool.gameObject);
			Pools.Remove(PoolName);
		}
	}

	public UnityEngine.Object Spawn(string PoolName, string AssetName)
	{
		if (Pools.TryGetValue(PoolName, out PoolBase Pool))
		{
			return Pool.Spawn(AssetName);
		}
		return null;
	}

	public void Unspawn(string PoolName, string AssetName, UnityEngine.Object Asset)
	{
		if (Pools.TryGetValue(PoolName, out PoolBase Pool))
		{
			Pool.Unspawn(AssetName, Asset);
		}
	}
}
