using UnityEngine;

public class FrameworkConfig : MonoBehaviour
{
	public GameDeploymentMode DeploymentMode;

	private MainLuaBehaviour MainLuaScriptModule;
	private void Awake()
	{
		FrameworkConstant.GDM = this.DeploymentMode;
		DontDestroyOnLoad(this.gameObject);

#if UNITY_EDITOR
		Debug.unityLogger.logEnabled = true;
#else
		if (Debug.isDebugBuild)
			Debug.unityLogger.logEnabled = true;
		else
			Debug.unityLogger.logEnabled = false;
#endif
	}

	void Start()
	{
		FrameworkManager.Event.Subscribe(0, OnLuaScriptsInitialized);

#if !UNITY_EDITOR
		if (FrameworkConstant.GDM != GameDeploymentMode.EditorMode)
		{
			FrameworkManager.Resource.ParseVersionFile();
		}
#endif

		//FrameworkManager.Resource.LoadUIPrefab("Test", Test);

		Debug.LogWarning("Initialize Lua Manager");
		FrameworkManager.Lua.Initialize();
	}

	private void OnLuaScriptsInitialized(object args)
	{
		if (FrameworkManager.Lua == null || FrameworkManager.Lua.LuaEnvironmentParser == null)
		{
			Debug.LogError("Lua environment is not initialized.");
			return;
		}

		MainLuaScriptModule = this.gameObject.AddComponent<MainLuaBehaviour>();

		// 加载 Lua 脚本
		//FrameworkManager.Lua.LoadLuaScript("Main");

		// 获取 Lua 返回的实例对象
		//var MainInstance = FrameworkManager.Lua.LuaEnvironmentParser.Global.Get<XLua.LuaTable>("MainInstance");
		//if (MainInstance is not null)
		//{
		//	XLua.LuaFunction StartFunction = MainInstance.Get<XLua.LuaFunction>("Start");
		//	if (StartFunction != null)
		//	{
		//		StartFunction.Call();
		//	}
		//}
	}

	public void OnApplicationQuit()
	{
		FrameworkManager.Event.Unsubscribe(0, OnLuaScriptsInitialized);

		Destroy(MainLuaScriptModule);
	}

	void Test(UnityEngine.Object obj)
	{
		Debug.Log("Name" + obj.name);
		Debug.Log("InstanceID:" + obj.GetInstanceID());

		UnityEngine.Object obj_instance = Instantiate(obj, Vector3.zero, Quaternion.identity);
		obj_instance.name = "Test0";
	}
}
