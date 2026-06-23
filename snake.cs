using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace TestProject
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            SQLitePCL.Batteries.Init();

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

        private List<Point> snake = new List<Point>();
        private Point mela;
        private Random rnd = new Random();
        private Point direzione = new Point(0, 0);
        private bool direzioneCambiataInQuestoTick = false;

        private int punteggio = 0;
        private int secondiTrascorsi = 0;
        private int record = 0;

        private int recordDaBattereInQuestaPartita = 0;
        private int frameAnimazioneRecord = 0;
        private bool primoTentativo = true;
        private bool animazioneRecordMostrata = false;

        private string dbPath; 

        private System.Windows.Forms.Timer timerTempo = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer timerMovimento = new System.Windows.Forms.Timer();

        private Font fontTesto = new Font("Segoe UI", 12, FontStyle.Bold);
        private Pen pennaGriglia = new Pen(Color.FromArgb(40, 40, 40));

        public Form1()
        {
            dbPath = $"Data Source={Path.Combine(Application.StartupPath, "snake_data.db")}";

            ClientSize = new Size(colonne * dimensioneCella, (righe * dimensioneCella) + altezzaHUD);
            BackColor = Color.Black;
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Text = "Snake Game - Salvataggio Database";

            Paint += DisegnaGioco;
            KeyDown += GestisciInput;

            timerTempo.Interval = 1000;
            timerTempo.Tick += AggiornaTempo;

            timerMovimento.Interval = 100;
            timerMovimento.Tick += TickMovimento;

            InizializzaDatabase();
            record = CaricaRecord();
            
            if (record > 0)
            {
                primoTentativo = false;
            }

            ResetGioco();
        }

        private void InizializzaDatabase()
        {
            using (var connessione = new SqliteConnection(dbPath))
            {
                connessione.Open();
                using (var comando = connessione.CreateCommand())
                {
                    comando.CommandText = "CREATE TABLE IF NOT EXISTS Salvataggi (Id INTEGER PRIMARY KEY, RecordCorrente INTEGER);";
                    comando.ExecuteNonQuery();

                    comando.CommandText = "SELECT COUNT(*) FROM Salvataggi;";
                    long conteggio = Convert.ToInt64(comando.ExecuteScalar());
                    
                    if (conteggio == 0)
                    {
                        comando.CommandText = "INSERT INTO Salvataggi (Id, RecordCorrente) VALUES (1, 0);";
                        comando.ExecuteNonQuery();
                    }
                }
            }
        }

        private int CaricaRecord()
        {
            using (var connessione = new SqliteConnection(dbPath))
            {
                connessione.Open();
                using (var comando = connessione.CreateCommand())
                {
                    comando.CommandText = "SELECT RecordCorrente FROM Salvataggi WHERE Id = 1;";
                    var risultato = comando.ExecuteScalar();
                    return risultato != null ? Convert.ToInt32(risultato) : 0;
                }
            }
        }

        private void SalvaRecordInDatabase(int nuovoRecord)
        {
            using (var connessione = new SqliteConnection(dbPath))
            {
                connessione.Open();
                using (var comando = connessione.CreateCommand())
                {
                    comando.CommandText = "UPDATE Salvataggi SET RecordCorrente = @record WHERE Id = 1;";
                    comando.Parameters.AddWithValue("@record", nuovoRecord);
                    comando.ExecuteNonQuery();
                }
            }
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
            direzioneCambiataInQuestoTick = false;

            recordDaBattereInQuestaPartita = record;
            frameAnimazioneRecord = 0;
            animazioneRecordMostrata = false;

            timerTempo.Stop();
            timerMovimento.Start();

            GeneraMela();
            Invalidate();
        }

        private void AggiornaTempo(object? sender, EventArgs e)
        {
            secondiTrascorsi++;
            Invalidate();
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

            string messaggioDiFinePartita;
            if (recordDaBattereInQuestaPartita < punteggio)
            {
                messaggioDiFinePartita = $"GAME OVER!\n\n★ NUOVO RECORD OTTENUTO: {punteggio} ★\nTempo resistito: {secondiTrascorsi}s\n\nVuoi fare un altro tentativo?";
            }
            else
            {
                messaggioDiFinePartita = $"GAME OVER!\n\nPunteggio ottenuto: {punteggio}\nRecord attuale: {record}\nTempo resistito: {secondiTrascorsi}s\n\nVuoi fare un altro tentativo?";
            }

            DialogResult risultato = MessageBox.Show(
                messaggioDiFinePartita,
                "Fine Partita",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information
            );

            if (risultato == DialogResult.Yes)
            {
                primoTentativo = false;
                ResetGioco();
            }
            else
            {
                Application.Exit();
            }
        }

        private void GestisciInput(object? sender, KeyEventArgs e)
        {
            if (direzioneCambiataInQuestoTick) return;

            if (direzione.X == 0 && direzione.Y == 0 &&
               (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right))
            {
                if (e.KeyCode == Keys.Left) return;
                timerTempo.Start();
            }

            int prevX = direzione.X;
            int prevY = direzione.Y;

            switch (e.KeyCode)
            {
                case Keys.Up:    if (prevY != 1)  direzione = new Point(0, -1); break;
                case Keys.Down:  if (prevY != -1) direzione = new Point(0, 1);  break;
                case Keys.Left:  if (prevX != 1)  direzione = new Point(-1, 0); break;
                case Keys.Right: if (prevX != -1) direzione = new Point(1, 0);  break;
            }

            if (direzione.X != prevX || direzione.Y != prevY)
            {
                direzioneCambiataInQuestoTick = true;
            }
        }

        private void TickMovimento(object? sender, EventArgs e)
        {
            if (direzione.X == 0 && direzione.Y == 0) return;

            direzioneCambiataInQuestoTick = false;

            if (frameAnimazioneRecord > 0) frameAnimazioneRecord--;

            Point nuovaTesta = new Point(snake[0].X + direzione.X, snake[0].Y + direzione.Y);

            if (nuovaTesta.X < 0 || nuovaTesta.X >= colonne || nuovaTesta.Y < 0 || nuovaTesta.Y >= righe || snake.Contains(nuovaTesta))
            {
                GestisciGameOver();
                return;
            }

            snake.Insert(0, nuovaTesta);

            if (nuovaTesta == mela)
            {
                punteggio += 10;

                if (punteggio > record)
                {
                    record = punteggio;
                    SalvaRecordInDatabase(record);
                }

                if (!primoTentativo && punteggio > recordDaBattereInQuestaPartita && !animazioneRecordMostrata)
                {
                    frameAnimazioneRecord = 24;
                    animazioneRecordMostrata = true;
                }

                GeneraMela();
            }
            else
            {
                snake.RemoveAt(snake.Count - 1);
            }

            Invalidate();
        }

        private void DisegnaGioco(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            g.DrawString($"Punti: {punteggio}", fontTesto, Brushes.White, 10, 10);
            g.DrawString($"Tempo: {secondiTrascorsi}s", fontTesto, Brushes.White, ClientSize.Width - 110, 10);

            if (!primoTentativo)
            {
                if (frameAnimazioneRecord > 0)
                {
                    Brush pennelloRecordAnimato = (frameAnimazioneRecord % 4 < 2) ? Brushes.Gold : Brushes.Magenta;
                    g.DrawString("★ NUOVO RECORD! ★", fontTesto, pennelloRecordAnimato, 160, 10);
                }
                else
                {
                    g.DrawString($"RECORD: {record}", fontTesto, Brushes.Yellow, 180, 10);
                }
            }

            g.DrawLine(Pens.White, 0, altezzaHUD, ClientSize.Width, altezzaHUD);

            for (int i = 0; i <= colonne; i++)
            {
                g.DrawLine(pennaGriglia, i * dimensioneCella, altezzaHUD, i * dimensioneCella, (righe * dimensioneCella) + altezzaHUD);
            }
            for (int j = 0; j <= righe; j++)
            {
                g.DrawLine(pennaGriglia, 0, (j * dimensioneCella) + altezzaHUD, colonne * dimensioneCella, (j * dimensioneCella) + altezzaHUD);
            }

            g.FillRectangle(Brushes.Red, mela.X * dimensioneCella, (mela.Y * dimensioneCella) + altezzaHUD, dimensioneCella - 1, dimensioneCella - 1);

            for (int i = 0; i < snake.Count; i++)
            {
                Brush colorePezzo = Brushes.DarkGreen;

                if (i == 0)
                {
                    colorePezzo = (frameAnimazioneRecord > 0 && frameAnimazioneRecord % 2 == 0) ? Brushes.Cyan : Brushes.Lime;
                }

                g.FillRectangle(colorePezzo, snake[i].X * dimensioneCella, (snake[i].Y * dimensioneCella) + altezzaHUD, dimensioneCella - 1, dimensioneCella - 1);
            }
        }
    }
}