#nullable disable
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
    public partial class Form1 : Form
    {
        private int dimensioneCella = 20;
        private int colonne = 25;
        private int righe = 25;
        private int altezzaHUD = 40;

        private List<Point> snake = new List<Point>();
        private Point mela;
        private Random rnd = new Random();
        private Point direzioneCorrente = new Point(0, 0);
        private List<Point> codaInput = new List<Point>();

        private int punteggio = 0;
        private int secondiTrascorsi = 0;
        private int record = 0; 

        private int recordDaBattereInQuestaPartita = 0;
        private int frameAnimazioneRecord = 0;
        private bool primoTentativo = true;
        private bool animazioneRecordMostrata = false;

        private string ultimoNomeGiocatore = "";
        private string dbPath;

        private System.Windows.Forms.Timer timerTempo = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer timerMovimento = new System.Windows.Forms.Timer();

        private Font fontTesto = new Font("Segoe UI", 12, FontStyle.Bold);
        private Pen pennaGriglia = new Pen(Color.FromArgb(40, 40, 40));

        public Form1()
        {
            dbPath = $"Data Source={Path.Combine(Application.StartupPath, "snake_arcade.db")}";

            ClientSize = new Size(colonne * dimensioneCella, (righe * dimensioneCella) + altezzaHUD);
            BackColor = Color.Black;
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Text = "Snake Arcade";

            Paint += DisegnaGioco;
            KeyDown += GestisciInput;

            timerTempo.Interval = 1000;
            timerTempo.Tick += AggiornaTempo;
            timerMovimento.Interval = 100;
            timerMovimento.Tick += TickMovimento;

            InizializzaDatabase();
            record = CaricaPrimoPosto();
            
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
                    comando.CommandText = "CREATE TABLE IF NOT EXISTS Classifica (Id INTEGER PRIMARY KEY AUTOINCREMENT, Nome TEXT, Punteggio INTEGER);";
                    comando.ExecuteNonQuery();

                    comando.CommandText = @"
                        DELETE FROM Classifica 
                        WHERE Id NOT IN (
                            SELECT c.Id FROM Classifica c 
                            WHERE c.Punteggio = (SELECT MAX(Punteggio) FROM Classifica WHERE Nome = c.Nome)
                            GROUP BY c.Nome
                        );";
                    comando.ExecuteNonQuery();

                    comando.CommandText = "SELECT COUNT(*) FROM Classifica;";
                    long conteggio = Convert.ToInt64(comando.ExecuteScalar());
                    
                    if (conteggio == 0)
                    {
                        comando.CommandText = @"
                            INSERT INTO Classifica (Nome, Punteggio) VALUES ('AAA', 50);
                            INSERT INTO Classifica (Nome, Punteggio) VALUES ('BBB', 40);
                            INSERT INTO Classifica (Nome, Punteggio) VALUES ('CCC', 30);
                            INSERT INTO Classifica (Nome, Punteggio) VALUES ('DDD', 20);
                            INSERT INTO Classifica (Nome, Punteggio) VALUES ('EEE', 10);";
                        comando.ExecuteNonQuery();
                    }
                }
            }
        }

        private int CaricaPrimoPosto()
        {
            using (var connessione = new SqliteConnection(dbPath))
            {
                connessione.Open();
                using (var comando = connessione.CreateCommand())
                {
                    comando.CommandText = "SELECT MAX(Punteggio) FROM Classifica;";
                    var risultato = comando.ExecuteScalar();
                    return risultato != DBNull.Value && risultato != null ? Convert.ToInt32(risultato) : 0;
                }
            }
        }

        private void SalvaInClassifica(string nome, int punti)
        {
            using (var connessione = new SqliteConnection(dbPath))
            {
                connessione.Open();
                int recordPrecedente = -1;

                using (var cmdSelect = connessione.CreateCommand())
                {
                    cmdSelect.CommandText = "SELECT MAX(Punteggio) FROM Classifica WHERE Nome = @nome;";
                    cmdSelect.Parameters.AddWithValue("@nome", nome);
                    var result = cmdSelect.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        recordPrecedente = Convert.ToInt32(result);
                    }
                }

                if (recordPrecedente >= 0)
                {
                    if (punti > recordPrecedente)
                    {
                        using (var cmdDelete = connessione.CreateCommand())
                        {
                            cmdDelete.CommandText = "DELETE FROM Classifica WHERE Nome = @nome;";
                            cmdDelete.Parameters.AddWithValue("@nome", nome);
                            cmdDelete.ExecuteNonQuery();
                        }

                        using (var cmdInsert = connessione.CreateCommand())
                        {
                            cmdInsert.CommandText = "INSERT INTO Classifica (Nome, Punteggio) VALUES (@nome, @punti);";
                            cmdInsert.Parameters.AddWithValue("@nome", nome);
                            cmdInsert.Parameters.AddWithValue("@punti", punti);
                            cmdInsert.ExecuteNonQuery();
                        }
                    }
                }
                else
                {
                    using (var cmdInsert = connessione.CreateCommand())
                    {
                        cmdInsert.CommandText = "INSERT INTO Classifica (Nome, Punteggio) VALUES (@nome, @punti);";
                        cmdInsert.Parameters.AddWithValue("@nome", nome);
                        cmdInsert.Parameters.AddWithValue("@punti", punti);
                        cmdInsert.ExecuteNonQuery();
                    }
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

            direzioneCorrente = new Point(0, 0);
            codaInput.Clear(); 
            punteggio = 0;
            secondiTrascorsi = 0;

            record = CaricaPrimoPosto();
            recordDaBattereInQuestaPartita = record;
            frameAnimazioneRecord = 0;
            animazioneRecordMostrata = false;

            timerTempo.Stop();
            timerMovimento.Start();

            GeneraMela();
            Invalidate();
        }

        private void AggiornaTempo(object sender, EventArgs e)
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

            using (InizialiForm popUpIniziali = new InizialiForm(punteggio))
            {
                if (popUpIniziali.ShowDialog() == DialogResult.OK)
                {
                    ultimoNomeGiocatore = popUpIniziali.Iniziali;
                    SalvaInClassifica(ultimoNomeGiocatore, punteggio);
                }
            }

            DialogResult risultato = MessageBox.Show(
                "Vuoi inserire un altro gettone e fare un'altra partita?",
                "Arcade Over",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
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

        private void GestisciInput(object sender, KeyEventArgs e)
        {
            Point ultimaDirezionePianificata = codaInput.Count > 0 ? codaInput[codaInput.Count - 1] : direzioneCorrente;

            if (direzioneCorrente.X == 0 && direzioneCorrente.Y == 0 && codaInput.Count == 0)
            {
                if (e.KeyCode == Keys.C)
                {
                    new ClassificaForm(dbPath, ultimoNomeGiocatore).ShowDialog();
                    return;
                }
            }

            Point nuovaDirezione = ultimaDirezionePianificata;
            switch (e.KeyCode)
            {
                case Keys.Up:    if (ultimaDirezionePianificata.Y != 1)  nuovaDirezione = new Point(0, -1); break;
                case Keys.Down:  if (ultimaDirezionePianificata.Y != -1) nuovaDirezione = new Point(0, 1);  break;
                case Keys.Left:  if (ultimaDirezionePianificata.X != 1)  nuovaDirezione = new Point(-1, 0); break;
                case Keys.Right: if (ultimaDirezionePianificata.X != -1) nuovaDirezione = new Point(1, 0);  break;
                default: return; // Ignora tasti non validi
            }
            if (direzioneCorrente.X == 0 && direzioneCorrente.Y == 0 && (nuovaDirezione.X != 0 || nuovaDirezione.Y != 0))
            {
                if (e.KeyCode == Keys.Left) return; 
                timerTempo.Start();
            }
            if (nuovaDirezione != ultimaDirezionePianificata && codaInput.Count < 2)
            {
                codaInput.Add(nuovaDirezione);
            }
        }

        private void TickMovimento(object sender, EventArgs e)
        {
            if (codaInput.Count > 0)
            {
                direzioneCorrente = codaInput[0];
                codaInput.RemoveAt(0);
            }

            if (direzioneCorrente.X == 0 && direzioneCorrente.Y == 0) return;

            if (frameAnimazioneRecord > 0) frameAnimazioneRecord--;

            Point nuovaTesta = new Point(snake[0].X + direzioneCorrente.X, snake[0].Y + direzioneCorrente.Y);

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

        private void DisegnaGioco(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            g.DrawString($"Punti: {punteggio}", fontTesto, Brushes.White, 10, 10);
            g.DrawString($"Tempo: {secondiTrascorsi}s", fontTesto, Brushes.White, ClientSize.Width - 110, 10);

            if (!primoTentativo)
            {
                if (frameAnimazioneRecord > 0)
                {
                    Brush pennelloRecordAnimato = (frameAnimazioneRecord % 4 < 2) ? Brushes.Gold : Brushes.Magenta;
                    g.DrawString("★ RECORD ASSOLUTO! ★", fontTesto, pennelloRecordAnimato, 150, 10);
                }
                else
                {
                    g.DrawString($"TOP SCORE: {record}", fontTesto, Brushes.Yellow, 175, 10);
                }
            }

            g.DrawLine(Pens.White, 0, altezzaHUD, ClientSize.Width, altezzaHUD);

            if (direzioneCorrente.X == 0 && direzioneCorrente.Y == 0 && codaInput.Count == 0)
            {
                string msg1 = "USA LE FRECCE PER INIZIARE";
                string msg2 = "PREMI 'C' PER LA CLASSIFICA";

                SizeF size1 = g.MeasureString(msg1, fontTesto);
                SizeF size2 = g.MeasureString(msg2, fontTesto);

                int panelWidth = (int)Math.Max(size1.Width, size2.Width) + 40;
                int panelHeight = (int)(size1.Height + size2.Height) + 30;
                int panelX = (ClientSize.Width - panelWidth) / 2;
                int panelY = (ClientSize.Height - panelHeight) / 2 + 20;

                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(200, 0, 0, 0)))
                {
                    g.FillRectangle(bgBrush, panelX, panelY, panelWidth, panelHeight);
                    g.DrawRectangle(Pens.DarkGray, panelX, panelY, panelWidth, panelHeight);
                }

                g.DrawString(msg1, fontTesto, Brushes.Orange, (ClientSize.Width - size1.Width) / 2, panelY + 10);
                g.DrawString(msg2, fontTesto, Brushes.Cyan, (ClientSize.Width - size2.Width) / 2, panelY + 15 + size1.Height);
            }

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
        public class ClassificaForm : Form
    {
        public ClassificaForm(string dbPath, string ultimoNome)
        {
            Text = "SALA GIOCHI - TOP 5";
            ClientSize = new Size(350, 390); 
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.Black;

            Label titolo = new Label() {
                Text = "🏆 HIGHSCORES 🏆",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.Gold,
                Top = 15, Left = 0, Width = 350,
                TextAlign = ContentAlignment.TopCenter
            };
            Controls.Add(titolo);

            using (var connessione = new SqliteConnection(dbPath))
            {
                connessione.Open();
                
                using (var comando = connessione.CreateCommand())
                {
                    comando.CommandText = "SELECT Nome, Punteggio FROM Classifica ORDER BY Punteggio DESC LIMIT 5;";
                    using (var reader = comando.ExecuteReader())
                    {
                        int pos = 1;
                        int startY = 60;
                        while (reader.Read())
                        {
                            string nome = reader.GetString(0);
                            int punti = reader.GetInt32(1);

                            Label lblPos = new Label() { Text = $"{pos}°", Font = new Font("Consolas", 14, FontStyle.Bold), ForeColor = Color.DarkGray, Top = startY, Left = 40, Width = 40 };
                            Label lblNome = new Label() { Text = nome, Font = new Font("Consolas", 14, FontStyle.Bold), ForeColor = Color.Cyan, Top = startY, Left = 110, Width = 60 };
                            Label lblPunti = new Label() { Text = punti.ToString("D5"), Font = new Font("Consolas", 14, FontStyle.Bold), ForeColor = Color.Lime, Top = startY, Left = 190, Width = 100, TextAlign = ContentAlignment.TopRight };

                            if(pos == 1) { lblPos.ForeColor = Color.Gold; lblNome.ForeColor = Color.Gold; lblPunti.ForeColor = Color.Gold; }
                            if(pos == 2) { lblPos.ForeColor = Color.Silver; lblNome.ForeColor = Color.Silver; lblPunti.ForeColor = Color.Silver; }
                            if(pos == 3) { lblPos.ForeColor = Color.FromArgb(205, 127, 50); lblNome.ForeColor = Color.FromArgb(205, 127, 50); lblPunti.ForeColor = Color.FromArgb(205, 127, 50); }

                            Controls.Add(lblPos);
                            Controls.Add(lblNome);
                            Controls.Add(lblPunti);

                            startY += 30;
                            pos++;
                        }
                    }
                }

                Label linea = new Label() {
                    Text = "---------------------------------------------",
                    Font = new Font("Consolas", 10, FontStyle.Bold),
                    ForeColor = Color.FromArgb(60, 60, 60),
                    Top = 220, Left = 20, Width = 310
                };
                Controls.Add(linea);

                if (!string.IsNullOrEmpty(ultimoNome))
                {
                    int posizioneGlobale = 1;
                    int punteggioSalvato = 0;

                    using (var cmdPos = connessione.CreateCommand())
                    {
                        cmdPos.CommandText = @"
                            SELECT 
                                (SELECT COUNT(*) FROM Classifica WHERE Punteggio > c.Punteggio) + 1 AS Rango,
                                Punteggio
                            FROM Classifica c 
                            WHERE Nome = @nome;";
                        cmdPos.Parameters.AddWithValue("@nome", ultimoNome);
                        using (var readerPos = cmdPos.ExecuteReader())
                        {
                            if (readerPos.Read())
                            {
                                posizioneGlobale = readerPos.GetInt32(0);
                                punteggioSalvato = readerPos.GetInt32(1);
                            }
                        }
                    }

                    Label lblTuaPos = new Label() { Text = $"{posizioneGlobale}°", Font = new Font("Consolas", 14, FontStyle.Bold), ForeColor = Color.Orange, Top = 245, Left = 40, Width = 55 };
                    Label lblTuoNome = new Label() { Text = $"{ultimoNome} (TU)", Font = new Font("Consolas", 14, FontStyle.Bold), ForeColor = Color.Orange, Top = 245, Left = 110, Width = 90 };
                    Label lblTuoPunti = new Label() { Text = punteggioSalvato.ToString("D5"), Font = new Font("Consolas", 14, FontStyle.Bold), ForeColor = Color.Orange, Top = 245, Left = 190, Width = 100, TextAlign = ContentAlignment.TopRight };

                    Controls.Add(lblTuaPos);
                    Controls.Add(lblTuoNome);
                    Controls.Add(lblTuoPunti);
                }
                else
                {
                    Label lblNoGiocato = new Label() {
                        Text = "FAI UNA PARTITA PER VEDERE IL TUO RANGO",
                        Font = new Font("Segoe UI", 9, FontStyle.Italic),
                        ForeColor = Color.Gray,
                        Top = 248, Left = 0, Width = 350,
                        TextAlign = ContentAlignment.TopCenter
                    };
                    Controls.Add(lblNoGiocato);
                }
            }

            Button btnChiudi = new Button() {
                Text = "CONTINUA",
                Top = 315, Left = 125, Width = 100, Height = 35,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnChiudi.FlatAppearance.BorderSize = 0;
            btnChiudi.Click += (s, e) => Close();
            Controls.Add(btnChiudi);
        }
    }
    public class InizialiForm : Form
    {
        private string _iniziali = "AAA";
        public string Iniziali { get { return _iniziali; } }
        
        private TextBox txtIniziali;

        public InizialiForm(int punteggio)
        {
            Text = "SALVATAGGIO PERFORMANCE";
            Size = new Size(320, 160);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.Black;
            ForeColor = Color.White;

            Label lbl = new Label() { 
                Text = "PARTITA TERMINATA!\nHai Totalizzato " + punteggio + " punti.\nRegistra le tue iniziali:", 
                Top = 15, Left = 20, Width = 260, Height = 50, 
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.TopCenter
            };

            txtIniziali = new TextBox() { 
                Top = 75, Left = 60, Width = 80, 
                MaxLength = 3, 
                CharacterCasing = CharacterCasing.Upper, 
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.Yellow
            };

            Button btn = new Button() { 
                Text = "OK", 
                Top = 75, Left = 160, Width = 80, Height = 30,
                DialogResult = DialogResult.OK,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.DarkGreen,
                ForeColor = Color.White
            };
            
            Controls.Add(lbl);
            Controls.Add(txtIniziali);
            Controls.Add(btn);
            AcceptButton = btn;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                if (string.IsNullOrWhiteSpace(txtIniziali.Text))
                {
                    MessageBox.Show("Devi inserire almeno una lettera!", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                }
                else
                {
                    _iniziali = txtIniziali.Text.PadRight(3, 'A');
                }
            }
        }
    }
}
