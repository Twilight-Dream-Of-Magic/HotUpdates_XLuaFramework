//#define UseRejettoHFS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Utilities;

public class HotUpdate : MonoBehaviour
{
	private bool IsRunning = false;

	private byte[] ReadOnlyRootFileListData;
	private byte[] ServerFileListData;

	private string ReadOnlyRoot;
	private string ReadWriteRoot;

	private string VersionFileList = string.Empty;
	private string VersionFileHashList = string.Empty;
	public string VersionFileName
	{
		set { VersionFileList = value; }
		get { return VersionFileList; }
	}

	public string VersionFileHashName
	{
		set { VersionFileHashList = value; }
		get { return VersionFileHashList; }
	}

	internal class DownloadFileInfo
	{
		public string URL = string.Empty;
		public string bundleName = string.Empty;
		public string fileName = string.Empty;
		public DownloadHandler fileData;
	}

	private int DownloadCount = 0;

	// Downloads a single file with retry logic and invokes the callback upon completion.
	// ¨C info: contains the URL and will hold the downloaded data on success.
	// ¨C callback: called with the filled info if successful; called with null if all retries fail.
	private IEnumerator DownloadFile(DownloadFileInfo info, Action<DownloadFileInfo> callback)
	{
		const int maxRetries = 3;

#if UseRejettoHFS
		string url = info.URL + "?dl";
#else
		string url = info.URL;
#endif

		for (int attempt = 0; attempt < maxRetries; attempt++)
		{
			// Always create a new UnityWebRequest for each attempt
			using (var webRequest = UnityWebRequest.Get(url))
			{
				yield return webRequest.SendWebRequest();

				// Debug log: show the attempt number, result, status code, and any error
				Debug.Log($"[DownloadFile] Attempt {attempt}/{maxRetries} for URL={url} ¡ú " + $"result={webRequest.result}, statusCode={webRequest.responseCode}, error={webRequest.error}");

				if (webRequest.result == UnityWebRequest.Result.Success)
				{
					// Success: store the downloaded data and invoke callback
					info.fileData = webRequest.downloadHandler;
					callback?.Invoke(info);
					yield break;
				}
				else if (attempt < maxRetries)
				{
					// Failed but we have retries left: wait a moment then retry
					Debug.LogWarning($"Download failed (attempt {attempt}). Retrying in 0.5s...");
					yield return new WaitForSeconds(0.5f);
				}
				else
				{
					// Final failure: no retries left, invoke callback with null
					Debug.LogError($"Failed to download file after {maxRetries} attempts: {info.URL}");
					webRequest.Dispose();
					callback?.Invoke(null);
				}
			}
		}
	}

	// Download multiple files sequentially, then call DownloadAllFileCallBack.
	IEnumerator DownloadFile(List<DownloadFileInfo> infos, Action<DownloadFileInfo> callback, Action downloadAllFileCallBack)
	{
		foreach (DownloadFileInfo info in infos)
		{
			if (!string.IsNullOrEmpty(info.URL))
			{
				yield return DownloadFile(info, callback);
				yield return new WaitForSeconds(0.1f);
			}
		}

		downloadAllFileCallBack?.Invoke();
	}

	// Parse the file list text and create a list of DownloadFileInfo objects.
	private List<DownloadFileInfo> FromFileList(string fileData)
	{
		string content = fileData.Trim().Replace("\r", "");
		string[] files = content.Split('\n'); //Get lines
		List<DownloadFileInfo> downloadFileInfos = new List<DownloadFileInfo>(files.Length);

		for (int i = 0; i < files.Length; i++)
		{
			string[] info = files[i].Split('|'); //Get sub-strings
			DownloadFileInfo fileInfo = new DownloadFileInfo
			{
				fileName = info[0],
				bundleName = info[1]
			};
			downloadFileInfos.Add(fileInfo);
		}

		return downloadFileInfos;
	}

	private List<DownloadFileInfo> FromFileHashList(string fileData)
	{
		string content = fileData.Trim().Replace("\r", "");
		string[] files = content.Split('\n'); //Get lines
		List<DownloadFileInfo> downloadFileInfos = new List<DownloadFileInfo>(files.Length);

		for (int i = 0; i < files.Length; i++)
		{
			string[] info = files[i].Split('|'); //Get sub-strings
			DownloadFileInfo fileInfo = new DownloadFileInfo
			{
				bundleName = info[0]
			};
			downloadFileInfos.Add(fileInfo);
		}

		return downloadFileInfos;
	}

	private Dictionary<string, string> file_hash_lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private Transform UI_RootParent;

	// MonoBehaviour Start method.
	private void Awake()
	{
		this.ReadOnlyRoot = PathUtil.GetReadOnlyRoot();
		this.ReadWriteRoot = PathUtil.GetReadableAndWriteableRoot();
		this.UI_RootParent = GameObject.Find("UI_RootCanvas").transform;
	}

	private void OnDestroy()
	{
		this.IsRunning = false;
	}

	private GameObject LoadingUIObject;
	private LoadingUIComponent LoadingUI;

	private GameObject LoginUIObject;

	public void DoHotUpdate()
	{
		FrameworkManager.Resource.LoadPrefab("Loading/LoadingUI", LoadingUILoaded);
	}

	private IEnumerator DoHotUpdateFiles()
	{
		if (this.IsRunning) 
			yield break;

		this.IsRunning = true;

		string[] paths = new string[] 
		{ 
			this.ReadOnlyRoot + this.VersionFileList,
			this.ReadWriteRoot + this.VersionFileList
		};

		// Determine if this is the first installation based on version file presence.
		var async_file_checker = new AsyncFileChecker
		(
			paths,
			(Dictionary<string, bool> results) =>
			{
				bool isFirstInstall = results[paths[0]] && !results[paths[1]];

				Debug.LogWarning($"[HotUpdate System] This {(isFirstInstall ? "is" : "is not")} considered a first installation.");
				if (isFirstInstall)
					ReleaseResources();
				else
					CheckUpdate();
			}
		);

		yield return async_file_checker.Run();
	}

	private void LoadingUILoaded(UnityEngine.Object @object)
	{
		if (System.Object.ReferenceEquals(@object, null))
		{
			return;
		}

		LoadingUIObject = UnityEngine.Object.Instantiate(@object) as GameObject;
		LoadingUI = LoadingUIObject.GetComponent<LoadingUIComponent>();
		LoadingUIObject.transform.SetParent(this.UI_RootParent);
		LoadingUIObject.transform.localPosition = Vector3.zero;
		LoadingUIObject.name = "LoadingUI";
		LoadingUIObject.SetActive(true);

		StartCoroutine(DoHotUpdateFiles());
	}

	private void LoginUILoaded(UnityEngine.Object @object)
	{
		if (System.Object.ReferenceEquals(@object, null))
		{
			return;
		}

		LoginUIObject = UnityEngine.Object.Instantiate(@object) as GameObject;
		LoginUIObject.transform.SetParent(this.UI_RootParent);
		LoginUIObject.transform.localPosition = Vector3.zero;
		LoginUIObject.name = "LoginUI";
		LoginUIObject.SetActive(true);

		Cleanup();
	}

	// Callback for when the server¡¯s VersionFileHashList comes down
	private void OnDownloadedFileHashList(DownloadFileInfo fileInfo, string rootPath)
	{
		if (fileInfo == null || fileInfo.fileData == null)
		{
			Debug.LogError("Failed to download server file-hash list.");
			Cleanup();
			return;
		}

		// Parse lines of ¡°assetPath|filehash¡±
		string text = fileInfo.fileData.text.Trim().Replace("\r", "");
		foreach (string line in text.Split('\n'))
		{
			var parts = line.Split('|');
			if (parts.Length >= 2)
			{
				file_hash_lookup[$"{rootPath}/{parts[0]}"] = parts[1];
			}
		}

		Debug.Log($"Parsed {file_hash_lookup.Count} hashes from server.");
	}

	// Callback after download of the file list from the read-only root.
	private void OnDownloadedClientFileList(DownloadFileInfo fileInfo)
	{
		if (fileInfo == null || fileInfo.fileData == null)
		{
			Debug.LogError("OnDownloadedFileList failed.");
			Cleanup();
			return;
		}

		Debug.LogFormat("OnDownloadedFileList Processing file: {0}", fileInfo.URL);

		this.DownloadCount = 0;
		ReadOnlyRootFileListData = fileInfo.fileData.data;
		List<DownloadFileInfo> file_infos = FromFileHashList(fileInfo.fileData.text);

		DownloadFileInfo main_bundle_info = new DownloadFileInfo
		{
			bundleName = PathUtil.InRootFolderPath.Replace("/", ""),
		};

		main_bundle_info.URL = $"{FrameworkConstant.HotUpdateRootURL}{PathUtil.InRootFolderPath}/{main_bundle_info.bundleName}";
		file_infos.Add(main_bundle_info);

		DownloadFileInfo main_bundle_manifest_info = new DownloadFileInfo
		{
			bundleName = PathUtil.InRootFolderPath.Replace("/", "") + ".manifest",
		};
		main_bundle_manifest_info.URL = $"{FrameworkConstant.HotUpdateRootURL}{PathUtil.InRootFolderPath}/{main_bundle_manifest_info.bundleName}";
		file_infos.Add(main_bundle_manifest_info);

		Debug.Log($"[HotUpdate] Main AssetsBundle URL  = {main_bundle_info.URL}");
		Debug.Log($"[HotUpdate] Main Manifest     URL  = {main_bundle_manifest_info.URL}");

		StartCoroutine(DownloadedClientFileListCoroutine(file_infos));
	}

	private IEnumerator DownloadedClientFileListCoroutine(List<DownloadFileInfo> file_infos)
	{
		string[] paths = file_infos.Select(info => $"{this.ReadOnlyRoot}/{info.bundleName}").ToArray();

		Dictionary<string, bool> async_file_checker_result = null;
		AsyncFileChecker async_file_checker = new AsyncFileChecker(paths, (callback_result) => async_file_checker_result = callback_result);
		yield return async_file_checker.Run();

		// Original existence and hash check
		for (int i = 0; i < file_infos.Count; i++)
		{
			string remote_hash;
			string local_path = paths[i];
			file_hash_lookup.TryGetValue(local_path, out remote_hash);

			if (async_file_checker_result.TryGetValue(local_path, out bool exists) && exists && !string.IsNullOrEmpty(remote_hash))
			{
				string local_hash = FileHashTool.ComputeMD5(FileReaderWriter.ReadExistingFile(local_path));
				if (!local_hash.Equals(remote_hash, StringComparison.OrdinalIgnoreCase))
				{
					Debug.LogErrorFormat("Local file is corrupted, Release File Stoped! The file inside the asset bundle is named: {0}", file_infos[i].fileName);
					yield break;
				}
				else
				{
					// Local Readonly -> Local ReadWrite
					string raw_file_path = $"{this.ReadOnlyRoot}/{file_infos[i].bundleName}";
					string url;

#if UNITY_ANDROID && !UNITY_EDITOR
					// Android's StreamingAssets already come with the "jar:file://...!assets" prefix
					url = raw_file_path;
#else
					// Other platforms require a file:// URI
					url = new Uri(raw_file_path).AbsoluteUri;
#endif
					file_infos[i].URL = url;
				}
			}
			else
			{
				Debug.LogErrorFormat("Local file is lost! The file inside the asset bundle is named: {0}", file_infos[i].fileName);
				yield break;
			}
		}

		LoadingUI.InitializeProgress(file_infos.Count, "Current Releasing Resource, Is Not Need Connect Network");
		yield return DownloadFile(file_infos, OnReleasedFile, OnReleasedAllFile);
	}

	// Callback after downloading the server file list.
	private void OnDownloadedServerFileList(DownloadFileInfo fileInfo)
	{
		if (fileInfo == null || fileInfo.fileData == null)
		{
			Debug.LogError("OnDownloadedServerFileList failed.");
			Cleanup();
			return;
		}

		Debug.LogFormat("OnDownloadedServerFileList Processing file: {0}", fileInfo.URL);

		this.DownloadCount = 0;
		this.ServerFileListData = fileInfo.fileData.data;
		List<DownloadFileInfo> file_infos = FromFileList(fileInfo.fileData.text);
		List<DownloadFileInfo> need_download_files = new List<DownloadFileInfo>();

		DownloadFileInfo main_bundle_info = new DownloadFileInfo
		{
			bundleName = PathUtil.InRootFolderPath.Replace("/", ""),
			URL = $"{FrameworkConstant.HotUpdateRootURL}{PathUtil.InRootFolderPath}{PathUtil.InRootFolderPath}"
		};
		need_download_files.Add(main_bundle_info);

		DownloadFileInfo main_bundle_manifest_info = new DownloadFileInfo
		{
			bundleName = PathUtil.InRootFolderPath.Replace("/", "") + ".manifest",
			URL = $"{FrameworkConstant.HotUpdateRootURL}{PathUtil.InRootFolderPath}{PathUtil.InRootFolderPath}.manifest"
		};
		need_download_files.Add(main_bundle_manifest_info);

		Debug.Log($"[HotUpdate] Main AssetsBundle URL  = {main_bundle_info.URL}");
		Debug.Log($"[HotUpdate] Main Manifest     URL  = {main_bundle_manifest_info.URL}");

		StartCoroutine(DownloadedServerFileListCoroutine(file_infos, need_download_files));
	}

	private IEnumerator DownloadedServerFileListCoroutine(List<DownloadFileInfo> file_infos, List<DownloadFileInfo> need_download_files)
	{
		string[] paths = file_infos.Select(info => $"{this.ReadWriteRoot}/{info.bundleName}").ToArray();

		Dictionary<string, bool> async_file_checker_result = null;
		AsyncFileChecker async_file_checker = new AsyncFileChecker(paths, (callback_result) => async_file_checker_result = callback_result);
		yield return async_file_checker.Run();

		// Check which files are missing locally.
		for (int i = 0; i < file_infos.Count; i++)
		{
			string remote_hash;
			string local_path = paths[i];
			file_hash_lookup.TryGetValue(local_path, out remote_hash);
			bool is_needs_update = true;

			if (async_file_checker_result.TryGetValue(local_path, out bool exists) && exists && !string.IsNullOrEmpty(remote_hash))
			{
				string local_hash = FileHashTool.ComputeMD5(FileReaderWriter.ReadExistingFile(local_path));
				is_needs_update = !local_hash.Equals(remote_hash, StringComparison.OrdinalIgnoreCase);
			}

			if (is_needs_update)
			{
				Debug.LogFormat("There are files that need to be updated! The file inside the asset bundle is named: {0}", file_infos[i].fileName);

				// Remote -> Local ReadWrite
				file_infos[i].URL = $"{FrameworkConstant.HotUpdateRootURL}{PathUtil.InRootFolderPath}/{file_infos[i].bundleName}";
				need_download_files.Add(file_infos[i]);
			}
		}

		if (need_download_files.Count > 0)
		{
			LoadingUI.InitializeProgress(need_download_files.Count, "Current File Updating ......");
			yield return DownloadFile(need_download_files, OnUpdatedFile, OnUpdatedAllFile);
		}
		else
		{
			LaunchGame();
		}

		yield break;
	}

	// Release initial resources by downloading the file list from the read-only root.
	private void ReleaseResources()
	{
		string file_hash_list_info_url = this.ReadOnlyRoot + this.VersionFileHashList;
		DownloadFileInfo file_hash_list_info = new DownloadFileInfo
		{
			URL = file_hash_list_info_url
		};

		Action<DownloadFileInfo> OnHashDownloaded = (downloadedHashInfo) =>
		{
			OnDownloadedFileHashList(downloadedHashInfo, this.ReadOnlyRoot);
			// After hash verification, fetch the actual file list
			OnDownloadedClientFileList(downloadedHashInfo);
		};

		StartCoroutine(DownloadFile(file_hash_list_info, OnHashDownloaded));
	}

	// Check for updates from the server by downloading the file list.
	private void CheckUpdate()
	{
		// 1. Construct the download info for the hash list
		string file_hash_list_info_url = $"{FrameworkConstant.HotUpdateRootURL}{PathUtil.InRootFolderPath}{this.VersionFileHashList}";
		DownloadFileInfo file_hash_list_info = new DownloadFileInfo
		{
			URL = file_hash_list_info_url
		};

		// 2. Construct the download info for the file list
		string file_list_info_url = $"{FrameworkConstant.HotUpdateRootURL}{PathUtil.InRootFolderPath}{this.VersionFileList}";
		DownloadFileInfo file_list_info = new DownloadFileInfo
		{
			URL = file_list_info_url
		};

		// 3. Callback after hash download: process the hash list first, then start downloading the file list
		Action<DownloadFileInfo> OnHashDownloaded = (downloadedHashInfo) =>
		{
			OnDownloadedFileHashList(downloadedHashInfo, this.ReadWriteRoot);
			// After hash verification, fetch the actual file list
			StartCoroutine(DownloadFile(file_list_info, OnDownloadedServerFileList));
		};

		// 4. Start downloading the hash list
		StartCoroutine(DownloadFile(file_hash_list_info, OnHashDownloaded));
	}

	// Write individual released file to disk.
	private void OnReleasedFile(DownloadFileInfo fileInfo)
	{
		string rootPath;
#if UNITY_EDITOR
		if (FrameworkConstant.GDM == GameDeploymentMode.EditorMode)
			rootPath = this.ReadOnlyRoot;
		else
			rootPath = this.ReadWriteRoot;
#else
		rootPath = ReadWriteRoot;
#endif
		FileReaderWriter.RewriteFile(rootPath + '/' + fileInfo.bundleName, fileInfo.fileData.data);
		this.DownloadCount++;
		LoadingUI.UpdateProgress(this.DownloadCount);
	}

	// After all files are released, write the file list to disk and check for updates.
	private void OnReleasedAllFile()
	{
		string rootPath;
#if UNITY_EDITOR
		if (FrameworkConstant.GDM == GameDeploymentMode.EditorMode)
			rootPath = this.ReadOnlyRoot;
		else
			rootPath = this.ReadWriteRoot;
#else
		rootPath = ReadWriteRoot;
#endif
		FileReaderWriter.RewriteFile(rootPath + this.VersionFileList, ReadOnlyRootFileListData);
		LoadingUI.InitializeProgress(0, "All File Released!");
		CheckUpdate();
	}

	// Write updated file to disk.
	private void OnUpdatedFile(DownloadFileInfo fileInfo)
	{
		FileReaderWriter.RewriteFile(this.ReadWriteRoot + '/' + fileInfo.bundleName, fileInfo.fileData.data);
		this.DownloadCount++;
		LoadingUI.UpdateProgress(this.DownloadCount);
	}

	// After updating all files, write the updated file list and launch the game.
	private void OnUpdatedAllFile()
	{
		FileReaderWriter.RewriteFile(this.ReadWriteRoot + this.VersionFileList, this.ServerFileListData);
		LoadingUI.InitializeProgress(0, "All File Updated!");
		LaunchGame();
	}

	// Launch the game.
	private void LaunchGame()
	{
		FrameworkManager.Resource.ParseVersionFile();
		FrameworkManager.Resource.LoadUIPrefab("Login/LoginUI", LoginUILoaded);
		LoadingUIObject.SetActive(false);
	}

	private void Cleanup()
	{
		Destroy(LoadingUIObject);
		Destroy(this);
	}
}
