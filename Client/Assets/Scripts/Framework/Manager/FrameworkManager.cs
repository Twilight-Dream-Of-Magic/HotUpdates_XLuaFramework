using UnityEngine;

public class FrameworkManager : MonoBehaviour
{
	private static ResourceManager _resource;
	public static ResourceManager Resource
	{
		get
		{
			return _resource;
		}
	}

	private static LuaManager _lua;
	public static LuaManager Lua
	{
		get
		{
			return _lua;
		}
	}

	private static UI_Manager _ui;
	public static UI_Manager UI
	{
		get
		{
			return _ui;
		}
	}

	private static EntityManager _entity;
	public static EntityManager Entity
	{
		get
		{
			return _entity;
		}
	}

	private static CustomSceneManager _scene;
	public static CustomSceneManager Scene
	{
		get
		{
			return _scene;
		}
	}

	private static EventManager _event;
	public static EventManager Event
	{
		get
		{
			return _event;
		}
	}

	private static PoolManager _pool;
	public static PoolManager Pool
	{
		get
		{
			return _pool;
		}
	}

	private static UISpecializedPoolManager _uipool;

	public static UISpecializedPoolManager UIPool
	{
		get
		{
			return _uipool;
		}
	}

	private static SoundManager _sound;
	public static SoundManager Sound
	{
		get
		{
			return _sound;
		}
	}

	private static NetManager _net;
	public static NetManager Net
	{
		get
		{
			return _net;
		}
	}


	private void Awake()
	{
		_resource = this.gameObject.AddComponent<ResourceManager>();
		_lua = this.gameObject.AddComponent<LuaManager>();
		_ui = this.gameObject.AddComponent<UI_Manager>();
		_entity = this.gameObject.AddComponent<EntityManager>();
		_scene = this.gameObject.AddComponent<CustomSceneManager>();
		_event = this.gameObject.AddComponent<EventManager>();
		_pool = this.gameObject.AddComponent<PoolManager>();
		_uipool = this.gameObject.AddComponent<UISpecializedPoolManager>();
		_sound = this.gameObject.AddComponent<SoundManager>();
		_net = this.gameObject.AddComponent<NetManager>();
	}

	private void OnApplicationQuit()
	{
		_event.Unsubscribe((int)GameEvent.LuaScriptsInitialize);
	}

	private void OnDestroy()
	{
		_event.Unsubscribe((int)GameEvent.LuaScriptsInitialize);
	}
}
