using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif
using UnityEngine.Rendering.Universal;

namespace MystifyFX {

    public class MystifyFXRendererFeature : ScriptableRendererFeature {

        class PassData {
            public CommandBuffer cmd;
            public Camera camera;
            public bool useCameraOpaqueTexture;
#if UNITY_2023_3_OR_NEWER
            public TextureHandle colorTexture;
            public TextureHandle depthTexture;
            public TextureHandle cameraOpaqueTexture;
#endif
        }

        class MystifyFXRenderPass : ScriptableRenderPass {

            enum Pass {
                CopyExact = 0,
                BlurHoriz = 1,
                BlurVert = 2
            }

            class MystifyEffectDistanceComparer : IComparer<MystifyEffect> {

                public Vector3 sortCameraPosition;
                public int Compare(MystifyEffect a, MystifyEffect b) {
                    float distanceA = (sortCameraPosition - a.transform.position).sqrMagnitude;
                    float distanceB = (sortCameraPosition - b.transform.position).sqrMagnitude;
                    return distanceB.CompareTo(distanceA);
                }
            }

            readonly PassData passData = new PassData();

            static MystifyFXRendererFeature settings;
            static Material bMat, hitMat, hexaMat;
            static Mesh quadMesh, discMesh, sphereMesh, hexaMesh, hexaWireMesh, cylinderMesh;
            static Matrix4x4 matrix;
            static RenderTextureDescriptor sourceDesc;
            static bool usesCameraOpaqueTexture;
            static readonly Plane[] planes = new Plane[6];
            static readonly List<Material> sharedMaterialsBuffer = new List<Material>(8);
            static readonly MystifyEffectDistanceComparer effectDistanceComparer = new MystifyEffectDistanceComparer();
            static Comparison<MystifyEffect> cachedEffectComparisonDelegate;

#if UNITY_2022_2_OR_NEWER
            static RTHandle source, depthRT;
#else
            static RenderTargetIdentifier source, depthRT;
#endif


            public bool Setup (MystifyFXRendererFeature settings, RenderPassEvent renderingPassEvent) {

                MystifyFXRenderPass.settings = settings;
                renderPassEvent = renderingPassEvent;
                ScriptableRenderPassInput input = ScriptableRenderPassInput.None;
                if (usesCameraDepthTexture) {
                    input |= ScriptableRenderPassInput.Depth;
                }
                usesCameraOpaqueTexture = renderPassEvent < RenderPassEvent.AfterRenderingTransparents;
                if (usesCameraOpaqueTexture) {
                    input |= ScriptableRenderPassInput.Color;
                }
                ConfigureInput(input);

                if (bMat == null) {
                    Shader shader = Shader.Find("Hidden/Kronnect/MystifyFX");
                    if (shader == null) {
                        Debug.LogWarning("Could not load MystifyFX shader.");
                    }
                    else {
                        bMat = CoreUtils.CreateEngineMaterial(shader);
                    }
                }
                if (hitMat == null) {
                    hitMat = Resources.Load<Material>("Materials/MystifyHitFX");
                }
                if (hexaMat == null) {
                    hexaMat = Resources.Load<Material>("Materials/MystifyHexa");
                }
                if (hexaWireMesh == null) {
                    hexaWireMesh = Resources.Load<Mesh>("Meshes/HexasphereWire");
                }
                return true;
            }

#if !UNITY_6000_4_OR_NEWER
#if UNITY_2023_3_OR_NEWER
            [Obsolete]
#endif
            public override void Configure (CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {

                if (bMat == null) return;

                sourceDesc = cameraTextureDescriptor;
                sourceDesc.colorFormat = RenderTextureFormat.ARGBHalf;
                sourceDesc.msaaSamples = 1;
                sourceDesc.depthBufferBits = 0;
            }
#endif

#if !UNITY_6000_4_OR_NEWER
#if UNITY_2023_3_OR_NEWER
            [Obsolete]
#endif
            public void SetupRenderTargets (ScriptableRenderer renderer) {
                source = renderer.cameraColorTargetHandle;
                depthRT = renderer.cameraDepthTargetHandle;
            }
#endif


#if !UNITY_6000_4_OR_NEWER
#if UNITY_2023_3_OR_NEWER
            [Obsolete]
#endif
            public override void Execute (ScriptableRenderContext context, ref RenderingData renderingData) {
                if (bMat == null) {
                    Debug.LogError("MystifyFX material not initialized.");
                    return;
                }

                var cmd = CommandBufferPool.Get("MystifyFX");

                passData.cmd = cmd;
                passData.camera = renderingData.cameraData.camera;

#if UNITY_2022_2_OR_NEWER
                source = renderingData.cameraData.renderer.cameraColorTargetHandle;
                depthRT = renderingData.cameraData.renderer.cameraDepthTargetHandle;
#endif

                RenderTextureDescriptor cameraDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                sourceDesc = cameraDescriptor;
                sourceDesc.colorFormat = RenderTextureFormat.ARGBHalf;
                sourceDesc.msaaSamples = 1;
                sourceDesc.depthBufferBits = 0;

                bool useCameraOpaqueTextureThisFrame = usesCameraOpaqueTexture;
                if (useCameraOpaqueTextureThisFrame) {
                    Texture cameraOpaqueTex = Shader.GetGlobalTexture(ShaderParams.cameraOpaqueTex);
                    useCameraOpaqueTextureThisFrame = cameraOpaqueTex != null;
                    if (useCameraOpaqueTextureThisFrame) {
                        cmd.SetGlobalTexture(ShaderParams.inputTex, cameraOpaqueTex);
                    }
                }
                passData.useCameraOpaqueTexture = useCameraOpaqueTextureThisFrame;

                ExecutePass(passData);
                context.ExecuteCommandBuffer(cmd);

                CommandBufferPool.Release(cmd);
            }
#endif


#if UNITY_2023_3_OR_NEWER
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {

                using (var builder = renderGraph.AddUnsafePass<PassData>("MystifyFX Pass RG", out var passData))
                {
                    UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                    passData.colorTexture = resourceData.activeColorTexture;
                    passData.depthTexture = resourceData.activeDepthTexture;
                    var cameraData = frameData.Get<UniversalCameraData>();
                    passData.camera = cameraData.camera;

                    builder.UseTexture(resourceData.activeColorTexture, AccessFlags.ReadWrite);
                    builder.UseTexture(resourceData.activeDepthTexture, AccessFlags.Read);
                    if (usesCameraDepthTexture) {    
                        builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
                    }
                    bool useCameraOpaqueTextureThisFrame = usesCameraOpaqueTexture && resourceData.cameraOpaqueTexture.IsValid();
                    passData.useCameraOpaqueTexture = useCameraOpaqueTextureThisFrame;
                    if (useCameraOpaqueTextureThisFrame) {
                        builder.UseTexture(resourceData.cameraOpaqueTexture, AccessFlags.Read);
                        passData.cameraOpaqueTexture = resourceData.cameraOpaqueTexture;
                    } else {
                        passData.cameraOpaqueTexture = TextureHandle.nullHandle;
                    }

                    builder.SetRenderFunc((PassData passData, UnsafeGraphContext context) => {
                        if (bMat == null) return;

                        CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
                        passData.cmd = cmd;

                        source = passData.colorTexture;
                        if (source.rt == null) return;

                        depthRT = passData.depthTexture;

                        if (passData.useCameraOpaqueTexture && passData.cameraOpaqueTexture.IsValid()) {
                            cmd.SetGlobalTexture(ShaderParams.inputTex, passData.cameraOpaqueTexture);
                        }

                        sourceDesc = source.rt.descriptor;
                        sourceDesc.msaaSamples = 1;
                        sourceDesc.depthBufferBits = 0;

                        ExecutePass(passData);
                    });
                }
            }
#endif

            static void DrawHexaGrid (CommandBuffer cmd, MystifyEffect effect) {
                var p = effect.sharedProfile;
                cmd.SetGlobalVector(ShaderParams.wireData, new Vector4(p.vertexDistortion * 0.1f, p.vertexDistortionSpeed, p.hexaGridScale, p.scaleFactor));
                cmd.SetGlobalVector(ShaderParams.wireData2, new Vector4(p.hexaGridRimPower, p.hexaGridNoiseStrength, p.hexaGridNoiseThreshold, p.hexaGridNoiseScale));
                cmd.SetGlobalVector(ShaderParams.wireData3, new Vector4(p.hexaGridSweepSpeed * 20f, p.hexaGridSweepAmount, p.hexaGridSweepFrequency, 0));
                cmd.SetGlobalColor(ShaderParams.wireColor, new Color(p.hexaGridColor.r, p.hexaGridColor.g, p.hexaGridColor.b, p.hexaGridColor.a * p.globalOpacity));
                cmd.SetGlobalColor(ShaderParams.noiseColor, new Color(p.hexaGridNoiseColor.r, p.hexaGridNoiseColor.g, p.hexaGridNoiseColor.b, p.hexaGridNoiseColor.a * p.globalOpacity));
                cmd.DrawMesh(hexaWireMesh, matrix, hexaMat);
            }

            static void ExecutePass (PassData passData) {

                CommandBuffer cmd = passData.cmd;

                int instancesCount = MystifyEffect.instances.Count;
                for (int k = instancesCount-1; k >= 0; k--) {
                    MystifyEffect effect = MystifyEffect.instances[k];
                    if (effect == null) {
                        MystifyEffect.instances.RemoveAt(k);
                        instancesCount--;
                    }
                }

                if (instancesCount == 0) return;

                Camera cam = passData.camera;
                int camCullingMask = cam.cullingMask;

                if (instancesCount > 1) {
                    effectDistanceComparer.sortCameraPosition = cam.transform.position;
                    if (cachedEffectComparisonDelegate == null) {
                        cachedEffectComparisonDelegate = effectDistanceComparer.Compare;
                    }
                    MystifyEffect.instances.Sort(cachedEffectComparisonDelegate);
                }

                bool needSetRenderTarget = true;
                float now = Time.timeSinceLevelLoad;
                bool inputTexRTPending = true;
                bool sourceCopyPending = !passData.useCameraOpaqueTexture;
                bool inputTexAllocated = false;

                bool blurPending = true;
                RenderBufferStoreAction currentDepthStoreAction = RenderBufferStoreAction.DontCare;
                RenderBufferLoadAction currentDepthLoadAction = RenderBufferLoadAction.DontCare;
                GeometryUtility.CalculateFrustumPlanes(cam, planes);

                float ti = now / 20f;

                for (int k = 0; k < instancesCount; k++) {
                    MystifyEffect effect = MystifyEffect.instances[k];
                    if (!effect.isActiveAndEnabled) continue;

                    if (effect.sharedProfile == null) continue;

                    if (effect.sharedProfile.useStopMotion && effect.sharedProfile.stopMotionInterval > 0) {
                        float stopMotionTimeInterval = effect.sharedProfile.stopMotionInterval / 20f;
                        ti = (int)(ti / stopMotionTimeInterval) * stopMotionTimeInterval;
                    }
                    cmd.SetGlobalFloat(ShaderParams.animationTime, ti);

                    Mesh mesh = null;
                    switch (effect.meshType) {
                        case MeshType.Existing:
                            break;
                        case MeshType.Quad:
                            if (quadMesh == null) {
                                quadMesh = Resources.Load<Mesh>("Meshes/Quad");
                            }
                            mesh = quadMesh;
                            break;
                        case MeshType.Disc:
                            if (discMesh == null) {
                                discMesh = Resources.Load<Mesh>("Meshes/Disc");
                            }
                            mesh = discMesh;
                            break;
                        case MeshType.Sphere:
                            if (sphereMesh == null) {
                                sphereMesh = Resources.Load<Mesh>("Meshes/Sphere");
                            }
                            mesh = sphereMesh;
                            break;
                        case MeshType.Hexasphere:
                            if (hexaMesh == null) {
                                hexaMesh = Resources.Load<Mesh>("Meshes/Hexasphere");
                            }
                            mesh = hexaMesh;
                            break;
                        case MeshType.Cylinder:
                            if (cylinderMesh == null) {
                                cylinderMesh = Resources.Load<Mesh>("Meshes/Cylinder");
                            }
                            mesh = cylinderMesh;
                            break;
                    }

                    bool usesMesh = mesh != null;
                    Transform t = effect.transform;
                    matrix = t.localToWorldMatrix;

                    // Check visibility
                    if (usesMesh) {
                        if ((camCullingMask & (1 << effect.gameObject.layer)) == 0) continue;
                        Bounds bounds = new Bounds(t.position, Vector3.zero);
                        Bounds meshBounds = mesh.bounds;
                        bounds.Encapsulate(matrix.MultiplyPoint3x4(meshBounds.min));
                        bounds.Encapsulate(matrix.MultiplyPoint3x4(meshBounds.max));
                        if (!GeometryUtility.TestPlanesAABB(planes, bounds)) continue;
                    }
                    else {
                        if (effect.renderers == null) {
                            effect.GetRenderers();
                        }
                        bool allHidden = true;
                        bool anyInMask = false;
                        foreach (var renderer in effect.renderers) {
                            if (renderer == null) continue;
                            if ((camCullingMask & (1 << renderer.gameObject.layer)) == 0) continue;
                            anyInMask = true;
                            if (renderer.isVisible) {
                                allHidden = false;
                            }
                            else if (effect.sharedProfile.alwaysOn) {
                                if (GeometryUtility.TestPlanesAABB(planes, renderer.bounds)) {
                                    allHidden = false;
                                    break;
                                }
                            }
                        }
                        if (!anyInMask || allHidden) continue;
                    }

                    // Update material properties
                    effect.UpdateMaterialPropertiesNow();
                    Material mat = effect.mat;
                    if (mat == null) break;

                    bool drawHexaGrid = effect.sharedProfile.hexaGrid;
                    bool requiresBlur = settings.blurMasterAmount > 0 && effect.sharedProfile.blurIntensity > 0;

                    // Copy source if needed and only once
                    if (sourceCopyPending || effect.sharedProfile.grabPass) {
                        sourceCopyPending = false;
                        if (inputTexRTPending) {
                            inputTexRTPending = false;
                            cmd.GetTemporaryRT(ShaderParams.inputTex, sourceDesc);
                            inputTexAllocated = true;
                        }
                        FullScreenBlit(cmd, source, ShaderParams.inputTex, bMat, (int)Pass.CopyExact);
                        needSetRenderTarget = true;
                    }

                    // Blur
                    if (requiresBlur && (blurPending || effect.sharedProfile.grabPass)) {
                        blurPending = false;
                        ApplyBlur(cmd, source);
                        needSetRenderTarget = true;
                    }

                    RenderBufferStoreAction depthStoreAction = effect.writesToDepth ? RenderBufferStoreAction.Store : RenderBufferStoreAction.DontCare;
                    RenderBufferLoadAction depthLoadAction = effect.sharedProfile.renderOnTop ? RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load;
                    if (needSetRenderTarget || currentDepthStoreAction != depthStoreAction || currentDepthLoadAction != depthLoadAction) {
                        needSetRenderTarget = false;
                        currentDepthStoreAction = depthStoreAction;
                        currentDepthLoadAction = depthLoadAction;
                        if (currentDepthStoreAction == RenderBufferStoreAction.DontCare && currentDepthLoadAction == RenderBufferLoadAction.DontCare) {
                            cmd.SetRenderTarget(source, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
                        }
                        else {
                            cmd.SetRenderTarget(source, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, depthRT, depthLoadAction, depthStoreAction);
                        }
                    }

                    // Draw effects
                    int hitsCount = effect.hitRequests.Count;
                    if (usesMesh) {

                        cmd.DrawMesh(mesh, matrix, mat);

                        if (drawHexaGrid) {
                            drawHexaGrid = false;
                            DrawHexaGrid(cmd, effect);
                        }

                        // Draw hits
                        cmd.SetGlobalFloat(ShaderParams.scale, effect.sharedProfile.scaleFactor);
                        for (int j = 0; j < hitsCount; j++) {
                            HitRequest request = effect.hitRequests[j];
                            Color hitColor = request.color;
                            hitColor.a = request.intensity;
                            cmd.SetGlobalColor(ShaderParams.hitColor, hitColor);
                            Vector4 hitPosition = new Vector4(request.position.x, request.position.y, request.position.z, request.noiseScale);
                            cmd.SetGlobalVector(ShaderParams.hitPosition, hitPosition);
                            cmd.SetGlobalVector(ShaderParams.hitData, new Vector4(request.radius * request.radius, request.speed, request.startTime, request.noiseAmount));
                            if (now - request.startTime > 4f) {
                                effect.hitRequests.RemoveAt(j);
                                hitsCount--;
                            }
                            cmd.DrawMesh(mesh, matrix, hitMat);
                        }
                    }
                    else {
                        foreach (var renderer in effect.renderers) {
                            if (renderer == null) continue;
                            if ((camCullingMask & (1 << renderer.gameObject.layer)) == 0) continue;
                            sharedMaterialsBuffer.Clear();
                            renderer.GetSharedMaterials(sharedMaterialsBuffer);
                            int sharedMaterialsCount = sharedMaterialsBuffer.Count;
                            int subMeshesCount = sharedMaterialsCount > 0 ? sharedMaterialsCount : 1;
                            for (int i = 0; i < subMeshesCount; i++) {
                                bool useObjectTexture = effect.sharedProfile.textureSource == TextureSource.Object || effect.sharedProfile.alphaCutoff > 0;
                                Material matObject = i < sharedMaterialsCount ? sharedMaterialsBuffer[i] : null;
                                Texture mainTex = (useObjectTexture && matObject != null) ? matObject.mainTexture : null;
                                if (useObjectTexture && mainTex != null) {
                                    cmd.SetGlobalTexture(ShaderParams.cutOutTex, mainTex);
                                    Vector2 scale = matObject.mainTextureScale;
                                    Vector2 offset = matObject.mainTextureOffset;
                                    cmd.SetGlobalVector(ShaderParams.cutOutTexST, new Vector4(scale.x, scale.y, offset.x, offset.y));
                                } else {
                                    cmd.SetGlobalTexture(ShaderParams.cutOutTex, Texture2D.whiteTexture);
                                    cmd.SetGlobalVector(ShaderParams.cutOutTexST, new Vector4(1, 1, 0, 0));
                                }
                                cmd.DrawRenderer(renderer, mat, i);

                                if (drawHexaGrid) {
                                    drawHexaGrid = false;
                                    matrix = renderer.transform.localToWorldMatrix;
                                    DrawHexaGrid(cmd, effect);
                                }

                                // Draw hits
                                for (int j = 0; j < hitsCount; j++) {
                                    HitRequest request = effect.hitRequests[j];
                                    cmd.SetGlobalFloat(ShaderParams.scale, effect.sharedProfile.scaleFactor);
                                    Color hitColor = request.color;
                                    hitColor.a = request.intensity;
                                    cmd.SetGlobalColor(ShaderParams.hitColor, hitColor);
                                    Vector4 hitPosition = new Vector4(request.position.x, request.position.y, request.position.z, request.noiseScale);
                                    cmd.SetGlobalVector(ShaderParams.hitPosition, hitPosition);
                                    cmd.SetGlobalVector(ShaderParams.hitData, new Vector4(request.radius * request.radius, request.speed, request.startTime, request.noiseAmount));
                                    if (now - request.startTime > 4f) {
                                        effect.hitRequests.RemoveAt(j);
                                        hitsCount--;
                                    }
                                    cmd.DrawRenderer(renderer, hitMat, i);
                                }
                            }
                        }
                    }
                }

                if (inputTexAllocated) {
                    cmd.ReleaseTemporaryRT(ShaderParams.inputTex);
                }
            }

            static Mesh _fullScreenMesh;

            static Mesh fullscreenMesh {
                get {
                    if (_fullScreenMesh != null) {
                        return _fullScreenMesh;
                    }
                    Mesh val = new Mesh();
                    _fullScreenMesh = val;
                    _fullScreenMesh.SetVertices(new List<Vector3> {
            new Vector3 (-1f, -1f, 0f),
            new Vector3 (-1f, 1f, 0f),
            new Vector3 (1f, -1f, 0f),
            new Vector3 (1f, 1f, 0f)
        });
                    _fullScreenMesh.SetUVs(0, new List<Vector2> {
            new Vector2 (0f, 0f),
            new Vector2 (0f, 1f),
            new Vector2 (1f, 0f),
            new Vector2 (1f, 1f)
        });
                    _fullScreenMesh.SetIndices(new int[6] { 0, 1, 2, 2, 1, 3 }, (MeshTopology)0, 0, false);
                    _fullScreenMesh.UploadMeshData(true);
                    return _fullScreenMesh;
                }
            }

            static void FullScreenBlit (CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material material, int passIndex) {
                destination = new RenderTargetIdentifier(destination, 0, CubemapFace.Unknown, -1);
                cmd.SetRenderTarget(destination);
                cmd.SetGlobalTexture(ShaderParams.mainTex, source);
                cmd.DrawMesh(fullscreenMesh, Matrix4x4.identity, material, 0, passIndex);
            }

            public void Cleanup () {
                CoreUtils.Destroy(bMat);
            }

            static void ApplyBlur (CommandBuffer cmd, RenderTargetIdentifier source) {

                int size;
                RenderTextureDescriptor rtBlurDesc = sourceDesc;

                float blurIntensity = settings.blurMasterAmount;
                if (blurIntensity < 1f) {
                    size = (int)Mathf.Lerp(rtBlurDesc.width, 512, blurIntensity);
                }
                else {
                    size = (int)(512 / blurIntensity);
                }
                float aspectRatio = (float)sourceDesc.height / sourceDesc.width;
                rtBlurDesc.width = size;
                rtBlurDesc.height = Mathf.Max(1, (int)(size * aspectRatio));
                cmd.ReleaseTemporaryRT(ShaderParams.blurRT);
                cmd.GetTemporaryRT(ShaderParams.blurRT, rtBlurDesc, FilterMode.Bilinear);

                float ratio = (float)sourceDesc.width / size;
                float blurScale = blurIntensity > 1f ? 1f : blurIntensity;

                cmd.SetGlobalFloat(ShaderParams.highlightThreshold, settings.blurHighlightThreshold);
                cmd.SetGlobalFloat(ShaderParams.highlightIntensity, settings.blurHighlightIntensity);
                cmd.GetTemporaryRT(ShaderParams.tempBlurDownscaling, rtBlurDesc, FilterMode.Bilinear);
                cmd.SetGlobalFloat(ShaderParams.blurScale, blurScale * ratio);
                FullScreenBlit(cmd, source, ShaderParams.tempBlurDownscaling, bMat, (int)Pass.BlurHoriz);
                cmd.SetGlobalFloat(ShaderParams.blurScale, blurScale);
                FullScreenBlit(cmd, ShaderParams.tempBlurDownscaling, ShaderParams.blurRT, bMat, (int)Pass.BlurVert);
                cmd.ReleaseTemporaryRT(ShaderParams.tempBlurDownscaling);

                BlurThis(cmd, rtBlurDesc, ShaderParams.blurRT, rtBlurDesc.width, rtBlurDesc.height, bMat, blurScale);
                BlurThis(cmd, rtBlurDesc, ShaderParams.blurRT, rtBlurDesc.width, rtBlurDesc.height, bMat, blurScale);
                BlurThis(cmd, rtBlurDesc, ShaderParams.blurRT, rtBlurDesc.width, rtBlurDesc.height, bMat, blurScale);
            }


            static void BlurThis (CommandBuffer cmd, RenderTextureDescriptor desc, int rt, int width, int height, Material blurMat, float blurScale = 1f) {
                desc.width = width;
                desc.height = height;
                cmd.GetTemporaryRT(ShaderParams.tempBlurRT, desc, FilterMode.Bilinear);
                cmd.SetGlobalFloat(ShaderParams.blurScale, blurScale);
                FullScreenBlit(cmd, rt, ShaderParams.tempBlurRT, blurMat, (int)Pass.BlurHoriz);
                FullScreenBlit(cmd, ShaderParams.tempBlurRT, rt, blurMat, (int)Pass.BlurVert);
                cmd.ReleaseTemporaryRT(ShaderParams.tempBlurRT);
            }
        }


        MystifyFXRenderPass mystifyFXPass;

        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        [Range(0, 5)]
        public float blurMasterAmount = 1f;
        public float blurHighlightThreshold = 1f;
        public float blurHighlightIntensity;

        [Tooltip("Specify which cameras can render LocalBlurFX effects")]
        public LayerMask allowedCameras = -1;

        public static bool installed;
        public static bool usesCameraDepthTexture;

        void OnDisable () {
            if (mystifyFXPass != null) {
                mystifyFXPass.Cleanup();
            }
            installed = false;
        }

        void OnValidate () {
            blurHighlightThreshold = Mathf.Max(0, blurHighlightThreshold);
            blurHighlightIntensity = Mathf.Max(0, blurHighlightIntensity);
        }

        public override void Create () {
            name = "MystifyFX";
            mystifyFXPass = new MystifyFXRenderPass();
        }

#if !UNITY_2023_3_OR_NEWER
        public override void SetupRenderPasses (ScriptableRenderer renderer, in RenderingData renderingData) {

            CameraData cameraData = renderingData.cameraData;
            Camera cam = cameraData.camera;

            if (cam.cameraType == CameraType.Preview) return;

            if ((allowedCameras & (1 << cam.gameObject.layer)) == 0) return;

            if (cam.targetTexture != null && cam.targetTexture.format == RenderTextureFormat.Depth) return; // ignore depth pre-pass cams!

            mystifyFXPass.SetupRenderTargets(renderer);
        }
#endif

        public override void AddRenderPasses (ScriptableRenderer renderer, ref RenderingData renderingData) {
            installed = true;
            CameraData cameraData = renderingData.cameraData;
            Camera cam = cameraData.camera;

            if (cam.cameraType == CameraType.Preview) return;

            if ((allowedCameras & (1 << cam.gameObject.layer)) == 0) return;

            if (cam.targetTexture != null && cam.targetTexture.format == RenderTextureFormat.Depth) return; // ignore depth pre-pass cams!

            if (mystifyFXPass.Setup(this, renderPassEvent)) {
                renderer.EnqueuePass(mystifyFXPass);
            }
        }
    }
}
