namespace ATAS.Indicators.Technical
{

    using ATAS.Indicators;
    using System;
    using System.ComponentModel.DataAnnotations;

    public class IndicatorTests : Indicator
    {
        private int _period = 10;

        [Display(GroupName = "Variables", Name = "Period", Order = 10)]
        public int Period { 
            get { return _period; } 
            set { _period = value;
                RecalculateValues();
            } 
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