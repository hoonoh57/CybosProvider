<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form은 Dispose를 재정의하여 구성 요소 목록을 정리합니다.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Windows Form 디자이너에 필요합니다.
    Private components As System.ComponentModel.IContainer

    '참고: 다음 프로시저는 Windows Form 디자이너에 필요합니다.
    '수정하려면 Windows Form 디자이너를 사용하십시오.  
    '코드 편집기에서는 수정하지 마세요.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.FlowLayoutPanel1 = New System.Windows.Forms.FlowLayoutPanel()
        Me.ComboBox1 = New System.Windows.Forms.ComboBox()
        Me.cmbTickers = New System.Windows.Forms.ComboBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.dtpStart = New System.Windows.Forms.DateTimePicker()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.cmbTimeFrame = New System.Windows.Forms.ComboBox()
        Me.cmbStrategy = New System.Windows.Forms.ComboBox()
        Me.btnConvert = New System.Windows.Forms.Button()
        Me.btnIndicators = New System.Windows.Forms.Button()
        Me.chkSimulation = New System.Windows.Forms.CheckBox()
        Me.chkApplyStrategy = New System.Windows.Forms.CheckBox()
        Me.dgvTickCandles = New System.Windows.Forms.DataGridView()
        Me.dgvTarget = New System.Windows.Forms.DataGridView()
        Me.TabControl1 = New System.Windows.Forms.TabControl()
        Me.TabPage1 = New System.Windows.Forms.TabPage()
        Me.TabPage2 = New System.Windows.Forms.TabPage()
        Me.TabPage3 = New System.Windows.Forms.TabPage()
        Me.panelChart = New System.Windows.Forms.Panel()
        Me.FlowLayoutPanel2 = New System.Windows.Forms.FlowLayoutPanel()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.pbProgress = New System.Windows.Forms.ProgressBar()
        Me.lblStatus = New System.Windows.Forms.Label()
        Me.FlowLayoutPanel3 = New System.Windows.Forms.FlowLayoutPanel()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.btnFirst = New System.Windows.Forms.Button()
        Me.btnPrevious = New System.Windows.Forms.Button()
        Me.btnPlayPause = New System.Windows.Forms.Button()
        Me.btnNext = New System.Windows.Forms.Button()
        Me.btnLast = New System.Windows.Forms.Button()
        Me.TrackBar1 = New System.Windows.Forms.TrackBar()
        Me.chkDisplaySignals = New System.Windows.Forms.CheckBox()
        Me.FlowLayoutPanel1.SuspendLayout()
        CType(Me.dgvTickCandles, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.dgvTarget, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.TabControl1.SuspendLayout()
        Me.TabPage1.SuspendLayout()
        Me.TabPage2.SuspendLayout()
        Me.TabPage3.SuspendLayout()
        Me.FlowLayoutPanel2.SuspendLayout()
        Me.FlowLayoutPanel3.SuspendLayout()
        CType(Me.TrackBar1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'FlowLayoutPanel1
        '
        Me.FlowLayoutPanel1.Controls.Add(Me.ComboBox1)
        Me.FlowLayoutPanel1.Controls.Add(Me.cmbTickers)
        Me.FlowLayoutPanel1.Controls.Add(Me.Label1)
        Me.FlowLayoutPanel1.Controls.Add(Me.dtpStart)
        Me.FlowLayoutPanel1.Controls.Add(Me.Button1)
        Me.FlowLayoutPanel1.Controls.Add(Me.Label2)
        Me.FlowLayoutPanel1.Controls.Add(Me.cmbTimeFrame)
        Me.FlowLayoutPanel1.Controls.Add(Me.chkApplyStrategy)
        Me.FlowLayoutPanel1.Controls.Add(Me.chkDisplaySignals)
        Me.FlowLayoutPanel1.Controls.Add(Me.cmbStrategy)
        Me.FlowLayoutPanel1.Controls.Add(Me.btnConvert)
        Me.FlowLayoutPanel1.Controls.Add(Me.btnIndicators)
        Me.FlowLayoutPanel1.Controls.Add(Me.chkSimulation)
        Me.FlowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top
        Me.FlowLayoutPanel1.Location = New System.Drawing.Point(0, 0)
        Me.FlowLayoutPanel1.Name = "FlowLayoutPanel1"
        Me.FlowLayoutPanel1.Size = New System.Drawing.Size(1100, 30)
        Me.FlowLayoutPanel1.TabIndex = 0
        '
        'ComboBox1
        '
        Me.ComboBox1.FormattingEnabled = True
        Me.ComboBox1.Location = New System.Drawing.Point(3, 3)
        Me.ComboBox1.Name = "ComboBox1"
        Me.ComboBox1.Size = New System.Drawing.Size(112, 20)
        Me.ComboBox1.TabIndex = 8
        '
        'cmbTickers
        '
        Me.cmbTickers.FormattingEnabled = True
        Me.cmbTickers.Location = New System.Drawing.Point(121, 3)
        Me.cmbTickers.Name = "cmbTickers"
        Me.cmbTickers.Size = New System.Drawing.Size(62, 20)
        Me.cmbTickers.TabIndex = 9
        '
        'Label1
        '
        Me.Label1.Location = New System.Drawing.Point(189, 3)
        Me.Label1.Margin = New System.Windows.Forms.Padding(3)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(46, 23)
        Me.Label1.TabIndex = 3
        Me.Label1.Text = "시작일"
        Me.Label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'dtpStart
        '
        Me.dtpStart.CustomFormat = "yyyy-MM-dd HH:mm"
        Me.dtpStart.Format = System.Windows.Forms.DateTimePickerFormat.Custom
        Me.dtpStart.Location = New System.Drawing.Point(241, 3)
        Me.dtpStart.Name = "dtpStart"
        Me.dtpStart.ShowUpDown = True
        Me.dtpStart.Size = New System.Drawing.Size(118, 21)
        Me.dtpStart.TabIndex = 2
        '
        'Button1
        '
        Me.Button1.Location = New System.Drawing.Point(365, 3)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(75, 23)
        Me.Button1.TabIndex = 4
        Me.Button1.Text = "Download"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'Label2
        '
        Me.Label2.Location = New System.Drawing.Point(446, 3)
        Me.Label2.Margin = New System.Windows.Forms.Padding(3)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(61, 23)
        Me.Label2.TabIndex = 4
        Me.Label2.Text = "변환캔들"
        Me.Label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'cmbTimeFrame
        '
        Me.cmbTimeFrame.FormattingEnabled = True
        Me.cmbTimeFrame.Location = New System.Drawing.Point(513, 3)
        Me.cmbTimeFrame.Name = "cmbTimeFrame"
        Me.cmbTimeFrame.Size = New System.Drawing.Size(51, 20)
        Me.cmbTimeFrame.TabIndex = 6
        '
        'cmbStrategy
        '
        Me.cmbStrategy.FormattingEnabled = True
        Me.cmbStrategy.Location = New System.Drawing.Point(726, 3)
        Me.cmbStrategy.Name = "cmbStrategy"
        Me.cmbStrategy.Size = New System.Drawing.Size(151, 20)
        Me.cmbStrategy.TabIndex = 12
        '
        'btnConvert
        '
        Me.btnConvert.Location = New System.Drawing.Point(883, 3)
        Me.btnConvert.Name = "btnConvert"
        Me.btnConvert.Size = New System.Drawing.Size(44, 23)
        Me.btnConvert.TabIndex = 7
        Me.btnConvert.Text = "차트"
        Me.btnConvert.UseVisualStyleBackColor = True
        '
        'btnIndicators
        '
        Me.btnIndicators.Location = New System.Drawing.Point(933, 3)
        Me.btnIndicators.Name = "btnIndicators"
        Me.btnIndicators.Size = New System.Drawing.Size(44, 23)
        Me.btnIndicators.TabIndex = 10
        Me.btnIndicators.Text = "지표"
        Me.btnIndicators.UseVisualStyleBackColor = True
        '
        'chkSimulation
        '
        Me.chkSimulation.AutoSize = True
        Me.chkSimulation.Location = New System.Drawing.Point(983, 3)
        Me.chkSimulation.Name = "chkSimulation"
        Me.chkSimulation.Size = New System.Drawing.Size(84, 16)
        Me.chkSimulation.TabIndex = 11
        Me.chkSimulation.Text = "시뮬레이션"
        Me.chkSimulation.UseVisualStyleBackColor = True
        '
        'chkApplyStrategy
        '
        Me.chkApplyStrategy.AutoSize = True
        Me.chkApplyStrategy.Location = New System.Drawing.Point(570, 3)
        Me.chkApplyStrategy.Name = "chkApplyStrategy"
        Me.chkApplyStrategy.Size = New System.Drawing.Size(72, 16)
        Me.chkApplyStrategy.TabIndex = 13
        Me.chkApplyStrategy.Text = "전략적용"
        Me.chkApplyStrategy.UseVisualStyleBackColor = True
        '
        'dgvTickCandles
        '
        Me.dgvTickCandles.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dgvTickCandles.Dock = System.Windows.Forms.DockStyle.Fill
        Me.dgvTickCandles.Location = New System.Drawing.Point(3, 3)
        Me.dgvTickCandles.Name = "dgvTickCandles"
        Me.dgvTickCandles.RowTemplate.Height = 23
        Me.dgvTickCandles.Size = New System.Drawing.Size(1086, 371)
        Me.dgvTickCandles.TabIndex = 1
        '
        'dgvTarget
        '
        Me.dgvTarget.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dgvTarget.Dock = System.Windows.Forms.DockStyle.Fill
        Me.dgvTarget.Location = New System.Drawing.Point(3, 3)
        Me.dgvTarget.Name = "dgvTarget"
        Me.dgvTarget.RowTemplate.Height = 23
        Me.dgvTarget.Size = New System.Drawing.Size(1086, 371)
        Me.dgvTarget.TabIndex = 2
        '
        'TabControl1
        '
        Me.TabControl1.Alignment = System.Windows.Forms.TabAlignment.Bottom
        Me.TabControl1.Controls.Add(Me.TabPage1)
        Me.TabControl1.Controls.Add(Me.TabPage2)
        Me.TabControl1.Controls.Add(Me.TabPage3)
        Me.TabControl1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TabControl1.Location = New System.Drawing.Point(0, 99)
        Me.TabControl1.Name = "TabControl1"
        Me.TabControl1.SelectedIndex = 0
        Me.TabControl1.Size = New System.Drawing.Size(1100, 403)
        Me.TabControl1.TabIndex = 9
        '
        'TabPage1
        '
        Me.TabPage1.Controls.Add(Me.dgvTickCandles)
        Me.TabPage1.Location = New System.Drawing.Point(4, 4)
        Me.TabPage1.Name = "TabPage1"
        Me.TabPage1.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage1.Size = New System.Drawing.Size(1092, 377)
        Me.TabPage1.TabIndex = 0
        Me.TabPage1.Text = "틱데이터"
        Me.TabPage1.UseVisualStyleBackColor = True
        '
        'TabPage2
        '
        Me.TabPage2.Controls.Add(Me.dgvTarget)
        Me.TabPage2.Location = New System.Drawing.Point(4, 4)
        Me.TabPage2.Name = "TabPage2"
        Me.TabPage2.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage2.Size = New System.Drawing.Size(1092, 377)
        Me.TabPage2.TabIndex = 1
        Me.TabPage2.Text = "변환캔들"
        Me.TabPage2.UseVisualStyleBackColor = True
        '
        'TabPage3
        '
        Me.TabPage3.Controls.Add(Me.panelChart)
        Me.TabPage3.Location = New System.Drawing.Point(4, 4)
        Me.TabPage3.Name = "TabPage3"
        Me.TabPage3.Size = New System.Drawing.Size(1092, 377)
        Me.TabPage3.TabIndex = 2
        Me.TabPage3.Text = "차트"
        Me.TabPage3.UseVisualStyleBackColor = True
        '
        'panelChart
        '
        Me.panelChart.BackColor = System.Drawing.Color.Black
        Me.panelChart.Dock = System.Windows.Forms.DockStyle.Fill
        Me.panelChart.Location = New System.Drawing.Point(0, 0)
        Me.panelChart.Name = "panelChart"
        Me.panelChart.Size = New System.Drawing.Size(1092, 377)
        Me.panelChart.TabIndex = 0
        '
        'FlowLayoutPanel2
        '
        Me.FlowLayoutPanel2.Controls.Add(Me.Label3)
        Me.FlowLayoutPanel2.Controls.Add(Me.pbProgress)
        Me.FlowLayoutPanel2.Controls.Add(Me.lblStatus)
        Me.FlowLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Top
        Me.FlowLayoutPanel2.Location = New System.Drawing.Point(0, 30)
        Me.FlowLayoutPanel2.Name = "FlowLayoutPanel2"
        Me.FlowLayoutPanel2.Size = New System.Drawing.Size(1100, 26)
        Me.FlowLayoutPanel2.TabIndex = 10
        '
        'Label3
        '
        Me.Label3.Location = New System.Drawing.Point(3, 0)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(46, 23)
        Me.Label3.TabIndex = 4
        Me.Label3.Text = "  상태: "
        Me.Label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'pbProgress
        '
        Me.pbProgress.Location = New System.Drawing.Point(55, 3)
        Me.pbProgress.Name = "pbProgress"
        Me.pbProgress.Size = New System.Drawing.Size(60, 23)
        Me.pbProgress.TabIndex = 0
        Me.pbProgress.Visible = False
        '
        'lblStatus
        '
        Me.lblStatus.AutoSize = True
        Me.lblStatus.Location = New System.Drawing.Point(121, 5)
        Me.lblStatus.Margin = New System.Windows.Forms.Padding(3, 5, 3, 5)
        Me.lblStatus.Name = "lblStatus"
        Me.lblStatus.Size = New System.Drawing.Size(29, 12)
        Me.lblStatus.TabIndex = 1
        Me.lblStatus.Text = "대기"
        Me.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'FlowLayoutPanel3
        '
        Me.FlowLayoutPanel3.Controls.Add(Me.Label4)
        Me.FlowLayoutPanel3.Controls.Add(Me.btnFirst)
        Me.FlowLayoutPanel3.Controls.Add(Me.btnPrevious)
        Me.FlowLayoutPanel3.Controls.Add(Me.btnPlayPause)
        Me.FlowLayoutPanel3.Controls.Add(Me.btnNext)
        Me.FlowLayoutPanel3.Controls.Add(Me.btnLast)
        Me.FlowLayoutPanel3.Controls.Add(Me.TrackBar1)
        Me.FlowLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Top
        Me.FlowLayoutPanel3.Location = New System.Drawing.Point(0, 56)
        Me.FlowLayoutPanel3.Name = "FlowLayoutPanel3"
        Me.FlowLayoutPanel3.Size = New System.Drawing.Size(1100, 43)
        Me.FlowLayoutPanel3.TabIndex = 11
        Me.FlowLayoutPanel3.Visible = False
        '
        'Label4
        '
        Me.Label4.Location = New System.Drawing.Point(3, 0)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(112, 23)
        Me.Label4.TabIndex = 4
        Me.Label4.Text = "시뮬레이션: "
        Me.Label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'btnFirst
        '
        Me.btnFirst.Location = New System.Drawing.Point(121, 3)
        Me.btnFirst.Name = "btnFirst"
        Me.btnFirst.Size = New System.Drawing.Size(44, 35)
        Me.btnFirst.TabIndex = 8
        Me.btnFirst.Text = "<<"
        Me.btnFirst.UseVisualStyleBackColor = True
        '
        'btnPrevious
        '
        Me.btnPrevious.Location = New System.Drawing.Point(171, 3)
        Me.btnPrevious.Name = "btnPrevious"
        Me.btnPrevious.Size = New System.Drawing.Size(44, 35)
        Me.btnPrevious.TabIndex = 9
        Me.btnPrevious.Text = "<"
        Me.btnPrevious.UseVisualStyleBackColor = True
        '
        'btnPlayPause
        '
        Me.btnPlayPause.BackColor = System.Drawing.Color.SteelBlue
        Me.btnPlayPause.ForeColor = System.Drawing.Color.White
        Me.btnPlayPause.Location = New System.Drawing.Point(221, 3)
        Me.btnPlayPause.Name = "btnPlayPause"
        Me.btnPlayPause.Size = New System.Drawing.Size(44, 35)
        Me.btnPlayPause.TabIndex = 10
        Me.btnPlayPause.Text = "▶"
        Me.btnPlayPause.UseVisualStyleBackColor = False
        '
        'btnNext
        '
        Me.btnNext.Location = New System.Drawing.Point(271, 3)
        Me.btnNext.Name = "btnNext"
        Me.btnNext.Size = New System.Drawing.Size(44, 35)
        Me.btnNext.TabIndex = 11
        Me.btnNext.Text = ">"
        Me.btnNext.UseVisualStyleBackColor = True
        '
        'btnLast
        '
        Me.btnLast.Location = New System.Drawing.Point(321, 3)
        Me.btnLast.Name = "btnLast"
        Me.btnLast.Size = New System.Drawing.Size(44, 35)
        Me.btnLast.TabIndex = 12
        Me.btnLast.Text = ">>"
        Me.btnLast.UseVisualStyleBackColor = True
        '
        'TrackBar1
        '
        Me.TrackBar1.Location = New System.Drawing.Point(371, 3)
        Me.TrackBar1.Name = "TrackBar1"
        Me.TrackBar1.Size = New System.Drawing.Size(266, 45)
        Me.TrackBar1.TabIndex = 13
        '
        'chkDisplaySignals
        '
        Me.chkDisplaySignals.AutoSize = True
        Me.chkDisplaySignals.Location = New System.Drawing.Point(648, 3)
        Me.chkDisplaySignals.Name = "chkDisplaySignals"
        Me.chkDisplaySignals.Size = New System.Drawing.Size(72, 16)
        Me.chkDisplaySignals.TabIndex = 14
        Me.chkDisplaySignals.Text = "신호표시"
        Me.chkDisplaySignals.UseVisualStyleBackColor = True
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 12.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1100, 502)
        Me.Controls.Add(Me.TabControl1)
        Me.Controls.Add(Me.FlowLayoutPanel3)
        Me.Controls.Add(Me.FlowLayoutPanel2)
        Me.Controls.Add(Me.FlowLayoutPanel1)
        Me.Name = "Form1"
        Me.Text = "Form1"
        Me.FlowLayoutPanel1.ResumeLayout(False)
        Me.FlowLayoutPanel1.PerformLayout()
        CType(Me.dgvTickCandles, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.dgvTarget, System.ComponentModel.ISupportInitialize).EndInit()
        Me.TabControl1.ResumeLayout(False)
        Me.TabPage1.ResumeLayout(False)
        Me.TabPage2.ResumeLayout(False)
        Me.TabPage3.ResumeLayout(False)
        Me.FlowLayoutPanel2.ResumeLayout(False)
        Me.FlowLayoutPanel2.PerformLayout()
        Me.FlowLayoutPanel3.ResumeLayout(False)
        Me.FlowLayoutPanel3.PerformLayout()
        CType(Me.TrackBar1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents FlowLayoutPanel1 As FlowLayoutPanel
    Friend WithEvents Label1 As Label
    Friend WithEvents dtpStart As DateTimePicker
    Friend WithEvents Button1 As Button
    Friend WithEvents dgvTickCandles As DataGridView
    Friend WithEvents dgvTarget As DataGridView
    Friend WithEvents Label2 As Label
    Friend WithEvents cmbTimeFrame As ComboBox
    Friend WithEvents btnConvert As Button
    Friend WithEvents TabControl1 As TabControl
    Friend WithEvents TabPage1 As TabPage
    Friend WithEvents TabPage2 As TabPage
    Friend WithEvents TabPage3 As TabPage
    Friend WithEvents ComboBox1 As ComboBox
    Friend WithEvents FlowLayoutPanel2 As FlowLayoutPanel
    Friend WithEvents pbProgress As ProgressBar
    Friend WithEvents lblStatus As Label
    Friend WithEvents cmbTickers As ComboBox
    Friend WithEvents Label3 As Label
    Friend WithEvents panelChart As Panel
    Friend WithEvents btnIndicators As Button
    Friend WithEvents chkSimulation As CheckBox
    Friend WithEvents FlowLayoutPanel3 As FlowLayoutPanel
    Friend WithEvents Label4 As Label
    Friend WithEvents btnFirst As Button
    Friend WithEvents btnPrevious As Button
    Friend WithEvents btnPlayPause As Button
    Friend WithEvents btnNext As Button
    Friend WithEvents btnLast As Button
    Friend WithEvents TrackBar1 As TrackBar
    Friend WithEvents cmbStrategy As ComboBox
    Friend WithEvents chkApplyStrategy As CheckBox
    Friend WithEvents chkDisplaySignals As CheckBox
End Class
