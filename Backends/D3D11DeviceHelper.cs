using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using Veldrid;
using Veldrid.D3D11;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace VeldridLib.Backends;

internal unsafe class D3D11DeviceHelper
{
    private const BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    private static readonly FieldInfo _immediateContextLockField = typeof(D3D11GraphicsDevice).GetField("_immediateContextLock", InstanceFlags)!;
    private static readonly FieldInfo _mappedResourceLockField = typeof(D3D11GraphicsDevice).GetField("_mappedResourceLock", InstanceFlags)!;
    private static readonly FieldInfo _mappedResourcesField = typeof(D3D11GraphicsDevice).GetField("_mappedResources", InstanceFlags)!;
    private static readonly FieldInfo _stagingResourcesLockField = typeof(D3D11GraphicsDevice).GetField("_stagingResourcesLock", InstanceFlags)!;
    private static readonly FieldInfo _availableStagingBuffersField = typeof(D3D11GraphicsDevice).GetField("_availableStagingBuffers", InstanceFlags)!;
    private static readonly FieldInfo _resetEventsLockField = typeof(D3D11GraphicsDevice).GetField("_resetEventsLock", InstanceFlags)!;
    private static readonly FieldInfo _resetEventField = typeof(D3D11GraphicsDevice).GetField("_resetEvents", InstanceFlags)!;
    private static readonly FieldInfo _deviceField = typeof(D3D11GraphicsDevice).GetField("_device", InstanceFlags)!;
    private static readonly FieldInfo _dxgiAdapterField = typeof(D3D11GraphicsDevice).GetField("_dxgiAdapter", InstanceFlags)!;
    private static readonly FieldInfo _deviceNameField = typeof(D3D11GraphicsDevice).GetField("_deviceName", InstanceFlags)!;
    private static readonly FieldInfo _vendorNameField = typeof(D3D11GraphicsDevice).GetField("_vendorName", InstanceFlags)!;
    private static readonly FieldInfo _deviceIdField = typeof(D3D11GraphicsDevice).GetField("_deviceId", InstanceFlags)!;
    private static readonly FieldInfo _apiVersionField = typeof(D3D11GraphicsDevice).GetField("_apiVersion", InstanceFlags)!;
    private static readonly FieldInfo _immediateContextField = typeof(D3D11GraphicsDevice).GetField("_immediateContext", InstanceFlags)!;
    private static readonly FieldInfo _featuresField = typeof(D3D11GraphicsDevice).GetField("<Features>k__BackingField", InstanceFlags)!;
    private static readonly FieldInfo _isDebugEnabledField = typeof(D3D11GraphicsDevice).GetField("<IsDebugEnabled>k__BackingField", InstanceFlags)!;
    private static readonly FieldInfo _supportsConcurrentResourcesField = typeof(D3D11GraphicsDevice).GetField("_supportsConcurrentResources", InstanceFlags)!;
    private static readonly FieldInfo _supportsCommandListsField = typeof(D3D11GraphicsDevice).GetField("_supportsCommandLists", InstanceFlags)!;
    private static readonly FieldInfo _d3d11ResourceFactoryField = typeof(D3D11GraphicsDevice).GetField("_d3d11ResourceFactory", InstanceFlags)!;
    private static readonly FieldInfo _d3d11Info = typeof(D3D11GraphicsDevice).GetField("_d3d11Info", InstanceFlags)!;

    public static D3D11GraphicsDevice MakeDevice(ref FNAReflector.FNA3DDevice fnaDevice)
    {
        ref FNAD3D11Renderer renderer = ref Unsafe.As<byte, FNAD3D11Renderer>(ref Unsafe.AsRef<byte>((void*)fnaDevice.driverData));

        D3D11GraphicsDevice d3d11gd = (D3D11GraphicsDevice)FormatterServices.GetUninitializedObject(typeof(D3D11GraphicsDevice));

        // imitating ctor behaviour
        // all these are readonly 
        _immediateContextLockField.SetValue(d3d11gd, new object());
        _mappedResourceLockField.SetValue(d3d11gd, new object());
        _mappedResourcesField.SetValue(d3d11gd, new Dictionary<MappedResourceCacheKey, MappedResourceInfo>());
        _stagingResourcesLockField.SetValue(d3d11gd, new object());
        _availableStagingBuffersField.SetValue(d3d11gd, new List<D3D11Buffer>());
        _resetEventsLockField.SetValue(d3d11gd, new object());
        _resetEventField.SetValue(d3d11gd, new List<ManualResetEvent[]>());

        ID3D11Device device = new(renderer.device);
        ID3D11DeviceContext context = new(renderer.context);
        IDXGIAdapter adapter = new(renderer.adapter);

        _deviceField.SetValue(d3d11gd, device);
        _dxgiAdapterField.SetValue(d3d11gd, adapter);

        AdapterDescription description = adapter.Description;
        _deviceNameField.SetValue(d3d11gd, description.Description);
        _vendorNameField.SetValue(d3d11gd, "id:" + description.VendorId.ToString("x8"));
        _deviceIdField.SetValue(d3d11gd, description.DeviceId);

        GraphicsApiVersion _apiVersion = device.FeatureLevel switch
        {
            FeatureLevel.Level_10_0 => new GraphicsApiVersion(10, 0, 0, 0),
            FeatureLevel.Level_10_1 => new GraphicsApiVersion(10, 1, 0, 0),
            FeatureLevel.Level_11_0 => new GraphicsApiVersion(11, 0, 0, 0),
            FeatureLevel.Level_11_1 => new GraphicsApiVersion(11, 1, 0, 0),
            FeatureLevel.Level_12_0 => new GraphicsApiVersion(12, 0, 0, 0),
            FeatureLevel.Level_12_1 => new GraphicsApiVersion(12, 1, 0, 0),
            FeatureLevel.Level_12_2 => new GraphicsApiVersion(12, 2, 0, 0),
            _ => default,
        };

        _apiVersionField.SetValue(d3d11gd, _apiVersion);
        _immediateContextField.SetValue(d3d11gd, context);

        device.CheckThreadingSupport(out bool _supportsConcurrentResources, out bool _supportsCommandLists);
        _supportsConcurrentResourcesField.SetValue(d3d11gd, _supportsConcurrentResources);
        _supportsCommandListsField.SetValue(d3d11gd, _supportsCommandLists);
        _isDebugEnabledField.SetValue(d3d11gd, (device.GetCreationFlags() & DeviceCreationFlags.Debug) != 0);

        GraphicsDeviceFeatures gdFeatures = new(computeShader: true, geometryShader: true, tessellationShaders: true, multipleViewports: true, samplerLodBias: true, drawBaseVertex: true, drawBaseInstance: true, drawIndirect: true, drawIndirectBaseInstance: true, fillModeWireframe: true, samplerAnisotropy: true, depthClipDisable: true, texture1D: true, independentBlend: true, structuredBuffer: true, subsetTextureView: true, commandListDebugMarkers: device.FeatureLevel >= FeatureLevel.Level_11_1, bufferRangeBinding: device.FeatureLevel >= FeatureLevel.Level_11_1, shaderFloat64: device.CheckFeatureSupport<FeatureDataDoubles>(Vortice.Direct3D11.Feature.Doubles).DoublePrecisionFloatShaderOps);
        _featuresField.SetValue(d3d11gd, gdFeatures);

        _d3d11ResourceFactoryField.SetValue(d3d11gd, new D3D11ResourceFactory(d3d11gd));
        _d3d11Info.SetValue(d3d11gd, new BackendInfoD3D11(d3d11gd));
        d3d11gd.PostDeviceCreated();

        return d3d11gd;
    }

    // https://github.com/FNA-XNA/FNA3D/blob/master/src/FNA3D_Driver_D3D11.c#L209
    [StructLayout(LayoutKind.Sequential)]
    unsafe struct FNAD3D11Renderer
    {
        public nint device; // ID3D11Device*
        public nint context; // ID3D11DeviceContext*
        public nint d3d11_dll;
        public nint dxgi_dll;
        public nint factory; // IDXGIFactory1 or IDXGIFactory2
        public nint adapter; // IDXGIAdapter*
        public nint annotation; // ID3D11UserDefinedAnnotation
        public bool supportsTearing;
        public nint ctxLock; // sdl Mutex*
        public nint iconv; // sdl Icon*

        // window surfaces
        public nint swapchainDatas; // D3D11SwapChainData**
        public int swapChainDataCount;
        public int swapChainDataCapacity;

        // faux backbuffer
        public nint backbuffer; // D3D11BackBuffer*
        public byte backBufferSizeChanged;
        public Rectangle prevSrcRect;
        public Rectangle prevDstRect;

        public struct FauxBackbufferResources
        {
            public nint vertexShader; // ID3D11VertexShader*
            public nint pixelShader; // ID3D11PixelShader*
            public nint samplerState; // ID3D11SamplerState*
            public nint vertexBuffer; // ID3D11Buffer*
            public nint indexBuffer; // ID3D11Buffer*
            public nint inputLayout; // ID3D11InputLayout*
            public nint rasterizerState; // ID3D11RasterizerState*
            public nint blendState; // ID3D11BlendState*
        }
        public FauxBackbufferResources fauxBackBufferResources;

        // capabilities
        public byte debugMode;
        public int supportsDxt1;
        public int supportsS3tc;
        public int supportsBc7;
        public byte supportsSRGBRenderTarget;
        public int maxMultiSampleCount;

        public byte syncInterval;

        public nint blendState; // ID3D11BlendState*
        public Color blendFactor;
    }
}
