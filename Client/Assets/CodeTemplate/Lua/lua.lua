--==================================================
-- #FILE_NAME#
-- xLuaCore LuaBehaviour 默认脚本模板 / Default xLuaCore LuaBehaviour Script Template
--==================================================

--[[
	描述 / Description:
	本模板适用于挂载在 LuaBehaviour 上的脚本。所有生命周期回调函数
	(Awake, Start, Update 等) 均直接定义在全局环境中，由 C# 侧自动
	绑定并调用，无需 return 模块。
	
	This template is for scripts attached to LuaBehaviour. All lifecycle
	callbacks (Awake, Start, Update, etc.) are defined in the global
	environment and will be auto-bound and invoked from C# side. No return
	of module table is needed.
]]

-- Lua模块引入示例
-- local moduleName = require("ModuleName")

-- —— 类名声明 / Class Name Declaration —— 
local CLASS_NAME = "#CLASS_NAME#"	  -- 脚本类名（与文件名保持一致）/ Script class name (matches file name)

-- —— XLua 注入的外部引用 / XLua Injected References —— 
-- 在 LuaBehaviour 中，C# 会将 Injections 列表中的 GameObject 等对象
-- 注入到脚本环境中，直接作为全局变量使用：
-- e.g. print(CLASS_NAME, "Injected:", SomeGO.name)
-- In LuaBehaviour, C# injects objects from Injections list into the script
-- environment as globals:
-- e.g. print(CLASS_NAME, "Injected:", SomeGO.name)

-- 拿到我们在 C# 里暴露的静态全局管理类
local LuaGHU = CS.LuaGlobalsHotUpdate

local PathUtil = CS.PathUtil
local Vector3 = CS.UnityEngine.Vector3
local Input = CS.UnityEngine.Input
local KeyCode = CS.UnityEngine.KeyCode
local Time = CS.UnityEngine.Time
local UnityEngine = CS.UnityEngine
local GameObject  = UnityEngine.GameObject

--==================================================
-- 生命周期回调 / Lifecycle Callbacks
--==================================================

--- Awake 回调 / Called on Awake
function OnAwake()
	print(CLASS_NAME, "[Awake] called")
	-- TODO: 在此编写 Awake 时的初始化逻辑
	-- TODO: Write initialization logic here
end

--- OnEnable 回调 / Called on OnEnable
function OnEnable()
	print(CLASS_NAME, "[OnEnable] called")
	-- TODO: 在此处理启用时的逻辑
	-- TODO: Handle logic when enabled
end

--- Start 回调 / Called on Start
function Start()
	print(CLASS_NAME, "[Start] called")
	-- TODO: 在此编写 Start 时的逻辑
	-- TODO: Write logic to run on Start
end

--- FixedUpdate 回调 / Called on FixedUpdate (physics)
function FixedUpdate()
	-- TODO: 在此编写物理更新逻辑
	-- TODO: Physics update logic here
end

--- Update 回调 / Called on Update (per frame)
function Update()
	-- TODO: 在此编写每帧更新逻辑
	-- TODO: Frame update logic here
end

--- LateUpdate 回调 / Called on LateUpdate (end of frame)
function LateUpdate()
	-- TODO: 在此编写帧末更新逻辑
	-- TODO: End-of-frame logic here
end

--- OnDisable 回调 / Called on OnDisable
function OnDisable()
	print(CLASS_NAME, "[OnDisable] called")
	-- TODO: 在此处理禁用时的清理或状态保存
	-- TODO: Cleanup or save state when disabled
end

--- OnApplicationQuit 回调 / Called on OnApplicationQuit
function OnApplicationQuit()
	print(CLASS_NAME, "[OnApplicationQuit] called")
	-- TODO: 在此处理应用退出前的逻辑
	-- TODO: Logic before application quits
end

--- OnDestroy 回调 / Called on OnDestroy
function OnDestroy()
	print(CLASS_NAME, "[OnDestroy] called")
	-- TODO: 在此释放资源、移除监听等
	-- TODO: Release resources, remove listeners, etc.
end

-- 注意：无需 return，LuaBehaviour 会直接绑定上述全局函数
-- Note: No return needed—LuaBehaviour binds the above global functions automatically
