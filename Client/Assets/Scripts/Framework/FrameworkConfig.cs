using UnityEngine;

[UnityEngine.DefaultExecutionOrder(-100)]
public class FrameworkConfig : MonoBehaviour
{
	public GameDeploymentMode DeploymentMode;
	public bool Logging = true;

	private string VersionFileList = FrameworkConstant.VersionFileListName;
	private string VersionFileHashList = FrameworkConstant.VersionFileHashListName;

	private HotUpdate HotUpdate;
	private MainLuaBehaviour MainLuaScriptModule;

	private string ReadOnlyRoot;
	private string ReadWriteRoot;

	private void Awake()
	{
		FrameworkConstant.GDM = this.DeploymentMode;
		FrameworkConstant.AllowLogging = Logging;
		DontDestroyOnLoad(this.gameObject);
	}

	void Start()
	{
		if (FrameworkConstant.GDM == GameDeploymentMode.UpdateMode)
		{
			this.HotUpdate = this.gameObject.AddComponent<HotUpdate>();
			this.HotUpdate.VersionFileName = this.VersionFileList;
			this.HotUpdate.VersionFileHashName = this.VersionFileHashList;
			this.HotUpdate.DoHotUpdateFiles();
		}

#if UNITY_EDITOR
		if (FrameworkConstant.GDM != GameDeploymentMode.EditorMode)
		{
			FrameworkManager.Resource.ParseVersionFile();
		}
#endif

		FrameworkManager.Event.Subscribe((int)GameEvent.LuaScriptsInitialize, OnLuaScriptsInitialized);
		//FrameworkManager.Resource.LoadUIPrefab("Test", Test);
	}

	private void OnLuaScriptsInitialized(object args)
	{
		if (System.Object.ReferenceEquals(FrameworkManager.Lua, null) || FrameworkManager.Lua.LuaEnvironmentParser == null)
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
		FrameworkManager.Event.Unsubscribe((int)GameEvent.LuaScriptsInitialize, OnLuaScriptsInitialized);

		Destroy(MainLuaScriptModule);
	}
}
