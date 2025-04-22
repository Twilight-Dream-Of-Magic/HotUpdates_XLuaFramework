using UnityEngine;

public class GameObjectPool : PoolBase
{
	public override UnityEngine.Object Spawn(string Name)
	{
		UnityEngine.Object @object = base.Spawn(Name);
		if(@object == null)
		{
			return null;
		}

		GameObject gameObject = @object as GameObject;
		gameObject.SetActive(true);
		return gameObject;
	}

	public override void Unspawn(string Name, UnityEngine.Object Object)
	{
		GameObject gameObject = @Object as GameObject;
		gameObject.SetActive(false);
		gameObject.transform.SetParent(this.transform, false);
		base.Unspawn(Name, Object);
	}

	public override void Release(UnityEngine.Object @Object)
	{
		base.Release(@Object);
		for (int i = 0; i < this.Objects.Count; i++)
		{
			GameObject LeftGameObject = this.Objects[i].Object as GameObject;
			GameObject RightGameObject = @Object as GameObject;
			if (LeftGameObject.name == RightGameObject.name && LeftGameObject.tag == RightGameObject.tag && LeftGameObject.GetInstanceID() == RightGameObject.GetInstanceID())
			{
				Destroy(LeftGameObject);
				FrameworkManager.Resource.DecrementAssetBundleReference(base.Objects[i].Name);
				this.Objects.Remove(this.Objects[i]);
				return;
			}
		}

		if(this.Objects.Count == 0 && !System.Object.ReferenceEquals(@Object, null))
		{
			Destroy(@Object);
		}
	}

	public override void ReleaseByTimer()
	{
		base.ReleaseByTimer();
		for (int i = 0; i < base.Objects.Count; i++)
		{
			if (System.DateTime.Now.Ticks - base.Objects[i].LastUseTime.Ticks >= base.AutoReleaseTimeSpan * 10000000)
			{
				Debug.LogFormat("GameObjectPool release object time: {0}", System.DateTime.Now);
				Destroy(base.Objects[i].Object);
				FrameworkManager.Resource.DecrementAssetBundleReference(base.Objects[i].Name);
				base.Objects.Remove(base.Objects[i]);
				return;
			}
		}
	}

	public override void ReleaseAll()
	{
		base.ReleaseAll();
		for (int i = 0; i < base.Objects.Count; i++)
		{
			Destroy(base.Objects[i].Object);
			base.Objects.Remove(base.Objects[i]);
		}
	}
}
