using UnityEngine;

[DisallowMultipleComponent]
public class Flicker : MonoBehaviour
{
    [SerializeField] private Material flickerFillMaterial;
    private Renderer[] renderers;
    private MaterialPropertyBlock mpb;
    private bool isFlickering = false;
    private float flickerTimer = 0f;
    private float flickerStartTime = -1f; // phase anchor

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();
        EnsureMaterialExists();
    }

    void OnValidate()
    {
        if (flickerFillMaterial == null)
            flickerFillMaterial = Resources.Load<Material>("Materials/Flicker/3D/FlickerFillMat");
    }
    void OnEnable()  { }
    void OnDisable() { RemoveMaterial(); StopFlickerState(); }

    void Update()
    {
        if (!enabled || !isFlickering || flickerFillMaterial == null || GlobalInput.Instance == null)
            return;

        // Anchor phase to first frame of this activation
        if (flickerStartTime < 0f)
            flickerStartTime = Time.unscaledTime;

        float elapsed = Time.unscaledTime - flickerStartTime;
        float phase = (elapsed * GlobalInput.Instance.flickerHz) % 1.0f;
        bool isOn = phase < 0.5f; // hard square wave, self-correcting

        foreach (var r in renderers)
        {
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor",      GlobalInput.Instance.idleColor);
            mpb.SetColor("_FlickerOnColor", GlobalInput.Instance.flickerOn);
            mpb.SetFloat("_FlickerState",   isOn ? 1f : 0f); // replaces _FlickerHz push
            r.SetPropertyBlock(mpb);
        }

        flickerTimer += Time.deltaTime;
        if (flickerTimer >= GlobalInput.Instance.flickerDuration)
        {
            StopFlickerState();
            enabled = false;
        }
    }

    public void StartFlicker()
    {
        ApplyMaterial();
        if (!enabled) enabled = true;
        isFlickering  = true;
        flickerTimer  = 0f;
        flickerStartTime = -1f; // reset phase anchor for clean onset
    }

    private void StopFlickerState()
    {
        isFlickering     = false;
        flickerTimer     = 0f;
        flickerStartTime = -1f;

        // Zero out flicker on all renderers
        foreach (var r in renderers)
        {
            r.GetPropertyBlock(mpb);
            mpb.SetFloat("_FlickerState", 0f);
            r.SetPropertyBlock(mpb);
        }
    }

    private void ApplyMaterial()
    {
        foreach (var r in renderers)
        {
            var mats = r.sharedMaterials;
            bool exists = false;

            // Check if material already exists (handling potential instances)
            foreach (var m in mats)
            {
                if (m == flickerFillMaterial || (m != null && flickerFillMaterial != null && 
                    m.name.Replace(" (Instance)", "") == flickerFillMaterial.name.Replace(" (Instance)", "")))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                var newMats = new Material[mats.Length + 1];
                mats.CopyTo(newMats, 0);
                newMats[mats.Length] = flickerFillMaterial;
                r.sharedMaterials = newMats; // Use sharedMaterials to avoid instantiation leaks
            }
        }
    }

    private void RemoveMaterial()
    {
        foreach (var r in renderers)
        {
            var mats = new System.Collections.Generic.List<Material>(r.sharedMaterials);
            
            // Remove by reference or matching name
            mats.RemoveAll(m => m == flickerFillMaterial || (m != null && flickerFillMaterial != null && 
                m.name.Replace(" (Instance)", "") == flickerFillMaterial.name.Replace(" (Instance)", "")));
                
            r.sharedMaterials = mats.ToArray(); // Use sharedMaterials to avoid instantiation leaks
        }
    }

    private void EnsureMaterialExists()
    {
        if (flickerFillMaterial != null)
            return;
        flickerFillMaterial = Resources.Load<Material>("Materials/Flicker/3D/FlickerFillMat");

        if (flickerFillMaterial != null)
        {
            flickerFillMaterial = Instantiate(flickerFillMaterial);
            flickerFillMaterial.name = "FlickerFill (Instance)";
        }
        else
        {
            Debug.LogError("[Flicker] FlickerFillMat not found in Resources!");
        }
    }
}