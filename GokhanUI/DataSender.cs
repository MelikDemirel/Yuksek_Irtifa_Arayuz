using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace Youtube3D_Simulasyon
{
    public sealed class DataSender
    {
        private readonly SerialPort _serialPort;
        private readonly object _gate = new object();
        private CancellationTokenSource _cts;
        private volatile bool _sending;
        private byte _counter = 0;

        public DataSender(string portName, int baudRate)
        {
            _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
            _serialPort.WriteTimeout = 1000;
            _serialPort.ReadTimeout = 1000;
        }

        public bool IsOpen => _serialPort.IsOpen;
        public bool IsSending => _sending;

        public void Open()
        {
            lock (_gate)
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                }
            }
        }

        public void Close()
        {
            lock (_gate)
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
            }
        }

        public void StopSending()
        {
            lock (_gate)
            {
                if (!_sending) return;
                _sending = false;
                try { _cts?.Cancel(); } catch { /* ignore */ }
            }
            // Portu istek üzerine açık bırakmak istersen burayı kaldırabilirsin.
            Close();
        }

        /// <summary>
        /// TEK SEFER paket gönderimi.
        /// </summary>
        public void SendOnce(
            byte teamID, float altitude, float gpsAltitude, float lat, float lon,
            float missionGpsAlt, float missionLat, float missionLon,
            float stageGpsAlt, float stageLat, float stageLon,
            float gyroX, float gyroY, float gyroZ,
            float accX, float accY, float accZ,
            float angle, byte status,
            bool closeAfter = true)
        {
            Open();
            var buffer = BuildPacket(teamID, altitude, gpsAltitude, lat, lon,
                                     missionGpsAlt, missionLat, missionLon,
                                     stageGpsAlt, stageLat, stageLon,
                                     gyroX, gyroY, gyroZ,
                                     accX, accY, accZ,
                                     angle, status);

            lock (_gate)
            {
                if (_serialPort.IsOpen)
                    _serialPort.Write(buffer, 0, buffer.Length);

                _counter = (byte)((_counter + 1) & 0xFF);
            }

            if (closeAfter)
                Close();
        }

        /// <summary>
        /// Sabit snapshot'ı periyodik gönderir (geri uyumluluk).
        /// </summary>
        public void StartSending(
            byte teamID, float altitude, float gpsAltitude, float lat, float lon,
            float missionGpsAlt, float missionLat, float missionLon,
            float stageGpsAlt, float stageLat, float stageLon,
            float gyroX, float gyroY, float gyroZ,
            float accX, float accY, float accZ,
            float angle, byte status, int periodMs = 1000)
        {
            if (_sending) return;

            Open();
            _cts = new CancellationTokenSource();
            _sending = true;

            Task.Run(async () =>
            {
                try
                {
                    while (!_cts.IsCancellationRequested)
                    {
                        var buffer = BuildPacket(teamID, altitude, gpsAltitude, lat, lon,
                                                 missionGpsAlt, missionLat, missionLon,
                                                 stageGpsAlt, stageLat, stageLon,
                                                 gyroX, gyroY, gyroZ,
                                                 accX, accY, accZ,
                                                 angle, status);

                        lock (_gate)
                        {
                            if (_serialPort.IsOpen)
                                _serialPort.Write(buffer, 0, buffer.Length);

                            _counter = (byte)((_counter + 1) & 0xFF);
                        }

                        await Task.Delay(periodMs, _cts.Token).ConfigureAwait(false);
                    }
                }
                catch (TaskCanceledException) { /* normal stop */ }
                finally
                {
                    _sending = false;
                    Close();
                }
            });
        }

        /// <summary>
        /// Her periyotta provider'dan taze veri alır ve periyodik olarak yollar.
        /// </summary>
        public void StartStreaming(byte teamID, Func<TelemetrySnapshot> provider, int periodMs = 1000)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (_sending) return;

            Open();
            _cts = new CancellationTokenSource();
            _sending = true;

            Task.Run(async () =>
            {
                try
                {
                    while (!_cts.IsCancellationRequested)
                    {
                        var s = provider();

                        var buffer = BuildPacket(teamID,
                            s.Altitude, s.GPSAltitude, s.Lat, s.Lon,
                            s.MissionGpsAlt, s.MissionLat, s.MissionLon,
                            s.StageGpsAlt, s.StageLat, s.StageLon,
                            s.GyroX, s.GyroY, s.GyroZ,
                            s.AccX, s.AccY, s.AccZ,
                            s.Angle, s.Status);

                        lock (_gate)
                        {
                            if (_serialPort.IsOpen)
                                _serialPort.Write(buffer, 0, buffer.Length);

                            _counter = (byte)((_counter + 1) & 0xFF);
                        }

                        await Task.Delay(periodMs, _cts.Token).ConfigureAwait(false);
                    }
                }
                catch (TaskCanceledException) { /* normal stop */ }
                finally
                {
                    _sending = false;
                    Close();
                }
            });
        }

        /// <summary>
        /// --- YENİ ---
        /// Veri değiştiğinde hemen gönderir. Küçük dalgalanmaları yutmak için epsilon eşikleri,
        /// portu boğmamak için min interval ve senkron takibi için heartbeat içerir.
        /// </summary>
        public void StartStreamingOnChange(
            byte teamID,
            Func<TelemetrySnapshot> provider,
            int checkIntervalMs = 50,
            int minSendIntervalMs = 40,
            int maxHeartbeatMs = 1000,
            float epsAlt = 0.01f,
            float epsGpsAlt = 0.01f,
            float epsLatLon = 0.00001f, // ~1 m civarı
            float epsIMU = 0.001f,
            float epsAngle = 0.01f)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (_sending) return;

            Open();
            _cts = new CancellationTokenSource();
            _sending = true;

            Task.Run(async () =>
            {
                TelemetrySnapshot lastSent = null;
                var lastSendTime = DateTime.UtcNow.AddYears(-1);

                try
                {
                    while (!_cts.IsCancellationRequested)
                    {
                        var cur = provider();
                        bool shouldSend = false;

                        if (lastSent == null)
                        {
                            shouldSend = true; // ilk paket
                        }
                        else
                        {
                            shouldSend =
                                Changed(lastSent.Altitude, cur.Altitude, epsAlt) ||
                                Changed(lastSent.GPSAltitude, cur.GPSAltitude, epsGpsAlt) ||
                                Changed(lastSent.Lat, cur.Lat, epsLatLon) ||
                                Changed(lastSent.Lon, cur.Lon, epsLatLon) ||
                                Changed(lastSent.MissionGpsAlt, cur.MissionGpsAlt, epsGpsAlt) ||
                                Changed(lastSent.MissionLat, cur.MissionLat, epsLatLon) ||
                                Changed(lastSent.MissionLon, cur.MissionLon, epsLatLon) ||
                                Changed(lastSent.StageGpsAlt, cur.StageGpsAlt, epsGpsAlt) ||
                                Changed(lastSent.StageLat, cur.StageLat, epsLatLon) ||
                                Changed(lastSent.StageLon, cur.StageLon, epsLatLon) ||
                                Changed(lastSent.GyroX, cur.GyroX, epsIMU) ||
                                Changed(lastSent.GyroY, cur.GyroY, epsIMU) ||
                                Changed(lastSent.GyroZ, cur.GyroZ, epsIMU) ||
                                Changed(lastSent.AccX, cur.AccX, epsIMU) ||
                                Changed(lastSent.AccY, cur.AccY, epsIMU) ||
                                Changed(lastSent.AccZ, cur.AccZ, epsIMU) ||
                                Changed(lastSent.Angle, cur.Angle, epsAngle) ||
                                lastSent.Status != cur.Status;
                        }

                        var now = DateTime.UtcNow;
                        var sinceLast = (now - lastSendTime).TotalMilliseconds;

                        // rate-limit
                        if (sinceLast < minSendIntervalMs)
                            shouldSend = false;

                        // heartbeat
                        if (!shouldSend && sinceLast >= maxHeartbeatMs)
                            shouldSend = true;

                        if (shouldSend)
                        {
                            var buffer = BuildPacket(teamID,
                                cur.Altitude, cur.GPSAltitude, cur.Lat, cur.Lon,
                                cur.MissionGpsAlt, cur.MissionLat, cur.MissionLon,
                                cur.StageGpsAlt, cur.StageLat, cur.StageLon,
                                cur.GyroX, cur.GyroY, cur.GyroZ,
                                cur.AccX, cur.AccY, cur.AccZ,
                                cur.Angle, cur.Status);

                            lock (_gate)
                            {
                                if (_serialPort.IsOpen)
                                    _serialPort.Write(buffer, 0, buffer.Length);

                                _counter = (byte)((_counter + 1) & 0xFF);
                            }

                            lastSent = cur.Clone();
                            lastSendTime = now;
                        }

                        await Task.Delay(checkIntervalMs, _cts.Token).ConfigureAwait(false);
                    }
                }
                catch (TaskCanceledException) { /* normal stop */ }
                finally
                {
                    _sending = false;
                    Close();
                }
            });
        }

        // --- paketleme ---

        private byte[] BuildPacket(byte teamID, float altitude, float gpsAltitude, float lat, float lon,
                                   float missionGpsAlt, float missionLat, float missionLon,
                                   float stageGpsAlt, float stageLat, float stageLon,
                                   float gyroX, float gyroY, float gyroZ,
                                   float accX, float accY, float accZ,
                                   float angle, byte status)
        {
            // [0]=FF [1]=FF [2]=54 [3]=52 [4]=teamID [5]=counter
            // 17 float (68 byte) + status + checksum + CR LF = toplam 78 byte
            var buffer = new byte[78];

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
            buffer[i++] = CalculateChecksum(buffer); // sum of [4..74] % 256
            buffer[i++] = 0x0D; // CR
            buffer[i++] = 0x0A; // LF

            return buffer;
        }

        private static void AppendFloat(byte[] buffer, ref int index, float value)
        {
            var b = BitConverter.GetBytes(value); // little-endian
            Buffer.BlockCopy(b, 0, buffer, index, 4);
            index += 4;
        }

        private static byte CalculateChecksum(byte[] buffer)
        {
            int sum = 0;
            for (int k = 4; k <= 74; k++)
                sum += buffer[k];
            return (byte)(sum & 0xFF);
        }

        private static bool Changed(float a, float b, float eps) => Math.Abs(a - b) > eps;
    }

    // Canlı akış için taze snapshot taşıyıcısı
    public sealed class TelemetrySnapshot
    {
        public float Altitude, GPSAltitude, Lat, Lon;
        public float MissionGpsAlt, MissionLat, MissionLon;
        public float StageGpsAlt, StageLat, StageLon;
        public float GyroX, GyroY, GyroZ;
        public float AccX, AccY, AccZ;
        public float Angle;
        public byte Status;

        public TelemetrySnapshot Clone() => new TelemetrySnapshot
        {
            Altitude = Altitude,
            GPSAltitude = GPSAltitude,
            Lat = Lat,
            Lon = Lon,
            MissionGpsAlt = MissionGpsAlt,
            MissionLat = MissionLat,
            MissionLon = MissionLon,
            StageGpsAlt = StageGpsAlt,
            StageLat = StageLat,
            StageLon = StageLon,
            GyroX = GyroX,
            GyroY = GyroY,
            GyroZ = GyroZ,
            AccX = AccX,
            AccY = AccY,
            AccZ = AccZ,
            Angle = Angle,
            Status = Status
        };
    }
}