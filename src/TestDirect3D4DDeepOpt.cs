using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TidsDirectSpaceDeepOpt
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

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            string csvPath = @"data\lottery_master.csv";
            var records = LoadData(csvPath);
            int n = records.Count;
            Console.WriteLine("Loaded {0} records for Deep 3D & 4D Space Optimization.", n);

            // Kiểm thử Walk-Forward OOS trên 500 kỳ gần nhất
            int testPeriods = 500;
            int startK = n - testPeriods;

            Console.WriteLine("=========================================================================================");
            Console.WriteLine("   NGHIÊN CỨU TOÁN HỌC ĐỘT PHÁ: ĐỊNH LƯỢNG TRỰC TIẾP KHÔNG GIAN 1.000 SỐ 3D & 10.000 SỐ 4D");
            Console.WriteLine("   (KIỂM THỬ THỰC NGHIỆM WALK-FORWARD TRÊN 500 KỲ: {0} -> {1})", records[startK].Date, records[n-1].Date);
            Console.WriteLine("=========================================================================================\n");

            // Bộ đếm cho các mô hình 3D (Không gian 1.000 trạng thái)
            // Mô hình A: Tensor Product 1D độc lập (k3 x k2 x k1)
            int hit3D_Tensor27 = 0;   // 3 x 3 x 3 = 27 số (2.7% không gian)
            int hit3D_Tensor64 = 0;   // 4 x 4 x 4 = 64 số (6.4% không gian)
            int hit3D_Tensor125 = 0;  // 5 x 5 x 5 = 125 số (12.5% không gian)

            // Mô hình B: Trường thế năng tương tác 3 hạt trực tiếp V_eff(d3, d2, d1) trên toàn bộ 1.000 số
            int hit3D_Pot27 = 0;      // Top 27 số năng lượng thấp nhất
            int hit3D_Pot50 = 0;      // Top 50 số
            int hit3D_Pot64 = 0;      // Top 64 số
            int hit3D_Pot100 = 0;     // Top 100 số
            int hit3D_Pot125 = 0;     // Top 125 số
            int hit3D_Pot200 = 0;     // Top 200 số

            // Bộ đếm cho các mô hình 4D (Không gian 10.000 trạng thái)
            // Mô hình C: Tensor Product 1D (k4 x k3 x k2 x k1)
            int hit4D_Tensor16 = 0;   // 2 x 2 x 2 x 2 = 16 số (0.16% không gian)
            int hit4D_Tensor81 = 0;   // 3 x 3 x 3 x 3 = 81 số (0.81% không gian)
            int hit4D_Tensor256 = 0;  // 4 x 4 x 4 x 4 = 256 số (2.56% không gian)

            // Mô hình D: Phân rã Trực giao Head(d4,d3) x Tail(d2,d1)
            int hit4D_HeadTail16 = 0;  // 4 đầu x 4 đuôi = 16 số (0.16%)
            int hit4D_HeadTail50 = 0;  // 5 đầu x 10 đuôi = 50 số (0.50%)
            int hit4D_HeadTail81 = 0;  // 9 đầu x 9 đuôi = 81 số (0.81%)
            int hit4D_HeadTail100 = 0; // 10 đầu x 10 đuôi = 100 số (1.00%)
            int hit4D_HeadTail200 = 0; // 10 đầu x 20 đuôi = 200 số (2.00%)

            // Giá trị trả thưởng chuẩn thị trường (cược 1k):
            long reward3D = 960000;   // 1:960
            long reward4D = 8880000;  // 1:8880

            for (int k = startK; k < n; k++)
            {
                var past = records.Take(k).ToList();
                var actual = records[k];
                int act3D = actual.State3D;
                int act4D = actual.State4D;
                int actD4 = actual.Digits5[1];
                int actD3 = actual.Digits5[2];
                int actD2 = actual.Digits5[3];
                int actD1 = actual.Digits5[4];

                // -------------------------------------------------------------
                // 1. TÍNH THẾ NĂNG 1D CHO TỪNG TRỤC (d4, d3, d2, d1) CÓ HIỆU CHỈNH
                // -------------------------------------------------------------
                double[] vD4 = ComputeDigitPotential(past, 1, actual.DayOfWeek);
                double[] vD3 = ComputeDigitPotential(past, 2, actual.DayOfWeek);
                double[] vD2 = ComputeDigitPotential(past, 3, actual.DayOfWeek);
                double[] vD1 = ComputeDigitPotential(past, 4, actual.DayOfWeek);

                // Top sorted digits for each dimension
                var sortD4 = Enumerable.Range(0, 10).OrderBy(d => vD4[d]).ToArray();
                var sortD3 = Enumerable.Range(0, 10).OrderBy(d => vD3[d]).ToArray();
                var sortD2 = Enumerable.Range(0, 10).OrderBy(d => vD2[d]).ToArray();
                var sortD1 = Enumerable.Range(0, 10).OrderBy(d => vD1[d]).ToArray();

                // -------------------------------------------------------------
                // MÔ HÌNH A: TENSOR PRODUCT 1D ĐỘC LẬP CHO 3D
                // -------------------------------------------------------------
                // 27 số = Top 3 d3 x Top 3 d2 x Top 3 d1
                bool inTensor27 = sortD3.Take(3).Contains(actD3) && sortD2.Take(3).Contains(actD2) && sortD1.Take(3).Contains(actD1);
                if (inTensor27) hit3D_Tensor27++;

                // 64 số = Top 4 d3 x Top 4 d2 x Top 4 d1
                bool inTensor64 = sortD3.Take(4).Contains(actD3) && sortD2.Take(4).Contains(actD2) && sortD1.Take(4).Contains(actD1);
                if (inTensor64) hit3D_Tensor64++;

                // 125 số = Top 5 d3 x Top 5 d2 x Top 5 d1
                bool inTensor125 = sortD3.Take(5).Contains(actD3) && sortD2.Take(5).Contains(actD2) && sortD1.Take(5).Contains(actD1);
                if (inTensor125) hit3D_Tensor125++;

                // -------------------------------------------------------------
                // MÔ HÌNH B: TRƯỜNG THẾ NĂNG TƯƠNG TÁC 3 HẠT TOÀN DIỆN V_eff TRÊN 1.000 SỐ 3D
                // -------------------------------------------------------------
                double[,] w32 = ComputeCoupling2D(past, 2, 3);
                double[,] w21 = ComputeCoupling2D(past, 3, 4);
                double[,] w31 = ComputeCoupling2D(past, 2, 4);
                double[] vParity8 = ComputeParity8Potential(past);
                double[] vSum3D = ComputeSum10Potential(past);

                var list3D = new List<Tuple<int, double>>(1000);
                for (int s = 0; s < 1000; s++)
                {
                    int d3 = s / 100;
                    int d2 = (s / 10) % 10;
                    int d1 = s % 10;

                    double v1D = vD3[d3] + vD2[d2] + vD1[d1];
                    double vCoupling = 0.5 * w32[d3, d2] + 0.5 * w21[d2, d1] + 0.3 * w31[d3, d1];
                    
                    int pIdx = ((d3 % 2) << 2) | ((d2 % 2) << 1) | (d1 % 2);
                    double vP8 = vParity8[pIdx];

                    int sum3 = (d3 + d2 + d1) % 10;
                    double vS = vSum3D[sum3];

                    // Kép / Gánh bias: ABA, AAB, ABB
                    double structBonus = 0.0;
                    if (d3 == d1) structBonus -= 0.3; // Dạng gánh ABA
                    if (d3 == d2 || d2 == d1) structBonus -= 0.2; // Dạng kép

                    double totalV = 0.7 * v1D + 0.5 * vCoupling + 0.6 * vP8 + 0.6 * vS + structBonus;
                    list3D.Add(new Tuple<int, double>(s, totalV));
                }

                var sorted3D = list3D.OrderBy(x => x.Item2).Select(x => x.Item1).ToArray();
                var set27 = new HashSet<int>(sorted3D.Take(27));
                var set50 = new HashSet<int>(sorted3D.Take(50));
                var set64 = new HashSet<int>(sorted3D.Take(64));
                var set100 = new HashSet<int>(sorted3D.Take(100));
                var set125 = new HashSet<int>(sorted3D.Take(125));
                var set200 = new HashSet<int>(sorted3D.Take(200));

                if (set27.Contains(act3D)) hit3D_Pot27++;
                if (set50.Contains(act3D)) hit3D_Pot50++;
                if (set64.Contains(act3D)) hit3D_Pot64++;
                if (set100.Contains(act3D)) hit3D_Pot100++;
                if (set125.Contains(act3D)) hit3D_Pot125++;
                if (set200.Contains(act3D)) hit3D_Pot200++;

                // -------------------------------------------------------------
                // MÔ HÌNH C: TENSOR PRODUCT 1D ĐỘC LẬP CHO 4D (k4 x k3 x k2 x k1)
                // -------------------------------------------------------------
                bool in4D_16 = sortD4.Take(2).Contains(actD4) && sortD3.Take(2).Contains(actD3) && sortD2.Take(2).Contains(actD2) && sortD1.Take(2).Contains(actD1);
                if (in4D_16) hit4D_Tensor16++;

                bool in4D_81 = sortD4.Take(3).Contains(actD4) && sortD3.Take(3).Contains(actD3) && sortD2.Take(3).Contains(actD2) && sortD1.Take(3).Contains(actD1);
                if (in4D_81) hit4D_Tensor81++;

                bool in4D_256 = sortD4.Take(4).Contains(actD4) && sortD3.Take(4).Contains(actD3) && sortD2.Take(4).Contains(actD2) && sortD1.Take(4).Contains(actD1);
                if (in4D_256) hit4D_Tensor256++;

                // -------------------------------------------------------------
                // MÔ HÌNH D: PHÂN RÃ TRỰC GIAO HEAD (d4,d3) x TAIL (d2,d1) CHO 4D
                // -------------------------------------------------------------
                double[,] w43 = ComputeCoupling2D(past, 1, 2);
                var headList = new List<Tuple<int, double>>(100);
                for (int h = 0; h < 100; h++)
                {
                    int d4 = h / 10;
                    int d3 = h % 10;
                    double vH = vD4[d4] + vD3[d3] + 0.6 * w43[d4, d3];
                    headList.Add(new Tuple<int, double>(h, vH));
                }
                var topHeads = headList.OrderBy(x => x.Item2).Select(x => x.Item1).ToArray();

                var tailList = new List<Tuple<int, double>>(100);
                for (int t = 0; t < 100; t++)
                {
                    int d2 = t / 10;
                    int d1 = t % 10;
                    double vT = vD2[d2] + vD1[d1] + 0.6 * w21[d2, d1];
                    tailList.Add(new Tuple<int, double>(t, vT));
                }
                var topTails = tailList.OrderBy(x => x.Item2).Select(x => x.Item1).ToArray();

                int actHead = actD4 * 10 + actD3;
                int actTail = actD2 * 10 + actD1;

                // 4 đầu x 4 đuôi = 16 số
                if (topHeads.Take(4).Contains(actHead) && topTails.Take(4).Contains(actTail)) hit4D_HeadTail16++;

                // 5 đầu x 10 đuôi = 50 số
                if (topHeads.Take(5).Contains(actHead) && topTails.Take(10).Contains(actTail)) hit4D_HeadTail50++;

                // 9 đầu x 9 đuôi = 81 số
                if (topHeads.Take(9).Contains(actHead) && topTails.Take(9).Contains(actTail)) hit4D_HeadTail81++;

                // 10 đầu x 10 đuôi = 100 số
                if (topHeads.Take(10).Contains(actHead) && topTails.Take(10).Contains(actTail)) hit4D_HeadTail100++;

                // 10 đầu x 20 đuôi = 200 số
                if (topHeads.Take(10).Contains(actHead) && topTails.Take(20).Contains(actTail)) hit4D_HeadTail200++;
            }

            // =========================================================================
            // BÁO CÁO KẾT QUẢ CHO KHÔNG GIAN 1.000 SỐ 3D
            // =========================================================================
            Console.WriteLine("=========================================================================================");
            Console.WriteLine("    KẾT QUẢ KIỂM THỬ KHÔNG GIAN 1.000 SỐ 3D TRÊN 500 KỲ (HOÀN TOÀN ĐỘC LẬP 2D)");
            Console.WriteLine("=========================================================================================");
            Console.WriteLine("Cấu Trúc Dàn 3D             | Số Lượng | Số Kỳ Trúng | Tỷ Lệ Thực Tế | Ngẫu Nhiên | Hiệu Suất ROI / Lợi Nhuận");
            Console.WriteLine("---------------------------------------------------------------------------------------------------------");
            PrintRow("Mô hình A: Tensor 3x3x3    ", 27, hit3D_Tensor27, testPeriods, 0.027, reward3D);
            PrintRow("Mô hình A: Tensor 4x4x4    ", 64, hit3D_Tensor64, testPeriods, 0.064, reward3D);
            PrintRow("Mô hình A: Tensor 5x5x5    ", 125, hit3D_Tensor125, testPeriods, 0.125, reward3D);
            Console.WriteLine("---------------------------------------------------------------------------------------------------------");
            PrintRow("Mô hình B: Thế năng Top 27 ", 27, hit3D_Pot27, testPeriods, 0.027, reward3D);
            PrintRow("Mô hình B: Thế năng Top 50 ", 50, hit3D_Pot50, testPeriods, 0.050, reward3D);
            PrintRow("Mô hình B: Thế năng Top 64 ", 64, hit3D_Pot64, testPeriods, 0.064, reward3D);
            PrintRow("Mô hình B: Thế năng Top 100", 100, hit3D_Pot100, testPeriods, 0.100, reward3D);
            PrintRow("Mô hình B: Thế năng Top 125", 125, hit3D_Pot125, testPeriods, 0.125, reward3D);
            PrintRow("Mô hình B: Thế năng Top 200", 200, hit3D_Pot200, testPeriods, 0.200, reward3D);
            Console.WriteLine("=========================================================================================\n");

            // =========================================================================
            // BÁO CÁO KẾT QUẢ CHO KHÔNG GIAN 10.000 SỐ 4D
            // =========================================================================
            Console.WriteLine("=========================================================================================");
            Console.WriteLine("   KẾT QUẢ KIỂM THỬ KHÔNG GIAN 10.000 SỐ 4D TRÊN 500 KỲ (HOÀN TOÀN ĐỘC LẬP 2D)");
            Console.WriteLine("=========================================================================================");
            Console.WriteLine("Cấu Trúc Dàn 4D             | Số Lượng | Số Kỳ Trúng | Tỷ Lệ Thực Tế | Ngẫu Nhiên | Doanh Thu / Chi Phí (Lãi)");
            Console.WriteLine("---------------------------------------------------------------------------------------------------------");
            PrintRow("Mô hình C: Tensor 2x2x2x2  ", 16, hit4D_Tensor16, testPeriods, 0.0016, reward4D);
            PrintRow("Mô hình C: Tensor 3x3x3x3  ", 81, hit4D_Tensor81, testPeriods, 0.0081, reward4D);
            PrintRow("Mô hình C: Tensor 4x4x4x4  ", 256, hit4D_Tensor256, testPeriods, 0.0256, reward4D);
            Console.WriteLine("---------------------------------------------------------------------------------------------------------");
            PrintRow("Mô hình D: Head4 x Tail4   ", 16, hit4D_HeadTail16, testPeriods, 0.0016, reward4D);
            PrintRow("Mô hình D: Head5 x Tail10  ", 50, hit4D_HeadTail50, testPeriods, 0.0050, reward4D);
            PrintRow("Mô hình D: Head9 x Tail9   ", 81, hit4D_HeadTail81, testPeriods, 0.0081, reward4D);
            PrintRow("Mô hình D: Head10 x Tail10 ", 100, hit4D_HeadTail100, testPeriods, 0.0100, reward4D);
            PrintRow("Mô hình D: Head10 x Tail20 ", 200, hit4D_HeadTail200, testPeriods, 0.0200, reward4D);
            Console.WriteLine("=========================================================================================");
        }

        private static void PrintRow(string name, int size, int hits, int total, double randProb, long reward)
        {
            double actRate = (double)hits / total * 100.0;
            double randRate = randProb * 100.0;
            long totalCost = (long)size * 1000 * total;
            long totalReward = (long)hits * reward;
            long pnl = totalReward - totalCost;
            double roi = totalCost > 0 ? ((double)pnl / totalCost * 100.0) : 0.0;
            string roiStr = pnl >= 0 
                ? string.Format("+{0:F1}% (LÃI +{1:N0}k)", roi, pnl / 1000) 
                : string.Format("{0:F1}% ({1:N0}k)", roi, pnl / 1000);
            
            Console.WriteLine("{0} | {1,8} | {2,4} kỳ / {3,-3} | {4,6:F2}%      | {5,5:F2}%     | {6}",
                name, size + " số", hits, total, actRate, randRate, roiStr);
        }

        private static double[] ComputeDigitPotential(List<Record> past, int digitIdx, string currentDow)
        {
            int n = past.Count;
            // Gan
            int[] gan = new int[10];
            for (int d = 0; d < 10; d++)
            {
                for (int i = n - 1; i >= 0; i--)
                {
                    if (past[i].Digits5[digitIdx] == d) break;
                    gan[d]++;
                }
            }

            // F30
            int[] f30 = new int[10];
            for (int i = Math.Max(0, n - 30); i < n; i++) f30[past[i].Digits5[digitIdx]]++;

            // DOW (Thứ trong tuần)
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

            // Drop (rơi từ giải ĐB kỳ trước)
            int lastVal = past[n - 1].Digits5[digitIdx];

            double[] v = new double[10];
            for (int d = 0; d < 10; d++)
            {
                // Gan Vàng (5 -> 14 kỳ)
                double r = 0.0;
                if (gan[d] == 0) r = 1.8; // Cản lặp
                else if (gan[d] >= 5 && gan[d] <= 14) r = -2.0; // Vùng hồi quy vàng
                else if (gan[d] > 25) r = 0.8; // Quá gan

                // Tần suất F30
                double k = -0.3 * (f30[d] - 3.0);

                // DOW
                double dowExp = dowTotal > 0 ? (double)dowTotal / 10.0 : 1.0;
                double dTerm = -0.4 * (dowCount[d] - dowExp);

                // Drop bóng/rơi
                double dropTerm = (d == lastVal) ? -0.5 : 0.0;

                v[d] = r + k + dTerm + dropTerm;
            }
            return v;
        }

        private static double[,] ComputeCoupling2D(List<Record> past, int idx1, int idx2)
        {
            int n = past.Count;
            int[,] f100 = new int[10, 10];
            int start = Math.Max(0, n - 100);
            for (int i = start; i < n; i++)
            {
                int d1 = past[i].Digits5[idx1];
                int d2 = past[i].Digits5[idx2];
                f100[d1, d2]++;
            }

            double[,] w = new double[10, 10];
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    w[i, j] = -0.4 * (f100[i, j] - 1.0);
                }
            }
            return w;
        }

        private static double[] ComputeParity8Potential(List<Record> past)
        {
            int n = past.Count;
            int[] f50 = new int[8];
            int start = Math.Max(0, n - 50);
            for (int i = start; i < n; i++)
            {
                int p = ((past[i].Digits5[2] % 2) << 2) | ((past[i].Digits5[3] % 2) << 1) | (past[i].Digits5[4] % 2);
                f50[p]++;
            }

            int[] gan = new int[8];
            for (int p = 0; p < 8; p++)
            {
                for (int i = n - 1; i >= 0; i--)
                {
                    int pI = ((past[i].Digits5[2] % 2) << 2) | ((past[i].Digits5[3] % 2) << 1) | (past[i].Digits5[4] % 2);
                    if (pI == p) break;
                    gan[p]++;
                }
            }

            double[] v = new double[8];
            for (int p = 0; p < 8; p++)
            {
                double r = (gan[p] >= 6 && gan[p] <= 18) ? -1.8 : (gan[p] == 0 ? 1.2 : 0.0);
                double k = -0.3 * (f50[p] - (50.0 / 8.0));
                v[p] = r + k;
            }
            return v;
        }

        private static double[] ComputeSum10Potential(List<Record> past)
        {
            int n = past.Count;
            int[] f50 = new int[10];
            int start = Math.Max(0, n - 50);
            for (int i = start; i < n; i++)
            {
                int s = (past[i].Digits5[2] + past[i].Digits5[3] + past[i].Digits5[4]) % 10;
                f50[s]++;
            }

            int[] gan = new int[10];
            for (int s = 0; s < 10; s++)
            {
                for (int i = n - 1; i >= 0; i--)
                {
                    int sI = (past[i].Digits5[2] + past[i].Digits5[3] + past[i].Digits5[4]) % 10;
                    if (sI == s) break;
                    gan[s]++;
                }
            }

            double[] v = new double[10];
            for (int s = 0; s < 10; s++)
            {
                double r = (gan[s] >= 6 && gan[s] <= 16) ? -1.5 : (gan[s] == 0 ? 1.0 : 0.0);
                double k = -0.3 * (f50[s] - 5.0);
                v[s] = r + k;
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
                    rec.Gdb = parts[3].Trim('\"');

                    string gdbClean = rec.Gdb.PadLeft(5, '0');
                    if (gdbClean.Length > 5) gdbClean = gdbClean.Substring(gdbClean.Length - 5);
                    rec.Digits5 = new int[5];
                    for (int d = 0; d < 5; d++) rec.Digits5[d] = gdbClean[d] - '0';

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
