using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace Utilities
{
	/// <summary>
	/// General async file existence checker:
	/// Given an array of full_paths, checks each one on the current platform
	/// (Editor/PC or Android/iOS), and finally returns all results in one callback.
	/// Supports both local file paths and remote URLs.
	/// </summary>
	public class AsyncFileChecker
	{
		private readonly string[] _full_paths;
		private readonly Action<Dictionary<string, bool>> _on_result_callback;

		public AsyncFileChecker(string[] full_paths, Action<Dictionary<string, bool>> on_result_callback)
		{
			_full_paths = full_paths;
			_on_result_callback = on_result_callback;
		}

		public IEnumerator Run()
		{
			Dictionary<string, bool> results = new Dictionary<string, bool>(_full_paths.Length);

			for (int i = 0; i < _full_paths.Length; i++)
			{
				string raw_path = _full_paths[i];
				bool exists = false;

				Uri uri;
				bool is_url = Uri.TryCreate(raw_path, UriKind.Absolute, out uri)
							  && (uri.Scheme == Uri.UriSchemeHttp
								  || uri.Scheme == Uri.UriSchemeHttps
								  || uri.Scheme == Uri.UriSchemeFtp);

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
			// On mobile platforms we always go through UnityWebRequest
			using (var uwr = new UnityWebRequest(uri.AbsoluteUri, UnityWebRequest.kHttpVerbHEAD))
			{
				uwr.downloadHandler = new DownloadHandlerBuffer();
				yield return uwr.SendWebRequest();
				exists = (uwr.result == UnityWebRequest.Result.Success);
			}
#else
				if (is_url)
				{
					// Remote URL: HEAD request
					using (var uwr = new UnityWebRequest(uri.AbsoluteUri, UnityWebRequest.kHttpVerbHEAD))
					{
						uwr.downloadHandler = new DownloadHandlerBuffer();
						yield return uwr.SendWebRequest();
						exists = (uwr.result == UnityWebRequest.Result.Success);
					}
				}
				else
				{
					// Local file: synchronous existence check
					string file_path = (uri != null && uri.IsFile) ? uri.LocalPath : raw_path;
					exists = FileReaderWriter.IsExist(file_path);
				}
#endif

				results[raw_path] = exists;
			}

			_on_result_callback?.Invoke(results);
			yield break;
		}
	}
}

