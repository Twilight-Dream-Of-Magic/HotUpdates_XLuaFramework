using UnityEngine;
using UnityEditor;

public class FileSecurityToolWindow : EditorWindow
{
	private string inputPath = "";
	private string outputPath = "";

	FileSecurityTool fileSecurityTool = new FileSecurityTool();

	[MenuItem("TDOM-FrameworkTool/Files Security Tool Window")]
	public static void ShowWindow()
	{
		GetWindow<FileSecurityToolWindow>("File Security Tool");
	}

	private void OnGUI()
	{
		GUILayout.Label("AssetBundle SecurityTool Window", EditorStyles.boldLabel);

		// 输入路径
		GUILayout.BeginHorizontal();
		GUILayout.Label("Input Path", GUILayout.Width(70));
		inputPath = GUILayout.TextField(inputPath);
		if (GUILayout.Button("Browse", GUILayout.Width(60)))
		{
			inputPath = EditorUtility.OpenFilePanel("Select AssetBundle", "", "");
		}
		GUILayout.EndHorizontal();

		// 输出路径
		GUILayout.BeginHorizontal();
		GUILayout.Label("Output Path", GUILayout.Width(70));
		outputPath = GUILayout.TextField(outputPath);
		if (GUILayout.Button("Browse", GUILayout.Width(60)))
		{
			outputPath = EditorUtility.SaveFilePanel("Save Encrypted AssetBundle", "", "", "");
		}
		GUILayout.EndHorizontal();

		// 添加密码输入GUI
		GUILayout.BeginHorizontal();
		GUILayout.Label("Password", GUILayout.Width(70));
		fileSecurityTool.Password = GUILayout.PasswordField(fileSecurityTool.Password, '*');
		GUILayout.EndHorizontal();

		// 加密和解密按钮
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Encrypt"))
		{
			if (string.IsNullOrEmpty(inputPath) || string.IsNullOrEmpty(outputPath))
			{
				EditorUtility.DisplayDialog("Error", "Please specify both input and output paths.", "OK");
				return;
			}
			fileSecurityTool.EncryptFileWithLittleOPC(inputPath, outputPath);
		}
		if (GUILayout.Button("Decrypt"))
		{
			if (string.IsNullOrEmpty(inputPath) || string.IsNullOrEmpty(outputPath))
			{
				EditorUtility.DisplayDialog("Error", "Please specify both input and output paths.", "OK");
				return;
			}
			fileSecurityTool.DecryptFileWithLittleOPC(inputPath, outputPath);
		}
		
		if (GUILayout.Button("SelfCheck"))
		{
			fileSecurityTool.SelfCheckWithLittleOPC();
		}
		
		GUILayout.EndHorizontal();
	}

}
