using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;
using Utilities;
using Newtonsoft.Json;

namespace TwilightDreamOfMagical.CryptedMemoryBundle
{
	public class URL_Tool
	{
		public static bool URL_Exists(string url)
		{
			if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
				return false;

			string scheme = uri.Scheme;

			// file://
			if (scheme == Uri.UriSchemeFile)
			{
				return File.Exists(uri.LocalPath);
			}
			// ftp://
			else if (scheme == Uri.UriSchemeFtp)
			{
				try
				{
					var ftpRequest = (FtpWebRequest)WebRequest.Create(uri);
					ftpRequest.Method = WebRequestMethods.Ftp.GetFileSize;

					var ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
					bool exists = ftpResponse.StatusCode == FtpStatusCode.FileStatus;
					ftpResponse.Close();
					return exists;
				}
				catch
				{
					return false;
				}
			}
			// http:// or https://
			else if (scheme == Uri.UriSchemeHttp || scheme == Uri.UriSchemeHttps)
			{
				try
				{
					var httpRequest = (HttpWebRequest)WebRequest.Create(uri);
					httpRequest.Method = "HEAD";
					httpRequest.AllowAutoRedirect = true;

					var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
					int code = (int)httpResponse.StatusCode;
					httpResponse.Close();
					return code >= 200 && code < 400;
				}
				catch
				{
					return false;
				}
			}
			// unsupported scheme
			else
			{
				return false;
			}
		}
	}

	public class DownloadedCryptedDatas
	{
		public string BundleName = string.Empty;
		public byte[] BundleDatas;
		public List<byte[]> DependencyBundleDatas = new List<byte[]>();
	}

	[Serializable]
	public class BundleInfo
	{
		public string AssetsName;
		public string BundleName;
		public List<string> DependenceFilePaths;
	}

	/// <summary>
	/// Crypted Memory Resource Manager.
	/// </summary>
	/// <remarks>
	/// Responsible for loading encrypted AssetBundles entirely in memory, 
	/// tracking dependencies via manifests, managing reference counts, 
	/// and unloading resources when no longer needed. 
	/// Decryption is performed on a background thread, 
	/// while all Unity API interactions (e.g. asset instantiation) 
	/// are marshaled back to the main thread using coroutines.
	/// </remarks>
	public class Manager : MonoBehaviour
	{
		[Tooltip("Securely erase decrypted bundle bytes from memory after load.")]
		public bool SecureEraseDecryptedData { get; set; } = true;

		[Tooltip("Name of the manifest bundle in StreamingAssets (without extension)")]
		public string manifestBundleName = "AssetBundles";

		private HotUpdate hotUpdate;
		private string VersionFilePath = FrameworkConstant.ProtectedVersionFileListName;
		public string VersionFileList
		{
			set { VersionFilePath = value; }
			get { return VersionFilePath; }
		}

		private Downloader bundleDownloader;
		private AssetLoader bundleLoader;
		private CryptCore bundleCryptCore;

		public string Password
		{
			get { return bundleCryptCore.Password; }
			set { bundleCryptCore.Password = value; }
		}

		private void Awake()
		{
			bundleCryptCore = new CryptCore();
			InitializeBundleLoader();
			InitializeBundleDownloader();
		}

		private void InitializeBundleLoader()
		{
			this.bundleLoader = this.gameObject.AddComponent<AssetLoader>();
			this.bundleLoader.ParseProtectedVersionFile();
		}

		private void InitializeBundleDownloader()
		{
			if (bundleLoader is not null)
			{
				this.bundleDownloader = this.gameObject.AddComponent<Downloader>();
			}
		}

		private void OnDestroy()
		{
			bundleCryptCore?.Dispose();
			Destroy(bundleDownloader);
			Destroy(bundleLoader);
		}

		/* APIs */

		/// <summary>
		/// Verifies and initializes the version file path, then ensures that a HotUpdate
		/// component is attached to the GameObject and starts the hot-update file process.
		/// </summary>
		/// <remarks>
		/// - If <see cref="VersionFilePath"/> is null or empty, it will be set to
		///   <see cref="FrameworkConstant.ProtectedVersionFileListName"/>.
		/// - Only one <see cref="HotUpdate"/> component will be added; subsequent calls
		///   will not re-add or restart if the component already exists.
		/// </remarks>
		public void CheckUpdateFiles()
		{
			if (string.IsNullOrEmpty(this.VersionFilePath))
				this.VersionFilePath = FrameworkConstant.ProtectedVersionFileListName;

			if (System.Object.ReferenceEquals(this.hotUpdate, null))
			{
				this.hotUpdate = this.gameObject.AddComponent<HotUpdate>();
				this.hotUpdate.VersionFileList = this.VersionFilePath;
				this.hotUpdate.DoHotUpdateFiles();
			}
		}

		public void LoadUIPrefab(string AssetName, Action<UnityEngine.Object> CallbackAction = null)
		{
			LoadAsset(PathUtil.GetUIPrefabPath(AssetName), CallbackAction);
		}

		public void LoadMusic(string AssetName, Action<UnityEngine.Object> CallbackAction = null)
		{
			LoadAsset(PathUtil.GetMusicPath(AssetName), CallbackAction);
		}

		public void LoadSound(string AssetName, Action<UnityEngine.Object> CallbackAction = null)
		{
			LoadAsset(PathUtil.GetSoundPath(AssetName), CallbackAction);
		}

		public void LoadScene(string AssetName, Action<UnityEngine.Object> CallbackAction = null)
		{
			LoadAsset(PathUtil.GetScenePath(AssetName), CallbackAction);
		}

		/// <summary>
		/// Asynchronously loads a Lua script asset and invokes a callback upon completion.
		/// </summary>
		/// <remarks>
		/// <para>Do not use <c>PathUtil</c> for building the asset path here.</para>
		/// <para>Refer to <see cref="LuaManager.LuaFileNameList"/> for the list of valid script identifiers.</para>
		/// </remarks>
		/// <param name="luaScriptPath">
		/// The identifier or relative path of the Lua script to load (without extension),  
		/// e.g. "foo/bar.lua.bytes". 
		/// This value is passed directly to the asset loading pipeline.
		/// </param>
		/// <param name="callbackAction">
		/// Optional callback that receives the loaded <see cref="UnityEngine.Object"/> (typically a <c>TextAsset</c>)  
		/// once the script is available. If <c>null</c>, no callback is invoked.
		/// </param>
		public void LoadLuaScript(string LuaScriptPath, Action<UnityEngine.Object> CallbackAction = null)
		{
			LoadAsset(LuaScriptPath, CallbackAction);
		}

		public void LoadPrefab(string PrefabPath, Action<UnityEngine.Object> CallbackAction = null)
		{
			LoadAsset(PrefabPath, CallbackAction);
		}

		/// <summary>
		/// Initiates the secure loading of an asset by its name.  
		/// This method validates the asset name, password, and stored password hash, 
		/// then delegates the download, decryption, and loading process to LoadAssetLogic.
		/// </summary>
		/// <param name="assetName">
		/// The logical name of the asset to load. This name must match an entry in BundleInfos.
		/// </param>
		/// <param name="onAssetLoadComplete">
		/// Optional callback invoked when loading completes or fails. 
		/// Receives the loaded UnityEngine.Object on success, or null on error.
		/// </param>
		public void LoadAsset
		(
			string assetName,
			Action<UnityEngine.Object> onAssetLoadComplete = null
		)
		{
			//Get asset bundle name
			if (!this.bundleLoader.AssetBundleInfos.TryGetValue(assetName, out BundleInfo bundleInfo))
			{
				Debug.LogError($"AssetName {assetName} has no corresponding BundleInfo");
				onAssetLoadComplete?.Invoke(null);
				return;
			}

			if (string.IsNullOrEmpty(assetName))
			{
				Debug.LogError("Asset name is null or empty.");
				onAssetLoadComplete?.Invoke(null);
				return;
			}

			// 1. Check that a password has been provided.
			if (string.IsNullOrEmpty(this.Password))
			{
				Debug.LogError("Password is null or empty.");
				onAssetLoadComplete?.Invoke(null);
				return;
			}

			// 2. Read the stored password hash.
			string hashPath = Path.Combine(PathUtil.BuildResourcesPath, "AssetsBundlePassword.hash");
			string storedPasswordHash = File.ReadAllText(hashPath, System.Text.Encoding.UTF8);

			if (string.IsNullOrEmpty(storedPasswordHash))
			{
				Debug.LogWarning("The stored password hash is lost; cannot quickly verify correctness.");
			}
			else
			{
				// Compute SHA256 of the provided password.
				string computedHash;
				using (var sha256 = SHA256.Create())
				{
					byte[] bytes = sha256.ComputeHash(
						System.Text.Encoding.UTF8.GetBytes(this.Password));
					var sb = new System.Text.StringBuilder();
					foreach (var b in bytes)
						sb.Append(b.ToString("x2"));
					computedHash = sb.ToString();
				}

				// Constant-time comparison to prevent timing attacks.
				if (!CryptCore.FixedTimeEquals(computedHash, storedPasswordHash))
				{
					Debug.LogError("Incorrect password");
					onAssetLoadComplete?.Invoke(null);
					return;
				}
			}

			LoadAssetLogic(assetName, bundleInfo.BundleName);
		}

		/// <summary>
		/// Performs the download, decryption, and asynchronous loading of an asset bundle and its dependencies,
		/// then loads the requested asset from the decrypted bundle.
		/// </summary>
		/// <param name="assetName">
		/// The name of the asset to load within the AssetBundle. Must match one of the bundle’s contained assets.
		/// </param>
		/// <param name="assetBundleName">
		/// The name of the encrypted AssetBundle (and dependency bundles) to download from the server.
		/// </param>
		/// <param name="onAssetLoadComplete">
		/// Optional callback invoked when the asset is loaded or if any step fails. 
		/// Receives the loaded UnityEngine.Object on success, or null on error.
		/// </param>
		private void LoadAssetLogic
		(
			string assetName,
			string assetBundleName,
			Action<UnityEngine.Object> onAssetLoadComplete = null
		)
		{
			// The URL endpoint where the encrypted bundle and its dependencies reside.
			// Must not be null or empty.
			string webUrl = FrameworkConstant.ResourcesURL + assetBundleName;

			// 3. Validate URL and asset name.
			if (!URL_Tool.URL_Exists(webUrl))
			{
				Debug.LogError("Download failed: URL is invaild.");
				onAssetLoadComplete?.Invoke(null);
				return;
			}

			// 5. Delegate to be invoked upon download completion.
			Action<DownloadedCryptedDatas> OnDownloadComplete = downloadedCryptedDatas =>
			{
				if (downloadedCryptedDatas == null)
				{
					Debug.LogError("Download failed: Downloaded data is null.");
					onAssetLoadComplete?.Invoke(null);
					return;
				}

				Debug.Log("Download complete. Processing data...");

				// 6. Decrypt and load all bundles into bundleLoader.AssetBundles (Need wait).
				this.bundleLoader.DecryptAndLoadBundleDatasAsync
				(
					SecureEraseDecryptedData,
					bundleCryptCore,
					downloadedCryptedDatas
				);

				// 6. Coroutine to be invoked upon process bundle data.
				StartCoroutine(OnProcessBundleWapperData(assetName, assetBundleName, onAssetLoadComplete));
			};

			// 4. Kick off the download coroutine.
			StartCoroutine
			(
				this.bundleDownloader.DownloadEncryptedBundleAndDependencyDatas
				(
					webUrl,
					assetName,
					this.bundleLoader.AssetBundleInfos,
					OnDownloadComplete
				)
			);
		}

		private IEnumerator OnProcessBundleWapperData(string assetName, string assetBundleName, Action<UnityEngine.Object> onAssetLoadComplete)
		{
			float timeout = 300f; //300 second
			float startTime = Time.time;
			Func<bool> WaitAssetBundleIsLoaded = () =>
			{
				return this.bundleLoader.AssetBundleDatas.ContainsKey(assetBundleName) || Time.time - startTime > timeout;
			};
			yield return new WaitUntil(WaitAssetBundleIsLoaded);

			// 6. Verify that the target asset bundle was registered.
			if (!this.bundleLoader.AssetBundleDatas.TryGetValue(assetBundleName, out var bundleWrapper))
			{
				Debug.LogError($"Timeout waiting for bundle '{assetBundleName}' to be ready loaded.");
				onAssetLoadComplete?.Invoke(null);
				yield break;
			}

			// Note: Scenes asset can't be loaded in this way, they are returned directly.
			// Please look class UnityEngine.AssetBundleCreateRequest
			if (assetName.EndsWith(".unity"))
			{
				onAssetLoadComplete?.Invoke(null);
				yield break;
			}

			// 7. Asynchronously load the asset from the bundle.
			AssetBundleRequest bundleRequest = bundleWrapper.Bundle.LoadAssetAsync(assetName);
			yield return bundleRequest;

			if (bundleRequest.asset == null)
			{
				Debug.LogError($"Failed to load asset '{assetName}' from bundle:{bundleWrapper.Bundle.name}.");
			}
			onAssetLoadComplete?.Invoke(bundleRequest.asset);
		}

		/// <summary>
		/// Automatically unloads an asset’s bundle and, optionally, all of its dependencies.
		/// </summary>
		/// <param name="asset">
		/// The Unity asset whose bundle you want to unload. Must not be null.
		/// </param>
		/// <param name="unloadDependencies">
		/// If true, unloads every bundle that this bundle depends on.
		/// </param>
		/// <param name="unloadBundleAssets">
		/// Passed to AssetBundle.Unload(unloadBundleAssets):
		/// true  = also unloads all loaded asset objects,
		/// false = only unloads the bundle data.
		/// </param>
		public void UnloadAsset
		(
			UnityEngine.Object asset,
			bool unloadDependencies,
			bool unloadBundleAssets
		)
		{
			if (asset == null)
			{
				Debug.LogError("UnloadAsset failed: Asset is null.");
				return;
			}

			// Look up the real bundle name via BundleInfos
			string bundleName = asset.name;
			if (this.bundleLoader != null
				&& this.bundleLoader.AssetBundleInfos.TryGetValue(asset.name, out BundleInfo info))
			{
				bundleName = info.BundleName;
			}

			var processedBundles = new HashSet<string>();
			UnloadBundleAndDependencies(bundleName, unloadDependencies, unloadBundleAssets, processedBundles);
		}

		/// <summary>
		/// Recursively unloads a bundle and, if requested, its dependencies using
		/// an explicit depth-first traversal to avoid stack overflows or cycles.
		/// </summary>
		/// <param name="bundleName">The key identifying the bundle in AssetBundles.</param>
		/// <param name="unloadDependencies">
		/// If true, dependencies listed in DependenceFilePaths will also be unloaded.
		/// </param>
		/// <param name="unloadBundleAssets">
		/// Controls the unloadAllLoadedObjects flag of AssetBundle.Unload.
		/// </param>
		/// <param name="processedBundles">
		/// A set of bundle names already processed to prevent infinite loops.
		/// </param>
		private void UnloadBundleAndDependencies(
			string bundleName,
			bool unloadDependencies,
			bool unloadBundleAssets,
			HashSet<string> processedBundles
		)
		{
			var bundleStack = new Stack<string>();
			bundleStack.Push(bundleName);

			while (bundleStack.Count > 0)
			{
				string currentName = bundleStack.Pop();
				if (processedBundles.Contains(currentName))
					continue;
				processedBundles.Add(currentName);

				if (bundleLoader.AssetBundleDatas.TryGetValue(currentName, out AssetLoader.BundleData bundleData))
				{
					// Decrement reference count
					bundleData.ReferencedCount--;

					if (bundleData.ReferencedCount <= 0)
					{
						// Unload the bundle (and optionally its assets)
						bundleData.Bundle.Unload(unloadDependencies ? true : unloadBundleAssets);
						bundleLoader.AssetBundleDatas.Remove(currentName);

						// If requested, queue up dependencies for unloading
						if (unloadDependencies
							&& bundleLoader.AssetBundleInfos.TryGetValue(currentName, out BundleInfo info))
						{
							foreach (var dep in info.DependenceFilePaths ?? Enumerable.Empty<string>())
							{
								bundleStack.Push(dep);
							}
						}
					}
				}
				else
				{
					Debug.LogError($"Bundle '{currentName}' not found");
				}
			}
		}

		/// <summary>
		/// Manually unloads an asset’s bundle and all of its dependencies
		/// that may have been left behind when Unload(false) was called previously.
		/// </summary>
		/// <param name="asset">The asset to unload (must not be null).</param>
		/// <param name="destroyAssetAfterUnload">
		/// If true, will call Object.Destroy on the asset after unloading bundles,
		/// provided the asset still exists.
		/// </param>
		public void ManualUnloadAsset(UnityEngine.Object asset, bool destroyAssetAfterUnload = false)
		{
			if (asset == null)
			{
				Debug.LogError("ManualUnloadAsset failed: asset is null.");
				return;
			}

			// 1. Get the bundle key from the asset’s name (or override via BundleInfo)
			string assetBundleName = asset.name;
			if (bundleLoader != null
				&& bundleLoader.AssetBundleInfos.TryGetValue(asset.name, out BundleInfo assetBundleInfo))
			{
				assetBundleName = assetBundleInfo.BundleName;
			}

			// 2. Retrieve the BundleData
			if (!bundleLoader.AssetBundleDatas.TryGetValue(assetBundleName, out AssetLoader.BundleData mainBundleData))
			{
				Debug.LogError($"ManualUnloadAsset: bundle '{assetBundleName}' not found.");
				return;
			}

			// 3. Decrement the main bundle’s reference count
			mainBundleData.ReferencedCount--;
			Debug.Log($"[Manual] Decremented ref count for '{assetBundleName}' to {mainBundleData.ReferencedCount}.");

			// 4. Always unload *all* dependencies, regardless of the main refernece count
			if (bundleLoader.AssetBundleInfos.TryGetValue(assetBundleName, out BundleInfo mainBundleInfo)
				&& mainBundleInfo.DependenceFilePaths != null)
			{
				for (int index = 0; index < mainBundleInfo.DependenceFilePaths.Count; index++)
				{
					string dependencyFilePath = mainBundleInfo.DependenceFilePaths[index];
					if (bundleLoader.AssetBundleDatas.TryGetValue(dependencyFilePath, out AssetLoader.BundleData dependencyBundleData))
					{
						dependencyBundleData.ReferencedCount--;
						Debug.Log(
							$"[Manual] Decremented ref count for dependency '{dependencyFilePath}' " +
							$"to {dependencyBundleData.ReferencedCount}.");

						if (dependencyBundleData.ReferencedCount <= 0)
						{
							dependencyBundleData.Bundle.Unload(false);
							bundleLoader.AssetBundleDatas.Remove(dependencyFilePath);
							Debug.Log(
								$"[Manual] Unloaded dependency '{dependencyFilePath}' " +
								"as ref count reached zero.");
						}
					}
				}
			}

			// 5. Unload the main bundle if its count is zero or below
			if (mainBundleData.ReferencedCount <= 0)
			{
				mainBundleData.Bundle.Unload(false);
				bundleLoader.AssetBundleDatas.Remove(assetBundleName);
				Debug.Log($"[Manual] Unloaded main bundle '{assetBundleName}' as ref count reached zero.");
			}
			else
			{
				Debug.Log($"[Manual] Main bundle '{assetBundleName}' still has references; not unloaded.");
			}

			// 6. Optionally destroy the asset object itself
			if (destroyAssetAfterUnload && asset != null)
			{
				Debug.Log($"[Manual] Destroying asset object '{asset.name}'.");
				UnityEngine.Object.Destroy(asset);
			}
		}
	}

	public class Builder
	{
		private CryptCore bundleCryptCore = new CryptCore();

		public string Password
		{
			get { return bundleCryptCore.Password; }
			set { bundleCryptCore.Password = value; }
		}

#if UNITY_EDITOR

		public void BuildEncryptedBundle(UnityEditor.BuildTarget buildTarget)
		{
			List<UnityEditor.AssetBundleBuild> assetBundleBuilds = new List<UnityEditor.AssetBundleBuild>();
			string[] files = Directory.GetFiles(PathUtil.BuildResourcesInputPath, "*", SearchOption.AllDirectories);
			// Use an instance of SecureMemoryBundleBuilder to access the BundleInfos dictionary
			Builder builderInstance = this;
			List<BundleInfo> bundleInfos = new List<BundleInfo>();

			for (int i = 0; i < files.Length; i++)
			{
				if (files[i].EndsWith(".meta"))
					continue;

				UnityEditor.AssetBundleBuild assetBundleBuild = new UnityEditor.AssetBundleBuild();
				string fileName = PathUtil.GetStandardPath(files[i]);
				Debug.Log("file: " + files[i]);

				string assetName = PathUtil.GetUnityRelativePath(fileName); // 转换为Unity相对路径
				assetBundleBuild.assetNames = new string[] { assetName };

				string bundleName = fileName.Replace(PathUtil.BuildResourcesInputPath, "").ToLower();
				if (bundleName.StartsWith("/"))
					bundleName = bundleName.Substring(1);
				assetBundleBuild.assetBundleName = bundleName + ".assetbundle";
				assetBundleBuilds.Add(assetBundleBuild);

				// 获取依赖的AssetBundle文件
				List<string> dependencies = GetDependenciesFilePaths(assetName); // Corrected type here

				BundleInfo bundleInfo;
				if (dependencies.Count > 0)
				{
					bundleInfo = new BundleInfo
					{
						AssetsName = assetName,
						BundleName = bundleName + ".assetbundle",  // 使用 ".assetbundle" 后缀
						DependenceFilePaths = dependencies
					};
				}
				else
				{
					bundleInfo = new BundleInfo
					{
						AssetsName = assetName,
						BundleName = bundleName + ".assetbundle",  // 使用 ".assetbundle" 后缀
						DependenceFilePaths = null
					};
				}

				bundleInfos.Add(bundleInfo);
			}

			if (files.Length > 0)
			{
				if (Directory.Exists(PathUtil.BundleOutputPath))
					Directory.Delete(PathUtil.BundleOutputPath, true);
				Directory.CreateDirectory(PathUtil.BundleOutputPath);

				UnityEditor.BuildPipeline.BuildAssetBundles(PathUtil.BundleOutputPath, assetBundleBuilds.ToArray(), UnityEditor.BuildAssetBundleOptions.None, buildTarget);

				EncryptAndSaveBundlesAsync(PathUtil.BundleOutputPath, bundleInfos, builderInstance);

				var json = JsonConvert.SerializeObject(bundleInfos);
				File.WriteAllText(PathUtil.BundleOutputPath + "/" + FrameworkConstant.ProtectedVersionFileListName, json, System.Text.Encoding.UTF8);
			}

			UnityEditor.AssetDatabase.Refresh();
		}

		private List<string> GetDependenciesFilePaths(string currentAssetName)
		{
			List<string> dependenceFilePaths = new List<string>();
			string[] files = UnityEditor.AssetDatabase.GetDependencies(currentAssetName);
			dependenceFilePaths = files.Where(file => !file.EndsWith(".cs") && !file.Equals(currentAssetName)).ToList();
			return dependenceFilePaths;
		}

		public void DecryptAndRestoreBundle()
		{
			string json = File.ReadAllText(Path.Combine(PathUtil.BuildResourcesPath, FrameworkConstant.ProtectedVersionFileListName), System.Text.Encoding.UTF8);
			List<BundleInfo> bundleInfos = JsonConvert.DeserializeObject<List<BundleInfo>>(json);
			Builder builderInstance = this;

			if (bundleInfos.Count > 0)
			{
				DecryptAndSaveBundlesAsync(PathUtil.BuildResourcesPath, bundleInfos, builderInstance);
			}

			UnityEditor.AssetDatabase.Refresh();
		}

		private async void EncryptAndSaveBundlesAsync(string bundleResourcesPath, List<BundleInfo> bundleInfos, Builder builderInstance)
		{
			if (builderInstance == null)
				return;

			// Save the Bundle file path to prevent repeated encryption
			HashSet<string> alreadyProcessedBundleFilePaths = new HashSet<string>();

			string bundleFilePath = null;
			string encryptedBundleFilePath = null;
			byte[] fileData = null;
			byte[] encryptedData = null;
			for (int index = 0; index < bundleInfos.Count; index++)
			{
				BundleInfo bundleInfo = bundleInfos[index];
				bundleFilePath = Path.Combine(bundleResourcesPath, bundleInfo.BundleName);
				
				if (!File.Exists(bundleFilePath))
				{
					Debug.LogError($"Bundle file not found: {bundleFilePath}");
					continue;
				}

				if (alreadyProcessedBundleFilePaths.Contains(bundleFilePath))
					continue;
				alreadyProcessedBundleFilePaths.Add(bundleFilePath);

				encryptedBundleFilePath = bundleFilePath + ".crypted";
				fileData = FileReaderWriter.ReadExistingFile(bundleFilePath);
				try
				{
					encryptedData = await builderInstance.bundleCryptCore.EncryptDataAsync(fileData);
					FileReaderWriter.WriteOrOverwriteFile(encryptedBundleFilePath, encryptedData);
					Debug.Log($"Encrypted bundle: {bundleFilePath} \n -> \n {encryptedBundleFilePath}");

					// Move bundleFilePath.meta to encryptedBundleFilePath.meta
					string metaPath = bundleFilePath + ".meta";
					string metaEncryptedPath = encryptedBundleFilePath + ".meta";
					if (File.Exists(metaPath))
					{
						File.Move(metaPath, metaEncryptedPath);
						Debug.Log($"Moved meta file: {metaPath} -> {metaEncryptedPath}");
					}
				}
				catch (Exception ex)
				{
					Debug.LogError($"Encryption failed for bundle: {bundleFilePath}, error: {ex.Message}");
				}

				bundleFilePath = null;
				encryptedBundleFilePath = null;
				fileData = null;
				encryptedData = null;
			}

			foreach (var filePath in alreadyProcessedBundleFilePaths)
			{
				File.Delete(filePath);
			}
			alreadyProcessedBundleFilePaths.Clear();
		}

		private async void DecryptAndSaveBundlesAsync(string bundleResourcesPath, List<BundleInfo> bundleInfos, Builder builderInstance)
		{
			if (builderInstance == null)
				return;

			// Save the Bundle file path to prevent repeated decryption
			HashSet<string> alreadyProcessedBundleFilePaths = new HashSet<string>();

			string decryptedBundleFilePath = null;
			string encryptedBundleFilePath = null;
			byte[] encryptedFileData = null;
			byte[] decryptedData = null;
			for (int index = 0; index < bundleInfos.Count; index++)
			{
				BundleInfo bundleInfo = bundleInfos[index];
				decryptedBundleFilePath = Path.Combine(bundleResourcesPath, bundleInfo.BundleName);
				encryptedBundleFilePath = decryptedBundleFilePath + ".crypted";
				if (!File.Exists(encryptedBundleFilePath))
				{
					Debug.LogError($"Encrypted bundle file not found: {encryptedBundleFilePath}");
					continue;
				}

				if (alreadyProcessedBundleFilePaths.Contains(encryptedBundleFilePath))
					continue;
				alreadyProcessedBundleFilePaths.Add(encryptedBundleFilePath);

				encryptedFileData = FileReaderWriter.ReadExistingFile(encryptedBundleFilePath);
				try
				{
					decryptedData = await builderInstance.bundleCryptCore.DecryptDataAsync(encryptedFileData);
					FileReaderWriter.WriteOrOverwriteFile(decryptedBundleFilePath, decryptedData);
					Debug.Log($"Decrypted bundle (save file): {encryptedBundleFilePath} \n -> \n {decryptedBundleFilePath}");

					// Move encryptedBundleFilePath.meta to decryptedBundleFilePath.meta
					string metaEncryptedPath = encryptedBundleFilePath + ".meta";
					string metaDecryptedPath = decryptedBundleFilePath + ".meta";
					if (File.Exists(metaEncryptedPath))
					{
						File.Move(metaEncryptedPath, metaDecryptedPath);
						Debug.Log($"Moved meta file: {metaEncryptedPath} -> {metaDecryptedPath}");
					}
				}
				catch (Exception ex)
				{
					Debug.LogError($"Decryption failed for bundle: {encryptedBundleFilePath}, error: {ex.Message}");
				}

				decryptedBundleFilePath = null;
				encryptedBundleFilePath = null;
				encryptedFileData = null;
				decryptedData = null;
			}

			foreach (var filePath in alreadyProcessedBundleFilePaths)
			{
				File.Delete(filePath);
			}
			alreadyProcessedBundleFilePaths.Clear();
		}
#endif
	}

	public class AssetLoader : MonoBehaviour
	{
		public class AtomicInt
		{
			private int value;
			public AtomicInt() { this.value = 0; }
			public AtomicInt(int value) { this.value = value; }
			public static implicit operator int(AtomicInt atomicInt)
			{
				return System.Threading.Interlocked.CompareExchange(ref atomicInt.value, 0, 0);
			}
			public static implicit operator AtomicInt(int newValue)
			{
				var atomicInt = new AtomicInt();
				atomicInt.value = newValue;
				return atomicInt;
			}

			public int Value
			{
				get
				{
					return value;
				}
			}
			public static AtomicInt operator ++(AtomicInt atomicInt)
			{
				System.Threading.Interlocked.Increment(ref atomicInt.value);
				return atomicInt;
			}
			public static AtomicInt operator --(AtomicInt atomicInt)
			{
				System.Threading.Interlocked.Decrement(ref atomicInt.value);
				return atomicInt;
			}
		}

		internal class BundleData
		{
			public AssetBundle Bundle;

			//资源包被几个对象给使用？
			public AtomicInt ReferencedCount;

			public BundleData(AssetBundle bundle)
			{
				Bundle = bundle;
				ReferencedCount = 1;
			}
		}

		//存放Bundle信息
		internal Dictionary<string, BundleInfo> AssetBundleInfos = new Dictionary<string, BundleInfo>();
		//存放Bundle资源
		internal Dictionary<string, BundleData> AssetBundleDatas = new Dictionary<string, BundleData>();

		public void ParseProtectedVersionFile()
		{
			string jsonPath = Path.Combine(PathUtil.BuildResourcesPath, FrameworkConstant.ProtectedVersionFileListName);
			if (!File.Exists(jsonPath))
			{
				Debug.LogError($"The protected version file list: {jsonPath} is lost! ");
				return;
			}

			string json = File.ReadAllText(jsonPath, System.Text.Encoding.UTF8);
			List<BundleInfo> bundleInfos = JsonConvert.DeserializeObject<List<BundleInfo>>(json);

			for (int i = 0; i < bundleInfos.Count; i++)
			{
				BundleInfo bundleInfo = bundleInfos[i];

				AssetBundleInfos.Add(bundleInfo.AssetsName, bundleInfo);

				if (bundleInfo.AssetsName.IndexOf("LuaScripts") > 0)
				{
					if (!FrameworkManager.Lua.LuaFileNameList.Contains(bundleInfo.AssetsName))
					{
						FrameworkManager.Lua.LuaFileNameList.Add(bundleInfo.AssetsName);
					}
				}
			}
		}

		// Modified helper method to decrypt bundle bytes. Returns null if the encrypted file does not exist.
		private async Task<byte[]> DecryptAndLoadBundleAsync(string bundleName, CryptCore secureMemoryBundleCore)
		{
			string decryptedBundleFilePath = Path.Combine(PathUtil.BuildResourcesInputPath, bundleName);
			string encryptedBundleFilePath = decryptedBundleFilePath + ".crypted";
			if (!File.Exists(encryptedBundleFilePath))
			{
				Debug.LogError($"Encrypted bundle file not found: {encryptedBundleFilePath}");
				return null;
			}

			byte[] fileData = FileReaderWriter.ReadExistingFile(encryptedBundleFilePath);
			byte[] decryptedData = null;
			try
			{
				decryptedData = await secureMemoryBundleCore.DecryptDataAsync(fileData);
				Debug.Log($"Decrypted bundle (not saved to file): {encryptedBundleFilePath} \n -> \n {decryptedBundleFilePath}");
			}
			catch (Exception ex)
			{
				Debug.LogError($"Decryption failed for bundle: {encryptedBundleFilePath}, error: {ex.Message}");
			}

			return decryptedData;
		}

		// Helper method to convert AssetBundle.LoadFromMemoryAsync into a Task.
		// Must Running On The Unity Main thread !!!
		private Task<AssetBundle> LoadAssetBundleFromMemoryAsync(byte[] bundleDataBytes)
		{
			var tcs = new TaskCompletionSource<AssetBundle>();
			// Asynchronously create the assetbundle from the memory bytes.
			AssetBundleCreateRequest request = AssetBundle.LoadFromMemoryAsync(bundleDataBytes);
			request.completed += operation =>
			{
				if (request.assetBundle != null)
					tcs.SetResult(request.assetBundle);
				else
					tcs.SetException(new InvalidOperationException("AssetBundle.LoadFromMemoryAsync returned null"));
			};
			return tcs.Task;
		}

		// Modified method to load a local bundle by decrypting the bytes.
		public async void DecryptAndLoadBundleFilesAsync(bool secureEraseDecryptedData, string assetName, CryptCore bundleCryptCore)
		{
			if (!this.AssetBundleInfos.ContainsKey(assetName))
			{
				Debug.LogError($"Asset name '{assetName}' not found in BundleInfos.");
				return;
			}

			// Initialize stack with the initial asset name
			Stack<string> bundleFilePathStack = new Stack<string>();
			bundleFilePathStack.Push(assetName);

			string currentAssetName = null;
			string bundleName = null;
			byte[] decryptedData = null;
			while (bundleFilePathStack.Count > 0)
			{
				currentAssetName = bundleFilePathStack.Pop();

				if (!this.AssetBundleInfos.TryGetValue(currentAssetName, out var bundleInfo))
				{
					Debug.LogError($"Asset name '{currentAssetName}' not found in BundleInfos.");
					continue;
				}

				bundleName = bundleInfo.BundleName;

				// Save or update the AssetBundle resource and increment its reference count
				if (!this.AssetBundleDatas.ContainsKey(bundleName))
				{
					// Call asynchronous method to decrypt and retrieve the bundle data
					decryptedData = await DecryptAndLoadBundleAsync(bundleName, bundleCryptCore);
					if (decryptedData == null)
					{
						Debug.LogError($"Failed to decrypt bundle: {bundleName}");
						return;
					}

					// Asynchronously load the AssetBundle from memory
					AssetBundle loadedBundle = await LoadAssetBundleFromMemoryAsync(decryptedData);
					if (loadedBundle == null)
					{
						Debug.LogError($"Failed to load AssetBundle from decrypted data: {bundleName}");
						return;
					}

					// Securely erase decrypted data if required
					if (secureEraseDecryptedData && decryptedData != null)
					{
						CryptographicOperations.ZeroMemory(decryptedData);
					}

					decryptedData = null;

					this.AssetBundleDatas[bundleName] = new BundleData(loadedBundle);
				}
				else
				{
					this.AssetBundleDatas[bundleName].ReferencedCount++;
					Debug.Log($"Incremented ref count for bundle '{bundleName}' to {this.AssetBundleDatas[bundleName].ReferencedCount}.");
				}

				// If there are dependency file paths, push each dependency to the stack for processing
				if (bundleInfo.DependenceFilePaths != null && bundleInfo.DependenceFilePaths.Count > 0)
				{
					for (int index = 0; index < bundleInfo.DependenceFilePaths.Count; index++)
					{
						bundleFilePathStack.Push(bundleInfo.DependenceFilePaths[index]);
					}
				}
			}
		}

		public async void DecryptAndLoadBundleDatasAsync(bool secureEraseDecryptedData, CryptCore bundleCryptCore, DownloadedCryptedDatas downloadedCryptedDatas)
		{
			if (downloadedCryptedDatas == null)
			{
				Debug.LogError("Downloaded crypted data is null.");
				return;
			}

			// Store or update the main AssetBundle using its name as key.
			// Increment Asset Bundles Reference
			if (!this.AssetBundleDatas.ContainsKey(downloadedCryptedDatas.BundleName))
			{
				// Process each dependency bundle: decrypt and load.
				if (downloadedCryptedDatas.DependencyBundleDatas != null)
				{
					byte[] encryptedDependency = null;
					byte[] decryptedDependency = null;

					for (int index = 0; index < downloadedCryptedDatas.DependencyBundleDatas.Count; index++)
					{
						encryptedDependency = downloadedCryptedDatas.DependencyBundleDatas[index];
						decryptedDependency = await bundleCryptCore.DecryptDataAsync(encryptedDependency);
						await Task.Yield(); //Switch to UnitySynchronizationContext (Main thread)
						AssetBundle dependencyBundle = null;

						try
						{
							dependencyBundle = await LoadAssetBundleFromMemoryAsync(decryptedDependency);
						}
						catch (Exception ex)
						{
							Debug.LogError($"Load Dependency Bundle Failed: {ex.Message}");
							break;
						}

						if (secureEraseDecryptedData && decryptedDependency != null)
						{
							CryptographicOperations.ZeroMemory(decryptedDependency);
						}

						// Store or update the dependency AssetBundle using its name as key.
						// Increment Asset Bundles Reference
						if (!this.AssetBundleDatas.ContainsKey(dependencyBundle.name))
							this.AssetBundleDatas[dependencyBundle.name] = new BundleData(dependencyBundle);
						else
						{
							this.AssetBundleDatas[dependencyBundle.name].ReferencedCount++;
							Debug.Log($"Incremented reference count for dependency bundle '{dependencyBundle.name}' to {this.AssetBundleDatas[dependencyBundle.name].ReferencedCount.Value}.");
						}
					}

					Debug.Log("Loaded dependency bundle.");

					encryptedDependency = null;
					decryptedDependency = null;
				}

				// Decrypt and load the main bundle.
				byte[] decryptedMain = await bundleCryptCore.DecryptDataAsync(downloadedCryptedDatas.BundleDatas);
				await Task.Yield(); //Switch to UnitySynchronizationContext (Main thread)
				AssetBundle mainBundle = null;

				try
				{
					mainBundle = await LoadAssetBundleFromMemoryAsync(decryptedMain);
				}
				catch (Exception ex)
				{
					Debug.LogError($"Load Main Bundle Failed: {ex.Message}");
				}

				if (secureEraseDecryptedData && decryptedMain != null)
				{
					CryptographicOperations.ZeroMemory(decryptedMain);
				}

				decryptedMain = null;

				this.AssetBundleDatas[downloadedCryptedDatas.BundleName] = new BundleData(mainBundle);
			}
			else
			{
				this.AssetBundleDatas[downloadedCryptedDatas.BundleName].ReferencedCount++;
				Debug.Log($"Incremented reference count for dependency bundle '{downloadedCryptedDatas.BundleName}' to {this.AssetBundleDatas[downloadedCryptedDatas.BundleName].ReferencedCount.Value}.");
			}

			Debug.Log("Loaded main bundle: " + downloadedCryptedDatas.BundleName);
		}
	}

	public class Downloader : MonoBehaviour
	{
		/// <summary>
		/// Downloads the main encrypted bundle and its dependency bundles.
		/// Assumes that web_url is the base URL ending with a '/'.
		/// </summary>
		public IEnumerator DownloadEncryptedBundleAndDependencyDatas(string web_url, string assetName, Dictionary<string, BundleInfo> bundleInfos, Action<DownloadedCryptedDatas> onCompleted)
		{
			if (bundleInfos == null || bundleInfos.Count == 0)
			{
				Debug.LogError("Dictionary<string, BundleInfo> BundleInfos is null or empty.");
				onCompleted?.Invoke(null);
				yield break;
			}

			if (!bundleInfos.ContainsKey(assetName))
			{
				Debug.LogError($"AssetName '{assetName}' not found in bundleInfos.");
				onCompleted?.Invoke(null);
				yield break;
			}

			BundleInfo mainBundleInfo = bundleInfos[assetName];
			DownloadedCryptedDatas result = new DownloadedCryptedDatas();
			result.BundleName = mainBundleInfo.BundleName;

			// Download main encrypted bundle
			string mainUrl = web_url + mainBundleInfo.BundleName;

			// Quick existence check
			if (!URL_Tool.URL_Exists(mainUrl))
			{
				Debug.LogError($"Bundle URL not found: {mainUrl}");
				onCompleted?.Invoke(null);
				yield break;           // stop the coroutine here
			}

			using (UnityEngine.Networking.UnityWebRequest webRequest = UnityEngine.Networking.UnityWebRequest.Get(mainUrl))
			{
				yield return webRequest.SendWebRequest();
#if UNITY_2020_1_OR_NEWER
				if (webRequest.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
#else
				if (!string.IsNullOrEmpty(webRequest.error))
#endif
				{
					Debug.LogError($"Failed to download main bundle: {mainUrl} - {webRequest.error}");
					onCompleted?.Invoke(null);
					yield break;
				}
				result.BundleDatas = webRequest.downloadHandler.data;
			}

			// Process dependency encrypted bundles using a stack
			Stack<string> bundleFilePathStack = new Stack<string>();
			if (mainBundleInfo.DependenceFilePaths != null && mainBundleInfo.DependenceFilePaths.Count > 0)
			{
				for (int index = 0; index < mainBundleInfo.DependenceFilePaths.Count; index++)
				{
					bundleFilePathStack.Push(mainBundleInfo.DependenceFilePaths[index]);
				}
			}

			string dependencyAsset = null;
			string dependencyUrl = null;

			while (bundleFilePathStack.Count > 0)
			{
				dependencyAsset = bundleFilePathStack.Pop();
				if (!bundleInfos.ContainsKey(dependencyAsset))
				{
					Debug.LogError($"Dependency asset '{dependencyAsset}' not found in bundleInfos.");
					continue;
				}

				BundleInfo dependencyInfo = bundleInfos[dependencyAsset];
				dependencyUrl = web_url + dependencyInfo.BundleName;

				// Quick existence check
				if (!URL_Tool.URL_Exists(dependencyUrl))
				{
					Debug.LogError($"Bundle URL not found: {dependencyUrl}");
					onCompleted?.Invoke(null);
					yield break;           // stop the coroutine here
				}

				using (UnityEngine.Networking.UnityWebRequest dependencyRequest = UnityEngine.Networking.UnityWebRequest.Get(dependencyUrl))
				{
					yield return dependencyRequest.SendWebRequest();
#if UNITY_2020_1_OR_NEWER
					if (dependencyRequest.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
#else
					if (!string.IsNullOrEmpty(dependencyRequest.error))
#endif
					{
						Debug.LogError($"Failed to download dependency bundle: {dependencyUrl} - {dependencyRequest.error}");
						continue;
					}
					result.DependencyBundleDatas.Add(dependencyRequest.downloadHandler.data);
				}

				dependencyAsset = null;
				dependencyUrl = null;

				// If the dependency has further dependencies, push them to the stack
				if (dependencyInfo.DependenceFilePaths != null && dependencyInfo.DependenceFilePaths.Count > 0)
				{
					for (int index = 0; index < dependencyInfo.DependenceFilePaths.Count; index++)
					{
						bundleFilePathStack.Push(dependencyInfo.DependenceFilePaths[index]);
					}
				}
			}

			onCompleted?.Invoke(result);
		}
	}

	public class CryptCore : IDisposable
	{
		internal FileSecurityTool fileSecurityTool;

		// Constant-time string comparison to mitigate timing attacks
		public static bool FixedTimeEquals(string a, string b)
		{
			if (a == null || b == null || a.Length != b.Length)
			{
				return false;
			}

			int result = 0;
			for (int i = 0; i < a.Length; i++)
			{
				result |= a[i] ^ b[i];
			}
			return result == 0;
		}

		public CryptCore()
		{
			fileSecurityTool = new FileSecurityTool();
		}

		public string Password
		{
			get { return fileSecurityTool.Password; }
			set { fileSecurityTool.Password = value; }
		}

		/// <summary>
		/// Asynchronously encrypts an AssetBundle's data and returns the encrypted result.
		/// The salt is appended to the encrypted data.
		/// </summary>
		public Task<byte[]> EncryptDataAsync(byte[] data)
		{
			var tcs = new TaskCompletionSource<byte[]>();

			Task.Run
			(
				() =>
				{
					try
					{
						const int saltLength = 8;
						byte[] salt = new byte[saltLength];
						using (var rng = new RNGCryptoServiceProvider())
						{
							rng.GetBytes(salt); // Generate a random salt
						}

						var opc = FileSecurityTool.CreateAlgorithmLOPC(this.fileSecurityTool.Password, salt);
						var encryptor = opc.CreateEncryptor(opc.Key, opc.IV);
						byte[] encryptedData = FileSecurityTool.EncryptDataWithLOPC(data, encryptor);

						// Append the salt to the encrypted data
						byte[] result = new byte[encryptedData.Length + saltLength];
						Array.Copy(encryptedData, 0, result, 0, encryptedData.Length);
						Array.Copy(salt, 0, result, encryptedData.Length, saltLength);

						// Set the result when the task is successful
						tcs.SetResult(result);
					}
					catch (Exception ex)
					{
						// Set the exception instead of throwing it
						tcs.SetException(ex);
					}
				}
			);

			return tcs.Task;
		}

		/// <summary>
		/// Asynchronously decrypts encrypted bundle data and returns the decrypted result.
		/// Assumes that the salt was appended to the end of the data.
		/// </summary>
		public Task<byte[]> DecryptDataAsync(byte[] encryptedData)
		{
			var tcs = new TaskCompletionSource<byte[]>();

			Task.Run
			(
				() =>
				{
					try
					{
						const int saltLength = 8;
						if (encryptedData.Length < saltLength)
						{
							tcs.SetException(new Exception("Encrypted data is too short to contain salt."));
							return;
						}

						byte[] salt = new byte[saltLength];
						Array.Copy(encryptedData, encryptedData.Length - saltLength, salt, 0, saltLength);
						byte[] cipher = new byte[encryptedData.Length - saltLength];
						Array.Copy(encryptedData, 0, cipher, 0, cipher.Length);

						var opc = FileSecurityTool.CreateAlgorithmLOPC(this.fileSecurityTool.Password, salt);
						var decryptor = opc.CreateDecryptor(opc.Key, opc.IV);
						byte[] decryptedData = FileSecurityTool.DecryptDataWithLOPC(cipher, decryptor);

						// Set the result when the task is successful
						tcs.SetResult(decryptedData);
					}
					catch (Exception ex)
					{
						// Set the exception instead of throwing it
						tcs.SetException(ex);
					}
				}
			);

			return tcs.Task;
		}

		public void Dispose()
		{
			fileSecurityTool.Dispose();
		}
	}
}