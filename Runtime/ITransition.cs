using EasyTransition;
using UnityEngine;

namespace Video360
{
    public interface ITransition
    {
        ScriptableObject Settings { get; set; }
        bool IsTransitioning { get; set; }
        bool CutPointReached { get; set; }
        float TransitionLength { get; }

        public void StartTransition(Canvas transitionCanvas);
    }

    public class EasyTransitionInstance : ITransition
    {
        public bool IsTransitioning { get; set; }
        public bool CutPointReached { get; set; }
        public float TransitionLength { get; private set; }
        public ScriptableObject Settings { get; set; }
        public TransitionSettings Transition { get; private set; }

        public void StartTransition(Canvas transitionCanvas)
        {
            // Check if Settings is a TransitionSettings object
            if (Settings is not TransitionSettings)
            {
                Debug.LogError("You have to assigned the wrong transition settings. Please assign a TransitionSettings object.");
                return;
            }
            Transition = (TransitionSettings)Settings;

            TransitionLength = Transition.transitionTime;
            TransitionManager.Instance().Transition(this, 0);
        }
    }
}