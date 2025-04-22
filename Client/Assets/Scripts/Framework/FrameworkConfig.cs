using UnityEngine;
using Utilities;

[UnityEngine.DefaultExecutionOrder(-100)]
public class FrameworkConfig : MonoBehaviour
{
	public GameDeploymentMode DeploymentMode;
	public bool Logging = true;

	private string VersionFileList = FrameworkConstant.VersionFileList;
	private string VersionFileHashList = FrameworkConstant.VersionFileHashList;

	private HotUpdate HotUpdate;
	private MainLuaBehaviour MainLuaScriptModule;

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
			this.HotUpdate.DoHotUpdate();
		}

		if (FrameworkConstant.GDM == GameDeploymentMode.PackageBundle)
		{
			string versionFileListPath = PathUtil.BuildResourcesPath + this.VersionFileList;
			if (!FileReaderWriter.IsExist(versionFileListPath))
			{
				Debug.LogErrorFormat("Version file list: {0} local file is lost! ", versionFileListPath);
				return;
			}

			FrameworkManager.Resource.ParseVersionFile();
		}

		FrameworkManager.Event.Subscribe((int)GameEvent.LuaScriptsInitialize, OnLuaScriptsInitialized);

#if UNITY_EDITOR
		if (FrameworkConstant.GDM == GameDeploymentMode.EditorMode)
		{
			Debug.LogWarning("Initialize Lua Manager");
			FrameworkManager.Lua.Initialize();
		}
#endif
	}

	private void OnLuaScriptsInitialized(object args)
	{
		if (System.Object.ReferenceEquals(FrameworkManager.Lua, null) || FrameworkManager.Lua.LuaEnvironmentParser == null)
		{
			Debug.LogError("Lua environment is not initialized.");
			return;
		}

		MainLuaScriptModule = MainLuaBehaviour.Instance;

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
		Destroy(MainLuaScriptModule);
	}
}
