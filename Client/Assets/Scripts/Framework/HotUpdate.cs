using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
		public string fileName = string.Empty;
		public string fileHash = string.Empty;
		public DownloadHandler fileData;
	}

	private int DownloadCount = 0;

	// Download a single file and call the callback after finishing.
	IEnumerator DownloadFile(DownloadFileInfo info, Action<DownloadFileInfo> callback)
	{
		int retryCount = 3;  // Maximum number of retries
		bool downloadSuccessful = false;

		UnityWebRequest webRequest = UnityWebRequest.Get(info.URL);

		while (retryCount > 0 && !downloadSuccessful)
		{
			yield return webRequest.SendWebRequest();

			bool hasError = webRequest.result == UnityWebRequest.Result.ProtocolError ||
							webRequest.result == UnityWebRequest.Result.ConnectionError ||
							webRequest.result == UnityWebRequest.Result.DataProcessingError;

			if (hasError)
			{
				// Log the error and decrease retry count
				Debug.LogError($"Download file error: {info.URL}. Retries left: {retryCount - 1}");
				retryCount--; // Decrease the retry count

				yield return null;

				// If retries are exhausted, exit the loop
				if (retryCount == 0)
				{
					Debug.LogError($"Failed to download file after 3 attempts: {info.URL}");
					webRequest.Dispose();
					callback?.Invoke(null);
					yield break;  // Exit and don't call the callback
				}
			}
			else
			{
				// Download was successful
				downloadSuccessful = true;
				info.fileData = webRequest.downloadHandler;
				callback?.Invoke(info);  // Call the callback
			}
		}

		webRequest.Dispose();
	}

	// Download multiple files sequentially, then call DownloadAllFileCallBack.
	IEnumerator DownloadFile(List<DownloadFileInfo> infos, Action<DownloadFileInfo> callback, Action downloadAllFileCallBack)
	{
		foreach (DownloadFileInfo info in infos)
		{
			if (!string.IsNullOrEmpty(info.URL) || !string.IsNullOrEmpty(info.fileHash))
			{
				yield return DownloadFile(info, callback);
			}
		}

		downloadAllFileCallBack?.Invoke();
	}

	// Parse the file list text and create a list of DownloadFileInfo objects.
	private List<DownloadFileInfo> GetFileList(string fileData, string path)
	{
		string content = fileData.Trim().Replace("\r", "");
		string[] files = content.Split('\n'); //Get lines
		List<DownloadFileInfo> downloadFileInfos = new List<DownloadFileInfo>(files.Length);

		for (int i = 0; i < files.Length; i++)
		{
			string[] info = files[i].Split('|'); //Get sub-strings
			DownloadFileInfo fileInfo = new DownloadFileInfo
			{
				fileName = info[1],
				URL = Path.Combine(path, info[1])
			};
			downloadFileInfos.Add(fileInfo);
		}

		return downloadFileInfos;
	}

	private Dictionary<string, string> fileHashLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	// MonoBehaviour Start method.
	private void Start()
	{
		this.ReadOnlyRoot = PathUtil.GetReadOnlyRoot();
		this.ReadWriteRoot = PathUtil.GetReadableAndWriteableRoot();
	}

	private void OnDestroy()
	{
		this.IsRunning = false;
	}

	private GameObject LoadingUIObject;
	private LoadingUIComponent LoadingUI;

	private GameObject LoginUIObject;

	public void DoHotUpdateFiles()
	{
		if (this.IsRunning) 
			return;
		
		this.IsRunning = true;

		FrameworkManager.Resource.LoadPrefab("Loading/LoadingUI", LoadingUILoaded);

		if (IsFirstInstall())
		{
			ReleaseResources();
		}
		else
		{
			CheckUpdate();
		}
	}

	private void LoadingUILoaded(UnityEngine.Object @object)
	{
		if (System.Object.ReferenceEquals(@object, null))
		{
			return;
		}

		LoadingUIObject = UnityEngine.Object.Instantiate(@object) as GameObject;
		LoadingUI = LoadingUIObject.GetComponent<LoadingUIComponent>();
		LoadingUIObject.transform.SetParent(this.transform);
		LoginUIObject.transform.localPosition = Vector3.zero;
		LoginUIObject.SetActive(true);
	}

	private void LoginUILoaded(UnityEngine.Object @object)
	{
		if (System.Object.ReferenceEquals(@object, null))
		{
			return;
		}

		LoginUIObject = UnityEngine.Object.Instantiate(@object) as GameObject;
		LoginUIObject.transform.SetParent(GameObject.Find("UI_RootCanvas").transform);
		LoginUIObject.transform.localPosition = Vector3.zero;
		LoginUIObject.name = "LoginUI";
		LoginUIObject.SetActive(true);

		Cleanup();
	}

	// Determine if this is the first installation based on version file presence.
	private bool IsFirstInstall()
	{
		bool isExistVersionFile_ReadOnlyRoot = FileReaderWriter.IsExist(Path.Combine(this.ReadOnlyRoot, this.VersionFileList));
		bool isExistVersionFile_ReadWriteRoot = FileReaderWriter.IsExist(Path.Combine(this.ReadWriteRoot, this.VersionFileList));
		return isExistVersionFile_ReadOnlyRoot && !isExistVersionFile_ReadWriteRoot;
	}

	// Release initial resources by downloading the file list from the read-only root.
	private void ReleaseResources()
	{
		string url = Path.Combine(this.ReadOnlyRoot, this.VersionFileList);
		DownloadFileInfo info = new DownloadFileInfo
		{
			URL = url
		};
		StartCoroutine(DownloadFile(info, OnDownloadedFileList));
	}

	// Callback after download of the file list from the read-only root.
	private void OnDownloadedFileList(DownloadFileInfo fileInfo)
	{
		if (fileInfo == null || fileInfo.fileData == null)
		{
			Debug.LogError("OnDownloadedFileList failed.");
			Cleanup();
			return;
		}

		Debug.LogFormat("OnDownloadedFileList Processing file: {0}", fileInfo.URL);
		ReadOnlyRootFileListData = fileInfo.fileData.data;
		List<DownloadFileInfo> fileInfos = GetFileList(fileInfo.fileData.text, this.ReadOnlyRoot);
		StartCoroutine(DownloadFile(fileInfos, OnReleasedFile, OnReleasedAllFile));
		LoadingUI.InitializeProgress(fileInfos.Count, "Current Releasing Resource, Is Not Need Connect Network");
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
		FileReaderWriter.RewriteFile(Path.Combine(rootPath, fileInfo.fileName), fileInfo.fileData.data);
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
		FileReaderWriter.RewriteFile(Path.Combine(rootPath, this.VersionFileList), ReadOnlyRootFileListData);
		CheckUpdate();
	}

	// Check for updates from the server by downloading the file list.
	private void CheckUpdate()
	{
		//Download the version file list
		string file_url = Path.Combine(FrameworkConstant.ResourcesURL, this.VersionFileList);
		DownloadFileInfo file_info = new DownloadFileInfo 
		{ 
			URL = file_url 
		};

		//Download the version file hash list and populate our lookup
		string file_hash_url = Path.Combine(FrameworkConstant.ResourcesURL, this.VersionFileHashList);
		DownloadFileInfo file_hash_info = new DownloadFileInfo 
		{
			URL = file_hash_url
		};

		Action<DownloadFileInfo> file_hash_url_callback = (downloaded_file_hash_info) =>
		{
			OnDownloadedServerFileHashList(downloaded_file_hash_info);
			StartCoroutine(DownloadFile(file_info, OnDownloadedServerFileList));
		};

		StartCoroutine(DownloadFile(file_hash_info, file_hash_url_callback));
	}

	// Callback for when the server¡¯s VersionFileHashList comes down
	private void OnDownloadedServerFileHashList(DownloadFileInfo fileInfo)
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
				fileHashLookup[parts[0]] = parts[1];
		}

		Debug.Log($"Parsed {fileHashLookup.Count} hashes from server.");
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
		List<DownloadFileInfo> fileInfos = GetFileList(fileInfo.fileData.text, this.VersionFileList);
		List<DownloadFileInfo> needDownloadFile = new List<DownloadFileInfo>();

		// Check which files are missing locally.
		for (int i = 0; i < fileInfos.Count; i++)
		{
			string localPath = Path.Combine(ReadWriteRoot, fileInfos[i].fileName);
			string remoteHash;
			fileHashLookup.TryGetValue(fileInfos[i].fileName, out remoteHash);
			bool needsUpdate = true;

			if (FileReaderWriter.IsExist(localPath) && !string.IsNullOrEmpty(remoteHash))
			{
				string localHash = FileHashTool.ComputeMD5(FileReaderWriter.ReadExistingFile(localPath));
				needsUpdate = !localHash.Equals(remoteHash, StringComparison.OrdinalIgnoreCase);
			}

			if (needsUpdate)
			{
				fileInfos[i].URL = Path.Combine(FrameworkConstant.ResourcesURL, fileInfos[i].fileName);
				needDownloadFile.Add(fileInfos[i]);
			}
		}

		if (needDownloadFile.Count > 0)
		{
			StartCoroutine(DownloadFile(needDownloadFile, OnUpdatedFile, OnUpdatedAllFile));
			LoadingUI.InitializeProgress(needDownloadFile.Count, "Current File Updating ......");
		}
		else
		{
			LaunchGame();
		}
	}

	// Write updated file to disk.
	private void OnUpdatedFile(DownloadFileInfo fileInfo)
	{
		FileReaderWriter.RewriteFile(Path.Combine(this.ReadWriteRoot, fileInfo.fileName), fileInfo.fileData.data);
		this.DownloadCount++;
		LoadingUI.UpdateProgress(this.DownloadCount);
	}

	// After updating all files, write the updated file list and launch the game.
	private void OnUpdatedAllFile()
	{
		FileReaderWriter.RewriteFile(Path.Combine(this.ReadWriteRoot, this.VersionFileList), this.ServerFileListData);
		LaunchGame();
		LoadingUI.InitializeProgress(0, "All Updated!");
	}

	// Launch the game.
	private void LaunchGame()
	{
		FrameworkManager.Resource.ParseVersionFile();
		FrameworkManager.Resource.LoadUIPrefab("Login/LoginUI", LoginUILoaded);
	}

	private void Cleanup()
	{
		Destroy(this);
	}
}
