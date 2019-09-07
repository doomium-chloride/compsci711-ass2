using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ass2
{
    public partial class Middleware
    {
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

        HashSet<int> confirmed = new HashSet<int>();

        Socket sendSocket;

        public int getID()
        {
            return middleWareID;
        }

        // This method sets up a socket for receiving messages from the Network
        async void ReceiveMulticast()
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

            //Console.WriteLine("Press ENTER to continue ...");
            //Console.ReadLine();

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

              

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        //Increments after returning
        string genTimestamp()
        {
            return timestring(++timestamp);
        }

        //Don't increment, just return with added $$
        string timestring(int time)
        {
            return "$" + time + "$";
        }
        string getTimestamp(int otherStamp)
        {
            timestamp = Math.Max(timestamp + 1, otherStamp);
            return timestring(timestamp);
        }


        public async void sendMessage()
        {
            try
            {

                // Create a TCP/IP  socket.
                sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

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

                await isSocketFree();
                sendSocket.Send(msg);

                updateList(listSent, message);

                //close socket
                sendSocket.Shutdown(SocketShutdown.Both);
                sendSocket.Close();

            

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

        async void proposeTimestamp(string message, int msgStamp)
        {
            // Create a TCP/IP  socket.
            sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Connect to the Network 
            sendSocket.Connect(remoteEP);

            string proposal = "timestamp:" + message + getTimestamp(msgStamp) + middleWareID + "<EOM>\n";
            // Generate and encode the multicast message into a byte array.
            byte[] msg = Encoding.ASCII.GetBytes(proposal);
            // Send the data to the network.

            await isSocketFree();

            sendSocket.Send(msg);

            //close socket
            sendSocket.Shutdown(SocketShutdown.Both);
            sendSocket.Close();

        }

        async void confirmTimestamp(string message, int finalTimestamp)
        {
            // Create a TCP/IP  socket.
            sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Connect to the Network 
            sendSocket.Connect(remoteEP);

            //Check if same timestamp has been used

            while (confirmed.Contains(finalTimestamp))
            {
                ++finalTimestamp;
            }

            string shirase = "confirm:" + message + timestring(finalTimestamp) + middleWareID + "<EOM>\n";
            // Generate and encode the multicast message into a byte array.
            byte[] msg = Encoding.ASCII.GetBytes(shirase);
            // Send the data to the network.
            Console.WriteLine("sending confirmation");

            await isSocketFree();
            confirmed.Add(finalTimestamp);
            sendSocket.Send(msg);

            //close socket
            sendSocket.Shutdown(SocketShutdown.Both);
            sendSocket.Close();

        }

        string timeOrder(int timestamp, int id)
        {
            return String.Format("{0:D8}.{1:D2}", timestamp, id);
        }

        void deliver()
        {
            List<KeyValuePair<string, string>> keyList = deliverable.ToList();

            foreach (KeyValuePair<string, string> item in keyList)
            {
                if (received.ContainsKey(item.Value))
                {
                    List<KeyValuePair<string, int>> receivedList = sortReceived();

                    if (!receivedList[0].Key.Equals(item.Value)) break;

                    string timestampString = item.Key.Split('.')[0];
                    int thisTimestamp = Int32.Parse(timestampString);
                    string delivery = item.Value + "$" + thisTimestamp;
                    ready.Enqueue(delivery);
                    received.Remove(item.Value);
                    deliverable.Remove(item.Key);

                    updateList(listReady, delivery);
                }
            }
            //print ready
            foreach (string str in ready)
            {
                Console.WriteLine("ready:{0}", str);
            }
        }

        void updateTimestamp(int newTime)
        {
            timestamp = Math.Max(timestamp, newTime) + 1;
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
                int proposeTime = dataTime;
                received.Add(dataBody, proposeTime);
                if (midWareID != middleWareID)
                    proposeTimestamp(dataBody, proposeTime);
                Console.WriteLine("received message:{0}", data);
                updateList(listReceived, data);
            }
            else if (midWareID == middleWareID && dataBody.StartsWith("t"))
            {
                // int[]{ timestamp, middlewares replied. starts at 1}
                string messageBody = dataBody.Split(':')[1];
                int[] current_status = sent[messageBody];
                current_status[0] = Math.Max(current_status[0], dataTime);
                current_status[1]++;
                Console.WriteLine(current_status[1]);
                if (current_status[1] == midwarecount)
                {
                    Console.WriteLine("entered confirm zone");
                    sent.Remove(messageBody);
                    confirmTimestamp(messageBody, current_status[0]);
                }
                Console.WriteLine("received timestamp:{0}", data);
            }
            else if (dataBody.StartsWith("c"))
            {
                string messageBody = dataBody.Split(':')[1];
                int finalTimestamp = dataTime;
                string deliveryKey = timeOrder(finalTimestamp, midWareID);
                updateTimestamp(finalTimestamp);
                Console.WriteLine("Adding to deliverable:{0}", deliveryKey);
                deliverable.Add(deliveryKey, messageBody);
                received[messageBody] = finalTimestamp;
                Console.WriteLine("received final:{0}", data);
                foreach (string x in deliverable.Keys)
                {
                    Console.WriteLine("deliverable:{0} {1}", deliverable[x], x);
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
                    int comparison = pair1.Value.CompareTo(pair2.Value);
                    if (comparison != 0)
                        return pair1.Value.CompareTo(pair2.Value);
                    else
                    {
                        int midware1 = Int32.Parse(pair1.Key.Split('@')[1]);
                        int midware2 = Int32.Parse(pair2.Key.Split('@')[1]);

                        return midware1.CompareTo(midware2);
                    }
                }
            );

            foreach (KeyValuePair<string, int> item in myList)
            {
                Console.WriteLine("Sorting: {0} - {1}", item.Key, item.Value);
            }

            return myList;
        }

        void updateList(ListBox list, string message)
        {
            list.BeginUpdate();
            list.Items.Add(message);
            list.EndUpdate();
        }

        async Task<bool> isSocketFree()
        {
            while (sendSocket.Poll(1000, SelectMode.SelectRead) && (sendSocket.Available == 0))
            {
                await Task.Delay(25);
            }
            return true;
        }
    }
}