using System;
using System.Drawing;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GokhanUI
{
    public partial class Form1 : Form
    {
        float x = 0, y = 0, z = 0;
        bool cx = false, cy = false, cz = false;
        private ArduinoSerialReader arduinoReader;

        public Form1(ArduinoSerialReader arduinoReader)
        {
            InitializeComponent();
            TimerXYZ.Interval = 100; // Timer interval'ını 100 ms olarak ayarladım
            this.arduinoReader = arduinoReader;

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Arka plan rengini ayarla
            GL.ClearColor(Color.Black);
        }

        private void TimerXYZ_Tick(object sender, EventArgs e)
        {
            if (arduinoReader != null && arduinoReader.IsOpen)
            {
                // Gyro verilerini kullan
                x = arduinoReader.GyroX;
                y = arduinoReader.GyroY;
                z = arduinoReader.GyroZ;

                UpdateLabels(x.ToString("F2"), y.ToString("F2"), z.ToString("F2"));
            }

            glControl1.Invalidate();
        }

        private void glControl1_Paint(object sender, PaintEventArgs e) // çizim kodları burası
        {
            float step = 1.0f;
            float topla = step;
            float radius = 3.0f;
            float dikey1 = radius, dikey2 = -radius;
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Clear(ClearBufferMask.DepthBufferBit);

            Matrix4 perspective = Matrix4.CreatePerspectiveFieldOfView(1.5f, 4 / 3, 1, 10000);//kamera uzaklığı için 1.7f değerini değiştir
            Matrix4 lookat = Matrix4.LookAt(25, 0, 0, 0, 0, 0, 0, 1, 0);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.LoadMatrix(ref perspective);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.LoadMatrix(ref lookat);
            GL.Viewport(0, 0, glControl1.Width, glControl1.Height);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);

            GL.Rotate(x, 1.0, 0.0, 0.0);//ÖNEMLİ
            GL.Rotate(z, 0.0, 1.0, 0.0);
            GL.Rotate(y, 0.0, 0.0, 1.0);


            silindir(step, topla, radius, 7, -11);
            koni(0.01f, 0.01f, 0, 3.0f, 15.0f, 7.0f);
            koni(0.01f, 0.01f, 5, 3.0f, -15, -11.0f);


            GL.Begin(BeginMode.Lines);

            //GL.Color3(Color.FromArgb(250, 0, 0));
            GL.Vertex3(-30.0, 0.0, 0.0);
            GL.Vertex3(30.0, 0.0, 0.0);


            //GL.Color3(Color.FromArgb(0, 0, 0));
            GL.Vertex3(0.0, 30.0, 0.0);
            GL.Vertex3(0.0, -30.0, 0.0);

            //GL.Color3(Color.FromArgb(0, 0, 250));
            GL.Vertex3(0.0, 0.0, 30.0);
            GL.Vertex3(0.0, 0.0, -30.0);


            /////////////
            GL.Begin(BeginMode.Lines);

            for (int i = 0; i < 360; i++)
            {
                double angle = i * Math.PI / 180.0; // Dereceyi radyana dönüştür

                // Her bir çizgiyi çiz
               // GL.Color3(Color.Black);
                GL.Vertex3(Math.Cos(angle) * 30.0, Math.Sin(angle) * 30.0, 0.0);
                GL.Vertex3(Math.Cos(angle) * 31.0, Math.Sin(angle) * 31.0, 0.0);

                // Her bir çizginin üstüne derecesini yazdır
                TextRenderer.DrawText(e.Graphics, i.ToString(), this.Font, new Point((int)(Math.Cos(angle) * 32.0), (int)(Math.Sin(angle) * 32.0)), Color.Black);
            }


            ///////////////



            GL.End();
            //GraphicsContext.CurrentContext.VSync = true;
            glControl1.SwapBuffers();
        } // çizim kodları bitişi
       

        private void ProcessSerialData(string data)
        {
            // Veriyi işle
            if (!string.IsNullOrEmpty(data))
            {
                char dataType = data[0]; // Verinin türünü belirleyen önek

                float value;
                if (float.TryParse(data.Substring(1), out value)) // Verinin geri kalanını sayıya dönüştür
                {
                    // Veri türüne göre uygun değişkene atama yap
                    switch (dataType)
                    {
                        case 'X':
                            x = value;
                            break;
                        case 'Y':
                            y = value;
                            break;
                        case 'Z':
                            z = value;
                            break;
                        default:
                            Console.WriteLine("Geçersiz veri türü: " + dataType);
                            break;
                    }

                    UpdateLabels(x.ToString(), y.ToString(), z.ToString());
                }
            }
            else
            {
                Console.WriteLine("Boş veri alındı.");
            }
        }

        // Method to update the text of lblX, lblY, and lblZ
        private void UpdateLabels(string xText, string yText, string zText)
        {
            if (lblX.InvokeRequired)
            {
                lblX.Invoke(new Action<string, string, string>(UpdateLabels), xText, yText, zText);
            }
            else
            {
                lblX.Text = xText;
                lblY.Text = yText;
                lblZ.Text = zText;
            }
        }
      
        private void btnX_Click(object sender, EventArgs e)
        {
            if (cx && cy && cz)
            {
                // Stop updates
                cx = false;
                cy = false;
                cz = false;
                TimerXYZ.Stop();
            }
            else
            {
                // Start updates
                cx = true;
                cy = true;
                cz = true;
                TimerXYZ.Start();
            }
        }

        private void glControl1_Load(object sender, EventArgs e)
        {
            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.Enable(EnableCap.DepthTest);//sonradan yazdık
        }
        private void silindir(float step, float topla, float radius, float dikey1, float dikey2)// roket gövdesi
        {
            float eski_step = 0.1f;
            GL.Begin(BeginMode.Quads);//Y EKSEN CIZIM DAİRENİN
            while (step <= 360)
            {
                if (step < 45)
                    GL.Color3(Color.FromArgb(255, 0, 0));
                else if (step < 90)
                    GL.Color3(Color.FromArgb(255, 255, 255));
                else if (step < 135)
                    GL.Color3(Color.FromArgb(255, 0, 0));
                else if (step < 180)
                    GL.Color3(Color.FromArgb(255, 255, 255));
                else if (step < 225)
                    GL.Color3(Color.FromArgb(255, 0, 0));
                else if (step < 270)
                    GL.Color3(Color.FromArgb(255, 255, 255));
                else if (step < 315)
                    GL.Color3(Color.FromArgb(255, 0, 0));
                else if (step < 360)
                    GL.Color3(Color.FromArgb(255, 255, 255));


                float ciz1_x = (float)(radius * Math.Cos(step * Math.PI / 180F));
                float ciz1_y = (float)(radius * Math.Sin(step * Math.PI / 180F));
                GL.Vertex3(ciz1_x, dikey1, ciz1_y);

                float ciz2_x = (float)(radius * Math.Cos((step + 2) * Math.PI / 180F));
                float ciz2_y = (float)(radius * Math.Sin((step + 2) * Math.PI / 180F));
                GL.Vertex3(ciz2_x, dikey1, ciz2_y);

                GL.Vertex3(ciz1_x, dikey2, ciz1_y);
                GL.Vertex3(ciz2_x, dikey2, ciz2_y);
                step += topla;
            }
            GL.End();
            GL.Begin(BeginMode.Lines);
            step = eski_step;
            topla = step;
            while (step <= 180)// UST KAPAK
            {
                if (step < 45)
                    GL.Color3(Color.FromArgb(255, 1, 1));
                else if (step < 90)
                    GL.Color3(Color.FromArgb(250, 250, 200));
                else if (step < 135)
                    GL.Color3(Color.FromArgb(255, 1, 1));
                else if (step < 180)
                    GL.Color3(Color.FromArgb(250, 250, 200));
                else if (step < 225)
                    GL.Color3(Color.FromArgb(255, 1, 1));
                else if (step < 270)
                    GL.Color3(Color.FromArgb(250, 250, 200));
                else if (step < 315)
                    GL.Color3(Color.FromArgb(255, 1, 1));
                else if (step < 360)
                    GL.Color3(Color.FromArgb(250, 250, 200));


                float ciz1_x = (float)(radius * Math.Cos(step * Math.PI / 180F));
                float ciz1_y = (float)(radius * Math.Sin(step * Math.PI / 180F));
                GL.Vertex3(ciz1_x, dikey1, ciz1_y);

                float ciz2_x = (float)(radius * Math.Cos((step + 180) * Math.PI / 180F));
                float ciz2_y = (float)(radius * Math.Sin((step + 180) * Math.PI / 180F));
                GL.Vertex3(ciz2_x, dikey1, ciz2_y);

                GL.Vertex3(ciz1_x, dikey1, ciz1_y);
                GL.Vertex3(ciz2_x, dikey1, ciz2_y);
                step += topla;
            }
            step = eski_step;
            topla = step;
            while (step <= 180)//ALT KAPAK
            {
                if (step < 45)
                    GL.Color3(Color.FromArgb(255, 1, 1));
                else if (step < 90)
                    GL.Color3(Color.FromArgb(250, 250, 200));
                else if (step < 135)
                    GL.Color3(Color.FromArgb(255, 1, 1));
                else if (step < 180)
                    GL.Color3(Color.FromArgb(250, 250, 200));
                else if (step < 225)
                    GL.Color3(Color.FromArgb(255, 1, 1));
                else if (step < 270)
                    GL.Color3(Color.FromArgb(250, 250, 200));
                else if (step < 315)
                    GL.Color3(Color.FromArgb(255, 1, 1));
                else if (step < 360)
                    GL.Color3(Color.FromArgb(250, 250, 200));

                float ciz1_x = (float)(radius * Math.Cos(step * Math.PI / 180F));
                float ciz1_y = (float)(radius * Math.Sin(step * Math.PI / 180F));
                GL.Vertex3(ciz1_x, dikey2, ciz1_y);

                float ciz2_x = (float)(radius * Math.Cos((step + 180) * Math.PI / 180F));
                float ciz2_y = (float)(radius * Math.Sin((step + 180) * Math.PI / 180F));
                GL.Vertex3(ciz2_x, dikey2, ciz2_y);

                GL.Vertex3(ciz1_x, dikey2, ciz1_y);
                GL.Vertex3(ciz2_x, dikey2, ciz2_y);
                step += topla;
            }
            GL.End();
        } // roket gövdesi sonu
       
        private void koni(float step, float topla, float radius1, float radius2, float dikey1, float dikey2) // roketin baş ve kıç kısmı
        {

            float eski_step = 0.1f;
           GL.Begin(BeginMode.Lines);//Y EKSEN CIZIM DAİRENİN
            while (step <= 360)
            {
                if (step < 45)
                    GL.Color3(1.0, 1.0, 1.0);
                else if (step < 90)
                    GL.Color3(1.0, 0.0, 0.0);
                else if (step < 135)
                    GL.Color3(1.0, 1.0, 1.0);
                else if (step < 180)
                    GL.Color3(1.0, 0.0, 0.0);
                else if (step < 225)
                    GL.Color3(1.0, 1.0, 1.0);
                else if (step < 270)
                    GL.Color3(1.0, 0.0, 0.0);
                else if (step < 315)
                    GL.Color3(1.0, 1.0, 1.0);
                else if (step < 360)
                    GL.Color3(1.0, 0.0, 0.0);


                float ciz1_x = (float)(radius1 * Math.Cos(step * Math.PI / 180F));
                float ciz1_y = (float)(radius1 * Math.Sin(step * Math.PI / 180F));
                GL.Vertex3(ciz1_x, dikey1, ciz1_y);

                float ciz2_x = (float)(radius2 * Math.Cos(step * Math.PI / 180F));
                float ciz2_y = (float)(radius2 * Math.Sin(step * Math.PI / 180F));
                GL.Vertex3(ciz2_x, dikey2, ciz2_y);
                step += topla;
            }
            GL.End();

            GL.Begin(BeginMode.Lines);
            step = eski_step;
            topla = step;
            while (step <= 180)// UST KAPAK
            {
                if (step < 45)
                    GL.Color3(Color.FromArgb(255, 1, 1));
                else if (step < 90)
                    GL.Color3(Color.FromArgb(250, 250, 200));
                else if (step < 135)
                    GL.Color3(Color.FromArgb(255, 1, 1));
                else if (step < 180)
                    GL.Color3(Color.FromArgb(250, 250, 200));
                else if (step < 225)
                    GL.Color3(Color.FromArgb(255, 1, 1));
                else if (step < 270)
                    GL.Color3(Color.FromArgb(250, 250, 200));
                else if (step < 315)
                    GL.Color3(Color.FromArgb(255, 1, 1));
                else if (step < 360)
                    GL.Color3(Color.FromArgb(250, 250, 200));


                float ciz1_x = (float)(radius2 * Math.Cos(step * Math.PI / 180F));
                float ciz1_y = (float)(radius2 * Math.Sin(step * Math.PI / 180F));
                GL.Vertex3(ciz1_x, dikey2, ciz1_y);

                float ciz2_x = (float)(radius2 * Math.Cos((step + 180) * Math.PI / 180F));
                float ciz2_y = (float)(radius2 * Math.Sin((step + 180) * Math.PI / 180F));
                GL.Vertex3(ciz2_x, dikey2, ciz2_y);

                GL.Vertex3(ciz1_x, dikey2, ciz1_y);
                GL.Vertex3(ciz2_x, dikey2, ciz2_y);
                step += topla;
            }
            step = eski_step;
            topla = step;
            GL.End();
        } 
    }
}
