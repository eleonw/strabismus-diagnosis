﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine;
using ViveSR.anipal.Eye;
using System.Runtime.InteropServices;
using System;

public class GazeRay : MonoBehaviour
{
    [SerializeField] private LineRenderer GazeRayRenderer;
    [SerializeField] private GameObject TargetObject;

    private GazeIndex BaseEye = GazeIndex.RIGHT;
    private Transform mainCamTransform;

    // Start is called before the first frame update
    void Start() {
        mainCamTransform = Camera.main.transform;
        if (!SRanipal_Eye_Framework.Instance.EnableEye) {
            Debug.Log("Eye is not enabled in SRanipal Eye Framework");
            enabled = false;
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING &&
                        SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.NOT_SUPPORT)
            return;

        Vector3 leftOriginLocal, leftDirectionLocal, leftOriginGlobal, leftDirectionGlobal,
            rightOriginLocal, rightDirectionLocal, rightOriginGlobal, rightDirectionGlobal;

        EyeData eyeData;
        SingleEyeData leftData, rightData;
        RaycastHit hitInfo;

        SRanipal_Eye.GetEyeData(out eyeData);
        leftData = eyeData.verbose_data.left;
        rightData = eyeData.verbose_data.right;
        
        SRanipal_Eye.GetGazeRay(GazeIndex.LEFT, out leftOriginLocal, out leftDirectionLocal, eyeData);
        SRanipal_Eye.GetGazeRay(GazeIndex.RIGHT, out rightOriginLocal, out rightDirectionLocal, eyeData);

        leftOriginGlobal = mainCamTransform.TransformPoint(leftOriginLocal);
        leftDirectionGlobal = mainCamTransform.TransformDirection(leftDirectionLocal);
        rightOriginGlobal = mainCamTransform.TransformPoint(rightOriginLocal);
        rightDirectionGlobal = mainCamTransform.TransformDirection(rightDirectionLocal);

        GlobalEyeData globalEyeData = new GlobalEyeData();
        globalEyeData.leftOrigin = leftOriginGlobal;
        globalEyeData.leftDirection = leftDirectionGlobal;
        globalEyeData.rightOrigin = rightOriginGlobal;
        globalEyeData.rightDirection = rightDirectionGlobal;

        Vector3 baseOrigin, baseDirection, targetOrigin, targetDirection;
        if (BaseEye == GazeIndex.LEFT) {
            baseOrigin = leftOriginGlobal;
            baseDirection = leftDirectionGlobal;
            targetOrigin = rightOriginGlobal;
            targetDirection = rightDirectionGlobal;
        } else {
            baseOrigin = rightOriginGlobal;
            baseDirection = rightDirectionGlobal;
            targetOrigin = leftOriginGlobal;
            targetDirection = leftDirectionGlobal;
        }

        if (Physics.Raycast(baseOrigin, baseDirection, out hitInfo, Mathf.Infinity)) {
            globalEyeData.hit = true;
            globalEyeData.targetPosition = hitInfo.point;
            globalEyeData.expectedDirection = Vector3.Normalize(hitInfo.point - targetOrigin);
            globalEyeData.strabismusDegree = Vector3.Angle(targetDirection, globalEyeData.expectedDirection);
            TargetObject.SendMessage("Hit");
        } else {
            TargetObject.SendMessage("UnHit");
        }

        IPC.Instance.SendEyeData(globalEyeData);

        Debug.Log("originGlobal: " + "x: " + baseOrigin.x.ToString("F4") + "y: " + baseOrigin.y.ToString("F4") + "z: " + baseOrigin.z.ToString("F4")
            + "\n  direction Global: " + "x: " + baseDirection.x.ToString("F4") + "y: " + baseDirection.y.ToString("F4") + "z: " + baseDirection.z.ToString("F4"));

        GazeRayRenderer.SetPosition(0, baseOrigin - Camera.main.transform.up * 0.05f);
        GazeRayRenderer.SetPosition(1, baseOrigin + baseDirection * 10);
    }
}
