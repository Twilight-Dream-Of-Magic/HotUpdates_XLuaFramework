using System.Collections.Generic;
using UnityEngine;

public class EntityManager : MonoBehaviour
{
	private Dictionary<string, GameObject> EntityCaches = new Dictionary<string, GameObject>();

	[SerializeField]
	private Transform EntitiesRootParent;
	private Dictionary<string, Transform> EntityLevelGroups = new Dictionary<string, Transform>();

	private void Awake()
	{
		EntitiesRootParent = this.transform.parent.Find("RootEntities");
	}

	private void OnApplicationQuit()
	{
		EntitiesRootParent = null;
	}

	/*
		Calling from lua scripts
		从 lua 脚本调用
	*/

	public void SetEntityLevelGroups(List<string> LevelGroupNames)
	{
		for (int i = 0; i < LevelGroupNames.Count; i++)
		{
			GameObject game_objects = new GameObject("LevelGroupName-" + LevelGroupNames[i]);
			game_objects.transform.SetParent(EntitiesRootParent, false);
			if (!EntityLevelGroups.ContainsKey(LevelGroupNames[i]))
				EntityLevelGroups.Add(LevelGroupNames[i], game_objects.transform);
		}
	}

	public Transform GetEntityLevelGroups(string LevelGroupName)
	{
		if (!EntityLevelGroups.ContainsKey(LevelGroupName))
			Debug.LogErrorFormat("This level group name: " + LevelGroupName + " is not exist!");
		return EntityLevelGroups[LevelGroupName];
	}

	//Create or show entity
	//创建或显示实体
	public void ShowEntity(string EntityName, string LevelGroupName, string LuaFilePath)
	{
		GameObject ui = null;
		if (EntityCaches.TryGetValue(EntityName, out ui))
		{
			EntityLogic ui_logic = ui.GetComponent<EntityLogic>();
			ui_logic.OnShow();
			return;
		}

		FrameworkManager.Resource.LoadPrefab
		(
			LuaFilePath,
			(UnityEngine.Object obj) =>
			{
				GameObject entity = Instantiate(obj) as GameObject;
				EntityCaches.Add(EntityName, entity);

				Transform parent = GetEntityLevelGroups(LevelGroupName);
				entity.transform.SetParent(parent, false);

				EntityLogic entity_logic = entity.AddComponent<EntityLogic>();
				entity_logic.Initialize(LuaFilePath);
				entity_logic.OnShow();
			}
		);
	}

	//Hide entity
	//隐藏实体
	public void HideEntity(string EntityName)
	{
		GameObject entity = null;
		if (EntityCaches.TryGetValue(EntityName, out entity))
		{
			EntityLogic entity_logic = entity.GetComponent<EntityLogic>();
			entity_logic.OnHide();
			return;
		}
	}

	//Delete entity
	//删除实体
	public void DeleteEntity(string EntityName)
	{
		GameObject entity = null;
		if (EntityCaches.TryGetValue(EntityName, out entity))
		{
			EntityLogic entity_logic = entity.GetComponent<EntityLogic>();
			entity_logic.OnHide();
			entity_logic.Uninitialize();

			EntityCaches.Remove(EntityName);
			Destroy(entity);

			return;
		}
	}
}
