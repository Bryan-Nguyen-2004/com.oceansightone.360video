/*
using System.Collections;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class Video360 : MonoBehaviour
{
    [SerializeField]
    private VideoPlayer videoPlayer;
    [Tooltip("The list of 360 video clips to play.")]
    public List<VideoClipWithTransition> videoClips = new List<VideoClipWithTransition>();

    [Header("Video Player Settings")]
    [Tooltip("If true, the videos will start playing as soon as the scene starts.")]
    [SerializeField]
    private bool playOnAwake = false;
    [Tooltip("If true, the videos will loop from the start after they have all been played.")]
    [SerializeField]
    private bool loop = true;

    [Header("Render Texture Settings")]
    [Tooltip("The level of anti-aliasing to apply to the 360 video. Higher levels of anti-aliasing will result in better quality, but will also require more processing power.")]
    [SerializeField]
    private AntiAliasingLevel antiAliasingLevel = AntiAliasingLevel.Off;
    
    [Header("Skybox Material Settings (Optional)")]
    [SerializeField]
    private Material skyboxMaterial;
    [Tooltip("Layout of 3D content in the source.")]
    [SerializeField]
    private Layout3D layout3D = Layout3D.None;

    private RenderTexture curRenderTexture;

    private void createSkyboxMaterial()
    {
        // Create a new Material with the 360 video as the main texture
        skyboxMaterial = new Material(Shader.Find("Skybox/Panoramic"));
        skyboxMaterial.name = "skyboxMaterial";
        skyboxMaterial.SetFloat("_Layout", (int)layout3D);
    }

    private void Awake()
    {
        // If no VideoPlayer is assigned, try to get one from the GameObject
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                Debug.LogError(
                    "No VideoPlayer component is assigned to the Video360 script. Please assign a VideoPlayer component to the Video360 script in the Inspector."
                );
                return;
            }
        }

        // Set the video player to render the video to a RenderTexture
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        // creates the skybox material which will be used to display the 360 video
        if (skyboxMaterial == null)
            createSkyboxMaterial();
        else if (skyboxMaterial.shader.name != "Skybox/Panoramic")
        {
            Debug.LogError(
                "The shader of the skybox material is not set to 'Skybox/Panoramic'. Please assign a material with the 'Skybox/Panoramic' shader to the Video360 script in the Inspector."
            );
            return;
        }
        RenderSettings.skybox = skyboxMaterial;

        // start playing the videos if playOnAwake is true
        if (playOnAwake)
        {
            StartPlayingVideos();
        }
    }

    private void createRenderTexture(VideoClip videoClip)
    {
        // Create a RenderTexture with the same dimensions as the video
        curRenderTexture = new RenderTexture((int)videoClip.width, (int)videoClip.height, 24);
        curRenderTexture.name = "360RenderTexture";
        curRenderTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        curRenderTexture.antiAliasing = (int)antiAliasingLevel;

        // I'm not sure if these settings are necessary
        // curRenderTexture.filterMode = FilterMode.Bilinear;
        // curRenderTexture.anisoLevel = 0;
        // curRenderTexture.useMipMap = false;
        // curRenderTexture.autoGenerateMips = false;
        // curRenderTexture.wrapMode = TextureWrapMode.Clamp;

        curRenderTexture.Create();

        // Set the RenderTexture as the target texture of the video player
        videoPlayer.targetTexture = curRenderTexture;

        // set the RenderTexture as the main texture of the skybox material
        skyboxMaterial.SetTexture("_MainTex", curRenderTexture);
    }

    /// <summary>
    ///     Plays all the videos from the start index to the end index, and loops them if loop is true.
    /// </summary>
    /// <param name="startIndex"></param>
    /// <param name="endIndex"></param>
    private IEnumerator PlayVideos(int startIndex, int endIndex)
    {
        // Variable to check if the video is done playing
        bool videoDone = false;

        // Set up the event to trigger when the video finishes
        videoPlayer.loopPointReached += vp =>
        {
            videoDone = true;
        };

        // play the videos in a loop if loop is true else play them once
        do
        {
            // Play all videos from the start index to the end index
            for (int i = startIndex; i < endIndex; i++)
            {
                videoDone = false;
                VideoClipWithTransition currentVideo = videoClips[i];

                // Check if the current video is valid
                if (currentVideo.videoClip == null)
                {
                    Debug.LogError(
                        "Can't play 360 video. No video clip is assigned to the Video360 script."
                    );
                    yield break;
                }
                if (
                    currentVideo.startTimeSecond < 0
                    || currentVideo.startTimeSecond > currentVideo.videoClip.length
                )
                {
                    Debug.LogError("Can't play 360 video. Start time is out of range.");
                    yield break;
                }
                if (
                    currentVideo.endTimeSecond != -1
                    && (
                        currentVideo.endTimeSecond < 0
                        || currentVideo.endTimeSecond > currentVideo.videoClip.length
                    )
                )
                {
                    Debug.LogError("Can't play 360 video. End time is out of range.");
                    yield break;
                }

                // Create a RenderTexture for the current video
                createRenderTexture(currentVideo.videoClip);

                // Set the clip of the video player to the video at the current index
                videoPlayer.clip = currentVideo.videoClip;

                // Set the volume of the video player
                videoPlayer.SetDirectAudioVolume(0, currentVideo.volume / 100f);

                // Set the playback speed of the video player
                videoPlayer.playbackSpeed = currentVideo.playbackSpeed;

                // Set the start time of the video player
                videoPlayer.time = currentVideo.startTimeSecond;

                // Set the end time of the current video to the length of the video if it's not set
                if (currentVideo.endTimeSecond == -1)
                {
                    currentVideo.endTimeSecond = (float)currentVideo.videoClip.length;
                }

                videoPlayer.Play();
                videoPlayer.prepareCompleted += (source) => Debug.Log("Playing Video " + i + " at " + Time.time.ToString("F3") + " seconds");

                // Wait until video is done before playing the next video
                while (!videoDone && videoPlayer.time < currentVideo.endTimeSecond)
                {
                    yield return null;
                }
                Debug.Log("Video " + i + " Done" + " at " + Time.time.ToString("F3") + " seconds");
            }

            // stops the player once all videos have been played
            videoPlayer.Stop();
        } while (loop);
    }

    /// <summary>
    ///         Starts playing the 360 videos from the start index to the end index.
    /// </summary>
    /// <param name="startIndex">The index of the first video to play (inclusive).</param>
    /// <param name="endIndex">The index of the last video to play (exclusive).</param>
    public void StartPlayingVideos(int startIndex = 0, int endIndex = -1)
    {
        // validate video clips and start/end indices
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
            // If no end index is given, set it to the last video clip
            endIndex = videoClips.Count;
        }
        else if (endIndex < startIndex || endIndex >= videoClips.Count)
        {
            Debug.LogError("Can't start playing 360 videos. End index out of range.");
            return;
        }

        // Set the video player to loop if loop is true
        videoPlayer.isLooping = loop;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

        // Start the PlayAllVideos coroutine
        StartCoroutine(PlayVideos(startIndex, endIndex));
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

        // Stop the current video if one is playing
        if (videoPlayer.isPlaying)
            videoPlayer.Stop();

        // set the clip of the video player to the chosen video and play it
        videoPlayer.clip = videoClips[index].videoClip;
        videoPlayer.Play();
    }

    /// <summary>
    ///    Stops the 360 video currently playing.
    /// </summary>
    public void StopVideo()
    {
        if (videoPlayer.clip == null)
        {
            Debug.LogError("Can't stop 360 video. No video clip is assigned to the VideoPlayer.");
            return;
        }
        if (!videoPlayer.isPlaying)
        {
            Debug.LogError("Can't stop 360 video. No video is currently playing.");
            return;
        }

        videoPlayer.Stop();
    }

    /// <summary>
    ///   Pauses the 360 video currently playing, and breaks the video loop.
    /// </summary>
    public void PauseVideo()
    {
        if (videoPlayer.clip == null)
        {
            Debug.LogError("Can't pause 360 video. No video clip is assigned to the VideoPlayer.");
            return;
        }
        if (!videoPlayer.isPlaying)
        {
            Debug.LogError("Can't pause 360 video. No video is currently playing.");
            return;
        }

        videoPlayer.Pause();
    }

    /// <summary>
    ///     Resumes the 360 video currently paused.
    /// </summary>
    public void ResumeVideo()
    {
        if (videoPlayer.clip == null)
        {
            Debug.LogError("Can't resume 360 video. No video clip is assigned to the VideoPlayer.");
            return;
        }
        if (videoPlayer.isPlaying)
        {
            Debug.LogError("Can't resume 360 video. A video is already playing.");
            return;
        }

        videoPlayer.Play();
    }
}

*/