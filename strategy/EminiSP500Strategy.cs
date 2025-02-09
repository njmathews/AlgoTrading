

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Strategies;

namespace NinjaTrader.Custom.Strategies
{
    public class EminiSP500Strategy : Strategy
    {
        private EMA emaShort;
        private EMA emaLong;
        private MACD macd;
        private RSI rsi;
        private ATR atr;
        private SUPERTREND superTrend;

        // Parameters
        private int quantity = 1; // Default quantity to purchase
        private double stopLossPrice = 0;
        private double mae = 0; // Maximum Adverse Excursion
        private double mfe = 0; // Maximum Favorable Excursion
        private double livePnl = 0; // Live Profit and Loss
		private double entryPrice = 0;
		private int entryBar = 0;
		private bool isTradeActive = false;
        // Daily trade tracking
        private Dictionary<DateTime, (int trades, double pnl)> dailyStats;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "E-mini S&P 500 trading strategy with trend and reversal indicators.";
                Name = "EminiSP500Strategy";
                Calculate = Calculate.OnEachTick;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                IsFillLimitOnTouch = false;
                Slippage = 1;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Day;
                OrderFillResolution = OrderFillResolution.Standard;
                StopTargetHandling = StopTargetHandling.ByStrategyPosition;
                IsInstantiatedOnEachOptimizationIteration = false;
            }
            else if (State == State.Configure)
            {
                AddDataSeries(Data.BarsPeriodType.Minute, 1);

                emaShort = EMA(9);
                emaLong = EMA(21);
                macd = MACD(12, 26, 9);
                rsi = RSI(14, 3);
                atr = ATR(14);
                superTrend = SUPERTREND(14);

                dailyStats = new Dictionary<DateTime, (int trades, double pnl)>();
            }
        }

        protected override void OnBarUpdate()
        {
            // Ensure we have enough bars to calculate indicators
            if (CurrentBar < Math.Max(emaLong.Period, atr.Period))
                return;

            // Define trend conditions
            bool isBullishTrend = emaShort[0] > emaLong[0] && macd.Diff[0] > 0 && rsi[0] > 50 && superTrend[0] > Close[0];
            bool isBearishReversal = Close[0] > Bollinger(2, 14).Upper[0] || RSI(7, 3)[0] > 70;
            bool isTrendShiftNegative = emaShort[0] < emaLong[0] && macd.Diff[0] < 0;

            // Entry logic: Buy when bullish trend is confirmed
            if (isBullishTrend && Position.MarketPosition == MarketPosition.Flat)
            {
                EnterLong(quantity, "BullishEntry");

                // Set initial stop loss at ATR-based level
                stopLossPrice = Close[0] - (atr[0] * 4);
                SetStopLoss(CalculationMode.Price, stopLossPrice);
            }

            // Adjust stop loss to breakeven when profitable
            if (Position.MarketPosition == MarketPosition.Long && Close[0] > Position.AveragePrice + (atr[0] * 1.5))
            {
                stopLossPrice = Position.AveragePrice; // Move stop loss to breakeven
                SetStopLoss(CalculationMode.Price, stopLossPrice);
            }

            // Exit logic: Exit on bearish reversal or trend shift
            if (Position.MarketPosition == MarketPosition.Long && (isBearishReversal || isTrendShiftNegative))
            {
                ExitLong(quantity, "ExitLong", "BullishEntry");
            }

            // Calculate MAE, MFE, and live PnL during an open position
            if (Position.MarketPosition == MarketPosition.Long)
            {
                double unrealizedPnl = Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency);
                livePnl = unrealizedPnl;
                mae = Math.Min(mae, unrealizedPnl);
                mfe = Math.Max(mfe, unrealizedPnl);
            }
			
			// Update background color based on trade status
			if (Position.MarketPosition == MarketPosition.Long)
			{
				isTradeActive = true;
				BackBrush = Brushes.White;
			}
			else
			{
				isTradeActive = false;
				BackBrush = null; // Default chart color
			}
		}

        protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)	
        {
			
			// On Buy Entry
	        if (execution.Order.OrderAction == OrderAction.Buy)
	        {
	            entryPrice = price;
	            entryBar = CurrentBar;
	            Draw.ArrowUp(this, "BuyArrow" + CurrentBar, false, 0, price, Brushes.Blue);
	        }

	        // On Sell Exit
	        if (execution.Order.OrderAction == OrderAction.Sell)
	        {
	            double exitPrice = price;
	            double pnl = (exitPrice - entryPrice) * quantity * Instrument.MasterInstrument.PointValue;
	
	            // Draw line from entry to exit
	            Draw.Line(this, "TradeLine" + CurrentBar, false, CurrentBar - entryBar, entryPrice, 0, exitPrice, Brushes.Gray, DashStyleHelper.Solid, 2);
	
	            // Draw symbol based on PnL
	            if (pnl < 0)
	            {
	                Draw.Square(this, "LossSquare" + CurrentBar, false, 0, exitPrice, Brushes.Red);
	            }
	            else
	            {
	                Draw.Diamond(this, "ProfitDiamond" + CurrentBar, false, 0, exitPrice, Brushes.Green);
	            }
	
	            // Draw PnL text above the bar
	            Draw.Text(this, "PnLText" + CurrentBar, $"PnL: {pnl:C2}", 0, High[0] + 4 * TickSize, Brushes.Black);
	        }
			//------------------
            if (execution.Order != null && execution.Order.OrderState == OrderState.Filled)
            {
                DateTime today = Time[0].Date;

                // Track daily trade count and PnL
                if (!dailyStats.ContainsKey(today))
                {
                    dailyStats[today] = (0, 0);
                }

                var stats = dailyStats[today];
				double realizedPnl =0;
                //double realizedPnl = execution.get.Order...GetProfitLoss(PerformanceUnit.Currency);
				if (SystemPerformance.AllTrades.Count > 0)
		        {
		            Trade lastTrade = SystemPerformance.AllTrades[SystemPerformance.AllTrades.Count - 1];
		            realizedPnl = lastTrade.ProfitCurrency;
		            Print($"Last Trade PnL: {realizedPnl}");
		        }
                dailyStats[today] = (stats.trades + 1, stats.pnl + realizedPnl);
            }
        }

        protected override void OnRender(NinjaTrader.Gui.Chart.ChartControl chartControl, NinjaTrader.Gui.Chart.ChartScale chartScale)
        {
            base.OnRender(chartControl, chartScale);

            // Display daily stats at the bottom corner
            string dailyStatsText = "";
            foreach (var entry in dailyStats)
            {
                dailyStatsText += $"{entry.Key:yyyy-MM-dd}: Trades: {entry.Value.trades}, PnL: {entry.Value.pnl:C}\n";
            }
            Draw.TextFixed(this, "DailyStats", dailyStatsText, TextPosition.BottomLeft, Brushes.Blue, null, null, null,10);

            // Display live stats during open positions
            if (Position.MarketPosition == MarketPosition.Long)
            {
                string liveStatsText = $"MAE: {mae:C}, MFE: {mfe:C}, Live PnL: {livePnl:C}";
                Draw.TextFixed(this, "LiveStats", liveStatsText, TextPosition.TopRight, Brushes.Red, null, null,null, 10);
            }
        }
    }
}
