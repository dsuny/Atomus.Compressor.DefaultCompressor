using System;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Atomus.Compressor
{
    public class DefaultCompressor : ICompress, ICompressAsync, IDecompress, IDecompressAsync
    {
        byte[] ICompress.ToBytes(byte[] source)
        {
            byte[] compressedByte;

            try
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (Stream stream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                    {
                        stream.Write(source, 0, source.Length);
                        stream.Close();
                    }

                    memoryStream.Position = 0;

                    compressedByte = new byte[memoryStream.Length];

                    memoryStream.Read(compressedByte, 0, (int)memoryStream.Length);
                    memoryStream.Close();
                }

                return compressedByte;
            }
            catch (AtomusException exception)
            {
                throw exception;
            }
            catch (Exception exception)
            {
                throw new AtomusException(exception);
            }
        }
        async Task<byte[]> ICompressAsync.ToBytesAsync(byte[] source)
        {
            byte[] compressedByte;
            int readBytes;

            try
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (Stream stream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                    {
                        await stream.WriteAsync(source, 0, source.Length);
                        stream.Close();
                    }

                    memoryStream.Position = 0;

                    compressedByte = new byte[memoryStream.Length];

                    readBytes = await memoryStream.ReadAsync(compressedByte, 0, (int)memoryStream.Length);
                    memoryStream.Close();
                }

                return compressedByte;
            }
            catch (AtomusException exception)
            {
                throw exception;
            }
            catch (Exception exception)
            {
                throw new AtomusException(exception);
            }
        }

        byte[] ICompress.ToBytes(ISerializable source)
        {
            BinaryFormatter binaryFormatter;
            MemoryStream memoryStream;

            try
            {
                if (source is DataSet set)
                    set.RemotingFormat = SerializationFormat.Binary;

                using (memoryStream = new MemoryStream())
                {
                    binaryFormatter = new BinaryFormatter();
                    binaryFormatter.Serialize(memoryStream, source);//직렬화
                    memoryStream.Close();
                }

                return ((ICompress)this).ToBytes(memoryStream.ToArray());
            }
            catch (AtomusException exception)
            {
                throw exception;
            }
            catch (Exception exception)
            {
                throw new AtomusException(exception);
            }
        }
        async Task<byte[]> ICompressAsync.ToBytesAsync(ISerializable source)
        {
            BinaryFormatter binaryFormatter;
            MemoryStream memoryStream;
            Task pendingTask = Task.FromResult<bool>(true);
            var previousTask = pendingTask;

            memoryStream = null;

            try
            {
                if (source is DataSet set)
                    set.RemotingFormat = SerializationFormat.Binary;

                using (memoryStream = new MemoryStream())
                {
                    binaryFormatter = new BinaryFormatter();

                    pendingTask = Task.Run(async () =>
                    {
                        await previousTask;
                        binaryFormatter.Serialize(memoryStream, source);//직렬화
                    }
                                            );
                    await pendingTask;
                    memoryStream.Close();
                }

                return await ((ICompressAsync)this).ToBytesAsync(memoryStream.ToArray());
            }
            catch (AtomusException exception)
            {
                throw exception;
            }
            catch (Exception exception)
            {
                throw new AtomusException(exception);
            }
        }

        string ICompress.ToString(ISerializable source)
        {
            byte[] compressedByte;

            try
            {
                compressedByte = ((ICompress)this).ToBytes(source);

                return Convert.ToBase64String(compressedByte);
            }
            catch (AtomusException exception)
            {
                throw exception;
            }
            catch (Exception exception)
            {
                throw new AtomusException(exception);
            }
        }
        async Task<string> ICompressAsync.ToStringAsync(ISerializable source)
        {
            byte[] compressedByte;

            try
            {
                compressedByte = await ((ICompressAsync)this).ToBytesAsync(source);

                return Convert.ToBase64String(compressedByte);
            }
            catch (AtomusException exception)
            {
                throw exception;
            }
            catch (Exception exception)
            {
                throw new AtomusException(exception);
            }
        }

        string ICompress.ToString(string source)
        {
            byte[] compressedByte;

            try
            {
                if (source != null)
                    if (!source.Equals(""))
                    {
                        compressedByte = ((ICompress)this).ToBytes(UTF8Encoding.Default.GetBytes(source));

                        return Convert.ToBase64String(compressedByte);
                    }

                return null;
            }
            catch (AtomusException exception)
            {
                throw exception;
            }
            catch (Exception exception)
            {
                throw new AtomusException(exception);
            }
        }
        async Task<string> ICompressAsync.ToStringAsync(string source)
        {
            byte[] compressedByte;

            try
            {
                if (source != null)
                    if (!source.Equals(""))
                    {
                        compressedByte = await ((ICompressAsync)this).ToBytesAsync(UTF8Encoding.Default.GetBytes(source));

                        return Convert.ToBase64String(compressedByte);
                    }

                return null;
            }
            catch (AtomusException exception)
            {
                throw exception;
            }
            catch (Exception exception)
            {
                throw new AtomusException(exception);
            }
        }


        byte[] IDecompress.ToBytes(byte[] source)
        {
            byte[] decompressedByte;
            int readBytes;

            try
            {
                using (MemoryStream sourceMemoryStream = new MemoryStream(source))
                {
                    using (MemoryStream resultMemoryStream = new MemoryStream())
                    {
                        using (Stream stream = new GZipStream(sourceMemoryStream, CompressionMode.Decompress))
                        {
                            sourceMemoryStream.Seek(0, 0);
                            decompressedByte = new byte[source.Length];

                            while (true)
                            {
                                readBytes = stream.Read(decompressedByte, 0, decompressedByte.Length);

                                if (readBytes < 1)
                                    break;

                                resultMemoryStream.Write(decompressedByte, 0, readBytes);
                            }

                            stream.Close();
                        }

                        decompressedByte = new byte[resultMemoryStream.Length];

                        resultMemoryStream.Seek(0, 0);
                        resultMemoryStream.Read(decompressedByte, 0, decompressedByte.Length);
                        resultMemoryStream.Close();
                    }
                    sourceMemoryStream.Close();
                }

                return decompressedByte;
            }
            catch (AtomusException exception)
            {
                throw exception;
            }
            catch (Exception exception)
            {
                throw new AtomusException(exception);
            }
        }
        async Task<byte[]> IDecompressAsync.ToBytesAsync(byte[] source)
        {
            byte[] decompressedByte;
            int readBytes;

            try
            {
                using (MemoryStream sourceMemoryStream = new MemoryStream(source))
                {
                    using (MemoryStream resultMemoryStream = new MemoryStream())
                    {
                        using (Stream stream = new GZipStream(sourceMemoryStream, CompressionMode.Decompress))
                        {
                            sourceMemoryStream.Seek(0, 0);
                            decompressedByte = new byte[source.Length];

                            while (true)
                            {
                                readBytes = await stream.ReadAsync(decompressedByte, 0, decompressedByte.Length);

                                if (readBytes < 1)
                                    break;

                                await resultMemoryStream.WriteAsync(decompressedByte, 0, readBytes);
                            }

                            stream.Close();
                        }

                        decompressedByte = new byte[resultMemoryStream.Length];

                        resultMemoryStream.Seek(0, 0);
                        readBytes = await resultMemoryStream.ReadAsync(decompressedByte, 0, decompressedByte.Length);
                        resultMemoryStream.Close();
                    }
                    sourceMemoryStream.Close();
                }

                return decompressedByte;
            }
            catch (AtomusException exception)
            {
                throw exception;
            }
            catch (Exception exception)
            {
                throw new AtomusException(exception);
            }
        }

        ISerializable IDecompress.ToSerializable(byte[] source)
        {
            ISerializable serializable;
            byte[] decompressedByte;
            BinaryFormatter binaryFormatter;

            try
            {
                decompressedByte = ((IDecompress)this).ToBytes(source);

                using (MemoryStream memoryStream = new MemoryStream(decompressedByte))
                {
                    memoryStream.Seek(0, 0);

                    binaryFormatter = new BinaryFormatter();

                    serializable = (ISerializable)binaryFormatter.Deserialize(memoryStream);//역직렬화
                    memoryStream.Close();
                }

                return serializable;
            }
            catch (AtomusException exception)
            {
                throw exception;
            }
            catch (Exception exception)
            {
                throw new AtomusException(exception);
            }
        }
        async Task<ISerializable> IDecompressAsync.ToSerializableAsync(byte[] source)
        {
            ISerializable serializable;
            byte[] decompressedByte;
            BinaryFormatter binaryFormatter;
            Task pendingTask = Task.FromResult<bool>(true);
            var previousTask = pendingTask;

            serializable = null;

            try
            {
                decompressedByte = await ((IDecompressAsync)this).ToBytesAsync(source);

                using (MemoryStream memoryStream = new MemoryStream(decompressedByte))
                {
                    memoryStream.Seek(0, 0);

                    binaryFormatter = new BinaryFormatter();

                    pendingTask = Task.Run(async () =>
                    {
                        await previousTask;
                        serializable = (ISerializable)binaryFormatter.Deserialize(memoryStream);//역직렬화
                    }
                                            );
                    await pendingTask;
                    memoryStream.Close();
                }

                return serializable;
            }
            catch (AtomusException exception)
            {
                throw exception;
            }
            catch (Exception exception)
            {
                throw new AtomusException(exception);
            }
        }

        ISerializable IDecompress.ToSerializable(string source)
        {
            try
            {
                return ((IDecompress)this).ToSerializable(Convert.FromBase64String(source));
            }
            catch (AtomusException exception)
            {
                throw exception;
            }
            catch (Exception exception)
            {
                throw new AtomusException(exception);
            }
        }
        async Task<ISerializable> IDecompressAsync.ToSerializableAsync(string source)
        {
            try
            {
                return await ((IDecompressAsync)this).ToSerializableAsync(Convert.FromBase64String(source));
            }
            catch (AtomusException exception)
            {
                throw exception;
            }
            catch (Exception exception)
            {
                throw new AtomusException(exception);
            }
        }

        string IDecompress.ToString(string source)
        {
            byte[] decompressedByte;

            try
            {
                if (source != null)
                    if (!source.Equals(""))
                    {
                        decompressedByte = ((IDecompress)this).ToBytes(Convert.FromBase64String(source));

                        return UTF8Encoding.Default.GetString(decompressedByte);
                    }

                return null;
            }
            catch (AtomusException exception)
            {
                throw exception;
            }
            catch (Exception exception)
            {
                throw new AtomusException(exception);
            }
        }
        async Task<string> IDecompressAsync.ToStringAsync(string source)
        {
            byte[] decompressedByte;

            try
            {
                if (source != null)
                    if (!source.Equals(""))
                    {
                        decompressedByte = await ((IDecompressAsync)this).ToBytesAsync(Convert.FromBase64String(source));

                        return UTF8Encoding.Default.GetString(decompressedByte);
                    }
                return null;
            }
            catch (AtomusException exception)
            {
                throw exception;
            }
            catch (Exception exception)
            {
                throw new AtomusException(exception);
            }
        }
    }
}
