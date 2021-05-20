﻿using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Communication.Modbus
{
    //单例
    public class RTU
    {
        private Action<int, List<byte>> ResponseData;

        private static RTU _instance;
        private static SerialInfo _serialInfo;

        SerialPort _serialPort;
        bool _isBusing;  //是否忙碌

        int _currentSlave;
        int _wordLen;
        int _funcCode;
        int _startAddr;

        public RTU(SerialInfo serialInfo)
        {
            _serialInfo = serialInfo;
        }

        public static RTU GetInstance(SerialInfo serialInfo)
        {
            lock ("rtu")
            {
                if (_instance == null)
                    _instance = new RTU(serialInfo);
                return _instance;
            }
        }

        public bool Connection()
        {
            try
            {
                if (_serialPort.IsOpen)
                    _serialPort.Close();

                _serialPort.PortName = _serialInfo.PortName;
                _serialPort.BaudRate = _serialInfo.BaudRate;
                _serialPort.Parity = _serialInfo.Parity;
                _serialPort.DataBits = _serialInfo.DataBit;
                _serialPort.StopBits = _serialInfo.StopBits;

                _serialPort.ReceivedBytesThreshold = 1;
                _serialPort.DataReceived += _serialPort_DataReceived;

                _serialPort.Open();
            }
            catch (Exception)
            {
                return false;
            }
            return false;
        }

        public void Dispose()
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
                _serialPort.Dispose();
                _serialPort = null;
            }
        }

        int _receiveByteCount = 0;
        byte[] _byteBuffer = new byte[512];

        /// <summary>
        /// 向串口发送数据，串口接收数据时触发，处理接收到的报文逻辑
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte _receiveBytes;
            while (_serialPort.BytesToRead > 0)
            {
                _receiveBytes = (byte)_serialPort.ReadByte();
                _byteBuffer[_receiveByteCount] = _receiveBytes;
                _receiveByteCount++;
                if (_receiveByteCount >= 512)
                {
                    _receiveByteCount = 0;
                    //清理串口输入缓冲区
                    _serialPort.DiscardInBuffer();
                    return;
                }
            }

            if (_byteBuffer[0] == (byte)_currentSlave && _byteBuffer[1] == _funcCode && _receiveByteCount >= _wordLen + 5)
            {
                ResponseData?.Invoke(_startAddr, new List<byte>(SubByteArray(_byteBuffer, 0, _wordLen + 3)));
            }
        }

        public async Task<bool> Send(int salveAddr, byte funcCode, int startAddr, int len)
        {
            _currentSlave = salveAddr;
            _funcCode = funcCode;
            _startAddr = startAddr;
            if (funcCode == 0x01)
                _wordLen = len / 8 + ((len % 8 > 0) ? 1 : 0);
            if (funcCode == 0x03)
                _wordLen = len;

            List<byte> sendBuffer = new List<byte>();
            sendBuffer.Add((byte)salveAddr);
            sendBuffer.Add(funcCode);
            sendBuffer.Add((byte)(startAddr / 256));
            sendBuffer.Add((byte)(startAddr % 256));
            sendBuffer.Add((byte)(len / 256));
            sendBuffer.Add((byte)(len % 256));

            byte[] crc = Crc16(sendBuffer.ToArray(), 6);
            sendBuffer.AddRange(crc);

            try
            {
                //为避免并发问题，忙的时候一直让它循环
                while (_isBusing) { };
                _isBusing = true;
                _serialPort.Write(sendBuffer.ToArray(), 0, 8);
                _isBusing = false;
                await Task.Delay(1000);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 截取Byte数组
        /// </summary>
        /// <returns></returns>
        private byte[] SubByteArray(byte[] data, int start, int len)
        {
            byte[] Res = new byte[len];
            if (data != null && data.Length > len)
            {
                for (int i = 0; i < len; i++)
                {
                    Res[i] = data[i + start];
                }
            }
            return Res;
        }

        #region  CRC校验
        private static readonly byte[] aucCRCHi = {
             0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
             0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
             0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
             0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
             0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
             0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
             0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
             0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
             0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
             0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
             0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
             0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
             0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
             0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
             0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
             0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
             0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
             0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
             0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
             0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
             0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
             0x00, 0xC1, 0x81, 0x40
         };
        private static readonly byte[] aucCRCLo = {
             0x00, 0xC0, 0xC1, 0x01, 0xC3, 0x03, 0x02, 0xC2, 0xC6, 0x06, 0x07, 0xC7,
             0x05, 0xC5, 0xC4, 0x04, 0xCC, 0x0C, 0x0D, 0xCD, 0x0F, 0xCF, 0xCE, 0x0E,
             0x0A, 0xCA, 0xCB, 0x0B, 0xC9, 0x09, 0x08, 0xC8, 0xD8, 0x18, 0x19, 0xD9,
             0x1B, 0xDB, 0xDA, 0x1A, 0x1E, 0xDE, 0xDF, 0x1F, 0xDD, 0x1D, 0x1C, 0xDC,
             0x14, 0xD4, 0xD5, 0x15, 0xD7, 0x17, 0x16, 0xD6, 0xD2, 0x12, 0x13, 0xD3,
             0x11, 0xD1, 0xD0, 0x10, 0xF0, 0x30, 0x31, 0xF1, 0x33, 0xF3, 0xF2, 0x32,
             0x36, 0xF6, 0xF7, 0x37, 0xF5, 0x35, 0x34, 0xF4, 0x3C, 0xFC, 0xFD, 0x3D,
             0xFF, 0x3F, 0x3E, 0xFE, 0xFA, 0x3A, 0x3B, 0xFB, 0x39, 0xF9, 0xF8, 0x38,
             0x28, 0xE8, 0xE9, 0x29, 0xEB, 0x2B, 0x2A, 0xEA, 0xEE, 0x2E, 0x2F, 0xEF,
             0x2D, 0xED, 0xEC, 0x2C, 0xE4, 0x24, 0x25, 0xE5, 0x27, 0xE7, 0xE6, 0x26,
             0x22, 0xE2, 0xE3, 0x23, 0xE1, 0x21, 0x20, 0xE0, 0xA0, 0x60, 0x61, 0xA1,
             0x63, 0xA3, 0xA2, 0x62, 0x66, 0xA6, 0xA7, 0x67, 0xA5, 0x65, 0x64, 0xA4,
             0x6C, 0xAC, 0xAD, 0x6D, 0xAF, 0x6F, 0x6E, 0xAE, 0xAA, 0x6A, 0x6B, 0xAB,
             0x69, 0xA9, 0xA8, 0x68, 0x78, 0xB8, 0xB9, 0x79, 0xBB, 0x7B, 0x7A, 0xBA,
             0xBE, 0x7E, 0x7F, 0xBF, 0x7D, 0xBD, 0xBC, 0x7C, 0xB4, 0x74, 0x75, 0xB5,
             0x77, 0xB7, 0xB6, 0x76, 0x72, 0xB2, 0xB3, 0x73, 0xB1, 0x71, 0x70, 0xB0,
             0x50, 0x90, 0x91, 0x51, 0x93, 0x53, 0x52, 0x92, 0x96, 0x56, 0x57, 0x97,
             0x55, 0x95, 0x94, 0x54, 0x9C, 0x5C, 0x5D, 0x9D, 0x5F, 0x9F, 0x9E, 0x5E,
             0x5A, 0x9A, 0x9B, 0x5B, 0x99, 0x59, 0x58, 0x98, 0x88, 0x48, 0x49, 0x89,
             0x4B, 0x8B, 0x8A, 0x4A, 0x4E, 0x8E, 0x8F, 0x4F, 0x8D, 0x4D, 0x4C, 0x8C,
             0x44, 0x84, 0x85, 0x45, 0x87, 0x47, 0x46, 0x86, 0x82, 0x42, 0x43, 0x83,
             0x41, 0x81, 0x80, 0x40
         };
        private byte[] Crc16(byte[] pucFrame, int usLen)
        {
            int i = 0;
            byte crcHi = 0xFF;
            byte crcLo = 0xFF;
            UInt16 iIndex = 0x0000;

            while (usLen-- > 0)
            {
                iIndex = (UInt16)(crcLo ^ pucFrame[i++]);
                crcLo = (byte)(crcHi ^ aucCRCHi[iIndex]);
                crcHi = aucCRCLo[iIndex];
            }

            return new byte[] { crcLo, crcHi };
        }


        #endregion
    }
}
