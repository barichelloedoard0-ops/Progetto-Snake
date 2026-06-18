using System;
using System.Collections.Generic; 
using System.Drawing;
using System.Windows.Forms;

namespace TestProject 
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1()); 
        }
    }
    public class Form1 : Form
    {

        private int dimensioneCella = 20;
        private int colonne = 25;
        private int righe = 25;
        private int altezzaHUD = 40; 

        // Logica Snake, Mela e Movimento
        private List<Point> snake = new List<Point>();
        private Point mela;
        private Random rnd = new Random();
        private Point direzione = new Point(0, 0); 
        
        // Punteggio, Tempo e RECORD
        private int punteggio = 0;
        private int secondiTrascorsi = 0;
        private int record = 0;
        
        // Timer
        private System.Windows.Forms.Timer timerTempo = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer timerMovimento = new System.Windows.Forms.Timer();
        
        private Font fontTesto = new Font("Segoe UI", 12, FontStyle.Bold); 

        public Form1()
        {
            this.ClientSize = new Size(colonne * dimensioneCella, (righe * dimensioneCella) + altezzaHUD);
            this.BackColor = Color.Black;
            this.DoubleBuffered = true; 
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Text = "Snake Game - Record Personale";

            // Eventi
            this.Paint += DisegnaGioco;
            this.KeyDown += GestisciInput;

            // Timer Cronometro
            timerTempo.Interval = 1000;
            timerTempo.Tick += AggiornaTempo;

            // Timer Movimento
            timerMovimento.Interval = 100;
            timerMovimento.Tick += TickMovimento;

            ResetGioco();
        }

        private void ResetGioco()
        {
            snake.Clear();
            int centroX = colonne / 2;
            int centroY = righe / 2;
            snake.Add(new Point(centroX, centroY));     
            snake.Add(new Point(centroX - 1, centroY)); 
            snake.Add(new Point(centroX - 2, centroY)); 
            
            direzione = new Point(0, 0); 
            punteggio = 0;          
            secondiTrascorsi = 0;
            
            timerTempo.Stop();      
            timerMovimento.Start(); 
            
            GeneraMela();
            this.Invalidate(); 
        }

        private void AggiornaTempo(object? sender, EventArgs e)
        {
            secondiTrascorsi++;
            this.Invalidate(); 
        }

        private void GeneraMela()
        {
            int x, y;
            do
            {
                x = rnd.Next(0, colonne);
                y = rnd.Next(0, righe);
            } 
            while (snake.Contains(new Point(x, y))); 

            mela = new Point(x, y);
        }

        private void GestisciGameOver()
        {
            timerMovimento.Stop(); 
            timerTempo.Stop();     

            DialogResult risultato = MessageBox.Show(
                $"GAME OVER!\n\nPunteggio ottenuto: {punteggio}\nRecord attuale: {record}\nTempo resistito: {secondiTrascorsi}s\n\nVuoi fare un altro tentativo?", 
                "Fine Partita", 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Information
            );

            if (risultato == DialogResult.Yes)
            {
                ResetGioco(); 
            }
            else
            {
                Application.Exit(); 
            }
        }

        private void GestisciInput(object? sender, KeyEventArgs e)
        {
            if (direzione.X == 0 && direzione.Y == 0 && 
               (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right))
            {
                timerTempo.Start();
            }

            switch (e.KeyCode)
            {
                case Keys.Up:    if (direzione.Y != 1)  direzione = new Point(0, -1); break;
                case Keys.Down:  if (direzione.Y != -1) direzione = new Point(0, 1);  break;
                case Keys.Left:  if (direzione.X != 1)  direzione = new Point(-1, 0); break;
                case Keys.Right: if (direzione.X != -1) direzione = new Point(1, 0);  break;
            }
        }

        private void TickMovimento(object? sender, EventArgs e)
        {
            if (direzione.X == 0 && direzione.Y == 0) return;

            Point nuovaTesta = new Point(snake[0].X + direzione.X, snake[0].Y + direzione.Y);

            // Collisioni
            if (nuovaTesta.X < 0 || nuovaTesta.X >= colonne || nuovaTesta.Y < 0 || nuovaTesta.Y >= righe || snake.Contains(nuovaTesta))
            {
                GestisciGameOver();
                return;
            }

            snake.Insert(0, nuovaTesta);

            // Controllo Mela
            if (nuovaTesta == mela)
            {
                punteggio += 10; 

                if (punteggio > record)
                {
                    record = punteggio;
                }

                GeneraMela(); 
            }
            else
            {
                snake.RemoveAt(snake.Count - 1);
            }

            this.Invalidate(); 
        }
        private void DisegnaGioco(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            g.DrawString($"Punti: {punteggio}", fontTesto, Brushes.White, 10, 10);
            g.DrawString($"RECORD: {record}", fontTesto, Brushes.Yellow, 180, 10);
            g.DrawString($"Tempo: {secondiTrascorsi}s", fontTesto, Brushes.White, this.ClientSize.Width - 110, 10);
            
            g.DrawLine(Pens.White, 0, altezzaHUD, this.ClientSize.Width, altezzaHUD);

            Pen pennaGriglia = new Pen(Color.FromArgb(40, 40, 40)); 

            // Griglia
            for (int i = 0; i <= colonne; i++)
            {
                g.DrawLine(pennaGriglia, i * dimensioneCella, altezzaHUD, i * dimensioneCella, (righe * dimensioneCella) + altezzaHUD);
            }
            for (int j = 0; j <= righe; j++)
            {
                g.DrawLine(pennaGriglia, 0, (j * dimensioneCella) + altezzaHUD, colonne * dimensioneCella, (j * dimensioneCella) + altezzaHUD);
            }

            // Mela
            g.FillRectangle(Brushes.Red, mela.X * dimensioneCella, (mela.Y * dimensioneCella) + altezzaHUD, dimensioneCella - 1, dimensioneCella - 1);

            // Snake
            for (int i = 0; i < snake.Count; i++)
            {
                Brush colorePezzo = (i == 0) ? Brushes.Lime : Brushes.DarkGreen;
                g.FillRectangle(colorePezzo, snake[i].X * dimensioneCella, (snake[i].Y * dimensioneCella) + altezzaHUD, dimensioneCella - 1, dimensioneCella - 1);
            }
        }
    }
}