using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;

public class Middleware
{
    // Depends on the folder

    const int myPort = 8083;

    int middleWareID = 2;

    // Below this it should be the same

    IPEndPoint remoteEP;

    const int midwarecount = 5;

    int msgNum = 0;

    int timestamp = 0;

    Queue<string> ready = new Queue<string>();

    Dictionary<string, int> received =
            new Dictionary<string, int>();

    SortedDictionary<string, string> deliverable =
            new SortedDictionary<string, string>();

    Dictionary<string, int[]> sent =
            new Dictionary<string, int[]>();

    Dictionary<string, int> timestampConfirmed =
            new Dictionary<string, int>();

    Socket sendSocket;

    // This method sets up a socket for receiving messages from the Network
    private async void ReceiveMulticast()
    {
        // Data buffer for incoming data.
        byte[] bytes = new Byte[1024];

        // Determine the IP address of localhost
        IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
        IPAddress ipAddress = null;
        foreach (IPAddress ip in ipHostInfo.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                ipAddress = ip;
                break;
            }
        }

        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, myPort);

        // Create a TCP/IP socket for receiving message from the Network.
        TcpListener listener = new TcpListener(localEndPoint);
        listener.Start(10);

        try
        {
            string data = null;

            // Start listening for connections.
            while (true)
            {
                Console.WriteLine("Waiting for a connection...");

                // Program is suspended while waiting for an incoming connection.
                TcpClient tcpClient = await listener.AcceptTcpClientAsync();

                Console.WriteLine("connectted");
                data = null;

                // Receive one message from the network
                while (true)
                {
                    bytes = new byte[1024];
                    NetworkStream readStream = tcpClient.GetStream();
                    int bytesRec = await readStream.ReadAsync(bytes, 0, 1024);
                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec);

                    // All messages ends with "<EOM>"
                    // Check whether a complete message has been received
                    if (data.IndexOf("<EOM>") > -1)
                    {
                        break;
                    }
                }
                processData(data);
                //Console.WriteLine("msg received:    {0}", data);

            }

        }
        catch (Exception ee)
        {
            Console.WriteLine(ee.ToString());
        }
    }

    // This method first sets up a task for receiving messages from the Network.
    // Then, it sends a multicast message to the Netwrok.
    public void DoWork()
    {
        Console.WriteLine("This is MW" + middleWareID);
        // Sets up a task for receiving messages from the Network.
        ReceiveMulticast();

        Console.WriteLine("Press ENTER to continue ...");
        Console.ReadLine();

        // Send a multicast message to the Network
        try
        {
            // Find the IP address of localhost
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = null;
            foreach (IPAddress ip in ipHostInfo.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipAddress = ip;
                    break;
                }
            }
            remoteEP = new IPEndPoint(ipAddress, 8081);

            try
            {

                bool terminate = false;

                // Create a TCP/IP  socket.
                sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                do
                {
                    // Create a TCP/IP  socket.
                    //sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    // Connect to the Network 
                    //sendSocket.Connect(remoteEP);

                    // send Message

                    Console.WriteLine("Enter x to send message and close. Enter to just send message...");
                    string continue_code = Console.ReadLine();
                    if (continue_code.Equals("x"))
                        terminate = true;

                    // Send the data to the network.
                    sendMessage();

                    //close socket
                    //sendSocket.Shutdown(SocketShutdown.Both);
                    //sendSocket.Close();
                } while (!terminate);


            }
            catch (ArgumentNullException ane)
            {
                Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
            }
            catch (SocketException se)
            {
                Console.WriteLine("SocketException : {0}", se.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.ToString());
            }

        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    public static int Main(String[] args)
    {
        Middleware m = new Middleware();
        m.DoWork();
        return 0;
    }

    //Increments after returning
    string genTimestamp()
    {
        return "$" + timestamp++ + "$";
    }

    //Don't increment, just return with added $$
    string timestring(int time)
    {
        return "$" + time + "$";
    }
    string getTimestamp(int otherStamp)
    {
        return timestring(Math.Max(timestamp, otherStamp));
    }
    string getTimestamp()
    {
        return timestring(timestamp);
    }


    public void sendMessage()
    {

        // Connect to the Network 
        sendSocket.Connect(remoteEP);

        // Generate message and increment
        string body = "Msg #" + msgNum++ + " from Middleware @" + middleWareID;

        string message = body + genTimestamp() + middleWareID + "<EOM>\n";

        sent.Add(body, new int[] { timestamp, 1 });

        // Generate and encode the multicast message into a byte array.
        //byte[] msg = Encoding.ASCII.GetBytes("From "+myPort + ": This is a test<EOM>\n");
        byte[] msg = Encoding.ASCII.GetBytes(message);

        // Send the data to the network.
        sendSocket.Send(msg);

        // Create a TCP/IP  socket.
        sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // Connect to the Network 
        sendSocket.Connect(remoteEP);
    }

    void updateTimestamp(int newTime)
    {
        timestamp = Math.Max(newTime, timestamp);
    }

    void proposeTimestamp(string message, int msgStamp)
    {
        // Create a TCP/IP  socket.
        sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // Connect to the Network 
        sendSocket.Connect(remoteEP);

        string proposal = "timestamp:" + message + getTimestamp(msgStamp) + middleWareID + "<EOM>\n";
        // Generate and encode the multicast message into a byte array.
        byte[] msg = Encoding.ASCII.GetBytes(proposal);
        // Send the data to the network.
        Console.WriteLine("proposed own timestammp");
        sendSocket.Send(msg);

        // Create a TCP/IP  socket.
        sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // Connect to the Network 
        sendSocket.Connect(remoteEP);
    }

    void confirmTimestamp(string message, int finalTimestamp)
    {
        string shirase = "confirm:" + message + timestring(finalTimestamp) + middleWareID + "<EOM>\n";
        // Generate and encode the multicast message into a byte array.
        byte[] msg = Encoding.ASCII.GetBytes(shirase);
        // Send the data to the network.
        Console.WriteLine("sending confirmation");
        sendSocket.Send(msg);

        // Create a TCP/IP  socket.
        sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // Connect to the Network 
        sendSocket.Connect(remoteEP);
    }

    string timeOrder(int timestamp, int id)
    {
        return String.Format("{0:D8}.{1:D8}", timestamp, id);
    }

    void deliver()
    {
        List<KeyValuePair<string, string>> keyList = deliverable.ToList();

        foreach (KeyValuePair<string, string> item in keyList)
        {
            if (received.ContainsKey(item.Value))
            {
                List<KeyValuePair<string, int>> receivedList = sortReceived();

                if (receivedList[0].Key != item.Value) return;

                string timestampString = item.Key.Split('.')[0];
                int thisTimestamp = Int32.Parse(timestampString);
                string delivery = item.Value + "$" + thisTimestamp;
                ready.Enqueue(delivery);
                received.Remove(item.Value);
                deliverable.Remove(item.Key);
            }
        }
        //print ready
        foreach (string str in ready)
        {
            Console.WriteLine("ready:{0}", str);
        }
    }

    void processData(string data)
    {
        // format is... info$timestamp$eom
        string[] dataFrag = data.Split('$');
        int dataTime = Int32.Parse(dataFrag[1]);
        string dataBody = dataFrag[0];
        int midWareID = Int32.Parse(dataBody.Split('@')[1]);
        Console.WriteLine("middleware id = {0}", midWareID);
        if (dataBody.StartsWith("M"))
        {
            int proposeTime = Math.Max(dataTime, timestamp);
            received.Add(dataBody, proposeTime);
            if (midWareID != middleWareID)
                proposeTimestamp(dataBody, proposeTime);
            Console.WriteLine("received message:{0}", data);
        }
        else if (midWareID == middleWareID && dataBody.StartsWith("t"))
        {
            // int[]{ timestamp, middlewares replied. starts at 1
            string messageBody = dataBody.Split(':')[1];
            int[] current_status = sent[messageBody];
            current_status[0] = Math.Max(current_status[0], dataTime);
            current_status[1]++;
            Console.WriteLine(current_status[1]);
            if (current_status[1] == midwarecount)
            {
                Console.WriteLine("entered confirm zone");
                timestampConfirmed.Add(messageBody, current_status[0]);
                confirmTimestamp(messageBody, current_status[0]);
                //sent.Remove(messageBody);
            }
            Console.WriteLine("received timestamp:{0}", data);
        }
        else if (dataBody.StartsWith("c"))
        {
            string messageBody = dataBody.Split(':')[1];
            int finalTimestamp = dataTime;
            string deliveryKey = timeOrder(finalTimestamp, midWareID);
            Console.WriteLine("Adding to deliverable:{0}", deliveryKey);
            deliverable.Add(deliveryKey, messageBody);
            Console.WriteLine("received final:{0}", data);
            foreach (string x in deliverable.Values)
            {
                Console.WriteLine("deliverable:{0}", x);
            }
        }
        deliver();
    }

    List<KeyValuePair<string, int>> sortReceived()
    {
        List<KeyValuePair<string, int>> myList = received.ToList();

        myList.Sort(
            delegate (KeyValuePair<string, int> pair1,
            KeyValuePair<string, int> pair2)
            {
                return pair1.Value.CompareTo(pair2.Value);
            }
        );

        return myList;
    }
}