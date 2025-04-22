// MouseLookController.cs
using UnityEngine;

/// <summary>
/// MouseLookController.cs
/// 控制玩家视角：类似 Minecraft 的第一人称视角操作
/// Handles first‑person camera look based on mouse movement, similar to Minecraft.
/// 
/// 用法／Usage:
/// 1. 挂到含有 Rigidbody 和 Collider 的玩家角色根对象上。  
///    Attach this to the same GameObject that has your Rigidbody and Collider.
/// 2. 在该物体下创建一个名为 "CameraPivot" 的子对象，并将 Camera 挂到它下面。  
///    Create a child GameObject named "CameraPivot" and put your Camera under it.
/// 3. 调整 Inspector 中的灵敏度／死区／俯仰角限制等参数。  
///    Tweak sensitivity, dead zone and pitch limits in the Inspector.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class MouseLookController : MonoBehaviour
{
	[Header("灵敏度／Sensitivity")]
	[Tooltip("鼠标灵敏度系数，越大视角转动越快\nMouse sensitivity multiplier.")]
	public float mouseSensitivity = 50f;

	[Header("俯仰角限制／Pitch Limits")]
	[Tooltip("俯仰角最小值（向下看）\nMinimum pitch angle (looking down).")]
	public float minimumPitch = -30f;
	[Tooltip("俯仰角最大值（向上看）\nMaximum pitch angle (looking up).")]
	public float maximumPitch = 60f;

	[Header("抖动过滤／Dead Zone")]
	[Tooltip("输入小于此值的鼠标抖动将被忽略\nMouse input below this magnitude will be discarded.")]
	public float deadZone = 0.02f;

	private Rigidbody rb;
	private Transform cameraPivot;
	private float pitch = 20f;

	// 暂存从 Update 读取的原始鼠标输入（去平滑）
	// Stores raw mouse input read in Update (no smoothing)
	private Vector2 lookInput;

	private void Start()
	{
		// 获取并配置刚体
		// Cache Rigidbody and freeze unwanted rotations
		rb = GetComponent<Rigidbody>();
		rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

		// 查找 CameraPivot 子物体
		// Find the child object used for pitching the camera
		cameraPivot = transform.Find("CameraPivot");
		if (System.Object.ReferenceEquals(cameraPivot , null))
		{
			Debug.LogError("CameraPivot not found! 请创建一个名为 CameraPivot 的子对象。");
		}
		else
		{
			// 初始化俯仰角
			// Initialize pitch angle from current local rotation
			pitch = cameraPivot.localEulerAngles.x;
		}

		// 锁定并隐藏鼠标光标
		// Lock and hide the cursor
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	private void Update()
	{
		// 使用平滑的鼠标输入（Input.GetAxis）
		float rawX = Input.GetAxis("Mouse X");
		float rawY = Input.GetAxis("Mouse Y");

		// 应用死区过滤
		lookInput.x = Mathf.Abs(rawX) > deadZone ? rawX : 0f;
		lookInput.y = Mathf.Abs(rawY) > deadZone ? rawY : 0f;
	}

	private void FixedUpdate()
	{
		// Yaw: 角色水平旋转
		if (lookInput.x != 0f)
		{
			float yawDelta = lookInput.x * mouseSensitivity * Time.fixedDeltaTime;
			Quaternion deltaRot = Quaternion.Euler(0f, yawDelta, 0f);
			rb.MoveRotation(rb.rotation * deltaRot);
		}

		// Pitch: 更新摄像头俯仰角
		if (lookInput.y != 0f)
		{
			pitch -= lookInput.y * mouseSensitivity * Time.fixedDeltaTime;
			pitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch);
			cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
		}
	}
}
