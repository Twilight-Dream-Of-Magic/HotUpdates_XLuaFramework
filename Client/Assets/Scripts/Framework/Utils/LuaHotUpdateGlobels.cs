using System.Collections.Generic;
using XLua;

/// <summary>
/// LuaHotUpdateGlobals 类
/// <para>设计模式：在我们的框架中，LuaBehaviour 会为每一个 *.bytes/Lua 文件都创建一张独立的 LuaTable 作为脚本环境，
/// 这样脚本内的所有读写操作都只作用于各自环境，不会污染或修改到全局表。</para>
/// <para>本类提供的接口则正好相反：
/// 它允许在热更新脚本中，按需注册、查询、删除、列举甚至清空全局表中的变量，从而实现全局模块的动态管理。
/// 对于可能引发范围性影响的操作（如清空全局表），会在控制台输出警告。</para>
/// </summary>
/// <remarks>
/// Pattern: LuaBehaviour enforces per-script isolated environments via separate LuaTable instances, 
/// so nothing ever writes back to the real _G by default.
/// This class exposes static methods to explicitly manipulate the real global table (LuaEnv.Global) at runtime—
/// ideal for hot‑update modules needing dynamic global management.
/// </remarks>
/// <see cref="LuaBehaviour"/>
[LuaCallCSharp]
public static class LuaGlobalsHotUpdate
{
	// 缓存 LuaManager 实例，避免每次都访问 FrameworkManager.Lua
	// Cache LuaManager instance to avoid repeated access
	private static LuaManager _luaManager;
	private static LuaManager LuaManagerInstance
	{
		get
		{
			if (System.Object.ReferenceEquals(_luaManager, null))
			{
				_luaManager = FrameworkManager.Lua;
				if (System.Object.ReferenceEquals(_luaManager, null))
				{
					LuaLogging.Error("[LuaHotUpdateGlobals] FrameworkManager.Lua (LuaManager) 实例为 null！");
				}
			}
			return _luaManager;
		}
	}

	/// <summary>
	/// 将一个模块对象注册到全局表<br/>
	/// Register a module object to the global table
	/// </summary>
	/// <param name="name">在全局表中使用的变量名 / Variable name in global table</param>
	/// <param name="module">Lua 侧模块实例（通常是 require(...) 返回的对象）/ Lua-side module instance (usually returned by require(...))</param>
	public static void RegisterGlobal(string name, object module, bool is_force)
	{
		var Environment = LuaManagerInstance?.LuaEnvironmentParser;
		if (Environment == null) 
			return;
		if (Environment.Global.Get<object>(name) != null )
		{
			if (!is_force)
				return;
			LuaLogging.Warn($"[LuaHotUpdateGlobals] 全局变量 '{name}' 已存在，将被覆盖。/ Global variable '{name}' already exists and will be overwritten.");
		}
		Environment.Global.Set<string, object>(name, module);
	}

	/// <summary>
	/// 从全局表中移除一个变量（等同于将其设置为 nil）<br/>
	/// Remove a variable from the global table (equivalent to setting it to nil)
	/// </summary>
	/// <param name="name">要从全局表删除的变量名 / Variable name to remove from global table</param>
	public static void UnregisterGlobal(string name)
	{
		var Environment = LuaManagerInstance?.LuaEnvironmentParser;
		if (Environment == null) 
			return;
		if (Environment.Global.Get<object>(name) == null)
		{
			LuaLogging.Warn($"[LuaHotUpdateGlobals] 尝试移除不存在的全局变量 '{name}'。/ Attempting to remove non-existent global variable '{name}'.");
			return;
		}
		Environment.Global.Set<string, object>(name, null);
	}

	/// <summary>
	/// 检查全局表中是否存在指定变量<br/>
	/// Check if a variable exists in the global table
	/// </summary>
	/// <param name="name">全局变量名 / Global variable name</param>
	/// <returns>存在返回 true，否则 false / Returns true if exists, otherwise false</returns>
	public static bool HasGlobal(string name)
	{
		var Environment = LuaManagerInstance?.LuaEnvironmentParser;
		if (Environment == null) 
			return false;
		return Environment.Global.Get<object>(name) != null;
	}

	/// <summary>
	/// 获取全局表中指定变量的值
	/// Get the value of a variable in the global table
	/// </summary>
	/// <param name="name">全局变量名 / Global variable name</param>
	/// <returns>返回对象或 null / Returns object or null</returns>
	public static object GetGlobal(string name)
	{
		var Environment = LuaManagerInstance?.LuaEnvironmentParser;
		if (Environment == null) 
			return null;
		return Environment.Global.Get<object>(name);
	}

	/// <summary>
	/// 列举所有当前全局表中的键<br/>
	/// List all keys in the current global table
	/// </summary>
	/// <returns>返回全局变量名列表 / Returns list of global variable names</returns>
	public static List<string> ListGlobals()
	{
		var Environment = LuaManagerInstance?.LuaEnvironmentParser;
		var result = new List<string>();
		if (Environment == null) return result;
		Environment.Global.ForEach<string, object>((k, v) => { result.Add(k); });
		return result;
	}

	/// <summary>
	/// 清空全局表中所有变量（危险操作）<br/>
	/// Clear all variables in the global table (dangerous operation)
	/// </summary>
	public static void ClearAllGlobals()
	{
		var Environment = LuaManagerInstance?.LuaEnvironmentParser;
		if (Environment == null) 
			return;
		LuaLogging.Warn("[LuaHotUpdateGlobals] 警告：即将清空全局表中的所有变量！/ Warning: About to clear all variables in the global table!");
		var keys = new List<string>();
		Environment.Global.ForEach<string, object>((k, v) => { keys.Add(k); });
		foreach (var key in keys)
		{
			Environment.Global.Set<string, object>(key, null);
		}
	}

	/// <summary>
	/// 批量注册模块到全局表<br/>
	/// Register multiple modules to the global table
	/// </summary>
	/// <param name="modules">模块字典，键为变量名，值为模块实例 / Dictionary of modules, key is variable name, value is module instance</param>
	public static void RegisterGlobals(Dictionary<string, object> modules, bool is_force)
	{
		foreach (var kv in modules)
		{
			RegisterGlobal(kv.Key, kv.Value, is_force);
		}
	}

	/// <summary>
	/// 批量移除全局表中的变量<br/>
	/// Remove multiple variables from the global table
	/// </summary>
	/// <param name="names">要移除的全局变量名列表 / List of global variable names to remove</param>
	public static void UnregisterGlobals(IEnumerable<string> names)
	{
		foreach (var name in names)
		{
			UnregisterGlobal(name);
		}
	}
}
