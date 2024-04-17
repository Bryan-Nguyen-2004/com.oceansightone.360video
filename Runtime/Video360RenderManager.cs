using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EasyTransition;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Video360
{
    public struct PlaybackState
    {
        public VideoPlayer VideoPlayer { get; }
        public Material Material { get; }
        public RenderTexture RenderTexture { get; private set; }

        public PlaybackState(VideoPlayer videoPlayer, Material material)
        {
            VideoPlayer = videoPlayer;
            Material = material;
            RenderTexture = null;
        }
    }

    public class Video360RenderManager
    {
        private PlaybackState _playbackStateA;
        private PlaybackState _playbackStateB;
        private PlaybackState _currentPlaybackState;

        public Video360RenderManager(PlaybackState playbackStateA, PlaybackState playbackStateB) {
            _playbackStateA = new PlaybackState(null, null);
            _playbackStateB = new PlaybackState(null, null);
            _currentPlaybackState = _playbackStateA;

        }

        private Material InitializeSkyboxMaterial(Layout3D layout3D, float rotation, bool doubleSidedGlobalIllumination)
        {
            // Create a new skybox material with the 'Skybox/Panoramic' shader and the specified settings.
            Material skyboxMaterial = new Material(Shader.Find("Skybox/Panoramic"));
            skyboxMaterial.name = "skyboxMaterial";
            skyboxMaterial.SetFloat("_Layout", (int)layout3D);
            skyboxMaterial.SetFloat("_Rotation", rotation);
            skyboxMaterial.EnableKeyword("_DOUBLE_SIDED_GLOBAL_ILLUMINATION");
            skyboxMaterial.SetFloat(
                "_DOUBLE_SIDED_GLOBAL_ILLUMINATION",
                doubleSidedGlobalIllumination ? 1 : 0
            );

            return skyboxMaterial;
        }

        private RenderTexture InitializeRenderTexture(VideoClip videoClip, AntiAliasingLevel antiAliasingLevel)
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

            curRenderTexture.Create();

            return curRenderTexture;
        }
    }
}