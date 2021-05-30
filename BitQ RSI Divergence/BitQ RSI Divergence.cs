using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;
using System.Collections;

namespace cAlgo
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class BitQRSIDivergence : Indicator
    {
        [Parameter()]
        public DataSeries Source { get; set; }
        [Parameter()]
        public int periods { get; set; }
        [Output("Main")]
        public IndicatorDataSeries Result { get; set; }

        public struct Point
        {
            public int index;
            public double value;
            public DateTime dateTime;
            public Point(int _index, double _value, DateTime _dateTime)
            {
                index = _index;
                value = _value;
                dateTime = _dateTime;
            }
        }

        private RelativeStrengthIndex rsi;
        private bool isLastPeak = false;
        private ArrayList peakList = new ArrayList();
        private ArrayList troughtList = new ArrayList();

        protected override void Initialize()
        {
            IndicatorArea.RemoveAllObjects();
            // Initialize and create nested indicators
            rsi = Indicators.RelativeStrengthIndex(Source, periods);
            IndicatorArea.DrawHorizontalLine("70", 70, Color.IndianRed, 1, LineStyle.LinesDots);
            IndicatorArea.DrawHorizontalLine("30", 30, Color.IndianRed, 1, LineStyle.LinesDots);

        }



        public override void Calculate(int index)
        {
            // Calculate value at specified index
            // Result[index] = ...
            Result[index] = rsi.Result[index];
            //if (index >= 1029 && index <= 1037)
            //{
            findPeakTrough(index);
            //}
        }


        public void findPeakTrough(int index)
        {
            if (index >= 3)
            {

                if (isOverBought(index - 1))
                {
                    if (Result[index - 2] < Result[index - 1] && Result[index - 1] > Result[index])
                    {
                        IndicatorArea.DrawIcon("RSI_peak_" + (index - 1).ToString(), ChartIconType.Circle, index - 1, Result[index - 1], Color.Cyan);
                        IndicatorArea.DrawText("RSI_peak_x" + (index - 1).ToString(), (index - 1).ToString(), index - 1, Result[index - 1], Color.Cyan);
                        var point = new Point(index - 1, Result[index - 1], Bars.OpenTimes.Last(index - 1));
                        peakList.Add(point);
                        if (isLastPeak)
                        {
                            filterBearish();
                        }
                        troughtList = new ArrayList();
                        isLastPeak = true;
                    }
                }
                if (isOverSold(index - 1))
                {
                    if (Result[index - 2] > Result[index - 1] && Result[index - 1] < Result[index])
                    {
                        IndicatorArea.DrawIcon("RSI_trought_" + (index - 1).ToString(), ChartIconType.Circle, index - 1, Result[index - 1], Color.Blue);
                        IndicatorArea.DrawText("RSI_trought_t_" + (index - 1).ToString(), (index - 1).ToString(), index - 1, Result[index - 1], Color.Blue);
                        var point = new Point(index - 1, Result[index - 1], Bars.OpenTimes.Last(index - 1));
                        troughtList.Add(point);
                        if (!isLastPeak)
                        {
                            filterBullish();
                        }
                        
                        peakList = new ArrayList();
                        isLastPeak = false;
                    }
                }
            }
        }

        public void filterBullish()
        {
            if (troughtList.Count >= 2)
            {
                Point currPoint = (Point)troughtList[troughtList.Count - 1];
                for(int i= 0; i < troughtList.Count; i++)
                {
                    Point iPoint = (Point)troughtList[i];
                    if (iPoint.value < currPoint.value)
                    {
                        if (!isBrokenLine(troughtList, i, troughtList.Count - 1, false))
                        {
                            if(isDivergenceTrought(iPoint, currPoint))
                            {
                                double open1 = Bars.OpenPrices[iPoint.index];
                                double close1 = Bars.ClosePrices[iPoint.index];
                                var trought1 = Math.Min(open1, close1);
                                var open2 = Bars.OpenPrices[currPoint.index];
                                var close2 = Bars.ClosePrices[currPoint.index];
                                var trought2 = Math.Min(open2, close2);
                                Chart.DrawTrendLine("Bullish_unconfirm1" + iPoint.index + "-" + currPoint.index, iPoint.index, trought1, currPoint.index, trought2, Color.Yellow, 2);
                                IndicatorArea.DrawTrendLine("Bullish_unconfirm1" + iPoint.index + "-" + currPoint.index, iPoint.index, iPoint.value, currPoint.index, currPoint.value, Color.Yellow, 3);
                            }
                        }
                    }
                }
            }
        }

        public void filterBearish()
        {
            if (peakList.Count >= 2)
            {
                Point currPoint = (Point)peakList[peakList.Count - 1];
                for (int i = 0; i < peakList.Count; i++)
                {
                    Point iPoint = (Point)peakList[i];
                    if (iPoint.value > currPoint.value)
                    {
                        if (!isBrokenLine(peakList, i, peakList.Count - 1, true))
                        {
                            if (isDivergencePeak(iPoint, currPoint))
                            {
                                double open1 = Bars.OpenPrices[iPoint.index];
                                double close1 = Bars.ClosePrices[iPoint.index];
                                var peak1 = Math.Max(open1, close1);
                                var open2 = Bars.OpenPrices[currPoint.index];
                                var close2 = Bars.ClosePrices[currPoint.index];
                                var peak2 = Math.Max(open2, close2);
                                Chart.DrawTrendLine("Bearish_unconfirm1" + iPoint.index + "-" + currPoint.index, iPoint.index, peak1, currPoint.index, peak2, Color.AliceBlue, 2);
                                IndicatorArea.DrawTrendLine("Bearish_unconfirm1" + iPoint.index + "-" + currPoint.index, iPoint.index, iPoint.value, currPoint.index, currPoint.value, Color.AliceBlue, 3);
                            }
                        }
                    }
                }

            }
        }

        public bool isDivergencePeak(Point startPoint, Point endPoint)
        {
            double open1 = Bars.OpenPrices[startPoint.index];
            double close1 = Bars.ClosePrices[startPoint.index];
            var peak1 = Math.Max(open1, close1);
            var open2 = Bars.OpenPrices[endPoint.index];
            var close2 = Bars.ClosePrices[endPoint.index];
            var peak2 = Math.Max(open2, close2);

            if (peak1 < peak2)
                return true;
            return false;
        }

        public bool isDivergenceTrought(Point startPoint, Point endPoint)
        {
            double open1 = Bars.OpenPrices[startPoint.index];
            double close1 = Bars.ClosePrices[startPoint.index];
            var trough1 = Math.Min(open1, close1);
            var open2 = Bars.OpenPrices[endPoint.index];
            var close2 = Bars.ClosePrices[endPoint.index];
            var trough2 = Math.Min(open2, close2);

            if (trough1 > trough2)
                return true;
            return false;
        }

        public bool isBrokenLine(ArrayList array, int startIndex, int endIndex, bool isPeak)
        {
            bool isBrokenLine = false;
            if (endIndex - startIndex == 1)
                return false;
            double b, w;
            findLine((Point)array[startIndex], (Point)array[endIndex], out b, out w);

            for (int i = startIndex + 1; i < endIndex; i++)
            {
                Point iPoint = (Point)array[i];
                var value = b * iPoint.index + w;
                if (isPeak)
                {
                    if (value < iPoint.value)
                    {
                        isBrokenLine = true;
                        break;
                    }
                } else
                {
                    if (value > iPoint.value)
                    {
                        isBrokenLine = true;
                        break;
                    }
                }
                
            }
            return isBrokenLine;
        }

        public void findLine(Point point1, Point point2, out double b, out double w)
        {
            b = (point2.value - point1.value) / (point2.index - point1.index);
            w = point1.value - b * point1.index;
        }

        public bool isOverBought(int index)
        {
            if (rsi.Result[index] >= 70)
                return true;
            return false;
        }

        public bool isOverSold(int index)
        {
            if (rsi.Result[index] <= 30)
                return true;
            return false;
        }


    }
}
