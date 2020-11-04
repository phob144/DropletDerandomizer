using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Catch.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;

namespace DropletDerandomizer.Osu
{
    public static partial class BeatmapOperator
    {
        public static CatchBeatmap Decode(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open))
            {
                using (LineBufferedReader reader = new LineBufferedReader(stream))
                {
                    return Decoder.GetDecoder<Beatmap>(reader).Decode(reader).GetCatchBeatmap();
                }
            }
        }

        private static CatchBeatmap GetCatchBeatmap(this Beatmap beatmap)
        {
            CatchBeatmapConverter converter = new CatchBeatmapConverter(beatmap, new CatchRuleset());
            CatchBeatmap catchBeatmap = converter.Convert() as CatchBeatmap;

            // call apply defaults for each hitobject to create NestedHitObjects
            foreach (var hitObject in catchBeatmap.HitObjects)
            {
                hitObject.ApplyDefaults(catchBeatmap.ControlPointInfo, catchBeatmap.BeatmapInfo.BaseDifficulty);
            }

            // apply random offsets and stuff
            CatchBeatmapProcessor processor = new CatchBeatmapProcessor(catchBeatmap);
            processor.PostProcess();

            return catchBeatmap;
        }
    }
}
