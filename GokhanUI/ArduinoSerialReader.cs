using System;
using System.IO;
using System.IO.Ports;
using System.Threading.Tasks;

namespace GokhanUI
{
    public class ArduinoSerialReader : IDisposable
    {
        private SerialPort _serialPort;
        public event Action DataUpdated;
        private DateTime _lastDataReceived = DateTime.Now;
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);
        private const int PacketSize = 64;

        public byte Status { get; private set; }//
        public ushort Voltage { get; private set; }//
        public ushort Current { get; private set; }//
        public float Pitch { get; private set; }//
        public float Roll { get; private set; }//
        public float Yaw { get; private set; }//
        public float AccelX { get; private set; }//
        public float AccelY { get; private set; }//
        public float AccelZ { get; private set; }//
        public float Angle { get; private set; }//
        public float Altitude { get; private set; }//
        public float GPSAltitude { get; private set; }//
        public float Latitude { get; private set; }//
        public float Longitude { get; private set; }//
        public float GyroX { get; private set; }//
        public float GyroY { get; private set; }//
        public float GyroZ { get; private set; }//
        public float Temperature { get; private set; }//
        public byte Humidity { get; private set; }//
        public float Velocity { get; private set; }//
        public short MaxAltitude { get; private set; } //
        public byte SatelliteCount { get; private set; }//
        public byte CRC { get; private set; }//

        public bool IsOpen => _serialPort?.IsOpen ?? false;

        public ArduinoSerialReader(string portName, int baudRate)
        {
            _serialPort = new SerialPort(portName, baudRate);
            _serialPort.DataReceived += DataReceivedHandler;
        }

        public void Open()
        {
            try
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                    _serialPort.DiscardInBuffer();
                    Console.WriteLine("🟢 Roket bağlantısı açıldı.");
                    LogError("Roket bağlantısı açıldı.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Seri port açılırken hata oluştu: {ex.Message}");
                LogError($"Seri port açılırken hata: {ex.Message}");
            }
        }

        public void Close()
        {
            if (_serialPort?.IsOpen ?? false)
            {
                _serialPort.Close();
                Console.WriteLine("🔴 Roket bağlantısı kapatıldı.");
                LogError("Roket bağlantısı kapatıldı.");
            }
        }

        private async void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                Console.WriteLine("⚠ Port kapalı, yeniden bağlanma deneniyor.");
                LogError("Port kapalı, yeniden bağlanma deneniyor.");
                try { Open(); } catch (Exception ex) { Console.WriteLine($"⚠ Yeniden bağlanma başarısız: {ex.Message}"); LogError($"Yeniden bağlanma başarısız: {ex.Message}"); }
                return;
            }

            await Task.Run(() =>
            {
                try
                {
                    // Buffer kontrolünü daha sık yap
                    if (_serialPort.BytesToRead > PacketSize * 5) // 10 yerine 5 yaparak daha sık temizleme
                    {
                        Console.WriteLine("⚠ Buffer doluyor, temizleniyor.");
                        LogError($"Buffer doluyor, temizleniyor. BytesToRead: {_serialPort.BytesToRead}");
                        _serialPort.DiscardInBuffer();
                    }

                    while (_serialPort.BytesToRead >= PacketSize)
                    {
                        _lastDataReceived = DateTime.Now;
                        byte[] buffer = new byte[PacketSize];
                        int bytesRead = _serialPort.Read(buffer, 0, PacketSize);

                        if (bytesRead != PacketSize)
                        {
                            Console.WriteLine($"⚠ Eksik veri alındı: {bytesRead} bayt.");
                            LogError($"Eksik veri alındı: {bytesRead} bayt.");
                            continue;
                        }

                        if (buffer[0] == 0xFF && buffer[PacketSize - 2] == 0x0D && buffer[PacketSize - 1] == 0x0A)
                        {
                            byte receivedCrc = buffer[PacketSize - 3];
                            byte calculatedCrc = CalculateChecksum(buffer, 1, PacketSize - 4);

                            if (receivedCrc == calculatedCrc)
                            {
                                ParseData(buffer);
                                DataUpdated?.Invoke();
                            }
                            else
                            {
                                Console.WriteLine("❌ CRC hatası: Paket bozuk.");
                                LogError($"CRC hatası: Alınan CRC={receivedCrc}, Hesaplanan CRC={calculatedCrc}");
                                _serialPort.DiscardInBuffer();
                                continue;
                            }
                        }
                        else
                        {
                            Console.WriteLine("⚠ Paket yapısı geçersiz.");
                            LogError($"Paket yapısı geçersiz: Başlangıç={buffer[0]}, Bitiş={buffer[PacketSize - 2]}{buffer[PacketSize - 1]}");
                            _serialPort.DiscardInBuffer();
                            continue;
                        }
                    }

                    if (DateTime.Now - _lastDataReceived > _timeout)
                    {
                        Console.WriteLine("⚠ Veri akışı kesildi, port kontrol ediliyor.");
                        LogError("Veri akışı kesildi, port kontrol ediliyor.");
                        Close();
                        try { Open(); } catch (Exception ex) { Console.WriteLine($"⚠ Yeniden bağlanma başarısız: {ex.Message}"); LogError($"Yeniden bağlanma başarısız: {ex.Message}"); }
                    }
                }
                catch (IOException ioex)
                {
                    Console.WriteLine($"⚠ IO Hatası: {ioex.Message}");
                    LogError($"IO Hatası: {ioex.Message}, StackTrace: {ioex.StackTrace}");
                    Close();
                    try { Open(); } catch (Exception ex) { Console.WriteLine($"⚠ Yeniden bağlanma başarısız: {ex.Message}"); LogError($"Yeniden bağlanma başarısız: {ex.Message}"); }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠ Veri ayrıştırılamadı: {ex.Message}");
                    LogError($"Veri ayrıştırılamadı: {ex.Message}, StackTrace: {ex.StackTrace}");
                }
            });
        }
        private void ParseData(byte[] buffer)
        {
            int i = 0;

            byte basla = buffer[i++];
            byte zaman = buffer[i++];
            byte durum = buffer[i++];

            byte rawTemp = buffer[i++];
            Temperature = rawTemp / 5.0f;

            Voltage = BitConverter.ToUInt16(buffer, i); i += 2;
            Current = BitConverter.ToUInt16(buffer, i); i += 2;

            Altitude = BitConverter.ToSingle(buffer, i); i += 4;
            GPSAltitude = BitConverter.ToSingle(buffer, i); i += 4;
            Latitude = BitConverter.ToSingle(buffer, i); i += 4;
            Longitude = BitConverter.ToSingle(buffer, i); i += 4;

            GyroX = BitConverter.ToSingle(buffer, i); i += 4;
            GyroY = BitConverter.ToSingle(buffer, i); i += 4;
            GyroZ = BitConverter.ToSingle(buffer, i); i += 4;

            AccelX = BitConverter.ToSingle(buffer, i); i += 4;
            AccelY = BitConverter.ToSingle(buffer, i); i += 4;
            AccelZ = BitConverter.ToSingle(buffer, i); i += 4;

            Angle = BitConverter.ToSingle(buffer, i); i += 4;

            Humidity = buffer[i++];
            byte rawPitch = buffer[i++];
            byte rawRoll = buffer[i++];
            byte rawYaw = buffer[i++];

            short rawVelocity = BitConverter.ToInt16(buffer, i); i += 2;
            MaxAltitude = BitConverter.ToInt16(buffer, i); i += 2;
            byte uyduData = buffer[i++];

            CRC = buffer[i++];
            byte cr = buffer[i++];
            byte lf = buffer[i++];

            int dakika = zaman >> 2;
            int saniye = ((zaman & 0x03) << 4) | (durum >> 4);
            Status = (byte)(durum & 0x0F);
            Velocity = rawVelocity / 10.0f;

            int signPitch = (uyduData & 0b00000100) != 0 ? -1 : 1;
            int signRoll = (uyduData & 0b00000010) != 0 ? -1 : 1;
            int signYaw = (uyduData & 0b00000001) != 0 ? -1 : 1;

            Pitch = rawPitch * signPitch;
            Roll = rawRoll * signRoll;
            Yaw = rawYaw * signYaw;

            SatelliteCount = (byte)(uyduData >> 3);
        }

        private byte CalculateChecksum(byte[] data, int start, int end)
        {
            byte sum = 0;
            for (int i = start; i <= end; i++)
            {
                sum += data[i];
            }
            return sum;
        }

        private void LogError(string message)
        {
            File.AppendAllText("error_log.txt", $"{DateTime.Now}: {message}\n");
        }

        public void Dispose()
        {
            _serialPort?.Close();
            _serialPort?.Dispose();
        }
    }
}