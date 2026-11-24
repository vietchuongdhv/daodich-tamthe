using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TidsPreWinAnalysis
{
    public class Record
    {
        public int Id;
        public string Date;
        public string DayOfWeek;
        public string Gdb;
        public int[] Digits5; // d5, d4, d3, d2, d1
        public int State3D;   // d3*100 + d2*10 + d1
        public int State4D;   // d4*1000 + d3*100 + d2*10 + d1
    }

    public class WinEvent
    {
        public int PeriodIndex;
        public string Date;
        public string DayOfWeek;
        public int Actual;
        public string ActualStr;
        public int GapFromPrevWin; // Khoảng cách từ lần trúng trước
        public double AvgEnergyTop; // Thế năng trung bình của các số được chọn
        public double EnergyGap;    // Độ dốc thế năng (V_mean - V_top)
        public double Entropy3D;    // Entropy của không gian
        public int NumDigitsInGoldenGan; // Số chữ số nằm trong dải Gan Vàng
        public int NumDigitsDropped;     // Số chữ số rơi từ kỳ trước
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            string csvPath = @"data\lottery_master.csv";
            var records = LoadData(csvPath);
            int n = records.Count;
            int testPeriods = 1000;
            int startK = n - testPeriods;

            Console.WriteLine("=========================================================================================");
            Console.WriteLine("   RÀ SOÁT HIỆN TƯỢNG VÀ TRẠNG THÁI THỊ TRƯỜNG TRƯỚC TẤT CẢ CÁC KỲ TRÚNG 3D & 4D");
            Console.WriteLine("   Mẫu nghiên cứu: {0} kỳ gần nhất (từ {1} đến {2})", testPeriods, records[startK].Date, records[n-1].Date);
            Console.WriteLine("=========================================================================================\n");

            // Phân tích cho Dàn 3D 27 số (3x3x3)
            var wins3D_27 = new List<WinEvent>();
            var wins4D_81 = new List<WinEvent>();
            var wins4D_36 = new List<WinEvent>();

            int lastWin3D_27 = -1;
            int lastWin4D_81 = -1;
            int lastWin4D_36 = -1;

            // Mảng lưu trạng thái hàng ngày để so sánh giữa ngày TRÚNG vs ngày XỊT
            var allDays3D = new List<WinEvent>();

            for (int k = startK; k < n; k++)
            {
                var past = records.Take(k).ToList();
                var actual = records[k];
                int actD4 = actual.Digits5[1];
                int actD3 = actual.Digits5[2];
                int actD2 = actual.Digits5[3];
                int actD1 = actual.Digits5[4];

                // 1. Tính thế năng 1D từng trục
                double[] vD4 = ComputeDigitPotential(past, 1, actual.DayOfWeek);
                double[] vD3 = ComputeDigitPotential(past, 2, actual.DayOfWeek);
                double[] vD2 = ComputeDigitPotential(past, 3, actual.DayOfWeek);
                double[] vD1 = ComputeDigitPotential(past, 4, actual.DayOfWeek);

                var sortD4 = Enumerable.Range(0, 10).OrderBy(d => vD4[d]).ToArray();
                var sortD3 = Enumerable.Range(0, 10).OrderBy(d => vD3[d]).ToArray();
                var sortD2 = Enumerable.Range(0, 10).OrderBy(d => vD2[d]).ToArray();
                var sortD1 = Enumerable.Range(0, 10).OrderBy(d => vD1[d]).ToArray();

                // Tính toán các chỉ số vật lý/toán học của ngày k
                // a) Độ sâu thế năng (Energy Depth / Gap)
                double avgV3_Top3 = sortD3.Take(3).Average(d => vD3[d]);
                double avgV2_Top3 = sortD2.Take(3).Average(d => vD2[d]);
                double avgV1_Top3 = sortD1.Take(3).Average(d => vD1[d]);
                double meanAll3D = (vD3.Average() + vD2.Average() + vD1.Average());
                double energyGap3D = meanAll3D - (avgV3_Top3 + avgV2_Top3 + avgV1_Top3);

                // b) Gan vàng của các trục
                int goldenCount3D = 0;
                goldenCount3D += CheckGoldenGan(past, 2, sortD3.Take(3).ToArray());
                goldenCount3D += CheckGoldenGan(past, 3, sortD2.Take(3).ToArray());
                goldenCount3D += CheckGoldenGan(past, 4, sortD1.Take(3).ToArray());

                // c) Số chữ số rơi từ hôm qua
                int dropCount3D = 0;
                var yesterday = past[k - 1];
                if (sortD3.Take(3).Contains(yesterday.Digits5[2])) dropCount3D++;
                if (sortD2.Take(3).Contains(yesterday.Digits5[3])) dropCount3D++;
                if (sortD1.Take(3).Contains(yesterday.Digits5[4])) dropCount3D++;

                var dayEv = new WinEvent
                {
                    PeriodIndex = k,
                    Date = actual.Date,
                    DayOfWeek = actual.DayOfWeek,
                    Actual = actual.State3D,
                    ActualStr = string.Format("{0:D3}", actual.State3D),
                    EnergyGap = energyGap3D,
                    NumDigitsInGoldenGan = goldenCount3D,
                    NumDigitsDropped = dropCount3D
                };
                allDays3D.Add(dayEv);

                // Kiểm tra trúng 3D 27 số
                bool hit3D = sortD3.Take(3).Contains(actD3) && sortD2.Take(3).Contains(actD2) && sortD1.Take(3).Contains(actD1);
                if (hit3D)
                {
                    dayEv.GapFromPrevWin = (lastWin3D_27 == -1) ? 0 : (k - lastWin3D_27);
                    lastWin3D_27 = k;
                    wins3D_27.Add(dayEv);
                }

                // Kiểm tra trúng 4D 81 số (3x3x3x3)
                bool hit4D_81 = sortD4.Take(3).Contains(actD4) && sortD3.Take(3).Contains(actD3) && sortD2.Take(3).Contains(actD2) && sortD1.Take(3).Contains(actD1);
                if (hit4D_81)
                {
                    var ev4D = new WinEvent
                    {
                        PeriodIndex = k,
                        Date = actual.Date,
                        DayOfWeek = actual.DayOfWeek,
                        Actual = actual.State4D,
                        ActualStr = string.Format("{0:D4}", actual.State4D),
                        GapFromPrevWin = (lastWin4D_81 == -1) ? 0 : (k - lastWin4D_81),
                        EnergyGap = energyGap3D,
                        NumDigitsInGoldenGan = goldenCount3D,
                        NumDigitsDropped = dropCount3D
                    };
                    lastWin4D_81 = k;
                    wins4D_81.Add(ev4D);
                }

                // Kiểm tra trúng 4D 36 số (2x2x3x3)
                bool hit4D_36 = sortD4.Take(2).Contains(actD4) && sortD3.Take(2).Contains(actD3) && sortD2.Take(3).Contains(actD2) && sortD1.Take(3).Contains(actD1);
                if (hit4D_36)
                {
                    var ev4D36 = new WinEvent
                    {
                        PeriodIndex = k,
                        Date = actual.Date,
                        DayOfWeek = actual.DayOfWeek,
                        Actual = actual.State4D,
                        ActualStr = string.Format("{0:D4}", actual.State4D),
                        GapFromPrevWin = (lastWin4D_36 == -1) ? 0 : (k - lastWin4D_36)
                    };
                    lastWin4D_36 = k;
                    wins4D_36.Add(ev4D36);
                }
            }

            // =========================================================================
            // BÁO CÁO PHÂN TÍCH QUY LUẬT TIỀN ĐIỀU KIỆN TRÚNG 3D (36 KỲ TRÚNG)
            // =========================================================================
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            Console.WriteLine("  1. PHÂN TÍCH CÁC KỲ TRÚNG DÀN 3D 27 SỐ (TỔNG CỘNG {0} KỲ TRÚNG / 1.000 KỲ)", wins3D_27.Count);
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            Console.WriteLine("Kỳ      | Ngày       | Thứ | Kết quả 3D | Khoảng cách nổ (Gap) | Độ chênh thế năng | Chữ số Gan vàng | Chữ số Rơi");
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            foreach (var w in wins3D_27)
            {
                Console.WriteLine("#{0,-5} | {1} | {2,-3} | {3,-10} | Cách kỳ trước: {4,2} ngày | Gap: {5,5:F2}        | {6}/9 số vàng   | {7}/3 số rơi",
                    w.PeriodIndex, w.Date, w.DayOfWeek, w.ActualStr, w.GapFromPrevWin, w.EnergyGap, w.NumDigitsInGoldenGan, w.NumDigitsDropped);
            }

            // Thống kê phân phối khoảng cách (Gaps) giữa các lần trúng 3D
            var gaps3D = wins3D_27.Skip(1).Select(x => x.GapFromPrevWin).ToList();
            Console.WriteLine("\n--- THỐNG KÊ KHOẢNG CÁCH GIỮA 2 LẦN TRÚNG 3D LIÊN TIẾP ---");
            Console.WriteLine("Khoảng cách ngắn nhất : {0} ngày", gaps3D.Min());
            Console.WriteLine("Khoảng cách trung bình: {0:F1} ngày", gaps3D.Average());
            Console.WriteLine("Trung vị (Median)     : {0} ngày", gaps3D.OrderBy(x => x).ElementAt(gaps3D.Count / 2));
            Console.WriteLine("Khoảng cách dài nhất  : {0} ngày", gaps3D.Max());
            
            int gap_1_to_15 = gaps3D.Count(g => g <= 15);
            int gap_16_to_35 = gaps3D.Count(g => g > 15 && g <= 35);
            int gap_over_35 = gaps3D.Count(g => g > 35);
            Console.WriteLine("Phân bổ chu kỳ: [<= 15 ngày]: {0} lần ({1:F1}%) | [16 -> 35 ngày]: {2} lần ({3:F1}%) | [> 35 ngày]: {4} lần ({5:F1}%)",
                gap_1_to_15, (double)gap_1_to_15 / gaps3D.Count * 100,
                gap_16_to_35, (double)gap_16_to_35 / gaps3D.Count * 100,
                gap_over_35, (double)gap_over_35 / gaps3D.Count * 100);

            // =========================================================================
            // BÁO CÁO PHÂN TÍCH QUY LUẬT TIỀN ĐIỀU KIỆN TRÚNG 4D (13 KỲ TRÚNG 81s VÀ 7 KỲ 36s)
            // =========================================================================
            Console.WriteLine("\n-----------------------------------------------------------------------------------------");
            Console.WriteLine("  2. PHÂN TÍCH CÁC KỲ TRÚNG DÀN 4D 81 SỐ (TỔNG CỘNG {0} KỲ TRÚNG / 1.000 KỲ)", wins4D_81.Count);
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            Console.WriteLine("Kỳ      | Ngày       | Thứ | Kết quả 4D | Khoảng cách nổ (Gap) | Độ chênh thế năng | Chữ số Gan vàng");
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            foreach (var w in wins4D_81)
            {
                Console.WriteLine("#{0,-5} | {1} | {2,-3} | {3,-10} | Cách kỳ trước: {4,3} ngày| Gap: {5,5:F2}        | {6}/9 số vàng",
                    w.PeriodIndex, w.Date, w.DayOfWeek, w.ActualStr, w.GapFromPrevWin, w.EnergyGap, w.NumDigitsInGoldenGan);
            }

            var gaps4D = wins4D_81.Skip(1).Select(x => x.GapFromPrevWin).ToList();
            Console.WriteLine("\n--- THỐNG KÊ KHOẢNG CÁCH GIỮA 2 LẦN TRÚNG 4D LIÊN TIẾP ---");
            Console.WriteLine("Khoảng cách ngắn nhất : {0} ngày", gaps4D.Min());
            Console.WriteLine("Khoảng cách trung bình: {0:F1} ngày", gaps4D.Average());
            Console.WriteLine("Khoảng cách dài nhất  : {0} ngày", gaps4D.Max());

            // =========================================================================
            // SO SÁNH ĐẶC TRƯNG TOÁN HỌC: NGÀY TRÚNG vs NGÀY BÌNH THƯỜNG
            // =========================================================================
            Console.WriteLine("\n=========================================================================================");
            Console.WriteLine("  3. SO SÁNH ĐẶC TRƯNG TOÁN HỌC GIỮA NGÀY TRÚNG 3D VÀ NGÀY THÔNG THƯỜNG");
            Console.WriteLine("=========================================================================================");
            double avgEnergyGap_Win = wins3D_27.Average(x => x.EnergyGap);
            double avgEnergyGap_All = allDays3D.Average(x => x.EnergyGap);
            double avgGolden_Win = wins3D_27.Average(x => x.NumDigitsInGoldenGan);
            double avgGolden_All = allDays3D.Average(x => x.NumDigitsInGoldenGan);
            double avgDrop_Win = wins3D_27.Average(x => x.NumDigitsDropped);
            double avgDrop_All = allDays3D.Average(x => x.NumDigitsDropped);

            Console.WriteLine("Chỉ số đo lường                 | Toàn bộ thị trường (1.000 ngày) | Các ngày trúng 3D (36 ngày) | Độ chênh lệch (Bias)");
            Console.WriteLine("-------------------------------------------------------------------------------------------------------");
            Console.WriteLine("Độ chênh thế năng (Energy Gap)  | {0,15:F2}             | {1,14:F2}           | {2,10:F2}", 
                avgEnergyGap_All, avgEnergyGap_Win, avgEnergyGap_Win - avgEnergyGap_All);
            Console.WriteLine("Số chữ số nằm trong Gan Vàng    | {0,15:F2} / 9 chữ số  | {1,14:F2} / 9 số   | {2,10:F2}", 
                avgGolden_All, avgGolden_Win, avgGolden_Win - avgGolden_All);
            Console.WriteLine("Số chữ số rơi từ giải ĐB hôm qua | {0,15:F2} / 3 trục    | {1,14:F2} / 3 trục | {2,10:F2}", 
                avgDrop_All, avgDrop_Win, avgDrop_Win - avgDrop_All);
            Console.WriteLine("=========================================================================================");
        }

        private static int CheckGoldenGan(List<Record> past, int digitIdx, int[] topDigits)
        {
            int n = past.Count;
            int count = 0;
            foreach (var d in topDigits)
            {
                int gan = 0;
                for (int i = n - 1; i >= 0; i--)
                {
                    if (past[i].Digits5[digitIdx] == d) break;
                    gan++;
                }
                if (gan >= 5 && gan <= 14) count++;
            }
            return count;
        }

        private static double[] ComputeDigitPotential(List<Record> past, int digitIdx, string currentDow)
        {
            int n = past.Count;
            int[] gan = new int[10];
            for (int d = 0; d < 10; d++)
            {
                for (int i = n - 1; i >= 0; i--)
                {
                    if (past[i].Digits5[digitIdx] == d) break;
                    gan[d]++;
                }
            }

            int[] f30 = new int[10];
            for (int i = Math.Max(0, n - 30); i < n; i++) f30[past[i].Digits5[digitIdx]]++;

            int[] dowCount = new int[10];
            int dowTotal = 0;
            for (int i = Math.Max(0, n - 150); i < n; i++)
            {
                if (past[i].DayOfWeek == currentDow)
                {
                    dowCount[past[i].Digits5[digitIdx]]++;
                    dowTotal++;
                }
            }

            int lastVal = past[n - 1].Digits5[digitIdx];

            double[] v = new double[10];
            for (int d = 0; d < 10; d++)
            {
                double r = 0.0;
                if (gan[d] == 0) r = 1.8;
                else if (gan[d] >= 5 && gan[d] <= 14) r = -2.0;
                else if (gan[d] > 25) r = 0.8;

                double k = -0.3 * (f30[d] - 3.0);
                double dowExp = dowTotal > 0 ? (double)dowTotal / 10.0 : 1.0;
                double dTerm = -0.4 * (dowCount[d] - dowExp);
                double dropTerm = (d == lastVal) ? -0.5 : 0.0;

                v[d] = r + k + dTerm + dropTerm;
            }
            return v;
        }

        private static List<Record> LoadData(string path)
        {
            var list = new List<Record>();
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = ParseCsvLine(line);
                if (parts.Count < 18) continue;
                try
                {
                    var rec = new Record();
                    rec.Id = int.Parse(parts[0]);
                    rec.Date = parts[1];
                    rec.DayOfWeek = parts[2].Trim('\"');
                    string gdb = parts[3].Trim('\"').PadLeft(5, '0');
                    if (gdb.Length > 5) gdb = gdb.Substring(gdb.Length - 5);
                    rec.Digits5 = new int[5];
                    for (int d = 0; d < 5; d++) rec.Digits5[d] = gdb[d] - '0';
                    rec.State3D = rec.Digits5[2] * 100 + rec.Digits5[3] * 10 + rec.Digits5[4];
                    rec.State4D = rec.Digits5[1] * 1000 + rec.Digits5[2] * 100 + rec.Digits5[3] * 10 + rec.Digits5[4];
                    list.Add(rec);
                }
                catch { }
            }
            return list;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var res = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    inQuotes = !inQuotes;
                    sb.Append(c);
                }
                else if (c == ',' && !inQuotes)
                {
                    res.Add(sb.ToString());
                    sb.Clear();
                }
                else sb.Append(c);
            }
            res.Add(sb.ToString());
            return res;
        }
    }
}
