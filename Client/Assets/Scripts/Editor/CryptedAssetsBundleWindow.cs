using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using TwilightDreamOfMagical.CryptedMemoryBundle;

public enum Platform
{
	Windows,
	Android,
	iOS
}

public class CryptedAssetsBundleWindow : EditorWindow
{
	private Platform selectedPlatform = Platform.Windows;
	private Builder builder;

	[MenuItem("TDOM-FrameworkTool/Crypted Build Window")]
	public static void ShowWindow()
	{
		var window = GetWindow<CryptedAssetsBundleWindow>("Crypted AssetsBundle Tool");
		window.minSize = new Vector2(600, 300); // Increased minimum size for a wider window
	}

	private void OnEnable()
	{
		builder = new Builder();
	}

	private void OnGUI()
	{
		GUILayout.Space(10);
		GUILayout.Label("Currently only supports our custom encryption algorithm.\nGeneral encryption algorithms will be supported in the future.");

		GUILayout.Label("Select Platform", EditorStyles.boldLabel);
		string[] platforms = Enum.GetNames(typeof(Platform));
		int selectedIndex = (int)selectedPlatform;
		selectedIndex = GUILayout.Toolbar(selectedIndex, platforms);
		selectedPlatform = (Platform)selectedIndex;

		GUILayout.Space(10);
		GUILayout.Label("Enter Password", EditorStyles.boldLabel);
		builder.Password = GUILayout.PasswordField(builder.Password, '*');

		GUILayout.Space(20);
		if (GUILayout.Button("Build AssetsBundle and Encrypt"))
		{
			BuildAndEncrypt();
		}

		if (GUILayout.Button("Decrypt Builded"))
		{
			DecryptBuilded();
		}

		if (GUILayout.Button("Clear Builded"))
		{
			ClearBuildDictionaryContent();
		}
	}

	private void OnDisable()
	{
		builder = null;
	}

	private void BuildAndEncrypt()
	{
		if (string.IsNullOrEmpty(builder.Password))
		{
			EditorUtility.DisplayDialog("Error", "Password is not provided", "Sure");
			return;
		}

		switch (selectedPlatform)
		{
			case Platform.Windows:
				builder.BuildEncryptedBundle(BuildTarget.StandaloneWindows);
				break;
			case Platform.Android:
				builder.BuildEncryptedBundle(BuildTarget.Android);
				break;
			case Platform.iOS:
				builder.BuildEncryptedBundle(BuildTarget.iOS);
				break;
			default:
				Debug.LogWarning("Unknown platform selected");
				break;
		}

		// Compute SHA-256 hash of the password
		using (SHA256 sha256Hash = SHA256.Create())
		{
			byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(builder.Password));
			StringBuilder hashBuilder = new StringBuilder();
			foreach (byte character in bytes)
			{
				hashBuilder.Append(character.ToString("x2"));
			}
			string passwordHash = hashBuilder.ToString();

			File.WriteAllText(Path.Combine(PathUtil.BundleOutputPath, "AssetsBundlePassword.hash"), passwordHash, Encoding.UTF8);
		}
	}

	private void DecryptBuilded()
	{
		if (string.IsNullOrEmpty(builder.Password))
		{
			EditorUtility.DisplayDialog("Error", "Password is not provided", "Sure");
			return;
		}

		string storedPasswordHash = File.ReadAllText(Path.Combine(PathUtil.BundleOutputPath, "AssetsBundlePassword.hash"), Encoding.UTF8);

		if (string.IsNullOrEmpty(storedPasswordHash))
		{
			EditorUtility.DisplayDialog("Warning", "The stored password hash is lost and it is impossible to quickly determine whether the password is correct.", "Sure");
		}
		else
		{
			// Compute SHA-256 hash of the provided password
			string computedHash;
			using (SHA256 sha256Hash = SHA256.Create())
			{
				byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(builder.Password));
				StringBuilder hashBuilder = new StringBuilder();
				foreach (byte b in bytes)
				{
					hashBuilder.Append(b.ToString("x2"));
				}
				computedHash = hashBuilder.ToString();
			}

			// Use constant-time comparison to prevent timing attacks
			if (!CryptCore.FixedTimeEquals(computedHash, storedPasswordHash))
			{
				EditorUtility.DisplayDialog("Error", "Incorrect password", "OK");
				return;
			}
		}

		builder.DecryptAndRestoreBundle();
	}

	private void ClearBuildDictionaryContent()
	{
		string buildPath = PathUtil.BundleOutputPath;

		if (Directory.Exists(buildPath))
		{
			try
			{
				// Delete all files in the build directory.
				foreach (string file in Directory.GetFiles(buildPath))
				{
					File.Delete(file);
				}

				// Delete all subdirectories recursively.
				foreach (string directory in Directory.GetDirectories(buildPath))
				{
					Directory.Delete(directory, true);
				}

				Debug.Log("Build dictionary content cleared successfully.");
			}
			catch (Exception ex)
			{
				Debug.LogError($"Error when clearing build dictionary content: {ex}");
			}
		}
		else
		{
			Debug.LogWarning("Build directory does not exist.");
		}
	}
}
