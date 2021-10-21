using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace modbusRtuTest
{
  class Program
  {
    static void Main(string[] args)
    {
      Modbus modbus = new Modbus();
      var sendData = Utill.stringToByte(modbus.getRtuProtocol("1", "3", "0", "10"));

      var serialPort = new SerialPort();
      serialPort.PortName = "COM30";
      serialPort.BaudRate = 9600;
      serialPort.DataBits = 8;
      serialPort.Parity = Parity.None;
      serialPort.StopBits = StopBits.One;

      serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

      serialPort.Open();
      while (true)
      {
        serialPort.Write(sendData, 0, sendData.Length);
        if (Console.ReadLine() == "exit") break;
      }
      serialPort.Close();
    }

    private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
    {
      var serialPort = (SerialPort)sender;
      byte[] data = new byte[serialPort.BytesToRead];

      serialPort.Read(data, 0, data.Length);

      Console.Write("Data Received: ");
      Console.WriteLine(BitConverter.ToString(data));
    }
  }

  class Utill
  {
    public static byte[] stringToByte(string data, char separator = '_')
    {
      var result = new List<byte>();

      var hexStrs = data.Split(separator);
      foreach (string hexStr in hexStrs) result.Add(Convert.ToByte(hexStr, 16));

      return result.ToArray();
    }
  }

  class Modbus
  {
    private const ushort CRC_16_IBM_MODBUS = 0xa001;
    private static ushort[] crcTable = generateCrcTable();

    private static ushort[] generateCrcTable()
    {
      ushort[] result = new ushort[256];

      for (ushort i = 0; i < 256; i++)
      {
        var crc = i;

        for (var j = 0; j < 8; j++)
          crc = (crc & 1) == 1 ? (ushort)((crc >> 1) ^ CRC_16_IBM_MODBUS) : (ushort)(crc >> 1);

        result[i] = crc;
      }

      return result;
    }

    private string getCrc(byte[] datas)
    {
      ushort crc = 0xffff;

      foreach (var data in datas) crc = (ushort)((crc >> 8) ^ crcTable[(crc ^ data) & 0xff]);

      return crc.ToString("X4").Insert(2, "_");
    }

    private string reverse(string strData)
    {
      var arrData = strData.Split('_');
      Array.Reverse(arrData);

      var result = "";
      foreach (var data in arrData) result += $"{data}";

      return result.Insert(2, "_");
    }

    private string toHexString(string data, int format = 1)
    {
      data = int.Parse(data).ToString($"X{format * 2}");

      var result = "";
      for (var i = 0; i < data.Length; i++)
      {
        result += data[i];
        if (i % 2 == 1 && i != data.Length - 1) result += "_";
      }

      return result;
    }

    private string combine(string str1, string str2, char separator = '_')
    {
      return $"{str1}{separator}{str2}";
    }

    private string makeCrcString(string data)
    {
      return reverse(getCrc(Utill.stringToByte(data)));
    }

    public string getRtuProtocol(string unitId, string funcCode, string startAddr, string dataCount)
    {
      var protocol = "";

      if (unitId != "" && funcCode != "" && startAddr != "" && dataCount != "")
      {
        protocol = Pipeline.excute(
            toHexString(unitId),
            value => combine(value, toHexString(funcCode)),
            value => combine(value, toHexString(startAddr, 2)),
            value => combine(value, toHexString(dataCount, 2)),
            value => combine(value, makeCrcString(value)));
      }

      return protocol;
    }
  }

  class Pipeline
  {
    public static dynamic excute(dynamic param, params Func<dynamic, dynamic>[] funcs)
    {
      var result = param;
      foreach (var func in funcs) result = func(result);

      return result;
    }
  }
}
