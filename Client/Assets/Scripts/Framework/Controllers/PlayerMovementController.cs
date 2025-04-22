// RigidbodyPlayer.cs
using UnityEngine;

/// <summary>
/// RigidbodyPlayer.cs
/// 根据键盘原始输入（无平滑）移动玩家刚体，沿摄像机朝向前进  
/// Moves the player Rigidbody based on raw keyboard input, aligned to the camera’s horizontal direction.
///
/// 用法／Usage:  
/// 1. 挂到含有 Rigidbody 和 Collider 的玩家角色根对象上。  
///    Attach to the same GameObject that has your Rigidbody and Collider.  
/// 2. 确保场景中有 Tag 为 MainCamera 的摄像机。  
///    Make sure a Camera tagged "MainCamera" exists in the scene.  
/// 3. 在 Inspector 中调整 movementSpeed 参数。  
///    Tweak `movementSpeed` in the Inspector.  
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class RigidbodyPlayer : MonoBehaviour
{
	[Header("移动设置 / Movement Settings")]
	[Tooltip("移动速度（米/秒）\nMovement speed (units per second).")]
	public float movementSpeed = 20f;

	private Rigidbody rigidBodyComponent;
	private Transform mainCameraTransform;

	private void Start()
	{
		// 缓存刚体，冻结 X/Z 轴旋转，启用连续碰撞检测，确保重力开启  
		// Cache Rigidbody, freeze X/Z rotations, enable continuous collision detection, ensure gravity is on
		rigidBodyComponent = GetComponent<Rigidbody>();
		rigidBodyComponent.constraints = RigidbodyConstraints.FreezeRotationX
									   | RigidbodyConstraints.FreezeRotationZ;
		rigidBodyComponent.collisionDetectionMode = CollisionDetectionMode.Continuous;
		rigidBodyComponent.useGravity = true;

		// 缓存主摄像机 Transform，用于计算移动方向  
		// Cache main camera transform for movement direction
		if (!System.Object.ReferenceEquals(Camera.main, null))
		{
			mainCameraTransform = Camera.main.transform;
		}
		else
		{
			Debug.LogError("Main Camera not found! 确保场景中有 Tag 为 MainCamera 的摄像机。");
		}
	}

	private void FixedUpdate()
	{
		// 读取原始轴值，无内置平滑，保证松开按键时立即停止  
		// Read raw input to stop immediately when keys are released
		float horizontalInput = Input.GetAxisRaw("Horizontal");
		float verticalInput = Input.GetAxisRaw("Vertical");

		// 计算摄像机的水平面前向和右向向量  
		// Project camera forward/right onto the horizontal plane
		Vector3 cameraForward = mainCameraTransform.forward;
		Vector3 cameraRight = mainCameraTransform.right;
		cameraForward.y = 0f;
		cameraRight.y = 0f;
		cameraForward.Normalize();
		cameraRight.Normalize();

		// 根据输入计算目标移动方向  
		// Calculate movement direction from input
		Vector3 movementDirection = cameraForward * verticalInput + cameraRight * horizontalInput;
		if (movementDirection.sqrMagnitude > 1f)
		{
			movementDirection.Normalize(); // 对角方向归一化，防止速度叠加
		}

		// 构造新速度：水平速度 = movementDirection * movementSpeed，垂直速度保留原来的 y 分量（重力/下落）  
		// Build new velocity: horizontal from movementDirection, preserve original vertical velocity (gravity/fall)
		Vector3 newVelocity = movementDirection * movementSpeed + Vector3.up * rigidBodyComponent.velocity.y;

		// 应用到刚体  
		// Apply to Rigidbody
		rigidBodyComponent.velocity = newVelocity;
	}
}
