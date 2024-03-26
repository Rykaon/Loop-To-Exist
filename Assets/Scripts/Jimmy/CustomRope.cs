using Obi;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CustomRope : MonoBehaviour
{
    [Header("Component References")]
    public ObiSolver solver; 
    [SerializeField] private MeshRenderer meshRenderer;

    public ObiRope rope = null;
    private ObiRopeBlueprint blueprint = null;
    private ObiParticleRenderer particleRenderer = null;
    private ObiRopeExtrudedRenderer extrudedRenderer = null;

    private ObiParticleAttachment startAttachment;
    private ObiParticleAttachment endAttachment;
    public Transform start;
    public Transform end;
    private ObiPinConstraintsBatch pinConstraint;
    //private ObiPinConstraintsBatch endPin;

    [Header("Rope Data")]
    [Range(0f, 1f)] [SerializeField] private float resolution = 0.5f;
    [SerializeField] private int pooledParticles = 100;
    [SerializeField] private float maxBending = 0.02f;
    [SerializeField] private ObiRopeSection section;
    [SerializeField] private bool particleRendererToggle = false;
    [SerializeField] private bool useTearing = false;
    [SerializeField] private float tearResistance = 800f;
    [SerializeField] private bool useSurfaceCollision = true;
    [Range(0f, 2f)] [SerializeField] private float stretchingScale = 1.5f;
    [SerializeField] private float ropeThickness = 0.5f;
    [SerializeField] private float ropeParticleInverseMass = 100f;
    [SerializeField] private float extraLenght = 1f;
    [SerializeField] private float ropeShootSpeed = 1f;

    public void Initialize(ObiCollider startCollider, Transform middle, ObiCollider endCollider)
    {
        blueprint = ScriptableObject.CreateInstance<ObiRopeBlueprint>();
        blueprint.resolution = resolution;
        blueprint.pooledParticles = pooledParticles;

        rope = transform.AddComponent<ObiRope>();

        if (particleRendererToggle)
        {
            particleRenderer = transform.AddComponent<ObiParticleRenderer>();
        }

        extrudedRenderer = transform.AddComponent<ObiRopeExtrudedRenderer>();
        extrudedRenderer.section = section;
        extrudedRenderer.uvScale = new Vector2(1f, 4f);
        extrudedRenderer.normalizeV = false;
        extrudedRenderer.uvAnchor = 1f;

        rope.maxBending = maxBending;
        rope.tearingEnabled = useTearing;

        if (useTearing)
        {
            rope.tearResistanceMultiplier = tearResistance;
        }

        rope.surfaceCollisions = useSurfaceCollision;
        rope.stretchingScale = stretchingScale;
        start = startCollider.transform;
        end = endCollider.transform;
        GenerateRope(startCollider, middle, endCollider);
    }

    public void GenerateRope(ObiCollider startCollider, Transform middle, ObiCollider endCollider)
    {
        Vector3 forward = Vector3.forward;
        int filter = ObiUtils.MakeFilter(65535, 0);
        ObiUtils.MakeFilter(0, 0);
        blueprint.path.Clear();
        blueprint.path.AddControlPoint((startCollider.transform.position - transform.position), Vector3.zero, Vector3.zero, Vector3.up, 1f / ropeParticleInverseMass, 0.1f, ropeThickness, filter, Color.white, "Start");
        if (middle != null)
        {
            blueprint.path.AddControlPoint((middle.position - transform.position), Vector3.zero, Vector3.zero, Vector3.up, 1f / ropeParticleInverseMass, 0.1f, ropeThickness, filter, Color.white, "Middle");
        }
        blueprint.path.AddControlPoint((endCollider.transform.position - transform.position), Vector3.zero, Vector3.zero, Vector3.up, 1f / ropeParticleInverseMass, 0.1f, ropeThickness, filter, Color.white, "End");

        blueprint.path.FlushEvents();

        blueprint.Generate();
        rope.ropeBlueprint = blueprint;

        AttachRope(startCollider, endCollider);
    }

    public void AttachRope(ObiCollider startCollider, ObiCollider endCollider)
    {
        if (startAttachment == null)
        {
            startAttachment = rope.AddComponent<ObiParticleAttachment>();
            startAttachment.attachmentType = ObiParticleAttachment.AttachmentType.Dynamic;
            startAttachment.compliance = 0;
            startAttachment.breakThreshold = float.PositiveInfinity;
        }
        startAttachment.target = startCollider.transform;


        if (endAttachment == null)
        {
            endAttachment = rope.AddComponent<ObiParticleAttachment>();
            endAttachment.attachmentType = ObiParticleAttachment.AttachmentType.Dynamic;
            endAttachment.compliance = 0;
            endAttachment.breakThreshold = float.PositiveInfinity;
        }
        endAttachment.target = endCollider.transform;


        List<ObiParticleGroup> particleGroups = blueprint.groups;
        foreach (ObiParticleGroup group in particleGroups)
        {
            if (group.name == "Start")
            {
                startAttachment.particleGroup = group;
            }
            else if (group.name == "End")
            {
                endAttachment.particleGroup = group;
            }
        }
    }

    public void SetAttachDynamic(bool attachDynamic)
    {
        if (startAttachment != null)
        {
            if (attachDynamic)
            {
                startAttachment.attachmentType = ObiParticleAttachment.AttachmentType.Dynamic;
                startAttachment.compliance = 0f;
                startAttachment.breakThreshold = float.PositiveInfinity;

                endAttachment.attachmentType = ObiParticleAttachment.AttachmentType.Dynamic;
                endAttachment.compliance = 0f;
                endAttachment.breakThreshold = float.PositiveInfinity;
            }
            else
            {
                startAttachment.attachmentType = ObiParticleAttachment.AttachmentType.Static;
                endAttachment.attachmentType = ObiParticleAttachment.AttachmentType.Static;
            }
        }
    }
}
