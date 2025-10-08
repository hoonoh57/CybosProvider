Imports System.Drawing.Drawing2D

#Region "지표 인스턴스 클래스"

''' <summary>
''' 지표 인스턴스 - 지표의 모든 설정 정보를 담는 클래스
''' INI 파일로 저장/로드 가능
''' </summary>
Public Class IndicatorInstance
    ' ===== 기본 정보 =====

    ''' <summary>지표 타입 (SMAIndicator, RSIIndicator 등)</summary>
    Public Property IndicatorType As Type

    ''' <summary>지표 이름 (예: "SMA(5)", "RSI(14)")</summary>
    Public Property Name As String

    ''' <summary>지표 파라미터 (period, color 등)</summary>
    Public Property Parameters As Dictionary(Of String, Object)

    ''' <summary>지표 메타데이터 (선택 사항)</summary>
    Public Property Metadata As List(Of SeriesMetadata)

    ''' <summary>화면 표시 여부</summary>
    Public Property IsVisible As Boolean = True

    ''' <summary>그리기 순서 (낮을수록 먼저 그림)</summary>
    Public Property ZOrder As Integer = 0

    ' ===== 표시 설정 =====

    ''' <summary>선 굵기</summary>
    Public Property LineWidth As Single = 2.0F

    ''' <summary>표시 모드 (오버레이 / 독립 패널)</summary>
    Public Property DisplayMode As ChartDisplayMode = ChartDisplayMode.Overlay

    ' ===== Y축 설정 =====

    ''' <summary>Y축 자동 스케일 여부</summary>
    Public Property AutoScale As Boolean = True

    ''' <summary>Y축 최소값 (수동 스케일 시)</summary>
    Public Property YAxisMin As Double? = Nothing

    ''' <summary>Y축 최대값 (수동 스케일 시)</summary>
    Public Property YAxisMax As Double? = Nothing

    ' ===== 기준선 =====

    ''' <summary>기준선 목록 (여러 개 가능)</summary>
    Public Property ReferenceLines As List(Of ReferenceLine)

    ' ===== 과열/침체 구간 =====

    ''' <summary>과열/침체 구간 표시 여부</summary>
    Public Property EnableZones As Boolean = False

    ''' <summary>과열 레벨 (예: RSI 70)</summary>
    Public Property OverboughtLevel As Double? = Nothing

    ''' <summary>침체 레벨 (예: RSI 30)</summary>
    Public Property OversoldLevel As Double? = Nothing

    ' ===== 다이버전스 =====

    ''' <summary>다이버전스 자동 표시 여부 (향후 구현)</summary>
    Public Property EnableDivergence As Boolean = False

    ' ===== 알림 설정 =====

    ''' <summary>알림 활성화 여부 (향후 구현)</summary>
    Public Property AlertEnabled As Boolean = False

    ''' <summary>알림 조건 ("값이 초과", "값이 미만" 등)</summary>
    Public Property AlertCondition As String = ""

    ''' <summary>알림 기준값</summary>
    Public Property AlertValue As Double = 0

    ''' <summary>
    ''' 생성자
    ''' </summary>
    Public Sub New()
        Parameters = New Dictionary(Of String, Object)
        ReferenceLines = New List(Of ReferenceLine)
    End Sub

    ''' <summary>
    ''' 고유 키 생성 (Dictionary 키로 사용)
    ''' Name을 키로 사용하여 간단하고 명확하게 관리
    ''' </summary>
    Public Function GetKey() As String
        Return Name
    End Function

    ''' <summary>
    ''' 실제 지표 객체 생성
    ''' 저장된 설정을 적용한 IIndicator 인스턴스 반환
    ''' </summary>
    Public Function CreateIndicator() As IIndicator
        Dim indicator As IIndicator = Nothing

        ' ===== 지표 타입별로 생성 =====

        If IndicatorType Is GetType(SMAIndicator) Then
            ' SMA / EMA 지표
            Dim period = If(Parameters.ContainsKey("period"), CInt(Parameters("period")), 5)
            Dim color As Color = If(Parameters.ContainsKey("color"), CType(Parameters("color"), Color), Color.Yellow)
            indicator = New SMAIndicator(period, color)

            ' Name 업데이트 (기간 변경 시)
            If Name.StartsWith("SMA") OrElse Name.StartsWith("EMA") Then
                Dim prefix = If(Name.StartsWith("EMA"), "EMA", "SMA")
                Name = $"{prefix}({period})"
            End If

        ElseIf IndicatorType Is GetType(RSIIndicator) Then
            ' RSI 지표
            Dim period = If(Parameters.ContainsKey("period"), CInt(Parameters("period")), 14)
            Dim color As Color = If(Parameters.ContainsKey("color"), CType(Parameters("color"), Color), Color.Purple)
            indicator = New RSIIndicator(period, color)

            ' Name 업데이트
            Name = $"RSI({period})"

        ElseIf IndicatorType Is GetType(MACDIndicator) Then
            ' MACD 지표
            indicator = New MACDIndicator()

        ElseIf IndicatorType Is GetType(TickIntensityIndicator) Then
            ' 틱강도 지표
            indicator = New TickIntensityIndicator()
        End If

        ' ===== 고급 설정 적용 =====

        If indicator IsNot Nothing Then
            Dim metadataList = indicator.GetSeriesMetadata()

            ' 각 시리즈 메타데이터에 설정 적용
            For Each meta In metadataList
                ApplyAdvancedSettings(meta)
            Next
        End If

        Return indicator
    End Function

    ''' <summary>
    ''' 시리즈 메타데이터에 고급 설정 적용
    ''' </summary>
    Public Sub ApplyAdvancedSettings(meta As SeriesMetadata)
        ' ===== Y축 범위 =====

        If AutoScale Then
            ' 자동 스케일
            meta.AxisInfo.AutoScale = True
            meta.AxisInfo.Min = Nothing
            meta.AxisInfo.Max = Nothing
        Else
            ' 수동 스케일
            meta.AxisInfo.AutoScale = False
            If YAxisMin.HasValue Then
                meta.AxisInfo.Min = YAxisMin.Value
            End If
            If YAxisMax.HasValue Then
                meta.AxisInfo.Max = YAxisMax.Value
            End If
        End If

        ' ===== 선 굵기 =====

        meta.LineWidth = LineWidth

        ' ===== 표시 모드 =====

        meta.DisplayMode = DisplayMode

        ' ===== 과열/침체 구간 =====

        ' ⭐ 중요: EnableZones도 전달해야 DrawSeries에서 그려짐
        meta.EnableZones = EnableZones

        If EnableZones Then
            meta.OverboughtLevel = OverboughtLevel
            meta.OversoldLevel = OversoldLevel
        Else
            meta.OverboughtLevel = Nothing
            meta.OversoldLevel = Nothing
        End If

        ' ===== 다이버전스 =====

        meta.EnableDivergence = EnableDivergence

        ' ===== 기준선 - 여러 개 지원 =====

        ''Logger.Instance.log($"[디버그] ApplyAdvancedSettings: {Name}, ReferenceLines.Count={ReferenceLines.Count}")

        ' 기존 단일 ReferenceLine도 유지 (하위 호환성)
        If ReferenceLines IsNot Nothing AndAlso ReferenceLines.Count > 0 Then
            Dim firstRef = ReferenceLines(0)
            meta.ReferenceLine = firstRef.Value
            meta.ReferenceLineColor = firstRef.Color
            meta.ReferenceLineStyle = firstRef.Style

            ''Logger.Instance.log($"[디버그]   첫 번째 기준선: Value={firstRef.Value}, Color={firstRef.Color.Name}, Style={firstRef.Style}")

            ' ⭐ 모든 기준선을 SeriesMetadata의 ReferenceLines 리스트에 복사
            meta.ReferenceLines.Clear()
            For i = 0 To ReferenceLines.Count - 1
                Dim refLine = ReferenceLines(i)
                meta.ReferenceLines.Add(New ReferenceLine With {
                    .Value = refLine.Value,
                    .Color = refLine.Color,
                    .Style = refLine.Style
                })
                ''Logger.Instance.log($"[디버그]   기준선[{i}] 복사: Value={refLine.Value}, Color={refLine.Color.Name} (A={refLine.Color.A}), Style={refLine.Style}")
            Next

            ''Logger.Instance.log($"[디버그]   meta.ReferenceLines.Count={meta.ReferenceLines.Count}")
        Else
            meta.ReferenceLine = Nothing
            meta.ReferenceLines.Clear()
            ''Logger.Instance.log($"[디버그]   기준선 없음")
        End If
    End Sub
End Class

#End Region

'#Region "기준선 클래스"

'''' <summary>
'''' 기준선 정보
'''' </summary>
'Public Class ReferenceLine
'    ''' <summary>기준선 값</summary>
'    Public Property Value As Double

'    ''' <summary>기준선 색상</summary>
'    Public Property Color As Color = Color.Gray

'    ''' <summary>기준선 스타일 (실선, 점선, 대시 등)</summary>
'    Public Property Style As DashStyle = DashStyle.Dash

'    Public Overrides Function ToString() As String
'        Return $"{Value:F2} ({Color.Name})"
'    End Function
'End Class

'#End Region

#Region "기본 지표 설정"

''' <summary>
''' 기본 지표 설정 제공
''' </summary>
Public Class DefaultIndicators
    ''' <summary>
    ''' 기본 지표 목록 생성
    ''' </summary>
    Public Shared Function GetDefaultIndicators() As List(Of IndicatorInstance)
        Dim defaults As New List(Of IndicatorInstance)

        ' ===== SMA(5) =====
        defaults.Add(New IndicatorInstance With {
            .IndicatorType = GetType(SMAIndicator),
            .Name = "SMA(5)",
            .Parameters = New Dictionary(Of String, Object) From {
                {"period", 5},
                {"color", Color.Yellow}
            },
            .IsVisible = True,
            .DisplayMode = ChartDisplayMode.Overlay,
            .LineWidth = 2.0F,
            .AutoScale = True,
            .ZOrder = 0
        })

        ' ===== SMA(20) =====
        defaults.Add(New IndicatorInstance With {
            .IndicatorType = GetType(SMAIndicator),
            .Name = "SMA(20)",
            .Parameters = New Dictionary(Of String, Object) From {
                {"period", 20},
                {"color", Color.Cyan}
            },
            .IsVisible = True,
            .DisplayMode = ChartDisplayMode.Overlay,
            .LineWidth = 2.0F,
            .AutoScale = True,
            .ZOrder = 1
        })

        ' ===== RSI(14) =====
        Dim rsi As New IndicatorInstance With {
            .IndicatorType = GetType(RSIIndicator),
            .Name = "RSI(14)",
            .Parameters = New Dictionary(Of String, Object) From {
                {"period", 14},
                {"color", Color.Purple}
            },
            .IsVisible = False,
            .DisplayMode = ChartDisplayMode.Separate,
            .AutoScale = False,
            .YAxisMin = 0,
            .YAxisMax = 100,
            .OverboughtLevel = 70,
            .OversoldLevel = 30,
            .EnableZones = True,
            .LineWidth = 2.0F,
            .ZOrder = 2
        }

        ' ⭐ 기준선 추가 (50, 70, 30)
        rsi.ReferenceLines.Add(New ReferenceLine With {
            .Value = 50,
            .Color = Color.Gray,
            .Style = DashStyle.Dash
        })
        rsi.ReferenceLines.Add(New ReferenceLine With {
            .Value = 70,
            .Color = Color.Red,
            .Style = DashStyle.Dash
        })
        rsi.ReferenceLines.Add(New ReferenceLine With {
            .Value = 30,
            .Color = Color.Blue,
            .Style = DashStyle.Dash
        })

        defaults.Add(rsi)

        ' ===== 틱강도 =====
        Dim tickIntensity As New IndicatorInstance With {
            .IndicatorType = GetType(TickIntensityIndicator),
            .Name = "틱강도",
            .Parameters = New Dictionary(Of String, Object),
            .IsVisible = False,
            .DisplayMode = ChartDisplayMode.Separate,
                    .AutoScale = True,
                    .OverboughtLevel = 15,
                    .OversoldLevel = 5,
                    .EnableZones = True, .ZOrder = 3
        }

        ' 기준선 추가
        tickIntensity.ReferenceLines.Add(New ReferenceLine With {
            .Value = 10.0,
            .Color = Color.Gray,
            .Style = DashStyle.Dash
        })

        defaults.Add(tickIntensity)

        Return defaults
    End Function
End Class

#End Region