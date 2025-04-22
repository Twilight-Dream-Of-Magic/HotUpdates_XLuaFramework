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

	private string VersionFilePath = FrameworkConstant.VersionFileListName;
	public string VersionFileList
	{
		set { VersionFilePath = value; }
		get { return VersionFilePath; }
	}

	internal class DownloadFileInfo
	{
		public string URL;
		public string fileName;
		public DownloadHandler fileData;
	}

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
			yield return DownloadFile(info, callback);
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

	public void DoHotUpdateFiles()
	{
		if (this.IsRunning) 
			return;
		
		this.IsRunning = true;

		if (IsFirstInstall())
		{
			ReleaseResources();
		}
		else
		{
			CheckUpdate();
		}
	}

	// Determine if this is the first installation based on version file presence.
	private bool IsFirstInstall()
	{
		bool isExistVersionFile_ReadOnlyRoot = FileReaderWriter.IsExist(Path.Combine(this.ReadOnlyRoot, this.VersionFilePath));
		bool isExistVersionFile_ReadWriteRoot = FileReaderWriter.IsExist(Path.Combine(this.ReadWriteRoot, this.VersionFilePath));
		return isExistVersionFile_ReadOnlyRoot && !isExistVersionFile_ReadWriteRoot;
	}

	// Release initial resources by downloading the file list from the read-only root.
	private void ReleaseResources()
	{
		string url = Path.Combine(this.ReadOnlyRoot, this.VersionFilePath);
		DownloadFileInfo info = new DownloadFileInfo { URL = url };
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
		FileReaderWriter.RewriteFile(Path.Combine(rootPath, this.VersionFilePath), ReadOnlyRootFileListData);
		CheckUpdate();
	}

	// Check for updates from the server by downloading the file list.
	private void CheckUpdate()
	{
		string url = Path.Combine(FrameworkConstant.ResourcesURL, this.VersionFilePath);
		DownloadFileInfo info = new DownloadFileInfo { URL = url };
		StartCoroutine(DownloadFile(info, OnDownloadedServerFileList));
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
		ServerFileListData = fileInfo.fileData.data;
		List<DownloadFileInfo> fileInfos = GetFileList(fileInfo.fileData.text, this.VersionFilePath);
		List<DownloadFileInfo> downloadFileInfos = new List<DownloadFileInfo>();

		// Check which files are missing locally.
		for (int i = 0; i < fileInfos.Count; i++)
		{
			string localFile = Path.Combine(this.ReadWriteRoot, fileInfos[i].fileName);
			if (!FileReaderWriter.IsExist(localFile))
			{
				fileInfos[i].URL = Path.Combine(FrameworkConstant.ResourcesURL, fileInfos[i].fileName);
				downloadFileInfos.Add(fileInfos[i]);
			}
		}

		if (downloadFileInfos.Count > 0)
		{
			StartCoroutine(DownloadFile(fileInfos, OnUpdatedFile, OnUpdatedAllFile));
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
	}

	// After updating all files, write the updated file list and launch the game.
	private void OnUpdatedAllFile()
	{
		FileReaderWriter.RewriteFile(Path.Combine(this.ReadWriteRoot, this.VersionFilePath), ServerFileListData);
		LaunchGame();



		Cleanup();
	}

	// Launch the game.
	private void LaunchGame()
	{
		FrameworkManager.Resource.ParseVersionFile();
		FrameworkManager.Resource.LoadUIPrefab("Login/LoginUI", LoginUILoaded);
	}

	private void LoginUILoaded(UnityEngine.Object @object)
	{
		GameObject go = UnityEngine.Object.Instantiate(@object) as GameObject;
		go.transform.SetParent(this.transform);
		go.SetActive(true);
		go.transform.localPosition = Vector3.zero;
	}

	private void Cleanup()
	{
		Destroy(this);
	}
}
