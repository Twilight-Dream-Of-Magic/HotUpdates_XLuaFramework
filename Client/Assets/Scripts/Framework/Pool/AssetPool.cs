using UnityEngine;

public class AssetPool : PoolBase
{
	public override UnityEngine.Object Spawn(string Name)
	{
		return base.Spawn(Name);
	}

	public override void Unspawn(string Name, UnityEngine.Object Object)
	{
		base.Unspawn(Name, Object);
	}

	public override void ReleaseByTimer()
	{
		base.ReleaseByTimer();
		for (int i = 0; i < base.Objects.Count; i++)
		{
			if (System.DateTime.Now.Ticks - base.Objects[i].LastUseTime.Ticks >= base.AutoReleaseTimeSpan * 10000000)
			{
				Debug.LogFormat("AssetPool release object time: {0}", System.DateTime.Now);
				FrameworkManager.Resource.UnloadBundle(base.Objects[i].Object);
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
