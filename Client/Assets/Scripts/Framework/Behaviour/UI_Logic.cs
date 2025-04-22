using System;

/// <summary>
/// UI logic component in the framework, responsible for binding and invoking Lua open/close events on top of LuaBehaviour.
/// 界面逻辑组件，继承自 LuaBehaviour，负责绑定并触发 Lua 中的打开/关闭 回调。
/// </summary>
public class UI_Logic : LuaBehaviour
{
	/// <summary>
	/// The asset name identifier for resource pooling operations.
	/// 资源池中对应的资源标识，用于回池操作时传入。
	/// </summary>
	public string AssetName;

	private Action LuaScriptOnOpen;
	private Action LuaScriptOnClose;
	private bool IsOpened = false;

	/// <summary>
	/// Initializes the LuaBehaviour and binds the OnOpen/OnClose callbacks from Lua.<br/>
	/// 初始化 LuaBehaviour 并绑定 Lua 中的 OnOpen/OnClose 回调。
	/// </summary>
	/// <param name="LuaFilePath">
	/// The Lua script path.
	/// Lua 脚本路径。
	/// </param>
	/// <remarks>
	/// See <see cref="UI_Manager.OpenUI(string, string, string)"/> for how this initialization is called.<br/>
	/// 请参见 <see cref="UI_Manager.OpenUI(string, string, string)"/> 方法如何调用此初始化。
	/// </remarks>
	public override void Initialize(string LuaFilePath)
	{
		base.Initialize(LuaFilePath);
		this.LuaScriptEnvironment.Get("OnOpenUI", out this.LuaScriptOnOpen);
		this.LuaScriptEnvironment.Get("OnCloseUI", out this.LuaScriptOnClose);
		IsOpened = false;
	}

	/// <summary>
	/// Invokes the Lua OnOpen callback and marks as opened.<br/>
	/// 触发 Lua 中的 OnOpen 回调，并标记为已打开状态。
	/// </summary>
	public void OnOpen()
	{
		if (IsOpened)
			return;
		LuaScriptOnOpen?.Invoke();
		IsOpened = true;
	}

	/// <summary>
	/// Invokes the Lua OnClose callback.<br/>
	/// 触发 Lua 中的 OnClose 回调。
	/// </summary>
	/// <remarks>
	/// The actual GameObject unspawn (return to pool) is handled in <see cref="UI_Manager.CloseUI(string)"/> or
	/// <see cref="UI_Manager.DeleteUI(string)"/>, which performs the resource pooling operations.<br/>
	/// 实际的对象回池由 <see cref="UI_Manager.CloseUI(string)"/> 或 <see cref="UI_Manager.DeleteUI(string)"/> 方法完成。
	/// </remarks>
	public void OnClose()
	{
		if (!IsOpened)
			return;
		LuaScriptOnClose?.Invoke();
	}

	/// <summary>
	/// Uninitializes and releases all Lua callbacks and environment resources.<br/>
	/// 反初始化并释放所有 Lua 回调及环境资源。
	/// </summary>
	/// <remarks>
	/// See <see cref="UI_Manager.DeleteUI(string)"/> for how this uninitialization is triggered.<br/>
	/// 请参见 <see cref="UI_Manager.DeleteUI(string)"/> 方法如何触发此反初始化。
	/// </remarks>
	public override void Uninitialize()
	{
		this.LuaScriptOnOpen = null;
		this.LuaScriptOnClose = null;
		this.IsOpened = false;
		base.Uninitialize();
	}
}