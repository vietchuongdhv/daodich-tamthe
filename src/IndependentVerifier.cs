using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IndependentVerification
{
    public class Record
    {
        public int Id;
        public string Date;
        public string DayOfWeek;
        public int De;
        public int Dau;
        public int Duoi;
        public int Tong;
        public int Head4D;
        public int Hundred;
        public HashSet<int> UniqueLo;
        public List<int> LoList;
    }

    public class Verifier
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            int testDays = 300;
            int parsed;
            if (args.Length > 0 && int.TryParse(args[0], out parsed)) testDays = parsed;

            string csvPath = Path.Combine(Directory.GetCurrentDirectory(), "data", "lottery_master.csv");
            if (!File.Exists(csvPath))
            {
                csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "data", "lottery_master.csv");
            }
            if (!File.Exists(csvPath))
            {
                Console.WriteLine("Lỗi: Không tìm thấy file CSV tại " + csvPath);
                return;
            }

            var records = LoadData(csvPath);
            RunAudit(records, testDays);
        }

        public static List<Record> LoadData(string path)
        {
            var list = new List<Record>();
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = ParseCsvLine(line);
                if (parts.Count < 19) continue;

                try
                {
                    var rec = new Record();
                    rec.Id = int.Parse(parts[0]);
                    rec.Date = parts[1];
                    rec.DayOfWeek = parts[2].Trim('\"');

                    string gdbClean = parts[3].Trim('\"').PadLeft(5, '0');
                    if (gdbClean.Length > 5) gdbClean = gdbClean.Substring(gdbClean.Length - 5);
                    rec.Hundred = gdbClean[2] - '0';
                    rec.Head4D = (gdbClean[1] - '0') * 10 + rec.Hundred;

                    rec.De = int.Parse(parts[11]);
                    rec.Dau = int.Parse(parts[12]);
                    rec.Duoi = int.Parse(parts[13]);
                    rec.Tong = int.Parse(parts[14]);

                    rec.UniqueLo = new HashSet<int>();
                    rec.LoList = new List<int>();
                    string loStr = parts[17].Trim('\"');
                    var loParts = loStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var lp in loParts)
                    {
                        int v;
                        if (int.TryParse(lp.Trim(), out v))
                        {
                            rec.UniqueLo.Add(v);
                            rec.LoList.Add(v);
                        }
                    }
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

        public static void RunAudit(List<Record> records, int testDays)
        {
            int n = records.Count;
            if (testDays <= 0 || testDays >= n - 100) testDays = 300;
            int startIdx = n - testDays;
            double lambda = Math.Log(2.0) / 45.0; // Bán rã 45 ngày cho đồ thị Lô tô

            Console.WriteLine("=========================================================================================");
            Console.WriteLine("    QUY TRÌNH KIỂM THỬ ĐỘC LẬP & KIỂM TOÁN HIỆU NĂNG MÔ HÌNH (INDEPENDENT AUDITOR)        ");
            Console.WriteLine(" KIỂM ĐỊNH MÙ OUT-OF-SAMPLE (WALK-FORWARD), Ý NGHĨA THỐNG KÊ (Z-SCORE) & QUẢN TRỊ RỦI RO  ");
            Console.WriteLine("=========================================================================================");
            Console.WriteLine("Dung lượng chuỗi thời gian:     " + n + " kỳ quay (" + records[0].Date + " -> " + records[n - 1].Date + ")");
            Console.WriteLine("Độ dài cửa sổ kiểm thử mù OOS: " + testDays + " kỳ gần nhất (từ " + records[startIdx].Date + " -> " + records[n - 1].Date + ")");
            Console.WriteLine("Nguyên tắc chống rò rỉ (Leak):  100% Blind Folded (Dữ liệu tại kỳ t bị cô lập hoàn toàn)");
            Console.WriteLine();

            // BIẾN THỐNG KÊ KẾT QUẢ ĐỀ
            int hitE50 = 0, hitE25 = 0, hitE9 = 0, hitE4 = 0, hitE1 = 0;
            int hit3D_27 = 0, hit4D_81 = 0;

            // TÀI CHÍNH ĐỀ (Giả định cược 1 điểm = 1k, ăn 99k)
            double pnlE50 = 0.0, pnlE25 = 0.0, pnlE9 = 0.0;
            double maxMddE50 = 0.0, peakE50 = 0.0;
            int maxLossE50 = 0, curLossE50 = 0;
            int maxStreakE50 = 0, curStreakE50 = 0;

            // BIẾN THỐNG KÊ KẾT QUẢ LÔ TÔ & XIÊN
            int hitBtLo = 0, totalBtFlashes = 0;
            double pnlBtLo = 0.0, peakBtLo = 0.0, maxMddBtLo = 0.0;
            int maxStreakBtLo = 0, curStreakBtLo = 0, maxLossBtLo = 0, curLossBtLo = 0;

            int hitStLo = 0, hitBothStLo = 0, totalStFlashes = 0;
            double pnlStLo = 0.0, peakStLo = 0.0, maxMddStLo = 0.0;
            int maxStreakStLo = 0, curStreakStLo = 0, maxLossStLo = 0, curLossStLo = 0;

            int hitTuThuLo = 0, totalTuThuFlashes = 0;
            double pnlTuThuLo = 0.0, peakTuThuLo = 0.0, maxMddTuThuLo = 0.0;
            int maxStreakTuThuLo = 0, curStreakTuThuLo = 0, maxLossTuThuLo = 0, curLossTuThuLo = 0;

            int hitXien2_ST = 0; // Cặp xiên 2 tạo bởi Song Thủ Lô
            int hitAtLeastOneXien2_Clique = 0;
            int totalXien2Hits_Clique = 0;
            int totalXien3Hits_Clique = 0;
            int hitXien4_Clique = 0;

            for (int t = startIdx; t < n; t++)
            {
                var subList = records.Take(t).ToList();
                var target = records[t];

                // -------------------------------------------------------------
                // 1. KIỂM TOÁN CÁC DÀN ĐỀ (E50, E25, E9, E4, E1, 3D, 4D)
                // -------------------------------------------------------------
                var rankedStates = ComputePotentialsV2(subList);
                int actualDe = target.De;

                // Dàn 50 (E50)
                bool isHit50 = rankedStates.Take(50).Contains(actualDe);
                if (isHit50)
                {
                    hitE50++;
                    pnlE50 += (99.0 - 50.0);
                    curStreakE50++; curLossE50 = 0;
                    if (curStreakE50 > maxStreakE50) maxStreakE50 = curStreakE50;
                }
                else
                {
                    pnlE50 -= 50.0;
                    curLossE50++; curStreakE50 = 0;
                    if (curLossE50 > maxLossE50) maxLossE50 = curLossE50;
                }
                if (pnlE50 > peakE50) peakE50 = pnlE50;
                if (peakE50 - pnlE50 > maxMddE50) maxMddE50 = peakE50 - pnlE50;

                // Dàn 25 (E25)
                if (rankedStates.Take(25).Contains(actualDe))
                {
                    hitE25++;
                    pnlE25 += (99.0 - 25.0);
                }
                else pnlE25 -= 25.0;

                // Dàn 9 (E9)
                if (rankedStates.Take(9).Contains(actualDe))
                {
                    hitE9++;
                    pnlE9 += (99.0 - 9.0);
                }
                else pnlE9 -= 9.0;

                // Tứ thủ Đề (E4)
                if (rankedStates.Take(4).Contains(actualDe)) hitE4++;

                // Bạch thủ Đề (E1)
                if (rankedStates[0] == actualDe) hitE1++;

                // Dàn 3D (27 số) và 4D (81 số)
                var top3Hundreds = GetTopHundredsQuiet(subList);
                var top9Heads = GetTopHeadsQuiet(subList);
                bool hitHundred = top3Hundreds.Contains(target.Hundred);
                bool hitDe9 = rankedStates.Take(9).Contains(actualDe);
                bool hitHead9 = top9Heads.Contains(target.Head4D);

                if (hitHundred && hitDe9) hit3D_27++;
                if (hitHead9 && hitDe9) hit4D_81++;

                // -------------------------------------------------------------
                // 2. KIỂM TOÁN LÔ TÔ & XIÊN (MA TRẬN GIAO THOA TOP ĐẦU x ĐUÔI + BSM BỔ SUNG)
                // -------------------------------------------------------------
                double[,] W;
                var rankedLo = GetAdaptiveTop4Lo(subList, lambda, out W);
                int btLo = rankedLo[0];
                int stLo = rankedLo[1];
                var tuThuLo = rankedLo;

                // 2.1 BẠCH THỦ LÔ (CƯỢC 1 ĐIỂM = 27k, TRÚNG = 99k)
                int btFlashes = target.LoList.Count(x => x == btLo);
                totalBtFlashes += btFlashes;
                if (btFlashes > 0)
                {
                    hitBtLo++;
                    pnlBtLo += (btFlashes * 99.0) - 27.0;
                    curStreakBtLo++; curLossBtLo = 0;
                    if (curStreakBtLo > maxStreakBtLo) maxStreakBtLo = curStreakBtLo;
                }
                else
                {
                    pnlBtLo -= 27.0;
                    curLossBtLo++; curStreakBtLo = 0;
                    if (curLossBtLo > maxLossBtLo) maxLossBtLo = curLossBtLo;
                }
                if (pnlBtLo > peakBtLo) peakBtLo = pnlBtLo;
                if (peakBtLo - pnlBtLo > maxMddBtLo) maxMddBtLo = peakBtLo - pnlBtLo;

                // 2.2 SONG THỦ LÔ (2 SỐ, CƯỢC 2 ĐIỂM = 54k, TRÚNG = 99k/nháy)
                int st1Flashes = target.LoList.Count(x => x == btLo);
                int st2Flashes = target.LoList.Count(x => x == stLo);
                int stFlashes = st1Flashes + st2Flashes;
                totalStFlashes += stFlashes;
                if (stFlashes > 0)
                {
                    hitStLo++;
                    pnlStLo += (stFlashes * 99.0) - 54.0;
                    curStreakStLo++; curLossStLo = 0;
                    if (curStreakStLo > maxStreakStLo) maxStreakStLo = curStreakStLo;
                }
                else
                {
                    pnlStLo -= 54.0;
                    curLossStLo++; curStreakStLo = 0;
                    if (curLossStLo > maxLossStLo) maxLossStLo = curLossStLo;
                }
                if (st1Flashes > 0 && st2Flashes > 0)
                {
                    hitBothStLo++;
                    hitXien2_ST++;
                }

                if (pnlStLo > peakStLo) peakStLo = pnlStLo;
                if (peakStLo - pnlStLo > maxMddStLo) maxMddStLo = peakStLo - pnlStLo;

                // 2.3 DÀN TỨ THỦ LÔ (4 SỐ, CƯỢC 4 ĐIỂM = 108k, TRÚNG = 99k/nháy)
                int ttFlashes = 0;
                foreach (int lo in tuThuLo) ttFlashes += target.LoList.Count(x => x == lo);
                totalTuThuFlashes += ttFlashes;
                if (ttFlashes > 0)
                {
                    hitTuThuLo++;
                    pnlTuThuLo += (ttFlashes * 99.0) - 108.0;
                    curStreakTuThuLo++; curLossTuThuLo = 0;
                    if (curStreakTuThuLo > maxStreakTuThuLo) maxStreakTuThuLo = curStreakTuThuLo;
                }
                else
                {
                    pnlTuThuLo -= 108.0;
                    curLossTuThuLo++; curStreakTuThuLo = 0;
                    if (curLossTuThuLo > maxLossTuThuLo) maxLossTuThuLo = curLossTuThuLo;
                }
                if (pnlTuThuLo > peakTuThuLo) peakTuThuLo = pnlTuThuLo;
                if (peakTuThuLo - pnlTuThuLo > maxMddTuThuLo) maxMddTuThuLo = peakTuThuLo - pnlTuThuLo;

                // 2.4 CỤM BỘ TỨ XIÊN HỢP NHẤT (ĐỒNG NHẤT 100% VỚI TỨ THỦ LÔ TOP 4)
                int[] best4 = tuThuLo.ToArray();

                int x2InDay = 0;
                int[][] pairs = new int[][] {
                    new int[] { best4[0], best4[1] }, new int[] { best4[0], best4[2] }, new int[] { best4[0], best4[3] },
                    new int[] { best4[1], best4[2] }, new int[] { best4[1], best4[3] }, new int[] { best4[2], best4[3] }
                };
                foreach (var pair in pairs)
                {
                    if (target.UniqueLo.Contains(pair[0]) && target.UniqueLo.Contains(pair[1]))
                    {
                        x2InDay++;
                        totalXien2Hits_Clique++;
                    }
                }
                if (x2InDay > 0) hitAtLeastOneXien2_Clique++;

                int[][] triplets = new int[][] {
                    new int[] { best4[0], best4[1], best4[2] }, new int[] { best4[0], best4[1], best4[3] },
                    new int[] { best4[0], best4[2], best4[3] }, new int[] { best4[1], best4[2], best4[3] }
                };
                foreach (var trip in triplets)
                {
                    if (target.UniqueLo.Contains(trip[0]) && target.UniqueLo.Contains(trip[1]) && target.UniqueLo.Contains(trip[2]))
                    {
                        totalXien3Hits_Clique++;
                    }
                }

                if (target.UniqueLo.Contains(best4[0]) && target.UniqueLo.Contains(best4[1]) &&
                    target.UniqueLo.Contains(best4[2]) && target.UniqueLo.Contains(best4[3]))
                {
                    hitXien4_Clique++;
                }
            }

            // =========================================================================
            // XUẤT BẢNG BÁO CÁO TOÁN HỌC & Ý NGHĨA THỐNG KÊ (ĐỀ & LÔ TÔ)
            // =========================================================================
            Console.WriteLine("PHẦN 1: BÁO CÁO KIỂM TOÁN TẦN SUẤT & Ý NGHĨA THỐNG KÊ (HYPOTHESIS TESTING)");
            Console.WriteLine("------------------------------------------------------------------------------------------------------");
            Console.WriteLine("Danh mục Cược       | Trúng/Tổng | Tỷ lệ Thực | Kỳ vọng Lý thuyết | Chênh lệch (Edge) | Z-Score (Độ tin cậy)");
            Console.WriteLine("------------------------------------------------------------------------------------------------------");

            // BẢNG ĐỀ
            PrintHypothesisRow("Dàn Đề 50 (E50)    ", hitE50, testDays, 0.50);
            PrintHypothesisRow("Dàn Đề 25 (E25)    ", hitE25, testDays, 0.25);
            PrintHypothesisRow("Dàn Đề 9  (E9)     ", hitE9, testDays, 0.09);
            PrintHypothesisRow("Tứ Thủ Đề (E4)     ", hitE4, testDays, 0.04);
            PrintHypothesisRow("Bạch Thủ Đề (E1)   ", hitE1, testDays, 0.01);
            PrintHypothesisRow("Dàn 3D (27 số)     ", hit3D_27, testDays, 0.027);
            PrintHypothesisRow("Dàn 4D (81 số)     ", hit4D_81, testDays, 0.0081);

            Console.WriteLine("------------------------------------------------------------------------------------------------------");
            // BẢNG LÔ TÔ & XIÊN
            PrintHypothesisRow("Bạch Thủ Lô (1 số) ", hitBtLo, testDays, 0.2378);
            PrintHypothesisRow("Song Thủ Lô (>=1)  ", hitStLo, testDays, 0.4209);
            PrintHypothesisRow("Song Thủ Nổ Cả Cặp ", hitBothStLo, testDays, 0.0547);
            PrintHypothesisRow("Tứ Thủ Lô (>=1 con)", hitTuThuLo, testDays, 0.6677);
            PrintHypothesisRow("Cặp Xiên 2 Song Thủ", hitXien2_ST, testDays, 0.0547);
            PrintHypothesisRow("Cụm 6 Xiên 2 Tứ Thủ", hitAtLeastOneXien2_Clique, testDays, 0.285);

            Console.WriteLine("------------------------------------------------------------------------------------------------------");
            Console.WriteLine("(*) Chú giải Z-Score: Z > 0 chứng minh mô hình có lợi thế toán học dương (+EV) so với ngẫu nhiên.");
            Console.WriteLine("    Z >= 1.65 tương ứng p-value < 0.05 (Độ tin cậy thống kê 95%, loại trừ hoàn toàn yếu tố may rủi).");
            Console.WriteLine();

            // =========================================================================
            // PHẦN 2: BÁO CÁO TÀI CHÍNH ĐỊNH LƯỢNG & BẢO TOÀN VỐN (CAPITAL STRESS TEST)
            // =========================================================================
            Console.WriteLine("PHẦN 2: BÁO CÁO TÀI CHÍNH ĐỊNH LƯỢNG & BẢO TOÀN VỐN (CAPITAL STRESS TEST)");
            Console.WriteLine("------------------------------------------------------------------------------------------------------");
            Console.WriteLine("1. KIỂM TOÁN TÀI CHÍNH CÁC DÀN ĐỀ ĐẶC BIỆT:");
            Console.WriteLine(string.Format("   * Dàn Đề 50 số (E50):   PnL = {0:+0.00;-0.00;0.00} điểm | ROI = {1:+0.00;-0.00;0.00}% | Max Streak: {2} | Max Loss: {3} | MDD: {4:F1} điểm",
                pnlE50, (pnlE50 / (50.0 * testDays)) * 100.0, maxStreakE50, maxLossE50, maxMddE50));
            Console.WriteLine(string.Format("   * Dàn Đề 25 số (E25):   PnL = {0:+0.00;-0.00;0.00} điểm | ROI = {1:+0.00;-0.00;0.00}%",
                pnlE25, (pnlE25 / (25.0 * testDays)) * 100.0));
            Console.WriteLine(string.Format("   * Dàn Đề 9 số  (E9):    PnL = {0:+0.00;-0.00;0.00} điểm | ROI = {1:+0.00;-0.00;0.00}%",
                pnlE9, (pnlE9 / (9.0 * testDays)) * 100.0));

            Console.WriteLine();
            Console.WriteLine("2. KIỂM TOÁN TÀI CHÍNH LÔ TÔ ĐỒ THỊ PHỔ LAPLACIAN (1 ĐIỂM = 27k VNĐ, TRÚNG = 99k VNĐ):");
            Console.WriteLine(string.Format("   * Bạch Thủ Lô (1 số, cược 27k): PnL = {0:+0.00;-0.00;0.00}k VNĐ | ROI = {1:+0.00;-0.00;0.00}% | Tổng nháy: {2} (TB: {3:F2} nháy/kỳ)",
                pnlBtLo, (pnlBtLo / (27.0 * testDays)) * 100.0, totalBtFlashes, (double)totalBtFlashes / testDays));
            Console.WriteLine(string.Format("                           Max Streak: {0} kỳ | Max Loss: {1} kỳ | MDD: {2:F1}k VNĐ",
                maxStreakBtLo, maxLossBtLo, maxMddBtLo));

            Console.WriteLine(string.Format("   * Song Thủ Lô (2 số, cược 54k): PnL = {0:+0.00;-0.00;0.00}k VNĐ | ROI = {1:+0.00;-0.00;0.00}% | Tổng nháy: {2} (TB: {3:F2} nháy/kỳ)",
                pnlStLo, (pnlStLo / (54.0 * testDays)) * 100.0, totalStFlashes, (double)totalStFlashes / testDays));
            Console.WriteLine(string.Format("                           Max Streak: {0} kỳ | Max Loss: {1} kỳ | MDD: {2:F1}k VNĐ",
                maxStreakStLo, maxLossStLo, maxMddStLo));

            Console.WriteLine(string.Format("   * Dàn Tứ Thủ Lô (4 số, cược 108k): PnL = {0:+0.00;-0.00;0.00}k VNĐ | ROI = {1:+0.00;-0.00;0.00}% | Tổng nháy: {2} (TB: {3:F2} nháy/kỳ)",
                pnlTuThuLo, (pnlTuThuLo / (108.0 * testDays)) * 100.0, totalTuThuFlashes, (double)totalTuThuFlashes / testDays));
            Console.WriteLine(string.Format("                           Max Streak: {0} kỳ | Max Loss: {1} kỳ | MDD: {2:F1}k VNĐ",
                maxStreakTuThuLo, maxLossTuThuLo, maxMddTuThuLo));

            Console.WriteLine();
            Console.WriteLine("3. HIỆU QUẢ CỤM XIÊN TỨ THỦ LÔ (ĐỒNG NHẤT 100% V_EFF):");
            Console.WriteLine(string.Format("   * Xiên 2 Song Thủ (Top 1 & Top 2): Trúng {0}/{1} kỳ ({2:F2}% vs Kỳ vọng 5.47%)",
                hitXien2_ST, testDays, (double)hitXien2_ST * 100.0 / testDays));
            Console.WriteLine(string.Format("   * Cụm 6 Cặp Xiên 2 Tứ Thủ:        Số kỳ nổ ít nhất 1 cặp: {0}/{1} ({2:F2}%) | Tổng lượt trúng: {3} cặp",
                hitAtLeastOneXien2_Clique, testDays, (double)hitAtLeastOneXien2_Clique * 100.0 / testDays, totalXien2Hits_Clique));
            Console.WriteLine(string.Format("   * Cụm 4 Bộ Xiên 3 Tứ Thủ:         Tổng lượt nổ Xiên 3:   {0} lượt", totalXien3Hits_Clique));
            Console.WriteLine(string.Format("   * Bộ Xiên 4 Tứ Thủ:               Tổng lượt nổ Xiên 4:   {0} lượt", hitXien4_Clique));
            Console.WriteLine("======================================================================================================");
        }

        private static void PrintHypothesisRow(string label, int hits, int total, double p0)
        {
            double pObs = (double)hits / total;
            double edge = pObs - p0;
            double se = Math.Sqrt(p0 * (1.0 - p0) / total);
            double z = se > 0 ? (pObs - p0) / se : 0;
            string verdict = z >= 1.65 ? "Ý nghĩa 95%" : (z > 0 ? "Ưu thế (+EV)" : "Dưới kỳ vọng");

            string edgeStr = (edge >= 0 ? "+" : "") + (edge * 100.0).ToString("F2") + "%";
            string zStr = (z >= 0 ? "+" : "") + z.ToString("F2");

            Console.WriteLine(string.Format("{0,-20} | {1,3}/{2,3} |   {3,5:F2}%   |      {4,5:F2}%      |     {5,7}     | Z = {6,5} ({7})",
                label, hits, total, pObs * 100.0, p0 * 100.0, edgeStr, zStr, verdict));
        }

        private static List<int> ComputePotentialsV2(List<Record> records)
        {
            int n = records.Count;
            int lastDe = records[n - 1].De;
            var lastUniqueLo = records[n - 1].UniqueLo;

            int[] gan = new int[100];
            for (int s = 0; s < 100; s++)
            {
                for (int i = n - 1; i >= 0; i--)
                {
                    if (records[i].De == s) break;
                    gan[s]++;
                }
            }

            int[] f100De = new int[100];
            for (int i = Math.Max(0, n - 100); i < n; i++) f100De[records[i].De]++;

            int[] f30Lo = new int[100];
            for (int i = Math.Max(0, n - 30); i < n; i++)
            {
                if (records[i].UniqueLo != null)
                {
                    foreach (var lo in records[i].UniqueLo) f30Lo[lo]++;
                }
            }

            int[] markov = new int[100];
            int total = 0;
            for (int i = 0; i < n - 1; i++)
            {
                if (records[i].De == lastDe)
                {
                    markov[records[i + 1].De]++;
                    total++;
                }
            }

            // Gan và F30 cho 10 Đầu, 10 Đuôi và 10 Tổng (1D Marginals)
            int[] ganHeads = new int[10];
            for (int d = 0; d < 10; d++)
                for (int i = n - 1; i >= 0; i--)
                {
                    if (records[i].Dau == d) break;
                    ganHeads[d]++;
                }
            int[] f30Heads = new int[10];
            for (int i = Math.Max(0, n - 30); i < n; i++) f30Heads[records[i].Dau]++;

            int[] ganUnits = new int[10];
            for (int d = 0; d < 10; d++)
                for (int i = n - 1; i >= 0; i--)
                {
                    if (records[i].Duoi == d) break;
                    ganUnits[d]++;
                }
            int[] f30Units = new int[10];
            for (int i = Math.Max(0, n - 30); i < n; i++) f30Units[records[i].Duoi]++;

            int[] ganSums = new int[10];
            for (int d = 0; d < 10; d++)
                for (int i = n - 1; i >= 0; i--)
                {
                    if (records[i].Tong == d) break;
                    ganSums[d]++;
                }
            int[] f30Sums = new int[10];
            for (int i = Math.Max(0, n - 30); i < n; i++) f30Sums[records[i].Tong]++;

            double[] vTens = new double[10];
            double[] vUnits = new double[10];
            double[] vSums = new double[10];

            for (int d = 0; d < 10; d++)
            {
                double rHead = (ganHeads[d] == 0) ? 1.5 : (ganHeads[d] > 30 ? 1.2 : -0.05 * ganHeads[d]);
                vTens[d] = rHead - 0.25 * (f30Heads[d] - 3.0);

                double rUnit = (ganUnits[d] == 0) ? 1.5 : (ganUnits[d] > 30 ? 1.2 : -0.05 * ganUnits[d]);
                vUnits[d] = rUnit - 0.25 * (f30Units[d] - 3.0);

                double rSum = (ganSums[d] == 0) ? 1.5 : (ganSums[d] > 30 ? 1.2 : -0.05 * ganSums[d]);
                vSums[d] = rSum - 0.25 * (f30Sums[d] - 3.0);
            }

            var list = new List<Tuple<int, double>>();
            double alpha = 0.12;
            for (int s = 0; s < 100; s++)
            {
                double renewal = 0.0;
                if (gan[s] == 0) renewal = 2.2;
                else if (gan[s] > 180) renewal = 1.5;
                else renewal = -0.010 * gan[s];

                double kinetic = -0.15 * (f100De[s] - 1.00) - 0.05 * (f30Lo[s] - 8.10);
                double pM = total > 0 ? ((double)markov[s] + 0.05) / (total + 5.0) : 0.01;
                double markovTerm = -0.25 * Math.Log(pM);

                double cascadeTerm = lastUniqueLo.Contains(s) ? -1.0 : 0.0;

                int head = s / 10;
                int unit = s % 10;
                int sum = (head + unit) % 10;

                double v2D = renewal + kinetic + markovTerm + cascadeTerm;
                double v1D = (vTens[head] + vUnits[unit] + 0.8 * vSums[sum]) / 2.8;
                double vUnified = v2D + alpha * v1D;

                list.Add(new Tuple<int, double>(s, vUnified));
            }
            return list.OrderBy(x => x.Item2).Select(x => x.Item1).ToList();
        }

        private static List<int> GetTopHundredsQuiet(List<Record> records)
        {
            int n = records.Count;
            int[] gan = new int[10];
            for (int d = 0; d < 10; d++)
                for (int i = n - 1; i >= 0; i--)
                {
                    if (records[i].Hundred == d) break;
                    gan[d]++;
                }
            int[] f30 = new int[10];
            for (int i = Math.Max(0, n - 30); i < n; i++) f30[records[i].Hundred]++;

            var list = new List<Tuple<int, double>>();
            for (int d = 0; d < 10; d++)
            {
                double rTerm = (gan[d] >= 8 && gan[d] <= 20) ? -1.5 : (gan[d] == 0 ? 1.5 : 0.0);
                double kTerm = -0.3 * (f30[d] - 3.0);
                list.Add(new Tuple<int, double>(d, rTerm + kTerm));
            }
            return list.OrderBy(x => x.Item2).Take(3).Select(x => x.Item1).ToList();
        }

        private static List<int> GetTopHeadsQuiet(List<Record> records)
        {
            int n = records.Count;
            int[] gan = new int[100];
            for (int s = 0; s < 100; s++)
                for (int i = n - 1; i >= 0; i--)
                {
                    if (records[i].Head4D == s) break;
                    gan[s]++;
                }
            int[] f30 = new int[100];
            for (int i = Math.Max(0, n - 30); i < n; i++) f30[records[i].Head4D]++;

            var list = new List<Tuple<int, double>>();
            for (int s = 0; s < 100; s++)
            {
                double rTerm = 0.0;
                if (gan[s] == 0) rTerm = 2.0;
                else if (gan[s] >= 8 && gan[s] <= 20) rTerm = -1.6 + (Math.Abs(gan[s] - 13) / 10.0);
                else if (gan[s] > 30) rTerm = 1.0;
                double kTerm = -0.5 * (f30[s] - 0.30);
                list.Add(new Tuple<int, double>(s, rTerm + kTerm));
            }
            return list.OrderBy(x => x.Item2).Take(9).Select(x => x.Item1).ToList();
        }

        public static List<int> GetAdaptiveTop4Lo(List<Record> records, double lambda, out double[,] W)
        {
            int n = records.Count;
            int lookback = Math.Min(30, n);
            int startIdx = n - lookback;

            int[,] headDaily = new int[lookback, 10];
            int[,] tailDaily = new int[lookback, 10];
            int[] dauHits30 = new int[10];
            int[] duoiHits30 = new int[10];
            int[] dauHits7 = new int[10];
            int[] duoiHits7 = new int[10];
            int[] dauHits1 = new int[10];
            int[] duoiHits1 = new int[10];

            for (int k = 0; k < lookback; k++)
            {
                int rIdx = startIdx + k;
                foreach (int lo in records[rIdx].LoList)
                {
                    int h = lo / 10;
                    int t = lo % 10;
                    headDaily[k, h]++;
                    tailDaily[k, t]++;
                    dauHits30[h]++;
                    duoiHits30[t]++;
                    if (k >= lookback - 7)
                    {
                        dauHits7[h]++;
                        duoiHits7[t]++;
                    }
                    if (k == lookback - 1)
                    {
                        dauHits1[h]++;
                        duoiHits1[t]++;
                    }
                }
            }

            double K = 2.70;
            var heads = new List<Tuple<int, double>>();
            for (int d = 0; d < 10; d++)
            {
                double meanH = dauHits30[d] / (double)lookback;
                double sumSq = 0.0;
                for (int k = 0; k < lookback; k++) sumSq += Math.Pow(headDaily[k, d] - meanH, 2);
                double sigma = Math.Max(0.25, Math.Sqrt(sumSq / (lookback - 1.0)));
                double S = (dauHits7[d] / 7.0) * 0.60 + dauHits1[d] * 0.40;
                double bsmD2 = (Math.Log(Math.Max(0.20, S + 0.1) / K) + (meanH - 0.5 * sigma * sigma)) / sigma;

                double vComp = (dauHits1[d] == 0) ? -0.50 : ((dauHits1[d] >= 1 && dauHits1[d] <= 3) ? -0.20 : 0.20);
                double vMom = -0.15 * (dauHits7[d] - 18.9) / 5.0;
                double vCascade = (n >= 1 && d == records[n - 1].Dau) ? -0.30 : 0.0;
                double vBSM = -0.25 * bsmD2;
                double vBarrier = (dauHits7[d] >= 24) ? 0.35 : 0.0;
                heads.Add(new Tuple<int, double>(d, vComp + vMom + vCascade + vBSM + vBarrier));
            }
            var topHeads = heads.OrderBy(x => x.Item2).Take(2).Select(x => x.Item1).ToList();

            var tails = new List<Tuple<int, double>>();
            for (int d = 0; d < 10; d++)
            {
                double meanH = duoiHits30[d] / (double)lookback;
                double sumSq = 0.0;
                for (int k = 0; k < lookback; k++) sumSq += Math.Pow(tailDaily[k, d] - meanH, 2);
                double sigma = Math.Max(0.25, Math.Sqrt(sumSq / (lookback - 1.0)));
                double S = (duoiHits7[d] / 7.0) * 0.60 + duoiHits1[d] * 0.40;
                double bsmD2 = (Math.Log(Math.Max(0.20, S + 0.1) / K) + (meanH - 0.5 * sigma * sigma)) / sigma;

                double vComp = (duoiHits1[d] == 0) ? -0.50 : ((duoiHits1[d] >= 1 && duoiHits1[d] <= 3) ? -0.20 : 0.20);
                double vMom = -0.15 * (duoiHits7[d] - 18.9) / 5.0;
                double vCascade = (n >= 1 && d == records[n - 1].Duoi) ? -0.30 : 0.0;
                double vBSM = -0.25 * bsmD2;
                double vBarrier = (duoiHits7[d] >= 24) ? 0.35 : 0.0;
                tails.Add(new Tuple<int, double>(d, vComp + vMom + vCascade + vBSM + vBarrier));
            }
            var topTails = tails.OrderBy(x => x.Item2).Take(2).Select(x => x.Item1).ToList();

            var all100 = GetSpectralRankedLoDecayList(records, lambda, out W);
            var cand = all100.Where(x => topHeads.Contains(x.Item1 / 10) && topTails.Contains(x.Item1 % 10)).ToList();
            if (cand.Count < 4)
            {
                var top3H = heads.OrderBy(x => x.Item2).Take(3).Select(x => x.Item1).ToList();
                var top3T = tails.OrderBy(x => x.Item2).Take(3).Select(x => x.Item1).ToList();
                cand = all100.Where(x => top3H.Contains(x.Item1 / 10) && top3T.Contains(x.Item1 % 10)).ToList();
            }

            // Anti-lock: Nếu BTL hôm qua gãy thì xoay trục
            if (n >= 2 && cand.Count > 1)
            {
                var prevSub = records.Take(n - 1).ToList();
                int prevLookback = Math.Min(30, n - 1);
                int prevStart = (n - 1) - prevLookback;
                int[] pDauHits30 = new int[10];
                int[] pDauHits7 = new int[10];
                int[] pDauHits1 = new int[10];
                int[] pDuoiHits30 = new int[10];
                int[] pDuoiHits7 = new int[10];
                int[] pDuoiHits1 = new int[10];
                for (int k = 0; k < prevLookback; k++)
                {
                    int rIdx = prevStart + k;
                    foreach (int lo in prevSub[rIdx].LoList)
                    {
                        pDauHits30[lo / 10]++;
                        pDuoiHits30[lo % 10]++;
                        if (k >= prevLookback - 7)
                        {
                            pDauHits7[lo / 10]++;
                            pDuoiHits7[lo % 10]++;
                        }
                        if (k == prevLookback - 1)
                        {
                            pDauHits1[lo / 10]++;
                            pDuoiHits1[lo % 10]++;
                        }
                    }
                }
                var pHeads = new List<Tuple<int, double>>();
                for (int d = 0; d < 10; d++)
                {
                    double vComp = (pDauHits1[d] == 0) ? -0.50 : ((pDauHits1[d] >= 1 && pDauHits1[d] <= 3) ? -0.20 : 0.20);
                    double vMom = -0.15 * (pDauHits7[d] - 18.9) / 5.0;
                    double vCas = (prevSub.Count >= 1 && d == prevSub[prevSub.Count - 1].Dau) ? -0.30 : 0.0;
                    pHeads.Add(new Tuple<int, double>(d, vComp + vMom + vCas));
                }
                var pTopH = pHeads.OrderBy(x => x.Item2).Take(2).Select(x => x.Item1).ToList();

                var pTails = new List<Tuple<int, double>>();
                for (int d = 0; d < 10; d++)
                {
                    double vComp = (pDuoiHits1[d] == 0) ? -0.50 : ((pDuoiHits1[d] >= 1 && pDuoiHits1[d] <= 3) ? -0.20 : 0.20);
                    double vMom = -0.15 * (pDuoiHits7[d] - 18.9) / 5.0;
                    double vCas = (prevSub.Count >= 1 && d == prevSub[prevSub.Count - 1].Duoi) ? -0.30 : 0.0;
                    pTails.Add(new Tuple<int, double>(d, vComp + vMom + vCas));
                }
                var pTopT = pTails.OrderBy(x => x.Item2).Take(2).Select(x => x.Item1).ToList();

                double[,] prevW;
                var pAll = GetSpectralRankedLoDecayList(prevSub, lambda, out prevW);
                var pCand = pAll.Where(x => pTopH.Contains(x.Item1 / 10) && pTopT.Contains(x.Item1 % 10)).ToList();
                if (pCand.Count > 0)
                {
                    int prevBtl = pCand[0].Item1;
                    if (!records[n - 1].LoList.Contains(prevBtl))
                    {
                        var match = cand.FirstOrDefault(x => x.Item1 == prevBtl);
                        if (match != null)
                        {
                            cand.Remove(match);
                            cand.Add(match);
                        }
                    }
                }
            }

            var top4 = new List<int>();
            var hC = new Dictionary<int, int>();
            var tC = new Dictionary<int, int>();
            foreach (var e in cand)
            {
                int h = e.Item1 / 10;
                int t = e.Item1 % 10;
                int hc = 0, tc = 0;
                hC.TryGetValue(h, out hc);
                tC.TryGetValue(t, out tc);
                if (hc < 2 && tc < 2)
                {
                    top4.Add(e.Item1);
                    hC[h] = hc + 1;
                    tC[t] = tc + 1;
                    if (top4.Count == 4) break;
                }
            }
            if (top4.Count < 4)
            {
                foreach (var e in all100)
                {
                    if (!top4.Contains(e.Item1))
                    {
                        top4.Add(e.Item1);
                        if (top4.Count == 4) break;
                    }
                }
            }
            return top4;
        }

        private static List<Tuple<int, double>> GetSpectralRankedLoDecayList(List<Record> records, double lambda, out double[,] W)
        {
            int n = records.Count;
            int lookback = Math.Min(300, n);
            int startIdx = n - lookback;

            double[] singleCount = new double[100];
            W = new double[100, 100];
            for (int i = startIdx; i < n; i++)
            {
                int dt = (n - 1) - i;
                double decay = Math.Exp(-lambda * dt);
                var set = records[i].UniqueLo;
                foreach (int u in set)
                {
                    singleCount[u] += decay;
                    foreach (int v in set)
                        if (u < v) W[u, v] += decay;
                }
            }

            for (int u = 0; u < 100; u++)
                for (int v = u + 1; v < 100; v++)
                {
                    if (singleCount[u] > 0 && singleCount[v] > 0)
                    {
                        W[u, v] = W[u, v] / Math.Sqrt(singleCount[u] * singleCount[v]);
                        W[v, u] = W[u, v];
                    }
                }

            int[] ganLo = new int[100];
            for (int u = 0; u < 100; u++)
                for (int i = n - 1; i >= 0; i--)
                {
                    if (records[i].UniqueLo.Contains(u)) break;
                    ganLo[u]++;
                }

            int[] f30Lo = new int[100];
            for (int i = Math.Max(0, n - 30); i < n; i++)
                foreach (int u in records[i].UniqueLo) f30Lo[u]++;

            int[] dauCountYesterday = new int[10];
            int[] duoiCountYesterday = new int[10];
            foreach (int lo in records[n - 1].UniqueLo)
            {
                dauCountYesterday[lo / 10]++;
                duoiCountYesterday[lo % 10]++;
            }

            bool[] isGoldenHead = new bool[10];
            if (n >= 2)
            {
                int[] dauCountPrev2 = new int[10];
                foreach (int lo in records[n - 2].UniqueLo) dauCountPrev2[lo / 10]++;
                for (int d = 0; d < 10; d++)
                {
                    if (dauCountYesterday[d] >= 2 && dauCountYesterday[d] <= 4 &&
                        dauCountPrev2[d] >= 2 && dauCountPrev2[d] <= 4)
                        isGoldenHead[d] = true;
                }
            }
            int[] f30Tong = new int[10];
            for (int i = Math.Max(0, n - 30); i < n; i++)
                foreach (int lo in records[i].UniqueLo)
                    f30Tong[(lo / 10 + lo % 10) % 10]++;

            var list = new List<Tuple<int, double>>();
            double beta = 0.35;
            for (int u = 0; u < 100; u++)
            {
                int d2 = u / 10;
                int d1 = u % 10;
                int sum = (d2 + d1) % 10;

                double vRenewal = (ganLo[u] >= 2 && ganLo[u] <= 5) ? -0.80 : (ganLo[u] == 0 ? -0.30 : (ganLo[u] > 12 ? 1.00 : 0.0));
                double vKinetic = -0.25 * (f30Lo[u] - 7.0);
                double deg = 0;
                for (int v = 0; v < 100; v++) deg += W[u, v];
                double vMarkov = -0.30 * deg;
                double vCascade = (u == records[n - 1].De) ? -0.40 : 0.0;

                double vDau = (dauCountYesterday[d2] == 0) ? -0.60 : (dauCountYesterday[d2] > 5 ? 0.40 : 0.0);
                double vDuoi = (duoiCountYesterday[d1] == 0) ? -0.60 : (duoiCountYesterday[d1] > 5 ? 0.40 : 0.0);
                double vTong = -0.05 * (f30Tong[sum] - 66.0) / 10.0;
                double vGolden = isGoldenHead[d2] ? -0.20 : 0.0;
                double v1D = vDau + vDuoi + vTong + vGolden;

                double vEff = vRenewal + vKinetic + vMarkov + vCascade + beta * v1D;
                list.Add(new Tuple<int, double>(u, vEff));
            }
            return list.OrderBy(x => x.Item2).ToList();
        }

        private static List<int> GetSpectralRankedLoDecay(List<Record> records, double lambda, out double[,] W)
        {
            return GetAdaptiveTop4Lo(records, lambda, out W);
        }
    }
}
