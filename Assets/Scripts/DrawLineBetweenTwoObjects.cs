using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static System.Diagnostics.Debug;
using System.Diagnostics;

public class DrawLineBetweenTwoObjects : MonoBehaviour
{
    private Transform start;
    private Transform end;
    private Vector3 staticStart;

    public float LineThickness = 10f;

    //public float OverrideLineThickness = -1f;

    //public float EndLineThickness => OverrideLineThickness > -1f ? OverrideLineThickness : LineThickness;

    //public Color LineColor = Color.green;

    public Material LineMaterial;

    //public Transform OverridereflectionProbe = null;

    public LineRenderer _renderer;

    long maxTimeout = 1000;

    private Stopwatch watch = Stopwatch.StartNew();

    public float startCutoffDistance;
    public float endCutoffDistance;

    public void Initialize(Transform start, Transform end, float startCutoffDistance, float endCutoffDistance)
    {
        this.start = start;
        this.end = end;

        this.startCutoffDistance = startCutoffDistance;
        this.endCutoffDistance = endCutoffDistance;

        _renderer = gameObject.AddComponent<LineRenderer>();

        CreateLineRenderer(LineMaterial);
    }

    public void Initialize(Vector3 start, Transform end, float startCutoffDistance, float endCutoffDistance)
    {
        this.staticStart = start;
        this.end = end;

        this.startCutoffDistance = startCutoffDistance;
        this.endCutoffDistance = endCutoffDistance;

        _renderer = gameObject.AddComponent<LineRenderer>();

        CreateLineRenderer(LineMaterial);
    }

    private Vector3 getStartPos()
    {
        if (start == null)
            return staticStart;
        else
            return start.position;
    }

    public void CreateLineRenderer(Material material)
    {
        _renderer.material = material;
        _renderer.textureMode = LineTextureMode.Tile;
        _renderer.alignment = LineAlignment.View;
        _renderer.startWidth = LineThickness;
        _renderer.endWidth = LineThickness;
        //_renderer.startColor = LineColor;
        //_renderer.endColor = LineColor;
        _renderer.generateLightingData = true;
        setStartAndEndPos();
        //_renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes;
        //_renderer.probeAnchor = OverridereflectionProbe;
        float width = _renderer.endWidth;
        _renderer.material.mainTextureScale = new Vector2(1f / width, 1.0f);

        //_renderer.material.mainTextureScale = new Vector2(.01f, .01f);
    }

    private void Update()
    {
        setStartAndEndPos();
    }

    public Vector2 middlePos()
    {
        Vector3 start = getStartPos();
        return new Vector2((start.x + end.position.x) / 2, (start.y + end.position.y) / 2);
    }

    private void setStartAndEndPos()
    {
        Vector3 startToUse = start == null ? staticStart : start.position;

        Vector3 endPos = Vector3.MoveTowards(end.position, startToUse, endCutoffDistance);
        Vector3 startPos = Vector3.MoveTowards(startToUse, end.position, startCutoffDistance);
        _renderer?.SetPositions(new Vector3[] { startPos, endPos });
    }
}