using Cinemachine;
using DG.Tweening.Plugins.Core.PathCore;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cinematic
{
    [Serializable]
    public class CinematicSequence
    {
        public enum Transition
        {
            Cut,
            Fade,
            InterpolateFromPlayer,
            InterpolateToPlayer,
            InterpolateLinear,
            InterpolateCurve
        }

        public Transition startTransition, endTransition;
        public float startDuration, endDuration;
        public List<CinematicPlan> plans;

        private GameManager gameManager;
        private CameraManager cameraManager;
        private DialogueManager dialogueManager;
        private CinemachineVirtualCamera cinematicCamera;
        private Transform cameraTarget;

        private Vector3 camPos;
        private Vector3 camRot;

        public void Execute(CameraManager cameraManager, GameManager gameManager, DialogueManager dialogueManager, CinemachineVirtualCamera cinematicCamera, Transform cameraTarget)
        {
            this.cameraManager = cameraManager;
            this.gameManager = gameManager;
            this.dialogueManager = dialogueManager;
            this.cinematicCamera = cinematicCamera;
            this.cameraTarget = cameraTarget;
            gameManager.mainPlayer.isActive = false;

            camPos = Camera.main.transform.position;
            camRot = Camera.main.transform.rotation.eulerAngles;

            Debug.Log(camPos + " // " + camRot);

            InitializePlans();
            cameraManager.StartCoroutine(ExecuteSequence());
        }

        private void InitializePlans()
        {
            int index = -1;
            for (int i = 0; i < plans.Count - 1; i++)
            {
                int next = i + 1;
                plans[i].index = i;

                if (plans[i].transition == Transition.InterpolateCurve)
                {
                    if (plans[i].isFirstDialogue)
                    {
                        index = i;
                        plans[index].curvedPlans = new List<CinematicPlan>();
                        plans[index].pathPosition = new List<Vector3>();
                        plans[index].pathRotation = new List<Vector3>();
                    }

                    plans[index].curvedPlans.Add(plans[i]);
                    plans[index].pathPosition.Add(plans[i].position);
                    plans[index].pathRotation.Add(plans[i].rotation);
                    plans[index].lastPlan = plans[i];
                }
            }
        }

        private IEnumerator ExecuteSequence()
        {
            gameManager.ChangeState(GameManager.ControlState.UI);

            if (startTransition == Transition.Fade)
            {
                cameraManager.BlackScreen(startDuration, startDuration);
                yield return new WaitForSecondsRealtime(startDuration);

                cameraTarget.position = plans[0].position;
                cameraTarget.rotation = Quaternion.Euler(plans[0].rotation);
                cameraManager.ChangeCamera(cinematicCamera);
                yield return new WaitForSecondsRealtime(startDuration);
            }
            else if (startTransition == Transition.InterpolateFromPlayer)
            {
                cameraTarget.position = camPos;
                cameraTarget.rotation = Quaternion.Euler(camRot);

                cameraManager.ChangeCamera(cinematicCamera);

                cameraTarget.DOMove(plans[0].position, startDuration).SetEase(Ease.InOutSine);
                cameraTarget.DORotate(plans[0].rotation, startDuration).SetEase(Ease.InOutSine);
                yield return new WaitForSecondsRealtime(startDuration);
            }
            else
            {
                cameraTarget.position = plans[0].position;
                cameraTarget.rotation = Quaternion.Euler(plans[0].rotation);
                cameraManager.ChangeCamera(cinematicCamera);
            }

            for (int i = 0; i < plans.Count; i++)
            {
                float duration = 0f;

                if (plans[i].transition == Transition.InterpolateCurve)
                {
                    for (int j = 0; j < plans[i].curvedPlans.Count - 1; j++)
                    {
                        duration += plans[i].curvedPlans[j].duration;
                    }

                    cameraManager.StartCoroutine(ExecutePath(plans[i], duration));

                    i = plans[i].lastPlan.index - 1;
                }
                else
                {
                    duration = plans[i].duration;

                    if (plans[i].transition == Transition.Fade)
                    {
                        duration += plans[i].transitionDuration;
                    }

                    cameraManager.StartCoroutine(ExecutePlan(plans[i]));
                }

                yield return new WaitForSecondsRealtime(duration);
            }

            if (endTransition == Transition.Fade)
            {
                cameraManager.BlackScreen(startDuration, startDuration);
                yield return new WaitForSecondsRealtime(startDuration);

                cameraTarget.position = plans[0].position;
                cameraTarget.rotation = Quaternion.Euler(plans[0].rotation);
                cameraManager.ChangeCamera(cameraManager.worldCamera);
                yield return new WaitForSecondsRealtime(startDuration);
            }
            else if (endTransition == Transition.InterpolateToPlayer)
            {
                cameraTarget.DOMove(camPos, startDuration).SetEase(Ease.InOutSine);
                cameraTarget.DORotate(camRot, startDuration).SetEase(Ease.InOutSine);

                yield return new WaitForSecondsRealtime(startDuration);
                cameraManager.ChangeCamera(cameraManager.worldCamera);
            }
            else
            {
                cameraManager.ChangeCamera(cameraManager.worldCamera);
            }

            gameManager.ChangeState(GameManager.ControlState.World);
        }

        private IEnumerator ExecutePlan(CinematicPlan plan)
        {
            Debug.Log(plan.transition + " // " + plan.index);
            if (plan.transition == Transition.Cut)
            {
                cameraTarget.position = plan.position;
                cameraTarget.rotation = Quaternion.Euler(plan.rotation);
            }
            else if (plan.transition == Transition.Fade)
            {
                cameraManager.BlackScreen(plan.transitionDuration, 0f);
                yield return new WaitForSecondsRealtime(plan.transitionDuration);
                cameraTarget.position = plan.position;
                cameraTarget.rotation = Quaternion.Euler(plan.rotation);
            }
            else if (plan.transition == Transition.InterpolateLinear)
            {
                cameraTarget.DOMove(plan.position, plan.duration).SetEase(Ease.InOutSine);
                cameraTarget.DORotate(plan.rotation, plan.duration).SetEase(Ease.InOutSine);
            }

            yield return new WaitForSecondsRealtime(plan.duration);
        }

        private IEnumerator ExecutePath(CinematicPlan plan, float duration)
        {
            Debug.Log(plan.transition + " // " + plan.index);
            cameraTarget.DOLocalPath(plan.pathPosition.ToArray(), duration, PathType.CatmullRom).SetEase(Ease.Linear);

            for (int i = 0; i < plan.curvedPlans.Count; i++)
            {
                cameraTarget.DORotate(plan.pathRotation[i], plan.curvedPlans[i].duration).SetEase(Ease.InOutSine);
                yield return new WaitForSecondsRealtime(plan.curvedPlans[i].duration);
            }
        }
    }

    [Serializable]
    public class CinematicPlan
    {

        [Header("General properties")]
        public CinematicSequence.Transition transition;
        public float duration;
        public float transitionDuration;
        public bool isFirstDialogue;
        public List<float> dialogueDurations;

        [Header("Play properties")]
        public Vector3 position;
        public Vector3 rotation;
        [HideInInspector] public float sceneSize = 10f;
        [HideInInspector] public Vector3 sceneLook = Vector3.zero;

        [HideInInspector] public int index = -1;
        [HideInInspector] public CinematicPlan lastPlan = null;
        [HideInInspector] public List<CinematicPlan> curvedPlans = null;
        [HideInInspector] public List<Vector3> pathPosition = null;
        [HideInInspector] public List<Vector3> pathRotation = null;

        public void SetotView()
        {
            SceneView sceneView = SceneView.lastActiveSceneView;

            if (sceneView != null)
            {
                position = sceneView.camera.transform.position;
                rotation = sceneView.camera.transform.rotation.eulerAngles;
                sceneSize = sceneView.size;
                sceneLook = sceneView.camera.transform.position + sceneView.camera.transform.forward * sceneView.cameraDistance;
                Debug.Log("Position = " + position + " // Rotation = " + rotation);
            }
            else
            {
                Debug.Log("SceneView not found");
            }
        }

        public void GetToView()
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.LookAt(sceneLook, Quaternion.Euler(rotation), sceneSize, sceneView.orthographic);
            }
        }
    }
}
