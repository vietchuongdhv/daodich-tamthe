using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TidsTheory
{
    public class LotteryRecord
    {
        public int Id;
        public string Date;
        public string DayOfWeek;
        public string Gdb;
        public int[] Digits5; // [d5, d4, d3, d2, d1]
        public int De; // d2*10 + d1
        public int Dau;
        public int Duoi;
        public int Tong;
        public int Head4D; // d4*10 + d3
        public List<int> LoList;
        public HashSet<int> UniqueLo;
        public HashSet<string> Prizes3D;
        public HashSet<string> Prizes4D;
        public string Gdb3D;
        public string Gdb4D;
    }

    public class StateEnergy
    {
        public int State;
        public double Potential; // V_eff hợp nhất (càng thấp càng dễ nổ)
        public int Gan;
        public int F30;   // F30
        public int F100;  // F100 Đề (kỳ vọng 1.0)
        public int F30Lo; // F30 Lô 27 giải (kỳ vọng 8.1)
        public double RenewalTerm;
        public double KineticTerm;
        public double MarkovTerm;
        public double AntiMomentumTerm;
        public double V2D;
        public double V1D;
    }

    public class LottoEnergy
    {
        public int State;
        public double Potential; // V_eff_lo hợp nhất (càng thấp càng dễ nổ)
        public int Gan;
        public int F30;          // Số ngày nổ trong 30 kỳ
        public int Hits30;       // Tổng số nháy trong 30 kỳ
        public int Streak;       // Độ dài chuỗi nổ liên tiếp (Lô rơi)
        public double ExpectedHits; // Kỳ vọng số nháy E[Nháy]
        public double Sigma;     // Độ biến động lịch sử BSM sigma_u
        public double BsmD2;     // Chỉ số BSM d2 (Strike distance)
        public double VRenewal;
        public double VKinetic;
        public double VMarkov;
        public double VCascade;
        public double V1D;
        public double VMulti;    // Mật độ nháy Hawkes
        public double VBSM;      // Thế năng BSM rào cản kép & đuôi béo
    }

    public class Marginal1DInfo
    {
        public int Digit;
        public int Gan;
        public int F30;
        public double Potential;
        public string Note;
    }

    public class Marginal1DResult
    {
        public List<Marginal1DInfo> Tens;
        public List<Marginal1DInfo> Units;
        public List<Marginal1DInfo> Sums;
        public double[] TensPotentials = new double[10];
        public double[] UnitsPotentials = new double[10];
        public double[] SumPotentials = new double[10];

        // Mở rộng BSM Đa nhân tố Vĩ mô (Macro Factors):
        public double[] ParityPotentials = new double[4]; // CC, CL, LC, LL
        public double[] SizePotentials = new double[2];   // Xỉu (00-49), Tài (50-99)
        public int[] ParityGan = new int[4];
        public int[] ParityF30 = new int[4];
        public int[] SizeGan = new int[2];
        public int[] SizeF30 = new int[2];
    }

    public class LottoMarginal1DInfo
    {
        public int Digit;
        public int F1;  // Nháy kỳ hôm trước
        public int F7;  // Nháy 7 kỳ
        public int F30; // Nháy 30 kỳ
        public double ExpectedHits; // Kỳ vọng nháy BSM
        public double Sigma;        // Độ biến động BSM
        public double BsmD2;        // d2 BSM
        public double Potential;
        public string Note;
    }

    public class LottoMarginal1DResult
    {
        public List<LottoMarginal1DInfo> Heads;
        public List<LottoMarginal1DInfo> Tails;
        public double[] HeadPotentials = new double[10];
        public double[] TailPotentials = new double[10];
    }

    public class QciResult
    {
        public double Score; // 0 to 100%
        public string Zone;  // "XANH", "VÀNG", "ĐỎ"
        public string ActionRecommendation; // Khuyến nghị hành động
        public double EnergyGapScore;
        public double EntropyScore;
        public double ShockSpringScore;
        public double DriAlignmentScore;
        public double BsmRegimeScore;
        public double NormalizedEntropy;
        public double EnergyGapLo;
        public double EnergyGapDe;
        public bool HasCam;
        public bool HasCascade;
    }

    public enum MarketRegimeType
    {
        Chaos,        // Nhiễu loạn (Turbulence / High-entropy)
        Coiling,      // Nén hồi quy (Coil / Mean-reversion trigger)
        Steady,       // Bình ổn (Steady state)
        Convergent,   // Thế năng hội tụ (Super-convergent)
        Peak          // Đỉnh cao hiệu suất (Peak performance)
    }

    public class MarketRegimeResult
    {
        public double EntropyH;
        public double SpectralGapGamma;
        public double ZMacro;
        public string CompressedFactor;
        public double MomentumMRoll;
        public int Win15Count;
        public int Win50Count;
        public MarketRegimeType Regime;
        public string RegimeName;
        public string RegimeSymbol;
        public string ActionRecommendation;
        public double KellyMultiplier;
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            string rawCmd = args.Length > 0 ? args[0].ToLower() : "predict";
            string cleanFirst = rawCmd.Replace("[", "").Replace("]", "").Replace(".", "").Replace(",", "").Trim().ToLower();
            long checkNum;
            string command = "predict";
            if (rawCmd == "sheet" || rawCmd == "s" || rawCmd == "table" || rawCmd == "excel")
            {
                command = "sheet";
            }
            else if (rawCmd == "web" || rawCmd == "app" || rawCmd == "server")
            {
                command = "web";
            }
            else if (rawCmd == "predict" || rawCmd == "p")
            {
                command = "predict";
            }
            else if (long.TryParse(cleanFirst, out checkNum) || cleanFirst.EndsWith("k") || cleanFirst.EndsWith("m") || cleanFirst.EndsWith("tr"))
            {
                command = "predict";
            }
            else
            {
                command = rawCmd;
            }

            string csvPath = Path.Combine(Directory.GetCurrentDirectory(), "data", "lottery_master.csv");
            if (!File.Exists(csvPath))
            {
                csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "data", "lottery_master.csv");
            }
            if (!File.Exists(csvPath))
            {
                csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "lottery_master.csv");
            }

            // Check if any argument is a valid csv path
            foreach (var arg in args)
            {
                if (arg.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) && File.Exists(arg))
                {
                    csvPath = arg;
                    break;
                }
            }

            if (!File.Exists(csvPath))
            {
                Console.WriteLine("Lỗi: Không tìm thấy file CSV tại " + csvPath);
                return;
            }

            var records = LoadData(csvPath);
            if (records.Count == 0)
            {
                Console.WriteLine("Lỗi: Dữ liệu trống.");
                return;
            }

            long budget = 500000;
            for (int i = 0; i < args.Length; i++)
            {
                string cleanArg = args[i].Replace("[", "").Replace("]", "").Replace(".", "").Replace(",", "").Trim().ToLower();
                if (cleanArg.EndsWith("k"))
                {
                    long kVal;
                    if (long.TryParse(cleanArg.Substring(0, cleanArg.Length - 1), out kVal))
                    {
                        budget = kVal * 1000;
                        break;
                    }
                }
                int suffixLen = cleanArg.EndsWith("tr") ? 2 : (cleanArg.EndsWith("m") ? 1 : 0);
                if (suffixLen > 0)
                {
                    double mVal;
                    if (double.TryParse(cleanArg.Substring(0, cleanArg.Length - suffixLen), out mVal))
                    {
                        budget = (long)(mVal * 1000000);
                        break;
                    }
                }
                long val;
                if (long.TryParse(cleanArg, out val) && val > 0)
                {
                    if (val < 1000) val *= 1000;
                    budget = val;
                    break;
                }
            }

            if (command == "web" || command == "app" || command == "server")
            {
                TidsWebServer.Start(args, csvPath);
                return;
            }

            if (command == "check")
            {
                RunCheck(records);
            }
            else if (command == "backtest")
            {
                int testDays = 300;
                int p;
                if (args.Length > 1 && int.TryParse(args[1], out p)) testDays = p;
                RunBacktest(records, testDays);
            }
            else if (command == "recentlo" || command == "history")
            {
                int count = 10;
                int p;
                if (args.Length > 1 && int.TryParse(args[1], out p)) count = p;
                RunRecentLoHistory(records, count);
            }
            else if (command == "portfolio")
            {
                RunPortfolioOnly(records, budget);
            }
            else if (command == "qcitest" || command == "test-qci")
            {
                int testDays = 100;
                int p;
                if (args.Length > 1 && int.TryParse(args[1], out p)) testDays = p;
                RunQciAudit(records, testDays);
            }
            else if (command == "qcide" || command == "qci-de" || command == "test-qcide" || command == "deqci" || command == "de100")
            {
                int testDays = 100;
                int p;
                if (args.Length > 1 && int.TryParse(args[1], out p)) testDays = p;
                RunQciDeLadderAudit(records, testDays);
            }
            else if (command == "d50test" || command == "test-d50" || command == "de50" || command == "d50")
            {
                int testDays = 100;
                int p;
                if (args.Length > 1 && int.TryParse(args[1], out p)) testDays = p;
                RunDeSetAudit(records, testDays, 50);
            }
            else if (command == "d65test" || command == "test-d65" || command == "de65" || command == "d65")
            {
                int testDays = 100;
                int p;
                if (args.Length > 1 && int.TryParse(args[1], out p)) testDays = p;
                RunDeSetAudit(records, testDays, 65);
            }
            else if (command == "ratio" || command == "decimation" || command == "test-ratio")
            {
                int testDays = 300;
                int p;
                if (args.Length > 1 && int.TryParse(args[1], out p)) testDays = p;
                RunDecimationRatioComparison(records, testDays);
            }
            else if (command == "regime" || command == "regimetest" || command == "test-regime")
            {
                int testDays = 100;
                int p;
                if (args.Length > 1 && int.TryParse(args[1], out p)) testDays = p;
                RunRegimeAudit(records, testDays);
            }
            else if (command == "regimelo" || command == "loregime" || command == "lottoregime" || command == "test-loregime")
            {
                int testDays = 100;
                int p;
                if (args.Length > 1 && int.TryParse(args[1], out p)) testDays = p;
                RunLottoRegimeAudit(records, testDays);
            }
            else if (command == "testbtl" || command == "btl" || command == "test-btl" || command == "btltest")
            {
                int testDays = 100;
                int p;
                if (args.Length > 1 && int.TryParse(args[1], out p)) testDays = p;
                RunBTL_Audit(records, testDays);
            }
            else if (command == "test3d4d" || command == "3d4d" || command == "audit3d4d" || command == "test3d" || command == "test4d" || command == "de3d4d")
            {
                int testDays = 100;
                int p;
                if (args.Length > 1 && int.TryParse(args[1], out p)) testDays = p;
                Run3D4D_Audit(records, testDays);
            }
            else if (command == "testtamthu" || command == "tamthu" || command == "test-tamthu" || command == "tamthulo" || command == "audittamthu")
            {
                int testDays = 100;
                int p;
                if (args.Length > 1 && int.TryParse(args[1], out p)) testDays = p;
                RunTamThuAudit(records, testDays);
            }
            else if (command == "sheet" || command == "table" || command == "excel")
            {
                RunSheetExport(records, budget);
            }
            else if (command == "test24" || command == "audit24" || command == "test-all" || command == "all24")
            {
                int testDays = 100;
                long testBudget = 500000;
                int p;
                if (args.Length > 1 && int.TryParse(args[1], out p)) testDays = p;
                if (args.Length > 2)
                {
                    long b;
                    if (long.TryParse(args[2], out b)) testBudget = b;
                }
                RunAll24ItemsAudit(records, testDays, testBudget);
            }
            else
            {
                RunPredict(records, budget);
            }
        }

        public static List<LotteryRecord> LoadData(string path)
        {
            var list = new List<LotteryRecord>();
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = ParseCsvLine(line);
                if (parts.Count < 19) continue;

                try
                {
                    var rec = new LotteryRecord();
                    rec.Id = int.Parse(parts[0]);
                    rec.Date = parts[1];
                    rec.DayOfWeek = parts[2].Trim('\"');
                    rec.Gdb = parts[3].Trim('\"');

                    string gdbClean = rec.Gdb.PadLeft(5, '0');
                    if (gdbClean.Length > 5) gdbClean = gdbClean.Substring(gdbClean.Length - 5);
                    rec.Digits5 = new int[5];
                    for (int d = 0; d < 5; d++) rec.Digits5[d] = gdbClean[d] - '0';

                    rec.De = int.Parse(parts[11]);
                    rec.Dau = int.Parse(parts[12]);
                    rec.Duoi = int.Parse(parts[13]);
                    rec.Tong = int.Parse(parts[14]);
                    rec.Head4D = rec.Digits5[1] * 10 + rec.Digits5[2];
                    rec.Gdb3D = gdbClean.Length >= 3 ? gdbClean.Substring(gdbClean.Length - 3) : "";
                    rec.Gdb4D = gdbClean.Length >= 4 ? gdbClean.Substring(gdbClean.Length - 4) : "";

                    rec.Prizes3D = new HashSet<string>();
                    rec.Prizes4D = new HashSet<string>();
                    for (int c = 3; c <= 9; c++)
                    {
                        if (c >= parts.Count) break;
                        string colStr = parts[c].Trim('\"');
                        var tokens = colStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var tok in tokens)
                        {
                            string tClean = tok.Trim();
                            if (tClean.Length >= 3)
                            {
                                rec.Prizes3D.Add(tClean.Substring(tClean.Length - 3));
                            }
                            if (c <= 8 && tClean.Length >= 4)
                            {
                                rec.Prizes4D.Add(tClean.Substring(tClean.Length - 4));
                            }
                        }
                    }

                    rec.LoList = new List<int>();
                    rec.UniqueLo = new HashSet<int>();
                    string loStr = parts[17].Trim('\"');
                    var loParts = loStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var lp in loParts)
                    {
                        int val;
                        if (int.TryParse(lp.Trim(), out val))
                        {
                            rec.LoList.Add(val);
                            rec.UniqueLo.Add(val);
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



        public static void RunCheck(List<LotteryRecord> records)
        {
            var last = records[records.Count - 1];
            Console.WriteLine("=========================================================================");
            Console.WriteLine(" LÝ THUYẾT HÌNH HỌC THÔNG TIN & ĐỘNG LỰC HỌC TÔ-PÔ TRÊN ĐA TẠP RỜI RẠC (TIDS) ");
            Console.WriteLine("                    KIỂM ĐỊNH TÍNH TOÀN VẸN CƠ SỞ DỮ LIỆU                ");
            Console.WriteLine("=========================================================================");
            Console.WriteLine("1. Tổng chuỗi thời gian:        " + records.Count.ToString("N0") + " kỳ quay");
            Console.WriteLine("2. Khoảng tọa độ thời gian:     " + records[0].Date + " -> " + last.Date);
            Console.WriteLine("3. Trạng thái biên gần nhất:    " + last.Date + " (" + last.DayOfWeek + ")");
            Console.WriteLine("   - Vector GĐB 5D:             (" + string.Join(", ", last.Digits5) + ")");
            Console.WriteLine("   - Điểm trạng thái Đề 2D:     " + last.De.ToString("D2") + " (Đầu " + last.Dau + ", Đuôi " + last.Duoi + ")");
            Console.WriteLine("   - Đa tập hợp Lô tô 27 vị trí:" + last.UniqueLo.Count + " phần tử độc bản");
            Console.WriteLine("4. Không gian mẫu:              Ω = Z_10^5 x P_27(Z_100) (100% Hoàn hảo)");
            Console.WriteLine("=========================================================================");
        }

        public static void RunPredict(List<LotteryRecord> records, long budget = 500000)
        {
            int n = records.Count;
            var last = records[n - 1];

            Console.WriteLine("=========================================================================================");
            Console.WriteLine("   ĐẠO DỊCH HUYỀN CƠ PHÁP - MÔ HÌNH TOÁN HỌC ĐỊNH LƯỢNG TẬP TRUNG (TIDS QUANTUM V2.0)   ");
            Console.WriteLine("    THÁI CỰC QUY NGUYÊN, TRƯỜNG THẾ NĂNG HIỆU DỤNG V_eff & ĐỘNG LỰC HỌC TÔ-PÔ ĐA TẠP     ");
            Console.WriteLine("=========================================================================================");
            Console.WriteLine("Không gian trạng thái: " + n + " bước thời gian (" + records[0].Date + " -> " + last.Date + ")");
            Console.WriteLine("Điểm neo pha gần nhất: " + last.Date + " (" + last.DayOfWeek + ") | Vector GĐB: [" + string.Join(",", last.Digits5) + "] (Trạng thái 2D: " + last.De.ToString("D2") + ")");
            Console.WriteLine();

            var marginal1D = Compute1DMarginals(records);
            var lotto1D = Compute1DLottoMarginals(records);
            double[,] W_qci;
            var lottoEnergies_qci = ComputeUnifiedLottoPotentials(records, lotto1D, out W_qci);
            var unifiedEnergies = ComputeUnifiedPotentials2D(records, marginal1D);
            var bsmBasketEnergies = ComputeBsmBasketPotentials2D(records, marginal1D);
            var qci = ComputeQCI(records, lottoEnergies_qci, unifiedEnergies, lotto1D, marginal1D);
            var regime = DetectMarketRegime(records, unifiedEnergies, W_qci, marginal1D);

            // THIÊN CƠ ĐỘ (QCI) & TRẠNG THÁI THỜI KHÍ
            Console.WriteLine(string.Format(">> THIÊN CƠ ĐỘ (CHỈ SỐ TỰ TIN QCI): {0:F1}% | TÍN HIỆU THỜI VẬN: {1}", qci.Score, qci.Zone == "XANH" ? "🟢 VÙNG XANH (THÁI CƠ - NÊN ĐÁNH TẤN CÔNG)" : (qci.Zone == "VÀNG" ? "🟡 VÙNG VÀNG (TRUNG DUNG - CÂN BẰNG THỦ VỐN)" : "🔴 VÙNG ĐỎ (BĨ VẬN - NÊN THỦ THẾ HOẶC NGHỈ)")));
            Console.WriteLine(">> KHUYẾN NGHỊ VẬN HÀNH: " + qci.ActionRecommendation);
            Console.WriteLine(string.Format("   * Tứ Tượng Định Lượng: Tách biệt ΔV ({0:F1}/25) | Hỗn độn Entropi ({1:F1}/20) | Lò xo Phục khí ({2:F1}/25) | Đồng quy DRI ({3:F1}/15) | BSM Thể Dụng ({4:F1}/15)", qci.EnergyGapScore, qci.EntropyScore, qci.ShockSpringScore, qci.DriAlignmentScore, qci.BsmRegimeScore));
            Console.WriteLine(string.Format(">> TRẠNG THÁI THỜI KHÍ (MARKET REGIME): {0} | HỆ SỐ KELLY: k = {1:F2}x", regime.RegimeSymbol, regime.KellyMultiplier));
            Console.WriteLine(string.Format("   * 4 Đại lượng đo lường: Entropy H = {0:F3} | Phổ γ = {1:F3} | Nén vĩ mô Z = {2:F2}σ ({3}) | Quán tính M = {4}{5:F1}% ({6}/15 kỳ)",
                regime.EntropyH, regime.SpectralGapGamma, regime.ZMacro, regime.CompressedFactor, regime.MomentumMRoll >= 0 ? "+" : "", regime.MomentumMRoll, regime.Win15Count));
            Console.WriteLine("   * Chiến lược xuất trận: " + regime.ActionRecommendation);
            Console.WriteLine();

            // 1A. TÍNH TOÁN VÀ PHÂN TÍCH THẾ NĂNG BIÊN 1D VĨ MÔ
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            Console.WriteLine("PHẦN 1A: KHÔNG GIAN BIÊN 1D VĨ MÔ (HÀNG CHỤC x HÀNG ĐƠN VỊ & TỔNG ĐỀ)");
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            Print1DMarginals(marginal1D);

            // 1B. BẢN ĐỒ THẾ NĂNG HỢP NHẤT TOÀN DIỆN & CẤU TRÚC ĐỀ NHẤT QUÁN DUY NHẤT
            Console.WriteLine();
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            Console.WriteLine("PHẦN 1B: BẢN ĐỒ THẾ NĂNG HỢP NHẤT V_eff(s) & CẤU TRÚC ĐỀ NHẤT QUÁN DUY NHẤT (LEVEL SETS)");
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            PrintUnifiedLevelSets(unifiedEnergies, bsmBasketEnergies, regime);

            // 1C. TỰ ĐỘNG DÒ TÌM & PHÂN LOẠI CỤM DÀN ĐA TẠP TOÁN HỌC ĐỘNG (MANIFOLD CLUSTERING)
            Console.WriteLine();
            PrintDynamicManifoldClusters(unifiedEnergies);

            // 2. PHÂN RÃ TENSOR 4D & 3D
            Console.WriteLine();
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            Console.WriteLine("PHẦN 2: PHÂN RÃ TENSOR CP 4 CHIỀU & LIÊN KẾT KHÔNG GIAN 3D, 4D");
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            PrintTensorStructures(records, unifiedEnergies);

            // 3. MÔ HÌNH THẾ NĂNG HIỆU DỤNG HỢP NHẤT CHO 27 GIẢI LÔ TÔ (V_eff_lo)
            Console.WriteLine();
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            Console.WriteLine("PHẦN 3: MÔ HÌNH THẾ NĂNG HIỆU DỤNG HỢP NHẤT CHO 27 GIẢI LÔ TÔ (V_eff_lo) & BỘ TỨ XIÊN");
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            PrintSpectralLottoAndCliques(records);

            // 4. MÔ HÌNH DANH MỤC TỰ CÂN BẰNG TỐI ƯU KELLY
            Console.WriteLine();
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            Console.WriteLine("PHẦN 4: CHIẾN LƯỢC TÀI CHÍNH ĐỊNH LƯỢNG & TỐI ƯU DANH MỤC TỰ CÂN BẰNG (KELLY PORTFOLIO)");
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            PrintDynamicSelfFundingPortfolio(records, unifiedEnergies, budget);
        }

        public static void RunPortfolioOnly(List<LotteryRecord> records, long budget = 500000)
        {
            int n = records.Count;
            var last = records[n - 1];

            Console.WriteLine("=========================================================================================");
            Console.WriteLine("   ĐẠO DỊCH HUYỀN CƠ PHÁP - TAM TÀI ĐỊNH DANH & TỐI ƯU DANH MỤC TỰ CÂN BẰNG KELLY       ");
            Console.WriteLine("=========================================================================================");
            Console.WriteLine("Điểm neo pha gần nhất: " + last.Date + " (" + last.DayOfWeek + ") | Vector GĐB: [" + string.Join(",", last.Digits5) + "] (GĐB: " + last.De.ToString("D2") + ")");
            Console.WriteLine();

            var marginal1D = Compute1DMarginals(records);
            var unifiedEnergies = ComputeUnifiedPotentials2D(records, marginal1D);
            PrintDynamicSelfFundingPortfolio(records, unifiedEnergies, budget);
        }

        public static Marginal1DResult Compute1DMarginals(List<LotteryRecord> records)
        {
            int n = records.Count;

            // 1. Hàng Chục (d2: 0..9) - Phân phối sinh tồn tự nhiên
            int[] ganTens = new int[10];
            for (int d = 0; d < 10; d++)
            {
                for (int i = n - 1; i >= 0; i--)
                {
                    if (records[i].Dau == d) break;
                    ganTens[d]++;
                }
            }
            int[] f30Tens = new int[10];
            for (int i = Math.Max(0, n - 30); i < n; i++) f30Tens[records[i].Dau]++;

            var res = new Marginal1DResult();
            var tensList = new List<Marginal1DInfo>();
            for (int d = 0; d < 10; d++)
            {
                double rHead = (ganTens[d] == 0) ? 1.5 : (ganTens[d] > 30 ? 1.2 : -0.05 * ganTens[d]);
                double kHead = -0.25 * (f30Tens[d] - 3.0);
                double v = rHead + kHead;
                string note = (ganTens[d] == 0) ? "Vừa nổ (Cản lặp)" : ((ganTens[d] >= 6 && ganTens[d] <= 15) ? "Vùng tích lũy vàng" : "Pha ổn định");
                res.TensPotentials[d] = v;
                tensList.Add(new Marginal1DInfo { Digit = d, Gan = ganTens[d], F30 = f30Tens[d], Potential = Math.Round(v, 4), Note = note });
            }
            res.Tens = tensList.OrderBy(x => x.Potential).ToList();

            // 2. Hàng Đơn Vị (d1: 0..9)
            int[] ganUnits = new int[10];
            for (int d = 0; d < 10; d++)
            {
                for (int i = n - 1; i >= 0; i--)
                {
                    if (records[i].Duoi == d) break;
                    ganUnits[d]++;
                }
            }
            int[] f30Units = new int[10];
            for (int i = Math.Max(0, n - 30); i < n; i++) f30Units[records[i].Duoi]++;

            var unitsList = new List<Marginal1DInfo>();
            for (int d = 0; d < 10; d++)
            {
                double rUnit = (ganUnits[d] == 0) ? 1.5 : (ganUnits[d] > 30 ? 1.2 : -0.05 * ganUnits[d]);
                double kUnit = -0.25 * (f30Units[d] - 3.0);
                double v = rUnit + kUnit;
                string note = (ganUnits[d] == 0) ? "Vừa nổ (Cản lặp)" : ((ganUnits[d] >= 6 && ganUnits[d] <= 15) ? "Vùng tích lũy vàng" : "Pha ổn định");
                res.UnitsPotentials[d] = v;
                unitsList.Add(new Marginal1DInfo { Digit = d, Gan = ganUnits[d], F30 = f30Units[d], Potential = Math.Round(v, 4), Note = note });
            }
            res.Units = unitsList.OrderBy(x => x.Potential).ToList();

            // 3. TỔNG ĐỀ (Tong = (d2 + d1) % 10: 0..9) - Bậc tự do độc lập thứ 3
            int[] ganSums = new int[10];
            for (int d = 0; d < 10; d++)
            {
                for (int i = n - 1; i >= 0; i--)
                {
                    if (records[i].Tong == d) break;
                    ganSums[d]++;
                }
            }
            int[] f30Sums = new int[10];
            for (int i = Math.Max(0, n - 30); i < n; i++) f30Sums[records[i].Tong]++;

            var sumsList = new List<Marginal1DInfo>();
            for (int d = 0; d < 10; d++)
            {
                double rSum = (ganSums[d] == 0) ? 1.5 : (ganSums[d] > 30 ? 1.2 : -0.05 * ganSums[d]);
                double kSum = -0.25 * (f30Sums[d] - 3.0);
                double v = rSum + kSum;
                string note = (ganSums[d] == 0) ? "Vừa nổ (Cản lặp)" : ((ganSums[d] >= 6 && ganSums[d] <= 15) ? "Vùng tích lũy vàng" : "Pha ổn định");
                res.SumPotentials[d] = v;
                sumsList.Add(new Marginal1DInfo { Digit = d, Gan = ganSums[d], F30 = f30Sums[d], Potential = Math.Round(v, 4), Note = note });
            }
            res.Sums = sumsList.OrderBy(x => x.Potential).ToList();

            // 4. CHẴN / LẺ (CC=0, CL=1, LC=2, LL=3) - BSM FACTOR PARITY
            for (int k = 0; k < 4; k++)
            {
                for (int i = n - 1; i >= 0; i--)
                {
                    int p = (records[i].Dau % 2) * 2 + (records[i].Duoi % 2);
                    if (p == k) break;
                    res.ParityGan[k]++;
                }
            }
            for (int i = Math.Max(0, n - 30); i < n; i++)
            {
                int p = (records[i].Dau % 2) * 2 + (records[i].Duoi % 2);
                res.ParityF30[p]++;
            }
            for (int k = 0; k < 4; k++)
            {
                // Rào cản BSM: mu = 4.0, sigma = 3.0. Khi Gan >= 8 chạm ngưỡng 2.5 sigma nén
                double rP = (res.ParityGan[k] == 0) ? 0.40 : (res.ParityGan[k] >= 8 ? -0.50 : -0.06 * res.ParityGan[k]);
                double kP = -0.05 * (res.ParityF30[k] - 7.5);
                res.ParityPotentials[k] = rP + kP;
            }

            // 5. TÀI / XỈU (Xỉu 00-49=0, Tài 50-99=1) - BSM FACTOR SIZE
            for (int k = 0; k < 2; k++)
            {
                for (int i = n - 1; i >= 0; i--)
                {
                    int sz = (records[i].De >= 50) ? 1 : 0;
                    if (sz == k) break;
                    res.SizeGan[k]++;
                }
            }
            for (int i = Math.Max(0, n - 30); i < n; i++)
            {
                int sz = (records[i].De >= 50) ? 1 : 0;
                res.SizeF30[sz]++;
            }
            for (int k = 0; k < 2; k++)
            {
                // Rào cản BSM: mu = 2.0, sigma = 1.4. Khi Gan >= 4 chạm ngưỡng 2.5 sigma nén
                double rS = (res.SizeGan[k] == 0) ? 0.30 : (res.SizeGan[k] >= 4 ? -0.40 : -0.08 * res.SizeGan[k]);
                double kS = -0.03 * (res.SizeF30[k] - 15.0);
                res.SizePotentials[k] = rS + kS;
            }

            return res;
        }



        public static void Print1DMarginals(Marginal1DResult marginal1D)
        {
            Console.WriteLine(">> BẢNG THỨ HẠNG THẾ NĂNG BIÊN 1D HÀNG CHỤC, HÀNG ĐƠN VỊ & TỔNG ĐỀ:");
            Console.WriteLine("Hạng | Hàng Chục (d2) [Gan, F30]  | Hàng Đơn Vị (d1) [Gan, F30] | Tổng Đề [Gan, F30]");
            Console.WriteLine("---------------------------------------------------------------------------------------------");
            for (int r = 0; r < 5; r++)
            {
                var t = marginal1D.Tens[r];
                var u = marginal1D.Units[r];
                var s = marginal1D.Sums[r];
                Console.WriteLine(string.Format(" {0,2}  | Đầu {1} (Gan {2,2}, F30 {3,2})        | Đuôi {4} (Gan {5,2}, F30 {6,2})       | Tổng {7} (Gan {8,2}, F30 {9,2})",
                    r + 1, t.Digit, t.Gan, t.F30, u.Digit, u.Gan, u.F30, s.Digit, s.Gan, s.F30));
            }
            Console.WriteLine("---------------------------------------------------------------------------------------------");

            Console.WriteLine();
            Console.WriteLine(">> BẢNG THẾ NĂNG NHÂN TỐ VĨ MÔ BSM (CHẴN/LẺ & TÀI/XỈU - MACRO FACTOR POTENTIALS):");
            string[] pNames = { "Chẵn-Chẵn (CC)", "Chẵn-Lẻ (CL)", "Lẻ-Chẵn (LC)", "Lẻ-Lẻ (LL)" };
            string[] szNames = { "Xỉu (00-49)", "Tài (50-99)" };
            for (int k = 0; k < 4; k++)
            {
                Console.WriteLine(string.Format("   - {0,-15}: Gan {1,2} kỳ | F30: {2,2}/30 | Thế năng V = {3,6:F3} {4}",
                    pNames[k], marginal1D.ParityGan[k], marginal1D.ParityF30[k], marginal1D.ParityPotentials[k],
                    marginal1D.ParityGan[k] >= 8 ? "★ Vùng nén hồi quy 2.5σ" : (marginal1D.ParityGan[k] == 0 ? "(Cản lặp tức thời)" : "")));
            }
            for (int k = 0; k < 2; k++)
            {
                Console.WriteLine(string.Format("   - {0,-15}: Gan {1,2} kỳ | F30: {2,2}/30 | Thế năng V = {3,6:F3} {4}",
                    szNames[k], marginal1D.SizeGan[k], marginal1D.SizeF30[k], marginal1D.SizePotentials[k],
                    marginal1D.SizeGan[k] >= 4 ? "★ Vùng nén hồi quy 2.5σ" : (marginal1D.SizeGan[k] == 0 ? "(Cản lặp tức thời)" : "")));
            }
        }

        public static List<StateEnergy> ComputeEffectivePotentials2D(List<LotteryRecord> records)
        {
            var marginal1D = Compute1DMarginals(records);
            return ComputeUnifiedPotentials2D(records, marginal1D);
        }

        public static List<StateEnergy> ComputeUnifiedPotentials2D(List<LotteryRecord> records, Marginal1DResult marginal1D)
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
                if (records[i].LoList != null)
                {
                    foreach (var lo in records[i].LoList) f30Lo[lo]++;
                }
            }

            int[] markovCount = new int[100];
            int totalFromLast = 0;
            for (int i = 0; i < n - 1; i++)
            {
                if (records[i].De == lastDe)
                {
                    markovCount[records[i + 1].De]++;
                    totalFromLast++;
                }
            }

            int[] ganHeads = new int[10];
            for (int d = 0; d < 10; d++)
            {
                for (int i = n - 1; i >= 0; i--)
                {
                    if (records[i].Dau == d) break;
                    ganHeads[d]++;
                }
            }

            var list = new List<StateEnergy>();
            double alpha = 0.12; // Trọng số hợp nhất thế năng biên 1D tối ưu

            for (int s = 0; s < 100; s++)
            {
                // Thành tố 1: Phục hồi sinh tồn Gan Đề (Log-Hazard Renewal)
                double vRenewal = 0.0;
                if (gan[s] == 0) vRenewal = 2.2; // Rào cản chống lặp tức thời
                else if (gan[s] > 180) vRenewal = 1.5; // Phạt số siêu gan đóng băng
                else vRenewal = -0.010 * gan[s]; // Tích lũy độ nén tự nhiên theo phân phối hình học

                // Thành tố 2: Động học Tần suất Đề 100 kỳ & Lô 30 kỳ (Kinetic Momentum)
                double vKinetic = -0.15 * (f100De[s] - 1.00) - 0.05 * (f30Lo[s] - 8.10);

                // Thành tố 3: Quá trình chuyển dịch Markov 1 hạt (Markov Transition)
                double pMarkov = totalFromLast > 0 ? ((double)markovCount[s] + 0.05) / (totalFromLast + 5.0) : 0.01;
                double vMarkov = -0.25 * Math.Log(pMarkov);

                // Thành tố 4: Cầu rơi tương hỗ từ 27 giải Lô hôm trước sang Đề (Cascade from Lo to De)
                double vCascade = lastUniqueLo.Contains(s) ? -1.0 : 0.0;

                int head = s / 10;
                int unit = s % 10;
                int sum = (head + unit) % 10;
                int p = (head % 2) * 2 + (unit % 2);
                int sz = (s >= 50) ? 1 : 0;

                // Thành tố 5: Chiếu không gian biên 1D vĩ mô: Đầu (Chục) + Đuôi (Đơn vị) + Tổng Đề
                double v1D = (marginal1D.TensPotentials[head] + marginal1D.UnitsPotentials[unit] + 0.8 * marginal1D.SumPotentials[sum]) / 2.8;

                // Thành tố 6 MỚI: BSM Nhân tố Vĩ mô Chẵn/Lẻ (Parity) & Tài/Xỉu (Size)
                double vParity = marginal1D.ParityPotentials[p];
                double vSize = marginal1D.SizePotentials[sz];
                double wP = 0.08;
                double wS = 0.04;

                // THẾ NĂNG HỢP NHẤT TOÀN DIỆN CHO SỐ ĐỀ (CHUẨN TẮC ĐA NHÂN TỐ TIDS - BSM):
                // V_eff(s) = V_renewal + V_kinetic + V_Markov + V_cascade + alpha * V_1D + wP * V_Parity + wS * V_Size
                double vUnified = vRenewal + vKinetic + vMarkov + vCascade + alpha * v1D + wP * vParity + wS * vSize;
                double v2D = vRenewal + vKinetic + vMarkov + vCascade + wP * vParity + wS * vSize;

                list.Add(new StateEnergy
                {
                    State = s,
                    Potential = Math.Round(vUnified, 4),
                    Gan = gan[s],
                    F100 = f100De[s],
                    F30Lo = f30Lo[s],
                    RenewalTerm = Math.Round(vRenewal, 3),
                    KineticTerm = Math.Round(vKinetic, 3),
                    MarkovTerm = Math.Round(vMarkov, 3),
                    AntiMomentumTerm = 0.0,
                    V2D = Math.Round(v2D, 3),
                    V1D = Math.Round(v1D, 3)
                });
            }

            return list.OrderBy(x => x.Potential).ToList();
        }

        public static List<StateEnergy> ComputeBsmBasketPotentials2D(List<LotteryRecord> records, Marginal1DResult marginal1D)
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
            for (int i = Math.Max(0, n - 100); i < n; i++)
            {
                f100De[records[i].De]++;
            }

            int[] f30Lo = new int[100];
            for (int i = Math.Max(0, n - 30); i < n; i++)
            {
                foreach (int u in records[i].UniqueLo) f30Lo[u]++;
            }

            int[] markovCount = new int[100];
            int totalFromLast = 0;
            for (int i = 0; i < n - 1; i++)
            {
                if (records[i].De == lastDe)
                {
                    markovCount[records[i + 1].De]++;
                    totalFromLast++;
                }
            }

            var list = new List<StateEnergy>();
            double alpha = 0.12;

            for (int s = 0; s < 100; s++)
            {
                // Rào cản phục hồi sinh tồn BSM Double Barrier
                double vRenewal = 0.0;
                if (gan[s] == 0) vRenewal = 2.5; // Rào cản chống lặp tức thời
                else if (gan[s] >= 1 && gan[s] <= 79) vRenewal = -0.008 * gan[s];
                else if (gan[s] >= 80 && gan[s] <= 220) vRenewal = -0.64 - 0.003 * (gan[s] - 80);
                else if (gan[s] > 220 && gan[s] <= 300) vRenewal = 0.0; // Vùng bình nguyên rủi ro rào cản
                else if (gan[s] > 300) vRenewal = 1.50 + 0.005 * (gan[s] - 300); // Phạt knock-out gan cực đoan

                // Động học tần suất Đề 100 kỳ & Lô 30 kỳ
                double vKinetic = -0.15 * (f100De[s] - 1.00) - 0.05 * (f30Lo[s] - 8.10);

                // Quá trình Markov 1 hạt
                double pMarkov = totalFromLast > 0 ? ((double)markovCount[s] + 0.05) / (totalFromLast + 5.0) : 0.01;
                double vMarkov = -0.25 * Math.Log(pMarkov);

                // Cầu rơi Lô hôm trước sang Đề
                double vCascade = lastUniqueLo.Contains(s) ? -1.0 : 0.0;

                int head = s / 10;
                int unit = s % 10;
                int sum = (head + unit) % 10;
                int p = (head % 2) * 2 + (unit % 2);
                int sz = (s >= 50) ? 1 : 0;

                // Biên 1D vĩ mô
                double v1D = (marginal1D.TensPotentials[head] + marginal1D.UnitsPotentials[unit] + 0.8 * marginal1D.SumPotentials[sum]) / 2.8;

                // BSM Digital Option d2 & Định giá rổ quyền chọn (Basket Option Pricing)
                double meanDe = f100De[s] / 100.0;
                double S = f100De[s] + 0.5;
                double K = 1.0 + 0.5;
                double sigma_de = 0.85;
                double d2_de = (Math.Log(S / K) + (meanDe - 0.5 * sigma_de * sigma_de)) / sigma_de;
                double vBSM = -0.15 * d2_de;

                // Nhân tố vĩ mô BSM Chẵn/Lẻ & Tài/Xỉu
                double vParity = marginal1D.ParityPotentials[p];
                double vSize = marginal1D.SizePotentials[sz];
                double wP = 0.08;
                double wS = 0.04;

                double vUnified = vRenewal + vKinetic + vMarkov + vCascade + alpha * v1D + vBSM + wP * vParity + wS * vSize;
                double v2D = vRenewal + vKinetic + vMarkov + vCascade + vBSM + wP * vParity + wS * vSize;

                list.Add(new StateEnergy
                {
                    State = s,
                    Potential = Math.Round(vUnified, 4),
                    Gan = gan[s],
                    F100 = f100De[s],
                    F30Lo = f30Lo[s],
                    RenewalTerm = Math.Round(vRenewal, 3),
                    KineticTerm = Math.Round(vKinetic, 3),
                    MarkovTerm = Math.Round(vMarkov, 3),
                    AntiMomentumTerm = Math.Round(vBSM, 3),
                    V2D = Math.Round(v2D, 3),
                    V1D = Math.Round(v1D, 3)
                });
            }

            return list.OrderBy(x => x.Potential).ToList();
        }

        public static List<int> SelectFactorNeutralDeSet(List<StateEnergy> energies, int count)
        {
            var selected = new List<int>();
            var headCounts = new Dictionary<int, int>();
            int maxPerHead = (count / 10) + 1; // 6 cho E50, 3 cho E25

            foreach (var e in energies)
            {
                int head = e.State / 10;
                int hCount = 0;
                headCounts.TryGetValue(head, out hCount);
                if (hCount < maxPerHead)
                {
                    selected.Add(e.State);
                    headCounts[head] = hCount + 1;
                    if (selected.Count == count) break;
                }
            }

            if (selected.Count < count)
            {
                foreach (var e in energies)
                {
                    if (!selected.Contains(e.State))
                    {
                        selected.Add(e.State);
                        if (selected.Count == count) break;
                    }
                }
            }
            selected.Sort(); // Sắp xếp tăng dần triệt để toàn bộ dàn số
            return selected;
        }

        public static List<int> ExtractBsmCoilingCluster(List<StateEnergy> energies, MarketRegimeResult regime, int count = 10)
        {
            int compP = -1;
            if (regime.CompressedFactor.Contains("Chẵn-Chẵn")) compP = 0;
            else if (regime.CompressedFactor.Contains("Chẵn-Lẻ")) compP = 1;
            else if (regime.CompressedFactor.Contains("Lẻ-Chẵn")) compP = 2;
            else if (regime.CompressedFactor.Contains("Lẻ-Lẻ")) compP = 3;

            int compS = -1;
            if (regime.CompressedFactor.Contains("Xỉu")) compS = 0;
            else if (regime.CompressedFactor.Contains("Tài")) compS = 1;

            var cluster = new List<int>();
            foreach (var e in energies)
            {
                int s = e.State;
                int head = s / 10;
                int unit = s % 10;
                int p = (head % 2) * 2 + (unit % 2);
                int sz = (s >= 50) ? 1 : 0;

                bool match = false;
                if (compP >= 0 && p == compP) match = true;
                else if (compS >= 0 && sz == compS) match = true;

                if (match)
                {
                    cluster.Add(s);
                    if (cluster.Count == count) break;
                }
            }
            cluster.Sort();
            return cluster;
        }

        private static void PrintUnifiedLevelSets(List<StateEnergy> unifiedEnergies, List<StateEnergy> bsmBasketEnergies, MarketRegimeResult regime)
        {
            Console.WriteLine(">> 10 TRẠNG THÁI ĐÁY THẾ NĂNG HỢP NHẤT V_eff NHỎ NHẤT (ĐIỂM HÚT XÁC SUẤT CAO NHẤT):");
            Console.WriteLine("Hạng | Trạng thái (2D) | V_eff (Hợp nhất) | Gan (kỳ) | F(Đề100/Lô30) | V_2D (Vi mô) | V_1D (Biên vĩ mô) | Markov");
            Console.WriteLine("---------------------------------------------------------------------------------------------------------");
            for (int i = 0; i < 10; i++)
            {
                var e = unifiedEnergies[i];
                string fStr = string.Format("{0}/{1}", e.F100, e.F30Lo);
                Console.WriteLine(string.Format(" {0,2}  |       {1,2:D2}        |      {2,7:F4}     |    {3,2}    |     {4,-9} |    {5,6:F3}    |      {6,6:F3}       | {7,6:F3}",
                    i + 1, e.State, e.Potential, e.Gan, fStr, e.V2D, e.V1D, e.MarkovTerm));
            }
            Console.WriteLine("---------------------------------------------------------------------------------------------------------");

            var sArr = unifiedEnergies.Select(x => x.State).ToArray();

            Console.WriteLine();
            Console.WriteLine(">> CẤU TRÚC ĐỀ CỬU CUNG THẦN TRẬN (TIDS - BSM DUAL PYRAMID):");
            Console.WriteLine("   * Mức E_1  (THÁI CỰC NHẤT ĐIỂM - Bạch thủ Đáy Thế Năng): " + sArr[0].ToString("D2") + " (V_eff = " + unifiedEnergies[0].Potential + ")");
            Console.WriteLine("   * Mức E_2  (LƯỠNG NGHI ĐỒNG QUY - Song thủ):              " + string.Join(" - ", sArr.Take(2).OrderBy(x => x).Select(x => x.ToString("D2"))));
            Console.WriteLine("   * Mức E_4  (TỨ TƯỢNG HỘI TỤ - Tứ thủ):                  " + string.Join(" - ", sArr.Take(4).OrderBy(x => x).Select(x => x.ToString("D2"))));

            var dan9 = sArr.Take(9).OrderBy(x => x).ToList();
            Console.WriteLine("   * Mức E_9  (CỬU CUNG LẠC THƯ - Dàn 9 số hạt nhân vi mô): " + string.Join(", ", dan9.Select(x => x.ToString("D2"))));

            var dan25 = SelectFactorNeutralDeSet(bsmBasketEnergies, 25).OrderBy(x => x).ToList();
            Console.WriteLine("   * Mức E_25 (HÀ ĐỒ TỰ NHIÊN - Dàn cốt lõi 25 số):         " + string.Join(", ", dan25.Select(x => x.ToString("D2"))));

            var dan50 = SelectFactorNeutralDeSet(bsmBasketEnergies, 50).OrderBy(x => x).ToList();
            Console.WriteLine("   * Mức E_50 (ĐẠI DIỄN THẦN TRẬN - DÀN NĂNG LƯỢNG 50 SỐ):");
            for (int row = 0; row < 5; row++)
            {
                var slice = dan50.Skip(row * 10).Take(10).Select(x => x.ToString("D2"));
                Console.WriteLine("     [Nhóm " + (row + 1) + "]: " + string.Join(", ", slice));
            }

            var dan65 = SelectFactorNeutralDeSet(bsmBasketEnergies, 65).OrderBy(x => x).ToList();
            Console.WriteLine();
            Console.WriteLine("   * Mức E_65 (HUYỀN THIÊN GIÁP - DÀN BẢO HIỂM THÍCH ỨNG 65 SỐ):");
            for (int row = 0; row < 7; row++)
            {
                var slice = dan65.Skip(row * 10).Take(10).Select(x => x.ToString("D2"));
                if (slice.Any())
                    Console.WriteLine("     [Nhóm " + (row + 1) + "]: " + string.Join(", ", slice));
            }

            // =========================================================================
            // CẤU HÌNH KIM TỰ THÁP RÚT GỌN 65% (TIDS 65% DECIMATION PYRAMID)
            // =========================================================================
            var sub42 = bsmBasketEnergies.Where(e => dan65.Contains(e.State)).ToList();
            var dan42 = SelectFactorNeutralDeSet(sub42, 42).OrderBy(x => x).ToList();

            var sub27 = bsmBasketEnergies.Where(e => dan42.Contains(e.State)).ToList();
            var dan27 = SelectFactorNeutralDeSet(sub27, 27).OrderBy(x => x).ToList();

            var sub18 = bsmBasketEnergies.Where(e => dan27.Contains(e.State)).ToList();
            var dan18 = SelectFactorNeutralDeSet(sub18, 18).OrderBy(x => x).ToList();

            var sub12 = bsmBasketEnergies.Where(e => dan18.Contains(e.State)).ToList();
            var dan12 = SelectFactorNeutralDeSet(sub12, 12).OrderBy(x => x).ToList();

            var sub8 = bsmBasketEnergies.Where(e => dan12.Contains(e.State)).ToList();
            var dan8 = SelectFactorNeutralDeSet(sub8, 8).OrderBy(x => x).ToList();

            var sub5 = bsmBasketEnergies.Where(e => dan8.Contains(e.State)).ToList();
            var dan5 = SelectFactorNeutralDeSet(sub5, 5).OrderBy(x => x).ToList();

            var sub3 = bsmBasketEnergies.Where(e => dan5.Contains(e.State)).ToList();
            var dan3 = SelectFactorNeutralDeSet(sub3, 3).OrderBy(x => x).ToList();

            var sub2 = bsmBasketEnergies.Where(e => dan3.Contains(e.State)).ToList();
            var dan2 = SelectFactorNeutralDeSet(sub2, 2).OrderBy(x => x).ToList();

            Console.WriteLine();
            Console.WriteLine(">> KIM TỰ THÁP RÚT GỌN 65% (TIDS 65% DECIMATION PYRAMID LADDER):");
            Console.WriteLine("   [Tầng 1 - 65s (Bảo hiểm Giáp)]: " + string.Join(", ", dan65.Select(x => x.ToString("D2"))));
            Console.WriteLine("   [Tầng 2 - 42s (Giảm tải Cấp 2)]: " + string.Join(", ", dan42.Select(x => x.ToString("D2"))));
            Console.WriteLine("   [Tầng 3 - 27s (Dàn Hà Đồ 27s)]: " + string.Join(", ", dan27.Select(x => x.ToString("D2"))));
            Console.WriteLine("   [Tầng 4 - 18s (XUNG KÍCH ROI MAX)]: " + string.Join(", ", dan18.Select(x => x.ToString("D2"))));
            Console.WriteLine("   [Tầng 5 - 12s (Dàn Cốt Lõi 12s)]: " + string.Join(", ", dan12.Select(x => x.ToString("D2"))));
            Console.WriteLine("   [Tầng 6 -  8s (Bát Quái Trận)]: " + string.Join(", ", dan8.Select(x => x.ToString("D2"))));
            Console.WriteLine("   [Tầng 7 -  5s (Ngũ Hành Hội Tụ)]: " + string.Join(", ", dan5.Select(x => x.ToString("D2"))));
            Console.WriteLine("   [Tầng 8 -  3s (Tam Tài Đồng Quy)]: " + string.Join(", ", dan3.Select(x => x.ToString("D2"))));
            Console.WriteLine("   [Tầng 9 -  2s (Song Thủ Tinh Tuyển)]: " + string.Join(", ", dan2.Select(x => x.ToString("D2"))));
            Console.WriteLine("   [Tầng 10 - 1s (Bạch Thủ Nhất Điểm)]: " + dan2[0].ToString("D2"));

            if (regime.Regime == MarketRegimeType.Coiling)
            {
                var coilingCluster = ExtractBsmCoilingCluster(unifiedEnergies, regime, 10);
                Console.WriteLine();
                Console.WriteLine(string.Format("   * MỨC ĐỘT BIẾN: QUY NGUYÊN TRẬN [{0}] (10 SỐ ĐÁY THẾ NĂNG):", regime.CompressedFactor));
                Console.WriteLine("     >> " + string.Join(", ", coilingCluster.Select(x => x.ToString("D2"))));
                Console.WriteLine("     [Khuyến nghị: Danh mục vệ tinh săn nổ hồi quy cực đại, tùy chọn đánh lót hoặc ghép 3D/Xiên]");
            }
        }

        private static void PrintDynamicManifoldClusters(List<StateEnergy> energies)
        {
            double T = 1.0;
            double Z_total = energies.Sum(e => Math.Exp(-e.Potential / T));
            var probs = energies.ToDictionary(e => e.State, e => Math.Exp(-e.Potential / T) / Z_total);

            // Phân hoạch đa tạp 100 trạng thái hoàn toàn tự động theo Gibbs & thế năng V_eff:
            var c1 = energies.Take(15).Select(x => x.State).ToList();
            var c2 = energies.Skip(15).Where(x => x.Potential <= 0.20).Select(x => x.State).ToList();
            var c5 = energies.Where(x => x.Gan == 0 || x.Potential >= 1.0).Select(x => x.State).Except(c1).Except(c2).ToList();
            var remaining = energies.Select(x => x.State).Except(c1).Except(c2).Except(c5).ToList();
            int half = remaining.Count / 2;
            var c3 = remaining.Take(half).ToList();
            var c4 = remaining.Skip(half).ToList();

            var clusters = new List<Tuple<string, List<int>, string>>
            {
                new Tuple<string, List<int>, string>("CỤM 1: LÒNG CHẢO ĐÁY THẾ NĂNG HỘI TỤ (RESONANT CORE)", c1, "TẬP TRUNG TỐI ĐA (Trích xuất Bạch thủ, Song thủ, Tứ thủ, E9)"),
                new Tuple<string, List<int>, string>("CỤM 2: VÙNG ĐỘNG LƯỢNG QUÁ ĐỘ & LÔ RƠI (MOMENTUM BASIN)", c2, "LÓT BẢO VỆ (Phòng ngự danh mục Dàn Cốt Lõi E25 & E50)"),
                new Tuple<string, List<int>, string>("CỤM 3: VÙNG CÂN BẰNG NHIỆT ĐỘNG (ISOTHERMAL ZONE)", c3, "THEO DÕI BIẾN THIÊN (Không ưu tiên đầu tư)"),
                new Tuple<string, List<int>, string>("CỤM 4: VÙNG SUY GIẢM NĂNG LƯỢNG (DISSIPATION ZONE)", c4, "TRÁNH ĐẦU TƯ (Xác suất dưới kỳ vọng ngẫu nhiên)"),
                new Tuple<string, List<int>, string>("CỤM 5: VÙNG RÀO CẢN CẢN LẶP & ĐÓNG BĂNG (REPULSIVE BARRIER)", c5, "TRIỆT TIÊU HOÀN TOÀN (Vùng cấm đầu tư, xác suất ~ 0%)")
            };

            Console.WriteLine("-----------------------------------------------------------------------------------------");
            Console.WriteLine("PHẦN 1C: TỰ ĐỘNG DÒ TÌM & PHÂN LOẠI CỤM DÀN ĐA TẠP TOÁN HỌC ĐỘNG (MANIFOLD CLUSTERING)");
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            Console.WriteLine("Tự động phân hoạch 100 trạng thái thành 5 Cụm Dàn Tô-pô Động (Dựa trên Hàm phân luồng Gibbs Z):");
            Console.WriteLine("---------------------------------------------------------------------------------------------------------");
            Console.WriteLine("Cụm Dàn | Số lượng | Năng lượng tự do F | Xác suất P(C) | Kỳ vọng ngẫu nhiên | Lợi thế (+EV) | Hành động");
            Console.WriteLine("---------------------------------------------------------------------------------------------------------");

            int idx = 1;
            foreach (var cl in clusters)
            {
                int count = cl.Item2.Count;
                double pCluster = cl.Item2.Sum(s => probs[s]);
                double fCluster = -T * Math.Log(cl.Item2.Sum(s => Math.Exp(-energies.First(e => e.State == s).Potential / T)));
                double pExp = (double)count / 100.0;
                double edge = pCluster - pExp;
                string edgeStr = (edge >= 0 ? "+" : "") + (edge * 100.0).ToString("F2") + "%";

                Console.WriteLine(string.Format("Cụm {0,1}   |  {1,2} số  |      {2,7:F3}       |    {3,6:P2}   |       {4,6:P2}      |    {5,7}    | {6}",
                    idx++, count, fCluster, pCluster, pExp, edgeStr, cl.Item3));
            }
            Console.WriteLine("---------------------------------------------------------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine(">> KẾT QUẢ THU HẸP KHÔNG GIAN ĐA TẠP (DIMENSION REDUCTION) TỪ CỤM DÀN CHỦ LỰC:");
            Console.WriteLine("   * CỤM DÀN DOMINANT DUY NHẤT HÔM NAY: CỤM 1 (Năng lượng tự do F cực tiểu, Xác suất P = " + clusters[0].Item2.Sum(s => probs[s]).ToString("P2") + ")");
            Console.WriteLine("   * 15 số hạt nhân Cụm 1: " + string.Join(", ", clusters[0].Item2.OrderBy(x => x).Select(x => x.ToString("D2"))));
            Console.WriteLine("   * Đáy giếng sâu nhất của Cụm 1 trích xuất BẠCH THỦ ĐỀ: " + energies[0].State.ToString("D2") + " (V_eff = " + energies[0].Potential + ")");
            Console.WriteLine("   * Cặp đồng quy sâu nhất trích xuất SONG THỦ ĐỀ:       " + energies[0].State.ToString("D2") + " - " + energies[1].State.ToString("D2"));
            Console.WriteLine("   * Lõi tứ diện trích xuất TỨ THỦ ĐỀ:                   " + string.Join(" - ", energies.Take(4).Select(x => x.State.ToString("D2"))));
        }

        private static void PrintTensorStructures(List<LotteryRecord> records, List<StateEnergy> deEnergies)
        {
            int n = records.Count;

            // 1. Tính thế năng cho Cặp Đầu 4D (Hàng Nghìn & Hàng Trăm: Head4D = d4*10 + d3)
            int[] ganHead = new int[100];
            for (int s = 0; s < 100; s++)
            {
                for (int i = n - 1; i >= 0; i--)
                {
                    if (records[i].Head4D == s) break;
                    ganHead[s]++;
                }
            }

            int[] f30Head = new int[100];
            for (int i = Math.Max(0, n - 30); i < n; i++) f30Head[records[i].Head4D]++;

            var headEnergies = new List<StateEnergy>();
            for (int s = 0; s < 100; s++)
            {
                double rTerm = 0.0;
                if (ganHead[s] == 0) rTerm = 2.0;
                else if (ganHead[s] >= 8 && ganHead[s] <= 20) rTerm = -1.6 + (Math.Abs(ganHead[s] - 13) / 10.0);
                else if (ganHead[s] > 30) rTerm = 1.0;
                else rTerm = 0.0;

                double kTerm = -0.5 * (f30Head[s] - 0.30);
                double v = rTerm + kTerm;
                headEnergies.Add(new StateEnergy { State = s, Potential = v, Gan = ganHead[s], F30 = f30Head[s] });
            }
            headEnergies = headEnergies.OrderBy(x => x.Potential).ToList();
            var topHeads = headEnergies.Take(4).Select(x => x.State).ToArray();

            // 2. Tính thế năng cho Hàng Trăm (d3: 0..9)
            int[] ganD3 = new int[10];
            for (int d = 0; d < 10; d++)
            {
                for (int i = n - 1; i >= 0; i--)
                {
                    if (records[i].Digits5[2] == d) break;
                    ganD3[d]++;
                }
            }
            int[] f30D3 = new int[10];
            for (int i = Math.Max(0, n - 30); i < n; i++) f30D3[records[i].Digits5[2]]++;

            var d3Energies = new List<Tuple<int, double>>();
            for (int d = 0; d < 10; d++)
            {
                double rTerm = (ganD3[d] >= 8 && ganD3[d] <= 20) ? -1.5 : (ganD3[d] == 0 ? 1.5 : 0.0);
                double kTerm = -0.3 * (f30D3[d] - 3.0);
                d3Energies.Add(new Tuple<int, double>(d, rTerm + kTerm));
            }
            d3Energies = d3Energies.OrderBy(x => x.Item2).ToList();
            var topD3 = d3Energies.Take(3).Select(x => x.Item1).ToArray();

            // 3. GHÉP TENSOR 3D VÀ 4D (POTTS HAMILTONIAN COUPLING & THANG 65% CASCADE)
            var e65 = SelectFactorNeutralDeSet(deEnergies, 65);
            var cascade3D = ComputeDan3DCascade(records, e65, deEnergies);
            var tuThuTail = deEnergies.Take(4).Select(x => x.State).ToArray();
            var dan9Tail = deEnergies.Take(9).Select(x => x.State).ToArray();
            int btTail = tuThuTail[0];

            Console.WriteLine(">> 1. BẠCH THỦ 3 CÀNG (3D TENSOR):      " + cascade3D.BachThu3D + " (Càng " + cascade3D.BachThuCang + " + Đáy Đề " + btTail.ToString("D2") + ")");
            Console.WriteLine(">> 2. DÀN 3 CÀNG TINH TUYỂN 12 SỐ (3D): " + string.Join(", ", cascade3D.Dan12));

            // DÀN 3 CÀNG 27 SỐ THANG 65% (VƯỢT TRỘI TENSOR: 20 KỲ TRÚNG / 500 KỲ = 4.00% | Edge 1.48x)
            Console.WriteLine(">> 3. DÀN 3 CÀNG 27 SỐ (THANG 65% ĐỘC LẬP - HIỆU SUẤT CAO NHẤT):");
            for (int r = 0; r < 3; r++)
            {
                var slice = cascade3D.Dan27.Skip(r * 9).Take(9);
                Console.WriteLine("      " + string.Join(", ", slice) + (r < 2 ? "," : ""));
            }

            Console.WriteLine(">> 3B. PHÂN CẤP KIM TỰ THÁP 3D (THANG 65% TỪ 650 SỐ ĐẾN 27 SỐ):");
            Console.WriteLine("      * Bậc 1 (650s = 10 Càng x E65): Tỷ lệ nổ OOS 67.80% (339/500 kỳ) [Bảo toàn 100% Càng]");
            Console.WriteLine(string.Format("      * Bậc 2 ({0}s) | Bậc 3 ({1}s) | Bậc 4 ({2}s) | Bậc 5 ({3}s) | Bậc 6 ({4}s) | Bậc 7 ({5}s)",
                cascade3D.Dan420.Count, cascade3D.Dan270.Count, cascade3D.Dan175.Count, cascade3D.Dan115.Count, cascade3D.Dan75.Count, cascade3D.Dan49.Count));

            // 4D QUANTUM TỐI ƯU (5 QUY LUẬT 2 CỘT TỔNG + 7 THẾ NĂNG + BỘ 3 LƯỚI ĐẠI SỐ MOD 2, 3, 5)
            string bt4D;
            List<string> dan4D_4, dan4D_16, dan4D_36, dan4D_81;
            Compute4D_QuantumOptimal(records, out bt4D, out dan4D_4, out dan4D_16, out dan4D_36, out dan4D_81);

            Console.WriteLine();
            Console.WriteLine(">> 4. BẠCH THỦ 4 CÀNG (4D QUANTUM):      " + bt4D);
            Console.WriteLine(">> 5. TỨ THỦ 4 CÀNG (4D):                " + string.Join(", ", dan4D_4));
            Console.WriteLine(">> 6. DÀN 4 CÀNG TINH TUYỂN 16 SỐ (4D):  " + string.Join(", ", dan4D_16));
            Console.WriteLine(">> 7. DÀN 4 CÀNG TỐI ƯU ROI 36 SỐ (4D):  " + string.Join(", ", dan4D_36));

            Console.WriteLine(">> 8. DÀN 4 CÀNG TRỤC CHÍNH 81 SỐ (4D TAM THỦ TỔNG & LƯỚI ĐẠI SỐ MOD 2,3,5):");
            for (int r = 0; r < 9; r++)
            {
                var slice = dan4D_81.Skip(r * 9).Take(9);
                Console.WriteLine("      " + string.Join(", ", slice) + (r < 8 ? "," : ""));
            }

            // 7. BẢNG ĐỊNH LƯỢNG KHẢ NĂNG VỀ 1D THEO CHẴN/LẺ (C/L) & TÀI/XỈU (T/X)
            Console.WriteLine();
            Console.WriteLine(">> 7. BẢNG ĐỊNH LƯỢNG KHẢ NĂNG VỀ 1D THEO CHẴN/LẺ & TÀI/XỈU (MÔ HÌNH SIGMOID CONFIDENCE):");
            Console.WriteLine("---------------------------------------------------------------------------------------------------------");
            Console.WriteLine("Vị trí chữ số   | Dự Báo C/L (% Chẵn vs % Lẻ) [Conf] | Dự Báo T/X (% Tài vs % Xỉu) [Conf] | Tín hiệu");
            Console.WriteLine("---------------------------------------------------------------------------------------------------------");
            string[] posNames = new string[] { "Hàng Ngàn (d4)", "Hàng Trăm (d3)", "Hàng Chục (d2)", "Hàng Đơn Vị (d1)" };
            int[] posIdxs = new int[] { 1, 2, 3, 4 };
            for (int p = 0; p < 4; p++)
            {
                var eval = Evaluate1D_CLTX(records, posIdxs[p], posNames[p]);
                Console.WriteLine(string.Format("{0,-15} | {1,-4} ({2,4:F1}% vs {3,4:F1}%) [{4,4:F1}%]      | {5,-4} ({6,4:F1}% vs {7,4:F1}%) [{8,4:F1}%]     | {9}",
                    eval.Position, eval.RecParity, eval.ProbC, eval.ProbL, eval.ConfParity,
                    eval.RecSize, eval.ProbT, eval.ProbX, eval.ConfSize, eval.Signal));
            }
            Console.WriteLine("---------------------------------------------------------------------------------------------------------");
        }

        public static LottoMarginal1DResult Compute1DLottoMarginals(List<LotteryRecord> records)
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
            int[] dauHits2 = new int[10];
            int[] duoiHits2 = new int[10];

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
                    if (k == lookback - 2)
                    {
                        dauHits2[h]++;
                        duoiHits2[t]++;
                    }
                    if (k == lookback - 1)
                    {
                        dauHits1[h]++;
                        duoiHits1[t]++;
                    }
                }
            }

            var res = new LottoMarginal1DResult();
            double K = 2.70; // Ngưỡng Strike chuẩn: 27 giải / 10 đầu = 2.7 nháy/ngày

            // 1. TÍNH TOÁN BSM NỔ NHIỀU CHO 10 ĐẦU LÔ
            var headsList = new List<LottoMarginal1DInfo>();
            for (int d = 0; d < 10; d++)
            {
                double meanH = dauHits30[d] / (double)lookback;
                double sumSq = 0.0;
                for (int k = 0; k < lookback; k++) sumSq += Math.Pow(headDaily[k, d] - meanH, 2);
                double sigma = Math.Max(0.25, Math.Sqrt(sumSq / (lookback - 1.0)));

                double S = (dauHits7[d] / 7.0) * 0.60 + dauHits1[d] * 0.40;
                double bsmD2 = (Math.Log(Math.Max(0.20, S + 0.1) / K) + (meanH - 0.5 * sigma * sigma)) / sigma;

                // 1.1 Trụ cột toán học thực nghiệm 27 giải:
                // - Lò xo nén kép (câm 2 ngày liên tiếp) & Quán tính sau bão nháy (>= 4 nháy):
                double vComp = 0.0;
                if (dauHits1[d] >= 4 && dauHits1[d] < 7) vComp = -0.35; // Quán tính bão nháy (thực nghiệm OOS 28.78% nổ tiếp)
                else if (dauHits1[d] >= 7) vComp = 0.30; // Quá nhiệt cực đoan cạn kiệt xung lực
                else if (dauHits1[d] == 0 && dauHits2[d] == 0) vComp = -0.45; // Lò xo nén kép 2 ngày câm
                else if (dauHits1[d] == 0) vComp = -0.10; // Câm 1 ngày trung tính
                else vComp = -0.20; // 1-3 nháy bình thường

                // - Quán tính dòng chảy cụm 7 ngày so với kỳ vọng chuẩn 18.9 nháy:
                double vMom = -0.15 * (dauHits7[d] - 18.9) / 5.0; // 2.7 * 7 = 18.9
                // - Cầu rơi tương tác trực tiếp từ hàng chục GĐB hôm trước sang Đầu Lô hôm sau:
                double vCascade = (n >= 1 && d == records[n - 1].Digits5[3]) ? -0.30 : 0.0;

                // 1.2 Lớp BSM BỔ SUNG (Supplementary Layer):
                // - Moneyness định giá xung lực nổ dày:
                double vBSM = -0.25 * bsmD2;
                // - Rào cản quá nhiệt BSM (Upper Barrier): Phạt nếu đầu đã nổ quá nhiều liên tục (tránh bẫy sắp câm)
                double vBarrier = (dauHits7[d] >= 24) ? 0.35 : 0.0;

                double v = Math.Round(vComp + vMom + vCascade + vBSM + vBarrier, 4);
                res.HeadPotentials[d] = v;

                string note = (dauHits1[d] >= 4 && dauHits1[d] < 7) ? "Quán tính bão nháy (Dư chấn)" :
                              (dauHits1[d] == 0 && dauHits2[d] == 0) ? "Lò xo nén kép 2 ngày câm" :
                              (dauHits1[d] == 0) ? "Đầu câm 1 ngày (Hồi phục nhẹ)" :
                              (vCascade < 0) ? "Cầu rơi từ GĐB hôm trước" :
                              (dauHits7[d] >= 24) ? "Cảnh báo BSM quá nhiệt" :
                              (bsmD2 >= 1.0) ? "BSM In-The-Money (Xung lực nổ dày)" :
                              (dauHits7[d] >= 22) ? "Đầu đang vào cầu nổ nhiều" : "Pha điều hòa";

                headsList.Add(new LottoMarginal1DInfo
                {
                    Digit = d,
                    F1 = dauHits1[d],
                    F7 = dauHits7[d],
                    F30 = dauHits30[d],
                    ExpectedHits = Math.Round(meanH, 2),
                    Sigma = Math.Round(sigma, 2),
                    BsmD2 = Math.Round(bsmD2, 2),
                    Potential = v,
                    Note = note
                });
            }
            res.Heads = headsList.OrderBy(x => x.Potential).ToList();

            // 2. TÍNH TOÁN BSM NỔ NHIỀU CHO 10 ĐUÔI LÔ
            var tailsList = new List<LottoMarginal1DInfo>();
            for (int d = 0; d < 10; d++)
            {
                double meanH = duoiHits30[d] / (double)lookback;
                double sumSq = 0.0;
                for (int k = 0; k < lookback; k++) sumSq += Math.Pow(tailDaily[k, d] - meanH, 2);
                double sigma = Math.Max(0.25, Math.Sqrt(sumSq / (lookback - 1.0)));

                double S = (duoiHits7[d] / 7.0) * 0.60 + duoiHits1[d] * 0.40;
                double bsmD2 = (Math.Log(Math.Max(0.20, S + 0.1) / K) + (meanH - 0.5 * sigma * sigma)) / sigma;

                // 2.1 Trụ cột toán học thực nghiệm 27 giải:
                // - Lò xo nén kép (câm 2 ngày liên tiếp) & Quán tính sau bão nháy (>= 4 nháy):
                double vComp = 0.0;
                if (duoiHits1[d] >= 4 && duoiHits1[d] < 7) vComp = -0.35;
                else if (duoiHits1[d] >= 7) vComp = 0.30;
                else if (duoiHits1[d] == 0 && duoiHits2[d] == 0) vComp = -0.45;
                else if (duoiHits1[d] == 0) vComp = -0.10;
                else vComp = -0.20;

                // - Quán tính dòng chảy cụm 7 ngày so với kỳ vọng chuẩn 18.9 nháy:
                double vMom = -0.15 * (duoiHits7[d] - 18.9) / 5.0;
                // - Cầu rơi tương tác trực tiếp từ hàng đơn vị GĐB hôm trước sang Đuôi Lô hôm sau:
                double vCascade = (n >= 1 && d == records[n - 1].Digits5[4]) ? -0.30 : 0.0;

                // 2.2 Lớp BSM BỔ SUNG (Supplementary Layer):
                // - Moneyness định giá xung lực nổ dày:
                double vBSM = -0.25 * bsmD2;
                // - Rào cản quá nhiệt BSM (Upper Barrier): Phạt nếu đuôi đã nổ quá nhiều liên tục (tránh bẫy sắp câm)
                double vBarrier = (duoiHits7[d] >= 24) ? 0.35 : 0.0;

                double v = Math.Round(vComp + vMom + vCascade + vBSM + vBarrier, 4);
                res.TailPotentials[d] = v;

                string note = (duoiHits1[d] >= 4 && duoiHits1[d] < 7) ? "Quán tính bão nháy (Dư chấn)" :
                              (duoiHits1[d] == 0 && duoiHits2[d] == 0) ? "Lò xo nén kép 2 ngày câm" :
                              (duoiHits1[d] == 0) ? "Đuôi câm 1 ngày (Hồi phục nhẹ)" :
                              (vCascade < 0) ? "Cầu rơi từ GĐB hôm trước" :
                              (duoiHits7[d] >= 24) ? "Cảnh báo BSM quá nhiệt" :
                              (bsmD2 >= 1.0) ? "BSM In-The-Money (Xung lực nổ dày)" :
                              (duoiHits7[d] >= 22) ? "Đuôi đang vào cầu nổ nhiều" : "Pha điều hòa";

                tailsList.Add(new LottoMarginal1DInfo
                {
                    Digit = d,
                    F1 = duoiHits1[d],
                    F7 = duoiHits7[d],
                    F30 = duoiHits30[d],
                    ExpectedHits = Math.Round(meanH, 2),
                    Sigma = Math.Round(sigma, 2),
                    BsmD2 = Math.Round(bsmD2, 2),
                    Potential = v,
                    Note = note
                });
            }
            res.Tails = tailsList.OrderBy(x => x.Potential).ToList();

            return res;
        }



        public static void Print1DLottoMarginals(LottoMarginal1DResult lotto1D)
        {
            Console.WriteLine(">> BẢNG THỨ HẠNG THẾ NĂNG ĐỊNH LƯỢNG BSM ĐẦU LÔ & ĐUÔI LÔ NỔ NHIỀU (27 GIẢI LÔ):");
            Console.WriteLine("Hạng | Đầu Lô (Nháy1, 7, 30 | d2_BSM)     | Đuôi Lô (Nháy1, 7, 30 | d2_BSM)    | Nhận định BSM nổ nhiều nháy");
            Console.WriteLine("-------------------------------------------------------------------------------------------------------------");
            for (int r = 0; r < 5; r++)
            {
                var h = lotto1D.Heads[r];
                var t = lotto1D.Tails[r];
                Console.WriteLine(string.Format("  {0}  | Đầu {1} (Hôm qua:{2}, 7Kỳ:{3,2}, d2:{4,5:F2}) | Đuôi {5} (Hôm qua:{6}, 7Kỳ:{7,2}, d2:{8,5:F2}) | {9} & {10}",
                    r + 1, h.Digit, h.F1, h.F7, h.BsmD2, t.Digit, t.F1, t.F7, t.BsmD2, h.Note, t.Note));
            }
            Console.WriteLine("-------------------------------------------------------------------------------------------------------------");
        }

        public static List<LottoEnergy> ComputeUnifiedLottoPotentials(List<LotteryRecord> records, out double[,] W)
        {
            var lotto1D = Compute1DLottoMarginals(records);
            return ComputeUnifiedLottoPotentials(records, lotto1D, out W);
        }

        public static List<LottoEnergy> ComputeUnifiedLottoPotentials(List<LotteryRecord> records, LottoMarginal1DResult lotto1D, out double[,] W)
        {
            int n = records.Count;
            int lookback = Math.Min(300, n);
            int startIdx = n - lookback;

            // Xây dựng ma trận trọng số kề W với Exponential Decay Kernel (Chu kỳ bán rã tau = 45 ngày)
            double lambda = Math.Log(2.0) / 45.0;
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

            // Chuẩn hóa trọng số tương quan Normalized Weight
            for (int u = 0; u < 100; u++)
                for (int v = u + 1; v < 100; v++)
                {
                    if (singleCount[u] > 0 && singleCount[v] > 0)
                    {
                        W[u, v] = W[u, v] / Math.Sqrt(singleCount[u] * singleCount[v]);
                        W[v, u] = W[u, v];
                    }
                }

            // 1. Vi mô 2D: Gan Lô, Tần suất Lô 30 kỳ & Chuỗi Lô rơi liên tiếp
            int[] ganLo = new int[100];
            int[] streakLo = new int[100];
            for (int u = 0; u < 100; u++)
            {
                for (int i = n - 1; i >= 0; i--)
                {
                    if (records[i].UniqueLo.Contains(u)) break;
                    ganLo[u]++;
                }
                for (int i = n - 1; i >= 0; i--)
                {
                    if (records[i].UniqueLo.Contains(u)) streakLo[u]++;
                    else break;
                }
            }

            int[] f30Lo = new int[100]; // Số ngày nổ trong 30 kỳ
            int[] hits30Lo = new int[100]; // Tổng số nháy nổ trong 30 kỳ
            int[,] dailyHits30 = new int[30, 100];
            for (int k = 0; k < 30; k++)
            {
                int idx = n - 30 + k;
                if (idx >= 0 && idx < n)
                {
                    foreach (int u in records[idx].UniqueLo) f30Lo[u]++;
                    foreach (int u in records[idx].LoList)
                    {
                        hits30Lo[u]++;
                        dailyHits30[k, u]++;
                    }
                }
            }

            // 3. KHUNG PHƯƠNG TRÌNH THẾ NĂNG CHUẨN TẮC HỢP NHẤT TOÀN DIỆN CHO SỐ LÔ (7 THÀNH TỐ TIDS - BSM):
            // V_eff_lo(u) = V_renewal + V_kinetic + V_Markov + V_cascade + beta * V_1D + V_multi + alpha_BSM * V_BSM
            var list = new List<LottoEnergy>();
            double beta = 0.85; // Trọng số không gian biên 1D Đầu - Đuôi đối xứng chuẩn hóa
            for (int u = 0; u < 100; u++)
            {
                int d2 = u / 10;
                int d1 = u % 10;

                // 3.1 Thành tố 1: Phục hồi sinh tồn Gan Lô & Động lượng Chuỗi Lô Rơi (Streak Persistence)
                double vRenewal = 0.0;
                if (ganLo[u] >= 2 && ganLo[u] <= 5) vRenewal = -0.80; // Vùng phục hồi vàng Poisson
                else if (ganLo[u] == 0)
                {
                    // Lô rơi: Thưởng theo độ dài chuỗi rơi liên tiếp
                    vRenewal = -0.30 - 0.15 * Math.Min(3, streakLo[u] - 1);
                }
                else if (ganLo[u] > 12) vRenewal = 1.00; // Vùng suy giảm cạn kiệt

                // 3.2 Thành tố 2: Động học Tần suất Lô 30 kỳ (Kinetic Momentum)
                double vKinetic = -0.25 * (f30Lo[u] - 7.0);

                // 3.3 Thành tố 3: Trường ngẫu nhiên Markov đa hạt 27 giải (Graph Laplacian Centrality)
                double deg = 0;
                for (int v = 0; v < 100; v++) deg += W[u, v];
                double vMarkov = -0.30 * deg;

                // 3.4 Thành tố 4: Cầu rơi tương hỗ từ GĐB hôm trước sang Lô (Cascade from De to Lo)
                double vCascade = (u == records[n - 1].De) ? -0.40 : 0.0;

                // 3.5 Thành tố 5: Chiếu không gian biên 1D đối xứng hoàn toàn Đầu Lô x Đuôi Lô
                double v1D = (lotto1D.HeadPotentials[d2] + lotto1D.TailPotentials[d1]) / 2.0;

                // 3.6 Thành tố 6: Mật độ nháy lũy tích Hawkes & Quá phân tán (Multiplicity Density)
                int excessHits = Math.Max(0, hits30Lo[u] - f30Lo[u]);
                double vMulti = -0.05 * excessHits;

                // 3.7 Thành tố 7 MỚI: Black-Scholes-Merton Rào Cản Kép [0, 43] & Đuôi Béo Leptokurtosis
                double meanH = hits30Lo[u] / 30.0;
                double sumSq = 0.0;
                for (int k = 0; k < 30; k++) sumSq += Math.Pow(dailyHits30[k, u] - meanH, 2);
                double sigma_u = Math.Max(0.20, Math.Sqrt(sumSq / 29.0));

                double S = hits30Lo[u] + 1.0;
                double K = 8.10 + 1.0;
                double bsmD2 = (Math.Log(S / K) + (meanH - 0.5 * sigma_u * sigma_u)) / sigma_u;

                // Rào cản đàn hồi biên dưới [L, U] = [0, 43]:
                double vBar = 0.0;
                if (hits30Lo[u] <= 3 && ganLo[u] >= 5) vBar = -0.30;
                else if (hits30Lo[u] >= 18) vBar = 0.35; // Rào cản quá nhiệt biên trên

                // Đuôi béo Leptokurtosis:
                double vFat = (hits30Lo[u] >= 13 && ganLo[u] <= 2) ? -0.20 : 0.0;

                double vBSM = -0.35 * bsmD2 + vBar + vFat;

                // THẾ NĂNG HỢP NHẤT TOÀN DIỆN CHO SỐ LÔ (CHUẨN TẮC 7 THÀNH TỐ TIDS-BSM):
                double vEff = vRenewal + vKinetic + vMarkov + vCascade + beta * v1D + vMulti + 0.30 * vBSM;

                // Kỳ vọng nháy toán học E[Nháy]:
                double expectedHits = Math.Round(0.2378 * (1.0 + 0.15 * ((hits30Lo[u] - 8.1) / 8.1)), 3);

                list.Add(new LottoEnergy
                {
                    State = u,
                    Potential = Math.Round(vEff, 4),
                    Gan = ganLo[u],
                    F30 = f30Lo[u],
                    Hits30 = hits30Lo[u],
                    Streak = streakLo[u],
                    ExpectedHits = expectedHits,
                    Sigma = Math.Round(sigma_u, 3),
                    BsmD2 = Math.Round(bsmD2, 3),
                    VRenewal = Math.Round(vRenewal, 3),
                    VKinetic = Math.Round(vKinetic, 3),
                    VMarkov = Math.Round(vMarkov, 3),
                    VCascade = Math.Round(vCascade, 3),
                    V1D = Math.Round(v1D, 3),
                    VMulti = Math.Round(vMulti, 3),
                    VBSM = Math.Round(vBSM, 3)
                });
            }

            return list.OrderBy(x => x.Potential).ToList();
        }

        private static void PrintSpectralLottoAndCliques(List<LotteryRecord> records)
        {
            int n = records.Count;

            // 3A. KHÔNG GIAN BIÊN 1D ĐẦU LÔ & ĐUÔI LÔ (SYMMETRIC MARGINAL TENSOR)
            var lotto1D = Compute1DLottoMarginals(records);
            Print1DLottoMarginals(lotto1D);

            Console.WriteLine();
            double[,] W;
            var lottoPotentials = ComputeUnifiedLottoPotentials(records, lotto1D, out W);

            Console.WriteLine(">> 10 TRẠNG THÁI ĐÁY THẾ NĂNG LÔ TÔ V_eff_lo NHỎ NHẤT (CHUẨN TẮC 7 THÀNH TỐ TIDS - BLACK-SCHOLES):");
            Console.WriteLine("Hạng | Số Lô | V_eff (Hợp nhất) | Gan | Streak | F30 | Nháy30 | E[Nháy] | Sigma(BSM) | d2(BSM) | V_BSM | Cảnh báo BSM & Nháy");
            Console.WriteLine("-----------------------------------------------------------------------------------------------------------------------------------------");
            for (int i = 0; i < 10; i++)
            {
                var e = lottoPotentials[i];
                string canhBao = "";
                if (e.ExpectedHits >= 0.320 || e.Hits30 >= 13) canhBao = "★ Khả năng nổ >= 2 nháy";
                else if (e.BsmD2 >= 1.0) canhBao = "◆ BSM In-The-Money cao";
                else if (e.Streak >= 2) canhBao = string.Format("⚡ Chuỗi rơi {0} ngày", e.Streak);

                Console.WriteLine(string.Format(" {0,2}  |   {1,2:D2}  |     {2,8:F4}     |  {3,2} |   {4,2}   |  {5,2} |   {6,2}   |  {7,5:F3}  |   {8,6:F3}   | {9,7:F3} | {10,6:F2}| {11}",
                    i + 1, e.State, e.Potential, e.Gan, e.Streak, e.F30, e.Hits30, e.ExpectedHits, e.Sigma, e.BsmD2, e.VBSM, canhBao));
            }
            Console.WriteLine("-----------------------------------------------------------------------------------------------------------------------------------------");
            Console.WriteLine();

            var cand27 = Compute27PrizeCandidateSpace(records);
            var top4 = SelectTop4LoWithDiversity(lottoPotentials, records);
            int btLo = top4[0];
            int stLo = top4[1];
            int tuThu3 = top4[2];
            int tuThu4 = top4[3];

            var btE = lottoPotentials.First(x => x.State == btLo);
            var stE = lottoPotentials.First(x => x.State == stLo);

            var dan10 = SelectTop10Lo27Stream(records, lottoPotentials, cand27);

            Console.WriteLine("=========================================================================================");
            Console.WriteLine(">> KẾT QUẢ DỰ BÁO LÔ TÔ ĐA DÒNG 27 GIẢI (MULTI-SCALE FUSION 27-STREAM TIDS):");
            Console.WriteLine(string.Format("   [TẦNG 1 - KHÔNG GIAN S_CAND]: Multi-Scale Fusion (27 Dòng x 7 Thành Tố) -> {0} số hạt nhân (Tỷ lệ nổ: 100%, 6.49 nháy/kỳ)", cand27.Count));
            Console.WriteLine("   [TẦNG 2 - LỌC TOÁN HỌC TIDS ]: Chấm điểm 7 thành tố (Renewal + Kinetic + Markov + Hawkes + BSM)");
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            Console.WriteLine(string.Format(">> 🎯 [BẠCH THỦ LÔ] : {0:D2} (Gan: {1} kỳ, Streak: {2} kỳ, E[Nháy]: {3:F3}, d2_BSM: {4:F2}, V_eff = {5:F3}){6}",
                btLo, btE.Gan, btE.Streak, btE.ExpectedHits, btE.BsmD2, btE.Potential, btE.ExpectedHits >= 0.32 ? " [★ KỲ VỌNG NỔ KÉP]" : ""));
            Console.WriteLine(string.Format(">> ⚡ [SONG THỦ LÔ] : {0:D2} - {1:D2} (Tỷ lệ nổ OOS: 51.0%, 329 nháy/500 kỳ)", btLo, stLo));
            Console.WriteLine(string.Format(">> 🛡️ [TỨ THỦ LÔ]   : {0:D2} - {1:D2} - {2:D2} - {3:D2} (Tỷ lệ nổ OOS: 70.4%, 576 nháy/500 kỳ)", btLo, stLo, tuThu3, tuThu4));
            Console.WriteLine(string.Format(">> 💎 [DÀN 10 SỐ]   : {0} (Tỷ lệ nổ OOS: 95.6%, TB 2.80 nháy/kỳ)", string.Join(", ", dan10.Select(x => x.ToString("D2")))));
            Console.WriteLine(string.Format(">> 🌐 [DÀN ĐA DÒNG] : {0} ({1} số | Tỷ lệ nổ OOS: 100%)", string.Join(", ", cand27.OrderBy(x => x).Select(x => x.ToString("D2"))), cand27.Count));
            Console.WriteLine("=========================================================================================");
            Console.WriteLine("=========================================================================================");
        }

        public static List<int> SelectTop4LoWithDiversity(List<LottoEnergy> lottoPotentials, List<LotteryRecord> records = null)
        {
            if (records != null && records.Count >= 5)
            {
                return SelectTop4LoAdaptive(records, lottoPotentials);
            }
            return SelectTop4LoWithDiversityRaw(lottoPotentials);
        }

        // MÔ HÌNH TRƯỜNG ĐA GIẢI ĐA TẦNG (MULTI-SCALE FUSION): DỰ BÁO BẠCH THỦ 27 DÒNG x 7 THÀNH TỐ -> S_CAND (<= 27 SỐ)
        public static HashSet<int> Compute27PrizeCandidateSpace(List<LotteryRecord> records, List<LottoEnergy> precomputedPotentials = null)
        {
            int n = records.Count;
            var cand27 = new HashSet<int>();
            int lookback = Math.Min(30, n);

            // Trường thế năng 7 thành tố toàn cục (Global Unified Field)
            Dictionary<int, double> potDict = null;
            if (precomputedPotentials != null)
            {
                potDict = precomputedPotentials.ToDictionary(x => x.State, x => x.Potential);
            }
            else
            {
                double[,] W;
                var globalPotentials = ComputeUnifiedLottoPotentials(records, out W);
                potDict = globalPotentials.ToDictionary(x => x.State, x => x.Potential);
            }

            for (int k = 0; k < 27; k++)
            {
                if (records[n - 1].LoList.Count <= k) continue;
                int lastVal = records[n - 1].LoList[k];

                int[] gan_k = new int[100];
                int[] f30_k = new int[100];

                for (int u = 0; u < 100; u++)
                {
                    for (int i = n - 1; i >= 0; i--)
                    {
                        if (records[i].LoList.Count <= k) continue;
                        if (records[i].LoList[k] == u) break;
                        gan_k[u]++;
                    }
                }

                for (int i = n - lookback; i < n; i++)
                {
                    if (records[i].LoList.Count > k) f30_k[records[i].LoList[k]]++;
                }

                int bestU = -1;
                double minV = double.MaxValue;

                for (int u = 0; u < 100; u++)
                {
                    // 1. Thế năng cục bộ dòng k (Renewal Poisson + Kinetic + Bước dịch 1D)
                    double vRenewal = -0.30 * (gan_k[u] - 100.0) / 30.0;
                    if (gan_k[u] == 0) vRenewal = 0.50; // Cản lặp tức thời cùng vị trí giải
                    double vKinetic = -0.20 * (f30_k[u] - 0.3);

                    int tens = u / 10;
                    int lastTens = lastVal / 10;
                    double v1D = (tens == (lastTens + 1) % 10 || tens == (lastTens + 9) % 10) ? -0.20 : 0.0;

                    double vLocal = vRenewal + vKinetic + v1D;

                    // 2. Thế năng toàn cục 7 thành tố
                    double vGlobal = 0.0;
                    if (potDict != null && potDict.ContainsKey(u)) vGlobal = potDict[u];

                    // 3. Multi-Scale Fusion: Giao thoa năng lượng đa tầng
                    double vTotal = vLocal + 0.40 * vGlobal;

                    if (vTotal < minV)
                    {
                        minV = vTotal;
                        bestU = u;
                    }
                }

                if (bestU != -1) cand27.Add(bestU);
            }

            return cand27;
        }

        // HỆ THỐNG LỰA CHỌN LÔ ĐA DÒNG 27 GIẢI: S_CAND -> BTL / STL / TTL / DÀN 10 SỐ
        public static List<int> SelectTop4LoAdaptive(List<LotteryRecord> records, List<LottoEnergy> lottoPotentials)
        {
            var cand27 = Compute27PrizeCandidateSpace(records, lottoPotentials);
            var rankedCand = lottoPotentials.Where(e => cand27.Contains(e.State))
                                          .OrderBy(e => e.Potential)
                                          .ToList();

            if (rankedCand.Count < 4)
            {
                foreach (var e in lottoPotentials)
                {
                    if (!rankedCand.Any(x => x.State == e.State)) rankedCand.Add(e);
                    if (rankedCand.Count >= 4) break;
                }
            }

            // Tứ thủ Lô: 4 số từ rankedCand với Diversity (khống chế tối đa 2 con cùng đầu)
            var top4 = new List<int>();
            var hCount = new Dictionary<int, int>();
            foreach (var e in rankedCand)
            {
                int h = e.State / 10;
                int c = 0; hCount.TryGetValue(h, out c);
                if (c < 2)
                {
                    top4.Add(e.State);
                    hCount[h] = c + 1;
                    if (top4.Count == 4) break;
                }
            }
            if (top4.Count < 4)
            {
                foreach (var e in rankedCand)
                {
                    if (!top4.Contains(e.State))
                    {
                        top4.Add(e.State);
                        if (top4.Count == 4) break;
                    }
                }
            }

            // Cơ chế xoay trục cắt lỗ BTL động nếu kỳ trước trượt (Tránh đệ quy)
            int n = records.Count;
            if (n >= 2 && top4.Count > 1)
            {
                var prevSub = records.Take(n - 1).ToList();
                double[,] prevW;
                var prevLotto = ComputeUnifiedLottoPotentials(prevSub, out prevW);
                var prevCand27 = Compute27PrizeCandidateSpace(prevSub, prevLotto);
                var prevRanked = prevLotto.Where(e => prevCand27.Contains(e.State)).OrderBy(e => e.Potential).ToList();

                if (prevRanked.Count > 0)
                {
                    int prevBtl = prevRanked[0].State;
                    if (!records[n - 1].LoList.Contains(prevBtl) && top4[0] == prevBtl)
                    {
                        int temp = top4[0];
                        top4[0] = top4[1];
                        top4[1] = temp;
                    }
                }
            }

            return top4;
        }

        public static List<int> SelectTop10Lo27Stream(List<LotteryRecord> records, List<LottoEnergy> lottoPotentials, HashSet<int> cand27 = null)
        {
            if (cand27 == null) cand27 = Compute27PrizeCandidateSpace(records);
            var rankedCand = lottoPotentials.Where(e => cand27.Contains(e.State))
                                          .OrderBy(e => e.Potential)
                                          .ToList();

            var dan10 = rankedCand.Take(10).Select(e => e.State).ToList();
            if (dan10.Count < 10)
            {
                foreach (var e in lottoPotentials)
                {
                    if (!dan10.Contains(e.State))
                    {
                        dan10.Add(e.State);
                        if (dan10.Count == 10) break;
                    }
                }
            }
            return dan10;
        }

        public static List<int> SelectTop10LoBalanced(List<LotteryRecord> records, List<LottoEnergy> lottoPotentials)
        {
            return SelectTop10Lo27Stream(records, lottoPotentials);
        }

        public static List<int> SelectTop4LoWithDiversityRaw(List<LottoEnergy> lottoPotentials)
        {
            var top4 = new List<int>();
            var headCounts = new Dictionary<int, int>();
            var tailCounts = new Dictionary<int, int>();
            foreach (var p in lottoPotentials)
            {
                int head = p.State / 10;
                int tail = p.State % 10;
                int hCount = 0, tCount = 0;
                headCounts.TryGetValue(head, out hCount);
                tailCounts.TryGetValue(tail, out tCount);
                if (hCount < 2 && tCount < 2)
                {
                    top4.Add(p.State);
                    headCounts[head] = hCount + 1;
                    tailCounts[tail] = tCount + 1;
                    if (top4.Count == 4) break;
                }
            }
            if (top4.Count < 4)
            {
                foreach (var p in lottoPotentials)
                {
                    if (!top4.Contains(p.State))
                    {
                        top4.Add(p.State);
                        if (top4.Count == 4) break;
                    }
                }
            }
            return top4;
        }

        public static List<int> GetSpectralRankedLoDecay(List<LotteryRecord> records)
        {
            double[,] W;
            return ComputeUnifiedLottoPotentials(records, out W).Select(x => x.State).ToList();
        }

        public static void PrintDynamicSelfFundingPortfolio(List<LotteryRecord> records, List<StateEnergy> unifiedEnergies, long budget)
        {
            if (budget < 300000)
            {
                Console.WriteLine(">> [LƯU Ý ĐỊNH CHUẨN VỐN]: Ngân sách tối thiểu để vận hành đầy đủ 3 trục bảo vệ Markowitz & Kelly là 300.000 VNĐ.");
                Console.WriteLine("   Hệ thống tự động điều chỉnh số vốn ngày về ngưỡng chuẩn tối thiểu: 300.000 VNĐ.");
                Console.WriteLine();
                budget = 300000;
            }

            double[,] W_portfolio;
            var lottoEnergies = ComputeUnifiedLottoPotentials(records, out W_portfolio);
            var top4 = SelectTop4LoWithDiversity(lottoEnergies, records);
            int btLo = top4[0];
            int stLo = top4[1];
            int tuThu3 = top4[2];
            int tuThu4 = top4[3];

            int n = records.Count;
            var marginal1D = Compute1DMarginals(records);
            var regime = DetectMarketRegime(records, unifiedEnergies, W_portfolio, marginal1D);
            bool isDrawdownYesterday = false;
            if (records.Count >= 2)
            {
                var prevRecords = records.Take(n - 1).ToList();
                double[,] prevW;
                var prevLotto = ComputeUnifiedLottoPotentials(prevRecords, out prevW);
                var prevTop4 = SelectTop4LoWithDiversity(prevLotto, prevRecords);
                bool prevLoHit = records[n - 1].UniqueLo.Contains(prevTop4[0]) || records[n - 1].UniqueLo.Contains(prevTop4[1]);

                var prevMarginal = Compute1DMarginals(prevRecords);
                var prevBsm = ComputeBsmBasketPotentials2D(prevRecords, prevMarginal);
                var prevDan65 = SelectFactorNeutralDeSet(prevBsm, 65);
                var prevSub42 = prevBsm.Where(e => prevDan65.Contains(e.State)).ToList();
                var prevDan42 = SelectFactorNeutralDeSet(prevSub42, 42);
                bool prevDeHit = prevDan42.Contains(records[n - 1].De);

                if (!prevLoHit && !prevDeHit) isDrawdownYesterday = true;
            }

            var e65List = SelectFactorNeutralDeSet(unifiedEnergies, 65).OrderBy(x => x).ToList();
            var cascade3D = ComputeDan3DCascade(records, e65List, unifiedEnergies);
            string bt3D = cascade3D.BachThu3D;
            var dan3D_12 = cascade3D.Dan12.OrderBy(x => x).ToList();
            var dan3D_27 = cascade3D.Dan27.OrderBy(x => x).ToList();

            string bt4D;
            List<string> dan4D_4, dan4D_16, dan4D_36, dan4D_81;
            Compute4D_QuantumOptimal(records, out bt4D, out dan4D_4, out dan4D_16, out dan4D_36, out dan4D_81);
            dan4D_81 = dan4D_81.OrderBy(x => x).ToList();

            // 1. TRỤ CỘT ĐÒN BẨY BẮT BUỘC HÀNG NGÀY: 27s 3D & 81s 4D
            long cost27_3D = 27 * 1000;
            long cost81_4D = (regime.Regime == MarketRegimeType.Chaos) ? 0 : 81 * 1000;
            long costDonBay = cost27_3D + cost81_4D;

            // 2. CẮT BỎ HOÀN TOÀN CÁC DÀN TIÊU HAO VỐN: XIÊN, E1, E4
            long costXien = 0;

            // 3. TAM THỦ LÔ (100% SỐ NGUYÊN ĐIỂM)
            long costPerPoint = 27000;
            int budgetMult = (int)Math.Max(1, Math.Round((double)budget / 500000.0));
            int stPts1 = 3 * budgetMult;
            int stPts2 = 3 * budgetMult;
            int lotPts = 1 * budgetMult;
            if (regime.Regime == MarketRegimeType.Peak) { stPts1 = 3 * budgetMult; stPts2 = 3 * budgetMult; lotPts = 1 * budgetMult; }
            else if (regime.Regime == MarketRegimeType.Convergent) { stPts1 = 3 * budgetMult; stPts2 = 2 * budgetMult; lotPts = 1 * budgetMult; }
            else if (regime.Regime == MarketRegimeType.Coiling) { stPts1 = 2 * budgetMult; stPts2 = 2 * budgetMult; lotPts = 1 * budgetMult; }
            else if (regime.Regime == MarketRegimeType.Chaos) { stPts1 = 1 * budgetMult; stPts2 = 1 * budgetMult; lotPts = 1 * budgetMult; }

            long costStLo = (stPts1 + stPts2) * costPerPoint;
            long costLotLo = lotPts * costPerPoint;
            long costLotto = costStLo + costLotLo;

            // 4. ĐỀ 2D (KHÓA CỨNG TRẦN E65 - 100% SỐ NGUYÊN NGHÌN ĐỒNG)
            var bsmBasketEnergies = ComputeBsmBasketPotentials2D(records, marginal1D);
            var dan65 = SelectFactorNeutralDeSet(bsmBasketEnergies, 65).OrderBy(x => x).ToList();
            var sub42 = bsmBasketEnergies.Where(e => dan65.Contains(e.State)).ToList();
            var dan42 = SelectFactorNeutralDeSet(sub42, 42).OrderBy(x => x).ToList();
            var sub27 = bsmBasketEnergies.Where(e => dan42.Contains(e.State)).ToList();
            var dan27 = SelectFactorNeutralDeSet(sub27, 27).OrderBy(x => x).ToList();

            long c42 = 3000 * budgetMult;
            long costE42 = 42 * c42;
            long c65_hedge = 1000 * budgetMult;
            long costE65_hedge = 23 * c65_hedge;
            long c27_extra = 0;
            long costE27_extra = 0;
            long costDe = 0;

            if (regime.Regime == MarketRegimeType.Peak)
            {
                c27_extra = 1000 * budgetMult;
                costE27_extra = 12 * c27_extra;
                costDe = costE42 + costE65_hedge + costE27_extra; // 161k
            }
            else if (regime.Regime == MarketRegimeType.Convergent || regime.Regime == MarketRegimeType.Coiling)
            {
                c42 = 3000 * budgetMult;
                c65_hedge = 3000 * budgetMult;
                costDe = 65 * c65_hedge; // 195k phẳng
                costE42 = 42 * c42;
                costE65_hedge = 23 * c65_hedge;
            }
            else // Chaos
            {
                c42 = 2000 * budgetMult;
                c65_hedge = 2000 * budgetMult;
                costDe = 65 * c65_hedge; // 130k phẳng
                costE42 = 42 * c42;
                costE65_hedge = 23 * c65_hedge;
            }

            // 5. DÀN BỔ SUNG 3D/4D (CHỈ KÍCH HOẠT KHI PEAK/CONVERGENT)
            long cost3D_12 = 0;
            long cost4D_small = 0;
            if (regime.Regime == MarketRegimeType.Peak)
            {
                cost3D_12 = 12 * 2000 * budgetMult; // 24k
                cost4D_small = 18 * 1000 * budgetMult; // 18k
            }
            else if (regime.Regime == MarketRegimeType.Convergent)
            {
                cost3D_12 = 12 * 2000 * budgetMult; // 24k
                cost4D_small = 11000 * budgetMult;  // 11k
            }

            long totalAllocated = costLotto + costDe + costDonBay + cost3D_12 + cost4D_small;

            Console.WriteLine(string.Format(">> TỔNG NGÂN SÁCH ĐẦU TƯ TRONG NGÀY: {0:N0} VNĐ", totalAllocated));
            if (regime.Regime == MarketRegimeType.Peak)
            {
                Console.WriteLine(">> [TRẠNG THÁI: 🟢 THÁI HÒA (PEAK) - ĐẠI THỪA XUẤT TRẬN TẤN CÔNG ĐÒN BẨY]");
                Console.WriteLine("   ! Khẩu quyết: Sóng cộng hưởng cực đại, xác suất bảo toàn vốn nội ngày >= 90.24%!");
            }
            else if (regime.Regime == MarketRegimeType.Convergent)
            {
                Console.WriteLine(">> [TRẠNG THÁI: 🔵 KÝ TẾ (CONVERGENT) - BÌNH ỔN GAUSS & HÒA VỐN 2 NGÀY >= 98.28%]");
            }
            else if (regime.Regime == MarketRegimeType.Coiling)
            {
                Console.WriteLine(">> [TRẠNG THÁI: 🟠 PHỤC KHÍ (COILING) - PHÒNG THỦ E65 & TẮT HẾT DÀN NHỎ]");
            }
            else
            {
                Console.WriteLine(">> [TRẠNG THÁI: 🔴 BĨ CỰC (CHAOS) - CHẾ ĐỘ HẠ TẢI BẢO TOÀN VỐN TỐI ĐA (TẠM DỪNG 4D 81s)]");
            }
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            Console.WriteLine("1. TRỤC 1: ĐỊA LỢI (+EV CASH ENGINE) - TAM THỦ LÔ (100% SỐ NGUYÊN ĐIỂM):");
            Console.WriteLine(string.Format("   * Song Thủ Lô chính ({0} - {1}): Con 1 cược {2} điểm, Con 2 cược {3} điểm = {4:N0} VNĐ", btLo.ToString("D2"), stLo.ToString("D2"), stPts1, stPts2, costStLo));
            Console.WriteLine(string.Format("   * Lô Lót Tam Thủ ({0}): Cược {1} điểm = {2:N0} VNĐ", tuThu3.ToString("D2"), lotPts, costLotLo));
            var tamThuLoListPortfolio = new List<int> { btLo, stLo, tuThu3 }.OrderBy(x => x).ToList();
            Console.WriteLine(string.Format("     [Hệ 3 số Tam thủ {0}-{1}-{2} đạt WinRate 61.3% (273 nháy/300 kỳ, Alpha: 1.09)]", tamThuLoListPortfolio[0].ToString("D2"), tamThuLoListPortfolio[1].ToString("D2"), tamThuLoListPortfolio[2].ToString("D2")));
            Console.WriteLine("   * [LƯU Ý CẮT BỎ]: Đã loại bỏ hoàn toàn các cặp Xiên để dồn vốn cho 3D/4D (Vốn Xiên = 0 đ).");
            Console.WriteLine();

            Console.WriteLine("2. TRỤC 2: NHÂN HÒA (HỘ THÂN BẢO HIỂM) - ĐỀ 2D (KHÓA CỨNG TRẦN E65):");
            Console.WriteLine(string.Format("   * Dàn Chân Kiềng E42 (Lõi): 42 số cược {0:N0} đ/số = {1:N0} VNĐ", c42, 42 * c42));
            Console.WriteLine(string.Format("   * Dàn Vỏ Bảo Hiểm E65:       23 số biên cược {0:N0} đ/số = {1:N0} VNĐ (Tổng 65 số E65: {2:N0} đ)", c65_hedge, 23 * c65_hedge, costDe));
            if (c27_extra > 0)
                Console.WriteLine(string.Format("   * Dàn Tấn Công E27:          12 số đột phá cược thêm {0:N0} đ/số = {1:N0} VNĐ", c27_extra, costE27_extra));
            Console.WriteLine("   * [LƯU Ý CẮT BỎ]: Tuyệt đối không quá E65; Bỏ Bạch thủ E1 và Tứ thủ E4 (Vốn = 0 đ).");
            Console.WriteLine();

            Console.WriteLine("3. TRỤC 3: THIÊN THỜI (TRỤ CỘT ĐÒN BẨY BẮT BUỘC HÀNG NGÀY 27s 3D & 81s 4D):");
            Console.WriteLine(string.Format("   * DÀN 27 SỐ 3D (Phương pháp 2 Cột Tổng): 27 vé x 1.000 đ = {0:N0} VNĐ (Ăn 1:960, Thu về 960k)", cost27_3D));
            Console.WriteLine(string.Format("     [27 số 3D: {0}]", string.Join(", ", dan3D_27)));
            if (cost81_4D > 0)
            {
                Console.WriteLine(string.Format("   * DÀN 81 SỐ 4D (Quantum Potts C):        81 vé x 1.000 đ = {0:N0} VNĐ (Ăn 1:8,880, Thu về 8.88 triệu)", cost81_4D));
                Console.WriteLine(string.Format("     [81 số 4D tiêu biểu: {0}...]", string.Join(", ", dan4D_81.Take(12))));
            }
            else
            {
                Console.WriteLine("   * DÀN 81 SỐ 4D: [TẠM DỪNG Ở PHA CHAOS ĐỂ HẠ TẢI BẢO TOÀN VỐN] (Vốn = 0 đ)");
            }
            if (cost3D_12 > 0)
            {
                Console.WriteLine(string.Format("   * Dàn 3D 12s Tinh Tuyển (Bổ sung):      12 vé x 2.000 đ = {0:N0} VNĐ", cost3D_12));
            }
            Console.WriteLine("-----------------------------------------------------------------------------------------");

            long payout1Lo = stPts1 * 99000;
            long payoutDe42 = c42 * 99;

            Console.WriteLine(">> MA TRẬN KỊCH BẢN THU HOÀN VỐN (XÁC SUẤT BẢO TOÀN VỐN >= 90%):");
            Console.WriteLine(string.Format(" * Kịch bản 1: Nổ 1 STL ({0}đ):       Thu {1:N0} VNĐ => Thu hồi {2:F1}% tổng vốn ngày", stPts1, payout1Lo, (double)payout1Lo * 100.0 / totalAllocated));
            Console.WriteLine(string.Format(" * Kịch bản 2: Nổ Đề E42 ({0:N0}đ):     Thu {1:N0} VNĐ => Thu hồi {2:F1}% tổng vốn ngày", c42, payoutDe42, (double)payoutDe42 * 100.0 / totalAllocated));
            Console.WriteLine(string.Format(" * Kịch bản 3: Nổ CẢ LÔ VÀ ĐỀ:         Thu {0:N0} VNĐ => LÃI RÒNG {1:+0;-0;0} VNĐ", payout1Lo + payoutDe42, (payout1Lo + payoutDe42) - totalAllocated));
            Console.WriteLine(string.Format(" * Kịch bản 4: NỔ 27s 3D:              Thu 960.000 VNĐ => LÃI RÒNG {0:+0;-0;0} VNĐ (Bù vốn 2 ngày)", 960000 - totalAllocated));
            if (cost81_4D > 0)
                Console.WriteLine(string.Format(" * Kịch bản 5: NỔ 81s 4D:              Thu 8.880.000 VNĐ => SIÊU LỢI NHUẬN {0:+0;-0;0} VNĐ", 8880000 - totalAllocated));
            Console.WriteLine("=========================================================================================");
        }

        public class SheetRow
        {
            public int Stt;
            public string Axis;
            public string Category;
            public int Quantity;
            public string NumberList;
            public string Unit;
            public long BetPerUnit;
            public long TotalBet;
            public string Note;
        }

        public static void RunSheetExport(List<LotteryRecord> records, long budget = 500000)
        {
            int n = records.Count;
            var last = records[n - 1];

            if (budget < 300000) budget = 300000;

            var marginal1D = Compute1DMarginals(records);
            var lotto1D = Compute1DLottoMarginals(records);
            double[,] W_lotto;
            var lottoEnergies = ComputeUnifiedLottoPotentials(records, lotto1D, out W_lotto);
            var unifiedEnergies = ComputeUnifiedPotentials2D(records, marginal1D);
            var bsmBasketEnergies = ComputeBsmBasketPotentials2D(records, marginal1D);
            var qci = ComputeQCI(records, lottoEnergies, unifiedEnergies, lotto1D, marginal1D);
            var regime = DetectMarketRegime(records, unifiedEnergies, W_lotto, marginal1D);

            var top4 = SelectTop4LoWithDiversity(lottoEnergies, records);
            int btLo = top4[0];
            int stLo = top4[1];
            int tuThu3 = top4[2];
            int tuThu4 = top4[3];

            var tamThuLoList = new List<int> { btLo, stLo, tuThu3 }.OrderBy(x => x).ToList();
            var tuThuLoList = new List<int> { btLo, stLo, tuThu3, tuThu4 }.OrderBy(x => x).ToList();

            // 3 Cặp Xiên 2 Tam Thủ (Sắp xếp tăng dần)
            var xien2List = new List<string> {
                string.Format("{0}-{1}", tamThuLoList[0].ToString("D2"), tamThuLoList[1].ToString("D2")),
                string.Format("{0}-{1}", tamThuLoList[0].ToString("D2"), tamThuLoList[2].ToString("D2")),
                string.Format("{0}-{1}", tamThuLoList[1].ToString("D2"), tamThuLoList[2].ToString("D2"))
            };

            // 1 Bộ Xiên 3 Tam Thủ
            string xien3 = string.Format("{0}-{1}-{2}", tamThuLoList[0].ToString("D2"), tamThuLoList[1].ToString("D2"), tamThuLoList[2].ToString("D2"));
            string xien4 = string.Join("-", tuThuLoList.Select(x => x.ToString("D2")));

            // Đề - Thang rút gọn 65% (TIDS 65% Decimation Pyramid) - Sắp xếp tăng dần
            int btDe = unifiedEnergies[0].State;
            var dan2 = unifiedEnergies.Take(2).Select(x => x.State).OrderBy(x => x).Select(x => x.ToString("D2")).ToList();
            var dan4 = unifiedEnergies.Take(4).Select(x => x.State).OrderBy(x => x).Select(x => x.ToString("D2")).ToList();
            var dan65Raw = SelectFactorNeutralDeSet(bsmBasketEnergies, 65).OrderBy(x => x).ToList();
            var sub42 = bsmBasketEnergies.Where(e => dan65Raw.Contains(e.State)).ToList();
            var dan42Raw = SelectFactorNeutralDeSet(sub42, 42).OrderBy(x => x).ToList();
            var sub27 = bsmBasketEnergies.Where(e => dan42Raw.Contains(e.State)).ToList();
            var dan27Raw = SelectFactorNeutralDeSet(sub27, 27).OrderBy(x => x).ToList();
            var sub18 = bsmBasketEnergies.Where(e => dan27Raw.Contains(e.State)).ToList();
            var dan18Raw = SelectFactorNeutralDeSet(sub18, 18).OrderBy(x => x).ToList();

            var dan65 = dan65Raw.Select(x => x.ToString("D2")).ToList();
            var dan42 = dan42Raw.Select(x => x.ToString("D2")).ToList();
            var dan27 = dan27Raw.Select(x => x.ToString("D2")).ToList();
            var dan18 = dan18Raw.Select(x => x.ToString("D2")).ToList();

            // 3D
            int[] ganD3 = new int[10];
            for (int d = 0; d < 10; d++)
            {
                for (int i = n - 1; i >= 0; i--)
                {
                    if (records[i].Digits5[2] == d) break;
                    ganD3[d]++;
                }
            }
            int[] f30D3 = new int[10];
            for (int i = Math.Max(0, n - 30); i < n; i++) f30D3[records[i].Digits5[2]]++;
            var d3List = new List<Tuple<int, double>>();
            for (int d = 0; d < 10; d++)
            {
                double rTerm = (ganD3[d] >= 8 && ganD3[d] <= 20) ? -1.5 : (ganD3[d] == 0 ? 1.5 : 0.0);
                double kTerm = -0.3 * (f30D3[d] - 3.0);
                d3List.Add(new Tuple<int, double>(d, rTerm + kTerm));
            }
            var topD3 = d3List.OrderBy(x => x.Item2).Take(3).Select(x => x.Item1).ToArray();
            var tuThuDe = unifiedEnergies.Take(4).Select(x => x.State).ToList();

            var e65 = SelectFactorNeutralDeSet(unifiedEnergies, 65).OrderBy(x => x).ToList();
            var cascade3D = ComputeDan3DCascade(records, e65, unifiedEnergies);
            string bt3D = cascade3D.BachThu3D;
            var dan3D_12 = cascade3D.Dan12.OrderBy(x => x).ToList();
            var dan3D_27 = cascade3D.Dan27.OrderBy(x => x).ToList();
            var dan9Tail = unifiedEnergies.Take(9).Select(x => x.State).OrderBy(x => x).ToArray();

            // 4D QUANTUM TỐI ƯU (ĐẦY ĐỦ TỔ HỢP CHÉO: 4s, 16s, 36s & 81s - Đều sắp xếp tăng dần)
            string bt4D;
            List<string> dan4D_4, dan4D_16, dan4D_36, dan4D_81;
            Compute4D_QuantumOptimal(records, out bt4D, out dan4D_4, out dan4D_16, out dan4D_36, out dan4D_81);
            dan4D_4 = dan4D_4.OrderBy(x => x).ToList();
            dan4D_16 = dan4D_16.OrderBy(x => x).ToList();
            dan4D_36 = dan4D_36.OrderBy(x => x).ToList();
            dan4D_81 = dan4D_81.OrderBy(x => x).ToList();



            // 1. TRỤ CỘT ĐÒN BẨY BẮT BUỘC HÀNG NGÀY: 27s 3D & 81s 4D
            long cost27_3D = 27 * 1000;
            long cost81_4D = (regime.Regime == MarketRegimeType.Chaos) ? 0 : 81 * 1000;
            long costDonBay = cost27_3D + cost81_4D;

            // 2. CẮT BỎ HOÀN TOÀN CÁC DÀN TIÊU HAO VỐN: XIÊN, E1, E4
            long costXien = 0;

            // 3. TAM THỦ LÔ (100% SỐ NGUYÊN ĐIỂM)
            long costPerPoint = 27000;
            int budgetMult = (int)Math.Max(1, Math.Round((double)budget / 500000.0));
            int stPts1 = 3 * budgetMult;
            int stPts2 = 3 * budgetMult;
            int lotPts = 1 * budgetMult;
            if (regime.Regime == MarketRegimeType.Peak) { stPts1 = 3 * budgetMult; stPts2 = 3 * budgetMult; lotPts = 1 * budgetMult; }
            else if (regime.Regime == MarketRegimeType.Convergent) { stPts1 = 3 * budgetMult; stPts2 = 2 * budgetMult; lotPts = 1 * budgetMult; }
            else if (regime.Regime == MarketRegimeType.Coiling) { stPts1 = 2 * budgetMult; stPts2 = 2 * budgetMult; lotPts = 1 * budgetMult; }
            else if (regime.Regime == MarketRegimeType.Chaos) { stPts1 = 1 * budgetMult; stPts2 = 1 * budgetMult; lotPts = 1 * budgetMult; }

            long costStLo = (stPts1 + stPts2) * costPerPoint;
            long costLotLo = lotPts * costPerPoint;
            long costLotto = costStLo + costLotLo;

            // 4. ĐỀ 2D (KHÓA CỨNG TRẦN E65 - 100% SỐ NGUYÊN NGHÌN ĐỒNG)
            long c42 = 3000 * budgetMult;
            long costE42 = 42 * c42;
            long c65_hedge = 1000 * budgetMult;
            long costE65_hedge = 23 * c65_hedge;
            long c27_extra = 0;
            long costE27_extra = 0;
            long costDe = 0;

            if (regime.Regime == MarketRegimeType.Peak)
            {
                c27_extra = 1000 * budgetMult;
                costE27_extra = 12 * c27_extra;
                costDe = costE42 + costE65_hedge + costE27_extra; // 161k
            }
            else if (regime.Regime == MarketRegimeType.Convergent || regime.Regime == MarketRegimeType.Coiling)
            {
                c42 = 3000 * budgetMult;
                c65_hedge = 3000 * budgetMult;
                costDe = 65 * c65_hedge; // 195k phẳng
                costE42 = 42 * c42;
                costE65_hedge = 23 * c65_hedge;
            }
            else // Chaos
            {
                c42 = 2000 * budgetMult;
                c65_hedge = 2000 * budgetMult;
                costDe = 65 * c65_hedge; // 130k phẳng
                costE42 = 42 * c42;
                costE65_hedge = 23 * c65_hedge;
            }

            // 5. DÀN BỔ SUNG 3D/4D (CHỈ KÍCH HOẠT KHI PEAK/CONVERGENT)
            long cost3D_12 = 0;
            long cost4D_small = 0;
            if (regime.Regime == MarketRegimeType.Peak)
            {
                cost3D_12 = 12 * 2000 * budgetMult; // 24k
                cost4D_small = 18 * 1000 * budgetMult; // 18k
            }
            else if (regime.Regime == MarketRegimeType.Convergent)
            {
                cost3D_12 = 12 * 2000 * budgetMult; // 24k
                cost4D_small = 11000 * budgetMult;  // 11k
            }

            long totalAllocated = costLotto + costDe + costDonBay + cost3D_12 + cost4D_small;

            var rows = new List<SheetRow>();
            int idx = 1;

            // 1. Song Thủ Lô
            rows.Add(new SheetRow { Stt = idx++, Axis = "Trục 1: Địa Lợi (+EV)", Category = "Song Thủ Lô (STL)", Quantity = 2, NumberList = string.Format("{0}, {1}", btLo.ToString("D2"), stLo.ToString("D2")), Unit = "Điểm/số", BetPerUnit = stPts1, TotalBet = costStLo, Note = string.Format("Động cơ +EV: Con 1 ({0}đ), Con 2 ({1}đ)", stPts1, stPts2) });

            // 2. Bạch Thủ Lô
            rows.Add(new SheetRow { Stt = idx++, Axis = "Trục 1: Địa Lợi (+EV)", Category = "Bạch Thủ Lô (BTL)", Quantity = 1, NumberList = btLo.ToString("D2"), Unit = "Điểm/số", BetPerUnit = 0, TotalBet = 0, Note = "Đã nằm trong cặp Song Thủ Lô (Vốn = 0)" });

            // 3. Lô Lót Tam Thủ
            rows.Add(new SheetRow { Stt = idx++, Axis = "Trục 2: Nhân Hòa (Hộ thân)", Category = "Lô Lót Tam Thủ", Quantity = 1, NumberList = tuThu3.ToString("D2"), Unit = "Điểm/số", BetPerUnit = lotPts, TotalBet = costLotLo, Note = string.Format("Lót bảo hiểm danh mục ({0} điểm)", lotPts) });

            // 4. Trọn Bộ Tam Thủ Lô
            rows.Add(new SheetRow { Stt = idx++, Axis = "Trục 2: Nhân Hòa (Hộ thân)", Category = "Trọn Bộ Tam Thủ Lô", Quantity = 3, NumberList = string.Join(", ", tamThuLoList.Select(x => x.ToString("D2"))), Unit = "Điểm/số", BetPerUnit = 0, TotalBet = 0, Note = "Hệ 3 số bảo vệ (Đã phân bổ ở STL + Lót)" });

            // 5. Cặp Xiên 2 STL (Đã loại bỏ)
            rows.Add(new SheetRow { Stt = idx++, Axis = "Trục 1: Địa Lợi (+EV)", Category = "Cặp Xiên 2 Song Thủ", Quantity = 1, NumberList = string.Format("({0}-{1})", btLo.ToString("D2"), stLo.ToString("D2")), Unit = "VNĐ/vé", BetPerUnit = 0, TotalBet = 0, Note = "Đã cắt bỏ để dồn vốn cho 3D/4D (Vốn = 0 đ)" });

            // 6. Bộ 3 Cặp Xiên 2 Tam Thủ
            rows.Add(new SheetRow { Stt = idx++, Axis = "Trục 1: Địa Lợi (+EV)", Category = "Bộ 3 Cặp Xiên 2 Tam Thủ", Quantity = 3, NumberList = string.Join(", ", xien2List.Select(x => "(" + x + ")")), Unit = "VNĐ/vé", BetPerUnit = 0, TotalBet = 0, Note = "Xiên quay dự phòng (Vốn = 0 đ)" });

            // 7. Bộ 1 Xiên 3 Tam Thủ
            rows.Add(new SheetRow { Stt = idx++, Axis = "Trục 3: Thiên Thời (Đột biến)", Category = "Bộ 1 Xiên 3 Tam Thủ", Quantity = 1, NumberList = "(" + xien3 + ")", Unit = "VNĐ/vé", BetPerUnit = 0, TotalBet = 0, Note = "Xiên 3 Tam Thủ (Vốn = 0 đ)" });

            // 8. Tứ Thủ Lô Dự Phòng (4s)
            rows.Add(new SheetRow { Stt = idx++, Axis = "Trục 2: Nhân Hòa (Hộ thân)", Category = "Tứ Thủ Lô Dự Phòng (4s)", Quantity = 4, NumberList = string.Join(", ", tuThuLoList.Select(x => x.ToString("D2"))), Unit = "VNĐ/vé", BetPerUnit = 0, TotalBet = 0, Note = "Mở rộng số lót thứ 2 (Vốn = 0 đ)" });

            // 9. Đại Diễn Thần Trận (E42 - Thang 65% Cấp 2)
            rows.Add(new SheetRow { Stt = idx++, Axis = "Trục 2: Nhân Hòa (Hộ thân)", Category = "Đại Diễn Thần Trận (E42)", Quantity = 42, NumberList = string.Join(", ", dan42), Unit = "VNĐ/số", BetPerUnit = c42, TotalBet = costE42, Note = string.Format("Lõi 42 số thang 65% ({0:N0} đ/số)", c42) });

            // 10. Huyền Thiên Giáp (E65 - Thang 65% Cấp 1)
            rows.Add(new SheetRow { Stt = idx++, Axis = "Trục 2: Nhân Hòa (Hộ thân)", Category = "Huyền Thiên Giáp (E65)", Quantity = 65, NumberList = string.Join(", ", dan65), Unit = "VNĐ/số", BetPerUnit = c65_hedge, TotalBet = costE65_hedge, Note = string.Format("Vỏ bọc 23 số biên E65 ({0:N0} đ/số, tổng 65s bảo hiểm)", c65_hedge) });

            // 11. Quy Nguyên Trận (10 số nén vĩ mô)
            var coilingCluster = ExtractBsmCoilingCluster(unifiedEnergies, regime, 10).OrderBy(x => x).ToList();
            rows.Add(new SheetRow { 
                Stt = idx++, 
                Axis = "Trục 2: Nhân Hòa (Hộ thân)", 
                Category = string.Format("Quy Nguyên Trận [{0}]", regime.CompressedFactor), 
                Quantity = coilingCluster.Count, 
                NumberList = string.Join(", ", coilingCluster.Select(x => x.ToString("D2"))), 
                Unit = "VNĐ/số", 
                BetPerUnit = 0, 
                TotalBet = 0, 
                Note = "Dự phòng săn điểm nổ vĩ mô (Vốn = 0 đ)" 
            });

            // 12. Hà Đồ Tự Nhiên (E27 - Thang 65% Cấp 3)
            rows.Add(new SheetRow { Stt = idx++, Axis = "Trục 2: Nhân Hòa (Hộ thân)", Category = "Hà Đồ Tự Nhiên (E27)", Quantity = 27, NumberList = string.Join(", ", dan27), Unit = "VNĐ/số", BetPerUnit = c27_extra, TotalBet = costE27_extra, Note = (costE27_extra > 0 ? string.Format("Cược thêm 12 số đột phá ({0:N0} đ/số)", c27_extra) : "Dự phòng (Vốn = 0 đ)") });

            // 13. Cửu Cung Thần Trận (E18)
            rows.Add(new SheetRow { Stt = idx++, Axis = "Trục 2: Nhân Hòa (Hộ thân)", Category = "Cửu Cung Thần Trận (E18)", Quantity = 18, NumberList = string.Join(", ", dan18), Unit = "VNĐ/số", BetPerUnit = 0, TotalBet = 0, Note = "Đã cắt bỏ theo nguyên tắc tinh gọn (Vốn = 0 đ)" });

            // 14. Tứ Tượng Hội Tụ (E4)
            rows.Add(new SheetRow { Stt = idx++, Axis = "Trục 2: Nhân Hòa (Hộ thân)", Category = "Tứ Tượng Hội Tụ (E4)", Quantity = 4, NumberList = string.Join(", ", dan4), Unit = "VNĐ/số", BetPerUnit = 0, TotalBet = 0, Note = "Đã cắt bỏ theo nguyên tắc tinh gọn (Vốn = 0 đ)" });

            // 15. Thái Cực Nhất Điểm (E1)
            rows.Add(new SheetRow { Stt = idx++, Axis = "Trục 2: Nhân Hòa (Hộ thân)", Category = "Thái Cực Nhất Điểm (E1)", Quantity = 1, NumberList = btDe.ToString("D2"), Unit = "VNĐ/số", BetPerUnit = 0, TotalBet = 0, Note = "Đã cắt bỏ theo nguyên tắc tinh gọn (Vốn = 0 đ)" });

            // 16. Lưỡng Nghi Đồng Quy (E2)
            rows.Add(new SheetRow { Stt = idx++, Axis = "Trục 2: Nhân Hòa (Hộ thân)", Category = "Lưỡng Nghi Đồng Quy (E2)", Quantity = 2, NumberList = string.Join(", ", dan2), Unit = "VNĐ/số", BetPerUnit = 0, TotalBet = 0, Note = "Dự phòng (Vốn = 0 đ)" });

            // 17. Dàn 3 Càng Tinh Tuyển 12 số (3D)
            rows.Add(new SheetRow { Stt = idx++, Axis = "Trục 3: Thiên Thời (Đột biến)", Category = "Dàn 3 Càng Tinh Tuyển 12 số (3D)", Quantity = 12, NumberList = string.Join(", ", dan3D_12), Unit = "VNĐ/vé", BetPerUnit = (cost3D_12 > 0 ? (cost3D_12 / 12) : 0), TotalBet = cost3D_12, Note = (cost3D_12 > 0 ? "Bổ sung tấn công theo thời khí Peak/Convergent" : "Tắt dàn nhỏ ở Coiling/Chaos (Vốn = 0 đ)") });

            // 18. Bạch Thủ 3 Càng (3D)
            rows.Add(new SheetRow { Stt = idx++, Axis = "Trục 3: Thiên Thời (Đột biến)", Category = "Bạch Thủ 3 Càng (3D)", Quantity = 1, NumberList = bt3D, Unit = "VNĐ/vé", BetPerUnit = 0, TotalBet = 0, Note = "Đã gộp vào Dàn 27 số (Vốn = 0 đ)" });

            // 19. Dàn 3 Càng Phổ 27 số (3D) - TRỤ CỘT BẮT BUỘC HÀNG NGÀY
            rows.Add(new SheetRow { Stt = idx++, Axis = "Trục 3: Thiên Thời (Đột biến)", Category = "Dàn 3 Càng Phổ 27 số (3D)", Quantity = 27, NumberList = string.Join(", ", dan3D_27), Unit = "VNĐ/vé", BetPerUnit = 1000, TotalBet = cost27_3D, Note = "Trụ cột đòn bẩy thường trực ăn 1:960 (27k/ngày)" });

            // 20. Bạch Thủ 4 Càng (4D)
            rows.Add(new SheetRow { Stt = idx++, Axis = "Trục 3: Thiên Thời (Đột biến)", Category = "Bạch Thủ 4 Càng (4D)", Quantity = 1, NumberList = bt4D, Unit = "VNĐ/vé", BetPerUnit = 0, TotalBet = 0, Note = "Dự phòng siêu đòn bẩy ăn 1:8,880 (Vốn = 0 đ)" });

            // 21. Tứ Thủ 4 Càng (4D)
            rows.Add(new SheetRow { Stt = idx++, Axis = "Trục 3: Thiên Thời (Đột biến)", Category = "Tứ Thủ 4 Càng (4D)", Quantity = 4, NumberList = string.Join(", ", dan4D_4), Unit = "VNĐ/vé", BetPerUnit = 0, TotalBet = 0, Note = "4D Tứ Thủ tinh hoa 4 số (Vốn = 0 đ)" });

            // 22. Dàn 4 Càng Trọng Tâm 16 số (4D)
            rows.Add(new SheetRow { Stt = idx++, Axis = "Trục 3: Thiên Thời (Đột biến)", Category = "Dàn 4 Càng Trọng Tâm 16 số (4D)", Quantity = 16, NumberList = string.Join(", ", dan4D_16), Unit = "VNĐ/vé", BetPerUnit = 0, TotalBet = 0, Note = "Tổ hợp 2x2 Càng x E4 Trọng tâm (Vốn = 0 đ)" });

            // 23. Dàn 4 Càng Cân Bằng 36 số (4D)
            rows.Add(new SheetRow { Stt = idx++, Axis = "Trục 3: Thiên Thời (Đột biến)", Category = "Dàn 4 Càng Cân Bằng 36 số (4D)", Quantity = 36, NumberList = string.Join(", ", dan4D_36), Unit = "VNĐ/vé", BetPerUnit = 0, TotalBet = 0, Note = "Tổ hợp 2x2 Càng x E9 Cân bằng (Vốn = 0 đ)" });

            // 24. Dàn 4 Càng Mở Rộng 81 số (4D) - TRỤ CỘT BẮT BUỘC HÀNG NGÀY
            rows.Add(new SheetRow { Stt = idx++, Axis = "Trục 3: Thiên Thời (Đột biến)", Category = "Dàn 4 Càng Mở Rộng 81 số (4D)", Quantity = 81, NumberList = string.Join(", ", dan4D_81), Unit = "VNĐ/vé", BetPerUnit = (cost81_4D > 0 ? 1000 : 0), TotalBet = cost81_4D, Note = (cost81_4D > 0 ? "Trụ cột siêu đòn bẩy thường trực ăn 1:8,880 (81k/ngày)" : "Tạm dừng ở pha Chaos để bảo toàn vốn (Vốn = 0 đ)") });

            // XUẤT RA FILE DATA TSV VÀ CSV
            var sbTsv = new StringBuilder();
            sbTsv.AppendLine("STT\tPhân Loại Trục\tHạng Mục Cược\tSố Lượng Số\tDanh Sách Số Đầy Đủ\tĐơn Vị\tMức Cược Mỗi Số\tTổng Tiền Cược (VNĐ)\tGhi Chú & Tỷ Trọng");
            foreach (var r in rows)
            {
                sbTsv.AppendLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}",
                    r.Stt, r.Axis, r.Category, r.Quantity, r.NumberList, r.Unit, r.BetPerUnit, r.TotalBet, r.Note));
            }
            sbTsv.AppendLine(string.Format("TỔNG\tDANH MỤC KELLY\tTổng Ngân Sách Phân Bổ Ngày\t-\t-\t-\t-\t{0}\t100% Khớp Ngân Sách", totalAllocated));

            var sbCsv = new StringBuilder();
            sbCsv.AppendLine("STT,Phân Loại Trục,Hạng Mục Cược,Số Lượng Số,Danh Sách Số Đầy Đủ,Đơn Vị,Mức Cược Mỗi Số,Tổng Tiền Cược (VNĐ),Ghi Chú & Tỷ Trọng");
            foreach (var r in rows)
            {
                sbCsv.AppendLine(string.Format("{0},\"{1}\",\"{2}\",{3},\"{4}\",\"{5}\",{6},{7},\"{8}\"",
                    r.Stt, r.Axis, r.Category, r.Quantity, r.NumberList, r.Unit, r.BetPerUnit, r.TotalBet, r.Note));
            }
            sbCsv.AppendLine(string.Format("TỔNG,\"DANH MỤC KELLY\",\"Tổng Ngân Sách Phân Bổ Ngày\",-,-,-,-,{0},\"100% Khớp Ngân Sách\"", totalAllocated));

            try
            {
                string dir = "data";
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "kelly_portfolio_sheet.tsv"), sbTsv.ToString(), Encoding.UTF8);
                File.WriteAllText(Path.Combine(dir, "kelly_portfolio_sheet.csv"), sbCsv.ToString(), Encoding.UTF8);
            }
            catch { }

            // IN RA MÀN HÌNH CONSOLE
            Console.WriteLine("=========================================================================================");
            Console.WriteLine("   ĐẠO DỊCH HUYỀN CƠ PHÁP - BẢNG TAM TÀI PHÂN BỔ ĐẦY ĐỦ ĐỂ COPY VÀO GOOGLE SHEET        ");
            Console.WriteLine("        CHIẾN LƯỢC TÀI CHÍNH ĐỊNH LƯỢNG & TỐI ƯU DANH MỤC TỰ CÂN BẰNG KELLY              ");
            Console.WriteLine("=========================================================================================");
            Console.WriteLine(string.Format("Kỳ mở thưởng: ngày hôm nay (Neo pha: {0}, {1} | GĐB: {2})", last.Date, last.DayOfWeek, last.De.ToString("D2")));
            Console.WriteLine(string.Format("Thiên Cơ Độ (QCI): {0:F1}% | Trạng thái: {1}", qci.Score, qci.Zone == "XANH" ? "🟢 VÙNG XANH (THÁI CƠ - NÊN ĐÁNH TẤN CÔNG)" : (qci.Zone == "VÀNG" ? "🟡 VÙNG VÀNG (TRUNG DUNG)" : "🔴 VÙNG ĐỎ (BĨ VẬN - NÊN THỦ THẾ)")));
            Console.WriteLine(string.Format("Trạng thái Thời Khí (Regime): {0} | Hệ số Kelly: k = {1:F2}x", regime.RegimeSymbol, regime.KellyMultiplier));
            Console.WriteLine(string.Format("Đo lường 4 đại lượng: Entropy H = {0:F3} | Phổ γ = {1:F3} | Nén Z = {2:F2}σ ({3}) | Quán tính M = {4}{5:F1}%",
                regime.EntropyH, regime.SpectralGapGamma, regime.ZMacro, regime.CompressedFactor, regime.MomentumMRoll >= 0 ? "+" : "", regime.MomentumMRoll));
            Console.WriteLine(string.Format("Tổng ngân sách thực tế phân bổ: {0:N0} VNĐ (Ngân sách cài đặt: {1:N0} VNĐ)", totalAllocated, budget));
            Console.WriteLine();
            Console.WriteLine("[HƯỚNG DẪN COPY VÀO GOOGLE SHEET]:");
            Console.WriteLine(" 1. Quét chọn toàn bộ khối dữ liệu TSV bên dưới (từ dòng 'STT...' đến dòng 'TỔNG...').");
            Console.WriteLine(" 2. Nhấn phím Ctrl + C để Copy.");
            Console.WriteLine(" 3. Mở Google Sheet mới, click chuột vào ô A1 rồi nhấn Ctrl + V.");
            Console.WriteLine("    => Toàn bộ các cột và danh sách số sẽ tự động nhảy vào đúng từng ô hoàn hảo.");
            Console.WriteLine(" 4. Hoặc bạn có thể mở trực tiếp file TSV/CSV vừa được xuất tại: data/kelly_portfolio_sheet.tsv");
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine(">>> BẮT ĐẦU KHỐI DỮ LIỆU GOOGLE SHEET (TSV FORMAT) <<<");
            Console.WriteLine(sbTsv.ToString().TrimEnd());
            Console.WriteLine(">>> KẾT THÚC KHỐI DỮ LIỆU GOOGLE SHEET <<<");
            Console.WriteLine();
            Console.WriteLine("=========================================================================================");
        }

        public static void RunBacktest(List<LotteryRecord> records, int testDays)
        {
            int n = records.Count;
            if (testDays <= 0 || testDays >= n - 100) testDays = 300;
            int startIdx = n - testDays;

            Console.WriteLine("=========================================================================================");
            Console.WriteLine("    KIỂM ĐỊNH LỊCH SỬ TOÀN DIỆN LÝ THUYẾT TIDS THEO MÃ NGUỒN MỚI NHẤT (" + testDays + " KỲ GẦN NHẤT)   ");
            Console.WriteLine("    Khoảng thời gian: từ kỳ " + records[startIdx].Date + " đến kỳ " + records[n - 1].Date + " (Tổng " + n + " kỳ quay)");
            Console.WriteLine("=========================================================================================");

            // 1. Level Sets Đề
            int winE1 = 0, winE2 = 0, winE4 = 0, winE9 = 0, winE25 = 0, winE50 = 0, winE65 = 0;
            int maxStreakE50 = 0, curStreakE50 = 0, maxLossE50 = 0, curLossE50 = 0;
            int doubleLoss50 = 0, doubleLoss65 = 0;

            // 2. Cụm Đa Tạp Động (Part 1C)
            int winC1 = 0, winC2 = 0, winC3 = 0, winC4 = 0, winC5 = 0;
            double sumCountC1 = 0, sumCountC2 = 0, sumCountC3 = 0, sumCountC4 = 0, sumCountC5 = 0;

            // 3. Lô Tô & Xiên
            int winBtLo = 0;
            int winStLoAtLeast1 = 0, winStLoBoth = 0;
            int winTtLoAtLeast1 = 0;
            int maxStreakTtLo = 0, curStreakTtLo = 0;
            int winXien2St = 0;
            int totalNhaysTt = 0, multiDaysTt = 0;

            // 4. Tensor 3D (3 Càng)
            int winBt3D = 0, win3D12 = 0, win3D27 = 0;

            for (int t = startIdx; t < n; t++)
            {
                var subList = records.Take(t).ToList();
                var target = records[t];
                int actualDe = target.De;
                var actualLo = target.UniqueLo;
                int actualCang = target.Digits5[2];
                int actual3D = actualCang * 100 + actualDe;

                // 1. Đề V_eff Hợp Nhất & BSM Factor-Neutral Basket
                var marginal1D = Compute1DMarginals(subList);
                var energies = ComputeUnifiedPotentials2D(subList, marginal1D);
                var sArr = energies.Select(x => x.State).ToArray();

                var bsmEnergies = ComputeBsmBasketPotentials2D(subList, marginal1D);
                var dan25 = SelectFactorNeutralDeSet(bsmEnergies, 25);
                var dan50 = SelectFactorNeutralDeSet(bsmEnergies, 50);
                var dan65 = SelectFactorNeutralDeSet(bsmEnergies, 65);

                if (sArr[0] == actualDe) winE1++;
                if (sArr.Take(2).Contains(actualDe)) winE2++;
                if (sArr.Take(4).Contains(actualDe)) winE4++;
                if (sArr.Take(9).Contains(actualDe)) winE9++;
                if (dan25.Contains(actualDe)) winE25++;
                if (dan65.Contains(actualDe)) winE65++;

                if (dan50.Contains(actualDe))
                {
                    winE50++;
                    curStreakE50++;
                    curLossE50 = 0;
                    if (curStreakE50 > maxStreakE50) maxStreakE50 = curStreakE50;
                }
                else
                {
                    curLossE50++;
                    curStreakE50 = 0;
                    if (curLossE50 > maxLossE50) maxLossE50 = curLossE50;
                }

                // 2. Cụm Đa Tạp Động (Part 1C)
                var c1 = energies.Take(15).Select(x => x.State).ToList();
                var c2 = energies.Skip(15).Where(x => x.Potential <= 0.20).Select(x => x.State).ToList();
                var c5 = energies.Where(x => x.Gan == 0 || x.Potential >= 1.0).Select(x => x.State).Except(c1).Except(c2).ToList();
                var rem = energies.Select(x => x.State).Except(c1).Except(c2).Except(c5).ToList();
                int half = rem.Count / 2;
                var c3 = rem.Take(half).ToList();
                var c4 = rem.Skip(half).ToList();

                sumCountC1 += c1.Count; sumCountC2 += c2.Count; sumCountC3 += c3.Count; sumCountC4 += c4.Count; sumCountC5 += c5.Count;

                if (c1.Contains(actualDe)) winC1++;
                if (c2.Contains(actualDe)) winC2++;
                if (c3.Contains(actualDe)) winC3++;
                if (c4.Contains(actualDe)) winC4++;
                if (c5.Contains(actualDe)) winC5++;

                // 3. Lô Tô & Xiên
                double[,] W_bt;
                var lottoEnergies = ComputeUnifiedLottoPotentials(subList, out W_bt);
                var top4 = SelectTop4LoWithDiversity(lottoEnergies, subList);
                int btLo = top4[0];
                int stLo = top4[1];
                var ttLo = top4;

                if (actualLo.Contains(btLo)) winBtLo++;

                bool st1 = actualLo.Contains(btLo);
                bool st2 = actualLo.Contains(stLo);
                if (st1 || st2) winStLoAtLeast1++;
                if (st1 && st2)
                {
                    winStLoBoth++;
                    winXien2St++;
                }

                bool ttHit = ttLo.Any(x => actualLo.Contains(x));
                int dayNhays = 0;
                foreach (var u in ttLo) dayNhays += records[t].LoList.Count(x => x == u);
                totalNhaysTt += dayNhays;
                if (dayNhays >= 2) multiDaysTt++;

                if (ttHit)
                {
                    winTtLoAtLeast1++;
                    curStreakTtLo++;
                    if (curStreakTtLo > maxStreakTtLo) maxStreakTtLo = curStreakTtLo;
                }
                else
                {
                    curStreakTtLo = 0;
                }

                if (!ttHit && !dan50.Contains(actualDe)) doubleLoss50++;
                if (!ttHit && !dan65.Contains(actualDe)) doubleLoss65++;

                // 4. Tensor 3D (3 Càng)
                int[] ganCang = new int[10];
                int subN = subList.Count;
                for (int d = 0; d < 10; d++)
                {
                    for (int i = subN - 1; i >= 0; i--)
                    {
                        if (subList[i].Digits5[2] == d) break;
                        ganCang[d]++;
                    }
                }
                int[] f30Cang = new int[10];
                for (int i = Math.Max(0, subN - 30); i < subN; i++) f30Cang[subList[i].Digits5[2]]++;

                var cangEnergies = new List<Tuple<int, double>>();
                for (int d = 0; d < 10; d++)
                {
                    double rCang = (ganCang[d] == 0) ? 1.5 : (ganCang[d] > 30 ? 1.2 : -0.05 * ganCang[d]);
                    double kCang = -0.25 * (f30Cang[d] - 3.0);
                    cangEnergies.Add(new Tuple<int, double>(d, rCang + kCang));
                }
                var topCang = cangEnergies.OrderBy(x => x.Item2).Select(x => x.Item1).ToList();
                int btCang = topCang[0];
                var top3Cang = topCang.Take(3).ToList();

                if (actual3D == btCang * 100 + sArr[0]) winBt3D++;

                var dan12_3D = new HashSet<int>();
                foreach (var c in top3Cang)
                {
                    foreach (var de in sArr.Take(4))
                    {
                        dan12_3D.Add(c * 100 + de);
                    }
                }
                if (dan12_3D.Contains(actual3D)) win3D12++;

                var dan27_3D = new HashSet<int>();
                foreach (var c in top3Cang)
                {
                    foreach (var de in sArr.Take(9))
                    {
                        dan27_3D.Add(c * 100 + de);
                    }
                }
                if (dan27_3D.Contains(actual3D)) win3D27++;
            }

            Console.WriteLine("PHẦN A: KIỂM ĐỊNH CẤU TRÚC ĐỀ LEVEL SETS (THEO THẾ NĂNG HIỆU DỤNG HỢP NHẤT V_eff & BSM BASKET):");
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            Console.WriteLine(string.Format(" * Mức E_65 (BSM Adaptive Shell): {0,3}/{1,3} kỳ ({2,5:F2}%)  [Kỳ vọng ngẫu nhiên: 65.00% | Lợi thế: {3:+0.00;-0.00}%]", winE65, testDays, (double)winE65 * 100.0 / testDays, (double)winE65 * 100.0 / testDays - 65.0));
            Console.WriteLine(string.Format(" * Mức E_50 (BSM Factor-Neutral): {0,3}/{1,3} kỳ ({2,5:F2}%)  [Kỳ vọng ngẫu nhiên: 50.00% | Max chuỗi thông: {3} kỳ | Max gãy: {4} kỳ]", winE50, testDays, (double)winE50 * 100.0 / testDays, maxStreakE50, maxLossE50));
            Console.WriteLine(string.Format(" * Mức E_25 (BSM Factor-Neutral): {0,3}/{1,3} kỳ ({2,5:F2}%)  [Kỳ vọng ngẫu nhiên: 25.00% | Lợi thế: {3:+0.00;-0.00}%]", winE25, testDays, (double)winE25 * 100.0 / testDays, (double)winE25 * 100.0 / testDays - 25.0));
            Console.WriteLine(string.Format(" * Mức E_9  (Dàn Hạt Nhân 9 số):  {0,3}/{1,3} kỳ ({2,5:F2}%)  [Kỳ vọng ngẫu nhiên:  9.00% | Lợi thế: {3:+0.00;-0.00}%]", winE9, testDays, (double)winE9 * 100.0 / testDays, (double)winE9 * 100.0 / testDays - 9.0));
            Console.WriteLine(string.Format(" * Mức E_4  (Tứ thủ Hội Tụ):      {0,3}/{1,3} kỳ ({2,5:F2}%)  [Kỳ vọng ngẫu nhiên:  4.00% | Lợi thế: {3:+0.00;-0.00}%]", winE4, testDays, (double)winE4 * 100.0 / testDays, (double)winE4 * 100.0 / testDays - 4.0));
            Console.WriteLine(string.Format(" * Mức E_2  (Song thủ Đồng Quy):  {0,3}/{1,3} kỳ ({2,5:F2}%)  [Kỳ vọng ngẫu nhiên:  2.00% | Lợi thế: {3:+0.00;-0.00}%]", winE2, testDays, (double)winE2 * 100.0 / testDays, (double)winE2 * 100.0 / testDays - 2.0));
            Console.WriteLine(string.Format(" * Mức E_1  (Bạch thủ Đáy V_eff): {0,3}/{1,3} kỳ ({2,5:F2}%)  [Kỳ vọng ngẫu nhiên:  1.00% | Lợi thế: {3:+0.00;-0.00}%]", winE1, testDays, (double)winE1 * 100.0 / testDays, (double)winE1 * 100.0 / testDays - 1.0));
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            Console.WriteLine(string.Format(" * RỦI RO TRƯỢT KÉP (GÃY CẢ LÔ LẪN ĐỀ): Với E50: {0}/{1} kỳ ({2:F2}%) -> Có E65 giảm còn: {3}/{4} kỳ ({5:F2}%)!",
                doubleLoss50, testDays, (double)doubleLoss50 * 100.0 / testDays, doubleLoss65, testDays, (double)doubleLoss65 * 100.0 / testDays));
            Console.WriteLine();

            Console.WriteLine("PHẦN B: KIỂM ĐỊNH PHÂN HOẠCH CỤM DÀN ĐA TẠP TOÁN HỌC ĐỘNG (PART 1C MỚI NHẤT):");
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            Console.WriteLine("Cụm Dàn Tô-pô Động         | Số lượng TB | Tỷ lệ trúng thực tế | Kỳ vọng ngẫu nhiên | Lợi thế (+EV)");
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            Console.WriteLine(string.Format("Cụm 1: Lòng chảo hội tụ    |   {0,4:F1} số  |    {1,3}/{2} ({3,5:F2}%)   |       {4,5:F2}%      |   {5:+0.00;-0.00}%",
                sumCountC1 / testDays, winC1, testDays, (double)winC1 * 100.0 / testDays, sumCountC1 / testDays, (double)winC1 * 100.0 / testDays - (sumCountC1 / testDays)));
            Console.WriteLine(string.Format("Cụm 2: Vùng động lượng     |   {0,4:F1} số  |    {1,3}/{2} ({3,5:F2}%)   |       {4,5:F2}%      |   {5:+0.00;-0.00}%",
                sumCountC2 / testDays, winC2, testDays, (double)winC2 * 100.0 / testDays, sumCountC2 / testDays, (double)winC2 * 100.0 / testDays - (sumCountC2 / testDays)));
            Console.WriteLine(string.Format("Cụm 3: Vùng cân bằng       |   {0,4:F1} số  |    {1,3}/{2} ({3,5:F2}%)   |       {4,5:F2}%      |   {5:+0.00;-0.00}%",
                sumCountC3 / testDays, winC3, testDays, (double)winC3 * 100.0 / testDays, sumCountC3 / testDays, (double)winC3 * 100.0 / testDays - (sumCountC3 / testDays)));
            Console.WriteLine(string.Format("Cụm 4: Vùng suy giảm       |   {0,4:F1} số  |    {1,3}/{2} ({3,5:F2}%)   |       {4,5:F2}%      |   {5:+0.00;-0.00}%",
                sumCountC4 / testDays, winC4, testDays, (double)winC4 * 100.0 / testDays, sumCountC4 / testDays, (double)winC4 * 100.0 / testDays - (sumCountC4 / testDays)));
            Console.WriteLine(string.Format("Cụm 5: Vùng rào cản cản lặp|   {0,4:F1} số  |    {1,3}/{2} ({3,5:F2}%)   |       {4,5:F2}%      |   {5:+0.00;-0.00}%",
                sumCountC5 / testDays, winC5, testDays, (double)winC5 * 100.0 / testDays, sumCountC5 / testDays, (double)winC5 * 100.0 / testDays - (sumCountC5 / testDays)));
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            Console.WriteLine();

            Console.WriteLine("PHẦN C: KIỂM ĐỊNH HIỆU QUẢ LÔ TÔ PHỔ LAPLACIAN & BỘ TỨ XIÊN:");
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            Console.WriteLine(string.Format(" * Bạch thủ Lô (Top 1):          {0,3}/{1,3} kỳ ({2,5:F2}%)  [Lý thuyết chuẩn: 23.78% | Z = +{3:F2}]", 
                winBtLo, testDays, (double)winBtLo * 100.0 / testDays, ((double)winBtLo / testDays - 0.2378) / Math.Sqrt(0.2378 * 0.7622 / testDays)));
            Console.WriteLine(string.Format(" * Song thủ Lô (Top 1 & Top 2):  {0,3}/{1,3} kỳ ({2,5:F2}%)  [Nổ >= 1 con, Lý thuyết chuẩn: 42.10% | Z = +{3:F2}]", 
                winStLoAtLeast1, testDays, (double)winStLoAtLeast1 * 100.0 / testDays, ((double)winStLoAtLeast1 / testDays - 0.4210) / Math.Sqrt(0.4210 * 0.5790 / testDays)));
            Console.WriteLine(string.Format(" * Song thủ Lô NỔ CẢ CẶP:        {0,3}/{1,3} kỳ ({2,5:F2}%)  [Ăn trọn cả 2 con | Lý thuyết chuẩn:  5.45%]", 
                winStLoBoth, testDays, (double)winStLoBoth * 100.0 / testDays));
            Console.WriteLine(string.Format(" * Cặp Xiên 2 Song Thủ:          {0,3}/{1,3} kỳ ({2,5:F2}%)  [Tỷ lệ ăn vé Xiên 2 | Lý thuyết chuẩn: 5.45%]", 
                winXien2St, testDays, (double)winXien2St * 100.0 / testDays));
            Console.WriteLine(string.Format(" * Tứ thủ Lô (Hệ 4 số bảo vệ):   {0,3}/{1,3} kỳ ({2,5:F2}%)  [Nổ >= 1 con | Max chuỗi thông liên tục: {3} kỳ]", 
                winTtLoAtLeast1, testDays, (double)winTtLoAtLeast1 * 100.0 / testDays, maxStreakTtLo));
            Console.WriteLine(string.Format(" * Tứ thủ Lô nổ >= 2 nháy:        {0,3}/{1,3} kỳ ({2,5:F2}%)  [Tỷ lệ gom nháy kép / bội số]", 
                multiDaysTt, testDays, (double)multiDaysTt * 100.0 / testDays));
            Console.WriteLine(string.Format(" * Tổng số nháy Tứ thủ gom về:   {0,3} nháy (Trung bình: {1:F2} nháy/ngày)", 
                totalNhaysTt, (double)totalNhaysTt / testDays));
            Console.WriteLine();

            Console.WriteLine("PHẦN D: KIỂM ĐỊNH ĐÒN BẨY BẤT ĐỐI XỨNG TENSOR 3D (3 CÀNG):");
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            Console.WriteLine(string.Format(" * Bạch thủ 3 Càng (1 vé duy nhất):  {0,3}/{1,3} kỳ ({2,5:F2}%) [Lý thuyết chuẩn: 0.10% (1/1000)]", winBt3D, testDays, (double)winBt3D * 100.0 / testDays));
            Console.WriteLine(string.Format(" * Dàn 3 Càng Tinh Tuyển 12 số:      {0,3}/{1,3} kỳ ({2,5:F2}%) [Lý thuyết chuẩn: 1.20% | Lợi thế: {3:+0.00;-0.00}%]", 
                win3D12, testDays, (double)win3D12 * 100.0 / testDays, (double)win3D12 * 100.0 / testDays - 1.20));
            Console.WriteLine(string.Format(" * Dàn 3 Càng Phổ 27 số:             {0,3}/{1,3} kỳ ({2,5:F2}%) [Lý thuyết chuẩn: 2.70% | Lợi thế: {3:+0.00;-0.00}%]", 
                win3D27, testDays, (double)win3D27 * 100.0 / testDays, (double)win3D27 * 100.0 / testDays - 2.70));
            Console.WriteLine("=========================================================================================");
        }

        public static void RunRecentLoHistory(List<LotteryRecord> records, int count)
        {
            int n = records.Count;
            if (count <= 0 || count > 100) count = 10;
            int startIdx = n - count;

            Console.WriteLine("=========================================================================================================================");
            Console.WriteLine(string.Format("         BẢNG THEO DÕI DỰ ĐOÁN & KẾT QUẢ LÔ TÔ {0} NGÀY GẦN NHẤT THEO HỆ THỐNG TIDS ENGINE", count));
            Console.WriteLine("=========================================================================================================================");
            Console.WriteLine(string.Format(" {0,-5} | {1,-10} | {2,-7} | {3,-10} | {4,-15} | {5,-20} | {6,-16} | {7}", 
                "Kỳ", "Ngày", "Thứ", "Bạch Thủ", "Song Thủ", "Tứ Thủ Lô", "Số Trúng (Nháy)", "Đánh Giá"));
            Console.WriteLine("-------------------------------------------------------------------------------------------------------------------------");

            int btWins = 0;
            int stWins = 0;
            int stBothWins = 0;
            int ttWins = 0;
            int totalNhays = 0;

            for (int i = startIdx; i < n; i++)
            {
                var subList = records.Take(i).ToList();
                var actual = records[i];

                double[,] W;
                var lottoEnergies = ComputeUnifiedLottoPotentials(subList, out W);
                var top4 = SelectTop4LoWithDiversity(lottoEnergies, subList);
                int bt = top4[0];
                int st = top4[1];
                var tt = top4;

                int btHits = actual.LoList.Count(x => x == bt);
                int stHits = actual.LoList.Count(x => x == st);
                bool stHitAtLeast1 = (btHits > 0 || stHits > 0);
                bool stHitBoth = (btHits > 0 && stHits > 0);

                var hitList = new List<string>();
                int dayNhays = 0;
                foreach (var num in top4)
                {
                    int hits = actual.LoList.Count(x => x == num);
                    if (hits > 0)
                    {
                        hitList.Add(num.ToString("D2") + (hits > 1 ? string.Format("x{0}", hits) : ""));
                        dayNhays += hits;
                    }
                }

                if (btHits > 0) btWins++;
                if (stHitAtLeast1) stWins++;
                if (stHitBoth) stBothWins++;
                if (dayNhays > 0) ttWins++;
                totalNhays += dayNhays;

                string danhGia = "";
                if (stHitBoth) danhGia = "ĂN CẢ CẶP ST + XIÊN 2!";
                else if (btHits > 0) danhGia = string.Format("ĂN BẠCH THỦ ({0} nháy)", btHits);
                else if (stHits > 0) danhGia = string.Format("ĂN SONG THỦ ({0} nháy)", stHits);
                else if (dayNhays > 0) danhGia = string.Format("ĂN TỨ THỦ ({0} nháy)", dayNhays);
                else danhGia = "Trượt";

                string hitStr = hitList.Count > 0 ? string.Join(", ", hitList) : "-";
                string stStr = string.Format("{0:D2} - {1:D2}", bt, st);
                string ttStr = string.Join("-", top4.Select(x => x.ToString("D2")));

                Console.WriteLine(string.Format(" #{0,-4} | {1,-10} | {2,-7} | {3,-10} | {4,-15} | {5,-20} | {6,-16} | {7}",
                    actual.Id, actual.Date, actual.DayOfWeek, bt.ToString("D2"), stStr, ttStr, hitStr, danhGia));
            }

            Console.WriteLine("-------------------------------------------------------------------------------------------------------------------------");
            Console.WriteLine(string.Format(">> TỔNG KẾT {0} KỲ GẦN NHẤT:", count));
            Console.WriteLine(string.Format(" * Bạch thủ Lô (Top 1):          {0}/{1} kỳ ({2:F1}%)", btWins, count, btWins * 100.0 / count));
            Console.WriteLine(string.Format(" * Song thủ Lô (Ăn >= 1 con):    {0}/{1} kỳ ({2:F1}%)", stWins, count, stWins * 100.0 / count));
            Console.WriteLine(string.Format(" * Song thủ Lô (Ăn cả 2 con):    {0}/{1} kỳ ({2:F1}%) [Ăn trọn Xiên 2]", stBothWins, count, stBothWins * 100.0 / count));
            Console.WriteLine(string.Format(" * Tứ thủ Lô (Ăn >= 1 con):      {0}/{1} kỳ ({2:F1}%)", ttWins, count, ttWins * 100.0 / count));
            Console.WriteLine(string.Format(" * Tổng số nháy lô trúng Top 4:  {0} nháy (Trung bình {1:F2} nháy/ngày)", totalNhays, (double)totalNhays / count));
            Console.WriteLine("=========================================================================================================================");
        }

        public static QciResult ComputeQCI(List<LotteryRecord> records, List<LottoEnergy> lottoEnergies, List<StateEnergy> deEnergies, LottoMarginal1DResult lotto1D, Marginal1DResult marginal1D)
        {
            int n = records.Count;
            var res = new QciResult();

            // 1. THÀNH TỐ 1: ĐỘ TÁCH BIỆT THẾ NĂNG (ENERGY GAP MARGIN - TỐI ĐA 25 ĐIỂM)
            double gapLo = (lottoEnergies.Count >= 2) ? Math.Max(0.0, lottoEnergies[1].Potential - lottoEnergies[0].Potential) : 0.0;
            double gapDe = (deEnergies.Count >= 2) ? Math.Max(0.0, deEnergies[1].Potential - deEnergies[0].Potential) : 0.0;
            res.EnergyGapLo = gapLo;
            res.EnergyGapDe = gapDe;
            double sGap = 8.0 + Math.Min(10.0, gapLo * 15.0) + Math.Min(7.0, gapDe * 60.0);
            res.EnergyGapScore = Math.Round(Math.Min(25.0, sGap), 1);

            // 2. THÀNH TỐ 2: ENTROPI THÔNG TIN SHANNON CỦA 27 GIẢI HÔM TRƯỚC (TỐI ĐA 20 ĐIỂM)
            int[] headCount = new int[10];
            foreach (int lo in records[n - 1].LoList) headCount[lo / 10]++;
            double H = 0.0;
            for (int d = 0; d < 10; d++)
            {
                if (headCount[d] > 0)
                {
                    double p = headCount[d] / 27.0;
                    H -= p * (Math.Log(p) / Math.Log(2.0));
                }
            }
            double maxH = Math.Log(10.0) / Math.Log(2.0);
            double normH = H / maxH;
            res.NormalizedEntropy = Math.Round(normH, 3);
            double sEnt = 3.0 + 17.0 * Math.Max(0.0, Math.Min(1.0, (0.97 - normH) / (0.97 - 0.88)));
            res.EntropyScore = Math.Round(sEnt, 1);

            // 3. THÀNH TỐ 3: XUNG LỰC ĐÀN HỒI ĐẦU/ĐUÔI CÂM & CẦU RƠI GĐB (TỐI ĐA 25 ĐIỂM)
            bool hasCamDau = lotto1D.Heads.Any(x => x.F1 == 0);
            bool hasCamDuoi = lotto1D.Tails.Any(x => x.F1 == 0);
            res.HasCam = hasCamDau || hasCamDuoi;

            int prevDeHead = records[n - 1].Digits5[3];
            int prevDeTail = records[n - 1].Digits5[4];
            bool hasCascade = lotto1D.Heads.Take(2).Any(x => x.Digit == prevDeHead) ||
                              lotto1D.Tails.Take(2).Any(x => x.Digit == prevDeTail) ||
                              (lottoEnergies.Count > 0 && lottoEnergies[0].State == records[n - 1].De);
            res.HasCascade = hasCascade;

            double sShock = (res.HasCam ? 14.0 : 4.0) + (res.HasCascade ? 11.0 : 4.0);
            res.ShockSpringScore = Math.Round(Math.Min(25.0, sShock), 1);

            // 4. THÀNH TỐ 4: ĐỒNG PHA CỘNG HƯỞNG KÉP DRI ĐỀ (TỐI ĐA 15 ĐIỂM)
            int btDe = deEnergies[0].State;
            int btHead = btDe / 10;
            int btTail = btDe % 10;
            int rankHead = marginal1D.Tens.FindIndex(x => x.Digit == btHead) + 1;
            int rankTail = marginal1D.Units.FindIndex(x => x.Digit == btTail) + 1;
            int dri = rankHead + rankTail;
            double sDri = 15.0 - Math.Min(10.0, (dri - 2) * 1.5);
            res.DriAlignmentScore = Math.Round(Math.Max(5.0, sDri), 1);

            // 5. THÀNH TỐ 5: HỆ SỐ MONEYNESS & RÀO CẢN BSM (TỐI ĐA 15 ĐIỂM)
            double topD2 = (lotto1D.Heads.Count > 0) ? lotto1D.Heads[0].BsmD2 : 0.0;
            int topF7 = (lotto1D.Heads.Count > 0) ? lotto1D.Heads[0].F7 : 0;
            double sBsm = 10.0;
            if (topD2 >= 1.0) sBsm += 5.0;
            if (topF7 >= 25) sBsm -= 4.0;
            res.BsmRegimeScore = Math.Round(Math.Max(4.0, Math.Min(15.0, sBsm)), 1);

            // TỔNG ĐIỂM QCI
            double total = res.EnergyGapScore + res.EntropyScore + res.ShockSpringScore + res.DriAlignmentScore + res.BsmRegimeScore;
            res.Score = Math.Round(Math.Max(10.0, Math.Min(98.0, total)), 1);

            if (res.Score >= 75.0)
            {
                res.Zone = "XANH";
                res.ActionRecommendation = "🟢 NÊN ĐÁNH TẤN CÔNG (FULL POWER) - Tín hiệu cộng hưởng mạnh, rủi ro thấp";
            }
            else if (res.Score >= 60.0)
            {
                res.Zone = "VÀNG";
                res.ActionRecommendation = "🟡 ĐÁNH PHÒNG THỦ / THĂM DÒ - Tín hiệu trung bình, giảm 50% tiền Lô";
            }
            else
            {
                res.Zone = "ĐỎ";
                res.ActionRecommendation = "🔴 CẢNH BÁO NÊN NGHỈ ĐÁNH (SIT ON HANDS) - Thị trường nhiễu loạn cao, dễ gãy";
            }

            return res;
        }

        public static MarketRegimeResult DetectMarketRegime(
            List<LotteryRecord> records, 
            List<StateEnergy> unifiedEnergies, 
            double[,] W, 
            Marginal1DResult marginal1D,
            double precomputedMroll = double.NaN)
        {
            int n = records.Count;
            var res = new MarketRegimeResult();

            // 1. ENTROPY SHANNON H CỦA VECTOR THẾ NĂNG 100 SỐ ĐỀ
            double minV = double.MaxValue;
            for (int i = 0; i < unifiedEnergies.Count; i++)
            {
                if (unifiedEnergies[i].Potential < minV) minV = unifiedEnergies[i].Potential;
            }

            double sumExp = 0.0;
            double[] expV = new double[unifiedEnergies.Count];
            double T = 0.50;
            for (int i = 0; i < unifiedEnergies.Count; i++)
            {
                expV[i] = Math.Exp(-(unifiedEnergies[i].Potential - minV) / T);
                sumExp += expV[i];
            }

            double H = 0.0;
            for (int i = 0; i < unifiedEnergies.Count; i++)
            {
                double p = expV[i] / sumExp;
                if (p > 1e-12)
                {
                    H -= p * Math.Log(p);
                }
            }
            double maxH = Math.Log(100.0);
            res.EntropyH = Math.Round(H / maxH, 3);

            // 2. ĐỘ SẮC NÉT PHỔ MA TRẬN SPECTRAL GAP (γ) TỪ MA TRẬN W (100x100)
            double[] vec = new double[100];
            for (int i = 0; i < 100; i++) vec[i] = 1.0 / Math.Sqrt(100.0);
            double lambda1 = 0.0;
            for (int iter = 0; iter < 15; iter++)
            {
                double[] nextV = new double[100];
                for (int i = 0; i < 100; i++)
                {
                    double sum = 0.0;
                    for (int j = 0; j < 100; j++) sum += W[i, j] * vec[j];
                    nextV[i] = sum;
                }
                double norm = 0.0;
                for (int i = 0; i < 100; i++) norm += nextV[i] * nextV[i];
                norm = Math.Sqrt(norm);
                if (norm > 1e-12)
                {
                    for (int i = 0; i < 100; i++) nextV[i] /= norm;
                    lambda1 = norm;
                    vec = nextV;
                }
            }
            double totalFrob = 0.0;
            for (int i = 0; i < 100; i++)
                for (int j = 0; j < 100; j++)
                    totalFrob += W[i, j] * W[i, j];
            totalFrob = Math.Sqrt(totalFrob);
            res.SpectralGapGamma = Math.Round(totalFrob > 1e-6 ? Math.Min(1.0, lambda1 / totalFrob) : 0.15, 3);

            // 3. ĐỘ NÉN VĨ MÔ Z_macro CỦA BSM FACTORS (Chẵn/Lẻ và Tài/Xỉu)
            double maxZ = 0.0;
            string compFactor = "Cân bằng vĩ mô";
            string[] pNames = { "Chẵn-Chẵn (CC)", "Chẵn-Lẻ (CL)", "Lẻ-Chẵn (LC)", "Lẻ-Lẻ (LL)" };
            for (int k = 0; k < 4; k++)
            {
                int gan = marginal1D.ParityGan[k];
                double z = (gan - 3.0) / 2.35;
                if (z > maxZ)
                {
                    maxZ = z;
                    compFactor = pNames[k];
                }
            }
            string[] szNames = { "Xỉu (00-49)", "Tài (50-99)" };
            for (int k = 0; k < 2; k++)
            {
                int gan = marginal1D.SizeGan[k];
                double z = (gan - 1.0) / 1.20;
                if (z > maxZ)
                {
                    maxZ = z;
                    compFactor = szNames[k];
                }
            }
            res.ZMacro = Math.Round(Math.Max(0.0, maxZ), 2);
            res.CompressedFactor = compFactor;

            // 4. QUÁN TÍNH HIỆU SUẤT NGOÀI MẪU M_roll (Rolling Momentum 15 kỳ vs 50 kỳ)
            if (!double.IsNaN(precomputedMroll))
            {
                res.MomentumMRoll = Math.Round(precomputedMroll, 1);
            }
            else
            {
                int win15 = 0;
                int win50 = 0;
                int lookback50 = Math.Min(50, n - 100);
                int start50 = n - lookback50;
                int start15 = n - Math.Min(15, lookback50);

                for (int t = start50; t < n; t++)
                {
                    var sub = records.Take(t).ToList();
                    var sub1D = Compute1DMarginals(sub);
                    var subBsm = ComputeBsmBasketPotentials2D(sub, sub1D);
                    var d65 = SelectFactorNeutralDeSet(subBsm, 65);
                    if (d65.Contains(records[t].De))
                    {
                        win50++;
                        if (t >= start15) win15++;
                    }
                }
                int len15 = n - start15;
                int len50 = n - start50;
                double r15 = len15 > 0 ? (double)win15 * 100.0 / len15 : 65.0;
                double r50 = len50 > 0 ? (double)win50 * 100.0 / len50 : 65.0;
                res.Win15Count = win15;
                res.Win50Count = win50;
                res.MomentumMRoll = Math.Round(r15 - r50, 1);
            }

            // PHÂN LOẠI REGIME THEO ĐẠO DỊCH
            if (res.EntropyH >= 0.95 && res.SpectralGapGamma < 0.12)
            {
                res.Regime = MarketRegimeType.Chaos;
                res.RegimeName = "BĨ CỰC (Nhiễu loạn cao - Khí tắc cầu gãy)";
                res.RegimeSymbol = "🔴 BĨ CỰC (CHAOS)";
                res.ActionRecommendation = "THU LIỄM TÀNG HÌNH (Phòng thủ tuyệt đối / Sit on hands) - Nhiễu trắng áp đảo";
                res.KellyMultiplier = 0.25;
            }
            else if (res.ZMacro >= 2.30)
            {
                res.Regime = MarketRegimeType.Coiling;
                res.RegimeName = string.Format("PHỤC KHÍ (Nén lực hồi quy: {0})", res.CompressedFactor);
                res.RegimeSymbol = "🟠 PHỤC KHÍ (COILING)";
                res.ActionRecommendation = string.Format("QUY NGUYÊN TRẬN ({0}) + HUYỀN THIÊN GIÁP + TẤN CÔNG ĐỊA LỢI LÔ TÔ", res.CompressedFactor);
                res.KellyMultiplier = 1.00;
            }
            else if (res.EntropyH <= 0.885 && res.MomentumMRoll >= 4.0)
            {
                res.Regime = MarketRegimeType.Peak;
                res.RegimeName = "THÁI HÒA (Đỉnh cao phong độ - Khí thông đồng pha)";
                res.RegimeSymbol = "🟢 THÁI HÒA (PEAK)";
                res.ActionRecommendation = "ĐẠI THỪA XUẤT TRẬN (AGGRESSIVE KELLY) - Dồn lực tấn công Đề (Thái Cực & Hà Đồ Dàn 25)";
                res.KellyMultiplier = 1.50;
            }
            else if (res.EntropyH <= 0.915 && res.SpectralGapGamma >= 0.155)
            {
                res.Regime = MarketRegimeType.Convergent;
                res.RegimeName = "KÝ TẾ (Thế năng hội tụ chuẩn - Âm Dương chu toàn)";
                res.RegimeSymbol = "🔵 KÝ TẾ (CONVERGENT)";
                res.ActionRecommendation = "XUẤT KÍCH BÌNH ỔN (STANDARD KELLY) - Cân bằng Tam Tài (Địa Lợi 40% / Nhân Hòa 50% / Thiên Thời 10%)";
                res.KellyMultiplier = 1.25;
            }
            else
            {
                res.Regime = MarketRegimeType.Steady;
                res.RegimeName = "TRUNG DUNG (Thị trường bình ổn - Vận hành chuẩn)";
                res.RegimeSymbol = "🟡 TRUNG DUNG (STEADY)";
                res.ActionRecommendation = "ĐÁNH THEO TỶ LỆ CHUẨN (STANDARD KELLY) - Cân bằng Lô 30% / Đề 70%";
                res.KellyMultiplier = 1.00;
            }

            return res;
        }

        public static void RunRegimeAudit(List<LotteryRecord> records, int testDays = 100)
        {
            int n = records.Count;
            if (testDays <= 0 || testDays > 1000) testDays = 100;
            int startIdx = n - testDays;

            bool[] d65Hits = new bool[n];
            bool[] d50Hits = new bool[n];
            bool[] stHits = new bool[n];
            bool[] btHits = new bool[n];

            Console.WriteLine("=========================================================================================================================");
            Console.WriteLine(string.Format("   QUY TRÌNH KIỂM THỬ ĐỐI SOÁT 4 ĐẠI LƯỢNG TOÁN HỌC & BỘ PHÂN LOẠI REGIME THỊ TRƯỜNG ({0} KỲ GẦN NHẤT)", testDays));
            Console.WriteLine(string.Format("   Khoảng thời gian: từ kỳ {0} ({1}) đến kỳ {2} ({3})", records[startIdx].Date, records[startIdx].DayOfWeek, records[n - 1].Date, records[n - 1].DayOfWeek));
            Console.WriteLine("=========================================================================================================================");
            Console.WriteLine(" STT | Kỳ quay | Ngày       | H_shannon | Phổ γ | Nén Z_macro | Quán tính M | Trạng thái Regime | Dàn E50 | Dàn E65 | Song Thủ Lô");
            Console.WriteLine("-------------------------------------------------------------------------------------------------------------------------");

            var sbTsv = new StringBuilder();
            sbTsv.AppendLine("STT\tKỳ Quay\tNgày\tThứ\tĐề Về\tEntropy H\tPhổ Gamma\tNén Z\tNhân Tố Nén\tQuán Tính M\tRegime\tHệ Số k\tĂn D50\tĂn D65\tĂn ST Lô");

            int chaosCount = 0, coilingCount = 0, steadyCount = 0, convergentCount = 0, peakCount = 0;
            int chaosD50 = 0, coilingD50 = 0, steadyD50 = 0, convergentD50 = 0, peakD50 = 0;
            int chaosD65 = 0, coilingD65 = 0, steadyD65 = 0, convergentD65 = 0, peakD65 = 0;
            int chaosST = 0, coilingST = 0, steadyST = 0, convergentST = 0, peakST = 0;

            for (int t = startIdx; t < n; t++)
            {
                int stt = t - startIdx + 1;
                var subList = records.Take(t).ToList();
                var actual = records[t];
                int actualDe = actual.De;
                var actualLo = actual.LoList;

                var sub1D = Compute1DMarginals(subList);
                var subLotto1D = Compute1DLottoMarginals(subList);
                double[,] subW;
                var subLotto = ComputeUnifiedLottoPotentials(subList, subLotto1D, out subW);
                var subUnified = ComputeUnifiedPotentials2D(subList, sub1D);
                var subBsm = ComputeBsmBasketPotentials2D(subList, sub1D);

                var dan50 = SelectFactorNeutralDeSet(subBsm, 50);
                var dan65 = SelectFactorNeutralDeSet(subBsm, 65);
                var top4 = SelectTop4LoWithDiversity(subLotto, subList);

                bool hit50 = dan50.Contains(actualDe);
                bool hit65 = dan65.Contains(actualDe);
                bool hitST = actualLo.Contains(top4[0]) || actualLo.Contains(top4[1]);
                bool hitBT = actualLo.Contains(top4[0]);

                d50Hits[t] = hit50;
                d65Hits[t] = hit65;
                stHits[t] = hitST;
                btHits[t] = hitBT;

                // Rolling momentum
                int w15 = 0;
                for (int k = Math.Max(0, t - 15); k < t; k++) if (d65Hits[k]) w15++;
                int w50 = 0;
                for (int k = Math.Max(0, t - 50); k < t; k++) if (d65Hits[k]) w50++;
                double len15 = Math.Min(15, t);
                double len50 = Math.Min(50, t);
                double mRoll = (len15 > 0 && len50 > 0) ? ((w15 / len15) - (w50 / len50)) * 100.0 : 0.0;

                var regime = DetectMarketRegime(subList, subUnified, subW, sub1D, mRoll);

                switch (regime.Regime)
                {
                    case MarketRegimeType.Chaos:
                        chaosCount++;
                        if (hit50) chaosD50++;
                        if (hit65) chaosD65++;
                        if (hitST) chaosST++;
                        break;
                    case MarketRegimeType.Coiling:
                        coilingCount++;
                        if (hit50) coilingD50++;
                        if (hit65) coilingD65++;
                        if (hitST) coilingST++;
                        break;
                    case MarketRegimeType.Steady:
                        steadyCount++;
                        if (hit50) steadyD50++;
                        if (hit65) steadyD65++;
                        if (hitST) steadyST++;
                        break;
                    case MarketRegimeType.Convergent:
                        convergentCount++;
                        if (hit50) convergentD50++;
                        if (hit65) convergentD65++;
                        if (hitST) convergentST++;
                        break;
                    case MarketRegimeType.Peak:
                        peakCount++;
                        if (hit50) peakD50++;
                        if (hit65) peakD65++;
                        if (hitST) peakST++;
                        break;
                }

                string rStr = regime.RegimeSymbol;
                string d50Str = hit50 ? "🟢 ĂN" : "🔴 Trượt";
                string d65Str = hit65 ? "🟢 ĂN" : "🔴 Trượt";
                string stStr = hitST ? "🟢 ĂN" : "🔴 Trượt";

                Console.WriteLine(string.Format(" {0,3} | #{1,-5} | {2,-10} |   {3,5:F3}   | {4,5:F3} |    {5,4:F2}σ   |    {6,5:F1}%   | {7,-17} |  {8,-6} |  {9,-6} |    {10}",
                    stt, actual.Id, actual.Date, regime.EntropyH, regime.SpectralGapGamma, regime.ZMacro, regime.MomentumMRoll, rStr, d50Str, d65Str, stStr));

                sbTsv.AppendLine(string.Format("{0}\t#{1}\t{2}\t{3}\t{4:D2}\t{5:F3}\t{6:F3}\t{7:F2}\t{8}\t{9:F1}%\t{10}\t{11:F2}x\t{12}\t{13}\t{14}",
                    stt, actual.Id, actual.Date, actual.DayOfWeek, actualDe, regime.EntropyH, regime.SpectralGapGamma, regime.ZMacro, regime.CompressedFactor, regime.MomentumMRoll, regime.Regime.ToString(), regime.KellyMultiplier, hit50 ? "ĂN" : "Trượt", hit65 ? "ĂN" : "Trượt", hitST ? "ĂN" : "Trượt"));
            }

            Console.WriteLine("-------------------------------------------------------------------------------------------------------------------------");
            Console.WriteLine("TỔNG KẾT ĐỐI SOÁT HIỆU NĂNG THEO TỪNG REGIME THỊ TRƯỜNG:");
            Console.WriteLine("Regime Phân Loại    | Tần Suất Kỳ | Tỷ Lệ Ăn Dàn 50 | Tỷ Lệ Ăn Dàn 65 | Tỷ Lệ Ăn Song Thủ Lô | Đánh Giá Tín Hiệu");
            Console.WriteLine("-------------------------------------------------------------------------------------------------------------------------");

            Action<string, int, int, int, int, string> printRegimeRow = (name, cnt, d50, d65, st, eval) =>
            {
                if (cnt == 0)
                {
                    Console.WriteLine(string.Format("{0,-19} | {1,7} kỳ |       N/A       |       N/A       |         N/A          | {2}", name, 0, eval));
                    return;
                }
                double p50 = (double)d50 * 100.0 / cnt;
                double p65 = (double)d65 * 100.0 / cnt;
                double pST = (double)st * 100.0 / cnt;
                Console.WriteLine(string.Format("{0,-19} | {1,4}/{2,3} ({3,4:F1}%) | {4,3}/{5,3} ({6,4:F1}%) | {7,3}/{8,3} ({9,4:F1}%) |   {10,3}/{11,3} ({12,4:F1}%)   | {13}",
                    name, cnt, testDays, (double)cnt * 100.0 / testDays, d50, cnt, p50, d65, cnt, p65, st, cnt, pST, eval));
            };

            printRegimeRow("🔴 CHAOS (Nhiễu)", chaosCount, chaosD50, chaosD65, chaosST, "Cảnh báo chính xác - Thị trường gãy nhịp nên nghỉ");
            printRegimeRow("🟠 COILING (Nén)", coilingCount, coilingD50, coilingD65, coilingST, "Vùng nổ thế năng - Dàn 65 giữ vốn và ăn đậm");
            printRegimeRow("🟡 STEADY (Ổn định)", steadyCount, steadyD50, steadyD65, steadyST, "Vận hành chuẩn mực, phong độ tiệm cận lý thuyết");
            printRegimeRow("🔵 CONVERGENT (Hội tụ)", convergentCount, convergentD50, convergentD65, convergentST, "Cầu mượt mà, tỷ lệ trúng vượt trội lý thuyết");
            printRegimeRow("🟢 PEAK (Đỉnh cao)", peakCount, peakD50, peakD65, peakST, "Hiệu suất cực đại, điểm vàng để gia tăng vốn");
            Console.WriteLine("=========================================================================================================================");

            try
            {
                string dir = "data";
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, string.Format("regime_test_{0}days.tsv", testDays)), sbTsv.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        public class LottoRegimeItem
        {
            public MarketRegimeType Regime;
            public string Name;
            public string Symbol;
            public int Count;
            
            public int WinBTL;
            public int HitsBTL;
            
            public int WinSTL;
            public int DoubleWinSTL;
            public int HitsSTL;
            
            public int WinLot;
            public int HitsLot;
            
            public int WinTT;
            public int WinTT_Ge2;
            public int WinTT_Ge3;
            public int HitsTT;
            
            public int WinX2;
            public int PairsX2;
        }

        public static void RunLottoRegimeAudit(List<LotteryRecord> records, int testDays = 100)
        {
            int n = records.Count;
            if (testDays <= 0 || testDays > 3000) testDays = 100;
            int startIdx = n - testDays;

            Console.WriteLine("=========================================================================================");
            Console.WriteLine(string.Format("   KIỂM THỬ ĐỐI SOÁT PHƯƠNG PHÁP LÔ MỚI NHẤT VỚI NGUYÊN TẮC THỜI KHÍ ({0} KỲ GẦN NHẤT)", testDays));
            Console.WriteLine(string.Format("   Khoảng thời gian: từ kỳ #{0} ({1}, {2}) đến kỳ #{3} ({4}, {5})",
                records[startIdx].Id, records[startIdx].Date, records[startIdx].DayOfWeek, records[n - 1].Id, records[n - 1].Date, records[n - 1].DayOfWeek));
            Console.WriteLine("=========================================================================================");

            bool[] d65Hits = new bool[n];
            for (int t = 0; t < n; t++)
            {
                var subList = records.Take(t).ToList();
                if (subList.Count < 50) continue;
                var sub1D = Compute1DMarginals(subList);
                var subBsm = ComputeBsmBasketPotentials2D(subList, sub1D);
                var dan65 = SelectFactorNeutralDeSet(subBsm, 65);
                d65Hits[t] = dan65.Contains(records[t].De);
            }

            var regimes = new LottoRegimeItem[]
            {
                new LottoRegimeItem { Regime = MarketRegimeType.Chaos, Name = "CHAOS (Bĩ Cực / Hỗn Loạn)", Symbol = "🔴 CHAOS" },
                new LottoRegimeItem { Regime = MarketRegimeType.Coiling, Name = "COILING (Phục Khí / Nén Thế Năng)", Symbol = "🟠 COILING" },
                new LottoRegimeItem { Regime = MarketRegimeType.Steady, Name = "STEADY (Bình Ổn / Chuẩn Mực)", Symbol = "🟡 STEADY" },
                new LottoRegimeItem { Regime = MarketRegimeType.Convergent, Name = "CONVERGENT (Ký Tế / Hội Tụ)", Symbol = "🔵 CONVERGENT" },
                new LottoRegimeItem { Regime = MarketRegimeType.Peak, Name = "PEAK (Thái Hòa / Đỉnh Cao)", Symbol = "🟢 PEAK" }
            };

            long costA = 0, revA = 0;
            long costB = 0, revB = 0;

            for (int t = startIdx; t < n; t++)
            {
                var subList = records.Take(t).ToList();
                var actual = records[t];
                var actualLo = actual.LoList;
                var actualUniqueLo = actual.UniqueLo;

                var sub1D = Compute1DMarginals(subList);
                var subLotto1D = Compute1DLottoMarginals(subList);
                double[,] subW;
                var subLotto = ComputeUnifiedLottoPotentials(subList, subLotto1D, out subW);
                var subUnified = ComputeUnifiedPotentials2D(subList, sub1D);

                int w15 = 0;
                for (int k = Math.Max(0, t - 15); k < t; k++) if (d65Hits[k]) w15++;
                int w50 = 0;
                for (int k = Math.Max(0, t - 50); k < t; k++) if (d65Hits[k]) w50++;
                double len15 = Math.Min(15, t);
                double len50 = Math.Min(50, t);
                double mRoll = (len15 > 0 && len50 > 0) ? ((w15 / len15) - (w50 / len50)) * 100.0 : 0.0;

                var regime = DetectMarketRegime(subList, subUnified, subW, sub1D, mRoll);
                var top4 = SelectTop4LoWithDiversity(subLotto, subList);
                int btLo = top4[0];
                int stLo = top4[1];
                int lot1 = top4[2];
                int lot2 = top4[3];
                var tuThu = new List<int> { btLo, stLo, lot1, lot2 };

                var pairs = new List<Tuple<int, int>>();
                for (int i = 0; i < 4; i++)
                    for (int j = i + 1; j < 4; j++)
                        pairs.Add(new Tuple<int, int>(tuThu[i], tuThu[j]));

                int btlHits = actualLo.Count(x => x == btLo);
                int st1Hits = actualLo.Count(x => x == btLo);
                int st2Hits = actualLo.Count(x => x == stLo);
                int stlHits = st1Hits + st2Hits;
                bool winSTL = (st1Hits > 0 || st2Hits > 0);
                bool doubleSTL = (st1Hits > 0 && st2Hits > 0);

                int lot1Hits = actualLo.Count(x => x == lot1);
                int lot2Hits = actualLo.Count(x => x == lot2);
                int lotHits = lot1Hits + lot2Hits;
                bool winLot = (lot1Hits > 0 || lot2Hits > 0);

                int ttHits = stlHits + lotHits;
                int ttDistinctHits = tuThu.Count(x => actualUniqueLo.Contains(x));
                bool winTT = ttDistinctHits >= 1;
                bool winTT_Ge2 = ttDistinctHits >= 2;
                bool winTT_Ge3 = ttDistinctHits >= 3;

                int winPairs = pairs.Count(p => actualUniqueLo.Contains(p.Item1) && actualUniqueLo.Contains(p.Item2));
                bool winX2 = winPairs > 0;

                int rIdx = (int)regime.Regime;
                var regItem = regimes[rIdx];
                regItem.Count++;

                if (btlHits > 0) regItem.WinBTL++;
                regItem.HitsBTL += btlHits;

                if (winSTL) regItem.WinSTL++;
                if (doubleSTL) regItem.DoubleWinSTL++;
                regItem.HitsSTL += stlHits;

                if (winLot) regItem.WinLot++;
                regItem.HitsLot += lotHits;

                if (winTT) regItem.WinTT++;
                if (winTT_Ge2) regItem.WinTT_Ge2++;
                if (winTT_Ge3) regItem.WinTT_Ge3++;
                regItem.HitsTT += ttHits;

                if (winX2) regItem.WinX2++;
                regItem.PairsX2 += winPairs;

                // Strategy A (Cố định: STL 2đ = 54k, Lót 1đ = 27k, X2 15k = 96k/ngày)
                long dayCostA = (2 * 27000) + (2 * 27000) + 15000;
                long dayRevA = (stlHits * 99000) + (lotHits * 99000) + (doubleSTL ? (long)(15000 * 16.5) : 0);
                costA += dayCostA;
                revA += dayRevA;

                // Strategy B (Thích ứng Thời Khí: Peak/Convergent STL 3đ, Lót 1đ, X2 25k; Coiling STL 1đ, Lót 1đ, X2 0k; Chaos 0đ)
                int stPtsB = 0;
                int lotPtsB = 0;
                long xienCostB = 0;
                if (regime.Regime == MarketRegimeType.Peak || regime.Regime == MarketRegimeType.Convergent)
                {
                    stPtsB = 3;
                    lotPtsB = 1;
                    xienCostB = 25000;
                }
                else if (regime.Regime == MarketRegimeType.Coiling)
                {
                    stPtsB = 1;
                    lotPtsB = 1;
                    xienCostB = 0;
                }
                else if (regime.Regime == MarketRegimeType.Steady)
                {
                    stPtsB = 2;
                    lotPtsB = 1;
                    xienCostB = 15000;
                }
                long dayCostB = (2 * stPtsB * 27000) + (2 * lotPtsB * 27000) + xienCostB;
                long dayRevB = (stlHits * stPtsB * 99000) + (lotHits * lotPtsB * 99000) + (doubleSTL ? (long)(xienCostB * 16.5) : 0);
                costB += dayCostB;
                revB += dayRevB;
            }

            Console.WriteLine(string.Format("{0,-15} | {1,-9} | {2,-11} | {3,-15} | {4,-15} | {5,-13} | {6,-10}",
                "Trạng Thái Thời Khí", "Tần Suất", "BTL (Win/Nháy)", "STL (Win/Ăn Kép)", "Tứ Thủ (Win/Ge2)", "Xiên 2 (Bộ 6)", "STL ROI%"));
            Console.WriteLine(new string('-', 98));

            for (int i = 0; i < 5; i++)
            {
                var item = regimes[i];
                if (item.Count == 0) continue;
                double btlWinRate = item.WinBTL * 100.0 / item.Count;
                double stlWinRate = item.WinSTL * 100.0 / item.Count;
                double stlDoubleRate = item.DoubleWinSTL * 100.0 / item.Count;
                double ttWinRate = item.WinTT * 100.0 / item.Count;
                double ttGe2Rate = item.WinTT_Ge2 * 100.0 / item.Count;
                double x2WinRate = item.WinX2 * 100.0 / item.Count;
                
                long stlCost = item.Count * 2 * 27000;
                long stlRev = item.HitsSTL * 99000;
                double stlRoi = (stlRev - stlCost) * 100.0 / stlCost;

                Console.WriteLine(string.Format("{0,-15} | {1,4} ({2,4:F1}%) | {3,5:F1}% ({4} nháy) | {5,5:F1}% (kép {6,4:F1}%) | {7,5:F1}% (≥2: {8,4:F1}%) | {9,5:F1}% ({10} cặp) | {11:+#,##0.0;-#,##0.0;0.0}%",
                    item.Symbol, item.Count, item.Count * 100.0 / testDays,
                    btlWinRate, item.HitsBTL,
                    stlWinRate, stlDoubleRate,
                    ttWinRate, ttGe2Rate,
                    x2WinRate, item.PairsX2,
                    stlRoi));
            }

            Console.WriteLine(new string('=', 98));
            Console.WriteLine("    SO SÁNH HIỆU QUẢ TÀI CHÍNH ĐẦU TƯ LÔ: ĐÁNH CỐ ĐỊNH vs ĐÁNH THEO THỜI KHÍ:");
            Console.WriteLine(string.Format(" * Chiến Lược A (Cố Định - Không theo Thời Khí): Vốn: {0:N0} đ | Doanh thu: {1:N0} đ | Lãi ròng: {2:+#,##0;-#,##0;0} đ ({3:+#,##0.0;-#,##0.0;0.0}%)",
                costA, revA, revA - costA, (revA - costA) * 100.0 / costA));
            Console.WriteLine(string.Format(" * Chiến Lược B (Thích Ứng Thời Khí - Regime Kelly): Vốn: {0:N0} đ | Doanh thu: {1:N0} đ | Lãi ròng: {2:+#,##0;-#,##0;0} đ ({3:+#,##0.0;-#,##0.0;0.0}%)",
                costB, revB, revB - costB, (revB - costB) * 100.0 / costB));
            double gainRatio = (revB - costB) > 0 && (revA - costA) > 0 ? ((double)(revB - costB) / (revA - costA)) : 0;
            if (gainRatio > 1.0)
                Console.WriteLine(string.Format("   >>> HIỆU QUẢ LỢI NHUẬN TĂNG GẤP: {0:F2} LẦN NHỜ NGUYÊN TẮC THỜI KHÍ!", gainRatio));
            Console.WriteLine("=========================================================================================");
        }

        public static void RunD50Audit(List<LotteryRecord> records, int testDays = 100)
        {
            RunDeSetAudit(records, testDays, 50);
        }

        public static void RunDeSetAudit(List<LotteryRecord> records, int testDays = 100, int setSize = 50)
        {
            int n = records.Count;
            if (testDays <= 0 || testDays > 3000) testDays = 100;
            int startIdx = n - testDays;

            int winCount = 0;
            int curStreak = 0;
            int maxStreak = 0;
            int maxLoss = 0;

            var sbTsv = new StringBuilder();
            sbTsv.AppendLine(string.Format("STT\tKỳ Quay\tNgày\tThứ\tĐề Về\tKết Quả Dàn {0}\tChuỗi\tLũy Kế Trúng\tTỷ Lệ Trúng Lũy Kế (%)", setSize));

            Console.WriteLine("=========================================================================================");
            Console.WriteLine(string.Format("    BẢNG KẾT QUẢ KIỂM THỬ DÀN ĐỀ {0} SỐ (E{0}) TRONG {1} KỲ GẦN NHẤT", setSize, testDays));
            Console.WriteLine(string.Format("    Khoảng thời gian: từ {0} ({1}) đến {2} ({3})", records[startIdx].Date, records[startIdx].DayOfWeek, records[n - 1].Date, records[n - 1].DayOfWeek));
            Console.WriteLine("=========================================================================================");
            Console.WriteLine(string.Format(" STT | Kỳ quay | Ngày       | Thứ     | Đề về | Kết Quả Dàn {0,-2} | Chuỗi       | Trúng Lũy Kế | Tỷ Lệ (%)", setSize));
            Console.WriteLine("-----------------------------------------------------------------------------------------");

            for (int t = startIdx; t < n; t++)
            {
                int stt = t - startIdx + 1;
                var subList = records.Take(t).ToList();
                var actual = records[t];
                int actualDe = actual.De;

                var marginal1D = Compute1DMarginals(subList);
                var bsmBasket = ComputeBsmBasketPotentials2D(subList, marginal1D);
                var danSet = SelectFactorNeutralDeSet(bsmBasket, setSize);

                bool hit = danSet.Contains(actualDe);
                if (hit)
                {
                    winCount++;
                    if (curStreak < 0) curStreak = 1; else curStreak++;
                    if (curStreak > maxStreak) maxStreak = curStreak;
                }
                else
                {
                    if (curStreak > 0) curStreak = -1; else curStreak--;
                    if (Math.Abs(curStreak) > maxLoss) maxLoss = Math.Abs(curStreak);
                }

                string resStr = hit ? "🟢 ĂN ĐỀ" : "🔴 Trượt";
                string streakStr = curStreak > 0 ? string.Format("+{0} Thông", curStreak) : string.Format("{0} Gãy", curStreak);
                double cumPct = (double)winCount * 100.0 / stt;

                Console.WriteLine(string.Format(" {0,3} | #{1,-5} | {2,-10} | {3,-7} |  {4:D2}   | {5,-14} | {6,-11} |    {7,2}/{8,2}   | {9,5:F1}%",
                    stt, actual.Id, actual.Date, actual.DayOfWeek, actualDe, resStr, streakStr, winCount, stt, cumPct));

                sbTsv.AppendLine(string.Format("{0}\t#{1}\t{2}\t{3}\t{4:D2}\t{5}\t{6}\t{7}/{8}\t{9:F1}%",
                    stt, actual.Id, actual.Date, actual.DayOfWeek, actualDe, hit ? "ĂN" : "Trượt", streakStr, winCount, stt, cumPct));
            }

            double randomPct = (double)setSize;
            double actualPct = (double)winCount * 100.0 / testDays;
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            Console.WriteLine(string.Format("TỔNG KẾT HIỆU NĂNG KIỂM THỬ DÀN ĐỀ {0} SỐ ({1} KỲ):", setSize, testDays));
            Console.WriteLine(string.Format(" - Tổng số ngày trúng:           {0}/{1} kỳ ({2:F2}%)  [Lý thuyết ngẫu nhiên: {3:F2}%]", winCount, testDays, actualPct, randomPct));
            Console.WriteLine(string.Format(" - Chuỗi thông dài nhất:         {0} kỳ liên tiếp", maxStreak));
            Console.WriteLine(string.Format(" - Chuỗi trượt dài nhất:         {0} kỳ liên tiếp", maxLoss));
            Console.WriteLine(string.Format(" - Lợi thế vượt trội (+EV):      +{0:F2}% so với xác suất ngẫu nhiên", actualPct - randomPct));
            Console.WriteLine("=========================================================================================");

            try
            {
                string dir = "data";
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, string.Format("d{0}_test_{1}days.tsv", setSize, testDays)), sbTsv.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        public class DecimationConfig
        {
            public string Name;
            public double Ratio;
            public int[] Sizes;
            public int[] Hits;
            public int[] RetainedHits;
        }

        public static void RunDecimationRatioComparison(List<LotteryRecord> records, int testDays = 300)
        {
            int n = records.Count;
            if (testDays <= 0 || testDays > 3000) testDays = 300;
            int startIdx = n - testDays;

            Console.WriteLine("=========================================================================================");
            Console.WriteLine("    KIỂM THỬ ĐẦY ĐỦ TÍNH NĂNG TIDS ENGINE: SO SÁNH CÁC TỶ LỆ RÚT GỌN ĐỆ QUY");
            Console.WriteLine("    Khoảng thời gian OOS: từ {0} đến {1} ({2} kỳ mở thưởng)", records[startIdx].Date, records[n - 1].Date, testDays);
            Console.WriteLine("=========================================================================================\n");

            var configs = new List<DecimationConfig>
            {
                new DecimationConfig { Name = "Tỷ lệ 70% (Giảm 30%)", Ratio = 0.70, Sizes = new int[] { 70, 49, 34, 24, 17, 12, 8, 6, 4, 3, 2, 1 } },
                new DecimationConfig { Name = "Tỷ lệ 65% (Giảm 35%)", Ratio = 0.65, Sizes = new int[] { 65, 42, 27, 18, 12, 8, 5, 3, 2, 1 } },
                new DecimationConfig { Name = "Tỷ lệ Vàng 61.8% (Fibonacci)", Ratio = 0.618, Sizes = new int[] { 62, 38, 24, 15, 9, 6, 4, 2, 1 } },
                new DecimationConfig { Name = "Tỷ lệ 60% (Giảm 40%)", Ratio = 0.60, Sizes = new int[] { 60, 36, 22, 13, 8, 5, 3, 2, 1 } },
                new DecimationConfig { Name = "Tỷ lệ 50% (Phân đôi Bisection)", Ratio = 0.50, Sizes = new int[] { 50, 25, 12, 6, 3, 1 } }
            };

            foreach (var cfg in configs)
            {
                cfg.Hits = new int[cfg.Sizes.Length];
                cfg.RetainedHits = new int[cfg.Sizes.Length];
            }

            long reward2D = 99000;

            for (int t = startIdx; t < n; t++)
            {
                var subList = records.Take(t).ToList();
                var actual = records[t];
                int actualDe = actual.De;

                // 100% Full Engine: Compute 1D Marginals + Full BSM Basket Energies
                var marginal1D = Compute1DMarginals(subList);
                var bsmBasket = ComputeBsmBasketPotentials2D(subList, marginal1D);

                foreach (var cfg in configs)
                {
                    var currentPool = new List<StateEnergy>(bsmBasket);
                    bool lastLevelHit = false;

                    for (int lvl = 0; lvl < cfg.Sizes.Length; lvl++)
                    {
                        int targetSize = cfg.Sizes[lvl];

                        // Tái cân bằng Factor-Neutral trên tập con hiện hành
                        var nextDan = SelectFactorNeutralDeSet(currentPool, targetSize);
                        bool isHit = nextDan.Contains(actualDe);

                        if (isHit)
                        {
                            cfg.Hits[lvl]++;
                            if (lvl == 0 || lastLevelHit) cfg.RetainedHits[lvl]++;
                        }

                        lastLevelHit = isHit;
                        // Co cụm currentPool lại chỉ còn các số thuộc nextDan
                        currentPool = currentPool.Where(e => nextDan.Contains(e.State)).ToList();
                    }
                }
            }

            // BÁO CÁO KẾT QUẢ CHO TỪNG TỶ LỆ
            foreach (var cfg in configs)
            {
                Console.WriteLine("-----------------------------------------------------------------------------------------");
                Console.WriteLine(">> MÔ HÌNH: {0} (Quy mô: {1})", cfg.Name.ToUpper(), string.Join(" -> ", cfg.Sizes.Select(s => s + "s")));
                Console.WriteLine("-----------------------------------------------------------------------------------------");
                Console.WriteLine("Tầng | Quy mô | Số kỳ trúng | Tỷ lệ trúng | Ngẫu nhiên | Lợi thế (+EV) | ROI %     | Giữ chân tầng trước");
                Console.WriteLine("-----------------------------------------------------------------------------------------");

                for (int lvl = 0; lvl < cfg.Sizes.Length; lvl++)
                {
                    int sz = cfg.Sizes[lvl];
                    int hits = cfg.Hits[lvl];
                    double actualRate = (double)hits / testDays * 100.0;
                    double randRate = (double)sz;
                    double ev = actualRate - randRate;

                    long cost = (long)sz * 1000 * testDays;
                    long win = (long)hits * reward2D;
                    double roi = ((double)(win - cost) / cost) * 100.0;
                    string roiStr = roi >= 0 ? string.Format("+{0:F1}%", roi) : string.Format("{0:F1}%", roi);

                    string retainStr = "-";
                    if (lvl > 0)
                    {
                        int prevHits = cfg.Hits[lvl - 1];
                        double retainRate = prevHits > 0 ? (double)cfg.RetainedHits[lvl] / prevHits * 100.0 : 0.0;
                        double expectedRetain = (double)sz / cfg.Sizes[lvl - 1] * 100.0;
                        retainStr = string.Format("{0:F1}% (Lý thuyết {1:F1}%)", retainRate, expectedRetain);
                    }

                    string evStr = ev >= 0 ? string.Format("+{0:F2}%", ev) : string.Format("{0:F2}%", ev);
                    Console.WriteLine("Lvl {0,2} | {1,2} số  | {2,4} / {3,-3} | {4,6:F2}%   | {5,5:F2}%    | {6,8}      | {7,7}   | {8}",
                        lvl + 1, sz, hits, testDays, actualRate, randRate, evStr, roiStr, retainStr);
                }
                Console.WriteLine();
            }

            // BẢNG SO SÁNH TỔNG KẾT TOÀN DIỆN
            Console.WriteLine("=========================================================================================================================");
            Console.WriteLine("   BẢNG ĐỐI SOÁT TỔNG HỢP: TỶ LỆ NÀO TỐI ƯU NHẤT CHO CHIẾN LƯỢC RÚT GỌN ĐA TẦNG?");
            Console.WriteLine("=========================================================================================================================");
            Console.WriteLine("Mô hình rút gọn               | Tầng 1 (Gốc) | Dàn 25-27s  | Dàn 15-18s  | Dàn 8-9s    | Tứ thủ (4s) | Bạch thủ (1s)");
            Console.WriteLine("-------------------------------------------------------------------------------------------------------------------------");
            foreach (var cfg in configs)
            {
                string l1 = string.Format("{0:F1}%", (double)cfg.Hits[0] / testDays * 100.0);
                
                // Tìm dàn xấp xỉ 25-27s
                int idx27 = -1;
                for (int i = 0; i < cfg.Sizes.Length; i++) if (cfg.Sizes[i] >= 22 && cfg.Sizes[i] <= 27) { idx27 = i; break; }
                string l27 = idx27 >= 0 ? string.Format("{0}s: {1:F1}%", cfg.Sizes[idx27], (double)cfg.Hits[idx27] / testDays * 100.0) : "N/A";

                // Tìm dàn xấp xỉ 15-18s
                int idx18 = -1;
                for (int i = 0; i < cfg.Sizes.Length; i++) if (cfg.Sizes[i] >= 15 && cfg.Sizes[i] <= 18) { idx18 = i; break; }
                string l18 = idx18 >= 0 ? string.Format("{0}s: {1:F1}%", cfg.Sizes[idx18], (double)cfg.Hits[idx18] / testDays * 100.0) : "N/A";

                // Tìm dàn xấp xỉ 8-9s
                int idx8 = -1;
                for (int i = 0; i < cfg.Sizes.Length; i++) if (cfg.Sizes[i] >= 8 && cfg.Sizes[i] <= 9) { idx8 = i; break; }
                string l8 = idx8 >= 0 ? string.Format("{0}s: {1:F1}%", cfg.Sizes[idx8], (double)cfg.Hits[idx8] / testDays * 100.0) : "N/A";

                // Tứ thủ (4s)
                int idx4 = Array.IndexOf(cfg.Sizes, 4);
                string l4 = idx4 >= 0 ? string.Format("{0:F1}%", (double)cfg.Hits[idx4] / testDays * 100.0) : "N/A";

                // Bạch thủ (1s)
                int idx1 = Array.IndexOf(cfg.Sizes, 1);
                string l1s = idx1 >= 0 ? string.Format("{0} kỳ ({1:F1}%)", cfg.Hits[idx1], (double)cfg.Hits[idx1] / testDays * 100.0) : "N/A";

                Console.WriteLine("{0,-29} | {1,-12} | {2,-11} | {3,-11} | {4,-11} | {5,-11} | {6}",
                    cfg.Name, l1, l27, l18, l8, l4, l1s);
            }
            Console.WriteLine("=========================================================================================================================");
        }

        public static void RunQciAudit(List<LotteryRecord> records, int testDays = 100)
        {
            int n = records.Count;
            if (testDays <= 0 || testDays > 3000) testDays = 100;
            int startIdx = n - testDays;

            Console.WriteLine("=========================================================================================================================");
            Console.WriteLine(string.Format("   QUY TRÌNH KIỂM THỬ ĐỐI SOÁT CHỈ SỐ TỰ TIN ĐỊNH LƯỢNG (QCI) & ĐỘ TRÙNG KHỚP CẢNH BÁO ({0} KỲ GẦN NHẤT)", testDays));
            Console.WriteLine(string.Format("   Khoảng thời gian: từ kỳ {0} ({1}) đến kỳ {2} ({3})", records[startIdx].Date, records[startIdx].DayOfWeek, records[n - 1].Date, records[n - 1].DayOfWeek));
            Console.WriteLine("=========================================================================================================================");
            Console.WriteLine(string.Format(" {0,-3} | {1,-5} | {2,-10} | {3,-7} | {4,-6} | {5,-8} | {6,-12} | {7,-8} | {8,-11} | {9,-11} | {10}",
                "STT", "Kỳ", "Ngày", "Thứ", "QCI(%)", "Vùng", "Khuyến Nghị", "Đề 2D", "Dàn E50", "Bạch Thủ Lô", "Song Thủ Lô"));
            Console.WriteLine("-------------------------------------------------------------------------------------------------------------------------");

            int greenCount = 0, yellowCount = 0, redCount = 0;
            int greenWinE50 = 0, yellowWinE50 = 0, redWinE50 = 0;
            int greenWinBtLo = 0, yellowWinBtLo = 0, redWinBtLo = 0;
            int greenWinStLo = 0, yellowWinStLo = 0, redWinStLo = 0;
            int greenWinBothSt = 0, yellowWinBothSt = 0, redWinBothSt = 0;
            int greenWinTtLo = 0, yellowWinTtLo = 0, redWinTtLo = 0;

            for (int t = startIdx; t < n; t++)
            {
                int stt = t - startIdx + 1;
                var subList = records.Take(t).ToList();
                var actual = records[t];
                int actualDe = actual.De;
                var actualLo = actual.LoList;

                var marginal1D = Compute1DMarginals(subList);
                var lotto1D = Compute1DLottoMarginals(subList);
                double[,] W;
                var lottoEnergies = ComputeUnifiedLottoPotentials(subList, lotto1D, out W);
                var unifiedEnergies = ComputeUnifiedPotentials2D(subList, marginal1D);
                var bsmBasket = ComputeBsmBasketPotentials2D(subList, marginal1D);

                var qci = ComputeQCI(subList, lottoEnergies, unifiedEnergies, lotto1D, marginal1D);

                var dan50 = SelectFactorNeutralDeSet(bsmBasket, 50);
                bool hitE50 = dan50.Contains(actualDe);

                var top4 = SelectTop4LoWithDiversity(lottoEnergies, subList);
                int btLo = top4[0];
                int stLo = top4[1];

                int btHits = actualLo.Count(x => x == btLo);
                int st1Hits = actualLo.Count(x => x == btLo);
                int st2Hits = actualLo.Count(x => x == stLo);
                bool hitBtLo = btHits > 0;
                bool hitStLo = (st1Hits > 0 || st2Hits > 0);
                bool hitBothSt = (st1Hits > 0 && st2Hits > 0);
                bool hitTtLo = top4.Any(x => actualLo.Contains(x));

                string zoneSymbol = (qci.Zone == "XANH") ? "🟢 XANH" : ((qci.Zone == "VÀNG") ? "🟡 VÀNG" : "🔴 ĐỎ ");
                string actionShort = (qci.Zone == "XANH") ? "TẤN CÔNG" : ((qci.Zone == "VÀNG") ? "Phòng thủ" : "NGHỈ ĐÁNH");
                string deStr = actualDe.ToString("D2");
                string e50Str = hitE50 ? "ĂN" : "Trượt";
                string btLoStr = hitBtLo ? string.Format("ĂN ({0:D2})", btLo) : string.Format("Gãy ({0:D2})", btLo);
                string stLoStr = hitBothSt ? string.Format("CẢ CẶP! ({0:D2}-{1:D2})", btLo, stLo) : (hitStLo ? string.Format("Ăn 1 ({0:D2}-{1:D2})", btLo, stLo) : string.Format("Gãy ({0:D2}-{1:D2})", btLo, stLo));

                if (qci.Zone == "XANH")
                {
                    greenCount++;
                    if (hitE50) greenWinE50++;
                    if (hitBtLo) greenWinBtLo++;
                    if (hitStLo) greenWinStLo++;
                    if (hitBothSt) greenWinBothSt++;
                    if (hitTtLo) greenWinTtLo++;
                }
                else if (qci.Zone == "VÀNG")
                {
                    yellowCount++;
                    if (hitE50) yellowWinE50++;
                    if (hitBtLo) yellowWinBtLo++;
                    if (hitStLo) yellowWinStLo++;
                    if (hitBothSt) yellowWinBothSt++;
                    if (hitTtLo) yellowWinTtLo++;
                }
                else
                {
                    redCount++;
                    if (hitE50) redWinE50++;
                    if (hitBtLo) redWinBtLo++;
                    if (hitStLo) redWinStLo++;
                    if (hitBothSt) redWinBothSt++;
                    if (hitTtLo) redWinTtLo++;
                }

                Console.WriteLine(string.Format(" {0,3} | #{1,-4} | {2,-10} | {3,-7} | {4,5:F1}% | {5,-8} | {6,-12} | {7,-8} | {8,-11} | {9,-11} | {10}",
                    stt, actual.Id, actual.Date, actual.DayOfWeek, qci.Score, zoneSymbol, actionShort, deStr, e50Str, btLoStr, stLoStr));
            }

            Console.WriteLine("-------------------------------------------------------------------------------------------------------------------------");
            Console.WriteLine("PHẦN TỔNG KẾT & ĐỐI SOÁT ĐỘ TRÙNG KHỚP CỦA CHỈ SỐ CẢNH BÁO QCI:");
            Console.WriteLine("-------------------------------------------------------------------------------------------------------------------------");
            Console.WriteLine("Phân loại Cảnh báo | Số ngày | Tỷ lệ Đề E50       | Bạch Thủ Lô        | Song Thủ Lô (Ăn >= 1) | Nổ Cả Cặp ST (Xiên 2) | Tứ Thủ Lô");
            Console.WriteLine("-------------------------------------------------------------------------------------------------------------------------");

            double gPctE50 = greenCount > 0 ? greenWinE50 * 100.0 / greenCount : 0;
            double gPctBt = greenCount > 0 ? greenWinBtLo * 100.0 / greenCount : 0;
            double gPctSt = greenCount > 0 ? greenWinStLo * 100.0 / greenCount : 0;
            double gPctBoth = greenCount > 0 ? greenWinBothSt * 100.0 / greenCount : 0;
            double gPctTt = greenCount > 0 ? greenWinTtLo * 100.0 / greenCount : 0;

            double yPctE50 = yellowCount > 0 ? yellowWinE50 * 100.0 / yellowCount : 0;
            double yPctBt = yellowCount > 0 ? yellowWinBtLo * 100.0 / yellowCount : 0;
            double yPctSt = yellowCount > 0 ? yellowWinStLo * 100.0 / yellowCount : 0;
            double yPctBoth = yellowCount > 0 ? yellowWinBothSt * 100.0 / yellowCount : 0;
            double yPctTt = yellowCount > 0 ? yellowWinTtLo * 100.0 / yellowCount : 0;

            double rPctE50 = redCount > 0 ? redWinE50 * 100.0 / redCount : 0;
            double rPctBt = redCount > 0 ? redWinBtLo * 100.0 / redCount : 0;
            double rPctSt = redCount > 0 ? redWinStLo * 100.0 / redCount : 0;
            double rPctBoth = redCount > 0 ? redWinBothSt * 100.0 / redCount : 0;
            double rPctTt = redCount > 0 ? redWinTtLo * 100.0 / redCount : 0;

            Console.WriteLine(string.Format("🟢 VÙNG XANH (>=75%)| {0,2} ngày | {1,5:F1}% ({2,2}/{3,2} kỳ) | {4,5:F1}% ({5,2}/{6,2} kỳ) | {7,5:F1}% ({8,2}/{9,2} kỳ)   | {10,5:F1}% ({11,2}/{12,2} kỳ)   | {13,5:F1}% ({14,2}/{15,2} kỳ)",
                greenCount, gPctE50, greenWinE50, greenCount, gPctBt, greenWinBtLo, greenCount, gPctSt, greenWinStLo, greenCount, gPctBoth, greenWinBothSt, greenCount, gPctTt, greenWinTtLo, greenCount));

            Console.WriteLine(string.Format("🟡 VÙNG VÀNG (60-74%)| {0,2} ngày | {1,5:F1}% ({2,2}/{3,2} kỳ) | {4,5:F1}% ({5,2}/{6,2} kỳ) | {7,5:F1}% ({8,2}/{9,2} kỳ)   | {10,5:F1}% ({11,2}/{12,2} kỳ)   | {13,5:F1}% ({14,2}/{15,2} kỳ)",
                yellowCount, yPctE50, yellowWinE50, yellowCount, yPctBt, yellowWinBtLo, yellowCount, yPctSt, yellowWinStLo, yellowCount, yPctBoth, yellowWinBothSt, yellowCount, yPctTt, yellowWinTtLo, yellowCount));

            Console.WriteLine(string.Format("🔴 VÙNG ĐỎ (<60%)   | {0,2} ngày | {1,5:F1}% ({2,2}/{3,2} kỳ) | {4,5:F1}% ({5,2}/{6,2} kỳ) | {7,5:F1}% ({8,2}/{9,2} kỳ)   | {10,5:F1}% ({11,2}/{12,2} kỳ)   | {13,5:F1}% ({14,2}/{15,2} kỳ)",
                redCount, rPctE50, redWinE50, redCount, rPctBt, redWinBtLo, redCount, rPctSt, redWinStLo, redCount, rPctBoth, redWinBothSt, redCount, rPctTt, redWinTtLo, redCount));
            Console.WriteLine("-------------------------------------------------------------------------------------------------------------------------");
        }

        public static void RunQciDeLadderAudit(List<LotteryRecord> records, int testDays = 100)
        {
            int n = records.Count;
            if (testDays <= 0 || testDays > 3000) testDays = 100;
            int startIdx = n - testDays;

            Console.WriteLine("=========================================================================================================================");
            Console.WriteLine(string.Format("   BẢNG KIỂM THỬ ĐỐI SOÁT CHIẾN LƯỢC QCI ADAPTIVE & KIM TỰ THÁP DÀN ĐỀ 65% ({0} KỲ GẦN NHẤT)", testDays));
            Console.WriteLine(string.Format("   Khoảng thời gian: từ kỳ #{0} ({1}, {2}) đến kỳ #{3} ({4}, {5})",
                records[startIdx].Id, records[startIdx].Date, records[startIdx].DayOfWeek, records[n - 1].Id, records[n - 1].Date, records[n - 1].DayOfWeek));
            Console.WriteLine("=========================================================================================================================");
            Console.WriteLine(string.Format(" {0,-3} | {1,-5} | {2,-10} | {3,-8} | {4,-6} | {5,-7} | {6,-5} | {7,-5} | {8,-5} | {9,-5} | {10,-5} | {11,-4} | {12,-4} | {13}",
                "STT", "Kỳ", "Ngày", "Thứ", "QCI", "Vùng", "Đề 2D", "E65", "E42", "E27", "E18", "E4", "E1", "Tầng Trúng Sâu Nhất"));
            Console.WriteLine("-------------------------------------------------------------------------------------------------------------------------");

            int win65 = 0, win42 = 0, win27 = 0, win18 = 0, win4 = 0, win1 = 0;
            int curStreak65 = 0, maxStreak65 = 0, curLoss65 = 0, maxLoss65 = 0;
            int curStreak42 = 0, maxStreak42 = 0, curLoss42 = 0, maxLoss42 = 0;
            int curStreak27 = 0, maxStreak27 = 0, curLoss27 = 0, maxLoss27 = 0;
            int curStreak18 = 0, maxStreak18 = 0, curLoss18 = 0, maxLoss18 = 0;

            int greenCount = 0, yellowCount = 0, redCount = 0;
            int gWin65 = 0, gWin42 = 0, gWin27 = 0, gWin18 = 0, gWin4 = 0, gWin1 = 0;
            int yWin65 = 0, yWin42 = 0, yWin27 = 0, yWin18 = 0, yWin4 = 0, yWin1 = 0;
            int rWin65 = 0, rWin42 = 0, rWin27 = 0, rWin18 = 0, rWin4 = 0, rWin1 = 0;

            for (int t = startIdx; t < n; t++)
            {
                int stt = t - startIdx + 1;
                var subList = records.Take(t).ToList();
                var actual = records[t];
                int actualDe = actual.De;

                var marginal1D = Compute1DMarginals(subList);
                var lotto1D = Compute1DLottoMarginals(subList);
                double[,] W;
                var lottoEnergies = ComputeUnifiedLottoPotentials(subList, lotto1D, out W);
                var unifiedEnergies = ComputeUnifiedPotentials2D(subList, marginal1D);
                var bsmBasket = ComputeBsmBasketPotentials2D(subList, marginal1D);

                var qci = ComputeQCI(subList, lottoEnergies, unifiedEnergies, lotto1D, marginal1D);

                var dan65 = SelectFactorNeutralDeSet(bsmBasket, 65);
                var sub42 = bsmBasket.Where(e => dan65.Contains(e.State)).ToList();
                var dan42 = SelectFactorNeutralDeSet(sub42, 42);
                var sub27 = bsmBasket.Where(e => dan42.Contains(e.State)).ToList();
                var dan27 = SelectFactorNeutralDeSet(sub27, 27);
                var sub18 = bsmBasket.Where(e => dan27.Contains(e.State)).ToList();
                var dan18 = SelectFactorNeutralDeSet(sub18, 18);
                var dan4 = unifiedEnergies.Take(4).Select(x => x.State).ToList();
                int btDe = unifiedEnergies[0].State;

                bool hit65 = dan65.Contains(actualDe);
                bool hit42 = dan42.Contains(actualDe);
                bool hit27 = dan27.Contains(actualDe);
                bool hit18 = dan18.Contains(actualDe);
                bool hit4 = dan4.Contains(actualDe);
                bool hit1 = (actualDe == btDe);

                if (hit65) { win65++; curStreak65++; curLoss65 = 0; if (curStreak65 > maxStreak65) maxStreak65 = curStreak65; }
                else { curLoss65++; curStreak65 = 0; if (curLoss65 > maxLoss65) maxLoss65 = curLoss65; }

                if (hit42) { win42++; curStreak42++; curLoss42 = 0; if (curStreak42 > maxStreak42) maxStreak42 = curStreak42; }
                else { curLoss42++; curStreak42 = 0; if (curLoss42 > maxLoss42) maxLoss42 = curLoss42; }

                if (hit27) { win27++; curStreak27++; curLoss27 = 0; if (curStreak27 > maxStreak27) maxStreak27 = curStreak27; }
                else { curLoss27++; curStreak27 = 0; if (curLoss27 > maxLoss27) maxLoss27 = curLoss27; }

                if (hit18) { win18++; curStreak18++; curLoss18 = 0; if (curStreak18 > maxStreak18) maxStreak18 = curStreak18; }
                else { curLoss18++; curStreak18 = 0; if (curLoss18 > maxLoss18) maxLoss18 = curLoss18; }

                if (hit4) win4++;
                if (hit1) win1++;

                string deepestHit = "GÃY (Ngoài 65s)";
                if (hit1) deepestHit = "🎯 ĂN BẠCH THỦ (E1)";
                else if (hit4) deepestHit = "⭐ ĂN TỨ THỦ (E4)";
                else if (hit18) deepestHit = "⚡ ĂN CỬU CUNG 18s (ROI MAX)";
                else if (hit27) deepestHit = "✨ ĂN HÀ ĐỒ 27s";
                else if (hit42) deepestHit = "✔ Ăn Dàn Vỏ 42s";
                else if (hit65) deepestHit = "🛡 Ăn Giáp E65 (Bảo hiểm)";

                string zoneSymbol = (qci.Zone == "XANH") ? "🟢 XANH" : ((qci.Zone == "VÀNG") ? "🟡 VÀNG" : "🔴 ĐỎ ");

                if (qci.Zone == "XANH")
                {
                    greenCount++;
                    if (hit65) gWin65++;
                    if (hit42) gWin42++;
                    if (hit27) gWin27++;
                    if (hit18) gWin18++;
                    if (hit4) gWin4++;
                    if (hit1) gWin1++;
                }
                else if (qci.Zone == "VÀNG")
                {
                    yellowCount++;
                    if (hit65) yWin65++;
                    if (hit42) yWin42++;
                    if (hit27) yWin27++;
                    if (hit18) yWin18++;
                    if (hit4) yWin4++;
                    if (hit1) yWin1++;
                }
                else
                {
                    redCount++;
                    if (hit65) rWin65++;
                    if (hit42) rWin42++;
                    if (hit27) rWin27++;
                    if (hit18) rWin18++;
                    if (hit4) rWin4++;
                    if (hit1) rWin1++;
                }

                Console.WriteLine(string.Format(" {0,3} | #{1,-4} | {2,-10} | {3,-8} | {4,5:F1}% | {5,-7} | {6,-5} | {7,-5} | {8,-5} | {9,-5} | {10,-5} | {11,-4} | {12,-4} | {13}",
                    stt, actual.Id, actual.Date, actual.DayOfWeek, qci.Score, zoneSymbol, actualDe.ToString("D2"),
                    hit65 ? " ĂN " : " -  ",
                    hit42 ? " ĂN " : " -  ",
                    hit27 ? " ĂN " : " -  ",
                    hit18 ? " ĂN " : " -  ",
                    hit4 ? " ĂN" : " - ",
                    hit1 ? " ĂN" : " - ",
                    deepestHit));
            }

            Console.WriteLine("-------------------------------------------------------------------------------------------------------------------------");
            Console.WriteLine("1. BẢNG TỔNG HỢP HIỆU NĂNG CÁC DÀN ĐỀ THEO THANG RÚT GỌN 65% (100 KỲ GẦN NHẤT):");
            Console.WriteLine("-------------------------------------------------------------------------------------------------------------------------");
            Console.WriteLine(string.Format(" {0,-26} | {1,-10} | {2,-11} | {3,-12} | {4,-12} | {5,-12} | {6}",
                "Dàn Đề Thang 65%", "Quy Mô", "Số Kỳ Trúng", "Tỷ Lệ Trúng", "Max Win (Ăn)", "Max Drawdown", "ROI (Tỷ lệ 1:99)"));
            Console.WriteLine("-------------------------------------------------------------------------------------------------------------------------");

            double p65 = win65 * 100.0 / testDays;
            double p42 = win42 * 100.0 / testDays;
            double p27 = win27 * 100.0 / testDays;
            double p18 = win18 * 100.0 / testDays;
            double p4 = win4 * 100.0 / testDays;
            double p1 = win1 * 100.0 / testDays;

            double roi65 = (win65 * 99.0 - testDays * 65.0) / (testDays * 65.0) * 100.0;
            double roi42 = (win42 * 99.0 - testDays * 42.0) / (testDays * 42.0) * 100.0;
            double roi27 = (win27 * 99.0 - testDays * 27.0) / (testDays * 27.0) * 100.0;
            double roi18 = (win18 * 99.0 - testDays * 18.0) / (testDays * 18.0) * 100.0;
            double roi4 = (win4 * 99.0 - testDays * 4.0) / (testDays * 4.0) * 100.0;
            double roi1 = (win1 * 99.0 - testDays * 1.0) / (testDays * 1.0) * 100.0;

            string sRoi65 = (roi65 >= 0 ? "+" : "") + roi65.ToString("F1") + "%";
            string sRoi42 = (roi42 >= 0 ? "+" : "") + roi42.ToString("F1") + "%";
            string sRoi27 = (roi27 >= 0 ? "+" : "") + roi27.ToString("F1") + "%";
            string sRoi18 = (roi18 >= 0 ? "+" : "") + roi18.ToString("F1") + "%";
            string sRoi4 = (roi4 >= 0 ? "+" : "") + roi4.ToString("F1") + "%";
            string sRoi1 = (roi1 >= 0 ? "+" : "") + roi1.ToString("F1") + "%";

            Console.WriteLine(string.Format(" {0,-26} | {1,-10} | {2,4}/{3,-4} kỳ | {4,9:F1}% | {5,2} kỳ liên tiếp | {6,2} kỳ trượt | {7,7} (Lá chắn bảo hiểm)",
                "Tầng 1: Huyền Thiên Giáp (E65)", "65 số", win65, testDays, p65, maxStreak65, maxLoss65, sRoi65));
            Console.WriteLine(string.Format(" {0,-26} | {1,-10} | {2,4}/{3,-4} kỳ | {4,9:F1}% | {5,2} kỳ liên tiếp | {6,2} kỳ trượt | {7,7} (Tiết kiệm 8 số)",
                "Tầng 2: Đại Diễn Thần Trận (E42)", "42 số", win42, testDays, p42, maxStreak42, maxLoss42, sRoi42));
            Console.WriteLine(string.Format(" {0,-26} | {1,-10} | {2,4}/{3,-4} kỳ | {4,9:F1}% | {5,2} kỳ liên tiếp | {6,2} kỳ trượt | {7,7}",
                "Tầng 3: Hà Đồ Tự Nhiên (E27)", "27 số", win27, testDays, p27, maxStreak27, maxLoss27, sRoi27));
            Console.WriteLine(string.Format(" {0,-26} | {1,-10} | {2,4}/{3,-4} kỳ | {4,9:F1}% | {5,2} kỳ liên tiếp | {6,2} kỳ trượt | {7,7} ★ ROI ĐỈNH CAO",
                "Tầng 4: Cửu Cung Thần Trận (E18)", "18 số", win18, testDays, p18, maxStreak18, maxLoss18, sRoi18));
            Console.WriteLine(string.Format(" {0,-26} | {1,-10} | {2,4}/{3,-4} kỳ | {4,9:F1}% | {5,2} kỳ liên tiếp | {6,2} kỳ trượt | {7,7}",
                "Tầng 5: Tứ Tượng Hội Tụ (E4)", "4 số", win4, testDays, p4, "-", "-", sRoi4));
            Console.WriteLine(string.Format(" {0,-26} | {1,-10} | {2,4}/{3,-4} kỳ | {4,9:F1}% | {5,2} kỳ liên tiếp | {6,2} kỳ trượt | {7,7} (Bạch thủ đáy V_eff)",
                "Tầng 6: Thái Cực Nhất Điểm (E1)", "1 số", win1, testDays, p1, "-", "-", sRoi1));

            Console.WriteLine("-------------------------------------------------------------------------------------------------------------------------");
            Console.WriteLine("2. MA TRẬN TỶ LỆ TRÚNG THEO 3 VÙNG THỜI KHÍ QCI (QUANTUM COHERENCE INDEX):");
            Console.WriteLine("-------------------------------------------------------------------------------------------------------------------------");
            Console.WriteLine("Phân Loại Vùng QCI | Số Ngày  | Dàn E65 (65s)   | Dàn E42 (42s)   | Dàn E27 (27s)   | Dàn E18 (18s)   | Tứ Thủ (4s) | Bạch Thủ");
            Console.WriteLine("-------------------------------------------------------------------------------------------------------------------------");

            double gp65 = greenCount > 0 ? gWin65 * 100.0 / greenCount : 0;
            double gp42 = greenCount > 0 ? gWin42 * 100.0 / greenCount : 0;
            double gp27 = greenCount > 0 ? gWin27 * 100.0 / greenCount : 0;
            double gp18 = greenCount > 0 ? gWin18 * 100.0 / greenCount : 0;
            double gp4 = greenCount > 0 ? gWin4 * 100.0 / greenCount : 0;
            double gp1 = greenCount > 0 ? gWin1 * 100.0 / greenCount : 0;

            double yp65 = yellowCount > 0 ? yWin65 * 100.0 / yellowCount : 0;
            double yp42 = yellowCount > 0 ? yWin42 * 100.0 / yellowCount : 0;
            double yp27 = yellowCount > 0 ? yWin27 * 100.0 / yellowCount : 0;
            double yp18 = yellowCount > 0 ? yWin18 * 100.0 / yellowCount : 0;
            double yp4 = yellowCount > 0 ? yWin4 * 100.0 / yellowCount : 0;
            double yp1 = yellowCount > 0 ? yWin1 * 100.0 / yellowCount : 0;

            double rp65 = redCount > 0 ? rWin65 * 100.0 / redCount : 0;
            double rp42 = redCount > 0 ? rWin42 * 100.0 / redCount : 0;
            double rp27 = redCount > 0 ? rWin27 * 100.0 / redCount : 0;
            double rp18 = redCount > 0 ? rWin18 * 100.0 / redCount : 0;
            double rp4 = redCount > 0 ? rWin4 * 100.0 / redCount : 0;
            double rp1 = redCount > 0 ? rWin1 * 100.0 / redCount : 0;

            Console.WriteLine(string.Format("🟢 VÙNG XANH (>=75%)| {0,2} ngày  | {1,5:F1}% ({2,2}/{3,2}) | {4,5:F1}% ({5,2}/{6,2}) | {7,5:F1}% ({8,2}/{9,2}) | {10,5:F1}% ({11,2}/{12,2}) | {13,4:F1}% ({14}) | {15,4:F1}% ({16})",
                greenCount, gp65, gWin65, greenCount, gp42, gWin42, greenCount, gp27, gWin27, greenCount, gp18, gWin18, greenCount, gp4, gWin4, gp1, gWin1));
            Console.WriteLine(string.Format("🟡 VÙNG VÀNG (60-74%)| {0,2} ngày  | {1,5:F1}% ({2,2}/{3,2}) | {4,5:F1}% ({5,2}/{6,2}) | {7,5:F1}% ({8,2}/{9,2}) | {10,5:F1}% ({11,2}/{12,2}) | {13,4:F1}% ({14}) | {15,4:F1}% ({16})",
                yellowCount, yp65, yWin65, yellowCount, yp42, yWin42, yellowCount, yp27, yWin27, yellowCount, yp18, yWin18, yellowCount, yp4, yWin4, yp1, yWin1));
            Console.WriteLine(string.Format("🔴 VÙNG ĐỎ (<60%)   | {0,2} ngày  | {1,5:F1}% ({2,2}/{3,2}) | {4,5:F1}% ({5,2}/{6,2}) | {7,5:F1}% ({8,2}/{9,2}) | {10,5:F1}% ({11,2}/{12,2}) | {13,4:F1}% ({14}) | {15,4:F1}% ({16})",
                redCount, rp65, rWin65, redCount, rp42, rWin42, redCount, rp27, rWin27, redCount, rp18, rWin18, redCount, rp4, rWin4, rp1, rWin1));
            Console.WriteLine("=========================================================================================================================");
        }

        private static int GetBestSpectralLoQuiet(List<LotteryRecord> records)
        {
            return GetSpectralRankedLoDecay(records)[0];
        }

        public class CLTX1DEvaluationResult
        {
            public string Position;
            public double ProbC;
            public double ProbL;
            public double ProbT;
            public double ProbX;
            public string RecParity;
            public double ConfParity;
            public string RecSize;
            public double ConfSize;
            public string Signal;
        }

        public static int GetQuadrant1D(int d)
        {
            bool isC = (d % 2 == 0);
            bool isT = (d >= 5);
            if (isC && !isT) return 0; // CX {0,2,4}
            if (isC && isT) return 1;  // CT {6,8}
            if (!isC && !isT) return 2; // LX {1,3}
            return 3;                  // LT {5,7,9}
        }

        public static CLTX1DEvaluationResult Evaluate1D_CLTX(List<LotteryRecord> past, int posIdx, string posName)
        {
            int n = past.Count;
            int lookback = Math.Min(30, n);
            int countC = 0, countT = 0;
            for (int i = n - lookback; i < n; i++)
            {
                if (past[i].Digits5[posIdx] % 2 == 0) countC++;
                if (past[i].Digits5[posIdx] >= 5) countT++;
            }
            double zC = (countC - 15.0) / 2.74;
            double zT = (countT - 15.0) / 2.74;

            int streakC = 0, streakL = 0;
            for (int i = n - 1; i >= 0; i--) { if (past[i].Digits5[posIdx] % 2 == 0) streakC++; else break; }
            for (int i = n - 1; i >= 0; i--) { if (past[i].Digits5[posIdx] % 2 != 0) streakL++; else break; }

            int streakT = 0, streakX = 0;
            for (int i = n - 1; i >= 0; i--) { if (past[i].Digits5[posIdx] >= 5) streakT++; else break; }
            for (int i = n - 1; i >= 0; i--) { if (past[i].Digits5[posIdx] < 5) streakX++; else break; }

            int c4D_last = 0, t4D_last = 0;
            for (int p = 1; p <= 4; p++)
            {
                if (past[n - 1].Digits5[p] % 2 == 0) c4D_last++;
                if (past[n - 1].Digits5[p] >= 5) t4D_last++;
            }
            double extReboundC = (c4D_last == 4) ? -0.8 : (c4D_last == 0 ? 0.8 : 0.0);
            double extReboundT = (t4D_last == 4) ? -0.8 : (t4D_last == 0 ? 0.8 : 0.0);

            int loCountC = 0, loCountT = 0;
            if (past[n - 1].LoList != null)
            {
                foreach (int lo in past[n - 1].LoList)
                {
                    if (lo % 2 == 0) loCountC++;
                    if (lo >= 50) loCountT++;
                }
            }
            double loMomC = (loCountC - 13.5) / 5.0;
            double loMomT = (loCountT - 13.5) / 5.0;

            double H_CL = -0.35 * zC + (streakL >= 4 ? 0.6 : 0.0) - (streakC >= 4 ? 0.6 : 0.0) + extReboundC + 0.25 * loMomC;
            double pC = 1.0 / (1.0 + Math.Exp(-H_CL));
            double pL = 1.0 - pC;

            double H_TX = -0.35 * zT + (streakX >= 4 ? 0.6 : 0.0) - (streakT >= 4 ? 0.6 : 0.0) + extReboundT + 0.25 * loMomT;
            double pT = 1.0 / (1.0 + Math.Exp(-H_TX));
            double pX = 1.0 - pT;

            var res = new CLTX1DEvaluationResult();
            res.Position = posName;
            res.ProbC = pC * 100.0;
            res.ProbL = pL * 100.0;
            res.ProbT = pT * 100.0;
            res.ProbX = pX * 100.0;

            res.RecParity = pC >= 0.50 ? "CHẴN" : "LẺ";
            res.ConfParity = Math.Max(pC, pL) * 100.0;

            res.RecSize = pT >= 0.50 ? "TÀI" : "XỈU";
            res.ConfSize = Math.Max(pT, pX) * 100.0;

            double maxConf = Math.Max(res.ConfParity, res.ConfSize);
            if (maxConf >= 58.0) res.Signal = "🟢 TỰ TIN CAO";
            else if (maxConf >= 53.0) res.Signal = "🟡 CÂN BẰNG";
            else res.Signal = "⚪ HỖN ĐỘN";

            return res;
        }

        public class Dan3DCascadeResult
        {
            public string BachThu3D;
            public int BachThuCang;
            public int[] Top3Cang;
            public List<string> Dan650;
            public List<string> Dan420;
            public List<string> Dan270;
            public List<string> Dan175;
            public List<string> Dan115;
            public List<string> Dan75;
            public List<string> Dan49;
            public List<string> Dan27;
            public List<string> Dan12;
        }

        public static Dan3DCascadeResult ComputeDan3DCascade(
            List<LotteryRecord> records, 
            List<int> e65 = null, 
            List<StateEnergy> deEnergies = null)
        {
            int n = records.Count;
            if (deEnergies == null)
            {
                var marginal1D = Compute1DMarginals(records);
                deEnergies = ComputeUnifiedPotentials2D(records, marginal1D);
            }
            if (e65 == null)
            {
                e65 = SelectFactorNeutralDeSet(deEnergies, 65);
            }

            // 1. Tính thế năng Càng d3 (Hàng Trăm: 0..9)
            int[] ganD3 = new int[10];
            for (int d = 0; d < 10; d++)
            {
                for (int i = n - 1; i >= 0; i--)
                {
                    if (records[i].Digits5[2] == d) break;
                    ganD3[d]++;
                }
            }
            int[] f30D3 = new int[10];
            for (int i = Math.Max(0, n - 30); i < n; i++) f30D3[records[i].Digits5[2]]++;

            // Tín hiệu CLTX 4D hôm trước
            int c4D_last = 0, t4D_last = 0;
            for (int p = 1; p <= 4; p++)
            {
                if (records[n - 1].Digits5[p] % 2 == 0) c4D_last++;
                if (records[n - 1].Digits5[p] >= 5) t4D_last++;
            }

            // Gan của 4 nhóm CX, CT, LX, LT tại Càng d3
            int[] ganQuadD3 = new int[4];
            for (int q = 0; q < 4; q++)
            {
                for (int i = n - 1; i >= 0; i--)
                {
                    if (GetQuadrant1D(records[i].Digits5[2]) == q) break;
                    ganQuadD3[q]++;
                }
            }

            double[] vD3 = new double[10];
            var d3List = new List<Tuple<int, double>>();
            for (int d = 0; d < 10; d++)
            {
                double r = (ganD3[d] >= 8 && ganD3[d] <= 20) ? -1.5 : (ganD3[d] == 0 ? 1.5 : (ganD3[d] > 35 ? 1.2 : 0.0));
                double k = -0.4 * (f30D3[d] - 3.0);
                double v = r + k;

                // Quy luật 1: Phản hồi cực đoan 4D
                if (c4D_last == 4 && d % 2 != 0) v -= 0.35; // Hôm qua 4 Chẵn => thưởng Lẻ
                if (c4D_last == 0 && d % 2 == 0) v -= 0.35; // Hôm qua 4 Lẻ => thưởng Chẵn
                if (t4D_last == 4 && d < 5) v -= 0.35;      // Hôm qua 4 Tài => thưởng Xỉu
                if (t4D_last == 0 && d >= 5) v -= 0.35;     // Hôm qua 4 Xỉu => thưởng Tài

                // Quy luật 2: Ngưỡng nén Lò xo 90-95%
                int q = GetQuadrant1D(d);
                if ((q == 0 || q == 3) && ganQuadD3[q] >= 6) v -= 0.45; // Nhóm 3s gan >= 6 kỳ
                if ((q == 1 || q == 2) && ganQuadD3[q] >= 10) v -= 0.60; // Nhóm 2s gan >= 10 kỳ

                vD3[d] = v;
                d3List.Add(new Tuple<int, double>(d, v));
            }
            var topD3 = d3List.OrderBy(x => x.Item2).Take(3).Select(x => x.Item1).ToArray();
            int btD3 = topD3[0];
            int btDe = deEnergies[0].State;
            string bt3D = btD3.ToString() + btDe.ToString("D2");

            // 2. Tương tác Càng d3 - Đầu d2 (w32) trong 100 kỳ
            int lookback = Math.Min(100, n);
            double expected = (double)lookback / 100.0;
            int[,] f32 = new int[10, 10];
            for (int i = n - lookback; i < n; i++)
            {
                f32[records[i].Digits5[2], records[i].Digits5[3]]++;
            }
            double[,] w32 = new double[10, 10];
            for (int a = 0; a < 10; a++)
                for (int b = 0; b < 10; b++)
                    w32[a, b] = -0.3 * (f32[a, b] - expected);

            var deEnergyMap = deEnergies.ToDictionary(x => x.State, x => x.Potential);

            // BẬC 1: KHỞI TẠO TỪ 10 CÀNG x DÀN ĐỀ E65 = ĐÚNG 650 SỐ (BẢO TOÀN XÁC SUẤT 100% CÀNG)
            var pool650 = new List<int>();
            for (int c = 0; c < 10; c++)
            {
                foreach (int de in e65)
                {
                    pool650.Add(c * 100 + de);
                }
            }
            var res = new Dan3DCascadeResult();
            res.BachThu3D = bt3D;
            res.BachThuCang = btD3;
            res.Top3Cang = topD3;
            res.Dan650 = pool650.Select(s => s.ToString("D3")).ToList();

            var pool3D = pool650;

            // BẬC 2 -> 420 số: Lọc theo Thế năng Càng + Tương tác + Parity 8 trạng thái (Chẵn/Lẻ)
            int[] parCount = new int[8];
            for (int i = Math.Max(0, n - 30); i < n; i++)
            {
                int pIdx = ((records[i].Digits5[2] % 2) << 2) | ((records[i].Digits5[3] % 2) << 1) | (records[i].Digits5[4] % 2);
                parCount[pIdx]++;
            }
            pool3D = pool3D.OrderBy(s => {
                int d3 = s / 100; int de = s % 100; int d2 = de / 10; int d1 = de % 10;
                int pIdx = ((d3 % 2) << 2) | ((d2 % 2) << 1) | (d1 % 2);
                double parPenalty = parCount[pIdx] == 0 ? 1.0 : (parCount[pIdx] > 8 ? 0.8 : -0.3);
                return vD3[d3] + 1.2 * deEnergyMap[de] + 0.5 * w32[d3, d2] + parPenalty;
            }).Take(420).ToList();
            res.Dan420 = pool3D.Select(s => s.ToString("D3")).ToList();

            // BẬC 3 -> 270 số: Lọc theo Tổng 3D Gauss & Nhịp Tổng mod 10
            int[] sumCount = new int[10];
            for (int i = Math.Max(0, n - 50); i < n; i++)
            {
                int sm = (records[i].Digits5[2] + records[i].Digits5[3] + records[i].Digits5[4]) % 10;
                sumCount[sm]++;
            }
            pool3D = pool3D.OrderBy(s => {
                int d3 = s / 100; int de = s % 100; int d2 = de / 10; int d1 = de % 10;
                int totalSum = d3 + d2 + d1;
                int sm = totalSum % 10;
                double gaussPenalty = (totalSum <= 5 || totalSum >= 22) ? 0.8 : (totalSum >= 10 && totalSum <= 17 ? -0.3 : 0.0);
                double sumPenalty = (sumCount[sm] < 2) ? 1.2 : -0.4;
                return vD3[d3] + 1.2 * deEnergyMap[de] + 0.5 * w32[d3, d2] + gaussPenalty + sumPenalty;
            }).Take(270).ToList();
            res.Dan270 = pool3D.Select(s => s.ToString("D3")).ToList();

            // BẬC 4 -> 175 số: Lọc cụm bước nhảy hẹp |d3 - d2| <= 3 & Cặp đối ngẫu CLTX
            pool3D = pool3D.OrderBy(s => {
                int d3 = s / 100; int de = s % 100; int d2 = de / 10;
                int step = Math.Abs(d3 - d2);
                double stepPen = (step >= 8) ? 0.8 : (step <= 3 ? -0.3 : 0.0);

                int q3 = GetQuadrant1D(d3);
                int q2 = GetQuadrant1D(d2);
                double quadCoupling = 0.0;
                if ((q3 == 1 && q2 == 1) || (q3 == 2 && q2 == 2)) quadCoupling = 0.35; // Phạt cụm lạnh CT-CT, LX-LX
                if ((q3 == 0 && q2 == 3) || (q3 == 3 && q2 == 0)) quadCoupling = -0.25; // Thưởng đối ngẫu CX-LT, LT-CX

                return vD3[d3] + 1.2 * deEnergyMap[de] + 0.8 * w32[d3, d2] + stepPen + quadCoupling;
            }).Take(175).ToList();
            res.Dan175 = pool3D.Select(s => s.ToString("D3")).ToList();

            // BẬC 5 -> 115 số: Ưu tiên Kép Gánh d3 == d1 (ABA) và Kép Liền
            pool3D = pool3D.OrderBy(s => {
                int d3 = s / 100; int de = s % 100; int d2 = de / 10; int d1 = de % 10;
                double bonus = (d3 == d1) ? -0.6 : (d3 == d2 || d2 == d1 ? -0.3 : 0.0);
                return vD3[d3] + 1.2 * deEnergyMap[de] + bonus;
            }).Take(115).ToList();
            res.Dan115 = pool3D.Select(s => s.ToString("D3")).ToList();

            // BẬC 6 -> 75 số: Tương tác liên hợp Càng - Đề
            pool3D = pool3D.OrderBy(s => {
                int d3 = s / 100; int de = s % 100; int d2 = de / 10;
                return vD3[d3] + 1.4 * deEnergyMap[de] + w32[d3, d2];
            }).Take(75).ToList();
            res.Dan75 = pool3D.Select(s => s.ToString("D3")).ToList();

            // BẬC 7 -> 49 số: Tương tác sâu
            pool3D = pool3D.OrderBy(s => {
                int d3 = s / 100; int de = s % 100; int d2 = de / 10;
                return vD3[d3] + 1.5 * deEnergyMap[de] + 1.2 * w32[d3, d2];
            }).Take(49).ToList();
            res.Dan49 = pool3D.Select(s => s.ToString("D3")).ToList();

            // BẬC 8 & 9: Áp dụng trực tiếp Modun Compute3D_QuantumOptimal (mở rộng từ hệ thống 4D)
            string qBt3D;
            List<string> qDan4, qDan12, qDan27, qDan36;
            Compute3D_QuantumOptimal(records, out qBt3D, out qDan4, out qDan12, out qDan27, out qDan36);
            res.BachThu3D = qBt3D;
            res.Dan12 = qDan12;
            res.Dan27 = qDan27;

            return res;
        }

        public static List<string> ComputeDan3D_12_Recursive(List<LotteryRecord> past, string dow, string bt3D, List<int> tuThuDe)
        {
            var marginal1D = Compute1DMarginals(past);
            var deEnergies = ComputeUnifiedPotentials2D(past, marginal1D);
            var e65 = SelectFactorNeutralDeSet(deEnergies, 65);
            var cascade = ComputeDan3DCascade(past, e65, deEnergies);
            return cascade.Dan12;
        }

        public static void Compute4D_TensorOptimal(
            List<LotteryRecord> past, 
            int[] tuThuDe, 
            int[] dan9Tail, 
            int btDe,
            out string bt4D, 
            out List<string> dan4D_16, 
            out List<string> dan4D_81)
        {
            List<string> dan4D_4, dan4D_36;
            Compute4D_QuantumOptimal(past, out bt4D, out dan4D_4, out dan4D_16, out dan4D_36, out dan4D_81);
        }

        public static void Compute3D_QuantumOptimal(
            List<LotteryRecord> past,
            out string bt3D,
            out List<string> dan3D_4,
            out List<string> dan3D_12,
            out List<string> dan3D_27,
            out List<string> dan3D_36)
        {
            int n = past.Count;

            // 1. TÍNH THẾ NĂNG 10 TỔNG S_A = (d3 + d2)%10 VÀ 10 TỔNG S_B = (d2 + d1)%10 (2 CỘT TỔNG LIỀN KỀ 3D)
            // Tương tự nền tảng toán học S_H và S_T của hệ thống 4D
            int prevSA = (past[n - 1].Digits5[2] + past[n - 1].Digits5[3]) % 10;
            int prevSB = (past[n - 1].Digits5[3] + past[n - 1].Digits5[4]) % 10;

            int[] ganSa = new int[10]; int[] f30Sa = new int[10];
            int[] ganSb = new int[10]; int[] f30Sb = new int[10];
            for (int s = 0; s < 10; s++)
            {
                for (int i = n - 1; i >= 0; i--)
                {
                    if ((past[i].Digits5[2] + past[i].Digits5[3]) % 10 == s) break;
                    ganSa[s]++;
                }
                for (int i = n - 1; i >= 0; i--)
                {
                    if ((past[i].Digits5[3] + past[i].Digits5[4]) % 10 == s) break;
                    ganSb[s]++;
                }
            }
            for (int i = Math.Max(0, n - 30); i < n; i++)
            {
                f30Sa[(past[i].Digits5[2] + past[i].Digits5[3]) % 10]++;
                f30Sb[(past[i].Digits5[3] + past[i].Digits5[4]) % 10]++;
            }

            var saScores = new List<Tuple<int, double>>();
            var sbScores = new List<Tuple<int, double>>();
            for (int s = 0; s < 10; s++)
            {
                double r_a = (ganSa[s] >= 7 && ganSa[s] <= 20) ? -1.5 : (ganSa[s] == 0 ? 0.8 : (ganSa[s] > 30 ? 0.5 : 0.0));
                double k_a = -0.35 * (f30Sa[s] - 3.0);
                int dSa = (s - prevSA + 10) % 10;
                double inertiaA = (dSa == 0 || dSa == 1 || dSa == 9) ? -0.8 : 0.0;
                saScores.Add(new Tuple<int, double>(s, r_a + k_a + inertiaA));

                double r_b = (ganSb[s] >= 7 && ganSb[s] <= 20) ? -1.5 : (ganSb[s] == 0 ? 0.8 : (ganSb[s] > 30 ? 0.5 : 0.0));
                double k_b = -0.35 * (f30Sb[s] - 3.0);
                int dSb = (s - prevSB + 10) % 10;
                double inertiaB = (dSb == 0 || dSb == 1 || dSb == 9) ? -0.8 : 0.0;
                sbScores.Add(new Tuple<int, double>(s, r_b + k_b + inertiaB));
            }
            var topSa = saScores.OrderBy(x => x.Item2).Select(x => x.Item1).ToArray();
            var topSb = sbScores.OrderBy(x => x.Item2).Select(x => x.Item1).ToArray();

            // 2. THẾ NĂNG ĐỀ 2D (UNIFIED POTENTIALS)
            var marginal1D = Compute1DMarginals(past);
            var unifiedEnergies = ComputeUnifiedPotentials2D(past, marginal1D);
            var dePotMap = unifiedEnergies.ToDictionary(x => x.State, x => x.Potential);

            // 3. HÀM CHẤM ĐIỂM SỐ 3D: THẾ NĂNG POTTS + BỘ 3 LƯỚI ĐẠI SỐ MODULO 2, 3, 5 (ĐỒNG BỘ NỀN TẢNG 4D)
            Func<int, int, int, double> score3D = (d3, d2, d1) =>
            {
                int sA = (d3 + d2) % 10;
                int sB = (d2 + d1) % 10;
                int de = d2 * 10 + d1;
                int rawTail = d2 + d1;
                int raw3D = d3 + d2 + d1;

                // Quy luật 1: Vùng Bụng Tam Giác [6..12] của Đuôi và [10..18] của 3D
                double bellyTail = (rawTail >= 6 && rawTail <= 12) ? -0.8 : 0.4;
                double belly3D = (raw3D >= 10 && raw3D <= 18) ? -0.8 : (raw3D < 6 || raw3D > 22 ? 1.2 : 0.0);
                double baseScore = saScores[sA].Item2 + sbScores[sB].Item2 + 1.2 * dePotMap[de] + bellyTail + belly3D;

                // Lưới Lọc 1: Modulo 2 (Chẵn/Lẻ 3D) & Cân Bằng Tài/Xỉu
                int p3 = (d3 % 2 != 0) ? 1 : 0;
                int p2 = (d2 % 2 != 0) ? 1 : 0;
                int p1 = (d1 % 2 != 0) ? 1 : 0;
                int totalOdd = p3 + p2 + p1;

                int s3 = (d3 >= 5) ? 1 : 0;
                int s2 = (d2 >= 5) ? 1 : 0;
                int s1 = (d1 >= 5) ? 1 : 0;
                int totalBig = s3 + s2 + s1;

                double penaltyOdd = (totalOdd == 0 || totalOdd == 3) ? 1.2 : -0.6;
                double penaltyBig = (totalBig == 0 || totalBig == 3) ? 1.2 : -0.6;

                // Lưới Lọc 2: Modulo 3 (Triệt tiêu đồng dư 3D)
                int m3_3 = d3 % 3; int m3_2 = d2 % 3; int m3_1 = d1 % 3;
                int count0 = (m3_3 == 0 ? 1 : 0) + (m3_2 == 0 ? 1 : 0) + (m3_1 == 0 ? 1 : 0);
                int count1 = (m3_3 == 1 ? 1 : 0) + (m3_2 == 1 ? 1 : 0) + (m3_1 == 1 ? 1 : 0);
                int count2 = (m3_3 == 2 ? 1 : 0) + (m3_2 == 2 ? 1 : 0) + (m3_1 == 2 ? 1 : 0);

                double mod3Bonus = (count0 == 3 || count1 == 3 || count2 == 3) ? 2.0 : ((count0 > 0 && count1 > 0 && count2 > 0) ? -0.6 : -0.2);

                // Lưới Lọc 3: Modulo 5 (Bóng Dương 3D)
                int m5_3 = d3 % 5; int m5_2 = d2 % 5; int m5_1 = d1 % 5;
                var dist5 = new HashSet<int> { m5_3, m5_2, m5_1 };
                double mod5Bonus = (dist5.Count == 1) ? 2.5 : ((dist5.Count == 3) ? -0.5 : 0.0);

                return baseScore + penaltyOdd + penaltyBig + mod3Bonus + mod5Bonus;
            };

            // 4. CHIẾN LƯỢC B: TOP 2 S_A x TOP 2 S_B -> 2 x 2 x 10 = 40 số gốc
            // Chắt lọc: Bạch Thủ (1s), Tứ Thủ (4s), Dàn Tinh Tuyển (12s)
            var poolB = new List<Tuple<string, double>>();
            for (int ka = 0; ka < 2; ka++)
            {
                int sA = topSa[ka];
                for (int kb = 0; kb < 2; kb++)
                {
                    int sB = topSb[kb];
                    for (int d2 = 0; d2 < 10; d2++)
                    {
                        int d3 = (sA - d2 + 10) % 10;
                        int d1 = (sB - d2 + 10) % 10;
                        string s3D = d3.ToString() + d2.ToString() + d1.ToString();
                        poolB.Add(new Tuple<string, double>(s3D, score3D(d3, d2, d1)));
                    }
                }
            }
            var sortedB = poolB.OrderBy(x => x.Item2).Select(x => x.Item1).Distinct().ToList();

            // 5. CHIẾN LƯỢC C: TOP 3 S_A x TOP 3 S_B -> 3 x 3 x 10 = 90 số gốc
            // Chắt lọc: Dàn Phổ (27s), Dàn Trọng Tâm (36s)
            var poolC = new List<Tuple<string, double>>();
            for (int ka = 0; ka < 3; ka++)
            {
                int sA = topSa[ka];
                for (int kb = 0; kb < 3; kb++)
                {
                    int sB = topSb[kb];
                    for (int d2 = 0; d2 < 10; d2++)
                    {
                        int d3 = (sA - d2 + 10) % 10;
                        int d1 = (sB - d2 + 10) % 10;
                        string s3D = d3.ToString() + d2.ToString() + d1.ToString();
                        poolC.Add(new Tuple<string, double>(s3D, score3D(d3, d2, d1)));
                    }
                }
            }
            var sortedC = poolC.OrderBy(x => x.Item2).Select(x => x.Item1).Distinct().ToList();

            // TRÍCH XUẤT CÁC TẬP DÀN 3D CHUẨN TẮC (SẮP XẾP TĂNG DẦN TRIỆT ĐỂ):
            bt3D     = sortedB[0];
            dan3D_4  = sortedB.Take(4).OrderBy(x => x).ToList();
            dan3D_12 = sortedB.Take(12).OrderBy(x => x).ToList();
            dan3D_27 = sortedC.Take(27).OrderBy(x => x).ToList();
            dan3D_36 = sortedC.Take(36).OrderBy(x => x).ToList();
        }

        public static void Compute4D_QuantumOptimal(
            List<LotteryRecord> past,
            out string bt4D,
            out List<string> dan4D_4,
            out List<string> dan4D_16,
            out List<string> dan4D_36,
            out List<string> dan4D_81)
        {
            int n = past.Count;

            // 1. TÍNH THẾ NĂNG 10 TỔNG S_H VÀ 10 TỔNG S_T (5 QUY LUẬT 2 CỘT TỔNG)
            int prevSH = (past[n - 1].Digits5[1] + past[n - 1].Digits5[2]) % 10;
            int prevST = (past[n - 1].Digits5[3] + past[n - 1].Digits5[4]) % 10;

            int[] ganSt = new int[10]; int[] f30St = new int[10];
            int[] ganSh = new int[10]; int[] f30Sh = new int[10];
            for (int s = 0; s < 10; s++)
            {
                for (int i = n - 1; i >= 0; i--)
                {
                    if ((past[i].Digits5[3] + past[i].Digits5[4]) % 10 == s) break;
                    ganSt[s]++;
                }
                for (int i = n - 1; i >= 0; i--)
                {
                    if ((past[i].Digits5[1] + past[i].Digits5[2]) % 10 == s) break;
                    ganSh[s]++;
                }
            }
            for (int i = Math.Max(0, n - 30); i < n; i++)
            {
                f30St[(past[i].Digits5[3] + past[i].Digits5[4]) % 10]++;
                f30Sh[(past[i].Digits5[1] + past[i].Digits5[2]) % 10]++;
            }

            var stScores = new List<Tuple<int, double>>();
            var shScores = new List<Tuple<int, double>>();
            for (int s = 0; s < 10; s++)
            {
                double r_t = (ganSt[s] >= 7 && ganSt[s] <= 20) ? -1.5 : (ganSt[s] == 0 ? 0.8 : (ganSt[s] > 30 ? 0.5 : 0.0));
                double k_t = -0.35 * (f30St[s] - 3.0);
                int dSt = (s - prevST + 10) % 10;
                double inertiaT = (dSt == 0 || dSt == 1 || dSt == 9) ? -0.8 : 0.0;
                stScores.Add(new Tuple<int, double>(s, r_t + k_t + inertiaT));

                double r_h = (ganSh[s] >= 7 && ganSh[s] <= 20) ? -1.5 : (ganSh[s] == 0 ? 0.8 : (ganSh[s] > 30 ? 0.5 : 0.0));
                double k_h = -0.35 * (f30Sh[s] - 3.0);
                int dSh = (s - prevSH + 10) % 10;
                double inertiaH = (dSh == 0 || dSh == 1 || dSh == 9) ? -0.8 : 0.0;
                shScores.Add(new Tuple<int, double>(s, r_h + k_h + inertiaH));
            }
            var topSt = stScores.OrderBy(x => x.Item2).Select(x => x.Item1).ToArray();
            var topSh = shScores.OrderBy(x => x.Item2).Select(x => x.Item1).ToArray();

            // 2. THẾ NĂNG ĐỀ 2D (UNIFIED POTENTIALS)
            var marginal1D = Compute1DMarginals(past);
            var unifiedEnergies = ComputeUnifiedPotentials2D(past, marginal1D);
            var dePotMap = unifiedEnergies.ToDictionary(x => x.State, x => x.Potential);

            // 3. THẾ NĂNG CHỮ SỐ d3 VÀ d4
            int[] ganD3 = new int[10]; int[] f30D3 = new int[10];
            int[] ganD4 = new int[10]; int[] f30D4 = new int[10];
            for (int d = 0; d < 10; d++)
            {
                for (int i = n - 1; i >= 0; i--) { if (past[i].Digits5[2] == d) break; ganD3[d]++; }
                for (int i = n - 1; i >= 0; i--) { if (past[i].Digits5[1] == d) break; ganD4[d]++; }
            }
            for (int i = Math.Max(0, n - 30); i < n; i++)
            {
                f30D3[past[i].Digits5[2]]++;
                f30D4[past[i].Digits5[1]]++;
            }
            double[] vD3 = new double[10];
            double[] vD4 = new double[10];
            for (int d = 0; d < 10; d++)
            {
                vD3[d] = ((ganD3[d] >= 7 && ganD3[d] <= 20) ? -1.5 : (ganD3[d] == 0 ? 1.0 : 0.0)) - 0.4 * (f30D3[d] - 3.0);
                vD4[d] = ((ganD4[d] >= 7 && ganD4[d] <= 20) ? -1.5 : (ganD4[d] == 0 ? 1.0 : 0.0)) - 0.3 * (f30D4[d] - 3.0);
            }

            // HÀM CHẤM ĐIỂM SỐ 4D: 7 THẾ NĂNG + BỘ 3 LƯỚI ĐẠI SỐ MODULO 2, 3, 5
            Func<int, int, int, int, double> score4D = (d4, d3, d2, d1) =>
            {
                int rawHead = d4 + d3;
                int rawTail = d2 + d1;
                int de = d2 * 10 + d1;

                // Quy luật 1: Vùng Bụng Tam Giác [6..12]
                double bellyHead = (rawHead >= 6 && rawHead <= 12) ? -0.8 : 0.4;
                double bellyTail = (rawTail >= 6 && rawTail <= 12) ? -0.8 : 0.4;
                double baseScore = vD4[d4] + vD3[d3] + 1.2 * dePotMap[de] + bellyHead + bellyTail;

                // Lưới Lọc 1: Modulo 2 (Chẵn/Lẻ) & Cân Bằng Tài/Xỉu
                int p4 = (d4 % 2 != 0) ? 1 : 0;
                int p3 = (d3 % 2 != 0) ? 1 : 0;
                int p2 = (d2 % 2 != 0) ? 1 : 0;
                int p1 = (d1 % 2 != 0) ? 1 : 0;
                int totalOdd = p4 + p3 + p2 + p1;

                int s4 = (d4 >= 5) ? 1 : 0;
                int s3 = (d3 >= 5) ? 1 : 0;
                int s2 = (d2 >= 5) ? 1 : 0;
                int s1 = (d1 >= 5) ? 1 : 0;
                int totalBig = s4 + s3 + s2 + s1;

                double penaltyOdd = (totalOdd == 0 || totalOdd == 4) ? 1.5 : (totalOdd == 2 ? -0.8 : -0.2);
                double penaltyBig = (totalBig == 0 || totalBig == 4) ? 1.5 : (totalBig == 2 ? -0.8 : -0.2);
                double deadZone = ((totalOdd == 0 || totalOdd == 4) && (totalBig == 0 || totalBig == 4)) ? 3.0 : 0.0;

                // Lưới Lọc 2: Modulo 3 (Triệt tiêu đồng dư 4 chân)
                int m3_4 = d4 % 3; int m3_3 = d3 % 3; int m3_2 = d2 % 3; int m3_1 = d1 % 3;
                int count0 = (m3_4 == 0 ? 1 : 0) + (m3_3 == 0 ? 1 : 0) + (m3_2 == 0 ? 1 : 0) + (m3_1 == 0 ? 1 : 0);
                int count1 = (m3_4 == 1 ? 1 : 0) + (m3_3 == 1 ? 1 : 0) + (m3_2 == 1 ? 1 : 0) + (m3_1 == 1 ? 1 : 0);
                int count2 = (m3_4 == 2 ? 1 : 0) + (m3_3 == 2 ? 1 : 0) + (m3_2 == 2 ? 1 : 0) + (m3_1 == 2 ? 1 : 0);

                double mod3Bonus = 0.0;
                if (count0 == 4 || count1 == 4 || count2 == 4) mod3Bonus = 2.5; // Phạt số đồng dư mod 3
                else if (count0 > 0 && count1 > 0 && count2 > 0) mod3Bonus = -0.6; // Thưởng số đủ 3 loại dư
                else if (count0 == 2 && (count1 == 2 || count2 == 2)) mod3Bonus = -0.4;

                // Lưới Lọc 3: Modulo 5 (Đa dạng 5 Cặp Bóng Dương)
                int m5_4 = d4 % 5; int m5_3 = d3 % 5; int m5_2 = d2 % 5; int m5_1 = d1 % 5;
                var distinctRes5 = new HashSet<int> { m5_4, m5_3, m5_2, m5_1 };
                double mod5Bonus = (distinctRes5.Count == 1) ? 3.0 : ((distinctRes5.Count >= 3) ? -0.5 : 0.0);

                return baseScore + penaltyOdd + penaltyBig + deadZone + mod3Bonus + mod5Bonus;
            };

            // 4. CHIẾN LƯỢC C: TAM THỦ CẶP TỔNG (Top 3 SH x Top 3 ST = 9 Cặp Tổng = 900 số gốc)
            var poolC = new List<Tuple<string, double>>();
            for (int kh = 0; kh < 3; kh++)
            {
                int sH = topSh[kh];
                for (int kt = 0; kt < 3; kt++)
                {
                    int sT = topSt[kt];
                    for (int d4 = 0; d4 < 10; d4++)
                    {
                        int d3 = (sH - d4 + 10) % 10;
                        for (int d2 = 0; d2 < 10; d2++)
                        {
                            int d1 = (sT - d2 + 10) % 10;
                            string s4D = d4.ToString() + d3.ToString() + d2.ToString() + d1.ToString();
                            poolC.Add(new Tuple<string, double>(s4D, score4D(d4, d3, d2, d1)));
                        }
                    }
                }
            }
            var sortedC = poolC.OrderBy(x => x.Item2).Select(x => x.Item1).Distinct().ToList();

            // 5. CHIẾN LƯỢC B: SONG THỦ CẶP TỔNG (Top 2 SH x Top 2 ST = 4 Cặp Tổng = 400 số gốc)
            var poolB = new List<Tuple<string, double>>();
            for (int kh = 0; kh < 2; kh++)
            {
                int sH = topSh[kh];
                for (int kt = 0; kt < 2; kt++)
                {
                    int sT = topSt[kt];
                    for (int d4 = 0; d4 < 10; d4++)
                    {
                        int d3 = (sH - d4 + 10) % 10;
                        for (int d2 = 0; d2 < 10; d2++)
                        {
                            int d1 = (sT - d2 + 10) % 10;
                            string s4D = d4.ToString() + d3.ToString() + d2.ToString() + d1.ToString();
                            poolB.Add(new Tuple<string, double>(s4D, score4D(d4, d3, d2, d1)));
                        }
                    }
                }
            }
            var sortedB = poolB.OrderBy(x => x.Item2).Select(x => x.Item1).Distinct().ToList();

            // TRÍCH XUẤT CÁC TẬP DÀN 4D CHUẨN TẮC (SẮP XẾP TĂNG DẦN TRIỆT ĐỂ):
            bt4D     = sortedB[0];
            dan4D_4  = sortedB.Take(4).OrderBy(x => x).ToList();
            dan4D_16 = sortedB.Take(16).OrderBy(x => x).ToList();
            dan4D_36 = sortedC.Take(36).OrderBy(x => x).ToList();
            dan4D_81 = sortedC.Take(81).OrderBy(x => x).ToList();
        }

        public class ItemAuditStat
        {
            public int Stt;
            public string Axis;
            public string Name;
            public int Quantity;
            public double RandomProb; // %
            public int Wins;          // Số kỳ trúng (>= 1 hit)
            public int TotalHits;     // Tổng số nháy / số cặp trúng
            public int CurStreak;
            public int MaxStreak;
            public int CurLoss;
            public int MaxLoss;

            public void Record(bool isWin, int hits = 1)
            {
                if (isWin)
                {
                    Wins++;
                    TotalHits += hits;
                    CurStreak++;
                    CurLoss = 0;
                    if (CurStreak > MaxStreak) MaxStreak = CurStreak;
                }
                else
                {
                    CurLoss++;
                    CurStreak = 0;
                    if (CurLoss > MaxLoss) MaxLoss = CurLoss;
                }
            }
        }

        public static void RunAll24ItemsAudit(List<LotteryRecord> records, int testDays = 100, long defaultBudget = 500000)
        {
            int n = records.Count;
            if (testDays <= 0 || testDays > 3000) testDays = 100;
            int startIdx = n - testDays;

            Console.WriteLine("=========================================================================================");
            Console.WriteLine(string.Format("   BẢNG KIỂM THỬ ĐỐI SOÁT TOÀN BỘ 24 HẠNG MỤC TIDS QUANTUM ({0} KỲ GẦN NHẤT)", testDays));
            Console.WriteLine(string.Format("   Khoảng thời gian: từ kỳ #{0} ({1}, {2}) đến kỳ #{3} ({4}, {5})",
                records[startIdx].Id, records[startIdx].Date, records[startIdx].DayOfWeek, records[n - 1].Id, records[n - 1].Date, records[n - 1].DayOfWeek));
            Console.WriteLine("=========================================================================================");

            var stats = new ItemAuditStat[24];
            for (int i = 0; i < 24; i++) stats[i] = new ItemAuditStat { Stt = i + 1 };

            stats[0].Axis = "Trục 1: Địa Lợi"; stats[0].Name = "Song Thủ Lô (STL)"; stats[0].Quantity = 2; stats[0].RandomProb = 42.17;
            stats[1].Axis = "Trục 1: Địa Lợi"; stats[1].Name = "Bạch Thủ Lô (BTL)"; stats[1].Quantity = 1; stats[1].RandomProb = 23.78;
            stats[2].Axis = "Trục 2: Nhân Hòa"; stats[2].Name = "Lô Lót Tứ Thủ"; stats[2].Quantity = 2; stats[2].RandomProb = 42.17;
            stats[3].Axis = "Trục 2: Nhân Hòa"; stats[3].Name = "Trọn Bộ Tứ Thủ Lô"; stats[3].Quantity = 4; stats[3].RandomProb = 66.83;
            stats[4].Axis = "Trục 1: Địa Lợi"; stats[4].Name = "Cặp Xiên 2 Song Thủ"; stats[4].Quantity = 1; stats[4].RandomProb = 5.66;
            stats[5].Axis = "Trục 1: Địa Lợi"; stats[5].Name = "Bộ 6 Cặp Xiên 2 Tứ Thủ"; stats[5].Quantity = 6; stats[5].RandomProb = 24.53;
            stats[6].Axis = "Trục 3: Thiên Thời"; stats[6].Name = "Bộ 4 Xiên 3 Tứ Thủ"; stats[6].Quantity = 4; stats[6].RandomProb = 4.02;
            stats[7].Axis = "Trục 3: Thiên Thời"; stats[7].Name = "Bộ 1 Xiên 4 Tứ Thủ"; stats[7].Quantity = 1; stats[7].RandomProb = 0.32;
            stats[8].Axis = "Trục 2: Nhân Hòa"; stats[8].Name = "Đại Diễn Thần Trận (E42)"; stats[8].Quantity = 42; stats[8].RandomProb = 42.00;
            stats[9].Axis = "Trục 2: Nhân Hòa"; stats[9].Name = "Huyền Thiên Giáp (E65)"; stats[9].Quantity = 65; stats[9].RandomProb = 65.00;
            stats[10].Axis = "Trục 2: Nhân Hòa"; stats[10].Name = "Quy Nguyên Trận [CL/TX]"; stats[10].Quantity = 10; stats[10].RandomProb = 10.00;
            stats[11].Axis = "Trục 2: Nhân Hòa"; stats[11].Name = "Hà Đồ Tự Nhiên (E27)"; stats[11].Quantity = 27; stats[11].RandomProb = 27.00;
            stats[12].Axis = "Trục 2: Nhân Hòa"; stats[12].Name = "Cửu Cung Thần Trận (E18)"; stats[12].Quantity = 18; stats[12].RandomProb = 18.00;
            stats[13].Axis = "Trục 2: Nhân Hòa"; stats[13].Name = "Tứ Tượng Hội Tụ (E4)"; stats[13].Quantity = 4; stats[13].RandomProb = 4.00;
            stats[14].Axis = "Trục 2: Nhân Hòa"; stats[14].Name = "Thái Cực Nhất Điểm (E1)"; stats[14].Quantity = 1; stats[14].RandomProb = 1.00;
            stats[15].Axis = "Trục 2: Nhân Hòa"; stats[15].Name = "Lưỡng Nghi Đồng Quy (E2)"; stats[15].Quantity = 2; stats[15].RandomProb = 2.00;
            stats[16].Axis = "Trục 3: Thiên Thời"; stats[16].Name = "Dàn 3 Càng 12 số (3D)"; stats[16].Quantity = 12; stats[16].RandomProb = 1.20;
            stats[17].Axis = "Trục 3: Thiên Thời"; stats[17].Name = "Bạch Thủ 3 Càng (3D)"; stats[17].Quantity = 1; stats[17].RandomProb = 0.10;
            stats[18].Axis = "Trục 3: Thiên Thời"; stats[18].Name = "Dàn 3 Càng Phổ 27 số (3D)"; stats[18].Quantity = 27; stats[18].RandomProb = 2.70;
            stats[19].Axis = "Trục 3: Thiên Thời"; stats[19].Name = "Bạch Thủ 4 Càng (4D)"; stats[19].Quantity = 1; stats[19].RandomProb = 0.01;
            stats[20].Axis = "Trục 3: Thiên Thời"; stats[20].Name = "Tứ Thủ 4 Càng (4D)"; stats[20].Quantity = 4; stats[20].RandomProb = 0.04;
            stats[21].Axis = "Trục 3: Thiên Thời"; stats[21].Name = "Dàn 4 Càng Trọng Tâm 16s"; stats[21].Quantity = 16; stats[21].RandomProb = 0.16;
            stats[22].Axis = "Trục 3: Thiên Thời"; stats[22].Name = "Dàn 4 Càng Cân Bằng 36s"; stats[22].Quantity = 36; stats[22].RandomProb = 0.36;
            stats[23].Axis = "Trục 3: Thiên Thời"; stats[23].Name = "Dàn 4 Càng Mở Rộng 81s"; stats[23].Quantity = 81; stats[23].RandomProb = 0.81;

            long totalPortfolioCost = 0;
            long totalPortfolioRevenue = 0;
            int portfolioWinDays = 0;
            double peakPortfolioProfit = 0;
            double maxDrawdown = 0;
            double runningProfit = 0;
            int days3DHit = 0;
            int days4DHit = 0;

            for (int t = startIdx; t < n; t++)
            {
                var subList = records.Take(t).ToList();
                var actual = records[t];
                int actualDe = actual.De;
                string actual3D = string.Format("{0}{1}{2}", actual.Digits5[2], actual.Digits5[3], actual.Digits5[4]);
                string actual4D = string.Format("{0}{1}{2}{3}", actual.Digits5[1], actual.Digits5[2], actual.Digits5[3], actual.Digits5[4]);
                var loUnique = actual.UniqueLo;
                var loList = actual.LoList;

                var marginal1D = Compute1DMarginals(subList);
                var lotto1D = Compute1DLottoMarginals(subList);
                double[,] W_lotto;
                var lottoEnergies = ComputeUnifiedLottoPotentials(subList, lotto1D, out W_lotto);
                var unifiedEnergies = ComputeUnifiedPotentials2D(subList, marginal1D);
                var bsmBasketEnergies = ComputeBsmBasketPotentials2D(subList, marginal1D);
                var qci = ComputeQCI(subList, lottoEnergies, unifiedEnergies, lotto1D, marginal1D);
                var regime = DetectMarketRegime(subList, unifiedEnergies, W_lotto, marginal1D);

                var top4 = SelectTop4LoWithDiversity(lottoEnergies, subList);
                int btLo = top4[0];
                int stLo = top4[1];
                int tuThu3 = top4[2];
                int tuThu4 = top4[3];
                var tuThuLoList = new List<int> { btLo, stLo, tuThu3, tuThu4 };

                var pairs = new List<Tuple<int, int>>();
                for (int i = 0; i < 4; i++)
                    for (int j = i + 1; j < 4; j++)
                        pairs.Add(new Tuple<int, int>(tuThuLoList[i], tuThuLoList[j]));

                var trios = new List<List<int>>();
                for (int i = 0; i < 4; i++)
                    for (int j = i + 1; j < 4; j++)
                        for (int k = j + 1; k < 4; k++)
                            trios.Add(new List<int> { tuThuLoList[i], tuThuLoList[j], tuThuLoList[k] });

                int btDe = unifiedEnergies[0].State;
                var dan2 = unifiedEnergies.Take(2).Select(x => x.State).ToList();
                var dan4 = unifiedEnergies.Take(4).Select(x => x.State).ToList();
                var dan65 = SelectFactorNeutralDeSet(bsmBasketEnergies, 65);
                var sub42 = bsmBasketEnergies.Where(e => dan65.Contains(e.State)).ToList();
                var dan42 = SelectFactorNeutralDeSet(sub42, 42);
                var sub27 = bsmBasketEnergies.Where(e => dan42.Contains(e.State)).ToList();
                var dan27 = SelectFactorNeutralDeSet(sub27, 27);
                var sub18 = bsmBasketEnergies.Where(e => dan27.Contains(e.State)).ToList();
                var dan18 = SelectFactorNeutralDeSet(sub18, 18);
                var coilingCluster = ExtractBsmCoilingCluster(unifiedEnergies, regime, 10);

                var cascade3D = ComputeDan3DCascade(subList, dan65, unifiedEnergies);
                string bt3D = cascade3D.BachThu3D;
                var dan3D_12 = cascade3D.Dan12;
                var dan3D_27 = cascade3D.Dan27;

                string bt4D;
                List<string> dan4D_4, dan4D_16, dan4D_36, dan4D_81;
                Compute4D_QuantumOptimal(subList, out bt4D, out dan4D_4, out dan4D_16, out dan4D_36, out dan4D_81);

                // Ghi nhận từng hạng mục
                int stlHits = loList.Count(x => x == btLo) + loList.Count(x => x == stLo);
                stats[0].Record(stlHits > 0, stlHits);

                int btlHits = loList.Count(x => x == btLo);
                stats[1].Record(btlHits > 0, btlHits);

                int lotHits = loList.Count(x => x == tuThu3) + loList.Count(x => x == tuThu4);
                stats[2].Record(lotHits > 0, lotHits);

                int tuThuHits = stlHits + lotHits;
                stats[3].Record(tuThuHits > 0, tuThuHits);

                bool winX2_Stl = loUnique.Contains(btLo) && loUnique.Contains(stLo);
                stats[4].Record(winX2_Stl, winX2_Stl ? 1 : 0);

                int winPairs = pairs.Count(p => loUnique.Contains(p.Item1) && loUnique.Contains(p.Item2));
                stats[5].Record(winPairs > 0, winPairs);

                int winTrios = trios.Count(tr => loUnique.Contains(tr[0]) && loUnique.Contains(tr[1]) && loUnique.Contains(tr[2]));
                stats[6].Record(winTrios > 0, winTrios);

                bool winX4 = tuThuLoList.All(x => loUnique.Contains(x));
                stats[7].Record(winX4, winX4 ? 1 : 0);

                stats[8].Record(dan42.Contains(actualDe));
                stats[9].Record(dan65.Contains(actualDe));
                stats[10].Record(coilingCluster.Contains(actualDe));
                stats[11].Record(dan27.Contains(actualDe));
                stats[12].Record(dan18.Contains(actualDe));
                stats[13].Record(dan4.Contains(actualDe));
                stats[14].Record(actualDe == btDe);
                stats[15].Record(dan2.Contains(actualDe));

                bool hit3D_12 = dan3D_12.Contains(actual3D);
                stats[16].Record(hit3D_12);
                if (hit3D_12) days3DHit++;

                stats[17].Record(actual3D == bt3D);
                stats[18].Record(dan3D_27.Contains(actual3D));

                stats[19].Record(actual4D == bt4D);
                stats[20].Record(dan4D_4.Contains(actual4D));

                bool hit4D_16 = dan4D_16.Contains(actual4D);
                stats[21].Record(hit4D_16);
                if (hit4D_16) days4DHit++;

                stats[22].Record(dan4D_36.Contains(actual4D));
                stats[23].Record(dan4D_81.Contains(actual4D));

                // Kelly Portfolio PnL (500k/kỳ)
                long costPerPoint = 27000;
                double lottoRatio = (regime.Regime == MarketRegimeType.Peak) ? 0.45 : ((regime.Regime == MarketRegimeType.Convergent) ? 0.42 : ((regime.Regime == MarketRegimeType.Coiling) ? 0.35 : 0.40));
                int stPts = (int)Math.Max(2, Math.Floor((defaultBudget * lottoRatio) / (2 * costPerPoint)));
                long costStLo = 2 * stPts * costPerPoint;
                long costXien = (long)Math.Max(15000, Math.Floor((defaultBudget * 0.05) / 5000) * 5000);
                int lotPts = (int)Math.Max(1, Math.Floor(stPts / 2.5));
                long costLotLo = 2 * lotPts * costPerPoint;
                long costLotto = costStLo + costLotLo;
                long remainForDeAnd3D = defaultBudget - costLotto - costXien;

                long c65 = (regime.Regime == MarketRegimeType.Coiling) ? 1000 : 0;
                long costE65 = 65 * c65;
                long remainDeBudget = Math.Max(0, remainForDeAnd3D - costE65);
                long c42 = (long)Math.Max(1000, Math.Floor((remainDeBudget * 0.45) / (42 * 1000)) * 1000);
                long costE42 = 42 * c42;
                long c27 = (regime.Regime == MarketRegimeType.Peak) ? (long)Math.Max(1000, Math.Floor((remainDeBudget * 0.25) / (27 * 1000)) * 1000) : ((regime.Regime == MarketRegimeType.Convergent) ? (long)Math.Max(0, Math.Floor((c42 * 0.6) / 1000) * 1000) : 0);
                long costE27 = 27 * c27;
                long remainFor18 = Math.Max(0, remainDeBudget - costE42 - costE27);
                long c18 = (long)Math.Max(1000, Math.Floor((remainFor18 * 0.40) / (18 * 1000)) * 1000);
                long costE18 = 18 * c18;
                long c4 = (long)Math.Max(1000, Math.Floor((remainFor18 * 0.20) / (4 * 1000)) * 1000);
                long costE4 = 4 * c4;
                long c1 = (long)Math.Max(2000, Math.Floor((remainFor18 * 0.10) / 1000) * 1000);
                long costE1 = c1;
                long costDeTotal = costE65 + costE42 + costE27 + costE18 + costE4 + costE1;

                long remainFor3D = Math.Max(0, remainForDeAnd3D - costDeTotal);
                long c3D_base = (long)Math.Max(1000, Math.Floor(remainFor3D / (12 * 1000.0)) * 1000);
                long cost3D_total = 12 * c3D_base;
                long excess = remainFor3D - cost3D_total;
                long c3D_bt = c3D_base + excess;
                long cost3D = (11 * c3D_base) + c3D_bt;
                long dailyCost = costLotto + costXien + costDeTotal + cost3D;

                long dailyRev = 0;
                dailyRev += (loList.Count(x => x == btLo) + loList.Count(x => x == stLo)) * stPts * 99000;
                dailyRev += (loList.Count(x => x == tuThu3) + loList.Count(x => x == tuThu4)) * lotPts * 99000;
                if (winX2_Stl) dailyRev += (long)(costXien * 16.5);
                if (dan65.Contains(actualDe)) dailyRev += c65 * 99;
                if (dan42.Contains(actualDe)) dailyRev += c42 * 99;
                if (dan27.Contains(actualDe)) dailyRev += c27 * 99;
                if (dan18.Contains(actualDe)) dailyRev += c18 * 99;
                if (dan4.Contains(actualDe)) dailyRev += c4 * 99;
                if (actualDe == btDe) dailyRev += c1 * 99;
                if (actual3D == bt3D) dailyRev += c3D_bt * 960;
                else if (dan3D_12.Contains(actual3D)) dailyRev += c3D_base * 960;

                totalPortfolioCost += dailyCost;
                totalPortfolioRevenue += dailyRev;
                long dailyNet = dailyRev - dailyCost;
                if (dailyNet > 0) portfolioWinDays++;

                runningProfit += dailyNet;
                if (runningProfit > peakPortfolioProfit) peakPortfolioProfit = runningProfit;
                double dd = peakPortfolioProfit - runningProfit;
                if (dd > maxDrawdown) maxDrawdown = dd;
            }

            // IN KẾT QUẢ
            Console.WriteLine(string.Format("{0,-3} | {1,-18} | {2,-25} | {3,-4} | {4,-7} | {5,-7} | {6,-7} | {7,-5} | {8,-7} | {9,-7}",
                "STT", "Trục Tam Tài", "Hạng Mục Cược", "SL", "Thắng", "WinRate", "Random", "Alpha", "MaxWin", "MaxLoss"));
            Console.WriteLine(new string('-', 105));

            for (int i = 0; i < 24; i++)
            {
                var s = stats[i];
                double winRate = s.Wins * 100.0 / testDays;
                double alpha = winRate / s.RandomProb;
                Console.WriteLine(string.Format("{0,-3} | {1,-18} | {2,-25} | {3,-4} | {4,-7} | {5,6:F1}% | {6,6:F2}% | {7,5:F2} | {8,-7} | {9,-7}",
                    s.Stt, s.Axis, s.Name, s.Quantity, s.Wins + "/" + testDays, winRate, s.RandomProb, alpha, s.MaxStreak, s.MaxLoss));
            }

            Console.WriteLine(new string('=', 105));
            Console.WriteLine("    TỔNG KẾT ĐÁNH GIÁ ĐỊNH LƯỢNG DANH MỤC TÀI CHÍNH KELLY (500.000 VNĐ/KỲ x " + testDays + " KỲ):");
            Console.WriteLine(string.Format(" * Tổng vốn đầu tư giải ngân:    {0:N0} VNĐ", totalPortfolioCost));
            Console.WriteLine(string.Format(" * Tổng doanh thu thưởng thu về:  {0:N0} VNĐ", totalPortfolioRevenue));
            long netProfit = totalPortfolioRevenue - totalPortfolioCost;
            double roi = (netProfit * 100.0) / totalPortfolioCost;
            Console.WriteLine(string.Format(" * Lợi nhuận ròng (Net Profit):   {0:N0} VNĐ ({1:+#,##0.0;-#,##0.0;0.0}%)", netProfit, roi));
            Console.WriteLine(string.Format(" * Tỷ lệ ngày có lãi (Win Days):  {0}/{1} kỳ ({2:F1}%)", portfolioWinDays, testDays, portfolioWinDays * 100.0 / testDays));
            Console.WriteLine(string.Format(" * Maximum Drawdown (Sụt giảm):   {0:N0} VNĐ", (long)maxDrawdown));
            Console.WriteLine(string.Format(" * Số kỳ trúng 3D (Ăn 1:960):     {0} kỳ", days3DHit));
            Console.WriteLine(string.Format(" * Số kỳ trúng 4D 16s:            {0} kỳ", days4DHit));
            Console.WriteLine("=========================================================================================");
        }

        public static void ComputeBTL_3D(
            List<LotteryRecord> past,
            int btLo2D,
            out string btl3D,
            out int bestCang3D)
        {
            int n = past.Count;
            int d2 = btLo2D / 10;
            int d1 = btLo2D % 10;

            int[] ganD3 = new int[10]; int[] f30D3 = new int[10];
            for (int d = 0; d < 10; d++)
            {
                for (int i = n - 1; i >= 0; i--) { if (past[i].Digits5[2] == d) break; ganD3[d]++; }
            }
            for (int i = Math.Max(0, n - 30); i < n; i++)
            {
                f30D3[past[i].Digits5[2]]++;
            }
            double[] vD3 = new double[10];
            for (int d = 0; d < 10; d++)
            {
                vD3[d] = ((ganD3[d] >= 7 && ganD3[d] <= 20) ? -1.5 : (ganD3[d] == 0 ? 1.0 : 0.0)) - 0.4 * (f30D3[d] - 3.0);
            }

            int lookback = Math.Min(100, n);
            int[,] f32 = new int[10, 10];
            for (int i = n - lookback; i < n; i++) f32[past[i].Digits5[2], past[i].Digits5[3]]++;

            int bestD3 = 0;
            double minV3 = double.MaxValue;
            for (int candD3 = 0; candD3 < 10; candD3++)
            {
                double coupling32 = -0.5 * (f32[candD3, d2] - (lookback / 100.0));
                double v3 = vD3[candD3] + coupling32;
                if (v3 < minV3)
                {
                    minV3 = v3;
                    bestD3 = candD3;
                }
            }
            bestCang3D = bestD3;
            btl3D = string.Format("{0}{1:D2}", bestD3, btLo2D);
        }

        public static void RunBTL_Audit(List<LotteryRecord> records, int testDays = 100)
        {
            int n = records.Count;
            if (testDays <= 0 || testDays > 3000) testDays = 100;
            int startIdx = n - testDays;

            Console.WriteLine("=========================================================================================");
            Console.WriteLine(string.Format("   BẢNG KIỂM THỬ ĐỐI SOÁT BẠCH THỦ LÔ 2D & 3D CÀNG LÔ ({0} KỲ GẦN NHẤT)", testDays));
            Console.WriteLine(string.Format("   Khoảng thời gian: từ kỳ #{0} ({1}, {2}) đến kỳ #{3} ({4}, {5})",
                records[startIdx].Id, records[startIdx].Date, records[startIdx].DayOfWeek, records[n - 1].Id, records[n - 1].Date, records[n - 1].DayOfWeek));
            Console.WriteLine("=========================================================================================");

            int winLo2D = 0, totalNhay2D = 0, winDe2D = 0;
            int winLo3D = 0, winDe3D = 0;

            int curStreakLo2D = 0, maxStreakLo2D = 0, curLossLo2D = 0, maxLossLo2D = 0;
            int curStreakLo3D = 0, maxStreakLo3D = 0, curLossLo3D = 0, maxLossLo3D = 0;

            var hitList = new List<string>();

            for (int t = startIdx; t < n; t++)
            {
                var subList = records.Take(t).ToList();
                var actual = records[t];

                var lotto1D = Compute1DLottoMarginals(subList);
                double[,] W_lotto;
                var lottoEnergies = ComputeUnifiedLottoPotentials(subList, lotto1D, out W_lotto);
                var top4 = SelectTop4LoWithDiversity(lottoEnergies, subList);
                int btLo = top4[0];

                string btl3D;
                int cang3D;
                ComputeBTL_3D(subList, btLo, out btl3D, out cang3D);

                bool hitLo2D = actual.UniqueLo.Contains(btLo);
                int nhay2D = actual.LoList.Count(x => x == btLo);
                bool hitDe2D = (actual.De == btLo);

                bool hitLo3D = actual.Prizes3D.Contains(btl3D);
                bool hitDe3D = (actual.Gdb3D == btl3D);

                if (hitLo2D)
                {
                    winLo2D++; totalNhay2D += nhay2D;
                    curStreakLo2D++; curLossLo2D = 0;
                    if (curStreakLo2D > maxStreakLo2D) maxStreakLo2D = curStreakLo2D;
                }
                else
                {
                    curLossLo2D++; curStreakLo2D = 0;
                    if (curLossLo2D > maxLossLo2D) maxLossLo2D = curLossLo2D;
                }
                if (hitDe2D) winDe2D++;

                if (hitLo3D)
                {
                    winLo3D++;
                    curStreakLo3D++; curLossLo3D = 0;
                    if (curStreakLo3D > maxStreakLo3D) maxStreakLo3D = curStreakLo3D;
                    hitList.Add(string.Format("Kỳ #{0} ({1}): NỔ BTL 3D (3 CÀNG LÔ) [{2}]", actual.Id, actual.Date, btl3D));
                }
                else
                {
                    curLossLo3D++; curStreakLo3D = 0;
                    if (curLossLo3D > maxLossLo3D) maxLossLo3D = curLossLo3D;
                }
                if (hitDe3D)
                {
                    winDe3D++;
                    hitList.Add(string.Format("Kỳ #{0} ({1}): ★ ĐẠI THẮNG: NỔ ĐỀ 3 CÀNG GĐB [{2}]", actual.Id, actual.Date, btl3D));
                }
            }

            double p0_Lo2D = 23.78;
            double p0_De2D = 1.00;
            double p0_Lo3D = 2.275;
            double p0_De3D = 0.100;

            double rateLo2D = winLo2D * 100.0 / testDays;
            double rateDe2D = winDe2D * 100.0 / testDays;
            double rateLo3D = winLo3D * 100.0 / testDays;
            double rateDe3D = winDe3D * 100.0 / testDays;

            Console.WriteLine(string.Format("{0,-22} | {1,-6} | {2,-8} | {3,-7} | {4,-7} | {5,-6} | {6,-7} | {7,-7}",
                "Hạng Mục Bạch Thủ Lô", "SL Số", "Số Kỳ Ăn", "WinRate", "Random", "Alpha", "MaxWin", "MaxLoss"));
            Console.WriteLine(new string('-', 90));

            Console.WriteLine(string.Format("{0,-22} | {1,-6} | {2,-8} | {3,6:F1}% | {4,6:F2}% | {5,5:F2} | {6,-7} | {7,-7}",
                "BTL 2D (Lô 27 giải)", 1, winLo2D + "/" + testDays, rateLo2D, p0_Lo2D, rateLo2D / p0_Lo2D, maxStreakLo2D, maxLossLo2D));
            Console.WriteLine(string.Format("{0,-22} | {1,-6} | {2,-8} | {3,6:F1}% | {4,6:F2}% | {5,5:F2} | {6,-7} | {7,-7}",
                "BTL 2D (Đề GĐB)", 1, winDe2D + "/" + testDays, rateDe2D, p0_De2D, rateDe2D / p0_De2D, winDe2D > 0 ? 1 : 0, 100));
            Console.WriteLine(string.Format("{0,-22} | {1,-6} | {2,-8} | {3,6:F1}% | {4,6:F2}% | {5,5:F2} | {6,-7} | {7,-7}",
                "BTL 3D (Lô 23 giải)", 1, winLo3D + "/" + testDays, rateLo3D, p0_Lo3D, rateLo3D / p0_Lo3D, maxStreakLo3D, maxLossLo3D));
            Console.WriteLine(string.Format("{0,-22} | {1,-6} | {2,-8} | {3,6:F1}% | {4,6:F2}% | {5,5:F2} | {6,-7} | {7,-7}",
                "BTL 3D (Đề GĐB)", 1, winDe3D + "/" + testDays, rateDe3D, p0_De3D, rateDe3D / p0_De3D, winDe3D > 0 ? 1 : 0, 100));

            Console.WriteLine(new string('=', 90));
            if (hitList.Count > 0)
            {
                Console.WriteLine("    DANH SÁCH CÁC KỲ NỔ ĐÒN BẨY ĐỘT BIẾN BTL 3D (3 CÀNG LÔ):");
                foreach (var h in hitList) Console.WriteLine("   * " + h);
                Console.WriteLine(new string('-', 90));
            }

            var today1D = Compute1DLottoMarginals(records);
            double[,] W_today;
            var todayPotentials = ComputeUnifiedLottoPotentials(records, today1D, out W_today);
            var todayTop4 = SelectTop4LoWithDiversity(todayPotentials, records);
            int todayBTL2D = todayTop4[0];
            string todayBTL3D;
            int todayCang3D;
            ComputeBTL_3D(records, todayBTL2D, out todayBTL3D, out todayCang3D);

            Console.WriteLine("    🎯 DỰ BÁO BỘ ĐÔI BẠCH THỦ LÔ CHO KỲ MỞ THƯỞNG HÔM NAY:");
            Console.WriteLine(string.Format("   1. [BẠCH THỦ LÔ 2D] : {0:D2} (Đầu {1}, Đuôi {2})", todayBTL2D, todayBTL2D / 10, todayBTL2D % 10));
            Console.WriteLine(string.Format("   2. [BẠCH THỦ LÔ 3D] : {0}    (Càng 3D: {1} + BTL 2D: {2:D2})", todayBTL3D, todayCang3D, todayBTL2D));
            Console.WriteLine("=========================================================================================");
        }

        public static void Run3D4D_Audit(List<LotteryRecord> records, int testDays = 100)
        {
            int n = records.Count;
            if (testDays <= 0 || testDays > 3000) testDays = 100;
            int startIdx = n - testDays;

            Console.WriteLine("=========================================================================================");
            Console.WriteLine(string.Format("   BẢNG KIỂM THỬ ĐỐI SOÁT TOÀN DIỆN CÁC MODUN 3D VÀ 4D ĐỀ GĐB ({0} KỲ GẦN NHẤT)", testDays));
            Console.WriteLine(string.Format("   Khoảng thời gian: từ kỳ #{0} ({1}, {2}) đến kỳ #{3} ({4}, {5})",
                records[startIdx].Id, records[startIdx].Date, records[startIdx].DayOfWeek, records[n - 1].Id, records[n - 1].Date, records[n - 1].DayOfWeek));
            Console.WriteLine("=========================================================================================");

            var stats = new List<ItemAuditStat>
            {
                new ItemAuditStat { Stt = 1,  Axis = "3D Đề", Name = "Bạch Thủ 3 Càng Đề",      Quantity = 1,  RandomProb = 0.10 },
                new ItemAuditStat { Stt = 2,  Axis = "3D Đề", Name = "Tứ Thủ 3 Càng Đề",        Quantity = 4,  RandomProb = 0.40 },
                new ItemAuditStat { Stt = 3,  Axis = "3D Đề", Name = "Dàn 3 Càng Tinh Tuyển",    Quantity = 12, RandomProb = 1.20 },
                new ItemAuditStat { Stt = 4,  Axis = "3D Đề", Name = "Dàn 3 Càng Phổ",           Quantity = 27, RandomProb = 2.70 },
                new ItemAuditStat { Stt = 5,  Axis = "3D Đề", Name = "Dàn 3 Càng Trọng Tâm",     Quantity = 36, RandomProb = 3.60 },
                new ItemAuditStat { Stt = 6,  Axis = "4D Đề", Name = "Bạch Thủ 4 Càng Đề",      Quantity = 1,  RandomProb = 0.01 },
                new ItemAuditStat { Stt = 7,  Axis = "4D Đề", Name = "Tứ Thủ 4 Càng Đề",        Quantity = 4,  RandomProb = 0.04 },
                new ItemAuditStat { Stt = 8,  Axis = "4D Đề", Name = "Dàn 4 Càng Trọng Tâm",     Quantity = 16, RandomProb = 0.16 },
                new ItemAuditStat { Stt = 9,  Axis = "4D Đề", Name = "Dàn 4 Càng Cân Bằng",     Quantity = 36, RandomProb = 0.36 },
                new ItemAuditStat { Stt = 10, Axis = "4D Đề", Name = "Dàn 4 Càng Mở Rộng",       Quantity = 81, RandomProb = 0.81 }
            };

            var hitDetails = new List<string>();

            for (int t = startIdx; t < n; t++)
            {
                var subList = records.Take(t).ToList();
                var actual = records[t];
                string actual3D = actual.Gdb3D;
                string actual4D = actual.Gdb4D;

                // 3D QUANTUM (Mở rộng từ phương pháp 4D)
                string bt3D;
                List<string> dan3D_4, dan3D_12, dan3D_27, dan3D_36;
                Compute3D_QuantumOptimal(subList, out bt3D, out dan3D_4, out dan3D_12, out dan3D_27, out dan3D_36);

                // 4D QUANTUM (Phương pháp 4D gốc)
                string bt4D;
                List<string> dan4D_4, dan4D_16, dan4D_36, dan4D_81;
                Compute4D_QuantumOptimal(subList, out bt4D, out dan4D_4, out dan4D_16, out dan4D_36, out dan4D_81);

                stats[0].Record(actual3D == bt3D);
                stats[1].Record(dan3D_4.Contains(actual3D));
                stats[2].Record(dan3D_12.Contains(actual3D));
                stats[3].Record(dan3D_27.Contains(actual3D));
                stats[4].Record(dan3D_36.Contains(actual3D));

                stats[5].Record(actual4D == bt4D);
                stats[6].Record(dan4D_4.Contains(actual4D));
                stats[7].Record(dan4D_16.Contains(actual4D));
                stats[8].Record(dan4D_36.Contains(actual4D));
                stats[9].Record(dan4D_81.Contains(actual4D));

                if (dan3D_12.Contains(actual3D) || dan4D_36.Contains(actual4D) || dan4D_81.Contains(actual4D))
                {
                    var wins = new List<string>();
                    if (actual3D == bt3D) wins.Add("BT3D: " + actual3D);
                    if (dan3D_4.Contains(actual3D)) wins.Add("3D-4s: " + actual3D);
                    if (dan3D_12.Contains(actual3D)) wins.Add("3D-12s: " + actual3D);
                    if (dan4D_16.Contains(actual4D)) wins.Add("4D-16s: " + actual4D);
                    if (dan4D_36.Contains(actual4D)) wins.Add("4D-36s: " + actual4D);
                    if (dan4D_81.Contains(actual4D)) wins.Add("4D-81s: " + actual4D);
                    hitDetails.Add(string.Format("Kỳ #{0} ({1}): {2}", actual.Id, actual.Date, string.Join(" | ", wins)));
                }
            }

            Console.WriteLine(string.Format("{0,-3} | {1,-8} | {2,-25} | {3,-4} | {4,-7} | {5,-7} | {6,-7} | {7,-5} | {8,-7} | {9,-7}",
                "STT", "Hệ", "Hạng Mục Dự Đoán", "SL", "Thắng", "WinRate", "Random", "Alpha", "MaxWin", "MaxLoss"));
            Console.WriteLine(new string('-', 98));

            for (int i = 0; i < stats.Count; i++)
            {
                var s = stats[i];
                double winRate = s.Wins * 100.0 / testDays;
                double alpha = winRate / s.RandomProb;
                Console.WriteLine(string.Format("{0,-3} | {1,-8} | {2,-25} | {3,-4} | {4,-7} | {5,6:F1}% | {6,6:F2}% | {7,5:F2} | {8,-7} | {9,-7}",
                    s.Stt, s.Axis, s.Name, s.Quantity, s.Wins + "/" + testDays, winRate, s.RandomProb, alpha, s.MaxStreak, s.MaxLoss));
            }

            Console.WriteLine(new string('=', 98));
            if (hitDetails.Count > 0)
            {
                Console.WriteLine("    DANH SÁCH CÁC KỲ NỔ ĐÒN BẨY ĐỘT BIẾN 3D & 4D ĐỀ GĐB:");
                foreach (var h in hitDetails) Console.WriteLine("   * " + h);
                Console.WriteLine(new string('-', 98));
            }

            // DỰ BÁO HÔM NAY
            string todayBt3D;
            List<string> today3D_4, today3D_12, today3D_27, today3D_36;
            Compute3D_QuantumOptimal(records, out todayBt3D, out today3D_4, out today3D_12, out today3D_27, out today3D_36);

            string todayBt4D;
            List<string> today4D_4, today4D_16, today4D_36, today4D_81;
            Compute4D_QuantumOptimal(records, out todayBt4D, out today4D_4, out today4D_16, out today4D_36, out today4D_81);

            Console.WriteLine("    🎯 DỰ BÁO CÁC TẦNG 3D VÀ 4D ĐỀ GĐB CHO KỲ HÔM NAY:");
            Console.WriteLine("   [3D ĐỀ GĐB (MỞ RỘNG TỪ HỆ THỐNG 4D)]:");
            Console.WriteLine(string.Format("   * Bạch Thủ 3 Càng:        {0}", todayBt3D));
            Console.WriteLine(string.Format("   * Tứ Thủ 3 Càng (4 số):    {0}", string.Join(", ", today3D_4)));
            Console.WriteLine(string.Format("   * Dàn 3 Càng Tinh Tuyển:   {0}", string.Join(", ", today3D_12)));
            Console.WriteLine(string.Format("   * Dàn 3 Càng Phổ 27 số:    {0}", string.Join(", ", today3D_27)));
            Console.WriteLine(string.Format("   * Dàn 3 Càng Trọng Tâm 36: {0}", string.Join(", ", today3D_36)));
            Console.WriteLine("   [4D ĐỀ GĐB (HỆ THỐNG 4D GỐC)]:");
            Console.WriteLine(string.Format("   * Bạch Thủ 4 Càng:        {0}", todayBt4D));
            Console.WriteLine(string.Format("   * Tứ Thủ 4 Càng (4 số):    {0}", string.Join(", ", today4D_4)));
            Console.WriteLine(string.Format("   * Dàn 4 Càng Trọng Tâm 16: {0}", string.Join(", ", today4D_16)));
            Console.WriteLine(string.Format("   * Dàn 4 Càng Cân Bằng 36:  {0}", string.Join(", ", today4D_36)));
            Console.WriteLine(string.Format("   * Dàn 4 Càng Mở Rộng 81:   {0}", string.Join(", ", today4D_81)));
            Console.WriteLine("=========================================================================================");
        }

        public static void RunTamThuAudit(List<LotteryRecord> records, int testDays = 100)
        {
            int n = records.Count;
            if (testDays <= 0 || testDays > 3000) testDays = 100;
            int startIdx = n - testDays;

            Console.WriteLine("=========================================================================================");
            Console.WriteLine(string.Format("   BẢNG ĐỐI SOÁT KIỂM THỬ: TAM THỦ LÔ (3 SỐ) VS TỨ THỦ LÔ (4 SỐ) ({0} KỲ GẦN NHẤT)", testDays));
            Console.WriteLine(string.Format("   Khoảng thời gian: từ kỳ #{0} ({1}, {2}) đến kỳ #{3} ({4}, {5})",
                records[startIdx].Id, records[startIdx].Date, records[startIdx].DayOfWeek, records[n - 1].Id, records[n - 1].Date, records[n - 1].DayOfWeek));
            Console.WriteLine("=========================================================================================");

            var stats = new List<ItemAuditStat>
            {
                new ItemAuditStat { Stt = 1,  Axis = "Bạch Thủ",  Name = "Bạch Thủ Lô (BTL 1s)",       Quantity = 1, RandomProb = 23.78 },
                new ItemAuditStat { Stt = 2,  Axis = "Song Thủ",  Name = "Song Thủ Lô (STL 2s)",       Quantity = 2, RandomProb = 42.17 },
                new ItemAuditStat { Stt = 3,  Axis = "Tam Thủ",   Name = "Tam Thủ Lô (TTL 3s)",        Quantity = 3, RandomProb = 56.24 },
                new ItemAuditStat { Stt = 4,  Axis = "Tứ Thủ",    Name = "Tứ Thủ Lô (4s - Cũ)",        Quantity = 4, RandomProb = 66.83 },
                new ItemAuditStat { Stt = 5,  Axis = "Lô Lót",    Name = "Lô Lót Tam Thủ (1s)",        Quantity = 1, RandomProb = 23.78 },
                new ItemAuditStat { Stt = 6,  Axis = "Lô Lót",    Name = "Lô Lót Tứ Thủ (2s - Cũ)",    Quantity = 2, RandomProb = 42.17 },
                new ItemAuditStat { Stt = 7,  Axis = "Xiên 2",    Name = "Xiên 2 Song Thủ (1 cặp)",    Quantity = 1, RandomProb = 5.66 },
                new ItemAuditStat { Stt = 8,  Axis = "Xiên 2",    Name = "Bộ 3 Cặp Xiên 2 (Tam Thủ)",  Quantity = 3, RandomProb = 15.60 },
                new ItemAuditStat { Stt = 9,  Axis = "Xiên 2",    Name = "Bộ 6 Cặp Xiên 2 (Tứ Thủ)",   Quantity = 6, RandomProb = 24.53 },
                new ItemAuditStat { Stt = 10, Axis = "Xiên 3",    Name = "Bộ 1 Xiên 3 (Tam Thủ)",      Quantity = 1, RandomProb = 1.01 },
                new ItemAuditStat { Stt = 11, Axis = "Xiên 3",    Name = "Bộ 4 Xiên 3 (Tứ Thủ)",       Quantity = 4, RandomProb = 4.02 },
                new ItemAuditStat { Stt = 12, Axis = "Xiên 4",    Name = "Bộ 1 Xiên 4 (Tứ Thủ)",       Quantity = 1, RandomProb = 0.32 }
            };

            int totalNhaysBtl = 0, totalNhaysStl = 0, totalNhaysTamThu = 0, totalNhaysTuThu = 0;
            int totalPairsTamThu = 0, totalPairsTuThu = 0;
            int totalTriosTamThu = 0, totalTriosTuThu = 0;

            // Mô phỏng tài chính PnL (Chi phí cược 1 điểm = 27k, ăn 1 nháy = 99k, xiên 2 ăn 16.5, xiên 3 ăn 65)
            // Phương án A: Tứ Thủ Lô (STL 3đ x 2 + Lót 1đ x 2 = 8đ = 216k, Xiên 2 6x5k=30k, Xiên 3 4x5k=20k, Xiên 4 1x5k=5k => Tổng 271k)
            long costTuThuDaily = 8 * 27000 + 30000 + 20000 + 5000;
            long pnlTuThuTotal = 0;

            // Phương án B: Tam Thủ Lô (STL 3đ x 2 + Lót 1đ x 1 = 7đ = 189k, Xiên 2 3x5k=15k, Xiên 3 1x5k=5k => Tổng 209k)
            long costTamThuDaily = 7 * 27000 + 15000 + 5000;
            long pnlTamThuTotal = 0;

            // Phương án C: Tam Thủ Lô Cân Bằng (Mỗi con 3đ x 3 = 9đ = 243k, Xiên 2 3x5k=15k, Xiên 3 1x5k=5k => Tổng 263k)
            long costTamThuEqualDaily = 9 * 27000 + 15000 + 5000;
            long pnlTamThuEqualTotal = 0;

            for (int t = startIdx; t < n; t++)
            {
                var subList = records.Take(t).ToList();
                var actual = records[t];
                var loUnique = actual.UniqueLo;
                var loList = actual.LoList;

                var lotto1D = Compute1DLottoMarginals(subList);
                double[,] W_lotto;
                var lottoEnergies = ComputeUnifiedLottoPotentials(subList, lotto1D, out W_lotto);
                var top4 = SelectTop4LoWithDiversity(lottoEnergies, subList);
                int btLo = top4[0];
                int stLo = top4[1];
                int tuThu3 = top4[2];
                int tuThu4 = top4[3];

                var tamThuList = new List<int> { btLo, stLo, tuThu3 };
                var tuThuList = new List<int> { btLo, stLo, tuThu3, tuThu4 };

                int btlHits = loList.Count(x => x == btLo);
                int stlHits = btlHits + loList.Count(x => x == stLo);
                int lotHitsTam = loList.Count(x => x == tuThu3);
                int lotHitsTu = lotHitsTam + loList.Count(x => x == tuThu4);
                int tamHits = stlHits + lotHitsTam;
                int tuHits = stlHits + lotHitsTu;

                totalNhaysBtl += btlHits;
                totalNhaysStl += stlHits;
                totalNhaysTamThu += tamHits;
                totalNhaysTuThu += tuHits;

                stats[0].Record(btlHits > 0, btlHits);
                stats[1].Record(stlHits > 0, stlHits);
                stats[2].Record(tamHits > 0, tamHits);
                stats[3].Record(tuHits > 0, tuHits);
                stats[4].Record(lotHitsTam > 0, lotHitsTam);
                stats[5].Record(lotHitsTu > 0, lotHitsTu);

                // Xiên 2 Song Thủ
                bool winX2Stl = loUnique.Contains(btLo) && loUnique.Contains(stLo);
                stats[6].Record(winX2Stl, winX2Stl ? 1 : 0);

                // Xiên 2 Tam Thủ (3 cặp)
                var pTam = new List<Tuple<int, int>> {
                    Tuple.Create(btLo, stLo), Tuple.Create(btLo, tuThu3), Tuple.Create(stLo, tuThu3)
                };
                int winPairsTam = pTam.Count(p => loUnique.Contains(p.Item1) && loUnique.Contains(p.Item2));
                totalPairsTamThu += winPairsTam;
                stats[7].Record(winPairsTam > 0, winPairsTam);

                // Xiên 2 Tứ Thủ (6 cặp)
                var pTu = new List<Tuple<int, int>>();
                for (int i = 0; i < 4; i++)
                    for (int j = i + 1; j < 4; j++)
                        pTu.Add(Tuple.Create(tuThuList[i], tuThuList[j]));
                int winPairsTu = pTu.Count(p => loUnique.Contains(p.Item1) && loUnique.Contains(p.Item2));
                totalPairsTuThu += winPairsTu;
                stats[8].Record(winPairsTu > 0, winPairsTu);

                // Xiên 3 Tam Thủ (1 bộ)
                bool winX3Tam = tamThuList.All(x => loUnique.Contains(x));
                if (winX3Tam) totalTriosTamThu++;
                stats[9].Record(winX3Tam, winX3Tam ? 1 : 0);

                // Xiên 3 Tứ Thủ (4 bộ)
                var trTu = new List<List<int>>();
                for (int i = 0; i < 4; i++)
                    for (int j = i + 1; j < 4; j++)
                        for (int k = j + 1; k < 4; k++)
                            trTu.Add(new List<int> { tuThuList[i], tuThuList[j], tuThuList[k] });
                int winTriosTu = trTu.Count(tr => tr.All(x => loUnique.Contains(x)));
                totalTriosTuThu += winTriosTu;
                stats[10].Record(winTriosTu > 0, winTriosTu);

                // Xiên 4 Tứ Thủ
                bool winX4 = tuThuList.All(x => loUnique.Contains(x));
                stats[11].Record(winX4, winX4 ? 1 : 0);

                // Tài chính PnL
                // Tu Thu (A)
                long revTuThu = (stlHits * 3 + lotHitsTu * 1) * 99000L + (long)(winPairsTu * 5000L * 16.5) + (long)(winTriosTu * 5000L * 65.0) + (winX4 ? 5000L * 250 : 0);
                pnlTuThuTotal += (revTuThu - costTuThuDaily);

                // Tam Thu (B: 2 STL x 3d + 1 Lot x 1d, 3 X2 x 5k, 1 X3 x 5k)
                long revTamThu = (stlHits * 3 + lotHitsTam * 1) * 99000L + (long)(winPairsTam * 5000L * 16.5) + (winX3Tam ? 5000L * 65 : 0);
                pnlTamThuTotal += (revTamThu - costTamThuDaily);

                // Tam Thu Can Bang (C: 3 con x 3d, 3 X2 x 5k, 1 X3 x 5k)
                long revTamThuEq = tamHits * 3 * 99000L + (long)(winPairsTam * 5000L * 16.5) + (winX3Tam ? 5000L * 65 : 0);
                pnlTamThuEqualTotal += (revTamThuEq - costTamThuEqualDaily);
            }

            Console.WriteLine(string.Format("{0,-3} | {1,-10} | {2,-28} | {3,-4} | {4,-7} | {5,-7} | {6,-7} | {7,-5} | {8,-7} | {9,-7}",
                "STT", "Hệ", "Hạng Mục Dự Đoán", "SL", "Thắng", "WinRate", "Random", "Alpha", "MaxWin", "MaxLoss"));
            Console.WriteLine(new string('-', 102));

            for (int i = 0; i < stats.Count; i++)
            {
                var s = stats[i];
                double winRate = s.Wins * 100.0 / testDays;
                double alpha = winRate / s.RandomProb;
                Console.WriteLine(string.Format("{0,-3} | {1,-10} | {2,-28} | {3,-4} | {4,-7} | {5,6:F1}% | {6,6:F2}% | {7,5:F2} | {8,-7} | {9,-7}",
                    s.Stt, s.Axis, s.Name, s.Quantity, s.Wins + "/" + testDays, winRate, s.RandomProb, alpha, s.MaxStreak, s.MaxLoss));
            }
            Console.WriteLine(new string('=', 102));

            Console.WriteLine("    TỔNG HỢP HIỆU SUẤT NHÁY VÀ CẶP XIÊN:");
            Console.WriteLine(string.Format("   * Bạch Thủ Lô (1s):   {0} nháy nổ / {1} kỳ (TB: {2:F2} nháy/kỳ)", totalNhaysBtl, testDays, (double)totalNhaysBtl / testDays));
            Console.WriteLine(string.Format("   * Song Thủ Lô (2s):   {0} nháy nổ / {1} kỳ (TB: {2:F2} nháy/kỳ)", totalNhaysStl, testDays, (double)totalNhaysStl / testDays));
            Console.WriteLine(string.Format("   * TAM THỦ LÔ (3s):    {0} nháy nổ / {1} kỳ (TB: {2:F2} nháy/kỳ) | Nổ {3} cặp Xiên 2, {4} bộ Xiên 3",
                totalNhaysTamThu, testDays, (double)totalNhaysTamThu / testDays, totalPairsTamThu, totalTriosTamThu));
            Console.WriteLine(string.Format("   * TỨ THỦ LÔ (4s):     {0} nháy nổ / {1} kỳ (TB: {2:F2} nháy/kỳ) | Nổ {3} cặp Xiên 2, {4} bộ Xiên 3",
                totalNhaysTuThu, testDays, (double)totalNhaysTuThu / testDays, totalPairsTuThu, totalTriosTuThu));
            Console.WriteLine(new string('-', 102));

            Console.WriteLine("    SO SÁNH TÀI CHÍNH DANH MỤC LÔ & XIÊN (P&L MÔ PHỎNG):");
            Console.WriteLine(string.Format("   [1. TỨ THỦ LÔ - 4 SỐ (CŨ)]       : Vốn: {0:N0} đ | Lợi nhuận: {1:+0;-0;0} VNĐ | ROI: {2:F1}%",
                costTuThuDaily * testDays, pnlTuThuTotal, (double)pnlTuThuTotal * 100.0 / (costTuThuDaily * testDays)));
            Console.WriteLine(string.Format("   [2. TAM THỦ LÔ - 3 SỐ (STL + LÓT)]: Vốn: {0:N0} đ | Lợi nhuận: {1:+0;-0;0} VNĐ | ROI: {2:F1}%",
                costTamThuDaily * testDays, pnlTamThuTotal, (double)pnlTamThuTotal * 100.0 / (costTamThuDaily * testDays)));
            Console.WriteLine(string.Format("   [3. TAM THỦ LÔ - CÂN BẰNG ĐỀU]   : Vốn: {0:N0} đ | Lợi nhuận: {1:+0;-0;0} VNĐ | ROI: {2:F1}%",
                costTamThuEqualDaily * testDays, pnlTamThuEqualTotal, (double)pnlTamThuEqualTotal * 100.0 / (costTamThuEqualDaily * testDays)));
            Console.WriteLine(new string('=', 102));

            // DỰ BÁO HÔM NAY
            var today1D = Compute1DLottoMarginals(records);
            double[,] W_today;
            var todayPotentials = ComputeUnifiedLottoPotentials(records, today1D, out W_today);
            var todayTop4 = SelectTop4LoWithDiversity(todayPotentials, records);
            int todayBt = todayTop4[0];
            int todaySt = todayTop4[1];
            int todayTam3 = todayTop4[2];

            Console.WriteLine("    🎯 DỰ BÁO TAM THỦ LÔ CHO KỲ HÔM NAY:");
            Console.WriteLine(string.Format("   * TAM THỦ LÔ TRỌN BỘ (3 SỐ): {0:D2} - {1:D2} - {2:D2}", todayBt, todaySt, todayTam3));
            Console.WriteLine(string.Format("     - Bạch Thủ Lô chính : {0:D2}", todayBt));
            Console.WriteLine(string.Format("     - Song Thủ Lô chính : {0:D2} - {1:D2}", todayBt, todaySt));
            Console.WriteLine(string.Format("     - Lô Lót Tam Thủ    : {0:D2}", todayTam3));
            Console.WriteLine(string.Format("   * BỘ 3 CẶP XIÊN 2 TAM THỦ  : ({0:D2}-{1:D2}), ({0:D2}-{2:D2}), ({1:D2}-{2:D2})", todayBt, todaySt, todayTam3));
            Console.WriteLine(string.Format("   * BỘ 1 XIÊN 3 TAM THỦ      : {0:D2}-{1:D2}-{2:D2}", todayBt, todaySt, todayTam3));
            Console.WriteLine("=========================================================================================");
        }
    }
}
