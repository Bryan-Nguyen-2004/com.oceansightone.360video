using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodiceApp;
using EasyTransition;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;

// public enum TransitionType
// {
//     Cut,
//     Fade,
//     Dissolve,
//     Wipe
// }

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

    // [Tooltip("The type of transition to use when transitioning to the next video.")]
    // public TransitionType transitionType;

    // [Tooltip("The time in seconds it takes to transition to the next video.")]
    // public float transitionTime;
    [Tooltip(
        "The transition settings to use when transitioning to the next video. If no transition is assigned, the video will cut to the next video."
    )]
    public TransitionSettings transition;

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

[
    RequireComponent(typeof(VideoPlayer)),
    RequireComponent(typeof(TransitionManager)),
    DisallowMultipleComponent
]
public class Video360 : MonoBehaviour
{
    [Tooltip("The list of 360 video clips to play.")]
    public List<VideoClipWithTransition> videoClips = new List<VideoClipWithTransition>();
    [SerializeField, Tooltip("The transition settings to use when transitioning to the first video. If no transition is assigned, it will cut to the first video.")]
    private TransitionSettings initialTransition;

    [Header("Video Player Settings")]
    [
        SerializeField,
        Tooltip(
            "Two VideoPlayers are required to play 360 videos smoothly, because you need to be able prepare one video in a videoplayer while the other videoplayer is playing a video so that there is no lag between video transitions. If no VideoPlayers are assigned in the Inspector, the Video360 script will automatically assign the first two VideoPlayers found on the GameObject to videoPlayer1 and videoPlayer2."
        )
    ]
    private VideoPlayer videoPlayer1;

    [
        SerializeField,
        Tooltip(
            "Two VideoPlayers are required to play 360 videos smoothly, because you need to be able prepare one video in a videoplayer while the other videoplayer is playing a video so that there is no lag between video transitions. If no VideoPlayers are assigned in the Inspector, the Video360 script will automatically assign the first two VideoPlayers found on the GameObject to videoPlayer1 and videoPlayer2."
        )
    ]
    private VideoPlayer videoPlayer2;
    [SerializeField, Tooltip("If true, the videos will start playing as soon as the scene starts.")]
    private bool playOnAwake = true;

    [
        SerializeField,
        Tooltip("If true, the videos will loop from the start after they have all been played.")
    ]
    private bool loop = false;

    [
        SerializeField,
        Tooltip(
            "If true, all GameObjects (besides the ones in the blacklist) in the scene will be set to inactive when the video starts playing."
        )
    ]
    private bool setGameObjectsInactive = true;

    [
        SerializeField,
        Tooltip(
            "The GameObjects to exclude from the setGameObjectsInactive setting. Their children will also be excluded."
        )
    ]
    private GameObject[] blackListedGameObjects;

    [
        SerializeField,
        Tooltip(
            "The tags to exclude from the setGameObjectsInactive setting. Their children will also be excluded."
        )
    ]
    private string[] blacklistedTags;

    [Header("Render Texture Settings (Optional)")]
    [
        SerializeField,
        Tooltip(
            "The level of anti-aliasing to apply to the 360 video. Higher levels of anti-aliasing will result in better quality, but will also require more processing power."
        )
    ]
    private AntiAliasingLevel antiAliasingLevel = AntiAliasingLevel.Off;

    [Header("Skybox Material Settings (Optional)")]
    [
        SerializeField,
        Tooltip(
            "The first skybox material to use for displaying the 360 video. If no material is assigned, a new material will be created with the 'Skybox/Panoramic' shader."
        )
    ]
    private Material skyboxMaterial1;

    [
        SerializeField,
        Tooltip(
            "The second skybox material to use for displaying the 360 video. If no material is assigned, a new material will be created with the 'Skybox/Panoramic' shader."
        )
    ]
    private Material skyboxMaterial2;

    [
        SerializeField,
        Tooltip(
            "The color to tint the skybox to. Unity adds this color to the Textures to change their appearance without altering the base Texture files."
        )
    ]
    private Color tintColor = Color.white;

    [
        SerializeField,
        Range(0, 8),
        Tooltip(
            "Adjusts the skybox’s exposure. This allows you to correct tonal values in the skybox Textures. Larger values produce a more exposed, seemingly brighter, skybox. Smaller values produce a less exposed, seemingly darker, skybox."
        )
    ]
    private float exposure = 1f;

    [
        SerializeField,
        Range(0, 360),
        Tooltip(
            "The rotation of the skybox around the positive y-axis. This changes the orientation of your skybox and is useful if you want a specific section of the skybox to be behind a particular part of your Scene."
        )
    ]
    private float rotation = 0f;

    [SerializeField, Tooltip("Layout of 3D content in the source.")]
    private Layout3D layout3D = Layout3D.None;

    [
        SerializeField,
        Tooltip(
            "Specifies whether the lightmapper accounts for both sides of the geometry when it calculates Global Illumination. When true, if you use the Progressive Lightmapper, back faces bounce light using the same emission and albedo as front faces."
        )
    ]
    private bool doubleSidedGlobalIllumination = false;

    private VideoPlayer curVideoPlayer;
    private Material curSkyboxMaterial;
    private Material originalSkyboxMaterial;
    private List<GameObject> allActiveGameObjects;
    private Image fadeOverlay;
    private TransitionManager transitionManager;
    private bool transitionPlaying = false;
    private bool transitionHalfway = true;

    private Material InitializeSkyboxMaterial()
    {
        // Create a new skybox material with the 'Skybox/Panoramic' shader and the specified settings.
        Material skyboxMaterial = new Material(Shader.Find("Skybox/Panoramic"));
        skyboxMaterial.name = "skyboxMaterial";
        skyboxMaterial.SetFloat("_Layout", (int)layout3D);
        //skyboxMaterial.SetColor("_Tint", tintColor);
        //skyboxMaterial.SetFloat("_Exposure", exposure);
        skyboxMaterial.SetFloat("_Rotation", rotation);
        skyboxMaterial.EnableKeyword("_DOUBLE_SIDED_GLOBAL_ILLUMINATION");
        skyboxMaterial.SetFloat(
            "_DOUBLE_SIDED_GLOBAL_ILLUMINATION",
            doubleSidedGlobalIllumination ? 1 : 0
        );

        return skyboxMaterial;
    }

    private RenderTexture InitializeRenderTexture(VideoClip videoClip)
    {
        // Create a RenderTexture with the same dimensions as the video and a depth of 0.
        RenderTexture curRenderTexture = new RenderTexture(
            (int)videoClip.width,
            (int)videoClip.height,
            0
        );
        curRenderTexture.name = "360RenderTexture";
        curRenderTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        curRenderTexture.antiAliasing = (int)antiAliasingLevel;

        // I'm not sure if these settings are necessary.
        // curRenderTexture.filterMode = FilterMode.Bilinear;
        // curRenderTexture.anisoLevel = 0;
        // curRenderTexture.useMipMap = false;
        // curRenderTexture.autoGenerateMips = false;
        // curRenderTexture.wrapMode = TextureWrapMode.Clamp;

        curRenderTexture.Create();

        return curRenderTexture;
    }

    private void InitializeFadeOverlay()
    {
        // Create a new Canvas GameObject
        GameObject canvasGameObject = new GameObject("Canvas");
        Canvas canvas = canvasGameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasGameObject.AddComponent<CanvasScaler>();
        canvasGameObject.AddComponent<GraphicRaycaster>();

        // Add the canvas to the blacklisted GameObjects
        blackListedGameObjects = blackListedGameObjects.Append(canvasGameObject).ToArray();

        // Create a new Image GameObject as a child of the Canvas
        GameObject imageGameObject = new GameObject("FadeOverlay");
        imageGameObject.transform.SetParent(canvasGameObject.transform);
        fadeOverlay = imageGameObject.AddComponent<Image>();

        // Set the Image to cover the whole screen
        fadeOverlay.rectTransform.anchorMin = new Vector2(0, 0);
        fadeOverlay.rectTransform.anchorMax = new Vector2(1, 1);
        fadeOverlay.rectTransform.anchoredPosition = new Vector2(0, 0);
        fadeOverlay.rectTransform.sizeDelta = new Vector2(0, 0);

        // Set the Image color to black and fully transparent
        fadeOverlay.color = new Color(0, 0, 0, 0);
    }

    private void UpdateActiveVideoPlayer(VideoPlayer videoPlayer, VideoClipWithTransition videoClip)
    {
        // Sets the setting of the video player to the settings of the video clip.
        curVideoPlayer.clip = videoClip.videoClip;

        curVideoPlayer.SetDirectAudioVolume(0, videoClip.volume / 100f);
        curVideoPlayer.playbackSpeed = videoClip.playbackSpeed;

        curVideoPlayer.time = videoClip.startTimeSecond;
        if (videoClip.endTimeSecond == -1)
            videoClip.endTimeSecond = (float)videoClip.videoClip.length;
    }

    private bool IsChildOfAnyBlacklistedObjects(Transform child)
    {
        // Check if the child is a child of any of the blacklisted GameObjects.
        foreach (GameObject parent in blackListedGameObjects)
        {
            if (child.IsChildOf(parent.transform))
            {
                return true;
            }
        }

        // Check if the child is a child of any GameObjects with blacklisted tags.
        Transform current = child;
        while (current != null)
        {
            if (blacklistedTags.Contains(current.tag))
            {
                return true;
            }
            current = current.parent;
        }

        return false;
    }

    private void DeactivateGameObjects()
    {
        // Set all GameObjects that are currently active to inactive, and store them in the allActiveGameObjects list.
        GameObject[] allGameObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
        foreach (GameObject go in allGameObjects)
        {
            // Check if the GameObject is active, not in the blacklisted GameObjects or tags, isnt the current GameObject, and isnt a child of any blacklisted GameObjects.
            if (
                go.activeInHierarchy
                && !blackListedGameObjects.Contains(go)
                && !blacklistedTags.Contains(go.tag)
                && go != this.gameObject
                && !IsChildOfAnyBlacklistedObjects(go.transform)
            )
            {
                allActiveGameObjects.Add(go);
                go.SetActive(false);
            }
        }
    }

    private void ReactivateGameObjects()
    {
        // Set all GameObjects that were active before the videos started playing back to active.
        foreach (GameObject go in allActiveGameObjects)
        {
            go.SetActive(true);
        }
    }

    /// <summary>
    ///     Plays all the videos from the start index to the end index, and loops them if loop is true.
    /// </summary>
    /// <param name="startIndex"></param>
    /// <param name="endIndex"></param>
    private IEnumerator Play360Videos(int startIndex, int endIndex)
    {
        bool videoDone = true;
        VideoPlayer prevVideoPlayer = null;
        VideoClipWithTransition prevVideo = null;

        // Set up an event to trigger when the video finishes.
        videoPlayer1.loopPointReached += vp =>
        {
            videoDone = true;
        };
        videoPlayer2.loopPointReached += vp =>
        {
            videoDone = true;
        };

        // Set all GameObjects that are currently active to inactive, and store them in the allActiveGameObjects list.
        if (setGameObjectsInactive)
            DeactivateGameObjects();

        if (initialTransition != null)
        {
            TransitionManager.Instance().Transition(initialTransition, 0);
        }

        // Play the videos in a loop if loop is true else play them once.
        do
        {
            // Play all videos from the start index to the end index.
            for (int i = startIndex; i < endIndex; i++)
            {
                VideoClipWithTransition curVideo = videoClips[i];

                // Check if the current video is valid.
                if (curVideo.videoClip == null)
                {
                    Debug.LogError(
                        "Can't play 360 video. No video clip is assigned to the Video360 script."
                    );
                    yield break;
                }
                if (
                    curVideo.startTimeSecond < 0
                    || curVideo.startTimeSecond > curVideo.videoClip.length
                )
                {
                    Debug.LogError("Can't play 360 video. Start time is out of range.");
                    yield break;
                }
                if (
                    curVideo.endTimeSecond != -1
                    && (
                        curVideo.endTimeSecond < 0
                        || curVideo.endTimeSecond > curVideo.videoClip.length
                    )
                )
                {
                    Debug.LogError("Can't play 360 video. End time is out of range.");
                    yield break;
                }

                // Create a RenderTexture for the current video.
                RenderTexture curRenderTexture = InitializeRenderTexture(curVideo.videoClip);

                // Update the active video player and skybox material.
                curSkyboxMaterial =
                    curVideoPlayer == videoPlayer1 ? skyboxMaterial1 : skyboxMaterial2;
                curVideoPlayer = curVideoPlayer == videoPlayer1 ? videoPlayer2 : videoPlayer1;
                UpdateActiveVideoPlayer(curVideoPlayer, curVideo);

                // Prepare the current video player to reduce lag between video transitions.
                curVideoPlayer.Prepare();

                // Set the current video player's RenderTexture and the skybox material's texture.
                curVideoPlayer.targetTexture = curRenderTexture;
                curSkyboxMaterial.SetTexture("_MainTex", curRenderTexture);

                // Wait for the previous video to finish playing and transition to hit the halfway point.
                if (prevVideoPlayer != null)
                {
                    while (!videoDone && prevVideoPlayer.time < prevVideo.endTimeSecond)
                    {
                        // Start the transition to the next video when the previous video is about to end.
                        if (
                            prevVideo.transition != null
                            && prevVideoPlayer.time
                                >= prevVideo.endTimeSecond - prevVideo.transition.transitionTime
                            && !transitionPlaying
                        )
                        {
                            TransitionManager.Instance().Transition(prevVideo.transition, 0);
                        }

                        // Trigger the OnVideoStay event while the video is playing.
                        prevVideo.OnVideoStay?.Invoke();
                        yield return null;
                    }
                    // Release the previous video player's RenderTexture, stop the video, and trigger the OnVideoEnd event.
                    prevVideoPlayer.targetTexture.Release();
                    prevVideoPlayer.Stop();
                    prevVideo.OnVideoEnd?.Invoke();
                }

                // Wait for the current video to be prepared and the transition to hit the halfway point.
                while (
                    !curVideoPlayer.isPrepared
                    || (prevVideo?.transition != null && !transitionHalfway)
                )
                {
                    Debug.Log(curVideoPlayer.time);
                    yield return null;
                }

                // Play the current video and trigger the OnVideoStart event.
                RenderSettings.skybox = curSkyboxMaterial;
                curVideoPlayer.Play();
                curVideo.OnVideoStart?.Invoke();

                // Update the previous video player and video.
                prevVideoPlayer = curVideoPlayer;
                prevVideo = curVideo;
                videoDone = false;
            }
        } while (loop);

        // Wait for the last video to finish playing and transition to hit the halfway point.
        while (!videoDone && prevVideoPlayer.time < prevVideo.endTimeSecond && !transitionHalfway)
        {
            // Start the ending transition video is about to end.
            if (
                prevVideo.transition != null
                && prevVideoPlayer.time
                    >= prevVideo.endTimeSecond - prevVideo.transition.transitionTime
                && !transitionPlaying
            )
            {
                TransitionManager.Instance().Transition(prevVideo.transition, 0);
            }

            // trigger the OnVideoStay event while the video is playing.
            prevVideo.OnVideoStay?.Invoke();
            yield return null;
        }

        // Release the last video player's RenderTexture and stop the video.
        curVideoPlayer.targetTexture.Release();
        curVideoPlayer.Stop();
        prevVideo.OnVideoEnd?.Invoke();

        // Reset the skybox material to the original skybox material.
        RenderSettings.skybox = originalSkyboxMaterial;

        // Set all GameObjects that were active before the videos started playing back to active.
        if (setGameObjectsInactive)
            ReactivateGameObjects();
    }

    /// <summary>
    ///         Starts playing the 360 videos from the start index to the end index.
    /// </summary>
    /// <param name="startIndex">The index of the first video to play (inclusive).</param>
    /// <param name="endIndex">The index of the last video to play (exclusive).</param>
    public void InitializePlayback(int startIndex = 0, int endIndex = -1)
    {
        // Validate video clips and start/end indices.
        if (videoClips.Count == 0)
        {
            Debug.LogError(
                "Can't start playing 360 videos. No video clips are assigned to the Video360 script."
            );
            return;
        }
        if (startIndex < 0 || startIndex >= videoClips.Count)
        {
            Debug.LogError("Can't start playing 360 videos. Start index out of range.");
            return;
        }
        if (endIndex == -1)
        {
            // If no end index is given, set it to the last video clip.
            endIndex = videoClips.Count;
        }
        else if (endIndex < startIndex || endIndex >= videoClips.Count)
        {
            Debug.LogError("Can't start playing 360 videos. End index out of range.");
            return;
        }

        // Make sure both video players are currently not playing.
        videoPlayer1.Stop();
        videoPlayer2.Stop();

        curVideoPlayer = videoPlayer1;
        curSkyboxMaterial = skyboxMaterial1;

        // Start the PlayAllVideos coroutine.
        StartCoroutine(Play360Videos(startIndex, endIndex));
    }

    private void Awake()
    {
        // Get the VideoPlayers if they are not assigned in the Inspector.
        if (videoPlayer1 == null || videoPlayer2 == null)
        {
            VideoPlayer[] videoPlayers = GetComponents<VideoPlayer>();

            if (videoPlayers.Length < 2)
            {
                Debug.LogError(
                    "Two VideoPlayers are required to play 360 videos smoothly. Please assign two VideoPlayers to the Video360 script in the Inspector."
                );
                return;
            }

            // Assign the first two video players found to videoPlayer1 and videoPlayer2.
            if (videoPlayer1 == null)
                videoPlayer1 = videoPlayers[0];
            if (videoPlayer2 == null)
                videoPlayer2 = videoPlayers[1];
        }

        // playOnAwake is set to false to prevent the videoPlayers from playing and incrementing their time on awake. which messes up the videoDone calculations.
        videoPlayer1.playOnAwake = false;
        videoPlayer2.playOnAwake = false;

        // waitForFirstFrame is set to true to ensure that the videoPlayers are prepared before playing.
        videoPlayer1.waitForFirstFrame = true;
        videoPlayer2.waitForFirstFrame = true;

        // renderMode is set to RenderTexture to display the 360 video to the skybox.
        videoPlayer1.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer2.renderMode = VideoRenderMode.RenderTexture;

        // skipOnDrop is set to true to skip frames if the videoPlayers can't keep up with the video, so that audio and video stay in sync.
        videoPlayer1.skipOnDrop = true;
        videoPlayer2.skipOnDrop = true;

        // audioOutputMode is set to Direct to send the audio directly to the audio listener, but this can be changed to AudioSource if you want to control the audio with an AudioSource.
        videoPlayer1.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer2.audioOutputMode = VideoAudioOutputMode.Direct;

        // Creates the first skybox material which will be used to display the 360 video.
        if (skyboxMaterial1 == null)
            skyboxMaterial1 = InitializeSkyboxMaterial();
        else if (skyboxMaterial1.shader.name != "Skybox/Panoramic")
        {
            Debug.LogError(
                "The shader of skybox material 1 is not set to 'Skybox/Panoramic'. Please assign a material with the 'Skybox/Panoramic' shader to the Video360 script in the Inspector."
            );
            return;
        }

        // Creates the second skybox material which will be used to display the 360 video.
        if (skyboxMaterial2 == null)
            skyboxMaterial2 = InitializeSkyboxMaterial();
        else if (skyboxMaterial2.shader.name != "Skybox/Panoramic")
        {
            Debug.LogError(
                "The shader of skybox material 2 is not set to 'Skybox/Panoramic'. Please assign a material with the 'Skybox/Panoramic' shader to the Video360 script in the Inspector."
            );
            return;
        }

        // Store the original skybox material.
        originalSkyboxMaterial = RenderSettings.skybox;

        // if setGameObjectsInactive is true, initialize the allActiveGameObjects list.
        if (setGameObjectsInactive)
        {
            allActiveGameObjects = new List<GameObject>();
        }

        // Initialize the fade overlay.
        InitializeFadeOverlay();

        // Get Transformation Manager
        transitionManager = TransitionManager.Instance();
        if (transitionManager == null)
        {
            Debug.LogError("Transition Manager is not found.");
            return;
        }

        // Set up events for the transition manager.
        transitionManager.onTransitionBegin += () =>
        {
            transitionPlaying = true;
        };
        transitionManager.onTransitionEnd += () =>
        {
            transitionPlaying = false;
            transitionHalfway = false;
        };
        transitionManager.onTransitionCutPointReached += () =>
        {
            transitionHalfway = true;
        };

        // Start playing the videos if playOnAwake is true.
        if (playOnAwake)
        {
            InitializePlayback();
        }
    }

    /// <summary>
    ///     Plays a single 360 video from the videoClips list at specified index.
    /// </summary>
    /// <param name="index">The index of the video to be played from videoClips</param>
    public void PlaySingleVideo(int index = 0)
    {
        if (index < 0 || index >= videoClips.Count)
        {
            Debug.LogError("Can't play single 360 Video. Index out of range.");
            return;
        }

        // Stop the current video if one is playing.
        if (curVideoPlayer.isPlaying)
            curVideoPlayer.targetTexture.Release();
        curVideoPlayer.Stop();

        // Create a RenderTexture for the current video.
        RenderTexture curRenderTexture = InitializeRenderTexture(videoClips[index].videoClip);

        // Set the current video player's RenderTexture and the skybox material's texture.
        curVideoPlayer.targetTexture = curRenderTexture;
        curSkyboxMaterial.SetTexture("_MainTex", curRenderTexture);

        UpdateActiveVideoPlayer(curVideoPlayer, videoClips[index]);

        // Set an event to clean up when the video finishes.
        curVideoPlayer.loopPointReached += vp =>
        {
            curVideoPlayer.targetTexture.Release();
            curVideoPlayer.Stop();
            RenderSettings.skybox = originalSkyboxMaterial;

            // Set all GameObjects that were active before the video started playing back to active.
            if (setGameObjectsInactive)
                ReactivateGameObjects();
        };

        // Set all GameObjects that are currently active to inactive, and store them in the allActiveGameObjects list.
        if (setGameObjectsInactive)
            DeactivateGameObjects();

        curVideoPlayer.Play();
    }

    /// <summary>
    ///    Stops the 360 video currently playing, and breaks the loop.
    /// </summary>
    public void StopVideo()
    {
        if (curVideoPlayer.clip == null)
        {
            Debug.LogError("Can't stop 360 video. No video clip is assigned to the VideoPlayer.");
            return;
        }
        if (!curVideoPlayer.isPlaying)
        {
            Debug.LogError("Can't stop 360 video. No video is currently playing.");
            return;
        }
        loop = false;

        // Stop the current video and clean up if one is playing.
        if (curVideoPlayer.isPlaying)
            curVideoPlayer.Stop();
        curVideoPlayer.targetTexture.Release();
        RenderSettings.skybox = originalSkyboxMaterial;
    }

    /// <summary>
    ///   Pauses the 360 video currently playing.
    /// </summary>
    public void PauseVideo()
    {
        if (curVideoPlayer.clip == null)
        {
            Debug.LogError("Can't pause 360 video. No video clip is assigned to the VideoPlayer.");
            return;
        }
        if (!curVideoPlayer.isPlaying)
        {
            Debug.LogError("Can't pause 360 video. No video is currently playing.");
            return;
        }

        curVideoPlayer.Pause();
    }

    /// <summary>
    ///     Resumes the 360 video currently paused.
    /// </summary>
    public void ResumeVideo()
    {
        if (curVideoPlayer.clip == null)
        {
            Debug.LogError("Can't resume 360 video. No video clip is assigned to the VideoPlayer.");
            return;
        }
        if (curVideoPlayer.isPlaying)
        {
            Debug.LogError("Can't resume 360 video. A video is already playing.");
            return;
        }

        curVideoPlayer.Play();
    }

    // private IEnumerator FadeToBlackCoroutine(float transitionTime)
    // {
    //     // Fade in
    //     for (float t = 0; t <= transitionTime; t += Time.deltaTime)
    //     {
    //         Debug.Log($"Fading to black {fadeOverlay.color}");
    //         fadeOverlay.color = new Color(0, 0, 0, t / transitionTime);
    //         yield return null;
    //     }

    //     // Fade out
    //     for (float t = transitionTime; t >= 0; t -= Time.deltaTime)
    //     {
    //         fadeOverlay.color = new Color(0, 0, 0, t / transitionTime);
    //         yield return null;
    //     }
    // }
}
