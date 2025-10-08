'Imports System.Drawing
'Imports System.Drawing.Drawing2D
'Imports System.ComponentModel

'#Region "charts"

'Public Enum ChartDisplayMode
'    Overlay
'    Separate
'End Enum

'Public Enum ChartType
'    Line
'    Candlestick
'    Bar
'    Area
'    Histogram
'    Scatter
'End Enum

'Public Class AxisInfo
'    Public Property Title As String
'    Public Property Min As Double?
'    Public Property Max As Double?
'    Public Property AutoScale As Boolean = True
'    Public Property GridLines As Boolean = True
'    Public Property GridColor As Color = Color.FromArgb(30, 128, 128, 128)
'    Public Property LabelFormat As String = "F2"
'    Public Property Position As AxisPosition = AxisPosition.Left
'End Class

'Public Enum AxisPosition
'    Left
'    Right
'    Top
'    Bottom
'End Enum

'Public Class SeriesMetadata
'    Public Property Name As String
'    Public Property DisplayMode As ChartDisplayMode = ChartDisplayMode.Overlay
'    Public Property ChartType As ChartType = ChartType.Line
'    Public Property Color As Color = Color.Blue
'    Public Property LineStyle As DashStyle = DashStyle.Solid
'    Public Property LineWidth As Single = 2.0F
'    Public Property FillColor As Color = Color.Transparent
'    Public Property FillOpacity As Integer = 50
'    Public Property Visible As Boolean = True
'    Public Property ZOrder As Integer = 0
'    Public Property ReferenceLine As Double? = Nothing
'    Public Property ReferenceLineColor As Color = Color.White
'    Public Property ReferenceLineStyle As DashStyle = DashStyle.Dash

'    Public Property ReferenceLines As List(Of ReferenceLine)

'    Public Property AxisInfo As AxisInfo
'    Public Property PanelIndex As Integer = 0
'    Public Property DataSelector As Func(Of CandleInfo, Double?)

'    Public Property OverboughtLevel As Double? = Nothing
'    Public Property OversoldLevel As Double? = Nothing
'    Public Property EnableZones As Boolean = False

'    Public Property EnableDivergence As Boolean = False

'    Public Sub New()
'        AxisInfo = New AxisInfo()
'        ReferenceLines = New List(Of ReferenceLine)
'    End Sub
'End Class

'Public Class ChartPanel
'    Public Property Index As Integer
'    Public Property Title As String
'    Public Property HeightRatio As Double = 1.0
'    Public Property BackColor As Color = Color.Black
'    Public Property Series As New List(Of SeriesMetadata)
'    Public Property YAxis As New AxisInfo()
'End Class

'#End Region

'#Region "indicators"

'Public Interface IIndicator
'    ReadOnly Property Name As String
'    ReadOnly Property Description As String
'    Function Calculate(candles As List(Of CandleInfo)) As Dictionary(Of String, List(Of Double?))
'    Function GetSeriesMetadata() As List(Of SeriesMetadata)
'End Interface

'Public Class SMAIndicator
'    Implements IIndicator

'    Private ReadOnly _period As Integer
'    Private ReadOnly _color As Color
'    Private _cachedMetadata As SeriesMetadata = Nothing

'    Public Sub New(period As Integer, Optional color As Color = Nothing)
'        _period = period
'        _color = If(color.IsEmpty, Color.Yellow, color)
'    End Sub

'    Public ReadOnly Property Name As String Implements IIndicator.Name
'        Get
'            Return $"SMA({_period})"
'        End Get
'    End Property

'    Public ReadOnly Property Description As String Implements IIndicator.Description
'        Get
'            Return $"{_period}???�순?�동?�균"
'        End Get
'    End Property

'    Public Function Calculate(candles As List(Of CandleInfo)) As Dictionary(Of String, List(Of Double?)) Implements IIndicator.Calculate
'        Dim result As New Dictionary(Of String, List(Of Double?))
'        Dim smaValues As New List(Of Double?)

'        For i = 0 To candles.Count - 1
'            If i >= _period - 1 Then
'                Dim sum = candles.Skip(i - _period + 1).Take(_period).Sum(Function(c) c.Close)
'                smaValues.Add(sum / _period)
'            Else
'                smaValues.Add(Nothing)
'            End If
'        Next

'        result(Name) = smaValues
'        Return result
'    End Function

'    Public Function GetSeriesMetadata() As List(Of SeriesMetadata) Implements IIndicator.GetSeriesMetadata

'        If _cachedMetadata Is Nothing Then
'            _cachedMetadata = New SeriesMetadata With {
'                .Name = Name,
'                .DisplayMode = ChartDisplayMode.Overlay,
'                .ChartType = ChartType.Line,
'                .Color = _color,
'                .LineWidth = 2.0F,
'                .PanelIndex = 0
'            }
'        End If

'        Return New List(Of SeriesMetadata) From {_cachedMetadata}
'    End Function
'End Class

'''' <summary>
'''' -
'''' </summary>
'Public Class RSIIndicator
'    Implements IIndicator

'    Private ReadOnly _period As Integer
'    Private ReadOnly _color As Color
'    Private _cachedMetadata As SeriesMetadata = Nothing

'    Public Sub New(Optional period As Integer = 14, Optional color As Color = Nothing)
'        _period = period
'        _color = If(color.IsEmpty OrElse color = Color.Transparent, Color.Purple, color)
'    End Sub

'    Public ReadOnly Property Name As String Implements IIndicator.Name
'        Get
'            Return $"RSI({_period})"
'        End Get
'    End Property

'    Public ReadOnly Property Description As String Implements IIndicator.Description
'        Get
'            Return $"{_period}???��?강도지??"
'        End Get
'    End Property

'    Public Function Calculate(candles As List(Of CandleInfo)) As Dictionary(Of String, List(Of Double?)) Implements IIndicator.Calculate
'        Dim result As New Dictionary(Of String, List(Of Double?))
'        Dim rsiValues As New List(Of Double?)

'        For i = 0 To candles.Count - 1
'            If i >= _period Then
'                Dim gains As Double = 0
'                Dim losses As Double = 0

'                For j As Integer = i - _period + 1 To i
'                    If j > 0 Then
'                        Dim change = candles(j).Close - candles(j - 1).Close
'                        If change > 0 Then
'                            gains += change
'                        Else
'                            losses += Math.Abs(change)
'                        End If
'                    End If
'                Next

'                Dim avgGain = gains / _period
'                Dim avgLoss = losses / _period

'                If avgLoss = 0 Then
'                    rsiValues.Add(100)
'                Else
'                    Dim rs = avgGain / avgLoss
'                    rsiValues.Add(100 - (100 / (1 + rs)))
'                End If
'            Else
'                rsiValues.Add(Nothing)
'            End If
'        Next

'        result(Name) = rsiValues
'        Return result
'    End Function

'    Public Function GetSeriesMetadata() As List(Of SeriesMetadata) Implements IIndicator.GetSeriesMetadata

'        If _cachedMetadata Is Nothing Then
'            _cachedMetadata = New SeriesMetadata With {
'                .Name = Name,
'                .DisplayMode = ChartDisplayMode.Separate,
'                .ChartType = ChartType.Line,
'                .Color = _color,
'                .LineWidth = 2.0F,
'                .PanelIndex = 1
'            }

'            _cachedMetadata.AxisInfo.Min = 0
'            _cachedMetadata.AxisInfo.Max = 100
'            _cachedMetadata.AxisInfo.AutoScale = False

'            _cachedMetadata.ReferenceLine = 50
'            _cachedMetadata.ReferenceLineColor = Color.Gray
'            _cachedMetadata.ReferenceLineStyle = DashStyle.Dash

'            _cachedMetadata.ReferenceLines.Clear()
'        End If

'        Return New List(Of SeriesMetadata) From {_cachedMetadata}
'    End Function
'End Class

'Public Class MACDIndicator
'    Implements IIndicator

'    Private _cachedMetadataList As List(Of SeriesMetadata) = Nothing

'    Public ReadOnly Property Name As String Implements IIndicator.Name
'        Get
'            Return "MACD"
'        End Get
'    End Property

'    Public ReadOnly Property Description As String Implements IIndicator.Description
'        Get
'            Return "MACD (12,26,9)"
'        End Get
'    End Property

'    Public Function Calculate(candles As List(Of CandleInfo)) As Dictionary(Of String, List(Of Double?)) Implements IIndicator.Calculate
'        Dim result As New Dictionary(Of String, List(Of Double?))
'        Dim macdValues As New List(Of Double?)
'        Dim signalValues As New List(Of Double?)
'        Dim histogramValues As New List(Of Double?)

'        For Each candle In candles
'            macdValues.Add(If(candle.macd <> 0, candle.macd, Nothing))
'            signalValues.Add(If(candle.macdSignal <> 0, candle.macdSignal, Nothing))
'            histogramValues.Add(If(candle.macdHistogram <> 0, candle.macdHistogram, Nothing))
'        Next

'        result("MACD") = macdValues
'        result("Signal") = signalValues
'        result("Histogram") = histogramValues
'        Return result
'    End Function

'    Public Function GetSeriesMetadata() As List(Of SeriesMetadata) Implements IIndicator.GetSeriesMetadata

'        If _cachedMetadataList Is Nothing Then
'            _cachedMetadataList = New List(Of SeriesMetadata) From {
'                New SeriesMetadata With {
'                    .Name = "MACD",
'                    .DisplayMode = ChartDisplayMode.Separate,
'                    .ChartType = ChartType.Line,
'                    .Color = Color.Orange,
'                    .LineWidth = 2.0F,
'                    .PanelIndex = 2,
'                    .ReferenceLine = 0
'                },
'                New SeriesMetadata With {
'                    .Name = "Signal",
'                    .DisplayMode = ChartDisplayMode.Separate,
'                    .ChartType = ChartType.Line,
'                    .Color = Color.Lime,
'                    .LineWidth = 1.0F,
'                    .PanelIndex = 2
'                },
'                New SeriesMetadata With {
'                    .Name = "Histogram",
'                    .DisplayMode = ChartDisplayMode.Separate,
'                    .ChartType = ChartType.Histogram,
'                    .Color = Color.Gray,
'                    .LineWidth = 1.0F,
'                    .PanelIndex = 2
'                }
'            }
'        End If

'        Return _cachedMetadataList
'    End Function
'End Class

'#End Region

'#Region "고성??차트 컨트�?"

'Public Class HighPerformanceChartControl
'    Inherits UserControl

'    Private _candles As List(Of CandleInfo)
'    Private ReadOnly _panels As New List(Of ChartPanel)
'    Private ReadOnly _indicators As New List(Of IIndicator)
'    Private ReadOnly _indicatorData As New Dictionary(Of String, Dictionary(Of String, List(Of Double?)))
'    Private _strategyLabel As String = String.Empty

'    Private _backBuffer As Bitmap
'    Private _visibleStartIndex As Integer = 0
'    Private _visibleEndIndex As Integer = 0
'    Private _candleWidth As Double = 5
'    Private ReadOnly _candleSpacing As Integer = 2

'    Private _isDragging As Boolean = False
'    Private _lastMousePos As Point
'    Private _mousePos As Point
'    Private _showCrosshair As Boolean = False
'    Private _currentPanelIndex As Integer = -1
'    Private _mouseLeftChart As Boolean = True

'    Private _isSimulationMode As Boolean = False
'    Private _simulationIndex As Integer = 0
'    Private _simulationMaxIndex As Integer = 0

'    Private ReadOnly _leftMargin As Integer = 10
'    Private ReadOnly _rightMargin As Integer = 70
'    Private ReadOnly _topMargin As Integer = 20
'    Private ReadOnly _bottomMargin As Integer = 20
'    Private ReadOnly _panelSpacing As Integer = 0

'    Private ReadOnly _backgroundColor As Color = Color.Black
'    Private ReadOnly _gridColor As Color = Color.FromArgb(30, 128, 128, 128)
'    Private ReadOnly _textColor As Color = Color.White
'    Private ReadOnly _bullishColor As Color = Color.Red
'    Private ReadOnly _bearishColor As Color = Color.Green
'    Private ReadOnly _baselineColor As Color = Color.White
'    Private ReadOnly _crosshairColor As Color = Color.Gray

'    Public Sub New()
'        InitializeComponents()
'        Me.DoubleBuffered = True
'        Me.SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer, True)

'        Dim mainPanel As New ChartPanel With {
'            .Index = 0,
'            .Title = "Price",
'            .HeightRatio = 0.7,
'            .BackColor = _backgroundColor
'        }
'        _panels.Add(mainPanel)
'    End Sub

'    Private Sub InitializeComponents()
'        Me.Name = "HighPerformanceChartControl"
'        Me.Size = New Size(800, 600)
'    End Sub

'    ' SetData 메서???�정
'    Public Sub SetData(candles As List(Of CandleInfo))
'        _candles = candles
'        If candles IsNot Nothing AndAlso candles.Count > 0 Then
'            _visibleEndIndex = candles.Count - 1
'            _visibleStartIndex = Math.Max(0, _visibleEndIndex - 100)
'            _simulationMaxIndex = candles.Count - 1
'        End If
'        CalculateAllIndicators()
'        Invalidate()
'    End Sub
'    Public Sub SetStrategyLabel(label As String)
'        Dim normalized As String = If(label, String.Empty).Trim()
'        If _strategyLabel <> normalized Then
'            _strategyLabel = normalized
'            Invalidate()
'        End If
'    End Sub

'    Public Sub AddIndicator(indicator As IIndicator)
'        Dim existing = _indicators.FirstOrDefault(Function(i) i.Name = indicator.Name)
'        If existing Is Nothing Then
'            _indicators.Add(indicator)
'            Dim metadata = indicator.GetSeriesMetadata()
'            For Each meta In metadata
'                If meta.DisplayMode = ChartDisplayMode.Separate Then
'                    EnsurePanelExists(meta.PanelIndex, indicator.Name)
'                End If
'            Next
'            If _candles IsNot Nothing Then
'                CalculateIndicator(indicator)
'                Invalidate()
'            End If
'        End If
'    End Sub

'    Public Sub RemoveIndicator(indicatorName As String)
'        Dim indicator = _indicators.FirstOrDefault(Function(i) i.Name = indicatorName)
'        If indicator IsNot Nothing Then
'            _indicators.Remove(indicator)
'            _indicatorData.Remove(indicatorName)
'            Invalidate()
'        End If
'    End Sub

'    Public Sub ClearIndicators()
'        _indicators.Clear()
'        _indicatorData.Clear()
'        Dim mainPanels = _panels.Where(Function(p) p.Index = 0).ToList()
'        _panels.Clear()
'        For Each p In mainPanels
'            _panels.Add(p)
'        Next
'        Invalidate()
'    End Sub

'    Private Sub EnsurePanelExists(panelIndex As Integer, title As String)
'        Dim panel = _panels.FirstOrDefault(Function(p) p.Index = panelIndex)
'        If panel Is Nothing Then
'            panel = New ChartPanel With {
'                .Index = panelIndex,
'                .Title = title,
'                .HeightRatio = 0.15,
'                .BackColor = _backgroundColor
'            }
'            _panels.Add(panel)
'            Dim sorted = _panels.OrderBy(Function(p) p.Index).ToList()
'            _panels.Clear()
'            For Each p In sorted
'                _panels.Add(p)
'            Next
'            AdjustPanelHeights()
'        End If
'    End Sub

'    Private Sub AdjustPanelHeights()
'        If _panels.Count = 1 Then
'            _panels(0).HeightRatio = 1.0
'        ElseIf _panels.Count = 2 Then
'            _panels(0).HeightRatio = 0.6
'            _panels(1).HeightRatio = 0.4
'        ElseIf _panels.Count = 3 Then
'            _panels(0).HeightRatio = 0.5
'            _panels(1).HeightRatio = 0.25
'            _panels(2).HeightRatio = 0.25
'        ElseIf _panels.Count = 4 Then
'            _panels(0).HeightRatio = 0.4
'            _panels(1).HeightRatio = 0.2
'            _panels(2).HeightRatio = 0.2
'            _panels(3).HeightRatio = 0.2
'        ElseIf _panels.Count > 4 Then
'            _panels(0).HeightRatio = 0.4
'            Dim remaining = 0.6 / (_panels.Count - 1)
'            For i = 1 To _panels.Count - 1
'                _panels(i).HeightRatio = remaining
'            Next
'        End If
'    End Sub

'    Private Sub CalculatePanelYAxisRange(ByVal panel As ChartPanel)
'        If _candles Is Nothing OrElse _candles.Count = 0 Then
'            panel.YAxis.Min = 0
'            panel.YAxis.Max = 100
'            Return
'        End If

'        If panel.Index = 0 Then
'            ' Price Panel
'            Dim visibleCandles = _candles.Skip(_visibleStartIndex).Take(_visibleEndIndex - _visibleStartIndex + 1).ToList()
'            If visibleCandles.Count = 0 Then
'                panel.YAxis.Min = 0
'                panel.YAxis.Max = 100
'                Return
'            End If

'            panel.YAxis.Min = visibleCandles.Min(Function(c) c.Low)
'            panel.YAxis.Max = visibleCandles.Max(Function(c) c.High)
'            If panel.YAxis.Min = panel.YAxis.Max Then
'                panel.YAxis.Min -= 1
'                panel.YAxis.Max += 1
'            End If
'        Else
'            ' Indicator Panel
'            Dim panelSeriesItems As New List(Of Tuple(Of IIndicator, SeriesMetadata))
'            For Each indicator In _indicators
'                If _indicatorData.ContainsKey(indicator.Name) Then
'                    For Each meta In indicator.GetSeriesMetadata()
'                        If meta.DisplayMode = ChartDisplayMode.Separate AndAlso meta.PanelIndex = panel.Index Then
'                            panelSeriesItems.Add(Tuple.Create(indicator, meta))
'                        End If
'                    Next
'                End If
'            Next

'            If panelSeriesItems.Count = 0 Then
'                panel.YAxis.Min = 0
'                panel.YAxis.Max = 100
'                Return
'            End If

'            ' Check for a non-auto-scaling series first (like RSI)
'            Dim nonAutoScaleItem = panelSeriesItems.FirstOrDefault(Function(item) Not item.Item2.AxisInfo.AutoScale AndAlso item.Item2.AxisInfo.Min.HasValue AndAlso item.Item2.AxisInfo.Max.HasValue)

'            If nonAutoScaleItem IsNot Nothing Then
'                panel.YAxis.Min = nonAutoScaleItem.Item2.AxisInfo.Min.Value
'                panel.YAxis.Max = nonAutoScaleItem.Item2.AxisInfo.Max.Value
'                panel.YAxis.AutoScale = False
'            Else
'                ' Calculate from all auto-scaling series on the panel
'                Dim minVal = Double.MaxValue
'                Dim maxVal = Double.MinValue
'                Dim visibleDataExists As Boolean = False

'                For Each item In panelSeriesItems
'                    Dim indicator = item.Item1
'                    Dim meta = item.Item2

'                    If _indicatorData.ContainsKey(indicator.Name) AndAlso _indicatorData(indicator.Name).ContainsKey(meta.Name) Then
'                        Dim data = _indicatorData(indicator.Name)(meta.Name)
'                        Dim visibleData = data.Skip(_visibleStartIndex).Take(_visibleEndIndex - _visibleStartIndex + 1).Where(Function(v) v.HasValue).Select(Function(v) v.Value).ToList()

'                        If visibleData.Count > 0 Then
'                            visibleDataExists = True
'                            minVal = Math.Min(minVal, visibleData.Min())
'                            maxVal = Math.Max(maxVal, visibleData.Max())
'                        End If
'                    End If
'                Next

'                If Not visibleDataExists Then
'                    panel.YAxis.Min = 0
'                    panel.YAxis.Max = 100
'                Else
'                    If minVal = maxVal Then
'                        minVal -= 1
'                        maxVal += 1
'                    Else
'                        ' Add 10% padding
'                        Dim padding = (maxVal - minVal) * 0.1
'                        minVal -= padding
'                        maxVal += padding
'                    End If
'                    panel.YAxis.Min = minVal
'                    panel.YAxis.Max = maxVal
'                End If
'                panel.YAxis.AutoScale = True
'            End If
'        End If
'    End Sub

'    Private Sub CalculateAllIndicators()
'        _indicatorData.Clear()
'        For Each indicator In _indicators
'            CalculateIndicator(indicator)
'        Next
'    End Sub

'    Private Sub CalculateIndicator(indicator As IIndicator)
'        If _candles Is Nothing OrElse _candles.Count = 0 Then Return
'        _indicatorData(indicator.Name) = indicator.Calculate(_candles)
'    End Sub

'    Protected Overrides Sub OnPaint(e As PaintEventArgs)
'        MyBase.OnPaint(e)

'        If _candles Is Nothing OrElse _candles.Count = 0 Then
'            DrawNoDataMessage(e.Graphics)
'            Return
'        End If

'        If _backBuffer Is Nothing OrElse _backBuffer.Width <> Width OrElse _backBuffer.Height <> Height Then
'            _backBuffer?.Dispose()
'            _backBuffer = New Bitmap(Width, Height)
'        End If

'        Using g As Graphics = Graphics.FromImage(_backBuffer)
'            g.SmoothingMode = SmoothingMode.AntiAlias
'            g.TextRenderingHint = Drawing.Text.TextRenderingHint.ClearTypeGridFit
'            g.Clear(_backgroundColor)

'            Dim currentY = 0
'            For i = 0 To _panels.Count - 1
'                Dim panel = _panels(i)
'                Dim panelHeight = CInt((Height - _panelSpacing * (_panels.Count - 1)) * panel.HeightRatio)
'                Dim panelRect As New Rectangle(0, currentY, Width, panelHeight)

'                ' ?�재 ?�널 ?�덱??결정
'                If _mousePos.Y >= panelRect.Top AndAlso _mousePos.Y <= panelRect.Bottom Then
'                    _currentPanelIndex = i
'                End If

'                DrawPanel(g, panel, panelRect, i)
'                currentY += panelHeight + _panelSpacing
'            Next

'            If (_showCrosshair AndAlso Not _mouseLeftChart) OrElse _mouseLeftChart Then
'                DrawCrosshair(g)
'                DrawCrosshairLabels(g)
'            End If
'        End Using

'        e.Graphics.DrawImageUnscaled(_backBuffer, 0, 0)
'    End Sub

'    Private Sub DrawPanel(g As Graphics, panel As ChartPanel, bounds As Rectangle, panelIndex As Integer)
'        CalculatePanelYAxisRange(panel)

'        Using brush As New SolidBrush(panel.BackColor)
'            g.FillRectangle(brush, bounds)
'        End Using

'        Dim currentBottomMargin = If(panelIndex < _panels.Count - 1, 2, _bottomMargin)

'        Dim currentTopMargin = If(panel.Index > 0, 5, _topMargin)

'        Dim chartRect As New Rectangle(
'            bounds.X + _leftMargin,
'            bounds.Y + currentTopMargin,
'            bounds.Width - _leftMargin - _rightMargin,
'            bounds.Height - currentTopMargin - currentBottomMargin)

'        DrawGrid(g, chartRect)

'        If panel.Index = 0 Then
'            DrawCandlesticks(g, chartRect, panel)
'            For Each indicator In _indicators
'                DrawIndicatorOnPanel(g, indicator, panel, chartRect, True)
'            Next
'        Else
'            For Each indicator In _indicators
'                DrawIndicatorOnPanel(g, indicator, panel, chartRect, False)
'            Next
'        End If

'        DrawAxes(g, chartRect, panel, bounds)
'        DrawPanelInfo(g, panel, bounds, chartRect, panelIndex)
'    End Sub

'    Private Sub DrawGrid(g As Graphics, bounds As Rectangle)
'        Using pen As New Pen(_gridColor)
'            For i = 0 To 5
'                Dim y = bounds.Y + (bounds.Height / 5) * i
'                g.DrawLine(pen, bounds.Left, CInt(y), bounds.Right, CInt(y))
'            Next

'            Dim visibleCount = _visibleEndIndex - _visibleStartIndex + 1
'            Dim istep = Math.Max(1, visibleCount \ 10)
'            For i = 0 To visibleCount Step istep
'                Dim x = bounds.Left + (bounds.Width / visibleCount) * i
'                g.DrawLine(pen, CInt(x), bounds.Top, CInt(x), bounds.Bottom)
'            Next
'        End Using
'    End Sub

'    Private Sub DrawCandlesticks(g As Graphics, bounds As Rectangle, panel As ChartPanel)
'        Dim visibleCandles = _candles.Skip(_visibleStartIndex).Take(_visibleEndIndex - _visibleStartIndex + 1).ToList()
'        If visibleCandles.Count = 0 Then Return

'        Dim minPrice = panel.YAxis.Min.Value
'        Dim maxPrice = panel.YAxis.Max.Value
'        Dim priceRange = maxPrice - minPrice
'        If priceRange = 0 Then priceRange = 1

'        For i = 0 To visibleCandles.Count - 1
'            Dim candle = visibleCandles(i)
'            Dim x = bounds.Left + (bounds.Width / visibleCandles.Count) * i + (bounds.Width / visibleCandles.Count) / 2

'            Dim openY = bounds.Bottom - CInt((candle.Open - minPrice) / priceRange * bounds.Height)
'            Dim closeY = bounds.Bottom - CInt((candle.Close - minPrice) / priceRange * bounds.Height)
'            Dim highY = bounds.Bottom - CInt((candle.High - minPrice) / priceRange * bounds.Height)
'            Dim lowY = bounds.Bottom - CInt((candle.Low - minPrice) / priceRange * bounds.Height)

'            Dim color = If(candle.Close >= candle.Open, _bullishColor, _bearishColor)
'            Dim cw = Math.Max(2, CInt((bounds.Width / visibleCandles.Count) - _candleSpacing))

'            Using pen As New Pen(color)
'                g.DrawLine(pen, CInt(x), highY, CInt(x), lowY)
'            End Using

'            Dim bodyTop = Math.Min(openY, closeY)
'            Dim bodyHeight = Math.Max(1, Math.Abs(closeY - openY))
'            Using brush As New SolidBrush(color)
'                g.FillRectangle(brush, CInt(x - cw \ 2), bodyTop, cw, bodyHeight)
'            End Using
'        Next
'    End Sub

'    Private Sub DrawIndicatorOnPanel(g As Graphics, indicator As IIndicator, panel As ChartPanel, bounds As Rectangle, isOverlay As Boolean)
'        If Not _indicatorData.ContainsKey(indicator.Name) Then Return

'        Dim metadata = indicator.GetSeriesMetadata()
'        Dim indicatorData = _indicatorData(indicator.Name)

'        For Each meta In metadata
'            If (isOverlay AndAlso meta.DisplayMode = ChartDisplayMode.Overlay AndAlso panel.Index = 0) OrElse
'               (Not isOverlay AndAlso meta.DisplayMode = ChartDisplayMode.Separate AndAlso meta.PanelIndex = panel.Index) Then
'                If indicatorData.ContainsKey(meta.Name) Then
'                    DrawSeries(g, indicatorData(meta.Name), meta, bounds, isOverlay, panel)
'                End If
'            End If
'        Next
'    End Sub

'    Private Sub DrawSeries(g As Graphics, data As List(Of Double?), meta As SeriesMetadata, bounds As Rectangle, isOverlay As Boolean, panel As ChartPanel)
'        Dim visibleData = data.Skip(_visibleStartIndex).Take(_visibleEndIndex - _visibleStartIndex + 1).ToList()
'        If visibleData.Count = 0 Then Return

'        Dim validData = visibleData.Where(Function(v) v.HasValue).Select(Function(v) v.Value).ToList()
'        If validData.Count = 0 Then Return

'        ' Y�?범위 계산
'        Dim minVal As Double = panel.YAxis.Min.Value
'        Dim maxVal As Double = panel.YAxis.Max.Value
'        Dim range As Double = maxVal - minVal

'        If range = 0 Then range = 1

'        ' ===== 과열/침체 구간 그리�?=====
'        If meta.EnableZones AndAlso Not isOverlay Then
'            ' 과열 구간
'            If meta.OverboughtLevel.HasValue Then
'                Dim obValue = meta.OverboughtLevel.Value

'                If obValue >= minVal AndAlso obValue <= maxVal Then
'                    Dim obY = CInt(bounds.Bottom - ((obValue - minVal) / range * bounds.Height))
'                    Dim zoneHeight = Math.Max(1, obY - bounds.Top)

'                    Using brush As New SolidBrush(Color.FromArgb(40, 255, 0, 0))
'                        g.FillRectangle(brush, bounds.Left, bounds.Top, bounds.Width, zoneHeight)
'                    End Using
'                End If
'            End If

'            ' 침체 구간
'            If meta.OversoldLevel.HasValue Then
'                Dim osValue = meta.OversoldLevel.Value

'                If osValue >= minVal AndAlso osValue <= maxVal Then
'                    Dim osY = CInt(bounds.Bottom - ((osValue - minVal) / range * bounds.Height))
'                    Dim zoneHeight = Math.Max(1, bounds.Bottom - osY)

'                    Using brush As New SolidBrush(Color.FromArgb(40, 0, 0, 255))
'                        g.FillRectangle(brush, bounds.Left, osY, bounds.Width, zoneHeight)
'                    End Using
'                End If
'            End If
'        End If

'        ' ===== 기�???그리�?- ?�러 �?지??=====
'        If meta.ReferenceLines IsNot Nothing AndAlso meta.ReferenceLines.Count > 0 Then
'            For Each refLine In meta.ReferenceLines
'                Dim refValue = refLine.Value

'                If refValue >= minVal AndAlso refValue <= maxVal Then
'                    Dim refY = CInt(bounds.Bottom - ((refValue - minVal) / range * bounds.Height))

'                    Using pen As New Pen(refLine.Color, 1.0F) With {.DashStyle = refLine.Style}
'                        g.DrawLine(pen, bounds.Left, refY, bounds.Right, refY)
'                    End Using
'                End If
'            Next
'        ElseIf meta.ReferenceLine.HasValue Then
'            ' �??�위 ?�환?? ?�일 ReferenceLine??지??
'            Dim refValue = meta.ReferenceLine.Value

'            If refValue >= minVal AndAlso refValue <= maxVal Then
'                Dim refY = CInt(bounds.Bottom - ((refValue - minVal) / range * bounds.Height))

'                Using pen As New Pen(meta.ReferenceLineColor, 1.0F) With {.DashStyle = meta.ReferenceLineStyle}
'                    g.DrawLine(pen, bounds.Left, refY, bounds.Right, refY)
'                End Using
'            End If
'        End If

'        ' 차트 그리�?
'        Select Case meta.ChartType
'            Case ChartType.Line
'                DrawLineChart(g, visibleData, bounds, minVal, range, meta)
'            Case ChartType.Histogram
'                DrawHistogram(g, visibleData, bounds, minVal, range, meta)
'        End Select
'    End Sub



'    Private Sub DrawLineChart(g As Graphics, data As List(Of Double?), bounds As Rectangle, minVal As Double, rangeVal As Double, meta As SeriesMetadata)
'        Dim points As New List(Of PointF)
'        For i = 0 To data.Count - 1
'            If data(i).HasValue Then
'                Dim x = CSng(bounds.Left + (bounds.Width / data.Count) * i + (bounds.Width / data.Count) / 2)
'                Dim y = CSng(bounds.Bottom - CInt((data(i).Value - minVal) / rangeVal * bounds.Height))
'                points.Add(New PointF(x, y))
'            End If
'        Next

'        If points.Count > 1 Then
'            Using pen As New Pen(meta.Color, meta.LineWidth) With {.DashStyle = meta.LineStyle}  ' ??.DashStyle 추�?
'                g.DrawLines(pen, points.ToArray())
'            End Using
'        End If
'    End Sub

'    Private Sub DrawHistogram(g As Graphics, data As List(Of Double?), bounds As Rectangle, minVal As Double, rangeVal As Double, meta As SeriesMetadata)
'        Dim barWidth = Math.Max(1, CInt((bounds.Width / data.Count) - 1))
'        Dim zeroY = bounds.Bottom - CInt((0 - minVal) / rangeVal * bounds.Height)

'        For i = 0 To data.Count - 1
'            If data(i).HasValue Then
'                Dim x = CSng(bounds.Left + (bounds.Width / data.Count) * i)
'                Dim y = CSng(bounds.Bottom - CInt((data(i).Value - minVal) / rangeVal * bounds.Height))
'                Dim barHeight = CSng(Math.Abs(y - zeroY))
'                Dim barColor = If(data(i).Value >= 0, meta.Color, Color.Red)
'                Using brush As New SolidBrush(barColor)
'                    g.FillRectangle(brush, x, CSng(Math.Min(y, zeroY)), CSng(barWidth), barHeight)
'                End Using
'            End If
'        Next
'    End Sub

'    Private Sub DrawAxes(g As Graphics, chartRect As Rectangle, panel As ChartPanel, bounds As Rectangle)
'        Using brush As New SolidBrush(_textColor)
'            Using font As New Font("Arial", 8)
'                ' Y-Axis for both Price and Indicator panels
'                If panel.YAxis.Min.HasValue AndAlso panel.YAxis.Max.HasValue Then
'                    Dim minVal = panel.YAxis.Min.Value
'                    Dim maxVal = panel.YAxis.Max.Value
'                    Dim range = maxVal - minVal
'                    If range = 0 Then Return

'                    Dim labelFormat = If(panel.Index = 0, "N0", "F2") ' Price vs Indicator format

'                    For i = 0 To 5
'                        Dim y = chartRect.Bottom - (chartRect.Height / 5) * i
'                        Dim yValue = minVal + (range / 5) * i
'                        g.DrawString(yValue.ToString(labelFormat), font, brush, chartRect.Right + 5, y - 7)
'                    Next
'                End If

'                ' X-Axis (Time) for the last panel
'                If panel.Index = _panels.Count - 1 Then
'                    Dim visibleCandles = _candles.Skip(_visibleStartIndex).Take(_visibleEndIndex - _visibleStartIndex + 1).ToList()
'                    If visibleCandles.Count > 0 Then
'                        Dim stepValue = Math.Max(1, visibleCandles.Count \ 10)
'                        For i = 0 To visibleCandles.Count - 1 Step stepValue
'                            If i < visibleCandles.Count Then
'                                Dim x = chartRect.Left + (chartRect.Width / visibleCandles.Count) * i
'                                Dim timeStr = visibleCandles(i).Timestamp.ToString("HH:mm")
'                                Dim size = g.MeasureString(timeStr, font)
'                                g.DrawString(timeStr, font, brush, CSng(x - size.Width / 2), chartRect.Bottom + 5)
'                            End If
'                        Next
'                    End If
'                End If
'            End Using
'        End Using
'    End Sub

'    Private Sub DrawPanelInfo(g As Graphics, panel As ChartPanel, bounds As Rectangle, chartRect As Rectangle, panelIndex As Integer)
'        Using brush As New SolidBrush(_textColor)
'            Using font As New Font("Arial", 9, FontStyle.Bold)
'                Using smallFont As New Font("Arial", 8)
'                    ' ?�널 ?�목
'                    g.DrawString(panel.Title, font, brush, bounds.X + 10, bounds.Y + 5)
'                    If panel.Index = 0 AndAlso Not String.IsNullOrEmpty(_strategyLabel) Then
'                        Dim labelText As String = $"Strategy: {_strategyLabel}"
'                        Dim titleWidth As Single = g.MeasureString(panel.Title, font).Width
'                        g.DrawString(labelText, smallFont, brush, bounds.X + 20 + titleWidth, bounds.Y + 6)
'                    End If

'                    Dim visibleCandles = _candles.Skip(_visibleStartIndex).Take(_visibleEndIndex - _visibleStartIndex + 1).ToList()
'                    If visibleCandles.Count = 0 Then Return

'                    ' 마우???�치 ?�는 마�?�?캔들 ?�덱??
'                    Dim targetIndex As Integer
'                    If _mouseLeftChart Then
'                        targetIndex = visibleCandles.Count - 1
'                    ElseIf _mousePos.Y >= chartRect.Top AndAlso _mousePos.Y <= chartRect.Bottom Then
'                        targetIndex = CInt((_mousePos.X - chartRect.Left) / (chartRect.Width / visibleCandles.Count))
'                        targetIndex = Math.Max(0, Math.Min(targetIndex, visibleCandles.Count - 1))
'                    Else
'                        targetIndex = visibleCandles.Count - 1
'                    End If

'                    If targetIndex < 0 OrElse targetIndex >= visibleCandles.Count Then Return

'                    Dim previousIndex = targetIndex - 1

'                    ' 메인 ?�널 (가�?
'                    If panel.Index = 0 Then
'                        Dim currentCandle = visibleCandles(targetIndex)
'                        Dim previousCandle As CandleInfo = If(previousIndex >= 0, visibleCandles(previousIndex), Nothing)

'                        Dim info = $"O:{currentCandle.Open:N0} H:{currentCandle.High:N0} L:{currentCandle.Low:N0} C:{currentCandle.Close:N0} V:{currentCandle.Volume:N0}"
'                        Dim xOffset As Single = bounds.X + 80
'                        g.DrawString(info, smallFont, brush, xOffset, bounds.Y + 5)
'                        xOffset += g.MeasureString(info, smallFont).Width

'                        ' 종�? ?�락�??�시
'                        If previousCandle IsNot Nothing AndAlso previousCandle.Close > 0 Then
'                            Dim pctChange = (currentCandle.Close - previousCandle.Close) / previousCandle.Close * 100
'                            Dim changeString = $"({pctChange.ToString("+0.00;-0.00")}%)"
'                            Dim changeColor = If(pctChange > 0, Color.Red, If(pctChange < 0, Color.LightGreen, _textColor))
'                            Using changeBrush As New SolidBrush(changeColor)
'                                g.DrawString(changeString, smallFont, changeBrush, xOffset, bounds.Y + 5)
'                            End Using
'                        End If

'                        ' ?�버?�이 지???�보
'                        Dim yOffset = bounds.Y + 22
'                        For Each indicator In _indicators
'                            If _indicatorData.ContainsKey(indicator.Name) Then
'                                Dim metadata = indicator.GetSeriesMetadata()
'                                For Each meta In metadata
'                                    If meta.DisplayMode = ChartDisplayMode.Overlay AndAlso meta.PanelIndex = 0 Then
'                                        Dim indicatorData = _indicatorData(indicator.Name)
'                                        If indicatorData.ContainsKey(meta.Name) Then
'                                            Dim dataList = indicatorData(meta.Name)
'                                            Dim dataIndex = _visibleStartIndex + targetIndex
'                                            If dataIndex >= 0 AndAlso dataIndex < dataList.Count AndAlso dataList(dataIndex).HasValue Then
'                                                Dim currentValue = dataList(dataIndex).Value
'                                                Dim xPos As Single = bounds.X + 80

'                                                Using colorBrush As New SolidBrush(meta.Color)
'                                                    Dim valueString = $"{meta.Name}:{currentValue:F2}"
'                                                    g.DrawString(valueString, smallFont, colorBrush, xPos, yOffset)
'                                                    xPos += g.MeasureString(valueString, smallFont).Width
'                                                End Using

'                                                ' ?�락�?계산 �??�시
'                                                If dataIndex > 0 AndAlso dataList(dataIndex - 1).HasValue Then
'                                                    Dim previousValue = dataList(dataIndex - 1).Value
'                                                    If previousValue <> 0 Then
'                                                        Dim pctChange = (currentValue - previousValue) / Math.Abs(previousValue) * 100
'                                                        Dim changeString = $"({pctChange.ToString("+0.00;-0.00")}%)"
'                                                        Dim changeColor = If(pctChange > 0, Color.Red, If(pctChange < 0, Color.LightGreen, _textColor))
'                                                        Using changeBrush As New SolidBrush(changeColor)
'                                                            g.DrawString(changeString, smallFont, changeBrush, xPos, yOffset)
'                                                        End Using
'                                                    End If
'                                                End If
'                                                yOffset += 15
'                                            End If
'                                        End If
'                                    End If
'                                Next
'                            End If
'                        Next
'                    Else
'                        ' 보조 지???�널
'                        Dim yOffset = bounds.Y + 22
'                        For Each indicator In _indicators
'                            If _indicatorData.ContainsKey(indicator.Name) Then
'                                Dim metadata = indicator.GetSeriesMetadata()
'                                For Each meta In metadata
'                                    If meta.DisplayMode = ChartDisplayMode.Separate AndAlso meta.PanelIndex = panel.Index Then
'                                        Dim indicatorData = _indicatorData(indicator.Name)
'                                        If indicatorData.ContainsKey(meta.Name) Then
'                                            Dim dataList = indicatorData(meta.Name)
'                                            Dim dataIndex = _visibleStartIndex + targetIndex
'                                            If dataIndex >= 0 AndAlso dataIndex < dataList.Count AndAlso dataList(dataIndex).HasValue Then
'                                                Dim currentValue = dataList(dataIndex).Value
'                                                Dim xPos As Single = bounds.X + 80

'                                                Using colorBrush As New SolidBrush(meta.Color)
'                                                    Dim valueString = $"{meta.Name}:{currentValue:F2}"
'                                                    g.DrawString(valueString, smallFont, colorBrush, xPos, yOffset)
'                                                    xPos += g.MeasureString(valueString, smallFont).Width
'                                                End Using

'                                                ' ?�락�?계산 �??�시
'                                                If dataIndex > 0 AndAlso dataList(dataIndex - 1).HasValue Then
'                                                    Dim previousValue = dataList(dataIndex - 1).Value
'                                                    If previousValue <> 0 Then
'                                                        Dim pctChange = (currentValue - previousValue) / Math.Abs(previousValue) * 100
'                                                        Dim changeString = $"({pctChange.ToString("+0.00;-0.00")}%)"
'                                                        Dim changeColor = If(pctChange > 0, Color.Red, If(pctChange < 0, Color.LightGreen, _textColor))
'                                                        Using changeBrush As New SolidBrush(changeColor)
'                                                            g.DrawString(changeString, smallFont, changeBrush, xPos, yOffset)
'                                                        End Using
'                                                    End If
'                                                End If
'                                                yOffset += 15
'                                            End If
'                                        End If
'                                    End If
'                                Next
'                            End If
'                        Next
'                    End If
'                End Using
'            End Using
'        End Using
'    End Sub

'    Private Sub DrawCrosshair(g As Graphics)
'        If Not _showCrosshair AndAlso Not _mouseLeftChart Then Return

'        Dim drawPos = If(_mouseLeftChart, New Point(Width - _rightMargin - 10, Height - _bottomMargin - 10), _mousePos)

'        Using pen As New Pen(_crosshairColor) With {.DashStyle = DashStyle.Dot}
'            g.DrawLine(pen, drawPos.X, 0, drawPos.X, Height)
'            g.DrawLine(pen, 0, drawPos.Y, Width, drawPos.Y)
'        End Using
'    End Sub

'    Private Sub DrawCrosshairLabels(g As Graphics)
'        Dim visibleCandles = _candles.Skip(_visibleStartIndex).Take(_visibleEndIndex - _visibleStartIndex + 1).ToList()
'        If visibleCandles.Count = 0 Then Return

'        Dim drawPos = If(_mouseLeftChart, New Point(Width - _rightMargin - 10, Height - _bottomMargin - 10), _mousePos)

'        ' ?�재 ?�널 찾기
'        Dim currentY = 0
'        Dim targetPanel As ChartPanel = Nothing
'        Dim chartRect As Rectangle = Nothing

'        For Each panel In _panels
'            Dim panelHeight = CInt((Height - _panelSpacing * (_panels.Count - 1)) * panel.HeightRatio)
'            Dim panelRect As New Rectangle(0, currentY, Width, panelHeight)

'            If drawPos.Y >= panelRect.Top AndAlso drawPos.Y <= panelRect.Bottom Then
'                targetPanel = panel
'                chartRect = New Rectangle(
'                    panelRect.X + _leftMargin,
'                    panelRect.Y + _topMargin,
'                    panelRect.Width - _leftMargin - _rightMargin,
'                    panelRect.Height - _topMargin - _bottomMargin)
'                Exit For
'            End If

'            currentY += panelHeight + _panelSpacing
'        Next

'        If targetPanel Is Nothing OrElse chartRect = Nothing Then Return

'        Using font As New Font("Arial", 8, FontStyle.Bold)
'            ' X�??�이�?(?�간)
'            Dim mouseIndex = CInt((drawPos.X - chartRect.Left) / (chartRect.Width / visibleCandles.Count))
'            mouseIndex = Math.Max(0, Math.Min(mouseIndex, visibleCandles.Count - 1))

'            If _mouseLeftChart Then
'                mouseIndex = visibleCandles.Count - 1
'            End If

'            If mouseIndex >= 0 AndAlso mouseIndex < visibleCandles.Count Then
'                Dim timeStr = visibleCandles(mouseIndex).Timestamp.ToString("MM-dd HH:mm")
'                Dim timeSize = g.MeasureString(timeStr, font)
'                Dim timeX = drawPos.X - timeSize.Width / 2
'                Dim timeY = Height - _bottomMargin + 5

'                Using bgBrush As New SolidBrush(Color.FromArgb(200, 60, 60, 60))
'                    g.FillRectangle(bgBrush, timeX - 2, timeY, timeSize.Width + 4, timeSize.Height)
'                End Using
'                Using brush As New SolidBrush(Color.Yellow)
'                    g.DrawString(timeStr, font, brush, timeX, timeY)
'                End Using
'            End If

'            ' Y�??�이�?(가�??�는 지??�?
'            If targetPanel.Index = 0 Then
'                ' 가�??�널
'                Dim minPrice = visibleCandles.Min(Function(c) c.Low)
'                Dim maxPrice = visibleCandles.Max(Function(c) c.High)
'                Dim priceRange = maxPrice - minPrice
'                If priceRange > 0 Then
'                    Dim priceRatio = (chartRect.Bottom - drawPos.Y) / CSng(chartRect.Height)
'                    Dim priceValue = minPrice + (priceRange * priceRatio)
'                    Dim priceStr = priceValue.ToString("N0")
'                    Dim priceSize = g.MeasureString(priceStr, font)

'                    Using bgBrush As New SolidBrush(Color.FromArgb(200, 60, 60, 60))
'                        g.FillRectangle(bgBrush, Width - _rightMargin + 2, drawPos.Y - priceSize.Height / 2, _rightMargin - 4, priceSize.Height)
'                    End Using
'                    Using brush As New SolidBrush(Color.Yellow)
'                        g.DrawString(priceStr, font, brush, Width - _rightMargin + 5, drawPos.Y - priceSize.Height / 2)
'                    End Using
'                End If
'            Else
'                ' 보조 지???�널 - RSI, MACD ?�의 �?
'                For Each indicator In _indicators
'                    If _indicatorData.ContainsKey(indicator.Name) Then
'                        Dim metadata = indicator.GetSeriesMetadata()
'                        Dim firstMeta = metadata.FirstOrDefault(Function(m) m.DisplayMode = ChartDisplayMode.Separate AndAlso m.PanelIndex = targetPanel.Index)
'                        If firstMeta IsNot Nothing Then
'                            Dim minVal = If(firstMeta.AxisInfo.Min.HasValue, firstMeta.AxisInfo.Min.Value, 0)
'                            Dim maxVal = If(firstMeta.AxisInfo.Max.HasValue, firstMeta.AxisInfo.Max.Value, 100)
'                            Dim valueRange = maxVal - minVal

'                            If valueRange > 0 Then
'                                Dim valueRatio = (chartRect.Bottom - drawPos.Y) / CSng(chartRect.Height)
'                                Dim indicatorValue = minVal + (valueRange * valueRatio)
'                                Dim valueStr = indicatorValue.ToString("F2")
'                                Dim valueSize = g.MeasureString(valueStr, font)

'                                Using bgBrush As New SolidBrush(Color.FromArgb(200, 60, 60, 60))
'                                    g.FillRectangle(bgBrush, Width - _rightMargin + 2, drawPos.Y - valueSize.Height / 2, _rightMargin - 4, valueSize.Height)
'                                End Using
'                                Using brush As New SolidBrush(Color.Yellow)
'                                    g.DrawString(valueStr, font, brush, Width - _rightMargin + 5, drawPos.Y - valueSize.Height / 2)
'                                End Using
'                            End If
'                            Exit For
'                        End If
'                    End If
'                Next
'            End If
'        End Using
'    End Sub

'    Private Sub DrawNoDataMessage(g As Graphics)
'        g.Clear(_backgroundColor)
'        Using brush As New SolidBrush(_textColor)
'            Using font As New Font("Arial", 12)
'                Dim message = "?�이?��? ?�습?�다"
'                Dim size = g.MeasureString(message, font)
'                g.DrawString(message, font, brush, (Width - size.Width) / 2, (Height - size.Height) / 2)
'            End Using
'        End Using
'    End Sub

'    Protected Overrides Sub OnMouseWheel(e As MouseEventArgs)
'        MyBase.OnMouseWheel(e)
'        If _candles Is Nothing OrElse _candles.Count = 0 Then Return

'        Dim visibleCount = _visibleEndIndex - _visibleStartIndex + 1
'        Dim zoomFactor = If(e.Delta > 0, 0.9, 1.1)
'        Dim newCount = CInt(visibleCount * zoomFactor)
'        newCount = Math.Max(10, Math.Min(newCount, _candles.Count))

'        Dim centerRatio = (_visibleEndIndex + _visibleStartIndex) / 2.0 / _candles.Count
'        Dim newCenter = CInt(_candles.Count * centerRatio)
'        _visibleStartIndex = Math.Max(0, newCenter - newCount \ 2)
'        _visibleEndIndex = Math.Min(_candles.Count - 1, _visibleStartIndex + newCount - 1)

'        Invalidate()
'    End Sub

'    ' OnMouseDown 메서???�정
'    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
'        MyBase.OnMouseDown(e)

'        If _isSimulationMode AndAlso e.Button = MouseButtons.Left Then
'            ' ?��??�이??모드?�서 ?�릭 ???�당 ?�치�??�동
'            Dim visibleCandles = _candles.Skip(_visibleStartIndex).Take(_visibleEndIndex - _visibleStartIndex + 1).ToList()
'            If visibleCandles.Count > 0 Then
'                Dim currentY = 0
'                For Each panel In _panels
'                    Dim panelHeight = CInt((Height - _panelSpacing * (_panels.Count - 1)) * panel.HeightRatio)
'                    Dim panelRect As New Rectangle(0, currentY, Width, panelHeight)

'                    If e.Y >= panelRect.Top AndAlso e.Y <= panelRect.Bottom Then
'                        Dim chartRect As New Rectangle(
'                        panelRect.X + _leftMargin,
'                        panelRect.Y + _topMargin,
'                        panelRect.Width - _leftMargin - _rightMargin,
'                        panelRect.Height - _topMargin - _bottomMargin)

'                        If e.X >= chartRect.Left AndAlso e.X <= chartRect.Right Then
'                            Dim clickIndex = CInt((e.X - chartRect.Left) / (chartRect.Width / visibleCandles.Count))
'                            clickIndex = Math.Max(0, Math.Min(clickIndex, visibleCandles.Count - 1))
'                            Dim absoluteIndex = _visibleStartIndex + clickIndex
'                            SetSimulationIndex(absoluteIndex)
'                            RaiseEvent SimulationIndexChanged(_simulationIndex, _simulationMaxIndex)
'                        End If
'                        Exit For
'                    End If
'                    currentY += panelHeight + _panelSpacing
'                Next
'            End If
'        ElseIf e.Button = MouseButtons.Left Then
'            _isDragging = True
'            _lastMousePos = e.Location
'        End If
'    End Sub

'    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
'        MyBase.OnMouseMove(e)
'        _mousePos = e.Location
'        _showCrosshair = True
'        _mouseLeftChart = False
'        _currentPanelIndex = -1

'        If _isDragging AndAlso _candles IsNot Nothing Then
'            Dim dx = e.X - _lastMousePos.X
'            Dim visibleCount = _visibleEndIndex - _visibleStartIndex + 1
'            Dim shift = -CInt(dx / (Width / visibleCount))

'            _visibleStartIndex = Math.Max(0, Math.Min(_candles.Count - visibleCount, _visibleStartIndex + shift))
'            _visibleEndIndex = _visibleStartIndex + visibleCount - 1

'            _lastMousePos = e.Location
'        End If

'        Invalidate()
'    End Sub

'    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
'        MyBase.OnMouseUp(e)
'        _isDragging = False
'    End Sub

'    Protected Overrides Sub OnMouseLeave(e As EventArgs)
'        MyBase.OnMouseLeave(e)
'        _showCrosshair = False
'        _mouseLeftChart = True
'        _currentPanelIndex = -1
'        Invalidate()
'    End Sub

'    ' ?��??�이??관??메서?�들 (End Class 바로 ?�에 추�?)

'    ''' <summary>
'    ''' ?��??�이??모드 ?�성??비활?�화
'    ''' </summary>
'    Public Sub SetSimulationMode(enabled As Boolean)
'        _isSimulationMode = enabled
'        If Not enabled Then
'            _visibleEndIndex = _simulationMaxIndex
'        End If
'        Invalidate()
'    End Sub

'    ''' <summary>
'    ''' ?��??�이???�덱???�정
'    ''' </summary>
'    Public Sub SetSimulationIndex(index As Integer)
'        If _candles Is Nothing OrElse _candles.Count = 0 Then Return
'        _simulationIndex = Math.Max(10, Math.Min(index, _simulationMaxIndex))

'        If _isSimulationMode Then
'            _visibleEndIndex = _simulationIndex
'            Dim visibleCount = 100
'            _visibleStartIndex = Math.Max(0, _visibleEndIndex - visibleCount)
'        End If
'        Invalidate()
'    End Sub

'    ''' <summary>
'    ''' ?�음 캔들�??�동
'    ''' </summary>
'    Public Function MoveNextCandle() As Boolean
'        If _simulationIndex < _simulationMaxIndex Then
'            _simulationIndex += 1
'            SetSimulationIndex(_simulationIndex)
'            Return True
'        End If
'        Return False
'    End Function

'    ''' <summary>
'    ''' ?�전 캔들�??�동
'    ''' </summary>
'    Public Function MovePreviousCandle() As Boolean
'        If _simulationIndex > 10 Then
'            _simulationIndex -= 1
'            SetSimulationIndex(_simulationIndex)
'            Return True
'        End If
'        Return False
'    End Function

'    ''' <summary>
'    ''' ?��??�이???�치 ?�보
'    ''' </summary>
'    Public Function GetSimulationInfo() As (CurrentIndex As Integer, MaxIndex As Integer, Percentage As Double)
'        Dim percentage = If(_simulationMaxIndex > 0, (_simulationIndex / CSng(_simulationMaxIndex)) * 100, 0)
'        Return (_simulationIndex, _simulationMaxIndex, percentage)
'    End Function

'    ''' <summary>
'    ''' ?��??�이???�덱??변�??�벤??
'    ''' </summary>
'    Public Event SimulationIndexChanged(currentIndex As Integer, maxIndex As Integer)




'End Class

'#End Region
