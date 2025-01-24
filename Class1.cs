namespace ATAS.Indicators.Technical
{

    using ATAS.Indicators;
    using ATAS.Indicators.Drawing;
    using ATAS.Types;
    using OFT.Rendering.Context;
    using OFT.Rendering.Settings;
    using OFT.Rendering.Tools;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.DataAnnotations;
    using System.Drawing;
    using Utils.Common.Logging;

    public enum ClusterT
    {
        DeltaPositive,
        DeltaNegative,
        Volume
    }
    public interface IValueStrategy
    {
        decimal GetValue( IndicatorCandle indicator);
        decimal GetPrice(IndicatorCandle indicator);
    }

    public class VolumeOption : IValueStrategy
    {

        public decimal GetValue( IndicatorCandle indicator)
        {
            return indicator.MaxVolumePriceInfo.Volume;
        }
        public decimal GetPrice(IndicatorCandle indicator)
        {
            return indicator.MaxVolumePriceInfo.Price;
        }
    }

    public class AskOption : IValueStrategy
    {
        public decimal GetValue( IndicatorCandle indicator)
        {
            return indicator.MaxAskPriceInfo.Ask;
        }
        public decimal GetPrice(IndicatorCandle indicator)
        {
            return indicator.MaxAskPriceInfo.Price;
        }
    }

    public class BidOption : IValueStrategy
    {
        public decimal GetValue(IndicatorCandle indicator)
        {
            return indicator.MaxBidPriceInfo.Bid;
        }
        public decimal GetPrice(IndicatorCandle indicator)
        {
            return indicator.MaxBidPriceInfo.Price;
        }
    }

    public class ValueStrategyFactory
    {
        public static IValueStrategy GetStrategy(ClusterT clusterType)
        {
            return clusterType switch
            {
                ClusterT.Volume => new VolumeOption(),
                ClusterT.DeltaPositive => new AskOption(),
                ClusterT.DeltaNegative => new BidOption(),
                _ => throw new ArgumentException("Invalid ClusterT value")
            };
        }
    }

    public class ItemClass : IComparable<ItemClass>
{
    public int Bar { get; set; }
    public decimal Value { get; set; }

    public decimal Price {  get; set; }

    public ItemClass(int bar, decimal value, decimal  price)
    {
        Bar = bar;
        Value = value;
        Price = price;
    }

    // Implement IComparable<T>
    public int CompareTo(ItemClass other)
    {
        if (other == null) throw new ArgumentNullException(nameof(other));
        return Value.CompareTo(other.Value);
    }
}
    public class ValueComparer : IComparer<ItemClass>
    {
        public int Compare(ItemClass x, ItemClass y)
        {
            if (x == null || y == null)
                throw new ArgumentNullException("CustomClass objects cannot be null.");

            return x.Value.CompareTo(y.Value);
        }
    }
    public class IndicatorTests : Indicator
    {
        private int _lookbackBars = 100; // Number of bars to look back
        private int _lineLength = 50;   // Length of horizontal lines
        private int _topItems = 10;     // Number of top items to display
        private Color _defaultColor = Color.Red; // Default color for lines
        private ClusterT _clusterType = ClusterT.Volume;
        private SortedSet<ItemClass> clusterInfo= new SortedSet<ItemClass>();



        public IndicatorTests()
        {
            EnableCustomDrawing = true;

            //Subscribing only to drawing on final layout
            SubscribeToDrawingEvents(DrawingLayouts.LatestBar);



        }
        protected override void OnRender(RenderContext context, DrawingLayouts layout)
        {
            // Filter the items in clusterInfo for the last 'LookbackBars'
            var start = Math.Max(0, CurrentBar - LookbackBars + 1);
            var filteredItems = clusterInfo
                .Where(item => item.Bar >= start && item.Bar <= CurrentBar) // Filter items within the lookback range
                .OrderByDescending(item => item.Value) // Sort by Value descending
                .Take(TopItems); // Take the specified number of top items

            

            // Draw horizontal lines for the top items
            foreach (var item in filteredItems)
            {
                var lineEndBar = Math.Min(CurrentBar, item.Bar + LineLength); // Ensure line doesn't extend beyond the current bar

                // Get the next color from ColorsSource, or fallback to default color
                var color= _defaultColor;

                HorizontalLinesTillTouch.Add(new LineTillTouch(
                    item.Bar,
                    item.Price,
                    new System.Drawing.Pen(new System.Drawing.SolidBrush(color), 2), // Use dynamic color
                    lineEndBar - item.Bar // Calculate the adjusted line length
                ));
            }
        }
        [Display(Name = "Colors", GroupName = "Examples")]
        public Color DefaultColpr
        {
            get { return _defaultColor; }
            set
            {
                _defaultColor = value;
                RecalculateValues();
            }
        }
        [Display(GroupName = "Variables", Name = "Lookback Bars", Order = 200)]
        public int LookbackBars
        {
            get { return _lookbackBars; }
            set
            {
                _lookbackBars = value;
                RecalculateValues();
            }
        }

        [Display(GroupName = "Variables", Name = "Line Length", Order = 50)]
        public int LineLength
        {
            get { return _lineLength; }
            set
            {
                _lineLength = value;
                RecalculateValues();
            }
        }

        [Display(GroupName = "Variables", Name = "Top Items", Order = 5)]
        public int TopItems
        {
            get { return _topItems; }
            set
            {
                _topItems = value;
                RecalculateValues();
            }
        }

        [Display(GroupName = "ClusterSettings", Name = "ClusterType")]
        public ClusterT clusterType
        {
            get { return _clusterType; }
            set {
                _clusterType = value;
                clusterInfo= new SortedSet<ItemClass>();
                RecalculateValues(); }
        } 

        protected override void OnCalculate(int bar, decimal value)
        {
            var candle = GetCandle(bar);

            // Get the selected strategy
            var strategy = ValueStrategyFactory.GetStrategy(clusterType);
            var calculatedValue = strategy.GetValue(candle);

            clusterInfo.RemoveWhere(item => item.Bar == bar);
            clusterInfo.Add(new ItemClass(bar, calculatedValue, strategy.GetPrice(candle)));

            //// Calculate the maximum value within the specified period
            //var start = Math.Max(0, bar - Period + 1);
            //var count = Math.Min(bar + 1, Period);
            //var max = strategy.GetPrice(candle);
            //for (var i = start + 1; i < start + count; i++)
            //{
            //    var tmpCandle = GetCandle(i);
            //    var tmpValue = strategy.GetPrice(tmpCandle);
            //    max = Math.Max(max, (decimal)tmpValue);
            //}

            //// Assign the max value to the current bar
            //this[bar] = max;
        }
    }
}