using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;
using System.Collections;

namespace cAlgo
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class BitQRSISignal : Indicator
    {
        [Parameter()]
        public DataSeries Source { get; set; }
        [Parameter()]
        public int periods { get; set; }
        [Parameter("isBot", DefaultValue = false)]
        public bool isBot { get; set; }
        [Output("Main")]
        public IndicatorDataSeries Result { get; set; }

        private RelativeStrengthIndex rsi;
        private bool isLastPeak = false;
        private ArrayList peakList = new ArrayList();
        private ArrayList troughtList = new ArrayList();
        public ArrayList RSIDiv = new ArrayList();
        public int currIndex = 0;

        protected override void Initialize()
        {
            // Initialize and create nested indicators
            rsi = Indicators.RelativeStrengthIndex(Source, periods);
            if (!isBot)
            {
                IndicatorArea.DrawHorizontalLine("70", 70, Color.IndianRed, 1, LineStyle.LinesDots);
                IndicatorArea.DrawHorizontalLine("30", 30, Color.IndianRed, 1, LineStyle.LinesDots);
            }
        }

        public override void Calculate(int index)
        {
            if(currIndex < index)
            {
                currIndex = index;
            } else
            {
                return;
            }
            // Calculate value at specified index
            // Result[index] = ...
            Result[index] = rsi.Result[index];
            Print("index=" + index + "; rsi="+ rsi.Result[index]);
            //if (index >= 1029 && index <= 1037)
            //{
            findPeakTrough(index);
            //}
        }

        public ArrayList getRSIDivData()
        {
            return RSIDiv;
        }


        public void findPeakTrough(int index)
        {
            if (index >= 3)
            {

                if (isOverBought(index - 1))
                {
                    if (Result[index - 2] < Result[index - 1] && Result[index - 1] > Result[index])
                    {
                        if (!isBot)
                        {
                            IndicatorArea.DrawIcon("RSI_peak_" + (index - 1).ToString(), ChartIconType.Circle, index - 1, Result[index - 1], Color.Cyan);
                            IndicatorArea.DrawText("RSI_peak_x" + (index - 1).ToString(), (index - 1).ToString(), index - 1, Result[index - 1], Color.Cyan);
                        }
                        var point = new Utils.Base.Point(index - 1, Result[index - 1], Bars.OpenTimes.Last(index - 1));
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
                        if (!isBot)
                        {
                            IndicatorArea.DrawIcon("RSI_trought_" + (index - 1).ToString(), ChartIconType.Circle, index - 1, Result[index - 1], Color.Blue);
                            IndicatorArea.DrawText("RSI_trought_t_" + (index - 1).ToString(), (index - 1).ToString(), index - 1, Result[index - 1], Color.Blue);
                        }
                        var point = new Utils.Base.Point(index - 1, Result[index - 1], Bars.OpenTimes.Last(index - 1));
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
                Utils.Base.Point currPoint = (Utils.Base.Point)troughtList[troughtList.Count - 1];
                for (int i = 0; i < troughtList.Count; i++)
                {
                    Utils.Base.Point iPoint = (Utils.Base.Point)troughtList[i];
                    if (iPoint.yValue < currPoint.yValue)
                    {
                        if (!isBrokenLine(troughtList, i, troughtList.Count - 1, false))
                        {
                            if (isDivergenceTrought(iPoint, currPoint))
                            {
                                double open1 = Bars.OpenPrices[iPoint.barIndex];
                                double close1 = Bars.ClosePrices[iPoint.barIndex];
                                var trought1 = Math.Min(open1, close1);
                                var open2 = Bars.OpenPrices[currPoint.barIndex];
                                var close2 = Bars.ClosePrices[currPoint.barIndex];
                                var trought2 = Math.Min(open2, close2);
                                if (!isBot)
                                {
                                    Chart.DrawTrendLine("Bullish_unconfirm1" + iPoint.barIndex + "-" + currPoint.barIndex, iPoint.barIndex, trought1, currPoint.barIndex, trought2, Color.Red, 2);
                                    IndicatorArea.DrawTrendLine("Bullish_unconfirm1" + iPoint.barIndex + "-" + currPoint.barIndex, iPoint.barIndex, iPoint.yValue, currPoint.barIndex, currPoint.yValue, Color.Red, 3);
                                }
                                // Add to chart data;
                                Utils.Base.RsiData rsiEle = new Utils.Base.RsiData(iPoint, currPoint, Utils.Base.RSI_TYPE.BULLISH);
                                RSIDiv.Add(rsiEle);
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
                Utils.Base.Point currPoint = (Utils.Base.Point)peakList[peakList.Count - 1];
                for (int i = 0; i < peakList.Count; i++)
                {
                    Utils.Base.Point iPoint = (Utils.Base.Point)peakList[i];
                    if (iPoint.yValue > currPoint.yValue)
                    {
                        if (!isBrokenLine(peakList, i, peakList.Count - 1, true))
                        {
                            if (isDivergencePeak(iPoint, currPoint))
                            {
                                double open1 = Bars.OpenPrices[iPoint.barIndex];
                                double close1 = Bars.ClosePrices[iPoint.barIndex];
                                var peak1 = Math.Max(open1, close1);
                                var open2 = Bars.OpenPrices[currPoint.barIndex];
                                var close2 = Bars.ClosePrices[currPoint.barIndex];
                                var peak2 = Math.Max(open2, close2);
                                if (!isBot)
                                {
                                    Chart.DrawTrendLine("Bearish_unconfirm1" + iPoint.barIndex + "-" + currPoint.barIndex, iPoint.barIndex, peak1, currPoint.barIndex, peak2, Color.AliceBlue, 2);
                                    IndicatorArea.DrawTrendLine("Bearish_unconfirm1" + iPoint.barIndex + "-" + currPoint.barIndex, iPoint.barIndex, iPoint.yValue, currPoint.barIndex, currPoint.yValue, Color.AliceBlue, 3);
                                }
                                // Add to chart data;
                                Utils.Base.RsiData rsiEle = new Utils.Base.RsiData(iPoint, currPoint, Utils.Base.RSI_TYPE.BEARISH);
                                RSIDiv.Add(rsiEle);
                            }
                        }
                    }
                }

            }
        }

        public bool isDivergencePeak(Utils.Base.Point startPoint, Utils.Base.Point endPoint)
        {
            double open1 = Bars.OpenPrices[startPoint.barIndex];
            double close1 = Bars.ClosePrices[startPoint.barIndex];
            var peak1 = Math.Max(open1, close1);
            var open2 = Bars.OpenPrices[endPoint.barIndex];
            var close2 = Bars.ClosePrices[endPoint.barIndex];
            var peak2 = Math.Max(open2, close2);

            if (peak1 < peak2)
                return true;
            return false;
        }

        public bool isDivergenceTrought(Utils.Base.Point startPoint, Utils.Base.Point endPoint)
        {
            double open1 = Bars.OpenPrices[startPoint.barIndex];
            double close1 = Bars.ClosePrices[startPoint.barIndex];
            var trough1 = Math.Min(open1, close1);
            var open2 = Bars.OpenPrices[endPoint.barIndex];
            var close2 = Bars.ClosePrices[endPoint.barIndex];
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
            var utils = new Utils.Base();
            utils.findLine((Utils.Base.Point)array[startIndex], (Utils.Base.Point)array[endIndex], out b, out w);

            for (int i = startIndex + 1; i < endIndex; i++)
            {
                Utils.Base.Point iPoint = (Utils.Base.Point)array[i];
                var value = b * iPoint.barIndex + w;
                if (isPeak)
                {
                    if (value < iPoint.yValue)
                    {
                        isBrokenLine = true;
                        break;
                    }
                }
                else
                {
                    if (value > iPoint.yValue)
                    {
                        isBrokenLine = true;
                        break;
                    }
                }

            }
            return isBrokenLine;
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
