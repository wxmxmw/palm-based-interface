using Oculus.Interaction.Body.Input;
using UnityEngine;

public sealed class TorsoLockedPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Body body;
    [SerializeField] private Transform panel;

    [Header("Placement")]
    [SerializeField] private float distance = 0.8f;
    [SerializeField] private float heightOffset = -0.05f;
    [SerializeField] private Vector3 rotationOffset;

    [Header("Torso Joint Weights")]
    [Range(0f, 1f)]
    [SerializeField] private float spineMiddleWeight = 0.45f;

    [Range(0f, 1f)]
    [SerializeField] private float spineUpperWeight = 0.40f;

    [Range(0f, 1f)]
    [SerializeField] private float chestWeight = 0.15f;

    [Header("Stabilization")]
    [Min(0.01f)]
    [SerializeField] private float positionSmoothTime = 0.18f;

    [Range(0f, 30f)]
    [SerializeField] private float directionDeadZoneDegrees = 5f;

    [Min(1f)]
    [SerializeField] private float maxDirectionDegreesPerSecond = 30f;

    [Min(0f)]
    [SerializeField] private float rotationSharpness = 8f;

    private Vector3 smoothedTorsoPosition;
    private Vector3 torsoPositionVelocity;
    private Vector3 stableTorsoForward;

    private bool hasStablePose;

    private void LateUpdate()
    {
        if (!TryGetTorsoPose(
                out Vector3 measuredTorsoPosition,
                out Vector3 measuredTorsoForward))
        {
            // 数据不可用时保持最后一次有效位置。
            return;
        }

        float deltaTime = Time.unscaledDeltaTime;

        if (!hasStablePose)
        {
            smoothedTorsoPosition = measuredTorsoPosition;
            stableTorsoForward = measuredTorsoForward;
            torsoPositionVelocity = Vector3.zero;
            hasStablePose = true;
        }
        else
        {
            // 只平滑躯干中心，不直接平滑已经带有
            // 0.8 米方向偏移的面板位置。
            smoothedTorsoPosition = Vector3.SmoothDamp(
                smoothedTorsoPosition,
                measuredTorsoPosition,
                ref torsoPositionVelocity,
                positionSmoothTime,
                Mathf.Infinity,
                deltaTime);

            float directionDifference = Vector3.Angle(
                stableTorsoForward,
                measuredTorsoForward);

            // 小范围变化视为追踪噪声。
            if (directionDifference > directionDeadZoneDegrees)
            {
                stableTorsoForward = Vector3.RotateTowards(
                    stableTorsoForward,
                    measuredTorsoForward,
                    maxDirectionDegreesPerSecond *
                    Mathf.Deg2Rad *
                    deltaTime,
                    0f);

                stableTorsoForward.Normalize();
            }
        }

        Vector3 targetPosition =
            smoothedTorsoPosition +
            stableTorsoForward * distance +
            Vector3.up * heightOffset;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                stableTorsoForward,
                Vector3.up) *
            Quaternion.Euler(rotationOffset);

        panel.position = targetPosition;

        float rotationT =
            1f - Mathf.Exp(
                -rotationSharpness * deltaTime);

        panel.rotation = Quaternion.Slerp(
            panel.rotation,
            targetRotation,
            rotationT);
    }

    private bool TryGetTorsoPose(
        out Vector3 torsoPosition,
        out Vector3 torsoForward)
    {
        torsoPosition = default;
        torsoForward = default;

        if (body == null ||
            panel == null ||
            !body.IsTrackedDataValid ||
            !body.IsHighConfidence)
        {
            return false;
        }

        if (!body.GetJointPose(
                BodyJointId.Body_SpineMiddle,
                out Pose spineMiddlePose) ||
            !body.GetJointPose(
                BodyJointId.Body_SpineUpper,
                out Pose spineUpperPose) ||
            !body.GetJointPose(
                BodyJointId.Body_Chest,
                out Pose chestPose))
        {
            return false;
        }

        float totalWeight =
            spineMiddleWeight +
            spineUpperWeight +
            chestWeight;

        if (totalWeight <= Mathf.Epsilon)
        {
            return false;
        }

        float middleWeight =
            spineMiddleWeight / totalWeight;

        float upperWeight =
            spineUpperWeight / totalWeight;

        float normalizedChestWeight =
            chestWeight / totalWeight;

        torsoPosition =
            spineMiddlePose.position * middleWeight +
            spineUpperPose.position * upperWeight +
            chestPose.position * normalizedChestWeight;

        Vector3 middleForward =
            GetHorizontalForward(spineMiddlePose.rotation);

        Vector3 upperForward =
            GetHorizontalForward(spineUpperPose.rotation);

        Vector3 chestForward =
            GetHorizontalForward(chestPose.rotation);

        if (middleForward.sqrMagnitude < 0.0001f ||
            upperForward.sqrMagnitude < 0.0001f ||
            chestForward.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        middleForward.Normalize();
        upperForward.Normalize();
        chestForward.Normalize();

        // 防止某个关节的轴偶尔翻转 180° 后抵消其他方向。
        AlignDirection(ref upperForward, middleForward);
        AlignDirection(ref chestForward, middleForward);

        torsoForward =
            middleForward * middleWeight +
            upperForward * upperWeight +
            chestForward * normalizedChestWeight;

        if (torsoForward.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        torsoForward.Normalize();
        return true;
    }

    private static Vector3 GetHorizontalForward(
        Quaternion rotation)
    {
        return Vector3.ProjectOnPlane(
            rotation * Vector3.forward,
            Vector3.up);
    }

    private static void AlignDirection(
        ref Vector3 direction,
        Vector3 reference)
    {
        if (Vector3.Dot(direction, reference) < 0f)
        {
            direction = -direction;
        }
    }

    private void OnValidate()
    {
        distance = Mathf.Max(0f, distance);
        positionSmoothTime =
            Mathf.Max(0.01f, positionSmoothTime);

        maxDirectionDegreesPerSecond =
            Mathf.Max(1f, maxDirectionDegreesPerSecond);

        rotationSharpness =
            Mathf.Max(0f, rotationSharpness);
    }
}