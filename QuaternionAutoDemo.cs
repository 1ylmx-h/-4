using System.Collections;
using TMPro;
using UnityEngine;

namespace QuaternionDemo
{
    /// <summary>
    /// 自动播放一段适合录屏的演示流程。
    /// 将本脚本与 QuaternionRotationDemo 挂到同一个空物体上即可。
    /// </summary>
    public class QuaternionAutoDemo : MonoBehaviour
    {
        [SerializeField] private QuaternionRotationDemo demo;
        [SerializeField] private TMP_Text chapterTitleText;

        [Header("自动播放设置")]
        [SerializeField] private bool playAutomatically = true;
        [Min(0.5f)]
        [SerializeField] private float pauseBetweenChapters = 1f;

        private Coroutine sequenceCoroutine;

        private void Awake()
        {
            if (demo == null)
            {
                demo = GetComponent<QuaternionRotationDemo>();
            }
        }

        private void Start()
        {
            if (playAutomatically)
            {
                PlayFullSequence();
            }
        }

        /// <summary>
        /// 可将此方法绑定到“自动演示”按钮。
        /// </summary>
        public void PlayFullSequence()
        {
            if (demo == null)
            {
                Debug.LogError("QuaternionAutoDemo：找不到 QuaternionRotationDemo。", this);
                return;
            }

            if (sequenceCoroutine != null)
            {
                StopCoroutine(sequenceCoroutine);
            }

            sequenceCoroutine = StartCoroutine(PlaySequence());
        }

        private IEnumerator PlaySequence()
        {
            demo.ResetRotation();
            SetChapter("1. Identity quaternion");
            yield return new WaitForSeconds(1.2f);

            SetChapter("2. Rotate 90 degrees around X");
            demo.PresetX90();
            yield return WaitForAnimationAndPause();

            SetChapter("3. Rotate 90 degrees around Y");
            demo.PresetY90();
            yield return WaitForAnimationAndPause();

            SetChapter("4. Rotate around an arbitrary axis");
            demo.PresetArbitraryAxis();
            yield return WaitForAnimationAndPause();

            SetChapter("5. Non-commutativity: X then Y");
            demo.ShowRotateXThenY();
            yield return WaitForAnimationAndPause();

            SetChapter("6. Non-commutativity: Y then X");
            demo.ShowRotateYThenX();
            yield return WaitForAnimationAndPause();

            SetChapter("Demo finished - press Auto Demo to replay");
            sequenceCoroutine = null;
        }

        private IEnumerator WaitForAnimationAndPause()
        {
            yield return null;

            while (demo.IsAnimating)
            {
                yield return null;
            }

            yield return new WaitForSeconds(pauseBetweenChapters);
        }

        private void SetChapter(string value)
        {
            if (chapterTitleText != null)
            {
                chapterTitleText.text = value;
            }
        }
    }
}
