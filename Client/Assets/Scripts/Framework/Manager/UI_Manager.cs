using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages UI lifecycle: creation, opening, closing, and deletion of UI elements.<br/>
/// 管理用户界面生命周期：创建、打开、关闭和删除 UI 元素。
/// </summary>
public class UI_Manager : MonoBehaviour
{
	/// <summary>
	/// Root parent transform under which all UI levels are organized.<br/>
	/// 根父物体，用于组织所有 UI 层级。
	/// </summary>
	[SerializeField]
	private Transform UI_RootParent;

	/// <summary>
	/// Tracks currently cached UI GameObjects by their asset path.<br/>
	/// 跟踪缓存的 UI 对象，键为资源路径。
	/// </summary>
	private Dictionary<string, GameObject> CachedUI = new Dictionary<string, GameObject>();

	/// <summary>
	/// Maps UI level names to their corresponding parent Transforms.<br/>
	/// 将 UI 层级名映射到它们对应的父 Transform。
	/// </summary>
	private Dictionary<string, Transform> UI_LevelGroups = new Dictionary<string, Transform>();

	// 记录每种 UI 的打开次数
	private Dictionary<string, int> OpenedUICounts = new Dictionary<string, int>();
	// 打开次数超过该阈值后，走池化逻辑  
	// If open-count > threshold, use pool logic
	private const int POOL_THRESHOLD = 3;

	/// <summary>
	/// Called when the script instance is being loaded.<br/>
	/// Awake 时初始化 UI_RootParent，查找场景中的 "UI_RootCanvas"。
	/// </summary>
	private void Awake()
	{
		UI_RootParent = this.transform.parent.Find("UI_RootCanvas");
	}

	/// <summary>
	/// Called when the MonoBehaviour will be OnApplicationQuit.<br/>
	/// OnApplicationQuit 时清理 UI_LevelGroups 缓存。
	/// </summary>
	private void OnApplicationQuit()
	{
		UI_LevelGroups.Clear();
		CachedUI.Clear();
		OpenedUICounts.Clear();
		UI_RootParent = null;
	}

	/*
		Calling from Lua scripts<br/>
		由 Lua 脚本调用
	*/

	/// <summary>
	/// Sets up UI level parent Transforms based on provided names.<br/>
	/// 根据提供的层级名称列表，创建并设置对应的父 Transform。
	/// </summary>
	/// <param name="LevelGroupNames">
	/// A list of UI level group identifiers.<br/>
	/// UI 层级组标识列表。
	/// </param>
	public void SetUI_LevelGroups(List<string> LevelGroupNames)
	{
		for (int i = 0; i < LevelGroupNames.Count; i++)
		{
			GameObject groupObj = new GameObject("LevelGroup-" + LevelGroupNames[i]);
			groupObj.transform.SetParent(UI_RootParent, false);
			if (!UI_LevelGroups.ContainsKey(LevelGroupNames[i]))
				UI_LevelGroups.Add(LevelGroupNames[i], groupObj.transform);
		}
	}

	/// <summary>
	/// Retrieves the Transform for a given UI level group name.<br/>
	/// 根据层级名称获取对应的 Transform。
	/// </summary>
	/// <param name="LevelGroupName">
	/// The UI level group identifier.<br/>
	/// UI 层级组标识。
	/// </param>
	/// <returns>
	/// The Transform corresponding to the level group.<br/>
	/// 返回该层级组的 Transform。
	/// </returns>
	public Transform GetUI_LevelGroups(string LevelGroupName)
	{
		if (!UI_LevelGroups.ContainsKey(LevelGroupName))
			Debug.LogErrorFormat("Level group not found: {0}", LevelGroupName);
		return UI_LevelGroups[LevelGroupName];
	}

	/// <summary>
	/// Opens or creates a UI element, initializes its logic, and parents it under the correct group.<br/>
	/// 打开或创建 UI 界面，初始化其逻辑组件，并设置父级。
	/// </summary>
	/// <param name="UI_Name">
	/// The name of the UI prefab.<br/>
	/// UI 预制体名称。
	/// </param>
	/// <param name="LevelGroupName">
	/// The level group under which to organize the UI.<br/>
	/// UI 所属层级组名称。
	/// </param>
	/// <param name="LuaFilePath">
	/// The Lua script path for the UI logic.<br/>
	/// UI 逻辑对应的 Lua 脚本路径。
	/// </param>
	public void OpenUI(string UI_Name, string LevelGroupName, string LuaFilePath)
	{
		string ui_path = PathUtil.GetUIPrefabPath(UI_Name);
		// —— 1. 更新打开次数 —— Update open-count
		OpenedUICounts.TryGetValue(ui_path, out int reference_count);
		reference_count++;
		OpenedUICounts[ui_path] = reference_count;

		// —— 2. 获取父节点 —— Get parent transform
		Transform parent = GetUI_LevelGroups(LevelGroupName);

		GameObject ui = null;
		UI_Logic ui_logic = null;
		RectTransform rect_transform = null;

		// —— 3. 小于等于阈值，优先使用字典缓存 —— Dictionary cache path
		if (reference_count <= POOL_THRESHOLD)
		{
			if (!CachedUI.ContainsKey(ui_path))
			{
				Debug.LogWarningFormat("OpenUI: no cached UI for key {0}", ui_path);
			}
			else
			{
				ui = CachedUI[ui_path];
				if (!System.Object.ReferenceEquals(ui, null))
				{
					ui.SetActive(true);
					rect_transform = ui.GetComponent<RectTransform>();
					rect_transform.localPosition = Vector3.zero;
					ui_logic = ui.GetComponent<UI_Logic>() ?? ui.AddComponent<UI_Logic>();
					ui_logic.AssetName = ui_path;
					ui_logic.OnOpen();
					return;
				}
			}
		}
		// —— 4. 超过阈值，尝试从专用池取 —— Pool path
		if (reference_count > POOL_THRESHOLD)
		{
			ui = FrameworkManager.UIPool.Spawn(ui_path);
			if (!System.Object.ReferenceEquals(ui, null))
			{
				// 激活并初始化池中实例  
				ui.transform.SetParent(parent, false);

				// 设置绘制位置
				rect_transform = ui.GetComponent<RectTransform>();
				rect_transform.localPosition = Vector3.zero;

				// 添加并初始化 UI_Logic  
				ui_logic = ui.GetComponent<UI_Logic>() ?? ui.AddComponent<UI_Logic>();
				ui_logic.AssetName = ui_path;
				ui_logic.OnOpen();
				CachedUI[ui_path] = ui;
				return;
			}
		}

		// —— 5. 缓存/池都未命中，则异步加载并实例化 —— Async load & instantiate
		FrameworkManager.Resource.LoadUIPrefab
		(
			UI_Name, (prefab) =>
			{
				ui = Instantiate(prefab) as GameObject;

				// 设置父节点  
				ui.transform.SetParent(parent, false);
				ui.SetActive(true);

				// 设置绘制位置
				rect_transform = ui.GetComponent<RectTransform>();
				rect_transform.localPosition = Vector3.zero;

				// 添加并初始化 UI_Logic  
				ui_logic = ui.AddComponent<UI_Logic>();
				ui_logic.AssetName = ui_path;
				ui_logic.Initialize(LuaFilePath);
				ui_logic.OnOpen();

				// 缓存到字典或记录到池  
				CachedUI[ui_path] = ui;
			}
		);
	}

	/// <summary>
	/// Gets a previously opened UI GameObject by name.<br/>
	/// 根据名称获取已打开的 UI 对象。
	/// </summary>
	/// <param name="UI_Name">
	/// The name of the UI prefab.<br/>
	/// UI 预制体名称。
	/// </param>
	/// <returns>
	/// The opened GameObject, or null if not found.<br/>
	/// 已打开的对象，若未找到返回 null。
	/// </returns>
	public GameObject GetOpenedUI(string UI_Name)
	{
		string ui_path = PathUtil.GetUIPrefabPath(UI_Name);
		CachedUI.TryGetValue(ui_path, out GameObject ui);
		return ui;
	}

	/// <summary>
	/// Closes a UI by invoking its OnClose logic and returning it to the pool.<br/>
	/// 关闭 UI，触发其 OnClose 逻辑并将其返回资源池。
	/// </summary>
	/// <param name="UI_Name">
	/// The name of the UI prefab to close.<br/>
	/// 要关闭的 UI 预制体名称。
	/// </param>
	public void CloseUI(string UI_Name)
	{
		GameObject ui = null;
		UI_Logic ui_logic = null;
		string ui_path = PathUtil.GetUIPrefabPath(UI_Name);

		// 1. 如果根本没有缓存，直接报错并返回
		if (!CachedUI.ContainsKey(ui_path))
		{
			Debug.LogErrorFormat("CloseUI: no cached UI for key {0}", ui_path);
			return;
		}

		// 2. 取出缓存，如果它被外部销毁，也直接返回
		// ui must not is null
		ui = CachedUI[ui_path];
		if (System.Object.ReferenceEquals(ui, null))
			return;

		// 3. 调用 OnClose 并清空 AssetName
		ui_logic = ui.GetComponent<UI_Logic>();
		ui_logic.OnClose();
		ui_logic.AssetName = string.Empty;

		// 4. 根据打开次数决定返池还是隐藏
		OpenedUICounts.TryGetValue(ui_path, out int reference_count);
		if (reference_count > POOL_THRESHOLD)
		{
			FrameworkManager.UIPool.Unspawn(ui_path, ui);
		}
		else
		{
			ui.SetActive(false);
		}
	}

	/// <summary>
	/// Deletes a UI by invoking its OnClose logic, uninitializing its UI_Logic, and then destroying or clearing it from the pool.<br/>
	/// 删除 UI：触发其 OnClose 逻辑，反初始化 UI_Logic，然后根据打开次数销毁或清理池中缓存。
	/// </summary>
	/// <param name="UI_Name">
	/// The name of the UI prefab to delete.<br/>
	/// 要删除的 UI 预制体名称。
	/// </param>
	public void DeleteUI(string UI_Name)
	{
		// 1. 准备工作
		GameObject ui = null;
		UI_Logic ui_logic = null;
		string ui_path = PathUtil.GetUIPrefabPath(UI_Name);

		// 2. 检查缓存
		if (!CachedUI.ContainsKey(ui_path))
		{
			Debug.LogWarningFormat("DeleteUI: no cached UI for key {0}", ui_path);
			return;
		}

		// 3. 获取实例
		ui = CachedUI[ui_path];
		if (System.Object.ReferenceEquals(ui, null))
			return;

		// 4. 调用 OnClose 及 Uninitialize
		ui_logic = ui.GetComponent<UI_Logic>();
		ui_logic.OnClose();
		ui_logic.Uninitialize();

		// 5. 根据打开次数决定清理方式
		OpenedUICounts.TryGetValue(ui_path, out int reference_count);
		if (reference_count > POOL_THRESHOLD)
		{
			// 高频 UI：清理专用池中所有此类型的缓存
			FrameworkManager.UIPool.RemovePool(ui_path);
		}
		else
		{
			// 低频 UI：直接销毁实例
			GameObject.Destroy(ui);
		}

		// 6. 清理记录
		CachedUI.Remove(ui_path);
		OpenedUICounts.Remove(ui_path);
	}
}
