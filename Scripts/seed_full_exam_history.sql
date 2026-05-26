-- ============================================================
-- Full year exam history for two contrasting students:
--   ABENA MENSAH (9004, BASIC 3)  →  top performer
--   KOJO OWUSU   (9009, BASIC 5)  →  struggling student
-- Each gets 9 subjects × 3 terms (T1, T2, T3 of 2026) = 27 rows
--
-- Score model:
--   cat1, cat2, cat3 : continuous-assessment scores, each /10
--   tl_cat           : cat1 + cat2 + cat3  (/30)
--   exam_score       : end-of-term exam    (/70)
--   gt               : grand total = tl_cat + exam_score  (/100)
--
-- Re-runnable: deletes both students' 2026 records first.
-- ============================================================

USE [Neat_Academy];
GO

SET NOCOUNT ON;
GO

DELETE FROM examss WHERE std_id IN (9004, 9009) AND year = '2026';
GO

-- ---- TERM 1, 2026 ------------------------------------------
INSERT INTO examss (std_id, std_name, std_class, subject, term, year, cat1, cat2, cat3, tl_cat, exam_score, gt, grade, remark) VALUES
(9004,'ABENA MENSAH','BASIC 3','English Language',  'TERM 1','2026', 9, 9, 8, 26, 60, 86,'A','Excellent'),
(9004,'ABENA MENSAH','BASIC 3','Mathematics',       'TERM 1','2026', 8, 9, 9, 26, 58, 84,'A','Very Good'),
(9004,'ABENA MENSAH','BASIC 3','Integrated Science','TERM 1','2026', 9, 8, 9, 26, 57, 83,'A','Very Good'),
(9004,'ABENA MENSAH','BASIC 3','Social Studies',    'TERM 1','2026', 8, 8, 9, 25, 55, 80,'A','Very Good'),
(9004,'ABENA MENSAH','BASIC 3','RME',               'TERM 1','2026', 9, 9, 9, 27, 62, 89,'A','Excellent'),
(9004,'ABENA MENSAH','BASIC 3','Ghanaian Language', 'TERM 1','2026', 8, 8, 8, 24, 52, 76,'B','Good'),
(9004,'ABENA MENSAH','BASIC 3','Creative Arts',     'TERM 1','2026', 9, 9, 8, 26, 59, 85,'A','Excellent'),
(9004,'ABENA MENSAH','BASIC 3','ICT',               'TERM 1','2026', 8, 9, 9, 26, 56, 82,'A','Very Good'),
(9004,'ABENA MENSAH','BASIC 3','Physical Education','TERM 1','2026', 9, 9, 9, 27, 60, 87,'A','Excellent');

-- ---- TERM 2, 2026 ------------------------------------------
INSERT INTO examss (std_id, std_name, std_class, subject, term, year, cat1, cat2, cat3, tl_cat, exam_score, gt, grade, remark) VALUES
(9004,'ABENA MENSAH','BASIC 3','English Language',  'TERM 2','2026', 9, 9, 9, 27, 61, 88,'A','Excellent'),
(9004,'ABENA MENSAH','BASIC 3','Mathematics',       'TERM 2','2026', 9, 9, 8, 26, 59, 85,'A','Excellent'),
(9004,'ABENA MENSAH','BASIC 3','Integrated Science','TERM 2','2026', 8, 9, 9, 26, 58, 84,'A','Very Good'),
(9004,'ABENA MENSAH','BASIC 3','Social Studies',    'TERM 2','2026', 9, 8, 8, 25, 56, 81,'A','Very Good'),
(9004,'ABENA MENSAH','BASIC 3','RME',               'TERM 2','2026', 9, 9, 9, 27, 63, 90,'A','Outstanding'),
(9004,'ABENA MENSAH','BASIC 3','Ghanaian Language', 'TERM 2','2026', 8, 9, 8, 25, 54, 79,'B','Good'),
(9004,'ABENA MENSAH','BASIC 3','Creative Arts',     'TERM 2','2026', 9, 9, 9, 27, 60, 87,'A','Excellent'),
(9004,'ABENA MENSAH','BASIC 3','ICT',               'TERM 2','2026', 9, 8, 9, 26, 58, 84,'A','Very Good'),
(9004,'ABENA MENSAH','BASIC 3','Physical Education','TERM 2','2026', 9, 9, 9, 27, 61, 88,'A','Excellent');

-- ---- TERM 3, 2026 ------------------------------------------
INSERT INTO examss (std_id, std_name, std_class, subject, term, year, cat1, cat2, cat3, tl_cat, exam_score, gt, grade, remark) VALUES
(9004,'ABENA MENSAH','BASIC 3','English Language',  'TERM 3','2026', 9, 9, 9, 27, 63, 90,'A','Outstanding'),
(9004,'ABENA MENSAH','BASIC 3','Mathematics',       'TERM 3','2026', 8, 9, 9, 26, 60, 86,'A','Excellent'),
(9004,'ABENA MENSAH','BASIC 3','Integrated Science','TERM 3','2026', 9, 8, 9, 26, 59, 85,'A','Excellent'),
(9004,'ABENA MENSAH','BASIC 3','Social Studies',    'TERM 3','2026', 9, 9, 8, 26, 57, 83,'A','Very Good'),
(9004,'ABENA MENSAH','BASIC 3','RME',               'TERM 3','2026', 9, 9, 9, 27, 64, 91,'A','Outstanding'),
(9004,'ABENA MENSAH','BASIC 3','Ghanaian Language', 'TERM 3','2026', 8, 9, 9, 26, 55, 81,'A','Very Good'),
(9004,'ABENA MENSAH','BASIC 3','Creative Arts',     'TERM 3','2026', 9, 9, 9, 27, 61, 88,'A','Excellent'),
(9004,'ABENA MENSAH','BASIC 3','ICT',               'TERM 3','2026', 9, 9, 8, 26, 59, 85,'A','Excellent'),
(9004,'ABENA MENSAH','BASIC 3','Physical Education','TERM 3','2026', 9, 9, 9, 27, 62, 89,'A','Excellent');
GO

-- ============================================================
-- KOJO OWUSU (9009, BASIC 5) — struggling student
-- Mostly C/D grades, with PE & Ghanaian Language as strengths.
-- Slight upward trend across terms.
-- ============================================================

-- ---- TERM 1, 2026 ------------------------------------------
INSERT INTO examss (std_id, std_name, std_class, subject, term, year, cat1, cat2, cat3, tl_cat, exam_score, gt, grade, remark) VALUES
(9009,'KOJO OWUSU','BASIC 5','English Language',  'TERM 1','2026', 5, 6, 5, 16, 38, 54,'D','Needs Work'),
(9009,'KOJO OWUSU','BASIC 5','Mathematics',       'TERM 1','2026', 4, 5, 5, 14, 35, 49,'F','Improve'),
(9009,'KOJO OWUSU','BASIC 5','Integrated Science','TERM 1','2026', 6, 6, 6, 18, 42, 60,'C','Satisfactory'),
(9009,'KOJO OWUSU','BASIC 5','Social Studies',    'TERM 1','2026', 5, 6, 5, 16, 40, 56,'D','Needs Work'),
(9009,'KOJO OWUSU','BASIC 5','RME',               'TERM 1','2026', 6, 7, 6, 19, 46, 65,'C','Satisfactory'),
(9009,'KOJO OWUSU','BASIC 5','Ghanaian Language', 'TERM 1','2026', 7, 7, 7, 21, 49, 70,'B','Good'),
(9009,'KOJO OWUSU','BASIC 5','Creative Arts',     'TERM 1','2026', 6, 7, 6, 19, 47, 66,'C','Satisfactory'),
(9009,'KOJO OWUSU','BASIC 5','ICT',               'TERM 1','2026', 5, 6, 5, 16, 41, 57,'D','Needs Work'),
(9009,'KOJO OWUSU','BASIC 5','Physical Education','TERM 1','2026', 8, 8, 7, 23, 53, 76,'B','Good');

-- ---- TERM 2, 2026 ------------------------------------------
INSERT INTO examss (std_id, std_name, std_class, subject, term, year, cat1, cat2, cat3, tl_cat, exam_score, gt, grade, remark) VALUES
(9009,'KOJO OWUSU','BASIC 5','English Language',  'TERM 2','2026', 6, 6, 5, 17, 40, 57,'D','Needs Work'),
(9009,'KOJO OWUSU','BASIC 5','Mathematics',       'TERM 2','2026', 5, 5, 6, 16, 38, 54,'D','Needs Work'),
(9009,'KOJO OWUSU','BASIC 5','Integrated Science','TERM 2','2026', 6, 7, 6, 19, 43, 62,'C','Satisfactory'),
(9009,'KOJO OWUSU','BASIC 5','Social Studies',    'TERM 2','2026', 6, 6, 6, 18, 42, 60,'C','Satisfactory'),
(9009,'KOJO OWUSU','BASIC 5','RME',               'TERM 2','2026', 7, 7, 6, 20, 48, 68,'C','Satisfactory'),
(9009,'KOJO OWUSU','BASIC 5','Ghanaian Language', 'TERM 2','2026', 7, 8, 7, 22, 50, 72,'B','Good'),
(9009,'KOJO OWUSU','BASIC 5','Creative Arts',     'TERM 2','2026', 7, 7, 6, 20, 48, 68,'C','Satisfactory'),
(9009,'KOJO OWUSU','BASIC 5','ICT',               'TERM 2','2026', 6, 6, 6, 18, 43, 61,'C','Satisfactory'),
(9009,'KOJO OWUSU','BASIC 5','Physical Education','TERM 2','2026', 8, 8, 8, 24, 55, 79,'B','Good');

-- ---- TERM 3, 2026 ------------------------------------------
INSERT INTO examss (std_id, std_name, std_class, subject, term, year, cat1, cat2, cat3, tl_cat, exam_score, gt, grade, remark) VALUES
(9009,'KOJO OWUSU','BASIC 5','English Language',  'TERM 3','2026', 6, 7, 6, 19, 42, 61,'C','Satisfactory'),
(9009,'KOJO OWUSU','BASIC 5','Mathematics',       'TERM 3','2026', 6, 6, 6, 18, 40, 58,'D','Needs Work'),
(9009,'KOJO OWUSU','BASIC 5','Integrated Science','TERM 3','2026', 7, 7, 6, 20, 45, 65,'C','Satisfactory'),
(9009,'KOJO OWUSU','BASIC 5','Social Studies',    'TERM 3','2026', 6, 7, 6, 19, 44, 63,'C','Satisfactory'),
(9009,'KOJO OWUSU','BASIC 5','RME',               'TERM 3','2026', 7, 7, 7, 21, 49, 70,'B','Good'),
(9009,'KOJO OWUSU','BASIC 5','Ghanaian Language', 'TERM 3','2026', 8, 8, 7, 23, 52, 75,'B','Good'),
(9009,'KOJO OWUSU','BASIC 5','Creative Arts',     'TERM 3','2026', 7, 7, 7, 21, 49, 70,'B','Good'),
(9009,'KOJO OWUSU','BASIC 5','ICT',               'TERM 3','2026', 6, 7, 6, 19, 45, 64,'C','Satisfactory'),
(9009,'KOJO OWUSU','BASIC 5','Physical Education','TERM 3','2026', 8, 9, 8, 25, 56, 81,'A','Very Good');
GO

-- ---- Verify -------------------------------------------------
SELECT
    std_name AS Student,
    term     AS Term,
    COUNT(*) AS Subjects,
    CAST(AVG(gt) AS DECIMAL(5,1)) AS [Avg Score]
FROM examss
WHERE std_id IN (9004, 9009) AND year = '2026'
GROUP BY std_name, term
ORDER BY std_name, term;

PRINT '';
PRINT 'Two students now each have 27 exam records (9 subjects × 3 terms) for 2026.';
PRINT '  ABENA MENSAH (9004) — top performer (all A grades)';
PRINT '  KOJO OWUSU   (9009) — struggling student (mostly C/D, slight improvement)';
GO
