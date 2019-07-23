using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Ports;
using System.Threading;
using System.IO;
using Microsoft.Win32;
using MaterialDesignThemes.Wpf;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using ToastNotifications.Messages;

namespace SPHelper
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private SerialPort _serialPort = new SerialPort();//声明串口

        private static string[] _ports;//声明串口数组

        IList<portdetail> portlist = new List<portdetail>();//创建下拉串口列表

        IList<portdetail> rateList = new List<portdetail>();//创建下拉波特率列表

        IList<portdetail> dataBits = new List<portdetail>();//创建下拉数据位列表

        IList<portdetail> stopBits = new List<portdetail>();//创建下拉停止位列表

        IList<portdetail> comParity = new List<portdetail>();//创建下拉校验位列表

        public bool? Hexmode;

        Notifier notifier = new Notifier(cfg =>
        {
            cfg.PositionProvider = new WindowPositionProvider(
                parentWindow: Application.Current.MainWindow,
                corner: Corner.BottomLeft,
                offsetX: 10,
                offsetY: 10);

            cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                notificationLifetime: TimeSpan.FromSeconds(1),
                maximumNotificationCount: MaximumNotificationCount.FromCount(3));

            cfg.Dispatcher = Application.Current.Dispatcher;
        });

        public class portdetail
        {
            public string com { get; set; }
            public string BaudRate { get; set; }
            public string Dbits { get; set; }
            public string Sbits { get; set; }
            public string ParityValue { get; set; }
            public string Parity { get; set; }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _ports = SerialPort.GetPortNames();
            if (_ports.Length > 0)
            {
                foreach (string port in _ports)
                {
                    portlist.Add(new portdetail() { com = port });
                }
                com_list.ItemsSource = portlist;
                com_list.DisplayMemberPath = "com";
                com_list.SelectedValuePath = "com";
                com_list.SelectedValue = _ports[0];
                //ReceivedMsg.AppendText("Port List Loaded.");
                ComOpen.IsEnabled = true;
            }
            else
            {
                //MessageBox.Show("No Serial Port Available!");
                notifier.ShowWarning("No Serial Port Available!");
            }

            rateList.Add(new portdetail() { BaudRate = "1200" });
            rateList.Add(new portdetail() { BaudRate = "2400" });
            rateList.Add(new portdetail() { BaudRate = "4800" });
            rateList.Add(new portdetail() { BaudRate = "9600" });
            rateList.Add(new portdetail() { BaudRate = "14400" });
            rateList.Add(new portdetail() { BaudRate = "19200" });
            rateList.Add(new portdetail() { BaudRate = "28800" });
            rateList.Add(new portdetail() { BaudRate = "38400" });
            rateList.Add(new portdetail() { BaudRate = "57600" });
            rateList.Add(new portdetail() { BaudRate = "115200" });
            buad_rate.ItemsSource = rateList;
            buad_rate.DisplayMemberPath = "BaudRate";
            buad_rate.SelectedValuePath = "BaudRate";
            buad_rate.SelectedIndex = 3;

            comParity.Add(new portdetail() { Parity = "None", ParityValue = "0" });
            comParity.Add(new portdetail() { Parity = "Odd", ParityValue = "1" });
            comParity.Add(new portdetail() { Parity = "Even", ParityValue = "2" });
            comParity.Add(new portdetail() { Parity = "Mark", ParityValue = "3" });
            comParity.Add(new portdetail() { Parity = "Space", ParityValue = "4" });
            ParityComCbobox.ItemsSource = comParity;
            ParityComCbobox.DisplayMemberPath = "Parity";
            ParityComCbobox.SelectedValuePath = "ParityValue";
            ParityComCbobox.SelectedIndex = 0;

            dataBits.Add(new portdetail() { Dbits = "8" });
            dataBits.Add(new portdetail() { Dbits = "7" });
            dataBits.Add(new portdetail() { Dbits = "6" });
            DataBitsCbobox.ItemsSource = dataBits;
            DataBitsCbobox.SelectedValuePath = "Dbits";
            DataBitsCbobox.DisplayMemberPath = "Dbits";
            DataBitsCbobox.SelectedIndex = 0;

            stopBits.Add(new portdetail() { Sbits = "1" });
            stopBits.Add(new portdetail() { Sbits = "1.5" });
            stopBits.Add(new portdetail() { Sbits = "2" });
            StopBitsCbobox.ItemsSource = stopBits;
            StopBitsCbobox.SelectedValuePath = "Sbits";
            StopBitsCbobox.DisplayMemberPath = "Sbits";
            StopBitsCbobox.SelectedIndex = 0;

            //Reloadbtn.Content = new PackIcon { Kind = PackIconKind.Refresh,Foreground= System.Windows.Media.Brushes.Black};
        }
        //private void Button_Click(object sender, RoutedEventArgs e)
        //{
        //    ReloadPort();
        //}

        private void ReloadPort()
        {
            portlist.Clear();
            com_list.DisplayMemberPath = null;
            com_list.SelectedValuePath = null;
            _ports = new string[SerialPort.GetPortNames().Length];
            _ports = SerialPort.GetPortNames();

            if (_ports.Length > 0)
            {
                foreach (string port in _ports)
                {
                    portlist.Add(new portdetail() { com = port });
                }
                com_list.ItemsSource = portlist;
                com_list.DisplayMemberPath = "com";
                com_list.SelectedValuePath = "com";
                com_list.SelectedValue = _ports[0];
                //ReceivedMsg.Clear();
                notifier.ShowSuccess("Port List Reloading Successful.");
            }
        }

        private void ComOpen_Click(object sender, RoutedEventArgs e)
        {
            if (com_list.SelectedValue == null)
            {
                //MessageBox.Show("No COM Port Selected!");
                notifier.ShowError("No COM Port Selected!");
                return;
            }

            if (_serialPort.IsOpen == false)
            {
                try
                {
                    _serialPort.PortName = com_list.SelectedValue.ToString();
                    _serialPort.BaudRate = Convert.ToInt32(buad_rate.SelectedValue);
                    _serialPort.Parity = (Parity)Convert.ToInt32(ParityComCbobox.SelectedValue);
                    _serialPort.StopBits = (StopBits)Convert.ToDouble(StopBitsCbobox.SelectedValue);
                    _serialPort.DataBits = Convert.ToInt32(DataBitsCbobox.SelectedValue);
                    _serialPort.ReadTimeout = 2000;
                    _serialPort.WriteTimeout = 2000;
                    _serialPort.ReadBufferSize = 10240;
                    _serialPort.WriteBufferSize = 10240;
                    _serialPort.Open();
                    ComSend.IsEnabled = _serialPort.IsOpen;
                    _serialPort.DataReceived += new SerialDataReceivedEventHandler(ReceiveMessage);
                }
                catch
                {
                    //MessageBox.Show("Port is occupied or invalid PortName!\nPlease refresh Com Device List and try again.");
                    notifier.ShowError("Port Unavailable!");
                    return;
                }
                ComOpen.Content = "Close";
                //AppendTextBox(_serialPort.PortName + " Opened.");
                notifier.ShowSuccess(_serialPort.PortName + " Opened.");
                ComSend.IsEnabled = _serialPort.IsOpen;
                com_list.IsEnabled = false;
                buad_rate.IsEnabled = false;
                ParityComCbobox.IsEnabled = false;
                DataBitsCbobox.IsEnabled = false;
                StopBitsCbobox.IsEnabled = false;
                reloadport.IsEnabled = false;
                ComOpen.MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));
            }
            else
            {
                try
                {
                    _serialPort.Close();
                    ComOpen.Content = "Open";
                    //AppendTextBox(_serialPort.PortName + " Closed.");
                    notifier.ShowInformation(_serialPort.PortName + " Closed.");
                    //_serialPort.DiscardOutBuffer();
                    //_serialPort.DiscardInBuffer();
                    ComSend.IsEnabled = false;
                    com_list.IsEnabled = true;
                    buad_rate.IsEnabled = true;
                    ParityComCbobox.IsEnabled = true;
                    DataBitsCbobox.IsEnabled = true;
                    StopBitsCbobox.IsEnabled = true;
                    reloadport.IsEnabled = true;
                }
                catch
                {
                    //MessageBox.Show("Can't Close COM Port!");
                    notifier.ShowError("Can't Close COM Port!");
                    return;
                }
            }

        }
        private void ComSend_Click(object sender, RoutedEventArgs e)
        {
            Send();
            ComSend.MoveFocus(new TraversalRequest(FocusNavigationDirection.Up));
        }

        private void Send()
        {
            string sendData = SendBox.Text;
            byte[] sendBuffer = null;
            if (hex.IsChecked != true)
            { 
                try
                {
                    _serialPort.Write(sendData+"\r\n");
                    SendBox.Clear();
                    //_serialPort.Write(sendCharArray, 0, sendCharArray.Length);
                    //_serialPort.Write(sendByteArray, 0, sendByteArray.Length);
                }
                catch
                {
                    //MessageBox.Show("Unable to send");
                    notifier.ShowError("Unable to send");
                    return;
                }
            }
            else
            {
                try
                {
                    sendData = sendData.Replace(" ", "");
                    sendData = sendData.Replace("\r", "");
                    sendData = sendData.Replace("\n", "");
                    if (sendData.Length == 1)
                    {
                        sendData = "0" + sendData;
                    }
                    else if (sendData.Length % 2 != 0)
                    {
                        sendData = sendData.Remove(sendData.Length - 1, 1);
                    }

                    List<string> sendData16 = new List<string>();
                    for (int i = 0; i < sendData.Length; i += 2)
                    {
                        sendData16.Add(sendData.Substring(i, 2));
                    }
                    sendBuffer = new byte[sendData16.Count];
                    for (int i = 0; i < sendData16.Count; i++)
                    {
                        sendBuffer[i] = (byte)(Convert.ToInt32(sendData16[i], 16));
                    }
                }
                catch
                {
                    //MessageBox.Show("HEX Message Only");
                    notifier.ShowWarning("HEX Message Only");
                    return;
                }
                try
                {
                    _serialPort.Write(sendBuffer, 0, sendBuffer.Length);
                    SendBox.Clear();
                }
                catch
                {
                    //MessageBox.Show("Unable to send");
                    notifier.ShowError("Unable to send");
                    return;
                }
            }
        }

        private void ClearMsg(object sender, RoutedEventArgs e)
        {
            ReceivedMsg.Clear();
        }

        //private void AppendTextBox(string appendix)
        //{
        //    if (ReceivedMsg.Text.Trim().Length <= 0)
        //    {
        //        ReceivedMsg.AppendText(DateTime.Now.ToString("hh:mm:ss") + " : " + appendix);
        //        ReceivedMsg.ScrollToEnd();
        //    }
        //    else
        //    {
        //        ReceivedMsg.AppendText("\n" + DateTime.Now.ToString("hh:mm:ss") + " : " + appendix);
        //        ReceivedMsg.ScrollToEnd();
        //    }
        //}

        private void AppendTextBox(string appendix)
        {
                ReceivedMsg.AppendText(appendix);
                ReceivedMsg.ScrollToEnd();
        }

        private void ReceiveMessage(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] recData = new byte[_serialPort.BytesToRead];
            _serialPort.Read(recData, 0, recData.Length);
            
            string Msg = Encoding.Default.GetString(recData);
            this.ReceivedMsg.Dispatcher.Invoke(
                delegate
                {
                    if (hex.IsChecked != true)
                    {
                        AppendTextBox(Msg);
                    }
                    else
                    {
                        StringBuilder recBuffer16 = new StringBuilder();
                        for (int i = 0; i < recData.Length; i++)
                        {
                            recBuffer16.AppendFormat("{0:X2}" + " ", recData[i]);
                        }
                        AppendTextBox(recBuffer16.ToString());
                    }
                }
                );
            }
         
        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            //dlg.DefaultExt = ".txt";
            //dlg.Filter ="Text Files|*.txt;*.json;*.xml;*.xaml;*.js;*.cs;*.config" + "|All Files|*.*";
            dlg.Filter = "All Files|*.*" + "|Text Files|*.txt;*.json;*.xml;*.xaml;*.js;*.cs;*.config";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                FileStream fs = new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read);
                StreamReader m_streamReader = new StreamReader(fs);
                m_streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
                string strLine = m_streamReader.ReadLine();
                while (strLine != null)
                {
                    if (SendBox.Text.Trim().Length <= 0)
                    {
                        SendBox.Text += strLine;
                    }
                    else
                    {
                        SendBox.Text += "\n" + strLine;
                    }
                    strLine = m_streamReader.ReadLine();
                }
                m_streamReader.Close();
            }
            OpenFile.MoveFocus(new TraversalRequest(FocusNavigationDirection.Up));
        }

        private void SendBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
            if (e.Key == Key.RightCtrl)
            {
                SendBox.Text += "\r\n";
                SendBox.SelectionStart = SendBox.Text.Length;
            }
        }

        private void SaveToFile_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = DateTime.Now.ToString("hhmmss");
            saveFileDialog.DefaultExt = ".txt";
            saveFileDialog.Filter = "Text file (*.txt)|*.txt|C# file (*.cs)|*.cs";
            //Nullable<bool> result = saveFileDialog.ShowDialog();
            //if (result == true)
            //{
            //    string filename = saveFileDialog.FileName;
            //}
            if (saveFileDialog.ShowDialog() == true)
                File.WriteAllText(saveFileDialog.FileName, ReceivedMsg.Text);
        }

        private void PopupBox_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            ReloadPort();
        }
    }
}
