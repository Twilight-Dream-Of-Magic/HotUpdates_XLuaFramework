using UnityEngine;

public class MainLuaBehaviour : LuaBehaviour
{
	private const string LuaModuleName = "Main";

	private void Awake()
	{
		Debug.LogWarning("Initialize Main LuaBehaviour");
		base.Initialize(LuaModuleName);

		Debug.LogWarning("Create all Object Pool");
		FrameworkManager.UIPool.CreatePool("UI");
		FrameworkManager.Pool.CreateGameObjectPool("Entity", 500);
		FrameworkManager.Pool.CreateAssetPool("AssetBundle", 300);
	}

	private void OnApplicationQuit()
	{
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
	}
}
