using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace D50Evaluation
{
    public class Record
    {
        public int Id;
        public string Date;
        public int De;
        public int Dau;
        public int Duoi;
        public HashSet<int> UniqueLo;
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            string csvPath = Path.Combine(Directory.GetCurrentDirectory(), "data", "lottery_master.csv");
            var lines = File.ReadAllLines(csvPath, Encoding.UTF8);
            var records = new List<Record>();

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var p = ParseCsvLine(lines[i]);
                if (p.Count < 19) continue;
                try
                {
                    var r = new Record();
                    r.Id = int.Parse(p[0]);
                    r.Date = p[1];
                    r.De = int.Parse(p[11]);
                    r.Dau = int.Parse(p[12]);
                    r.Duoi = int.Parse(p[13]);
                    r.UniqueLo = new HashSet<int>();
                    var lo = p[17].Trim('\"').Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var s in lo) { int v; if (int.TryParse(s.Trim(), out v)) r.UniqueLo.Add(v); }
                    records.Add(r);
                }
                catch { }
            }

            int n = records.Count;
            int testDays = 300;
            int startIdx = n - testDays;

            int hitLvl = 0, hitHead50 = 0, hitPruned = 0, hitEnsemble = 0;
            int maxStreakLvl = 0, curStreakLvl = 0, maxLossLvl = 0, curLossLvl = 0;
            int maxStreakHead = 0, curStreakHead = 0, maxLossHead = 0, curLossHead = 0;
            int maxStreakEns = 0, curStreakEns = 0, maxLossEns = 0, curLossEns = 0;

            for (int t = startIdx; t < n; t++)
            {
                var sub = records.Take(t).ToList();
                int actualDe = records[t].De;
                int actualDau = records[t].Dau;

                // 1. LevelSet E50
                var d50_Lvl = GetD50_LevelSet(sub);
                bool wLvl = d50_Lvl.Contains(actualDe);
                if (wLvl) { hitLvl++; curStreakLvl++; curLossLvl = 0; if (curStreakLvl > maxStreakLvl) maxStreakLvl = curStreakLvl; }
                else { curLossLvl++; curStreakLvl = 0; if (curLossLvl > maxLossLvl) maxLossLvl = curLossLvl; }

                // 2. Top 5 Heads x 10 Units
                var d50_Head = GetD50_Top5Heads(sub);
                bool wHead = d50_Head.Contains(actualDe);
                if (wHead) { hitHead50++; curStreakHead++; curLossHead = 0; if (curStreakHead > maxStreakHead) maxStreakHead = curStreakHead; }
                else { curLossHead++; curStreakHead = 0; if (curLossHead > maxLossHead) maxLossHead = curLossHead; }

                // 3. Ensemble Hybrid (Dead-zone Pruned + Adaptive Ranking)
                var d50_Ens = GetD50_Ensemble(sub);
                bool wEns = d50_Ens.Contains(actualDe);
                if (wEns) { hitEnsemble++; curStreakEns++; curLossEns = 0; if (curStreakEns > maxStreakEns) maxStreakEns = curStreakEns; }
                else { curLossEns++; curStreakEns = 0; if (curLossEns > maxLossEns) maxLossEns = curLossEns; }
            }

            Console.WriteLine("=========================================================================");
            Console.WriteLine("          KIỂM THỬ THỰC NGHIỆM CÁC CHIẾN LƯỢC DÀN 50 SỐ TRÊN " + testDays + " KỲ          ");
            Console.WriteLine("=========================================================================");
            Console.WriteLine(string.Format("1. Dàn 50 Tập Mức Năng Lượng (Level Set E50):"));
            Console.WriteLine(string.Format("   - Tỷ lệ trúng: {0}/{1} kỳ ({2:F2}%)  [Chuỗi thông cực đại: {3} kỳ | Chuỗi thua max: {4} kỳ]",
                hitLvl, testDays, (double)hitLvl * 100 / testDays, maxStreakLvl, maxLossLvl));
            Console.WriteLine();
            Console.WriteLine(string.Format("2. Dàn 50 Top 5 Đầu Động Lượng (5 Đầu x 10 Đuôi):"));
            Console.WriteLine(string.Format("   - Tỷ lệ trúng: {0}/{1} kỳ ({2:F2}%)  [Chuỗi thông cực đại: {3} kỳ | Chuỗi thua max: {4} kỳ]",
                hitHead50, testDays, (double)hitHead50 * 100 / testDays, maxStreakHead, maxLossHead));
            Console.WriteLine();
            Console.WriteLine(string.Format("3. Dàn 50 Thích Nghi Đa Mô Hình (Ensemble Consensus & Dead-zone Filtering):"));
            Console.WriteLine(string.Format("   - Tỷ lệ trúng: {0}/{1} kỳ ({2:F2}%)  [Chuỗi thông cực đại: {3} kỳ | Chuỗi thua max: {4} kỳ]",
                hitEnsemble, testDays, (double)hitEnsemble * 100 / testDays, maxStreakEns, maxLossEns));
            Console.WriteLine("=========================================================================");
        }

        private static HashSet<int> GetD50_LevelSet(List<Record> records)
        {
            int n = records.Count;
            int lastDe = records[n - 1].De;
            var lastLo = records[n - 1].UniqueLo;

            int[] gan = new int[100];
            for (int s = 0; s < 100; s++)
            {
                for (int i = n - 1; i >= 0; i--) { if (records[i].De == s) break; gan[s]++; }
            }
            int[] f30 = new int[100];
            for (int i = Math.Max(0, n - 30); i < n; i++) f30[records[i].De]++;

            var list = new List<Tuple<int, double>>();
            for (int s = 0; s < 100; s++)
            {
                double r = (gan[s] == 0) ? 2.5 : ((gan[s] >= 8 && gan[s] <= 20) ? -1.8 + Math.Abs(gan[s] - 13)/10.0 : ((gan[s] > 30) ? 1.2 : -0.3));
                double k = -0.6 * (f30[s] - 0.30);
                double c = lastLo.Contains(s) ? -0.45 : 0.0;
                list.Add(new Tuple<int, double>(s, r + k + c));
            }
            return new HashSet<int>(list.OrderBy(x => x.Item2).Take(50).Select(x => x.Item1));
        }

        private static HashSet<int> GetD50_Top5Heads(List<Record> records)
        {
            int n = records.Count;
            int[] ganHeads = new int[10];
            for (int d = 0; d < 10; d++)
            {
                for (int i = n - 1; i >= 0; i--) { if (records[i].Dau == d) break; ganHeads[d]++; }
            }
            int[] f30Heads = new int[10];
            for (int i = Math.Max(0, n - 30); i < n; i++) f30Heads[records[i].Dau]++;

            var list = new List<Tuple<int, double>>();
            for (int d = 0; d < 10; d++)
            {
                double r = (ganHeads[d] == 0) ? 1.5 : ((ganHeads[d] >= 8 && ganHeads[d] <= 18) ? -1.6 + Math.Abs(ganHeads[d] - 12)/10.0 : -0.3);
                double k = -0.3 * (f30Heads[d] - 3.0);
                list.Add(new Tuple<int, double>(d, r + k));
            }
            var top5Heads = list.OrderBy(x => x.Item2).Take(5).Select(x => x.Item1).ToList();

            var res = new HashSet<int>();
            foreach (var h in top5Heads)
                for (int u = 0; u < 10; u++) res.Add(h * 10 + u);
            return res;
        }

        private static HashSet<int> GetD50_Ensemble(List<Record> records)
        {
            int n = records.Count;
            int lastDe = records[n - 1].De;
            var lastLo = records[n - 1].UniqueLo;

            int[] gan = new int[100];
            for (int s = 0; s < 100; s++)
            {
                for (int i = n - 1; i >= 0; i--) { if (records[i].De == s) break; gan[s]++; }
            }
            int[] f30 = new int[100];
            for (int i = Math.Max(0, n - 30); i < n; i++) f30[records[i].De]++;

            // Lọc Vùng Chết (Dead Zone): Loại trừ số vừa nổ (gan 0) và số gan lì (> 35)
            // Tính điểm số tổng hợp (Composite Score)
            var list = new List<Tuple<int, double>>();
            for (int s = 0; s < 100; s++)
            {
                if (gan[s] == 0) continue; // Triệt tiêu số vừa nổ

                double score = 0.0;
                // Ưu đãi chu kỳ vàng
                if (gan[s] >= 7 && gan[s] <= 22) score += 2.0;
                else if (gan[s] > 32) score -= 2.5; // Phạt số gan bão hòa

                // Động lượng ngắn hạn F30
                score += f30[s] * 0.8;

                // Lô rơi sang đề
                if (lastLo.Contains(s)) score += 1.2;

                list.Add(new Tuple<int, double>(s, score));
            }

            return new HashSet<int>(list.OrderByDescending(x => x.Item2).Take(50).Select(x => x.Item1));
        }

        private static List<string> ParseCsvLine(string line)
        {
            var res = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"') inQuotes = !inQuotes;
                else if (c == ',' && !inQuotes) { res.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
            res.Add(sb.ToString());
            return res;
        }
    }
}
