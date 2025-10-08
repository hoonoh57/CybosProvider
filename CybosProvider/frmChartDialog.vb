#Region "향상된 지표 관리 다이얼로그"

Imports System.Drawing.Drawing2D

Public Class frmEnhancedChartDialog
    Inherits Form

    Private ReadOnly _chart As HighPerformanceChartControl
    Private ReadOnly _indicatorInstances As Dictionary(Of String, IndicatorInstance)

    ' 왼쪽 패널 (활성 지표 목록)
    Private lstActiveIndicators As ListBox
    Private btnMoveUp As Button
    Private btnMoveDown As Button
    Private btnGroup As Button
    Private cmbPreset As ComboBox
    Private btnSavePreset As Button
    Private btnDeletePreset As Button

    ' 오른쪽 패널 (지표 설정)
    Private grpIndicatorSettings As GroupBox
    Private lblIndicatorName As Label
    Private txtIndicatorName As TextBox

    ' 기본 설정
    Private lblType As Label
    Private cmbType As ComboBox
    Private lblPeriod As Label
    Private numPeriod As NumericUpDown
    Private lblColor As Label
    Private pnlColor As Panel
    Private btnColor As Button
    Private lblLineWidth As Label
    Private numLineWidth As NumericUpDown

    ' 표시 모드
    Private grpDisplayMode As GroupBox
    Private rdoOverlay As RadioButton
    Private rdoSeparatePanel As RadioButton

    ' 기준선 설정
    Private grpReferenceLines As GroupBox
    Private lstReferenceLines As ListBox
    Private btnAddRefLine As Button
    Private btnRemoveRefLine As Button
    Private numRefLineValue As NumericUpDown
    Private pnlRefLineColor As Panel
    Private btnRefLineColor As Button
    Private cmbRefLineStyle As ComboBox

    ' 과열/침체 구간
    Private grpZones As GroupBox
    Private chkEnableZones As CheckBox
    Private lblOverbought As Label
    Private numOverbought As NumericUpDown
    Private lblOversold As Label
    Private numOversold As NumericUpDown

    ' Y축 범위
    Private grpYAxis As GroupBox
    Private chkAutoScale As CheckBox
    Private lblYMin As Label
    Private numYMin As NumericUpDown
    Private lblYMax As Label
    Private numYMax As NumericUpDown

    ' 다이버전스
    Private chkDivergence As CheckBox

    ' 알림 설정
    Private grpAlerts As GroupBox
    Private chkEnableAlert As CheckBox
    Private cmbAlertCondition As ComboBox
    Private numAlertValue As NumericUpDown

    ' 하단 버튼
    Private btnPreview As Button
    Private btnApply As Button
    Private btnAdd As Button
    Private btnDelete As Button
    Private btnClose As Button

    Private _previewMode As Boolean = False
    Private _selectedIndicatorKey As String = Nothing

    Public Sub New(chart As HighPerformanceChartControl, indicatorInstances As Dictionary(Of String, IndicatorInstance))
        _chart = chart
        _indicatorInstances = indicatorInstances

        InitializeComponents()
        LoadIndicators()
        LoadPresets()
    End Sub

    Private Sub InitializeComponents()
        Me.Text = "지표 관리"
        Me.Size = New Size(800, 650)
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent

        ' ===== 왼쪽 패널: 활성 지표 목록 =====
        lstActiveIndicators = New ListBox With {
            .Location = New Point(10, 40),
            .Size = New Size(250, 420),
            .Font = New Font("맑은 고딕", 9)
        }
        AddHandler lstActiveIndicators.SelectedIndexChanged, AddressOf lstActiveIndicators_SelectedIndexChanged
        Me.Controls.Add(lstActiveIndicators)

        Dim lblActiveList As New Label With {
            .Text = "활성 지표",
            .Location = New Point(10, 15),
            .Size = New Size(100, 20),
            .Font = New Font("맑은 고딕", 9, FontStyle.Bold)
        }
        Me.Controls.Add(lblActiveList)

        btnAdd = New Button With {
            .Text = "추가",
            .Location = New Point(10, 465),
            .Size = New Size(55, 28)
        }
        AddHandler btnAdd.Click, AddressOf btnAdd_Click
        Me.Controls.Add(btnAdd)

        btnDelete = New Button With {
            .Text = "삭제",
            .Location = New Point(70, 465),
            .Size = New Size(55, 28)
        }
        AddHandler btnDelete.Click, AddressOf btnDelete_Click
        Me.Controls.Add(btnDelete)

        Dim btnToggleVisibility As New Button With {
            .Text = "표시/숨김",
            .Location = New Point(130, 465),
            .Size = New Size(75, 28)
        }
        AddHandler btnToggleVisibility.Click, AddressOf btnToggleVisibility_Click
        Me.Controls.Add(btnToggleVisibility)

        btnMoveUp = New Button With {
            .Text = "▲",
            .Location = New Point(210, 465),
            .Size = New Size(25, 28)
        }
        AddHandler btnMoveUp.Click, AddressOf btnMoveUp_Click
        Me.Controls.Add(btnMoveUp)

        btnMoveDown = New Button With {
            .Text = "▼",
            .Location = New Point(240, 465),
            .Size = New Size(25, 28)
        }
        AddHandler btnMoveDown.Click, AddressOf btnMoveDown_Click
        Me.Controls.Add(btnMoveDown)

        btnGroup = New Button With {
            .Text = "그룹화",
            .Location = New Point(10, 500),
            .Size = New Size(70, 28)
        }
        Me.Controls.Add(btnGroup)

        ' 프리셋
        Dim lblPreset As New Label With {
            .Text = "프리셋:",
            .Location = New Point(10, 535),
            .Size = New Size(60, 20)
        }
        Me.Controls.Add(lblPreset)

        cmbPreset = New ComboBox With {
            .Location = New Point(70, 533),
            .Size = New Size(120, 21),
            .DropDownStyle = ComboBoxStyle.DropDownList
        }
        AddHandler cmbPreset.SelectedIndexChanged, AddressOf cmbPreset_SelectedIndexChanged
        Me.Controls.Add(cmbPreset)

        btnSavePreset = New Button With {
            .Text = "저장",
            .Location = New Point(195, 532),
            .Size = New Size(55, 23)
        }
        AddHandler btnSavePreset.Click, AddressOf btnSavePreset_Click
        Me.Controls.Add(btnSavePreset)

        btnDeletePreset = New Button With {
            .Text = "삭제",
            .Location = New Point(195, 557),
            .Size = New Size(55, 23)
        }
        Me.Controls.Add(btnDeletePreset)

        ' ===== 오른쪽 패널: 지표 설정 =====
        grpIndicatorSettings = New GroupBox With {
            .Text = "지표 설정",
            .Location = New Point(270, 10),
            .Size = New Size(510, 545)
        }
        Me.Controls.Add(grpIndicatorSettings)

        Dim yPos As Integer = 25

        ' 지표 이름
        Dim lblName As New Label With {
            .Text = "이름:",
            .Location = New Point(10, yPos),
            .Size = New Size(80, 20)
        }
        grpIndicatorSettings.Controls.Add(lblName)

        txtIndicatorName = New TextBox With {
            .Location = New Point(100, yPos - 2),
            .Size = New Size(150, 21),
            .ReadOnly = True,
            .BackColor = Color.WhiteSmoke
        }
        grpIndicatorSettings.Controls.Add(txtIndicatorName)

        yPos += 30

        ' 지표 타입
        lblType = New Label With {
            .Text = "지표 타입:",
            .Location = New Point(10, yPos),
            .Size = New Size(80, 20)
        }
        grpIndicatorSettings.Controls.Add(lblType)

        cmbType = New ComboBox With {
            .Location = New Point(100, yPos - 2),
            .Size = New Size(150, 21),
            .DropDownStyle = ComboBoxStyle.DropDownList
        }
        cmbType.Items.AddRange(New String() {"SMA", "EMA", "RSI", "MACD", "틱강도"})
        AddHandler cmbType.SelectedIndexChanged, AddressOf cmbType_SelectedIndexChanged
        grpIndicatorSettings.Controls.Add(cmbType)

        yPos += 30

        ' 기간
        lblPeriod = New Label With {
            .Text = "기간:",
            .Location = New Point(10, yPos),
            .Size = New Size(80, 20)
        }
        grpIndicatorSettings.Controls.Add(lblPeriod)

        numPeriod = New NumericUpDown With {
            .Location = New Point(100, yPos - 2),
            .Size = New Size(80, 21),
            .Minimum = 1,
            .Maximum = 500,
            .Value = 5
        }
        grpIndicatorSettings.Controls.Add(numPeriod)

        yPos += 30

        ' 색상
        lblColor = New Label With {
            .Text = "색상:",
            .Location = New Point(10, yPos),
            .Size = New Size(80, 20)
        }
        grpIndicatorSettings.Controls.Add(lblColor)

        pnlColor = New Panel With {
            .Location = New Point(100, yPos - 2),
            .Size = New Size(60, 21),
            .BorderStyle = BorderStyle.FixedSingle,
            .BackColor = Color.Yellow
        }
        grpIndicatorSettings.Controls.Add(pnlColor)

        btnColor = New Button With {
            .Text = "선택",
            .Location = New Point(165, yPos - 2),
            .Size = New Size(60, 21)
        }
        AddHandler btnColor.Click, AddressOf btnColor_Click
        grpIndicatorSettings.Controls.Add(btnColor)

        yPos += 30

        ' 선 굵기
        Dim lblWidth As New Label With {
            .Text = "선 굵기:",
            .Location = New Point(10, yPos),
            .Size = New Size(80, 20)
        }
        grpIndicatorSettings.Controls.Add(lblWidth)

        numLineWidth = New NumericUpDown With {
            .Location = New Point(100, yPos - 2),
            .Size = New Size(80, 21),
            .Minimum = 1,
            .Maximum = 5,
            .Value = 2,
            .DecimalPlaces = 1,
            .Increment = 0.5D
        }
        grpIndicatorSettings.Controls.Add(numLineWidth)

        yPos += 35

        ' 표시 모드
        grpDisplayMode = New GroupBox With {
            .Text = "표시 모드",
            .Location = New Point(10, yPos),
            .Size = New Size(240, 55)
        }
        grpIndicatorSettings.Controls.Add(grpDisplayMode)

        rdoOverlay = New RadioButton With {
            .Text = "메인 차트 오버레이",
            .Location = New Point(10, 20),
            .Size = New Size(150, 20),
            .Checked = True
        }
        grpDisplayMode.Controls.Add(rdoOverlay)

        rdoSeparatePanel = New RadioButton With {
            .Text = "독립 패널",
            .Location = New Point(10, 40),
            .Size = New Size(150, 20)
        }
        grpDisplayMode.Controls.Add(rdoSeparatePanel)

        yPos += 65

        ' 기준선 설정
        grpReferenceLines = New GroupBox With {
            .Text = "기준선",
            .Location = New Point(10, yPos),
            .Size = New Size(240, 120)
        }
        grpIndicatorSettings.Controls.Add(grpReferenceLines)

        lstReferenceLines = New ListBox With {
            .Location = New Point(10, 20),
            .Size = New Size(150, 90)
        }
        grpReferenceLines.Controls.Add(lstReferenceLines)

        btnAddRefLine = New Button With {
            .Text = "+ 추가",
            .Location = New Point(165, 20),
            .Size = New Size(65, 25)
        }
        AddHandler btnAddRefLine.Click, AddressOf btnAddRefLine_Click
        grpReferenceLines.Controls.Add(btnAddRefLine)

        btnRemoveRefLine = New Button With {
            .Text = "- 제거",
            .Location = New Point(165, 50),
            .Size = New Size(65, 25)
        }
        AddHandler btnRemoveRefLine.Click, AddressOf btnRemoveRefLine_Click
        grpReferenceLines.Controls.Add(btnRemoveRefLine)

        ' 기준선 설정 컨트롤들 (우측)
        Dim lblRefValue As New Label With {
            .Text = "값:",
            .Location = New Point(260, yPos + 25),
            .Size = New Size(40, 20)
        }
        grpIndicatorSettings.Controls.Add(lblRefValue)

        numRefLineValue = New NumericUpDown With {
            .Location = New Point(305, yPos + 23),
            .Size = New Size(80, 21),
            .Minimum = -10000,
            .Maximum = 10000,
            .DecimalPlaces = 2,
            .Value = 1D
        }
        grpIndicatorSettings.Controls.Add(numRefLineValue)

        pnlRefLineColor = New Panel With {
            .Location = New Point(305, yPos + 53),
            .Size = New Size(40, 21),
            .BorderStyle = BorderStyle.FixedSingle,
            .BackColor = Color.Gray
        }
        grpIndicatorSettings.Controls.Add(pnlRefLineColor)

        btnRefLineColor = New Button With {
            .Text = "색상",
            .Location = New Point(350, yPos + 53),
            .Size = New Size(50, 21)
        }
        AddHandler btnRefLineColor.Click, AddressOf btnRefLineColor_Click
        grpIndicatorSettings.Controls.Add(btnRefLineColor)

        cmbRefLineStyle = New ComboBox With {
            .Location = New Point(305, yPos + 83),
            .Size = New Size(95, 21),
            .DropDownStyle = ComboBoxStyle.DropDownList
        }
        cmbRefLineStyle.Items.AddRange(New String() {"실선", "점선", "대시"})
        cmbRefLineStyle.SelectedIndex = 1
        grpIndicatorSettings.Controls.Add(cmbRefLineStyle)

        yPos += 130

        ' 과열/침체 구간
        grpZones = New GroupBox With {
            .Text = "과열/침체 구간",
            .Location = New Point(10, yPos),
            .Size = New Size(240, 90)
        }
        grpIndicatorSettings.Controls.Add(grpZones)

        chkEnableZones = New CheckBox With {
            .Text = "구간 표시 활성화",
            .Location = New Point(10, 20),
            .Size = New Size(150, 20)
        }
        grpZones.Controls.Add(chkEnableZones)

        lblOverbought = New Label With {
            .Text = "과열:",
            .Location = New Point(10, 45),
            .Size = New Size(60, 20)
        }
        grpZones.Controls.Add(lblOverbought)

        numOverbought = New NumericUpDown With {
            .Location = New Point(70, 43),
            .Size = New Size(80, 21),
            .Minimum = -10000,
            .Maximum = 10000,
            .DecimalPlaces = 2,
            .Value = 1.5D
        }
        grpZones.Controls.Add(numOverbought)

        Dim lblObUnit As New Label With {
            .Text = "이상",
            .Location = New Point(155, 45),
            .Size = New Size(40, 20)
        }
        grpZones.Controls.Add(lblObUnit)

        lblOversold = New Label With {
            .Text = "침체:",
            .Location = New Point(10, 67),
            .Size = New Size(60, 20)
        }
        grpZones.Controls.Add(lblOversold)

        numOversold = New NumericUpDown With {
            .Location = New Point(70, 65),
            .Size = New Size(80, 21),
            .Minimum = -10000,
            .Maximum = 10000,
            .DecimalPlaces = 2,
            .Value = 0.5D
        }
        grpZones.Controls.Add(numOversold)

        Dim lblOsUnit As New Label With {
            .Text = "이하",
            .Location = New Point(155, 67),
            .Size = New Size(40, 20)
        }
        grpZones.Controls.Add(lblOsUnit)

        ' Y축 범위 (우측)
        grpYAxis = New GroupBox With {
            .Text = "Y축 범위",
            .Location = New Point(260, yPos),
            .Size = New Size(140, 90)
        }
        grpIndicatorSettings.Controls.Add(grpYAxis)

        chkAutoScale = New CheckBox With {
            .Text = "자동 조정",
            .Location = New Point(10, 20),
            .Size = New Size(100, 20),
            .Checked = True
        }
        AddHandler chkAutoScale.CheckedChanged, AddressOf chkAutoScale_CheckedChanged
        grpYAxis.Controls.Add(chkAutoScale)

        Dim lblMin As New Label With {
            .Text = "최소:",
            .Location = New Point(10, 45),
            .Size = New Size(40, 20)
        }
        grpYAxis.Controls.Add(lblMin)

        numYMin = New NumericUpDown With {
            .Location = New Point(55, 43),
            .Size = New Size(70, 21),
            .Minimum = -10000,
            .Maximum = 10000,
            .Value = 0,
            .Enabled = False
        }
        grpYAxis.Controls.Add(numYMin)

        Dim lblMax As New Label With {
            .Text = "최대:",
            .Location = New Point(10, 67),
            .Size = New Size(40, 20)
        }
        grpYAxis.Controls.Add(lblMax)

        numYMax = New NumericUpDown With {
            .Location = New Point(55, 65),
            .Size = New Size(70, 21),
            .Minimum = -10000,
            .Maximum = 10000,
            .Value = 100,
            .Enabled = False
        }
        grpYAxis.Controls.Add(numYMax)

        yPos += 100

        ' 다이버전스
        chkDivergence = New CheckBox With {
            .Text = "다이버전스 자동 표시",
            .Location = New Point(10, yPos),
            .Size = New Size(200, 20)
        }
        grpIndicatorSettings.Controls.Add(chkDivergence)

        yPos += 30

        ' 알림 설정
        grpAlerts = New GroupBox With {
            .Text = "알림 설정",
            .Location = New Point(10, yPos),
            .Size = New Size(390, 65)
        }
        grpIndicatorSettings.Controls.Add(grpAlerts)

        chkEnableAlert = New CheckBox With {
            .Text = "알림 활성화",
            .Location = New Point(10, 20),
            .Size = New Size(100, 20)
        }
        grpAlerts.Controls.Add(chkEnableAlert)

        cmbAlertCondition = New ComboBox With {
            .Location = New Point(10, 40),
            .Size = New Size(150, 21),
            .DropDownStyle = ComboBoxStyle.DropDownList
        }
        cmbAlertCondition.Items.AddRange(New String() {"값이 초과", "값이 미만", "상향 돌파", "하향 돌파"})
        cmbAlertCondition.SelectedIndex = 0
        grpAlerts.Controls.Add(cmbAlertCondition)

        numAlertValue = New NumericUpDown With {
            .Location = New Point(165, 40),
            .Size = New Size(80, 21),
            .Minimum = -10000,
            .Maximum = 10000,
            .DecimalPlaces = 2
        }
        grpAlerts.Controls.Add(numAlertValue)

        ' ===== 하단 버튼 =====
        btnPreview = New Button With {
            .Text = "미리보기",
            .Location = New Point(270, 565),
            .Size = New Size(90, 30)
        }
        AddHandler btnPreview.Click, AddressOf btnPreview_Click
        Me.Controls.Add(btnPreview)

        btnApply = New Button With {
            .Text = "적용",
            .Location = New Point(365, 565),
            .Size = New Size(90, 30)
        }
        AddHandler btnApply.Click, AddressOf btnApply_Click
        Me.Controls.Add(btnApply)

        btnAdd = New Button With {
            .Text = "추가",
            .Location = New Point(460, 565),
            .Size = New Size(90, 30)
        }
        AddHandler btnAdd.Click, AddressOf btnAdd_Click
        Me.Controls.Add(btnAdd)

        btnDelete = New Button With {
            .Text = "삭제",
            .Location = New Point(555, 565),
            .Size = New Size(90, 30)
        }
        AddHandler btnDelete.Click, AddressOf btnDelete_Click
        Me.Controls.Add(btnDelete)

        btnClose = New Button With {
            .Text = "닫기",
            .Location = New Point(690, 565),
            .Size = New Size(90, 30)
        }
        AddHandler btnClose.Click, Sub() Me.Close()
        Me.Controls.Add(btnClose)
    End Sub

    Private Sub LoadIndicators()
        lstActiveIndicators.Items.Clear()

        For Each kvp In _indicatorInstances.OrderBy(Function(k) k.Value.ZOrder)
            Dim visibleIcon = If(kvp.Value.IsVisible, "☑", "☐")
            Dim displayText = $"{visibleIcon} {kvp.Value.Name}"
            lstActiveIndicators.Items.Add(displayText)
        Next
    End Sub

    Private Sub LoadPresets()
        cmbPreset.Items.Clear()
        cmbPreset.Items.AddRange(New String() {"기본 설정", "단타용", "스윙용"})

        ' ⭐ 중요: SelectedIndexChanged 이벤트를 발생시키지 않고 선택
        RemoveHandler cmbPreset.SelectedIndexChanged, AddressOf cmbPreset_SelectedIndexChanged
        cmbPreset.SelectedIndex = 0
        AddHandler cmbPreset.SelectedIndexChanged, AddressOf cmbPreset_SelectedIndexChanged
    End Sub

    ' lstActiveIndicators_SelectedIndexChanged - 지표 선택 시
    Private Sub lstActiveIndicators_SelectedIndexChanged(sender As Object, e As EventArgs)

        If lstActiveIndicators.SelectedIndex < 0 Then
            Return
        End If

        Dim orderedKeys = _indicatorInstances.OrderBy(Function(k) k.Value.ZOrder).Select(Function(k) k.Key).ToList()
        Dim selectedKey = orderedKeys(lstActiveIndicators.SelectedIndex)
        _selectedIndicatorKey = selectedKey
        Dim instance = _indicatorInstances(selectedKey)

        ' UI에 설정 로드
        txtIndicatorName.Text = instance.Name

        ' 기간
        If instance.Parameters.ContainsKey("period") Then
            numPeriod.Value = CInt(instance.Parameters("period"))
        End If

        ' 색상
        If instance.Parameters.ContainsKey("color") Then
            pnlColor.BackColor = CType(instance.Parameters("color"), Color)
        End If

        ' 선 굵기
        numLineWidth.Value = CDec(instance.LineWidth)

        ' 표시 모드
        If instance.DisplayMode = ChartDisplayMode.Overlay Then
            rdoOverlay.Checked = True
        Else
            rdoSeparatePanel.Checked = True
        End If

        ' Y축 설정
        chkAutoScale.Checked = instance.AutoScale
        numYMin.Value = If(instance.YAxisMin.HasValue, CDec(instance.YAxisMin.Value), 0)
        numYMax.Value = If(instance.YAxisMax.HasValue, CDec(instance.YAxisMax.Value), 100)

        ' 과열/침체 구간
        chkEnableZones.Checked = instance.EnableZones
        numOverbought.Value = If(instance.OverboughtLevel.HasValue, CDec(instance.OverboughtLevel.Value), 70)
        numOversold.Value = If(instance.OversoldLevel.HasValue, CDec(instance.OversoldLevel.Value), 30)

        ' 다이버전스
        chkDivergence.Checked = instance.EnableDivergence

        ' 기준선 로드
        LoadReferenceLines(instance)

    End Sub

    Private Sub LoadReferenceLines(instance As IndicatorInstance)
        lstReferenceLines.Items.Clear()
        If instance.ReferenceLines IsNot Nothing Then
            For Each refLine In instance.ReferenceLines
                Dim colorName = GetColorName(refLine.Color)
                Dim styleName = GetDashStyleName(refLine.Style)
                lstReferenceLines.Items.Add($"{refLine.Value:F2} ({colorName}) {styleName}")
            Next
        End If
    End Sub

    Private Sub cmbType_SelectedIndexChanged(sender As Object, e As EventArgs)
        ' 모든 지표에서 기간 수정 가능하도록 변경
        Select Case cmbType.SelectedIndex
            Case 0, 1 ' SMA, EMA
                numPeriod.Enabled = True
                lblPeriod.Enabled = True
                btnColor.Enabled = True
                lblColor.Enabled = True
            Case 2 ' RSI
                numPeriod.Enabled = True  ' RSI도 기간 수정 가능
                lblPeriod.Enabled = True
                btnColor.Enabled = True
                lblColor.Enabled = True
            Case 3, 4 ' MACD, 틱강도
                numPeriod.Enabled = False
                lblPeriod.Enabled = False
                btnColor.Enabled = False
                lblColor.Enabled = False
        End Select
    End Sub

    Private Sub chkAutoScale_CheckedChanged(sender As Object, e As EventArgs)
        numYMin.Enabled = Not chkAutoScale.Checked
        numYMax.Enabled = Not chkAutoScale.Checked
    End Sub

    Private Sub btnColor_Click(sender As Object, e As EventArgs)
        Using colorDialog As New ColorDialog()
            colorDialog.Color = pnlColor.BackColor
            If colorDialog.ShowDialog() = DialogResult.OK Then
                pnlColor.BackColor = colorDialog.Color
            End If
        End Using
    End Sub

    Private Sub btnRefLineColor_Click(sender As Object, e As EventArgs)
        Using colorDialog As New ColorDialog()
            colorDialog.Color = pnlRefLineColor.BackColor
            If colorDialog.ShowDialog() = DialogResult.OK Then
                pnlRefLineColor.BackColor = colorDialog.Color
            End If
        End Using
    End Sub

    Private Sub btnAddRefLine_Click(sender As Object, e As EventArgs)
        If _selectedIndicatorKey Is Nothing Then
            MessageBox.Show("먼저 지표를 선택하세요.", "알림")
            Return
        End If

        Dim value = numRefLineValue.Value
        Dim color = pnlRefLineColor.BackColor
        Dim styleText = cmbRefLineStyle.Text

        ' 리스트박스에 추가 (친숙한 색상 이름 사용)
        Dim colorName = GetColorName(color)
        lstReferenceLines.Items.Add($"{value:F2} ({colorName}) {styleText}")
    End Sub

    Private Sub btnRemoveRefLine_Click(sender As Object, e As EventArgs)
        If lstReferenceLines.SelectedIndex >= 0 Then
            lstReferenceLines.Items.RemoveAt(lstReferenceLines.SelectedIndex)
        End If
    End Sub

    ' btnToggleVisibility_Click - 표시/숨김 토글
    Private Sub btnToggleVisibility_Click(sender As Object, e As EventArgs)
        If lstActiveIndicators.SelectedIndex < 0 Then
            MessageBox.Show("지표를 선택하세요.", "알림")
            Return
        End If

        Dim orderedKeys = _indicatorInstances.OrderBy(Function(k) k.Value.ZOrder).Select(Function(k) k.Key).ToList()
        Dim selectedKey = orderedKeys(lstActiveIndicators.SelectedIndex)
        Dim instance = _indicatorInstances(selectedKey)

        ' 상태 토글
        instance.IsVisible = Not instance.IsVisible

        If instance.IsVisible Then
            Dim indicator = instance.CreateIndicator()
            If indicator IsNot Nothing Then
                _chart.AddIndicator(indicator)
            End If
        Else
            _chart.RemoveIndicator(instance.Name)
        End If

        ' 목록 새로고침
        Dim currentIndex = lstActiveIndicators.SelectedIndex
        LoadIndicators()

        ' 같은 위치 다시 선택
        If currentIndex < lstActiveIndicators.Items.Count Then
            lstActiveIndicators.SelectedIndex = currentIndex
        End If

        Logger.Instance.log($"[{instance.Name}] 표시: {instance.IsVisible}")
    End Sub

    Private Sub btnMoveUp_Click(sender As Object, e As EventArgs)
        If lstActiveIndicators.SelectedIndex > 0 Then
            Dim idx = lstActiveIndicators.SelectedIndex
            Dim item = lstActiveIndicators.Items(idx)
            lstActiveIndicators.Items.RemoveAt(idx)
            lstActiveIndicators.Items.Insert(idx - 1, item)
            lstActiveIndicators.SelectedIndex = idx - 1

            ' ZOrder 업데이트
            UpdateZOrder()
        End If
    End Sub

    Private Sub btnMoveDown_Click(sender As Object, e As EventArgs)
        If lstActiveIndicators.SelectedIndex >= 0 AndAlso lstActiveIndicators.SelectedIndex < lstActiveIndicators.Items.Count - 1 Then
            Dim idx = lstActiveIndicators.SelectedIndex
            Dim item = lstActiveIndicators.Items(idx)
            lstActiveIndicators.Items.RemoveAt(idx)
            lstActiveIndicators.Items.Insert(idx + 1, item)
            lstActiveIndicators.SelectedIndex = idx + 1

            ' ZOrder 업데이트
            UpdateZOrder()
        End If
    End Sub

    Private Sub UpdateZOrder()
        Dim keys = _indicatorInstances.Keys.ToList()
        For i = 0 To lstActiveIndicators.Items.Count - 1
            Dim key = keys(i)
            _indicatorInstances(key).ZOrder = i
        Next
    End Sub

    Private Sub btnPreview_Click(sender As Object, e As EventArgs)
        _previewMode = True
        ApplySettings(True)
        MessageBox.Show("미리보기가 적용되었습니다. '적용' 버튼을 누르면 저장됩니다.", "미리보기")
    End Sub

    Private Sub btnApply_Click(sender As Object, e As EventArgs)
        'ApplySettings(False)
        '_previewMode = False
        'MessageBox.Show("설정이 적용되었습니다.", "알림")

        Logger.Instance.log("=== 적용 버튼 클릭 ===")
        ApplySettings(False)
        MessageBox.Show("설정이 적용되었습니다.", "알림")


    End Sub

    ' ApplySettings - 설정 적용 (완전 구현)
    Private Sub ApplySettings(isPreview As Boolean)
        If _selectedIndicatorKey Is Nothing Then
            Return
        End If

        Dim instance = _indicatorInstances(_selectedIndicatorKey)

        ' ===== 기본 파라미터 업데이트 =====
        If numPeriod.Enabled AndAlso numPeriod.Value > 0 Then
            If Not instance.Parameters.ContainsKey("period") Then
                instance.Parameters.Add("period", CInt(numPeriod.Value))
            Else
                instance.Parameters("period") = CInt(numPeriod.Value)
            End If
        End If

        If pnlColor.BackColor <> Color.Empty Then
            If Not instance.Parameters.ContainsKey("color") Then
                instance.Parameters.Add("color", pnlColor.BackColor)
            Else
                instance.Parameters("color") = pnlColor.BackColor
            End If
        End If

        ' ===== 선 굵기 =====
        instance.LineWidth = CSng(numLineWidth.Value)

        ' ===== 표시 모드 =====
        instance.DisplayMode = If(rdoOverlay.Checked, ChartDisplayMode.Overlay, ChartDisplayMode.Separate)

        ' ===== Y축 범위 =====
        instance.AutoScale = chkAutoScale.Checked
        If chkAutoScale.Checked Then
            instance.YAxisMin = Nothing
            instance.YAxisMax = Nothing
        Else
            instance.YAxisMin = CDbl(numYMin.Value)
            instance.YAxisMax = CDbl(numYMax.Value)
        End If

        ' ===== 과열/침체 구간 =====
        instance.EnableZones = chkEnableZones.Checked
        If chkEnableZones.Checked Then
            instance.OverboughtLevel = CDbl(numOverbought.Value)
            instance.OversoldLevel = CDbl(numOversold.Value)
        Else
            instance.OverboughtLevel = Nothing
            instance.OversoldLevel = Nothing
        End If

        ' ===== 다이버전스 =====
        instance.EnableDivergence = chkDivergence.Checked

        ' ===== 알림 설정 =====
        instance.AlertEnabled = chkEnableAlert.Checked
        If chkEnableAlert.Checked Then
            instance.AlertCondition = cmbAlertCondition.Text
            instance.AlertValue = CDbl(numAlertValue.Value)
        End If

        '' ===== 기준선 업데이트 (⭐ 핵심) =====
        'instance.ReferenceLines.Clear()
        'Logger.Instance.log($"[디버그] 기준선 리스트 개수: {lstReferenceLines.Items.Count}")

        'For Each item As String In lstReferenceLines.Items
        '    Try
        '        Logger.Instance.log($"[디버그] 기준선 항목: '{item}'")

        '        ' 형식: "50.00 (Gray) 점선"
        '        Dim parts = item.Split(" "c)
        '        Logger.Instance.log($"[디버그] 파싱 결과: parts.Length={parts.Length}, parts={String.Join("|", parts)}")

        '        If parts.Length >= 2 Then
        '            Dim valueStr = parts(0)

        '            ' 기존 문제 라인
        '            'Dim colorStr = parts(1).Trim("("c, ")"c)

        '            '--- 새 로직 : 앞 "(" 하나만 제거, 뒤 ")"은 절대 건드리지 않음 -------------
        '            Dim rawColor As String = parts(1)                 ' ex: "(RGB(128,128,128)"
        '            If rawColor.StartsWith("("c) Then
        '                rawColor = rawColor.Substring(1)              ' ex: "RGB(128,128,128)"
        '            End If
        '            Dim colorStr As String = rawColor                 ' 최종 colorStr

        '            Dim styleStr = If(parts.Length >= 3, parts(2), "점선")

        '            Dim value As Double
        '            If Double.TryParse(valueStr, value) Then
        '                ' ⭐ 색상 매핑 (한글 → Color)
        '                Dim refColor As Color = Color.Gray

        '                ' RGB(r,g,b) 형식 처리
        '                If colorStr.StartsWith("RGB(", StringComparison.OrdinalIgnoreCase) AndAlso colorStr.EndsWith(")") Then
        '                    Try
        '                        Dim rgb = colorStr.Substring(4, colorStr.Length - 5).Split(","c)
        '                        If rgb.Length = 3 Then
        '                            refColor = Color.FromArgb(Integer.Parse(rgb(0)), Integer.Parse(rgb(1)), Integer.Parse(rgb(2)))
        '                            Logger.Instance.log($"[디버그] RGB 색상 파싱: {colorStr} -> R={refColor.R}, G={refColor.G}, B={refColor.B}")
        '                        End If
        '                    Catch ex As Exception
        '                        Logger.Instance.log($"[디버그] RGB 파싱 실패: {colorStr}")
        '                    End Try
        '                Else
        '                    ' 이름 기반 색상 매핑
        '                    Select Case colorStr.ToLower()
        '                        Case "red", "빨강", "빨간색"
        '                            refColor = Color.Red
        '                        Case "blue", "파랑", "파란색"
        '                            refColor = Color.Blue
        '                        Case "green", "녹색", "초록"
        '                            refColor = Color.Green
        '                        Case "yellow", "노랑", "노란색"
        '                            refColor = Color.Yellow
        '                        Case "orange", "오렌지", "주황"
        '                            refColor = Color.Orange
        '                        Case "purple", "보라", "보라색"
        '                            refColor = Color.Purple
        '                        Case "gray", "grey", "회색"
        '                            refColor = Color.Gray
        '                        Case "white", "흰색"
        '                            refColor = Color.White
        '                        Case "black", "검정", "검은색"
        '                            refColor = Color.Black
        '                        Case "cyan", "시안"
        '                            refColor = Color.Cyan
        '                        Case "magenta", "마젠타"
        '                            refColor = Color.Magenta
        '                        Case Else
        '                            ' Color.FromName() 시도
        '                            Dim namedColor = Color.FromName(colorStr)
        '                            If namedColor.A <> 0 OrElse namedColor.R <> 0 OrElse namedColor.G <> 0 OrElse namedColor.B <> 0 Then
        '                                refColor = namedColor
        '                            Else
        '                                Logger.Instance.log($"[디버그] 색상 '{colorStr}' 인식 실패 -> Gray로 대체")
        '                            End If
        '                    End Select
        '                End If

        '                Dim refStyle As DashStyle = DashStyle.Dash

        '                Select Case styleStr
        '                    Case "실선"
        '                        refStyle = DashStyle.Solid
        '                    Case "점선"
        '                        refStyle = DashStyle.Dot
        '                    Case "대시"
        '                        refStyle = DashStyle.Dash
        '                End Select

        '                instance.ReferenceLines.Add(New ReferenceLine With {
        '                    .Value = value,
        '                    .Color = refColor,
        '                    .Style = refStyle
        '                })

        '                Logger.Instance.log($"[디버그] 기준선 추가: Value={value}, Color={refColor.Name} (A={refColor.A}, R={refColor.R}, G={refColor.G}, B={refColor.B}), Style={refStyle}")
        '            End If
        '        End If
        '    Catch ex As Exception
        '        Logger.Instance.log($"기준선 파싱 오류: {ex.Message}", Warning.ErrorInfo)
        '    End Try
        'Next

        ' ===== 기준선 업데이트 (⭐ 핵심) =====
        instance.ReferenceLines.Clear()
        Logger.Instance.log($"[디버그] 기준선 리스트 개수: {lstReferenceLines.Items.Count}")

        For Each item As String In lstReferenceLines.Items
            Try
                Logger.Instance.log($"[디버그] 기준선 항목: '{item}'")

                '--- 공백 2번만 Split(3) 효과와 동일 : VB2015 이하도 OK -------------
                Dim valueStr As String = ""
                Dim colorStr As String = ""
                Dim styleStr As String = "점선"          '기본값

                Dim sp1 As Integer = item.IndexOf(" "c)
                If sp1 >= 0 Then
                    valueStr = item.Substring(0, sp1).Trim()

                    Dim sp2 As Integer = item.IndexOf(" "c, sp1 + 1)
                    If sp2 >= 0 Then
                        colorStr = item.Substring(sp1 + 1, sp2 - sp1 - 1).Trim()
                        styleStr = item.Substring(sp2 + 1).Trim()
                    Else
                        colorStr = item.Substring(sp1 + 1).Trim()
                    End If
                Else
                    valueStr = item.Trim()
                End If
                '---------------------------------------------------------------------

                '--- 앞 "(" 하나만 제거, 뒤 ")"은 보존 ------------------------------
                If colorStr.StartsWith("("c) Then colorStr = colorStr.Substring(1)
                '---------------------------------------------------------------------

                Dim value As Double
                If Double.TryParse(valueStr, value) Then
                    Dim refColor As Color = Color.Gray

                    ' RGB(r,g,b) 형식 처리
                    If colorStr.StartsWith("RGB(", StringComparison.OrdinalIgnoreCase) AndAlso
               colorStr.EndsWith(")") Then
                        Try
                            Dim rgb = colorStr.Substring(4, colorStr.Length - 5).Split(","c)
                            If rgb.Length = 3 Then
                                refColor = Color.FromArgb(CInt(rgb(0)), CInt(rgb(1)), CInt(rgb(2)))
                                Logger.Instance.log($"[디버그] RGB 색상 파싱: {colorStr} -> R={refColor.R}, G={refColor.G}, B={refColor.B}")
                            End If
                        Catch ex As Exception
                            Logger.Instance.log($"[디버그] RGB 파싱 실패: {colorStr}")
                        End Try
                    Else
                        ' 이름 기반 색상 매핑
                        Select Case colorStr.ToLower()
                            Case "red", "빨강", "빨간색" : refColor = Color.Red
                            Case "blue", "파랑", "파란색" : refColor = Color.Blue
                            Case "green", "녹색", "초록" : refColor = Color.Green
                            Case "yellow", "노랑", "노란색" : refColor = Color.Yellow
                            Case "orange", "오렌지", "주황" : refColor = Color.Orange
                            Case "purple", "보라", "보라색" : refColor = Color.Purple
                            Case "gray", "grey", "회색" : refColor = Color.Gray
                            Case "white", "흰색" : refColor = Color.White
                            Case "black", "검정", "검은색" : refColor = Color.Black
                            Case "cyan", "시안" : refColor = Color.Cyan
                            Case "magenta", "마젠타" : refColor = Color.Magenta
                            Case Else
                                Dim namedColor = Color.FromName(colorStr)
                                If namedColor.A <> 0 OrElse namedColor.R <> 0 OrElse namedColor.G <> 0 OrElse namedColor.B <> 0 Then
                                    refColor = namedColor
                                Else
                                    Logger.Instance.log($"[디버그] 색상 '{colorStr}' 인식 실패 -> Gray로 대체")
                                End If
                        End Select
                    End If

                    Dim refStyle As DashStyle = DashStyle.Dot
                    Select Case styleStr
                        Case "실선" : refStyle = DashStyle.Solid
                        Case "점선" : refStyle = DashStyle.Dot
                        Case "대시" : refStyle = DashStyle.Dash
                    End Select

                    instance.ReferenceLines.Add(New ReferenceLine With {
                .Value = value,
                .Color = refColor,
                .Style = refStyle
            })

                    Logger.Instance.log($"[디버그] 기준선 추가: Value={value}, Color={refColor.Name} (A={refColor.A}, R={refColor.R}, G={refColor.G}, B={refColor.B}), Style={refStyle}")
                End If
            Catch ex As Exception
                Logger.Instance.log($"기준선 파싱 오류: {ex.Message}", Warning.ErrorInfo)
            End Try
        Next




        Logger.Instance.log($"[디버그] 최종 기준선 개수: {instance.ReferenceLines.Count}")

        ' ===== 차트 업데이트 =====
        If instance.IsVisible Then
            Logger.Instance.log($"[디버그] 차트 업데이트 시작: {instance.Name}")
            _chart.RemoveIndicator(instance.Name)

            Dim indicator = instance.CreateIndicator()
            If indicator IsNot Nothing Then
                Logger.Instance.log($"[디버그] CreateIndicator 성공: {indicator.Name}")

                ' 생성된 지표의 메타데이터 확인
                Dim metadataList = indicator.GetSeriesMetadata()
                For Each meta In metadataList
                    Logger.Instance.log($"[디버그] 메타데이터: {meta.Name}")
                    Logger.Instance.log($"[디버그]   EnableZones={meta.EnableZones}, OB={meta.OverboughtLevel}, OS={meta.OversoldLevel}")
                    Logger.Instance.log($"[디버그]   AutoScale={meta.AxisInfo.AutoScale}, Min={meta.AxisInfo.Min}, Max={meta.AxisInfo.Max}")
                    Logger.Instance.log($"[디버그]   ReferenceLines.Count={meta.ReferenceLines.Count}")

                    For i = 0 To meta.ReferenceLines.Count - 1
                        Dim refLine = meta.ReferenceLines(i)
                        Logger.Instance.log($"[디버그]     기준선[{i}]: Value={refLine.Value}, Color={refLine.Color.Name} (A={refLine.Color.A}, R={refLine.Color.R}), Style={refLine.Style}")
                    Next
                Next

                _chart.AddIndicator(indicator)
                Logger.Instance.log($"[{instance.Name}] 설정 적용: Zones={instance.EnableZones}, 기준선={instance.ReferenceLines.Count}개")
            Else
                Logger.Instance.log($"[{instance.Name}] CreateIndicator 실패", Warning.ErrorInfo)
            End If
        End If
    End Sub

    Private Sub btnAdd_Click(sender As Object, e As EventArgs)
        ' 기존 추가 로직
        If cmbType.SelectedIndex < 0 Then
            MessageBox.Show("지표 타입을 선택하세요.", "알림")
            Return
        End If

        Dim instance As New IndicatorInstance With {.IsVisible = True}

        Select Case cmbType.SelectedIndex
            Case 0
                instance.IndicatorType = GetType(SMAIndicator)
                instance.Parameters("period") = CInt(numPeriod.Value)
                instance.Parameters("color") = pnlColor.BackColor
                instance.Name = $"SMA({numPeriod.Value})"
            Case 1
                instance.IndicatorType = GetType(SMAIndicator)
                instance.Parameters("period") = CInt(numPeriod.Value)
                instance.Parameters("color") = pnlColor.BackColor
                instance.Name = $"EMA({numPeriod.Value})"
            Case 2
                instance.IndicatorType = GetType(RSIIndicator)
                instance.Name = "RSI(14)"
            Case 3
                instance.IndicatorType = GetType(MACDIndicator)
                instance.Name = "MACD"
            Case 4
                instance.IndicatorType = GetType(TickIntensityIndicator)
                instance.Name = "틱강도"
        End Select

        Dim key = instance.GetKey()
        If _indicatorInstances.ContainsKey(key) Then
            MessageBox.Show("이미 동일한 지표가 존재합니다.", "알림")
            Return
        End If

        _indicatorInstances(key) = instance

        If instance.IsVisible Then
            Dim indicator = instance.CreateIndicator()
            If indicator IsNot Nothing Then
                _chart.AddIndicator(indicator)
            End If
        End If

        LoadIndicators()
        MessageBox.Show("지표가 추가되었습니다.", "알림")
    End Sub

    Private Sub btnDelete_Click(sender As Object, e As EventArgs)
        If lstActiveIndicators.SelectedIndex < 0 Then
            MessageBox.Show("삭제할 지표를 선택하세요.", "알림")
            Return
        End If

        Dim selectedKey = _indicatorInstances.Keys.ElementAt(lstActiveIndicators.SelectedIndex)
        Dim instance = _indicatorInstances(selectedKey)

        If MessageBox.Show($"{instance.Name} 지표를 삭제하시겠습니까?", "확인", MessageBoxButtons.YesNo) = DialogResult.Yes Then
            _chart.RemoveIndicator(instance.Name)
            _indicatorInstances.Remove(selectedKey)
            LoadIndicators()
            MessageBox.Show("지표가 삭제되었습니다.", "알림")
        End If
    End Sub

    Private Sub cmbPreset_SelectedIndexChanged(sender As Object, e As EventArgs)
        Select Case cmbPreset.SelectedIndex
            Case 0 ' 기본 설정
                LoadDefaultPreset()
            Case 1 ' 단타용
                LoadScalpingPreset()
            Case 2 ' 스윙용
                LoadSwingPreset()
        End Select

        Logger.Instance.log($"프리셋 적용: {cmbPreset.Text}")
    End Sub

    Private Sub LoadDefaultPreset()
        ' 기본 설정: SMA(5), SMA(20)만 활성화
        For Each kvp In _indicatorInstances
            Dim instance = kvp.Value
            Dim shouldBeVisible = (instance.Name = "SMA(5)" OrElse instance.Name = "SMA(20)")

            If instance.IsVisible <> shouldBeVisible Then
                instance.IsVisible = shouldBeVisible

                If instance.IsVisible Then
                    Dim indicator = instance.CreateIndicator()
                    If indicator IsNot Nothing Then
                        _chart.AddIndicator(indicator)
                    End If
                Else
                    _chart.RemoveIndicator(instance.Name)
                End If
            End If
        Next

        LoadIndicators()
    End Sub

    Private Sub LoadScalpingPreset()
        For Each kvp In _indicatorInstances
            Dim instance = kvp.Value
            Dim shouldBeVisible = (instance.Name = "SMA(5)" OrElse instance.Name = "틱강도")

            If instance.IsVisible <> shouldBeVisible Then
                instance.IsVisible = shouldBeVisible

                If instance.IsVisible Then
                    Dim indicator = instance.CreateIndicator()
                    If indicator IsNot Nothing Then
                        _chart.AddIndicator(indicator)
                    End If
                Else
                    _chart.RemoveIndicator(instance.Name)
                End If
            End If
        Next

        LoadIndicators()
    End Sub

    Private Sub LoadSwingPreset()
        ' SMA(60) 추가 (없으면)
        If Not _indicatorInstances.ContainsKey("SMA(60)") Then
            Dim sma60 As New IndicatorInstance With {
                .IndicatorType = GetType(SMAIndicator),
                .Name = "SMA(60)",
                .Parameters = New Dictionary(Of String, Object) From {
                    {"period", 60},
                    {"color", Color.Orange}
                },
                .IsVisible = True,
                .DisplayMode = ChartDisplayMode.Overlay,
                .LineWidth = 2.0F,
                .ZOrder = 2
            }
            _indicatorInstances("SMA(60)") = sma60
        End If

        For Each kvp In _indicatorInstances
            Dim instance = kvp.Value
            Dim shouldBeVisible = (instance.Name = "SMA(20)" OrElse
                                  instance.Name = "SMA(60)" OrElse
                                  instance.Name.StartsWith("RSI") OrElse
                                  instance.Name = "MACD")

            If instance.IsVisible <> shouldBeVisible Then
                instance.IsVisible = shouldBeVisible

                If instance.IsVisible Then
                    Dim indicator = instance.CreateIndicator()
                    If indicator IsNot Nothing Then
                        _chart.AddIndicator(indicator)
                    End If
                Else
                    _chart.RemoveIndicator(instance.Name)
                End If
            End If
        Next

        LoadIndicators()
    End Sub
    Private Sub btnSavePreset_Click(sender As Object, e As EventArgs)
        Dim presetName = InputBox("프리셋 이름을 입력하세요:", "프리셋 저장")
        If String.IsNullOrWhiteSpace(presetName) Then Return

        ' 프리셋 저장 로직 (파일 또는 설정에 저장)
        MessageBox.Show($"프리셋 '{presetName}'이(가) 저장되었습니다.", "알림")

        ' 콤보박스에 추가
        If Not cmbPreset.Items.Contains(presetName) Then
            cmbPreset.Items.Add(presetName)
        End If
    End Sub

    ' 닫기 버튼
    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        MyBase.OnFormClosing(e)
    End Sub
    ''' <summary>
    ''' Color를 친숙한 이름으로 변환
    ''' </summary>
    Private Function GetColorName(c As Color) As String
        If c = Color.Red Then Return "Red"
        If c = Color.Blue Then Return "Blue"
        If c = Color.Green Then Return "Green"
        If c = Color.Yellow Then Return "Yellow"
        If c = Color.Orange Then Return "Orange"
        If c = Color.Purple Then Return "Purple"
        If c = Color.Gray Then Return "Gray"
        If c = Color.White Then Return "White"
        If c = Color.Black Then Return "Black"
        If c = Color.Cyan Then Return "Cyan"
        If c = Color.Magenta Then Return "Magenta"

        ' 기타 - 이름 반환 (또는 RGB 표시)
        If c.IsNamedColor Then
            Return c.Name
        Else
            Return $"RGB({c.R},{c.G},{c.B})"
        End If
    End Function

    ''' <summary>
    ''' DashStyle을 한글 이름으로 변환
    ''' </summary>
    Private Function GetDashStyleName(style As DashStyle) As String
        Select Case style
            Case DashStyle.Solid
                Return "실선"
            Case DashStyle.Dash
                Return "대시"
            Case DashStyle.Dot
                Return "점선"
            Case DashStyle.DashDot
                Return "대시점선"
            Case DashStyle.DashDotDot
                Return "대시점점선"
            Case Else
                Return "점선"
        End Select
    End Function


End Class

#End Region




'#Region "향상된 지표 관리 다이얼로그"

'Imports System.Drawing.Drawing2D

'Public Class frmEnhancedChartDialog
'    Inherits Form

'    Private ReadOnly _chart As HighPerformanceChartControl
'    Private ReadOnly _indicatorInstances As Dictionary(Of String, IndicatorInstance)

'    ' 왼쪽 패널 (활성 지표 목록)
'    Private lstActiveIndicators As ListBox
'    Private btnMoveUp As Button
'    Private btnMoveDown As Button
'    Private btnGroup As Button
'    Private cmbPreset As ComboBox
'    Private btnSavePreset As Button
'    Private btnDeletePreset As Button

'    ' 오른쪽 패널 (지표 설정)
'    Private grpIndicatorSettings As GroupBox
'    Private lblIndicatorName As Label
'    Private txtIndicatorName As TextBox

'    ' 기본 설정
'    Private lblType As Label
'    Private cmbType As ComboBox
'    Private lblPeriod As Label
'    Private numPeriod As NumericUpDown
'    Private lblColor As Label
'    Private pnlColor As Panel
'    Private btnColor As Button
'    Private lblLineWidth As Label
'    Private numLineWidth As NumericUpDown

'    ' 표시 모드
'    Private grpDisplayMode As GroupBox
'    Private rdoOverlay As RadioButton
'    Private rdoSeparatePanel As RadioButton

'    ' 기준선 설정
'    Private grpReferenceLines As GroupBox
'    Private lstReferenceLines As ListBox
'    Private btnAddRefLine As Button
'    Private btnRemoveRefLine As Button
'    Private numRefLineValue As NumericUpDown
'    Private pnlRefLineColor As Panel
'    Private btnRefLineColor As Button
'    Private cmbRefLineStyle As ComboBox

'    ' 과열/침체 구간
'    Private grpZones As GroupBox
'    Private chkEnableZones As CheckBox
'    Private lblOverbought As Label
'    Private numOverbought As NumericUpDown
'    Private lblOversold As Label
'    Private numOversold As NumericUpDown

'    ' Y축 범위
'    Private grpYAxis As GroupBox
'    Private chkAutoScale As CheckBox
'    Private lblYMin As Label
'    Private numYMin As NumericUpDown
'    Private lblYMax As Label
'    Private numYMax As NumericUpDown

'    ' 다이버전스
'    Private chkDivergence As CheckBox

'    ' 알림 설정
'    Private grpAlerts As GroupBox
'    Private chkEnableAlert As CheckBox
'    Private cmbAlertCondition As ComboBox
'    Private numAlertValue As NumericUpDown

'    ' 하단 버튼
'    Private btnPreview As Button
'    Private btnApply As Button
'    Private btnAdd As Button
'    Private btnDelete As Button
'    Private btnClose As Button

'    Private _previewMode As Boolean = False
'    Private _selectedIndicatorKey As String = Nothing

'    Public Sub New(chart As HighPerformanceChartControl, indicatorInstances As Dictionary(Of String, IndicatorInstance))
'        _chart = chart
'        _indicatorInstances = indicatorInstances

'        InitializeComponents()
'        LoadIndicators()
'        LoadPresets()
'    End Sub

'    Private Sub InitializeComponents()
'        Me.Text = "지표 관리"
'        Me.Size = New Size(800, 650)
'        Me.FormBorderStyle = FormBorderStyle.FixedDialog
'        Me.MaximizeBox = False
'        Me.MinimizeBox = False
'        Me.StartPosition = FormStartPosition.CenterParent

'        ' ===== 왼쪽 패널: 활성 지표 목록 =====
'        lstActiveIndicators = New ListBox With {
'            .Location = New Point(10, 40),
'            .Size = New Size(250, 420),
'            .Font = New Font("맑은 고딕", 9)
'        }
'        AddHandler lstActiveIndicators.SelectedIndexChanged, AddressOf lstActiveIndicators_SelectedIndexChanged
'        Me.Controls.Add(lstActiveIndicators)

'        Dim lblActiveList As New Label With {
'            .Text = "활성 지표",
'            .Location = New Point(10, 15),
'            .Size = New Size(100, 20),
'            .Font = New Font("맑은 고딕", 9, FontStyle.Bold)
'        }
'        Me.Controls.Add(lblActiveList)

'        btnAdd = New Button With {
'            .Text = "추가",
'            .Location = New Point(10, 465),
'            .Size = New Size(55, 28)
'        }
'        AddHandler btnAdd.Click, AddressOf btnAdd_Click
'        Me.Controls.Add(btnAdd)

'        btnDelete = New Button With {
'            .Text = "삭제",
'            .Location = New Point(70, 465),
'            .Size = New Size(55, 28)
'        }
'        AddHandler btnDelete.Click, AddressOf btnDelete_Click
'        Me.Controls.Add(btnDelete)

'        Dim btnToggleVisibility As New Button With {
'            .Text = "표시/숨김",
'            .Location = New Point(130, 465),
'            .Size = New Size(75, 28)
'        }
'        AddHandler btnToggleVisibility.Click, AddressOf btnToggleVisibility_Click
'        Me.Controls.Add(btnToggleVisibility)

'        btnMoveUp = New Button With {
'            .Text = "▲",
'            .Location = New Point(210, 465),
'            .Size = New Size(25, 28)
'        }
'        AddHandler btnMoveUp.Click, AddressOf btnMoveUp_Click
'        Me.Controls.Add(btnMoveUp)

'        btnMoveDown = New Button With {
'            .Text = "▼",
'            .Location = New Point(240, 465),
'            .Size = New Size(25, 28)
'        }
'        AddHandler btnMoveDown.Click, AddressOf btnMoveDown_Click
'        Me.Controls.Add(btnMoveDown)

'        btnGroup = New Button With {
'            .Text = "그룹화",
'            .Location = New Point(10, 500),
'            .Size = New Size(70, 28)
'        }
'        Me.Controls.Add(btnGroup)

'        ' 프리셋
'        Dim lblPreset As New Label With {
'            .Text = "프리셋:",
'            .Location = New Point(10, 535),
'            .Size = New Size(60, 20)
'        }
'        Me.Controls.Add(lblPreset)

'        cmbPreset = New ComboBox With {
'            .Location = New Point(70, 533),
'            .Size = New Size(120, 21),
'            .DropDownStyle = ComboBoxStyle.DropDownList
'        }
'        AddHandler cmbPreset.SelectedIndexChanged, AddressOf cmbPreset_SelectedIndexChanged
'        Me.Controls.Add(cmbPreset)

'        btnSavePreset = New Button With {
'            .Text = "저장",
'            .Location = New Point(195, 532),
'            .Size = New Size(55, 23)
'        }
'        AddHandler btnSavePreset.Click, AddressOf btnSavePreset_Click
'        Me.Controls.Add(btnSavePreset)

'        btnDeletePreset = New Button With {
'            .Text = "삭제",
'            .Location = New Point(195, 557),
'            .Size = New Size(55, 23)
'        }
'        Me.Controls.Add(btnDeletePreset)

'        ' ===== 오른쪽 패널: 지표 설정 =====
'        grpIndicatorSettings = New GroupBox With {
'            .Text = "지표 설정",
'            .Location = New Point(270, 10),
'            .Size = New Size(510, 545)
'        }
'        Me.Controls.Add(grpIndicatorSettings)

'        Dim yPos As Integer = 25

'        ' 지표 이름
'        Dim lblName As New Label With {
'            .Text = "이름:",
'            .Location = New Point(10, yPos),
'            .Size = New Size(80, 20)
'        }
'        grpIndicatorSettings.Controls.Add(lblName)

'        txtIndicatorName = New TextBox With {
'            .Location = New Point(100, yPos - 2),
'            .Size = New Size(150, 21),
'            .ReadOnly = True,
'            .BackColor = Color.WhiteSmoke
'        }
'        grpIndicatorSettings.Controls.Add(txtIndicatorName)

'        yPos += 30

'        ' 지표 타입
'        lblType = New Label With {
'            .Text = "지표 타입:",
'            .Location = New Point(10, yPos),
'            .Size = New Size(80, 20)
'        }
'        grpIndicatorSettings.Controls.Add(lblType)

'        cmbType = New ComboBox With {
'            .Location = New Point(100, yPos - 2),
'            .Size = New Size(150, 21),
'            .DropDownStyle = ComboBoxStyle.DropDownList
'        }
'        cmbType.Items.AddRange(New String() {"SMA", "EMA", "RSI", "MACD", "틱강도"})
'        AddHandler cmbType.SelectedIndexChanged, AddressOf cmbType_SelectedIndexChanged
'        grpIndicatorSettings.Controls.Add(cmbType)

'        yPos += 30

'        ' 기간
'        lblPeriod = New Label With {
'            .Text = "기간:",
'            .Location = New Point(10, yPos),
'            .Size = New Size(80, 20)
'        }
'        grpIndicatorSettings.Controls.Add(lblPeriod)

'        numPeriod = New NumericUpDown With {
'            .Location = New Point(100, yPos - 2),
'            .Size = New Size(80, 21),
'            .Minimum = 1,
'            .Maximum = 500,
'            .Value = 5
'        }
'        grpIndicatorSettings.Controls.Add(numPeriod)

'        yPos += 30

'        ' 색상
'        lblColor = New Label With {
'            .Text = "색상:",
'            .Location = New Point(10, yPos),
'            .Size = New Size(80, 20)
'        }
'        grpIndicatorSettings.Controls.Add(lblColor)

'        pnlColor = New Panel With {
'            .Location = New Point(100, yPos - 2),
'            .Size = New Size(60, 21),
'            .BorderStyle = BorderStyle.FixedSingle,
'            .BackColor = Color.Yellow
'        }
'        grpIndicatorSettings.Controls.Add(pnlColor)

'        btnColor = New Button With {
'            .Text = "선택",
'            .Location = New Point(165, yPos - 2),
'            .Size = New Size(60, 21)
'        }
'        AddHandler btnColor.Click, AddressOf btnColor_Click
'        grpIndicatorSettings.Controls.Add(btnColor)

'        yPos += 30

'        ' 선 굵기
'        Dim lblWidth As New Label With {
'            .Text = "선 굵기:",
'            .Location = New Point(10, yPos),
'            .Size = New Size(80, 20)
'        }
'        grpIndicatorSettings.Controls.Add(lblWidth)

'        numLineWidth = New NumericUpDown With {
'            .Location = New Point(100, yPos - 2),
'            .Size = New Size(80, 21),
'            .Minimum = 1,
'            .Maximum = 5,
'            .Value = 2,
'            .DecimalPlaces = 1,
'            .Increment = 0.5D
'        }
'        grpIndicatorSettings.Controls.Add(numLineWidth)

'        yPos += 35

'        ' 표시 모드
'        grpDisplayMode = New GroupBox With {
'            .Text = "표시 모드",
'            .Location = New Point(10, yPos),
'            .Size = New Size(240, 55)
'        }
'        grpIndicatorSettings.Controls.Add(grpDisplayMode)

'        rdoOverlay = New RadioButton With {
'            .Text = "메인 차트 오버레이",
'            .Location = New Point(10, 20),
'            .Size = New Size(150, 20),
'            .Checked = True
'        }
'        grpDisplayMode.Controls.Add(rdoOverlay)

'        rdoSeparatePanel = New RadioButton With {
'            .Text = "독립 패널",
'            .Location = New Point(10, 40),
'            .Size = New Size(150, 20)
'        }
'        grpDisplayMode.Controls.Add(rdoSeparatePanel)

'        yPos += 65

'        ' 기준선 설정
'        grpReferenceLines = New GroupBox With {
'            .Text = "기준선",
'            .Location = New Point(10, yPos),
'            .Size = New Size(240, 120)
'        }
'        grpIndicatorSettings.Controls.Add(grpReferenceLines)

'        lstReferenceLines = New ListBox With {
'            .Location = New Point(10, 20),
'            .Size = New Size(150, 90)
'        }
'        grpReferenceLines.Controls.Add(lstReferenceLines)

'        btnAddRefLine = New Button With {
'            .Text = "+ 추가",
'            .Location = New Point(165, 20),
'            .Size = New Size(65, 25)
'        }
'        AddHandler btnAddRefLine.Click, AddressOf btnAddRefLine_Click
'        grpReferenceLines.Controls.Add(btnAddRefLine)

'        btnRemoveRefLine = New Button With {
'            .Text = "- 제거",
'            .Location = New Point(165, 50),
'            .Size = New Size(65, 25)
'        }
'        AddHandler btnRemoveRefLine.Click, AddressOf btnRemoveRefLine_Click
'        grpReferenceLines.Controls.Add(btnRemoveRefLine)

'        ' 기준선 설정 컨트롤들 (우측)
'        Dim lblRefValue As New Label With {
'            .Text = "값:",
'            .Location = New Point(260, yPos + 25),
'            .Size = New Size(40, 20)
'        }
'        grpIndicatorSettings.Controls.Add(lblRefValue)

'        numRefLineValue = New NumericUpDown With {
'            .Location = New Point(305, yPos + 23),
'            .Size = New Size(80, 21),
'            .Minimum = -10000,
'            .Maximum = 10000,
'            .DecimalPlaces = 2,
'            .Value = 1D
'        }
'        grpIndicatorSettings.Controls.Add(numRefLineValue)

'        pnlRefLineColor = New Panel With {
'            .Location = New Point(305, yPos + 53),
'            .Size = New Size(40, 21),
'            .BorderStyle = BorderStyle.FixedSingle,
'            .BackColor = Color.Gray
'        }
'        grpIndicatorSettings.Controls.Add(pnlRefLineColor)

'        btnRefLineColor = New Button With {
'            .Text = "색상",
'            .Location = New Point(350, yPos + 53),
'            .Size = New Size(50, 21)
'        }
'        AddHandler btnRefLineColor.Click, AddressOf btnRefLineColor_Click
'        grpIndicatorSettings.Controls.Add(btnRefLineColor)

'        cmbRefLineStyle = New ComboBox With {
'            .Location = New Point(305, yPos + 83),
'            .Size = New Size(95, 21),
'            .DropDownStyle = ComboBoxStyle.DropDownList
'        }
'        cmbRefLineStyle.Items.AddRange(New String() {"실선", "점선", "대시"})
'        cmbRefLineStyle.SelectedIndex = 1
'        grpIndicatorSettings.Controls.Add(cmbRefLineStyle)

'        yPos += 130

'        ' 과열/침체 구간
'        grpZones = New GroupBox With {
'            .Text = "과열/침체 구간",
'            .Location = New Point(10, yPos),
'            .Size = New Size(240, 90)
'        }
'        grpIndicatorSettings.Controls.Add(grpZones)

'        chkEnableZones = New CheckBox With {
'            .Text = "구간 표시 활성화",
'            .Location = New Point(10, 20),
'            .Size = New Size(150, 20)
'        }
'        grpZones.Controls.Add(chkEnableZones)

'        lblOverbought = New Label With {
'            .Text = "과열:",
'            .Location = New Point(10, 45),
'            .Size = New Size(60, 20)
'        }
'        grpZones.Controls.Add(lblOverbought)

'        numOverbought = New NumericUpDown With {
'            .Location = New Point(70, 43),
'            .Size = New Size(80, 21),
'            .Minimum = -10000,
'            .Maximum = 10000,
'            .DecimalPlaces = 2,
'            .Value = 1.5D
'        }
'        grpZones.Controls.Add(numOverbought)

'        Dim lblObUnit As New Label With {
'            .Text = "이상",
'            .Location = New Point(155, 45),
'            .Size = New Size(40, 20)
'        }
'        grpZones.Controls.Add(lblObUnit)

'        lblOversold = New Label With {
'            .Text = "침체:",
'            .Location = New Point(10, 67),
'            .Size = New Size(60, 20)
'        }
'        grpZones.Controls.Add(lblOversold)

'        numOversold = New NumericUpDown With {
'            .Location = New Point(70, 65),
'            .Size = New Size(80, 21),
'            .Minimum = -10000,
'            .Maximum = 10000,
'            .DecimalPlaces = 2,
'            .Value = 0.5D
'        }
'        grpZones.Controls.Add(numOversold)

'        Dim lblOsUnit As New Label With {
'            .Text = "이하",
'            .Location = New Point(155, 67),
'            .Size = New Size(40, 20)
'        }
'        grpZones.Controls.Add(lblOsUnit)

'        ' Y축 범위 (우측)
'        grpYAxis = New GroupBox With {
'            .Text = "Y축 범위",
'            .Location = New Point(260, yPos),
'            .Size = New Size(140, 90)
'        }
'        grpIndicatorSettings.Controls.Add(grpYAxis)

'        chkAutoScale = New CheckBox With {
'            .Text = "자동 조정",
'            .Location = New Point(10, 20),
'            .Size = New Size(100, 20),
'            .Checked = True
'        }
'        AddHandler chkAutoScale.CheckedChanged, AddressOf chkAutoScale_CheckedChanged
'        grpYAxis.Controls.Add(chkAutoScale)

'        Dim lblMin As New Label With {
'            .Text = "최소:",
'            .Location = New Point(10, 45),
'            .Size = New Size(40, 20)
'        }
'        grpYAxis.Controls.Add(lblMin)

'        numYMin = New NumericUpDown With {
'            .Location = New Point(55, 43),
'            .Size = New Size(70, 21),
'            .Minimum = -10000,
'            .Maximum = 10000,
'            .Value = 0,
'            .Enabled = False
'        }
'        grpYAxis.Controls.Add(numYMin)

'        Dim lblMax As New Label With {
'            .Text = "최대:",
'            .Location = New Point(10, 67),
'            .Size = New Size(40, 20)
'        }
'        grpYAxis.Controls.Add(lblMax)

'        numYMax = New NumericUpDown With {
'            .Location = New Point(55, 65),
'            .Size = New Size(70, 21),
'            .Minimum = -10000,
'            .Maximum = 10000,
'            .Value = 100,
'            .Enabled = False
'        }
'        grpYAxis.Controls.Add(numYMax)

'        yPos += 100

'        ' 다이버전스
'        chkDivergence = New CheckBox With {
'            .Text = "다이버전스 자동 표시",
'            .Location = New Point(10, yPos),
'            .Size = New Size(200, 20)
'        }
'        grpIndicatorSettings.Controls.Add(chkDivergence)

'        yPos += 30

'        ' 알림 설정
'        grpAlerts = New GroupBox With {
'            .Text = "알림 설정",
'            .Location = New Point(10, yPos),
'            .Size = New Size(390, 65)
'        }
'        grpIndicatorSettings.Controls.Add(grpAlerts)

'        chkEnableAlert = New CheckBox With {
'            .Text = "알림 활성화",
'            .Location = New Point(10, 20),
'            .Size = New Size(100, 20)
'        }
'        grpAlerts.Controls.Add(chkEnableAlert)

'        cmbAlertCondition = New ComboBox With {
'            .Location = New Point(10, 40),
'            .Size = New Size(150, 21),
'            .DropDownStyle = ComboBoxStyle.DropDownList
'        }
'        cmbAlertCondition.Items.AddRange(New String() {"값이 초과", "값이 미만", "상향 돌파", "하향 돌파"})
'        cmbAlertCondition.SelectedIndex = 0
'        grpAlerts.Controls.Add(cmbAlertCondition)

'        numAlertValue = New NumericUpDown With {
'            .Location = New Point(165, 40),
'            .Size = New Size(80, 21),
'            .Minimum = -10000,
'            .Maximum = 10000,
'            .DecimalPlaces = 2
'        }
'        grpAlerts.Controls.Add(numAlertValue)

'        ' ===== 하단 버튼 =====
'        btnPreview = New Button With {
'            .Text = "미리보기",
'            .Location = New Point(270, 565),
'            .Size = New Size(90, 30)
'        }
'        AddHandler btnPreview.Click, AddressOf btnPreview_Click
'        Me.Controls.Add(btnPreview)

'        btnApply = New Button With {
'            .Text = "적용",
'            .Location = New Point(365, 565),
'            .Size = New Size(90, 30)
'        }
'        AddHandler btnApply.Click, AddressOf btnApply_Click
'        Me.Controls.Add(btnApply)

'        btnAdd = New Button With {
'            .Text = "추가",
'            .Location = New Point(460, 565),
'            .Size = New Size(90, 30)
'        }
'        AddHandler btnAdd.Click, AddressOf btnAdd_Click
'        Me.Controls.Add(btnAdd)

'        btnDelete = New Button With {
'            .Text = "삭제",
'            .Location = New Point(555, 565),
'            .Size = New Size(90, 30)
'        }
'        AddHandler btnDelete.Click, AddressOf btnDelete_Click
'        Me.Controls.Add(btnDelete)

'        btnClose = New Button With {
'            .Text = "닫기",
'            .Location = New Point(690, 565),
'            .Size = New Size(90, 30)
'        }
'        AddHandler btnClose.Click, Sub() Me.Close()
'        Me.Controls.Add(btnClose)
'    End Sub

'    Private Sub LoadIndicators()
'        lstActiveIndicators.Items.Clear()

'        For Each kvp In _indicatorInstances.OrderBy(Function(k) k.Value.ZOrder)
'            Dim visibleIcon = If(kvp.Value.IsVisible, "☑", "☐")
'            Dim displayText = $"{visibleIcon} {kvp.Value.Name}"
'            lstActiveIndicators.Items.Add(displayText)
'        Next
'    End Sub

'    Private Sub LoadPresets()
'        cmbPreset.Items.Clear()
'        cmbPreset.Items.AddRange(New String() {"기본 설정", "단타용", "스윙용"})

'        ' ⭐ 중요: SelectedIndexChanged 이벤트를 발생시키지 않고 선택
'        RemoveHandler cmbPreset.SelectedIndexChanged, AddressOf cmbPreset_SelectedIndexChanged
'        cmbPreset.SelectedIndex = 0
'        AddHandler cmbPreset.SelectedIndexChanged, AddressOf cmbPreset_SelectedIndexChanged
'    End Sub

'    ' lstActiveIndicators_SelectedIndexChanged - 지표 선택 시
'    Private Sub lstActiveIndicators_SelectedIndexChanged(sender As Object, e As EventArgs)

'        If lstActiveIndicators.SelectedIndex < 0 Then
'            Return
'        End If

'        Dim orderedKeys = _indicatorInstances.OrderBy(Function(k) k.Value.ZOrder).Select(Function(k) k.Key).ToList()
'        Dim selectedKey = orderedKeys(lstActiveIndicators.SelectedIndex)
'        _selectedIndicatorKey = selectedKey
'        Dim instance = _indicatorInstances(selectedKey)

'        ' UI에 설정 로드
'        txtIndicatorName.Text = instance.Name

'        ' 기간
'        If instance.Parameters.ContainsKey("period") Then
'            numPeriod.Value = CInt(instance.Parameters("period"))
'        End If

'        ' 색상
'        If instance.Parameters.ContainsKey("color") Then
'            pnlColor.BackColor = CType(instance.Parameters("color"), Color)
'        End If

'        ' 선 굵기
'        numLineWidth.Value = CDec(instance.LineWidth)

'        ' 표시 모드
'        If instance.DisplayMode = ChartDisplayMode.Overlay Then
'            rdoOverlay.Checked = True
'        Else
'            rdoSeparatePanel.Checked = True
'        End If

'        ' Y축 설정
'        chkAutoScale.Checked = instance.AutoScale
'        numYMin.Value = If(instance.YAxisMin.HasValue, CDec(instance.YAxisMin.Value), 0)
'        numYMax.Value = If(instance.YAxisMax.HasValue, CDec(instance.YAxisMax.Value), 100)

'        ' 과열/침체 구간
'        chkEnableZones.Checked = instance.EnableZones
'        numOverbought.Value = If(instance.OverboughtLevel.HasValue, CDec(instance.OverboughtLevel.Value), 70)
'        numOversold.Value = If(instance.OversoldLevel.HasValue, CDec(instance.OversoldLevel.Value), 30)

'        ' 다이버전스
'        chkDivergence.Checked = instance.EnableDivergence

'        ' 기준선 로드
'        LoadReferenceLines(instance)

'    End Sub

'    Private Sub LoadReferenceLines(instance As IndicatorInstance)
'        lstReferenceLines.Items.Clear()
'        If instance.ReferenceLines IsNot Nothing Then
'            For Each refLine In instance.ReferenceLines
'                Dim colorName = GetColorName(refLine.Color)
'                Dim styleName = GetDashStyleName(refLine.Style)
'                lstReferenceLines.Items.Add($"{refLine.Value:F2} ({colorName}) {styleName}")
'            Next
'        End If
'    End Sub

'    Private Sub cmbType_SelectedIndexChanged(sender As Object, e As EventArgs)
'        ' 모든 지표에서 기간 수정 가능하도록 변경
'        Select Case cmbType.SelectedIndex
'            Case 0, 1 ' SMA, EMA
'                numPeriod.Enabled = True
'                lblPeriod.Enabled = True
'                btnColor.Enabled = True
'                lblColor.Enabled = True
'            Case 2 ' RSI
'                numPeriod.Enabled = True  ' RSI도 기간 수정 가능
'                lblPeriod.Enabled = True
'                btnColor.Enabled = True
'                lblColor.Enabled = True
'            Case 3, 4 ' MACD, 틱강도
'                numPeriod.Enabled = False
'                lblPeriod.Enabled = False
'                btnColor.Enabled = False
'                lblColor.Enabled = False
'        End Select
'    End Sub

'    Private Sub chkAutoScale_CheckedChanged(sender As Object, e As EventArgs)
'        numYMin.Enabled = Not chkAutoScale.Checked
'        numYMax.Enabled = Not chkAutoScale.Checked
'    End Sub

'    Private Sub btnColor_Click(sender As Object, e As EventArgs)
'        Using colorDialog As New ColorDialog()
'            colorDialog.Color = pnlColor.BackColor
'            If colorDialog.ShowDialog() = DialogResult.OK Then
'                pnlColor.BackColor = colorDialog.Color
'            End If
'        End Using
'    End Sub

'    Private Sub btnRefLineColor_Click(sender As Object, e As EventArgs)
'        Using colorDialog As New ColorDialog()
'            colorDialog.Color = pnlRefLineColor.BackColor
'            If colorDialog.ShowDialog() = DialogResult.OK Then
'                pnlRefLineColor.BackColor = colorDialog.Color
'            End If
'        End Using
'    End Sub

'    Private Sub btnAddRefLine_Click(sender As Object, e As EventArgs)
'        If _selectedIndicatorKey Is Nothing Then
'            MessageBox.Show("먼저 지표를 선택하세요.", "알림")
'            Return
'        End If

'        Dim value = numRefLineValue.Value
'        Dim color = pnlRefLineColor.BackColor
'        Dim styleText = cmbRefLineStyle.Text

'        ' 리스트박스에 추가 (친숙한 색상 이름 사용)
'        Dim colorName = GetColorName(color)
'        lstReferenceLines.Items.Add($"{value:F2} ({colorName}) {styleText}")
'    End Sub

'    Private Sub btnRemoveRefLine_Click(sender As Object, e As EventArgs)
'        If lstReferenceLines.SelectedIndex >= 0 Then
'            lstReferenceLines.Items.RemoveAt(lstReferenceLines.SelectedIndex)
'        End If
'    End Sub

'    ' btnToggleVisibility_Click - 표시/숨김 토글
'    Private Sub btnToggleVisibility_Click(sender As Object, e As EventArgs)
'        If lstActiveIndicators.SelectedIndex < 0 Then
'            MessageBox.Show("지표를 선택하세요.", "알림")
'            Return
'        End If

'        Dim orderedKeys = _indicatorInstances.OrderBy(Function(k) k.Value.ZOrder).Select(Function(k) k.Key).ToList()
'        Dim selectedKey = orderedKeys(lstActiveIndicators.SelectedIndex)
'        Dim instance = _indicatorInstances(selectedKey)

'        ' 상태 토글
'        instance.IsVisible = Not instance.IsVisible

'        If instance.IsVisible Then
'            Dim indicator = instance.CreateIndicator()
'            If indicator IsNot Nothing Then
'                _chart.AddIndicator(indicator)
'            End If
'        Else
'            _chart.RemoveIndicator(instance.Name)
'        End If

'        ' 목록 새로고침
'        Dim currentIndex = lstActiveIndicators.SelectedIndex
'        LoadIndicators()

'        ' 같은 위치 다시 선택
'        If currentIndex < lstActiveIndicators.Items.Count Then
'            lstActiveIndicators.SelectedIndex = currentIndex
'        End If

'        Logger.Instance.log($"[{instance.Name}] 표시: {instance.IsVisible}")
'    End Sub

'    Private Sub btnMoveUp_Click(sender As Object, e As EventArgs)
'        If lstActiveIndicators.SelectedIndex > 0 Then
'            Dim idx = lstActiveIndicators.SelectedIndex
'            Dim item = lstActiveIndicators.Items(idx)
'            lstActiveIndicators.Items.RemoveAt(idx)
'            lstActiveIndicators.Items.Insert(idx - 1, item)
'            lstActiveIndicators.SelectedIndex = idx - 1

'            ' ZOrder 업데이트
'            UpdateZOrder()
'        End If
'    End Sub

'    Private Sub btnMoveDown_Click(sender As Object, e As EventArgs)
'        If lstActiveIndicators.SelectedIndex >= 0 AndAlso lstActiveIndicators.SelectedIndex < lstActiveIndicators.Items.Count - 1 Then
'            Dim idx = lstActiveIndicators.SelectedIndex
'            Dim item = lstActiveIndicators.Items(idx)
'            lstActiveIndicators.Items.RemoveAt(idx)
'            lstActiveIndicators.Items.Insert(idx + 1, item)
'            lstActiveIndicators.SelectedIndex = idx + 1

'            ' ZOrder 업데이트
'            UpdateZOrder()
'        End If
'    End Sub

'    Private Sub UpdateZOrder()
'        Dim keys = _indicatorInstances.Keys.ToList()
'        For i = 0 To lstActiveIndicators.Items.Count - 1
'            Dim key = keys(i)
'            _indicatorInstances(key).ZOrder = i
'        Next
'    End Sub

'    Private Sub btnPreview_Click(sender As Object, e As EventArgs)
'        _previewMode = True
'        ApplySettings(True)
'        MessageBox.Show("미리보기가 적용되었습니다. '적용' 버튼을 누르면 저장됩니다.", "미리보기")
'    End Sub

'    Private Sub btnApply_Click(sender As Object, e As EventArgs)
'        'ApplySettings(False)
'        '_previewMode = False
'        'MessageBox.Show("설정이 적용되었습니다.", "알림")

'        Logger.Instance.log("=== 적용 버튼 클릭 ===")
'        ApplySettings(False)
'        MessageBox.Show("설정이 적용되었습니다.", "알림")


'    End Sub

'    ' ApplySettings - 설정 적용 (완전 구현)
'    Private Sub ApplySettings(isPreview As Boolean)
'        If _selectedIndicatorKey Is Nothing Then
'            Return
'        End If

'        Dim instance = _indicatorInstances(_selectedIndicatorKey)

'        ' ===== 기본 파라미터 업데이트 =====
'        If numPeriod.Enabled AndAlso numPeriod.Value > 0 Then
'            If Not instance.Parameters.ContainsKey("period") Then
'                instance.Parameters.Add("period", CInt(numPeriod.Value))
'            Else
'                instance.Parameters("period") = CInt(numPeriod.Value)
'            End If
'        End If

'        If pnlColor.BackColor <> Color.Empty Then
'            If Not instance.Parameters.ContainsKey("color") Then
'                instance.Parameters.Add("color", pnlColor.BackColor)
'            Else
'                instance.Parameters("color") = pnlColor.BackColor
'            End If
'        End If

'        ' ===== 선 굵기 =====
'        instance.LineWidth = CSng(numLineWidth.Value)

'        ' ===== 표시 모드 =====
'        instance.DisplayMode = If(rdoOverlay.Checked, ChartDisplayMode.Overlay, ChartDisplayMode.Separate)

'        ' ===== Y축 범위 =====
'        instance.AutoScale = chkAutoScale.Checked
'        If chkAutoScale.Checked Then
'            instance.YAxisMin = Nothing
'            instance.YAxisMax = Nothing
'        Else
'            instance.YAxisMin = CDbl(numYMin.Value)
'            instance.YAxisMax = CDbl(numYMax.Value)
'        End If

'        ' ===== 과열/침체 구간 =====
'        instance.EnableZones = chkEnableZones.Checked
'        If chkEnableZones.Checked Then
'            instance.OverboughtLevel = CDbl(numOverbought.Value)
'            instance.OversoldLevel = CDbl(numOversold.Value)
'        Else
'            instance.OverboughtLevel = Nothing
'            instance.OversoldLevel = Nothing
'        End If

'        ' ===== 다이버전스 =====
'        instance.EnableDivergence = chkDivergence.Checked

'        ' ===== 알림 설정 =====
'        instance.AlertEnabled = chkEnableAlert.Checked
'        If chkEnableAlert.Checked Then
'            instance.AlertCondition = cmbAlertCondition.Text
'            instance.AlertValue = CDbl(numAlertValue.Value)
'        End If

'        '' ===== 기준선 업데이트 (⭐ 핵심) =====
'        'instance.ReferenceLines.Clear()
'        'Logger.Instance.log($"[디버그] 기준선 리스트 개수: {lstReferenceLines.Items.Count}")

'        'For Each item As String In lstReferenceLines.Items
'        '    Try
'        '        Logger.Instance.log($"[디버그] 기준선 항목: '{item}'")

'        '        ' 형식: "50.00 (Gray) 점선"
'        '        Dim parts = item.Split(" "c)
'        '        Logger.Instance.log($"[디버그] 파싱 결과: parts.Length={parts.Length}, parts={String.Join("|", parts)}")

'        '        If parts.Length >= 2 Then
'        '            Dim valueStr = parts(0)

'        '            ' 기존 문제 라인
'        '            'Dim colorStr = parts(1).Trim("("c, ")"c)

'        '            '--- 새 로직 : 앞 "(" 하나만 제거, 뒤 ")"은 절대 건드리지 않음 -------------
'        '            Dim rawColor As String = parts(1)                 ' ex: "(RGB(128,128,128)"
'        '            If rawColor.StartsWith("("c) Then
'        '                rawColor = rawColor.Substring(1)              ' ex: "RGB(128,128,128)"
'        '            End If
'        '            Dim colorStr As String = rawColor                 ' 최종 colorStr

'        '            Dim styleStr = If(parts.Length >= 3, parts(2), "점선")

'        '            Dim value As Double
'        '            If Double.TryParse(valueStr, value) Then
'        '                ' ⭐ 색상 매핑 (한글 → Color)
'        '                Dim refColor As Color = Color.Gray

'        '                ' RGB(r,g,b) 형식 처리
'        '                If colorStr.StartsWith("RGB(", StringComparison.OrdinalIgnoreCase) AndAlso colorStr.EndsWith(")") Then
'        '                    Try
'        '                        Dim rgb = colorStr.Substring(4, colorStr.Length - 5).Split(","c)
'        '                        If rgb.Length = 3 Then
'        '                            refColor = Color.FromArgb(Integer.Parse(rgb(0)), Integer.Parse(rgb(1)), Integer.Parse(rgb(2)))
'        '                            Logger.Instance.log($"[디버그] RGB 색상 파싱: {colorStr} -> R={refColor.R}, G={refColor.G}, B={refColor.B}")
'        '                        End If
'        '                    Catch ex As Exception
'        '                        Logger.Instance.log($"[디버그] RGB 파싱 실패: {colorStr}")
'        '                    End Try
'        '                Else
'        '                    ' 이름 기반 색상 매핑
'        '                    Select Case colorStr.ToLower()
'        '                        Case "red", "빨강", "빨간색"
'        '                            refColor = Color.Red
'        '                        Case "blue", "파랑", "파란색"
'        '                            refColor = Color.Blue
'        '                        Case "green", "녹색", "초록"
'        '                            refColor = Color.Green
'        '                        Case "yellow", "노랑", "노란색"
'        '                            refColor = Color.Yellow
'        '                        Case "orange", "오렌지", "주황"
'        '                            refColor = Color.Orange
'        '                        Case "purple", "보라", "보라색"
'        '                            refColor = Color.Purple
'        '                        Case "gray", "grey", "회색"
'        '                            refColor = Color.Gray
'        '                        Case "white", "흰색"
'        '                            refColor = Color.White
'        '                        Case "black", "검정", "검은색"
'        '                            refColor = Color.Black
'        '                        Case "cyan", "시안"
'        '                            refColor = Color.Cyan
'        '                        Case "magenta", "마젠타"
'        '                            refColor = Color.Magenta
'        '                        Case Else
'        '                            ' Color.FromName() 시도
'        '                            Dim namedColor = Color.FromName(colorStr)
'        '                            If namedColor.A <> 0 OrElse namedColor.R <> 0 OrElse namedColor.G <> 0 OrElse namedColor.B <> 0 Then
'        '                                refColor = namedColor
'        '                            Else
'        '                                Logger.Instance.log($"[디버그] 색상 '{colorStr}' 인식 실패 -> Gray로 대체")
'        '                            End If
'        '                    End Select
'        '                End If

'        '                Dim refStyle As DashStyle = DashStyle.Dash

'        '                Select Case styleStr
'        '                    Case "실선"
'        '                        refStyle = DashStyle.Solid
'        '                    Case "점선"
'        '                        refStyle = DashStyle.Dot
'        '                    Case "대시"
'        '                        refStyle = DashStyle.Dash
'        '                End Select

'        '                instance.ReferenceLines.Add(New ReferenceLine With {
'        '                    .Value = value,
'        '                    .Color = refColor,
'        '                    .Style = refStyle
'        '                })

'        '                Logger.Instance.log($"[디버그] 기준선 추가: Value={value}, Color={refColor.Name} (A={refColor.A}, R={refColor.R}, G={refColor.G}, B={refColor.B}), Style={refStyle}")
'        '            End If
'        '        End If
'        '    Catch ex As Exception
'        '        Logger.Instance.log($"기준선 파싱 오류: {ex.Message}", Warning.ErrorInfo)
'        '    End Try
'        'Next

'        ' ===== 기준선 업데이트 (⭐ 핵심) =====
'        instance.ReferenceLines.Clear()
'        Logger.Instance.log($"[디버그] 기준선 리스트 개수: {lstReferenceLines.Items.Count}")

'        For Each item As String In lstReferenceLines.Items
'            Try
'                Logger.Instance.log($"[디버그] 기준선 항목: '{item}'")

'                '--- 공백 2번만 Split(3) 효과와 동일 : VB2015 이하도 OK -------------
'                Dim valueStr As String = ""
'                Dim colorStr As String = ""
'                Dim styleStr As String = "점선"          '기본값

'                Dim sp1 As Integer = item.IndexOf(" "c)
'                If sp1 >= 0 Then
'                    valueStr = item.Substring(0, sp1).Trim()

'                    Dim sp2 As Integer = item.IndexOf(" "c, sp1 + 1)
'                    If sp2 >= 0 Then
'                        colorStr = item.Substring(sp1 + 1, sp2 - sp1 - 1).Trim()
'                        styleStr = item.Substring(sp2 + 1).Trim()
'                    Else
'                        colorStr = item.Substring(sp1 + 1).Trim()
'                    End If
'                Else
'                    valueStr = item.Trim()
'                End If
'                '---------------------------------------------------------------------

'                '--- 앞 "(" 하나만 제거, 뒤 ")"은 보존 ------------------------------
'                If colorStr.StartsWith("("c) Then colorStr = colorStr.Substring(1)
'                '---------------------------------------------------------------------

'                Dim value As Double
'                If Double.TryParse(valueStr, value) Then
'                    Dim refColor As Color = Color.Gray

'                    ' RGB(r,g,b) 형식 처리
'                    If colorStr.StartsWith("RGB(", StringComparison.OrdinalIgnoreCase) AndAlso
'               colorStr.EndsWith(")") Then
'                        Try
'                            Dim rgb = colorStr.Substring(4, colorStr.Length - 5).Split(","c)
'                            If rgb.Length = 3 Then
'                                refColor = Color.FromArgb(CInt(rgb(0)), CInt(rgb(1)), CInt(rgb(2)))
'                                Logger.Instance.log($"[디버그] RGB 색상 파싱: {colorStr} -> R={refColor.R}, G={refColor.G}, B={refColor.B}")
'                            End If
'                        Catch ex As Exception
'                            Logger.Instance.log($"[디버그] RGB 파싱 실패: {colorStr}")
'                        End Try
'                    Else
'                        ' 이름 기반 색상 매핑
'                        Select Case colorStr.ToLower()
'                            Case "red", "빨강", "빨간색" : refColor = Color.Red
'                            Case "blue", "파랑", "파란색" : refColor = Color.Blue
'                            Case "green", "녹색", "초록" : refColor = Color.Green
'                            Case "yellow", "노랑", "노란색" : refColor = Color.Yellow
'                            Case "orange", "오렌지", "주황" : refColor = Color.Orange
'                            Case "purple", "보라", "보라색" : refColor = Color.Purple
'                            Case "gray", "grey", "회색" : refColor = Color.Gray
'                            Case "white", "흰색" : refColor = Color.White
'                            Case "black", "검정", "검은색" : refColor = Color.Black
'                            Case "cyan", "시안" : refColor = Color.Cyan
'                            Case "magenta", "마젠타" : refColor = Color.Magenta
'                            Case Else
'                                Dim namedColor = Color.FromName(colorStr)
'                                If namedColor.A <> 0 OrElse namedColor.R <> 0 OrElse namedColor.G <> 0 OrElse namedColor.B <> 0 Then
'                                    refColor = namedColor
'                                Else
'                                    Logger.Instance.log($"[디버그] 색상 '{colorStr}' 인식 실패 -> Gray로 대체")
'                                End If
'                        End Select
'                    End If

'                    Dim refStyle As DashStyle = DashStyle.Dot
'                    Select Case styleStr
'                        Case "실선" : refStyle = DashStyle.Solid
'                        Case "점선" : refStyle = DashStyle.Dot
'                        Case "대시" : refStyle = DashStyle.Dash
'                    End Select

'                    instance.ReferenceLines.Add(New ReferenceLine With {
'                .Value = value,
'                .Color = refColor,
'                .Style = refStyle
'            })

'                    Logger.Instance.log($"[디버그] 기준선 추가: Value={value}, Color={refColor.Name} (A={refColor.A}, R={refColor.R}, G={refColor.G}, B={refColor.B}), Style={refStyle}")
'                End If
'            Catch ex As Exception
'                Logger.Instance.log($"기준선 파싱 오류: {ex.Message}", Warning.ErrorInfo)
'            End Try
'        Next




'        Logger.Instance.log($"[디버그] 최종 기준선 개수: {instance.ReferenceLines.Count}")

'        ' ===== 차트 업데이트 =====
'        If instance.IsVisible Then
'            Logger.Instance.log($"[디버그] 차트 업데이트 시작: {instance.Name}")
'            _chart.RemoveIndicator(instance.Name)

'            Dim indicator = instance.CreateIndicator()
'            If indicator IsNot Nothing Then
'                Logger.Instance.log($"[디버그] CreateIndicator 성공: {indicator.Name}")

'                ' 생성된 지표의 메타데이터 확인
'                Dim metadataList = indicator.GetSeriesMetadata()
'                For Each meta In metadataList
'                    Logger.Instance.log($"[디버그] 메타데이터: {meta.Name}")
'                    Logger.Instance.log($"[디버그]   EnableZones={meta.EnableZones}, OB={meta.OverboughtLevel}, OS={meta.OversoldLevel}")
'                    Logger.Instance.log($"[디버그]   AutoScale={meta.AxisInfo.AutoScale}, Min={meta.AxisInfo.Min}, Max={meta.AxisInfo.Max}")
'                    Logger.Instance.log($"[디버그]   ReferenceLines.Count={meta.ReferenceLines.Count}")

'                    For i = 0 To meta.ReferenceLines.Count - 1
'                        Dim refLine = meta.ReferenceLines(i)
'                        Logger.Instance.log($"[디버그]     기준선[{i}]: Value={refLine.Value}, Color={refLine.Color.Name} (A={refLine.Color.A}, R={refLine.Color.R}), Style={refLine.Style}")
'                    Next
'                Next

'                _chart.AddIndicator(indicator)
'                Logger.Instance.log($"[{instance.Name}] 설정 적용: Zones={instance.EnableZones}, 기준선={instance.ReferenceLines.Count}개")
'            Else
'                Logger.Instance.log($"[{instance.Name}] CreateIndicator 실패", Warning.ErrorInfo)
'            End If
'        End If
'    End Sub

'    Private Sub btnAdd_Click(sender As Object, e As EventArgs)
'        ' 기존 추가 로직
'        If cmbType.SelectedIndex < 0 Then
'            MessageBox.Show("지표 타입을 선택하세요.", "알림")
'            Return
'        End If

'        Dim instance As New IndicatorInstance With {.IsVisible = True}

'        Select Case cmbType.SelectedIndex
'            Case 0
'                instance.IndicatorType = GetType(SMAIndicator)
'                instance.Parameters("period") = CInt(numPeriod.Value)
'                instance.Parameters("color") = pnlColor.BackColor
'                instance.Name = $"SMA({numPeriod.Value})"
'            Case 1
'                instance.IndicatorType = GetType(SMAIndicator)
'                instance.Parameters("period") = CInt(numPeriod.Value)
'                instance.Parameters("color") = pnlColor.BackColor
'                instance.Name = $"EMA({numPeriod.Value})"
'            Case 2
'                instance.IndicatorType = GetType(RSIIndicator)
'                instance.Name = "RSI(14)"
'            Case 3
'                instance.IndicatorType = GetType(MACDIndicator)
'                instance.Name = "MACD"
'            Case 4
'                instance.IndicatorType = GetType(TickIntensityIndicator)
'                instance.Name = "틱강도"
'        End Select

'        Dim key = instance.GetKey()
'        If _indicatorInstances.ContainsKey(key) Then
'            MessageBox.Show("이미 동일한 지표가 존재합니다.", "알림")
'            Return
'        End If

'        _indicatorInstances(key) = instance

'        If instance.IsVisible Then
'            Dim indicator = instance.CreateIndicator()
'            If indicator IsNot Nothing Then
'                _chart.AddIndicator(indicator)
'            End If
'        End If

'        LoadIndicators()
'        MessageBox.Show("지표가 추가되었습니다.", "알림")
'    End Sub

'    Private Sub btnDelete_Click(sender As Object, e As EventArgs)
'        If lstActiveIndicators.SelectedIndex < 0 Then
'            MessageBox.Show("삭제할 지표를 선택하세요.", "알림")
'            Return
'        End If

'        Dim selectedKey = _indicatorInstances.Keys.ElementAt(lstActiveIndicators.SelectedIndex)
'        Dim instance = _indicatorInstances(selectedKey)

'        If MessageBox.Show($"{instance.Name} 지표를 삭제하시겠습니까?", "확인", MessageBoxButtons.YesNo) = DialogResult.Yes Then
'            _chart.RemoveIndicator(instance.Name)
'            _indicatorInstances.Remove(selectedKey)
'            LoadIndicators()
'            MessageBox.Show("지표가 삭제되었습니다.", "알림")
'        End If
'    End Sub

'    Private Sub cmbPreset_SelectedIndexChanged(sender As Object, e As EventArgs)
'        Select Case cmbPreset.SelectedIndex
'            Case 0 ' 기본 설정
'                LoadDefaultPreset()
'            Case 1 ' 단타용
'                LoadScalpingPreset()
'            Case 2 ' 스윙용
'                LoadSwingPreset()
'        End Select

'        Logger.Instance.log($"프리셋 적용: {cmbPreset.Text}")
'    End Sub

'    Private Sub LoadDefaultPreset()
'        ' 기본 설정: SMA(5), SMA(20)만 활성화
'        For Each kvp In _indicatorInstances
'            Dim instance = kvp.Value
'            Dim shouldBeVisible = (instance.Name = "SMA(5)" OrElse instance.Name = "SMA(20)")

'            If instance.IsVisible <> shouldBeVisible Then
'                instance.IsVisible = shouldBeVisible

'                If instance.IsVisible Then
'                    Dim indicator = instance.CreateIndicator()
'                    If indicator IsNot Nothing Then
'                        _chart.AddIndicator(indicator)
'                    End If
'                Else
'                    _chart.RemoveIndicator(instance.Name)
'                End If
'            End If
'        Next

'        LoadIndicators()
'    End Sub

'    Private Sub LoadScalpingPreset()
'        For Each kvp In _indicatorInstances
'            Dim instance = kvp.Value
'            Dim shouldBeVisible = (instance.Name = "SMA(5)" OrElse instance.Name = "틱강도")

'            If instance.IsVisible <> shouldBeVisible Then
'                instance.IsVisible = shouldBeVisible

'                If instance.IsVisible Then
'                    Dim indicator = instance.CreateIndicator()
'                    If indicator IsNot Nothing Then
'                        _chart.AddIndicator(indicator)
'                    End If
'                Else
'                    _chart.RemoveIndicator(instance.Name)
'                End If
'            End If
'        Next

'        LoadIndicators()
'    End Sub

'    Private Sub LoadSwingPreset()
'        ' SMA(60) 추가 (없으면)
'        If Not _indicatorInstances.ContainsKey("SMA(60)") Then
'            Dim sma60 As New IndicatorInstance With {
'                .IndicatorType = GetType(SMAIndicator),
'                .Name = "SMA(60)",
'                .Parameters = New Dictionary(Of String, Object) From {
'                    {"period", 60},
'                    {"color", Color.Orange}
'                },
'                .IsVisible = True,
'                .DisplayMode = ChartDisplayMode.Overlay,
'                .LineWidth = 2.0F,
'                .ZOrder = 2
'            }
'            _indicatorInstances("SMA(60)") = sma60
'        End If

'        For Each kvp In _indicatorInstances
'            Dim instance = kvp.Value
'            Dim shouldBeVisible = (instance.Name = "SMA(20)" OrElse
'                                  instance.Name = "SMA(60)" OrElse
'                                  instance.Name.StartsWith("RSI") OrElse
'                                  instance.Name = "MACD")

'            If instance.IsVisible <> shouldBeVisible Then
'                instance.IsVisible = shouldBeVisible

'                If instance.IsVisible Then
'                    Dim indicator = instance.CreateIndicator()
'                    If indicator IsNot Nothing Then
'                        _chart.AddIndicator(indicator)
'                    End If
'                Else
'                    _chart.RemoveIndicator(instance.Name)
'                End If
'            End If
'        Next

'        LoadIndicators()
'    End Sub
'    Private Sub btnSavePreset_Click(sender As Object, e As EventArgs)
'        Dim presetName = InputBox("프리셋 이름을 입력하세요:", "프리셋 저장")
'        If String.IsNullOrWhiteSpace(presetName) Then Return

'        ' 프리셋 저장 로직 (파일 또는 설정에 저장)
'        MessageBox.Show($"프리셋 '{presetName}'이(가) 저장되었습니다.", "알림")

'        ' 콤보박스에 추가
'        If Not cmbPreset.Items.Contains(presetName) Then
'            cmbPreset.Items.Add(presetName)
'        End If
'    End Sub

'    ' 닫기 버튼
'    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
'        MyBase.OnFormClosing(e)
'    End Sub
'    ''' <summary>
'    ''' Color를 친숙한 이름으로 변환
'    ''' </summary>
'    Private Function GetColorName(c As Color) As String
'        If c = Color.Red Then Return "Red"
'        If c = Color.Blue Then Return "Blue"
'        If c = Color.Green Then Return "Green"
'        If c = Color.Yellow Then Return "Yellow"
'        If c = Color.Orange Then Return "Orange"
'        If c = Color.Purple Then Return "Purple"
'        If c = Color.Gray Then Return "Gray"
'        If c = Color.White Then Return "White"
'        If c = Color.Black Then Return "Black"
'        If c = Color.Cyan Then Return "Cyan"
'        If c = Color.Magenta Then Return "Magenta"

'        ' 기타 - 이름 반환 (또는 RGB 표시)
'        If c.IsNamedColor Then
'            Return c.Name
'        Else
'            Return $"RGB({c.R},{c.G},{c.B})"
'        End If
'    End Function

'    ''' <summary>
'    ''' DashStyle을 한글 이름으로 변환
'    ''' </summary>
'    Private Function GetDashStyleName(style As DashStyle) As String
'        Select Case style
'            Case DashStyle.Solid
'                Return "실선"
'            Case DashStyle.Dash
'                Return "대시"
'            Case DashStyle.Dot
'                Return "점선"
'            Case DashStyle.DashDot
'                Return "대시점선"
'            Case DashStyle.DashDotDot
'                Return "대시점점선"
'            Case Else
'                Return "점선"
'        End Select
'    End Function


'End Class

'#End Region