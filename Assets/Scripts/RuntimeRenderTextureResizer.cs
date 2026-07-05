using UnityEngine;
using UnityEngine.UI;

public class RuntimeRenderTextureResizer : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private RawImage targetRawImage;
    [SerializeField] private RenderTexture sourceRenderTexture;

    private RenderTexture runtimeRenderTexture;
    private int currentWidth;
    private int currentHeight;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        if (sourceRenderTexture == null && targetCamera != null)
        {
            sourceRenderTexture = targetCamera.targetTexture;
        }

        if (targetRawImage == null)
        {
            RawImage[] rawImages = Resources.FindObjectsOfTypeAll<RawImage>();
            for (int i = 0; i < rawImages.Length; i++)
            {
                if (rawImages[i].name == "RawImage" && rawImages[i].GetComponentInParent<Canvas>(true) != null && rawImages[i].GetComponentInParent<Canvas>(true).name == "ShowCanvas")
                {
                    targetRawImage = rawImages[i];
                    break;
                }
            }
        }

        ResizeIfNeeded(true);
    }

    private void Update()
    {
        ResizeIfNeeded(false);
    }

    private void OnDestroy()
    {
        if (runtimeRenderTexture != null)
        {
            if (targetCamera != null && targetCamera.targetTexture == runtimeRenderTexture)
            {
                targetCamera.targetTexture = sourceRenderTexture;
            }
            if (targetRawImage != null && targetRawImage.texture == runtimeRenderTexture)
            {
                targetRawImage.texture = sourceRenderTexture;
            }
            runtimeRenderTexture.Release();
            Destroy(runtimeRenderTexture);
        }
    }

    private void ResizeIfNeeded(bool force)
    {
        int width = Mathf.Max(1, Screen.width);
        int height = Mathf.Max(1, Screen.height);
        if (!force && width == currentWidth && height == currentHeight)
        {
            return;
        }

        currentWidth = width;
        currentHeight = height;

        RenderTexture oldTexture = runtimeRenderTexture;
        RenderTextureDescriptor descriptor = sourceRenderTexture != null
            ? sourceRenderTexture.descriptor
            : new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 24);
        descriptor.width = width;
        descriptor.height = height;

        runtimeRenderTexture = new RenderTexture(descriptor)
        {
            name = $"RuntimeRT_{width}x{height}"
        };
        runtimeRenderTexture.Create();

        if (targetCamera != null)
        {
            targetCamera.targetTexture = runtimeRenderTexture;
        }
        if (targetRawImage != null)
        {
            targetRawImage.texture = runtimeRenderTexture;
        }

        if (oldTexture != null)
        {
            oldTexture.Release();
            Destroy(oldTexture);
        }
    }
}
