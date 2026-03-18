using UnityEngine;

/// <summary>
/// Shared state ScriptableObject between RPGCinemachineInput and RPGPlayerController.
/// Create one via Assets → Create → RPG / Camera State and assign it to both scripts.
/// </summary>
[CreateAssetMenu(fileName = "CameraState", menuName = "RPG/Camera State")]
public class CameraState : ScriptableObject
{
    /// <summary>Current camera yaw in degrees — written by RPGCinemachineInput.</summary>
    public float CameraYaw;

    /// <summary>True while RMB is held (freelook). Player should not rotate while this is true.</summary>
    public bool IsFreelooking;
}