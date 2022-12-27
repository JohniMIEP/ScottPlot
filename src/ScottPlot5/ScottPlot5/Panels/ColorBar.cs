﻿using ScottPlot.Axis;
using ScottPlot.Axis.StandardAxes;
using ScottPlot.LayoutSystem;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScottPlot.Panels
{
    public class ColorBar : IPanel
    {
        public IHasColorAxis Source { get; set; }

        public Edge Edge { get; set; }
        public float Width { get; set; } = 50;
        public float Margin { get; set; } = 15;
        public bool ShowDebugInformation { get; set; } = false;

        public ColorBar(IHasColorAxis source, Edge edge = Edge.Right)
        {
            Source = source;
            Edge = edge;
        }

        // Unfortunately the size of the axis depends on the size of the plotting window, so we just have to guess here. 2000 should be larger than most
        public float Measure() => Margin + GetAxis(2000).Measure() + Width;

        public void Render(SKSurface surface, PixelRect rect)
        {
            using var _ = new SKAutoCanvasRestore(surface.Canvas);

            SKRect colorbarRect = Edge switch
            {
                Edge.Left => new(rect.Left - Width, rect.Top, rect.Left, rect.Top + rect.Height),
                Edge.Right => new(rect.Right, rect.Top, rect.Right + Width, rect.Top + rect.Height),
                Edge.Bottom => new(rect.Left, rect.Bottom, rect.Left + rect.Width, rect.Bottom + Width),
                Edge.Top => new(rect.Left, rect.Top - Width, rect.Left + rect.Width, rect.Top),
                _ => throw new ArgumentOutOfRangeException(nameof(Edge))
            };

            SKPoint marginTranslation = GetTranslation(Margin);
            SKPoint axisTranslation = GetTranslation(Width);

            using var bmp = GetBitmap();

            surface.Canvas.Translate(marginTranslation);
            surface.Canvas.DrawBitmap(bmp, colorbarRect);

            var colorbarLength = Edge.IsVertical() ? rect.Height : rect.Width;
            var axis = GetAxis(colorbarLength);


            surface.Canvas.Translate(axisTranslation);
            axis.Render(surface, rect);

        }

        private SKPoint GetTranslation(float magnitude) => Edge switch
        {
            Edge.Left => new(-magnitude, 0),
            Edge.Right => new(magnitude, 0),
            Edge.Bottom => new(0, magnitude),
            Edge.Top => new(0, -magnitude),
            _ => throw new ArgumentOutOfRangeException(nameof(Edge))
        };

        private SKBitmap GetBitmap()
        {
            uint[] argbs = Enumerable.Range(0, 256).Select(i => Source.Colormap.GetColor((Edge.IsVertical() ? 255 - i : i) / 255f).ARGB).ToArray();

            int bmpWidth = Edge.IsVertical() ? 1 : 256;
            int bmpHeight = !Edge.IsVertical() ? 1 : 256;

            return Drawing.BitmapFromArgbs(argbs, bmpWidth, bmpHeight);
        }

        private IAxis GetAxis(float length)
        {
            IAxis axis = Edge switch
            {
                Edge.Left => new LeftAxis(),
                Edge.Right => new RightAxis(),
                Edge.Bottom => new BottomAxis(),
                Edge.Top => new TopAxis(),
                _ => throw new ArgumentOutOfRangeException(nameof(Edge))
            };

            axis.Label.Text = "";

            var range = Source.GetRange();
            axis.Min = range.Min;
            axis.Max = range.Max;

            axis.TickGenerator.Regenerate(axis.Range, length);

            return axis;
        }
    }
}