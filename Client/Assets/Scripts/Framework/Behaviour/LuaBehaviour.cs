using System;
using System.Collections.Generic;
using UnityEngine;
using XLua;

[System.Serializable]
public class LuaInjection
{
	public string name;
	public GameObject value;
}

[LuaCallCSharp]
public class LuaBehaviour : MonoBehaviour
{
	//All LuaBehaviour shared one LuaEnvironment only!
	private LuaManager LuaManager = FrameworkManager.Lua;
	private LuaEnv LuaEnvironment = FrameworkManager.Lua.LuaEnvironmentParser;
	//Setting up separate Lua table environments for each Lua script can somewhat prevent conflicts between global variables and function names of Lua scripts.
	protected LuaTable LuaScriptEnvironment;

	//Lua lifecycle callback function from the Unity C# lifecycle.
	[CSharpCallLua]
	private Action LuaScriptOnAwake;
	[CSharpCallLua]
	private Action LuaScriptOnEnable;
	[CSharpCallLua]
	private Action LuaScriptStart;
	[CSharpCallLua]
	private Action LuaScriptFixedUpdate;
	[CSharpCallLua]
	private Action LuaScriptUpdate;
	[CSharpCallLua]
	private Action LuaScriptLateUpdate;
	[CSharpCallLua]
	private Action LuaScriptOnDisable;
	[CSharpCallLua]
	protected Action LuaScriptOnDestroy;
	[CSharpCallLua]
	protected Action LuaScriptOnApplicationQuit;

	public List<LuaInjection> Injections = new List<LuaInjection>();

	//Whether LuaScipt is bind or not

	private bool IsBindedLua = false;

	/// <summary>
	/// Indicates whether this Lua behaviour is bound to a Lua script.
	/// 表示这个Lua行为是否绑定了Lua脚本。
	/// </summary>
	public bool IsBindedLuaScript
	{
		get
		{
			return this.IsBindedLua;
		}
		internal set
		{
			this.IsBindedLua = value;
		}
	}

	private bool IsDisposedValue = false;

	/// <summary>
	/// Gets or sets whether the Lua environment is disposed.
	/// If the Lua environment is alive or destroyed, it will follow the state of the Lua environment.
	/// Otherwise, it will depend on the lifecycle of this instance.
	/// 如果Lua环境存活或者销毁，就以该环境为准；
	/// 否则就按自身生命周期的依赖标记为准。
	/// </summary>
	public bool IsDisposed
	{
		get
		{
			// If the Lua environment is disposed, return true.
			if (LuaManager.IsLuaEnvironmentDisposed())
				return true;

			// Otherwise, return the local disposed state.
			return IsDisposedValue;
		}
		internal set
		{
			// If the Lua environment is not disposed, ensure the local disposed state is false.
			if (!LuaManager.IsLuaEnvironmentDisposed())
			{
				IsDisposedValue = false;
				return;
			}

			// Otherwise, set the local disposed state to the provided value.
			IsDisposedValue = value;
		}
	}

	/// <summary>
	/// 当外部（或者在 Inspector 里通过 UnityEvent）调用 Initialize 时，会先把 Injections 注入，绑定并调用 Lua 的 Awake/OnEnable/Start…
	/// 由 <see cref="MainLuaBehaviour"/> 组件 的 Unity生命周期函数 Awake 调用
	/// </summary>
	public virtual void Initialize(string LuaFilePath)
	{
		//If the instance is not disposed of (i.e., not destructed) and is binded to a Lua script, then it has been initialized.
		//如果这个实例没有弃置(也就是没有销毁)，并且绑定了Lua脚本，那就说明已经初始化过了。
		if (!this.IsDisposedValue && this.IsBindedLua)
		{
			return;
		}

		if (System.Object.ReferenceEquals(this.LuaScriptEnvironment, null))
		{
			this.LuaScriptEnvironment = LuaEnvironment.NewTable();
		}

		LuaTable meta = LuaEnvironment.NewTable();
		meta.Set("__index", LuaEnvironment.Global);
		this.LuaScriptEnvironment.SetMetaTable(meta);
		meta.Dispose();

		this.LuaScriptEnvironment.Set("self", this);

		foreach (var injection in Injections)
		{
			this.LuaScriptEnvironment.Set(injection.name, injection.value);
		}

		if (!string.IsNullOrEmpty(LuaFilePath))
		{
			try
			{
				this.LuaEnvironment.DoString(FrameworkManager.Lua.LuaLibraryFileCustomLoader(LuaFilePath), LuaFilePath, LuaScriptEnvironment);
				this.IsBindedLua = true;
			}
			catch (Exception ex)
			{
				Debug.LogErrorFormat("Lua script load error: {0}", ex);
				return;
			}
		}

		this.LuaScriptEnvironment.Get("OnAwake", out this.LuaScriptOnAwake);
		this.LuaScriptEnvironment.Get("OnEnable", out this.LuaScriptOnEnable);
		this.LuaScriptEnvironment.Get("Start", out this.LuaScriptStart);
		this.LuaScriptEnvironment.Get("FixedUpdate", out this.LuaScriptFixedUpdate);
		this.LuaScriptEnvironment.Get("Update", out this.LuaScriptUpdate);
		this.LuaScriptEnvironment.Get("LateUpdate", out this.LuaScriptLateUpdate);
		this.LuaScriptEnvironment.Get("OnDisable", out this.LuaScriptOnDisable);
		this.LuaScriptEnvironment.Get("OnDestroy", out this.LuaScriptOnDestroy);
		this.LuaScriptEnvironment.Get("OnApplicationQuit", out this.LuaScriptOnApplicationQuit);

		this.IsDisposedValue = false;

		//Lua OnAwake
		this.LuaScriptOnAwake?.Invoke();
	}

	/// <summary>
	/// 当需要卸载脚本、释放资源时调用 Uninitialize，会清除所有 Lua 回调并 Dispose 环境表。
	/// 由 <see cref="MainLuaBehaviour"/> 组件 的 Unity生命周期函数 OnApplicationQuit 调用
	/// </summary>
	public virtual void Uninitialize()
	{
		//If this instance disposed (i.e., has destructed) and unbinds the Lua script, then it has been uninitialized.
		//如果这个实例弃置(也就是有销毁)，并且解绑了Lua脚本，那就说明已经反初始化过了。
		if (this.IsDisposedValue && !this.IsBindedLua)
		{
			return;
		}

		//Lua OnApplicationQuit
		this.LuaScriptOnApplicationQuit?.Invoke();

		this.LuaScriptOnAwake = null;
		this.LuaScriptOnEnable = null;
		this.LuaScriptStart = null;
		this.LuaScriptFixedUpdate = null;
		this.LuaScriptUpdate = null;
		this.LuaScriptLateUpdate = null;
		this.LuaScriptOnDisable = null;
		this.LuaScriptOnDestroy = null;
		this.LuaScriptOnApplicationQuit = null;

		this.IsBindedLua = false;

		if (!System.Object.ReferenceEquals(this.LuaScriptEnvironment, null))
		{
			this.LuaScriptEnvironment?.Dispose(false);
			this.LuaScriptEnvironment = null;
			this.IsDisposedValue = true;
		}
	}

	private void OnEnable()
	{
		if (IsDisposed)
			return;

		this.LuaScriptOnEnable?.Invoke();
	}

	private void Start()
	{
		this.LuaScriptStart?.Invoke();
	}

	private void FixedUpdate()
	{
		this.LuaScriptFixedUpdate?.Invoke();
	}

	private void Update()
	{
		this.LuaScriptUpdate?.Invoke();
	}

	private void LateUpdate()
	{
		this.LuaScriptLateUpdate?.Invoke();
	}

	private void OnDisable()
	{
		if (IsDisposed)
			return;

		this.LuaScriptOnDisable?.Invoke();
	}

	private void OnDestroy()
	{
		if (IsDisposed)
			return;

		this.LuaScriptOnDestroy?.Invoke();

		Uninitialize();
		this.IsDisposed = true;
	}
}
