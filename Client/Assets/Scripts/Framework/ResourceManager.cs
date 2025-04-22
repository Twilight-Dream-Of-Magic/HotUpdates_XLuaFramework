using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class ResourceManager : MonoBehaviour
{
	internal class BundleInfo
	{
		public string AssetsName;
		public string BundleName;
		public List<string> DependenceFilePaths;
	}

	internal class BundleData
	{
		public AssetBundle Bundle;

		//资源包被几个对象给使用？
		public int ReferencedCount;

		public BundleData(AssetBundle bundle)
		{
			Bundle = bundle;
			ReferencedCount = 1;
		}
	}

	//存放Bundle信息
	private Dictionary<string, BundleInfo> BundleInfos = new Dictionary<string, BundleInfo>();
	//存放Bundle资源
	private Dictionary<string, BundleData> AssetBundles = new Dictionary<string, BundleData>();

	public void ParseVersionFile()
	{
		string versionFileListPath = PathUtil.BuildResourcesPath + FrameworkConstant.VersionFileList;
		if (!File.Exists(versionFileListPath))
		{
			Debug.LogError($"The version file list: {versionFileListPath} is lost! ");
			return;
		}

		string[] data = File.ReadAllLines(versionFileListPath);

		for (int i = 0; i < data.Length; i++)
		{
			BundleInfo bundleInfo = new BundleInfo();
			string[] info = data[i].Split('|');

			//资源名
			bundleInfo.AssetsName = info[0];
			//Bundle名称
			bundleInfo.BundleName = info[1];

			bundleInfo.DependenceFilePaths = new List<string>(info.Length - 2);
			//第3位开始才是依赖
			for (int j = 2; j < info.Length; j++)
			{
				bundleInfo.DependenceFilePaths.Add(info[j]);
			}

			BundleInfos.Add(bundleInfo.AssetsName, bundleInfo);

			if (bundleInfo.AssetsName.IndexOf("LuaScripts") > 0)
			{
				if (!FrameworkManager.Lua.LuaFileNameList.Contains(bundleInfo.AssetsName))
				{
					FrameworkManager.Lua.LuaFileNameList.Add(bundleInfo.AssetsName);
				}
			}
		}
	}

	/// <summary>
	/// 异步加载指定名称的 Bundle 及其所有依赖，最后启动加载资产的协程。
	/// Asynchronously loads the specified AssetBundle (and its dependencies), then starts the coroutine to load the asset.
	/// </summary>
	/// <param name="AssetName">要加载的资源名称。/ Name of the asset to load.</param>
	/// <param name="CallbackAction">加载完成后的回调函数，可为空。/ Callback invoked with the loaded UnityEngine.Object, may be null.</param>
	private IEnumerator LoadBundleAsync(string AssetName, Action<UnityEngine.Object> CallbackAction = null)
	{
		if (!BundleInfos.ContainsKey(AssetName))
		{
			Debug.LogError($"AssetName '{AssetName}' not found in bundleInfos.");
			CallbackAction?.Invoke(null);
			yield break;
		}

		// 用于存放待加载的 Bundle 名称栈 / Stack to hold bundle names to load
		Stack<string> bundleFilePathStack = new Stack<string>();
		bundleFilePathStack.Push(AssetName);

		// 根 Bundle 名称 / Root bundle name
		string bundleNameRoot = BundleInfos[AssetName].BundleName;

		// ——————————————————————————————————————————————————
		// 三阶段加载策略：
		// 1) 第一次对象池和缓存都不命中 → 加载依赖并移除缓存，退回对象池
		// 2) 第二次无缓存 → 对象池命中并写入缓存
		// 3) 第三次 → 缓存命中直接加载
		// ——————————————————————————————————————————————————

		// 检查是否已经缓存该 Bundle，并增加引用计数
		// Check cache for bundle and increment reference count if found
		BundleData bundleData = GetAssetBundleAndIncrementReference(bundleNameRoot);
		if (bundleData != null)
		{
			// 如果已缓存，则直接启动加载资产的协程
			// If cached, start loading asset coroutine immediately
			StartCoroutine(LoadAssetBundle(AssetName, CallbackAction, bundleData.Bundle));
			yield break;
		}

		// AssetBundle 尝试从对象池获取 / Try to spawn from pool
		UnityEngine.Object InPoolMainObject = FrameworkManager.Pool.Spawn("AssetBundle", bundleNameRoot);
		if (!System.Object.ReferenceEquals(InPoolMainObject, null))
		{
			UnityEngine.AssetBundle mainAssetBundle = InPoolMainObject as UnityEngine.AssetBundle;
			StartCoroutine(LoadAssetBundle(AssetName, CallbackAction, mainAssetBundle));

			//如果这个资产包没缓存，现在就缓存它 / If this asset bundle is not cached, cache it now!
			if (!AssetBundles.ContainsKey(bundleNameRoot))
				AssetBundles[bundleNameRoot] = new BundleData(mainAssetBundle);

			yield break;
		}

		/* 
		* 如果缓存和对象池都没命中，那就是第一次加载Asset Bundle
		* If neither the cache nor the object pool hit, it's the 1st time the Asset Bundle is loaded 
		*/

		// 依次加载栈中所有依赖 / Load each dependency in the stack
		while (bundleFilePathStack.Count > 0)
		{
			string currentAssetName = bundleFilePathStack.Pop();
			string currentBundleName = BundleInfos[currentAssetName].BundleName;
			string currentBundlePath = PathUtil.BuildResourcesPath + currentBundleName;
			List<string> DependencyFilePaths = BundleInfos[currentAssetName].DependenceFilePaths;

			// 获取缓存的 BundleData 并增加引用计数 / Get cached BundleData and increment reference
			BundleData currentBundleData = GetAssetBundleAndIncrementReference(currentBundleName);

			// 如果未在缓存中 / If not cached
			if (currentBundleData == null)
			{
				// 再次尝试从对象池获取 / Try to spawn from pool
				UnityEngine.Object InPoolObject = FrameworkManager.Pool.Spawn("AssetBundle", currentBundleName);
				if (InPoolObject is not null)
				{
					AssetBundle assetBundle = InPoolObject as AssetBundle;
					currentBundleData = new BundleData(assetBundle);
				}
				else
				{
					// 异步从文件加载 Bundle，并等待完成 / Load bundle from file asynchronously and wait
					AssetBundleCreateRequest bundleCreateRequest = AssetBundle.LoadFromFileAsync(currentBundlePath);
					yield return bundleCreateRequest;
					currentBundleData = new BundleData(bundleCreateRequest.assetBundle);
				}

				// Bundle添加到缓存 / Bundle add to cache
				AssetBundles[currentBundleName] = currentBundleData;
			}

			// 如果存在依赖项，则将其压入栈中继续加载 / If dependencies exist, push them onto the stack
			if (DependencyFilePaths is not null && DependencyFilePaths.Count > 0)
			{
				for (int dependencyIndex = 0; dependencyIndex < DependencyFilePaths.Count; dependencyIndex++)
				{
					bundleFilePathStack.Push(DependencyFilePaths[dependencyIndex]);
				}
			}
		}

		// 从缓存中取得根 BundleData，并退还给对象池，然后加载资产 / Retrieve root bundle, unspawn to pool, then load asset
		bundleData = AssetBundles[bundleNameRoot];
		FrameworkManager.Pool.Unspawn("AssetBundle", bundleNameRoot, bundleData.Bundle);
		StartCoroutine(LoadAssetBundle(AssetName, CallbackAction, bundleData.Bundle));
		// 保证第2次加载Assets Bundle是从对象池 / Ensures that the 2nd time load of the Assets Bundle is from the object pool.
		AssetBundles.Remove(bundleNameRoot);
	}

	/// <summary>
	/// 从已加载的 AssetBundle 中异步加载指定资源，并通过回调返回结果。
	/// Asynchronously loads the specified asset from a loaded AssetBundle and returns it via callback.
	/// </summary>
	/// <param name="AssetName">要加载的资源名称。/ Name of the asset to load.</param>
	/// <param name="CallbackAction">加载完成后的回调函数，可为空。/ Callback invoked with the loaded UnityEngine.Object, may be null.</param>
	/// <param name="assetBundle">已加载的 AssetBundle 实例。/ The loaded AssetBundle instance.</param>
	private IEnumerator LoadAssetBundle(string AssetName, Action<UnityEngine.Object> CallbackAction, AssetBundle assetBundle)
	{
		// 场景资产无法用此方法加载，会返回 null。/ Scenes cannot be loaded this way; they return null.
		// 请参阅 UnityEngine.AssetBundleCreateRequest 类 / See UnityEngine.AssetBundleCreateRequest
		if (AssetName.EndsWith(".unity"))
		{
			CallbackAction?.Invoke(null);
			yield break;
		}

		// 异步加载资源并等待完成 / Async load asset and wait
		AssetBundleRequest bundleRequest = assetBundle.LoadAssetAsync(AssetName);
		yield return bundleRequest;

		// 完成后执行回调 / Invoke callback upon completion
		if (!System.Object.ReferenceEquals(CallbackAction, null) && !System.Object.ReferenceEquals(bundleRequest, null))
		{
			CallbackAction.Invoke(bundleRequest.asset);
		}
	}

	/// <summary>
	/// 获取指定名称的 AssetBundle，并对其引用计数加一。
	/// Get the AssetBundle by name and increment its reference count.
	/// </summary>
	/// <param name="Name">
	/// 要获取的资源或 Bundle 名称。如果在 BundleInfos 中存在，则使用映射后的 BundleName，否则直接使用 Name。
	/// The asset or bundle identifier. If found in BundleInfos, its BundleName is used; otherwise the provided Name is used.
	/// </param>
	/// <returns>
	/// 如果缓存中存在对应的 BundleData，则返回该实例并已将其 ReferencedCount 加一；否则返回 null。
	/// Returns the BundleData instance with incremented ReferencedCount if present in cache; otherwise null.
	/// </returns>
	private BundleData GetAssetBundleAndIncrementReference(string Name)
	{
		// 尝试从 BundleInfos 获取实际的 bundle 名称 / Try to map to the real bundle name via BundleInfos
		string bundleName = "";
		if (BundleInfos.ContainsKey(Name))
			bundleName = BundleInfos[Name].BundleName;
		else
			bundleName = Name;

		// 从缓存中查找 BundleData / Look up BundleData in cache
		BundleData bundleData = null;
		if (AssetBundles.TryGetValue(bundleName, out bundleData))
		{
			// 引用计数加一 / Increment reference count
			bundleData.ReferencedCount++;
			return bundleData;
		}
		// 缓存中不存在，返回 null / Not found in cache, return null
		return null;
	}

	/// <summary>
	/// 对指定资源对应的 AssetBundle 及其所有依赖资源执行引用计数减一，并在计数归零时退还对象池并移除缓存。
	/// Decrement the reference counts for the specified AssetBundle and all its dependencies, unspawn and remove from cache when count reaches zero.
	/// </summary>
	/// <param name="AssetName">
	/// 要释放引用的资源名称，用于查找其对应的 BundleName 及依赖列表。
	/// The asset name whose bundle reference is to be released; used to look up its BundleName and dependency list.
	/// </param>
	public void DecrementAssetBundleReference(string AssetName)
	{
		// 使用栈结构深度优先遍历所有依赖 / Use a stack for DFS traversal of all dependencies
		Stack<string> bundleFilePathStack = new Stack<string>();
		bundleFilePathStack.Push(AssetName);

		while (bundleFilePathStack.Count > 0)
		{
			string currentAssetName = bundleFilePathStack.Pop();
			string bundleName = null;

			// 获取当前资源对应的 BundleName / Map current asset name to its bundle name
			if (BundleInfos.ContainsKey(currentAssetName))
				bundleName = BundleInfos[currentAssetName].BundleName;
			else
				continue;

			// 如果缓存中存在该 BundleData / If corresponding BundleData exists in cache
			if (AssetBundles.TryGetValue(bundleName, out BundleData bundleData))
			{
				if (bundleData.ReferencedCount > 0)
				{
					// 引用计数减一 / Decrement reference count
					bundleData.ReferencedCount--;
					Debug.LogFormat("AssetBundle name: {0} referenced count: {1}", bundleName, bundleData.ReferencedCount);
				}
				if (bundleData.ReferencedCount <= 0)
				{
					// 当引用计数归零时，退还对象池并移除缓存 / When count reaches zero, unspawn to pool and remove from cache
					Debug.LogFormat("Unspawn and return to object pool, AssetBundle name: {0}", bundleName);
					FrameworkManager.Pool.Unspawn("AssetBundle", bundleName, bundleData.Bundle);
					AssetBundles.Remove(bundleName);
				}
			}

			// 将所有依赖项压入栈，以便后续处理 / Push all dependencies onto stack for further processing
			List<string> dependenceFilePaths = BundleInfos[currentAssetName].DependenceFilePaths;
			if (dependenceFilePaths is not null && dependenceFilePaths.Count > 0)
			{
				for (int dependencyIndex = 0; dependencyIndex < dependenceFilePaths.Count; dependencyIndex++)
				{
					bundleFilePathStack.Push(dependenceFilePaths[dependencyIndex]);
				}
			}
		}
	}

#if UNITY_EDITOR
	void EditorLoadAsset(string AssetName, Action<UnityEngine.Object> CallbackAction = null)
	{
		UnityEngine.Object UnityObject = UnityEditor.AssetDatabase.LoadAssetAtPath(AssetName, typeof(UnityEngine.Object));
		if (System.Object.ReferenceEquals(UnityObject, null))
			Debug.LogError("Assets name is not exist:" + AssetName);
		else
			CallbackAction?.Invoke(UnityObject);
	}
#endif

	private IEnumerator LoadWithResourcesFallback(string AssetName, Action<UnityEngine.Object> CallbackAction)
	{
		ResourceRequest request = Resources.LoadAsync<UnityEngine.Object>(AssetName);
		yield return request;

		if (!System.Object.ReferenceEquals(request.asset, null))
		{
			CallbackAction?.Invoke(request.asset);
			yield break;
		}

		yield return LoadBundleAsync(AssetName, CallbackAction);
	}

	private void LoadAsset(string AssetName, Action<UnityEngine.Object> CallbackAction = null)
	{
#if UNITY_EDITOR
		if (FrameworkConstant.GDM == GameDeploymentMode.EditorMode)
		{
			EditorLoadAsset(AssetName, CallbackAction);
			return;
		}
#endif
		StartCoroutine(LoadWithResourcesFallback(AssetName, CallbackAction));
	}

	public void LoadUIPrefab(string AssetName, Action<UnityEngine.Object> CallbackAction = null)
	{
		LoadAsset(PathUtil.GetUIPrefabPath(AssetName), CallbackAction);
	}

	public void LoadMusic(string AssetName, Action<UnityEngine.Object> CallbackAction = null)
	{
		LoadAsset(PathUtil.GetMusicPath(AssetName), CallbackAction);
	}

	public void LoadSound(string AssetName, Action<UnityEngine.Object> CallbackAction = null)
	{
		LoadAsset(PathUtil.GetSoundPath(AssetName), CallbackAction);
	}

	public void LoadEffectPrefab(string AssetName, Action<UnityEngine.Object> CallbackAction = null)
	{
		LoadAsset(PathUtil.GetEffectPrefabPath(AssetName), CallbackAction);
	}

	public void LoadModelPrefab(string AssetName, Action<UnityEngine.Object> CallbackAction = null)
	{
		LoadAsset(PathUtil.GetModelPrefabPath(AssetName), CallbackAction);
	}
	public void LoadSprite(string AssetName, Action<UnityEngine.Object> CallbackAction = null)
	{
		LoadAsset(PathUtil.GetSpritePath(AssetName), CallbackAction);
	}

	public void LoadTexture(string AssetName, Action<UnityEngine.Object> CallbackAction = null)
	{
		LoadAsset(PathUtil.GetTexturePath(AssetName), CallbackAction);
	}

	public void LoadMaterial(string AssetName, Action<UnityEngine.Object> CallbackAction = null)
	{
		LoadAsset(PathUtil.GetMaterialPath(AssetName), CallbackAction);
	}

	public void LoadScene(string AssetName, Action<UnityEngine.Object> CallbackAction = null)
	{
		LoadAsset(PathUtil.GetScenePath(AssetName), CallbackAction);
	}

	public void LoadPhysicsMaterial(string AssetName, Action<UnityEngine.Object> CallbackAction = null)
	{
		LoadAsset(PathUtil.GetPhysicsMaterialPath(AssetName), CallbackAction);
	}

	/// <summary>
	/// Asynchronously loads a Lua script asset and invokes a callback upon completion.
	/// </summary>
	/// <remarks>
	/// <para>Do not use <c>PathUtil</c> for building the asset path here.</para>
	/// <para>Refer to <see cref="LuaManager.LuaFileNameList"/> for the list of valid script identifiers.</para>
	/// </remarks>
	/// <param name="luaScriptPath">
	/// The identifier or relative path of the Lua script to load (without extension),  
	/// e.g. "foo/bar.lua.bytes". 
	/// This value is passed directly to the asset loading pipeline.
	/// </param>
	/// <param name="callbackAction">
	/// Optional callback that receives the loaded <see cref="UnityEngine.Object"/> (typically a <c>TextAsset</c>)  
	/// once the script is available. If <c>null</c>, no callback is invoked.
	/// </param>
	public void LoadLuaScript(string LuaScriptPath, Action<UnityEngine.Object> CallbackAction = null)
	{
		LoadAsset(LuaScriptPath, CallbackAction);
	}

	public void LoadPrefab(string PrefabPath, Action<UnityEngine.Object> CallbackAction = null)
	{
		LoadAsset(PrefabPath, CallbackAction);
	}

	public void UnloadBundle(UnityEngine.Object @Object)
	{
		AssetBundle assetBundle = @Object as AssetBundle;
		assetBundle.Unload(true);
	}
}
