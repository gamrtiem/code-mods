using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using UnityEngine;
using Color = UnityEngine.Color;

namespace ProdzModpackUtils
{
    public class Utils
    {
        public static Color TRANSPARENT = new(0, 0, 0, 0);
        public static Sprite GetComposite(Texture2D bg, Texture2D fg)
        {
            Vector2 newSize = new(Mathf.Max(bg.width, fg.width), Mathf.Max(bg.height, fg.height));
            Vector2 offsetBG = new(Mathf.Floor((bg.width - newSize.x) / 2), Mathf.Floor((bg.width - newSize.y) / 2));
            Vector2 offsetFG = new(Mathf.Floor((fg.width - newSize.x) / 2), Mathf.Floor((fg.width - newSize.y) / 2));
            var tex = new Texture2D((int)newSize.x, (int)newSize.y);
            for (int x = 0; x < newSize.x; x++) for (int y = 0; y < newSize.y; y++) 
                tex.SetPixel(x, y, Over(Pixel(bg, x + offsetBG.x, y + offsetBG.y), Pixel(fg, x + offsetFG.x, y + offsetFG.y)));
            return Sprite.Create(tex, new(0, 0, newSize.x, newSize.y), new Vector2(0.5f, 0.5f), 3f);
        }
        public static Color Pixel(Texture2D img, float x, float y)
        {
            if (0 > x || x >= img.width || 0 > y || y >= img.height) return TRANSPARENT;
            return img.GetPixel((int)x, (int)y);
        }
        public static Color Over(Color bg, Color fg)
        {
            var a = bg.a * (1 - fg.a) + fg.a;
            return ((Vector4)fg * fg.a + (Vector4)bg * bg.a * (1 - fg.a)) / a;
        }
        public static Sprite Load(params string[] path)
        {
            var size = ImageHelper.GetDimensions(Path.Combine(path));
            var bytes = File.ReadAllBytes(Path.Combine(path));
            Texture2D texture = new(size.Width, size.Height, TextureFormat.RGB24, false) { filterMode = FilterMode.Trilinear };
            texture.LoadImage(bytes);
            return Sprite.Create(texture, new Rect(0, 0, size.Width, size.Height), new Vector2(0.5f, 0.5f), 3f);
        }
    }
    public static class ImageHelper // stackoverflow GO
    {
        const string errorMessage = "Could not recognize image format.";

        private static Dictionary<byte[], Func<BinaryReader, Size>> imageFormatDecoders = new Dictionary<byte[], Func<BinaryReader, Size>>()
        {
            { new byte[]{ 0x42, 0x4D }, DecodeBitmap},
            { new byte[]{ 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, DecodeGif },
            { new byte[]{ 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }, DecodeGif },
            { new byte[]{ 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, DecodePng },
            { new byte[]{ 0xff, 0xd8 }, DecodeJfif },
        };

        /// <summary>
        /// Gets the dimensions of an image.
        /// </summary>
        /// <param name="path">The path of the image to get the dimensions of.</param>
        /// <returns>The dimensions of the specified image.</returns>
        /// <exception cref="ArgumentException">The image was of an unrecognized format.</exception>
        public static Size GetDimensions(string path)
        {
            using (BinaryReader binaryReader = new BinaryReader(File.OpenRead(path)))
            {
                try
                {
                    return GetDimensions(binaryReader);
                }
                catch (ArgumentException e)
                {
                    if (e.Message.StartsWith(errorMessage))
                    {
                        throw new ArgumentException(errorMessage, "path", e);
                    }
                    else
                    {
                        throw e;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the dimensions of an image.
        /// </summary>
        /// <param name="path">The path of the image to get the dimensions of.</param>
        /// <returns>The dimensions of the specified image.</returns>
        /// <exception cref="ArgumentException">The image was of an unrecognized format.</exception>    
        public static Size GetDimensions(BinaryReader binaryReader)
        {
            int maxMagicBytesLength = imageFormatDecoders.Keys.OrderByDescending(x => x.Length).First().Length;

            byte[] magicBytes = new byte[maxMagicBytesLength];

            for (int i = 0; i < maxMagicBytesLength; i += 1)
            {
                magicBytes[i] = binaryReader.ReadByte();

                foreach (var kvPair in imageFormatDecoders)
                {
                    if (magicBytes.StartsWith(kvPair.Key))
                    {
                        return kvPair.Value(binaryReader);
                    }
                }
            }

            throw new ArgumentException(errorMessage, "binaryReader");
        }

        private static bool StartsWith(this byte[] thisBytes, byte[] thatBytes)
        {
            for (int i = 0; i < thatBytes.Length; i += 1)
            {
                if (thisBytes[i] != thatBytes[i])
                {
                    return false;
                }
            }
            return true;
        }

        private static short ReadLittleEndianInt16(this BinaryReader binaryReader)
        {
            byte[] bytes = new byte[sizeof(short)];
            for (int i = 0; i < sizeof(short); i += 1)
            {
                bytes[sizeof(short) - 1 - i] = binaryReader.ReadByte();
            }
            return BitConverter.ToInt16(bytes, 0);
        }

        private static int ReadLittleEndianInt32(this BinaryReader binaryReader)
        {
            byte[] bytes = new byte[sizeof(int)];
            for (int i = 0; i < sizeof(int); i += 1)
            {
                bytes[sizeof(int) - 1 - i] = binaryReader.ReadByte();
            }
            return BitConverter.ToInt32(bytes, 0);
        }

        private static Size DecodeBitmap(BinaryReader binaryReader)
        {
            binaryReader.ReadBytes(16);
            int width = binaryReader.ReadInt32();
            int height = binaryReader.ReadInt32();
            return new Size(width, height);
        }

        private static Size DecodeGif(BinaryReader binaryReader)
        {
            int width = binaryReader.ReadInt16();
            int height = binaryReader.ReadInt16();
            return new Size(width, height);
        }

        private static Size DecodePng(BinaryReader binaryReader)
        {
            binaryReader.ReadBytes(8);
            int width = binaryReader.ReadLittleEndianInt32();
            int height = binaryReader.ReadLittleEndianInt32();
            return new Size(width, height);
        }

        private static Size DecodeJfif(BinaryReader binaryReader)
        {
            while (binaryReader.ReadByte() == 0xff)
            {
                byte marker = binaryReader.ReadByte();
                short chunkLength = binaryReader.ReadLittleEndianInt16();

                if (marker == 0xc0)
                {
                    binaryReader.ReadByte();

                    int height = binaryReader.ReadLittleEndianInt16();
                    int width = binaryReader.ReadLittleEndianInt16();
                    return new Size(width, height);
                }

                binaryReader.ReadBytes(chunkLength - 2);
            }

            throw new ArgumentException(errorMessage);
        }
    }
}