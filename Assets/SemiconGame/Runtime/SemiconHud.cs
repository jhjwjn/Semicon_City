using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SemiconCity.Game
{
    public sealed class SemiconHud : MonoBehaviour
    {
        [SerializeField] private Text creditsText;
        [SerializeField] private Text objectiveIndexText;
        [SerializeField] private Text objectiveTitleText;
        [SerializeField] private Text objectiveDetailText;
        [SerializeField] private Text objectiveDistanceText;
        [SerializeField] private Text interactionText;
        [SerializeField] private CanvasGroup interactionGroup;
        [SerializeField] private CanvasGroup toastGroup;
        [SerializeField] private Text toastText;

        private Coroutine toastRoutine;

        private void Start()
        {
            var state = SemiconGameState.Instance;
            if (state != null)
            {
                state.StateChanged += RefreshResources;
            }
            RefreshResources();
            SetInteraction(string.Empty, false);
            SetGroup(toastGroup, false);
        }

        private void OnDestroy()
        {
            if (SemiconGameState.Instance != null)
            {
                SemiconGameState.Instance.StateChanged -= RefreshResources;
            }
        }

        public void SetInteraction(string message, bool visible)
        {
            if (interactionText != null)
            {
                interactionText.text = message;
            }
            SetGroup(interactionGroup, visible);
        }

        public void SetObjective(string index, string title, string detail, float distance = -1f)
        {
            if (objectiveIndexText != null) objectiveIndexText.text = index;
            if (objectiveTitleText != null) objectiveTitleText.text = title;
            if (objectiveDetailText != null) objectiveDetailText.text = detail;
            if (objectiveDistanceText != null)
                objectiveDistanceText.text = distance >= 0f ? $"{distance:0} m" : string.Empty;
        }

        public string CurrentObjectiveTitle => objectiveTitleText != null ? objectiveTitleText.text : string.Empty;
        public string CurrentObjectiveDetail => objectiveDetailText != null ? objectiveDetailText.text : string.Empty;

        public void ShowToast(string message, float duration = 2.4f)
        {
            if (toastText != null)
            {
                toastText.text = message;
            }
            if (toastRoutine != null)
            {
                StopCoroutine(toastRoutine);
            }
            toastRoutine = StartCoroutine(ShowToastRoutine(duration));
        }

        private IEnumerator ShowToastRoutine(float duration)
        {
            SetGroup(toastGroup, true);
            yield return new WaitForSecondsRealtime(duration);
            SetGroup(toastGroup, false);
        }

        private void RefreshResources()
        {
            var state = SemiconGameState.Instance;
            if (state == null)
            {
                return;
            }
            if (creditsText != null)
            {
                creditsText.text = $"{state.Credits:N0}";
            }
        }

        private static void SetGroup(CanvasGroup group, bool visible)
        {
            if (group == null)
            {
                return;
            }
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
