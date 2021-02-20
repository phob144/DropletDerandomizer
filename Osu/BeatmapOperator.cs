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
using DropletDerandomizer.Extensions;
using System.Numerics;
using System.Data;
using System.Runtime.CompilerServices;

namespace DropletDerandomizer.Osu
{
    public static partial class BeatmapOperator
    {
        // derandomizationRate = 0-20, used to determine the maximum offset reduction (the higher it is, the smoother the sliders will be)
        public static Beatmap DerandomizeDroplets(string path, double derandomizationRate)
        {
            // used to determine droplets' positions
            CatchBeatmap catchBeatmap = Decode(path);
            // used for operations on the map's sliders and writing. i don't have the sanity to work with 20 different hitobject classes in lazer libraries
            Beatmap beatmap = BeatmapDecoder.Decode(path);

            // create a green line for each red line before separating them
            beatmap.TimingPoints.AssignInheritedPoints();

            // separate green lines from red lines because OsuParsers doesn't support that
            List<TimingPoint> greenLines = beatmap.TimingPoints.FindAll(x => !x.Inherited);
            List<TimingPoint> redLines = beatmap.TimingPoints.FindAll(x => x.Inherited);

            List<SliderInfo> derandomizedSliderInfo = new List<SliderInfo>();

            for (int i = 0; i < catchBeatmap.HitObjects.Count; i++)
            {
                var hitObject = catchBeatmap.HitObjects[i];

                // we don't care about fruits or spinners
                if (!(hitObject is JuiceStream))
                    continue;

                SliderInfo sliderInfo = new SliderInfo(beatmap.HitObjects[i] as Slider);

                foreach (var nested in hitObject.NestedHitObjects)
                {
                    var catchObj = nested as CatchHitObject;

                    // using reflection, because peppy is trying to hide his sins by making xOffset internal
                    double xOffset = (float)typeof(CatchHitObject).GetProperty("XOffset", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(catchObj);

                    sliderInfo.NestedObjects.Add(new NestedObjectInfo
                    {
                        // subtracting xOffset for the first time returns the x position on the original slider path at this time
                        // subtracting it for the second time with derandomizationRate applied moves the droplet closer to the original slider's path
                        X = catchObj.X - xOffset - xOffset * derandomizationRate / 20,
                        StartTime = catchObj.StartTime
                    });
                }

                derandomizedSliderInfo.Add(sliderInfo);
            }

            // give more room for droplet adjustments, create a list of multipliers parallel to green lines
            List<double> multipliers =
                greenLines.Select(
                    x => x.GetLowestRequiredSVMultiplier(redLines.GetFirstLowerOrEqual(x.Offset, y => y.Offset),
                    beatmap.DifficultySection.SliderMultiplier))
                .ToList();

            // apply the multiplier to all green lines. dividing, because BeatLength is unconverted (-100 / SV)
            beatmap.TimingPoints.ForEach(x =>
            {
                if (!x.Inherited)
                    x.BeatLength /= multipliers[greenLines.FindIndex(y => y.Offset == x.Offset)];
            });

            for (int i = 0; i < derandomizedSliderInfo.Count; i++)
            {
                var slider = derandomizedSliderInfo[i];
                var multiplier = multipliers[greenLines.IndexOf(greenLines.GetFirstLowerOrEqual(slider.BaseSlider.StartTime, x => x.Offset))];

                beatmap.DerandomizeAndReplace(slider, multiplier);
            }

            // rename the difficulty
            beatmap.MetadataSection.Version += " (derandomized)";

            return beatmap;
        }

        private static void DerandomizeAndReplace(this Beatmap beatmap, SliderInfo sliderInfo, double multiplier)
        {
            Slider baseSlider = sliderInfo.BaseSlider;

            // apply the SV multiplier to slider length
            baseSlider.PixelLength *= multiplier;

            TimingPoint greenLine = beatmap.TimingPoints.FindAll(x => !x.Inherited).GetFirstLowerOrEqual(baseSlider.StartTime, x => x.Offset);
            TimingPoint redLine = beatmap.TimingPoints.FindAll(x => x.Inherited).GetFirstLowerOrEqual(baseSlider.StartTime, x => x.Offset);

            // https://osu.ppy.sh/help/wiki/osu!_File_Formats/Osu_(file_format)#sliders -> length
            double sliderTimeLength = baseSlider.PixelLength * redLine.BeatLength * greenLine.BeatLength / (-10000 * beatmap.DifficultySection.SliderMultiplier);

            List<Vector2> sliderPoints = baseSlider.SliderPoints;
            sliderPoints.Clear();

            // the direction the slider is supposed to go on the y axis. for readability
            int yMultiplier = 1;

            // skipping one, because it's the slider head
            foreach (var nested in sliderInfo.NestedObjects.Skip(1))
            {
                Vector2 lastNodePos = sliderPoints.LastOrDefault();

                // if there are no elements in the list, it means we are at the first droplet. in this case, take the slider head's position as lastNodePos
                if (sliderPoints.Count == 0)
                    lastNodePos = baseSlider.Position;

                // https://osu.ppy.sh/help/wiki/osu!_File_Formats/Osu_(file_format)#sliders changed to return the pixel length instead
                double nestedLength = -10000 * (nested.StartTime - sliderInfo.NestedObjects[sliderInfo.NestedObjects.IndexOf(nested) - 1].StartTime) * beatmap.DifficultySection.SliderMultiplier / (redLine.BeatLength * greenLine.BeatLength);

                // used for calculating yOffset
                double xOffset = nested.X - lastNodePos.X;

                // x^2 + y^2 = len^2; y^2 = len^2 - x^2; y = sqrt(len^2 - x^2)
                double yOffset = Math.Sqrt((nestedLength * nestedLength) - (xOffset * xOffset));

                // change the direction if about to go out of bounds
                if ((lastNodePos.Y + yOffset >= 386 && yMultiplier == 1) || (lastNodePos.Y - yOffset <= 0 && yMultiplier == -1))
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

        private static double GetLowestRequiredSVMultiplier(this TimingPoint greenLine, TimingPoint redLine, double sliderMultiplier)
        {
            double timeBetweenDroplets = redLine.BeatLength;

            // droplet density calculation from osu!lazer source code
            while (timeBetweenDroplets > 100)
                timeBetweenDroplets /= 2;

            // https://osu.ppy.sh/help/wiki/osu!_File_Formats/Osu_(file_format)#sliders changed to return the pixel length instead
            double length = -10000 * timeBetweenDroplets * sliderMultiplier / (redLine.BeatLength * greenLine.BeatLength);

            // xOffset's range is [-20,20], so the furthest the droplets can be offset from each other is 40px
            // add 0.2 to make up for floating point errors
            return ((length + 40) / length) + 0.2;
        }

        private static void AssignInheritedPoints(this List<TimingPoint> timingPoints)
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
