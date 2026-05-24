using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Generates a Student Terminal Report PDF that exactly matches the
    /// Kingdom Preparatory School official report card design:
    ///   - Navy header with logo placeholder, school name block and photo box
    ///   - 6-row × 2-column student info table
    ///   - Subjects table: Class Score (50%) | Exams Score (50%) | Total (100%) | Grade | Position | Remarks
    ///   - Grading System side panel (Score/Grade/Remarks × 5 levels)
    ///   - Affective domain rows (Attitude, Interest, Conduct, Teacher remarks)
    ///   - "Promoted to:" banner + dual signature boxes
    ///   - Bottom navy grading bar (gold label + Score/Grade/Remarks rows)
    /// </summary>
    public static class ReportCardPdfService
    {
        // ── Page ────────────────────────────────────────────────────────────
        private const double PW = 595;  // A4 pt
        private const double PH = 842;
        private const double ML = 30;   // left margin
        private const double MR = 30;   // right margin
        private const double BW = PW - ML - MR;  // body width = 535

        // ── Colours (match the physical report card) ────────────────────────
        private static readonly XColor Navy    = XColor.FromArgb( 13,  27,  75);
        private static readonly XColor NavyMid = XColor.FromArgb( 20,  40, 100);
        private static readonly XColor Gold    = XColor.FromArgb(200, 155,  10);
        private static readonly XColor LBlue   = XColor.FromArgb(189, 214, 238);
        private static readonly XColor LGray   = XColor.FromArgb(209, 209, 209);
        private static readonly XColor MGray   = XColor.FromArgb(165, 165, 165);
        private static readonly XColor White   = XColors.White;
        private static readonly XColor Black   = XColors.Black;

        // ── Font helpers ────────────────────────────────────────────────────
        private static XFont F(double sz, bool bold = false) =>
            new XFont("Arial", sz, bold ? XFontStyleEx.Bold : XFontStyleEx.Regular);

        private static XFont FB(double sz) => F(sz, true);

        // ── Brush / Pen helpers ─────────────────────────────────────────────
        private static XSolidBrush Br(XColor c) => new XSolidBrush(c);
        private static XPen        Pn(XColor c, double w = 0.5) => new XPen(c, w);

        // ════════════════════════════════════════════════════════════════════
        //  PUBLIC ENTRY POINT
        // ════════════════════════════════════════════════════════════════════
        public static void Export(Dictionary<string, string> data)
        {
            try
            {
                string name     = V(data, "NAME", "Student");
                string safeName = name.Replace(" ", "_").Replace("/", "-");
                string fileName = $"ReportCard_{safeName}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                string path     = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

                using (var doc = new PdfDocument())
                {
                    doc.Info.Title = $"Report Card – {name}";
                    var page = doc.AddPage();
                    page.Width  = PW;
                    page.Height = PH;

                    using (var g = XGraphics.FromPdfPage(page))
                    {
                        double y = PH;               // cursor: moves DOWN as we draw (PDF y=0 at bottom)
                        y = DrawHeader(g, y, data);
                        y = DrawInfoTable(g, y, data);
                        y = DrawSubjectsSection(g, y, data);
                        y = DrawAffectiveDomain(g, y);
                        y = DrawSignatures(g, y);
                        DrawBottomGradingBar(g, y);
                    }

                    doc.Save(path);
                }

                MessageBox.Show(
                    $"Report card saved to Desktop:\n{fileName}",
                    "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);

                try { Process.Start(path); } catch { /* viewer not found */ }
            }
            catch (Exception ex)
            {
                MessageBox.Show("PDF generation failed:\n" + ex.Message,
                    "PDF Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  SECTION 1 — HEADER BAND (navy background, logo, school name, photo)
        // ════════════════════════════════════════════════════════════════════
        private static double DrawHeader(XGraphics g, double top, Dictionary<string, string> data)
        {
            const double H = 115;
            double y = top - H;

            // Navy background
            g.DrawRectangle(Br(Navy), ML - 2, y, BW + 4, H);

            // ── Logo box (left) ──
            const double LW = 90, LH = 90;
            double lx = ML, ly = y + (H - LH) / 2;
            g.DrawRectangle(Pn(Gold, 1.5), lx, ly, LW, LH);
            g.DrawEllipse(Pn(Gold, 1),     lx + 4, ly + 4, LW - 8, LH - 8);
            CentreText(g, "KPS",          lx, ly + LH/2 - 8,  LW, F(15, true),  Gold);
            CentreText(g, "AKIM ABENASE", lx, ly + LH/2 + 2,  LW, F(5.5),       Gold);
            CentreText(g, "I.H.S.",       lx, ly + LH/2 + 10, LW, F(5.5),       Gold);
            CentreText(g, "KNOWLEDGE IS POWER", lx, ly+4, LW, F(4.5), Gold);

            // ── Centre text block ──
            double tx = ML + LW + 8;
            double tw = BW - LW - 8 - 82;
            CentreText(g, "KINGDOM PREPARATORY SCHOOL", tx, y + H - 27, tw, FB(14), White);
            CentreText(g, "AKIM ODA- ABENASE",          tx, y + H - 43, tw, FB(10), White);
            CentreText(g, "0548050141/0246087609",       tx, y + H - 57, tw, F(9),   White);
            CentreText(g, "STUDENT TERMINAL REPORT",    tx, y + H - 73, tw, FB(11), White);

            // ── Photo box (right) ──
            const double PW2 = 78, PH2 = 88;
            double px = ML + BW - PW2, py = y + (H - PH2) / 2;
            g.DrawRectangle(Pn(Gold, 1.5), px, py, PW2, PH2);
            // If student photo byte data were available it would be drawn here
            CentreText(g, "Photo", px, py + PH2/2 - 4, PW2, F(8),
                XColor.FromArgb(180, 180, 180));

            return y;   // bottom of header band = new cursor
        }

        // ════════════════════════════════════════════════════════════════════
        //  SECTION 2 — STUDENT INFO TABLE (6 rows × 2 column-pairs)
        // ════════════════════════════════════════════════════════════════════
        private static double DrawInfoTable(XGraphics g, double top, Dictionary<string, string> data)
        {
            const double RH  = 21;
            const double LBW = 90;       // label-cell width inside each column-pair
            double c1w = BW * 0.43;      // total width of left column-pair
            double c2w = BW - c1w;       // total width of right column-pair
            double x1  = ML;
            double x1v = x1 + LBW;
            double x2  = ML + c1w;
            double x2v = x2 + LBW;

            double y = top;

            string[] lblL = { "Student Name:", "Admission No.:", "Class/Form:", "Gender:", "Term:", "Closing Date:" };
            string[] valL = {
                V(data, "NAME"),
                V(data, "STD_ID", ""),
                V(data, "CLASS", ""),
                V(data, "GENDER", ""),
                V(data, "TERMS"),
                V(data, "CLOSING_DATE", "FRIDAY, 1ST AUGUST, 2025")
            };
            string[] lblR = { "Resuming Date:", "Attendance:", "Number On Roll:", "Position in Class:", "Average Score:", "Academic Year" };

            for (int i = 0; i < 6; i++)
            {
                y -= RH;

                // Left label cell
                DrawCell(g, x1,  y, LBW,        RH, White, MGray);
                LeftText(g, lblL[i], x1, y, LBW, RH, FB(8.5), Black);

                // Left value cell
                DrawCell(g, x1v, y, c1w - LBW,  RH, White, MGray);
                CentreTextInCell(g, valL[i], x1v, y, c1w - LBW, RH, F(8.5), Black);

                // Right label cell
                DrawCell(g, x2,  y, LBW,        RH, White, MGray);
                LeftText(g, lblR[i], x2, y, LBW, RH, FB(8.5), Black);

                // Right value cell
                DrawCell(g, x2v, y, c2w - LBW,  RH, White, MGray);

                // Special rendering per row
                switch (i)
                {
                    case 1: // Attendance: "39  Out of  [blank]"
                        string att = V(data, "ATTENDANCE", "—");
                        g.DrawString(att, FB(13), Br(Black), x2v + 10, y + RH - 7);
                        g.DrawString("Out of", F(8), Br(Black), x2v + 35, y + RH - 7);
                        break;
                    case 2: // Number on roll — bold large
                        CentreTextInCell(g, V(data, "NUMBER_ON_ROLL", "—"),
                            x2v, y, c2w - LBW, RH, FB(13), Black);
                        break;
                    case 5: // Academic Year — very large bold
                        CentreTextInCell(g, V(data, "YEAR"),
                            x2v, y, c2w - LBW, RH, FB(15), Black);
                        break;
                    default:
                        string rv = i == 0 ? V(data, "RESUMING_DATE", "MON.,1ST SEPTEMBER,2025")
                                  : i == 3 ? FormatRank(V(data, "TOTAL_RANK"))
                                  : i == 4 ? V(data, "AVG_SCORE", "")
                                  : "";
                        CentreTextInCell(g, rv, x2v, y, c2w - LBW, RH, F(8.5), Black);
                        break;
                }
            }

            return y - 2;
        }

        // ════════════════════════════════════════════════════════════════════
        //  SECTION 3 — SUBJECTS TABLE + GRADING SYSTEM SIDE PANEL
        // ════════════════════════════════════════════════════════════════════
        private static double DrawSubjectsSection(XGraphics g, double top,
            Dictionary<string, string> data)
        {
            // ── Column layout ──
            double tblW  = BW * 0.695;
            double gradW = BW - tblW - 3;
            double gx    = ML + tblW + 3;

            // Subject table columns: Subject | ClassSc | ExamSc | Total | Grade | Pos | Remarks
            double[] cw = { 105, 48, 48, 48, 32, 52, 0 };
            cw[6] = tblW - (cw[0]+cw[1]+cw[2]+cw[3]+cw[4]+cw[5]);
            double[] cx = new double[7];
            cx[0] = ML;
            for (int i = 1; i < 7; i++) cx[i] = cx[i-1] + cw[i-1];

            const double HDR_H  = 32;
            const double ROW_H  = 22;
            const double GROW_H = 18;   // grading side-panel row height

            double y = top;

            // ── TABLE HEADER ──
            y -= HDR_H;

            string[] colHdrs = {
                "Subjects",
                "Class\nScore\n(50%)",
                "Exams\nScore\n(50%)",
                "Total\nScore\n(100%)",
                "Grade",
                "Position\nPer\nSubject",
                "Remarks"
            };
            for (int c = 0; c < 7; c++)
            {
                DrawCell(g, cx[c], y, cw[c], HDR_H, Navy, LGray, 0.3);
                DrawMultilineCenter(g, colHdrs[c], cx[c], y, cw[c], HDR_H, FB(7.5), White);
            }

            // Grading System header (navy bg, gold text)
            DrawCell(g, gx, y, gradW, HDR_H, Navy, null);
            CentreText(g, "Grading System", gx, y + HDR_H/2 - 4, gradW, FB(9), Gold);

            // Grading sub-header (Score / Grade / Remarks)
            double[] gcw = { gradW * 0.28, gradW * 0.21, gradW * 0.51 };
            double[] gcx = { gx, gx + gcw[0], gx + gcw[0] + gcw[1] };
            double gy = y;  // grading cursor tracks independently

            gy -= GROW_H;
            DrawCell(g, gx, gy, gradW, GROW_H, LGray, MGray, 0.3);
            string[] ghdr = { "Score", "Grade", "Remarks" };
            for (int c = 0; c < 3; c++)
                CentreTextInCell(g, ghdr[c], gcx[c], gy, gcw[c], GROW_H, FB(7), Black);
            DrawGradingGridLines(g, gcx, gcw, gy, GROW_H);

            // ── GRADING ROWS DATA ──
            var gradingRows = new[]
            {
                ("80+",   "1", "Advance(A)"),
                ("75-79", "2", "Proficiency(P)"),
                ("70-74", "3", "Approaching\nProficiency(AP)"),
                ("65-69", "4", "Developing"),
                ("64% -", "5", "Beginning"),
            };

            // ── SUBJECTS ──
            var subjects = new[]
            {
                ("Literacy",          "ENG"     ),
                ("Numeracy",          "MATHS"   ),
                ("Pre-Writing",       "SCI"     ),
                ("Pre-Reading",       "SOCIAL"  ),
                ("Creative Arts",     "CRE_ART" ),
                ("OWOP",              "COMP"    ),
                ("Career Technology", "CAREER"  ),
                ("R.M.E.",            "RME"     ),
                ("Ghanaian Language", "GHA_LANG"),
            };

            for (int si = 0; si < subjects.Length; si++)
            {
                y  -= ROW_H;
                var (slabel, skey) = subjects[si];
                bool alt = (si % 2 == 1);
                XColor bg = alt ? LBlue : White;

                // Row background
                g.DrawRectangle(Br(bg), ML, y, tblW, ROW_H);

                // Cell values
                string classSc = V(data, skey + "_CAT",    "");
                string examSc  = V(data, skey + "_EXAM",   "");
                string total   = V(data, skey,              "");
                string grade   = V(data, skey + "_GRADE",  "");
                string pos     = FormatRank(V(data, skey + "_POS", ""));
                string remark  = V(data, skey + "_REMARK", "");

                // Suppress "0" zeros — look cleaner as blank
                if (total  == "0") total  = "";
                if (classSc == "0") classSc = "";
                if (examSc  == "0") examSc  = "";

                string[] vals = { slabel, classSc, examSc, total, grade, pos, remark };
                for (int c = 0; c < 7; c++)
                {
                    g.DrawRectangle(Pn(LGray, 0.4), cx[c], y, cw[c], ROW_H);
                    if (c == 0)
                        LeftText(g, vals[c], cx[c], y, cw[c], ROW_H, FB(8.5), Black);
                    else
                        CentreTextInCell(g, vals[c], cx[c], y, cw[c], ROW_H, F(8), Black);
                }

                // Grading side rows (only first 5 subjects)
                if (si < gradingRows.Length)
                {
                    gy -= GROW_H;
                    XColor gbg = alt ? LBlue : White;
                    g.DrawRectangle(Br(gbg), gx, gy, gradW, GROW_H);
                    var (sc, gr, rem) = gradingRows[si];
                    DrawGradingGridLines(g, gcx, gcw, gy, GROW_H);
                    CentreTextInCell(g, sc, gcx[0], gy, gcw[0], GROW_H, F(7.5), Black);
                    CentreTextInCell(g, gr, gcx[1], gy, gcw[1], GROW_H, F(7.5), Black);
                    DrawMultilineCenter(g, rem, gcx[2], gy, gcw[2], GROW_H, F(6.5), Black);
                }
            }

            // ── TOTALS ROW ──
            y -= ROW_H;
            g.DrawRectangle(Br(LBlue), ML, y, tblW, ROW_H);
            g.DrawRectangle(Pn(MGray, 0.5), ML, y, tblW, ROW_H);

            string totCat  = V(data, "TOTAL_CAT",   "");
            string totExam = V(data, "TOTAL_EXAM",  "");
            string totScr  = V(data, "TOTAL_SCORE", "");
            string subCnt  = V(data, "SUBJECT_COUNT", "");

            string[] totVals = { "Total", totCat, totExam, totScr, subCnt, "", "" };
            for (int c = 0; c < 7; c++)
            {
                g.DrawRectangle(Pn(MGray, 0.4), cx[c], y, cw[c], ROW_H);
                if (c == 0)
                    LeftText(g, totVals[c], cx[c], y, cw[c], ROW_H, FB(9), Black);
                else
                    CentreTextInCell(g, totVals[c], cx[c], y, cw[c], ROW_H, FB(9), Black);
            }

            return y - 3;
        }

        // ════════════════════════════════════════════════════════════════════
        //  SECTION 4 — AFFECTIVE DOMAIN ROWS
        // ════════════════════════════════════════════════════════════════════
        private static double DrawAffectiveDomain(XGraphics g, double top)
        {
            const double RH = 22;
            double y = top;
            string[] rows = {
                "Attitude:", "Interest:", "Conduct:",
                "Class Teacher's Remarks:", "Head Teacher's Remarks:"
            };
            foreach (var lbl in rows)
            {
                y -= RH;
                DrawCell(g, ML, y, BW, RH, White, MGray);
                LeftText(g, lbl, ML, y, BW, RH, FB(8.5), Black);
            }
            return y - 4;
        }

        // ════════════════════════════════════════════════════════════════════
        //  SECTION 5 — PROMOTED TO + DUAL SIGNATURE BOXES
        // ════════════════════════════════════════════════════════════════════
        private static double DrawSignatures(XGraphics g, double top)
        {
            // "Promoted to:" banner
            const double PROM_H = 22;
            double y = top - PROM_H;
            DrawCell(g, ML, y, BW, PROM_H, White, MGray);
            CentreText(g, "Promoted to:", ML, y + PROM_H/2 - 4, BW, FB(9), Black);

            // Signature boxes
            const double SIG_H = 65;
            double sw = BW / 2 - 5;
            y -= SIG_H;
            DrawCell(g, ML,          y, sw, SIG_H, White, MGray);
            DrawCell(g, ML + sw + 10,y, sw, SIG_H, White, MGray);

            // Signature lines
            double lineY = y + 18;
            g.DrawLine(Pn(Black, 0.8), ML + 12,           lineY, ML + sw - 12,       lineY);
            g.DrawLine(Pn(Black, 0.8), ML + sw + 22,       lineY, ML + sw * 2 + 8,   lineY);

            CentreText(g, "School Director's Signature",
                ML,           y + 8, sw,   F(8, true), Black);
            CentreText(g, "Head Teacher's Signature & Stamp",
                ML + sw + 10, y + 8, sw,   F(8, true), Black);

            return y - 3;
        }

        // ════════════════════════════════════════════════════════════════════
        //  SECTION 6 — BOTTOM GRADING BAR
        // ════════════════════════════════════════════════════════════════════
        private static void DrawBottomGradingBar(XGraphics g, double top)
        {
            const double GBR_H  = 15;    // each row height
            const double LBL_W  = 68;    // gold "Grading System" label width
            const double RLBL_W = 42;    // "Score"/"Grade"/"Remarks" label width

            // Gold spanning label (3 rows tall)
            double by = top - 3 * GBR_H;
            g.DrawRectangle(Br(Gold), ML, by, LBL_W, 3 * GBR_H);
            g.DrawRectangle(Pn(MGray, 0.4), ML, by, LBL_W, 3 * GBR_H);
            CentreText(g, "Grading", ML, by + GBR_H * 2 - 2,  LBL_W, FB(7.5), Black);
            CentreText(g, "System",  ML, by + GBR_H     - 2,  LBL_W, FB(7.5), Black);

            string[] rowLabels = { "Score",   "Grade",   "Remarks" };
            string[][] vals    = {
                new[] { "80+",     "75-79",          "70-74",                  "65-69",     "64" },
                new[] { "1",       "2",              "3",                      "4",          "5" },
                new[] { "Advance", "Proficiency(P)", "Approaching Proficiency","Developing", "Beginning" }
            };

            double colW5 = (BW - LBL_W - RLBL_W) / 5.0;

            for (int ri = 0; ri < 3; ri++)
            {
                double ry = top - (ri + 1) * GBR_H;
                // Navy row background
                g.DrawRectangle(Br(NavyMid), ML + LBL_W, ry, BW - LBL_W, GBR_H);
                // Row label cell
                g.DrawRectangle(Pn(LGray, 0.4),
                    ML + LBL_W, ry, RLBL_W, GBR_H);
                CentreTextInCell(g, rowLabels[ri],
                    ML + LBL_W, ry, RLBL_W, GBR_H, FB(7.5), White);
                // 5 value cells
                for (int ci = 0; ci < 5; ci++)
                {
                    double vx = ML + LBL_W + RLBL_W + ci * colW5;
                    g.DrawRectangle(Pn(LGray, 0.4), vx, ry, colW5, GBR_H);
                    double fs = (ri == 2) ? 6.5 : 7.5;
                    CentreTextInCell(g, vals[ri][ci], vx, ry, colW5, GBR_H, F(fs), White);
                }
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  LOW-LEVEL DRAWING HELPERS
        // ════════════════════════════════════════════════════════════════════

        /// Draw a filled+stroked rectangle cell.
        private static void DrawCell(XGraphics g,
            double x, double y, double w, double h,
            XColor bg, XColor? border = null, double bw = 0.5)
        {
            g.DrawRectangle(Br(bg), x, y, w, h);
            if (border.HasValue)
                g.DrawRectangle(Pn(border.Value, bw), x, y, w, h);
        }

        /// Horizontally centred text at a given y baseline.
        private static void CentreText(XGraphics g, string text,
            double x, double y, double w, XFont font, XColor color)
        {
            g.DrawString(text, font, Br(color),
                new XRect(x, y, w, font.Height + 4), XStringFormats.Center);
        }

        /// Text centred both horizontally and vertically inside a cell.
        private static void CentreTextInCell(XGraphics g, string text,
            double x, double y, double w, double h, XFont font, XColor color)
        {
            g.DrawString(text, font, Br(color),
                new XRect(x, y, w, h), XStringFormats.Center);
        }

        /// Left-aligned text vertically centred inside a cell.
        private static void LeftText(XGraphics g, string text,
            double x, double y, double w, double h, XFont font, XColor color)
        {
            var fmt = new XStringFormat
            {
                Alignment     = XStringAlignment.Near,
                LineAlignment = XLineAlignment.Center
            };
            g.DrawString(text, font, Br(color), new XRect(x + 4, y, w - 8, h), fmt);
        }

        /// Multi-line text (split on \n) centred in a cell.
        private static void DrawMultilineCenter(XGraphics g, string text,
            double x, double y, double w, double h, XFont font, XColor color)
        {
            string[] lines = text.Split('\n');
            double lh = h / lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                double ly = y + i * lh;
                CentreTextInCell(g, lines[i].Trim(), x, ly, w, lh, font, color);
            }
        }

        /// Draw vertical grid lines for the 3-column grading side panel.
        private static void DrawGradingGridLines(XGraphics g,
            double[] gcx, double[] gcw, double gy, double gh)
        {
            for (int c = 0; c < 3; c++)
                g.DrawRectangle(Pn(LGray, 0.3), gcx[c], gy, gcw[c], gh);
        }

        // ════════════════════════════════════════════════════════════════════
        //  DATA HELPERS
        // ════════════════════════════════════════════════════════════════════

        private static string V(Dictionary<string, string> d, string key,
            string fallback = "")
        {
            if (d == null) return fallback;
            return d.TryGetValue(key, out string v) && !string.IsNullOrWhiteSpace(v)
                   ? v : fallback;
        }

        private static string FormatRank(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "0") return "";
            if (int.TryParse(value, out int n) && n > 0)
                return n + OrdinalSuffix(n);
            return value;
        }

        private static string OrdinalSuffix(int n)
        {
            switch (n % 100) { case 11: case 12: case 13: return "th"; }
            switch (n % 10)  { case 1: return "st"; case 2: return "nd"; case 3: return "rd"; }
            return "th";
        }
    }
}
