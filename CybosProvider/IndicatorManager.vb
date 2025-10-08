Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Linq

#Region "지표 매니저"

Public Class IndicatorManager
    Private ReadOnly _candles As List(Of CandleInfo)
    Private ReadOnly _calculatedIndicators As New Dictionary(Of String, Dictionary(Of String, List(Of Double?)))

    Public Sub New(candles As List(Of CandleInfo))
        _candles = candles
    End Sub

    Public Sub CalculateAllIndicators(indicators As List(Of IIndicator))
        _calculatedIndicators.Clear()
        For Each indicator In indicators
            CalculateIndicator(indicator)
        Next
    End Sub

    Public Sub CalculateIndicator(indicator As IIndicator)
        If _candles Is Nothing OrElse _candles.Count = 0 Then Return
        _calculatedIndicators(indicator.Name) = indicator.Calculate(_candles)
    End Sub

    Public Function GetIndicatorData(indicatorName As String) As Dictionary(Of String, List(Of Double?))
        If _calculatedIndicators.ContainsKey(indicatorName) Then
            Return _calculatedIndicators(indicatorName)
        End If
        Return Nothing
    End Function

    Public Function GetAllIndicatorData() As Dictionary(Of String, Dictionary(Of String, List(Of Double?)))
        Return _calculatedIndicators
    End Function

    Public Shared Sub CalculateBasicIndicators(candles As List(Of CandleInfo))
        For i As Integer = 0 To candles.Count - 1
            If i >= 4 Then
                candles(i).tickAvg5 = CInt(candles.Skip(i - 4).Take(5).Average(Function(c) c.tickCount))
            End If
            If i >= 19 Then
                candles(i).tickAvg20 = CInt(candles.Skip(i - 19).Take(20).Average(Function(c) c.tickCount))
            End If
            If i >= 59 Then
                candles(i).tickAvg60 = CInt(candles.Skip(i - 59).Take(60).Average(Function(c) c.tickCount))
            End If

            If i >= 4 Then
                candles(i).sma5 = candles.Skip(i - 4).Take(5).Average(Function(c) c.Close)
            End If
            If i >= 19 Then
                candles(i).sma20 = candles.Skip(i - 19).Take(20).Average(Function(c) c.Close)
            End If
            If i >= 59 Then
                candles(i).sma60 = candles.Skip(i - 59).Take(60).Average(Function(c) c.Close)
            End If
            If i >= 119 Then
                candles(i).sma120 = candles.Skip(i - 119).Take(120).Average(Function(c) c.Close)
            End If

            If i = 0 Then
                candles(i).ema5 = candles(i).Close
                candles(i).ema20 = candles(i).Close
            Else
                Dim k5 As Double = 2.0 / 6
                Dim k20 As Double = 2.0 / 21
                candles(i).ema5 = (candles(i).Close * k5) + (candles(i - 1).ema5 * (1 - k5))
                candles(i).ema20 = (candles(i).Close * k20) + (candles(i - 1).ema20 * (1 - k20))
            End If

            If i >= 19 Then
                Dim closePrices = candles.Skip(i - 19).Take(20).Select(Function(c) c.Close).ToList()
                Dim mean = closePrices.Average()
                Dim stdDev = Math.Sqrt(closePrices.Sum(Function(x) Math.Pow(x - mean, 2)) / 20)

                candles(i).bollingerUpper = mean + (stdDev * 2)
                candles(i).bollingerMiddle = mean
                candles(i).bollingerLower = mean - (stdDev * 2)
                candles(i).bollingerWidth = candles(i).bollingerUpper - candles(i).bollingerLower
            End If

            If i >= 14 Then
                Dim gains As Double = 0
                Dim losses As Double = 0

                For j As Integer = i - 13 To i
                    If j > 0 Then
                        Dim change = candles(j).Close - candles(j - 1).Close
                        If change > 0 Then
                            gains += change
                        Else
                            losses += Math.Abs(change)
                        End If
                    End If
                Next

                Dim avgGain = gains / 14
                Dim avgLoss = losses / 14

                If avgLoss = 0 Then
                    candles(i).rsi = 100
                Else
                    Dim rs = avgGain / avgLoss
                    candles(i).rsi = 100 - (100 / (1 + rs))
                End If
            End If

            If i >= 25 Then
                Dim ema12 = CalculateEMA(candles, i, 12)
                Dim ema26 = CalculateEMA(candles, i, 26)
                candles(i).macd = ema12 - ema26

                If i >= 33 Then
                    candles(i).macdSignal = CalculateMACDSignal(candles, i, 9)
                    candles(i).macdHistogram = candles(i).macd - candles(i).macdSignal
                End If
            End If

            If i >= 4 Then
                candles(i).volumeAvg5 = candles.Skip(i - 4).Take(5).Average(Function(c) c.Volume)
            End If
            If i >= 19 Then
                candles(i).volumeAvg20 = candles.Skip(i - 19).Take(20).Average(Function(c) c.Volume)
            End If
        Next
    End Sub

    Private Shared Function CalculateEMA(candles As List(Of CandleInfo), currentIndex As Integer, period As Integer) As Double
        If currentIndex < period - 1 Then Return 0

        Dim k As Double = 2.0 / (period + 1)
        Dim ema As Double = candles(currentIndex - period + 1).Close

        For i As Integer = currentIndex - period + 2 To currentIndex
            ema = (candles(i).Close * k) + (ema * (1 - k))
        Next

        Return ema
    End Function

    Private Shared Function CalculateMACDSignal(candles As List(Of CandleInfo), currentIndex As Integer, period As Integer) As Double
        If currentIndex < 25 + period - 1 Then Return 0

        Dim k As Double = 2.0 / (period + 1)
        Dim signal As Double = candles(currentIndex - period + 1).macd

        For i As Integer = currentIndex - period + 2 To currentIndex
            signal = (candles(i).macd * k) + (signal * (1 - k))
        Next

        Return signal
    End Function
End Class

#End Region

#Region "틱강도 지표"

''' <summary>
''' 틱강도 지표 - tickCount와 이동평균을 활용한 거래 활동성 분석 (타임프레임 정규화 적용)
''' </summary>
Public Class TickIntensityIndicator
    Implements IIndicator

    Private _cachedMetadataList As List(Of SeriesMetadata) = Nothing

    Public ReadOnly Property Name As String Implements IIndicator.Name
        Get
            Return "틱강도"
        End Get
    End Property

    Public ReadOnly Property Description As String Implements IIndicator.Description
        Get
            Return "틱 개수와 이동평균을 통한 거래 활동성 분석 (타임프레임 정규화)"
        End Get
    End Property

    Public Function Calculate(candles As List(Of CandleInfo)) As Dictionary(Of String, List(Of Double?)) Implements IIndicator.Calculate
        Dim result As New Dictionary(Of String, List(Of Double?))
        Dim tickCounts As New List(Of Double?)
        Dim tickAvg5List As New List(Of Double?)
        Dim tickAvg20List As New List(Of Double?)
        Dim tickAvg60List As New List(Of Double?)
        Dim intensityRatio As New List(Of Double?)

        ' 첫 번째 캔들의 타임프레임 확인
        Dim timeframe As String = If(candles.Count > 0, candles(0).timeframe, "")
        Dim isTickChart As Boolean = timeframe.StartsWith("T")

        For Each candle In candles
            ' 타임프레임별 정규화 처리
            If isTickChart Then
                ' 틱차트: 거래량 기반
                tickCounts.Add(If(candle.Volume > 0, candle.Volume, Nothing))
                tickAvg5List.Add(If(candle.volumeAvg5 > 0, candle.volumeAvg5, Nothing))
                tickAvg20List.Add(If(candle.volumeAvg20 > 0, candle.volumeAvg20, Nothing))

                ' 틱차트는 60평균 거래량을 계산하지 않으므로 tickAvg60 사용
                If candle.tickAvg60 > 0 Then
                    tickAvg60List.Add(CDbl(candle.tickAvg60))
                Else
                    tickAvg60List.Add(Nothing)
                End If

                ' 거래량 강도비율
                If candle.volumeAvg20 > 0 Then
                    intensityRatio.Add((candle.Volume / candle.volumeAvg20) * 10)
                Else
                    intensityRatio.Add(Nothing)
                End If
            Else
                ' 시간봉: 1분당 정규화된 틱수
                Dim minutes As Integer = GetTimeframeMinutes(candle.timeframe)

                If minutes > 0 Then
                    Dim normalizedTick = candle.tickCount / CDbl(minutes)
                    Dim normalizedAvg5 = If(candle.tickAvg5 > 0, candle.tickAvg5 / CDbl(minutes), 0)
                    Dim normalizedAvg20 = If(candle.tickAvg20 > 0, candle.tickAvg20 / CDbl(minutes), 0)
                    Dim normalizedAvg60 = If(candle.tickAvg60 > 0, candle.tickAvg60 / CDbl(minutes), 0)

                    tickCounts.Add(If(normalizedTick > 0, normalizedTick, Nothing))
                    tickAvg5List.Add(If(normalizedAvg5 > 0, normalizedAvg5, Nothing))
                    tickAvg20List.Add(If(normalizedAvg20 > 0, normalizedAvg20, Nothing))
                    tickAvg60List.Add(If(normalizedAvg60 > 0, normalizedAvg60, Nothing))

                    ' 정규화된 강도비율
                    If normalizedAvg20 > 0 Then
                        intensityRatio.Add((normalizedTick / normalizedAvg20) * 10)
                    Else
                        intensityRatio.Add(Nothing)
                    End If
                Else
                    ' 타임프레임 파싱 실패
                    tickCounts.Add(Nothing)
                    tickAvg5List.Add(Nothing)
                    tickAvg20List.Add(Nothing)
                    tickAvg60List.Add(Nothing)
                    intensityRatio.Add(Nothing)
                End If
            End If
        Next

        ' 데이터 이름을 타임프레임에 맞게 설정
        If isTickChart Then
            result("거래량") = tickCounts
            result("거래량5평균") = tickAvg5List
            result("거래량20평균") = tickAvg20List
            result("틱60평균") = tickAvg60List
        Else
            result("분당틱수") = tickCounts
            result("분당5평균") = tickAvg5List
            result("분당20평균") = tickAvg20List
            result("분당60평균") = tickAvg60List
        End If
        result("강도비율") = intensityRatio

        Return result
    End Function

    Private Function GetTimeframeMinutes(timeframe As String) As Integer
        If String.IsNullOrEmpty(timeframe) Then Return 0
        If Not timeframe.StartsWith("m") Then Return 0

        Dim numberPart = timeframe.Substring(1)
        Dim minutes As Integer
        If Integer.TryParse(numberPart, minutes) Then
            Return minutes
        End If
        Return 0
    End Function

    Public Function GetSeriesMetadata() As List(Of SeriesMetadata) Implements IIndicator.GetSeriesMetadata
        ' ⭐ 메타데이터 캐싱: 한 번만 생성하고 재사용
        If _cachedMetadataList Is Nothing Then
            _cachedMetadataList = New List(Of SeriesMetadata) From {
                New SeriesMetadata With {
                    .Name = "분당틱수",
                    .DisplayMode = ChartDisplayMode.Separate,
                    .ChartType = ChartType.Histogram,
                    .Color = Color.FromArgb(100, Color.Gray),
                    .LineWidth = 1.0F,
                    .PanelIndex = 3
                },
                New SeriesMetadata With {
                    .Name = "분당5평균",
                    .DisplayMode = ChartDisplayMode.Separate,
                    .ChartType = ChartType.Line,
                    .Color = Color.Yellow,
                    .LineWidth = 1.0F,
                    .PanelIndex = 3
                },
                New SeriesMetadata With {
                    .Name = "분당20평균",
                    .DisplayMode = ChartDisplayMode.Separate,
                    .ChartType = ChartType.Line,
                    .Color = Color.Cyan,
                    .LineWidth = 2.0F,
                    .PanelIndex = 3
                },
                New SeriesMetadata With {
                    .Name = "분당60평균",
                    .DisplayMode = ChartDisplayMode.Separate,
                    .ChartType = ChartType.Line,
                    .Color = Color.Orange,
                    .LineWidth = 1.0F,
                    .PanelIndex = 3
                },
                New SeriesMetadata With {
                    .Name = "강도비율",
                    .DisplayMode = ChartDisplayMode.Separate,
                    .ChartType = ChartType.Line,
                    .Color = Color.CornflowerBlue,
                    .LineWidth = 2.0F,
                    .PanelIndex = 3,
                    .ReferenceLine = 10.0,
                    .ReferenceLineColor = Color.Gray,
                    .ReferenceLineStyle = DashStyle.Dash
                },
                New SeriesMetadata With {
                    .Name = "거래량",
                    .DisplayMode = ChartDisplayMode.Separate,
                    .ChartType = ChartType.Histogram,
                    .Color = Color.FromArgb(100, Color.Gray),
                    .LineWidth = 1.0F,
                    .PanelIndex = 3
                },
                New SeriesMetadata With {
                    .Name = "거래량5평균",
                    .DisplayMode = ChartDisplayMode.Separate,
                    .ChartType = ChartType.Line,
                    .Color = Color.Yellow,
                    .LineWidth = 1.0F,
                    .PanelIndex = 3
                },
                New SeriesMetadata With {
                    .Name = "거래량20평균",
                    .DisplayMode = ChartDisplayMode.Separate,
                    .ChartType = ChartType.Line,
                    .Color = Color.Cyan,
                    .LineWidth = 2.0F,
                    .PanelIndex = 3
                },
                New SeriesMetadata With {
                    .Name = "틱60평균",
                    .DisplayMode = ChartDisplayMode.Separate,
                    .ChartType = ChartType.Line,
                    .Color = Color.Orange,
                    .LineWidth = 1.0F,
                    .PanelIndex = 3
                }
            }
        End If

        Return _cachedMetadataList
    End Function
End Class

#End Region

#Region "지표 설정 정보"

Public Class ReferenceLine
    Public Property Value As Double
    Public Property Color As Color = Color.Gray
    Public Property Style As DashStyle = DashStyle.Dash

    Public Overrides Function ToString() As String
        Return $"{Value:F2} ({Color.Name})"
    End Function
End Class
#End Region

'Public Class IndicatorInstance
'    Public Property IndicatorType As Type
'    Public Property Name As String
'    Public Property Parameters As Dictionary(Of String, Object)
'    Public Property Metadata As List(Of SeriesMetadata)
'    Public Property IsVisible As Boolean = True

'    ' 새로운 속성들
'    Public Property ZOrder As Integer = 0
'    Public Property ReferenceLines As List(Of ReferenceLine)
'    Public Property OverboughtLevel As Double? = Nothing
'    Public Property OversoldLevel As Double? = Nothing
'    Public Property EnableZones As Boolean = False
'    Public Property EnableDivergence As Boolean = False
'    Public Property YAxisMin As Double? = Nothing
'    Public Property YAxisMax As Double? = Nothing
'    Public Property AutoScale As Boolean = True
'    Public Property LineWidth As Single = 2.0F
'    Public Property DisplayMode As ChartDisplayMode = ChartDisplayMode.Overlay

'    ' 알림 설정
'    Public Property AlertEnabled As Boolean = False
'    Public Property AlertCondition As String = ""
'    Public Property AlertValue As Double = 0

'    Public Sub New()
'        Parameters = New Dictionary(Of String, Object)
'        ReferenceLines = New List(Of ReferenceLine)
'    End Sub

'    ' GetKey() 메서드 수정 - 안정적인 키 생성
'    Public Function GetKey() As String
'        ' 지표 타입과 이름만으로 키 생성 (Parameters 제외)
'        Return Name '$"{IndicatorType.Name}_{Name}"
'    End Function

'    'Public Function CreateIndicator() As IIndicator
'    '    Dim indicator As IIndicator = Nothing

'    '    If IndicatorType Is GetType(SMAIndicator) Then
'    '        Dim period = If(Parameters.ContainsKey("period"), CInt(Parameters("period")), 5)
'    '        Dim color As Color = If(Parameters.ContainsKey("color"), CType(Parameters("color"), Color), Color.Yellow)
'    '        indicator = New SMAIndicator(period, color)

'    '    ElseIf IndicatorType Is GetType(RSIIndicator) Then
'    '        Dim period = If(Parameters.ContainsKey("period"), CInt(Parameters("period")), 14)
'    '        Dim color As Color = If(Parameters.ContainsKey("color"), CType(Parameters("color"), Color), Color.Purple)
'    '        indicator = New RSIIndicator(period, color)

'    '    ElseIf IndicatorType Is GetType(MACDIndicator) Then
'    '        indicator = New MACDIndicator()

'    '    ElseIf IndicatorType Is GetType(TickIntensityIndicator) Then
'    '        indicator = New TickIntensityIndicator()
'    '    End If

'    '    ' 고급 설정 적용
'    '    If indicator IsNot Nothing Then
'    '        Dim metadata = indicator.GetSeriesMetadata()
'    '        For Each meta In metadata
'    '            ApplyAdvancedSettings(meta)
'    '        Next
'    '    End If

'    '    Return indicator
'    'End Function

'    ' CreateIndicator 수정 - Name 업데이트 반영
'    Public Function CreateIndicator() As IIndicator
'        Dim indicator As IIndicator = Nothing

'        If IndicatorType Is GetType(SMAIndicator) Then
'            Dim period = If(Parameters.ContainsKey("period"), CInt(Parameters("period")), 5)
'            Dim color As Color = If(Parameters.ContainsKey("color"), CType(Parameters("color"), Color), Color.Yellow)
'            indicator = New SMAIndicator(period, color)

'            ' Name 업데이트 (기간이 변경된 경우)
'            If Name.StartsWith("SMA") OrElse Name.StartsWith("EMA") Then
'                Dim prefix = If(Name.StartsWith("EMA"), "EMA", "SMA")
'                Name = $"{prefix}({period})"
'            End If

'        ElseIf IndicatorType Is GetType(RSIIndicator) Then
'            Dim period = If(Parameters.ContainsKey("period"), CInt(Parameters("period")), 14)
'            Dim color As Color = If(Parameters.ContainsKey("color"), CType(Parameters("color"), Color), Color.Purple)
'            indicator = New RSIIndicator(period, color)

'            ' Name 업데이트
'            Name = $"RSI({period})"

'        ElseIf IndicatorType Is GetType(MACDIndicator) Then
'            indicator = New MACDIndicator()

'        ElseIf IndicatorType Is GetType(TickIntensityIndicator) Then
'            indicator = New TickIntensityIndicator()
'        End If

'        ' 고급 설정 적용
'        If indicator IsNot Nothing Then
'            Dim metadataList = indicator.GetSeriesMetadata()
'            For Each meta In metadataList
'                ApplyAdvancedSettings(meta)
'            Next
'        End If

'        Return indicator
'    End Function



'    Public Sub ApplyAdvancedSettings(meta As SeriesMetadata)
'        ' Y축 범위 적용
'        If Not AutoScale Then
'            meta.AxisInfo.AutoScale = False
'            If YAxisMin.HasValue Then meta.AxisInfo.Min = YAxisMin.Value
'            If YAxisMax.HasValue Then meta.AxisInfo.Max = YAxisMax.Value
'        Else
'            meta.AxisInfo.AutoScale = True
'        End If

'        ' 선 굵기 적용
'        meta.LineWidth = LineWidth

'        ' 표시 모드 적용
'        meta.DisplayMode = DisplayMode

'        ' 과열/침체 구간 적용 (수정)
'        meta.EnableZones = EnableZones
'        If EnableZones Then
'            If OverboughtLevel.HasValue Then
'                meta.OverboughtLevel = OverboughtLevel.Value
'            End If
'            If OversoldLevel.HasValue Then
'                meta.OversoldLevel = OversoldLevel.Value
'            End If
'        End If

'        ' 다이버전스 적용
'        meta.EnableDivergence = EnableDivergence

'        ' 기준선 적용
'        If ReferenceLines IsNot Nothing AndAlso ReferenceLines.Count > 0 Then
'            Dim firstRefLine = ReferenceLines(0)
'            meta.ReferenceLine = firstRefLine.Value
'            meta.ReferenceLineColor = firstRefLine.Color
'            meta.ReferenceLineStyle = firstRefLine.Style
'        End If
'    End Sub
'End Class

'Public Class IndicatorPreset
'    Public Property Name As String
'    Public Property Indicators As List(Of IndicatorInstance)

'    Public Sub New()
'        Indicators = New List(Of IndicatorInstance)
'    End Sub
'End Class

'Public Class DefaultIndicators
'    Public Shared Function GetDefaultIndicators() As List(Of IndicatorInstance)
'        Dim defaults As New List(Of IndicatorInstance)

'        defaults.Add(New IndicatorInstance With {
'            .IndicatorType = GetType(SMAIndicator),
'            .Name = "SMA(5)",
'            .Parameters = New Dictionary(Of String, Object) From {
'                {"period", 5},
'                {"color", Color.Yellow}
'            },
'            .IsVisible = True,
'            .DisplayMode = ChartDisplayMode.Overlay,
'            .LineWidth = 2.0F
'        })

'        defaults.Add(New IndicatorInstance With {
'            .IndicatorType = GetType(SMAIndicator),
'            .Name = "SMA(20)",
'            .Parameters = New Dictionary(Of String, Object) From {
'                {"period", 20},
'                {"color", Color.Cyan}
'            },
'            .IsVisible = True,
'            .DisplayMode = ChartDisplayMode.Overlay,
'            .LineWidth = 2.0F
'        })

'        defaults.Add(New IndicatorInstance With {
'            .IndicatorType = GetType(RSIIndicator),
'            .Name = "RSI(14)",
'            .Parameters = New Dictionary(Of String, Object),
'            .IsVisible = False,
'            .DisplayMode = ChartDisplayMode.Separate,
'            .AutoScale = False,
'            .YAxisMin = 0,
'            .YAxisMax = 100,
'            .OverboughtLevel = 70,
'            .OversoldLevel = 30,
'            .EnableZones = True
'        })
'        defaults(2).ReferenceLines.Add(New ReferenceLine With {.Value = 50, .Color = Color.Gray, .Style = DashStyle.Dash})
'        defaults(2).ReferenceLines.Add(New ReferenceLine With {.Value = 70, .Color = Color.Red, .Style = DashStyle.Dot})
'        defaults(2).ReferenceLines.Add(New ReferenceLine With {.Value = 30, .Color = Color.Blue, .Style = DashStyle.Dot})

'        defaults.Add(New IndicatorInstance With {
'            .IndicatorType = GetType(TickIntensityIndicator),
'            .Name = "틱강도",
'            .Parameters = New Dictionary(Of String, Object),
'            .IsVisible = False,
'            .DisplayMode = ChartDisplayMode.Separate,
'            .OverboughtLevel = 1.5,
'            .OversoldLevel = 0.5,
'            .EnableZones = True
'        })
'        defaults(3).ReferenceLines.Add(New ReferenceLine With {.Value = 1.0, .Color = Color.Gray, .Style = DashStyle.Dash})

'        Return defaults
'    End Function

'    Public Shared Function GetScalpingPreset() As IndicatorPreset
'        Dim preset As New IndicatorPreset With {.Name = "단타용"}

'        preset.Indicators.Add(New IndicatorInstance With {
'            .IndicatorType = GetType(SMAIndicator),
'            .Name = "SMA(5)",
'            .Parameters = New Dictionary(Of String, Object) From {{"period", 5}, {"color", Color.Yellow}},
'            .IsVisible = True,
'            .DisplayMode = ChartDisplayMode.Overlay
'        })

'        preset.Indicators.Add(New IndicatorInstance With {
'            .IndicatorType = GetType(TickIntensityIndicator),
'            .Name = "틱강도",
'            .Parameters = New Dictionary(Of String, Object),
'            .IsVisible = True,
'            .DisplayMode = ChartDisplayMode.Separate,
'            .EnableZones = True,
'            .OverboughtLevel = 1.5,
'            .OversoldLevel = 0.5
'        })

'        Return preset
'    End Function

'    Public Shared Function GetSwingPreset() As IndicatorPreset
'        Dim preset As New IndicatorPreset With {.Name = "스윙용"}

'        preset.Indicators.Add(New IndicatorInstance With {
'            .IndicatorType = GetType(SMAIndicator),
'            .Name = "SMA(20)",
'            .Parameters = New Dictionary(Of String, Object) From {{"period", 20}, {"color", Color.Cyan}},
'            .IsVisible = True
'        })

'        preset.Indicators.Add(New IndicatorInstance With {
'            .IndicatorType = GetType(SMAIndicator),
'            .Name = "SMA(60)",
'            .Parameters = New Dictionary(Of String, Object) From {{"period", 60}, {"color", Color.Orange}},
'            .IsVisible = True
'        })

'        preset.Indicators.Add(New IndicatorInstance With {
'            .IndicatorType = GetType(RSIIndicator),
'            .Name = "RSI(14)",
'            .IsVisible = True,
'            .DisplayMode = ChartDisplayMode.Separate,
'            .EnableZones = True
'        })

'        preset.Indicators.Add(New IndicatorInstance With {
'            .IndicatorType = GetType(MACDIndicator),
'            .Name = "MACD",
'            .IsVisible = True,
'            .DisplayMode = ChartDisplayMode.Separate
'        })

'        Return preset
'    End Function
'End Class

'#End Region