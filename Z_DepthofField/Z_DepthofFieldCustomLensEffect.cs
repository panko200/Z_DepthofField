using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Vortice;
using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;
using static Z_DepthofField.Z_DepthofFieldEffect;

namespace Z_DepthofField
{
    internal class Z_DepthofFieldCustomLensEffect : D2D1CustomShaderEffectBase
    {
        public float Radius { set => SetValue(0, value); }
        public float Brightness { set => SetValue(1, value); }
        public float EdgeStrength { set => SetValue(2, value); }
        public float Quality { set => SetValue(3, value); }
        public bool FixSize { set => SetValue(4, value); }

        public Z_DepthofFieldCustomLensEffect(IGraphicsDevicesAndContext devices) : base(Create<EffectImpl>(devices)) { }

        [CustomEffect(1)]
        private class EffectImpl : D2D1CustomShaderEffectImplBase<EffectImpl>
        {
            [StructLayout(LayoutKind.Sequential)]
            struct ConstantBuffer { public float Radius; public float Brightness; public float EdgeStrength; public float Quality; }
            ConstantBuffer constants;
            bool fixSize;

            public EffectImpl() : base(LoadShader()) { }

            protected override void UpdateConstants() => drawInformation?.SetPixelShaderConstantBuffer(constants);

            public override void MapInputRectsToOutputRect(RawRect[] inputRects, RawRect[] inputOpaqueSubRects, out RawRect outputRect, out RawRect outputOpaqueSubRect)
            {
                if (inputRects.Length > 0)
                {
                    // サイズ固定がONなら枠を広げない。OFFならRadius分広げる。
                    int range = fixSize ? 0 : (int)Math.Ceiling(constants.Radius);
                    outputRect = new RawRect(inputRects[0].Left - range, inputRects[0].Top - range, inputRects[0].Right + range, inputRects[0].Bottom + range);
                }
                else { outputRect = default; }
                outputOpaqueSubRect = default;
            }

            public override void MapOutputRectToInputRects(RawRect outputRect, RawRect[] inputRects)
            {
                if (inputRects.Length > 0)
                {
                    int range = (int)Math.Ceiling(constants.Radius);
                    inputRects[0] = new RawRect(outputRect.Left - range, outputRect.Top - range, outputRect.Right + range, outputRect.Bottom + range);
                }
            }

            private static byte[] LoadShader()
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var resourceName = "Z_DepthofField.Shaders.Z_DepthofFieldLensShader.cso";
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) return Array.Empty<byte>();
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                return ms.ToArray();
            }

            [CustomEffectProperty(PropertyType.Float, 0)] public float Radius { get => constants.Radius; set { constants.Radius = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, 1)] public float Brightness { get => constants.Brightness; set { constants.Brightness = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, 2)] public float EdgeStrength { get => constants.EdgeStrength; set { constants.EdgeStrength = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, 3)] public float Quality { get => constants.Quality; set { constants.Quality = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Bool, 4)] public bool FixSize { get => fixSize; set { fixSize = value; } }
        }
    }
}
