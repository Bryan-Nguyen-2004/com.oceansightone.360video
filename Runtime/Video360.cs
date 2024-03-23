using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodiceApp;
using EasyTransition;
using TMPro;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

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

    [
        SerializeField,
        Tooltip(
            "The transition settings to use when transitioning to the first video. If no transition is assigned, it will cut to the first video."
        )
    ]
    private TransitionSettings initialTransition;

    [
        SerializeField,
        Tooltip(
            "The scene to switch to once all videos have been played. If set to None, the current scene will remain."
        )
    ]
    private string nextSceneName;

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
    private bool playOnAwake = false;

    [
        SerializeField,
        Tooltip("If true, the videos will loop from the start after they have all been played.")
    ]
    private bool loop = false;

    [Header("Black List Settings")]
    [
        SerializeField,
        Tooltip(
            "If true, all GameObjects (besides the ones in the blacklist) in the scene will be set to inactive when the video starts playing."
        )
    ]
    private bool setGameObjectsInactive = false;

    [
        SerializeField,
        Tooltip(
            "The GameObjects to exclude from the setGameObjectsInactive setting. Their children will also be excluded."
        )
    ]
    private GameObject[] blackListedGameObjects = new GameObject[0];

    [
        SerializeField,
        Tooltip(
            "The tags to exclude from the setGameObjectsInactive setting. Their children will also be excluded."
        )
    ]
    private string[] blacklistedTags = new string[0];

    [
        SerializeField,
        Tooltip("The names of GameObjects to exclude from the setGameObjectsInactive setting.")
    ]
    private string[] blackListedNames = new string[0];

    [
        SerializeField,
        Tooltip(
            "If true, all children of the blacklisted GameObjects will also remain active  (It is highly recommended to keep this true)."
        )
    ]
    private bool blackListChildrenToo = true;

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

    [Header("Miscellaneous Settings")]
    [
        SerializeField,
        Tooltip(
            "The origin of the index value used in the PlayVideoAtIndex method. This will be overriden if an index is passed to the method."
        )
    ]
    private Selectable indexValueOrigin = null;

    // Private variables
    private VideoPlayer curVideoPlayer = null;
    private VideoPlayer prevVideoPlayer = null;
    private Material curSkyboxMaterial = null;
    private Material originalSkyboxMaterial;
    private List<GameObject> allActiveGameObjects; // stores all game objects originally active in the scene
    private TransitionManager transitionManager;
    private bool transitionHalfway = false;
    private VideoClipWithTransition prevVideo = null;
    private bool videoDone = true;
    private int videoIndex = 0;
    private Coroutine play360VideosCoroutine; // stores the coroutine for playing the 360 videos to check if it's already running
    private int prevIndex = -1;
    private bool prevVideoAlreadyAssigned = false; // stores whether or not the prev coroutine was called
    private AsyncOperation sceneLoad = null; // stores the scene load operation for the next scene

    private void ResetGlobalVariables()
    {
        prevVideoPlayer = null;
        curVideoPlayer = null;
        curSkyboxMaterial = null;
        originalSkyboxMaterial = null;
        transitionHalfway = false;
        prevVideo = null;
        videoDone = true;
        videoIndex = 0;
        play360VideosCoroutine = null;
        prevIndex = -1;
    }

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

    private void SetUpForNewVideo(VideoClipWithTransition video)
    {
        // Create a RenderTexture for the current video.
        RenderTexture curRenderTexture = InitializeRenderTexture(video.videoClip);

        // Update the active video player
        UpdateActiveVideoPlayer(curVideoPlayer, video);

        // Prepare the current video player to reduce lag between video transitions.
        curVideoPlayer.Prepare();

        // Set the current video player's RenderTexture and the skybox material's texture.
        curVideoPlayer.targetTexture = curRenderTexture;
        curSkyboxMaterial.SetTexture("_MainTex", curRenderTexture);
    }

    private void CleanUpVideoPlayer(VideoPlayer videoPlayer)
    {
        // Release the video player's RenderTexture and stop the video.
        videoPlayer.targetTexture.Release();
        videoPlayer.Stop();

        // Reset the skybox material to the original skybox material.
        RenderSettings.skybox = originalSkyboxMaterial;

        // Set all GameObjects that were active before the videos started playing back to active.
        if (setGameObjectsInactive)
            ReactivateGameObjects();

        prevVideoPlayer = null;
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

        // Check if the child is a child of any GameObjects with a blacklisted tag or name.
        Transform current = child;
        while (current != null)
        {
            if (blacklistedTags.Contains(current.tag) || blackListedNames.Contains(current.name))
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
                && !blackListedNames.Contains(go.name)
                && go != this.gameObject
                && !(blackListChildrenToo && IsChildOfAnyBlacklistedObjects(go.transform)) // If blackListChildrenToo is true, check if the GameObject is a child of any blacklisted GameObjects.
                && go.scene.name != "DontDestroyOnLoad" // This is to prevent the fade overlay from being set to inactive.
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
        bool objectsDeactivated = false;

        // Set up an event to trigger when the video finishes.
        videoPlayer1.loopPointReached += vp =>
        {
            videoDone = true;
        };
        videoPlayer2.loopPointReached += vp =>
        {
            videoDone = true;
        };

        // Load the next scene if one is assigned and its not already loaded
        if (nextSceneName != null && sceneLoad == null)
        {
            sceneLoad = SceneManager.LoadSceneAsync(nextSceneName);
            sceneLoad.allowSceneActivation = false;
        }

        // Start the initial transition if one is assigned.
        if (initialTransition != null)
            TransitionManager.Instance().Transition(initialTransition, 0);
        else
            transitionHalfway = true; // Just cut so set transitionHalfway to true

        // Play the videos in a loop if loop is true else play them once.
        do
        {
            // Play all videos from the start index to the end index.
            for (videoIndex = startIndex; videoIndex <= endIndex; videoIndex++)
            {
                VideoClipWithTransition curVideo = prevVideo; // curVideo is set to prevVideo to prevent a null reference exception in the last iteration where you're justw aiting for the last video to finish

                // Only prepare a video if you're not just waiting for the last video to finish
                if (videoIndex != endIndex)
                {
                    curVideo = videoClips[videoIndex];

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
                    if (curVideo.endTimeSecond != -1 && curVideo.endTimeSecond < 0)
                    {
                        Debug.LogError("Can't play 360 video. End time is out of range.");
                        yield break;
                    }

                    // Switch the active video player and skybox material.
                    curSkyboxMaterial =
                        curVideoPlayer == videoPlayer1 ? skyboxMaterial1 : skyboxMaterial2;
                    curVideoPlayer = curVideoPlayer == videoPlayer1 ? videoPlayer2 : videoPlayer1;

                    // Prepare the current video player to reduce lag between video transitions.
                    SetUpForNewVideo(curVideo);
                }

                // Wait for the previous video to finish playing and transition to hit the halfway point.
                if (prevVideoPlayer != null)
                {
                    while (!videoDone && prevVideoPlayer.time < prevVideo.endTimeSecond)
                    {
                        // Start the transition to the next video when the previous video is about to end (this is timed so that when the video ends, the halfway point of the transition should trigger).
                        if (
                            prevVideoPlayer.time
                                >= prevVideo.endTimeSecond - prevVideo.transition.transitionTime
                            && !transitionManager.runningTransition
                        )
                        {
                            TransitionManager.Instance().Transition(prevVideo.transition, 0);
                        }

                        // If the previous video has no transition, set transitionHalfway to true.
                        if (prevVideo.transition == null)
                            transitionHalfway = true;

                        // Trigger the OnVideoStay event while the video is playing.
                        prevVideo.OnVideoStay?.Invoke();
                        yield return null;
                    }
                }

                // if the prevvideo was the last one, break the loop
                if (videoIndex == endIndex)
                    break;

                // Wait for the current video to be prepared and the transition to hit the halfway point.
                while (!curVideoPlayer.isPrepared || !transitionHalfway)
                {
                    yield return null;
                }

                // Release the previous video player's RenderTexture, stop the video, and trigger the OnVideoEnd event.
                if (prevVideo != null)
                {
                    prevVideoPlayer.targetTexture.Release();
                    prevVideoPlayer.Stop();
                    prevVideo.OnVideoEnd?.Invoke();
                }

                // Play the current video and trigger the OnVideoStart event.
                RenderSettings.skybox = curSkyboxMaterial;
                curVideoPlayer.Play();
                curVideo?.OnVideoStart?.Invoke();

                // Set all GameObjects that are currently active to inactive, and store them in the allActiveGameObjects list.
                if (!objectsDeactivated && setGameObjectsInactive) // only deactivate objects once
                {
                    DeactivateGameObjects();
                    objectsDeactivated = true;
                }

                // if prev was called, prevvideo was already updated in the prev coroutine, so skip updating it here
                if (!prevVideoAlreadyAssigned)
                    prevVideo = curVideo;
                else
                    prevVideoAlreadyAssigned = false;

                // Update the previous video player and video.
                prevVideoPlayer = curVideoPlayer;
                videoDone = false;
                prevIndex = videoIndex;
            }
        } while (loop && videoClips.Count > 0); // Only loop if there are videos to play and loop is true

        // wait for final transition to hit halfway point
        while (!transitionHalfway)
        {
            yield return null;
        }

        // activate the loaded scene
        if (nextSceneName != null)
            sceneLoad.allowSceneActivation = true;

        // Release the previous video player's RenderTexture, stop the video, and trigger the OnVideoEnd event.
        if (videoClips.Count > 0) // Only do this if videos actually played
        {
            CleanUpVideoPlayer(prevVideoPlayer);
            prevVideo.OnVideoEnd?.Invoke();
        }
        ResetGlobalVariables();
    }

    /// <summary>
    ///         Starts playing the 360 videos from the start index to the end index.
    /// </summary>
    /// <param name="startIndex">The index of the first video to play (inclusive).</param>
    /// <param name="endIndex">The index of the last video to play (exclusive).</param>
    public void StartPlayback(int startIndex = 0, int endIndex = -1)
    {
        // If the coroutine is already running, don't start it again
        if (play360VideosCoroutine != null)
        {
            Debug.LogWarning("Can't start playing 360 videos. A video is already playing.");
            return;
        }

        // Validate video clips and start/end indices.
        if (videoClips.Count != 0) // It's okay to have no video clips if you're just transitioning to the next scene
        {
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
        }

        // Make sure both video players are currently not playing.
        videoPlayer1.Stop();
        videoPlayer2.Stop();

        curVideoPlayer = videoPlayer1;
        curSkyboxMaterial = skyboxMaterial1;

        // Start the PlayAllVideos coroutine.
        play360VideosCoroutine = StartCoroutine(Play360Videos(startIndex, endIndex));
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

        // Start playing the videos if playOnAwake is true.
        if (playOnAwake)
        {
            StartPlayback();
        }
    }

    public void Start()
    {
        // Get Transformation Manager
        transitionManager = TransitionManager.Instance();
        if (transitionManager == null)
        {
            Debug.LogError("Transition Manager is not found.");
            return;
        }

        // Set up events for the transition manager.
        transitionManager.onTransitionEnd += () =>
        {
            transitionHalfway = false;
        };
        transitionManager.onTransitionCutPointReached += () =>
        {
            transitionHalfway = true;
        };
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
    private IEnumerator StopVideoCoroutine()
    {
        if (transitionManager.runningTransition)
            yield break;

        // If a video is currently playing, stop it and clean up.
        if (curVideoPlayer?.isPlaying == true || prevVideoPlayer?.isPlaying == true)
        {
            // Get the video player that is currently playing
            VideoPlayer videoPlayer =
                curVideoPlayer?.isPlaying == true ? curVideoPlayer : prevVideoPlayer;

            // Load the next scene if one is assigned and its not already loaded
            if (nextSceneName != null && sceneLoad == null)
            {
                sceneLoad = SceneManager.LoadSceneAsync(nextSceneName);
                sceneLoad.allowSceneActivation = false;
            }

            // Play transition and wait for it to hit halfway mark
            transitionManager.Transition(videoClips[prevIndex].transition, 0);
            while (!transitionHalfway)
            {
                yield return null;
            }

            // Load the next scene if one is assigned
            if (nextSceneName != null)
                sceneLoad.allowSceneActivation = true;

            CleanUpVideoPlayer(videoPlayer);
            this.StopAllCoroutines();
            ResetGlobalVariables();
        }
        else
        {
            Debug.LogError("Can't stop 360 video. No video is currently playing.");
        }
    }

    /// <summary>
    ///   Pauses the 360 video currently playing.
    /// </summary>
    public void PauseVideo()
    {
        // There is a bug that can occur when pausing a video while an EasyTransition is playing, causing the transition to repeat again and again or the game just crashes.
        // If you remove this line and add a Debug.Log() inside the transitionManager.onTransitionCutPointReached, you can see it get called repeatedly
        if (transitionManager.runningTransition)
            return;

        if (curVideoPlayer?.isPlaying == true)
        {
            curVideoPlayer.Pause();
        }
        else if (prevVideoPlayer?.isPlaying == true)
        {
            prevVideoPlayer.Pause();
        }
        else
        {
            Debug.LogError("Can't pause 360 video. No video is currently playing.");
        }
    }

    /// <summary>
    ///     Resumes the 360 video currently paused.
    /// </summary>
    public void ResumeVideo()
    {
        if (transitionManager.runningTransition)
            return;
        
        if (curVideoPlayer?.isPaused == true)
        {
            curVideoPlayer.Play();
        }
        else if (prevVideoPlayer?.isPaused == true)
        {
            prevVideoPlayer.Play();
        }
        else
        {
            Debug.LogError("Can't resume 360 video. No video is currently paused.");
        }
    }

    /// <summary>
    ///    Plays the next 360 video in the videoClips list.
    /// </summary>
    public void PlayNextVideo()
    {
        // if transition playing, return
        if (transitionManager.runningTransition)
            return;

        // if videoDone is false, then a video is currently playing or is paused and we should just mark it done.
        if (!videoDone)
        {
            // Transition to the next video
            if (loop && videoIndex == 0)
            {
                if (initialTransition != null)
                    transitionManager.Transition(initialTransition, 0);
            }
            else
                transitionManager.Transition(videoClips[prevIndex].transition, 0);

            videoDone = true;
            transitionHalfway = false;
        }
        // else then just start playback
        else
            StartPlayback();
    }

    private IEnumerator PlayVideoAtIndexCoroutine(int index)
    {
        if (videoClips.Count == 0)
        {
            Debug.LogError(
                "Can't play 360 video. No video clips are assigned to the Video360 script."
            );
            yield break;
        }
        if (index < -1 || index >= videoClips.Count)
        {
            Debug.LogError("Can't play 360 video. Index out of range.");
            yield break;
        }

        // If transition playing or function already called, return
        if (transitionManager.runningTransition || prevVideoAlreadyAssigned)
            yield break;

        // If no index is given, use the indexValueOrigin to get the index
        if (index == -1)
        {
            if (indexValueOrigin == null)
            {
                Debug.LogError("Can't play 360 video. No index is given and no indexValueOrigin is assigned.");
                yield break;
            }

            if (indexValueOrigin is TMP_Dropdown dropdown)
            {
                index = dropdown.value;
            }
            else if (indexValueOrigin is TMP_InputField inputField)
            {
                index = int.Parse(inputField.text);
            }
            else
            {
                Debug.LogError("The indexValueOrigin is not a Dropdown or InputField.");
                yield break;
            }
        }

        // If video is currently playing
        if (!videoDone)
        {
            prevVideoAlreadyAssigned = true;

            // if waiting on last video, manually switch active video player, bc it wasn't automatically done in the loop
            if (videoIndex == videoClips.Count)
            {
                curVideoPlayer = curVideoPlayer == videoPlayer1 ? videoPlayer2 : videoPlayer1;
                curSkyboxMaterial =
                    curSkyboxMaterial == skyboxMaterial1 ? skyboxMaterial2 : skyboxMaterial1;
            }
            else // else the video player was already prepared so we have to release the render texture
            {
                // Release the prepared video player's RenderTexture
                curVideoPlayer.targetTexture.Release();
            }

            // Prepare the video player to reduce lag between video transitions.
            SetUpForNewVideo(videoClips[index]);

            // Wait for the player to be prepared
            while (!curVideoPlayer.isPrepared)
            {
                yield return null;
            }

            // Play the transition of the current video and wait for it to hit the halfway point.
            TransitionManager.Instance().Transition(videoClips[prevIndex].transition, 0);
            while (!transitionHalfway)
            {
                yield return null;
            }

            videoIndex = index;
            prevVideo = videoClips[index];
            videoDone = true;
        }
        else // else then just start playback at the index
        {
            StartPlayback(index, -1);
        }
    }

    /// <summary>
    ///    Stops the 360 video currently playing, and breaks the loop.
    /// </summary>
    public void StopVideo()
    {
        StartCoroutine(StopVideoCoroutine());
    }

    /// <summary>
    ///     Plays the previous 360 video in the videoClips list.
    /// </summary>
    public void PlayPrevVideo()
    {
        // If first video is playing just stop it
        if (prevIndex == 0)
            StartCoroutine(StopVideoCoroutine());
        else
            StartCoroutine(PlayVideoAtIndexCoroutine(prevIndex - 1));
    }

    /// <summary>
    ///     Calls the StartPlayback method with default parameters (used for buttons, because buttons can only have one parameter).
    /// </summary>
    public void StartPlaybackFromButton()
    {
        StartPlayback();
    }

    /// <summary>
    ///    Plays the video at the index specified in the argument or the index specified in the Index Value Origin if no argument is provided.
    /// </summary>
    public void PlayVideoAtIndex(int index = -1)
    {
        StartCoroutine(PlayVideoAtIndexCoroutine(index));
    }
}
