#region License
/* NotSFML - SFML/Tao Reimplementation for Atom Zombie Smasher
 *
 * Copyright (c) 2016 Ethan Lee.
 *
 * This software is provided 'as-is', without any express or implied warranty.
 * In no event will the authors be held liable for any damages arising from
 * the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 * claim that you wrote the original software. If you use this software in a
 * product, an acknowledgment in the product documentation would be
 * appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not be
 * misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source distribution.
 *
 * Ethan "flibitijibibo" Lee <flibitijibibo@flibitijibibo.com>
 * -----------------------------------------------------------------------------
 * NotSFML reuses and reimplements code from SFML 1.6:
 *
 * SFML - Copyright (c) 2007-2009 Laurent Gomila
 *
 * This software is provided 'as-is', without any express or implied warranty.
 * In no event will the authors be held liable for any damages arising from
 * the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 * claim that you wrote the original software. If you use this software in a
 * product, an acknowledgment in the product documentation would be
 * appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not be
 * misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source distribution.
 *
 * Laurent Gomila <laurent.gom@gmail.com>
 * -----------------------------------------------------------------------------
 * NotSFML uses code from Mesa's GLU implementation:
 *
 * SGI FREE SOFTWARE LICENSE B (Version 2.0, Sept. 18, 2008)
 * Copyright (C) 1991-2000 Silicon Graphics, Inc. All Rights Reserved.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice including the dates of first publication and
 * either this permission notice or a reference to
 * http://oss.sgi.com/projects/FreeB/
 * shall be included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
 * SILICON GRAPHICS, INC. BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF
 * OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 *
 * Except as contained in this notice, the name of Silicon Graphics, Inc.
 * shall not be used in advertising or otherwise to promote the sale, use or
 * other dealings in this Software without prior written authorization from
 * Silicon Graphics, Inc.
 * -----------------------------------------------------------------------------
 */
#endregion

#region Using Statements
using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using SDL2;
using OpenAL;

using SFML.Window;
using SFML.Graphics;
using Tao.OpenGl;
#endregion

namespace SFML.Window
{
	#region Non-SFML Globals

	public static class SDLGlobals
	{
		private static readonly string dispVar = Environment.GetEnvironmentVariable(
			"AZS_DEFAULT_DISPLAY"
		);
		internal static readonly int Display = (
			string.IsNullOrEmpty(dispVar) ?
				0 :
				int.Parse(dispVar)
		);
		public static int DisplayWidth;
		public static int DisplayHeight;

		private static IntPtr alDevice;
		private static IntPtr alContext;

		public static void Init()
		{
			SDL.SDL_SetMainReady();

			// If available, load the SDL_GameControllerDB
			string mappingsDB = Path.Combine(
				AppDomain.CurrentDomain.BaseDirectory,
				"gamecontrollerdb.txt"
			);
			if (File.Exists(mappingsDB))
			{
				SDL.SDL_GameControllerAddMappingsFromFile(
					mappingsDB
				);
			}

			SDL.SDL_Init(
				SDL.SDL_INIT_VIDEO |
				SDL.SDL_INIT_GAMECONTROLLER
			);

			string envDevice = Environment.GetEnvironmentVariable("FNA_AUDIO_DEVICE_NAME");
			if (string.IsNullOrEmpty(envDevice))
			{
				envDevice = string.Empty;
			}
			alDevice = ALC10.alcOpenDevice(envDevice);
			if (ALC10.alcGetError(alDevice) != ALC10.ALC_NO_ERROR || alDevice == IntPtr.Zero)
			{
				throw new InvalidOperationException("Could not open audio device!");
			}

			int[] attribute = new int[0];
			alContext = ALC10.alcCreateContext(alDevice, attribute);
			if (ALC10.alcGetError(alDevice) != ALC10.ALC_NO_ERROR || alContext == IntPtr.Zero)
			{
				ALC10.alcCloseDevice(alDevice);
				throw new InvalidOperationException("Could not create OpenAL context!");
			}

			ALC10.alcMakeContextCurrent(alContext);
			if (ALC10.alcGetError(alDevice) != ALC10.ALC_NO_ERROR)
			{
				ALC10.alcDestroyContext(alContext);
				ALC10.alcCloseDevice(alDevice);
				throw new InvalidOperationException("Could not make OpenAL context current!");
			}

			float[] ori = new float[]
			{
				0.0f, 0.0f, -1.0f, 0.0f, 1.0f, 0.0f
			};
			AL10.alListenerfv(AL10.AL_ORIENTATION, ori);
			AL10.alListener3f(AL10.AL_POSITION, 0.0f, 0.0f, 0.0f);
			AL10.alListener3f(AL10.AL_VELOCITY, 0.0f, 0.0f, 0.0f);
			AL10.alListenerf(AL10.AL_GAIN, 1.0f);

			SDL.SDL_StartTextInput();

			SDL.SDL_DisplayMode mode;
			SDL.SDL_GetCurrentDisplayMode(
				Display,
				out mode
			);
			DisplayWidth = mode.w;
			DisplayHeight = mode.h;
		}

		public static void Quit()
		{
			ALC10.alcMakeContextCurrent(IntPtr.Zero);
			if (alContext != IntPtr.Zero)
			{
				ALC10.alcDestroyContext(alContext);
				alContext = IntPtr.Zero;
			}
			if (alDevice != IntPtr.Zero)
			{
				ALC10.alcCloseDevice(alDevice);
				alDevice = IntPtr.Zero;
			}
			SDL.SDL_StopTextInput();
			SDL.SDL_Quit();
		}

		public static Action MusicUpdate;
	}

	#endregion

	#region stb Library Interop

	internal static class stb
	{
		[DllImport("atomstb", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr stbi_load(
			[MarshalAs(UnmanagedType.LPStr)]
				string filename,
			out int x,
			out int y,
			out int comp,
			int req_comp
		);


		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct stbtt_bakedchar
		{
			public ushort x0, y0, x1, y1;
			public float xoff, yoff, xadvance;
		}

		[DllImport("atomstb", CallingConvention = CallingConvention.Cdecl)]
		public static extern int stbtt_BakeFontBitmap(
			byte[] buf,
			int offset,
			float pixel_height,
			byte[] pixels,
			int pw,
			int ph,
			int first_char,
			int num_chars,
			IntPtr chardata
		);


		[DllImport("atomstb", CallingConvention = CallingConvention.Cdecl)]
		public static extern int stb_vorbis_decode_filename(
			[MarshalAs(UnmanagedType.LPStr)]
				string filename,
			out int channels,
			out int sample_rate,
			out IntPtr output
		);

		[DllImport("atomstb", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr stb_vorbis_open_filename(
			[MarshalAs(UnmanagedType.LPStr)]
				string filename,
			out int error,
			IntPtr alloc_buffer
		);

		[DllImport("atomstb", CallingConvention = CallingConvention.Cdecl)]
		public static extern void stb_vorbis_close(IntPtr p);

		[DllImport("atomstb", CallingConvention = CallingConvention.Cdecl)]
		public static extern void stb_vorbis_seek_start(IntPtr p);

		[DllImport("atomstb", CallingConvention = CallingConvention.Cdecl)]
		public static extern int stb_vorbis_get_samples_float_interleaved(
			IntPtr f,
			int channels,
			float[] buffer,
			int num_floats
		);


		[DllImport("msvcr100.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void free(IntPtr mem);
	}

	#endregion

	#region Window Reimplementation, lots of copypasta from SFML and SDL2_FNAPlatform

	public class VideoMode
	{
		public uint Width;
		public uint Height;

		public static VideoMode DesktopMode = GetDesktopMode();
		private static VideoMode GetDesktopMode()
		{
			SDL.SDL_DisplayMode mode = new SDL.SDL_DisplayMode();
			SDL.SDL_GetCurrentDisplayMode(
				SDLGlobals.Display,
				out mode
			);
			return new VideoMode((uint) mode.w, (uint) mode.h, 32);
		}
		public static readonly uint ModesCount = (uint) SDL.SDL_GetNumDisplayModes(SDLGlobals.Display);

		public VideoMode(uint w, uint h, uint bpp)
		{
			Width = w;
			Height = h;
		}

		public bool IsValid()
		{
			return true;
		}

		public static VideoMode GetMode(uint index)
		{
			SDL.SDL_DisplayMode mode = new SDL.SDL_DisplayMode();
			SDL.SDL_GetDisplayMode(
				SDLGlobals.Display,
				(int) index,
				out mode
			);
			return new VideoMode((uint) mode.w, (uint) mode.h, 32);
		}
	}

	[Flags]
	public enum Styles
	{
		Fullscreen = 1,
		Titlebar = 2,
		Close = 4
	}

	public class Window
	{
		public uint Width
		{
			get;
			private set;
		}
		public uint Height
		{
			get;
			private set;
		}

		public readonly Input Input;

		public event EventHandler<TextEventArgs> TextEntered;
		public event EventHandler<MouseWheelEventArgs> MouseWheelMoved;
		public event EventHandler<JoyMoveEventArgs> JoyMoved;
		public event EventHandler<KeyEventArgs> KeyPressed;
		public event EventHandler Closed;
		public event EventHandler LostFocus;
		public event EventHandler GainedFocus;

		private IntPtr window;
		private IntPtr context;
		private bool run;
		private uint lastTicks;
		private uint lastDisplay;
		private uint fpsLimit;

		public Window(VideoMode mode, string title, Styles style)
		{
			int loc = SDL.SDL_WINDOWPOS_CENTERED_DISPLAY(SDLGlobals.Display);
			SDL.SDL_WindowFlags flags = SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL;
			if ((style & Styles.Fullscreen) == Styles.Fullscreen)
			{
				flags |= SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP;
			}
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_RED_SIZE, 8);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_GREEN_SIZE, 8);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_BLUE_SIZE, 8);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_ALPHA_SIZE, 8);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE, 24);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_STENCIL_SIZE, 8);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DOUBLEBUFFER, 1);
			window = SDL.SDL_CreateWindow(
				title,
				loc,
				loc,
				(int) mode.Width,
				(int) mode.Height,
				flags
			);
			context = SDL.SDL_GL_CreateContext(window);
			if (!SDL.SDL_GetPlatform().Equals("Mac OS X"))
			{
				int w, h, bpp;
				IntPtr img = stb.stbi_load(
					Path.Combine("content", "textures", "icon.png"),
					out w,
					out h,
					out bpp,
					4
				);
				IntPtr icon = SDL.SDL_CreateRGBSurfaceFrom(
					img,
					w,
					h,
					32,
					w * 4,
					0x000000FF,
					0x0000FF00,
					0x00FF0000,
					0xFF000000
				);
				SDL.SDL_SetWindowIcon(
					window,
					icon
				);
				SDL.SDL_FreeSurface(icon);
				stb.free(img);
			}
			run = true;
			lastTicks = 0;
			lastDisplay = 0;
			fpsLimit = 0;

			Width = mode.Width;
			Height = mode.Height;
			Input = new Input();

			Tao.OpenGl.Gl.Init();

			Gl.glViewport(
				0,
				0,
				(int) Width,
				(int) Height
			);
			Gl.glEnable(Gl.GL_TEXTURE_2D);
		}

		public void Resize(int width, int height, Styles style)
		{
			uint flags = SDL.SDL_GetWindowFlags(window);
			if ((flags & (uint) SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN) != 0)
			{
				if ((style & Styles.Fullscreen) == 0)
				{
					SDL.SDL_SetWindowFullscreen(window, 0);
					SDL.SDL_SetWindowSize(window, width, height);
					int pos = SDL.SDL_WINDOWPOS_CENTERED_DISPLAY(SDLGlobals.Display);
					SDL.SDL_SetWindowPosition(
						window,
						pos,
						pos
					);
				}
				else if ((flags & (uint) SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP) == (uint) SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP)
				{
					if (width != SDLGlobals.DisplayWidth || height != SDLGlobals.DisplayHeight)
					{
						SDL.SDL_SetWindowFullscreen(window, 0);
						SDL.SDL_SetWindowSize(window, width, height);
						SDL.SDL_SetWindowFullscreen(window, (uint) SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN);
					}
				}
				else
				{
					if (width == SDLGlobals.DisplayWidth && height == SDLGlobals.DisplayHeight)
					{
						SDL.SDL_SetWindowFullscreen(window, 0);
						SDL.SDL_SetWindowFullscreen(window, (uint) SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP);
					}
				}
			}
			else
			{
				SDL.SDL_SetWindowSize(window, width, height);
				if ((style & Styles.Fullscreen) != 0)
				{
					SDL.SDL_SetWindowFullscreen(
						window,
						(uint) ((width == SDLGlobals.DisplayWidth && height == SDLGlobals.DisplayHeight) ?
							SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP :
							SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN
						)
					);
				}
			}
			Width = (uint) width;
			Height = (uint) height;
			Gl.glViewport(
				0,
				0,
				width,
				height
			);
		}

		public void Close()
		{
			run = false;
			SDL.SDL_GL_DeleteContext(context);
			SDL.SDL_DestroyWindow(window);
		}

		public bool IsOpened()
		{
			return run;
		}

		public void Clear(Color clear)
		{
			Gl.glClearColor(
				clear.R / 255.0f,
				clear.G / 255.0f,
				clear.B / 255.0f,
				clear.A / 255.0f
			);
			Gl.glClear(Gl.GL_COLOR_BUFFER_BIT);
		}

		public void Display()
		{
			uint ticks = SDL.SDL_GetTicks();
			int diff = (int) fpsLimit - (int) (ticks - lastDisplay);
			if (fpsLimit > 0 && diff > 0)
			{
				SDL.SDL_Delay((uint) diff);
			}
			lastDisplay = ticks;
			SDL.SDL_GL_SwapWindow(window);
		}

		public void UseVerticalSync(bool vsync)
		{
			if (vsync && SDL.SDL_GL_SetSwapInterval(-1) != -1)
			{
				Console.WriteLine("Using EXT_swap_control_tear VSync!");
			}
			else
			{
				SDL.SDL_GL_SetSwapInterval(
					vsync ? 1 : 0
				);
			}
		}

		public void SetPosition(int x, int y)
		{
			SDL.SDL_SetWindowPosition(
				window,
				x,
				y
			);
		}

		public void ShowMouseCursor(bool show)
		{
			SDL.SDL_ShowCursor(show ? 1 : 0);
		}

		public float GetFrameTime()
		{
			uint next = SDL.SDL_GetTicks();
			float result = (next - lastTicks) / 1000.0f;
			lastTicks = next;
			return result;
		}

		public void SetFramerateLimit(uint frames)
		{
			fpsLimit = 1000 / frames;
		}

		public void Draw(Drawable item)
		{
			item.Draw(this);
		}

		public void DispatchEvents()
		{
			SDL.SDL_Event evt = new SDL.SDL_Event();
			while (SDL.SDL_PollEvent(out evt) == 1)
			{
				if (evt.type == SDL.SDL_EventType.SDL_QUIT)
				{
					run = false;
					Closed(this, EventArgs.Empty);
				}
				else if (evt.type == SDL.SDL_EventType.SDL_KEYDOWN)
				{
					KeyCode key;
					if (SDLToSFML.TryGetValue((int) evt.key.keysym.sym, out key))
					{
						if (!Input.keys.Contains(key))
						{
							Input.keys.Add(key);
							if (KeyPressed != null)
							{
								KeyPressed(this, new KeyEventArgs(key));
							}
						}
					}
				}
				else if (evt.type == SDL.SDL_EventType.SDL_KEYUP)
				{
					KeyCode key;
					if (SDLToSFML.TryGetValue((int) evt.key.keysym.sym, out key))
					{
						Input.keys.Remove(key);
					}
				}
				else if (evt.type == SDL.SDL_EventType.SDL_MOUSEMOTION)
				{
					Input.mouseX = evt.motion.x;
					Input.mouseY = evt.motion.y;
				}
				else if (evt.type == SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN)
				{
					Input.mouseDown[evt.button.button - 1] = true;
				}
				else if (evt.type == SDL.SDL_EventType.SDL_MOUSEBUTTONUP)
				{
					Input.mouseDown[evt.button.button - 1] = false;
				}
				else if (evt.type == SDL.SDL_EventType.SDL_MOUSEWHEEL)
				{
					if (MouseWheelMoved != null)
					{
						MouseWheelMoved(this, new MouseWheelEventArgs(evt.wheel.y));
					}
				}
				else if (evt.type == SDL.SDL_EventType.SDL_WINDOWEVENT)
				{
					if (evt.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED)
					{
						SDL.SDL_DisableScreenSaver();
						if (GainedFocus != null)
						{
							GainedFocus(this, EventArgs.Empty);
						}
					}
					else if (evt.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST)
					{
						SDL.SDL_EnableScreenSaver();
						if (LostFocus != null)
						{
							LostFocus(this, EventArgs.Empty);
						}
					}
					else if (evt.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_ENTER)
					{
						SDL.SDL_DisableScreenSaver();
					}
					else if (evt.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE)
					{
						SDL.SDL_EnableScreenSaver();
					}
				}
				else if (evt.type == SDL.SDL_EventType.SDL_TEXTINPUT)
				{
					if (TextEntered != null)
					{
						string text;

						// Based on the SDL2# LPUtf8StrMarshaler
						unsafe
						{
							byte* endPtr = evt.text.text;
							while (*endPtr != 0)
							{
								endPtr++;
							}
							byte[] bytes = new byte[endPtr - evt.text.text];
							Marshal.Copy((IntPtr) evt.text.text, bytes, 0, bytes.Length);
							text = System.Text.Encoding.UTF8.GetString(bytes);
						}

						if (text.Length > 0)
						{
							TextEntered(this, new TextEventArgs(text));
						}
					}
				}
				else if (evt.type == SDL.SDL_EventType.SDL_CONTROLLERDEVICEADDED)
				{
					Input.AddController(evt.cdevice.which);
				}
				else if (evt.type == SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMOVED)
				{
					Input.RemoveController(evt.cdevice.which);
				}
				else if (evt.type == SDL.SDL_EventType.SDL_CONTROLLERBUTTONDOWN)
				{
					Input.ChangeButton(evt.cbutton.which, evt.cbutton.button, true);
				}
				else if (evt.type == SDL.SDL_EventType.SDL_CONTROLLERBUTTONUP)
				{
					Input.ChangeButton(evt.cbutton.which, evt.cbutton.button, false);
				}
				else if (evt.type == SDL.SDL_EventType.SDL_CONTROLLERAXISMOTION)
				{
					int device;
					if (JoyMoved != null && Input.ControllerIndex(evt.caxis.which, out device))
					{
						JoyMoved(
							this,
							new JoyMoveEventArgs(
								(uint) device,
								(JoyAxis) evt.caxis.axis,
								evt.caxis.axisValue / 327.67f
							)
						);
					}
				}
			}
			if (SDLGlobals.MusicUpdate != null)
			{
				SDLGlobals.MusicUpdate();
			}
		}

		private static readonly Dictionary<int, KeyCode> SDLToSFML = new Dictionary<int, KeyCode>()
		{
			{ (int) SDL.SDL_Keycode.SDLK_a, KeyCode.A },
			{ (int) SDL.SDL_Keycode.SDLK_b, KeyCode.B },
			{ (int) SDL.SDL_Keycode.SDLK_c, KeyCode.C },
			{ (int) SDL.SDL_Keycode.SDLK_d, KeyCode.D },
			{ (int) SDL.SDL_Keycode.SDLK_e, KeyCode.E },
			{ (int) SDL.SDL_Keycode.SDLK_f, KeyCode.F },
			{ (int) SDL.SDL_Keycode.SDLK_g, KeyCode.G },
			{ (int) SDL.SDL_Keycode.SDLK_h, KeyCode.H },
			{ (int) SDL.SDL_Keycode.SDLK_i, KeyCode.I },
			{ (int) SDL.SDL_Keycode.SDLK_j, KeyCode.J },
			{ (int) SDL.SDL_Keycode.SDLK_k, KeyCode.K },
			{ (int) SDL.SDL_Keycode.SDLK_l, KeyCode.L },
			{ (int) SDL.SDL_Keycode.SDLK_m, KeyCode.M },
			{ (int) SDL.SDL_Keycode.SDLK_n, KeyCode.N },
			{ (int) SDL.SDL_Keycode.SDLK_o, KeyCode.O },
			{ (int) SDL.SDL_Keycode.SDLK_p, KeyCode.P },
			{ (int) SDL.SDL_Keycode.SDLK_q, KeyCode.Q },
			{ (int) SDL.SDL_Keycode.SDLK_r, KeyCode.R },
			{ (int) SDL.SDL_Keycode.SDLK_s, KeyCode.S },
			{ (int) SDL.SDL_Keycode.SDLK_t, KeyCode.T },
			{ (int) SDL.SDL_Keycode.SDLK_u, KeyCode.U },
			{ (int) SDL.SDL_Keycode.SDLK_v, KeyCode.V },
			{ (int) SDL.SDL_Keycode.SDLK_w, KeyCode.W },
			{ (int) SDL.SDL_Keycode.SDLK_x, KeyCode.X },
			{ (int) SDL.SDL_Keycode.SDLK_y, KeyCode.Y },
			{ (int) SDL.SDL_Keycode.SDLK_z, KeyCode.Z },
			{ (int) SDL.SDL_Keycode.SDLK_0, KeyCode.Num0 },
			{ (int) SDL.SDL_Keycode.SDLK_1, KeyCode.Num1 },
			{ (int) SDL.SDL_Keycode.SDLK_2, KeyCode.Num2 },
			{ (int) SDL.SDL_Keycode.SDLK_3, KeyCode.Num3 },
			{ (int) SDL.SDL_Keycode.SDLK_4, KeyCode.Num4 },
			{ (int) SDL.SDL_Keycode.SDLK_5, KeyCode.Num5 },
			{ (int) SDL.SDL_Keycode.SDLK_6, KeyCode.Num6 },
			{ (int) SDL.SDL_Keycode.SDLK_7, KeyCode.Num7 },
			{ (int) SDL.SDL_Keycode.SDLK_8, KeyCode.Num8 },
			{ (int) SDL.SDL_Keycode.SDLK_9, KeyCode.Num9 },
			{ (int) SDL.SDL_Keycode.SDLK_ESCAPE, KeyCode.Escape  },
			{ (int) SDL.SDL_Keycode.SDLK_LCTRL, KeyCode.LControl },
			{ (int) SDL.SDL_Keycode.SDLK_LSHIFT, KeyCode.LShift },
			{ (int) SDL.SDL_Keycode.SDLK_LALT, KeyCode.LAlt },
			{ (int) SDL.SDL_Keycode.SDLK_LGUI, KeyCode.LSystem },
			{ (int) SDL.SDL_Keycode.SDLK_RCTRL, KeyCode.RControl },
			{ (int) SDL.SDL_Keycode.SDLK_RSHIFT, KeyCode.RShift },
			{ (int) SDL.SDL_Keycode.SDLK_RALT, KeyCode.RAlt },
			{ (int) SDL.SDL_Keycode.SDLK_RGUI, KeyCode.RSystem },
			{ (int) SDL.SDL_Keycode.SDLK_MENU, KeyCode.Menu },
			{ (int) SDL.SDL_Keycode.SDLK_LEFTBRACKET, KeyCode.LBracket },
			{ (int) SDL.SDL_Keycode.SDLK_RIGHTBRACKET, KeyCode.RBracket },
			{ (int) SDL.SDL_Keycode.SDLK_SEMICOLON, KeyCode.SemiColon },
			{ (int) SDL.SDL_Keycode.SDLK_COMMA, KeyCode.Comma },
			{ (int) SDL.SDL_Keycode.SDLK_PERIOD, KeyCode.Period },
			{ (int) SDL.SDL_Keycode.SDLK_QUOTE, KeyCode.Quote },
			{ (int) SDL.SDL_Keycode.SDLK_SLASH, KeyCode.Slash },
			{ (int) SDL.SDL_Keycode.SDLK_BACKSLASH, KeyCode.BackSlash },
			{ (int) SDL.SDL_Keycode.SDLK_BACKQUOTE, KeyCode.Tilde },
			{ (int) SDL.SDL_Keycode.SDLK_EQUALS, KeyCode.Equal },
			{ (int) SDL.SDL_Keycode.SDLK_MINUS, KeyCode.Dash },
			{ (int) SDL.SDL_Keycode.SDLK_SPACE, KeyCode.Space },
			{ (int) SDL.SDL_Keycode.SDLK_RETURN, KeyCode.Return },
			{ (int) SDL.SDL_Keycode.SDLK_BACKSPACE, KeyCode.Back },
			{ (int) SDL.SDL_Keycode.SDLK_TAB, KeyCode.Tab },
			{ (int) SDL.SDL_Keycode.SDLK_PAGEUP, KeyCode.PageUp },
			{ (int) SDL.SDL_Keycode.SDLK_PAGEDOWN, KeyCode.PageDown },
			{ (int) SDL.SDL_Keycode.SDLK_END, KeyCode.End },
			{ (int) SDL.SDL_Keycode.SDLK_HOME, KeyCode.Home },
			{ (int) SDL.SDL_Keycode.SDLK_INSERT, KeyCode.Insert },
			{ (int) SDL.SDL_Keycode.SDLK_DELETE, KeyCode.Delete },
			{ (int) SDL.SDL_Keycode.SDLK_KP_PLUS, KeyCode.Add },
			{ (int) SDL.SDL_Keycode.SDLK_KP_MINUS, KeyCode.Subtract },
			{ (int) SDL.SDL_Keycode.SDLK_KP_MULTIPLY, KeyCode.Multiply },
			{ (int) SDL.SDL_Keycode.SDLK_KP_DIVIDE, KeyCode.Divide },
			{ (int) SDL.SDL_Keycode.SDLK_LEFT, KeyCode.Left },
			{ (int) SDL.SDL_Keycode.SDLK_RIGHT, KeyCode.Right },
			{ (int) SDL.SDL_Keycode.SDLK_UP, KeyCode.Up },
			{ (int) SDL.SDL_Keycode.SDLK_DOWN, KeyCode.Down },
			{ (int) SDL.SDL_Keycode.SDLK_KP_0, KeyCode.Numpad0 },
			{ (int) SDL.SDL_Keycode.SDLK_KP_1, KeyCode.Numpad1 },
			{ (int) SDL.SDL_Keycode.SDLK_KP_2, KeyCode.Numpad2 },
			{ (int) SDL.SDL_Keycode.SDLK_KP_3, KeyCode.Numpad3 },
			{ (int) SDL.SDL_Keycode.SDLK_KP_4, KeyCode.Numpad4 },
			{ (int) SDL.SDL_Keycode.SDLK_KP_5, KeyCode.Numpad5 },
			{ (int) SDL.SDL_Keycode.SDLK_KP_6, KeyCode.Numpad6 },
			{ (int) SDL.SDL_Keycode.SDLK_KP_7, KeyCode.Numpad7 },
			{ (int) SDL.SDL_Keycode.SDLK_KP_8, KeyCode.Numpad8 },
			{ (int) SDL.SDL_Keycode.SDLK_KP_9, KeyCode.Numpad9 },
			{ (int) SDL.SDL_Keycode.SDLK_F1, KeyCode.F1 },
			{ (int) SDL.SDL_Keycode.SDLK_F2, KeyCode.F2 },
			{ (int) SDL.SDL_Keycode.SDLK_F3, KeyCode.F3 },
			{ (int) SDL.SDL_Keycode.SDLK_F4, KeyCode.F4 },
			{ (int) SDL.SDL_Keycode.SDLK_F5, KeyCode.F5 },
			{ (int) SDL.SDL_Keycode.SDLK_F6, KeyCode.F6 },
			{ (int) SDL.SDL_Keycode.SDLK_F7, KeyCode.F7 },
			{ (int) SDL.SDL_Keycode.SDLK_F8, KeyCode.F8 },
			{ (int) SDL.SDL_Keycode.SDLK_F9, KeyCode.F9 },
			{ (int) SDL.SDL_Keycode.SDLK_F10, KeyCode.F10 },
			{ (int) SDL.SDL_Keycode.SDLK_F11, KeyCode.F11 },
			{ (int) SDL.SDL_Keycode.SDLK_F12, KeyCode.F12 },
			{ (int) SDL.SDL_Keycode.SDLK_F13, KeyCode.F13 },
			{ (int) SDL.SDL_Keycode.SDLK_F14, KeyCode.F14 },
			{ (int) SDL.SDL_Keycode.SDLK_F15, KeyCode.F15 },
			{ (int) SDL.SDL_Keycode.SDLK_PAUSE, KeyCode.Pause }
		};
	}

	public class Input
	{
		internal List<KeyCode> keys;
		internal bool[] mouseDown;
		internal int mouseX;
		internal int mouseY;

		private IntPtr[] jDevices;
		private Dictionary<int, int> jInstance;
		private bool[] jButtons;

		public Input()
		{
			keys = new List<KeyCode>();
			mouseDown = new bool[8];
			mouseX = 0;
			mouseY = 0;

			jDevices = new IntPtr[2];
			jInstance = new Dictionary<int, int>();
			jButtons = new bool[30];
		}

		public bool IsKeyDown(KeyCode key)
		{
			return keys.Contains(key);
		}

		public bool IsMouseButtonDown(MouseButton button)
		{
			return mouseDown[(int) button];
		}

		public int GetMouseX()
		{
			return mouseX;
		}

		public int GetMouseY()
		{
			return mouseY;
		}

		public bool IsJoystickButtonDown(uint index, uint button)
		{
			return jButtons[index * 15 + button];
		}

		internal void AddController(int dev)
		{
			int which = -1;
			for (int i = 0; i < jDevices.Length; i += 1)
			{
				if (jDevices[i] == IntPtr.Zero)
				{
					which = i;
					break;
				}
			}
			if (which == -1)
			{
				return;
			}

			jDevices[which] = SDL.SDL_GameControllerOpen(dev);

			int thisInstance = SDL.SDL_JoystickInstanceID(
				SDL.SDL_GameControllerGetJoystick(jDevices[which])
			);
			if (jInstance.ContainsKey(thisInstance))
			{
				// Duplicate? Usually this is OSX being dumb, but...?
				jDevices[which] = IntPtr.Zero;
				return;
			}
			jInstance.Add(thisInstance, which);

			Array.Clear(jButtons, which * 15, 15);

			Console.WriteLine(
				"Controller " + which.ToString() + ": " +
				SDL.SDL_GameControllerName(jDevices[which])
			);
		}

		internal void RemoveController(int dev)
		{
			int output;
			if (!jInstance.TryGetValue(dev, out output))
			{
				return;
			}
			jInstance.Remove(dev);
			SDL.SDL_GameControllerClose(jDevices[output]);
			jDevices[output] = IntPtr.Zero;
			Array.Clear(jButtons, output * 15, 15);
			Console.WriteLine("Removed device, player: " + output.ToString());
		}

		internal void ChangeButton(int which, int button, bool down)
		{
			int output;
			if (!jInstance.TryGetValue(which, out output))
			{
				return;
			}
			jButtons[output * 15 + button] = down;
		}

		internal bool ControllerIndex(int which, out int output)
		{
			return jInstance.TryGetValue(which, out output);
		}
	}

	public enum JoyAxis
	{
		AxisX,
		AxisY,
		AxisZ,
		AxisR,
		AxisU,
		AxisV,
		AxisPOV
	}

	public enum MouseButton
	{
	}

	public enum KeyCode
	{
		A = 'a',
		B = 'b',
		C = 'c',
		D = 'd',
		E = 'e',
		F = 'f',
		G = 'g',
		H = 'h',
		I = 'i',
		J = 'j',
		K = 'k',
		L = 'l',
		M = 'm',
		N = 'n',
		O = 'o',
		P = 'p',
		Q = 'q',
		R = 'r',
		S = 's',
		T = 't',
		U = 'u',
		V = 'v',
		W = 'w',
		X = 'x',
		Y = 'y',
		Z = 'z',
		Num0 = '0',
		Num1 = '1',
		Num2 = '2',
		Num3 = '3',
		Num4 = '4',
		Num5 = '5',
		Num6 = '6',
		Num7 = '7',
		Num8 = '8',
		Num9 = '9',
		Escape = 256,
		LControl,
		LShift,
		LAlt,
		LSystem,
		RControl,
		RShift,
		RAlt,
		RSystem,
		Menu,
		LBracket,
		RBracket,
		SemiColon,
		Comma,
		Period,
		Quote,
		Slash,
		BackSlash,
		Tilde,
		Equal,
		Dash,
		Space,
		Return,
		Back,
		Tab,
		PageUp,
		PageDown,
		End,
		Home,
		Insert,
		Delete,
		Add,
		Subtract,
		Multiply,
		Divide,
		Left,
		Right,
		Up,
		Down,
		Numpad0,
		Numpad1,
		Numpad2,
		Numpad3,
		Numpad4,
		Numpad5,
		Numpad6,
		Numpad7,
		Numpad8,
		Numpad9,
		F1,
		F2,
		F3,
		F4,
		F5,
		F6,
		F7,
		F8,
		F9,
		F10,
		F11,
		F12,
		F13,
		F14,
		F15,
		Pause,
		Count
	}

	public class TextEventArgs : EventArgs
	{
		public string Unicode;
		public TextEventArgs(string unicode)
		{
			Unicode = unicode;
		}
	}

	public class JoyMoveEventArgs : EventArgs
	{
		public uint JoystickId;
		public JoyAxis Axis;
		public float Position;
		public JoyMoveEventArgs(uint joystick, JoyAxis axis, float position)
		{
			JoystickId = joystick;
			Axis = axis;
			Position = position;
		}
	}

	public class MouseWheelEventArgs : EventArgs
	{
		public int Delta;
		public MouseWheelEventArgs(int delta)
		{
			Delta = delta;
		}
	}

	public class KeyEventArgs : EventArgs
	{
		public KeyCode Code;
		public KeyEventArgs(KeyCode code)
		{
			Code = code;
		}
	}

	#endregion
}

namespace SFML.Graphics
{
	#region Graphics Reimplementation, mostly SFML copypasta

	public struct Vector2
	{
		public float X;
		public float Y;

		public Vector2(float a, float b)
		{
			X = a;
			Y = b;
		}

		public static Vector2 operator+(Vector2 a, Vector2 b)
		{
			return new Vector2(a.X + b.X, a.Y + b.Y);
		}

		public static Vector2 operator-(Vector2 a, Vector2 b)
		{
			return new Vector2(a.X - b.X, a.Y - b.Y);
		}

		public static Vector2 operator*(Vector2 src, float m)
		{
			return new Vector2(src.X * m, src.Y * m);
		}
	}

	public struct Vector3
	{
		public float X;
		public float Y;
		public float Z;

		public Vector3(float a, float b, float c)
		{
			X = a;
			Y = b;
			Z = c;
		}

		public static Vector3 operator+(Vector3 a, Vector3 b)
		{
			return new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
		}

		public static Vector3 operator-(Vector3 a, Vector3 b)
		{
			return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
		}
	}

	public struct IntRect
	{
		public int X1;
		public int Y1;
		public int X2;
		public int Y2;

		public int Width
		{
			get
			{
				return X2 - X1;
			}
		}

		public int Height
		{
			get
			{
				return Y2 - Y1;
			}
		}

		public IntRect(int x1, int y1, int x2, int y2)
		{
			X1 = x1;
			Y1 = y1;
			X2 = x2;
			Y2 = y2;
		}
	}

	public struct FloatRect
	{
		public float X1;
		public float Y1;
		public float X2;
		public float Y2;

		public float Width
		{
			get
			{
				return X2 - X1;
			}
		}

		public float Height
		{
			get
			{
				return Y2 - Y1;
			}
		}

		public FloatRect(float x1, float y1, float x2, float y2)
		{
			X1 = x1;
			Y1 = y1;
			X2 = x2;
			Y2 = y2;
		}
	}

	public class Color
	{
		public byte R;
		public byte G;
		public byte B;
		public byte A;

		public static readonly Color Alpha = new Color(0, 0, 0, 0);
		public static readonly Color Black = new Color(0, 0, 0, 255);
		public static readonly Color White = new Color(255, 255, 255, 255);
		public static readonly Color Green = new Color(0, 255, 0, 255);
		public static readonly Color Red = new Color(255, 0, 0, 255);
		public static readonly Color Cyan = new Color(255, 0, 255, 255);
		public static readonly Color Yellow = new Color(255, 255, 0, 255);

		public Color(byte r, byte g, byte b, byte a = 255)
		{
			R = r;
			G = g;
			B = b;
			A = a;
		}

		public Color(Color other)
		{
			R = other.R;
			G = other.G;
			B = other.B;
			A = other.A;
		}
	}

	public abstract class Drawable
	{
		public Vector2 Position = new Vector2(0.0f, 0.0f);
		public Vector2 Center = new Vector2(0.0f, 0.0f);
		public Vector2 Scale = new Vector2(1.0f, 1.0f);
		public float Rotation = 0.0f;
		public Color Color = Color.White;
		public BlendMode BlendMode = BlendMode.Alpha;
		private float[] matrix = new float[16]
		{
			0.0f, 0.0f, 0.0f, 0.0f,
			0.0f, 0.0f, 0.0f, 0.0f,
			0.0f, 0.0f, 1.0f, 0.0f,
			0.0f, 0.0f, 0.0f, 1.0f,
		};

		public abstract void Render();

		public void Draw(SFML.Window.Window win)
		{
			// RenderTarget::Draw
			Gl.glMatrixMode(Gl.GL_PROJECTION);
			matrix[0] = 2.0f / (float) win.Width;
			matrix[1] = 0.0f;
			matrix[4] = 0.0f;
			matrix[5] = 2.0f / (float) -win.Height;
			matrix[12] = -1.0f;
			matrix[13] = 1.0f;
			Gl.glLoadMatrixf(matrix);
			Gl.glMatrixMode(Gl.GL_MODELVIEW);
			Gl.glLoadIdentity();

			// Drawable::Draw
			Gl.glMatrixMode(Gl.GL_MODELVIEW);
			Gl.glPushMatrix();
			double angle = Rotation * Math.PI / 180.0;
			double cos = Math.Cos(angle);
			double sin = Math.Sin(angle);
			double sxCos = Scale.X * cos;
			double syCos = Scale.Y * cos;
			double sxSin = Scale.X * sin;
			double sySin = Scale.Y * sin;
			matrix[0] =  (float)  sxCos;
			matrix[1] =  (float) -sxSin;
			matrix[4] =  (float) sySin;
			matrix[5] =  (float) syCos;
			matrix[12] = (float) (-Center.X * sxCos - Center.Y * sySin + Position.X);
			matrix[13] = (float) ( Center.X * sxSin - Center.Y * syCos + Position.Y);
			Gl.glMultMatrixf(matrix);
			Gl.glEnable(Gl.GL_BLEND);
			if (BlendMode == BlendMode.Alpha)
			{
				Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			}
			else if (BlendMode == BlendMode.Add)
			{
				Gl.glBlendFunc(Gl.GL_SRC_ALPHA, 1);
			}
			Gl.glColor4f(
				Color.R / 255.0f,
				Color.G / 255.0f,
				Color.B / 255.0f,
				Color.A / 255.0f
			);
			Render();
			Gl.glMatrixMode(Gl.GL_MODELVIEW);
			Gl.glPopMatrix();
		}
	}

	public class Image : IDisposable
	{
		public readonly int Width;
		public readonly int Height;
		private int texture;

		public Image(int width, int height, IntPtr pixels)
		{
			Gl.glGenTextures(1, out texture);
			Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture);
			Gl.glTexImage2D(
				Gl.GL_TEXTURE_2D,
				0,
				Gl.GL_RGBA8,
				width,
				height,
				0,
				Gl.GL_RGBA,
				Gl.GL_UNSIGNED_BYTE,
				pixels
			);
			Gl.glTexParameteri(
				Gl.GL_TEXTURE_2D,
				Gl.GL_TEXTURE_WRAP_S,
				Gl.GL_CLAMP
			);
			Gl.glTexParameteri(
				Gl.GL_TEXTURE_2D,
				Gl.GL_TEXTURE_WRAP_T,
				Gl.GL_CLAMP
			);
			Gl.glTexParameteri(
				Gl.GL_TEXTURE_2D,
				Gl.GL_TEXTURE_MAG_FILTER,
				Gl.GL_LINEAR
			);
			Gl.glTexParameteri(
				Gl.GL_TEXTURE_2D,
				Gl.GL_TEXTURE_MIN_FILTER,
				Gl.GL_LINEAR
			);
			// GLTODO: PrevTexture? -flibit
		}

		public Image(string fileName, bool smooth = true)
		{
			int w, h, bpp;
			IntPtr img = stb.stbi_load(
				fileName,
				out w,
				out h,
				out bpp,
				4
			);
			IntPtr surface = SDL.SDL_CreateRGBSurfaceFrom(
				img,
				w,
				h,
				32,
				w * 4,
				0x000000FF,
				0x0000FF00,
				0x00FF0000,
				0xFF000000
			);
			unsafe
			{
				SDL.SDL_Surface* surPtr = (SDL.SDL_Surface*) surface;
				Width = surPtr->w;
				Height = surPtr->h;
				Gl.glGenTextures(1, out texture);
				Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture);
				SDL.SDL_PixelFormat* format = (SDL.SDL_PixelFormat*) surPtr->format;
				bool hasAlpha = SDL.SDL_BITSPERPIXEL(format->format) == 32;
				Gl.glTexImage2D(
					Gl.GL_TEXTURE_2D,
					0,
					hasAlpha ? Gl.GL_RGBA8 : Gl.GL_RGB8,
					surPtr->w,
					surPtr->h,
					0,
					hasAlpha ? Gl.GL_RGBA : Gl.GL_RGB,
					Gl.GL_UNSIGNED_BYTE,
					surPtr->pixels
				);
				Gl.glTexParameteri(
					Gl.GL_TEXTURE_2D,
					Gl.GL_TEXTURE_WRAP_S,
					Gl.GL_CLAMP
				);
				Gl.glTexParameteri(
					Gl.GL_TEXTURE_2D,
					Gl.GL_TEXTURE_WRAP_T,
					Gl.GL_CLAMP
				);
				Gl.glTexParameteri(
					Gl.GL_TEXTURE_2D,
					Gl.GL_TEXTURE_MAG_FILTER,
					smooth ? Gl.GL_LINEAR : Gl.GL_NEAREST
				);
				Gl.glTexParameteri(
					Gl.GL_TEXTURE_2D,
					Gl.GL_TEXTURE_MIN_FILTER,
					smooth ? Gl.GL_LINEAR : Gl.GL_NEAREST
				);
				// GLTODO: PrevTexture? -flibit
			}
			SDL.SDL_FreeSurface(surface);
			stb.free(img);
		}

		public void Dispose()
		{
			Gl.glDeleteTextures(1, ref texture);
		}

		public void Bind()
		{
			Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture);
		}
	}

	public class Sprite : Drawable, IDisposable
	{
		public Image Image
		{
			get
			{
				return img;
			}
			set
			{
				if (img == null && value != null)
				{
					SubRect = new IntRect(0, 0, (int) value.Width, (int) value.Height);
				}
				img = value;
			}
		}

		public float Width
		{
			get
			{
				return (SubRect.Width) * Scale.X;
			}
			set
			{
				Scale.X = value / (float) SubRect.Width;
			}
		}

		public float Height
		{
			get
			{
				return (SubRect.Height) * Scale.Y;
			}
			set
			{
				Scale.Y = value / (float) SubRect.Height;
			}
		}

		public IntRect SubRect;
		private Image img;

		public Sprite()
		{
			SubRect = new IntRect(0, 0, 1, 1);
		}

		public Sprite(Image image)
		{
			Image = image;
		}

		public void Dispose()
		{
			img = null;
		}

		public override void Render()
		{
			if (img == null)
			{
				Console.WriteLine("NULL IMAGE");
				return; // Should only be loading mouse cursor...? -flibit
			}
			float left = (SubRect.X1 / (float) Image.Width);
			float top = (SubRect.Y1 / (float) Image.Height);
			float right = (SubRect.X2 / (float) Image.Width);
			float bottom = (SubRect.Y2 / (float) Image.Height);
			float width = SubRect.Width;
			float height = SubRect.Height;
			img.Bind();
			Gl.glBegin(Gl.GL_QUADS);
				Gl.glTexCoord2f(left, top);
					Gl.glVertex2f(0.0f, 0.0f);
				Gl.glTexCoord2f(left, bottom);
					Gl.glVertex2f(0.0f, height);
				Gl.glTexCoord2f(right, bottom);
					Gl.glVertex2f(width, height);
				Gl.glTexCoord2f(right, top);
					Gl.glVertex2f(width, 0.0f);
			Gl.glEnd();
		}
	}

	public class Font
	{
		public const int IMG_SIZE = 512;

		public readonly uint Size;

		internal stb.stbtt_bakedchar[] ASCIIGlyphs;

		private int texture;

		public Font(string name, uint size)
		{
			ASCIIGlyphs = new stb.stbtt_bakedchar[96 * 2];
			Size = size;

			Gl.glGenTextures(1, out texture);
			Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture);
			Gl.glTexImage2D(
				Gl.GL_TEXTURE_2D,
				0,
				Gl.GL_RGBA8,
				IMG_SIZE,
				IMG_SIZE * 2,
				0,
				Gl.GL_RGBA,
				Gl.GL_UNSIGNED_BYTE,
				IntPtr.Zero
			);
			Gl.glTexParameteri(
				Gl.GL_TEXTURE_2D,
				Gl.GL_TEXTURE_WRAP_S,
				Gl.GL_CLAMP
			);
			Gl.glTexParameteri(
				Gl.GL_TEXTURE_2D,
				Gl.GL_TEXTURE_WRAP_T,
				Gl.GL_CLAMP
			);
			Gl.glTexParameteri(
				Gl.GL_TEXTURE_2D,
				Gl.GL_TEXTURE_MAG_FILTER,
				Gl.GL_LINEAR
			);
			Gl.glTexParameteri(
				Gl.GL_TEXTURE_2D,
				Gl.GL_TEXTURE_MIN_FILTER,
				Gl.GL_LINEAR
			);
			// GLTODO: PrevTexture? -flibit

			byte[] ttf = File.ReadAllBytes(name);
			byte[] img = new byte[IMG_SIZE * IMG_SIZE];
			byte[] imgRGBA = new byte[IMG_SIZE * IMG_SIZE * 4];
			GCHandle imgHandle = GCHandle.Alloc(imgRGBA, GCHandleType.Pinned);
			IntPtr pix = imgHandle.AddrOfPinnedObject();
			GCHandle glyphHandle = GCHandle.Alloc(ASCIIGlyphs, GCHandleType.Pinned);
			IntPtr glyph = glyphHandle.AddrOfPinnedObject();
			for (int i = 1; i >= 0; i -= 1)
			{
				stb.stbtt_BakeFontBitmap(
					ttf,
					0,
					(float) size,
					img,
					IMG_SIZE,
					IMG_SIZE,
					32 + (128 * i),
					96,
					glyph
				);
				if (i == 1)
				{
					Array.Copy(ASCIIGlyphs, 0, ASCIIGlyphs, 96, 96);
				}
				for (int j = 0; j < img.Length; j += 1)
				{
					imgRGBA[j * 4] = 255;
					imgRGBA[j * 4 + 1] = 255;
					imgRGBA[j * 4 + 2] = 255;
					imgRGBA[j * 4 + 3] = img[j];
				}
				Gl.glTexSubImage2D(
					Gl.GL_TEXTURE_2D,
					0,
					0,
					IMG_SIZE * i,
					IMG_SIZE,
					IMG_SIZE,
					Gl.GL_RGBA,
					Gl.GL_UNSIGNED_BYTE,
					pix
				);
			}
			glyphHandle.Free();
			imgHandle.Free();
			imgRGBA = null;
			img = null;
			ttf = null;
		}

		// FIXME: Dispose! -flibit

		internal void Bind()
		{
			Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture);
		}
	}

	public class String2D : Drawable, IDisposable
	{
		public string Text
		{
			set
			{
				if (value != currentString)
				{
					currentString = value;
					needsUpdate = true;
				}
			}
		}

		public Font Font
		{
			set
			{
				if (value != font)
				{
					font = value;
					needsUpdate = true;
				}
			}
		}

		private string currentString;

		private Font font;
		private float size;

		private bool needsUpdate;

		private float Width = 0.0f;
		private float Height = 0.0f;

		public String2D()
		{
			// FIXME: Default font? -flibit
			font = null;
			size = 30.0f;
			currentString = string.Empty;
			needsUpdate = false; // Would be true if we had a font
		}

		public void Dispose()
		{
			Font = null;
		}

		public FloatRect GetRect()
		{
			if (needsUpdate)
			{
				needsUpdate = false;
				Width = 0.0f;
				Height = 0.0f;
				if (!string.IsNullOrEmpty(currentString))
				{
					float curWidth = 0.0f;
					float factor = size / (float) font.Size;

					for (int i = 0; i < currentString.Length; i += 1)
					{
						char sym = currentString[i];
						stb.stbtt_bakedchar glyph;
						if (sym > 31 && sym < 127)
						{
							glyph = font.ASCIIGlyphs[sym - 32];
							if (sym == ' ')
							{
								curWidth += glyph.xadvance * factor;
								continue;
							}
						}
						else if (sym > 159 && sym < 255)
						{
							glyph = font.ASCIIGlyphs[sym - 64];
						}
						else if (sym == '\t')
						{
							curWidth += font.ASCIIGlyphs[0].xadvance * 4 * factor;
							continue;
						}
						else if (sym == '\v')
						{
							Height += size * 4;
							continue;
						}
						else if (sym == '\n')
						{
							Height += size;
							if (curWidth > Width)
							{
								Width = curWidth;
							}
							curWidth = 0;
							continue;
						}
						else if (sym == '\r')
						{
							// Ahem, Win32 newlines...
							continue;
						}
						else if (sym == 8217) // ASZ assets fix
						{
							glyph = font.ASCIIGlyphs['\'' - 32];
						}
						else
						{
							Console.WriteLine("Missing char? " + sym.ToString());
							continue;
						}

						curWidth += glyph.xadvance * factor;
					}

					if (curWidth > Width)
					{
						Width += curWidth;
					}
					Height += size;
				}
			}
			return new FloatRect(
				-Center.X * Scale.X + Position.X,
				-Center.Y * Scale.Y + Position.Y,
				(Width - Center.X) * Scale.X + Position.X,
				(Height - Center.Y) * Scale.Y + Position.Y
			);
		}

		public Vector2 GetCharacterPos(uint index)
		{
			if (index > currentString.Length)
			{
				index = (uint) currentString.Length;
			}

			float factor = size / (float) font.Size;

			Vector2 pos = new Vector2(0.0f, 0.0f);
			for (int i = 0; i < index; i += 1)
			{
				char sym = currentString[i];
				stb.stbtt_bakedchar glyph;
				if (sym > 31 && sym < 127)
				{
					glyph = font.ASCIIGlyphs[sym - 32];
					if (sym == ' ')
					{
						pos.X += glyph.xadvance * factor;
						continue;
					}
				}
				else if (sym > 159 && sym < 255)
				{
					glyph = font.ASCIIGlyphs[sym - 64];
				}
				else if (sym == '\t')
				{
					pos.X += font.ASCIIGlyphs[0].xadvance * 4 * factor;
					continue;
				}
				else if (sym == '\v')
				{
					pos.Y += size * 4;
					continue;
				}
				else if (sym == '\n')
				{
					pos.Y += size;
					pos.X = 0;
					continue;
				}
				else if (sym == '\r')
				{
					// Ahem, Win32 newlines...
					continue;
				}
				else if (sym == 8217) // ASZ assets fix
				{
					glyph = font.ASCIIGlyphs['\'' - 32];
				}
				else
				{
					Console.WriteLine("Missing char? " + sym.ToString());
					continue;
				}
				pos.X += glyph.xadvance * factor;
			}
			return pos;
		}

		public override void Render()
		{
			if (string.IsNullOrEmpty(currentString))
			{
				return;
			}

			float charSize = font.Size;
			float factor = size / charSize;
			float x = 0.0f;
			float y = charSize;

			font.Bind();
			Gl.glScalef(factor, factor, 1.0f);
			Gl.glBegin(Gl.GL_QUADS);
			for (int i = 0; i < currentString.Length; i += 1)
			{
				char sym = currentString[i];
				stb.stbtt_bakedchar glyph;
				if (sym > 31 && sym < 127)
				{
					glyph = font.ASCIIGlyphs[sym - 32];
					if (sym == ' ')
					{
						x += glyph.xadvance;
						continue;
					}
				}
				else if (sym > 159 && sym < 255)
				{
					glyph = font.ASCIIGlyphs[sym - 64];
				}
				else if (sym == '\t')
				{
					x += font.ASCIIGlyphs[0].xadvance * 4;
					continue;
				}
				else if (sym == '\v')
				{
					y += charSize * 4.0f;
					continue;
				}
				else if (sym == '\n')
				{
					y += charSize;
					x = 0;
					continue;
				}
				else if (sym == '\r')
				{
					// Ahem, Win32 newlines...
					continue;
				}
				else if (sym == 8217) // ASZ assets fix
				{
					glyph = font.ASCIIGlyphs['\'' - 32];
				}
				else
				{
					Console.WriteLine("Missing char? " + sym.ToString());
					continue;
				}

				float left = (float) Math.Floor(x + glyph.xoff + 0.5f);
				float top = (float) Math.Floor(y + glyph.yoff + 0.5f);
				float right = left + glyph.x1 - glyph.x0;
				float bottom = top + glyph.y1 - glyph.y0;
				Gl.glTexCoord2f(
					glyph.x0 / (float) Font.IMG_SIZE,
					glyph.y0 / (Font.IMG_SIZE * 2.0f)
				);
				Gl.glVertex2f(
					left,
					top
				);
				Gl.glTexCoord2f(
					glyph.x0 / (float) Font.IMG_SIZE,
					glyph.y1 / (Font.IMG_SIZE * 2.0f)
				);
				Gl.glVertex2f(
					left,
					bottom
				);
				Gl.glTexCoord2f(
					glyph.x1 / (float) Font.IMG_SIZE,
					glyph.y1 / (Font.IMG_SIZE * 2.0f)
				);
				Gl.glVertex2f(
					right,
					bottom
				);
				Gl.glTexCoord2f(
					glyph.x1 / (float) Font.IMG_SIZE,
					glyph.y0 / (Font.IMG_SIZE * 2.0f)
				);
				Gl.glVertex2f(
					right,
					top
				);

				x += glyph.xadvance;
			}
			Gl.glEnd();
		}
	}

	public enum BlendMode
	{
		Add,
		Alpha
	}

	public class RenderWindow : SFML.Window.Window
	{
		public RenderWindow(VideoMode mode, string title, Styles style) : base(mode, title, style)
		{
		}
	}

	#endregion
}

namespace SFML.Audio
{
	#region Audio Reimplementation, OpenAL#

	public enum SoundStatus
	{
		Playing,
		Stopped
	}

	public class Sound
	{
		public float Volume
		{
			get
			{
				float result;
				AL10.alGetSourcef(source, AL10.AL_GAIN, out result);
				return result * 100.0f;
			}
			set
			{
				AL10.alSourcef(source, AL10.AL_GAIN, value / 100.0f);
			}
		}

		public SoundStatus Status
		{
			get
			{
				int result;
				AL10.alGetSourcei(source, AL10.AL_SOURCE_STATE, out result);
				return (result != AL10.AL_STOPPED && result != AL10.AL_INITIAL) ?
					SoundStatus.Playing :
					SoundStatus.Stopped;
			}
		}

		public bool Loop
		{
			get
			{
				int result;
				AL10.alGetSourcei(
					source,
					AL10.AL_LOOPING,
					out result
				);
				return result == AL10.AL_TRUE;
			}
			set
			{
				AL10.alSourcei(
					source,
					AL10.AL_LOOPING,
					AL10.AL_TRUE
				);
			}
		}

		public float Pitch
		{
			get
			{
				float result;
				AL10.alGetSourcef(source, AL10.AL_PITCH, out result);
				return result;
			}
			set
			{
				AL10.alSourcef(
					source,
					AL10.AL_PITCH,
					value
				);
			}
		}

		public SoundBuffer SoundBuffer
		{
			set
			{
				AL10.alSourcei(
					source,
					AL10.AL_BUFFER,
					(int) value.buffer
				);
			}
		}

		private uint source;

		public Sound()
		{
			AL10.alGenSources(1, out source);
		}

		~Sound()
		{
			AL10.alDeleteSources(1, ref source);
		}

		public void Play()
		{
			AL10.alSourcePlay(source);
		}

		public void Stop()
		{
			AL10.alSourceStop(source);
		}

		public void Pause()
		{
			AL10.alSourcePause(source);
		}
	}

	public class Music
	{
		public float Volume
		{
			get
			{
				float result;
				AL10.alGetSourcef(source, AL10.AL_GAIN, out result);
				return result * 100.0f;
			}
			set
			{
				AL10.alSourcef(source, AL10.AL_GAIN, value / 100.0f);
			}
		}

		public SoundStatus Status
		{
			get
			{
				int result;
				AL10.alGetSourcei(source, AL10.AL_SOURCE_STATE, out result);
				return (result != AL10.AL_STOPPED && result != AL10.AL_INITIAL) ?
					SoundStatus.Playing :
					SoundStatus.Stopped;
			}
		}

		public bool Loop;

		private uint source;
		private uint[] musBuffers;
		private IntPtr vorbis;
		private bool vorbisOver;

		public Music(string name)
		{
			musBuffers = new uint[2];
			AL10.alGenSources(1, out source);
			AL10.alGenBuffers(musBuffers.Length, musBuffers);
			int error;
			vorbis = stb.stb_vorbis_open_filename(
				name,
				out error,
				IntPtr.Zero
			);
			vorbisOver = false;
			Loop = false;
		}

		~Music()
		{
			stb.stb_vorbis_close(vorbis);
			AL10.alDeleteBuffers(musBuffers.Length, musBuffers);
			AL10.alDeleteSources(1, ref source);
		}

		public void Play()
		{
			float[] buf = new float[44100 * 2];
			for (int i = 0; i < musBuffers.Length; i += 1)
			{
				int samples = stb.stb_vorbis_get_samples_float_interleaved(
					vorbis,
					2,
					buf,
					buf.Length
				);
				AL10.alBufferData(
					musBuffers[i],
					ALEXT.AL_FORMAT_STEREO_FLOAT32,
					buf,
					samples * 8,
					44100
				);
			}
			AL10.alSourceQueueBuffers(source, musBuffers.Length, musBuffers);
			AL10.alSourcePlay(source);
			vorbisOver = false;
			SDLGlobals.MusicUpdate += Update;
		}

		public void Stop()
		{
			SDLGlobals.MusicUpdate -= Update;
			uint[] bufs = new uint[musBuffers.Length];
			AL10.alSourceStop(source);
			AL10.alSourceUnqueueBuffers(source, bufs.Length, bufs);
			stb.stb_vorbis_seek_start(vorbis);
		}

		public void Update()
		{
			if (vorbisOver)
			{
				SDLGlobals.MusicUpdate -= Update; // FIXME: ???
				return;
			}
			int finished;
			AL10.alGetSourcei(source, AL10.AL_BUFFERS_PROCESSED, out finished);
			uint[] bufs = null;
			float[] buf = null;
			if (finished > 0)
			{
				bufs = new uint[finished];
				buf = new float[44100 * 2];
				AL10.alSourceUnqueueBuffers(source, finished, bufs);
			}
			for (int i = 0; i < finished; i += 1)
			{
				int samples = stb.stb_vorbis_get_samples_float_interleaved(
					vorbis,
					2,
					buf,
					buf.Length
				);
				if (samples == 0)
				{
					if (Loop)
					{
						stb.stb_vorbis_seek_start(vorbis);
						i -= 1;
						continue;
					}
					else
					{
						vorbisOver = true;
						break;
					}
				}
				AL10.alBufferData(
					bufs[i],
					ALEXT.AL_FORMAT_STEREO_FLOAT32,
					buf,
					samples * 8,
					44100
				);
				AL10.alSourceQueueBuffers(source, 1, ref bufs[i]);
			}
		}
	}

	public class SoundBuffer
	{
		internal uint buffer;

		public SoundBuffer(string name)
		{
			AL10.alGenBuffers(1, out buffer);
			int channels, sampleRate;
			IntPtr output;
			int samples = stb.stb_vorbis_decode_filename(
				name,
				out channels,
				out sampleRate,
				out output
			);
			AL10.alBufferData(
				buffer,
				(channels == 2) ?
					AL10.AL_FORMAT_STEREO16 :
					AL10.AL_FORMAT_MONO16,
				output,
				samples * 2 * channels,
				sampleRate
			);
			stb.free(output);
		}

		~SoundBuffer()
		{
			AL10.alDeleteBuffers(1, ref buffer);
		}
	}

	#endregion
}

namespace Tao.OpenGl
{
	#region Tao.Gl Reimplementation, styled like OpenAL# and FNA OpenGLDevice_GL

	public static class Gl
	{
		/* typedef int GLenum */
		public const int GL_TRUE =			0x0001;
		public const int GL_FALSE =			0x0000;
		public const int GL_VENDOR =			0x1F00;
		public const int GL_RENDERER =			0x1F01;
		public const int GL_VERSION =			0x1F02;
		public const int GL_LINES =			0x0001;
		public const int GL_QUADS =			0x0007;
		public const int GL_COLOR_BUFFER_BIT =		0x4000;
		public const int GL_DEPTH_BUFFER_BIT =		0x0100;
		public const int GL_MODELVIEW =			0x1700;
		public const int GL_PROJECTION =		0x1701;
		public const int GL_BLEND =			0x0BE2;
		public const int GL_ALPHA_TEST =		0x0BC0;
		public const int GL_DEPTH_TEST =		0x0B71;
		public const int GL_TEXTURE_2D =		0x0DE1;
		public const int GL_SRC_ALPHA =			0x0302;
		public const int GL_ONE_MINUS_SRC_ALPHA =	0x0303;
		public const int GL_MODELVIEW_MATRIX =		0x0BA6;
		public const int GL_PROJECTION_MATRIX =		0x0BA7;
		public const int GL_VIEWPORT =			0x0BA2;
		public const int GL_CURRENT_BIT =		0x00000001;
		public const int GL_VIEWPORT_BIT =		0x00000800;
		public const int GL_TRANSFORM_BIT =		0x00001000;
		public const int GL_ENABLE_BIT =		0x00002000;
		public const int GL_TEXTURE_BIT =		0x00040000;
		public const int GL_RGB =			0x1907;
		public const int GL_RGBA =			0x1908;
		public const int GL_RGB8 =			0x8051;
		public const int GL_RGBA8 =			0x8058;
		public const int GL_UNSIGNED_BYTE =		0x1401;
		public const int GL_CLAMP =			0x2900;
		public const int GL_NEAREST =			0x2600;
		public const int GL_LINEAR =			0x2601;
		public const int GL_TEXTURE_MAG_FILTER =	0x2800;
		public const int GL_TEXTURE_MIN_FILTER =	0x2801;
		public const int GL_TEXTURE_WRAP_S =		0x2802;
		public const int GL_TEXTURE_WRAP_T =		0x2803;

		public delegate void GenTextures(int n, out int textures);
		public static GenTextures glGenTextures;

		public delegate void DeleteTextures(int n, ref int textures);
		public static DeleteTextures glDeleteTextures;

		public delegate void BindTexture(int target, int texture);
		public static BindTexture glBindTexture;

		public delegate void TexImage2D(
			int target,
			int level,
			int internalFormat,
			int width,
			int height,
			int border,
			int format,
			int type,
			IntPtr data
		);
		public static TexImage2D glTexImage2D;

		public delegate void TexSubImage2D(
			int target,
			int level,
			int xoffset,
			int yoffset,
			int width,
			int height,
			int format,
			int type,
			IntPtr data
		);
		public static TexSubImage2D glTexSubImage2D;

		public delegate void Vertex2f(float x, float y);
		public static Vertex2f glVertex2f;

		public delegate void LoadMatrixf(float[] mult);
		public static LoadMatrixf glLoadMatrixf;

		public delegate void MultMatrixf(float[] mult);
		public static MultMatrixf glMultMatrixf;

		public delegate void MultMatrixd(double[] mult);
		public static MultMatrixd glMultMatrixd;

		public delegate void ClearColor(float r, float g, float b, float a);
		public static ClearColor glClearColor;

		public delegate void TexParameteri(int target, int pname, int param);
		public static TexParameteri glTexParameteri;

		public delegate void Scalef(float x, float y, float z);
		public static Scalef glScalef;

		public delegate void Viewport(int x, int y, int width, int height);
		public static Viewport glViewport;

		public delegate void Enable(int cap);
		public static Enable glEnable;

		public delegate void Disable(int cap);
		public static Disable glDisable;

		public delegate void Clear(int mask);
		public static Clear glClear;

		public delegate void Begin(int mode);
		public static Begin glBegin;

		public delegate void End();
		public static End glEnd;

		public delegate void Vertex3f(float x, float y, float z);
		public static Vertex3f glVertex3f;

		public delegate void TexCoord2f(float x, float y);
		public static TexCoord2f glTexCoord2f;

		public delegate void Color3f(float r, float g, float b);
		public static Color3f glColor3f;

		public delegate void Color4f(float r, float g, float b, float a);
		public static Color4f glColor4f;

		public delegate void LineWidth(float width);
		public static LineWidth glLineWidth;

		public delegate void DepthMask(byte flag);
		public static DepthMask glDepthMask;

		public delegate void Translatef(float x, float y, float z);
		public static Translatef glTranslatef;

		public delegate void Rotatef(float angle, float x, float y, float z);
		public static Rotatef glRotatef;

		public delegate void BlendFunc(int sfactor, int dfactor);
		public static BlendFunc glBlendFunc;

		public delegate void LoadIdentity();
		public static LoadIdentity glLoadIdentity;

		public delegate void MatrixMode(int mode);
		public static MatrixMode glMatrixMode;

		public delegate void PushMatrix();
		public static PushMatrix glPushMatrix;

		public delegate void PopMatrix();
		public static PopMatrix glPopMatrix;

		public delegate void PushAttrib(int mask);
		public static PushAttrib glPushAttrib;

		public delegate void PopAttrib();
		public static PopAttrib glPopAttrib;

		public delegate void GetIntegerv(int name, int[] values);
		public static GetIntegerv glGetIntegerv;

		public delegate void GetDoublev(int name, double[] values);
		public static GetDoublev glGetDoublev;

		public delegate IntPtr GetString(int name);
		public static GetString glGetString;

		public static void Init()
		{
			glEnable = (Enable) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glEnable"),
				typeof(Enable)
			);
			glDisable = (Disable) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glDisable"),
				typeof(Disable)
			);
			glClear = (Clear) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glClear"),
				typeof(Clear)
			);
			glBegin = (Begin) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glBegin"),
				typeof(Begin)
			);
			glEnd = (End) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glEnd"),
				typeof(End)
			);
			glVertex3f = (Vertex3f) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glVertex3f"),
				typeof(Vertex3f)
			);
			glTexCoord2f = (TexCoord2f) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glTexCoord2f"),
				typeof(TexCoord2f)
			);
			glColor3f = (Color3f) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glColor3f"),
				typeof(Color3f)
			);
			glColor4f = (Color4f) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glColor4f"),
				typeof(Color4f)
			);
			glLineWidth = (LineWidth) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glLineWidth"),
				typeof(LineWidth)
			);
			glDepthMask = (DepthMask) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glDepthMask"),
				typeof(DepthMask)
			);
			glTranslatef = (Translatef) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glTranslatef"),
				typeof(Translatef)
			);
			glRotatef = (Rotatef) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glRotatef"),
				typeof(Rotatef)
			);
			glBlendFunc = (BlendFunc) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glBlendFunc"),
				typeof(BlendFunc)
			);
			glLoadIdentity = (LoadIdentity) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glLoadIdentity"),
				typeof(LoadIdentity)
			);
			glMatrixMode = (MatrixMode) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glMatrixMode"),
				typeof(MatrixMode)
			);
			glPushMatrix = (PushMatrix) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glPushMatrix"),
				typeof(PushMatrix)
			);
			glPopMatrix = (PopMatrix) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glPopMatrix"),
				typeof(PopMatrix)
			);
			glPushAttrib = (PushAttrib) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glPushAttrib"),
				typeof(PushAttrib)
			);
			glPopAttrib = (PopAttrib) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glPopAttrib"),
				typeof(PopAttrib)
			);
			glGetIntegerv = (GetIntegerv) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glGetIntegerv"),
				typeof(GetIntegerv)
			);
			glGetDoublev = (GetDoublev) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glGetDoublev"),
				typeof(GetDoublev)
			);
			glGetString = (GetString) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glGetString"),
				typeof(GetString)
			);

			glGenTextures = (GenTextures) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glGenTextures"),
				typeof(GenTextures)
			);
			glDeleteTextures = (DeleteTextures) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glDeleteTextures"),
				typeof(DeleteTextures)
			);
			glBindTexture = (BindTexture) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glBindTexture"),
				typeof(BindTexture)
			);
			glTexImage2D = (TexImage2D) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glTexImage2D"),
				typeof(TexImage2D)
			);
			glTexSubImage2D = (TexSubImage2D) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glTexSubImage2D"),
				typeof(TexSubImage2D)
			);
			glVertex2f = (Vertex2f) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glVertex2f"),
				typeof(Vertex2f)
			);
			glLoadMatrixf = (LoadMatrixf) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glLoadMatrixf"),
				typeof(LoadMatrixf)
			);
			glMultMatrixf = (MultMatrixf) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glMultMatrixf"),
				typeof(MultMatrixf)
			);
			glMultMatrixd = (MultMatrixd) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glMultMatrixd"),
				typeof(MultMatrixd)
			);
			glClearColor = (ClearColor) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glClearColor"),
				typeof(ClearColor)
			);
			glTexParameteri = (TexParameteri) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glTexParameteri"),
				typeof(TexParameteri)
			);
			glScalef = (Scalef) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glScalef"),
				typeof(Scalef)
			);
			glViewport = (Viewport) Marshal.GetDelegateForFunctionPointer(
				SDL.SDL_GL_GetProcAddress("glViewport"),
				typeof(Viewport)
			);
		}
	}

	#endregion

	#region GLU Reimplementation, based on Mesa libutil/project.c

	public static class Glu
	{
		public static void gluPerspective(
			double fovy,
			double aspect,
			double zNear,
			double zFar
		) {
			double fovRad = fovy / 2.0 * Math.PI / 180.0;
			double f = Math.Cos(fovRad) / Math.Sin(fovRad);
			double[] matrix = new double[16]
			{
				f / aspect, 0.0, 0.0, 0.0,
				0.0, f, 0.0, 0.0,
				0.0, 0.0, (zFar + zNear) / (zNear - zFar), (2.0 * zFar * zNear) / (zNear - zFar),
				0.0, 0.0, -1.0, 0.0
			};
			Gl.glMultMatrixd(matrix);
		}

		public static int gluProject(
			double objX,
			double objY,
			double objZ,
			double[] model,
			double[] proj,
			int[] view,
			out double winX,
			out double winY,
			out double winZ
		) {
			double[] din = new double[4]
			{
				objX,
				objY,
				objZ,
				1.0
			};
			double[] dout = new double[4];

			MultiplyVecd(model, din, dout);
			MultiplyVecd(proj, dout, din);

			if (din[3] == 0.0)
			{
				winX = 0;
				winY = 0;
				winZ = 0;
				return 0;
			}

			din[0] /= din[3];
			din[1] /= din[3];
			din[2] /= din[3];
			din[0] = din[0] * 0.5 + 0.5;
			din[1] = din[1] * 0.5 + 0.5;

			winX = din[0] * view[2] + view[0];
			winY = din[1] * view[3] + view[1];
			winZ = din[2] * 0.5 + 0.5;

			return 1;
		}

		public static int gluUnProject(
			double winX,
			double winY,
			double winZ,
			double[] model,
			double[] proj,
			int[] view,
			out double objX,
			out double objY,
			out double objZ
		) {
			double[] final = new double[16];
			double[] din = new double[4]
			{
				winX,
				winY,
				winZ,
				1.0
			};
			double[] dout = new double[4];

			Multiply(model, proj, final);
			if (!InvertMatrixd(final))
			{
				objX = 0;
				objY = 0;
				objZ = 0;
				return 0;
			}

			din[0] = (din[0] - view[0]) / view[2];
			din[1] = (din[1] - view[1]) / view[3];

			din[0] = din[0] * 2.0 - 1.0;
			din[1] = din[1] * 2.0 - 1.0;
			din[2] = din[2] * 2.0 - 1.0;

			MultiplyVecd(final, din, dout);

			if (dout[3] == 0.0)
			{
				objX = 0;
				objY = 0;
				objZ = 0;
				return 0;
			}

			objX = dout[0] / dout[3];
			objY = dout[1] / dout[3];
			objZ = dout[2] / dout[3];

			return 1;
		}

		private static void Multiply(
			double[] a,
			double[] b,
			double[] r
		) {
			for (int i = 0; i < 4; i += 1)
			{
				for (int j = 0; j < 4; j += 1)
				{
					r[i * 4 + j] = 
						a[i * 4] * b[j] +
						a[i * 4 + 1] * b[4 + j] +
						a[i * 4 + 2] * b[8 + j] +
						a[i * 4 + 3] * b[12 + j];
				}
			}
		}

		private static void MultiplyVecd(
			double[] matrix,
			double[] din,
			double[] dout
		) {
			for (int i = 0; i < 4; i += 1)
			{
				dout[i] = 
					din[0] * matrix[i] +
					din[1] * matrix[4 + i] +
					din[2] * matrix[8 + i] +
					din[3] * matrix[12 + i];
			}
		}

		private static bool InvertMatrixd(double[] m)
		{

			double[] inv = new double[16];
			inv[0] =   m[5]*m[10]*m[15] - m[5]*m[11]*m[14] - m[9]*m[6]*m[15]
				+ m[9]*m[7]*m[14] + m[13]*m[6]*m[11] - m[13]*m[7]*m[10];
			inv[4] =  -m[4]*m[10]*m[15] + m[4]*m[11]*m[14] + m[8]*m[6]*m[15]
				- m[8]*m[7]*m[14] - m[12]*m[6]*m[11] + m[12]*m[7]*m[10];
			inv[8] =   m[4]*m[9]*m[15] - m[4]*m[11]*m[13] - m[8]*m[5]*m[15]
				+ m[8]*m[7]*m[13] + m[12]*m[5]*m[11] - m[12]*m[7]*m[9];
			inv[12] = -m[4]*m[9]*m[14] + m[4]*m[10]*m[13] + m[8]*m[5]*m[14]
				- m[8]*m[6]*m[13] - m[12]*m[5]*m[10] + m[12]*m[6]*m[9];
			inv[1] =  -m[1]*m[10]*m[15] + m[1]*m[11]*m[14] + m[9]*m[2]*m[15]
				- m[9]*m[3]*m[14] - m[13]*m[2]*m[11] + m[13]*m[3]*m[10];
			inv[5] =   m[0]*m[10]*m[15] - m[0]*m[11]*m[14] - m[8]*m[2]*m[15]
				+ m[8]*m[3]*m[14] + m[12]*m[2]*m[11] - m[12]*m[3]*m[10];
			inv[9] =  -m[0]*m[9]*m[15] + m[0]*m[11]*m[13] + m[8]*m[1]*m[15]
				- m[8]*m[3]*m[13] - m[12]*m[1]*m[11] + m[12]*m[3]*m[9];
			inv[13] =  m[0]*m[9]*m[14] - m[0]*m[10]*m[13] - m[8]*m[1]*m[14]
				+ m[8]*m[2]*m[13] + m[12]*m[1]*m[10] - m[12]*m[2]*m[9];
			inv[2] =   m[1]*m[6]*m[15] - m[1]*m[7]*m[14] - m[5]*m[2]*m[15]
				+ m[5]*m[3]*m[14] + m[13]*m[2]*m[7] - m[13]*m[3]*m[6];
			inv[6] =  -m[0]*m[6]*m[15] + m[0]*m[7]*m[14] + m[4]*m[2]*m[15]
				- m[4]*m[3]*m[14] - m[12]*m[2]*m[7] + m[12]*m[3]*m[6];
			inv[10] =  m[0]*m[5]*m[15] - m[0]*m[7]*m[13] - m[4]*m[1]*m[15]
				+ m[4]*m[3]*m[13] + m[12]*m[1]*m[7] - m[12]*m[3]*m[5];
			inv[14] = -m[0]*m[5]*m[14] + m[0]*m[6]*m[13] + m[4]*m[1]*m[14]
				- m[4]*m[2]*m[13] - m[12]*m[1]*m[6] + m[12]*m[2]*m[5];
			inv[3] =  -m[1]*m[6]*m[11] + m[1]*m[7]*m[10] + m[5]*m[2]*m[11]
				- m[5]*m[3]*m[10] - m[9]*m[2]*m[7] + m[9]*m[3]*m[6];
			inv[7] =   m[0]*m[6]*m[11] - m[0]*m[7]*m[10] - m[4]*m[2]*m[11]
				+ m[4]*m[3]*m[10] + m[8]*m[2]*m[7] - m[8]*m[3]*m[6];
			inv[11] = -m[0]*m[5]*m[11] + m[0]*m[7]*m[9] + m[4]*m[1]*m[11]
				- m[4]*m[3]*m[9] - m[8]*m[1]*m[7] + m[8]*m[3]*m[5];
			inv[15] =  m[0]*m[5]*m[10] - m[0]*m[6]*m[9] - m[4]*m[1]*m[10]
				+ m[4]*m[2]*m[9] + m[8]*m[1]*m[6] - m[8]*m[2]*m[5];

			double det = m[0]*inv[0] + m[1]*inv[4] + m[2]*inv[8] + m[3]*inv[12];
			if (det == 0)
			{
				return false;
			}
			det = 1.0 / det;

			for (int i = 0; i < 16; i += 1)
			{
				m[i] = inv[i] * det;
			}

			return true;
		}
	}

	#endregion
}
