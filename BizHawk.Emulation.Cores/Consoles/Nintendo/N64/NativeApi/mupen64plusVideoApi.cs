using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using static BizHawk.Emulation.Cores.Nintendo.N64.NativeApi.mupen64plusApi;

namespace BizHawk.Emulation.Cores.Nintendo.N64.NativeApi
{
	public class mupen64plusVideoApi
	{
		IntPtr GfxDll;// Graphics plugin specific

		internal enum m64p_GLattr
		{
			M64P_GL_DOUBLEBUFFER = 1,
			M64P_GL_BUFFER_SIZE,
			M64P_GL_DEPTH_SIZE,
			M64P_GL_RED_SIZE,
			M64P_GL_GREEN_SIZE,
			M64P_GL_BLUE_SIZE,
			M64P_GL_ALPHA_SIZE,
			M64P_GL_SWAP_CONTROL,
			M64P_GL_MULTISAMPLEBUFFERS,
			M64P_GL_MULTISAMPLESAMPLES,
			M64P_GL_CONTEXT_MAJOR_VERSION,
			M64P_GL_CONTEXT_MINOR_VERSION,
			M64P_GL_CONTEXT_PROFILE_MASK
		}

		internal enum m64p_video_mode
		{
			M64VIDEO_NONE = 1,
			M64VIDEO_WINDOWED,
			M64VIDEO_FULLSCREEN
		}

		internal enum m64p_video_flags
		{
			M64VIDEOFLAG_SUPPORT_RESIZING = 1
		}

		/// <summary>
		/// This function should be called from within the InitiateGFX() video plugin function call.The default SDL implementation of this function simply calls
		/// SDL_InitSubSystem(SDL_INIT_VIDEO). It does not open a rendering window or switch video modes.
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate m64p_error VidExtFuncInit();

		/// <summary>
		/// This function closes any open rendering window and shuts down the video system. The default SDL implementation of this function calls
		/// SDL_QuitSubSystem(SDL_INIT_VIDEO). This function should be called from within the RomClose() video plugin function.
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate m64p_error VidExtFuncQuit();

		/// <summary>
		/// This function is used to enumerate the available resolutions for fullscreen video modes. A pointer to an array is passed into the function, which is
		/// then filled with resolution sizes.
		/// </summary>
		/// <param name="m64p_2d_size">array of struct containing both width, height</param>
		/// <param name="length">length of array</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate m64p_error VidExtFuncListModes(IntPtr m64p_2d_size, IntPtr length);

		/// <summary>
		/// This function creates a rendering window or switches into a fullscreen video mode. Any desired OpenGL attributes should be set before calling this function.
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="bitsPerPixel"></param>
		/// <param name="mode"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate m64p_error VidExtFuncSetMode(int width, int height, int bitsPerPixel, m64p_video_mode mode, m64p_video_flags flags);

		/// <summary>
		/// This function is used to get a pointer to an OpenGL extension function. This is only necessary on the Windows platform, because the OpenGL implementation
		/// shipped with Windows only supports OpenGL version 1.1.
		/// </summary>
		/// <param name="addr"></param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate IntPtr VidExtFuncGLGetProc(string addr);

		/// <summary>
		/// This function is used to set certain OpenGL attributes which must be specified before creating the rendering window with VidExt_SetVideoMode.
		/// </summary>
		/// <param name="attr"></param>
		/// <param name="val"></param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate m64p_error VidExtFuncGLSetAttr(m64p_GLattr attr, int val);

		/// <summary>
		/// This function is used to get the value of OpenGL attributes.These values may be changed when calling VidExt_SetVideoMode.
		/// </summary>
		/// <param name="attr"></param>
		/// <param name="val"></param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate m64p_error VidExtFuncGLGetAttr(m64p_GLattr attr, IntPtr val);

		/// <summary>
		/// This function is used to swap the front/back buffers after rendering an output video frame.
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate m64p_error VidExtFuncGLSwapBuf();

		/// <summary>
		/// This function sets the caption text of the emulator rendering window.
		/// </summary>
		/// <param name="caption"></param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate m64p_error VidExtFuncSetCaption(string caption);

		/// <summary>
		/// This function toggles between fullscreen and windowed rendering modes.
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate m64p_error VidExtFuncToggleFS();

		/// <summary>
		/// This function resizes the opengl rendering window to match the given size.
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate m64p_error VidExtFuncResizeWindow(int width, int height);

		/// <summary>
		/// On some platforms(for instance, iOS) the default framebuffer object depends on the surface being rendered to, and might be different from 0.
		/// This function should be called after VidExt_SetVideoMode to retrieve the name of the default FBO. Calling this function may have performance implications
		/// and it should not be called every time the default FBO is bound.
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate uint VidExtFuncGLGetDefaultFramebuffer();

		[StructLayout(LayoutKind.Sequential)]
		public struct m64p_video_extension_functions
		{
			internal uint functions;
			internal VidExtFuncInit vidExtFuncInit;
			internal VidExtFuncQuit vidExtFuncQuit;
			internal VidExtFuncListModes vidExtFuncListModes;
			internal VidExtFuncSetMode vidExtFuncSetMode;
			internal VidExtFuncGLGetProc vidExtFuncGLGetProc;
			internal VidExtFuncGLSetAttr vidExtFuncGLSetAttr;
			internal VidExtFuncGLGetAttr vidExtFuncGLGetAttr;
			internal VidExtFuncGLSwapBuf vidExtFuncGLSwapBuf;
			internal VidExtFuncSetCaption vidExtFuncSetCaption;
			internal VidExtFuncToggleFS vidExtFuncToggleFS;
			internal VidExtFuncResizeWindow vidExtFuncResizeWindow;
			internal VidExtFuncGLGetDefaultFramebuffer vidExtFuncGLGetDefaultFramebuffer;
		}
		private m64p_video_extension_functions vidExtFunctions;
		private IntPtr vidExtFunctionsPtr;

		[DllImport("kernel32.dll")]
		public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

		public mupen64plusVideoApi(mupen64plusApi core, VideoPluginSettings settings)
		{
			this.core = core;
			string videoplugin;
			switch (settings.Plugin)
			{
				default:
				case PluginType.Rice:
					videoplugin = "mupen64plus-video-rice.dll";
					break;
				case PluginType.Glide:
					videoplugin = "mupen64plus-video-glide64.dll";
					break;
				case PluginType.GlideMk2:
					videoplugin = "mupen64plus-video-glide64mk2.dll";
					break;
				case PluginType.GLideN64:
					videoplugin = "mupen64plus-video-GLideN64.dll";
					break;
			}
			graphicsMode = new GraphicsMode();
			vidExtFunctions = new m64p_video_extension_functions();
			vidExtFunctions.functions = 12;
			vidExtFunctions.vidExtFuncInit = new VidExtFuncInit(Init);
			vidExtFunctions.vidExtFuncQuit = new VidExtFuncQuit(Quit);
			vidExtFunctions.vidExtFuncListModes = new VidExtFuncListModes(ListModes);
			vidExtFunctions.vidExtFuncSetMode = new VidExtFuncSetMode(SetMode);
			vidExtFunctions.vidExtFuncGLGetProc = new VidExtFuncGLGetProc(GLGetProc);
			vidExtFunctions.vidExtFuncGLSetAttr = new VidExtFuncGLSetAttr(GLSetAttr);
			vidExtFunctions.vidExtFuncGLGetAttr = new VidExtFuncGLGetAttr(GLGetAttr);
			vidExtFunctions.vidExtFuncGLSwapBuf = new VidExtFuncGLSwapBuf(GLSwapBuf);
			vidExtFunctions.vidExtFuncSetCaption = new VidExtFuncSetCaption(SetCaption);
			vidExtFunctions.vidExtFuncToggleFS = new VidExtFuncToggleFS(ToggleFS);
			vidExtFunctions.vidExtFuncResizeWindow = new VidExtFuncResizeWindow(ResizeWindow);
			vidExtFunctions.vidExtFuncGLGetDefaultFramebuffer = new VidExtFuncGLGetDefaultFramebuffer(GLGetDefaultFramebuffer);
			vidExtFunctionsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(vidExtFunctions));
			Marshal.StructureToPtr<m64p_video_extension_functions>(vidExtFunctions, vidExtFunctionsPtr, false);
			m64p_error result = core.m64pCoreOverrideVidExt(vidExtFunctionsPtr);
			GfxDll = core.AttachPlugin(mupen64plusApi.m64p_plugin_type.M64PLUGIN_GFX, videoplugin);
		}

		// Create a hidden shared openGL context for the plugins to render to. Use this to copy the final framebuffer over to mupen
		private GraphicsMode graphicsMode;
		private GraphicsContext graphicsContext;
		private NativeWindow nativeWindow;
		private mupen64plusApi core;

		private int majorVersion = 2;
		private int minorVersion = 1;
		private int doubleBuffer = 1;
		private int bufferSize = 0;
		private int depthSize = 16;
		private int width = 320;
		private int height = 240;

		private m64p_error Init()
		{
			return m64p_error.M64ERR_SUCCESS;
		}

		private m64p_error Quit()
		{

			return m64p_error.M64ERR_SUCCESS;
		}

		private m64p_error ListModes(IntPtr m64p_2d_size, IntPtr length)
		{
			return m64p_error.M64ERR_SUCCESS;
		}

		private Dictionary<m64p_video_mode, GameWindowFlags> modeMapping = new Dictionary<m64p_video_mode, GameWindowFlags>{
			{ m64p_video_mode.M64VIDEO_NONE, GameWindowFlags.Default },
			{ m64p_video_mode.M64VIDEO_WINDOWED, GameWindowFlags.FixedWindow },
			{ m64p_video_mode.M64VIDEO_FULLSCREEN, GameWindowFlags.Fullscreen },
		};

		private m64p_error SetMode(int width, int height, int bitsPerPixel, m64p_video_mode screenMode, m64p_video_flags flags)
		{
			this.width = width;
			this.height = height;
			graphicsMode = new GraphicsMode(new ColorFormat(bitsPerPixel), graphicsMode.Depth, graphicsMode.Stencil, graphicsMode.Samples, graphicsMode.AccumulatorFormat, graphicsMode.Buffers, graphicsMode.Stereo);
			nativeWindow = new NativeWindow(width, height, "", modeMapping[screenMode], graphicsMode, DisplayDevice.Default);
			graphicsContext = new GraphicsContext(graphicsMode, nativeWindow.WindowInfo);
			graphicsContext.MakeCurrent(nativeWindow.WindowInfo);
			return m64p_error.M64ERR_SUCCESS;
		}

		private m64p_error GLSetAttr(m64p_GLattr attr, int val)
		{
			switch (attr)
			{
				case m64p_GLattr.M64P_GL_DOUBLEBUFFER:
					doubleBuffer = val;
					break;
				case m64p_GLattr.M64P_GL_BUFFER_SIZE:
					bufferSize = val;
					break;
				case m64p_GLattr.M64P_GL_DEPTH_SIZE:
					depthSize = val;
					break;
				case m64p_GLattr.M64P_GL_RED_SIZE:
					break;
				case m64p_GLattr.M64P_GL_GREEN_SIZE:
					break;
				case m64p_GLattr.M64P_GL_BLUE_SIZE:
					break;
				case m64p_GLattr.M64P_GL_ALPHA_SIZE:
					break;
				case m64p_GLattr.M64P_GL_SWAP_CONTROL:
					break;
				case m64p_GLattr.M64P_GL_MULTISAMPLEBUFFERS:
					break;
				case m64p_GLattr.M64P_GL_MULTISAMPLESAMPLES:
					break;
				case m64p_GLattr.M64P_GL_CONTEXT_MAJOR_VERSION:
					majorVersion = val;
					break;
				case m64p_GLattr.M64P_GL_CONTEXT_MINOR_VERSION:
					minorVersion = val;
					break;
				case m64p_GLattr.M64P_GL_CONTEXT_PROFILE_MASK:
					break;
			}
			return m64p_error.M64ERR_SUCCESS;
		}

		private m64p_error GLGetAttr(m64p_GLattr attr, IntPtr val)
		{
			return m64p_error.M64ERR_SUCCESS;
		}
		private IntPtr GLGetProc(string func)
		{
			return IntPtr.Zero;
		}

		private int[] m64pBuffer = new int[0];
		private m64p_error GLSwapBuf()
		{
			if (m64pBuffer.Length != width * height)
			{
				m64pBuffer = new int[width * height];
			}
			GL.ReadPixels(0, 0, width, height, PixelFormat.Bgra, PixelType.UnsignedByte, m64pBuffer);
			// TODO(jroweboy): Don't include a reference to core. Expose BeforeRender from this class instead
			core.FireRenderEvent();
			graphicsContext.SwapBuffers();
			return m64p_error.M64ERR_SUCCESS;
		}

		private m64p_error SetCaption(string caption)
		{
			return m64p_error.M64ERR_SUCCESS;
		}

		private m64p_error ToggleFS()
		{
			return m64p_error.M64ERR_SUCCESS;
		}

		private m64p_error ResizeWindow(int width, int height)
		{
			this.width = width;
			this.height = height;
			return m64p_error.M64ERR_SUCCESS;
		}

		private uint GLGetDefaultFramebuffer()
		{
			return 0;
		}

		public void GetScreenDimensions(ref int width, ref int height)
		{
			width = this.width;
			height = this.height;
		}

		/// <summary>
		/// This function copies the frame buffer from mupen64plus
		/// </summary>
		public void Getm64pFrameBuffer(int[] buffer, ref int width, ref int height)
		{
			// TODO(jroweboy): Manipulate the pixel data on the GPU instead
			// vflip
			int fromindex = width * (height - 1) * 4;
			int toindex = 0;

			for (int j = 0; j < height; j++)
			{
				System.Buffer.BlockCopy(m64pBuffer, fromindex, buffer, toindex, width * 4);
				fromindex -= width * 4;
				toindex += width * 4;
			}

			// opaque
			unsafe
			{
				fixed (int* ptr = &buffer[0])
				{
					int l = buffer.Length;
					for (int i = 0; i < l; i++)
					{
						ptr[i] |= unchecked((int)0xff000000);
					}
				}
			}
		}
	}


	public class VideoPluginSettings
	{
		public PluginType Plugin;
		//public Dictionary<string, int> IntParameters = new Dictionary<string,int>();
		//public Dictionary<string, string> StringParameters = new Dictionary<string,string>();

		public Dictionary<string, object> Parameters = new Dictionary<string, object>();
		public int Height;
		public int Width;

		public VideoPluginSettings(PluginType Plugin, int Width, int Height)
		{
			this.Plugin = Plugin;
			this.Width = Width;
			this.Height = Height;
		}
	}
}
