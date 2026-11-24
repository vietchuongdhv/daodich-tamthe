using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

class DrawData
{
    public int Id;
    public string Date;
    public string DayOfWeek;
    public string Gdb;
    public string G1;
    public string G2;
    public string G3;
    public string G4;
    public string G5;
    public string G6;
    public string G7;
    public string[] LoRaw;

    public DrawData(int id, string date, string dow, string gdb, string g1, string g2, string g3, string g4, string g5, string g6, string g7, string[] lo)
    {
        Id = id; Date = date; DayOfWeek = dow; Gdb = gdb; G1 = g1; G2 = g2; G3 = g3; G4 = g4; G5 = g5; G6 = g6; G7 = g7; LoRaw = lo;
    }
}

class Program
{
    static void Main()
    {
        string csvPath = Path.Combine("data", "lottery_master.csv");
        var lines = File.ReadAllLines(csvPath, Encoding.UTF8).ToList();
        
        var batch = new List<DrawData>()
        {
            // 1. 2026-11-10
            new DrawData(
                8599, "2026-11-10", "\"Thứ ba\"", "73683", "25992",
                "\"46401,80718\"",
                "\"34541,62689,58806,26594,11823,33335\"",
                "\"8889,8400,5739,5668\"",
                "\"6370,8957,4414,5050,6394,7747\"",
                "\"868,593,607\"",
                "\"14,00,83,56\"",
                new string[] { "83", "92", "01", "18", "41", "89", "06", "94", "23", "35", "89", "00", "39", "68", "70", "57", "14", "50", "94", "47", "68", "93", "07", "14", "00", "83", "56" }
            ),
            // 2. 2026-11-11
            new DrawData(
                8600, "2026-11-11", "\"Thứ tư\"", "74129", "97579",
                "\"20960,11980\"",
                "\"13449,30793,18746,86606,00972,26471\"",
                "\"1760,9505,3676,7568\"",
                "\"1494,1478,5404,5724,9888,5597\"",
                "\"714,093,910\"",
                "\"51,34,12,95\"",
                new string[] { "29", "79", "60", "80", "49", "93", "46", "06", "72", "71", "60", "05", "76", "68", "94", "78", "04", "24", "88", "97", "14", "93", "10", "51", "34", "12", "95" }
            ),
            // 3. 2026-11-12
            new DrawData(
                8601, "2026-11-12", "\"Thứ năm\"", "43454", "20276",
                "\"47709,79463\"",
                "\"29689,87069,43753,32903,40181,30096\"",
                "\"3601,0920,3991,4207\"",
                "\"2532,4992,3601,5624,0585,3325\"",
                "\"436,772,492\"",
                "\"48,27,33,99\"",
                new string[] { "54", "76", "09", "63", "89", "69", "53", "03", "81", "96", "01", "20", "91", "07", "32", "92", "01", "24", "85", "25", "36", "72", "92", "48", "27", "33", "99" }
            ),
            // 4. 2026-11-13
            new DrawData(
                8602, "2026-11-13", "\"Thứ sáu\"", "75451", "73501",
                "\"26712,37832\"",
                "\"47367,75024,83712,63799,45491,63479\"",
                "\"3809,4568,1965,8562\"",
                "\"7700,4231,1104,3529,8253,0835\"",
                "\"146,899,498\"",
                "\"23,00,57,61\"",
                new string[] { "51", "01", "12", "32", "67", "24", "12", "99", "91", "79", "09", "68", "65", "62", "00", "31", "04", "29", "53", "35", "46", "99", "98", "23", "00", "57", "61" }
            ),
            // 5. 2026-11-14
            new DrawData(
                8603, "2026-11-14", "\"Thứ bảy\"", "20893", "14291",
                "\"11312,51790\"",
                "\"44626,32334,34538,30377,40265,28940\"",
                "\"8890,2514,1620,6160\"",
                "\"4556,4376,8015,4048,6878,8916\"",
                "\"111,198,517\"",
                "\"31,28,73,48\"",
                new string[] { "93", "91", "12", "90", "26", "34", "38", "77", "65", "40", "90", "14", "20", "60", "56", "76", "15", "48", "78", "16", "11", "98", "17", "31", "28", "73", "48" }
            )
        };

        foreach (var item in batch)
        {
            int id = item.Id;
            string date = item.Date;
            string dayOfWeek = item.DayOfWeek;
            string gdb = item.Gdb;
            string g1 = item.G1;
            string g2 = item.G2;
            string g3 = item.G3;
            string g4 = item.G4;
            string g5 = item.G5;
            string g6 = item.G6;
            string g7 = item.G7;
            string[] loRaw = item.LoRaw;

            int de = int.Parse(gdb.Substring(3, 2));
            int dau = de / 10;
            int duoi = de % 10;
            int tong = (dau + duoi) % 10;

            var uniqueLo = loRaw.Distinct().OrderBy(x => x).ToList();
            int uniqueCount = uniqueLo.Count;
            string uniqueLoStr = "\"" + string.Join(",", uniqueLo) + "\"";
            string allLoStr = "\"" + string.Join(",", loRaw) + "\"";
            
            // Build raw digits
            string rawDigits = "\"" + gdb + g1 + g2.Replace("\"", "").Replace(",", "") 
                + g3.Replace("\"", "").Replace(",", "") 
                + g4.Replace("\"", "").Replace(",", "") 
                + g5.Replace("\"", "").Replace(",", "") 
                + g6.Replace("\"", "").Replace(",", "") 
                + g7.Replace("\"", "").Replace(",", "") + "\"";

            string newLine = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18}",
                id, date, dayOfWeek, gdb, g1, g2, g3, g4, g5, g6, g7, de, dau, duoi, tong, uniqueCount, uniqueLoStr, allLoStr, rawDigits);

            lines.Add(newLine);
            Console.WriteLine("Prepared record " + id + " (" + date + "): De = " + de.ToString("D2"));
        }

        File.WriteAllLines(csvPath, lines, Encoding.UTF8);
        Console.WriteLine("Appended 5 records to " + csvPath);

        // Update metadata.json
        string metaPath = Path.Combine("data", "metadata.json");
        if (File.Exists(metaPath))
        {
            string meta = File.ReadAllText(metaPath, Encoding.UTF8);
            meta = meta.Replace("\"total_records\": 8598", "\"total_records\": 8603");
            meta = meta.Replace("\"end_date\": \"2026-11-09\"", "\"end_date\": \"2026-11-14\"");
            File.WriteAllText(metaPath, meta, Encoding.UTF8);
            Console.WriteLine("Updated metadata.json to 8603 records, end_date = 2026-11-14");
        }
    }
}
