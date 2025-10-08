Imports System.Linq

Public Class Form1
    ' === Strategy integration fields ===
    Private _strategies As New List(Of ITradeStrategy)
    Private _engine As New StrategyEngine()
    Private _currentStrategy As ITradeStrategy = Nothing
    Private ReadOnly _strategySignalsCache As New Dictionary(Of String, List(Of TradeSignal))
    Private _lastStrategyLogSignature As String = String.Empty

    Private Enum StrategyApplyMode
        FullChart = 0
        SimulationFollow = 1
    End Enum

    Private _applyMode As StrategyApplyMode = StrategyApplyMode.FullChart
    'Private _chkApplyStrategy As CheckBox = Nothing
    'Private _cmbStrategy As ComboBox = Nothing ' found via Controls.Find("cmbStrategy", True)
    Private _cmbStrategyScope As ComboBox = Nothing

    Private _tickCandles As New List(Of Candle)
    Private _candles As New List(Of CandleInfo)
    Private ReadOnly _chart As New HighPerformanceChartControl
    Private WithEvents _tmrSimulation As New Timer With {.Interval = 500}

    Private Async Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim code As String = cmbTickers.Text
        Dim startDate As Date = dtpStart.Value
        _tickCandles = Await New CybosDataProvider().DownloadTickCandles(code, startDate)
        dgvTickCandles.DataSource = _tickCandles
        Logger.Instance.log($"틱 데이터 다운로드 완료: {_tickCandles.Count}개")
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles Me.Load
        _chart.Dock = DockStyle.Fill
        panelChart.Controls.Add(_chart)

        AddHandler Logger.Instance.logEvent, AddressOf logEvent
        AddHandler Logger.Instance.progressStatus, AddressOf onProgress
        AddHandler _chart.SimulationIndexChanged, AddressOf OnSimulationIndexChanged

        dtpStart.Value = Date.Parse("2025-10-01 14:30")
        cmbTickers.Text = "034020"

        cmbTimeFrame.Items.AddRange(New String() {"T5", "T10", "T20", "T60", "T120", "T360", "m1", "m3", "m5", "m10", "m15", "m30", "m60"})
        cmbTimeFrame.SelectedIndex = 5

        chkSimulation.Checked = False
        FlowLayoutPanel3.Visible = False
        RegisterStrategies()
        CreateStrategyToggles()
    End Sub

    Private Sub logEvent(smsg As String)
        lblStatus.Text = smsg
        Debug.Print(smsg)
    End Sub

    Private Sub onProgress(value As Integer)
        Dim minvalue = Math.Max(100, value)
        Dim maxValue As Integer = Math.Min(0, value)
        Me.pbProgress.Value = value
    End Sub
    Private Sub btnConvert_Click(sender As Object, e As EventArgs) Handles btnConvert.Click
        If _tickCandles Is Nothing OrElse _tickCandles.Count = 0 Then
            MessageBox.Show("먼저 틱 데이터를 다운로드해주세요.", "알림")
            Return
        End If

        Dim timeFrame As String = cmbTimeFrame.Text
        _candles = ConvertToTargetCandles(timeFrame)
        _strategySignalsCache.Clear()
        _lastStrategyLogSignature = String.Empty
        dgvTarget.DataSource = Nothing
        dgvTarget.DataSource = _candles

        InitializeChartWithDefaults(_chart, _candles)

        ' 트랙바 설정
        If _candles.Count > 0 Then
            TrackBar1.Minimum = 10
            TrackBar1.Maximum = _candles.Count - 1
            TrackBar1.Value = _candles.Count - 1
        End If

        Logger.Instance.log($"변환 완료: {_candles.Count}개 {timeFrame} 캔들")
        If chkApplyStrategy IsNot Nothing AndAlso chkApplyStrategy.Checked Then
            Dim forceFull As Boolean = (chkDisplaySignals IsNot Nothing AndAlso chkDisplaySignals.Checked)
            Dim syncSim As Boolean = (Not forceFull) AndAlso (chkSimulation IsNot Nothing AndAlso chkSimulation.Checked AndAlso _applyMode = StrategyApplyMode.SimulationFollow)
            RefreshStrategyOverlay(simulationSync:=syncSim, forceFull:=forceFull)
        Else
            _lastStrategyLogSignature = String.Empty
            _chart.SetStrategyLabel(String.Empty)
        End If
    End Sub
    Private Function ConvertToTargetCandles(timeframe As String) As List(Of CandleInfo)
        Dim candles As New List(Of CandleInfo)
        Dim tf As String
        Dim interval As Integer

        If timeframe.StartsWith("m") OrElse timeframe.StartsWith("T") Then
            tf = timeframe.Substring(0, 1)
            interval = CInt(timeframe.Substring(1))
        Else
            tf = timeframe.Substring(0, 1)
            interval = 1
        End If

        Select Case tf
            Case "m"
                candles = AggregateToMinuteCandles(_tickCandles, interval)
            Case "T"
                candles = AggregateToTickCandles(_tickCandles, interval)
            Case Else
                MessageBox.Show("일봉, 주봉, 월봉은 아직 구현되지 않았습니다.", "알림")
        End Select

        Return candles
    End Function

    Private Sub btnIndicators_Click(sender As Object, e As EventArgs) Handles btnIndicators.Click
        ShowIndicatorDialog(_chart)
    End Sub

    Private Sub chkSimulation_CheckedChanged(sender As Object, e As EventArgs) Handles chkSimulation.CheckedChanged
        If _candles Is Nothing OrElse _candles.Count = 0 Then
            chkSimulation.Checked = False
            MessageBox.Show("먼저 차트를 생성하세요.", "알림")
            Return
        End If
        _lastStrategyLogSignature = String.Empty

        If chkSimulation.Checked Then
            chkSimulation.BackColor = Color.Red
            chkSimulation.ForeColor = Color.White
            FlowLayoutPanel3.Visible = True
            _chart.SetSimulationMode(True)

            ' 시작 위치를 20% 지점으로
            Dim startIndex = Math.Max(10, CInt(_candles.Count * 0.2))
            _chart.SetSimulationIndex(startIndex)
            TrackBar1.Value = startIndex
            UpdateSimulationInfo()
            RefreshStrategyOverlayDuringSimulation()
        Else
            chkSimulation.BackColor = Color.White
            chkSimulation.ForeColor = Color.Black
            FlowLayoutPanel3.Visible = False
            _chart.SetSimulationMode(False)

            If _tmrSimulation.Enabled Then
                _tmrSimulation.Stop()
                btnPlayPause.Text = "▶"
                btnPlayPause.BackColor = Color.SteelBlue
            End If
            If chkApplyStrategy IsNot Nothing AndAlso chkApplyStrategy.Checked Then
                Dim forceFull As Boolean = (chkDisplaySignals IsNot Nothing AndAlso chkDisplaySignals.Checked)
                RefreshStrategyOverlay(simulationSync:=False, forceFull:=forceFull)
            Else
                _chart.SetStrategyLabel(String.Empty)
            End If
        End If

    End Sub

#Region "Simulation"

    Private Sub btnFirst_Click(sender As Object, e As EventArgs) Handles btnFirst.Click
        _chart.SetSimulationIndex(10)
        UpdateSimulationInfo()
        RefreshStrategyOverlayDuringSimulation()
    End Sub

    Private Sub btnPrevious_Click(sender As Object, e As EventArgs) Handles btnPrevious.Click
        If _chart.MovePreviousCandle() Then
            UpdateSimulationInfo()
            RefreshStrategyOverlayDuringSimulation()
        End If
    End Sub

    Private Sub btnNext_Click(sender As Object, e As EventArgs) Handles btnNext.Click
        If _chart.MoveNextCandle() Then
            UpdateSimulationInfo()
            RefreshStrategyOverlayDuringSimulation()
        End If
    End Sub

    Private Sub btnLast_Click(sender As Object, e As EventArgs) Handles btnLast.Click
        _chart.SetSimulationIndex(_candles.Count - 1)
        UpdateSimulationInfo()
        RefreshStrategyOverlayDuringSimulation()
    End Sub

    Private Sub btnPlayPause_Click(sender As Object, e As EventArgs) Handles btnPlayPause.Click
        If _tmrSimulation.Enabled Then
            _tmrSimulation.Stop()
            btnPlayPause.Text = "▶"
            btnPlayPause.BackColor = Color.SteelBlue
        Else
            _tmrSimulation.Start()
            btnPlayPause.Text = "?"
            btnPlayPause.BackColor = Color.OrangeRed
        End If
    End Sub

    Private Sub _tmrSimulation_Tick(sender As Object, e As EventArgs) Handles _tmrSimulation.Tick
        If Not _chart.MoveNextCandle() Then
            _tmrSimulation.Stop()
            btnPlayPause.Text = "▶"
            btnPlayPause.BackColor = Color.SteelBlue
            MessageBox.Show("시뮬레이션이 완료되었습니다.", "알림")
        Else
            UpdateSimulationInfo()
        End If

        RefreshStrategyOverlayDuringSimulation()

        ' 전략 시그널 동기화(시뮬 연동 모드일 때만)
    End Sub

    Private Sub RefreshStrategyOverlayDuringSimulation()
        If chkApplyStrategy Is Nothing OrElse Not chkApplyStrategy.Checked Then Return

        If chkDisplaySignals IsNot Nothing AndAlso chkDisplaySignals.Checked Then
            RefreshStrategyOverlay(forceFull:=True)
            Return
        End If

        If chkSimulation Is Nothing OrElse Not chkSimulation.Checked Then Return

        If _applyMode <> StrategyApplyMode.SimulationFollow Then
            RefreshStrategyOverlay(forceFull:=False)
            Return
        End If

        RefreshStrategyOverlay(simulationSync:=True, forceFull:=False)
    End Sub

    Private Sub TrackBar1_Scroll(sender As Object, e As EventArgs) Handles TrackBar1.Scroll
        _chart.SetSimulationIndex(TrackBar1.Value)
        UpdateSimulationInfo()
        RefreshStrategyOverlayDuringSimulation()

        ' 전략 시그널 동기화(시뮬 연동 모드일 때만)
    End Sub

    Private Sub OnSimulationIndexChanged(currentIndex As Integer, maxIndex As Integer)
        If TrackBar1.Value <> currentIndex Then
            TrackBar1.Value = currentIndex
        End If
        UpdateSimulationInfo()
        RefreshStrategyOverlayDuringSimulation()
    End Sub

    Private Sub UpdateSimulationInfo()
        Dim info = _chart.GetSimulationInfo()
        If info.CurrentIndex >= 0 AndAlso info.CurrentIndex < _candles.Count Then
            Dim currentCandle = _candles(info.CurrentIndex)
            Label4.Text = $"시뮬레이션: {info.CurrentIndex}/{info.MaxIndex} ({info.Percentage:F1}%) | {currentCandle.Timestamp:yyyy-MM-dd HH:mm}"
        End If
    End Sub

#End Region

    Private Sub RegisterStrategies()
        ' 1. 기본 전략 등록
        Dim s As New M1_Ema200_Tick_Rsi7_Macd_Strategy()
        _strategies.Clear()
        _strategies.Add(s)
        _currentStrategy = s

        ' 2. 사용자 콤보박스 발견(디자이너에 이미 추가되어 있을 수 있음)
        If _cmbStrategy Is Nothing Then
            Dim found = Me.Controls.Find("cmbStrategy", True)
            If found IsNot Nothing AndAlso found.Length > 0 Then
                _cmbStrategy = TryCast(found(0), ComboBox)
            End If
        End If

        If _cmbStrategy IsNot Nothing Then
            _cmbStrategy.Items.Clear()
            For Each st In _strategies
                _cmbStrategy.Items.Add(st.Name)
            Next
            If _cmbStrategy.Items.Count > 0 Then
                _cmbStrategy.SelectedIndex = 0
            End If
            ' 런타임 이벤트 핸들러 연결
            AddHandler _cmbStrategy.SelectedIndexChanged, AddressOf cmbStrategy_SelectedIndexChanged
        End If
    End Sub
    Private Sub CreateStrategyToggles()
        ' 체크박스(전략 적용 토글)
        chkApplyStrategy = New CheckBox() With {
            .Name = "chkApplyStrategy",
            .Text = "전략 적용",
            .AutoSize = True
        }
        chkApplyStrategy.Checked = False
        AddHandler chkApplyStrategy.CheckedChanged, AddressOf chkApplyStrategy_CheckedChanged

        ' 적용 범위 콤보(전체차트 / 시뮬연동)
        _cmbStrategyScope = New ComboBox() With {
            .Name = "cmbStrategyScope",
            .DropDownStyle = ComboBoxStyle.DropDownList
        }
        _cmbStrategyScope.Items.AddRange(New Object() {"전체차트", "시뮬연동"})
        _cmbStrategyScope.SelectedIndex = 0
        AddHandler _cmbStrategyScope.SelectedIndexChanged, Sub(sender As Object, e As EventArgs)
                                                               _applyMode = If(_cmbStrategyScope.SelectedIndex = 0, StrategyApplyMode.FullChart, StrategyApplyMode.SimulationFollow)
                                                               If chkApplyStrategy IsNot Nothing AndAlso chkApplyStrategy.Checked Then
                                                                   _lastStrategyLogSignature = String.Empty
                                                                   Dim forceFull As Boolean = (chkDisplaySignals IsNot Nothing AndAlso chkDisplaySignals.Checked)
                                                                   Dim syncSim As Boolean = (Not forceFull) AndAlso (chkSimulation IsNot Nothing AndAlso chkSimulation.Checked AndAlso _applyMode = StrategyApplyMode.SimulationFollow)
                                                                   RefreshStrategyOverlay(simulationSync:=syncSim, forceFull:=forceFull)
                                                               End If
                                                           End Sub

        ' 배치: 시뮬 패널이 있으면 그 뒤에, 없으면 폼 좌상단
        Dim host As Control = Nothing
        Dim found = Me.Controls.Find("FlowLayoutPanel3", True)
        If found IsNot Nothing AndAlso found.Length > 0 Then host = found(0)
        If host Is Nothing Then host = Me

    End Sub
    ' 전략 콤보 변경 → 현재 전략 교체 후 토글이 켜져 있으면 오버레이 갱신
    Private Sub cmbStrategy_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbStrategy.SelectedIndexChanged
        Try
            If cmbStrategy Is Nothing OrElse cmbStrategy.SelectedIndex < 0 Then Exit Sub
            If _strategies Is Nothing OrElse _strategies.Count = 0 Then Exit Sub

            Dim name As String = CStr(cmbStrategy.SelectedItem)
            Dim st As ITradeStrategy = Nothing
            For Each s In _strategies
                If s.Name = name Then st = s : Exit For
            Next
            If st IsNot Nothing Then _currentStrategy = st

            If chkApplyStrategy IsNot Nothing AndAlso chkApplyStrategy.Checked Then
                _lastStrategyLogSignature = String.Empty
                Dim forceFull As Boolean = (chkDisplaySignals IsNot Nothing AndAlso chkDisplaySignals.Checked)
                Dim syncSim As Boolean = (Not forceFull) AndAlso (chkSimulation IsNot Nothing AndAlso chkSimulation.Checked AndAlso _applyMode = StrategyApplyMode.SimulationFollow)
                RefreshStrategyOverlay(simulationSync:=syncSim, forceFull:=forceFull)
            End If
        Catch
            ' 필요 시 로깅
        End Try
    End Sub

    ' 전략 적용 토글 온/오프
    Private Sub chkApplyStrategy_CheckedChanged(sender As Object, e As EventArgs) Handles chkApplyStrategy.CheckedChanged
        Try
            If chkApplyStrategy.Checked Then
                Dim forceFull As Boolean = (chkDisplaySignals IsNot Nothing AndAlso chkDisplaySignals.Checked)
                Dim syncSim As Boolean = (Not forceFull) AndAlso (chkSimulation IsNot Nothing AndAlso chkSimulation.Checked AndAlso _applyMode = StrategyApplyMode.SimulationFollow)
                _lastStrategyLogSignature = String.Empty
                RefreshStrategyOverlay(simulationSync:=syncSim, forceFull:=forceFull)
            Else
                If chkDisplaySignals IsNot Nothing AndAlso chkDisplaySignals.Checked Then
                    chkDisplaySignals.Checked = False
                End If
                _chart.SetStrategyLabel(String.Empty)
                _lastStrategyLogSignature = String.Empty
                ' 전략 해제: 기본 지표만 복원
                If _candles IsNot Nothing AndAlso _candles.Count > 0 Then
                    Dim simOn As Boolean = False
                    Dim simIndex As Integer = 0
                    Try
                        If chkSimulation IsNot Nothing Then simOn = chkSimulation.Checked
                        If _chart IsNot Nothing Then
                            Dim info = _chart.GetSimulationInfo()
                            simIndex = info.CurrentIndex
                        End If
                    Catch
                    End Try

                    InitializeChartWithDefaults(_chart, _candles)
                    _chart.SetData(_candles)
                    If simOn Then
                        _chart.SetSimulationMode(True)
                        _chart.SetSimulationIndex(simIndex)
                    End If
                    _chart.Invalidate()
                End If
            End If
        Catch
        End Try
    End Sub

    Private Function GetStrategySignals(strategy As ITradeStrategy) As List(Of TradeSignal)
        If strategy Is Nothing Then Return New List(Of TradeSignal)()
        If _candles Is Nothing OrElse _candles.Count = 0 Then Return New List(Of TradeSignal)()

        Dim key As String = strategy.Name
        Dim cached As List(Of TradeSignal) = Nothing
        If _strategySignalsCache.TryGetValue(key, cached) Then
            Return New List(Of TradeSignal)(cached)
        End If

        If _engine Is Nothing Then _engine = New StrategyEngine()
        Dim computed As List(Of TradeSignal) = _engine.Run(strategy, _candles)
        Dim copy As New List(Of TradeSignal)(computed)
        _strategySignalsCache(key) = copy
        Return New List(Of TradeSignal)(copy)
    End Function

    ' === 전략 결과를 차트에 오버레이 (튜플 비교 수정 + LINQ 없이 구현) ===
    Private Sub RefreshStrategyOverlay(Optional simulationSync As Boolean = False, Optional forceFull As Boolean = False)
        Try
            ' 데이터/전략 확인
            If _candles Is Nothing OrElse _candles.Count = 0 Then
                _chart.SetStrategyLabel(String.Empty)
                _lastStrategyLogSignature = String.Empty
                Exit Sub
            End If

            If _currentStrategy Is Nothing AndAlso cmbStrategy IsNot Nothing AndAlso cmbStrategy.SelectedIndex >= 0 AndAlso _strategies IsNot Nothing Then
                Dim name As String = CStr(cmbStrategy.SelectedItem)
                For Each s In _strategies
                    If s.Name = name Then _currentStrategy = s : Exit For
                Next
            End If
            If _currentStrategy Is Nothing Then
                _chart.SetStrategyLabel(String.Empty)
                _lastStrategyLogSignature = String.Empty
                InitializeChartWithDefaults(_chart, _candles)
                _chart.Invalidate()
                Exit Sub
            End If

            ' 시뮬 상태 보존
            Dim simOn As Boolean = False
            Dim simIndex As Integer = 0
            Try
                If chkSimulation IsNot Nothing Then simOn = chkSimulation.Checked
                If _chart IsNot Nothing Then
                    Dim info = _chart.GetSimulationInfo()
                    simIndex = info.CurrentIndex
                End If
            Catch
            End Try

            ' 기본 지표 복원 (사용자 기본 세팅 유지)
            InitializeChartWithDefaults(_chart, _candles)

            ' 전략 실행 결과 캐시 활용
            Dim allSignals As List(Of TradeSignal) = GetStrategySignals(_currentStrategy)

            Dim forceFullScope As Boolean = forceFull OrElse (chkDisplaySignals IsNot Nothing AndAlso chkDisplaySignals.Checked)
            Dim useSimulationScope As Boolean = Not forceFullScope AndAlso simulationSync
            Try
                If Not forceFullScope AndAlso Not useSimulationScope AndAlso _applyMode = StrategyApplyMode.SimulationFollow Then
                    useSimulationScope = True
                End If
            Catch
            End Try

            Dim scopeLabel As String = If(forceFullScope, "Full Chart", If(useSimulationScope, "Simulation", "Full Chart"))
            If chkApplyStrategy IsNot Nothing AndAlso chkApplyStrategy.Checked Then
                _chart.SetStrategyLabel($"{_currentStrategy.Name} [{scopeLabel}]")
            Else
                If chkDisplaySignals IsNot Nothing AndAlso chkDisplaySignals.Checked Then
                    chkDisplaySignals.Checked = False
                End If
                _chart.SetStrategyLabel(String.Empty)
            End If

            Dim limitIndex As Integer = -1
            If useSimulationScope AndAlso simOn Then
                limitIndex = simIndex
            End If

            Dim signals As List(Of TradeSignal) = allSignals
            If limitIndex >= 0 Then
                Dim filtered As New List(Of TradeSignal)
                For Each s In allSignals
                    If s.Index <= limitIndex Then
                        filtered.Add(s)
                    End If
                Next
                signals = filtered
            End If

            ' 매수/매도 인덱스 수집 ? LINQ 없이
            Dim buyIdx As New List(Of Integer)
            Dim sellIdx As New List(Of Integer)
            For Each s In signals
                If s.Side = TradeSignalType.Buy Then
                    buyIdx.Add(s.Index)
                ElseIf s.Side = TradeSignalType.Sell Then
                    sellIdx.Add(s.Index)
                End If
            Next

            Dim latestSignalIndex As Integer = If(signals.Count > 0, signals(signals.Count - 1).Index, -1)
            Dim logSignature As String = If(signals.Count > 0, $"{_currentStrategy.Name}|{latestSignalIndex}|{signals.Count}", $"{_currentStrategy.Name}|NONE")
            If _lastStrategyLogSignature <> logSignature Then
                Dim scopeSummary As String
                If useSimulationScope AndAlso simOn AndAlso limitIndex >= 0 Then
                    scopeSummary = $"(Simulation {Math.Min(limitIndex, _candles.Count - 1)}/{_candles.Count - 1})"
                Else
                    scopeSummary = "(Full)"
                End If
                'Logger.Instance.log($"[{_currentStrategy.Name}] Signals {scopeSummary} - Buys {buyIdx.Count}, Sells {sellIdx.Count}")
                If signals.Count > 0 Then
                    Dim prevIndex As Integer = -1
                    If Not String.IsNullOrEmpty(_lastStrategyLogSignature) Then
                        Dim parts = _lastStrategyLogSignature.Split("|"c)
                        If parts.Length >= 2 AndAlso parts(1) <> "NONE" Then
                            Integer.TryParse(parts(1), prevIndex)
                            If prevIndex >= latestSignalIndex Then prevIndex = -1
                        End If
                    End If
                    For Each sig In signals
                        If sig.Index > prevIndex Then
                            Dim sideLabel As String = If(sig.Side = TradeSignalType.Buy, "BUY", "SELL")
                            'Logger.Instance.log($"    - {sig.Time:yyyy-MM-dd HH:mm} {sideLabel} @ {sig.Price:N2} ({sig.Reason})")
                        End If
                    Next
                Else
                    Logger.Instance.log("    - No signals")
                End If
                _lastStrategyLogSignature = logSignature
            End If

            ' 신호 오버레이(스캐터) 추가
            Dim sigInd As IIndicator = New TradeSignalIndicator(buyIdx, sellIdx)
            _chart.AddIndicator(sigInd)

            ' 데이터/시뮬 상태 복원
            _chart.SetData(_candles)
            If simOn Then
                _chart.SetSimulationMode(True)
                _chart.SetSimulationIndex(simIndex)
            End If

            _chart.Invalidate()
        Catch
            ' 필요시 로깅
        End Try
    End Sub

    Private Sub chkDisplaySignals_CheckedChanged(sender As Object, e As EventArgs) Handles chkDisplaySignals.CheckedChanged
        Try
            _lastStrategyLogSignature = String.Empty
            If chkDisplaySignals.Checked Then
                If chkApplyStrategy IsNot Nothing AndAlso Not chkApplyStrategy.Checked Then
                    chkApplyStrategy.Checked = True
                    Return
                End If
                RefreshStrategyOverlay(forceFull:=True)
            Else
                If chkApplyStrategy IsNot Nothing AndAlso chkApplyStrategy.Checked Then
                    Dim syncSim As Boolean = (chkSimulation IsNot Nothing AndAlso chkSimulation.Checked AndAlso _applyMode = StrategyApplyMode.SimulationFollow)
                    RefreshStrategyOverlay(simulationSync:=syncSim, forceFull:=False)
                Else
                    _chart.SetStrategyLabel(String.Empty)
                End If
            End If
        Catch
        End Try
    End Sub
End Class

' Form1.vb 내부의 Module

#Region "Form1 확장 모듈"

Public Module Form1Helper
    ''' <summary>
    ''' 전역 지표 인스턴스 Dictionary
    ''' 프로그램 실행 중 모든 지표 설정을 여기에 저장
    ''' </summary>
    Private _indicatorInstances As New Dictionary(Of String, IndicatorInstance)

    ''' <summary>
    ''' 지표가 초기화되었는지 여부
    ''' </summary>
    Private _isInitialized As Boolean = False

    ''' <summary>
    ''' 지표 인스턴스 초기화 보장
    ''' 설정 파일이 있으면 로드, 없으면 기본값 사용
    ''' </summary>
    Private Sub EnsureIndicatorsInitialized()
        ' 이미 초기화되었으면 건너뜀
        If _isInitialized Then
            Return
        End If

        ' 1. INI 파일에서 설정 로드 시도
        Dim loadedSettings = ChartSettingsManager.Instance.LoadIndicatorSettings()

        If loadedSettings IsNot Nothing AndAlso loadedSettings.Count > 0 Then
            ' 저장된 설정이 있으면 사용
            _indicatorInstances = loadedSettings
            Logger.Instance.log($"지표 설정 로드: {_indicatorInstances.Count}개")
        Else
            ' 저장된 설정이 없으면 기본값 사용
            Dim defaults = DefaultIndicators.GetDefaultIndicators()

            For Each defaultInd In defaults
                Dim key = defaultInd.GetKey()
                _indicatorInstances(key) = defaultInd
            Next
            Logger.Instance.log($"기본 지표 로드: {_indicatorInstances.Count}개")
        End If

        _isInitialized = True
    End Sub

    ''' <summary>
    ''' 차트 초기화 및 지표 적용
    ''' </summary>
    ''' <param name="chart">차트 컨트롤</param>
    ''' <param name="candles">캔들 데이터</param>
    Public Sub InitializeChartWithDefaults(chart As HighPerformanceChartControl, candles As List(Of CandleInfo))
        ' 캔들 데이터의 기본 지표 계산 (SMA, EMA, RSI, MACD 등)
        IndicatorManager.CalculateBasicIndicators(candles)

        ' 지표 초기화 (설정 파일 로드 또는 기본값)
        EnsureIndicatorsInitialized()

        ' 차트 초기화 (기존 지표 모두 제거)
        chart.ClearIndicators()

        ' 활성화된 지표만 차트에 추가
        Dim addedCount As Integer = 0
        For Each kvp In _indicatorInstances.OrderBy(Function(k) k.Value.ZOrder)
            If kvp.Value.IsVisible Then
                ' 지표 생성 (설정 적용됨)
                Dim indicator = kvp.Value.CreateIndicator()

                If indicator IsNot Nothing Then
                    chart.AddIndicator(indicator)
                    addedCount += 1
                End If
            End If
        Next

        ' 캔들 데이터를 차트에 설정
        chart.SetData(candles)

        Logger.Instance.log($"차트 초기화 완료: {addedCount}개 지표 적용")
    End Sub

    ''' <summary>
    ''' 지표 관리 다이얼로그 표시
    ''' </summary>
    ''' <param name="chart">차트 컨트롤</param>
    Public Sub ShowIndicatorDialog(chart As HighPerformanceChartControl)
        ' 지표 초기화 보장
        EnsureIndicatorsInitialized()

        ' 지표 관리 다이얼로그 표시 (모달)
        Using dialog As New frmEnhancedChartDialog(chart, _indicatorInstances)
            dialog.ShowDialog()
        End Using

        ' ? 중요: 다이얼로그 닫을 때 자동으로 설정 저장
        SaveCurrentSettings()
    End Sub

    ''' <summary>
    ''' 현재 지표 설정을 INI 파일에 저장
    ''' </summary>
    Public Sub SaveCurrentSettings()
        ChartSettingsManager.Instance.SaveIndicatorSettings(_indicatorInstances)
    End Sub

    ''' <summary>
    ''' 지표 인스턴스 Dictionary 가져오기
    ''' </summary>
    Public Function GetIndicatorInstances() As Dictionary(Of String, IndicatorInstance)
        EnsureIndicatorsInitialized()
        Return _indicatorInstances
    End Function

    ''' <summary>
    ''' 설정 리셋 (기본값으로 되돌림)
    ''' </summary>
    Public Sub ResetToDefaults()
        ' INI 파일 삭제
        ChartSettingsManager.Instance.ResetSettings()

        ' 메모리 초기화
        _indicatorInstances.Clear()
        _isInitialized = False

        ' 기본값으로 재초기화
        EnsureIndicatorsInitialized()

        Logger.Instance.log("설정 리셋 완료")
    End Sub

    ''' <summary>
    ''' 틱 데이터를 분봉으로 변환
    ''' </summary>
    Public Function AggregateToMinuteCandles(tickCandles As List(Of Candle), interval As Integer) As List(Of CandleInfo)
        Logger.Instance.log($"=== 분봉 변환 시작: {interval}분봉 ===")

        Dim minuteCandles As New List(Of CandleInfo)
        If tickCandles.Count = 0 Then Return minuteCandles

        ' 시간대별로 그룹화
        Dim groupedTicks = tickCandles.GroupBy(Function(t)
                                                   ' 기준 시간 (시작 시각)
                                                   Dim baseTime = New DateTime(t.Timestamp.Year, t.Timestamp.Month, t.Timestamp.Day, t.Timestamp.Hour, 0, 0)
                                                   ' 분 그룹 (예: 5분봉이면 0, 5, 10, 15, ...)
                                                   Dim minuteGroup = (t.Timestamp.Minute \ interval) * interval
                                                   Return baseTime.AddMinutes(minuteGroup)
                                               End Function).OrderBy(Function(g) g.Key)

        ' 각 그룹을 하나의 캔들로 변환
        For Each group In groupedTicks
            Dim ticks = group.OrderBy(Function(t) t.Timestamp).ToList()

            ' 캔들 정보 생성
            Dim candleInfo As New CandleInfo With {
                .Timestamp = group.Key,                          ' 그룹의 시작 시간
                .Open = ticks.First().Open,                      ' 첫 틱의 시가
                .High = ticks.Max(Function(t) t.High),           ' 최고가
                .Low = ticks.Min(Function(t) t.Low),             ' 최저가
                .Close = ticks.Last().Close,                     ' 마지막 틱의 종가
                .Volume = ticks.Sum(Function(t) t.Volume),       ' 거래량 합계
                .timeframe = $"m{interval}",                     ' 타임프레임 표시
                .tickCount = ticks.Count                         ' 이 캔들을 구성한 틱 개수
            }

            ' 캔들 패턴 정보 계산
            candleInfo.CalculateCandleInfo()
            minuteCandles.Add(candleInfo)
        Next

        ' 이동평균, RSI 등 계산
        IndicatorManager.CalculateBasicIndicators(minuteCandles)

        Logger.Instance.log($"분봉 변환 완료: {minuteCandles.Count}개")
        Return minuteCandles
    End Function

    ''' <summary>
    ''' 틱 데이터를 틱봉으로 변환
    ''' </summary>
    Public Function AggregateToTickCandles(tickCandles As List(Of Candle), interval As Integer) As List(Of CandleInfo)
        Logger.Instance.log($"=== 틱봉 변환 시작: {interval}틱봉 ===")

        Dim resultCandles As New List(Of CandleInfo)
        If tickCandles.Count = 0 Then Return resultCandles

        Dim tickGroup As New List(Of Candle)

        ' N개의 틱을 하나의 캔들로 묶기
        For i As Integer = 0 To tickCandles.Count - 1
            tickGroup.Add(tickCandles(i))

            ' interval 개수만큼 모이거나, 마지막 틱이면 캔들 생성
            If tickGroup.Count >= interval OrElse i = tickCandles.Count - 1 Then
                Dim candleInfo As New CandleInfo With {
                    .Timestamp = tickGroup.First().Timestamp,        ' 첫 틱의 시간
                    .Open = tickGroup.First().Open,                  ' 첫 틱의 시가
                    .High = tickGroup.Max(Function(t) t.High),       ' 최고가
                    .Low = tickGroup.Min(Function(t) t.Low),         ' 최저가
                    .Close = tickGroup.Last().Close,                 ' 마지막 틱의 종가
                    .Volume = tickGroup.Sum(Function(t) t.Volume),   ' 거래량 합계
                    .timeframe = $"T{interval}",                     ' 타임프레임 표시
                    .tickCount = tickGroup.Count                     ' 실제 틱 개수
                }

                ' 캔들 패턴 정보 계산
                candleInfo.CalculateCandleInfo()
                resultCandles.Add(candleInfo)

                ' 다음 그룹을 위해 초기화
                tickGroup.Clear()
            End If
        Next

        ' 이동평균, RSI 등 계산
        IndicatorManager.CalculateBasicIndicators(resultCandles)

        Logger.Instance.log($"틱봉 변환 완료: {resultCandles.Count}개")
        Return resultCandles
    End Function
End Module

#End Region




