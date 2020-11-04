using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Catch.Beatmaps;
using osu.Game.Rulesets.Catch.MathUtils;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using System.IO;

// FINISH IT TOMMOROW PLEASE IT LOOKS SO PROMISING

namespace DropletDerandomizer.Osu
{
    public static class BeatmapOperator
    {
        public static Beatmap Derandomize(this Beatmap beatmap)
        {
            CatchBeatmap catchBeatmap = GetCatchBeatmap(beatmap);

            foreach (var hitObject in beatmap.HitObjects)
            {

            }
        }

        public static Beatmap Decode(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open))
            {
                using (LineBufferedReader reader = new LineBufferedReader(stream))
                {
                    return Decoder.GetDecoder<Beatmap>(reader).Decode(reader);
                }
            }
        }

        private static CatchBeatmap GetCatchBeatmap(Beatmap beatmap)
        {
            CatchBeatmapConverter converter = new CatchBeatmapConverter(beatmap, new CatchRuleset());
            CatchBeatmap catchBeatmap = converter.Convert() as CatchBeatmap;

            // call apply defaults for each hitobject to create NestedHitObjects
            foreach (var hitObject in catchBeatmap.HitObjects)
            {
                hitObject.ApplyDefaults(catchBeatmap.ControlPointInfo, catchBeatmap.BeatmapInfo.BaseDifficulty);
            }

            // apply random offsets
            CatchBeatmapProcessor processor = new CatchBeatmapProcessor(catchBeatmap);
            processor.PostProcess();

            return catchBeatmap;
        }
    }
}
