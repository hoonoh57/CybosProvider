Imports CPSYSDIBLib
Imports CPUTILLib
Imports DSCBO1Lib

Public Class CybosDataProvider
    Private objCpCodeMgr As CpCodeMgr
    Private objCpTick As StockChart


    Public Sub New()
        objCpCodeMgr = New CpCodeMgr()
        objCpTick = New StockChart()
    End Sub

    ''' <summary>
    ''' 코스피, 코스닥 전체 종목코드를 리스트로 반환
    ''' </summary>
    Public Function GetStockCodeList() As List(Of String)
        Dim codeList As New List(Of String)

        ' KOSPI
        Dim kospiCodes = objCpCodeMgr.GetStockListByMarket(CPE_MARKET_KIND.CPC_MARKET_KOSPI)
        For Each code In kospiCodes
            codeList.Add(code.ToString())
        Next

        ' KOSDAQ
        Dim kosdaqCodes = objCpCodeMgr.GetStockListByMarket(CPE_MARKET_KIND.CPC_MARKET_KOSDAQ)
        For Each code In kosdaqCodes
            codeList.Add(code.ToString())
        Next

        Return codeList
    End Function

    ''' <summary>
    ''' OHLCV(일/주/월) 데이터 요청
    ''' </summary>
    Public Function GetChartData(code As String, freq As Char, count As Integer) As List(Of CandleInfo)
        Dim dataList As New List(Of CandleInfo)
        Dim objStockChart As New StockChart()

        objStockChart.SetInputValue(0, code)
        objStockChart.SetInputValue(1, Asc("2"c)) ' 요청 구분: 2(개수)
        objStockChart.SetInputValue(4, count) ' 요청 개수
        objStockChart.SetInputValue(5, {0, 2, 3, 4, 5, 8}) ' 요청 필드: 날짜, 시, 고, 저, 종, 거래량
        objStockChart.SetInputValue(6, Asc(freq)) ' 차트 구분: D(일), W(주), M(월)
        objStockChart.SetInputValue(9, Asc("1"c)) ' 수정주가 구분: 1(적용)

        objStockChart.BlockRequest()

        Dim numData As Integer = objStockChart.GetHeaderValue(3)

        For i As Integer = 0 To numData - 1
            Dim candle As New CandleInfo()
            candle.Timestamp = ParseDateTime(objStockChart.GetDataValue(0, i), 0)
            candle.Open = Convert.ToDouble(objStockChart.GetDataValue(1, i))
            candle.High = Convert.ToDouble(objStockChart.GetDataValue(2, i))
            candle.Low = Convert.ToDouble(objStockChart.GetDataValue(3, i))
            candle.Close = Convert.ToDouble(objStockChart.GetDataValue(4, i))
            candle.Volume = Convert.ToDouble(objStockChart.GetDataValue(5, i))
            dataList.Add(candle)
        Next

        Return dataList.OrderBy(Function(c) c.Timestamp).ToList()
    End Function

    ''' <summary>
    ''' 외국인 순매수 데이터 요청
    ''' </summary>
    Public Function GetForeignerNetBuyData(code As String, count As Integer) As List(Of ForeignerData)
        Dim dataList As New List(Of ForeignerData)
        Dim objFrg As New CpSvr7254()

        objFrg.SetInputValue(0, code)
        objFrg.SetInputValue(1, count) ' 요청개수
        objFrg.SetInputValue(2, "1"c)  ' 구분: 1(외국인)
        objFrg.SetInputValue(3, "1"c)  ' 단위: 1(순매수량)

        objFrg.BlockRequest()

        Dim numData As Integer = objFrg.GetHeaderValue(0)

        For i As Integer = 0 To numData - 1
            Dim fData As New ForeignerData()
            fData.TradeDate = ParseDateTime(objFrg.GetDataValue(0, i), 0) ' 일자
            fData.NetBuyVolume = objFrg.GetDataValue(2, i) ' 외국인 순매수량
            dataList.Add(fData)
        Next

        Return dataList.OrderBy(Function(d) d.TradeDate).ToList()
    End Function

    ''' <summary>
    ''' 지정 일자/시간부터 현재까지 1틱 캔들 데이터 다운로드
    ''' </summary>
    ''' <param name="stockCode">종목코드 (예: "A005930", "005930", "U001", "J517016")</param>
    ''' <param name="startDateTime">시작일시 (이 시간 이후의 데이터만 수집)</param>
    ''' <returns>캔들 데이터 리스트 (Timestamp, Open, High, Low, Close, Volume)</returns>
    Public Async Function DownloadTickCandles(stockCode As String, startDateTime As DateTime) As Task(Of List(Of Candle))

        Dim candleList As New List(Of Candle)
        Dim code As String = If(Not stockCode.StartsWith("A") AndAlso Not stockCode.StartsWith("U") AndAlso Not stockCode.StartsWith("J"), "A" & stockCode, stockCode)

        Try
            Dim totalRequested As Long = 0
            Dim totalReceived As Long = 0
            Dim requestCount As Integer = 0
            Dim maxRequests As Integer = 5000  ' 최대 요청 횟수
            Dim requestSize As Integer = 5000   ' 한 번에 요청할 개수

            Console.WriteLine($"종목코드: {code}")
            Console.WriteLine($"시작일시: {startDateTime:yyyy-MM-dd HH:mm:ss}")
            Console.WriteLine($"종료일시: 현재 (최근 거래일)")
            Console.WriteLine($"데이터 다운로드 시작...")
            Console.WriteLine()

            ' 첫 번째 요청 설정 - 틱/분은 반드시 개수 요청('2') 사용
            objCpTick.SetInputValue(0, code)                   ' 종목코드
            objCpTick.SetInputValue(1, Asc("2"c))              ' '2' = 개수 요청 (틱은 개수로만 가능)
            objCpTick.SetInputValue(4, requestSize)            ' 요청 개수
            objCpTick.SetInputValue(5, {0, 1, 2, 3, 4, 5, 8})  ' 필드: 날짜, 시간(HHmm), 시/고/저/종, 거래량
            objCpTick.SetInputValue(6, Asc("T"c))              ' 'T' = 틱봉
            objCpTick.SetInputValue(7, 1)                      ' 주기: 1틱
            objCpTick.SetInputValue(9, Asc("1"c))              ' '1' = 수정주가 적용

            Dim shouldContinue As Boolean = True
            Dim reachedStartTime As Boolean = False

            Do While shouldContinue
                ' API 요청
                objCpTick.BlockRequest()

                ' 응답 상태 확인
                Dim rqStatus As Integer = objCpTick.GetDibStatus()
                If rqStatus <> 0 Then
                    Console.WriteLine($"요청 오류 (코드: {rqStatus}): {objCpTick.GetDibMsg1()}")
                    Exit Do
                End If

                ' 수신 데이터 개수
                Dim numData As Integer = objCpTick.GetHeaderValue(3)

                If numData = 0 Then
                    Console.WriteLine("더 이상 데이터가 없습니다.")
                    Exit Do
                End If

                totalRequested += requestSize
                totalReceived += numData
                requestCount += 1

                ' 데이터 추출
                Dim oldestDateTime As DateTime = DateTime.MaxValue
                Dim newestDateTime As DateTime = DateTime.MinValue
                Dim addedCount As Integer = 0

                For i As Integer = 0 To numData - 1
                    ' 날짜와 시간 읽기
                    Dim tradeDate As Integer = Convert.ToInt32(objCpTick.GetDataValue(0, i))  'yyyyMMdd
                    Dim tradeTime As Integer = Convert.ToInt32(objCpTick.GetDataValue(1, i))  'HHmm

                    ' DateTime으로 변환
                    Dim timestamp As DateTime = ParseDateTime(tradeDate, tradeTime)

                    ' 가장 최근/오래된 데이터 추적
                    If i = 0 Then newestDateTime = timestamp
                    If i = numData - 1 Then oldestDateTime = timestamp

                    ' *** 핵심 로직: 지정한 시작일시보다 작은 데이터가 나오면 플래그 설정 ***
                    If timestamp < startDateTime Then
                        reachedStartTime = True
                        ' 시작일시 이전 데이터는 추가하지 않음
                        Continue For
                    End If

                    ' 시작 일시 이후의 데이터만 추가
                    Dim candle As New Candle()
                    candle.Timestamp = timestamp
                    candle.Open = Convert.ToDouble(objCpTick.GetDataValue(2, i))
                    candle.High = Convert.ToDouble(objCpTick.GetDataValue(3, i))
                    candle.Low = Convert.ToDouble(objCpTick.GetDataValue(4, i))
                    candle.Close = Convert.ToDouble(objCpTick.GetDataValue(5, i))
                    candle.Volume = Convert.ToDouble(objCpTick.GetDataValue(6, i))

                    candleList.Add(candle)
                    addedCount += 1
                Next

                'Logger.Instance.log($"요청 {requestCount}: {numData}개 수신, {addedCount}개 추가 (총: {candleList.Count}개)")
                Logger.Instance.log($"  최근: {newestDateTime:yyyy-MM-dd HH:mm:ss} | 가장 오래된: {oldestDateTime:yyyy-MM-dd HH:mm:ss}")

                ' *** 시작일시보다 이전 데이터가 나타났으면 루프 종료 ***
                If reachedStartTime Then
                    Logger.Instance.log($"시작 일시({startDateTime:yyyy-MM-dd HH:mm:ss})보다 이전 데이터에 도달했습니다. 다운로드를 종료합니다.", Warning.WarningInfo)
                    Exit Do
                End If

                ' Continue 여부 확인
                Dim bContinue As Boolean = objCpTick.Continue

                If Not bContinue Then
                    Logger.Instance.log("모든 데이터 수신 완료 (Continue = False)")
                    Exit Do
                End If

                ' 최대 요청 횟수 체크
                If requestCount >= maxRequests Then
                    Logger.Instance.log($"최대 요청 횟수({maxRequests})에 도달했습니다.")
                    Exit Do
                End If

                ' API 요청 제한 준수 (15초당 15회)
                Await Task.Delay(20)

            Loop

            ' 데이터를 시간순으로 정렬 (과거→현재)
            candleList = candleList.OrderBy(Function(c) c.Timestamp).ToList()

            Console.WriteLine()
            Console.WriteLine($"=== 다운로드 완료 ===")
            Console.WriteLine($"총 요청 횟수: {requestCount}회")
            Console.WriteLine($"총 수신 데이터: {totalReceived}개")
            Console.WriteLine($"필터링 후 데이터: {candleList.Count}개")

            If candleList.Count > 0 Then
                Dim firstCandle = candleList.First()
                Dim lastCandle = candleList.Last()
                Console.WriteLine($"기간: {firstCandle.Timestamp:yyyy-MM-dd HH:mm:ss} ~ {lastCandle.Timestamp:yyyy-MM-dd HH:mm:ss}")
            End If

        Catch ex As Exception
            Logger.Instance.log(ex.Message, Warning.ErrorInfo)
            Console.WriteLine(ex.StackTrace)
        End Try

        Logger.Instance.log($"총 건수({candleList.Count}) 건 수신을 완료했습니다.")
        Return candleList
    End Function

    ''' <summary>
    ''' 날짜(YYYYMMDD)와 시간(HHmm)을 DateTime으로 변환
    ''' </summary>
    Private Function ParseDateTime(tradeDate As Integer, tradeTime As Integer) As DateTime
        Try
            Dim dateStr As String = tradeDate.ToString("D8")
            Dim timeStr As String = tradeTime.ToString("D4")

            Dim year As Integer = Convert.ToInt32(dateStr.Substring(0, 4))
            Dim month As Integer = Convert.ToInt32(dateStr.Substring(4, 2))
            Dim day As Integer = Convert.ToInt32(dateStr.Substring(6, 2))

            Dim hour As Integer = Convert.ToInt32(timeStr.Substring(0, 2))
            Dim minute As Integer = Convert.ToInt32(timeStr.Substring(2, 2))
            Dim second As Integer = 0

            Return New DateTime(year, month, day, hour, minute, second)
        Catch ex As Exception
            Console.WriteLine($"날짜 변환 오류: Date={tradeDate}, Time={tradeTime}")
            Return DateTime.MinValue
        End Try
    End Function

    ''' <summary>
    ''' 다운로드한 데이터를 CSV 파일로 저장
    ''' </summary>
    Public Sub SaveToCSV(candleList As List(Of Candle), filePath As String)
        Try
            Using writer As New System.IO.StreamWriter(filePath, False, System.Text.Encoding.UTF8)
                ' 헤더 작성
                writer.WriteLine("Timestamp,Open,High,Low,Close,Volume")

                ' 데이터 작성
                For Each candle As Candle In candleList
                    writer.WriteLine($"{candle.Timestamp:yyyy-MM-dd HH:mm:ss},{candle.Open},{candle.High},{candle.Low},{candle.Close},{candle.Volume}")
                Next
            End Using

            Console.WriteLine($"CSV 파일 저장 완료: {filePath}")
        Catch ex As Exception
            Console.WriteLine($"파일 저장 오류: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' 헤더 정보 출력
    ''' </summary>
    Public Sub PrintHeaderInfo()
        Try
            Console.WriteLine()
            Console.WriteLine("=== 헤더 정보 ===")
            Console.WriteLine($"종목코드: {objCpTick.GetHeaderValue(0)}")
            Console.WriteLine($"필드개수: {objCpTick.GetHeaderValue(1)}")
            Console.WriteLine($"수신개수: {objCpTick.GetHeaderValue(3)}")
            Console.WriteLine($"최근거래일: {objCpTick.GetHeaderValue(5)}")
            Console.WriteLine($"전일종가: {objCpTick.GetHeaderValue(6)}")
            Console.WriteLine($"현재가: {objCpTick.GetHeaderValue(7)}")
            Console.WriteLine($"거래량: {objCpTick.GetHeaderValue(10)}")
            Console.WriteLine()
        Catch ex As Exception
            Console.WriteLine($"헤더 정보 출력 오류: {ex.Message}")
        End Try
    End Sub
End Class

' 캔들 데이터 구조체
Public Class Candle
    Public Property Timestamp As DateTime
    Public Property Open As Double
    Public Property High As Double
    Public Property Low As Double
    Public Property Close As Double
    Public Property Volume As Double

    Public Sub New()
    End Sub

    Public Sub New(timestamp As DateTime, open As Double, high As Double, low As Double, close As Double, volume As Double)
        Me.Timestamp = timestamp
        Me.Open = open
        Me.High = high
        Me.Low = low
        Me.Close = close
        Me.Volume = volume
    End Sub

    Public Overrides Function ToString() As String
        Return $"{Timestamp:yyyy-MM-dd HH:mm:ss} | O:{Open} H:{High} L:{Low} C:{Close} V:{Volume}"
    End Function
End Class

' 외국인 매매 데이터 구조체
Public Class ForeignerData
    Public Property TradeDate As DateTime
    Public Property NetBuyVolume As Double
End Class

Public Class CandleInfo
    Inherits Candle

    ' 기본 정보
    Public Property timeframe As String
    Public Property tickCount As Integer   ' 해당 캔들의 틱 개수

    ' 캔들 패턴 정보
    Public Property bodySize As Double     ' 몸통 크기 (Close - Open)
    Public Property upperShadow As Double  ' 위꼬리 (High - Max(Open, Close))
    Public Property lowerShadow As Double  ' 아래꼬리 (Min(Open, Close) - Low)
    Public Property totalRange As Double   ' 전체 범위 (High - Low)
    Public Property bodyRatio As Double    ' 몸통 비율 (bodySize / totalRange)
    Public Property isGreen As Boolean     ' 양봉 여부
    Public Property isRed As Boolean       ' 음봉 여부
    Public Property isDoji As Boolean      ' 도지 여부
    Public Property changePercent As Double ' 변화율 (%)

    ' tickCount 이동평균
    Public Property tickAvg5 As Integer    ' 5 이동평균
    Public Property tickAvg20 As Integer   ' 20 이동평균
    Public Property tickAvg60 As Integer   ' 60 이동평균

    ' 가격 이동평균 (SMA)
    Public Property sma5 As Double         ' 5일 단순이동평균
    Public Property sma20 As Double        ' 20일 단순이동평균
    Public Property sma60 As Double        ' 60일 단순이동평균
    Public Property sma120 As Double       ' 120일 단순이동평균

    ' 지수이동평균 (EMA)
    Public Property ema5 As Double         ' 5일 지수이동평균
    Public Property ema20 As Double        ' 20일 지수이동평균

    ' 볼린저 밴드
    Public Property bollingerUpper As Double   ' 상단 밴드
    Public Property bollingerMiddle As Double  ' 중간 밴드 (20 SMA)
    Public Property bollingerLower As Double   ' 하단 밴드
    Public Property bollingerWidth As Double   ' 밴드 폭

    ' RSI
    Public Property rsi As Double          ' RSI (14)
    Public Property rsiDivergence As Boolean ' has rsi divergence 

    ' MACD
    Public Property macd As Double         ' MACD (12, 26)
    Public Property macdSignal As Double   ' Signal Line (9)
    Public Property macdHistogram As Double ' Histogram

    ' 거래량 정보
    Public Property volumeAvg5 As Double   ' 5일 평균 거래량
    Public Property volumeAvg20 As Double  ' 20일 평균 거래량
    Public Property volumeRatio As Double  ' 거래량 비율 (Volume / volumeAvg20)

    ' 지지/저항 정보
    Public Property distanceFromHigh As Double  ' 당일고가로부터 거리 (%)
    Public Property distanceFromLow As Double   ' 당일저가로부터 거리 (%)
    Public Property distanceFromVWAP As Double   ' 당일vwap으로부터 거리 (%)

    Public Sub New()
        MyBase.New()
    End Sub

    ''' <summary>
    ''' 캔들의 기본 정보 계산
    ''' </summary>
    Public Sub CalculateCandleInfo()
        ' 몸통 크기
        bodySize = Math.Abs(Close - Open)

        ' 위/아래 꼬리
        Dim maxBody = Math.Max(Open, Close)
        Dim minBody = Math.Min(Open, Close)
        upperShadow = High - maxBody
        lowerShadow = minBody - Low

        ' 전체 범위
        totalRange = High - Low

        ' 몸통 비율
        If totalRange > 0 Then
            bodyRatio = bodySize / totalRange
        End If

        ' 양봉/음봉
        isGreen = Close > Open
        isRed = Close < Open

        ' 도지 (몸통이 전체 범위의 5% 이하)
        isDoji = bodyRatio <= 0.05

        ' 변화율
        If Open > 0 Then
            'changePercent = ((Close - Open) / Open - 1) * 100
            changePercent = ((Close / Open) - 1) * 100

        End If

        ' 고가/저가로부터 거리
        If High > 0 Then
            distanceFromHigh = ((High - Close) / High) * 100
        End If

        If Low > 0 Then
            distanceFromLow = ((Close - Low) / Low) * 100
        End If
    End Sub

    ''' <summary>
    ''' 캔들 패턴 문자열 표현
    ''' </summary>
    Public Function GetCandlePattern() As String
        If isDoji Then Return "도지"
        If isGreen AndAlso bodyRatio > 0.7 Then Return "강한양봉"
        If isRed AndAlso bodyRatio > 0.7 Then Return "강한음봉"
        If upperShadow > bodySize * 2 Then Return "위꼬리긴캔들"
        If lowerShadow > bodySize * 2 Then Return "아래꼬리긴캔들"
        If isGreen Then Return "양봉"
        If isRed Then Return "음봉"
        Return "보합"
    End Function

    Public Overrides Function ToString() As String
        Return $"{Timestamp:yyyy-MM-dd HH:mm} [{timeframe}] O:{Open} H:{High} L:{Low} C:{Close} V:{Volume} T:{tickCount}"
    End Function
End Class