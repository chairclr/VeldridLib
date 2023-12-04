
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Vulkan;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace VeldridLib.Backends;

internal class VulkanDeviceHelper
{
    private readonly static FieldInfo ResourceField = typeof(Veldrid.Vk.VkGraphicsDevice).GetField("<ResourceFactory>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private readonly static FieldInfo FeaturesField = typeof(Veldrid.Vk.VkGraphicsDevice).GetField("<Features>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private readonly static FieldInfo VulkanInfoField = typeof(Veldrid.Vk.VkGraphicsDevice).GetField("_vulkanInfo", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
    private readonly static FieldInfo FiltersField = typeof(Veldrid.Vk.VkGraphicsDevice).GetField("_filters", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;

    private readonly static FieldInfo _graphicsCommandPoolLockField = typeof(Veldrid.Vk.VkGraphicsDevice).GetField("_graphicsCommandPoolLock", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
    private readonly static FieldInfo _graphicsQueueLockField = typeof(Veldrid.Vk.VkGraphicsDevice).GetField("_graphicsQueueLock", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
    private readonly static FieldInfo _stagingResourcesLockField = typeof(Veldrid.Vk.VkGraphicsDevice).GetField("_stagingResourcesLock", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;

    private readonly static FieldInfo _availableStagingBuffersField = typeof(Veldrid.Vk.VkGraphicsDevice).GetField("_availableStagingBuffers", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
    private readonly static FieldInfo _availableStagingTexturesField = typeof(Veldrid.Vk.VkGraphicsDevice).GetField("_availableStagingTextures", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;

    private readonly static FieldInfo _submittedStagingTexturesField = typeof(Veldrid.Vk.VkGraphicsDevice).GetField("_submittedStagingTextures", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
    private readonly static FieldInfo _submittedStagingBuffersField = typeof(Veldrid.Vk.VkGraphicsDevice).GetField("_submittedStagingBuffers", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
    private readonly static FieldInfo _submittedSharedCommandPoolsField = typeof(Veldrid.Vk.VkGraphicsDevice).GetField("_submittedSharedCommandPools", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;

    public unsafe static Veldrid.Vk.VkGraphicsDevice MakeDevice(ref FNAReflector.FNA3DDevice fnaDevice)
    {
        ref VulkanRenderer vkr = ref Unsafe.As<byte, VulkanRenderer>(ref Unsafe.AsRef<byte>((void*)fnaDevice.driverData));

        Veldrid.Vk.VkGraphicsDevice vkDevice = (Veldrid.Vk.VkGraphicsDevice)RuntimeHelpers.GetUninitializedObject(typeof(Veldrid.Vk.VkGraphicsDevice));
        FiltersField.SetValue(vkDevice, new ConcurrentDictionary<VkFormat, VkFilter>());
        vkDevice._sharedGraphicsCommandPools = new Stack<Veldrid.Vk.VkGraphicsDevice.SharedCommandPool>();
        _graphicsCommandPoolLockField.SetValue(vkDevice, new object());
        _graphicsQueueLockField.SetValue(vkDevice, new object());
        _stagingResourcesLockField.SetValue(vkDevice, new object());

        _availableStagingTexturesField.SetValue(vkDevice, new List<Veldrid.Vk.VkTexture>());
        _availableStagingBuffersField.SetValue(vkDevice, new List<Veldrid.Vk.VkBuffer>());

        _submittedStagingTexturesField.SetValue(vkDevice, new Dictionary<VkCommandBuffer, Veldrid.Vk.VkTexture>());
        _submittedStagingBuffersField.SetValue(vkDevice, new Dictionary<VkCommandBuffer, Veldrid.Vk.VkBuffer>());
        _submittedSharedCommandPoolsField.SetValue(vkDevice, new Dictionary<VkCommandBuffer, Veldrid.Vk.VkGraphicsDevice.SharedCommandPool>());

        vkDevice._instance = vkr.instance;
        vkDevice._device = vkr.logicalDevice;
        vkDevice._physicalDevice = vkr.physicalDevice;

        fixed (byte* p = vkr.physicalDeviceProperties.properties.deviceName)
            vkDevice._deviceName = Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(p));
            
        vkDevice._vendorName = "id:" + vkr.physicalDeviceProperties.properties.vendorID.ToString("x8");
        VkConformanceVersion conforming = vkr.physicalDeviceDriverProperties.conformanceVersion;
        vkDevice._apiVersion = new Veldrid.GraphicsApiVersion(conforming.major, conforming.minor, conforming.subminor, conforming.patch);

        fixed (byte* p = vkr.physicalDeviceDriverProperties.driverName)
            vkDevice._deviceName = Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(p));
        fixed (byte* p = vkr.physicalDeviceDriverProperties.driverInfo)
            vkDevice._driverInfo = Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(p));

        vkDevice._physicalDeviceProperties = vkr.physicalDeviceProperties.properties;

        VulkanNative.vkGetPhysicalDeviceFeatures(vkDevice._physicalDevice, out vkDevice._physicalDeviceFeatures);
        VulkanNative.vkGetPhysicalDeviceMemoryProperties(vkDevice._physicalDevice, out vkDevice._physicalDeviceMemProperties);

        vkDevice._getBufferMemoryRequirements2 = vkDevice.GetDeviceProcAddr<Veldrid.Vk.vkGetBufferMemoryRequirements2_t>("vkGetBufferMemoryRequirements2")
            ?? vkDevice.GetDeviceProcAddr<Veldrid.Vk.vkGetBufferMemoryRequirements2_t>("vkGetBufferMemoryRequirements2KHR");
        vkDevice._getImageMemoryRequirements2 = vkDevice.GetDeviceProcAddr<Veldrid.Vk.vkGetImageMemoryRequirements2_t>("vkGetImageMemoryRequirements2")
            ?? vkDevice.GetDeviceProcAddr<Veldrid.Vk.vkGetImageMemoryRequirements2_t>("vkGetImageMemoryRequirements2KHR");

        vkDevice._setObjectNameDelegate = Marshal.GetDelegateForFunctionPointer<Veldrid.Vk.vkDebugMarkerSetObjectNameEXT_t>(
            vkDevice.GetInstanceProcAddr("vkDebugMarkerSetObjectNameEXT"));
        vkDevice._markerBegin = Marshal.GetDelegateForFunctionPointer<Veldrid.Vk.vkCmdDebugMarkerBeginEXT_t>(
            vkDevice.GetInstanceProcAddr("vkCmdDebugMarkerBeginEXT"));
        vkDevice._markerEnd = Marshal.GetDelegateForFunctionPointer<Veldrid.Vk.vkCmdDebugMarkerEndEXT_t>(
            vkDevice.GetInstanceProcAddr("vkCmdDebugMarkerEndEXT"));
        vkDevice._markerInsert = Marshal.GetDelegateForFunctionPointer<Veldrid.Vk.vkCmdDebugMarkerInsertEXT_t>(
            vkDevice.GetInstanceProcAddr("vkCmdDebugMarkerInsertEXT"));

        vkDevice._memoryManager = new Veldrid.Vk.VkDeviceMemoryManager(
            vkDevice._device,
            vkDevice._physicalDevice,
            vkDevice._physicalDeviceProperties.limits.bufferImageGranularity,
            vkDevice._getBufferMemoryRequirements2,
            vkDevice._getImageMemoryRequirements2);

        FeaturesField.SetValue(vkDevice, new Veldrid.GraphicsDeviceFeatures(
                computeShader: true,
                geometryShader: vkDevice._physicalDeviceFeatures.geometryShader,
                tessellationShaders: vkDevice._physicalDeviceFeatures.tessellationShader,
                multipleViewports: vkDevice._physicalDeviceFeatures.multiViewport,
                samplerLodBias: true,
                drawBaseVertex: true,
                drawBaseInstance: true,
                drawIndirect: true,
                drawIndirectBaseInstance: vkDevice._physicalDeviceFeatures.drawIndirectFirstInstance,
                fillModeWireframe: vkDevice._physicalDeviceFeatures.fillModeNonSolid,
                samplerAnisotropy: vkDevice._physicalDeviceFeatures.samplerAnisotropy,
                depthClipDisable: vkDevice._physicalDeviceFeatures.depthClamp,
                texture1D: true,
                independentBlend: vkDevice._physicalDeviceFeatures.independentBlend,
                structuredBuffer: true,
                subsetTextureView: true,
                commandListDebugMarkers: vkDevice._debugMarkerEnabled,
                bufferRangeBinding: true,
                shaderFloat64: vkDevice._physicalDeviceFeatures.shaderFloat64));

        ResourceField.SetValue(vkDevice, new Veldrid.Vk.VkResourceFactory(vkDevice));

        vkDevice.CreateDescriptorPool();
        vkDevice.CreateGraphicsCommandPool();
        for (int i = 0; i < Veldrid.Vk.VkGraphicsDevice.SharedCommandPoolCount; i++)
        {
            vkDevice._sharedGraphicsCommandPools.Push(new Veldrid.Vk.VkGraphicsDevice.SharedCommandPool(vkDevice, true));
        }

        VulkanInfoField.SetValue(vkDevice, new Veldrid.BackendInfoVulkan(vkDevice));

        vkDevice.PostDeviceCreated();

        // TODO: Steal swapchain and backbuffer from FNA3D
        
        return vkDevice;
    }

#pragma warning disable CS0649
    // https://registry.khronos.org/vulkan/specs/1.3-extensions/man/html/VkPhysicalDeviceProperties.html
    [StructLayout(LayoutKind.Sequential)]
    unsafe struct VkPhysicalDeviceDriverProperties
    {
        public VkStructureType sType;
        public void* pNext;
        public int driverID;
        public fixed byte driverName[256];
        public fixed byte driverInfo[256];
        public VkConformanceVersion conformanceVersion;
    }

    // https://registry.khronos.org/vulkan/specs/1.3-extensions/man/html/VkConformanceVersionKHR.html
    [StructLayout(LayoutKind.Sequential)]
    private struct VkConformanceVersion
    {
        public byte major;
        public byte minor;
        public byte subminor;
        public byte patch;
    }

    // https://registry.khronos.org/vulkan/specs/1.3-extensions/man/html/VkPhysicalDeviceProperties2.html
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct VkPhysicalDeviceProperties2
    {
        public VkStructureType sType;
        public void* pNext;
        public VkPhysicalDeviceProperties properties;
    };

    // https://github.com/FNA-XNA/FNA3D/blob/9d9c817e0b4fc3349e5e53a1e906a807f28a6933/src/FNA3D_Driver_Vulkan.c#L1088
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct VulkanRenderer
    {
        public FNAReflector.FNA3DDevice* parentDevice;
        public nint* allocator;
        public VkInstance instance;
        public VkPhysicalDevice physicalDevice;
        public VkPhysicalDeviceProperties2 physicalDeviceProperties;
        public VkPhysicalDeviceDriverProperties physicalDeviceDriverProperties;
        public VkDevice logicalDevice;
    }
#pragma warning restore CS0649
}
