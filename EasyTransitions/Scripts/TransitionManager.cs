using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Video360;

namespace EasyTransition
{
    public class TransitionManager : MonoBehaviour
    {
        [SerializeField]
        private GameObject transitionTemplate;

        public bool runningTransition;

        public UnityAction onTransitionBegin;
        public UnityAction onTransitionCutPointReached;
        public UnityAction onTransitionEnd;

        private static TransitionManager instance;

        /// To fix bug with switching scenes
        public bool switchingScenes = false;

        private void Awake()
        {
            instance = this;
            Debug.Log(transitionTemplate);
        }

        public static TransitionManager Instance()
        {
            if (instance == null)
                Debug.LogError("You tried to access the instance before it exists.");

            return instance;
        }

        /// <summary>
        /// Starts a transition without loading a new level.
        /// </summary>
        /// <param name="transition">The settings of the transition you want to use.</param>
        /// <param name="startDelay">The delay before the transition starts.</param>
        public void Transition(EasyTransitionInstance instance, float startDelay)
        {
            if (instance == null || instance.Transition == null || runningTransition)
            {
                Debug.LogError("You have to assign a transition.");
                return;
            }

            /// Added this to comply with interface
            instance.IsTransitioning = true;

            runningTransition = true;
            StartCoroutine(Timer(startDelay, instance));
        }

        IEnumerator Timer(float delay, EasyTransitionInstance instance)
        {
            TransitionSettings transitionSettings = instance.Transition;

            yield return new WaitForSecondsRealtime(delay);

            onTransitionBegin?.Invoke();

            GameObject template = Instantiate(transitionTemplate) as GameObject;
            template.GetComponent<EZTransition>().transitionSettings = transitionSettings;

            float transitionTime = transitionSettings.transitionTime;
            if (transitionSettings.autoAdjustTransitionTime)
                transitionTime = transitionTime / transitionSettings.transitionSpeed;

            yield return new WaitForSecondsRealtime(transitionTime);

            /// Added this to comply with interface
            instance.CutPointReached = true;

            onTransitionCutPointReached?.Invoke();

            template
                .GetComponent<EZTransition>()
                .OnSceneLoad(SceneManager.GetActiveScene(), LoadSceneMode.Single);

            yield return new WaitForSecondsRealtime(transitionSettings.destroyTime);

            /// Added this to comply with interface
            instance.IsTransitioning = false;

            onTransitionEnd?.Invoke();

            runningTransition = false;
        }

        private IEnumerator Start()
        {
            while (this.gameObject.activeInHierarchy)
            {
                //Check for multiple instances of the Transition Manager component
                var managerCount = GameObject.FindObjectsOfType<TransitionManager>(true).Length;
                if (managerCount > 1)
                    Debug.LogError(
                        $"There are {managerCount.ToString()} Transition Managers in your scene. Please ensure there is only one Transition Manager in your scene or overlapping transitions may occur."
                    );

                yield return new WaitForSecondsRealtime(1f);
            }
        }
    }
}
