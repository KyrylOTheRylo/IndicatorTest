namespace ATAS.Indicators.Technical
{

    using ATAS.Indicators;
    using ATAS.Indicators.Drawing;
    using OFT.Rendering.Context;
    using OFT.Rendering.Settings;
    using OFT.Rendering.Tools;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Drawing;

    public enum ClusterT
    {
        DeltaPositive,
        DeltaNegative,
        Volume
    }
    public class IndicatorTests : Indicator
    {
        private int _period = 10;
        private ClusterT _clusterType = ClusterT.Volume;
        private SortedSet<double> clusterInfo= new SortedSet<double>();



        public IndicatorTests()
        {
            EnableCustomDrawing = true;

            //Subscribing only to drawing on final layout
            SubscribeToDrawingEvents(DrawingLayouts.Final);
            LineSeries.Add(new LineSeries("Down")
            {
                Color = System.Windows.Media.Colors.Orange,
                LineDashStyle = LineDashStyle.Dash,
                Value = 97000,
                Width = 1
            });


        }
        protected override void OnRender(RenderContext context, DrawingLayouts layout)
        {
            // creating pen, width 4px
            var pen = new RenderPen(Color.BlueViolet, 4);
            HorizontalLinesTillTouch.Add(new LineTillTouch(CurrentBar-65, 105000, new System.Drawing.Pen(new System.Drawing.SolidBrush(Color.Black))));
            

            
        }

        [Display(GroupName = "Variables", Name = "Period", Order = 10)]
        public int Period { 
            get { return _period; } 
            set { _period = value;
                RecalculateValues();
            } 
        }
        [Display(GroupName = "ClusterSettings", Name = "ClusterType")]
        public ClusterT clusterType
        {
            get { return _clusterType; }
            set {
                _clusterType = value;
                RecalculateValues(); }
        } 

        protected override void OnCalculate(int bar, decimal value)
        {
            var start = Math.Max(0, bar - Period + 1);
            var count = Math.Min(bar + 1, Period);
            var max = (decimal)SourceDataSeries[start];
            for (var i = start + 1; i < start + count; i++)
            {
                max = Math.Max(max, (decimal)SourceDataSeries[i]);
            }

            this[bar] = max;
        }
    }
}