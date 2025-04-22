using UnityEngine;
using UnityEngine.UI;

public class LoadingUIComponent : MonoBehaviour
{
	[SerializeField]
	private Image ProgressValue;

	[SerializeField]
	private Text ProgressText;

	[SerializeField]
	private GameObject ProgressBar;

	[SerializeField]
	private Text ProgressDescription;

	private float Max;

	internal void InitializeProgress(float max, string description)
	{
		this.Max = max;
		this.ProgressBar.SetActive(true);
		this.ProgressDescription.gameObject.SetActive(true);
		this.ProgressDescription.text = description;
		this.ProgressValue.fillAmount = max > 0 ? 0 : 100;
		this.ProgressText.gameObject.SetActive(max > 0);
	}

	public void UpdateProgress(float progress)
	{
		this.ProgressValue.fillAmount = progress / this.Max;
		this.ProgressText.text = string.Format("{0:0}%", this.ProgressValue.fillAmount * 100);
	}
}
