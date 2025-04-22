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
	private Text ProgressTitleDescription;

	private float Max;

	private Slider Slider;

	internal void InitializeProgress(float max, string description)
	{
		if (System.Object.ReferenceEquals(Slider, null))
		{
			Slider = ProgressBar.GetComponent<Slider>();
		}

		this.Max = max;
		ProgressBar.SetActive(true);
		ProgressTitleDescription.text = description;

		if (max > 0f)
		{
			ProgressValue.fillAmount = 0f;
			ProgressText.gameObject.SetActive(true);
			ProgressText.text = "0%";
			Slider.value = 0f;
		}
		else
		{
			ProgressValue.fillAmount = 0f;
			Slider.value = 0f;
			ProgressText.gameObject.SetActive(false);
		}
	}

	internal void UpdateProgress(float progress)
	{
		if (Max <= 0f)
			return;

		float ratio = progress / Max;
		ProgressValue.fillAmount = ratio;
		Slider.value = ratio;
		ProgressText.text = $"{ratio * 100f:0}%";
	}
}
