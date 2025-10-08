Imports System.IO
Imports System.Text
Imports System.Drawing
Imports System.Drawing.Drawing2D

#Region "INI 파일 관리자"

''' <summary>
''' INI 파일을 읽고 쓰는 헬퍼 클래스
''' Windows API를 사용하지 않고 순수 VB.NET으로 구현
''' </summary>
Public Class IniFileManager
    Private ReadOnly _filePath As String
    Private ReadOnly _data As New Dictionary(Of String, Dictionary(Of String, String))

    ''' <summary>
    ''' INI 파일 관리자 생성
    ''' </summary>
    ''' <param name="filePath">INI 파일 경로</param>
    Public Sub New(filePath As String)
        _filePath = filePath
        LoadFile()
    End Sub

    ''' <summary>
    ''' INI 파일 로드
    ''' </summary>
    Private Sub LoadFile()
        _data.Clear()

        ' 파일이 없으면 빈 Dictionary 유지
        If Not File.Exists(_filePath) Then
            Return
        End If

        Try
            Dim currentSection As String = ""
            Dim lines = File.ReadAllLines(_filePath, Encoding.UTF8)

            For Each line In lines
                Dim trimmed = line.Trim()

                ' 빈 줄이나 주석 무시
                If String.IsNullOrEmpty(trimmed) OrElse trimmed.StartsWith(";") OrElse trimmed.StartsWith("#") Then
                    Continue For
                End If

                ' 섹션 헤더 [SectionName]
                If trimmed.StartsWith("[") AndAlso trimmed.EndsWith("]") Then
                    currentSection = trimmed.Substring(1, trimmed.Length - 2)
                    If Not _data.ContainsKey(currentSection) Then
                        _data(currentSection) = New Dictionary(Of String, String)
                    End If
                    Continue For
                End If

                ' 키=값 형식
                Dim equalIndex = trimmed.IndexOf("="c)
                If equalIndex > 0 AndAlso Not String.IsNullOrEmpty(currentSection) Then
                    Dim key = trimmed.Substring(0, equalIndex).Trim()
                    Dim value = trimmed.Substring(equalIndex + 1).Trim()
                    _data(currentSection)(key) = value
                End If
            Next
        Catch ex As Exception
            Logger.Instance.log($"INI 파일 로드 오류: {ex.Message}", Warning.ErrorInfo)
        End Try
    End Sub

    ''' <summary>
    ''' INI 파일 저장
    ''' </summary>
    Public Sub SaveFile()
        Try
            Dim lines As New List(Of String)

            ' 각 섹션별로 저장
            For Each section In _data
                lines.Add($"[{section.Key}]")
                For Each kvp In section.Value
                    lines.Add($"{kvp.Key}={kvp.Value}")
                Next
                lines.Add("") ' 섹션 구분을 위한 빈 줄
            Next

            ' 파일 저장
            File.WriteAllLines(_filePath, lines, Encoding.UTF8)
        Catch ex As Exception
            Logger.Instance.log($"INI 파일 저장 오류: {ex.Message}", Warning.ErrorInfo)
        End Try
    End Sub

    ''' <summary>
    ''' 값 읽기
    ''' </summary>
    Public Function Read(section As String, key As String, Optional defaultValue As String = "") As String
        If _data.ContainsKey(section) AndAlso _data(section).ContainsKey(key) Then
            Return _data(section)(key)
        End If
        Return defaultValue
    End Function

    ''' <summary>
    ''' 값 쓰기
    ''' </summary>
    Public Sub Write(section As String, key As String, value As String)
        If Not _data.ContainsKey(section) Then
            _data(section) = New Dictionary(Of String, String)
        End If
        _data(section)(key) = value
    End Sub

    ''' <summary>
    ''' Boolean 값 읽기
    ''' </summary>
    Public Function ReadBoolean(section As String, key As String, Optional defaultValue As Boolean = False) As Boolean
        Dim value = Read(section, key, defaultValue.ToString())
        Dim result As Boolean
        If Boolean.TryParse(value, result) Then
            Return result
        End If
        Return defaultValue
    End Function

    ''' <summary>
    ''' Integer 값 읽기
    ''' </summary>
    Public Function ReadInteger(section As String, key As String, Optional defaultValue As Integer = 0) As Integer
        Dim value = Read(section, key, defaultValue.ToString())
        Dim result As Integer
        If Integer.TryParse(value, result) Then
            Return result
        End If
        Return defaultValue
    End Function

    ''' <summary>
    ''' Double 값 읽기
    ''' </summary>
    Public Function ReadDouble(section As String, key As String, Optional defaultValue As Double = 0) As Double
        Dim value = Read(section, key, defaultValue.ToString())
        Dim result As Double
        If Double.TryParse(value, result) Then
            Return result
        End If
        Return defaultValue
    End Function

    ''' <summary>
    ''' Color 값 읽기 (ARGB 형식)
    ''' </summary>
    Public Function ReadColor(section As String, key As String, Optional defaultValue As Color = Nothing) As Color
        If defaultValue = Nothing Then defaultValue = Color.White

        Dim value = Read(section, key, "")
        If String.IsNullOrEmpty(value) Then Return defaultValue

        Try
            ' ARGB 형식: "255,255,0,0" (Alpha, Red, Green, Blue)
            Dim parts = value.Split(","c)
            If parts.Length = 4 Then
                Return Color.FromArgb(
                    Integer.Parse(parts(0)),
                    Integer.Parse(parts(1)),
                    Integer.Parse(parts(2)),
                    Integer.Parse(parts(3))
                )
            End If
        Catch
        End Try

        Return defaultValue
    End Function

    ''' <summary>
    ''' Color 값 쓰기
    ''' </summary>
    Public Sub WriteColor(section As String, key As String, color As Color)
        Write(section, key, $"{color.A},{color.R},{color.G},{color.B}")
    End Sub
End Class

#End Region

#Region "차트 설정 관리자"

''' <summary>
''' 차트 및 지표 설정을 INI 파일로 저장/로드하는 클래스
''' </summary>
Public Class ChartSettingsManager
    Private Shared _instance As ChartSettingsManager
    Private ReadOnly _iniFile As IniFileManager
    Private ReadOnly _settingsPath As String

    ''' <summary>
    ''' 싱글톤 인스턴스
    ''' </summary>
    Public Shared ReadOnly Property Instance As ChartSettingsManager
        Get
            If _instance Is Nothing Then
                _instance = New ChartSettingsManager()
            End If
            Return _instance
        End Get
    End Property

    ''' <summary>
    ''' 생성자 (private - 싱글톤 패턴)
    ''' </summary>
    Private Sub New()
        ' 실행 파일과 같은 폴더에 settings.ini 생성
        _settingsPath = Path.Combine(Application.StartupPath, "ChartSettings.ini")
        _iniFile = New IniFileManager(_settingsPath)

        Logger.Instance.log($"설정 파일 경로: {_settingsPath}")
    End Sub

    ''' <summary>
    ''' 지표 설정 저장
    ''' </summary>
    Public Sub SaveIndicatorSettings(indicators As Dictionary(Of String, IndicatorInstance))
        Logger.Instance.log("=== 지표 설정 저장 시작 ===")

        Try
            ' 지표 개수 저장
            _iniFile.Write("Indicators", "Count", indicators.Count.ToString())

            ' 각 지표별로 저장
            Dim index As Integer = 0
            For Each kvp In indicators
                Dim instance = kvp.Value
                Dim section = $"Indicator_{index}"

                ' 기본 정보
                _iniFile.Write(section, "Name", instance.Name)
                _iniFile.Write(section, "Type", instance.IndicatorType.Name)
                _iniFile.Write(section, "IsVisible", instance.IsVisible.ToString())
                _iniFile.Write(section, "ZOrder", instance.ZOrder.ToString())

                ' 파라미터
                If instance.Parameters.ContainsKey("period") Then
                    _iniFile.Write(section, "Period", instance.Parameters("period").ToString())
                End If

                If instance.Parameters.ContainsKey("color") Then
                    Dim color = CType(instance.Parameters("color"), Color)
                    _iniFile.WriteColor(section, "Color", color)
                End If

                ' 고급 설정
                _iniFile.Write(section, "LineWidth", instance.LineWidth.ToString())
                _iniFile.Write(section, "DisplayMode", instance.DisplayMode.ToString())
                _iniFile.Write(section, "AutoScale", instance.AutoScale.ToString())

                ' Y축 범위
                If instance.YAxisMin.HasValue Then
                    _iniFile.Write(section, "YAxisMin", instance.YAxisMin.Value.ToString())
                End If
                If instance.YAxisMax.HasValue Then
                    _iniFile.Write(section, "YAxisMax", instance.YAxisMax.Value.ToString())
                End If

                ' 과열/침체 구간
                _iniFile.Write(section, "EnableZones", instance.EnableZones.ToString())
                If instance.OverboughtLevel.HasValue Then
                    _iniFile.Write(section, "OverboughtLevel", instance.OverboughtLevel.Value.ToString())
                End If
                If instance.OversoldLevel.HasValue Then
                    _iniFile.Write(section, "OversoldLevel", instance.OversoldLevel.Value.ToString())
                End If

                ' 다이버전스
                _iniFile.Write(section, "EnableDivergence", instance.EnableDivergence.ToString())

                ' 기준선 (최대 5개까지 저장)
                _iniFile.Write(section, "ReferenceLineCount", Math.Min(instance.ReferenceLines.Count, 5).ToString())
                For i = 0 To Math.Min(instance.ReferenceLines.Count - 1, 4)
                    Dim refLine = instance.ReferenceLines(i)
                    _iniFile.Write(section, $"RefLine_{i}_Value", refLine.Value.ToString())
                    _iniFile.WriteColor(section, $"RefLine_{i}_Color", refLine.Color)
                    _iniFile.Write(section, $"RefLine_{i}_Style", CInt(refLine.Style).ToString())
                Next

                Logger.Instance.log($"  저장: {instance.Name} (IsVisible={instance.IsVisible}, EnableZones={instance.EnableZones})")
                index += 1
            Next

            ' 파일 저장
            _iniFile.SaveFile()
            Logger.Instance.log($"설정 저장 완료: {indicators.Count}개 지표")

        Catch ex As Exception
            Logger.Instance.log($"설정 저장 오류: {ex.Message}", Warning.ErrorInfo)
        End Try
    End Sub

    ''' <summary>
    ''' 지표 설정 로드
    ''' </summary>
    Public Function LoadIndicatorSettings() As Dictionary(Of String, IndicatorInstance)
        Logger.Instance.log("=== 지표 설정 로드 시작 ===")

        Dim indicators As New Dictionary(Of String, IndicatorInstance)

        Try
            ' 저장된 지표 개수 확인
            Dim count = _iniFile.ReadInteger("Indicators", "Count", 0)

            If count = 0 Then
                Logger.Instance.log("저장된 설정이 없음 - 기본값 사용")
                Return Nothing
            End If

            Logger.Instance.log($"저장된 지표 개수: {count}")

            ' 각 지표 로드
            For i = 0 To count - 1
                Dim section = $"Indicator_{i}"
                Dim instance As New IndicatorInstance()

                ' 기본 정보
                instance.Name = _iniFile.Read(section, "Name", "")
                If String.IsNullOrEmpty(instance.Name) Then Continue For

                Dim typeName = _iniFile.Read(section, "Type", "")
                instance.IndicatorType = GetIndicatorType(typeName)
                If instance.IndicatorType Is Nothing Then Continue For

                instance.IsVisible = _iniFile.ReadBoolean(section, "IsVisible", False)
                instance.ZOrder = _iniFile.ReadInteger(section, "ZOrder", i)

                ' 파라미터
                instance.Parameters = New Dictionary(Of String, Object)

                Dim period = _iniFile.ReadInteger(section, "Period", -1)
                If period > 0 Then
                    instance.Parameters("period") = period
                End If

                Dim ccolor = _iniFile.ReadColor(section, "Color", Color.Yellow)
                instance.Parameters("color") = ccolor

                ' 고급 설정
                instance.LineWidth = CSng(_iniFile.ReadDouble(section, "LineWidth", 2.0))

                Dim displayModeStr = _iniFile.Read(section, "DisplayMode", "Overlay")
                instance.DisplayMode = If(displayModeStr = "Separate", ChartDisplayMode.Separate, ChartDisplayMode.Overlay)

                instance.AutoScale = _iniFile.ReadBoolean(section, "AutoScale", True)

                ' ⭐ RSI 지표는 항상 0~100 범위 고정
                If typeName = "RSIIndicator" Then
                    instance.AutoScale = False
                    instance.YAxisMin = 0
                    instance.YAxisMax = 100
                End If

                ' Y축 범위
                Dim yMinStr = _iniFile.Read(section, "YAxisMin", "")
                If Not String.IsNullOrEmpty(yMinStr) AndAlso typeName <> "RSIIndicator" Then
                    instance.YAxisMin = _iniFile.ReadDouble(section, "YAxisMin", 0)
                End If

                Dim yMaxStr = _iniFile.Read(section, "YAxisMax", "")
                If Not String.IsNullOrEmpty(yMaxStr) AndAlso typeName <> "RSIIndicator" Then
                    instance.YAxisMax = _iniFile.ReadDouble(section, "YAxisMax", 100)
                End If

                ' 과열/침체 구간
                instance.EnableZones = _iniFile.ReadBoolean(section, "EnableZones", False)

                Dim obStr = _iniFile.Read(section, "OverboughtLevel", "")
                If Not String.IsNullOrEmpty(obStr) Then
                    instance.OverboughtLevel = _iniFile.ReadDouble(section, "OverboughtLevel", 70)
                End If

                Dim osStr = _iniFile.Read(section, "OversoldLevel", "")
                If Not String.IsNullOrEmpty(osStr) Then
                    instance.OversoldLevel = _iniFile.ReadDouble(section, "OversoldLevel", 30)
                End If

                ' 다이버전스
                instance.EnableDivergence = _iniFile.ReadBoolean(section, "EnableDivergence", False)

                ' 기준선
                instance.ReferenceLines = New List(Of ReferenceLine)
                Dim refLineCount = _iniFile.ReadInteger(section, "ReferenceLineCount", 0)
                For j = 0 To refLineCount - 1
                    Dim refLine As New ReferenceLine()
                    refLine.Value = _iniFile.ReadDouble(section, $"RefLine_{j}_Value", 0)
                    refLine.Color = _iniFile.ReadColor(section, $"RefLine_{j}_Color", Color.Gray)

                    Dim styleInt = _iniFile.ReadInteger(section, $"RefLine_{j}_Style", 1)
                    refLine.Style = CType(styleInt, DashStyle)

                    instance.ReferenceLines.Add(refLine)
                Next

                ' Dictionary에 추가
                indicators(instance.Name) = instance
                Logger.Instance.log($"  로드: {instance.Name} (Visible={instance.IsVisible}, Zones={instance.EnableZones}, AutoScale={instance.AutoScale}, 기준선={instance.ReferenceLines.Count}개)")
            Next

            Logger.Instance.log($"설정 로드 완료: {indicators.Count}개 지표")
            Return indicators

        Catch ex As Exception
            Logger.Instance.log($"설정 로드 오류: {ex.Message}", Warning.ErrorInfo)
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' 타입 이름으로 지표 Type 객체 가져오기
    ''' </summary>
    Private Function GetIndicatorType(typeName As String) As Type
        Select Case typeName
            Case "SMAIndicator"
                Return GetType(SMAIndicator)
            Case "RSIIndicator"
                Return GetType(RSIIndicator)
            Case "MACDIndicator"
                Return GetType(MACDIndicator)
            Case "TickIntensityIndicator"
                Return GetType(TickIntensityIndicator)
            Case Else
                Return Nothing
        End Select
    End Function

    ''' <summary>
    ''' 설정 파일 삭제 (기본값으로 리셋)
    ''' </summary>
    Public Sub ResetSettings()
        Try
            If File.Exists(_settingsPath) Then
                File.Delete(_settingsPath)
                Logger.Instance.log("설정 파일 삭제됨 - 기본값으로 리셋")
            End If
        Catch ex As Exception
            Logger.Instance.log($"설정 파일 삭제 오류: {ex.Message}", Warning.ErrorInfo)
        End Try
    End Sub
End Class

#End Region