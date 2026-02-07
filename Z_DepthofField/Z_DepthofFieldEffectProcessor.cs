using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Vortice.Direct2D1;
using Vortice.Direct2D1.Effects;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;
using static Z_DepthofField.Z_DepthofFieldEffect;

namespace Z_DepthofField
{
    internal class Z_DepthofFieldEffectProcessor : IVideoEffectProcessor
    {
        private readonly IGraphicsDevicesAndContext _devices;
        private readonly Z_DepthofFieldEffect _item;
        private ID2D1Image? _input;

        private readonly GaussianBlur _gaussianBlur;
        private readonly Z_DepthofFieldCustomLensEffect _lensBlur;

        private readonly ID2D1Image _gaussianOutput;
        private readonly ID2D1Image _lensOutput;

        public Z_DepthofFieldEffectProcessor(IGraphicsDevicesAndContext devices, Z_DepthofFieldEffect item)
        {
            _devices = devices;
            _item = item;
            var dc = _devices.DeviceContext;

            _gaussianBlur = new GaussianBlur(dc);
            _gaussianOutput = _gaussianBlur.Output;

            _lensBlur = new Z_DepthofFieldCustomLensEffect(devices);
            _lensOutput = _lensBlur.Output;
        }

        public ID2D1Image Output => _item.BlurType == BlurType.Gaussian ? _gaussianOutput : _lensOutput;

        public DrawDescription Update(EffectDescription desc)
        {
            if (_input == null) return desc.DrawDescription;

            var frame = (long)desc.ItemPosition.Frame;
            var len = (long)desc.ItemDuration.Frame;
            var fps = desc.FPS;

            // --- 1. 距離計算（維持） ---
            if (!Matrix4x4.Invert(desc.DrawDescription.Camera, out Matrix4x4 invView)) invView = Matrix4x4.Identity;
            Vector3 worldEye = Vector3.Transform(new Vector3(0, 0, 1000), invView);
            Vector3 itemPosWorld = new Vector3((float)desc.DrawDescription.Draw.X, (float)desc.DrawDescription.Draw.Y, (float)desc.DrawDescription.Draw.Z);
            float distance = (_item.Mode == FocusMode.Spherical) ? Vector3.Distance(worldEye, itemPosWorld) : Math.Abs(Vector3.Dot(itemPosWorld - worldEye, Vector3.Normalize(new Vector3(-invView.M31, -invView.M32, -invView.M33))));

            // --- 2. ボケ量計算（維持） ---
            float focusDist = (float)_item.FocusDistance.GetValue(frame, len, fps);
            float focusRange = (float)_item.FocusRange.GetValue(frame, len, fps);
            float maxBlur = (float)_item.MaxBlur.GetValue(frame, len, fps);
            float blurAmount = 0;
            float nearBoundary = focusDist - (focusRange / 2f);
            float farBoundary = focusDist + (focusRange / 2f);
            if (distance < nearBoundary) blurAmount = (nearBoundary - distance) * (float)_item.NearBlurScale.GetValue(frame, len, fps);
            else if (distance > farBoundary) blurAmount = (distance - farBoundary) * (float)_item.FarBlurScale.GetValue(frame, len, fps);
            blurAmount = Math.Min(blurAmount, maxBlur);

            // --- 3. エフェクト適用（使い分け） ---
            if (_item.BlurType == BlurType.Gaussian)
            {
                _gaussianBlur.SetInput(0, _input, true);
                _gaussianBlur.StandardDeviation = blurAmount < 0.1f ? 0f : blurAmount;
                _gaussianBlur.BorderMode = _item.FixSize ? BorderMode.Hard : BorderMode.Soft;
                _gaussianBlur.Optimization = GaussianBlurOptimization.Quality;
            }
            else
            {
                _lensBlur.SetInput(0, _input, true);
                _lensBlur.Radius = blurAmount;
                _lensBlur.Brightness = (float)_item.BokehBrightness.GetValue(frame, len, fps) / 100f;
                _lensBlur.EdgeStrength = (float)_item.BokehEdge.GetValue(frame, len, fps);
                _lensBlur.Quality = (float)_item.BokehQuality.GetValue(frame, len, fps);
                _lensBlur.FixSize = _item.FixSize;
            }

            return desc.DrawDescription;
        }

        public void SetInput(ID2D1Image? input) => _input = input;
        public void ClearInput() => _input = null;
        public void Dispose()
        {
            _gaussianOutput.Dispose();
            _gaussianBlur.Dispose();
            _lensOutput.Dispose();
            _lensBlur.Dispose();
        }
    }
}
