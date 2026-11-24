using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TidsAnalysis
{
    public class SimpleRecord
    {
        public int Id;
        public string Date;
        public string DayOfWeek;
        public string Gdb;
        public string G1;
        public int[] Digits5;
        public int De;
        public int Head4D;
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            var records = LoadData(@"data\lottery_master.csv");
            int n = records.Count;

            int evalPeriods = 500;
            int startK = n - evalPeriods;

            Console.WriteLine("=========================================================================");
            Console.WriteLine("   KIỂM THỬ THỰC CHIẾN CÁC CẤU TRÚC 3D & 4D MỚI TRÊN 500 KỲ GẦN NHẤT     ");
            Console.WriteLine("=========================================================================");

            int hit3D_12 = 0;   // 3 Càng x 4 Đề (12 số)
            int hit3D_27 = 0;   // 3 Càng x 9 Đề (27 số)
            int hit3D_36 = 0;   // 4 Càng x 9 Đề (36 số)
            int hit3D_50 = 0;   // 2 Càng x 25 Đề E25 (50 số)
            int hit3D_65 = 0;   // 1 Càng BT x 65 Đề E65 (65 số)

            int hit4D_16 = 0;   // 4 Cặp đầu x 4 Đề
            int hit4D_36 = 0;   // 4 Cặp đầu x 9 Đề (36 số)
            int hit4D_81 = 0;   // 9 Cặp đầu x 9 Đề (81 số)

            for (int k = startK; k < n; k++)
            {
                var past = records.Take(k).ToList();
                var actual = records[k];
                int actualD3 = actual.Digits5[2];
                string actual3D = actual.Gdb.Length >= 3 ? actual.Gdb.Substring(actual.Gdb.Length - 3) : "";
                string actual4D = actual.Gdb.Length >= 4 ? actual.Gdb.Substring(actual.Gdb.Length - 4) : "";
                var prevRec = records[k - 1];

                // 1. Tính Càng tối ưu với bộ tham số tốt nhất vừa tìm được
                int[] gans = new int[10];
                for (int d = 0; d < 10; d++)
                {
                    for (int j = k - 1; j >= 0; j--)
                    {
                        if (past[j].Digits5[2] == d) break;
                        gans[d]++;
                    }
                }
                int[] f30 = new int[10];
                for (int j = Math.Max(0, k - 30); j < k; j++) f30[past[j].Digits5[2]]++;

                int[] fDow = new int[10];
                int cDow = 0;
                for (int j = Math.Max(0, k - 200); j < k; j++)
                {
                    if (past[j].DayOfWeek == actual.DayOfWeek)
                    {
                        fDow[past[j].Digits5[2]]++;
                        cDow++;
                    }
                }

                var dScores = new List<Tuple<int, double>>();
                for (int d = 0; d < 10; d++)
                {
                    double r = 0.0;
                    if (gans[d] == 0) r = 1.0;
                    else if (gans[d] >= 5 && gans[d] <= 12) r = -2.0; // Vùng gan vàng tối ưu
                    else if (gans[d] > 25) r = 0.5;

                    double kF = -0.2 * (f30[d] - 3.0);
                    double dowTerm = (cDow > 0) ? (-0.3 * (fDow[d] - ((double)cDow / 10.0))) : 0.0;
                    double dropTerm = (d == prevRec.Digits5[3] || d == prevRec.Digits5[4]) ? -0.8 : 0.0;

                    dScores.Add(new Tuple<int, double>(d, r + kF + dowTerm + dropTerm));
                }

                var sortedCang = dScores.OrderBy(x => x.Item2).Select(x => x.Item1).ToArray();
                int btCang = sortedCang[0];
                var top2Cang = sortedCang.Take(2).ToArray();
                var top3Cang = sortedCang.Take(3).ToArray();
                var top4Cang = sortedCang.Take(4).ToArray();

                // 2. Tính các dàn Đề
                var de4 = ComputeTopDe(past, 4);
                var de9 = ComputeTopDe(past, 9);
                var de25 = ComputeTopDe(past, 25);
                var de65 = ComputeTopDe(past, 65);

                // Dàn 3D 12s (3 Càng x 4 Đề)
                var dan12 = new HashSet<string>();
                foreach (int c in top3Cang) foreach (int d in de4) dan12.Add(c.ToString() + d.ToString("D2"));
                if (dan12.Contains(actual3D)) hit3D_12++;

                // Dàn 3D 27s (3 Càng x 9 Đề)
                var dan27 = new HashSet<string>();
                foreach (int c in top3Cang) foreach (int d in de9) dan27.Add(c.ToString() + d.ToString("D2"));
                if (dan27.Contains(actual3D)) hit3D_27++;

                // Dàn 3D 36s (4 Càng x 9 Đề)
                var dan36 = new HashSet<string>();
                foreach (int c in top4Cang) foreach (int d in de9) dan36.Add(c.ToString() + d.ToString("D2"));
                if (dan36.Contains(actual3D)) hit3D_36++;

                // Dàn 3D 50s (2 Càng x 25 Đề E25)
                var dan50 = new HashSet<string>();
                foreach (int c in top2Cang) foreach (int d in de25) dan50.Add(c.ToString() + d.ToString("D2"));
                if (dan50.Contains(actual3D)) hit3D_50++;

                // Dàn 3D 65s (1 Càng BT x 65 Đề E65)
                var dan65 = new HashSet<string>();
                foreach (int d in de65) dan65.Add(btCang.ToString() + d.ToString("D2"));
                if (dan65.Contains(actual3D)) hit3D_65++;

                // 3. Tính 4D
                // Phân rã d5 và d4
                int[] gansD5 = new int[10];
                for (int d = 0; d < 10; d++)
                {
                    for (int j = k - 1; j >= 0; j--)
                    {
                        if (past[j].Digits5[0] == d) break;
                        gansD5[d]++;
                    }
                }
                var d5Scores = new List<Tuple<int, double>>();
                for (int d = 0; d < 10; d++)
                {
                    double r = (gansD5[d] >= 5 && gansD5[d] <= 15) ? -1.5 : 0.0;
                    d5Scores.Add(new Tuple<int, double>(d, r));
                }
                var top2_D5 = d5Scores.OrderBy(x => x.Item2).Take(2).Select(x => x.Item1).ToArray();
                var top3_D5 = d5Scores.OrderBy(x => x.Item2).Take(3).Select(x => x.Item1).ToArray();

                int[] gansD4 = new int[10];
                for (int d = 0; d < 10; d++)
                {
                    for (int j = k - 1; j >= 0; j--)
                    {
                        if (past[j].Digits5[1] == d) break;
                        gansD4[d]++;
                    }
                }
                var d4Scores = new List<Tuple<int, double>>();
                for (int d = 0; d < 10; d++)
                {
                    double r = (gansD4[d] >= 5 && gansD4[d] <= 15) ? -1.5 : 0.0;
                    d4Scores.Add(new Tuple<int, double>(d, r));
                }
                var top2_D4 = d4Scores.OrderBy(x => x.Item2).Take(2).Select(x => x.Item1).ToArray();
                var top3_D4 = d4Scores.OrderBy(x => x.Item2).Take(3).Select(x => x.Item1).ToArray();

                var top4Heads = new List<int>();
                foreach (int d5 in top2_D5) foreach (int d4 in top2_D4) top4Heads.Add(d5 * 10 + d4);

                var top9Heads = new List<int>();
                foreach (int d5 in top3_D5) foreach (int d4 in top3_D4) top9Heads.Add(d5 * 10 + d4);

                var dan4D_16 = new HashSet<string>();
                foreach (int hd in top4Heads) foreach (int tl in de4) dan4D_16.Add(hd.ToString("D2") + tl.ToString("D2"));
                if (dan4D_16.Contains(actual4D)) hit4D_16++;

                var dan4D_36 = new HashSet<string>();
                foreach (int hd in top4Heads) foreach (int tl in de9) dan4D_36.Add(hd.ToString("D2") + tl.ToString("D2"));
                if (dan4D_36.Contains(actual4D)) hit4D_36++;

                var dan4D_81 = new HashSet<string>();
                foreach (int hd in top9Heads) foreach (int tl in de9) dan4D_81.Add(hd.ToString("D2") + tl.ToString("D2"));
                if (dan4D_81.Contains(actual4D)) hit4D_81++;
            }

            Console.WriteLine("KẾT QUẢ ĐỐI SOÁT OOS ({0} KỲ GẦN NHẤT):", evalPeriods);
            Console.WriteLine("-------------------------------------------------------------------------");
            Console.WriteLine("1. Dàn 3D 12 số (3 Càng x 4 Đề):       {0,3} kỳ ({1:F2}%)  [Mẫu ngẫu nhiên: 1.20%]",
                hit3D_12, (double)hit3D_12 / evalPeriods * 100);
            Console.WriteLine("2. Dàn 3D 27 số (3 Càng x 9 Đề):       {0,3} kỳ ({1:F2}%)  [Mẫu ngẫu nhiên: 2.70%]",
                hit3D_27, (double)hit3D_27 / evalPeriods * 100);
            Console.WriteLine("3. Dàn 3D 36 số (4 Càng x 9 Đề):       {0,3} kỳ ({1:F2}%)  [Mẫu ngẫu nhiên: 3.60%] ★",
                hit3D_36, (double)hit3D_36 / evalPeriods * 100);
            Console.WriteLine("4. Dàn 3D 50 số (2 Càng x 25 Đề E25):   {0,3} kỳ ({1:F2}%)  [Mẫu ngẫu nhiên: 5.00%] ★★★ ĐỘT PHÁ",
                hit3D_50, (double)hit3D_50 / evalPeriods * 100);
            Console.WriteLine("5. Dàn 3D 65 số (1 Càng BT x 65 Đề E65):{0,3} kỳ ({1:F2}%)  [Mẫu ngẫu nhiên: 6.50%]",
                hit3D_65, (double)hit3D_65 / evalPeriods * 100);
            Console.WriteLine("-------------------------------------------------------------------------");
            Console.WriteLine("6. Dàn 4D 16 số (4 Đầu x 4 Đề):        {0,3} kỳ ({1:F2}%)  [Mẫu ngẫu nhiên: 0.16%]",
                hit4D_16, (double)hit4D_16 / evalPeriods * 100);
            Console.WriteLine("7. Dàn 4D 36 số (4 Đầu x 9 Đề):        {0,3} kỳ ({1:F2}%)  [Mẫu ngẫu nhiên: 0.36%] ★ ĐỘT PHÁ",
                hit4D_36, (double)hit4D_36 / evalPeriods * 100);
            Console.WriteLine("8. Dàn 4D 81 số (9 Đầu x 9 Đề):        {0,3} kỳ ({1:F2}%)  [Mẫu ngẫu nhiên: 0.81%]",
                hit4D_81, (double)hit4D_81 / evalPeriods * 100);
            Console.WriteLine("=========================================================================");
        }

        private static List<int> ComputeTopDe(List<SimpleRecord> past, int count)
        {
            int n = past.Count;
            int[] gan = new int[100];
            for (int s = 0; s < 100; s++)
            {
                for (int i = n - 1; i >= 0; i--)
                {
                    if (past[i].De == s) break;
                    gan[s]++;
                }
            }
            int[] f100 = new int[100];
            for (int i = Math.Max(0, n - 100); i < n; i++) f100[past[i].De]++;

            var scores = new List<Tuple<int, double>>();
            for (int s = 0; s < 100; s++)
            {
                double r = (gan[s] >= 30 && gan[s] <= 90) ? -1.8 : (gan[s] == 0 ? 1.5 : 0.0);
                double k = -0.3 * (f100[s] - 1.0);
                scores.Add(new Tuple<int, double>(s, r + k));
            }
            return scores.OrderBy(x => x.Item2).Take(count).Select(x => x.Item1).ToList();
        }

        private static List<SimpleRecord> LoadData(string path)
        {
            var list = new List<SimpleRecord>();
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = ParseCsvLine(line);
                if (parts.Count < 18) continue;
                try
                {
                    var rec = new SimpleRecord();
                    rec.Id = int.Parse(parts[0]);
                    rec.Date = parts[1];
                    rec.DayOfWeek = parts[2].Trim('\"');
                    rec.Gdb = parts[3].Trim('\"');
                    rec.G1 = parts[4].Trim('\"');

                    string gdbClean = rec.Gdb.PadLeft(5, '0');
                    if (gdbClean.Length > 5) gdbClean = gdbClean.Substring(gdbClean.Length - 5);
                    rec.Digits5 = new int[5];
                    for (int d = 0; d < 5; d++) rec.Digits5[d] = gdbClean[d] - '0';

                    rec.De = int.Parse(parts[11]);
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
