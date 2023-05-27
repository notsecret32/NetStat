using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;

namespace NetStat
{
    namespace Server
    {
        public partial class ServerForm : Form
        {
            /// <summary>
            /// IP-адрес сервера
            /// </summary>
            private IPAddress ipAddress;

            /// <summary>
            /// Конечная точка сервера. Содержит IP-адрес и порт.
            /// </summary>
            private IPEndPoint ipEndPoint;

            /// <summary>
            /// Сокет сервера.
            /// </summary>
            private Socket server;

            /// <summary>
            /// Поток входных данных.
            /// </summary>
            private MemoryStream inputStream = new MemoryStream();

            /// <summary>
            /// Поток выходных данных.
            /// </summary>
            private MemoryStream outputStream = new MemoryStream();

            /// <summary>
            /// Преобразователь в бинарный вид.
            /// </summary>
            private readonly BinaryFormatter formatter = new BinaryFormatter();

            /// <summary>
            /// Конструктор по умолчанию
            /// </summary>
            public ServerForm()
            {
                InitializeComponent();
            }

            /// <summary>
            /// Callback-функция, реагирует на нажатие кнопки.
            /// Запускает сервер и ждет взаимодействия с клиентом.
            /// </summary>
            /// <param name="sender">Объект, который вызвал функцию</param>
            /// <param name="e">
            /// Параметр, ссылающийся на базовый класс для классов, содержащих данные событий, и предоставляет
            /// значение для событий, не содержащих данных.
            /// </param>
            private async void OnStartServerClick(object sender, EventArgs e)
            {
                try
                {
                    // Создаем конечную точку
                    ipAddress = IPAddress.Parse(ipAddressText.Text);
                    ipEndPoint = new IPEndPoint(ipAddress, int.Parse(portText.Text));

                    // Инициализируем сокет сервера
                    server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    // Связываем серрвеер с конечной точкой
                    server.Bind(ipEndPoint);

                    // Измееняем форму, для избежания лишних проблем (они нам не нужны)
                    ipAddressText.Enabled  = false;
                    portText.Enabled       = false;
                    btnStartServer.Enabled = false;
                    btnStopServer.Enabled  = true;
                    serverLogs.Items.Add($"Сервер {ipEndPoint.Address}:{ipEndPoint.Port} был запущен");

                    // Устанавливаем сервер в режим прослушки
                    // Это значит, что мы ждем какое-то взаимодействие
                    server.Listen(1000);
                    serverLogs.Items.Add($"Сервер начал прослушку");

                    while (true)
                    {
                        // Получаем сокет подключившегося клиента
                        Socket connectedClient = await server.AcceptAsync();

                        // Если подключиться не удалось
                        if (!connectedClient.Connected)
                        {
                            // Выводим ожибку
                            serverLogs.Items.Add($"Клиенту не удалось подключиться");
                            // Выходим из функции
                            return;
                        }

                        // Иначе идем дальше
                        serverLogs.Items.Add($"Клиент {connectedClient.RemoteEndPoint} подключился");

                        // Ждем сообщение от клиента
                        // Создаем массив для хранения объекта в виде массива байтов
                        byte[] receivedBytes = new byte[server.ReceiveBufferSize];
                        // Как только получили объект, записываем полученные данные
                        int receivedBytesRead = await connectedClient.ReceiveAsync(new ArraySegment<byte>(receivedBytes), SocketFlags.None);
                        // Создаем поток исходя из полученных данных
                        inputStream = new MemoryStream(receivedBytes, 0, receivedBytesRead)
                        {
                            // Устанавливаем начальную позицию в 0
                            Position = 0
                        };
                        // Десериализируем и преоразовываем в объект
                        NetStatObject responceNetStatObject = (NetStatObject)formatter.Deserialize(inputStream);
                        // Записываем в логи сервера
                        serverLogs.Items.Add($"Клиент {connectedClient.RemoteEndPoint} отправил запрос: {responceNetStatObject.RequestList}");
                        
                        // Здесь мы должны взаимодействовать с объектом
                        responceNetStatObject.Message = "Объект успешно получен на сервер и отправлеен обратно клиенту!";

                        // Отправляем объект обратно клиенту
                        // Создаем обратный, выходной поток данных
                        outputStream = new MemoryStream();
                        // Сериализуем объект
                        formatter.Serialize(outputStream, responceNetStatObject);
                        // И преоразовываем в массив байтов
                        byte[] netStatObjectBytes = outputStream.ToArray();
                        // Асинхронно отправляем результат, в виде массива байтов, на сервер
                        _ = await connectedClient.SendAsync(new ArraySegment<byte>(netStatObjectBytes), SocketFlags.None);
                        // Оповещаем об отправке объекта, в виде массива данных, в логи сервера
                        serverLogs.Items.Add($"Сервер отправил объект клиенту!");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            /// <summary>
            /// Callback-функция, реагирует на нажатие кнопки.
            /// Останавливает сервер.
            /// </summary>
            /// <param name="sender">Объект, который вызвал функцию</param>
            /// <param name="e">
            /// Параметр, ссылающийся на базовый класс для классов, содержащих данные событий, и предоставляет
            /// значение для событий, не содержащих данных.
            /// </param>
            private void OnStopServerClick(object sender, EventArgs e)
            {
                ipAddressText.Enabled  = true;
                portText.Enabled       = true;
                btnStartServer.Enabled = true;
                btnStopServer.Enabled  = false;

                serverLogs.Items.Add($"Сервер был выключен");

                server.Shutdown(SocketShutdown.Both);
                server.Close();
            }
        }
    }
}