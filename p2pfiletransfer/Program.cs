using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("P2P File Transfer");
        Console.Write("Enter 'S' for sender or 'R' for receiver: ");
        string choice = Console.ReadLine();

        if (choice.ToUpper() == "S")
        {
            Console.Write("Enter 'G' for global file transfer or 'L' for local file transfer: ");
            string transferChoice = Console.ReadLine();
            if (transferChoice.ToUpper() == "G")
            {
                Sender();
            }
            else if (transferChoice.ToUpper() == "L")
            {
                LocalFileSender();
            }
            else
            {
                Console.WriteLine("Invalid transfer choice. Please enter 'G' for global or 'L' for local.");
            }
        }
        else if (choice.ToUpper() == "R")
        {
            Console.Write("Enter 'G' for global file transfer or 'L' for local file transfer: ");
            string transferChoice = Console.ReadLine();
            if (transferChoice.ToUpper() == "G")
            {
                Receiver();
            }
            else if (transferChoice.ToUpper() == "L")
            {
                LocalFileReceiver();
            }
            else
            {
                Console.WriteLine("Invalid transfer choice. Please enter 'G' for global or 'L' for local.");
            }
        }
        else
        {
            Console.WriteLine("Invalid choice. Please enter 'S' for sender or 'R' for receiver.");
        }
    }

    static void Sender()
    {
        while (true)
        {
            Console.WriteLine("Sender Mode");

            Console.Write("Enter the receiver's IP address: ");
            string receiverIpAddress = Console.ReadLine();
            Console.Write("Enter the port to connect to: ");
            int port = int.Parse(Console.ReadLine());

            Console.Write("Enter the path of the file to send: ");
            string filePath = Console.ReadLine();

            try
            {
                using (TcpClient senderClient = new TcpClient(receiverIpAddress, port))
                {
                    NetworkStream networkStream = senderClient.GetStream();

                    // Send file name and size
                    string fileName = Path.GetFileName(filePath);
                    byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
                    byte[] fileNameLengthBytes = BitConverter.GetBytes(fileNameBytes.Length);
                    byte[] fileSizeBytes = BitConverter.GetBytes(new FileInfo(filePath).Length);

                    networkStream.Write(fileNameLengthBytes, 0, 4);
                    networkStream.Write(fileNameBytes, 0, fileNameBytes.Length);
                    networkStream.Write(fileSizeBytes, 0, 8);

                    Console.WriteLine($"Sending file: {fileName}");

                    // Send file data
                    using (FileStream fileStream = File.OpenRead(filePath))
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        long totalBytesSent = 0;
                        DateTime startTime = DateTime.Now;
                        System.Timers.Timer speedTimer = new System.Timers.Timer(1000);
                        speedTimer.Elapsed += (sender, e) =>
                        {
                            TimeSpan elapsedTime = DateTime.Now - startTime;
                            double speed = totalBytesSent / elapsedTime.TotalSeconds;
                            double remainingTime = (fileStream.Length - totalBytesSent) / speed;

                            Console.WriteLine($"Speed: {FormatBytes(speed)}/s, Remaining Time: {FormatTime(remainingTime)}");
                        };
                        speedTimer.Start();

                        while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            networkStream.Write(buffer, 0, bytesRead);
                            totalBytesSent += bytesRead;
                        }

                        speedTimer.Stop();
                    }

                    Console.WriteLine("File sent successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }

    static void LocalFileSender()
    {

        while (true)
        {
             Console.WriteLine("Local File Sender Mode");

            Console.Write("Enter the path of the file to send: ");
            string filePath = Console.ReadLine();

            Console.Write("Enter the receiver's IP address: ");
            string receiverIpAddress = Console.ReadLine();

            Console.Write("Enter the port to connect to on the receiver's side: ");
            int port = int.Parse(Console.ReadLine());

            try
            {
                using (TcpClient senderClient = new TcpClient(receiverIpAddress, port))
                {
                    NetworkStream networkStream = senderClient.GetStream();

                    // Send file name and size
                    string fileName = Path.GetFileName(filePath);
                    byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
                    byte[] fileNameLengthBytes = BitConverter.GetBytes(fileNameBytes.Length);
                    byte[] fileSizeBytes = BitConverter.GetBytes(new FileInfo(filePath).Length);

                    networkStream.Write(fileNameLengthBytes, 0, 4);
                    networkStream.Write(fileNameBytes, 0, fileNameBytes.Length);
                    networkStream.Write(fileSizeBytes, 0, 8);

                    Console.WriteLine($"Sending file: {fileName}");

                    // Send file data
                    using (FileStream fileStream = File.OpenRead(filePath))
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        long totalBytesSent = 0;

                        DateTime startTime = DateTime.Now;
                        System.Timers.Timer speedTimer = new System.Timers.Timer(1000);
                        speedTimer.Elapsed += (sender, e) =>
                        {
                            TimeSpan elapsedTime = DateTime.Now - startTime;
                            double speed = totalBytesSent / elapsedTime.TotalSeconds;
                            double remainingTime = (fileStream.Length - totalBytesSent) / speed;

                            Console.WriteLine($"Speed: {FormatBytes(speed)}/s, Remaining Time: {FormatTime(remainingTime)}");
                        };
                        speedTimer.Start();

                        while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            networkStream.Write(buffer, 0, bytesRead);
                            totalBytesSent += bytesRead;
                        }

                        speedTimer.Stop();
                    }

                    Console.WriteLine("File sent successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
           
        

    }

    static void Receiver()
    {
        while (true)
        {
            Console.WriteLine("Receiver Mode");

            Console.Write("Enter the port to listen on: ");
            int port = int.Parse(Console.ReadLine());

            string globalIpAddress = GetGlobalIpAddressAsync().Result;
            Console.WriteLine($"Global IP Address: {globalIpAddress}, Port: {port}");

            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            Console.WriteLine("Waiting for a sender to connect...");

            try
            {
                using (TcpClient senderClient = listener.AcceptTcpClient())
                {
                    Console.WriteLine("Sender connected.");

                    NetworkStream networkStream = senderClient.GetStream();

                    // Receive file name and size
                    byte[] fileNameLengthBytes = new byte[4];
                    networkStream.Read(fileNameLengthBytes, 0, 4);
                    int fileNameLength = BitConverter.ToInt32(fileNameLengthBytes, 0);

                    byte[] fileNameBytes = new byte[fileNameLength];
                    networkStream.Read(fileNameBytes, 0, fileNameLength);
                    string fileName = Encoding.UTF8.GetString(fileNameBytes);

                    byte[] fileSizeBytes = new byte[8];
                    networkStream.Read(fileSizeBytes, 0, 8);
                    long fileSize = BitConverter.ToInt64(fileSizeBytes, 0);

                    Console.WriteLine($"Receiving file: {fileName}, Size: {fileSize} bytes");

                    // Receive file data
                    using (FileStream fileStream = File.Create(fileName))
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        long totalBytesReceived = 0;
                        DateTime startTime = DateTime.Now;
                        System.Timers.Timer speedTimer = new System.Timers.Timer(1000);
                        speedTimer.Elapsed += (sender, e) =>
                        {
                            TimeSpan elapsedTime = DateTime.Now - startTime;
                            double speed = totalBytesReceived / elapsedTime.TotalSeconds;
                            double remainingTime = (fileSize - totalBytesReceived) / speed;

                            Console.WriteLine($"Speed: {FormatBytes(speed)}/s, Remaining Time: {FormatTime(remainingTime)}");
                        };
                        speedTimer.Start();

                        while ((bytesRead = networkStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            fileStream.Write(buffer, 0, bytesRead);
                            totalBytesReceived += bytesRead;
                        }

                        speedTimer.Stop();
                    }

                    Console.WriteLine("File received successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            listener.Stop();
        }
    }

    static void LocalFileReceiver()
    {
        while (true)
        {
            Console.WriteLine("Local File Receiver Mode");

            Console.Write("Enter the port to listen on: ");
            int port = int.Parse(Console.ReadLine());

            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            Console.WriteLine($"Listening on IP: {GetLocalIpAddress()} and Port: {port}");
            Console.WriteLine("Waiting for a sender to connect...");

            try
            {
                using (TcpClient senderClient = listener.AcceptTcpClient())
                {
                    Console.WriteLine("Sender connected.");

                    NetworkStream networkStream = senderClient.GetStream();

                    // Receive file name and size
                    byte[] fileNameLengthBytes = new byte[4];
                    networkStream.Read(fileNameLengthBytes, 0, 4);
                    int fileNameLength = BitConverter.ToInt32(fileNameLengthBytes, 0);

                    byte[] fileNameBytes = new byte[fileNameLength];
                    networkStream.Read(fileNameBytes, 0, fileNameLength);
                    string fileName = Encoding.UTF8.GetString(fileNameBytes);

                    byte[] fileSizeBytes = new byte[8];
                    networkStream.Read(fileSizeBytes, 0, 8);
                    long fileSize = BitConverter.ToInt64(fileSizeBytes, 0);

                    Console.WriteLine($"Receiving file: {fileName}, Size: {fileSize} bytes");

                    // Receive file data
                    using (FileStream fileStream = File.Create(fileName))
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        long totalBytesReceived = 0;

                        DateTime startTime = DateTime.Now;
                        System.Timers.Timer speedTimer = new System.Timers.Timer(1000);
                        speedTimer.Elapsed += (sender, e) =>
                        {
                            TimeSpan elapsedTime = DateTime.Now - startTime;
                            double speed = totalBytesReceived / elapsedTime.TotalSeconds;
                            double remainingTime = (fileSize - totalBytesReceived) / speed;

                            Console.WriteLine($"Speed: {FormatBytes(speed)}/s, Remaining Time: {FormatTime(remainingTime)}");
                        };
                        speedTimer.Start();

                        while ((bytesRead = networkStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            fileStream.Write(buffer, 0, bytesRead);
                            totalBytesReceived += bytesRead;
                        }

                        speedTimer.Stop();
                    }

                    Console.WriteLine("File received successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            listener.Stop();
        }
    }
        


    static async Task<string> GetGlobalIpAddressAsync()
    {
        using (var httpClient = new HttpClient())
        {
            try
            {
                var response = await httpClient.GetStringAsync("https://api.ipify.org?format=text");
                return response.Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to retrieve global IP address: {ex.Message}");
                return "Unknown";
            }
        }
    }

    static string GetLocalIpAddress()
    {
        string hostName = Dns.GetHostName();
        IPHostEntry entry = Dns.GetHostEntry(hostName);

        foreach (IPAddress address in entry.AddressList)
        {
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                return address.ToString();
            }
        }

        return "127.0.0.1";
    }

    static string FormatBytes(double bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int suffixIndex = 0;
        while (bytes >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            bytes /= 1024;
            suffixIndex++;
        }

        return $"{bytes:0.00} {suffixes[suffixIndex]}";
    }

    static string FormatTime(double seconds)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
        return timeSpan.ToString(@"hh\:mm\:ss");
    }
}



