using System;
using UnityEngine;
using UnityEngine.Rendering;

public class MainCamera : MonoBehaviour
{
    Portal[] portals;

    void Awake()
    {
        portals = FindObjectsOfType<Portal>();
    }

    void RenderingPortal(ScriptableRenderContext context, Camera cam)
    {
        foreach (var p in portals)
        {
            p.PrePortalRender();
        }

        foreach (var p in portals)
        {
            p.Render(context);
        }

        foreach (var p in portals)
        {
            p.PostPortalRender();
        }
    }

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += RenderingPortal;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= RenderingPortal;
    }
}