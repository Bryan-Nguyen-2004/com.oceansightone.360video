using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasyTransition
{
    public class EZTransition : MonoBehaviour
    {
        public TransitionSettings transitionSettings;

        public Transform transitionPanelIN;
        public Transform transitionPanelOUT;

        public CanvasScaler transitionCanvas;

        public Material multiplyColorMaterial;
        public Material additiveColorMaterial;

        bool hasTransitionTriggeredOnce;
        Canvas canvas;
        Transform prevParent;
        float canvasScale = 0.00005f;

        private void Start()
        {
            //Making sure not to destroy the transition when a new scene gets load
            DontDestroyOnLoad(gameObject);

            //Setting the resolution of the transition canvas
            transitionCanvas.referenceResolution = transitionSettings.refrenceResolution;

            /// Find the camera in the scene tagged as "MainCamera"
            Camera VR_Camera = Camera.main;

            /// Get the canvas component from the transitionCanvas object
            canvas = transitionCanvas.gameObject.GetComponent<Canvas>();
            canvas.name = "TransitionCanvas";
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = VR_Camera;

            /// Make the canvas a child of the VR camera
            prevParent = canvas.transform.parent;
            canvas.transform.SetParent(VR_Camera.transform);

            /// Position the canvas in front of the VR camera
            canvas.transform.localPosition = Vector3.forward * .015f;

            /// Ensure the canvas faces the same direction as the camera
            //canvas.transform.localRotation = Quaternion.identity;

            /// Set the planeDistance to a small value to render the canvas in front of other objects
            canvas.planeDistance = 0.015f;

            /// Scale the canvas based on the distance from the camera to ensure it fills the view
            canvas.transform.localScale = new Vector3(canvasScale, canvasScale, canvasScale);

            /// Ensure the canvas faces the camera
            canvas.transform.LookAt(VR_Camera.transform);

            transitionPanelIN.gameObject.SetActive(false);
            transitionPanelOUT.gameObject.SetActive(false);

            transitionPanelIN.gameObject.SetActive(false);
            transitionPanelOUT.gameObject.SetActive(false);

            //Setting up the transition objects
            transitionPanelIN.gameObject.SetActive(true);
            GameObject transitionIn = Instantiate(
                transitionSettings.transitionIn,
                transitionPanelIN
            );
            transitionIn.AddComponent<CanvasGroup>().blocksRaycasts =
                transitionSettings.blockRaycasts;

            //Setting the materials
            multiplyColorMaterial = transitionSettings.multiplyColorMaterial;
            additiveColorMaterial = transitionSettings.addColorMaterial;

            //Checking if the materials were correctly set
            if (multiplyColorMaterial == null || additiveColorMaterial == null)
                Debug.LogWarning(
                    "There are no color tint materials set for the transition. Changing the color tint will not affect the transition anymore!"
                );

            //Changing the color of the transition
            if (!transitionSettings.isCutoutTransition)
            {
                if (transitionIn.TryGetComponent<Image>(out Image parentImage))
                {
                    if (transitionSettings.colorTintMode == ColorTintMode.Multiply)
                    {
                        parentImage.material = multiplyColorMaterial;
                        parentImage.material.SetColor("_Color", transitionSettings.colorTint);
                    }
                    else if (transitionSettings.colorTintMode == ColorTintMode.Add)
                    {
                        parentImage.material = additiveColorMaterial;
                        parentImage.material.SetColor("_Color", transitionSettings.colorTint);
                    }
                }
                for (int i = 0; i < transitionIn.transform.childCount; i++)
                {
                    if (
                        transitionIn
                            .transform.GetChild(i)
                            .TryGetComponent<Image>(out Image childImage)
                    )
                    {
                        if (transitionSettings.colorTintMode == ColorTintMode.Multiply)
                        {
                            childImage.material = multiplyColorMaterial;
                            childImage.material.SetColor("_Color", transitionSettings.colorTint);
                        }
                        else if (transitionSettings.colorTintMode == ColorTintMode.Add)
                        {
                            childImage.material = additiveColorMaterial;
                            childImage.material.SetColor("_Color", transitionSettings.colorTint);
                        }
                    }
                }
            }

            //Flipping the scale if needed
            if (transitionSettings.flipX)
                transitionIn.transform.localScale = new Vector3(
                    -transitionIn.transform.localScale.x,
                    transitionIn.transform.localScale.y,
                    transitionIn.transform.localScale.z
                );
            if (transitionSettings.flipY)
                transitionIn.transform.localScale = new Vector3(
                    transitionIn.transform.localScale.x,
                    -transitionIn.transform.localScale.y,
                    transitionIn.transform.localScale.z
                );

            //Changing the animator speed
            if (
                transitionIn.TryGetComponent<Animator>(out Animator parentAnim)
                && transitionSettings.transitionSpeed != 0
            )
                parentAnim.speed = transitionSettings.transitionSpeed;
            else
            {
                for (int c = 0; c < transitionIn.transform.childCount; c++)
                {
                    if (
                        transitionIn
                            .transform.GetChild(c)
                            .TryGetComponent<Animator>(out Animator childAnim)
                        && transitionSettings.transitionSpeed != 0
                    )
                        childAnim.speed = transitionSettings.transitionSpeed;
                }
            }

            //Adding the funcion OnSceneLoad() to the sceneLoaded action
            SceneManager.sceneLoaded += OnSceneLoad;
        }

        public void OnSceneLoad(Scene scene, LoadSceneMode mode)
        {
            //Checking if this transition instance has allready played
            if (hasTransitionTriggeredOnce)
                return;

            transitionPanelIN.gameObject.SetActive(false);

            //Setting up the transition
            transitionPanelOUT.gameObject.SetActive(true);
            GameObject transitionOut = Instantiate(
                transitionSettings.transitionOut,
                transitionPanelOUT
            );
            transitionOut.AddComponent<CanvasGroup>().blocksRaycasts =
                transitionSettings.blockRaycasts;

            //Changing the color of the transition
            if (!transitionSettings.isCutoutTransition)
            {
                if (transitionOut.TryGetComponent<Image>(out Image parentImage))
                {
                    if (transitionSettings.colorTintMode == ColorTintMode.Multiply)
                    {
                        parentImage.material = multiplyColorMaterial;
                        parentImage.material.SetColor("_Color", transitionSettings.colorTint);
                    }
                    else if (transitionSettings.colorTintMode == ColorTintMode.Add)
                    {
                        parentImage.material = additiveColorMaterial;
                        parentImage.material.SetColor("_Color", transitionSettings.colorTint);
                    }
                }
                for (int i = 0; i < transitionOut.transform.childCount; i++)
                {
                    if (
                        transitionOut
                            .transform.GetChild(i)
                            .TryGetComponent<Image>(out Image childImage)
                    )
                    {
                        if (transitionSettings.colorTintMode == ColorTintMode.Multiply)
                        {
                            childImage.material = multiplyColorMaterial;
                            childImage.material.SetColor("_Color", transitionSettings.colorTint);
                        }
                        else if (transitionSettings.colorTintMode == ColorTintMode.Add)
                        {
                            childImage.material = additiveColorMaterial;
                            childImage.material.SetColor("_Color", transitionSettings.colorTint);
                        }
                    }
                }
            }

            //Flipping the scale if needed
            if (transitionSettings.flipX)
                transitionOut.transform.localScale = new Vector3(
                    -transitionOut.transform.localScale.x,
                    transitionOut.transform.localScale.y,
                    transitionOut.transform.localScale.z
                );
            if (transitionSettings.flipY)
                transitionOut.transform.localScale = new Vector3(
                    transitionOut.transform.localScale.x,
                    -transitionOut.transform.localScale.y,
                    transitionOut.transform.localScale.z
                );

            //Changeing the animator speed
            if (
                transitionOut.TryGetComponent<Animator>(out Animator parentAnim)
                && transitionSettings.transitionSpeed != 0
            )
                parentAnim.speed = transitionSettings.transitionSpeed;
            else
            {
                for (int c = 0; c < transitionOut.transform.childCount; c++)
                {
                    if (
                        transitionOut
                            .transform.GetChild(c)
                            .TryGetComponent<Animator>(out Animator childAnim)
                        && transitionSettings.transitionSpeed != 0
                    )
                        childAnim.speed = transitionSettings.transitionSpeed;
                }
            }

            //Turning on a safety switch so this transition instance cannot be triggered more than once
            hasTransitionTriggeredOnce = true;

            //Adjusting the destroy time if needed
            float destroyTime = transitionSettings.destroyTime;
            if (transitionSettings.autoAdjustTransitionTime)
                destroyTime = destroyTime / transitionSettings.transitionSpeed;

            /// Set the parent of the canvas back to the original parent (band-aid fix for an async issue when the scene switches)
            if (TransitionManager.Instance().switchingScenes)
                canvas.transform.SetParent(prevParent);
            else
                StartCoroutine(SetParentBack());

            //Destroying the transition
            Destroy(gameObject, destroyTime);
        }

        /// Coroutine to set parent of the canvas back to "TransitionTemplate(Clone)"
        IEnumerator SetParentBack()
        {
            yield return new WaitForSeconds(transitionSettings.destroyTime - .1f);
            canvas.transform.SetParent(prevParent);
        }
    }
}