using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using OsuParsers.Beatmaps;
using OsuParsers.Decoders;
using osu.Game.Rulesets.Catch.Beatmaps;
using osu.Game.Rulesets.Catch.Objects;
using OsuParsers.Beatmaps.Objects;
using System.Linq;
using DropletDerandomizer.Exstensions;
using System.Numerics;
using System.Data;

namespace DropletDerandomizer.Osu
{
    public static partial class BeatmapOperator
    {
        public static Beatmap DerandomizeDroplets(string path)
        {
            // used to determine droplets' positions
            CatchBeatmap catchBeatmap = Decode(path);
            // used for operations on the map's sliders and writing. didn't have the sanity to work with 20 different hitobject classes in lazer libraries
            Beatmap beatmap = BeatmapDecoder.Decode(path);

            // create a green line for each red line
            beatmap.TimingPoints.AssignInherited();

            // give more room for droplet adjustments, gonna make it better in the future
            beatmap.TimingPoints.ForEach(x =>
            {
                if (!x.Inherited)
                    x.BeatLength /= 2;
            });

            List<SliderInfo> derandomizedSliderInfo = new List<SliderInfo>();

            for (int i = 0; i < catchBeatmap.HitObjects.Count; i++)
            {
                var hitObject = catchBeatmap.HitObjects[i];

                if (!(hitObject is JuiceStream))
                    continue;

                SliderInfo sliderInfo = new SliderInfo(beatmap.HitObjects[i] as Slider);

                foreach (var nested in hitObject.NestedHitObjects)
                {
                    var catchObj = nested as CatchHitObject;

                    // reflection, because xOffset is internal
                    double xOffset = (float)typeof(CatchHitObject).GetProperty("XOffset", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(catchObj);

#warning if catchObj.X - xOffset doesn't work correctly, try catchObj.X + xOffset

                    sliderInfo.NestedObjects.Add(new NestedObjectInfo
                    {
                        X = catchObj.X - 2 * xOffset,
                        StartTime = catchObj.StartTime
                    });
                }

                derandomizedSliderInfo.Add(sliderInfo);
            }

            foreach (var slider in derandomizedSliderInfo)
            {
                if (slider.BaseSlider.Repeats > 1)
                    continue;

                beatmap.DerandomizeAndReplace(slider);
            }

            return beatmap;
        }

        private static void DerandomizeAndReplace(this Beatmap beatmap, SliderInfo sliderInfo)
        {
            Slider baseSlider = sliderInfo.BaseSlider;
            // BRUTE FORCE DEBUG
            baseSlider.PixelLength *= 2;

            // -100 -> 1x SV
            TimingPoint greenLine = beatmap.TimingPoints.FindAll(x => !x.Inherited).GetFirstLowerOrEqual(baseSlider.StartTime, x => x.Offset);
            TimingPoint redLine = beatmap.TimingPoints.FindAll(x => x.Inherited).GetFirstLowerOrEqual(baseSlider.StartTime, x => x.Offset);

            // https://osu.ppy.sh/help/wiki/osu!_File_Formats/Osu_(file_format)#sliders -> length
            double sliderTimeLength = baseSlider.PixelLength * redLine.BeatLength * greenLine.BeatLength / (-10000 * beatmap.DifficultySection.SliderMultiplier);

            List<Vector2> sliderPoints = baseSlider.SliderPoints;
            sliderPoints.Clear();

            // the direction the slider is supposed to go on the y axis. for readability's sake
            int yMultiplier = 1;

            // skipping one, because it's the slider head
            foreach (var nested in sliderInfo.NestedObjects.Skip(1))
            {
                Vector2 lastNodePos = sliderPoints.LastOrDefault();

                if (sliderPoints.Count == 0)
                    lastNodePos = baseSlider.Position;

                // used for calculating yOffset 2 lines later
                double xOffset = nested.X - lastNodePos.X;

                double nestedLength = -10000 * (nested.StartTime - sliderInfo.NestedObjects[sliderInfo.NestedObjects.IndexOf(nested) - 1].StartTime) * beatmap.DifficultySection.SliderMultiplier / (redLine.BeatLength * greenLine.BeatLength);

                // x^2 + y^2 = len^2; y^2 = len^2 - x^2; y = sqrt(len^2 - x^2)
                double yOffset = Math.Sqrt((nestedLength * nestedLength) - (xOffset * xOffset));

                // change the direction if about to go out of bounds
                if (lastNodePos.Y > 512 || lastNodePos.Y < 0)
                    yMultiplier *= -1;

                double x = nested.X;
                double y = lastNodePos.Y + (yMultiplier * yOffset);

                // adding twice to make a red anchor
                sliderPoints.Add(new Vector2((float)Math.Round(x), (float)Math.Round(y)));
                sliderPoints.Add(new Vector2((float)Math.Round(x), (float)Math.Round(y)));
            }

            // replace the old slider
            beatmap.HitObjects[beatmap.HitObjects.FindIndex(x => x.StartTime == baseSlider.StartTime)] = baseSlider;
        }

        private static void AssignInherited(this List<TimingPoint> timingPoints)
        {
            List<TimingPoint> filtered = timingPoints.FindAll(x => x.Inherited);

            filtered.ForEach(x => 
            {
                if (timingPoints.Find(y => !y.Inherited && y.Offset == x.Offset) == null)
                    timingPoints.Insert(timingPoints.IndexOf(x), new TimingPoint
                    {
                        Inherited = false,
                        BeatLength = -100,
                        Offset = x.Offset,
                        Effects = x.Effects,
                        CustomSampleSet = x.CustomSampleSet,
                        SampleSet = x.SampleSet,
                        TimeSignature = x.TimeSignature,
                        Volume = x.Volume
                    });
            });
        }
    }
}
