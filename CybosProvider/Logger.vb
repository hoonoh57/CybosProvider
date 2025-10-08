Public Class Logger

    Public Event logEvent(smsg As String)
    Public Event progressStatus(value As Integer)
    Private Shared _instance As Logger

    Public Shared ReadOnly Property Instance() As Logger
        Get
            If _instance Is Nothing Then
                If _instance Is Nothing Then
                    _instance = New Logger()
                End If
            End If
            Return _instance
        End Get
    End Property

    Public Sub log(smsg As String, Optional warning As Warning = Warning.DebugInfo)
        Dim warnLevel As String = ""
        Select Case warning
            Case Warning.DebugInfo
                warnLevel = "[정상]"
            Case Warning.ErrorInfo
                warnLevel = "[오류]"
            Case Warning.WarningInfo
                warnLevel = "[경고]"
            Case Else
        End Select
        smsg = $"[{Now.ToString("HH:mm:ss")}] {warnLevel} {smsg}"
        RaiseEvent logEvent(smsg)
    End Sub

    Public Sub progress(value As Integer)
        RaiseEvent progressStatus(value)
    End Sub
End Class
Public Enum Warning
    DebugInfo
    ErrorInfo
    WarningInfo
End Enum
