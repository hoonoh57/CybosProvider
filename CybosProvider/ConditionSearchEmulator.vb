Option Strict On
Option Explicit On
Imports System
Imports System.Collections.Generic

' Snapshot of condition search results (time -> codes)
Public Class ConditionSnapshot
    Public Property Timestamp As DateTime
    Public Property Codes As List(Of String)
End Class

Public Class ConditionSearchEmulator
    Private ReadOnly _snapshots As New List(Of ConditionSnapshot)

    Public Sub AddSnapshot(ts As DateTime, codes As IEnumerable(Of String))
        _snapshots.Add(New ConditionSnapshot With {.Timestamp = ts, .Codes = New List(Of String)(codes)})
    End Sub

    Public Function GetSnapshot(ts As DateTime) As ConditionSnapshot
        For Each s In _snapshots
            If s.Timestamp = ts Then Return s
        Next
        Return Nothing
    End Function
End Class
