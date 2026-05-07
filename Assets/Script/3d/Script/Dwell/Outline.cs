using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class Outline : MonoBehaviour
{
    private static HashSet<Mesh> registeredMeshes = new HashSet<Mesh>();
    [SerializeField] private static Material cachedMaskMaterial;
    [SerializeField] private static Material cachedFillMaterial;

    private Renderer[] renderers;
    private MaterialPropertyBlock mpb;

    [SerializeField] private float outlineWidth = 10f;
    
    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    void OnEnable()
    {
        EnsureMaterialExist();
        ApplyMaterials();
        LoadSmoothNormals();

        // Start with progress at zero so the outline is invisible until a gaze begins.
        // No toggle flag needed — _OutlineWidth and _Progress drive visibility.
        SetProgress(0f);
    }

    private void EnsureMaterialExist()
    {
        if (cachedMaskMaterial == null)
        {
            cachedMaskMaterial = Instantiate(Resources.Load<Material>("Materials/Outline/3D/OutlineMask"));
            cachedMaskMaterial.name = "OutlineMask (Instance)";
        }

        if (cachedFillMaterial == null)
        {
            cachedFillMaterial = Instantiate(Resources.Load<Material>("Materials/Outline/3D/OutlineFill"));
            cachedFillMaterial.name = "OutlineFill (Instance)";
        }
    }

    void ApplyMaterials()
    {
        foreach (var renderer in renderers)
        {
            var mats = renderer.sharedMaterials.ToList();
            if (!mats.Contains(cachedMaskMaterial)) mats.Add(cachedMaskMaterial);
            if (!mats.Contains(cachedFillMaterial)) mats.Add(cachedFillMaterial);
            renderer.sharedMaterials = mats.ToArray();
        }
    }

    void LoadSmoothNormals()
    {
        foreach (var meshFilter in GetComponentsInChildren<MeshFilter>())
        {
            Mesh mesh = meshFilter.mesh;
            if (!registeredMeshes.Add(mesh)) continue;

            var smoothNormals = ComputeSmoothNormals(mesh);
            mesh.SetUVs(3, smoothNormals);
        }
    }

    List<Vector3> ComputeSmoothNormals(Mesh mesh)
    {
        var groups = mesh.vertices.Select((v, i) => new KeyValuePair<Vector3, int>(v, i))
                                  .GroupBy(p => p.Key);

        var smoothNormals = new List<Vector3>(mesh.normals);

        foreach (var group in groups)
        {
            if (group.Count() == 1) continue;

            Vector3 avg = Vector3.zero;
            foreach (var pair in group)
                avg += smoothNormals[pair.Value];
            avg.Normalize();

            foreach (var pair in group)
                smoothNormals[pair.Value] = avg;
        }

        return smoothNormals;
    }

    /// <summary>
    /// The ONLY runtime driver of outline state.
    /// Pass progress = 0 to hide, 0–1 to animate the fill color.
    /// </summary>
    public void SetProgress(float progress)
    {
        foreach (var renderer in renderers)
        {
            renderer.GetPropertyBlock(mpb);
            mpb.SetFloat("_Progress", progress);
            mpb.SetFloat("_OutlineWidth", outlineWidth);
            UpdateZTest(mpb);
            renderer.SetPropertyBlock(mpb);
        }
    }

    void UpdateZTest(MaterialPropertyBlock block)
    {
        float zMask = (float)UnityEngine.Rendering.CompareFunction.Always;
        float zFill = (float)UnityEngine.Rendering.CompareFunction.LessEqual;

        block.SetFloat("_ZTestMask", zMask);
        block.SetFloat("_ZTestFill", zFill);
    }

    public void ApplyGlobalColors()
    {
        foreach (var renderer in renderers)
        {
            renderer.GetPropertyBlock(mpb);
            mpb.SetColor("_IdleColor", GlobalInput.Instance.idleColor);
            mpb.SetColor("_MidColor", GlobalInput.Instance.midColor);
            mpb.SetColor("_ActiveColor", GlobalInput.Instance.activeColor);
            renderer.SetPropertyBlock(mpb);
        }
    }

    /// <summary>
    /// Hides the outline by resetting progress to zero.
    /// Replaces the old SetOutlineEnabled(false) / ResetOutline() calls.
    /// </summary>
    public void ResetOutline()
    {
        SetProgress(0f);
    }

    void OnDisable()
    {
        foreach (var renderer in renderers)
            renderer.SetPropertyBlock(null);
    }
}