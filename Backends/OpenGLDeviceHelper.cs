using System;
using System.Runtime.InteropServices;
using Veldrid.OpenGL;
using Terraria;
using Veldrid;
using System.Runtime.CompilerServices;

namespace VeldridLib.Backends;

internal class OepnGLDeviceHelper
{
    public unsafe static Veldrid.OpenGL.OpenGLGraphicsDevice MakeDevice(ref FNAReflector.FNA3DDevice fnaDevice)
    {
        ref OpenGLRenderer oglr = ref Unsafe.As<byte, OpenGLRenderer>(ref Unsafe.AsRef<byte>((void*)fnaDevice.driverData));

        nint sdlHandle = Main.instance.Window.Handle;
        nint context = oglr.context;

        OpenGLPlatformInfo platformInfo = new OpenGLPlatformInfo(
            oglr.context,
            SDL2.SDL.SDL_GL_GetProcAddress,
            context => SDL2.SDL.SDL_GL_MakeCurrent(sdlHandle, context),
            SDL2.SDL.SDL_GL_GetCurrentContext,
            () => SDL2.SDL.SDL_GL_MakeCurrent(IntPtr.Zero, IntPtr.Zero),
            SDL2.SDL.SDL_GL_DeleteContext,
            () => SDL2.SDL.SDL_GL_SwapWindow(sdlHandle),
            sync => SDL2.SDL.SDL_GL_SetSwapInterval(sync ? 1 : 0)
        );

        Veldrid.OpenGL.OpenGLGraphicsDevice glDevice = new Veldrid.OpenGL.OpenGLGraphicsDevice(new GraphicsDeviceOptions(false) { PreferDepthRangeZeroToOne = false, SyncToVerticalBlank = true }, platformInfo, 1, 1);

        // TODO: Steal swapchain and backbuffer from FNA3D
        // TODO: FIX EVERYTHING??
        glDevice._mainSwapchain.Dispose();
        glDevice._swapchainFramebuffer.Dispose();
        glDevice._mainSwapchain = null;
        glDevice._swapchainFramebuffer = null;
        //SDL2.SDL.SDL_GL_MakeCurrent(sdlHandle, context);

        return glDevice;
    }

#pragma warning disable CS0649
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct OpenGLRenderer
    {
        public FNAReflector.FNA3DDevice* parentDevice;
        public nint* allocator;
        public nint context;
        public byte useES3;
        public byte useCoreProfile;
        public byte isEGL;

    }
#pragma warning restore CS0649
}