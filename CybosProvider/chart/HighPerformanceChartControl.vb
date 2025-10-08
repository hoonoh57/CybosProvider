Option Strict On
Option Explicit On
Option Infer On

Imports System
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.ComponentModel
Imports System.Linq
Imports System.Collections.Generic
Imports System.Windows.Forms

' =====================================================================================
'  Part 2: HighPerformanceChartControl
'  - 그리드/캔들/오버레이/보조패널/십자선/라벨/줌&팬/시뮬레이션 지원
'  - Part 1(공통+인디케이터)가 동일 프로젝트에 포함되어 있어야 컴파일됩니다.
'  - 깨진 주석들을 모두 의미 있는 한국어 설명으로 교체했습니다.
' =====================================================================================

#Region "고성능 차트 컨트롤 본체"

Public Class HighPerformanceChartControl
    Inherits UserControl

    ' ----- 데이터/상태 -----
    Private _candles As List(Of CandleInfo) = Nothing
    Private ReadOnly _panels As New List(Of ChartPanel)()
    Private ReadOnly _indicators As New List(Of IIndicator)()
    ' indicatorName → (seriesName → List(Of Double?))
    Private ReadOnly _indicatorData As New Dictionary(Of String, Dictionary(Of String, List(Of Double?)))()

    ' 전략 라벨(메인 패널 타이틀 옆 표시용)
    Private _strategyLabel As String = String.Empty

    ' 백 버퍼(깨끗한 더블버퍼링)
    Private _backBuffer As Bitmap = Nothing

    ' 뷰포트(보이는 구간: 인덱스 범위)
    Private _visibleStartIndex As Integer = 0
    Private _visibleEndIndex As Integer = 0

    ' 캔들 폭/간격
    Private _candleWidth As Double = 5
    Private ReadOnly _candleSpacing As Integer = 2

    ' 마우스 인터랙션
    Private _isDragging As Boolean = False
    Private _lastMousePos As Point
    Private _mousePos As Point = Point.Empty
    Private _showCrosshair As Boolean = False
    Private _currentPanelIndex As Integer = -1
    Private _mouseLeftChart As Boolean = True

    ' 시뮬레이션(점진적 재생)
    Private _isSimulationMode As Boolean = False
    Private _simulationIndex As Integer = 0
    Private _simulationMaxIndex As Integer = 0

    ' 레이아웃 여백/스페이싱
    Private ReadOnly _leftMargin As Integer = 10
    Private ReadOnly _rightMargin As Integer = 70
    Private ReadOnly _topMargin As Integer = 20
    Private ReadOnly _bottomMargin As Integer = 20
    Private ReadOnly _panelSpacing As Integer = 0

    ' 색상 테마
    Private ReadOnly _backgroundColor As Color = Color.Black
    Private ReadOnly _gridColor As Color = Color.FromArgb(30, 128, 128, 128)
    Private ReadOnly _textColor As Color = Color.White
    Private ReadOnly _bullishColor As Color = Color.Red
    Private ReadOnly _bearishColor As Color = Color.Green
    Private ReadOnly _baselineColor As Color = Color.White
    Private ReadOnly _crosshairColor As Color = Color.Gray

    ' ===== 생성자/초기화 =====
    Public Sub New()
        InitializeComponents()

        ' 더블버퍼 최적화
        Me.DoubleBuffered = True
        Me.SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer, True)

        ' 기본 메인 패널(가격)
        Dim mainPanel As New ChartPanel With {
            .Index = 0,
            .Title = "Price",
            .HeightRatio = 0.7,
            .BackColor = _backgroundColor
        }
        _panels.Add(mainPanel)
    End Sub

    Private Sub InitializeComponents()
        Me.Name = "HighPerformanceChartControl"
        Me.Size = New Size(800, 600)
    End Sub

    ' ===== 데이터 주입 =====

    ''' <summary>
    ''' 캔들 데이터 설정(뷰포트/시뮬 인덱스 보존 옵션 제공)
    ''' </summary>
    Public Sub SetData(candles As List(Of CandleInfo), Optional preserveViewport As Boolean = False)
        Dim previousVisibleStart = _visibleStartIndex
        Dim previousVisibleEnd = _visibleEndIndex
        Dim previousSimulationIndex = _simulationIndex
        Dim hadExistingData = _candles IsNot Nothing AndAlso _candles.Count > 0

        _candles = candles

        If candles IsNot Nothing AndAlso candles.Count > 0 Then
            _simulationMaxIndex = candles.Count - 1

            If preserveViewport AndAlso hadExistingData Then
                _visibleStartIndex = Math.Max(0, Math.Min(previousVisibleStart, _simulationMaxIndex))
                _visibleEndIndex = Math.Max(_visibleStartIndex, Math.Min(previousVisibleEnd, _simulationMaxIndex))
                _simulationIndex = Math.Max(0, Math.Min(previousSimulationIndex, _simulationMaxIndex))
            Else
                _visibleEndIndex = _simulationMaxIndex
                _visibleStartIndex = Math.Max(0, _visibleEndIndex - 100) ' 최근 100개 기본
                _simulationIndex = Math.Max(0, Math.Min(_simulationIndex, _simulationMaxIndex))
            End If
        Else
            _visibleStartIndex = 0
            _visibleEndIndex = 0
            _simulationIndex = 0
            _simulationMaxIndex = 0
        End If

        CalculateAllIndicators()
        Invalidate()
    End Sub
    ''' <summary>
    ''' 전략명 텍스트를 갱신하고 차트를 다시 그립니다.
    ''' (렌더링 위치/색상은 DrawPanelInfo에서 처리: 메인 패널 캔들 영역 좌측 하단, 빨간색)
    ''' </summary>
    Public Sub SetStrategyLabel(label As String)
        Dim normalized As String = If(label, String.Empty).Trim()
        If _strategyLabel <> normalized Then
            _strategyLabel = normalized
            Invalidate() ' 화면 다시 그리기 → DrawPanelInfo에서 새 위치로 표시
        End If
    End Sub
            
' HighPerformanceChartControl 내부
Private _strategyLabelColor As Color = Color.Red
Public Sub SetStrategyLabelColor(c As Color)
    _strategyLabelColor = c
    Invalidate()
End Sub

    ' ===== 인디케이터 관리 =====

    ''' <summary>
    ''' 인디케이터 추가(이미 존재하면 무시). Separate 메타데이터는 패널 자동 생성.
    ''' </summary>
    Public Sub AddIndicator(indicator As IIndicator)
        Dim existing = _indicators.FirstOrDefault(Function(i) i.Name = indicator.Name)
        If existing Is Nothing Then
            _indicators.Add(indicator)

            ' Separate 표시면 패널 보장
            For Each meta In indicator.GetSeriesMetadata()
                If meta.DisplayMode = ChartDisplayMode.Separate Then
                    EnsurePanelExists(meta.PanelIndex, indicator.Name)
                End If
            Next

            If _candles IsNot Nothing Then
                CalculateIndicator(indicator)
                Invalidate()
            End If
        End If
    End Sub

    Public Function GetIndicatorByName(indicatorName As String) As IIndicator
        Return _indicators.FirstOrDefault(Function(i) i.Name = indicatorName)
    End Function

    Public Sub RemoveIndicator(indicatorName As String)
        Dim indicator = _indicators.FirstOrDefault(Function(i) i.Name = indicatorName)
        If indicator IsNot Nothing Then
            _indicators.Remove(indicator)
            _indicatorData.Remove(indicatorName)
            Invalidate()
        End If
    End Sub

    ''' <summary>무효화 없이 제거(일부 이벤트 루프에서 사용)</summary>
    Public Sub RemoveIndicatorSilent(indicatorName As String)
        Dim indicator = _indicators.FirstOrDefault(Function(i) i.Name = indicatorName)
        If indicator IsNot Nothing Then
            _indicators.Remove(indicator)
            _indicatorData.Remove(indicatorName)
        End If
    End Sub

    ''' <summary>메인 패널만 남기고 보조/지표 모두 초기화</summary>
    Public Sub ClearIndicators()
        _indicators.Clear()
        _indicatorData.Clear()

        Dim mainPanels = _panels.Where(Function(p) p.Index = 0).ToList()
        _panels.Clear()
        For Each p In mainPanels
            _panels.Add(p)
        Next

        Invalidate()
    End Sub

    ''' <summary>지정 패널이 없으면 생성하고 패널 높이비 재조정</summary>
    Private Sub EnsurePanelExists(panelIndex As Integer, title As String)
        Dim panel = _panels.FirstOrDefault(Function(p) p.Index = panelIndex)
        If panel Is Nothing Then
            panel = New ChartPanel With {
                .Index = panelIndex,
                .Title = title,
                .HeightRatio = 0.15,
                .BackColor = _backgroundColor
            }
            _panels.Add(panel)

            ' 패널 인덱스 순서 정렬
            Dim sorted = _panels.OrderBy(Function(p) p.Index).ToList()
            _panels.Clear()
            For Each p In sorted
                _panels.Add(p)
            Next

            AdjustPanelHeights()
        End If
    End Sub

    ''' <summary>패널 개수에 따른 합리적 높이 배분</summary>
    Private Sub AdjustPanelHeights()
        If _panels.Count = 1 Then
            _panels(0).HeightRatio = 1.0
        ElseIf _panels.Count = 2 Then
            _panels(0).HeightRatio = 0.6
            _panels(1).HeightRatio = 0.4
        ElseIf _panels.Count = 3 Then
            _panels(0).HeightRatio = 0.5
            _panels(1).HeightRatio = 0.25
            _panels(2).HeightRatio = 0.25
        ElseIf _panels.Count = 4 Then
            _panels(0).HeightRatio = 0.4
            _panels(1).HeightRatio = 0.2
            _panels(2).HeightRatio = 0.2
            _panels(3).HeightRatio = 0.2
        ElseIf _panels.Count > 4 Then
            _panels(0).HeightRatio = 0.4
            Dim remaining = 0.6 / (_panels.Count - 1)
            For i = 1 To _panels.Count - 1
                _panels(i).HeightRatio = remaining
            Next
        End If
    End Sub

    ' ===== 패널 Y축 범위 산출 =====
    Private Sub CalculatePanelYAxisRange(ByVal panel As ChartPanel)
        If _candles Is Nothing OrElse _candles.Count = 0 Then
            panel.YAxis.Min = 0
            panel.YAxis.Max = 100
            Return
        End If

        If panel.Index = 0 Then
            ' 가격 패널: 보이는 캔들 고저 범위
            Dim visibleCandles = _candles.Skip(_visibleStartIndex).Take(_visibleEndIndex - _visibleStartIndex + 1).ToList()
            If visibleCandles.Count = 0 Then
                panel.YAxis.Min = 0
                panel.YAxis.Max = 100
                Return
            End If

            panel.YAxis.Min = visibleCandles.Min(Function(c) c.Low)
            panel.YAxis.Max = visibleCandles.Max(Function(c) c.High)
            If panel.YAxis.Min = panel.YAxis.Max Then
                panel.YAxis.Min -= 1
                panel.YAxis.Max += 1
            End If
        Else
            ' 보조 패널: 해당 패널에 그려지는 모든 시리즈의 가시 구간 최소/최대 집계
            Dim panelSeriesItems As New List(Of Tuple(Of IIndicator, SeriesMetadata))()
            For Each indicator In _indicators
                If _indicatorData.ContainsKey(indicator.Name) Then
                    For Each meta In indicator.GetSeriesMetadata()
                        If meta.DisplayMode = ChartDisplayMode.Separate AndAlso meta.PanelIndex = panel.Index Then
                            panelSeriesItems.Add(Tuple.Create(indicator, meta))
                        End If
                    Next
                End If
            Next

            If panelSeriesItems.Count = 0 Then
                panel.YAxis.Min = 0
                panel.YAxis.Max = 100
                Return
            End If

            ' 1) 고정 축 스케일 우선(RSI처럼)
            Dim nonAutoScaleItem = panelSeriesItems.FirstOrDefault(Function(item) _
                Not item.Item2.AxisInfo.AutoScale AndAlso item.Item2.AxisInfo.Min.HasValue AndAlso item.Item2.AxisInfo.Max.HasValue)

            If nonAutoScaleItem IsNot Nothing Then
                panel.YAxis.Min = nonAutoScaleItem.Item2.AxisInfo.Min.Value
                panel.YAxis.Max = nonAutoScaleItem.Item2.AxisInfo.Max.Value
                panel.YAxis.AutoScale = False
            Else
                ' 2) 자동 스케일 집계
                Dim minVal = Double.MaxValue
                Dim maxVal = Double.MinValue
                Dim visibleDataExists As Boolean = False

                For Each item In panelSeriesItems
                    Dim indicator = item.Item1
                    Dim meta = item.Item2

                    If _indicatorData.ContainsKey(indicator.Name) AndAlso _indicatorData(indicator.Name).ContainsKey(meta.Name) Then
                        Dim data = _indicatorData(indicator.Name)(meta.Name)
                        Dim visibleData = data.Skip(_visibleStartIndex).Take(_visibleEndIndex - _visibleStartIndex + 1) _
                                              .Where(Function(v) v.HasValue).Select(Function(v) v.Value).ToList()

                        If visibleData.Count > 0 Then
                            visibleDataExists = True
                            minVal = Math.Min(minVal, visibleData.Min())
                            maxVal = Math.Max(maxVal, visibleData.Max())
                        End If
                    End If
                Next

                If Not visibleDataExists Then
                    panel.YAxis.Min = 0
                    panel.YAxis.Max = 100
                Else
                    If minVal = maxVal Then
                        minVal -= 1
                        maxVal += 1
                    Else
                        ' 10% 패딩
                        Dim padding = (maxVal - minVal) * 0.1
                        minVal -= padding
                        maxVal += padding
                    End If

                    panel.YAxis.Min = minVal
                    panel.YAxis.Max = maxVal
                End If
                panel.YAxis.AutoScale = True
            End If
        End If
    End Sub

    ' ===== 인디케이터 계산 =====
    Private Sub CalculateAllIndicators()
        _indicatorData.Clear()
        For Each indicator In _indicators
            CalculateIndicator(indicator)
        Next
    End Sub

    Private Sub CalculateIndicator(indicator As IIndicator)
        If _candles Is Nothing OrElse _candles.Count = 0 Then Return
        _indicatorData(indicator.Name) = indicator.Calculate(_candles)
    End Sub

    ' ===== 페인팅 =====
    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)

        If _candles Is Nothing OrElse _candles.Count = 0 Then
            DrawNoDataMessage(e.Graphics)
            Return
        End If

        ' 백버퍼 크기 보장
        If _backBuffer Is Nothing OrElse _backBuffer.Width <> Width OrElse _backBuffer.Height <> Height Then
            _backBuffer?.Dispose()
            _backBuffer = New Bitmap(Width, Height)
        End If

        Using g As Graphics = Graphics.FromImage(_backBuffer)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.TextRenderingHint = Drawing.Text.TextRenderingHint.ClearTypeGridFit
            g.Clear(_backgroundColor)

            Dim currentY = 0

            For i = 0 To _panels.Count - 1
                Dim panel = _panels(i)
                Dim panelHeight = CInt((Height - _panelSpacing * (_panels.Count - 1)) * panel.HeightRatio)
                Dim panelRect As New Rectangle(0, currentY, Width, panelHeight)

                ' 현재 마우스가 위치한 패널 인덱스 추적
                If _mousePos.Y >= panelRect.Top AndAlso _mousePos.Y <= panelRect.Bottom Then
                    _currentPanelIndex = i
                End If

                DrawPanel(g, panel, panelRect, i)
                currentY += panelHeight + _panelSpacing
            Next

            ' 십자선 + 레이블 (차트 밖이면 마지막 봉 기준으로 표시)
            If (_showCrosshair AndAlso Not _mouseLeftChart) OrElse _mouseLeftChart Then
                DrawCrosshair(g)
                DrawCrosshairLabels(g)
            End If
        End Using

        e.Graphics.DrawImageUnscaled(_backBuffer, 0, 0)
    End Sub

    ''' <summary>패널 단위로 그리기(그리드 → 시리즈 → 축/정보)</summary>
    Private Sub DrawPanel(g As Graphics, panel As ChartPanel, bounds As Rectangle, panelIndex As Integer)
        CalculatePanelYAxisRange(panel)

        Using brush As New SolidBrush(panel.BackColor)
            g.FillRectangle(brush, bounds)
        End Using

        Dim currentBottomMargin = If(panelIndex < _panels.Count - 1, 2, _bottomMargin)
        Dim currentTopMargin = If(panel.Index > 0, 5, _topMargin)

        Dim chartRect As New Rectangle(
            bounds.X + _leftMargin,
            bounds.Y + currentTopMargin,
            bounds.Width - _leftMargin - _rightMargin,
            bounds.Height - currentTopMargin - currentBottomMargin
        )

        DrawGrid(g, chartRect)

        If panel.Index = 0 Then
            DrawCandlesticks(g, chartRect, panel)
            For Each indicator In _indicators
                DrawIndicatorOnPanel(g, indicator, panel, chartRect, True)
            Next
        Else
            For Each indicator In _indicators
                DrawIndicatorOnPanel(g, indicator, panel, chartRect, False)
            Next
        End If

        DrawAxes(g, chartRect, panel, bounds)
        DrawPanelInfo(g, panel, bounds, chartRect, panelIndex)
    End Sub

    ''' <summary>배경 그리드(가로 6줄, 세로 10분할)</summary>
    Private Sub DrawGrid(g As Graphics, bounds As Rectangle)
        Using pen As New Pen(_gridColor)
            ' 수평선(6개)
            For i = 0 To 5
                Dim y = bounds.Y + (bounds.Height / 5) * i
                g.DrawLine(pen, bounds.Left, CInt(y), bounds.Right, CInt(y))
            Next

            ' 수직선: 가시 캔들 10분할
            Dim visibleCount = _visibleEndIndex - _visibleStartIndex + 1
            Dim istep = Math.Max(1, visibleCount \ 10)

            For i = 0 To visibleCount Step istep
                Dim x = bounds.Left + (bounds.Width / visibleCount) * i
                g.DrawLine(pen, CInt(x), bounds.Top, CInt(x), bounds.Bottom)
            Next
        End Using
    End Sub

    ''' <summary>캔들스틱 (메인 패널 전용)</summary>
    Private Sub DrawCandlesticks(g As Graphics, bounds As Rectangle, panel As ChartPanel)
        Dim visibleCandles = _candles.Skip(_visibleStartIndex).Take(_visibleEndIndex - _visibleStartIndex + 1).ToList()
        If visibleCandles.Count = 0 Then Return

        Dim minPrice = panel.YAxis.Min.Value
        Dim maxPrice = panel.YAxis.Max.Value
        Dim priceRange = maxPrice - minPrice
        If priceRange = 0 Then priceRange = 1

        For i = 0 To visibleCandles.Count - 1
            Dim candle = visibleCandles(i)
            Dim x = bounds.Left + (bounds.Width / visibleCandles.Count) * i + (bounds.Width / visibleCandles.Count) / 2
            Dim openY = bounds.Bottom - CInt((candle.Open - minPrice) / priceRange * bounds.Height)
            Dim closeY = bounds.Bottom - CInt((candle.Close - minPrice) / priceRange * bounds.Height)
            Dim highY = bounds.Bottom - CInt((candle.High - minPrice) / priceRange * bounds.Height)
            Dim lowY = bounds.Bottom - CInt((candle.Low - minPrice) / priceRange * bounds.Height)

            Dim color = If(candle.Close >= candle.Open, _bullishColor, _bearishColor)
            Dim cw = Math.Max(2, CInt((bounds.Width / visibleCandles.Count) - _candleSpacing))

            ' 심지
            Using pen As New Pen(color)
                g.DrawLine(pen, CInt(x), highY, CInt(x), lowY)
            End Using

            ' 몸통
            Dim bodyTop = Math.Min(openY, closeY)
            Dim bodyHeight = Math.Max(1, Math.Abs(closeY - openY))
            Using brush As New SolidBrush(color)
                g.FillRectangle(brush, CInt(x - cw \ 2), bodyTop, cw, bodyHeight)
            End Using
        Next
    End Sub

    ''' <summary>하나의 인디케이터가 가진 모든 시리즈를 해당 패널에 그림</summary>
    Private Sub DrawIndicatorOnPanel(g As Graphics, indicator As IIndicator, panel As ChartPanel, bounds As Rectangle, isOverlay As Boolean)
        If Not _indicatorData.ContainsKey(indicator.Name) Then Return

        Dim metadata = indicator.GetSeriesMetadata()
        Dim indicatorData = _indicatorData(indicator.Name)

        For Each meta In metadata
            Dim drawThis As Boolean =
                (isOverlay AndAlso meta.DisplayMode = ChartDisplayMode.Overlay AndAlso panel.Index = 0) OrElse
                (Not isOverlay AndAlso meta.DisplayMode = ChartDisplayMode.Separate AndAlso meta.PanelIndex = panel.Index)

            If drawThis AndAlso indicatorData.ContainsKey(meta.Name) Then
                DrawSeries(g, indicatorData(meta.Name), meta, bounds, isOverlay, panel)
            End If
        Next
    End Sub

    ''' <summary>시리즈 단위 그리기(Line/Histogram/Scatter + 기준선/과열/침체)</summary>
    Private Sub DrawSeries(g As Graphics, data As List(Of Double?), meta As SeriesMetadata, bounds As Rectangle, isOverlay As Boolean, panel As ChartPanel)
        Dim visibleData = data.Skip(_visibleStartIndex).Take(_visibleEndIndex - _visibleStartIndex + 1).ToList()
        If visibleData.Count = 0 Then Return

        Dim validData = visibleData.Where(Function(v) v.HasValue).Select(Function(v) v.Value).ToList()
        If validData.Count = 0 Then Return

        ' Y축 범위
        Dim minVal As Double = panel.YAxis.Min.Value
        Dim maxVal As Double = panel.YAxis.Max.Value
        Dim range As Double = maxVal - minVal
        If range = 0 Then range = 1

        ' (별도 패널) 과열/침체 영역
        If meta.EnableZones AndAlso Not isOverlay Then
            ' 과열
            If meta.OverboughtLevel.HasValue Then
                Dim obValue = meta.OverboughtLevel.Value
                If obValue >= minVal AndAlso obValue <= maxVal Then
                    Dim obY = CInt(bounds.Bottom - ((obValue - minVal) / range * bounds.Height))
                    Dim zoneHeight = Math.Max(1, obY - bounds.Top)
                    Using brush As New SolidBrush(Color.FromArgb(40, 255, 0, 0))
                        g.FillRectangle(brush, bounds.Left, bounds.Top, bounds.Width, zoneHeight)
                    End Using
                End If
            End If
            ' 침체
            If meta.OversoldLevel.HasValue Then
                Dim osValue = meta.OversoldLevel.Value
                If osValue >= minVal AndAlso osValue <= maxVal Then
                    Dim osY = CInt(bounds.Bottom - ((osValue - minVal) / range * bounds.Height))
                    Dim zoneHeight = Math.Max(1, bounds.Bottom - osY)
                    Using brush As New SolidBrush(Color.FromArgb(40, 0, 0, 255))
                        g.FillRectangle(brush, bounds.Left, osY, bounds.Width, zoneHeight)
                    End Using
                End If
            End If
        End If

        ' 기준선(여러 개 → 단일) 우선순서로 그림
        If meta.ReferenceLines IsNot Nothing AndAlso meta.ReferenceLines.Count > 0 Then
            For Each refLine In meta.ReferenceLines
                Dim refValue = refLine.Value
                If refValue >= minVal AndAlso refValue <= maxVal Then
                    Dim refY = CInt(bounds.Bottom - ((refValue - minVal) / range * bounds.Height))
                    Using pen As New Pen(refLine.Color, 1.0F) With {.DashStyle = refLine.Style}
                        g.DrawLine(pen, bounds.Left, refY, bounds.Right, refY)
                    End Using
                End If
            Next
        ElseIf meta.ReferenceLine.HasValue Then
            Dim refValue = meta.ReferenceLine.Value
            If refValue >= minVal AndAlso refValue <= maxVal Then
                Dim refY = CInt(bounds.Bottom - ((refValue - minVal) / range * bounds.Height))
                Using pen As New Pen(meta.ReferenceLineColor, 1.0F) With {.DashStyle = meta.ReferenceLineStyle}
                    g.DrawLine(pen, bounds.Left, refY, bounds.Right, refY)
                End Using
            End If
        End If

        ' 실제 시리즈 그리기
        Select Case meta.ChartType
            Case ChartType.Line
                DrawLineChart(g, visibleData, bounds, minVal, range, meta)
            Case ChartType.Histogram
                DrawHistogram(g, visibleData, bounds, minVal, range, meta)
            Case ChartType.Scatter
                DrawScatterChart(g, visibleData, bounds, minVal, range, meta)
        End Select
    End Sub

    Private Sub DrawLineChart(g As Graphics, data As List(Of Double?), bounds As Rectangle, minVal As Double, rangeVal As Double, meta As SeriesMetadata)
        Dim points As New List(Of PointF)()
        For i = 0 To data.Count - 1
            If data(i).HasValue Then
                Dim x = CSng(bounds.Left + (bounds.Width / data.Count) * i + (bounds.Width / data.Count) / 2)
                Dim y = CSng(bounds.Bottom - CInt((data(i).Value - minVal) / rangeVal * bounds.Height))
                points.Add(New PointF(x, y))
            End If
        Next

        If points.Count > 1 Then
            Using pen As New Pen(meta.Color, meta.LineWidth) With {.DashStyle = meta.LineStyle}
                g.DrawLines(pen, points.ToArray())
            End Using
        End If
    End Sub

    Private Sub DrawHistogram(g As Graphics, data As List(Of Double?), bounds As Rectangle, minVal As Double, rangeVal As Double, meta As SeriesMetadata)
        Dim barWidth = Math.Max(1, CInt((bounds.Width / data.Count) - 1))
        Dim zeroY = bounds.Bottom - CInt((0 - minVal) / rangeVal * bounds.Height)

        For i = 0 To data.Count - 1
            If data(i).HasValue Then
                Dim x = CSng(bounds.Left + (bounds.Width / data.Count) * i)
                Dim y = CSng(bounds.Bottom - CInt((data(i).Value - minVal) / rangeVal * bounds.Height))
                Dim barHeight = CSng(Math.Abs(y - zeroY))
                Dim barColor = If(data(i).Value >= 0, meta.Color, Color.Red)

                Using brush As New SolidBrush(barColor)
                    g.FillRectangle(brush, CSng(x), CSng(Math.Min(y, zeroY)), CSng(barWidth), barHeight)
                End Using
            End If
        Next
    End Sub

    Private Sub DrawScatterChart(g As Graphics, data As List(Of Double?), bounds As Rectangle, minVal As Double, rangeVal As Double, meta As SeriesMetadata)
        If data Is Nothing OrElse data.Count = 0 Then Return

        Dim pointSize As Single = 8 ' 마커 크기

        For i = 0 To data.Count - 1
            If data(i).HasValue Then
                Dim x As Single = CSng(bounds.Left + (bounds.Width / data.Count) * i + (bounds.Width / data.Count) / 2)
                Dim y As Single = CSng(bounds.Bottom - CInt((data(i).Value - minVal) / rangeVal * bounds.Height))

                Using brush As New SolidBrush(meta.Color)
                    If meta.Name = "BUY" Then
                        ' 매수: 위를 가리키는 삼각형 (캔들 아래쪽에 찍히도록 살짝 내림)
                        Dim yPos = y + 5
                        Dim pts As PointF() = {
                            New PointF(x, yPos),
                            New PointF(x - pointSize / 2, yPos + pointSize),
                            New PointF(x + pointSize / 2, yPos + pointSize)
                        }
                        g.FillPolygon(brush, pts)
                    ElseIf meta.Name = "SELL" Then
                        ' 매도: 아래를 가리키는 삼각형 (캔들 위쪽에 찍히도록 살짝 올림)
                        Dim yPos = y - 5
                        Dim pts As PointF() = {
                            New PointF(x, yPos),
                            New PointF(x - pointSize / 2, yPos - pointSize),
                            New PointF(x + pointSize / 2, yPos - pointSize)
                        }
                        g.FillPolygon(brush, pts)
                    Else
                        ' 일반 점(원)
                        g.FillEllipse(brush, CSng(x - pointSize / 2), CSng(y - pointSize / 2), CSng(pointSize), CSng(pointSize))
                    End If
                End Using
            End If
        Next
    End Sub

    ''' <summary>축 라벨(Y: 각 패널, X: 마지막 패널)</summary>
    Private Sub DrawAxes(g As Graphics, chartRect As Rectangle, panel As ChartPanel, bounds As Rectangle)
        Using brush As New SolidBrush(_textColor)
            Using font As New Font("Arial", 8)
                ' Y축(가격/지표 값)
                If panel.YAxis.Min.HasValue AndAlso panel.YAxis.Max.HasValue Then
                    Dim minVal = panel.YAxis.Min.Value
                    Dim maxVal = panel.YAxis.Max.Value
                    Dim range = maxVal - minVal
                    If range = 0 Then Return

                    Dim labelFormat = If(panel.Index = 0, "N0", "F2") ' 가격 패널/보조 패널 포맷

                    For i = 0 To 5
                        Dim y = chartRect.Bottom - (chartRect.Height / 5) * i
                        Dim yValue = minVal + (range / 5) * i
                        g.DrawString(yValue.ToString(labelFormat), font, brush, CSng(chartRect.Right + 5), CSng(y - 7))
                    Next
                End If

                ' X축(시간) - 마지막 패널에만
                If panel.Index = _panels.Count - 1 Then
                    Dim visibleCandles = _candles.Skip(_visibleStartIndex).Take(_visibleEndIndex - _visibleStartIndex + 1).ToList()
                    If visibleCandles.Count > 0 Then
                        Dim stepValue = Math.Max(1, visibleCandles.Count \ 10)
                        For i = 0 To visibleCandles.Count - 1 Step stepValue
                            Dim x = chartRect.Left + (chartRect.Width / visibleCandles.Count) * i
                            Dim timeStr = visibleCandles(i).Timestamp.ToString("HH:mm")
                            Dim size = g.MeasureString(timeStr, font)
                            g.DrawString(timeStr, font, brush, CSng(x - size.Width / 2), chartRect.Bottom + 5)
                        Next
                    End If
                End If
            End Using
        End Using
    End Sub

    ''' <summary>패널 타이틀/전일 대비/오버레이 지표값 등 정보 표시</summary>
    Private Sub DrawPanelInfo(g As Graphics, panel As ChartPanel, bounds As Rectangle, chartRect As Rectangle, panelIndex As Integer)
        Using brush As New SolidBrush(_textColor)
            Using font As New Font("Arial", 9, FontStyle.Bold)
                Using smallFont As New Font("Arial", 8)

                    ' 패널 제목 (좌상단)
                    g.DrawString(panel.Title, font, brush, bounds.X + 10, bounds.Y + 5)

                    ' ─────────────────────────────────────────────────────────
                    ' 전략명: 캔들차트(메인 패널) 영역의 "좌측 하단"에 붉은색으로 표시
                    '  - chartRect 기준 좌하단에 여백을 두고 배치
                    '  - 기존: 제목 오른쪽에 표시 → 제거
                    ' ─────────────────────────────────────────────────────────
                    If panel.Index = 0 AndAlso Not String.IsNullOrEmpty(_strategyLabel) Then
                        Dim labelText As String = $"Strategy: {_strategyLabel}"
                        Dim padding As Integer = 6
                        Dim labelSize As SizeF = g.MeasureString(labelText, smallFont)
                        Dim labelX As Single = chartRect.Left + padding
                        Dim labelY As Single = chartRect.Bottom - labelSize.Height - padding

                        Using labelBrush As New SolidBrush(Color.Red)
                            g.DrawString(labelText, smallFont, labelBrush, labelX, labelY)
                        End Using
                    End If
                    ' ─────────────────────────────────────────────────────────

                    Dim visibleCandles = _candles.Skip(_visibleStartIndex).Take(_visibleEndIndex - _visibleStartIndex + 1).ToList()
                    If visibleCandles.Count = 0 Then Return

                    ' 현재 마우스가 가리키는 인덱스(없으면 마지막)
                    Dim targetIndex As Integer
                    If _mouseLeftChart Then
                        targetIndex = visibleCandles.Count - 1
                    ElseIf _mousePos.Y >= chartRect.Top AndAlso _mousePos.Y <= chartRect.Bottom Then
                        targetIndex = CInt((_mousePos.X - chartRect.Left) / (chartRect.Width / visibleCandles.Count))
                        targetIndex = Math.Max(0, Math.Min(targetIndex, visibleCandles.Count - 1))
                    Else
                        targetIndex = visibleCandles.Count - 1
                    End If
                    If targetIndex < 0 OrElse targetIndex >= visibleCandles.Count Then Return

                    Dim previousIndex = targetIndex - 1

                    If panel.Index = 0 Then
                        ' ─ 메인 패널: O/H/L/C/V + 등락률
                        Dim currentCandle = visibleCandles(targetIndex)
                        Dim previousCandle As CandleInfo = If(previousIndex >= 0, visibleCandles(previousIndex), Nothing)
                        Dim info = $"O:{currentCandle.Open:N0} H:{currentCandle.High:N0} L:{currentCandle.Low:N0} C:{currentCandle.Close:N0} V:{currentCandle.Volume:N0}"
                        Dim xOffset As Single = bounds.X + 80
                        g.DrawString(info, smallFont, brush, xOffset, bounds.Y + 5)
                        xOffset += g.MeasureString(info, smallFont).Width

                        ' 종가 등락(전봉 대비)
                        If previousCandle.Timestamp <> Date.MinValue AndAlso previousCandle.Close > 0 Then
                            Dim pctChange = (currentCandle.Close - previousCandle.Close) / previousCandle.Close * 100
                            Dim changeString = $"({pctChange.ToString("+0.00;-0.00")}%)"
                            Dim changeColor = If(pctChange > 0, Color.Red, If(pctChange < 0, Color.LightGreen, _textColor))
                            Using changeBrush As New SolidBrush(changeColor)
                                g.DrawString(changeString, smallFont, changeBrush, xOffset, bounds.Y + 5)
                            End Using
                        End If

                        ' 오버레이 지표값 + 직전 대비 변화율
                        Dim yOffset = bounds.Y + 22
                        For Each indicator In _indicators
                            If _indicatorData.ContainsKey(indicator.Name) Then
                                For Each meta In indicator.GetSeriesMetadata()
                                    If meta.DisplayMode = ChartDisplayMode.Overlay AndAlso meta.PanelIndex = 0 Then
                                        Dim indicatorData = _indicatorData(indicator.Name)
                                        If indicatorData.ContainsKey(meta.Name) Then
                                            Dim dataList = indicatorData(meta.Name)
                                            Dim dataIndex = _visibleStartIndex + targetIndex
                                            If dataIndex >= 0 AndAlso dataIndex < dataList.Count AndAlso dataList(dataIndex).HasValue Then
                                                Dim currentValue = dataList(dataIndex).Value
                                                Dim xPos As Single = bounds.X + 80

                                                Using colorBrush As New SolidBrush(meta.Color)
                                                    Dim valueString = $"{meta.Name}:{currentValue:F2}"
                                                    g.DrawString(valueString, smallFont, colorBrush, xPos, yOffset)
                                                    xPos += g.MeasureString(valueString, smallFont).Width
                                                End Using

                                                ' 변화율(전값 대비)
                                                If dataIndex > 0 AndAlso dataList(dataIndex - 1).HasValue Then
                                                    Dim previousValue = dataList(dataIndex - 1).Value
                                                    If previousValue <> 0 Then
                                                        Dim pctChange = (currentValue - previousValue) / Math.Abs(previousValue) * 100
                                                        Dim changeString = $"({pctChange.ToString("+0.00;-0.00")}%)"
                                                        Dim changeColor = If(pctChange > 0, Color.Red, If(pctChange < 0, Color.LightGreen, _textColor))
                                                        Using changeBrush As New SolidBrush(changeColor)
                                                            g.DrawString(changeString, smallFont, changeBrush, xPos, yOffset)
                                                        End Using
                                                    End If
                                                End If

                                                yOffset += 15
                                            End If
                                        End If
                                    End If
                                Next
                            End If
                        Next

                    Else
                        ' ─ 보조 패널: 해당 패널 지표값 + 직전 대비 변화율
                        Dim yOffset = bounds.Y + 22
                        For Each indicator In _indicators
                            If _indicatorData.ContainsKey(indicator.Name) Then
                                For Each meta In indicator.GetSeriesMetadata()
                                    If meta.DisplayMode = ChartDisplayMode.Separate AndAlso meta.PanelIndex = panel.Index Then
                                        Dim indicatorData = _indicatorData(indicator.Name)
                                        If indicatorData.ContainsKey(meta.Name) Then
                                            Dim dataList = indicatorData(meta.Name)
                                            Dim dataIndex = _visibleStartIndex + targetIndex
                                            If dataIndex >= 0 AndAlso dataIndex < dataList.Count AndAlso dataList(dataIndex).HasValue Then
                                                Dim currentValue = dataList(dataIndex).Value
                                                Dim xPos As Single = bounds.X + 80

                                                Using colorBrush As New SolidBrush(meta.Color)
                                                    Dim valueString = $"{meta.Name}:{currentValue:F2}"
                                                    g.DrawString(valueString, smallFont, colorBrush, xPos, yOffset)
                                                    xPos += g.MeasureString(valueString, smallFont).Width
                                                End Using

                                                If dataIndex > 0 AndAlso dataList(dataIndex - 1).HasValue Then
                                                    Dim previousValue = dataList(dataIndex - 1).Value
                                                    If previousValue <> 0 Then
                                                        Dim pctChange = (currentValue - previousValue) / Math.Abs(previousValue) * 100
                                                        Dim changeString = $"({pctChange.ToString("+0.00;-0.00")}%)"
                                                        Dim changeColor = If(pctChange > 0, Color.Red, If(pctChange < 0, Color.LightGreen, _textColor))
                                                        Using changeBrush As New SolidBrush(changeColor)
                                                            g.DrawString(changeString, smallFont, changeBrush, xPos, yOffset)
                                                        End Using
                                                    End If
                                                End If

                                                yOffset += 15
                                            End If
                                        End If
                                    End If
                                Next
                            End If
                        Next
                    End If

                End Using
            End Using
        End Using
    End Sub

    ''' <summary>십자선(차트 바깥이면 마지막 위치로 고정 표시)</summary>
    Private Sub DrawCrosshair(g As Graphics)
        If Not _showCrosshair AndAlso Not _mouseLeftChart Then Return

        Dim drawPos = If(_mouseLeftChart, New Point(Width - _rightMargin - 10, Height - _bottomMargin - 10), _mousePos)
        Using pen As New Pen(_crosshairColor) With {.DashStyle = DashStyle.Dot}
            g.DrawLine(pen, drawPos.X, 0, drawPos.X, Height)
            g.DrawLine(pen, 0, drawPos.Y, Width, drawPos.Y)
        End Using
    End Sub

    ''' <summary>십자선 라벨(X: 시간, Y: 가격/지표)</summary>
    Private Sub DrawCrosshairLabels(g As Graphics)
        Dim visibleCandles = _candles.Skip(_visibleStartIndex).Take(_visibleEndIndex - _visibleStartIndex + 1).ToList()
        If visibleCandles.Count = 0 Then Return

        Dim drawPos = If(_mouseLeftChart, New Point(Width - _rightMargin - 10, Height - _bottomMargin - 10), _mousePos)

        ' 현재 마우스가 위치한 패널/차트 영역 찾기
        Dim currentY = 0
        Dim targetPanel As ChartPanel = Nothing
        Dim chartRect As Rectangle = Rectangle.Empty

        For Each panel In _panels
            Dim panelHeight = CInt((Height - _panelSpacing * (_panels.Count - 1)) * panel.HeightRatio)
            Dim panelRect As New Rectangle(0, currentY, Width, panelHeight)

            If drawPos.Y >= panelRect.Top AndAlso drawPos.Y <= panelRect.Bottom Then
                targetPanel = panel
                chartRect = New Rectangle(
                    panelRect.X + _leftMargin,
                    panelRect.Y + _topMargin,
                    panelRect.Width - _leftMargin - _rightMargin,
                    panelRect.Height - _topMargin - _bottomMargin)
                Exit For
            End If

            currentY += panelHeight + _panelSpacing
        Next

        If targetPanel Is Nothing OrElse chartRect = Rectangle.Empty Then Return

        Using font As New Font("Arial", 8, FontStyle.Bold)
            ' X축(시간) 라벨
            Dim mouseIndex = CInt((drawPos.X - chartRect.Left) / (chartRect.Width / visibleCandles.Count))
            mouseIndex = Math.Max(0, Math.Min(mouseIndex, visibleCandles.Count - 1))
            If _mouseLeftChart Then
                mouseIndex = visibleCandles.Count - 1
            End If

            If mouseIndex >= 0 AndAlso mouseIndex < visibleCandles.Count Then
                Dim timeStr = visibleCandles(mouseIndex).Timestamp.ToString("MM-dd HH:mm")
                Dim timeSize = g.MeasureString(timeStr, font)
                Dim timeX = drawPos.X - timeSize.Width / 2
                Dim timeY = Height - _bottomMargin + 5

                Using bgBrush As New SolidBrush(Color.FromArgb(200, 60, 60, 60))
                    g.FillRectangle(bgBrush, timeX - 2, timeY, timeSize.Width + 4, timeSize.Height)
                End Using
                Using brush As New SolidBrush(Color.Yellow)
                    g.DrawString(timeStr, font, brush, timeX, timeY)
                End Using
            End If

            ' Y축 라벨(가격/지표 값)
            If targetPanel.Index = 0 Then
                ' 가격 패널
                Dim minPrice = visibleCandles.Min(Function(c) c.Low)
                Dim maxPrice = visibleCandles.Max(Function(c) c.High)
                Dim priceRange = maxPrice - minPrice
                If priceRange > 0 Then
                    Dim priceRatio = (chartRect.Bottom - drawPos.Y) / CSng(chartRect.Height)
                    Dim priceValue = minPrice + (priceRange * priceRatio)
                    Dim priceStr = priceValue.ToString("N0")
                    Dim priceSize = g.MeasureString(priceStr, font)

                    Using bgBrush As New SolidBrush(Color.FromArgb(200, 60, 60, 60))
                        g.FillRectangle(bgBrush, Width - _rightMargin + 2, drawPos.Y - priceSize.Height / 2, _rightMargin - 4, priceSize.Height)
                    End Using
                    Using brush As New SolidBrush(Color.Yellow)
                        g.DrawString(priceStr, font, brush, Width - _rightMargin + 5, drawPos.Y - priceSize.Height / 2)
                    End Using
                End If
            Else
                ' 보조 패널 (첫 번째 Separate 메타의 범위를 사용)
                For Each indicator In _indicators
                    If _indicatorData.ContainsKey(indicator.Name) Then
                        Dim meta = indicator.GetSeriesMetadata() _
                                   .FirstOrDefault(Function(m) m.DisplayMode = ChartDisplayMode.Separate AndAlso m.PanelIndex = targetPanel.Index)
                        If meta IsNot Nothing Then
                            Dim minVal = If(meta.AxisInfo.Min.HasValue, meta.AxisInfo.Min.Value, 0)
                            Dim maxVal = If(meta.AxisInfo.Max.HasValue, meta.AxisInfo.Max.Value, 100)
                            Dim valueRange = maxVal - minVal

                            If valueRange > 0 Then
                                Dim valueRatio = (chartRect.Bottom - drawPos.Y) / CSng(chartRect.Height)
                                Dim indicatorValue = minVal + (valueRange * valueRatio)
                                Dim valueStr = indicatorValue.ToString("F2")
                                Dim valueSize = g.MeasureString(valueStr, font)

                                Using bgBrush As New SolidBrush(Color.FromArgb(200, 60, 60, 60))
                                    g.FillRectangle(bgBrush, Width - _rightMargin + 2, drawPos.Y - valueSize.Height / 2, _rightMargin - 4, valueSize.Height)
                                End Using
                                Using brush As New SolidBrush(Color.Yellow)
                                    g.DrawString(valueStr, font, brush, Width - _rightMargin + 5, drawPos.Y - valueSize.Height / 2)
                                End Using
                            End If
                            Exit For
                        End If
                    End If
                Next
            End If
        End Using
    End Sub

    ''' <summary>데이터 없음 표시</summary>
    Private Sub DrawNoDataMessage(g As Graphics)
        g.Clear(_backgroundColor)
        Using brush As New SolidBrush(_textColor)
            Using font As New Font("Arial", 12)
                Dim message = "캔들이 없습니다!"
                Dim size = g.MeasureString(message, font)
                g.DrawString(message, font, brush, (Width - size.Width) / 2, (Height - size.Height) / 2)
            End Using
        End Using
    End Sub

    ' ===== 입력: 줌/팬/시뮬레이션 =====

    Protected Overrides Sub OnMouseWheel(e As MouseEventArgs)
        MyBase.OnMouseWheel(e)
        If _candles Is Nothing OrElse _candles.Count = 0 Then Return

        Dim visibleCount = _visibleEndIndex - _visibleStartIndex + 1
        Dim zoomFactor = If(e.Delta > 0, 0.9, 1.1)
        Dim newCount = CInt(visibleCount * zoomFactor)
        newCount = Math.Max(10, Math.Min(newCount, _candles.Count))

        Dim centerRatio = (_visibleEndIndex + _visibleStartIndex) / 2.0 / _candles.Count
        Dim newCenter = CInt(_candles.Count * centerRatio)

        _visibleStartIndex = Math.Max(0, newCenter - newCount \ 2)
        _visibleEndIndex = Math.Min(_candles.Count - 1, _visibleStartIndex + newCount - 1)

        Invalidate()
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e)

        If _isSimulationMode AndAlso e.Button = MouseButtons.Left Then
            ' 시뮬레이션 모드: 클릭 위치로 재생 포지션 이동
            Dim visibleCandles = _candles.Skip(_visibleStartIndex).Take(_visibleEndIndex - _visibleStartIndex + 1).ToList()
            If visibleCandles.Count > 0 Then
                Dim currentY = 0
                For Each panel In _panels
                    Dim panelHeight = CInt((Height - _panelSpacing * (_panels.Count - 1)) * panel.HeightRatio)
                    Dim panelRect As New Rectangle(0, currentY, Width, panelHeight)

                    If e.Y >= panelRect.Top AndAlso e.Y <= panelRect.Bottom Then
                        Dim chartRect As New Rectangle(
                            panelRect.X + _leftMargin,
                            panelRect.Y + _topMargin,
                            panelRect.Width - _leftMargin - _rightMargin,
                            panelRect.Height - _topMargin - _bottomMargin)

                        If e.X >= chartRect.Left AndAlso e.X <= chartRect.Right Then
                            Dim clickIndex = CInt((e.X - chartRect.Left) / (chartRect.Width / visibleCandles.Count))
                            clickIndex = Math.Max(0, Math.Min(clickIndex, visibleCandles.Count - 1))
                            Dim absoluteIndex = _visibleStartIndex + clickIndex

                            SetSimulationIndex(absoluteIndex)
                            RaiseEvent SimulationIndexChanged(_simulationIndex, _simulationMaxIndex)
                        End If
                        Exit For
                    End If

                    currentY += panelHeight + _panelSpacing
                Next
            End If

        ElseIf e.Button = MouseButtons.Left Then
            ' 일반 드래그(패닝)
            _isDragging = True
            _lastMousePos = e.Location
        End If
    End Sub

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        MyBase.OnMouseMove(e)

        _mousePos = e.Location
        _showCrosshair = True
        _mouseLeftChart = False
        _currentPanelIndex = -1

        If _isDragging AndAlso _candles IsNot Nothing Then
            Dim dx = e.X - _lastMousePos.X
            Dim visibleCount = _visibleEndIndex - _visibleStartIndex + 1
            Dim shift = -CInt(dx / (Width / visibleCount))

            _visibleStartIndex = Math.Max(0, Math.Min(_candles.Count - visibleCount, _visibleStartIndex + shift))
            _visibleEndIndex = _visibleStartIndex + visibleCount - 1

            _lastMousePos = e.Location
        End If

        Invalidate()
    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        MyBase.OnMouseUp(e)
        _isDragging = False
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        MyBase.OnMouseLeave(e)
        _showCrosshair = False
        _mouseLeftChart = True
        _currentPanelIndex = -1
        Invalidate()
    End Sub

    ' ===== 시뮬레이션 모드 제어 =====

    Public Sub SetSimulationMode(enabled As Boolean)
        _isSimulationMode = enabled
        If Not enabled Then
            _visibleEndIndex = _simulationMaxIndex
        End If
        Invalidate()
    End Sub

    Public Sub SetSimulationIndex(index As Integer)
        If _candles Is Nothing OrElse _candles.Count = 0 Then Return

        _simulationIndex = Math.Max(10, Math.Min(index, _simulationMaxIndex))
        If _isSimulationMode Then
            _visibleEndIndex = _simulationIndex
            Dim visibleCount = 100
            _visibleStartIndex = Math.Max(0, _visibleEndIndex - visibleCount)
        End If
        Invalidate()
    End Sub

    Public Function MoveNextCandle() As Boolean
        If _simulationIndex < _simulationMaxIndex Then
            _simulationIndex += 1
            SetSimulationIndex(_simulationIndex)
            Return True
        End If
        Return False
    End Function

    Public Function MovePreviousCandle() As Boolean
        If _simulationIndex > 10 Then
            _simulationIndex -= 1
            SetSimulationIndex(_simulationIndex)
            Return True
        End If
        Return False
    End Function

    ''' <summary>
    ''' 시뮬 정보(현재/최대/진행률%) 반환
    ''' ⚠ 튜플은 값 형식이므로 `IsNot Nothing` 비교를 하면 컴파일 오류(BC31419)가 납니다.
    '''    → 필요 시 사용자 정의 구조체를 만들어 참조형으로 감싸 사용하세요.
    ''' </summary>
    Public Function GetSimulationInfo() As (CurrentIndex As Integer, MaxIndex As Integer, Percentage As Double)
        Dim percentage = If(_simulationMaxIndex > 0, (_simulationIndex / CSng(_simulationMaxIndex)) * 100, 0)
        Return (_simulationIndex, _simulationMaxIndex, percentage)
    End Function

    ' 인덱스 변경 이벤트(시뮬 UI와 연동)
    Public Event SimulationIndexChanged(currentIndex As Integer, maxIndex As Integer)
End Class

#End Region
