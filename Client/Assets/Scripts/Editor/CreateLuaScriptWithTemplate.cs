using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor.ProjectWindowCallback;

public class CreateLuaScriptWithTemplate
{
	[MenuItem("Assets/Create/Lua Script", false, 80)]
	public static void CreateNewLua()
	{
		string path = GetSelectedPathOrFallback();
		string templatePath = "Assets/CodeTemplate/Lua/lua.lua"; // 确保这个路径是你的Lua模板文件的路径
																	// 注意新的默认文件名现在包含".bytes"扩展名
		ProjectWindowUtil.StartNameEditingIfProjectWindowExists
		(
			0,
			ScriptableObject.CreateInstance<MyDoCreateScriptAsset>(),
			path + "/NewLuaScript.lua.bytes", null, templatePath
		);

		AssetDatabase.Refresh();
	}

	public static string GetSelectedPathOrFallback()
	{
		string path = "Assets";
		foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
		{
			path = AssetDatabase.GetAssetPath(obj);
			if (!string.IsNullOrEmpty(path) && File.Exists(path))
			{
				path = Path.GetDirectoryName(path);
				break;
			}
		}
		return path;
	}
}

class MyDoCreateScriptAsset : EndNameEditAction
{
	public override void Action(int instanceId, string pathName, string resourceFile)
	{
		UnityEngine.Object asset = CreateScriptAssetFromTemplate(pathName, resourceFile);
		ProjectWindowUtil.ShowCreatedAsset(asset);
	}

	internal static UnityEngine.Object CreateScriptAssetFromTemplate(string pathName, string resourceFile)
	{
		string fullPath = Path.GetFullPath(pathName);
		string template = File.ReadAllText(resourceFile);

		// 1. 去掉外层 .bytes → e.g. "MyScript.lua" 或 "MyScript"
		string nameWithoutBytes = Path.GetFileNameWithoutExtension(pathName);

		// 2. 保证有 .lua 后缀 → 如果已经包含就不加，否则自动加上
		string fileName = nameWithoutBytes.EndsWith(".lua")
			? nameWithoutBytes
			: nameWithoutBytes + ".lua";

		// 3. 得到真正的类名（去掉 .lua）→ e.g. "MyScript"
		string className = Path.GetFileNameWithoutExtension(fileName);

		// 3. 替换占位符
		template = Regex.Replace(template, "#FILE_NAME#", fileName);
		template = Regex.Replace(template, "#CLASS_NAME#", className);

		// 4. 写文件（UTF8 无 BOM）
		using var writer = new StreamWriter(fullPath, false, new UTF8Encoding(false));
		writer.Write(template);

		AssetDatabase.ImportAsset(pathName);
		return AssetDatabase.LoadAssetAtPath(pathName, typeof(UnityEngine.Object));
	}
}
