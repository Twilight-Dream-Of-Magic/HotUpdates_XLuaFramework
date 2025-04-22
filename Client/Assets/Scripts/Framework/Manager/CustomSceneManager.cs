using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomSceneManager : MonoBehaviour
{
	private string SceneLogicName = "[SceneLogic]";

	private void Awake()
	{
		SceneManager.activeSceneChanged += OnActiveSceneChanged;
	}

	private void OnDestroy()
	{
		SceneManager.activeSceneChanged -= OnActiveSceneChanged;
	}

	/// <summary>
	/// 切换场景之后的回调函数
	/// </summary>
	/// <param name="a"></param>
	/// <param name="b"></param>
	private void OnActiveSceneChanged(Scene a, Scene b)
	{
		if (!a.isLoaded || b.isLoaded)
			return;

		CustomSceneLogic a_logic = GetCustomSceneLogic(a);
		CustomSceneLogic b_logic = GetCustomSceneLogic(b);

		a_logic?.OnInactive();
		b_logic?.OnActive();
	}

	private bool IsLoadedScene(string SceneName)
	{
		Scene scene = SceneManager.GetSceneByName(SceneName);
		return scene.isLoaded;
	}

	private IEnumerator LoadScene(string SceneName, string LuaScriptName, LoadSceneMode Mode)
	{
		if (IsLoadedScene(SceneName))
			yield break;

		AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(SceneName, Mode);
		asyncOperation.allowSceneActivation = true;
		yield return asyncOperation;

		Scene scene = SceneManager.GetSceneByName(SceneName);
		GameObject gameObjects = new GameObject(SceneLogicName);
		SceneManager.MoveGameObjectToScene(gameObjects, scene);
		Debug.Log(SceneName);

		CustomSceneLogic customSceneLogic = gameObjects.AddComponent<CustomSceneLogic>();
		customSceneLogic.SceneName = SceneName;
		customSceneLogic.Initialize(LuaScriptName);
		customSceneLogic.OnEnter();
	}

	private IEnumerator LoadSceneAndSetActive(string SceneName, string LuaScriptName)
	{
		Scene scene = SceneManager.GetSceneByName(SceneName);

		if (IsLoadedScene(SceneName))
		{
			SceneManager.SetActiveScene(scene);
		}
		else
		{
			AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
			asyncOperation.allowSceneActivation = true;
			yield return asyncOperation;

			scene = SceneManager.GetSceneByName(SceneName);
			SceneManager.SetActiveScene(scene);
		}
		
		GameObject gameObjects = new GameObject(SceneLogicName);
		SceneManager.MoveGameObjectToScene(gameObjects, scene);
		Debug.Log(SceneName);

		CustomSceneLogic customSceneLogic = gameObjects.AddComponent<CustomSceneLogic>();
		customSceneLogic.SceneName = SceneName;
		customSceneLogic.Initialize(LuaScriptName);
		customSceneLogic.OnEnter();
	}

	private CustomSceneLogic GetCustomSceneLogic(Scene scene)
	{
		GameObject[] gameObjects = scene.GetRootGameObjects();
		foreach (GameObject gameObject in gameObjects)
		{
			if (gameObject.name.CompareTo(SceneLogicName) == 0)
			{
				CustomSceneLogic customSceneLogic = gameObject.GetComponent<CustomSceneLogic>();
				return customSceneLogic;
			}
		}
		return null;
	}

	private IEnumerator UnloadScene(string SceneName)
	{
		Scene scene = SceneManager.GetSceneByName(SceneName);
		if (!scene.isLoaded)
		{
			Debug.LogError("The scene not is loaded!");
			yield break;
		}
		CustomSceneLogic customSceneLogic = GetCustomSceneLogic(scene);
		customSceneLogic?.OnExit();
		customSceneLogic?.Uninitialize();
		AsyncOperation asyncOperation = SceneManager.UnloadSceneAsync(scene, UnloadSceneOptions.None);
		yield return asyncOperation;
		Debug.Log(SceneName);
	}

	/*
		Calling from lua scripts
		从 lua 脚本调用
	*/

	/// <summary>
	/// Activate a loaded scene by name.
	/// 激活已加载的场景
	/// </summary>
	/// <param name="sceneName">The name of the scene to activate 场景名称</param>
	public void SetActive(string sceneName)
	{
		// Try to get the Scene object by name
		// 尝试通过名称获取 Scene 对象
		Scene scene = SceneManager.GetSceneByName(sceneName);

		// If the scene handle is not valid, bail out
		// 如果场景句柄无效，则直接返回
		if (!scene.IsValid())
		{
			Debug.LogError($"[SetActive] Invalid scene name: '{sceneName}'. Make sure it’s added to Build Settings.");
			// [SetActive] 无效的场景名称：'{sceneName}'。请确认已将该场景添加到构建设置中。
			return;
		}

		// If the scene isn’t loaded yet, we cannot activate it
		// 如果场景尚未加载，则无法将其设为活动场景
		if (!scene.isLoaded)
		{
			Debug.LogError($"[SetActive] Scene '{sceneName}' is not loaded. Load or Additively load it before activation.");
			// [SetActive] 场景 '{sceneName}' 尚未加载。请先加载或以 Additive 方式加载该场景。
			return;
		}

		// Finally, set this scene as active
		// 将该场景设为活动场景
		bool switched = SceneManager.SetActiveScene(scene);
		if (switched)
		{
			Debug.Log($"[SetActive] Successfully activated scene: '{sceneName}'.");
			// [SetActive] 成功激活场景：'{sceneName}'。
		}
		else
		{
			Debug.LogError($"[SetActive] Failed to activate scene: '{sceneName}'.");
			// [SetActive] 激活场景失败：'{sceneName}'。
		}
	}

	public void LoadSceneAsync(string SceneName, string LuaScriptName)
	{
		FrameworkManager.Resource.LoadScene
		(
			SceneName,
			(UnityEngine.Object obj) =>
			{
				StartCoroutine(LoadScene(SceneName, LuaScriptName, LoadSceneMode.Additive));
			}
		);
	}

	public void LoadSceneAndSetActiveAsync(string SceneName, string LuaScriptName, LoadSceneMode mode = LoadSceneMode.Single)
	{
		FrameworkManager.Resource.LoadScene
		(
			SceneName,
			(UnityEngine.Object obj) =>
			{
				StartCoroutine(LoadSceneAndSetActive(SceneName, LuaScriptName));
			}
		);
	}

	public void ChangeSceneAsync(string SceneName, string LuaScriptName)
	{
		FrameworkManager.Resource.LoadScene
		(
			SceneName,
			(UnityEngine.Object obj) =>
			{
				StartCoroutine(LoadScene(SceneName, LuaScriptName, LoadSceneMode.Single));
			}
		);
	}

	public void UnloadSceneAsync(string SceneName)
	{
		StartCoroutine(UnloadScene(SceneName));
	}
}
