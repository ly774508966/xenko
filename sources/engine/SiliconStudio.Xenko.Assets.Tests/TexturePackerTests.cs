﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using NUnit.Framework;

using SiliconStudio.Assets;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Assets.Textures.Packing;
using SiliconStudio.Xenko.Engine;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.TextureConverter;

namespace SiliconStudio.Xenko.Assets.Tests
{
    [TestFixture]
    public class TexturePackerTests
    {
        private const string TexturePackerFolder = "SiliconStudio.Xenko.Assets.Tests/" + "TexturePacking/";
        private const string ImageOutputPath = TexturePackerFolder+"OutputImages/";
        private const string ImageInputPath = TexturePackerFolder + "IntputImages/";
        private const string GoldImagePath = TexturePackerFolder + "GoldImages/";

        public static void LoadXenkoAssemblies()
        {
            RuntimeHelpers.RunModuleConstructor(typeof(Asset).Module.ModuleHandle);
        }

        [TestFixtureSetUp]
        public void InitializeTest()
        {
            LoadXenkoAssemblies();
            Game.InitializeAssetDatabase();
        }

        [Test]
        public void TestMaxRectsPackWithoutRotation()
        {
            var maxRectPacker = new MaxRectanglesBinPack();
            maxRectPacker.Initialize(100, 100, false);

            // This data set remain only 1 rectangle that cant be packed
            var elementToPack = new List<AtlasTextureElement>
            {
                CreateElement(null, 80, 100),
                CreateElement(null, 100, 20),
            };

            maxRectPacker.PackRectangles(elementToPack);

            Assert.AreEqual(1, elementToPack.Count);
            Assert.AreEqual(1, maxRectPacker.PackedElements.Count);
        }

        [Test]
        public void TestMaxRectsPackWithRotation()
        {
            var maxRectPacker = new MaxRectanglesBinPack();
            maxRectPacker.Initialize(100, 100, true);

            // This data set remain only 1 rectangle that cant be packed
            var packRectangles = new List<AtlasTextureElement>
            {
                CreateElement("A", 80, 100),
                CreateElement("B", 100, 20),
            };

            maxRectPacker.PackRectangles(packRectangles);

            Assert.AreEqual(0, packRectangles.Count);
            Assert.AreEqual(2, maxRectPacker.PackedElements.Count);
            Assert.IsTrue(maxRectPacker.PackedElements.Find(e => e.Name == "B").DestinationRegion.IsRotated);
        }

        /// <summary>
        /// Test packing 7 rectangles
        /// </summary>
        [Test]
        public void TestMaxRectsPackArbitaryRectangles()
        {
            var maxRectPacker = new MaxRectanglesBinPack();
            maxRectPacker.Initialize(100, 100, true);

            // This data set remain only 1 rectangle that cant be packed
            var packRectangles = new List<AtlasTextureElement>
            {
                CreateElement(null, 55, 70),
                CreateElement(null, 55, 30),
                CreateElement(null, 25, 30),
                CreateElement(null, 20, 30),
                CreateElement(null, 45, 30),
                CreateElement(null, 25, 40),
                CreateElement(null, 20, 40),
            };

            maxRectPacker.PackRectangles(packRectangles);

            Assert.AreEqual(1, packRectangles.Count);
            Assert.AreEqual(6, maxRectPacker.PackedElements.Count);
        }

        [Test]
        public void TestTexturePackerFitAllElements()
        {
            var textureElements = CreateFakeTextureElements();

            var texturePacker = new TexturePacker
            {
                AllowMultipack = false,
                AllowRotation = true,
                AllowNonPowerOfTwo = true,
                MaxHeight = 2000,
                MaxWidth = 2000
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.IsTrue(canPackAllTextures);
        }

        public List<AtlasTextureElement> CreateFakeTextureElements()
        {
            return new List<AtlasTextureElement>
            {
                CreateElement("A", 100, 200),
                CreateElement("B", 400, 300),
            };
        }

        [Test]
        public void TestTexturePackerEmptyList()
        {
            var textureElements = new List<AtlasTextureElement>();

            var texturePacker = new TexturePacker
            {
                AllowMultipack = true,
                AllowRotation = true,
                MaxHeight = 300,
                MaxWidth = 300,
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.AreEqual(0, textureElements.Count);
            Assert.AreEqual(0, texturePacker.AtlasTextureLayouts.Count);
            Assert.IsTrue(canPackAllTextures);
        }

        [Test]
        public void TestRotationElement1()
        {
            var textureElements = new List<AtlasTextureElement>
            {
                CreateElementFromFile("imageRotated0", 0, TextureAddressMode.Clamp, TextureAddressMode.Clamp)
            };
            textureElements[0].SourceRegion.IsRotated = true;

            var texturePacker = new TexturePacker
            {
                AllowMultipack = true,
                AllowRotation = true,
                MaxWidth = 128,
                MaxHeight = 256,
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.IsTrue(canPackAllTextures);
            Assert.AreEqual(1, texturePacker.AtlasTextureLayouts.Count);

            var atlasTexture = AtlasTextureFactory.CreateTextureAtlas(texturePacker.AtlasTextureLayouts[0], false);

            SaveAndCompareTexture(atlasTexture, "TestRotationElement1");
        }

        [Test]
        public void TestRotationElement2()
        {
            var textureElements = new List<AtlasTextureElement>
            {
                CreateElementFromFile("imageRotated1", 0, TextureAddressMode.Clamp, TextureAddressMode.Clamp)
            };

            var texturePacker = new TexturePacker
            {
                AllowMultipack = true,
                AllowRotation = true,
                MaxWidth = 256,
                MaxHeight = 128,
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.IsTrue(canPackAllTextures);
            Assert.AreEqual(1, texturePacker.AtlasTextureLayouts.Count);

            var atlasTexture = AtlasTextureFactory.CreateTextureAtlas(texturePacker.AtlasTextureLayouts[0], false);

            SaveAndCompareTexture(atlasTexture, "TestRotationElement2");
        }

        [Test]
        public void TestTexturePackerWithMultiPack()
        {
            var textureElements = CreateFakeTextureElements();

            var texturePacker = new TexturePacker
            {
                AllowMultipack = true,
                AllowRotation = true,
                MaxHeight = 300,
                MaxWidth = 300,
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.AreEqual(2, textureElements.Count);
            Assert.AreEqual(0, texturePacker.AtlasTextureLayouts.Count);
            Assert.IsFalse(canPackAllTextures);

            texturePacker.Reset();
            texturePacker.MaxWidth = 1500;
            texturePacker.MaxHeight = 800;

            canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.IsTrue(canPackAllTextures);
            Assert.AreEqual(1, texturePacker.AtlasTextureLayouts.Count);
            Assert.AreEqual(textureElements.Count, texturePacker.AtlasTextureLayouts[0].Textures.Count);

            Assert.IsTrue(MathUtil.IsPow2(texturePacker.AtlasTextureLayouts[0].Width));
            Assert.IsTrue(MathUtil.IsPow2(texturePacker.AtlasTextureLayouts[0].Height));
        }

        [Test]
        public void TestTexturePackerWithBorder()
        {
            var textureAtlases = new List<AtlasTextureLayout>();

            var textureElements = new List<AtlasTextureElement>
            {
                CreateElement("A", 100, 200, 2),
                CreateElement("B", 57, 22, 2),
            };

            var texturePacker = new TexturePacker
            {
                AllowMultipack = true,
                AllowRotation = true,
                MaxHeight = 512,
                MaxWidth = 512
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);
            textureAtlases.AddRange(texturePacker.AtlasTextureLayouts);

            Assert.IsTrue(canPackAllTextures);
            Assert.AreEqual(2, textureElements.Count);
            Assert.AreEqual(1, textureAtlases.Count);

            Assert.IsTrue(MathUtil.IsPow2(textureAtlases[0].Width));
            Assert.IsTrue(MathUtil.IsPow2(textureAtlases[0].Height));

            // Test if border is applied in width and height
            var textureA = textureAtlases[0].Textures.Find(rectangle => rectangle.Name == "A");
            var textureB = textureAtlases[0].Textures.Find(rectangle => rectangle.Name == "B");

            Assert.AreEqual(textureA.SourceRegion.Width + 2 * textureA.BorderSize, textureA.DestinationRegion.Width);
            Assert.AreEqual(textureA.SourceRegion.Height + 2 * textureA.BorderSize, textureA.DestinationRegion.Height);

            Assert.AreEqual(textureB.SourceRegion.Width + 2 * textureB.BorderSize,
                (!textureB.DestinationRegion.IsRotated) ? textureB.DestinationRegion.Width : textureB.DestinationRegion.Height);
            Assert.AreEqual(textureB.SourceRegion.Height + 2 * textureB.BorderSize,
                (!textureB.DestinationRegion.IsRotated) ? textureB.DestinationRegion.Height : textureB.DestinationRegion.Width);
        }

        private AtlasTextureElement CreateElement(string name, int width, int height, int borderSize = 0, Color? color = null)
        {
            return CreateElement(name, width, height, borderSize, TextureAddressMode.Clamp, color);
        }

        private AtlasTextureElement CreateElement(string name, int width, int height, int borderSize, TextureAddressMode borderMode, Color? color = null, Color? borderColor = null)
        {
            Image image = null;
            if (color != null)
                image = CreateMockTexture(width, height, color.Value);

            return new AtlasTextureElement(name, image, new RotableRectangle(0, 0, width, height), borderSize, borderMode, borderMode, borderColor);
        }

        private AtlasTextureElement CreateElementFromFile(string name, int borderSize, TextureAddressMode borderModeU, TextureAddressMode borderModeV, RotableRectangle? imageRegion = null)
        {
            using (var texTool = new TextureTool())
            {
                var image = LoadImage(texTool, new UFile(ImageInputPath + "/" + name + ".png"));
                var region = imageRegion ?? new RotableRectangle(0, 0, image.Description.Width, image.Description.Height);

                return new AtlasTextureElement(name, image, region, borderSize, borderModeU, borderModeV, Color.SteelBlue);
            }
        }

        private Image CreateMockTexture(int width, int height, Color color)
        {
            var texture = Image.New2D(width, height, 1, PixelFormat.R8G8B8A8_UNorm);

            unsafe
            {
                var ptr = (Color*)texture.DataPointer;

                // Fill in mock data
                for (var y = 0; y < height; ++y)
                    for (var x = 0; x < width; ++x)
                    {
                        ptr[y * width + x] = y < height / 2 ? color : Color.White;
                    }
            }

            return texture;
        }

        [Test]
        public void TestTextureAtlasFactory()
        {
            var textureElements = new List<AtlasTextureElement>
            {
                CreateElement("A", 100, 200, 0, Color.MediumPurple),
            };

            var texturePacker = new TexturePacker
            {
                AllowMultipack = false,
                AllowRotation = true,
                MaxHeight = 2000,
                MaxWidth = 2000
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.IsTrue(canPackAllTextures);

            // Obtain texture atlases
            var textureAtlases = texturePacker.AtlasTextureLayouts;

            Assert.AreEqual(1, textureAtlases.Count);
            Assert.IsTrue(MathUtil.IsPow2(textureAtlases[0].Width));
            Assert.IsTrue(MathUtil.IsPow2(textureAtlases[0].Height));

            // Create atlas texture
            var atlasTexture = AtlasTextureFactory.CreateTextureAtlas(textureAtlases[0], false);

            Assert.AreEqual(textureAtlases[0].Width, atlasTexture.Description.Width);
            Assert.AreEqual(textureAtlases[0].Height, atlasTexture.Description.Height);

            SaveAndCompareTexture(atlasTexture, "TestTextureAtlasFactory");

            textureElements[0].Texture.Dispose();
            atlasTexture.Dispose();
        }

        [Test]
        public void TestTextureAtlasFactoryRotation()
        {
            var textureElements = new List<AtlasTextureElement>
            {
                CreateElementFromFile("image9", 25, TextureAddressMode.Clamp, TextureAddressMode.Clamp),
                CreateElementFromFile("image10", 25, TextureAddressMode.Mirror, TextureAddressMode.Mirror),
            };

            var texturePacker = new TexturePacker
            {
                AllowMultipack = false,
                AllowRotation = true,
                AllowNonPowerOfTwo = true,
                MaxWidth = 306,
                MaxHeight = 356,
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.IsTrue(canPackAllTextures);

            // Obtain texture atlases
            var textureAtlases = texturePacker.AtlasTextureLayouts;

            Assert.AreEqual(1, textureAtlases.Count);
            Assert.AreEqual(texturePacker.MaxWidth, textureAtlases[0].Width);
            Assert.AreEqual(texturePacker.MaxHeight, textureAtlases[0].Height);

            // Create atlas texture
            var atlasTexture = AtlasTextureFactory.CreateTextureAtlas(textureAtlases[0], false);

            Assert.AreEqual(textureAtlases[0].Width, atlasTexture.Description.Width);
            Assert.AreEqual(textureAtlases[0].Height, atlasTexture.Description.Height);

            SaveAndCompareTexture(atlasTexture, "TestTextureAtlasFactoryRotation");

            textureElements[0].Texture.Dispose();
            atlasTexture.Dispose();
        }

        [Test]
        public void TestTextureAtlasFactoryRotation2()
        {
            var textureElements = new List<AtlasTextureElement>
            {
                CreateElementFromFile("image9", 25, TextureAddressMode.Clamp, TextureAddressMode.Clamp),
                CreateElementFromFile("image10", 25, TextureAddressMode.Mirror, TextureAddressMode.Mirror),
            };

            var texturePacker = new TexturePacker
            {
                AllowMultipack = false,
                AllowRotation = true,
                AllowNonPowerOfTwo = true,
                MaxWidth = 356,
                MaxHeight = 306,
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.IsTrue(canPackAllTextures);

            // Obtain texture atlases
            var textureAtlases = texturePacker.AtlasTextureLayouts;

            Assert.AreEqual(1, textureAtlases.Count);
            Assert.AreEqual(texturePacker.MaxWidth, textureAtlases[0].Width);
            Assert.AreEqual(texturePacker.MaxHeight, textureAtlases[0].Height);

            // Create atlas texture
            var atlasTexture = AtlasTextureFactory.CreateTextureAtlas(textureAtlases[0], false);

            Assert.AreEqual(textureAtlases[0].Width, atlasTexture.Description.Width);
            Assert.AreEqual(textureAtlases[0].Height, atlasTexture.Description.Height);

            SaveAndCompareTexture(atlasTexture, "TestTextureAtlasFactoryRotation2");

            textureElements[0].Texture.Dispose();
            atlasTexture.Dispose();
        }

        [Test]
        public void TestRegionOutOfTexture()
        {
            var textureElements = new List<AtlasTextureElement>
            {
                CreateElementFromFile("image9", 10, TextureAddressMode.Mirror, TextureAddressMode.Clamp, new RotableRectangle(-100, 30, 400, 250)),
                CreateElementFromFile("image10", 10, TextureAddressMode.Wrap, TextureAddressMode.Border, new RotableRectangle(-50, -30, 300, 400, true)),
            };

            var texturePacker = new TexturePacker
            {
                AllowMultipack = false,
                AllowRotation = true,
                AllowNonPowerOfTwo = true,
                MaxWidth = 1024,
                MaxHeight = 1024,
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.IsTrue(canPackAllTextures);

            // Obtain texture atlases
            var textureAtlases = texturePacker.AtlasTextureLayouts;

            Assert.AreEqual(1, textureAtlases.Count);

            // Create atlas texture
            var atlasTexture = AtlasTextureFactory.CreateTextureAtlas(textureAtlases[0], false);

            Assert.AreEqual(textureAtlases[0].Width, atlasTexture.Description.Width);
            Assert.AreEqual(textureAtlases[0].Height, atlasTexture.Description.Height);

            SaveAndCompareTexture(atlasTexture, "TestRegionOutOfTexture");

            textureElements[0].Texture.Dispose();
            atlasTexture.Dispose();
        }

        [Test]
        public void TestTextureAtlasFactoryImageParts()
        {
            var textureElements = new List<AtlasTextureElement>
            {
                CreateElementFromFile("imagePart0", 26, TextureAddressMode.Border, TextureAddressMode.Mirror, new RotableRectangle(0, 0, 128, 128)),
                CreateElementFromFile("imagePart0", 26, TextureAddressMode.Clamp, TextureAddressMode.Clamp, new RotableRectangle(128, 128, 128, 128)),
                CreateElementFromFile("imagePart0", 26, TextureAddressMode.MirrorOnce, TextureAddressMode.Wrap, new RotableRectangle(128, 0, 128, 128)),
                CreateElementFromFile("imagePart1", 26, TextureAddressMode.Clamp, TextureAddressMode.Mirror, new RotableRectangle(376, 0, 127, 256)),
                CreateElementFromFile("imagePart1", 26, TextureAddressMode.Mirror, TextureAddressMode.Clamp, new RotableRectangle(10, 10, 254, 127)),
                CreateElement("empty", 0, 0, 26),
                CreateElementFromFile("imagePart2", 26, TextureAddressMode.Clamp, TextureAddressMode.Clamp, new RotableRectangle(0, 0, 128, 64)),
            };

            var texturePacker = new TexturePacker
            {
                AllowMultipack = false,
                AllowRotation = true,
                MaxWidth = 2048,
                MaxHeight = 2048,
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.IsTrue(canPackAllTextures);

            // Obtain texture atlases
            var textureAtlases = texturePacker.AtlasTextureLayouts;

            Assert.AreEqual(1, textureAtlases.Count);
            Assert.AreEqual(texturePacker.MaxWidth/2, textureAtlases[0].Width);
            Assert.AreEqual(texturePacker.MaxHeight/4, textureAtlases[0].Height);

            // Create atlas texture
            var atlasTexture = AtlasTextureFactory.CreateTextureAtlas(textureAtlases[0], false);

            Assert.AreEqual(textureAtlases[0].Width, atlasTexture.Description.Width);
            Assert.AreEqual(textureAtlases[0].Height, atlasTexture.Description.Height);

            SaveAndCompareTexture(atlasTexture, "TestTextureAtlasFactoryImageParts");

            textureElements[0].Texture.Dispose();
            atlasTexture.Dispose();
        }

        private void SaveAndCompareTexture(Image outputImage, string fileName, ImageFileType extension = ImageFileType.Png)
        {
            // save
            Directory.CreateDirectory(ImageOutputPath);
            outputImage.Save(new FileStream(ImageOutputPath + fileName + extension.ToFileExtension(), FileMode.Create), extension); 

            // Compare
            using(var texTool = new TextureTool())
            {
                var referenceImage = LoadImage(texTool, new UFile(GoldImagePath + "/" + fileName + extension.ToFileExtension()));
                Assert.IsTrue(CompareImages(outputImage, referenceImage), "The texture outputted differs from the gold image.");
            }
        }

        // Note: this comparison function is not very robust and might have to be improved at some point (does not take in account RowPitch, etc...)
        private bool CompareImages(Image outputImage, Image referenceImage)
        {
            if (outputImage.Description != referenceImage.Description)
                return false;
            
            unsafe
            {
                var ptr1 = (Color*)outputImage.DataPointer;
                var ptr2 = (Color*)referenceImage.DataPointer;

                // Fill in mock data
                for (var i = 0; i < outputImage.Description.Height * outputImage.Description.Width; ++i)
                {
                    if (*ptr1 != *ptr2)
                        return false;

                    ++ptr1;
                    ++ptr2;
                }
            }

            return true;
        }

        [Test]
        public void TestNullSizeTexture()
        {
            var textureElements = new List<AtlasTextureElement> { CreateElement("A", 0, 0) };

            var texturePacker = new TexturePacker
            {
                AllowMultipack = false,
                AllowRotation = true,
                MaxHeight = 2000,
                MaxWidth = 2000
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.IsTrue(canPackAllTextures);

            // Obtain texture atlases
            var textureAtlases = texturePacker.AtlasTextureLayouts;

            Assert.AreEqual(0, textureAtlases.Count);
        }

        [Test]
        public void TestNullSizeElements()
        {
            var textureElements = new List<AtlasTextureElement>
            {
                CreateElement("A", 10, 10, 5),
                CreateElement("B", 11, 0, 6),
                CreateElement("C", 12, 13, 7),
                CreateElement("D", 0, 14, 8),
                CreateElement("E", 14, 15, 9),
                CreateElement("F", 0, 0, 10),
                CreateElement("G", 16, 17, 11),
            };

            var texturePacker = new TexturePacker
            {
                AllowMultipack = false,
                AllowRotation = true,
                MaxHeight = 2000,
                MaxWidth = 2000
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.IsTrue(canPackAllTextures);

            // Obtain texture atlases
            var textureAtlases = texturePacker.AtlasTextureLayouts;

            Assert.AreEqual(1, textureAtlases.Count);
            Assert.AreEqual(4, textureAtlases[0].Textures.Count);
            Assert.IsNull(textureAtlases[0].Textures.Find(e => e.Name == "B"));
            Assert.IsNull(textureAtlases[0].Textures.Find(e => e.Name == "D"));
            Assert.IsNull(textureAtlases[0].Textures.Find(e => e.Name == "F"));
            Assert.IsNotNull(textureAtlases[0].Textures.Find(e => e.Name == "A"));
            Assert.IsNotNull(textureAtlases[0].Textures.Find(e => e.Name == "C"));
            Assert.IsNotNull(textureAtlases[0].Textures.Find(e => e.Name == "E"));
            Assert.IsNotNull(textureAtlases[0].Textures.Find(e => e.Name == "G"));
        }

        [Test]
        public void TestWrapBorderMode()
        {
            // Positive sets
            Assert.AreEqual(0, AtlasTextureFactory.GetSourceTextureCoordinate(0, 10, TextureAddressMode.Wrap));
            Assert.AreEqual(5, AtlasTextureFactory.GetSourceTextureCoordinate(5, 10, TextureAddressMode.Wrap));
            Assert.AreEqual(9, AtlasTextureFactory.GetSourceTextureCoordinate(9, 10, TextureAddressMode.Wrap));

            Assert.AreEqual(0, AtlasTextureFactory.GetSourceTextureCoordinate(10, 10, TextureAddressMode.Wrap));
            Assert.AreEqual(5, AtlasTextureFactory.GetSourceTextureCoordinate(15, 10, TextureAddressMode.Wrap));
            Assert.AreEqual(9, AtlasTextureFactory.GetSourceTextureCoordinate(19, 10, TextureAddressMode.Wrap));

            Assert.AreEqual(0, AtlasTextureFactory.GetSourceTextureCoordinate(20, 10, TextureAddressMode.Wrap));
            Assert.AreEqual(5, AtlasTextureFactory.GetSourceTextureCoordinate(25, 10, TextureAddressMode.Wrap));
            Assert.AreEqual(9, AtlasTextureFactory.GetSourceTextureCoordinate(29, 10, TextureAddressMode.Wrap));

            // Negative sets
            Assert.AreEqual(6, AtlasTextureFactory.GetSourceTextureCoordinate(-4, 10, TextureAddressMode.Wrap));
            Assert.AreEqual(1, AtlasTextureFactory.GetSourceTextureCoordinate(-9, 10, TextureAddressMode.Wrap));

            Assert.AreEqual(0, AtlasTextureFactory.GetSourceTextureCoordinate(-10, 10, TextureAddressMode.Wrap));
            Assert.AreEqual(6, AtlasTextureFactory.GetSourceTextureCoordinate(-14, 10, TextureAddressMode.Wrap));
            Assert.AreEqual(1, AtlasTextureFactory.GetSourceTextureCoordinate(-19, 10, TextureAddressMode.Wrap));

            Assert.AreEqual(0, AtlasTextureFactory.GetSourceTextureCoordinate(-20, 10, TextureAddressMode.Wrap));
            Assert.AreEqual(6, AtlasTextureFactory.GetSourceTextureCoordinate(-24, 10, TextureAddressMode.Wrap));
            Assert.AreEqual(1, AtlasTextureFactory.GetSourceTextureCoordinate(-29, 10, TextureAddressMode.Wrap));
        }

        [Test]
        public void TestClampBorderMode()
        {
            // Positive sets
            Assert.AreEqual(0, AtlasTextureFactory.GetSourceTextureCoordinate(0, 10, TextureAddressMode.Clamp));
            Assert.AreEqual(5, AtlasTextureFactory.GetSourceTextureCoordinate(5, 10, TextureAddressMode.Clamp));
            Assert.AreEqual(9, AtlasTextureFactory.GetSourceTextureCoordinate(9, 10, TextureAddressMode.Clamp));

            Assert.AreEqual(9, AtlasTextureFactory.GetSourceTextureCoordinate(10, 10, TextureAddressMode.Clamp));
            Assert.AreEqual(9, AtlasTextureFactory.GetSourceTextureCoordinate(15, 10, TextureAddressMode.Clamp));
            Assert.AreEqual(9, AtlasTextureFactory.GetSourceTextureCoordinate(19, 10, TextureAddressMode.Clamp));

            Assert.AreEqual(9, AtlasTextureFactory.GetSourceTextureCoordinate(20, 10, TextureAddressMode.Clamp));
            Assert.AreEqual(9, AtlasTextureFactory.GetSourceTextureCoordinate(25, 10, TextureAddressMode.Clamp));
            Assert.AreEqual(9, AtlasTextureFactory.GetSourceTextureCoordinate(29, 10, TextureAddressMode.Clamp));

            // Negative sets
            Assert.AreEqual(0, AtlasTextureFactory.GetSourceTextureCoordinate(-4, 10, TextureAddressMode.Clamp));
            Assert.AreEqual(0, AtlasTextureFactory.GetSourceTextureCoordinate(-9, 10, TextureAddressMode.Clamp));

            Assert.AreEqual(0, AtlasTextureFactory.GetSourceTextureCoordinate(-10, 10, TextureAddressMode.Clamp));
            Assert.AreEqual(0, AtlasTextureFactory.GetSourceTextureCoordinate(-14, 10, TextureAddressMode.Clamp));
            Assert.AreEqual(0, AtlasTextureFactory.GetSourceTextureCoordinate(-19, 10, TextureAddressMode.Clamp));

            Assert.AreEqual(0, AtlasTextureFactory.GetSourceTextureCoordinate(-20, 10, TextureAddressMode.Clamp));
            Assert.AreEqual(0, AtlasTextureFactory.GetSourceTextureCoordinate(-24, 10, TextureAddressMode.Clamp));
            Assert.AreEqual(0, AtlasTextureFactory.GetSourceTextureCoordinate(-29, 10, TextureAddressMode.Clamp));
        }

        [Test]
        public void TestMirrorBorderMode()
        {
            // Positive sets
            Assert.AreEqual(0, AtlasTextureFactory.GetSourceTextureCoordinate(0, 10, TextureAddressMode.Mirror));
            Assert.AreEqual(5, AtlasTextureFactory.GetSourceTextureCoordinate(5, 10, TextureAddressMode.Mirror));
            Assert.AreEqual(9, AtlasTextureFactory.GetSourceTextureCoordinate(9, 10, TextureAddressMode.Mirror));

            Assert.AreEqual(9, AtlasTextureFactory.GetSourceTextureCoordinate(10, 10, TextureAddressMode.Mirror));
            Assert.AreEqual(8, AtlasTextureFactory.GetSourceTextureCoordinate(11, 10, TextureAddressMode.Mirror));
            Assert.AreEqual(7, AtlasTextureFactory.GetSourceTextureCoordinate(12, 10, TextureAddressMode.Mirror));

            Assert.AreEqual(9, AtlasTextureFactory.GetSourceTextureCoordinate(20, 10, TextureAddressMode.Mirror));
            Assert.AreEqual(8, AtlasTextureFactory.GetSourceTextureCoordinate(21, 10, TextureAddressMode.Mirror));

            // Negative Sets
            Assert.AreEqual(1, AtlasTextureFactory.GetSourceTextureCoordinate(-1, 10, TextureAddressMode.Mirror));
            Assert.AreEqual(2, AtlasTextureFactory.GetSourceTextureCoordinate(-2, 10, TextureAddressMode.Mirror));
            Assert.AreEqual(3, AtlasTextureFactory.GetSourceTextureCoordinate(-3, 10, TextureAddressMode.Mirror));

            Assert.AreEqual(9, AtlasTextureFactory.GetSourceTextureCoordinate(-9, 10, TextureAddressMode.Mirror));
            Assert.AreEqual(0, AtlasTextureFactory.GetSourceTextureCoordinate(-10, 10, TextureAddressMode.Mirror));
            Assert.AreEqual(1, AtlasTextureFactory.GetSourceTextureCoordinate(-11, 10, TextureAddressMode.Mirror));

            Assert.AreEqual(0, AtlasTextureFactory.GetSourceTextureCoordinate(-20, 10, TextureAddressMode.Mirror));
            Assert.AreEqual(1, AtlasTextureFactory.GetSourceTextureCoordinate(-21, 10, TextureAddressMode.Mirror));
        }

        [Test]
        public void TestMirrorOnceBorderMode()
        {
            // Positive sets
            Assert.AreEqual(0, AtlasTextureFactory.GetSourceTextureCoordinate(0, 10, TextureAddressMode.MirrorOnce));
            Assert.AreEqual(5, AtlasTextureFactory.GetSourceTextureCoordinate(5, 10, TextureAddressMode.MirrorOnce));
            Assert.AreEqual(9, AtlasTextureFactory.GetSourceTextureCoordinate(9, 10, TextureAddressMode.MirrorOnce));

            Assert.AreEqual(9, AtlasTextureFactory.GetSourceTextureCoordinate(10, 10, TextureAddressMode.MirrorOnce));
            Assert.AreEqual(9, AtlasTextureFactory.GetSourceTextureCoordinate(11, 10, TextureAddressMode.MirrorOnce));
            Assert.AreEqual(9, AtlasTextureFactory.GetSourceTextureCoordinate(12, 10, TextureAddressMode.MirrorOnce));

            Assert.AreEqual(9, AtlasTextureFactory.GetSourceTextureCoordinate(20, 10, TextureAddressMode.MirrorOnce));
            Assert.AreEqual(9, AtlasTextureFactory.GetSourceTextureCoordinate(21, 10, TextureAddressMode.MirrorOnce));

            // Negative Sets
            Assert.AreEqual(1, AtlasTextureFactory.GetSourceTextureCoordinate(-1, 10, TextureAddressMode.MirrorOnce));
            Assert.AreEqual(2, AtlasTextureFactory.GetSourceTextureCoordinate(-2, 10, TextureAddressMode.MirrorOnce));
            Assert.AreEqual(3, AtlasTextureFactory.GetSourceTextureCoordinate(-3, 10, TextureAddressMode.MirrorOnce));

            Assert.AreEqual(9, AtlasTextureFactory.GetSourceTextureCoordinate(-9, 10, TextureAddressMode.MirrorOnce));
            Assert.AreEqual(9, AtlasTextureFactory.GetSourceTextureCoordinate(-10, 10, TextureAddressMode.MirrorOnce));
            Assert.AreEqual(9, AtlasTextureFactory.GetSourceTextureCoordinate(-11, 10, TextureAddressMode.MirrorOnce));

            Assert.AreEqual(9, AtlasTextureFactory.GetSourceTextureCoordinate(-20, 10, TextureAddressMode.MirrorOnce));
            Assert.AreEqual(9, AtlasTextureFactory.GetSourceTextureCoordinate(-21, 10, TextureAddressMode.MirrorOnce));
        }

        [Test]
        public void TestImageCreationGetAndSet()
        {
            const int width = 256;
            const int height = 128;

            var source = Image.New2D(width, height, 1, PixelFormat.R8G8B8A8_UNorm);

            Assert.AreEqual(source.TotalSizeInBytes, PixelFormat.R8G8B8A8_UNorm.SizeInBytes() * width * height);
            Assert.AreEqual(source.PixelBuffer.Count, 1);

            Assert.AreEqual(1, source.Description.MipLevels);
            Assert.AreEqual(1, source.Description.ArraySize);

            Assert.AreEqual(width * height * 4,
                source.PixelBuffer[0].Width * source.PixelBuffer[0].Height * source.PixelBuffer[0].PixelSize);

            // Set Pixel
            var pixelBuffer = source.PixelBuffer[0];
            pixelBuffer.SetPixel(0, 0, (byte)255);

            // Get Pixel
            var fromPixels = pixelBuffer.GetPixels<byte>();
            Assert.AreEqual(fromPixels[0], 255);

            // Dispose images
            source.Dispose();
        }

        [Test]
        public void TestImageDataPointerManipulation()
        {
            const int width = 256;
            const int height = 128;

            var source = Image.New2D(width, height, 1, PixelFormat.R8G8B8A8_UNorm);

            Assert.AreEqual(source.TotalSizeInBytes, PixelFormat.R8G8B8A8_UNorm.SizeInBytes() * width * height);
            Assert.AreEqual(source.PixelBuffer.Count, 1);

            Assert.AreEqual(1, source.Description.MipLevels);
            Assert.AreEqual(1, source.Description.ArraySize);

            Assert.AreEqual(width * height * 4,
                source.PixelBuffer[0].Width * source.PixelBuffer[0].Height * source.PixelBuffer[0].PixelSize);

            unsafe
            {
                var ptr = (Color*)source.DataPointer;

                // Clean the data
                for (var i = 0; i < source.PixelBuffer[0].Height * source.PixelBuffer[0].Width; ++i)
                    ptr[i] = Color.Transparent;

                // Set a specific pixel to red
                ptr[0] = Color.Red;
            }

            var pixelBuffer = source.PixelBuffer[0];

            // Get Pixel
            var fromPixels = pixelBuffer.GetPixels<Color>();
            Assert.AreEqual(Color.Red, fromPixels[0]);

            // Dispose images
            source.Dispose();
        }

        [Test]
        public void TestCreateTextureAtlasToOutput()
        {
            var textureElements = new List<AtlasTextureElement>
            {
                CreateElement("MediumPurple", 130, 158, 10, TextureAddressMode.Border, Color.MediumPurple, Color.SteelBlue),
                CreateElement("Red", 127, 248, 10, TextureAddressMode.Border, Color.Red, Color.SteelBlue),
                CreateElement("Blue", 212, 153, 10, TextureAddressMode.Border, Color.Blue, Color.SteelBlue),
                CreateElement("Gold", 78, 100, 10, TextureAddressMode.Border, Color.Gold, Color.SteelBlue),
                CreateElement("RosyBrown", 78, 100, 10, TextureAddressMode.Border, Color.RosyBrown, Color.SteelBlue),
                CreateElement("SaddleBrown", 400, 100, 10, TextureAddressMode.Border, Color.SaddleBrown, Color.SteelBlue),
                CreateElement("Salmon", 400, 200, 10, TextureAddressMode.Border, Color.Salmon, Color.SteelBlue),
                CreateElement("PowderBlue", 190, 200, 10, TextureAddressMode.Border, Color.PowderBlue, Color.SteelBlue),
                CreateElement("Orange", 200, 230, 10, TextureAddressMode.Border, Color.Orange, Color.SteelBlue),
                CreateElement("Silver", 100, 170, 10, TextureAddressMode.Border, Color.Silver, Color.SteelBlue),
                CreateElement("SlateGray", 100, 170, 10, TextureAddressMode.Border, Color.SlateGray, Color.SteelBlue),
                CreateElement("Tan", 140, 110, 10, TextureAddressMode.Border, Color.Tan, Color.SteelBlue),
            };

            var texturePacker = new TexturePacker
            {
                AllowMultipack = false,
                AllowRotation = true,
                MaxHeight = 1024,
                MaxWidth = 1024
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.IsTrue(canPackAllTextures);

            // Obtain texture atlases
            var textureAtlases = texturePacker.AtlasTextureLayouts;

            Assert.AreEqual(1, textureAtlases.Count);

            if (!texturePacker.AllowNonPowerOfTwo)
            {
                Assert.IsTrue(MathUtil.IsPow2(textureAtlases[0].Width));
                Assert.IsTrue(MathUtil.IsPow2(textureAtlases[0].Height));
            }

            // Create atlas texture
            var atlasTexture = AtlasTextureFactory.CreateTextureAtlas(textureAtlases[0], false);
            SaveAndCompareTexture(atlasTexture, "TestCreateTextureAtlasToOutput");

            Assert.AreEqual(textureAtlases[0].Width, atlasTexture.Description.Width);
            Assert.AreEqual(textureAtlases[0].Height, atlasTexture.Description.Height);

            atlasTexture.Dispose();

            foreach (var texture in textureAtlases.SelectMany(textureAtlas => textureAtlas.Textures))
            {
                texture.Texture.Dispose();
            }
        }

        [Test]
        public void TestLoadImagesToCreateAtlas()
        {
            var textureElements = new List<AtlasTextureElement>();

            for (var i = 0; i < 8; ++i)
                textureElements.Add(CreateElementFromFile("image" + i, 100, TextureAddressMode.Wrap, TextureAddressMode.Border));

            for (var i = 0; i < 8; ++i)
                textureElements.Add(CreateElementFromFile("image" + i, 100, TextureAddressMode.Mirror, TextureAddressMode.Clamp));

            var texturePacker = new TexturePacker
            {
                AllowMultipack = false,
                AllowRotation = false,
                MaxHeight = 2048,
                MaxWidth = 2048
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.IsTrue(canPackAllTextures);

            // Obtain texture atlases
            var textureAtlases = texturePacker.AtlasTextureLayouts;

            // Create atlas texture
            var atlasTexture = AtlasTextureFactory.CreateTextureAtlas(textureAtlases[0], false);

            SaveAndCompareTexture(atlasTexture, "TestLoadImagesToCreateAtlas", ImageFileType.Dds);
            atlasTexture.Dispose();

            foreach (var texture in textureAtlases.SelectMany(textureAtlas => textureAtlas.Textures))
                texture.Texture.Dispose();
        }

        private Image LoadImage(TextureTool texTool, UFile sourcePath)
        {
            using (var texImage = texTool.Load(sourcePath, false))
            {
                // Decompresses the specified texImage
                texTool.Decompress(texImage, false);

                if (texImage.Format == PixelFormat.B8G8R8A8_UNorm)
                    texTool.SwitchChannel(texImage);

                return texTool.ConvertToXenkoImage(texImage);
            }
        }
    }
}
