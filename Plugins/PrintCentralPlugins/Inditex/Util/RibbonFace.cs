using System;
using System.Drawing;

namespace SmartdotsPlugins.Inditex.Util
{
    // TODO: move to common namespace, this logic is not exclusive inditext, this util is for all brands
    public class RibbonFace
    {
        public Font Font;
        public float WidthInInches;
        public float HeightInInches;
        public string FittingText;
        public int LineWidth;

        public float WidthInPixels { get => WidthInInches * 96f; }
        public float HeightInPixels { get => HeightInInches * 96f; }

        public RibbonFace()
        {

        }

        public RibbonFace(Font font, float widthInInches, float heightInInches)
        {
            Font = font;
            WidthInInches = widthInInches;
            HeightInInches = heightInInches;
        }

        public bool ContentFits(Graphics g, string text)
        {
            var size = g.MeasureString(text, this.Font, (int)Math.Ceiling(this.WidthInPixels), new StringFormat(StringFormatFlags.MeasureTrailingSpaces));
            var result = size.Height < this.HeightInPixels;
            return result;
        }
    }
}
