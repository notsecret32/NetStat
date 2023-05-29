using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;

namespace NetStat
{
    namespace Client
    {
        public partial class ClientForm : Form
        {
            /// <summary>
            /// Сокет клиента.
            /// </summary>
            private Socket client;

            /// <summary>
            /// Хранит состояние клиента. 
            /// True - подключен к серверу. False - не подключен к серверу.
            /// </summary>
            private bool isClientConnected = false;

            private NetStatObject netStatObject = new NetStatObject();

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
            /// Конструктор по умолчанию.
            /// </summary>
            public ClientForm()
            {
                InitializeComponent();
            }

            /// <summary>
            /// Callback-функция, реагирует на нажатие кнопки.
            /// Переключает состояние клиента на «Подключен» и «Не подключен».
            /// </summary>
            /// <param name="sender">Объект, который вызвал функцию.</param>
            /// <param name="e">
            /// Параметр, ссылающийся на базовый класс для классов, содержащих данные событий, и предоставляет
            /// значение для событий, не содержащих данных.
            /// </param>
            private async void OnToggleServerClick(object sender, EventArgs e)
            {
                try
                {
                    // Если клиент не подключен к серверу
                    if (!isClientConnected)
                    {
                        // Инициализируем сокет клиента
                        client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        // Асинхронно подключаемся к серверу
                        await client.ConnectAsync(ipAddressText.Text, int.Parse(portText.Text));

                        // Если удалось подключиться
                        if (client.Connected)
                        {
                            clientLogs.Items.Add($"Подключение к серверу {client.RemoteEndPoint} успешно установлено!");
                            btnToggleConnection.Text = "Отключиться";
                            btnGetData.Enabled = true;
                            isClientConnected = true;
                            ipAddressText.Enabled = false;
                            portText.Enabled = false;
                        }

                        return;
                    }

                    // Если клииент подключен к серверу
                    if (isClientConnected)
                    {
                        clientLogs.Items.Add($"Отключение от сервера");
                        btnToggleConnection.Text = "Подключиться";
                        btnGetData.Enabled = false;
                        isClientConnected = false;
                        ipAddressText.Enabled = true;
                        portText.Enabled = true;

                        client.Shutdown(SocketShutdown.Both);
                        client.Close();

                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            /// <summary>
            /// Callback-функция, реагирует на нажатие кнопки.
            /// Получает данные с сервера и выводит результат в форму.
            /// </summary>
            /// <param name="sender">Объект, который вызвал функцию.</param>
            /// <param name="e">
            /// Параметр, ссылающийся на базовый класс для классов, содержащих данные событий, и предоставляет
            /// значение для событий, не содержащих данных.
            /// </param>
            private void OnGetDataClick(object sender, EventArgs e)
            {
                try
                {
                    SendObject();
                    ReceiveObject();
                }
                catch (SocketException ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            private async void SendObject()
            {
                // Для начала отправим серверу объект NetStatObject
                // Инициализируем объект
                netStatObject = new NetStatObject
                {
                    RequestList = RequestList.GET_TCP_DATA
                };

                // Сериализуем его в поток байтов
                formatter.Serialize(outputStream, netStatObject);
                // Записываем получившийся поток байтов в переменную
                byte[] netStatObjectBytes = outputStream.ToArray();

                // Отправляем объект на сервер в виде массива байтов, асинхронно
                _ = await client.SendAsync(new ArraySegment<byte>(netStatObjectBytes), SocketFlags.None);
                // Сообщаем об этом пользователю
                clientLogs.Items.Add($"На сервер был отправлен запрос: {netStatObject.RequestList}");
            }

            private async void ReceiveObject()
            {
                // Получаем объект от сервера
                // Подготавливаем массив байтов для хранения объектв
                byte[] receivedBytes = new byte[client.ReceiveBufferSize];
                // Асинхронно получаем объект в виде байтом с сервера
                int receivedBytesRead = await client.ReceiveAsync(new ArraySegment<byte>(receivedBytes), SocketFlags.None);
                // Инициализируем потом байтов изходя из полученных данных
                inputStream = new MemoryStream(receivedBytes, 0, receivedBytesRead)
                {
                    // Позиция в потоке
                    Position = 0
                };
                // Десериализируем и преобразовываем в объект
                netStatObject = (NetStatObject)formatter.Deserialize(inputStream);
                // Уведомляем об этом пользователя
                clientLogs.Items.Add($"==========Данные полученные с сервера==========");
                clientLogs.Items.Add($"Сообщение: {netStatObject.Message}");
                clientLogs.Items.Add($"IP-Адрес клиента: {netStatObject.IPAddress}");
                clientLogs.Items.Add($"Порт клиента: {netStatObject.Port}");
                clientLogs.Items.Add($"Байт получено: {netStatObject.BytesReveived}");
                clientLogs.Items.Add($"Байт отправлено: {netStatObject.BytesSended}");
                clientLogs.Items.Add($"Тип протокола: {netStatObject.ProtocolType}");
            }
        }
    }
}