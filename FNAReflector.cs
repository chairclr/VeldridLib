using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;
using Vulkan;

namespace VeldridLib;

internal class FNAReflector
{
    private readonly static FieldInfo FNADeviceField = typeof(GraphicsDevice).GetField("GLDevice", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static Veldrid.GraphicsDevice? VeldridGraphicsDevice;

    private static unsafe FNA3DDevice* GetFNADevice(GraphicsDevice graphicsDevice)
    {
        nint value = (nint)FNADeviceField.GetValue(graphicsDevice)!;

        return (FNA3DDevice*)value;
    }

    public static unsafe Veldrid.GraphicsDevice GetGraphicsDevice(GraphicsDevice graphicsDevice)
    {
        if (VeldridGraphicsDevice is not null)
        {
            return VeldridGraphicsDevice;
        }

        FNA3DDevice* fnaDevice = GetFNADevice(graphicsDevice);
        Veldrid.GraphicsBackend backend = DetermineFNADriver(fnaDevice);

        return backend switch
        {
            Veldrid.GraphicsBackend.Vulkan => VeldridGraphicsDevice = VulkanDeviceHelper.MakeDevice(fnaDevice),
            Veldrid.GraphicsBackend.OpenGL => VeldridGraphicsDevice = OepnGLDeviceHelper.MakeDevice(fnaDevice),
            Veldrid.GraphicsBackend.Direct3D11 => VeldridGraphicsDevice = D3D11DeviceHelper.MakeDevice(fnaDevice),
            _ => throw new Exception($"Unsupported FNA3D Driver '{backend}'"),
        };
    }

    private static unsafe Veldrid.GraphicsBackend DetermineFNADriver(FNA3DDevice* fnaDevice)
    {
        FNA3D_SysRendererEXT sysRenderer;
        ((delegate*<FNA3DDevice*, FNA3D_SysRendererEXT*, void>)fnaDevice->GetSysRenderer)(fnaDevice, &sysRenderer);

        return sysRenderer.rendererType switch
        {
            FNA3D_SysRendererTypeEXT.FNA3D_RENDERER_TYPE_VULKAN_EXT => Veldrid.GraphicsBackend.Vulkan,
            FNA3D_SysRendererTypeEXT.FNA3D_RENDERER_TYPE_OPENGL_EXT => Veldrid.GraphicsBackend.OpenGL,
            FNA3D_SysRendererTypeEXT.FNA3D_RENDERER_TYPE_D3D11_EXT => Veldrid.GraphicsBackend.Direct3D11,
            _ => throw new Exception($"Unknown FNA3D SysRenderer '{sysRenderer.rendererType}'"),
        };
    }

#pragma warning disable CS0649
    [StructLayout(LayoutKind.Sequential)]
    internal struct FNA3DDevice
    {
        public nint DestroyDevice;
        public nint SwapBuffers;
        public nint Clear;
        public nint DrawIndexedPrimitives;
        public nint DrawInstancedPrimitives;
        public nint Drawprimitives;
        public nint SetViewport;
        public nint SetScissorRect;
        public nint GetBlendFactor;
        public nint SetBlendFactor;
        public nint GetMultiSampleMask;
        public nint SetMultiSampleMask;
        public nint GetReferenceStencil;
        public nint SetReferenceStencil;
        public nint SetBlendState;
        public nint SetDepthStencilState;
        public nint ApplyRasterizerState;
        public nint VerifySampler;
        public nint VerifyVertexSampler;
        public nint ApplyVertexBufferBindings;
        public nint SetRenderTargets;
        public nint ResolveTarget;
        public nint ResetBackbuffer;
        public nint ReadBackbuffer;
        public nint GetBackbufferSize;
        public nint GetBackbufferSurfaceFormat;
        public nint GetBackbufferDepthFormat;
        public nint GetBackbufferMultiSampleCount;
        public nint CreateTexture2D;
        public nint CreateTexture3D;
        public nint CreateTextureCube;
        public nint AddDisposeTexture;
        public nint SetTextureData2D;
        public nint SetTextureData3D;
        public nint SetTextureDataCube;
        public nint SetTextureDataYUV;
        public nint GetTextureData2D;
        public nint GetTextureData3D;
        public nint GetTextureDataCube;
        public nint GenColorRenderBuffer;
        public nint GenDepthStencilRenderbuffer;
        public nint AddDisposeRenderbuffer;
        public nint GenVertexBuffer;
        public nint AddDisposeVertexBuffer;
        public nint SetVertexBufferData;
        public nint GetVertexBufferData;
        public nint GenIndexBuffer;
        public nint AddDisposeIndexBuffer;
        public nint SetIndexBufferData;
        public nint GetIndexBufferData;
        public nint CreateEffect;
        public nint CloneEffect;
        public nint AddDisposeEffect;
        public nint SetEffectTechnique;
        public nint ApplyEffect;
        public nint BeginPassRestore;
        public nint EndPassRestore;
        public nint CreateQuery;
        public nint AddDisposeQuery;
        public nint QueryBegin;
        public nint QueryEnd;
        public nint QueryComplete;
        public nint QueryPixelCount;
        public nint SupportsDXT1;
        public nint SupportsS3TC;
        public nint SupportsBC7;
        public nint SupportsHardwareInstancing;
        public nint SupportsNoOverwrite;
        public nint SupportsSRGBRenderTargets;
        public nint GetMaxTextureSlots;
        public nint GetMaxMultiSampleCount;
        public nint SetStringMarker;
        public nint GetSysRenderer;
        public nint CreateSysTexture;
        public nint driverData;
    }

    [StructLayout(LayoutKind.Sequential, Size = 72)]
    private struct FNA3D_SysRendererEXT
    {
        public uint version;
        public FNA3D_SysRendererTypeEXT rendererType;
    }

    private enum FNA3D_SysRendererTypeEXT : int
    {
        FNA3D_RENDERER_TYPE_OPENGL_EXT,
        FNA3D_RENDERER_TYPE_VULKAN_EXT,
        FNA3D_RENDERER_TYPE_D3D11_EXT,
        FNA3D_RENDERER_TYPE_METAL_EXT /* REMOVED, DO NOT USE */
    }
#pragma warning restore CS0649
}