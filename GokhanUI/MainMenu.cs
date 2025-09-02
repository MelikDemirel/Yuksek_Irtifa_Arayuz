using System;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Globalization;


namespace GokhanUI
{
    public partial class MainMenu : Form
    {
        private int _dataCounter = 0; // Veri sayacı

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
        private float _lastGyroX = 0;
        private float _lastGyroZ = 0;
        private float _lastGyroY = 0;
        private float _lastAccelX = 0;
        private float _lastAccelY = 0;
        private float _lastAccelZ = 0;
        private float _lastAngle = 0;
        private byte _lastStatus = 1;
        private Timer _portCheckTimer; // Port durumunu kontrol için timer
        private StreamWriter _logWriter; // Tek log dosyası için StreamWriter


        // MainMenu sınıfının alanlarına EKLE
        private Panel[] _statusPanels;
        private Label[] _statusLabels;
        private bool[] _statusLit = new bool[10];

        public MainMenu()
        {
            InitializeComponent();
            InitializeComboBox();
            InitializePortCheckTimer();
            InitializeLogging(); // Log dosyasını başlat
        }

        private void InitializePortCheckTimer()
        {
            _portCheckTimer = new Timer
            {
                Interval = 1000 // Her 1 saniyede bir kontrol et
            };
            _portCheckTimer.Tick += PortCheckTimer_Tick;
            _portCheckTimer.Start();
        }

        private void PortCheckTimer_Tick(object sender, EventArgs e)
        {
            // Roket portu kontrol
            if (_serialReader != null && !_serialReader.IsOpen && !_isManualClose)
            {
                UpdateButtonState(button5, false);
                UpdateCleanButtonState();
                Console.WriteLine("Roket portu kapandi, UI guncelleniyor.");
            }

            // Görev yükü portu kontrol
            if (_gorevYukuReader != null && !_gorevYukuReader.IsOpen && !_isManualClose)
            {
                UpdateButtonState(button2, false);
                UpdateCleanButtonState();
                Console.WriteLine("Gorev yuku portu kapandi, UI guncelleniyor.");
            }
        }
        private void UpdateButtonState(Button button, bool isOpen)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateButtonState(button, isOpen)));
                return;
            }

            button.Text = isOpen ? "Kapat" : "Bağlan";
            button.BackColor = isOpen ? Color.Red : Color.Lime;
        }

        private void UpdateCleanButtonState()
        {
            bool isActive = (_serialReader != null && _serialReader.IsOpen) ||
                           (_gorevYukuReader != null && _gorevYukuReader.IsOpen) ||
                           (_dataSender != null && _dataSender.IsSending);
            button3.Enabled = !isActive;
        }


        private void MainMenu_Load(object sender, EventArgs e)
        {
            _statusPanels = new[] { statusPanel1, statusPanel2, statusPanel3, statusPanel4, statusPanel5, statusPanel6, statusPanel7, statusPanel8, statusPanel9, statusPanel10 };
            _statusLabels = new[] { statusLabel1, statusLabel2, statusLabel3, statusLabel4, statusLabel5, statusLabel6, statusLabel7, statusLabel8, statusLabel9, statusLabel10 };

            // Sabit başlık metinleri
            statusLabel1.Text = "Roket Hazır";
            statusLabel2.Text = "Uçuş Başladı";
            statusLabel3.Text = "Burnout";
            statusLabel4.Text = "Eşik İrtifası Aşıldı";
            statusLabel5.Text = "Eşik Açısı Geçildi";
            statusLabel6.Text = "Roket Düşüşe Geçti";
            statusLabel7.Text = "Sürüklenme Paraşütü Açıldı";
            statusLabel8.Text = "Ana Paraşüt İrtifasına İnildi";
            statusLabel9.Text = "Ana Paraşüt Açıldı";
            statusLabel10.Text = "Uçuş Bitti";

            // başlangıç reset
            UpdateRocketStatus(0);


            // NOT: Buradaki eski kırmızıya-boyama döngüsünü ve
            // ikinci bir UpdateRocketStatus(0b0000) çağrısını SİL.



            comboBoxColors.Items.AddRange(new[] { "Koyu Kırmızı", "Lacivert", "Orman Yeşili", "Hardal Sarısı", "Elif", "Gri" });
            this.BackColor = Properties.Settings.Default.BackgroundColor;
            UpdateRocketStatus((byte)255); // Tüm paneller kırmızı kalır
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
            gaugeVoltage.Min = 10.5f;
            gaugeVoltage.Max = 12.6f;
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
            chart3.ChartAreas[0].AxisY.Title = "AÇI";
            chart3.ChartAreas[0].AxisY.TitleForeColor = Color.Black;
            chart3.Series[0].Color = Color.Lime;

            //kalınlık
            chart1.Series[0].BorderWidth = 3;
            chart2.Series[0].BorderWidth = 3;
            chart3.Series[0].BorderWidth = 3;

        }

        private void InitializeLogging()
        {
            string logDirectory = Path.Combine(Application.StartupPath, "Logs");
            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string logPath = Path.Combine(logDirectory, $"TelemetryLog_{timestamp}.csv");

            try
            {
                _logWriter = new StreamWriter(logPath, false, new UTF8Encoding(true));
                _logWriter.WriteLine("Timestamp;Roket_İrtifa;Roket_İrtifa_İrtifa;Roket_Gps_Enlem;Roket_Gps_Boylam;Roket_hız;Roket_Açı;Roket_Voltaj;Roket_Güç;Roket_Pitch;Roket_ROLL;Roket_YAW;Roket_İvmeX;Roket_İvmeY;Roket_İvmeZ;Roket_GyroX;Roket_GyroY;Roket_GyroZ;Roket_Sıcaklık;Roket_Nem;Roket_UyduSaysı;Roket_CheckSum;Roket_Bilinmeyen;Roket_Zaman;GorevYuku_Latitude;GorevYuku_Longitude;GorevYuku_Velocity;GorevYuku_Angle;GorevYuku_Voltage;GorevYuku_Current;GorevYuku_Temperature;GorevYuku_Pressure;GorevYuku_MagneticField;GorevYuku_CRC;GorevYuku_Humidity;GorevYuku_gorevdurum");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Log dosyası oluşturulurken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

                if (!SerialPort.GetPortNames().Contains(selectedPort))
                {
                    MessageBox.Show($"Port {selectedPort} artık mevcut değil. Lütfen mevcut portları yenileyin.",
                        "Port Bulunamadı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

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
                    UpdateButtonState(button5, true);
                    _isManualClose = false;
                    UpdateCleanButtonState();
                    Console.WriteLine("Roket portu açıldı.");
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

        private void UpdateUI()
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)UpdateUI);
                return;
            }
            if (_serialReader == null || !_serialReader.IsOpen) return;

            try
            {


                UpdateRocketStatus(_serialReader.Status);

                float gaugeAlt = _serialReader.Altitude;
                float gaugeAng = _serialReader.Angle;
                float gaugeVolt = _serialReader.Voltage;

                if (gaugeAlt < 0.5)
                    gaugeAltitude.Value = 0.0f;
                else if (gaugeAlt > 9000)
                    gaugeAltitude.Value = 9000;
                else
                    gaugeAltitude.Value = gaugeAlt;

                if (gaugeVolt < 10.5)
                    gaugeVoltage.Value = 10.5f;
                else if (gaugeVolt > 12.6)
                    gaugeVoltage.Value = 12.6f;
                else
                    gaugeVoltage.Value = gaugeVolt;

                if (gaugeAng < 0.5)
                    gaugeAngle.Value = 0;
                else if (gaugeAng > 180)
                    gaugeAngle.Value = 180;
                else
                    gaugeAngle.Value = gaugeAng;

                // Gauge güncellemeleri
                gaugeAngle.Text = gaugeAng.ToString("F2") + " °";
                gaugeVoltage.Text = gaugeVolt.ToString("F2") + " V";
                gaugeAltitude.Text = gaugeAlt.ToString("F0") + " m";

                // Gauge'ları yeniden çiz
                gaugeAltitude.Invalidate();
                gaugeAngle.Invalidate();
                gaugeVoltage.Invalidate();

                // Toplu güncelleme için StringBuilder kullanımı
                var updates = new List<Action>();
                if (txtBoxRoketIrtifa.Text != _serialReader.Altitude.ToString("F2"))
                    updates.Add(() => txtBoxRoketIrtifa.Text = _serialReader.Altitude.ToString("F2"));
                if (txtBoxRoketVoltage.Text != _serialReader.Voltage.ToString())
                    updates.Add(() => txtBoxRoketVoltage.Text = _serialReader.Voltage.ToString());
                if (txtBoxRoketCurrent.Text != _serialReader.Current.ToString() + " mW/s")
                    updates.Add(() => txtBoxRoketCurrent.Text = _serialReader.Current.ToString()+ " mW/s");
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
                // Sıcaklık - derece C işareti ile
                if (txtRoketTemperature.Text != $"{_serialReader.Temperature:F2} °C")
                    updates.Add(() => txtRoketTemperature.Text = $"{_serialReader.Temperature:F2} °C");

                if (txtRoketHumidity.Text != _serialReader.Humidity.ToString()+ " %")
                    updates.Add(() => txtRoketHumidity.Text = _serialReader.Humidity.ToString() + " %");
                // Açı - derece işareti ile
                if (txtBoxRoketAngle.Text != $"{_serialReader.Angle:F2} °")
                    updates.Add(() => txtBoxRoketAngle.Text = $"{_serialReader.Angle:F2} °");

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
                if (txtBoxRoketZaman.Text != $"{_serialReader.Dakika:D2}:{_serialReader.Saniye:D2}")
                    txtBoxRoketZaman.Text = $"{_serialReader.Dakika:D2}:{_serialReader.Saniye:D2}";

                foreach (var update in updates)
                    update();

                _lastAltitude = _serialReader.Altitude;
                _lastGPSAltitude = _serialReader.GPSAltitude;
                _lastLatitude = _serialReader.Latitude;
                _lastLongitude = _serialReader.Longitude;
                _lastGyroX = _serialReader.GyroX;
                _lastGyroY = _serialReader.GyroY;
                _lastGyroZ = _serialReader.GyroZ;
                _lastAccelX = _serialReader.AccelX;
                _lastAccelY = _serialReader.AccelY;
                _lastAccelZ = _serialReader.AccelZ;
                _lastAngle = _serialReader.Angle;
               
                LogData();
                UpdateCharts();
            }

            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }


        }
        private void UpdateGorevYukuUI()

        {
            if (InvokeRequired) { Invoke(new Action(UpdateGorevYukuUI)); return; }
            if (_gorevYukuReader == null || !_gorevYukuReader.IsOpen) return;
                 if (txtBoxGorevYukuMaxAltitude.Text != _gorevYukuReader.MaxAltitude.ToString("F2"))// hiza dönece
                txtBoxGorevYukuMaxAltitude.Text = _gorevYukuReader.MaxAltitude.ToString("F2");// hiza dönece
            if (txtBoxGorevYukuHiz.Text != _gorevYukuReader.Velocity.ToString("F2"))// hiza dönece
                txtBoxGorevYukuHiz.Text = _gorevYukuReader.Velocity.ToString("F2");// hiza dönece
            if (txtBoxGorevYukuIrtifa.Text != _gorevYukuReader.Altitude.ToString("F2"))
                txtBoxGorevYukuIrtifa.Text = _gorevYukuReader.Altitude.ToString("F2");
            if (txtBoxGorevYukuGpsIrtifa.Text != _gorevYukuReader.GPSAltitude.ToString("F2"))
                txtBoxGorevYukuGpsIrtifa.Text = _gorevYukuReader.GPSAltitude.ToString("F2");
            _lastGorevYukuGPSAltitude = _gorevYukuReader.GPSAltitude;
            if (txtBoxGorevYukuNem.Text != _gorevYukuReader.Humidity.ToString()+ " %")
                txtBoxGorevYukuNem.Text = _gorevYukuReader.Humidity.ToString()+" %";
            if (txtBoxGorevYukuChecksum.Text != _gorevYukuReader.CRC.ToString())
                txtBoxGorevYukuChecksum.Text = _gorevYukuReader.CRC.ToString();
            if (txtBoxGorevYukuVoltage.Text != _gorevYukuReader.Voltage.ToString())
                txtBoxGorevYukuVoltage.Text = _gorevYukuReader.Voltage.ToString();
            if (txtBoxGorevYukuCurrent.Text != _gorevYukuReader.Current.ToString() + " mW/s")
                txtBoxGorevYukuCurrent.Text = _gorevYukuReader.Current.ToString() + " mW/s";
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
            if (txtBoxGorevYukuTemperature.Text != $"{_gorevYukuReader.Temperature:F2} °C")
                txtBoxGorevYukuTemperature.Text = $"{_gorevYukuReader.Temperature:F2} °C";
            if (txtBoxGorevYukuAngle.Text != $"{_gorevYukuReader.Angle:F2} °")
                txtBoxGorevYukuAngle.Text = $"{_gorevYukuReader.Angle:F2} °";
            if (txtBoxGorevYukuUyduSayisi.Text != _gorevYukuReader.SatelliteCount.ToString())
                txtBoxGorevYukuUyduSayisi.Text = _gorevYukuReader.SatelliteCount.ToString();

            LogData();

        }
        private void UpdateCharts()
        {
           
            if (_serialReader == null) return;

            _dataCounter++;

            chart1.Series[0].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            chart2.Series[0].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            chart3.Series[0].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;

            chart1.ChartAreas[0].AxisY.Minimum = -200;
            chart1.ChartAreas[0].AxisY.Maximum = 9000;
            chart2.ChartAreas[0].AxisY.Minimum = 0;
            chart2.ChartAreas[0].AxisY.Maximum = 600;
            chart3.ChartAreas[0].AxisY.Minimum = 0;
            chart3.ChartAreas[0].AxisY.Maximum = 180;

            int maxPoints = 30;
            int minX = Math.Max(1, _dataCounter - maxPoints + 1);
            int maxX = _dataCounter + 1;

            chart1.ChartAreas[0].AxisX.Minimum = minX;
            chart1.ChartAreas[0].AxisX.Maximum = maxX;
            chart2.ChartAreas[0].AxisX.Minimum = minX;
            chart2.ChartAreas[0].AxisX.Maximum = maxX;
            chart3.ChartAreas[0].AxisX.Minimum = minX;
            chart3.ChartAreas[0].AxisX.Maximum = maxX;

            float irtifa = _serialReader.Altitude;
            if (irtifa < -200) irtifa = -200;
            if (irtifa > 9000) irtifa = 9000;
            chart1.Series[0].Points.AddXY(_dataCounter, irtifa);

            float velocity = _serialReader.Velocity;
            if (velocity < 0) velocity = 0;
            if (velocity > 600) velocity = 600;
            chart2.Series[0].Points.AddXY(_dataCounter, velocity);

            float angle = _serialReader.Angle;
            if (angle < 0) angle = 0;
            if (angle > 180) angle = 180;
            chart3.Series[0].Points.AddXY(_dataCounter, angle);

            if (chart1.Series[0].Points.Count > maxPoints) chart1.Series[0].Points.RemoveAt(0);
            if (chart2.Series[0].Points.Count > maxPoints) chart2.Series[0].Points.RemoveAt(0);
            if (chart3.Series[0].Points.Count > maxPoints) chart3.Series[0].Points.RemoveAt(0);

            chart1.Invalidate();
            chart2.Invalidate();
            chart3.Invalidate();
        }



        private void button5_Click(object sender, EventArgs e)
        {
            if (_serialReader != null && _serialReader.IsOpen)
            {
                _isManualClose = true;
                _serialReader.Close();
                _serialReader = null;
                UpdateButtonState(button5, false);
                UpdateCleanButtonState();
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
                UpdateButtonState(button2, false);
                UpdateCleanButtonState();
            }
            else
            {
                _isManualClose = false;
                OpenGorevYukuPort();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (_dataSender != null && _dataSender.IsSending)
                {
                    _dataSender.StopSending();
                    UpdateButtonState(button1, false);
                    button7.Enabled = true;
                    UpdateCleanButtonState();
                    return;
                }

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

                _dataSender.StartStreamingOnChange(
                    teamID,
                    () => new TelemetrySnapshot
                    {
                        Altitude = _serialReader != null ? _lastAltitude : 0,
                        GPSAltitude = _serialReader != null ? _lastGPSAltitude : 0,
                        Lat = _serialReader != null ? _lastLatitude : 38.3687f,
                        Lon = _serialReader != null ? _lastLongitude : 34.0360f,
                        MissionGpsAlt = _gorevYukuReader != null ? _lastGorevYukuGPSAltitude : 0,
                        MissionLat = _gorevYukuReader != null ? _lastGorevYukuLatitude : 38.3687f,
                        MissionLon = _gorevYukuReader != null ? _lastGorevYukuLongitude : 34.0360f,
                        StageGpsAlt = 0,
                        StageLat = 0,
                        StageLon = 0,
                        GyroX = _serialReader != null ? _lastGyroX : 0,
                        GyroY = _serialReader != null ? _lastGyroY : 0,
                        GyroZ = _serialReader != null ? _lastGyroZ : 0,
                        AccX = _serialReader != null ? _lastAccelX : 0,
                        AccY = _serialReader != null ? _lastAccelY : 0,
                        AccZ = _serialReader != null ? _lastAccelZ : 0,
                        Angle = _serialReader != null ? _lastAngle : 0,
                        Status = _serialReader != null ? _lastStatus : (byte)1
                    },
                    checkIntervalMs: 50,
                    minSendIntervalMs: 40,
                    maxHeartbeatMs: 1000
                );

                UpdateButtonState(button1, true);
                button7.Enabled = false;
                UpdateCleanButtonState();
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show($"Port erişim hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateButtonState(button1, false);
                button7.Enabled = true;
                UpdateCleanButtonState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Canlı veri gönderimi başlatılamadı: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateButtonState(button1, false);
                button7.Enabled = true;
                UpdateCleanButtonState();
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

            _dataSender.SendOnce(
                teamID,
                1f, 1f, 1f, 1f, 1f, 38.3687f, 34.0360f, 0, 0, 0,
                1f, 1f, 1f, 1f, 1f, 1f, 1f, (byte)1,
                closeAfter: true
            );
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


        }
        // MainMenu sınıfının İÇİNE ekleyin (ör. sınıfın en altına, OnFormClosing'den sonra)
        private static string NumOrNA(string text, int decimals = 2)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "N/A";

            // "12.34 °C", "10,50 V", "85 %", "+1.23", "-0,45" gibi değerlerden sayıyı ayıkla
            var sb = new StringBuilder(text.Length);
            foreach (char ch in text)
            {
                if (char.IsDigit(ch) || ch == '-' || ch == '+' || ch == '.' || ch == ',')
                    sb.Append(ch);
            }

            // Virgülü noktaya çevirip InvariantCulture ile parse et
            var cleaned = sb.ToString().Replace(',', '.');

            if (double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                return d.ToString("F" + decimals, CultureInfo.InvariantCulture);

            return "N/A";
        }

        private void LogData()
        {
            try
            {
                if (_logWriter == null) return;

                // UI thread güvenliği: LogData bazen farklı yerlerden çağrılabiliyor
                if (InvokeRequired)
                {
                    BeginInvoke((Action)LogData);
                    return;
                }

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);

                // --- ROKET (TextBox'lardan) ---
                // Not: bazı textbox’lar birim içeriyor -> NumOrNA temizliyor.
                string r_alt = NumOrNA(txtBoxRoketIrtifa.Text);
                string r_gpsAlt = NumOrNA(txtBoxRoketGpsIrtifa.Text);
                string r_lat = NumOrNA(txtBoxRoketGpsEnlem.Text, 6);
                string r_lon = NumOrNA(txtBoxRoketGpsBoylam.Text, 6);
                string r_vel = NumOrNA(txtBoxRoketVelocity.Text);
                string r_angle = NumOrNA(txtBoxRoketAngle.Text);
                string r_volt = NumOrNA(txtBoxRoketVoltage.Text);
                string r_curr = NumOrNA(txtBoxRoketCurrent.Text);
                string r_pitch = NumOrNA(txtBoxRoketPitch.Text);
                string r_roll = NumOrNA(txtBoxRoketRoll.Text);
                string r_yaw = NumOrNA(txtBoxRoketYaw.Text);
                string r_ax = NumOrNA(txtBoxRoketIvmeX.Text);
                string r_ay = NumOrNA(txtBoxRoketIvmeY.Text);
                string r_az = NumOrNA(txtBoxRoketIvmeZ.Text);
                string r_gx = NumOrNA(txtBoxRoketGyroX.Text);
                string r_gy = NumOrNA(txtBoxRoketGyroY.Text);
                string r_gz = NumOrNA(txtBoxRoketGyroZ.Text);
                string r_temp = NumOrNA(txtRoketTemperature.Text);
                string r_hum = NumOrNA(txtRoketHumidity.Text, 0); // yüzde tam sayı geliyor
                string r_sat = NumOrNA(txtBoxRoketUyduSayisi.Text, 0);
                string r_crc = NumOrNA(txtBoxRoketChecksum.Text, 0);
                // Status UI’da label ile görünüyor; en güvenlisi _lastStatus:
                string r_status = _lastStatus.ToString(CultureInfo.InvariantCulture);
                string r_time = string.IsNullOrWhiteSpace(txtBoxRoketZaman.Text) ? "N/A" : txtBoxRoketZaman.Text.Trim();

                // --- GÖREV YÜKÜ (TextBox'lardan) ---
                string g_alt = NumOrNA(txtBoxGorevYukuIrtifa.Text);
                string g_gpsAlt = NumOrNA(txtBoxGorevYukuGpsIrtifa.Text);
                string g_lat = NumOrNA(txtBoxGorevYukuLatitude.Text, 6);
                string g_lon = NumOrNA(txtBoxGorevYukuLongitude.Text, 6);
                string g_vel = NumOrNA(txtBoxGorevYukuHiz.Text);
                string g_angle = NumOrNA(txtBoxGorevYukuAngle.Text);
                string g_volt = NumOrNA(txtBoxGorevYukuVoltage.Text);
                string g_curr = NumOrNA(txtBoxGorevYukuCurrent.Text);
                string g_pitch = NumOrNA(txtBoxGorevYukuPitch.Text);
                string g_roll = NumOrNA(txtBoxGorevYukuRoll.Text);
                string g_yaw = NumOrNA(txtBoxGorevYukuYaw.Text);
                string g_ax = NumOrNA(txtBoxGorevYukuIvmeX.Text);
                string g_ay = NumOrNA(txtBoxGorevYukuIvmeY.Text);
                string g_az = NumOrNA(txtBoxGorevYukuIvmeZ.Text);
                string g_gx = NumOrNA(txtBoxGorevYukuGyroX.Text);
                string g_gy = NumOrNA(txtBoxGorevYukuGyroY.Text);
                string g_gz = NumOrNA(txtBoxGorevYukuGyroZ.Text);
                string g_temp = NumOrNA(txtBoxGorevYukuTemperature.Text);
                string g_hum = NumOrNA(txtBoxGorevYukuNem.Text, 0);
                string g_sat = NumOrNA(txtBoxGorevYukuUyduSayisi.Text, 0);
                string g_crc = NumOrNA(txtBoxGorevYukuChecksum.Text, 0);
                // Görev durumu UI’da ayrı label; sende _gorevYukuReader.Status varsa kullanırdın.
                // Burada UI ile tutarlı olması için, reader yoksa N/A, varsa sayıya çevir:
                string g_status = (_gorevYukuReader != null) ? _gorevYukuReader.Status.ToString(CultureInfo.InvariantCulture) : "N/A";
                string g_time = string.IsNullOrWhiteSpace(txtBoxGorevYukuZaman.Text) ? "N/A" : txtBoxGorevYukuZaman.Text.Trim();

                var line = string.Join(";", new[]
                {
            timestamp,
            // ROKET
            r_alt,r_gpsAlt,r_lat,r_lon,
            r_vel,r_angle,r_volt,r_curr,
            r_pitch,r_roll,r_yaw,
            r_ax,r_ay,r_az,
            r_gx,r_gy,r_gz,
            r_temp,r_hum,r_sat,
            r_crc,r_status,r_time,
            // GÖREV YÜKÜ
            g_alt,g_gpsAlt,g_lat,g_lon,
            g_vel,g_angle,g_volt,g_curr,
            g_pitch,g_roll,g_yaw,
            g_ax,g_ay,g_az,
            g_gx,g_gy,g_gz,
            g_temp,g_hum,g_sat,
            g_crc,g_status,g_time
        });

                _logWriter.WriteLine(line);
                _logWriter.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Log yazma hatası: {ex.Message}");
            }
        }


        private void button3_Click(object sender, EventArgs e)
        {
            if (_serialReader != null && _serialReader.IsOpen)
            {
                _isManualClose = true;
                _serialReader.Close();
                _serialReader.ClearBuffer(); // Buffer temizleme
                _serialReader = null;
                UpdateButtonState(button5, false);
            }
            if (_gorevYukuReader != null && _gorevYukuReader.IsOpen)
            {
                _isManualClose = true;
                _gorevYukuReader.Close();
                _gorevYukuReader.ClearBuffer(); // Buffer temizleme
                _gorevYukuReader = null;
                UpdateButtonState(button2, false);
            }
            if (_dataSender != null && _dataSender.IsSending)
            {
                _dataSender.StopSending();
                UpdateButtonState(button1, false);
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
                    if (textBox.Name.Contains("Voltage"))
                        textBox.Text = "0.00 V";
                    else if (textBox.Name.Contains("Current"))
                        textBox.Text = "0.00 A";
                    else if (textBox.Name.Contains("Temperature"))
                        textBox.Text = "0.00 °C";
                    else if (textBox.Name.Contains("Angle"))
                        textBox.Text = "0.00 °";
                    else if (textBox.Name.Contains("Checksum") || textBox.Name.Contains("Humidity") || textBox.Name.Contains("Nem"))
                        textBox.Text = "0";
                    else
                        textBox.Text = "0.00";
                }
            }

            gaugeAltitude.Value = 0;
            gaugeAltitude.Text = "0 m";
            gaugeAngle.Value = 0;
            gaugeAngle.Text = "0 °";
            gaugeVoltage.Value = 0;
            gaugeVoltage.Text = "0 V";
            gaugeAltitude.Invalidate();
            gaugeAngle.Invalidate();
            gaugeVoltage.Invalidate();

            chart1.Series[0].Points.Clear();
            chart2.Series[0].Points.Clear();
            chart3.Series[0].Points.Clear();
            chart1.Invalidate();
            chart2.Invalidate();
            chart3.Invalidate();

            // button3_Click içinde status reset için sadece ŞUNU bırak
            UpdateRocketStatus(0);

            // 🚀 Roket TextBox'ları
            txtBoxRoketIrtifa.Clear();
            txtBoxRoketVoltage.Clear();
            txtBoxRoketCurrent.Clear();
            txtBoxRoketPitch.Clear();
            txtBoxRoketRoll.Clear();
            txtBoxRoketYaw.Clear();
            txtBoxRoketIvmeX.Clear();
            txtBoxRoketIvmeY.Clear();
            txtBoxRoketIvmeZ.Clear();
            txtBoxRoketGpsIrtifa.Clear();
            txtBoxRoketGpsEnlem.Clear();
            txtBoxRoketGpsBoylam.Clear();
            txtRoketTemperature.Clear();
            txtRoketHumidity.Clear();
            txtBoxRoketAngle.Clear();
            txtBoxRoketChecksum.Clear();
            txtBoxRoketVelocity.Clear();
            txtBoxRoketMaxIrtifa.Clear();
            txtBoxRoketUyduSayisi.Clear();
            txtBoxRoketGyroX.Clear();
            txtBoxRoketGyroY.Clear();
            txtBoxRoketGyroZ.Clear();
            txtBoxRoketZaman.Clear();

            // 📡 Görev Yükü TextBox'ları
            txtBoxGorevYukuHiz.Clear();
            txtBoxGorevYukuIrtifa.Clear();
            txtBoxGorevYukuGpsIrtifa.Clear();
            txtBoxGorevYukuNem.Clear();
            txtBoxGorevYukuChecksum.Clear();
            txtBoxGorevYukuVoltage.Clear();
            txtBoxGorevYukuCurrent.Clear();
            txtBoxGorevYukuPitch.Clear();
            txtBoxGorevYukuRoll.Clear();
            txtBoxGorevYukuYaw.Clear();
            txtBoxGorevYukuIvmeX.Clear();
            txtBoxGorevYukuIvmeY.Clear();
            txtBoxGorevYukuIvmeZ.Clear();
            txtBoxGorevYukuZaman.Clear();
            txtBoxGorevYukuLatitude.Clear();
            txtBoxGorevYukuLongitude.Clear();
            txtBoxGorevYukuGyroX.Clear();
            txtBoxGorevYukuGyroY.Clear();
            txtBoxGorevYukuGyroZ.Clear();
            txtBoxGorevYukuTemperature.Clear();
            txtBoxGorevYukuAngle.Clear();
            txtBoxGorevYukuUyduSayisi.Clear();

            _dataCounter = 0;

            Properties.Settings.Default.Save();
            comboBoxColors.SelectedIndex = -1;

        }
        private void UpdateRocketStatus(byte status)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<byte>(UpdateRocketStatus), status);
                return;
            }

            // 10 panel/label referansı
            var statusPanels = new[] { statusPanel1, statusPanel2, statusPanel3, statusPanel4, statusPanel5, statusPanel6, statusPanel7, statusPanel8, statusPanel9, statusPanel10 };
            var statusLabels = new[] { statusLabel1, statusLabel2, statusLabel3, statusLabel4, statusLabel5, statusLabel6, statusLabel7, statusLabel8, statusLabel9, statusLabel10 };

            // Sabit başlık metinleri (sabit kalsın diye burada bir kez set etmek sorun değil)
            statusLabel1.Text = "Roket Hazır";
            statusLabel2.Text = "Uçuş Başladı";
            statusLabel3.Text = "Burnout";
            statusLabel4.Text = "Eşik İrtifası Aşıldı";
            statusLabel5.Text = "Eşik Açısı Geçildi";
            statusLabel6.Text = "Roket Düşüşe Geçti";
            statusLabel7.Text = "Sürüklenme Paraşütü Açıldı";
            statusLabel8.Text = "Ana Paraşüt İrtifasına İnildi";
            statusLabel9.Text = "Ana Paraşüt Açıldı";
            statusLabel10.Text = "Uçuş Bitti";

            // Renk ve font sabitleri
            Color red = Color.FromArgb(244, 67, 54);
            Color green = Color.FromArgb(76, 175, 80);
            Font normal = new Font("Segoe UI", 10F, FontStyle.Regular);
            Font bold = new Font("Segoe UI", 12F, FontStyle.Bold);

            // 0 → TAM RESET (hepsi kırmızı)
            if (status == 0)
            {
                for (int i = 0; i < statusPanels.Length; i++)
                {
                    statusPanels[i].BackColor = red;
                    statusLabels[i].ForeColor = Color.White;
                    statusLabels[i].Font = normal;
                }

                if (currentStatusLabel != null)
                {
                    currentStatusLabel.Text = "SİSTEM HAZIRLIK";
                    currentStatusLabel.ForeColor = Color.Orange;
                    currentStatusLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                    currentStatusLabel.TextAlign = ContentAlignment.MiddleCenter;
                }

                _lastStatus = 0;
                return;
            }

            // 255 → BAĞLANTI YOK (reset + farklı mesaj)
            if (status == 255)
            {
                for (int i = 0; i < statusPanels.Length; i++)
                {
                    statusPanels[i].BackColor = red;
                    statusLabels[i].ForeColor = Color.White;
                    statusLabels[i].Font = normal;
                }

                if (currentStatusLabel != null)
                {
                    currentStatusLabel.Text = "❌ BAĞLANTI YOK";
                    currentStatusLabel.ForeColor = red;
                    currentStatusLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                    currentStatusLabel.TextAlign = ContentAlignment.MiddleCenter;
                }

                _lastStatus = 0;
                return;
            }

            // 1..10 → sadece ilgili index’i yeşil/bold yap (KÜMÜLATİF: öncekilere dokunma)
            int idx = status - 1;
            if (idx < 0 || idx >= statusPanels.Length)
            {
                if (currentStatusLabel != null)
                {
                    currentStatusLabel.Text = "❓ BİLİNMEYEN DURUM";
                    currentStatusLabel.ForeColor = Color.Gray;
                    currentStatusLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                    currentStatusLabel.TextAlign = ContentAlignment.MiddleCenter;
                }
                return;
            }

            statusPanels[idx].BackColor = green;
            statusLabels[idx].ForeColor = Color.White;
            statusLabels[idx].Font = bold;

            // Alt “mevcut durum” etiketi
            if (currentStatusLabel != null)
            {
                string txt; Color col;
                switch (status)
                {
                    case 1: txt = " ROKET HAZIR"; col = green; break;
                    case 2: txt = " UÇUŞ BAŞLADI"; col = Color.FromArgb(33, 150, 243); break;
                    case 3: txt = " BURNOUT"; col = Color.FromArgb(255, 193, 7); break;
                    case 4: txt = " EŞİK İRTİFASI AŞILDI"; col = Color.Purple; break;
                    case 5: txt = " EŞİK AÇISI GEÇİLDİ"; col = Color.FromArgb(255, 87, 34); break;
                    case 6: txt = " ROKET DÜŞÜŞE GEÇTİ"; col = green; break;
                    case 7: txt = " SÜRÜKLENME PARAŞÜTÜ AÇILDI"; col = Color.FromArgb(33, 150, 243); break;
                    case 8: txt = " ANA PARAŞÜT İRTİFASINA İNİLDİ"; col = Color.FromArgb(255, 193, 7); break;
                    case 9: txt = " ANA PARAŞÜT AÇILDI"; col = green; break;
                    case 10: txt = " UÇUŞ BİTTİ"; col = Color.FromArgb(0, 150, 136); break;
                    default: txt = "❓ BİLİNMEYEN DURUM"; col = Color.Gray; break;
                }

                currentStatusLabel.Text = txt;
                currentStatusLabel.ForeColor = col;
                currentStatusLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                currentStatusLabel.TextAlign = ContentAlignment.MiddleCenter;
            }

            // _lastStatus eşleme kuralı:
            // 1..6  → 1  (Sürüklenme paraşütüne kadar)
            // 7..8  → 2  (Sürüklenme + ana paraşüt irtifasına iniş)
            // 9..10 → 4  (Ana paraşüt açıldı ve uçuş bitti)
            if (status >= 1 && status <= 6)
                _lastStatus = 1;
            else if (status == 7 || status == 8)
                _lastStatus = 2;
            else if (status >= 9 && status <= 10)
                _lastStatus = 4;
            else
                _lastStatus = 0; // güvenlik: aralık dışı

        }



        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            _logWriter?.Close();
            _logWriter?.Dispose();
        }

    }
}