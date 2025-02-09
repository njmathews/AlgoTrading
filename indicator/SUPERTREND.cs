#region Using declarations
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
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class SUPERTREND : Indicator
	{
		private SMA sma;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Tracks market direction and provides dynamic support/resistance levels.";
				Name										= "SUPERTREND";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				AddPlot(Brushes.Green, "Support");
                AddPlot(Brushes.Red, "Resistance");
			}
			 else if (State == State.DataLoaded)
            {
                sma = SMA(Period);
            }
			
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < Period)
                return;

            if (Close[0] > sma[0])
            {
                Values[0][0] = sma[0]; // Support
                Values[1][0] = double.NaN; // No Resistance
            }
            else
            {
                Values[0][0] = double.NaN; // No Support
                Values[1][0] = sma[0]; // Resistance
            }
		}
		 [Range(1, int.MaxValue), NinjaScriptProperty]
        public int Period { get; set; } = 14;
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private SUPERTREND[] cacheSUPERTREND;
		public SUPERTREND SUPERTREND(int period)
		{
			return SUPERTREND(Input, period);
		}

		public SUPERTREND SUPERTREND(ISeries<double> input, int period)
		{
			if (cacheSUPERTREND != null)
				for (int idx = 0; idx < cacheSUPERTREND.Length; idx++)
					if (cacheSUPERTREND[idx] != null && cacheSUPERTREND[idx].Period == period && cacheSUPERTREND[idx].EqualsInput(input))
						return cacheSUPERTREND[idx];
			return CacheIndicator<SUPERTREND>(new SUPERTREND(){ Period = period }, input, ref cacheSUPERTREND);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.SUPERTREND SUPERTREND(int period)
		{
			return indicator.SUPERTREND(Input, period);
		}

		public Indicators.SUPERTREND SUPERTREND(ISeries<double> input , int period)
		{
			return indicator.SUPERTREND(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.SUPERTREND SUPERTREND(int period)
		{
			return indicator.SUPERTREND(Input, period);
		}

		public Indicators.SUPERTREND SUPERTREND(ISeries<double> input , int period)
		{
			return indicator.SUPERTREND(input, period);
		}
	}
}

#endregion
