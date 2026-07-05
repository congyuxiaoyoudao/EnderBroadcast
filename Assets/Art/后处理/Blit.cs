using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;

/*
 * @Cyanilux https://github.com/Cyanilux/URP_BlitRenderFeature
 * Modified for Unity 2022.3 / URP 14 (non-RenderGraph path)
*/

namespace Cyan {
    public class Blit : ScriptableRendererFeature {

        [System.Serializable]
        public class FeatureSettings {

            public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingTransparents;

            [System.Flags]
            public enum Requirements {
                None                = 0,
                DepthTexture        = 1 << 0,
                NormalsTexture      = 1 << 1,
                OpaqueTexture       = 1 << 2,
                MotionVectorTexture = 1 << 3,
            }

            [Header("Inputs")]
            public Requirements requirements;
            public string[] globalTextures = new string[]{};

            public enum Destination {
                CameraColor,
                GlobalTexture
            }

            [Header("Destination")]
            public Destination dstType = Destination.CameraColor;
            public bool showInSceneView = true;
            public string dstGlobalTexture = "_BlitPassTexture";
            public enum FormatMode { SameAsCamera, CameraFormatWithAlpha, GraphicsFormat }
            public FormatMode colorFormat;
            public GraphicsFormat format;
            public bool bindDepthStencilBuffer;

            [Header("Material")]
            public Material blitMaterial;
            public int blitPassIndex;
        }

        public FeatureSettings settings;

        class BlitPass : ScriptableRenderPass {

            private FeatureSettings settings;
            private int dstGlobalTextureID;
            private int[] globalTextures;
            private RenderTextureDescriptor blitTextureDescriptor;

            public BlitPass(FeatureSettings settings) {
                this.settings = settings;
                dstGlobalTextureID = Shader.PropertyToID(settings.dstGlobalTexture);

                int len = settings.globalTextures.Length;
                globalTextures = new int[len];
                for (int i = 0; i < len; i++) {
                    globalTextures[i] = Shader.PropertyToID(settings.globalTextures[i]);
                }
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
                var descriptor = renderingData.cameraData.cameraTargetDescriptor;
                descriptor.depthBufferBits = 0;

                if (settings.colorFormat == FeatureSettings.FormatMode.CameraFormatWithAlpha) {
                    descriptor.graphicsFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
                } else if (settings.colorFormat == FeatureSettings.FormatMode.GraphicsFormat) {
                    descriptor.graphicsFormat = settings.format;
                }

                blitTextureDescriptor = descriptor;

                cmd.GetTemporaryRT(dstGlobalTextureID, descriptor);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
                CommandBuffer cmd = CommandBufferPool.Get("Blit Pass");
                cmd.Clear();

                // Set global textures
                for (int i = 0; i < globalTextures.Length; i++) {
                    cmd.SetGlobalTexture(globalTextures[i], dstGlobalTextureID);
                }

                RenderTargetIdentifier cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle.nameID;

                if (settings.dstType == FeatureSettings.Destination.GlobalTexture) {
                    // Blit to global texture
                    cmd.Blit(cameraColorTarget, dstGlobalTextureID, settings.blitMaterial, settings.blitPassIndex);
                    cmd.SetGlobalTexture(dstGlobalTextureID, dstGlobalTextureID);
                } else {
                    if (settings.blitMaterial == null) {
                        // Simple copy
                        cmd.Blit(cameraColorTarget, BuiltinRenderTextureType.CameraTarget);
                    } else {
                        // Blit camera color to temp RT, then back
                        int tempRT = Shader.PropertyToID("_TempBlitRT");
                        cmd.GetTemporaryRT(tempRT, blitTextureDescriptor);
                        cmd.Blit(cameraColorTarget, tempRT, settings.blitMaterial, settings.blitPassIndex);
                        cmd.Blit(tempRT, cameraColorTarget);
                        cmd.ReleaseTemporaryRT(tempRT);
                    }
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void OnCameraCleanup(CommandBuffer cmd) {
                if (dstGlobalTextureID != -1)
                    cmd.ReleaseTemporaryRT(dstGlobalTextureID);
            }
        }

        private BlitPass m_ScriptablePass;

        public override void Create() {
            settings ??= new FeatureSettings();
            if (settings.blitMaterial != null) {
                settings.blitPassIndex = Mathf.Clamp(settings.blitPassIndex, -1, settings.blitMaterial.passCount - 1);
            }
            m_ScriptablePass = new BlitPass(settings) {
                renderPassEvent = settings.injectionPoint
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            if (renderingData.cameraData.isPreviewCamera) return;
            if (settings.dstType == FeatureSettings.Destination.CameraColor && !settings.showInSceneView && renderingData.cameraData.isSceneViewCamera) return;

            m_ScriptablePass.ConfigureInput((ScriptableRenderPassInput)settings.requirements);
            renderer.EnqueuePass(m_ScriptablePass);
        }
    }
}
