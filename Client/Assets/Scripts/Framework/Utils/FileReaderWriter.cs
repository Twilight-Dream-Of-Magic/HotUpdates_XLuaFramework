using System;
using System.IO;
using UnityEngine;

namespace Utilities
{
	/// <summary>
	/// Provides methods to read and write files with fine control over progress and overwrite behavior.
	/// </summary>
	public static class FileReaderWriter
	{
		private const int BufferSize = 81920; // 80 KB

		public static bool IsExist(string path)
		{
			FileInfo file = new FileInfo(path);
			return file.Exists;
		}

		/// <summary>
		/// Reads an existing file in chunks and returns its content as a byte array.
		/// </summary>
		/// <param name="inputFile">Path to the existing file.</param>
		/// <param name="progressCallback">Optional callback receiving (bytesRead, totalBytes).</param>
		/// <returns>Byte array containing file data.</returns>
		public static byte[] ReadExistingFile(string inputFile, Action<long, long> progressCallback = null)
		{
			if (!File.Exists(inputFile))
				throw new FileNotFoundException("Input file not found.", inputFile);

			try
			{
				using (var fs = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					using (var ms = new MemoryStream())
					{
						long totalLength = fs.Length;
						long totalRead = 0;
						byte[] buffer = new byte[BufferSize];
						int bytesRead;

						while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
						{
							ms.Write(buffer, 0, bytesRead);
							totalRead += bytesRead;
							progressCallback?.Invoke(totalRead, totalLength);
						}

						return ms.ToArray();
					}
				}
			}
			catch (IOException ioException)
			{
				Debug.LogError(ioException.Message);
				return null;
			}
		}

		/// <summary>
		/// Writes data to a new file. Throws if the file already exists.
		/// </summary>
		/// <param name="outputFile">Path to the new file.</param>
		/// <param name="data">Data to write.</param>
		/// <param name="progressCallback">Optional callback receiving (bytesWritten, totalBytes).</param>
		public static void WriteNewFile(string outputFile, byte[] data, Action<long, long> progressCallback = null)
		{
			if (File.Exists(outputFile))
				throw new IOException($"Output file '{outputFile}' already exists.");

			// FileMode.CreateNew throws IOException if file exists as well
			WriteInternal(outputFile, data, FileMode.CreateNew, progressCallback);
		}

		/// <summary>
		/// Writes data to a file, overwriting if it already exists.
		/// </summary>
		/// <param name="outputFile">Path to the file.</param>
		/// <param name="data">Data to write.</param>
		/// <param name="progressCallback">Optional callback receiving (bytesWritten, totalBytes).</param>
		public static void WriteOrOverwriteFile(string outputFile, byte[] data, Action<long, long> progressCallback = null)
		{
			// FileMode.Create creates or overwrites
			WriteInternal(outputFile, data, FileMode.Create, progressCallback);
		}

		/// <summary>
		/// Rewrites the file at the specified path by deleting the existing file (if any) and writing new data.
		/// Ensures that the necessary directory exists before writing.
		/// </summary>
		/// <param name="path">The file path to rewrite.</param>
		/// <param name="data">The byte array containing the new data to write.</param>
		public static void RewriteFile(string path, byte[] data)
		{
			path = PathUtil.GetStandardPath(path);
			string directory = path.Substring(0, path.LastIndexOf("/"));
			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			FileInfo file = new FileInfo(path);
			if (file.Exists)
			{
				file.Delete();
			}

			try
			{
				using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
				{
					fs.Write(data, 0, data.Length);
				}
			}
			catch (IOException ioException)
			{
				Debug.LogError(ioException.Message);
			}
		}

		/// <summary>
		/// Appends data to a file, creating it if it does not exist.
		/// </summary>
		/// <param name="outputFile">Path to the file.</param>
		/// <param name="data">Data to append.</param>
		/// <param name="progressCallback">Optional callback receiving (bytesWritten, totalBytes).</param>
		public static void AppendToFile(string outputFile, byte[] data, Action<long, long> progressCallback = null)
		{
			WriteInternal(outputFile, data, FileMode.Append, progressCallback);
		}

		/// <summary>
		/// Internal helper for writing data in chunks.
		/// </summary>
		private static void WriteInternal(string path, byte[] data, FileMode mode, Action<long, long> progressCallback)
		{
			path = PathUtil.GetStandardPath(path);
			try
			{
				using (var fs = new FileStream(path, mode, FileAccess.Write, FileShare.None, BufferSize, FileOptions.SequentialScan))
				{
					long total = data.LongLength;
					long written = 0;

					while (written < total)
					{
						int chunkSize = (int)Math.Min(BufferSize, total - written);
						fs.Write(data, (int)written, chunkSize);
						written += chunkSize;
						progressCallback?.Invoke(written, total);
					}

					fs.Flush(true);
				}
			}
			catch (IOException ioException)
			{
				Debug.LogError(ioException.Message);
			}
		}
	}
}
