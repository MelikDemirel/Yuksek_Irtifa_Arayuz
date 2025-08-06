using System;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;

namespace GokhanUI
{
    public partial class MainMenu : Form
    {
        private ArduinoSerialReader _serialReader;
        private GorevYukuSerialReader _gorevYukuReader;
        private DataSender _dataSender;
        private bool _isManualClose = false;
        private float _lastAltitude = 0;
        private float _lastGPSAltitude = 0;
        private float _lastLatitude = 0;
        private float _lastLongitude = 0;
        private float _lastGorevYukuGPSAltitude = 0;
        private float _lastGorevYukuLatitude = 0;
        private float _lastGorevYukuLongitude = 0;
        private float _lastPitch = 0;
        private float _lastRoll = 0;
        private float _lastYaw = 0;
        private float _lastAccelX = 0;
        private float _lastAccelY = 0;
        private float _lastAccelZ = 0;
        private float _lastAngle = 0;
        private byte _lastStatus = 1;

        public MainMenu()
        {
            InitializeComponent();
            InitializeComboBox();
            StartPortMonitoring();
        }

        private void MainMenu_Load(object sender, EventArgs e)
        {
            comboBoxColors.Items.AddRange(new[] { "Koyu Kırmızı", "Lacivert", "Orman Yeşili", "Hardal Sarısı", "Elif", "Gri" });
            this.BackColor = Properties.Settings.Default.BackgroundColor;
            UpdateRocketStatus(0b0000);

            // Gauge ayarları
            gaugeAltitude.Caption = "İrtifa";
            gaugeAltitude.Unit = "m";
            gaugeAltitude.Min = 0;
            gaugeAltitude.Max = 9000;
            gaugeAltitude.Value = 0;
            gaugeAltitude.BarColor = Color.Lime;

            gaugeAngle.Caption = "Açı";
            gaugeAngle.Unit = "°";
            gaugeAngle.Min = 0;
            gaugeAngle.Max = 180;
            gaugeAngle.Value = 0;
            gaugeAngle.BarColor = Color.Yellow;

            gaugeVoltage.Caption = "Voltaj";
            gaugeVoltage.Unit = "mV";
            gaugeVoltage.Min = 0;
            gaugeVoltage.Max = 5000;
            gaugeVoltage.Value = 0;
            gaugeVoltage.BarColor = Color.Cyan;


            // Grafik başlıklarını ve stillerini ayarla
            chart1.ChartAreas[0].BackColor = Color.White;
            chart1.ChartAreas[0].AxisX.Title = "Zaman";
            chart1.ChartAreas[0].AxisX.TitleForeColor = Color.Black;
            chart1.ChartAreas[0].AxisY.Title = "İrtifa (m)";
            chart1.ChartAreas[0].AxisY.TitleForeColor = Color.Black;
            chart1.Series[0].Color = Color.Lime; // Grafik çizgi rengi

            chart2.ChartAreas[0].BackColor = Color.White;
            chart2.ChartAreas[0].AxisX.Title = "Zaman";
            chart2.ChartAreas[0].AxisX.TitleForeColor = Color.Black;
            chart2.ChartAreas[0].AxisY.Title = "Hız (m/s)";
            chart2.ChartAreas[0].AxisY.TitleForeColor = Color.Black;
            chart2.Series[0].Color = Color.Lime;

            chart3.ChartAreas[0].BackColor = Color.White;
            chart3.ChartAreas[0].AxisX.Title = "Zaman";
            chart3.ChartAreas[0].AxisX.TitleForeColor = Color.Black;
            chart3.ChartAreas[0].AxisY.Title = "Sıcaklık (°C)";
            chart3.ChartAreas[0].AxisY.TitleForeColor = Color.Black;
            chart3.Series[0].Color = Color.Lime;
        }
        private void InitializeComboBox()
        {
            string[] ports = SerialPort.GetPortNames();
            comboBox1.Items.AddRange(ports);
            comboBox3.Items.AddRange(ports);
            comboBox6.Items.AddRange(ports);

            string[] baudRates = { "9600", "19200", "115200" };
            comboBox2.Items.AddRange(baudRates);
            comboBox4.Items.AddRange(baudRates);
            comboBox5.Items.AddRange(baudRates);
        }

        private void btnApplyColor_Click(object sender, EventArgs e)
        {
            if (comboBoxColors.SelectedItem == null)
            {
                MessageBox.Show("Lütfen bir renk seçin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Color selectedColor = Color.White;
            switch (comboBoxColors.SelectedItem.ToString())
            {
                case "Koyu Kırmızı": selectedColor = Color.DarkRed; break;
                case "Lacivert": selectedColor = Color.FromArgb(3, 13, 66); break;
                case "Orman Yeşili": selectedColor = Color.ForestGreen; break;
                case "Hardal Sarısı": selectedColor = Color.Goldenrod; break;
                case "Elif": selectedColor = Color.LightPink; break;
                case "Gri": selectedColor = Color.FromArgb(128, 128, 128); break;
            }

            this.BackColor = selectedColor;
            Properties.Settings.Default.BackgroundColor = selectedColor;
            Properties.Settings.Default.Save();
        }

        private void OpenSerialPort()
        {
            if (_serialReader != null && _serialReader.IsOpen) return;

            if (comboBox1.SelectedItem != null && comboBox4.SelectedItem != null)
            {
                string selectedPort = comboBox1.SelectedItem.ToString();

                // Aynı port görev yükü tarafından kullanılıyor mu kontrol et
                if (_gorevYukuReader != null && _gorevYukuReader.IsOpen &&
                    comboBox6.SelectedItem != null &&
                    comboBox6.SelectedItem.ToString() == selectedPort)
                {
                    MessageBox.Show($"Port {selectedPort} şu anda görev yükü tarafından kullanılıyor. " +
                                  "Lütfen farklı bir port seçin veya görev yükü bağlantısını kapatın.",
                                  "Port Kullanımda", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    _serialReader = new ArduinoSerialReader(selectedPort, int.Parse(comboBox4.SelectedItem.ToString()));
                    _serialReader.DataUpdated += UpdateUI;
                    _serialReader.Open();
                    button5.Text = "Kapat";
                    button5.BackColor = Color.Red;
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show($"Port {selectedPort} başka bir uygulama tarafından kullanılıyor. " +
                                  "Portu kullanan uygulamayı kapatın veya farklı bir port seçin.",
                                  "Port Erişim Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Roket bağlantısı kurulurken hata oluştu: {ex.Message}",
                                  "Bağlantı Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Lütfen bir COM portu ve baudrate seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OpenGorevYukuPort()
        {
            if (_gorevYukuReader != null && _gorevYukuReader.IsOpen) return;

            if (comboBox6.SelectedItem != null && comboBox5.SelectedItem != null)
            {
                string selectedPort = comboBox6.SelectedItem.ToString();

                // Aynı port roket tarafından kullanılıyor mu kontrol et
                if (_serialReader != null && _serialReader.IsOpen &&
                    comboBox1.SelectedItem != null &&
                    comboBox1.SelectedItem.ToString() == selectedPort)
                {
                    MessageBox.Show($"Port {selectedPort} şu anda roket tarafından kullanılıyor. " +
                                  "Lütfen farklı bir port seçin veya roket bağlantısını kapatın.",
                                  "Port Kullanımda", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    _gorevYukuReader = new GorevYukuSerialReader(selectedPort, int.Parse(comboBox5.SelectedItem.ToString()));
                    _gorevYukuReader.DataUpdated += UpdateGorevYukuUI;
                    _gorevYukuReader.Open();
                    button2.Text = "Kapat";
                    button2.BackColor = Color.Red;
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show($"Port {selectedPort} başka bir uygulama tarafından kullanılıyor. " +
                                  "Portu kullanan uygulamayı kapatın veya farklı bir port seçin.",
                                  "Port Erişim Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Görev yükü bağlantısı kurulurken hata oluştu: {ex.Message}",
                                  "Bağlantı Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Lütfen bir COM portu ve baudrate seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async void UpdateUI()
        {
            if (InvokeRequired)
            {
                await Task.Run(() => Invoke(new Action(UpdateUI)));
                return;
            }
            if (_serialReader == null || !_serialReader.IsOpen) return;

            try
            {
                UpdateRocketStatus(_serialReader.Status);
                // Gauge güncellemeleri (EKLENECEK)
                gaugeAltitude.Value = _serialReader.Altitude;
                gaugeAngle.Value = _serialReader.Angle;
                gaugeVoltage.Value = _serialReader.Voltage;

                // Gauge'ları yeniden çizmek için Invalidate çağır
                gaugeAltitude.Invalidate();
                gaugeAngle.Invalidate();
                gaugeVoltage.Invalidate();

                // Toplu güncelleme için StringBuilder kullanımı
                var updates = new List<Action>();
                if (txtBoxRoketIrtifa.Text != _serialReader.Altitude.ToString("F2"))
                    updates.Add(() => txtBoxRoketIrtifa.Text = _serialReader.Altitude.ToString("F2"));
                if (txtBoxRoketVoltage.Text != _serialReader.Voltage.ToString())
                    updates.Add(() => txtBoxRoketVoltage.Text = _serialReader.Voltage.ToString());
                if (txtBoxRoketCurrent.Text != _serialReader.Current.ToString())
                    updates.Add(() => txtBoxRoketCurrent.Text = _serialReader.Current.ToString());
                if (txtBoxRoketPitch.Text != _serialReader.Pitch.ToString("F2"))
                    updates.Add(() => txtBoxRoketPitch.Text = _serialReader.Pitch.ToString("F2"));
                if (txtBoxRoketRoll.Text != _serialReader.Roll.ToString("F2"))
                    updates.Add(() => txtBoxRoketRoll.Text = _serialReader.Roll.ToString("F2"));
                if (txtBoxRoketYaw.Text != _serialReader.Yaw.ToString("F2"))
                    updates.Add(() => txtBoxRoketYaw.Text = _serialReader.Yaw.ToString("F2"));
                if (txtBoxRoketIvmeX.Text != _serialReader.AccelX.ToString("F2"))
                    updates.Add(() => txtBoxRoketIvmeX.Text = _serialReader.AccelX.ToString("F2"));
                if (txtBoxRoketIvmeY.Text != _serialReader.AccelY.ToString("F2"))
                    updates.Add(() => txtBoxRoketIvmeY.Text = _serialReader.AccelY.ToString("F2"));
                if (txtBoxRoketIvmeZ.Text != _serialReader.AccelZ.ToString("F2"))
                    updates.Add(() => txtBoxRoketIvmeZ.Text = _serialReader.AccelZ.ToString("F2"));
                if (txtBoxRoketGpsIrtifa.Text != _serialReader.GPSAltitude.ToString("F2"))
                    updates.Add(() => txtBoxRoketGpsIrtifa.Text = _serialReader.GPSAltitude.ToString("F2"));
                if (txtBoxRoketGpsEnlem.Text != _serialReader.Latitude.ToString("F6"))
                    updates.Add(() => txtBoxRoketGpsEnlem.Text = _serialReader.Latitude.ToString("F6"));
                if (txtBoxRoketGpsBoylam.Text != _serialReader.Longitude.ToString("F6"))
                    updates.Add(() => txtBoxRoketGpsBoylam.Text = _serialReader.Longitude.ToString("F6"));
                if (txtRoketTemperature.Text != _serialReader.Temperature.ToString("F2"))
                    updates.Add(() => txtRoketTemperature.Text = _serialReader.Temperature.ToString("F2"));
                if (txtRoketHumidity.Text != _serialReader.Humidity.ToString())
                    updates.Add(() => txtRoketHumidity.Text = _serialReader.Humidity.ToString());
                if (txtBoxRoketAngle.Text != _serialReader.Angle.ToString("F2"))
                    updates.Add(() => txtBoxRoketAngle.Text = _serialReader.Angle.ToString("F2"));
                if (txtBoxRoketChecksum.Text != _serialReader.CRC.ToString())
                    updates.Add(() => txtBoxRoketChecksum.Text = _serialReader.CRC.ToString());
                if (txtBoxRoketVelocity.Text != _serialReader.Velocity.ToString("F2"))
                    updates.Add(() => txtBoxRoketVelocity.Text = _serialReader.Velocity.ToString("F2"));
                if (txtBoxRoketMaxIrtifa.Text != _serialReader.MaxAltitude.ToString())
                    updates.Add(() => txtBoxRoketMaxIrtifa.Text = _serialReader.MaxAltitude.ToString());
                if (txtBoxRoketUyduSayisi.Text != _serialReader.SatelliteCount.ToString())
                    updates.Add(() => txtBoxRoketUyduSayisi.Text = _serialReader.SatelliteCount.ToString());
                if (txtBoxRoketGyroX.Text != _serialReader.GyroX.ToString("F2"))
                    updates.Add(() => txtBoxRoketGyroX.Text = _serialReader.GyroX.ToString("F2"));
                if (txtBoxRoketGyroY.Text != _serialReader.GyroY.ToString("F2"))
                    updates.Add(() => txtBoxRoketGyroY.Text = _serialReader.GyroY.ToString("F2"));
                if (txtBoxRoketGyroZ.Text != _serialReader.GyroZ.ToString("F2"))
                    updates.Add(() => txtBoxRoketGyroZ.Text = _serialReader.GyroZ.ToString("F2"));

                foreach (var update in updates)
                    update();

                _lastAltitude = _serialReader.Altitude;
                _lastGPSAltitude = _serialReader.GPSAltitude;
                _lastLatitude = _serialReader.Latitude;
                _lastLongitude = _serialReader.Longitude;
                _lastPitch = _serialReader.Pitch;
                _lastRoll = _serialReader.Roll;
                _lastYaw = _serialReader.Yaw;
                _lastAccelX = _serialReader.AccelX;
                _lastAccelY = _serialReader.AccelY;
                _lastAccelZ = _serialReader.AccelZ;
                _lastAngle = _serialReader.Angle;
                _lastStatus = _serialReader.Status;

                UpdateCharts();
            }
            catch (Exception ex)
            {
                LogError($"UI güncelleme hatası: {ex.Message}");
            }
        }
        private void UpdateGorevYukuUI()

        {
            if (InvokeRequired) { Invoke(new Action(UpdateGorevYukuUI)); return; }
            if (_gorevYukuReader == null || !_gorevYukuReader.IsOpen) return;


            if (txtBoxGorevYukuHiz.Text != _gorevYukuReader.MaxAltitude.ToString("F2"))// hiza dönece
                txtBoxGorevYukuHiz.Text = _gorevYukuReader.MaxAltitude.ToString("F2");// hiza dönece
            if (txtBoxGorevYukuIrtifa.Text != _gorevYukuReader.Altitude.ToString("F2"))
                txtBoxGorevYukuIrtifa.Text = _gorevYukuReader.Altitude.ToString("F2");
            if (txtBoxGorevYukuGpsIrtifa.Text != _gorevYukuReader.GPSAltitude.ToString("F2"))
                txtBoxGorevYukuGpsIrtifa.Text = _gorevYukuReader.GPSAltitude.ToString("F2");
            _lastGorevYukuGPSAltitude = _gorevYukuReader.GPSAltitude;
            if (txtBoxGorevYukuNem.Text != _gorevYukuReader.Humidity.ToString())
                txtBoxGorevYukuNem.Text = _gorevYukuReader.Humidity.ToString();
            if (txtBoxGorevYukuChecksum.Text != _gorevYukuReader.CRC.ToString())
                txtBoxGorevYukuChecksum.Text = _gorevYukuReader.CRC.ToString();
            if (txtBoxGorevYukuVoltage.Text != _gorevYukuReader.Voltage.ToString())
                txtBoxGorevYukuVoltage.Text = _gorevYukuReader.Voltage.ToString();
            if (txtBoxGorevYukuCurrent.Text != _gorevYukuReader.Current.ToString())
                txtBoxGorevYukuCurrent.Text = _gorevYukuReader.Current.ToString();
            if (txtBoxGorevYukuPitch.Text != _gorevYukuReader.Pitch.ToString("F2"))
                txtBoxGorevYukuPitch.Text = _gorevYukuReader.Pitch.ToString("F2");
            if (txtBoxGorevYukuRoll.Text != _gorevYukuReader.Roll.ToString("F2"))
                txtBoxGorevYukuRoll.Text = _gorevYukuReader.Roll.ToString("F2");
            if (txtBoxGorevYukuYaw.Text != _gorevYukuReader.Yaw.ToString("F2"))
                txtBoxGorevYukuYaw.Text = _gorevYukuReader.Yaw.ToString("F2");
            if (txtBoxGorevYukuIvmeX.Text != _gorevYukuReader.AccelX.ToString("F2"))
                txtBoxGorevYukuIvmeX.Text = _gorevYukuReader.AccelX.ToString("F2");
            if (txtBoxGorevYukuIvmeY.Text != _gorevYukuReader.AccelY.ToString("F2"))
                txtBoxGorevYukuIvmeY.Text = _gorevYukuReader.AccelY.ToString("F2");
            if (txtBoxGorevYukuIvmeZ.Text != _gorevYukuReader.AccelZ.ToString("F2"))
                txtBoxGorevYukuIvmeZ.Text = _gorevYukuReader.AccelZ.ToString("F2");
            if (txtBoxGorevYukuZaman.Text != $"{_gorevYukuReader.Dakika:D2}:{_gorevYukuReader.Saniye:D2}")
                txtBoxGorevYukuZaman.Text = $"{_gorevYukuReader.Dakika:D2}:{_gorevYukuReader.Saniye:D2}";
            if (txtBoxGorevYukuLatitude.Text != _gorevYukuReader.Latitude.ToString("F6"))
                txtBoxGorevYukuLatitude.Text = _gorevYukuReader.Latitude.ToString("F6");
            _lastGorevYukuLatitude = _gorevYukuReader.Latitude;
            if (txtBoxGorevYukuLongitude.Text != _gorevYukuReader.Longitude.ToString("F6"))
                txtBoxGorevYukuLongitude.Text = _gorevYukuReader.Longitude.ToString("F6");
            _lastGorevYukuLongitude = _gorevYukuReader.Longitude;
            if (txtBoxGorevYukuGyroX.Text != _gorevYukuReader.GyroX.ToString("F2"))
                txtBoxGorevYukuGyroX.Text = _gorevYukuReader.GyroX.ToString("F2");
            if (txtBoxGorevYukuGyroY.Text != _gorevYukuReader.GyroY.ToString("F2"))
                txtBoxGorevYukuGyroY.Text = _gorevYukuReader.GyroY.ToString("F2");
            if (txtBoxGorevYukuGyroZ.Text != _gorevYukuReader.GyroZ.ToString("F2"))
                txtBoxGorevYukuGyroZ.Text = _gorevYukuReader.GyroZ.ToString("F2");
            if (txtBoxGorevYukuTemperature.Text != _gorevYukuReader.Temperature.ToString("F2"))
                txtBoxGorevYukuTemperature.Text = _gorevYukuReader.Temperature.ToString("F2");
            if (txtBoxGorevYukuAngle.Text != _gorevYukuReader.Angle.ToString())
                txtBoxGorevYukuAngle.Text = _gorevYukuReader.Angle.ToString();
            if (txtBoxGorevYukuUyduSayisi.Text != _gorevYukuReader.SatelliteCount.ToString())
                txtBoxGorevYukuUyduSayisi.Text = _gorevYukuReader.SatelliteCount.ToString();
        }

        private void UpdateCharts()
        {
            if (_serialReader == null) return;

            chart1.Series[0].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            chart2.Series[0].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            chart3.Series[0].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;

            // İrtifa limiti: -200 ile 9000
            chart1.ChartAreas[0].AxisY.Minimum = -200;
            chart1.ChartAreas[0].AxisY.Maximum = 9000;

            chart1.Series[0].Points.AddY(_serialReader.Altitude);
            chart2.Series[0].Points.AddY(_serialReader.Velocity);
            chart3.Series[0].Points.AddY(_serialReader.Temperature);

            int maxPoints = 10; // Son 10 veriyi göster
            if (chart1.Series[0].Points.Count > maxPoints) chart1.Series[0].Points.RemoveAt(0);
            if (chart2.Series[0].Points.Count > maxPoints) chart2.Series[0].Points.RemoveAt(0);
            if (chart3.Series[0].Points.Count > maxPoints) chart3.Series[0].Points.RemoveAt(0);

            chart1.Invalidate();
            chart2.Invalidate();
            chart3.Invalidate();
        }
        private async void StartPortMonitoring()
        {
            while (true)
            {
                if (_serialReader != null && !_serialReader.IsOpen && !_isManualClose)
                {
                    LogError("Roket portu kapalı, yeniden bağlanma deneniyor.");
                    try { _serialReader.Open(); } catch (Exception ex) { LogError($"Roket portu yeniden bağlanma başarısız: {ex.Message}"); }
                }
                if (_gorevYukuReader != null && !_gorevYukuReader.IsOpen && !_isManualClose)
                {
                    LogError("Görev yükü portu kapalı, yeniden bağlanma deneniyor.");
                    try { _gorevYukuReader.Open(); } catch (Exception ex) { LogError($"Görev yükü portu yeniden bağlanma başarısız: {ex.Message}"); }
                }
                await Task.Delay(5000);
            }
        }

        private void LogError(string message)
        {
            File.AppendAllText("error_log.txt", $"{DateTime.Now}: {message}\n");
            if (InvokeRequired)
            {
                Invoke(new Action(() => MessageBox.Show(message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error)));
            }
            else
            {
                MessageBox.Show(message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (_serialReader != null && _serialReader.IsOpen)
            {
                _isManualClose = true;
                _serialReader.Close();
                _serialReader = null;
                button5.Text = "Bağlan";
                button5.BackColor = Color.Lime;
            }
            else
            {
                _isManualClose = false;
                OpenSerialPort();
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            if (_gorevYukuReader != null && _gorevYukuReader.IsOpen)
            {
                _isManualClose = true;
                _gorevYukuReader.Close();
                _gorevYukuReader = null;
                button2.Text = "Bağlan";
                button2.BackColor = Color.Lime;
            }
            else
            {
                _isManualClose = false;
                OpenGorevYukuPort();
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (_dataSender != null && _dataSender.IsSending)
            {
                _dataSender.StopSending();
                button1.Text = "Başlat";
                button1.BackColor = Color.Lime;
                return;
            }

            if (comboBox3.SelectedItem == null || comboBox2.SelectedItem == null || string.IsNullOrWhiteSpace(textBox1.Text))
            {
                MessageBox.Show("Lütfen port, baudrate ve takım ID bilgilerini girin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_serialReader == null && _gorevYukuReader == null)
            {
                MessageBox.Show("Roket veya görev yükü bağlantısı kurulmadı. Veri gönderimi yapılamaz.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string selectedPort = comboBox3.SelectedItem.ToString();
            int selectedBaudRate = int.Parse(comboBox2.SelectedItem.ToString());

            if (!byte.TryParse(textBox1.Text, out byte teamID))
            {
                MessageBox.Show("Geçerli bir takım ID girin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _dataSender = new DataSender(selectedPort, selectedBaudRate);
            _dataSender.Open();

            button1.Text = "Durdur";
            button1.BackColor = Color.Red;

            while (_dataSender.IsSending)
            {
                // En son verileri kullan
                _dataSender.StartSending(
                    teamID,
                    _serialReader != null ? _lastAltitude : 0,
                    _serialReader != null ? _lastGPSAltitude : 0,
                    _serialReader != null ? _lastLatitude : 0,
                    _serialReader != null ? _lastLongitude : 0,
                    _gorevYukuReader != null ? _lastGorevYukuGPSAltitude : 0,
                    _gorevYukuReader != null ? _lastGorevYukuLatitude : 0,
                    _gorevYukuReader != null ? _lastGorevYukuLongitude : 0,
                    0, 0, 0,
                    _serialReader != null ? _lastPitch : 0,
                    _serialReader != null ? _lastRoll : 0,
                    _serialReader != null ? _lastYaw : 0,
                    _serialReader != null ? _lastAccelX : 0,
                    _serialReader != null ? _lastAccelY : 0,
                    _serialReader != null ? _lastAccelZ : 0,
                    _serialReader != null ? _lastAngle : 0,
                    _serialReader != null ? _lastStatus : (byte)1
                );
                await Task.Delay(1000); // 1 saniye aralıkla gönderim
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var form3d = new Form1(_serialReader);
            form3d.Show();
        }

        private void BtnMap_Click(object sender, EventArgs e)
        {
            var mapForm = new Map();
            mapForm.Show();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (comboBox3.SelectedItem == null || comboBox2.SelectedItem == null || string.IsNullOrWhiteSpace(textBox1.Text))
            {
                MessageBox.Show("Lütfen port, baudrate ve takım ID bilgilerini girin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!byte.TryParse(textBox1.Text, out byte teamID))
            {
                MessageBox.Show("Geçerli bir takım ID girin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string selectedPort = comboBox3.SelectedItem.ToString();
            int selectedBaudRate = int.Parse(comboBox2.SelectedItem.ToString());

            _dataSender = new DataSender(selectedPort, selectedBaudRate);
            _dataSender.Open();

            _dataSender.StartSending(
                teamID,
                100.50f,
                105.25f,
                39.9256f,
                32.8351f,
                102.75f,
                39.9257f,
                32.8352f,
                0, 0, 0,
                10.5f,
                5.2f,
                -3.1f,
                0.5f,
                0.3f,
                9.8f,
                15.0f,
                (byte)1
            );
            _dataSender.Close();

            MessageBox.Show("Dummy veri bir kez gönderildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            comboBox3.Items.Clear();
            comboBox6.Items.Clear();

            string[] ports = SerialPort.GetPortNames();
            comboBox1.Items.AddRange(ports);
            comboBox3.Items.AddRange(ports);
            comboBox6.Items.AddRange(ports);

            MessageBox.Show("COM portları yenilendi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (_serialReader != null && _serialReader.IsOpen)
            {
                _isManualClose = true;
                _serialReader.Close();
                _serialReader = null;
                button5.Text = "Bağlan";
                button5.BackColor = Color.Lime;
            }
            if (_gorevYukuReader != null && _gorevYukuReader.IsOpen)
            {
                _isManualClose = true;
                _gorevYukuReader.Close();
                _gorevYukuReader = null;
                button2.Text = "Bağlan";
                button2.BackColor = Color.Lime;
            }
            if (_dataSender != null && _dataSender.IsSending)
            {
                _dataSender.StopSending();
                button1.Text = "Başlat";
                button1.BackColor = Color.Lime;
            }

            comboBox1.SelectedIndex = -1;
            comboBox2.SelectedIndex = -1;
            comboBox3.SelectedIndex = -1;
            comboBox4.SelectedIndex = -1;
            comboBox5.SelectedIndex = -1;
            comboBox6.SelectedIndex = -1;

            foreach (Control control in this.Controls)
            {
                if (control is TextBox textBox)
                {
                    textBox.Clear();
                }
            }

            chart1.Series[0].Points.Clear();
            chart2.Series[0].Points.Clear();
            chart3.Series[0].Points.Clear();

            Properties.Settings.Default.Save();
            comboBoxColors.SelectedIndex = -1;

            MessageBox.Show("Tüm veriler ve portlar temizlendi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void UpdateRocketStatus(byte status)
        {
            // Panelleri ve etiketleri diziye koy
            var statusPanels = new[] { statusPanel1, statusPanel2, statusPanel3, statusPanel4, statusPanel5, statusPanel6, statusPanel7 };
            var statusLabels = new[] { statusLabel1, statusLabel2, statusLabel3, statusLabel4, statusLabel5, statusLabel6, statusLabel6 };

            // Başlangıçta tüm panelleri kırmızı yap
            for (int i = 0; i < statusPanels.Length; i++)
            {
                statusPanels[i].BackColor = Color.FromArgb(244, 67, 54); // Kırmızı
                statusLabels[i].ForeColor = Color.White;
                statusLabels[i].Font = new Font("Segoe UI", 6F, FontStyle.Regular);
            }

            // Sabit label metinleri
            statusLabel1.Text = "Roket Hazır";
            statusLabel2.Text = "Burnout";
            statusLabel3.Text = "Eşik İrtifası";
            statusLabel4.Text = "Eşik Açısı";
            statusLabel5.Text = "Düşüş";
            statusLabel6.Text = "Sürüklenme Paraşütü";
            statusLabel7.Text = "Ana Paraşüt İrtifası";


            string currentStatusText = "";
            Color currentStatusColor = Color.White;

            // Duruma göre panelleri sırayla yeşil yap
            switch (status)
            {
                case 0b0000: // Başlangıç - Hepsi kırmızı kalsın
                    currentStatusText = "🔴 SİSTEM HAZIRLIK";
                    currentStatusColor = Color.FromArgb(255, 87, 34);
                    break;

                case 0b0001: // Roket Hazır - Sadece 1. panel yeşil
                    statusPanels[0].BackColor = Color.FromArgb(76, 175, 80);
                    statusLabels[0].Font = new Font("Segoe UI", 8F, FontStyle.Bold);
                    currentStatusText = "✅ ROKET HAZIR";
                    currentStatusColor = Color.FromArgb(76, 175, 80);
                    break;

                case 0b0010: // Burnout - 1. ve 2. panel yeşil
                    for (int i = 0; i <= 1; i++)
                    {
                        statusPanels[i].BackColor = Color.FromArgb(76, 175, 80);
                        statusLabels[i].Font = new Font("Segoe UI", 8F, FontStyle.Bold);
                    }
                    currentStatusText = "🔥 BURNOUT";
                    currentStatusColor = Color.FromArgb(33, 150, 243);
                    break;

                case 0b0011: // Eşik İrtifası Aşıldı - 1., 2. ve 3. panel yeşil
                    for (int i = 0; i <= 2; i++)
                    {
                        statusPanels[i].BackColor = Color.FromArgb(76, 175, 80);
                        statusLabels[i].Font = new Font("Segoe UI", 8F, FontStyle.Bold);
                    }
                    currentStatusText = "📈 EŞİK İRTİFASI AŞILDI";
                    currentStatusColor = Color.FromArgb(255, 193, 7);
                    break;

                case 0b0100: // Eşik Açısı Geçildi - 1., 2., 3. ve 4. panel yeşil
                    for (int i = 0; i <= 3; i++)
                    {
                        statusPanels[i].BackColor = Color.FromArgb(76, 175, 80);
                        statusLabels[i].Font = new Font("Segoe UI", 8F, FontStyle.Bold);
                    }
                    currentStatusText = "📐 EŞİK AÇISI GEÇİLDİ";
                    currentStatusColor = Color.FromArgb(156, 39, 176);
                    break;

                case 0b0101: // Roket Düşüşe Geçti - 1., 2., 3., 4. ve 5. panel yeşil
                    for (int i = 0; i <= 4; i++)
                    {
                        statusPanels[i].BackColor = Color.FromArgb(76, 175, 80);
                        statusLabels[i].Font = new Font("Segoe UI", 8F, FontStyle.Bold);
                    }
                    currentStatusText = "⬇️ ROKET DÜŞÜŞE GEÇTİ";
                    currentStatusColor = Color.FromArgb(255, 87, 34);
                    break;

                case 0b0110: // Sürüklenme Paraşütü Açıldı - 1., 2., 3., 4., 5. ve 6. panel yeşil
                    for (int i = 0; i <= 5; i++)
                    {
                        statusPanels[i].BackColor = Color.FromArgb(76, 175, 80);
                        statusLabels[i].Font = new Font("Segoe UI", 8F, FontStyle.Bold);
                    }
                    currentStatusText = "🪂 SÜRÜKLENME PARAŞÜTÜ AÇILDI";
                    currentStatusColor = Color.FromArgb(76, 175, 80);
                    break;

                case 0b0111: // Ana Paraşüt İrtifasına İnildi - 1., 2., 3., 4., 5., 6. ve 7. panel yeşil
                    for (int i = 0; i <= 6; i++)
                    {
                        statusPanels[i].BackColor = Color.FromArgb(76, 175, 80);
                        statusLabels[i].Font = new Font("Segoe UI", 8F, FontStyle.Bold);
                    }
                    currentStatusText = "📏 ANA PARAŞÜT İRTİFASINA İNİLDİ";
                    currentStatusColor = Color.FromArgb(33, 150, 243);
                    break;

                case 0b1000: // Ana Paraşüt Açıldı - Tüm paneller yeşil
                    for (int i = 0; i < statusPanels.Length; i++)
                    {
                        statusPanels[i].BackColor = Color.FromArgb(76, 175, 80);
                        statusLabels[i].Font = new Font("Segoe UI", 8F, FontStyle.Bold);
                    }
                    currentStatusText = "🪂 ANA PARAŞÜT AÇILDI";
                    currentStatusColor = Color.FromArgb(76, 175, 80);
                    break;

                default: // Diğer durumlar - Hepsi kırmızı kalsın
                    currentStatusText = "❓ BİLİNMEYEN DURUM";
                    currentStatusColor = Color.Gray;
                    break;
            }
            // Ana durum etiketini güncelle
            if (currentStatusLabel != null)
            {
                currentStatusLabel.Text = currentStatusText;
                currentStatusLabel.ForeColor = currentStatusColor;
                currentStatusLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                currentStatusLabel.TextAlign = ContentAlignment.MiddleCenter;
            }
        }

      
    }
}