using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace ValintaMusicStreaming
{
    /// <summary>
    /// Example: Scrolling status text. NOTE: Doesn't work if timescale is set to 0
    /// Add vertical scrollbar is in the demo scenes
    /// </summary>
    public class VStatusScroller : MonoBehaviour
    {
        [SerializeField]
        private Scrollbar m_statusScroller;

        private float m_lerpTime = 3f;
        private float m_currentLerpTime = 0;
        private float m_startValue = 0;
        private float m_endValue = 1;
        private bool m_isScrolling = false;
        private bool m_isReversed = false;

        void Update()
        {
            if (m_isScrolling)
            {
                m_currentLerpTime += Time.deltaTime;
                if (m_currentLerpTime > m_lerpTime)
                {
                    m_currentLerpTime = m_lerpTime;
                }

                float percentageCompleted = m_currentLerpTime / m_lerpTime;
                m_statusScroller.value = Mathf.Lerp(m_startValue, m_endValue, percentageCompleted);

                if (Mathf.Abs(m_statusScroller.value - m_endValue) < float.Epsilon)
                {
                    m_isScrolling = false;
                    StartCoroutine(AutoScroll());
                }
            }
        }

        void OnEnable()
        {
            ResetScroller();
            StartCoroutine(AutoScroll());
        }

        void OnDisable()
        {
            StopCoroutine(AutoScroll());
            ResetScroller();
        }

        IEnumerator AutoScroll()
        {
            yield return new WaitForSeconds(0.3f);

            m_currentLerpTime = 0;

            if (m_isReversed)
            {
                m_startValue = 1;
                m_endValue = 0;
            }
            else
            {
                m_startValue = 0;
                m_endValue = 1;
            }

            m_isScrolling = true;
            m_isReversed = !m_isReversed;
        }

        private void ResetScroller()
        {
            StopAllCoroutines();
            m_isReversed = false;
            m_isScrolling = false;
            m_startValue = 0;
            m_endValue = 1;
            m_currentLerpTime = 0;
            m_statusScroller.value = 0;
        }
    }
}
