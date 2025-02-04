namespace ATAS.Indicators.Technical
{

    using ATAS.Indicators;
    using ATAS.Indicators.Drawing;
    using OFT.Rendering.Context;
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
    public interface IValueStrategy
    {
        decimal GetValue(IndicatorCandle indicator);

        decimal GetPrice(IndicatorCandle indicator);
        List<(decimal price, decimal value)> GetPricesNValues(IndicatorCandle indicator, int chunksize = 1);

        public class VolumeOption : IValueStrategy
        {

            public decimal GetValue(IndicatorCandle indicator)
            {
                return indicator.MaxVolumePriceInfo.Volume;
            }
            public decimal GetPrice(IndicatorCandle indicator)
            {
                return indicator.MaxVolumePriceInfo.Price;
            }

            public List<(decimal price, decimal value)> GetPricesNValues(IndicatorCandle indicator, int chunksize = 1)
            {
                var prices = indicator.GetAllPriceLevels();
                List<(decimal price, decimal value)> answer = new List<(decimal price, decimal value)>();
                for (int i = 0; i < prices.Count(); i++)
                {
                    int startIndex = Math.Max(0, i - chunksize);

                    decimal volume = 0;
                    for (int j = startIndex; j <= i; j++)
                    {
                        volume += prices.ElementAt(j).Volume;
                    }
                    answer.Add((prices.ElementAt(i).Price, volume));

                }
                answer = answer.OrderByDescending(item => item.value).ToList();

                return answer;




            }
        }

        public class AskOption : IValueStrategy
        {
            public decimal GetValue(IndicatorCandle indicator)
            {
                return indicator.MaxAskPriceInfo.Ask;
            }
            public decimal GetPrice(IndicatorCandle indicator)
            {
                return indicator.MaxAskPriceInfo.Price;
            }

            public List<(decimal price, decimal value)> GetPricesNValues(IndicatorCandle indicator, int chunksize = 1)
            {
                var prices = indicator.GetAllPriceLevels();
                List<(decimal price, decimal value)> answer = new List<(decimal price, decimal value)>();
                for (int i = 0; i < prices.Count(); i++)
                {
                    int startIndex = Math.Max(0, i - chunksize);

                    decimal volume = 0;
                    for (int j = startIndex; j <= i; j++)
                    {
                        volume += prices.ElementAt(j).Ask;
                    }
                    answer.Add((prices.ElementAt(i).Price, volume));

                }
                answer = answer.OrderByDescending(item => item.value).ToList();

                return answer;
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
            public List<(decimal price, decimal value)> GetPricesNValues(IndicatorCandle indicator, int chunksize = 1)
            {
                var prices = indicator.GetAllPriceLevels();
                List<(decimal price, decimal value)> answer = new List<(decimal price, decimal value)>();
                for (int i = 0; i < prices.Count(); i++)
                {
                    int startIndex = Math.Max(0, i - chunksize+1);

                    decimal volume = 0;
                    for (int j = startIndex; j <= i; j++)
                    {
                        volume += prices.ElementAt(j).Bid;
                    }
                    answer.Add((prices.ElementAt(startIndex).Price, volume));

                }
                answer = answer.OrderByDescending(item => item.value).ToList();

                return answer;
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

            public decimal Price { get; set; }

            public ItemClass(int bar, decimal value, decimal price)
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
            private int _lookbackBars = 400; // Number of bars to look back
            private int _lineLength = 350;   // Length of horizontal lines
            private int _topItems = 10;     // Number of top items to display
            private int _pricesLevels = 1; // Number of price levels to use for calculation
            private int _bars_to_use = 1; // Number of bars to use for calculation
            private Color _defaultColor = Color.Red; // Default color for lines
            private bool _tillTouch = false;
            private ClusterT _clusterType = ClusterT.DeltaNegative;
            private SortedSet<ItemClass> clusterInfo = new SortedSet<ItemClass>();
            private SortedSet<ItemClass> _singleclusterInfo = new SortedSet<ItemClass>();



            public IndicatorTests()
            {
                EnableCustomDrawing = true;

                //Subscribing only to drawing on final layout
                SubscribeToDrawingEvents(DrawingLayouts.Final);



            }
            protected override void OnRender(RenderContext context, DrawingLayouts layout)
            {
                // Filter the items in clusterInfo for the last 'LookbackBars'
                
                var start = Math.Max(0, CurrentBar - LookbackBars + 1);
                var filteredItems = clusterInfo
                    .Where(item => item.Bar >= start && item.Bar < CurrentBar - 1) // Filter items within the lookback range
                    .OrderByDescending(item => item.Value) // Sort by Value descending
                    .Take(TopItems); // Take the specified number of top items



                // Draw horizontal lines for the top items
                foreach (var item in filteredItems)
                {
                    var lineEndBar = Math.Min(CurrentBar, item.Bar + LineLength); // Ensure line doesn't extend beyond the current bar

                    // Get the next color from ColorsSource, or fallback to default color
                    var color = _defaultColor;
                    // draw only if it isn't already drawn
                    if (HorizontalLinesTillTouch.Count(x => x.FirstPrice == item.Price && x.FirstBar == item.Bar ) == 0)
                    {
                        if (_tillTouch)
                        {
                            HorizontalLinesTillTouch.Add(new LineTillTouch(
                            item.Bar,
                            item.Price,
                            new System.Drawing.Pen(new System.Drawing.SolidBrush(color), 2) // Use dynamic color
                        ));
                        }
                        else
                        {
                            HorizontalLinesTillTouch.Add(new LineTillTouch(
                                item.Bar,
                                item.Price,
                                new System.Drawing.Pen(new System.Drawing.SolidBrush(color), 2), // Use dynamic color
                                lineEndBar - item.Bar // Calculate the adjusted line length
                            ));
                        }
                    }
                    
                    
                }
            }
            [Display(GroupName = "Variables", Name = "Till Touch")]
            public bool TillTouch
            {
                get { return _tillTouch; }
                set
                {
                    _tillTouch = value;
                    RecalculateValues();
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
            [Display(GroupName = "Variables", Name = "Lookback Bars", Order = 400)]
            public int LookbackBars
            {
                get { return _lookbackBars; }
                set
                {
                    _lookbackBars = value;
                    RecalculateValues();
                }
            }

            [Display(GroupName = "Variables", Name = "Line Length", Order = 350)]
            public int LineLength
            {
                get { return _lineLength; }
                set
                {
                    _lineLength = value;
                    RecalculateValues();
                }
            }
            [Display(GroupName = "Variables", Name = "Prices Count", Order = 1)]
            public int PricesLevels
            {
                get { return _pricesLevels; }
                set
                {
                    _pricesLevels = value;
                    RecalculateValues();
                }
            }
            [Display(GroupName = "Variables", Name = "Bars to use", Order = 1)]
            public int BarsToUse
            {
                get { return _bars_to_use; }
                set
                {
                    _bars_to_use = value;
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
                set
                {
                    _clusterType = value;
                    clusterInfo = new SortedSet<ItemClass>();
                    RecalculateValues();
                }
            }

            protected override void OnCalculate(int bar, decimal value)
            {
                if (CurrentBar - LookbackBars - BarsToUse >= bar )
                {
                    return;
                }
                clusterInfo = new SortedSet<ItemClass>(
                        clusterInfo.Where(key => key.Bar != bar),
                        new ValueComparer()
                    );
                _singleclusterInfo = new SortedSet<ItemClass>(
                        _singleclusterInfo.Where(key => key.Bar != bar)
                    );
                var candle = GetCandle(bar);


                // Get the selected strate
                var strategy = ValueStrategyFactory.GetStrategy(clusterType);
                var items = strategy.GetPricesNValues(candle, PricesLevels);
                var tmpcluster = new SortedSet<ItemClass>();
                for (int i = 0; i < items.Count() ; i++)
                {
                    tmpcluster.Add(new ItemClass(bar, items.ElementAt(i).value, items.ElementAt(i).price));
                    _singleclusterInfo.Add(new ItemClass(bar, items.ElementAt(i).value, items.ElementAt(i).price));
                }
                var startindex = Math.Max(0, bar - BarsToUse +1 );

                foreach (var item in tmpcluster)
                {
                    decimal tmpValue = item.Value;
                    var previousbarsOnThatLevel = _singleclusterInfo.Where(x => x.Price == item.Price && x.Bar >= startindex && x.Bar < bar).ToList();
                    foreach (var prevItem in previousbarsOnThatLevel)
                    {

                        tmpValue += prevItem.Value;
                    }
                    clusterInfo.Add(new ItemClass(item.Bar, tmpValue, item.Price));
                    
                }
                if (bar % (1+ BarsToUse) == 0)
                {
                    clusterInfo = new SortedSet<ItemClass>(
                        clusterInfo.Where(key => key.Bar >= bar - LookbackBars - 1).OrderByDescending(key => key.Value),
                        new ValueComparer()
                    );
                    _singleclusterInfo = new SortedSet<ItemClass>(
                        _singleclusterInfo.Where(key => key.Bar >= bar - BarsToUse - 1)
                    );
                }


            }
        }
    }
}
