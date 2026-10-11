using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QuaternionDemo
{
    /// <summary>
    /// 四元数旋转交互演示的主控制器。
    ///
    /// 功能：
    /// 1. 通过轴角参数生成四元数；
    /// 2. 用 Slerp 在起始旋转和目标旋转之间插值；
    /// 3. 显示四元数、欧拉角和模长；
    /// 4. 演示旋转的非交换性；
    /// 5. 使用 LineRenderer 显示局部坐标轴、旋转轴和方向轨迹。
    /// </summary>
    public class QuaternionRotationDemo : MonoBehaviour
    {
        [Header("必须指定的演示对象")]
        [Tooltip("需要进行旋转的物体，例如 Cube、飞机模型或箭头模型。")]
        [SerializeField] private Transform demoObject;

        [Header("轴角控制滑块")]
        [SerializeField] private Slider axisXSlider;
        [SerializeField] private Slider axisYSlider;
        [SerializeField] private Slider axisZSlider;
        [SerializeField] private Slider angleSlider;
        [SerializeField] private Slider interpolationSlider;

        [Header("文字显示（TextMeshPro）")]
        [SerializeField] private TMP_Text axisValueText;
        [SerializeField] private TMP_Text angleValueText;
        [SerializeField] private TMP_Text interpolationValueText;
        [SerializeField] private TMP_Text quaternionValueText;
        [SerializeField] private TMP_Text eulerValueText;
        [SerializeField] private TMP_Text magnitudeValueText;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private TMP_Text explanationText;

        [Header("可视化线条（可以留空，由脚本自动创建）")]
        [SerializeField] private LineRenderer localXAxisLine;
        [SerializeField] private LineRenderer localYAxisLine;
        [SerializeField] private LineRenderer localZAxisLine;
        [SerializeField] private LineRenderer rotationAxisLine;
        [SerializeField] private LineRenderer directionPathLine;

        [Header("动画设置")]
        [Min(0.2f)]
        [SerializeField] private float slerpDuration = 2.5f;

        [Min(0.5f)]
        [SerializeField] private float axisDisplayLength = 2.2f;

        [Range(8, 80)]
        [SerializeField] private int pathSamples = 32;

        [SerializeField] private bool playAnimationOnStart = false;

        private Quaternion startRotation = Quaternion.identity;
        private Quaternion targetRotation = Quaternion.identity;
        private Quaternion currentRotation = Quaternion.identity;
        private Coroutine animationCoroutine;
        private Material lineMaterial;
        private bool suppressSliderEvent;

        public bool IsAnimating { get; private set; }
        public Quaternion CurrentRotation => currentRotation;

        private void Awake()
        {
            if (demoObject == null)
            {
                Debug.LogError(
                    "QuaternionRotationDemo：没有指定 Demo Object。" +
                    "请把要旋转的物体拖到脚本的 Demo Object 槽位。",
                    this);
                enabled = false;
                return;
            }

            ConfigureSliders();
            CreateMissingLineRenderers();
            RegisterSliderEvents();

            currentRotation = demoObject.localRotation;
            startRotation = currentRotation;
            targetRotation = BuildQuaternionFromSliders();

            RefreshAllVisuals(currentRotation);
        }

        private void Start()
        {
            if (playAnimationOnStart)
            {
                PlaySlerp();
            }
        }

        private void OnDestroy()
        {
            UnregisterSliderEvents();

            if (lineMaterial != null)
            {
                Destroy(lineMaterial);
            }
        }

        private void ConfigureSliders()
        {
            ConfigureSlider(axisXSlider, -1f, 1f, 0f);
            ConfigureSlider(axisYSlider, -1f, 1f, 1f);
            ConfigureSlider(axisZSlider, -1f, 1f, 0f);
            ConfigureSlider(angleSlider, 0f, 360f, 120f);
            ConfigureSlider(interpolationSlider, 0f, 1f, 0f);
        }

        private static void ConfigureSlider(
            Slider slider,
            float minimum,
            float maximum,
            float defaultValue)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = minimum;
            slider.maxValue = maximum;

            // 只有数值超出范围时才写入默认值，避免覆盖你在 Inspector 中设置的值。
            if (slider.value < minimum || slider.value > maximum)
            {
                slider.SetValueWithoutNotify(defaultValue);
            }
        }

        private void RegisterSliderEvents()
        {
            if (axisXSlider != null) axisXSlider.onValueChanged.AddListener(OnAxisOrAngleChanged);
            if (axisYSlider != null) axisYSlider.onValueChanged.AddListener(OnAxisOrAngleChanged);
            if (axisZSlider != null) axisZSlider.onValueChanged.AddListener(OnAxisOrAngleChanged);
            if (angleSlider != null) angleSlider.onValueChanged.AddListener(OnAxisOrAngleChanged);
            if (interpolationSlider != null) interpolationSlider.onValueChanged.AddListener(OnInterpolationChanged);
        }

        private void UnregisterSliderEvents()
        {
            if (axisXSlider != null) axisXSlider.onValueChanged.RemoveListener(OnAxisOrAngleChanged);
            if (axisYSlider != null) axisYSlider.onValueChanged.RemoveListener(OnAxisOrAngleChanged);
            if (axisZSlider != null) axisZSlider.onValueChanged.RemoveListener(OnAxisOrAngleChanged);
            if (angleSlider != null) angleSlider.onValueChanged.RemoveListener(OnAxisOrAngleChanged);
            if (interpolationSlider != null) interpolationSlider.onValueChanged.RemoveListener(OnInterpolationChanged);
        }

        private void OnAxisOrAngleChanged(float unusedValue)
        {
            if (suppressSliderEvent)
            {
                return;
            }

            targetRotation = BuildQuaternionFromSliders();
            UpdateText(currentRotation);
            UpdateWorldLines(currentRotation);
        }

        private void OnInterpolationChanged(float interpolation)
        {
            if (suppressSliderEvent || IsAnimating)
            {
                return;
            }

            currentRotation = Quaternion.Slerp(startRotation, targetRotation, interpolation);
            ApplyRotation(currentRotation);
            SetState($"Manual Slerp: t = {interpolation:F2}");
        }

        /// <summary>
        /// 按下“播放”按钮时调用。
        /// </summary>
        public void PlaySlerp()
        {
            targetRotation = BuildQuaternionFromSliders();
            StartAnimation(targetRotation, "Slerp interpolation");
        }

        /// <summary>
        /// 立即应用当前滑块给出的轴角旋转，不播放动画。
        /// </summary>
        public void ApplyImmediately()
        {
            StopCurrentAnimation();
            startRotation = currentRotation;
            targetRotation = BuildQuaternionFromSliders();
            currentRotation = targetRotation;
            SetInterpolationSlider(1f);
            ApplyRotation(currentRotation);
            SetState("Target rotation applied immediately");
        }

        /// <summary>
        /// 重置为单位四元数，即不旋转。
        /// </summary>
        public void ResetRotation()
        {
            StopCurrentAnimation();
            startRotation = Quaternion.identity;
            targetRotation = Quaternion.identity;
            currentRotation = Quaternion.identity;
            SetInterpolationSlider(0f);
            demoObject.localRotation = Quaternion.identity;
            ApplyRotation(currentRotation);
            SetState("Identity quaternion q = (0, 0, 0, 1)");
            SetExplanation("Identity rotation: the object keeps its original orientation.");
        }

        public void PresetX90()
        {
            SetAxisAngleAndPlay(Vector3.right, 90f, "Preset: rotate 90 degrees around X");
        }

        public void PresetY90()
        {
            SetAxisAngleAndPlay(Vector3.up, 90f, "Preset: rotate 90 degrees around Y");
        }

        public void PresetZ90()
        {
            SetAxisAngleAndPlay(Vector3.forward, 90f, "Preset: rotate 90 degrees around Z");
        }

        public void PresetArbitraryAxis()
        {
            SetAxisAngleAndPlay(
                new Vector3(1f, 1f, 0.5f),
                135f,
                "Preset: rotate 135 degrees around axis (1, 1, 0.5)");
        }

        /// <summary>
        /// 演示“先绕 X，再绕 Y”。
        /// Unity 的乘法 qY * qX 表示先执行 qX，再执行 qY。
        /// </summary>
        public void ShowRotateXThenY()
        {
            Quaternion rotateX = QuaternionMath.FromAxisAngle(Vector3.right, 90f);
            Quaternion rotateY = QuaternionMath.FromAxisAngle(Vector3.up, 90f);
            Quaternion combined = QuaternionMath.Multiply(rotateY, rotateX);

            StartAnimation(combined, "Non-commutativity: X first, then Y");
            SetExplanation(
                "X then Y: q = qY * qX. Compare it with Y then X; " +
                "the final orientations are different.");
        }

        /// <summary>
        /// 演示“先绕 Y，再绕 X”。
        /// </summary>
        public void ShowRotateYThenX()
        {
            Quaternion rotateX = QuaternionMath.FromAxisAngle(Vector3.right, 90f);
            Quaternion rotateY = QuaternionMath.FromAxisAngle(Vector3.up, 90f);
            Quaternion combined = QuaternionMath.Multiply(rotateX, rotateY);

            StartAnimation(combined, "Non-commutativity: Y first, then X");
            SetExplanation(
                "Y then X: q = qX * qY. Rotation multiplication is not commutative, " +
                "so qX * qY is generally not equal to qY * qX.");
        }

        private void SetAxisAngleAndPlay(Vector3 axis, float angle, string message)
        {
            SetSliderValues(axis, angle);
            targetRotation = QuaternionMath.FromAxisAngle(axis, angle);
            StartAnimation(targetRotation, message);
            SetExplanation(
                "Axis-angle conversion: q = [axis * sin(angle/2), cos(angle/2)].");
        }

        private void StartAnimation(Quaternion destination, string message)
        {
            StopCurrentAnimation();
            startRotation = currentRotation;
            targetRotation = destination;
            animationCoroutine = StartCoroutine(AnimateSlerp(message));
        }

        private IEnumerator AnimateSlerp(string message)
        {
            IsAnimating = true;
            SetState(message);
            SetInterpolationSlider(0f);

            float elapsed = 0f;
            while (elapsed < slerpDuration)
            {
                elapsed += Time.deltaTime;
                float linearT = Mathf.Clamp01(elapsed / slerpDuration);

                // SmoothStep 只改变播放速度曲线；空间方向仍然由 Slerp 计算。
                float smoothT = Mathf.SmoothStep(0f, 1f, linearT);
                currentRotation = Quaternion.Slerp(startRotation, targetRotation, smoothT);

                SetInterpolationSlider(smoothT);
                ApplyRotation(currentRotation);
                yield return null;
            }

            currentRotation = targetRotation;
            SetInterpolationSlider(1f);
            ApplyRotation(currentRotation);
            IsAnimating = false;
            animationCoroutine = null;
        }

        private void StopCurrentAnimation()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }

            IsAnimating = false;
        }

        private Quaternion BuildQuaternionFromSliders()
        {
            Vector3 axis = ReadAxisFromSliders();
            float angle = angleSlider != null ? angleSlider.value : 0f;
            return QuaternionMath.FromAxisAngle(axis, angle);
        }

        private Vector3 ReadAxisFromSliders()
        {
            Vector3 axis = new Vector3(
                axisXSlider != null ? axisXSlider.value : 0f,
                axisYSlider != null ? axisYSlider.value : 1f,
                axisZSlider != null ? axisZSlider.value : 0f);

            if (axis.sqrMagnitude < 0.0001f)
            {
                axis = Vector3.up;
            }

            return axis.normalized;
        }

        private void SetSliderValues(Vector3 axis, float angle)
        {
            axis.Normalize();
            suppressSliderEvent = true;

            if (axisXSlider != null) axisXSlider.SetValueWithoutNotify(axis.x);
            if (axisYSlider != null) axisYSlider.SetValueWithoutNotify(axis.y);
            if (axisZSlider != null) axisZSlider.SetValueWithoutNotify(axis.z);
            if (angleSlider != null) angleSlider.SetValueWithoutNotify(angle);

            suppressSliderEvent = false;
            UpdateText(currentRotation);
            UpdateWorldLines(currentRotation);
        }

        private void SetInterpolationSlider(float value)
        {
            suppressSliderEvent = true;
            if (interpolationSlider != null)
            {
                interpolationSlider.SetValueWithoutNotify(value);
            }
            suppressSliderEvent = false;
        }

        private void ApplyRotation(Quaternion rotation)
        {
            demoObject.localRotation = rotation;
            RefreshAllVisuals(rotation);
        }

        private void RefreshAllVisuals(Quaternion rotation)
        {
            UpdateText(rotation);
            UpdateWorldLines(rotation);
        }

        private void UpdateText(Quaternion rotation)
        {
            Vector3 axis = ReadAxisFromSliders();
            float angle = angleSlider != null ? angleSlider.value : 0f;
            float interpolation = interpolationSlider != null ? interpolationSlider.value : 0f;
            Vector3 euler = rotation.eulerAngles;

            SetText(axisValueText, $"Axis = ({axis.x:F2}, {axis.y:F2}, {axis.z:F2})");
            SetText(angleValueText, $"Angle = {angle:F1} degrees");
            SetText(interpolationValueText, $"Interpolation t = {interpolation:F2}");
            SetText(
                quaternionValueText,
                $"Quaternion q = ({rotation.x:F3}, {rotation.y:F3}, " +
                $"{rotation.z:F3}, {rotation.w:F3})");
            SetText(
                eulerValueText,
                $"Euler = ({euler.x:F1}, {euler.y:F1}, {euler.z:F1}) degrees");
            SetText(magnitudeValueText, $"|q| = {QuaternionMath.Magnitude(rotation):F4}");
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        private void SetState(string message)
        {
            SetText(stateText, message);
        }

        private void SetExplanation(string message)
        {
            SetText(explanationText, message);
        }

        private void CreateMissingLineRenderers()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogWarning("找不到 Sprites/Default Shader，辅助线可能无法显示。", this);
                return;
            }

            lineMaterial = new Material(shader);

            localXAxisLine = EnsureLine(localXAxisLine, "Local X Axis", Color.red, 0.035f);
            localYAxisLine = EnsureLine(localYAxisLine, "Local Y Axis", Color.green, 0.035f);
            localZAxisLine = EnsureLine(localZAxisLine, "Local Z Axis", Color.blue, 0.035f);
            rotationAxisLine = EnsureLine(rotationAxisLine, "Rotation Axis", Color.yellow, 0.045f);
            directionPathLine = EnsureLine(
                directionPathLine,
                "Slerp Direction Path",
                new Color(1f, 0.35f, 0.9f),
                0.025f);
        }

        private LineRenderer EnsureLine(
            LineRenderer existing,
            string objectName,
            Color color,
            float width)
        {
            if (existing != null)
            {
                return existing;
            }

            GameObject lineObject = new GameObject(objectName);
            lineObject.transform.SetParent(transform, false);

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.material = lineMaterial;
            line.startColor = color;
            line.endColor = color;
            line.startWidth = width;
            line.endWidth = width;
            line.numCapVertices = 6;
            line.numCornerVertices = 4;
            line.positionCount = 2;
            return line;
        }

        private void UpdateWorldLines(Quaternion rotation)
        {
            if (demoObject == null)
            {
                return;
            }

            Vector3 origin = demoObject.position;
            DrawTwoPointLine(localXAxisLine, origin, origin + rotation * Vector3.right * axisDisplayLength);
            DrawTwoPointLine(localYAxisLine, origin, origin + rotation * Vector3.up * axisDisplayLength);
            DrawTwoPointLine(localZAxisLine, origin, origin + rotation * Vector3.forward * axisDisplayLength);

            Vector3 axis = ReadAxisFromSliders();
            Vector3 halfAxis = axis * axisDisplayLength * 1.15f;
            DrawTwoPointLine(rotationAxisLine, origin - halfAxis, origin + halfAxis);

            if (directionPathLine == null)
            {
                return;
            }

            directionPathLine.positionCount = pathSamples + 1;
            float pathRadius = axisDisplayLength * 0.82f;

            for (int index = 0; index <= pathSamples; index++)
            {
                float t = index / (float)pathSamples;
                Quaternion sample = Quaternion.Slerp(startRotation, targetRotation, t);
                Vector3 sampleDirection = sample * Vector3.forward;
                directionPathLine.SetPosition(index, origin + sampleDirection * pathRadius);
            }
        }

        private static void DrawTwoPointLine(
            LineRenderer line,
            Vector3 start,
            Vector3 end)
        {
            if (line == null)
            {
                return;
            }

            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
        }
    }
}
