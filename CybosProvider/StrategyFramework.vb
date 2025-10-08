Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing

' ===============================
' TradeSignal & Strategy contracts
' ===============================
Public Enum EvaluationMode
    OnBarClose = 0
    OnTick = 1
End Enum

Public Enum EntryTiming
    NextBarOpen = 0
End Enum

Public Enum TradeSignalType
    Buy = 0
    Sell = 1
End Enum

Public Class TradeSignal
    Public Property Index As Integer
    Public Property Time As DateTime
    Public Property Price As Double
    Public Property Side As TradeSignalType
    Public Property Reason As String
End Class

Public Interface ITradeStrategy
    ReadOnly Property Name As String
    ReadOnly Property Mode As EvaluationMode
    Function GetRequiredIndicators() As List(Of IndicatorInstance)
    Function EvaluateOne(candles As List(Of CandleInfo), iCloseBar As Integer) As List(Of TradeSignal)
End Interface

' ===============================
' StrategyEngine (OnBarClose, NextBarOpen)
' ===============================
Public Class StrategyEngine
    Public Property EntryRule As EntryTiming = EntryTiming.NextBarOpen
    Public Property Slippage As Double = 0.0 ' points
    Public Property FeeRate As Double = 0.0  ' e.g., 0.00015

    Private ReadOnly _exec As New ExecutionSimulator()

    Public Function Run(strategy As ITradeStrategy, candles As List(Of CandleInfo)) As List(Of TradeSignal)
        Dim signals As New List(Of TradeSignal)
        If candles Is Nothing OrElse candles.Count = 0 Then Return signals

        Dim i As Integer = 0
        Select Case strategy.Mode
            Case EvaluationMode.OnBarClose
                For i = 1 To candles.Count - 2 ' close bar i, fill at i+1 open
                    Dim s = strategy.EvaluateOne(candles, i)
                    If s IsNot Nothing AndAlso s.Count > 0 Then
                        signals.AddRange(s)
                    End If
                Next
            Case EvaluationMode.OnTick
                ' Not used for now
        End Select

        ' Normalize fill prices (NextBarOpen)
        Dim filled As New List(Of TradeSignal)
        For Each s In signals
            Dim fillIdx As Integer = Math.Min(s.Index + 1, candles.Count - 1)
            Dim price As Double = candles(fillIdx).Open
            If s.Side = TradeSignalType.Buy Then
                price += Slippage
            Else
                price -= Slippage
            End If
            filled.Add(New TradeSignal With {
                .Index = fillIdx,
                .Time = candles(fillIdx).Timestamp,
                .Price = price,
                .Side = s.Side,
                .Reason = s.Reason & " / NextOpen Fill"
            })
        Next
        Return filled
    End Function
End Class

' ===============================
' ExecutionSimulator (minimal)
' ===============================
Public Class ExecutionSimulator
    ' Placeholder for future market/limit/partial fill logic
End Class

' ===============================
' TradeSignalIndicator (scatter-like rendering via indicator series)
' ===============================
Public Class TradeSignalIndicator
    Implements IIndicator

    Private ReadOnly _buys As HashSet(Of Integer)
    Private ReadOnly _sells As HashSet(Of Integer)
    Private _meta As List(Of SeriesMetadata)

    Public Sub New(buyIndexes As IEnumerable(Of Integer), sellIndexes As IEnumerable(Of Integer))
        _buys = New HashSet(Of Integer)(buyIndexes)
        _sells = New HashSet(Of Integer)(sellIndexes)
        _meta = New List(Of SeriesMetadata) From {
            New SeriesMetadata With {.Name = "BUY", .DisplayMode = ChartDisplayMode.Overlay, .ChartType = ChartType.Scatter,
                                     .Color = Color.Lime, .LineWidth = 1.0F, .PanelIndex = 0},
            New SeriesMetadata With {.Name = "SELL", .DisplayMode = ChartDisplayMode.Overlay, .ChartType = ChartType.Scatter,
                                     .Color = Color.Red, .LineWidth = 1.0F, .PanelIndex = 0}
        }
    End Sub

    Public ReadOnly Property Name As String Implements IIndicator.Name
        Get
            Return "TRADE_SIGNALS"
        End Get
    End Property

    Public ReadOnly Property Description As String Implements IIndicator.Description
        Get
            Return "Buy/Sell markers as scatter points"
        End Get
    End Property

    Public Function Calculate(candles As List(Of CandleInfo)) As Dictionary(Of String, List(Of Double?)) Implements IIndicator.Calculate
        Dim buySeries As New List(Of Double?)(New Double?(candles.Count - 1) {})
        Dim sellSeries As New List(Of Double?)(New Double?(candles.Count - 1) {})
        For i = 0 To candles.Count - 1
            If _buys.Contains(i) Then
                buySeries(i) = Math.Min(candles(i).Open, candles(i).Close)
            End If
            If _sells.Contains(i) Then
                sellSeries(i) = Math.Max(candles(i).Open, candles(i).Close)
            End If
        Next
        Return New Dictionary(Of String, List(Of Double?)) From {
            {"BUY", buySeries}, {"SELL", sellSeries}
        }
    End Function

    Public Function GetSeriesMetadata() As List(Of SeriesMetadata) Implements IIndicator.GetSeriesMetadata
        Return _meta
    End Function
End Class

' ===============================
' EMA indicator (parametric)
' ===============================
Public Class EMAIndicator
    Implements IIndicator
    Private ReadOnly _period As Integer
    Private ReadOnly _color As Color
    Private _cached As List(Of SeriesMetadata)

    Public Sub New(period As Integer, Optional color As Color = Nothing)
        _period = period
        _color = If(color.IsEmpty, Color.Cyan, color)
    End Sub

    Public ReadOnly Property Name As String Implements IIndicator.Name
        Get
            Return $"EMA({_period})"
        End Get
    End Property

    Public ReadOnly Property Description As String Implements IIndicator.Description
        Get
            Return "Exponential Moving Average"
        End Get
    End Property

    Public Function Calculate(candles As List(Of CandleInfo)) As Dictionary(Of String, List(Of Double?)) Implements IIndicator.Calculate
        Dim closes = New List(Of Double)(candles.Count)
        For Each c In candles : closes.Add(c.Close) : Next
        Dim series = IndicatorMath.EMA(closes, _period)
        Return New Dictionary(Of String, List(Of Double?)) From {{Name, series}}
    End Function

    Public Function GetSeriesMetadata() As List(Of SeriesMetadata) Implements IIndicator.GetSeriesMetadata
        If _cached Is Nothing Then
            _cached = New List(Of SeriesMetadata) From {
                New SeriesMetadata With {.Name = Name, .DisplayMode = ChartDisplayMode.Overlay, .ChartType = ChartType.Line,
                                         .Color = _color, .LineWidth = 2.0F, .PanelIndex = 0}
            }
        End If
        Return _cached
    End Function
End Class

' ===============================
' IndicatorMath (EMA/RSI helpers)
' ===============================
Public Module IndicatorMath
    Public Function EMA(values As IList(Of Double), period As Integer) As List(Of Double?)
        Dim out As New List(Of Double?)(Enumerable.Repeat(CType(Nothing, Double?), values.Count))
        If values.Count = 0 OrElse period <= 1 Then Return out
        Dim k As Double = 2.0 / (period + 1.0)
        Dim emaPrev As Double = 0
        Dim cnt As Integer = 0
        For i = 0 To values.Count - 1
            Dim v = values(i)
            cnt += 1
            If cnt = period Then
                Dim sma As Double = 0
                For j = i - period + 1 To i : sma += values(j) : Next
                sma /= period
                emaPrev = sma
                out(i) = emaPrev
            ElseIf cnt > period Then
                emaPrev = v * k + emaPrev * (1 - k)
                out(i) = emaPrev
            End If
        Next
        Return out
    End Function

    Public Function RSI(values As IList(Of Double), period As Integer) As List(Of Double?)
        Dim out As New List(Of Double?)(Enumerable.Repeat(CType(Nothing, Double?), values.Count))
        If values.Count < 2 OrElse period <= 1 Then Return out

        Dim gains As Double = 0, losses As Double = 0
        For i = 1 To values.Count - 1
            Dim change = values(i) - values(i - 1)
            If i <= period Then
                If change > 0 Then gains += change Else losses -= change
                If i = period Then
                    Dim avgGain = gains / period, avgLoss = losses / period
                    If avgLoss = 0 Then
                        out(i) = 100
                    Else
                        Dim rs = avgGain / avgLoss
                        out(i) = 100 - 100 / (1 + rs)
                    End If
                End If
            Else
                Dim gain = If(change > 0, change, 0)
                Dim loss = If(change < 0, -change, 0)
                gains = (gains * (period - 1) + gain) / period
                losses = (losses * (period - 1) + loss) / period
                If losses = 0 Then
                    out(i) = 100
                Else
                    Dim rs = gains / losses
                    out(i) = 100 - 100 / (1 + rs)
                End If
            End If
        Next
        Return out
    End Function
End Module

' ===============================
' Example Strategy: M1 EMA200 + TickIntensity + RSI7 + MACD
' ===============================
Public Class M1_Ema200_Tick_Rsi7_Macd_Strategy
    Implements ITradeStrategy

    Public ReadOnly Property Name As String Implements ITradeStrategy.Name
        Get
            Return "M1 EMA200 + TickIntensity + RSI7 + MACD"
        End Get
    End Property

    Public ReadOnly Property Mode As EvaluationMode Implements ITradeStrategy.Mode
        Get
            Return EvaluationMode.OnBarClose
        End Get
    End Property

    Public Function GetRequiredIndicators() As List(Of IndicatorInstance) Implements ITradeStrategy.GetRequiredIndicators
        Dim list As New List(Of IndicatorInstance)
        list.Add(New IndicatorInstance With {.IndicatorType = GetType(EMAIndicator), .Name = "EMA(200)",
                                             .Parameters = New Dictionary(Of String, Object) From {{"period", 200}}})
        list.Add(New IndicatorInstance With {.IndicatorType = GetType(SMAIndicator), .Name = "SMA(5)",
                                             .Parameters = New Dictionary(Of String, Object) From {{"period", 5}}})
        list.Add(New IndicatorInstance With {.IndicatorType = GetType(SMAIndicator), .Name = "SMA(20)",
                                             .Parameters = New Dictionary(Of String, Object) From {{"period", 20}}})
        list.Add(New IndicatorInstance With {.IndicatorType = GetType(RSIIndicator), .Name = "RSI(7)",
                                             .Parameters = New Dictionary(Of String, Object) From {{"period", 7}}})
        list.Add(New IndicatorInstance With {.IndicatorType = GetType(MACDIndicator), .Name = "MACD(12,26,9)"})
        list.Add(New IndicatorInstance With {.IndicatorType = GetType(TickIntensityIndicator), .Name = "틱강도"})
        Return list
    End Function

    Public Function EvaluateOne(candles As List(Of CandleInfo), iCloseBar As Integer) As List(Of TradeSignal) _
        Implements ITradeStrategy.EvaluateOne

        Dim s As New List(Of TradeSignal)
        If iCloseBar < 200 OrElse iCloseBar >= candles.Count - 1 Then Return s

        ' Pull series from CandleInfo (assumed to be pre-calculated or dynamic)
        Dim c As CandleInfo = candles(iCloseBar)
        Dim ema200 As Double = GetEMA(candles, iCloseBar, 200)
        Dim rsi7 As Double = GetRSI(candles, iCloseBar, 7)

        Dim tfMin As Integer = GetTfMinutes(candles(0))
        Dim tickPerMin As Double = c.tickCount / Math.Max(1, tfMin)
        Dim avg20PerMin As Double = c.tickAvg20 / Math.Max(1, tfMin)
        Dim intensityRatio As Double = 0
        If avg20PerMin > 0 Then intensityRatio = (tickPerMin / avg20PerMin) * 10.0

        Dim bullishTickMA As Boolean = (c.tickAvg5 > c.tickAvg20 AndAlso c.tickAvg20 > c.tickAvg60)

        Dim condPriceEma As Boolean = (c.Close >= ema200)
        Dim condSmaCross As Boolean = (c.sma5 > c.sma20)
        Dim condTickIntensity As Boolean = (intensityRatio >= 20)
        Dim condTickPerMin As Boolean = (tickPerMin >= 20)
        Dim condRsiCrossUp As Boolean = (GetRSI(candles, iCloseBar - 1, 7) <= 50 AndAlso rsi7 > 50)
        Dim condMacdAbove As Boolean = (c.macd > c.macdSignal)

        Static inPos As Boolean = False
        If Not inPos Then
            If condPriceEma AndAlso condSmaCross AndAlso condTickIntensity AndAlso bullishTickMA AndAlso _
               condTickPerMin AndAlso condRsiCrossUp AndAlso condMacdAbove Then
                inPos = True
                s.Add(New TradeSignal With {.Index = iCloseBar, .Time = c.Timestamp, .Price = c.Close, .Side = TradeSignalType.Buy,
                                            .Reason = "ALL CONDS MET"})
            End If
        Else
            Dim exitCond As Boolean = (c.sma5 < c.sma20) OrElse (rsi7 < 50) OrElse (c.macd < c.macdSignal) OrElse (c.Close < ema200)
            If exitCond Then
                inPos = False
                s.Add(New TradeSignal With {.Index = iCloseBar, .Time = c.Timestamp, .Price = c.Close, .Side = TradeSignalType.Sell,
                                            .Reason = "COND BREAK"})
            End If
        End If
        Return s
    End Function

    Private Function GetTfMinutes(c As CandleInfo) As Integer
        If c Is Nothing OrElse String.IsNullOrEmpty(c.Timeframe) Then Return 1
        If c.Timeframe.StartsWith("m") Then
            Dim n As Integer
            If Integer.TryParse(c.Timeframe.Substring(1), n) Then Return Math.Max(1, n)
        End If
        Return 1
    End Function

    Private Function GetEMA(candles As List(Of CandleInfo), i As Integer, period As Integer) As Double
        ' On-demand compute using closes up to i
        Dim closes As New List(Of Double)
        For k = 0 To i : closes.Add(candles(k).Close) : Next
        Dim arr = IndicatorMath.EMA(closes, period)
        If i < arr.Count AndAlso arr(i).HasValue Then Return arr(i).Value
        Return closes(i)
    End Function

    Private Function GetRSI(candles As List(Of CandleInfo), i As Integer, period As Integer) As Double
        If i <= 0 Then Return 50
        Dim closes As New List(Of Double)
        For k = 0 To i : closes.Add(candles(k).Close) : Next
        Dim arr = IndicatorMath.RSI(closes, period)
        If i < arr.Count AndAlso arr(i).HasValue Then Return arr(i).Value
        Return 50
    End Function
End Class
