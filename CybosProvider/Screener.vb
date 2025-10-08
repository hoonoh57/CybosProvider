
Imports System.Collections.Generic
Imports System.Linq

Public Class Screener
    Private ReadOnly _dataProvider As CybosDataProvider

    Public Sub New(dataProvider As CybosDataProvider)
        _dataProvider = dataProvider
    End Sub

    Public Function Run() As List(Of String)
        Dim matchedStocks As New List(Of String)
        Dim allStocks = _dataProvider.GetStockCodeList()

        Console.WriteLine($"총 {allStocks.Count}개 종목 검색 시작...")

        Dim count = 0
        For Each code In allStocks
            count += 1
            Console.WriteLine($"({count}/{allStocks.Count}) {code} 검색 중...")

            Try
                ' 3가지 조건을 모두 만족하는지 확인
                If CheckRsiCondition(code) AndAlso CheckForeignerCondition(code) AndAlso CheckVolumeCondition(code) Then
                    matchedStocks.Add(code)
                    Console.WriteLine($"########## 종목 포착: {code} ##########")
                End If
            Catch ex As Exception
                Console.WriteLine($"오류 발생: {code} - {ex.Message}")
            End Try
        Next

        Console.WriteLine($"검색 완료. 총 {matchedStocks.Count}개 종목 포착.")
        Return matchedStocks
    End Function

    ''' <summary>
    ''' 조건 1: 월봉 RSI가 30을 상향 돌파
    ''' </summary>
    Private Function CheckRsiCondition(code As String) As Boolean
        ' RSI 계산을 위해 최소 15개월 데이터 필요
        Dim monthlyData = _dataProvider.GetChartData(code, "M"c, 15)
        If monthlyData.Count < 15 Then Return False

        Dim closePrices = monthlyData.Select(Function(c) c.Close).ToList()
        Dim rsiValues = CalculateRSI(closePrices, 14)

        If rsiValues.Count < 2 Then Return False

        Dim lastRsi = rsiValues(rsiValues.Count - 1)
        Dim prevRsi = rsiValues(rsiValues.Count - 2)

        If lastRsi.HasValue AndAlso prevRsi.HasValue Then
            If lastRsi > 30 AndAlso prevRsi <= 30 Then
                Return True
            End If
        End If

        Return False
    End Function

    ''' <summary>
    ''' 조건 2: 외국인 3주 연속 순매수
    ''' </summary>
    Private Function CheckForeignerCondition(code As String) As Boolean
        ' 3주 = 15 거래일, 여유있게 20일 데이터 요청
        Dim dailyFrgData = _dataProvider.GetForeignerNetBuyData(code, 20)
        If dailyFrgData.Count < 15 Then Return False

        ' 주간 데이터로 집계
        Dim weeklyNetBuy = dailyFrgData _
            .GroupBy(Function(d) Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(d.TradeDate, Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday)) _
            .Select(Function(g) New With { .Week = g.Key, .NetBuy = g.Sum(Function(d) d.NetBuyVolume) }) _
            .OrderByDescending(Function(w) w.Week) _
            .Take(3) _
            .ToList()

        If weeklyNetBuy.Count < 3 Then Return False

        ' 3주 연속 순매수 확인
        Return weeklyNetBuy.All(Function(w) w.NetBuy > 0)
    End Function

    ''' <summary>
    ''' 조건 3: 주봉 거래량이 20주 평균의 2배 이상
    ''' </summary>
    Private Function CheckVolumeCondition(code As String) As Boolean
        ' 20주 평균 계산을 위해 최소 21주 데이터 필요
        Dim weeklyData = _dataProvider.GetChartData(code, "W"c, 21)
        If weeklyData.Count < 21 Then Return False

        Dim lastVolume = weeklyData.Last().Volume
        Dim avgVolume = weeklyData.Take(20).Average(Function(c) c.Volume)

        If avgVolume > 0 AndAlso lastVolume > avgVolume * 2 Then
            Return True
        End If

        Return False
    End Function

    ''' <summary>
    ''' RSI 계산 로직
    ''' </summary>
    Private Function CalculateRSI(closePrices As List(Of Double), period As Integer) As List(Of Double?)
        Dim rsiValues As New List(Of Double?)
        If closePrices.Count <= period Then Return rsiValues

        Dim gains As Double = 0
        Dim losses As Double = 0

        ' 첫 번째 RSI 값 계산
        For i = 1 To period
            Dim change = closePrices(i) - closePrices(i - 1)
            If change > 0 Then
                gains += change
            Else
                losses += Math.Abs(change)
            End If
        Next

        Dim avgGain = gains / period
        Dim avgLoss = losses / period

        For i = 0 To period - 1
            rsiValues.Add(Nothing)
        Next

        If avgLoss = 0 Then
            rsiValues.Add(100)
        Else
            Dim rs = avgGain / avgLoss
            rsiValues.Add(100 - (100 / (1 + rs)))
        End If

        ' 이후 RSI 값 계산 (EMA 방식)
        For i = period + 1 To closePrices.Count - 1
            Dim change = closePrices(i) - closePrices(i - 1)
            Dim currentGain = If(change > 0, change, 0)
            Dim currentLoss = If(change < 0, Math.Abs(change), 0)

            avgGain = (avgGain * (period - 1) + currentGain) / period
            avgLoss = (avgLoss * (period - 1) + currentLoss) / period

            If avgLoss = 0 Then
                rsiValues.Add(100)
            Else
                Dim rs = avgGain / avgLoss
                rsiValues.Add(100 - (100 / (1 + rs)))
            End If
        Next

        Return rsiValues
    End Function

End Class
