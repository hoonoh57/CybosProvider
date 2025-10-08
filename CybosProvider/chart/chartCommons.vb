Option Strict On
Option Explicit On
Option Infer On

Imports System
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.ComponentModel
Imports System.Linq
Imports System.Collections.Generic

' =====================================================================================
'  Part 1: 공통 타입(캔들/참조선/축/시리즈/패널) + 인디케이터 인터페이스/구현(SMA, RSI, MACD)
'  - 깨진 주석(문자깨짐)은 모두 의미에 맞게 한국어로 복원/보완하였습니다.
'  - 본 파일은 차트 렌더링 컨트롤(HighPerformanceChartControl) 없이도 단독 컴파일됩니다.
'  - 차트 컨트롤 본체는 Part 2(HighPerformanceChartControl)에서 제공합니다.
' =====================================================================================

#Region "공통 데이터 타입 (Candle/ReferenceLine)"

'''' <summary>
'''' 캔들(시가/고가/저가/종가/거래량/시각) + MACD 파생값(옵션)
'''' </summary>
'Public Structure CandleInfo
'    Public Property Timestamp As DateTime  ' 시간(X축)
'    Public Property Open As Double         ' 시가
'    Public Property High As Double         ' 고가
'    Public Property Low As Double          ' 저가
'    Public Property Close As Double        ' 종가
'    Public Property Volume As Double       ' 거래량

'    ' ▼ 필요 시 외부(지표 엔진)에서 채워 넣는 파생 필드들
'    Public Property macd As Double         ' MACD 라인 값
'    Public Property macdSignal As Double   ' 시그널 값
'    Public Property macdHistogram As Double ' 히스토그램 값 (macd - signal)
'End Structure

'''' <summary>
'''' 보조선(기준선) 정의: 값/색상/선스타일
'''' </summary>
'Public Class ReferenceLine
'    Public Property Value As Double
'    Public Property Color As Color = Color.Gray
'    Public Property Style As DashStyle = DashStyle.Dash
'End Class

#End Region

#Region "차트 모드/타입 열거형"

''' <summary>시리즈 표시 방식: 메인 패널 오버레이 vs 별도 패널</summary>
Public Enum ChartDisplayMode
    Overlay
    Separate
End Enum

''' <summary>시리즈 차트 타입</summary>
Public Enum ChartType
    Line
    Candlestick
    Bar
    Area
    Histogram
    Scatter
End Enum

''' <summary>축의 위치</summary>
Public Enum AxisPosition
    Left
    Right
    Top
    Bottom
End Enum

#End Region

#Region "축/시리즈/패널 메타데이터"

''' <summary>
''' 축 정보(Y축 중심): 자동 범위/그리드/라벨 포맷 등
''' </summary>
Public Class AxisInfo
    Public Property Title As String = ""
    Public Property Min As Double? = Nothing
    Public Property Max As Double? = Nothing
    Public Property AutoScale As Boolean = True
    Public Property GridLines As Boolean = True
    Public Property GridColor As Color = Color.FromArgb(30, 128, 128, 128)
    Public Property LabelFormat As String = "F2"
    Public Property Position As AxisPosition = AxisPosition.Left
End Class

''' <summary>
''' 시리즈(한 지표/선/막대 등)를 그리기 위한 메타데이터
''' </summary>
Public Class SeriesMetadata
    Public Property Name As String = ""
    Public Property DisplayMode As ChartDisplayMode = ChartDisplayMode.Overlay
    Public Property ChartType As ChartType = ChartType.Line
    Public Property Color As Color = Color.Blue
    Public Property LineStyle As DashStyle = DashStyle.Solid
    Public Property LineWidth As Single = 2.0F
    Public Property FillColor As Color = Color.Transparent
    Public Property FillOpacity As Integer = 50
    Public Property Visible As Boolean = True
    Public Property ZOrder As Integer = 0

    ' 단일 기준선
    Public Property ReferenceLine As Double? = Nothing
    Public Property ReferenceLineColor As Color = Color.White
    Public Property ReferenceLineStyle As DashStyle = DashStyle.Dash

    ' 다중 기준선
    Public Property ReferenceLines As List(Of ReferenceLine)

    ' 전용 축 설정(미지정 시 패널 Y축을 사용)
    Public Property AxisInfo As AxisInfo

    ' 별도 패널 인덱스(Overlay면 0 고정, Separate면 ≥1)
    Public Property PanelIndex As Integer = 0

    ' (선택) 데이터 선택자: Candle → 값
    Public Property DataSelector As Func(Of CandleInfo, Double?)

    ' (선택) 과열/침체 영역
    Public Property OverboughtLevel As Double? = Nothing
    Public Property OversoldLevel As Double? = Nothing
    Public Property EnableZones As Boolean = False

    ' (선택) 다이버전스 탐지 스위치(렌더링 시 활용 가능)
    Public Property EnableDivergence As Boolean = False

    Public Sub New()
        AxisInfo = New AxisInfo()
        ReferenceLines = New List(Of ReferenceLine)()
    End Sub
End Class

''' <summary>
''' 패널(메인/보조) 정의: 제목, 높이 비율, 배경색, 보유 시리즈 등
''' </summary>
Public Class ChartPanel
    Public Property Index As Integer
    Public Property Title As String = ""
    Public Property HeightRatio As Double = 1.0
    Public Property BackColor As Color = Color.Black
    Public Property Series As New List(Of SeriesMetadata)()
    Public Property YAxis As New AxisInfo()
End Class

#End Region

#Region "인디케이터 인터페이스"

''' <summary>
''' 모든 인디케이터가 구현해야 하는 표준 인터페이스
''' </summary>
Public Interface IIndicator
    ReadOnly Property Name As String
    ReadOnly Property Description As String
    ''' <summary>
    ''' 캔들 배열을 받아 {시리즈명 → 값 목록} 사전 반환
    ''' </summary>
    Function Calculate(candles As List(Of CandleInfo)) As Dictionary(Of String, List(Of Double?))
    ''' <summary>
    ''' 렌더링에 필요한 시리즈 메타데이터(1개 이상) 반환
    ''' </summary>
    Function GetSeriesMetadata() As List(Of SeriesMetadata)
End Interface

#End Region

#Region "SMA(단순이동평균) 인디케이터"

''' <summary>
''' SMA(n): n개 종가 평균
''' </summary>
Public Class SMAIndicator
    Implements IIndicator

    Private ReadOnly _period As Integer
    Private ReadOnly _color As Color
    Private _cachedMetadata As SeriesMetadata = Nothing

    Public Sub New(period As Integer, Optional color As Color = Nothing)
        _period = period
        _color = If(color.IsEmpty, Color.Yellow, color)
    End Sub

    Public ReadOnly Property Name As String Implements IIndicator.Name
        Get
            Return $"SMA({_period})"
        End Get
    End Property

    Public ReadOnly Property Description As String Implements IIndicator.Description
        Get
            Return $"{_period} 기간 단순이동평균"
        End Get
    End Property

    Public Function Calculate(candles As List(Of CandleInfo)) As Dictionary(Of String, List(Of Double?)) _
        Implements IIndicator.Calculate

        Dim result As New Dictionary(Of String, List(Of Double?))()
        Dim smaValues As New List(Of Double?)()

        For i = 0 To candles.Count - 1
            If i >= _period - 1 Then
                Dim sum = candles.Skip(i - _period + 1).Take(_period).Sum(Function(c) c.Close)
                smaValues.Add(sum / _period)
            Else
                smaValues.Add(Nothing)
            End If
        Next

        result(Me.Name) = smaValues
        Return result
    End Function

    Public Function GetSeriesMetadata() As List(Of SeriesMetadata) Implements IIndicator.GetSeriesMetadata
        If _cachedMetadata Is Nothing Then
            _cachedMetadata = New SeriesMetadata With {
                .Name = Me.Name,
                .DisplayMode = ChartDisplayMode.Overlay,
                .ChartType = ChartType.Line,
                .Color = _color,
                .LineWidth = 2.0F,
                .PanelIndex = 0
            }
        End If

        Return New List(Of SeriesMetadata) From {_cachedMetadata}
    End Function
End Class

#End Region

#Region "RSI 인디케이터"

''' <summary>
''' RSI(n): 상승/하락 평균비로 모멘텀 측정 (0~100)
''' </summary>
Public Class RSIIndicator
    Implements IIndicator

    Private ReadOnly _period As Integer
    Private ReadOnly _color As Color
    Private _cachedMetadata As SeriesMetadata = Nothing

    Public Sub New(Optional period As Integer = 14, Optional color As Color = Nothing)
        _period = period
        _color = If(color.IsEmpty OrElse color = Color.Transparent, Color.Purple, color)
    End Sub

    Public ReadOnly Property Name As String Implements IIndicator.Name
        Get
            Return $"RSI({_period})"
        End Get
    End Property

    Public ReadOnly Property Description As String Implements IIndicator.Description
        Get
            Return $"{_period} 기간 RSI"
        End Get
    End Property

    Public Function Calculate(candles As List(Of CandleInfo)) As Dictionary(Of String, List(Of Double?)) _
        Implements IIndicator.Calculate

        Dim result As New Dictionary(Of String, List(Of Double?))()
        Dim rsiValues As New List(Of Double?)()

        For i = 0 To candles.Count - 1
            If i >= _period Then
                Dim gains As Double = 0
                Dim losses As Double = 0

                For j = i - _period + 1 To i
                    If j > 0 Then
                        Dim change = candles(j).Close - candles(j - 1).Close
                        If change > 0 Then
                            gains += change
                        Else
                            losses += Math.Abs(change)
                        End If
                    End If
                Next

                Dim avgGain = gains / _period
                Dim avgLoss = losses / _period

                If avgLoss = 0 Then
                    rsiValues.Add(100)
                Else
                    Dim rs = avgGain / avgLoss
                    rsiValues.Add(100 - (100 / (1 + rs)))
                End If
            Else
                rsiValues.Add(Nothing)
            End If
        Next

        result(Me.Name) = rsiValues
        Return result
    End Function

    Public Function GetSeriesMetadata() As List(Of SeriesMetadata) Implements IIndicator.GetSeriesMetadata
        If _cachedMetadata Is Nothing Then
            _cachedMetadata = New SeriesMetadata With {
                .Name = Me.Name,
                .DisplayMode = ChartDisplayMode.Separate,
                .ChartType = ChartType.Line,
                .Color = _color,
                .LineWidth = 2.0F,
                .PanelIndex = 1
            }

            ' RSI는 0~100 고정 스케일
            _cachedMetadata.AxisInfo.Min = 0
            _cachedMetadata.AxisInfo.Max = 100
            _cachedMetadata.AxisInfo.AutoScale = False

            ' 기준선(50)
            _cachedMetadata.ReferenceLine = 50
            _cachedMetadata.ReferenceLineColor = Color.Gray
            _cachedMetadata.ReferenceLineStyle = DashStyle.Dash

            _cachedMetadata.ReferenceLines.Clear()
        End If

        Return New List(Of SeriesMetadata) From {_cachedMetadata}
    End Function
End Class

#End Region

#Region "MACD 인디케이터"

''' <summary>
''' MACD(12,26,9) - 데이터는 CandleInfo의 macd/macdSignal/macdHistogram 필드에서 읽음
''' </summary>
Public Class MACDIndicator
    Implements IIndicator

    Private _cachedMetadataList As List(Of SeriesMetadata) = Nothing

    Public ReadOnly Property Name As String Implements IIndicator.Name
        Get
            Return "MACD"
        End Get
    End Property

    Public ReadOnly Property Description As String Implements IIndicator.Description
        Get
            Return "MACD (12,26,9)"
        End Get
    End Property

    Public Function Calculate(candles As List(Of CandleInfo)) As Dictionary(Of String, List(Of Double?)) _
        Implements IIndicator.Calculate

        Dim result As New Dictionary(Of String, List(Of Double?))()
        Dim macdValues As New List(Of Double?)()
        Dim signalValues As New List(Of Double?)()
        Dim histogramValues As New List(Of Double?)()

        For Each candle In candles
            ' 0은 미계산으로 간주해 Nothing 처리 (시각적으로 공백 구간을 허용)
            macdValues.Add(If(candle.macd <> 0, candle.macd, Nothing))
            signalValues.Add(If(candle.macdSignal <> 0, candle.macdSignal, Nothing))
            histogramValues.Add(If(candle.macdHistogram <> 0, candle.macdHistogram, Nothing))
        Next

        result("MACD") = macdValues
        result("Signal") = signalValues
        result("Histogram") = histogramValues
        Return result
    End Function

    Public Function GetSeriesMetadata() As List(Of SeriesMetadata) Implements IIndicator.GetSeriesMetadata
        If _cachedMetadataList Is Nothing Then
            _cachedMetadataList = New List(Of SeriesMetadata) From {
                New SeriesMetadata With {
                    .Name = "MACD",
                    .DisplayMode = ChartDisplayMode.Separate,
                    .ChartType = ChartType.Line,
                    .Color = Color.Orange,
                    .LineWidth = 2.0F,
                    .PanelIndex = 2,
                    .ReferenceLine = 0
                },
                New SeriesMetadata With {
                    .Name = "Signal",
                    .DisplayMode = ChartDisplayMode.Separate,
                    .ChartType = ChartType.Line,
                    .Color = Color.Lime,
                    .LineWidth = 1.0F,
                    .PanelIndex = 2
                },
                New SeriesMetadata With {
                    .Name = "Histogram",
                    .DisplayMode = ChartDisplayMode.Separate,
                    .ChartType = ChartType.Histogram,
                    .Color = Color.Gray,
                    .LineWidth = 1.0F,
                    .PanelIndex = 2
                }
            }
        End If

        Return _cachedMetadataList
    End Function
End Class

#End Region
