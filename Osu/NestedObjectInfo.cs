using System;
using System.Collections.Generic;
using System.Text;

namespace DropletDerandomizer.Osu
{
    public class NestedObjectInfo
    {
        public double StartTime { get; set; }
        // X value that will result in a correctly set droplet after adding XOffset
        public double X { get; set; }

        // object type is irrelevant, since we're going to set it in its place anyways
    }
}
