using UnityEngine;

public class MainLuaBehaviour : LuaBehaviour
{
	private const string LuaModuleName = "Main";
	private static MainLuaBehaviour _instance;
	public static MainLuaBehaviour Instance
	{
		get
		{
			if (System.Object.ReferenceEquals(_instance, null))
			{
				//FrameworkConfig.cs GameObject is DontDestroyOnLoaded!!!
				GameObject FrameworkRootGameObject = GameObject.Find("FrameworkRoot");
				if (System.Object.ReferenceEquals(FrameworkRootGameObject, null))
				{
					FrameworkRootGameObject = new GameObject("FrameworkRoot");
					DontDestroyOnLoad(FrameworkRootGameObject);
				}
				_instance = FrameworkRootGameObject.AddComponent<MainLuaBehaviour>();
			}

			return _instance;
		}	
	}

	private void Awake()
	{
		// Duplicate instance? �� Destroy this instance
		if (!System.Object.ReferenceEquals(_instance, null) && _instance != this)
		{
			Debug.LogWarning("[MainLuaBehaviour] Duplicate detected, destroying this GameObject.");
#if UNITY_EDITOR
			Destroy(gameObject);
#else
			DestroyImmediate(gameObject);
#endif
			return;
		}

		// Only when reaching here for the first time, register itself as the singleton
		_instance = this;

		Debug.LogWarning("Initialize Main LuaBehaviour");
		base.Initialize(LuaModuleName);

		Debug.LogWarning("Create all Object Pool");
		FrameworkManager.UIPool.CreatePool("UI");
		FrameworkManager.Pool.CreateGameObjectPool("Entity", 500);
		FrameworkManager.Pool.CreateAssetPool("AssetBundle", 300);
	}

	private void OnApplicationQuit()
	{
		// Duplicate instance? �� Do not execute anything!
		if (!System.Object.ReferenceEquals(_instance, null) && _instance != this)
		{
			return;
		}

		//Instance is disposed? �� Do not execute anything!
		if (this.IsDisposed)
		{
			return;
		}

		base.LuaScriptOnApplicationQuit?.Invoke();
		base.LuaScriptOnApplicationQuit = null;

		Debug.LogWarning("Remove all Object Pool");
		FrameworkManager.UIPool.RemovePool("UI");
		FrameworkManager.Pool.RemovePool("Entity");
		FrameworkManager.Pool.RemovePool("AssetBundle");

		Debug.LogWarning("Uninitialize Main LuaBehaviour");
		base.Uninitialize();

		Debug.LogWarning("Finalize Lua Manager");
		FrameworkManager.Lua.DoFinalize();

		_instance = null;
	}
}
