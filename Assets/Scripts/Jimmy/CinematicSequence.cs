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
            Fixed,
            Fade,
            InterpolateFromPlayer,
            InterpolateToPlayer,
            Interpolate
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

            cameraManager.StartCoroutine(PlayCinematicSequence());
        }

        private IEnumerator PlayCinematicSequence()
        {
            gameManager.ChangeState(GameManager.ControlState.UI);

            if (startTransition == Transition.Fade)
            {
                cameraManager.BlackScreen(startDuration, startDuration);
                yield return new WaitForSecondsRealtime(startDuration);

                cameraTarget.position = plans[0].position;
                cameraTarget.rotation = Quaternion.Euler(plans[0].rotation);
                cameraManager.ChangeCamera(cameraManager.cinematicCamera);
                yield return new WaitForSecondsRealtime(startDuration);
            }
            else if (startTransition == Transition.InterpolateToPlayer)
            {
                cameraTarget.DOMove(plans[0].position, startDuration).SetEase(Ease.InOutSine);
                cameraTarget.DORotate(plans[0].rotation, startDuration).SetEase(Ease.InOutSine);

                yield return new WaitForSecondsRealtime(startDuration);
                cameraManager.ChangeCamera(cameraManager.cinematicCamera);
            }

            List<CinematicPlan> sequencePlans = new List<CinematicPlan>();
            List<Vector3> positions = new List<Vector3>(); // Initialize current interpolate chain
            List<Vector3> rotations = new List<Vector3>();
            List<AnimationCurve> positionCurves = new List<AnimationCurve>();
            List<AnimationCurve> rotationCurves = new List<AnimationCurve>();
            List<float> durations = new List<float>();
            float totalDuration = 0f;

            for (int i = 0; i < plans.Count; i++)
            {
                if (plans[i].isFirstDialogue)
                {
                    yield return cameraManager.StartCoroutine(ExecuteDialogue(plans[i]));
                }

                if (plans[i].transition == Transition.Fixed)
                {
                    yield return new WaitForSecondsRealtime(plans[i].duration);
                }
                else if (plans[i].transition == Transition.Fade)
                {
                    cameraManager.BlackScreen(plans[i].transitionDuration, 0f);
                    yield return new WaitForSecondsRealtime(plans[i].transitionDuration);
                    cameraTarget.position = plans[i].position;
                    cameraTarget.rotation = Quaternion.Euler(plans[i].rotation);
                    yield return new WaitForSecondsRealtime(plans[i].duration);
                }
                else if (plans[i].transition == Transition.Interpolate)
                {
                    sequencePlans.Add(plans[i]);
                    positions.Add(plans[i].position); // Add current plan position to the chain
                    rotations.Add(plans[i].rotation);
                    positionCurves.Add(plans[i].positionCurve);
                    rotationCurves.Add(plans[i].rotationCurve);
                    durations.Add(plans[i].duration);
                    totalDuration += plans[i].duration;

                    // Check subsequent plans for interpolation until a non-interpolation plan is found
                    int j = i + 1;
                    for (; j < plans.Count; j++)
                    {
                        if (plans[j].transition != Transition.Interpolate)
                        {
                            break; // End the loop if a non-interpolate plan is found
                        }
                        sequencePlans.Add(plans[j]);
                        positions.Add(plans[j].position); // Add current plan position to the chain
                        rotations.Add(plans[j].rotation);
                        positionCurves.Add(plans[j].positionCurve);
                        rotationCurves.Add(plans[j].rotationCurve);
                        durations.Add(plans[j].duration);
                        totalDuration += plans[j].duration;
                    }

                    Vector3[] positionTangents = CalculateTangents(positions.ToArray());
                    Vector3[] rotationTangents = CalculateTangents(rotations.ToArray());

                    Vector3[][] positionControlPoints;
                    Vector3[][] rotationControlPoints;

                    CalculateControlPoints(positions.ToArray(), positionTangents, out positionControlPoints);
                    CalculateControlPoints(rotations.ToArray(), rotationTangents, out rotationControlPoints);

                    float[] interpolatedDurations = InterpolateDurations(positions.ToArray(), durations.ToArray());

                    // Execute interpolate chain only once for the whole sequence
                    yield return cameraManager.StartCoroutine(ExecuteInterpolateSequence(positions.ToArray(), positionControlPoints, positionCurves.ToArray(), rotations.ToArray(), rotationControlPoints, rotationCurves.ToArray(), interpolatedDurations, totalDuration));

                    sequencePlans.Clear();
                    positions.Clear(); // Clear current interpolate chain
                    rotations.Clear();
                    positionCurves.Clear();
                    rotationCurves.Clear();
                    durations.Clear();
                    totalDuration = 0f;

                    // Set index to the last plan checked for interpolation
                    i = j - 1;
                }
            }

            if (endTransition == Transition.Fade)
            {
                cameraManager.BlackScreen(endDuration, endDuration);
                yield return new WaitForSecondsRealtime(endDuration);

                cameraTarget.position = camPos;
                cameraTarget.rotation = Quaternion.Euler(camRot);
                cameraManager.ChangeCamera(cameraManager.worldCamera);
                yield return new WaitForSecondsRealtime(endDuration);
            }
            else if (endTransition == Transition.InterpolateToPlayer)
            {
                cameraTarget.DOMove(camPos, endDuration).SetEase(Ease.InOutSine);
                cameraTarget.DORotate(camRot, endDuration).SetEase(Ease.InOutSine);

                yield return new WaitForSecondsRealtime(endDuration);
                cameraManager.ChangeCamera(cameraManager.worldCamera);
            }

            gameManager.ChangeState(GameManager.ControlState.World);
        }

        private IEnumerator ExecuteInterpolateSequence(Vector3[] positions, Vector3[][] positionControlPoints, AnimationCurve[] positionCurves, Vector3[] rotations, Vector3[][] rotationControlsPoints, AnimationCurve[] rotationCurves, float[] durations, float totalDuration)
        {
            float totalElapsedTime = 0f;
            float elapsedTime = 0f;

            for (int i = 0; i < positions.Length - 1; i++)
            {
                elapsedTime = 0f;

                while (elapsedTime < durations[i])
                {
                    float t = elapsedTime / durations[i];

                    cameraTarget.position = CubicBezier(positions[i], positionControlPoints[i][0], positionControlPoints[i][1], positions[i + 1], positionCurves[i].Evaluate(t));
                    cameraTarget.rotation = CubicBezier(Quaternion.Euler(rotations[i]), Quaternion.Euler(rotationControlsPoints[i][0]), Quaternion.Euler(rotationControlsPoints[i][1]), Quaternion.Euler(rotations[i + 1]), rotationCurves[i].Evaluate(t));

                    elapsedTime += Time.deltaTime;
                    totalElapsedTime += Time.deltaTime;
                    yield return null;
                }
            }
        }

        private Vector3[] CalculateTangents(Vector3[] positions)
        {
            int count = positions.Length;
            Vector3[] tangents = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                Vector3 p0 = i > 0 ? positions[i - 1] : positions[i];
                Vector3 p1 = positions[i];
                Vector3 p2 = i < count - 1 ? positions[i + 1] : positions[i];
                tangents[i] = (p2 - p0) * 0.5f;
            }

            return tangents;
        }

        private void CalculateControlPoints(Vector3[] positions, Vector3[] tangents, out Vector3[][] controlPoints)
        {
            int count = positions.Length;
            controlPoints = new Vector3[count][];

            for (int i = 0; i < count; i++)
            {
                controlPoints[i] = new Vector3[2];

                if (i < count - 1)
                {
                    controlPoints[i][0] = positions[i] + tangents[i] / 3f;
                    controlPoints[i][1] = positions[i + 1] - tangents[i + 1] / 3f;
                }
            }
        }

        private Vector3 CubicBezier(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector3 p = uuu * a; // first term
            p += 3 * uu * t * b; // second term
            p += 3 * u * tt * c; // third term
            p += ttt * d; // fourth term

            return p;
        }

        private Quaternion CubicBezier(Quaternion a, Quaternion b, Quaternion c, Quaternion d, float t)
        {
            Quaternion ab = Quaternion.Slerp(a, b, t);
            Quaternion bc = Quaternion.Slerp(b, c, t);
            Quaternion cd = Quaternion.Slerp(c, d, t);
            Quaternion ab_bc = Quaternion.Slerp(ab, bc, t);
            Quaternion bc_cd = Quaternion.Slerp(bc, cd, t);

            return Quaternion.Slerp(ab_bc, bc_cd, t);
        }

        private float[] InterpolateDurations(Vector3[] positions, float[] durations)
        {
            int count = positions.Length - 1;
            float[] distances = new float[count];
            float[] speeds = new float[count];
            float[] interpolatedDurations = new float[count];

            // Calculer les distances et les vitesses initiales
            for (int i = 0; i < count; i++)
            {
                distances[i] = Vector3.Distance(positions[i], positions[i + 1]);
                speeds[i] = distances[i] / durations[i];
            }

            // Interpoler les vitesses pour un changement en douceur
            float[] interpolatedSpeeds = new float[count];
            interpolatedSpeeds[0] = speeds[0];
            interpolatedSpeeds[count - 1] = speeds[count - 1];

            for (int i = 1; i < count - 1; i++)
            {
                interpolatedSpeeds[i] = (speeds[i - 1] + speeds[i] + speeds[i + 1]) / 3f;
            }

            // Recalculer les durées en fonction des vitesses interpolées
            for (int i = 0; i < count; i++)
            {
                interpolatedDurations[i] = distances[i] / interpolatedSpeeds[i];
            }

            return interpolatedDurations;
        }
        private IEnumerator ExecuteDialogue(CinematicPlan dialoguePlan)
        {
            DialogueManager.instance.EnterDialogueMode(dialoguePlan.inkJSON, false, false);

            for (int i = 0; i < dialoguePlan.dialogueDurations.Count; i++)
            {
                yield return new WaitForSecondsRealtime(dialoguePlan.dialogueDurations[i]);
                DialogueManager.instance.ContinueStory();
            }
        }

        /*private void InitializePlans()
        {
            int index = -1;
            for (int i = 0; i < plans.Count; i++)
            {
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
                float timeToDialogue = -1f;
                CinematicPlan dialoguePlan = null;

                if (plans[i].transition == Transition.InterpolateCurve)
                {
                    for (int j = 0; j < plans[i].curvedPlans.Count; j++)
                    {
                        if (plans[i].curvedPlans[j].isFirstDialogue)
                        {
                            timeToDialogue = duration;
                            dialoguePlan = plans[i].curvedPlans[j];
                        }
                        
                        duration += plans[i].curvedPlans[j].duration;
                    }

                    cameraManager.StartCoroutine(ExecutePath(plans[i], duration, timeToDialogue, dialoguePlan));

                    i = plans[i].lastPlan.index + 1;
                }
                else
                {
                    duration = plans[i].duration;

                    if (plans[i].transition == Transition.Fade)
                    {
                        duration += plans[i].transitionDuration;
                    }

                    if (plans[i].isFirstDialogue)
                    {
                        dialoguePlan = plans[i];
                    }

                    cameraManager.StartCoroutine(ExecutePlan(plans[i], dialoguePlan));
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

        private IEnumerator ExecutePlan(CinematicPlan plan, CinematicPlan dialoguePlan)
        {
            Debug.Log(plan.transition + " // " + plan.index);

            if (dialoguePlan != null)
            {
                cameraManager.StartCoroutine(ExecuteDialogue(dialoguePlan));
            }

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

        private IEnumerator ExecutePath(CinematicPlan plan, float duration, float timeToDialogue, CinematicPlan dialoguePlan)
        {
            Debug.Log(plan.transition + " // " + plan.index);
            cameraTarget.DOLocalPath(plan.pathPosition.ToArray(), duration, PathType.CatmullRom).SetEase(Ease.Linear);

            int index = 0;
            float totalElapsedTime = 0f;
            float elapsedTime = 0f;
            bool isDialogueLaunched = false;

            while (totalElapsedTime < duration)
            {
                cameraTarget.DORotate(plan.pathRotation[index], plan.curvedPlans[index].duration).SetEase(Ease.InOutSine);

                if (elapsedTime >= plan.curvedPlans[index].duration)
                {
                    index++;
                    elapsedTime = 0f;
                }

                if (totalElapsedTime >= timeToDialogue && dialoguePlan != null && !isDialogueLaunched)
                {
                    isDialogueLaunched = true;
                    cameraManager.StartCoroutine(ExecuteDialogue(dialoguePlan));
                }

                totalElapsedTime += Time.fixedDeltaTime;
                elapsedTime += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
        }

        private IEnumerator ExecuteDialogue(CinematicPlan dialoguePlan)
        {
            DialogueManager.instance.EnterDialogueMode(dialoguePlan.inkJSON, false, false);

            for (int i = 0; i < dialoguePlan.dialogueDurations.Count; i++)
            {
                yield return new WaitForSecondsRealtime(dialoguePlan.dialogueDurations[i]);

                DialogueManager.instance.ContinueStory();
            }
        }*/
    }

    [Serializable]
    public class CinematicPlan
    {

        [Header("General properties")]
        public CinematicSequence.Transition transition;
        public float duration;
        public float transitionDuration;
        public bool isFirstDialogue;
        public TextAsset inkJSON;
        public List<float> dialogueDurations;

        [Header("Play properties")]
        public Vector3 position;
        public Vector3 rotation;
        public AnimationCurve positionCurve;
        public AnimationCurve rotationCurve;
        [HideInInspector] public float sceneSize = 10f;
        [HideInInspector] public Vector3 sceneLook = Vector3.zero;

        [HideInInspector] public int index = -1;
        [HideInInspector] public CinematicPlan lastPlan = null;
        [HideInInspector] public List<CinematicPlan> curvedPlans = null;
        [HideInInspector] public List<Vector3> pathPosition = null;
        [HideInInspector] public List<Vector3> pathRotation = null;

        public void SetotView()
        {
            #if UNITY_EDITOR
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
            #endif
        }

        public void GetToView()
        {
            #if UNITY_EDITOR
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.LookAt(sceneLook, Quaternion.Euler(rotation), sceneSize, sceneView.orthographic);
            }
            #endif
        }
    }
}
