using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.UI;

public class PalmMenuController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Hand rightHand;
    [SerializeField] private Camera headCamera;
    [SerializeField] private GameObject menuRoot;

    [Header("Gesture Timing")]
    [Min(0.01f)]
    [SerializeField] private float holdSeconds = 0.7f;

    [Min(0f)]
    [SerializeField] private float cooldownSeconds = 1.0f;

    [Header("Gesture Conditions")]
    [Tooltip("手腕相对 HMD 的最低高度")]
    [SerializeField] private float minHeightAboveHead = -0.3f;

    [Range(0f, 90f)]
    [SerializeField] private float palmUpMaxAngle = 45f;

    [Tooltip("手指伸直程度：1 为完全笔直，数值越小越宽松。")]
    [Range(0.5f, 1f)]
    [SerializeField] private float openHandThreshold = 0.85f;

    [Header("Reserved For Step 4")]
    [Min(0.1f)]
    [SerializeField] private float menuDistance = 0.8f;

    [Header("Gesture Progress")]
    [SerializeField] private GameObject gestureProgressRoot;
    [SerializeField] private Image gestureProgressFill;

    public bool IsMenuVisible { get; private set; }
    public float MenuDistance => menuDistance;

    private float gestureTimer;
    private float cooldownEndTime;

    // 切换以后必须观察到一次真正的“姿势释放”。
    private bool waitingForGestureRelease;

    private void Awake()
    {

        IsMenuVisible = true;

        if (menuRoot != null)
        {
            menuRoot.SetActive(true);
        }
        ResetGestureProgress();
    }

    private void Update()
    {
        // 追踪丢失时只清空计时，不把它当作用户主动松手。
        if (!IsRightHandTracked())
        {
            ResetGestureProgress();
            return;
        }

        bool gestureValid = IsToggleGestureValid();

        if (!gestureValid)
        {
            ResetGestureProgress();
            waitingForGestureRelease = false;
            return;
        }

        // 上一次切换后，持续保持相同手势不能再次触发。
        if (waitingForGestureRelease)
        {
            ResetGestureProgress();
            return;
        }

        // 冷却期内忽略姿势，不累计时间。
        if (Time.unscaledTime < cooldownEndTime)
        {
            ResetGestureProgress();
            return;
        }

        gestureTimer += Time.unscaledDeltaTime;

        float progress = Mathf.Clamp01(
        gestureTimer / holdSeconds
        );

        ShowGestureProgress(progress);

        if (gestureTimer >= holdSeconds)
        {
            ToggleMenu();

            gestureTimer = 0f;
            waitingForGestureRelease = true;
            cooldownEndTime = Time.unscaledTime + cooldownSeconds;
            ResetGestureProgress();
        }
    }

    private bool IsRightHandTracked()
    {
        return rightHand != null
               && rightHand.Handedness == Handedness.Right
               && rightHand.IsConnected
               && rightHand.IsTrackedDataValid
               && rightHand.IsHighConfidence;
    }

    private bool IsToggleGestureValid()
    {
        if (headCamera == null)
        {
            return false;
        }

        if (!rightHand.GetJointPose(
                HandJointId.HandWristRoot,
                out Pose wristPose))
        {
            return false;
        }

        // 1. 高度判定
        float minimumY =
            headCamera.transform.position.y + minHeightAboveHead;

        if (wristPose.position.y < minimumY)
        {
            return false;
        }

        // Meta Interaction SDK 中，右手掌心方向是腕部局部的 RightPalmar。
        Vector3 palmNormal =
            wristPose.rotation * Constants.RightPalmar;

        // 2. 掌心朝上判定
        float palmUpAngle =
            Vector3.Angle(palmNormal, Vector3.up);

        if (palmUpAngle > palmUpMaxAngle)
        {
            return false;
        }

        // 3. 五指展开判定
        return IsOpenHand();
    }

    private bool IsOpenHand()
    {
        for (int i = 0; i < Constants.NUM_FINGERS; i++)
        {
            HandFinger finger = (HandFinger)i;

            if (!rightHand.GetFingerIsHighConfidence(finger))
            {
                return false;
            }

            if (!IsFingerExtended(finger))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsFingerExtended(HandFinger finger)
    {
        HandJointId[] joints =
            HandJointUtils.FingerToJointList[(int)finger];

        if (joints == null || joints.Length < 3)
        {
            return false;
        }

        if (!rightHand.GetJointPose(joints[0], out Pose firstPose))
        {
            return false;
        }

        Vector3 firstPosition = firstPose.position;
        Vector3 previousPosition = firstPosition;
        Vector3 tipPosition = firstPosition;
        float pathLength = 0f;

        for (int i = 1; i < joints.Length; i++)
        {
            if (!rightHand.GetJointPose(joints[i], out Pose jointPose))
            {
                return false;
            }

            Vector3 currentPosition = jointPose.position;
            pathLength += Vector3.Distance(
                previousPosition,
                currentPosition);

            previousPosition = currentPosition;
            tipPosition = currentPosition;
        }

        if (pathLength <= Mathf.Epsilon)
        {
            return false;
        }

        // 手指伸直时，首尾直线距离接近所有骨节长度之和。
        float straightness =
            Vector3.Distance(firstPosition, tipPosition) / pathLength;

        return straightness >= openHandThreshold;
    }

    private void ToggleMenu()
    {
        IsMenuVisible = !IsMenuVisible;

        if (menuRoot != null)
        {
            menuRoot.SetActive(IsMenuVisible);
        }

        Debug.Log(
            IsMenuVisible
                ? "Palm menu changed to Visible."
                : "Palm menu changed to Hidden.",
            this);
    }

    private void OnValidate()
    {
        holdSeconds = Mathf.Max(0.01f, holdSeconds);
        cooldownSeconds = Mathf.Max(0f, cooldownSeconds);
        menuDistance = Mathf.Max(0.1f, menuDistance);
    }
    private void ShowGestureProgress(float progress)
    {
        if (gestureProgressRoot != null)
        {
            gestureProgressRoot.SetActive(true);
        }

        if (gestureProgressFill != null)
        {
            gestureProgressFill.fillAmount = progress;
        }
    }

    private void ResetGestureProgress()
    {
        gestureTimer = 0f;

        if (gestureProgressFill != null)
        {
            gestureProgressFill.fillAmount = 0f;
        }

        if (gestureProgressRoot != null)
        {
            gestureProgressRoot.SetActive(false);
        }
    }

}
