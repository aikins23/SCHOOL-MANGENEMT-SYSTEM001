using System;
using System.Drawing;

class Program {
    static void Main() {
        // Create a 1200x800 background image with a school-themed design
        Bitmap bmp = new Bitmap(1200, 800);
        Graphics g = Graphics.FromImage(bmp);
        
        // Light blue sky color
        Color skyBlue = Color.FromArgb(135, 206, 235);
        g.Clear(skyBlue);
        
        // Add a subtle gradient effect
        using (LinearGradientBrush brush = new LinearGradientBrush(
            new Point(0, 0), new Point(0, 800),
            Color.FromArgb(135, 206, 235),
            Color.FromArgb(200, 230, 255)))
        {
            g.FillRectangle(brush, 0, 0, 1200, 800);
        }
        
        // Add subtle school-themed decorations
        using (Pen pen = new Pen(Color.FromArgb(100, 149, 237), 2))
        {
            // Draw some subtle lines
            for (int i = 0; i < 1200; i += 150)
            {
                g.DrawLine(pen, i, 0, i, 50);
            }
        }
        
        // Save the image
        bmp.Save(@"C:\Users\DELL\Downloads\New folder (2)\IPMC PROJECT BUABENG EMMANUEL AIKINS (1)\BUABENG EMMANUEL AIKINS - Copy\Resources\school_background.png");
        g.Dispose();
        bmp.Dispose();
        
        Console.WriteLine("Background image created successfully!");
    }
}
