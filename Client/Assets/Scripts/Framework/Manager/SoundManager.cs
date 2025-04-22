using UnityEngine;

public class SoundManager : MonoBehaviour
{
	[SerializeField]
	private AudioSource Music;
	[SerializeField]
	private AudioSource Sound;

	private float MusicVolume
	{
		get { return PlayerPrefs.GetFloat("MusicVolume", 1.0f); }
		set
		{
			Music.volume = value;
			PlayerPrefs.SetFloat("MusicVolume", value);
		}
	}

	private float SoundVolume
	{
		get { return PlayerPrefs.GetFloat("SoundVolume", 1.0f); }
		set
		{
			Sound.volume = value;
			PlayerPrefs.SetFloat("SoundVolume", value);
		}
	}

	private void Awake()
	{
		Music = this.gameObject.AddComponent<AudioSource>();
		Music.playOnAwake = false;
		Music.loop = true;

		Sound = this.gameObject.AddComponent<AudioSource>();
		Sound.loop = false;
	}

	public void PlayMusic(string Name)
	{
		if (this.MusicVolume < 0.1f)
			return;
		string oldName = "";
		if (!System.Object.ReferenceEquals(Music.clip, null))
			oldName = Music.clip.name;

		if (oldName == Name)
		{
			Music.Play();
			return;
		}

		FrameworkManager.Resource.LoadMusic
		(
			Name,
			(UnityEngine.Object obj) =>
			{
				Music.clip = obj as AudioClip;
				Music.Play();
			}
		);
	}

	public void PauseMusic()
	{
		Music.Pause();
	}

	public void UnPauseMusic()
	{
		Music.UnPause();
	}

	public void StopMusic()
	{
		Music.Stop();
	}

	public void PlaySound(string Name)
	{
		if (this.SoundVolume < 0.1f)
			return;

		FrameworkManager.Resource.LoadSound
		(
			Name,
			(UnityEngine.Object obj) =>
			{
				Sound.PlayOneShot(obj as AudioClip);
			}
		);
	}

	public void SetMusicVolume(float value)
	{
		this.MusicVolume = value;
	}

	public void SetSoundVolume(float value)
	{
		this.SoundVolume = value;
	}
}
