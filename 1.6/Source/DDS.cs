using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits {
	// Partial DDS file structure from DirectX 9.0 documentation
	// Most of this is not really needed ingame since Unity only supports DXT1 and DXT5
	// …but I still like to at least validate the file since other formats can be found in the wild.
	// Did you know that ImageMagick defaults to DXT5? You can use `-define dds:compression=dxt1` to change that behavior. (TIL)
	public class DDS {
		#region File parsing
		[Flags]
		public enum DDSD : uint {
			Caps = 0x00000001,
			Height = 0x00000002,
			Width = 0x00000004,
			Pitch = 0x00000008,
			PixelFormat = 0x00001000,
			MipmapCount = 0x00020000,
			LinearSize = 0x00080000,
			Depth = 0x00800000
		}
		[Flags]
		public enum DDPF : uint {
			AlphaPixels = 0x00000001,
			FourCC = 0x00000004,
			RGB = 0x00000040,
		}

		public struct HeaderDDS {
			// Everything is LittleEndian
			public uint dwMagic; // Must be 0x20534444 (== "DDS " in ASCII LE)
			public uint dwSize; // Must be 0x0000007C (124)
			public DDSD dwFlags; // DDSD flags; Must match DDSD.Caps | DDSD.PixelFormat | DDSD.Width | DDSD.Height
			public uint dwHeight; // In pixels
			public uint dwWidth; // In pixels
			public uint dwPitchOrLinearSize; // If has flag DDSD.Pitch, then bytes per line; otherwise must have flag DDSD.LinearSize, then total amount of bytes
			public uint dwDepth; // For volume textures, must have flag DDSD.Depth
			public uint dwMipMapCount; // For images with mipmap levels, must have flag DDSD.MipmapCount;
			public DDS_PixelFormat ddpfPixelFormat;
			// There's more stuff after this, but we don't need that.

			public uint DataOffset => 4 + dwSize; // 4 for magic; == 0x80 (128)

			public static HeaderDDS Parse(byte[] data) {
				HeaderDDS dds = new HeaderDDS();
				using (MemoryStream ms = new MemoryStream(data))
				using (BinaryReader br = new BinaryReader(ms)) {
					dds.dwMagic = br.ReadUInt32();
					if (dds.dwMagic != 0x20534444) throw new FormatException($"Invalid DDS magic: 0x{dds.dwMagic:X8}");
					dds.dwSize = br.ReadUInt32();
					if (dds.dwSize != 0x0000007C) throw new FormatException($"Invalid DDS header size: 0x{dds.dwSize:X8}");
					dds.dwFlags = (DDSD)br.ReadUInt32();
					if (!dds.dwFlags.HasFlag(DDSD.Caps)) throw new FormatException($"Invalid DDS flag, no DDSD_CAPS: 0x{dds.dwFlags:X8}");
					if (!dds.dwFlags.HasFlag(DDSD.PixelFormat)) throw new FormatException($"Invalid DDS flag, no DDSD_PIXELFORMAT: 0x{dds.dwFlags:X8}");
					if (!dds.dwFlags.HasFlag(DDSD.Width)) throw new FormatException($"Invalid DDS flag, no DDSD_WIDTH: 0x{dds.dwFlags:X8}");
					if (!dds.dwFlags.HasFlag(DDSD.Height)) throw new FormatException($"Invalid DDS flag, no DDSD_HEIGHT: 0x{dds.dwFlags:X8}");
					dds.dwHeight = br.ReadUInt32();
					dds.dwWidth = br.ReadUInt32();
					dds.dwPitchOrLinearSize = br.ReadUInt32();
					dds.dwDepth = br.ReadUInt32();
					dds.dwMipMapCount = br.ReadUInt32();
					br.ReadBytes(4 * 11); // 11 dword reserved
					dds.ddpfPixelFormat.dwSize = br.ReadUInt32();
					if (dds.ddpfPixelFormat.dwSize != 0x00000020) throw new FormatException($"Invalid DDS pixel format size: 0x{dds.ddpfPixelFormat.dwSize:X8}");
					dds.ddpfPixelFormat.dwFlags = (DDPF)br.ReadUInt32();
					dds.ddpfPixelFormat.dwFourCC = br.ReadUInt32();
					return dds;
				}
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct DDS_PixelFormat {
			public uint dwSize; // Must be 0x00000020 (32)
			[MarshalAs(UnmanagedType.U4)]
			public DDPF dwFlags; // DDPF flags
			public uint dwFourCC; // Four-character code for format, must have flag DDPF.FourCC
			public uint dwRGBBitCount; // For RGB formats, this is the total number of bits in the format. dwFlags should include DDPF.RGB in this case.
			public uint dwRBitMask; // For RGB formats, this field contains the masks for the red channel
			public uint dwGBitMask; // For RGB formats, this field contains the masks for the green channel
			public uint dwBBitMask; // For RGB formats, this field contains the masks for the blue channel
			public uint dwRGBAlphaBitMask; // For RGB formats, this contains the mask for the alpha channel, if any. dwFlags should include DDPF.AlphaPixels in this case.

			[Conditional("DEBUG")]
			public void PrintDebug() {
				Log.Message($"[PF] ==============================");
				Log.Message($"[PF] dwFlags = {dwFlags} (0x{(uint)dwFlags:X8})");
				Log.Message($"[PF] dwFlags = {Encoding.ASCII.GetString(BitConverter.GetBytes(dwFourCC))} (0x{dwFourCC:X8})");
				Log.Message($"[PF] dwRGBBitCount = {dwRGBBitCount} (0x({dwRGBBitCount:X8})");
				Log.Message($"[PF] dwRBitMask = 0x{dwRBitMask:X8}");
				Log.Message($"[PF] dwGBitMask = 0x{dwGBitMask:X8}");
				Log.Message($"[PF] dwBBitMask = 0x{dwBBitMask:X8}");
				Log.Message($"[PF] dwRGBAlphaBitMask = 0x{dwRGBAlphaBitMask:X8}");
				Log.Message($"[PF] ==============================");
			}
		}
		#endregion

		public HeaderDDS Header { get; }
		public byte[] DXT { get; }
		public TextureFormat Format { get; }
		public int MipMapCount { get; }
		public int Width => (int)Header.dwWidth;
		public int Height => (int)Header.dwHeight;

		private static readonly TextureFormat[] CompressedFormats = new TextureFormat[] {
			TextureFormat.DXT1,
			TextureFormat.DXT5,
			TextureFormat.BC4,
			TextureFormat.BC5,
			TextureFormat.BC6H,
			TextureFormat.BC7
		};

		public DDS(byte[] data) {
			Header = HeaderDDS.Parse(data);

			TextureFormat? fmt = ParseTextureFormat(Header.ddpfPixelFormat);
			if(!fmt.HasValue) {
				Header.ddpfPixelFormat.PrintDebug();
				throw new FormatException($"Unknown DDS pixel format");
			}
			Format = fmt.Value;

			if (CompressedFormats.Contains(Format)) {
				if (Header.dwWidth % 4 != 0 || Header.dwHeight % 4 != 0) {
					throw new FormatException($"DDS format requires dimensions to be divisable by 4: {Header.dwWidth}x{Header.dwHeight}");
				}
			}

			// DDSD_LINEARSIZE is required for compressed formats and DXTn are all compressed
			if (!Header.dwFlags.HasFlag(DDSD.LinearSize))
				throw new FormatException($"Linear size flag not set for a compressed format (0x{(uint)Header.dwFlags:X8})");
			// DDSD_PITCH is the opposite of DDSD_LINEARSIZE, so it must not be set for compressed DXTn formats
			if (Header.dwFlags.HasFlag(DDSD.Pitch))
				throw new FormatException($"Pitch flag set for a compressed format (0x{(uint)Header.dwFlags:X8})");

			// Pixel data size should be equal to dwPitchOrLinearSize since DDSD_LINEARSIZE is required for DXT, but I can't bring myself to trust it.
			DXT = new byte[data.Length - Header.DataOffset];
			Buffer.BlockCopy(data, (int)Header.DataOffset, DXT, 0, data.Length - (int)Header.DataOffset);

			// Maybe this is important, I dunno.
			MipMapCount = Header.dwFlags.HasFlag(DDSD.MipmapCount) && Header.dwMipMapCount > 1 ? (int)Header.dwMipMapCount : 1;
		}

		// Took those out of https://learn.microsoft.com/en-us/windows/win32/direct3ddds/dx-graphics-dds-pguide
		private static readonly uint fourccDXT1 = MakeFourCC("DXT1");
		private static readonly uint fourccDXT5 = MakeFourCC("DXT5");
		private static readonly uint fourccYUY2 = MakeFourCC("YUY2");
		private static readonly uint fourccBC4 = MakeFourCC("BC4U"); // Or is it BC4S? I've no idea.
		private static readonly uint fourccBC5 = MakeFourCC("ATI2"); // Or is it BC5S? I've no idea either.
		private static uint MakeFourCC(string s) {
			byte[] bytes = Encoding.ASCII.GetBytes(s);
			return (
				(uint)(bytes[0] << 0) |
				(uint)(bytes[1] << 8) |
				(uint)(bytes[2] << 16) |
				(uint)(bytes[3] << 24)
			);
		}

		static DDS() {
			/*
			#if DEBUG
			Log.Message($"[4CC] DXT1 = {Encoding.ASCII.GetString(BitConverter.GetBytes(fourccDXT1))} (0x{fourccDXT1:X8})");
			Log.Message($"[4CC] DXT5 = {Encoding.ASCII.GetString(BitConverter.GetBytes(fourccDXT5))} (0x{fourccDXT5:X8})");
			Log.Message($"[4CC] YUY2 = {Encoding.ASCII.GetString(BitConverter.GetBytes(fourccYUY2))} (0x{fourccYUY2:X8})");
			Log.Message($"[4CC] BC4U = {Encoding.ASCII.GetString(BitConverter.GetBytes(fourccBC4))} (0x{fourccBC4:X8})");
			Log.Message($"[4CC] ATI2 = {Encoding.ASCII.GetString(BitConverter.GetBytes(fourccBC5))} (0x{fourccBC5:X8})");
			#endif
			*/
		}

		// Skipped formats because bit masks don't fit into DWORD (32bit):
		//   RGBAHalf   = R16 + G16 + B16 + A16 =  64 bits
		//   RGFloat    = R32 + G32             =  64 bits
		//   RGBAFloat  = R32 + G32 + B32 + A32 = 128 bits
		//   RGB48      = R16 + G16 + B16       =  48 bits
		//   RGBA64     = R16 + G16 + B16 + A16 =  64 bits
		// Skipped formats because I couldn't figure out their associated TextureFormat:
		//   BC4S, BC55
		// Skipped formats because I couldn't figure out their mask/4cc:
		//   RGB9e5Float, BC6H, BC7, DXT1Crunched, DXT5Crunched, ETC_RGB4,
		//   EAC_*, ETC2_*, ASTC_*
		// Skipped formats because my Unity doesn't have those TextureFormat enum values:
		//   R8_SIGNED, RG16_SIGNED, RGBA_SIGNED,
		//   R16_SIGNED, RG32_SIGNED, RGB48_SIGNED, RGBA64_SIGNED
		private static TextureFormat? ParseTextureFormat(DDS_PixelFormat dds) {
			if (dds.dwFlags.HasFlag(DDPF.FourCC)) {
				if (dds.dwFourCC == fourccDXT1) return TextureFormat.DXT1;
				if (dds.dwFourCC == fourccDXT5) return TextureFormat.DXT5;
				if (dds.dwFourCC == fourccBC4) return TextureFormat.BC4;
				if (dds.dwFourCC == fourccBC5) return TextureFormat.BC5;
				if (dds.dwFourCC == fourccYUY2) return TextureFormat.YUY2;
			} else if (dds.dwFlags.HasFlag(DDPF.RGB)) {
				switch (dds.dwRGBBitCount) {
					case 32:
						if (CompareMask(dds, 0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000)) return TextureFormat.RGBA32;
						if (CompareMask(dds, 0x0000FF00, 0x00FF0000, 0xFF000000, 0x000000FF)) return TextureFormat.ARGB32;
						if (CompareMask(dds, 0x00FF0000, 0x0000FF00, 0x000000FF, 0xFF000000)) return TextureFormat.BGRA32;
						if (CompareMask(dds, 0xFFFFFFFF, 0x00000000, 0x00000000)) return TextureFormat.RFloat;
						if (CompareMask(dds, 0x0000FFFF, 0xFFFF0000, 0x00000000)) return TextureFormat.RG32;
						if (CompareMask(dds, 0x0000FFFF, 0xFFFF0000, 0x00000000)) return TextureFormat.RGHalf;
						break;
					case 24:
						if (CompareMask(dds, 0x0000FF, 0x00FF00, 0xFF0000)) return TextureFormat.RGB24;
						break;
					case 16:
						if (CompareMask(dds, 0x00F0, 0x0F00, 0xF000, 0x000F)) return TextureFormat.ARGB4444;
						if (CompareMask(dds, 0x001F, 0x07E0, 0xF800)) return TextureFormat.RGB565;
						if (CompareMask(dds, 0xFFFF, 0x0000, 0x0000)) return TextureFormat.R16;
						if (CompareMask(dds, 0x000F, 0x00F0, 0x0F00, 0xF000)) return TextureFormat.RGBA4444;
						if (CompareMask(dds, 0xFFFF, 0x0000, 0x0000)) return TextureFormat.RHalf;
						if (CompareMask(dds, 0x00FF, 0xFF00, 0x0000)) return TextureFormat.RG16;
						break;
					case 8:
						if (CompareMask(dds, 0x00, 0x00, 0x00, 0xFF)) return TextureFormat.Alpha8;
						if (CompareMask(dds, 0xFF, 0x00, 0x00)) return TextureFormat.R8;
						break;
				}
			}
			// Please don't make me go into DX10 extension...
			return null;
		}

		private static bool CompareMask(DDS_PixelFormat dds, uint r, uint g, uint b) {
			if (dds.dwFlags.HasFlag(DDPF.AlphaPixels)) return false;
			return dds.dwRBitMask == r && dds.dwGBitMask == g && dds.dwBBitMask == b;
		}
		private static bool CompareMask(DDS_PixelFormat dds, uint r, uint g, uint b, uint a) {
			if (!dds.dwFlags.HasFlag(DDPF.AlphaPixels)) return false;
			return dds.dwRBitMask == r && dds.dwGBitMask == g && dds.dwBBitMask == b && dds.dwRGBAlphaBitMask == a;
		}

		public Texture2D CreateTexture() {
			Texture2D tex = new Texture2D(Width, Height, Format, MipMapCount, false);
			tex.LoadRawTextureData(DXT);
			return tex;
		}
	}
}
