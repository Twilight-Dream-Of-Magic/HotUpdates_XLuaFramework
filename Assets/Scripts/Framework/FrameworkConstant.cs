
public enum GameDeploymentMode
{
	EditorMode,
	PackageBundle,
	UpdateMode
}

public class FrameworkConstant
{
	public const string BundleExtension = ".ab";
	public const string VersionFileListName = "versionfile_list.txt";
	public const string ProtectedVersionFileListName = "protected_versionfile_list.json";

	public static GameDeploymentMode GDM = GameDeploymentMode.EditorMode;

	//热更新的资源地址
	public const string ResourcesURL = "http://127.0.0.1/AssetBundles";
}
