using OsuParsers.Beatmaps.Objects;
using System;
using System.Collections.Generic;
using System.Text;

namespace DropletDerandomizer.Osu
{
    public class SliderInfo
    {
        public Slider BaseSlider { get; set; }
        public List<NestedObjectInfo> NestedObjects { get; set; }

        public SliderInfo(Slider slider)
        {
            BaseSlider = slider;
            NestedObjects = new List<NestedObjectInfo>();
        }
    }
}
