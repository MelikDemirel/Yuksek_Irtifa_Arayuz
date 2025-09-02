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
        private readonly object _lock = new object(); // Okuma için kilit

        public byte Status { get; private set; }
        public float Voltage { get; private set; }
        public ushort Current { get; private set; }
        public float Pitch { get; private set; }
        public float Roll { get; private set; }
        public float Yaw { get; private set; }
        public float AccelX { get; private set; }
        public float AccelY { get; private set; }
        public float AccelZ { get; private set; }
        public float Angle { get; private set; }
        public float Altitude { get; private set; }
        public float GPSAltitude { get; private set; }
        public float Latitude { get; private set; }
        public float Longitude { get; private set; }
        public float GyroX { get; private set; }
        public float GyroY { get; private set; }
        public float GyroZ { get; private set; }
        public float Temperature { get; private set; }
        public byte Humidity { get; private set; }
        public float Velocity { get; private set; }
        public short MaxAltitude { get; private set; }
        public byte SatelliteCount { get; private set; }
        public byte CRC { get; private set; }

        public bool IsOpen => _serialPort?.IsOpen ?? false;

        public int Dakika { get; private set; }
        public int Saniye { get; private set; }

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
                    Console.WriteLine("Roket baglantisi acildi.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Seri port acilirken hata olustu: {ex.Message}");
            }
        }

        public void Close()
        {
            if (_serialPort?.IsOpen ?? false)
            {
                _serialPort.Close();
                Console.WriteLine("Roket baglantisi kapatildi.");
            }
        }

        // Reset veya gürültü sonrası buffer temizleme için ek metod
        public void ClearBuffer()
        {
            if (_serialPort?.IsOpen ?? false)
            {
                _serialPort.DiscardInBuffer();
                Console.WriteLine("Buffer manuel temizlendi.");
            }
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            lock (_lock) // Tek thread erişimi için kilit
            {
                try
                {
                    // Buffer kontrolü
                    if (_serialPort.BytesToRead > PacketSize * 5)
                    {
                        Console.WriteLine("Buffer doluyor, temizleniyor.");
                        _serialPort.DiscardInBuffer();
                    }

                    while (_serialPort.BytesToRead >= PacketSize)
                    {
                        _lastDataReceived = DateTime.Now;
                        byte[] buffer = new byte[PacketSize];
                        int bytesRead = _serialPort.Read(buffer, 0, PacketSize);

                        if (bytesRead != PacketSize)
                        {
                            Console.WriteLine($"Eksik veri alindi: {bytesRead} bayt.");
                            continue;
                        }

                        // Başlık ve bitiş kontrolü + hizalama düzeltme
                        if (buffer[0] != 0xFF)
                        {
                            Console.WriteLine("Paket baslangici hatali. Hizalama duzeltmesi deneniyor.");
                            bool resynced = false;
                            for (int j = 1; j < bytesRead; j++)
                            {
                                if (buffer[j] == 0xFF)
                                {
                                    int remaining = bytesRead - j;
                                    byte[] newBuffer = new byte[PacketSize];
                                    Array.Copy(buffer, j, newBuffer, 0, remaining);

                                    int additionalRead = _serialPort.Read(newBuffer, remaining, PacketSize - remaining);
                                    if (additionalRead + remaining == PacketSize)
                                    {
                                        buffer = newBuffer;
                                        resynced = true;
                                        Console.WriteLine("Hizalama duzeltildi.");
                                        break;
                                    }
                                }
                            }
                            if (!resynced || buffer[0] != 0xFF)
                            {
                                continue;
                            }
                        }

                        if (buffer[PacketSize - 2] != 0x0D || buffer[PacketSize - 1] != 0x0A)
                        {
                            Console.WriteLine("Paket bitisi hatali.");
                            continue;
                        }

                        // CRC kontrolü
                        byte receivedCrc = buffer[PacketSize - 3];
                        byte calculatedCrc = CalculateChecksum(buffer, 1, PacketSize - 4);

                        if (receivedCrc != calculatedCrc)
                        {
                            Console.WriteLine($"CRC hatasi: Alinan {receivedCrc}, Hesaplanan {calculatedCrc}");
                            continue;
                        }

                        // Veriyi parse et
                        ParseData(buffer);
                        DataUpdated?.Invoke();
                        Console.WriteLine("Gecerli paket parse edildi.");
                    }

                    // Timeout kontrolü
                    if (DateTime.Now - _lastDataReceived > _timeout)
                    {
                        Console.WriteLine("Veri akisi kesildi, port kontrol ediliyor.");
                        // Yeniden bağlanma yerine loglama, gerekirse UI'dan manuel müdahale
                    }
                }
                catch (IOException ioex)
                {
                    Console.WriteLine($"IO Hatasi: {ioex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Veri ayristirilamadi: {ex.Message}");
                }
            }
        }

        private void ParseData(byte[] buffer)
        {
            int i = 0;

            byte basla = buffer[i++]; // 0xFF
            byte zaman = buffer[i++];
            byte durum = buffer[i++];

            float rawTemp = buffer[i++];
            Temperature = rawTemp / 2.0f;

            float hamVoltage = BitConverter.ToUInt16(buffer, i); i += 2;
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
            byte cr = buffer[i++]; // 0x0D
            byte lf = buffer[i++]; // 0x0A

            int dakika = zaman >> 2;
            int saniye = ((zaman & 0x03) << 4) | (durum >> 4);

            Dakika = dakika;
            Saniye = saniye;

            Status = (byte)(durum & 0x0F);
            Velocity = rawVelocity / 10.0f;

            int signPitch = (uyduData & 0b00000100) != 0 ? -1 : 1;
            int signRoll = (uyduData & 0b00000010) != 0 ? -1 : 1;
            int signYaw = (uyduData & 0b00000001) != 0 ? -1 : 1;

            Pitch = rawPitch * signPitch;
            Roll = rawRoll * signRoll;
            Yaw = rawYaw * signYaw;

            SatelliteCount = (byte)(uyduData >> 3);
            Voltage = hamVoltage / 100;
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

        public void Dispose()
        {
            _serialPort?.Close();
            _serialPort?.Dispose();
        }
    }
}