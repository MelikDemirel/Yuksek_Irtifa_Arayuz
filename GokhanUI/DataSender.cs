using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace GokhanUI
{
    public class DataSender
    {
        private SerialPort _serialPort;
        private bool _sending;
        private CancellationTokenSource _cancellationTokenSource;
        private byte _counter = 0;

        public DataSender(string portName, int baudRate)
        {
            _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
        }

        public void Open()
        {
            if (!_serialPort.IsOpen)
            {
                try
                {
                    _serialPort.Open();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠ Port açılırken hata: {ex.Message}");
                }
            }
        }

        public void Close()
        {
            if (_serialPort.IsOpen)
                _serialPort.Close();
        }

        public bool IsSending => _sending;

        public void StartSending(byte teamID, float altitude, float gpsAltitude, float lat, float lon,
                                 float missionGpsAlt, float missionLat, float missionLon,
                                 float stageGpsAlt, float stageLat, float stageLon,
                                 float gyroX, float gyroY, float gyroZ,
                                 float accX, float accY, float accZ,
                                 float angle, byte status)
        {
            if (_sending)
                return;

            _sending = true;
            _cancellationTokenSource = new CancellationTokenSource();
            Open();

            Task.Run(async () =>
            {
                while (_sending)
                {
                    byte[] data = BuildPacket(teamID, altitude, gpsAltitude, lat, lon,
                                              missionGpsAlt, missionLat, missionLon,
                                              stageGpsAlt, stageLat, stageLon,
                                              gyroX, gyroY, gyroZ,
                                              accX, accY, accZ,
                                              angle, status);

                    if (_serialPort.IsOpen)
                        _serialPort.Write(data, 0, data.Length);

                    try { await Task.Delay(1000, _cancellationTokenSource.Token); }
                    catch (TaskCanceledException) { break; }

                    _counter = (byte)((_counter + 1) % 256);
                }

                Close();
            });
        }

        public void StopSending()
        {
            if (_sending)
            {
                _sending = false;
                _cancellationTokenSource.Cancel();
                Close();
            }
        }

        private byte[] BuildPacket(byte teamID, float altitude, float gpsAltitude, float lat, float lon,
                                   float missionGpsAlt, float missionLat, float missionLon,
                                   float stageGpsAlt, float stageLat, float stageLon,
                                   float gyroX, float gyroY, float gyroZ,
                                   float accX, float accY, float accZ,
                                   float angle, byte status)
        {
            byte[] buffer = new byte[78];
            buffer[0] = 0xFF;
            buffer[1] = 0xFF;
            buffer[2] = 0x54;
            buffer[3] = 0x52;
            buffer[4] = teamID;
            buffer[5] = _counter;

            int i = 6;
            AppendFloat(buffer, ref i, altitude);
            AppendFloat(buffer, ref i, gpsAltitude);
            AppendFloat(buffer, ref i, lat);
            AppendFloat(buffer, ref i, lon);
            AppendFloat(buffer, ref i, missionGpsAlt);
            AppendFloat(buffer, ref i, missionLat);
            AppendFloat(buffer, ref i, missionLon);
            AppendFloat(buffer, ref i, stageGpsAlt);
            AppendFloat(buffer, ref i, stageLat);
            AppendFloat(buffer, ref i, stageLon);
            AppendFloat(buffer, ref i, gyroX);
            AppendFloat(buffer, ref i, gyroY);
            AppendFloat(buffer, ref i, gyroZ);
            AppendFloat(buffer, ref i, accX);
            AppendFloat(buffer, ref i, accY);
            AppendFloat(buffer, ref i, accZ);
            AppendFloat(buffer, ref i, angle);

            buffer[i++] = status;
            buffer[i++] = CalculateChecksum(buffer);
            buffer[i++] = 0x0D;
            buffer[i++] = 0x0A;

            return buffer;
        }

        private void AppendFloat(byte[] buffer, ref int index, float value)
        {
            byte[] floatBytes = BitConverter.GetBytes(value);
            Array.Copy(floatBytes, 0, buffer, index, 4);
            index += 4;
        }

        private byte CalculateChecksum(byte[] buffer)
        {
            int sum = 0;
            for (int i = 4; i <= 74; i++)
                sum += buffer[i];
            return (byte)(sum % 256);
        }
    }
}