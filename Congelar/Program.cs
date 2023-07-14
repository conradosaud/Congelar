using static System.Windows.Forms.DataFormats;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using static System.Formats.Asn1.AsnWriter;
using System.Windows.Input;

namespace Congelar
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out Point lpPoint);

        static void Main(string[] args)
        {


            // Verifica se há pelo menos dois monitores conectados
            if (Screen.AllScreens.Length < 2)
            {
                Console.WriteLine("Não há dois monitores conectados.");
                return;
            }

            // Obtém as informações dos monitores
            Screen primaryScreen = Screen.PrimaryScreen;
            Screen secondaryScreen = Screen.AllScreens[1];

            // Cria uma janela para exibir o conteúdo do segundo monitor
            Form mirrorForm = new Form();
            mirrorForm.StartPosition = FormStartPosition.Manual;
            mirrorForm.Bounds = secondaryScreen.Bounds;
            mirrorForm.FormBorderStyle = FormBorderStyle.None;
            mirrorForm.WindowState = FormWindowState.Maximized;
            mirrorForm.BackColor = Color.Black;

            // Cria um PictureBox para exibir o conteúdo capturado do primeiro monitor
            PictureBox pictureBox = new PictureBox();
            pictureBox.Dock = DockStyle.Fill;
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            mirrorForm.Controls.Add(pictureBox);

            Label statusLabel = new Label();
            statusLabel.Text = "CONGELADO";
            statusLabel.ForeColor = Color.White;
            statusLabel.BackColor = Color.Black;
            statusLabel.Font = new Font(statusLabel.Font.FontFamily, 30, FontStyle.Bold);
            statusLabel.AutoSize = true;
            statusLabel.Padding = new Padding(10);
            statusLabel.Visible = false; // Inicialmente, o Label não é visível
            statusLabel.Location = new Point(mirrorForm.Width - statusLabel.Width - 200, 10); // Posiciona o Label no canto superior direito
            mirrorForm.Controls.Add(statusLabel);
            statusLabel.BringToFront(); // Garante que o Label seja exibido acima do PictureBox

            Image cursorImage = Image.FromFile("C:/Users/Conrado/Desktop/test/cursor.png");

            mirrorForm.Show();


            bool isPaused = false; // Variável para indicar se a captura está pausada
            bool shouldCapture = true; // Variável para indicar se a captura deve ser feita

            // Loop separado para a captura de tela
            var captureLoop = new System.Threading.Thread(() =>
            {
                using (var graphics = Graphics.FromImage(new Bitmap(primaryScreen.Bounds.Width, primaryScreen.Bounds.Height)))
                {
                    while (shouldCapture)
                    {
                        // Verifica se a tecla F10 foi pressionada
                        if (IsKeyPressed(Keys.F10))
                        {
                            isPaused = !isPaused; // Inverte o estado de pausa
                            mirrorForm.Invoke(new Action(UpdateStatusLabel));
                        }

                        // Executa a captura apenas se a captura não estiver pausada
                        if (!isPaused)
                        {
                            // Captura o conteúdo do primeiro monitor
                            using (var bitmap = new Bitmap(primaryScreen.Bounds.Width, primaryScreen.Bounds.Height))
                            {
                                using (var g = Graphics.FromImage(bitmap))
                                {
                                    g.CopyFromScreen(primaryScreen.Bounds.X, primaryScreen.Bounds.Y, 0, 0, bitmap.Size);

                                    // Obtém a posição atual do cursor no primeiro monitor
                                    GetCursorPos(out Point cursorPos);
                                    Point secondaryCursorPos = new Point(cursorPos.X - primaryScreen.Bounds.X, cursorPos.Y - primaryScreen.Bounds.Y);

                                    // Desenha a imagem do cursor personalizado na posição correspondente no segundo monitor
                                    g.DrawImage(cursorImage, secondaryCursorPos);
                                }

                                // Atualiza o PictureBox com o conteúdo capturado do primeiro monitor
                                pictureBox.Image?.Dispose();
                                pictureBox.Image = new Bitmap(bitmap);
                            }
                        }

                        System.Threading.Thread.Sleep(1); // Aguarda 1ms antes de verificar a tecla F10 novamente e o loop
                    }
                }
            });

            void UpdateStatusLabel()
            {
                statusLabel.Visible = isPaused; // Torna o Label visível se a captura estiver pausada
            }

            mirrorForm.FormClosing += (s, e) =>
            {
                shouldCapture = false; // Ao fechar a janela, interrompe o loop de captura
                captureLoop.Join(); // Aguarda o fim do loop de captura antes de encerrar o programa
            };

            captureLoop.Start(); // Inicia o loop de captura em uma nova thread
            Application.Run(mirrorForm);

            bool IsKeyPressed(Keys key)
            {
                short keyState = GetAsyncKeyState((int)key);
                return (keyState & 0x8000) != 0;
            }

        }

        
        public class MirrorApplicationContext : ApplicationContext
        {
            private Screen primaryScreen;
            private PictureBox pictureBox;            

            public MirrorApplicationContext(Screen primaryScreen, PictureBox pictureBox)
            {
                this.primaryScreen = primaryScreen;
                this.pictureBox = pictureBox;

                Application.Idle += Application_Idle;
            }

            private void Application_Idle(object sender, EventArgs e)
            {

                    // Captura o conteúdo do primeiro monitor
                    using (var bitmap = new Bitmap(primaryScreen.Bounds.Width, primaryScreen.Bounds.Height))
                    {
                        using (var g = Graphics.FromImage(bitmap))
                        {
                            g.CopyFromScreen(primaryScreen.Bounds.X, primaryScreen.Bounds.Y, 0, 0, bitmap.Size);
                        }

                        // Atualiza a imagem do PictureBox com o conteúdo capturado do primeiro monitor
                        pictureBox.Image?.Dispose();

                        pictureBox.Image = new Bitmap(bitmap);


                    }
                
            }

           


        }

    }
}