using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Effects;

namespace Z_DepthofField
{

    public enum BlurType { [Display(Name = "ガウスぼかし")] Gaussian, [Display(Name = "レンズぼかし")] Lens }
    public enum FocusMode { [Display(Name = "直線距離 (球面)")] Spherical, [Display(Name = "奥行きのみ (平面)")] Planar }

    [VideoEffect("Z軸被写界深度", ["描画"], ["Z-DoF", "被写界深度", "ぼけ","ボケ"])]
    public class Z_DepthofFieldEffect : VideoEffectBase
    {
        public override string Label => "Z軸被写界深度";

        [Display(GroupName = "基本設定", Name = "ぼかしの種類", Description = "ガウスぼかしかレンズぼかしかを選べます。")]
        [EnumComboBox]
        public BlurType BlurType { get => blurType; set => Set(ref blurType, value); }
        private BlurType blurType = BlurType.Gaussian;

        [Display(GroupName = "基本設定", Name = "計算モード", Description = "カメラと物体の直線距離によってぼかしをかけるかを決めるか、\nカメラと物体との、カメラから見たZ軸距離によってぼかしを決めるか。")]
        [EnumComboBox]
        public FocusMode Mode { get => mode; set => Set(ref mode, value); }
        private FocusMode mode = FocusMode.Planar;

        [Display(GroupName = "基本設定", Name = "サイズ固定", Description = "ONにすると、ぼかしてもアイテムの大きさが変わりません（端が透けるのを防ぎます）。")]
        [ToggleSlider]
        public bool FixSize { get => fixSize; set => Set(ref fixSize, value); }
        private bool fixSize = false;

        [Display(GroupName = "ピント設定", Name = "ピント距離", Description = "カメラからの距離です。YMM4の標準では、アイテムとカメラの距離は1000あるので、\n1000を標準としています。")]
        [AnimationSlider("F0", "px", 0, 5000)]
        public Animation FocusDistance { get; } = new Animation(1000, 0, 100000);

        [Display(GroupName = "ピント設定", Name = "ピント範囲", Description = "この範囲内にあるアイテムはボケずにクッキリ表示されます。")]
        [AnimationSlider("F0", "px", 0, 1000)]
        public Animation FocusRange { get; } = new Animation(0, 0, 100000);

        [Display(GroupName = "ボケ倍率", Name = "前(手前)", Description = "設定距離よりも近いときの、距離に応じてボケる強さの係数です。")]
        [AnimationSlider("F3", "", 0, 0.1)]
        public Animation NearBlurScale { get; } = new Animation(0.01f, 0, 1.0);

        [Display(GroupName = "ボケ倍率", Name = "後(奥)", Description = "設定距離よりも遠いときの、距離に応じてボケる強さの係数です。")]
        [AnimationSlider("F3", "", 0, 0.1)]
        public Animation FarBlurScale { get; } = new Animation(0.01f, 0, 1.0);

        [Display(GroupName = "ボケ倍率", Name = "最大ボケ量", Description = "最大のボケ率です。これ以上はボケません。ボケボケ防止策")]
        [AnimationSlider("F0", "px", 0, 100)]
        public Animation MaxBlur { get; } = new Animation(20, 0, 500);

        // --- レンズぼかし専用パラメータ ---
        [Display(GroupName = "レンズぼかし詳細", Name = "明るさ", Description = "レンズぼかしされたところが明るくなります。")]
        [AnimationSlider("F1", "%", 0, 200)]
        public Animation BokehBrightness { get; } = new Animation(100, 0, 1000);

        [Display(GroupName = "レンズぼかし詳細", Name = "エッジ強度", Description = "エッジ強度らしい。")]
        [AnimationSlider("F1", "", 0, 10)]
        public Animation BokehEdge { get; } = new Animation(2, 0, 20);

        [Display(GroupName = "レンズぼかし詳細", Name = "品質(サンプル数)", Description = "数値が高ければ高いほど、ぼかしの品質がよくなります。")]
        [AnimationSlider("F0", "", 1, 100)]
        public Animation BokehQuality { get; } = new Animation(16, 1, 512);

        public override IEnumerable<string> CreateExoVideoFilters(int keyFrameIndex, ExoOutputDescription exoOutputDescription) => [];
        public override IVideoEffectProcessor CreateVideoEffect(IGraphicsDevicesAndContext devices) => new Z_DepthofFieldEffectProcessor(devices, this);

        protected override IEnumerable<IAnimatable> GetAnimatables() =>
            [FocusDistance, FocusRange, NearBlurScale, FarBlurScale, MaxBlur, BokehBrightness, BokehEdge, BokehQuality];
    }
}
