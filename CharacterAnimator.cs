// Idan Sharon 205411515, Daniel Brown 311340723

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{
    public TextAsset BVHFile; // The BVH file that defines the animation and skeleton
    public bool animate; // Indicates whether or not the animation should be running
    private BVHData data; // BVH data of the BVHFile will be loaded here
    private int currFrame = 0; // Current frame of the animation

    private float timer = 0.0f; // Timer between frames


    // Start is called before the first frame update
    void Start()
    {
        BVHParser parser = new BVHParser();
        data = parser.Parse(BVHFile);
        CreateJoint(data.rootJoint, Vector3.zero);
    }

    // Returns a Matrix4x4 representing a rotation aligning the up direction of an object with the given v
    Matrix4x4 RotateTowardsVector(Vector3 v)
    {
        Vector3 normalized = Vector3.Normalize(v);
        float DegX = -(90 - (Mathf.Atan2(normalized.y, normalized.z)) * Mathf.Rad2Deg);
        Matrix4x4 matX = MatrixUtils.RotateX(DegX);
        normalized = matX * normalized;
        float DegZ = 90 - (Mathf.Atan2(normalized.y, normalized.x) * Mathf.Rad2Deg);
        Matrix4x4 matZ = MatrixUtils.RotateZ(DegZ);
        return matX.inverse * matZ.inverse;
    }

    // Creates a Cylinder GameObject between two given points in 3D space
    GameObject CreateCylinderBetweenPoints(Vector3 p1, Vector3 p2, float diameter)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Matrix4x4 T = MatrixUtils.Translate((p1 + p2) / 2);
        Matrix4x4 R = RotateTowardsVector(p2-p1);
        Matrix4x4 S = MatrixUtils.Scale(new Vector3(diameter, Vector3.Distance(p1, p2)/2, diameter));
        MatrixUtils.ApplyTransform(cylinder, T*R*S);
        return cylinder;
    }

    // Creates a GameObject representing a given BVHJoint and recursively creates GameObjects for it's child joints
    GameObject CreateJoint(BVHJoint joint, Vector3 parentPosition)
    {
        joint.gameObject = new GameObject(joint.name);
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.parent = joint.gameObject.transform;
        if (joint.name == "Head")
        {
            Vector3 scale = new Vector3(8, 8, 8);
            MatrixUtils.ApplyTransform(sphere, MatrixUtils.Scale(scale));
        }
        else
        {
            Vector3 scale = new Vector3(2, 2, 2);
            MatrixUtils.ApplyTransform(sphere, MatrixUtils.Scale(scale));
        }
        MatrixUtils.ApplyTransform(joint.gameObject, MatrixUtils.Translate(parentPosition)* MatrixUtils.Translate(joint.offset));
        foreach (BVHJoint child in joint.children)
        {
            CreateJoint(child, joint.gameObject.transform.position);
            CreateCylinderBetweenPoints(joint.gameObject.transform.position, child.gameObject.transform.position, 0.5f).transform.parent = joint.gameObject.transform;
        }
        return null;
    }

    // Transforms BVHJoint according to the keyframe channel data, and recursively transforms its children
    private void TransformJoint(BVHJoint joint, Matrix4x4 parentTransform, float[] keyframe)
    {
        Matrix4x4[] order = new Matrix4x4[3];
        order[joint.rotationOrder.x] = MatrixUtils.RotateX(keyframe[joint.rotationChannels.x]); ;
        order[joint.rotationOrder.y] = MatrixUtils.RotateY(keyframe[joint.rotationChannels.y]);
        order[joint.rotationOrder.z] = MatrixUtils.RotateZ(keyframe[joint.rotationChannels.z]);
        Matrix4x4 R = order[0] * order[1] * order[2];
        Matrix4x4 T = MatrixUtils.Translate(joint.offset);
        Matrix4x4 M = parentTransform *T * R;
        MatrixUtils.ApplyTransform(joint.gameObject, M);
        foreach (BVHJoint child in joint.children)
        {
            TransformJoint(child, M, keyframe);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (animate)
        {
            timer += Time.deltaTime;
            if (timer > data.frameLength)
            {
                currFrame += (int)(timer / data.frameLength);
                if (currFrame > data.numFrames - 1)
                {
                    currFrame = currFrame % (data.numFrames-1);
                }
                Vector3 rootPos = new Vector3(data.keyframes[currFrame][data.rootJoint.positionChannels.x], data.keyframes[currFrame][data.rootJoint.positionChannels.y], data.keyframes[currFrame][data.rootJoint.positionChannels.z]);
                TransformJoint(data.rootJoint, MatrixUtils.Translate(rootPos), data.keyframes[currFrame]);
                timer = 0;
            }
        }
    }
}
