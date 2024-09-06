using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework;

namespace AddVertexColorPipeline
{
    [ContentProcessor]
    public class AddVertexColorProcessor : ModelProcessor
    {
        public AddVertexColorProcessor() : base() { }

        protected override void ProcessGeometryUsingMaterial(MaterialContent material, IEnumerable<GeometryContent> geometryCollection, ContentProcessorContext context)
        {
            // Begin dark arts.
            // Loop through the GeometryContent items and check each one for a Color0 channel. If it doesn't have one,
            // then get the number of vertices in the current GeometryContent, build a List<Vector4> of all white (Vector4.One)
            // Color0 channels, and add that channel to the geometry.
            foreach (var geometry in geometryCollection)
            {
                if (!geometry.Vertices.Channels.Contains(VertexChannelNames.Color(0)))
                {
                    int count = geometry.Vertices.VertexCount;
                    List<Vector4> channelData = new List<Vector4>(count);
                    for (int i = 0; i < count; i++)
                    {
                        channelData.Add(Vector4.One);
                    }
                    geometry.Vertices.Channels.Add(VertexChannelNames.Color(0), channelData);
                }
            }

            base.ProcessGeometryUsingMaterial(material, geometryCollection, context);
        }

    }
}
