using UnityEngine;

namespace MinerUnity.Terrain
{
    /// <summary>
    /// Unity 6.3.
    /// Відповідає лише за візуальне попередження і анімацію падіння.
    /// Не змінює WorldData.
    /// </summary>
    public class StoneView : MonoBehaviour
    {
        [Header("Fall")]
        [SerializeField] private float fallSpeed = 8f;

        [Header("Warning")]
        [SerializeField] private float warningShakeAmplitude = 0.04f;
        [SerializeField] private float warningShakeFrequency = 35f;

        private enum VisualState
        {
            Idle,
            Warning,
            Falling
        }

        private VisualState state = VisualState.Idle;

        private Vector3 stableWorldPosition;
        private Vector3 targetWorldPosition;

        private float warningStartTime;
        private float warningDuration;

        public bool IsBusy => state != VisualState.Idle;

        private void Awake()
        {
            stableWorldPosition = transform.position;
            targetWorldPosition = transform.position;
        }

        /// <summary>
        /// Запускає попередження перед падінням.
        /// duration - скільки секунд камінь буде тремтіти.
        /// </summary>
        public void PlayWarning(float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            stableWorldPosition = SnapToCellCenter(transform.position);
            transform.position = stableWorldPosition;

            warningStartTime = Time.time;
            warningDuration = duration;
            state = VisualState.Warning;
        }

        /// <summary>
        /// Запускає фактичну анімацію падіння до цільової клітинки.
        /// </summary>
        public void PlayFallToGridY(int targetGridY)
        {
            stableWorldPosition = SnapToCellCenter(transform.position);
            transform.position = stableWorldPosition;

            targetWorldPosition = new Vector3(
                stableWorldPosition.x,
                targetGridY + 0.5f,
                stableWorldPosition.z
            );

            state = VisualState.Falling;
        }

        private void Update()
        {
            switch (state)
            {
                case VisualState.Warning:
                    UpdateWarning();
                    break;

                case VisualState.Falling:
                    UpdateFalling();
                    break;
            }
        }

        private void UpdateWarning()
        {
            float elapsed = Time.time - warningStartTime;

            if (elapsed >= warningDuration)
            {
                transform.position = stableWorldPosition;
                state = VisualState.Idle;
                return;
            }

            float normalized = Mathf.Clamp01(elapsed / warningDuration);

            // Наприкінці попередження можна дати трохи сильніший тремор.
            float amplitude = Mathf.Lerp(
                warningShakeAmplitude * 0.5f,
                warningShakeAmplitude,
                normalized
            );

            float xOffset = Mathf.Sin(Time.time * warningShakeFrequency) * amplitude;

            transform.position = stableWorldPosition + new Vector3(xOffset, 0f, 0f);
        }

        private void UpdateFalling()
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetWorldPosition,
                fallSpeed * Time.deltaTime
            );

            if ((transform.position - targetWorldPosition).sqrMagnitude < 0.0001f)
            {
                transform.position = targetWorldPosition;
                stableWorldPosition = targetWorldPosition;
                state = VisualState.Idle;
            }
        }

        private Vector3 SnapToCellCenter(Vector3 worldPosition)
        {
            return new Vector3(
                Mathf.Floor(worldPosition.x) + 0.5f,
                Mathf.Floor(worldPosition.y) + 0.5f,
                worldPosition.z
            );
        }
    }
}
