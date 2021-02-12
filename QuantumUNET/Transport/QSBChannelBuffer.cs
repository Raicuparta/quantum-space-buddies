using QuantumUNET.Messages;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace QuantumUNET.Transport
{
	internal class QSBChannelBuffer : IDisposable
	{
		public int NumMsgsOut { get; private set; }
		public int NumBufferedMsgsOut { get; private set; }
		public int NumBytesOut { get; private set; }
		public int NumMsgsIn { get; private set; }
		public int NumBytesIn { get; private set; }
		public int NumBufferedPerSecond { get; private set; }
		public int LastBufferedPerSecond { get; private set; }
		public const int MaxPendingPacketCount = 16;
		public const int MaxBufferedPackets = 512;
		public float MaxDelay = 0.01f;

		private readonly QSBNetworkConnection _connection;
		private QSBChannelPacket _currentPacket;
		private float _lastFlushTime;
		private readonly byte _channelId;
		private int _maxPacketSize;
		private readonly bool _isReliable;
		private bool _allowFragmentation;
		private bool _isBroken;
		private int _maxPendingPacketCount;
		private const int _maxFreePacketCount = 512;
		private readonly Queue<QSBChannelPacket> _pendingPackets;
		private static List<QSBChannelPacket> _freePackets;
		internal static int _pendingPacketCount;
		private float _lastBufferedMessageCountTimer = Time.realtimeSinceStartup;
		private static readonly QSBNetworkWriter _sendWriter = new QSBNetworkWriter();
		private static readonly QSBNetworkWriter _fragmentWriter = new QSBNetworkWriter();
		private const int _packetHeaderReserveSize = 100;
		private bool _disposed;
		internal QSBNetBuffer _fragmentBuffer = new QSBNetBuffer();
		private bool _readingFragment;

		public QSBChannelBuffer(QSBNetworkConnection conn, int bufferSize, byte cid, bool isReliable, bool isSequenced)
		{
			_connection = conn;
			_maxPacketSize = bufferSize - 100;
			_currentPacket = new QSBChannelPacket(_maxPacketSize, isReliable);
			_channelId = cid;
			_maxPendingPacketCount = 16;
			_isReliable = isReliable;
			_allowFragmentation = isReliable && isSequenced;
			if (isReliable)
			{
				_pendingPackets = new Queue<QSBChannelPacket>();
				if (_freePackets == null)
				{
					_freePackets = new List<QSBChannelPacket>();
				}
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed && disposing)
			{
				if (_pendingPackets != null)
				{
					while (_pendingPackets.Count > 0)
					{
						_pendingPacketCount--;
						var item = _pendingPackets.Dequeue();
						if (_freePackets.Count < 512)
						{
							_freePackets.Add(item);
						}
					}
					_pendingPackets.Clear();
				}
			}
			_disposed = true;
		}

		public bool SetOption(ChannelOption option, int value)
		{
			bool result;
			if (option != ChannelOption.MaxPendingBuffers)
			{
				if (option != ChannelOption.AllowFragmentation)
				{
					if (option != ChannelOption.MaxPacketSize)
					{
						result = false;
					}
					else if (!_currentPacket.IsEmpty() || _pendingPackets.Count > 0)
					{
						Debug.LogError("Cannot set MaxPacketSize after sending data.");
						result = false;
					}
					else if (value <= 0)
					{
						Debug.LogError("Cannot set MaxPacketSize less than one.");
						result = false;
					}
					else if (value > _maxPacketSize)
					{
						Debug.LogError(
							$"Cannot set MaxPacketSize to greater than the existing maximum ({_maxPacketSize}).");
						result = false;
					}
					else
					{
						_currentPacket = new QSBChannelPacket(value, _isReliable);
						_maxPacketSize = value;
						result = true;
					}
				}
				else
				{
					_allowFragmentation = value != 0;
					result = true;
				}
			}
			else if (!_isReliable)
			{
				result = false;
			}
			else if (value < 0 || value >= 512)
			{
				Debug.LogError(
					$"Invalid MaxPendingBuffers for channel {_channelId}. Must be greater than zero and less than {512}");
				result = false;
			}
			else
			{
				_maxPendingPacketCount = value;
				result = true;
			}
			return result;
		}

		public void CheckInternalBuffer()
		{
			if (Time.realtimeSinceStartup - _lastFlushTime > MaxDelay && !_currentPacket.IsEmpty())
			{
				SendInternalBuffer();
				_lastFlushTime = Time.realtimeSinceStartup;
			}
			if (Time.realtimeSinceStartup - _lastBufferedMessageCountTimer > 1f)
			{
				LastBufferedPerSecond = NumBufferedPerSecond;
				NumBufferedPerSecond = 0;
				_lastBufferedMessageCountTimer = Time.realtimeSinceStartup;
			}
		}

		public bool SendWriter(QSBNetworkWriter writer)
		{
			var arraySegment = writer.AsArraySegment();
			return SendBytes(arraySegment.Array, arraySegment.Count);
		}

		public bool Send(short msgType, QSBMessageBase msg)
		{
			_sendWriter.StartMessage(msgType);
			msg.Serialize(_sendWriter);
			_sendWriter.FinishMessage();
			NumMsgsOut++;
			return SendWriter(_sendWriter);
		}

		internal bool HandleFragment(QSBNetworkReader reader)
		{
			bool result;
			if (reader.ReadByte() == 0)
			{
				if (!_readingFragment)
				{
					_fragmentBuffer.SeekZero();
					_readingFragment = true;
				}
				var array = reader.ReadBytesAndSize();
				_fragmentBuffer.WriteBytes(array, (ushort)array.Length);
				result = false;
			}
			else
			{
				_readingFragment = false;
				result = true;
			}
			return result;
		}

		internal bool SendFragmentBytes(byte[] bytes, int bytesToSend)
		{
			var num = 0;
			while (bytesToSend > 0)
			{
				var num2 = Math.Min(bytesToSend, _maxPacketSize - 32);
				var array = new byte[num2];
				Array.Copy(bytes, num, array, 0, num2);
				_fragmentWriter.StartMessage(17);
				_fragmentWriter.Write(0);
				_fragmentWriter.WriteBytesFull(array);
				_fragmentWriter.FinishMessage();
				SendWriter(_fragmentWriter);
				num += num2;
				bytesToSend -= num2;
			}
			_fragmentWriter.StartMessage(17);
			_fragmentWriter.Write(1);
			_fragmentWriter.FinishMessage();
			SendWriter(_fragmentWriter);
			return true;
		}

		internal bool SendBytes(byte[] bytes, int bytesToSend)
		{
			bool result;
			if (bytesToSend >= 65535)
			{
				Debug.LogError($"ChannelBuffer:SendBytes cannot send packet larger than {ushort.MaxValue} bytes");
				result = false;
			}
			else if (bytesToSend <= 0)
			{
				Debug.LogError("ChannelBuffer:SendBytes cannot send zero bytes");
				result = false;
			}
			else if (bytesToSend > _maxPacketSize)
			{
				if (_allowFragmentation)
				{
					result = SendFragmentBytes(bytes, bytesToSend);
				}
				else
				{
					Debug.LogError(
						$"Failed to send big message of {bytesToSend} bytes. The maximum is {_maxPacketSize} bytes on channel:{_channelId}");
					result = false;
				}
			}
			else if (!_currentPacket.HasSpace(bytesToSend))
			{
				if (_isReliable)
				{
					if (_pendingPackets.Count == 0)
					{
						if (!_currentPacket.SendToTransport(_connection, _channelId))
						{
							QueuePacket();
						}
						_currentPacket.Write(bytes, bytesToSend);
						result = true;
					}
					else if (_pendingPackets.Count >= _maxPendingPacketCount)
					{
						if (!_isBroken)
						{
							Debug.LogError($"ChannelBuffer buffer limit of {_pendingPackets.Count} packets reached.");
						}
						_isBroken = true;
						result = false;
					}
					else
					{
						QueuePacket();
						_currentPacket.Write(bytes, bytesToSend);
						result = true;
					}
				}
				else if (!_currentPacket.SendToTransport(_connection, _channelId))
				{
					Debug.Log($"ChannelBuffer SendBytes no space on unreliable channel {_channelId}");
					result = false;
				}
				else
				{
					_currentPacket.Write(bytes, bytesToSend);
					result = true;
				}
			}
			else
			{
				_currentPacket.Write(bytes, bytesToSend);
				result = MaxDelay != 0f || SendInternalBuffer();
			}
			return result;
		}

		private void QueuePacket()
		{
			_pendingPacketCount++;
			_pendingPackets.Enqueue(_currentPacket);
			_currentPacket = AllocPacket();
		}

		private QSBChannelPacket AllocPacket()
		{
			QSBChannelPacket result;
			if (_freePackets.Count == 0)
			{
				result = new QSBChannelPacket(_maxPacketSize, _isReliable);
			}
			else
			{
				var channelPacket = _freePackets[_freePackets.Count - 1];
				_freePackets.RemoveAt(_freePackets.Count - 1);
				channelPacket.Reset();
				result = channelPacket;
			}
			return result;
		}

		private static void FreePacket(QSBChannelPacket packet)
		{
			if (_freePackets.Count < 512)
			{
				_freePackets.Add(packet);
			}
		}

		public bool SendInternalBuffer()
		{
			bool result;
			if (_isReliable && _pendingPackets.Count > 0)
			{
				while (_pendingPackets.Count > 0)
				{
					var channelPacket = _pendingPackets.Dequeue();
					if (!channelPacket.SendToTransport(_connection, _channelId))
					{
						_pendingPackets.Enqueue(channelPacket);
						break;
					}
					_pendingPacketCount--;
					FreePacket(channelPacket);
					if (_isBroken && _pendingPackets.Count < _maxPendingPacketCount / 2)
					{
						Debug.LogWarning("ChannelBuffer recovered from overflow but data was lost.");
						_isBroken = false;
					}
				}
				result = true;
			}
			else
			{
				result = _currentPacket.SendToTransport(_connection, _channelId);
			}
			return result;
		}
	}
}