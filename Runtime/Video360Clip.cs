using EasyTransition;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

namespace Video360
{
    public enum AntiAliasingLevel
    {
        Off = 1,
        X2 = 2,
        X4 = 4,
        X8 = 8
    }

    public enum Layout3D
    {
        None,
        SideBySide,
        OverUnder
    }

    [System.Serializable]
    public class VideoClipWithTransition : ISerializationCallbackReceiver
    {
        [Tooltip("The 360 video clip to play.")]
        public VideoClip videoClip;

        [Tooltip("The volume of the video. Set to 0 to mute the video.")]
        [Range(0f, 100f)]
        public float volume;

        [Tooltip(
            "The transition type to use when transitioning to the next video. If no type is assigned, it will just cut to the next video."
        )]
        public ITransition transitionType;

        [Tooltip(
            "The transition settings to use when transitioning to the next video. If no settings are assigned, but the TransitionType is assigned, an error will be thrown."
        )]
        public ScriptableObject transitionSettings;

        [Tooltip("The time in seconds to start playing the video from.")]
        public float startTimeSecond;

        [Tooltip(
            "The time in seconds to stop playing the video at. Set to -1 to play the video to the end."
        )]
        public float endTimeSecond;

        [Tooltip("The speed at which the video will be played.")]
        [Range(0.1f, 10f)]
        public float playbackSpeed;

        [Tooltip("The events to trigger when the video starts playing.")]
        public UnityEvent OnVideoStart;

        [Tooltip("The events to trigger while the video is playing.")]
        public UnityEvent OnVideoStay;

        [Tooltip("The events to trigger when the video finishes playing.")]
        public UnityEvent OnVideoEnd;

        [SerializeField, HideInInspector]
        private bool _serialized = false;

        public void OnBeforeSerialize() { }

        // Set default values for the video clip (this is the only way I could find to set default values for serializable classes)
        public void OnAfterDeserialize()
        {
            if (_serialized == false)
            {
                _serialized = true;
                playbackSpeed = 1.0f;
                endTimeSecond = -1.0f;
                volume = 100.0f;
            }
        }
    }
}