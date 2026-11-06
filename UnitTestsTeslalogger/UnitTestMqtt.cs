using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TeslaLogger;

namespace UnitTestsTeslalogger
{
    [TestClass]

    public class UnitTestMqtt
    {
        private const string s_AllCarsUrl = "http://localhost:5000/getallcars";
        private const string s_firstCarCurrentJson = "http://localhost:5000/currentjson/1";
        private const string s_InvalidCarCurrentJson = "http://localhost:5000/currentjson/-1";

        [TestCleanup]
        public void Cleanup()
        {
            // need to set the static instance field to null via reflection, because each TestMethod requires a fresh MQTT instance
            var instanceFild = typeof(MQTT).GetField("_Mqtt", BindingFlags.Static | BindingFlags.NonPublic);
            instanceFild?.SetValue(null, null);

            // Car instances created in a test accumulate in the static allcars list and leak into other tests
            Car.Allcars.Clear();

            // same for the static connecting flag
            var connectingField = typeof(MQTT).GetField("connecting", BindingFlags.Static | BindingFlags.NonPublic);
            connectingField?.SetValue(null, false);
        }

        [TestMethod]
        public void Work_does_refresh_all_cars_on_currentjson_not_found()
        {
            var mqttClientMock = new Mock<IMqttClient>();
            var mock = mqttClientMock.SetupGet(client => client.IsConnected).Returns(true);

            var webDownloaderMock = new Mock<IWebDownloader>();

            var initialJson = "[{\"id\":1,\"display_name\":\"Testcar\",\"vin\":\"5YJ3E7EB3KFXXXXXX\",\"inactive\":null}]";
            var laterJson = "[]";

            webDownloaderMock.SetupSequence(m => m.DownloadString(s_AllCarsUrl)).Returns(initialJson).Returns(laterJson);

            var responseMock = new Mock<HttpWebResponse>();
            responseMock.SetupGet(response => response.StatusCode).Returns(HttpStatusCode.NotFound);

            webDownloaderMock.Setup(m => m.DownloadString(s_firstCarCurrentJson)).Throws(new WebException("The remote server returned an error: (404) Not Found", null, WebExceptionStatus.ProtocolError, responseMock.Object));

            var f = typeof(MQTTWebDownloader).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            f.SetValue(null, webDownloaderMock.Object);

            var mqtt = MQTT.GetSingleton();

            var f2 = typeof(MQTT).GetField("client", BindingFlags.NonPublic | BindingFlags.Instance);
            f2.SetValue(mqtt, mqttClientMock.Object);

            Assert.IsNotNull(mqtt);

            var c = new Car(1, "", "", 0, "", DateTime.MinValue, "", "", "", "", "", "5YJ3E7EB3KFXXXXXX", "", null, false);
            mqtt.Work();

            webDownloaderMock.Verify(m => m.DownloadString(s_InvalidCarCurrentJson), Times.Never);
            webDownloaderMock.Verify(m => m.DownloadString(s_AllCarsUrl), Times.Exactly(2));

            var f3 = typeof(MQTT).GetField("allCars", BindingFlags.NonPublic | BindingFlags.Instance);
            var v3 = f3?.GetValue(mqtt) as HashSet<string>;
            Assert.IsNotNull(v3);
            Assert.AreEqual(0, v3.Count);

            mqttClientMock.Verify(m => m.Unsubscribe(new[] { "teslalogger/command/5YJ3E7EB3KFXXXXXX/+" }), Times.Once);
        }

        [TestMethod]
        public void Work_resubscribes_command_topics_after_reconnect()
        {
            var mqttClientMock = new Mock<IMqttClient>();
            bool isConnected = false;
            mqttClientMock.SetupGet(client => client.IsConnected).Returns(() => isConnected);

            var webDownloaderMock = new Mock<IWebDownloader>();
            var carsJson = "[{\"id\":1,\"display_name\":\"Testcar\",\"vin\":\"5YJ3E7EB3KFXXXXXX\",\"inactive\":null}]";
            webDownloaderMock.Setup(m => m.DownloadString(s_AllCarsUrl)).Returns(carsJson);
            webDownloaderMock.Setup(m => m.DownloadString(s_firstCarCurrentJson)).Returns("{}");

            var f = typeof(MQTTWebDownloader).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            f.SetValue(null, webDownloaderMock.Object);

            var mqtt = MQTT.GetSingleton();

            var f2 = typeof(MQTT).GetField("client", BindingFlags.NonPublic | BindingFlags.Instance);
            f2.SetValue(mqtt, mqttClientMock.Object);

            Assert.IsNotNull(mqtt);

            var c = new Car(1, "", "", 0, "", DateTime.MinValue, "", "", "", "", "", "5YJ3E7EB3KFXXXXXX", "", null, false);

            // broker down: a connect is attempted, but nothing is subscribed
            isConnected = false;
            mqtt.Work();
            mqttClientMock.Verify(m => m.Connect(It.IsAny<string>(), null, null, false, 0, true, "teslalogger/system/status", "offline", true, 30), Times.Once);
            mqttClientMock.Verify(m => m.Subscribe(It.IsAny<string[]>(), It.IsAny<byte[]>()), Times.Never);

            // broker back up: the reconnect uses cleanSession=true, so the command subscriptions must be restored
            isConnected = true;
            mqtt.Work();

            // QOS_LEVEL_AT_MOST_ONCE = 0
            mqttClientMock.Verify(m => m.Subscribe(new[] { "teslalogger/command/5YJ3E7EB3KFXXXXXX/+" }, new byte[] { 0 }), Times.Once);
        }

        [TestMethod]
        public void Work_retries_after_connection_refused()
        {
            var mqttClientMock = new Mock<IMqttClient>();
            mqttClientMock.SetupGet(client => client.IsConnected).Returns(false);
            mqttClientMock.Setup(client => client.Connect(It.IsAny<string>(), null, null, false, 0, true, "teslalogger/system/status", "offline", true, 30)).Returns(5);

            var webDownloaderMock = new Mock<IWebDownloader>();
            webDownloaderMock.Setup(m => m.DownloadString(s_AllCarsUrl)).Returns("[]");

            var f = typeof(MQTTWebDownloader).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            f.SetValue(null, webDownloaderMock.Object);

            var mqtt = MQTT.GetSingleton();

            var f2 = typeof(MQTT).GetField("client", BindingFlags.NonPublic | BindingFlags.Instance);
            f2.SetValue(mqtt, mqttClientMock.Object);

            mqtt.Work();
            mqtt.Work();

            mqttClientMock.Verify(m => m.Connect(It.IsAny<string>(), null, null, false, 0, true, "teslalogger/system/status", "offline", true, 30), Times.Exactly(2));
            mqttClientMock.Verify(m => m.Close(), Times.Exactly(2));
        }

        [TestMethod]
        public void Connection_refused_by_broker_does_not_leave_receive_thread_running()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            Task brokerTask = Task.Run(() =>
            {
                using (Socket socket = listener.AcceptSocket())
                {
                    byte[] buffer = new byte[1024];
                    socket.Receive(buffer);
                    socket.Send(new byte[] { 0x20, 0x02, 0x00, 0x05 });
                    Thread.Sleep(100);
                }
            });

            try
            {
                var createMethod = typeof(MqttClientWrapper).GetMethod("CreateClient", BindingFlags.Public | BindingFlags.Static);
                object sslProtocol = Enum.ToObject(createMethod.GetParameters()[5].ParameterType, 0);
                var client = (IMqttClient)createMethod.Invoke(null, new object[] { "127.0.0.1", port, false, null, null, sslProtocol });

                byte returnCode = client.Connect("test-client", null, null, false, 0, true, "teslalogger/system/status", "offline", true, 30);
                Assert.AreEqual(5, returnCode);

                var clientField = typeof(MqttClientWrapper).GetField("_client", BindingFlags.NonPublic | BindingFlags.Instance);
                object mqttClient = clientField.GetValue(client);
                var isRunningField = mqttClient.GetType().GetField("isRunning", BindingFlags.NonPublic | BindingFlags.Instance);

                Thread.Sleep(300);
                Assert.IsTrue((bool)isRunningField.GetValue(mqttClient), "M2Mqtt leaves its receive thread running after a refused connect");

                client.Close();

                Assert.IsFalse((bool)isRunningField.GetValue(mqttClient), "Close must stop the orphaned receive thread");
            }
            finally
            {
                listener.Stop();
                brokerTask.Wait(5000);
            }
        }

        [TestMethod]
        public void Work_does_not_try_to_retrieve_currentjson_for_invalid_car_id()
        {
            var mqttClientMock = new Mock<IMqttClient>();
            var mock = mqttClientMock.SetupGet(client => client.IsConnected).Returns(true);

            var webDownloaderMock = new Mock<IWebDownloader>();

            var initialJson = "[{\"id\":1,\"display_name\":\"Testcar\",\"vin\":\"5YJ3E7EB3KFXXXXXX\",\"inactive\":null}]";
            var laterJson = "[]";

            webDownloaderMock.SetupSequence(m => m.DownloadString(s_AllCarsUrl)).Returns(initialJson).Returns(laterJson);

            var responseMock = new Mock<HttpWebResponse>();
            responseMock.SetupGet(response => response.StatusCode).Returns(HttpStatusCode.NotFound);

            webDownloaderMock.Setup(m => m.DownloadString(s_firstCarCurrentJson)).Throws(new WebException("The remote server returned an error: (404) Not Found", null, WebExceptionStatus.ProtocolError, responseMock.Object));

            var f = typeof(MQTTWebDownloader).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            f.SetValue(null, webDownloaderMock.Object);

            var mqtt = MQTT.GetSingleton();

            var f2 = typeof(MQTT).GetField("client", BindingFlags.NonPublic | BindingFlags.Instance);
            f2.SetValue(mqtt, mqttClientMock.Object);

            Assert.IsNotNull(mqtt);

            var c = new Car(1, "", "", 0, "", DateTime.MinValue, "", "", "", "", "", "", "", null, false);
            mqtt.Work();

            webDownloaderMock.Verify(m => m.DownloadString(s_InvalidCarCurrentJson), Times.Never);
            webDownloaderMock.Verify(m => m.DownloadString(s_AllCarsUrl), Times.Exactly(2));

            var f3 = typeof(MQTT).GetField("allCars", BindingFlags.NonPublic | BindingFlags.Instance);
            var v3 = f3?.GetValue(mqtt) as HashSet<string>;
            Assert.IsNotNull(v3);
            Assert.AreEqual(0, v3.Count);

            mqttClientMock.Verify(m => m.Unsubscribe(new[] { "teslalogger/command/5YJ3E7EB3KFXXXXXX/+" }), Times.Once);
        }

        [TestMethod]
        public void Work_does_not_try_to_retrieve_currentjson_for_inactive_car()
        {
            var mqttClientMock = new Mock<IMqttClient>();
            var mock = mqttClientMock.SetupGet(client => client.IsConnected).Returns(true);

            var webDownloaderMock = new Mock<IWebDownloader>();

            var initialJson = "[{\"id\":1,\"display_name\":\"Testcar\",\"vin\":\"5YJ3E7EB3KFXXXXXX\",\"inactive\":null}]";

            webDownloaderMock.SetupSequence(m => m.DownloadString(s_AllCarsUrl)).Returns(initialJson);

            var f = typeof(MQTTWebDownloader).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            f.SetValue(null, webDownloaderMock.Object);

            var mqtt = MQTT.GetSingleton();

            var f2 = typeof(MQTT).GetField("client", BindingFlags.NonPublic | BindingFlags.Instance);
            f2.SetValue(mqtt, mqttClientMock.Object);

            Assert.IsNotNull(mqtt);

            mqtt.Work();

            webDownloaderMock.Verify(m => m.DownloadString(s_InvalidCarCurrentJson), Times.Never);
            webDownloaderMock.Verify(m => m.DownloadString(s_firstCarCurrentJson), Times.Never);
            webDownloaderMock.Verify(m => m.DownloadString(s_AllCarsUrl), Times.Exactly(1));

            var f3 = typeof(MQTT).GetField("allCars", BindingFlags.NonPublic | BindingFlags.Instance);
            var v3 = f3?.GetValue(mqtt) as HashSet<string>;
            Assert.IsNotNull(v3);
            Assert.AreEqual(0, v3.Count);

            mqttClientMock.Verify(m => m.Unsubscribe(It.IsAny<string[]>()), Times.Never);
        }
    }
}
