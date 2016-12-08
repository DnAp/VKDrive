using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using System.Xml.Linq;
using System.Collections;
using VKDrive.Files;
using VKDrive.Utils;
using DokanNet;
using System.Data.SQLite;
using log4net;
using System.Reflection;

namespace VKDrive
{

    class DownloadManager
    {
        protected int MaxBufferBlock;
        protected int DownloadBlockSize;
        protected System.IO.FileStream FStream;
        protected Object FStreamLock;
        protected string CacheFileName;

        const int StatusProcess = 1;
        const int StatusOk = 2;
        const int StatusError = 3;

        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected DownloadManager()
        {
            //new Dictionary<string, DMBlockInfo>();
            MaxBufferBlock = Properties.Settings.Default.CacheLength;
            DownloadBlockSize = Properties.Settings.Default.DownloadBlockSize;
            CacheFileName = System.IO.Path.GetTempFileName();
            Console.WriteLine(CacheFileName);
            FStream = System.IO.File.Create(CacheFileName);
            FStreamLock = new Object();
        }

        ~DownloadManager()
        {
            try
            {
                System.IO.File.Delete(CacheFileName);
            }
            catch (Exception) { }
        }

        private sealed class SingletonCreator
        {
            private static readonly DownloadManager instance = new DownloadManager();
            public static DownloadManager Instance { get { return instance; } }
        }

        public static DownloadManager Instance
        {
            get { return SingletonCreator.Instance; }
        }

        private int GetBlockId(long offset)
        {
            decimal d = offset / DownloadBlockSize;
            return System.Convert.ToInt32(Math.Ceiling(d));
        }


        public NtStatus GetBlock(Download finfo, byte[] buffer, ref int readBytes, long offset)
        {
            _log.Debug("DM: c " + offset + " читать " + buffer.Length);
            #region Получение первичной информации
            try
            {
                lock (FStreamLock)
                {
                    // кажись это нафик не нужный код
                    if (finfo.Length == 0)
                    {
                        HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(finfo.Url);
                        webReq.Timeout = Properties.Settings.Default.Timeout * 1000;
                        webReq.Method = "HEAD";
                        var result = webReq.GetResponse();
                        finfo.Length = result.ContentLength;
                        result.Close();
                    }
                }
            }
            catch (Exception)
            {
                return DokanResult.Error;
            }
            #endregion
            
            if (offset >= finfo.Length)
            {
                readBytes = 0;
                return DokanResult.Success;
            }
            

            // Выбираем все блоки которые необходимы для загрузки

            long numByteToRead = offset + buffer.Length;
            
            // Попросили больше чем файл
            if (numByteToRead > finfo.Length)
            {
                numByteToRead = finfo.Length;
            }
            
            int blockIdStart = GetBlockId(offset);
            int blockIdEnd = GetBlockId(numByteToRead - 1);
            // Здесь -1, потомучто мы запрашиваем с 4 байта 4 байта.
            // Нам нужны 4,5,6,7. А если мы сложим 4+4 получим 8. Лишний байт может вызывать докачку блока.

            // С какого байта читать первый блок
            int strartBlockFromRead = Convert.ToInt32(offset - (blockIdStart * DownloadBlockSize));

            // До какого байта должен быть прочитан последний блок
            int endBlockToRead = Convert.ToInt32(numByteToRead - (blockIdEnd * DownloadBlockSize));
            // Console.WriteLine("End block" + endBlockToRead);
            try
            {
                _log.Debug("Readed: " + blockIdStart + ".." + blockIdEnd);
                StartBlockDownload(finfo, blockIdStart, blockIdEnd);
            }
            catch (Exception)
            {
                return DokanResult.Error;
            }
            
            readBytes = 0;
            int counter;

            Dictionary<int, int[]> positionBlock = new Dictionary<int, int[]>();

            int[] fileUid = finfo.GetUniqueId();

            counter = 0;
                
            // todo Раньше тут была интересная штука чтоб отдавать последний блок когда его еще не докачали 
            while (true)
            {
                SQLiteDataReader rows = Db.Instance.Execute(@"SELECT COUNT(*) FROM file_parts 
                    WHERE uid = " + fileUid[0] + " AND fid=" + fileUid[1] + " AND block_id>=" + blockIdStart 
                                    + " AND block_id<=" + blockIdEnd + " AND status = " + StatusProcess);
                if (rows.Read() && rows.GetInt32(0) == 0)
                {
                    // Все блоки переключили статус на зачачено
                    rows = Db.Instance.Execute("SELECT block_id, byte, byte_offset FROM file_parts "
                        + " WHERE uid = " + fileUid[0] + " AND fid=" + fileUid[1] + " AND block_id>=" + blockIdStart
                        + " AND block_id<=" + blockIdEnd + " AND status = " + StatusOk);
                    while (rows.Read())
                    {
                        positionBlock.Add(
                            rows.GetInt32(0), 
                            new int[] {rows.GetInt32(1), rows.GetInt32(2)}
                        );
                    }
                    if (positionBlock.Count < blockIdEnd - blockIdStart + 1)
                    { // Кто-то загрузился с ошибкой
                        return DokanResult.Error;
                    }

                    break;
                }

                counter++;
                if (counter == 20 * (blockIdEnd - blockIdStart + 1))
                {
                    _log.Warn("Длительное ожидание файла");
                }
                    
                Thread.Sleep(Properties.Settings.Default.DownloadSleepTimeout);
            }
            

            for (int blockId = blockIdStart; blockId <= blockIdEnd; blockId++)
            {
                lock (FStreamLock)
                {
                    
                    int count;
                    if (blockIdStart == blockId)
                    {
                        FStream.Position = positionBlock[blockId][1] + strartBlockFromRead;

                        if (positionBlock[blockId][0] - strartBlockFromRead > buffer.Length)
                        {
                            count = Convert.ToInt32(buffer.Length);
                        }
                        else
                        {
                            count = positionBlock[blockId][0] - strartBlockFromRead;
                        }
                    }
                    else
                    {
                        FStream.Position = positionBlock[blockId][1];
                        if (positionBlock[blockId][0] > buffer.Length - readBytes)
                        {
                            count = Convert.ToInt32(buffer.Length - readBytes);
                        }
                        else
                        {
                            count = positionBlock[blockId][0];
                        }
                    }
                    _log.Debug("Переписываем из файла с " + FStream.Position + ", блок длинной " + count);
                    readBytes += FStream.Read(
                        buffer,
                        Convert.ToInt32(readBytes),
                        count
                    );
                }

            }
            _log.Debug("Итого отдали " + readBytes + " из " + buffer.Length + " запрашиваемых");

            return DokanResult.Success;
        }

        private void StartBlockDownload(Download finfo, int blockIdStart, int blockIdEnd)
        {
            
            //Console.WriteLine(finfo.FileName + "\t" + blockIdStart + ".." + blockIdEnd+"\t\t"+finfo.Url);
            List<int[]> blockToDownload = new List<int[]>();
            int[] tmp = finfo.GetUniqueId();
            if (tmp.Count() < 2)
                return;

            int[] between = null;
            int download = DownloadBlockSize;
            for (int blockId = blockIdStart; blockId <= blockIdEnd; blockId++)
            {
                if (finfo.Length < DownloadBlockSize * (blockId+1) )
                {
                    download = Convert.ToInt32(finfo.Length) - (DownloadBlockSize * blockId);
                }
                SQLiteDataReader result = Db.Instance.Execute("INSERT OR IGNORE INTO file_parts "
                    + "( uid, fid, block_id, byte, byte_offset, status ) VALUES "
                    + "( " + tmp[0] + ", " + tmp[1] + ", " + blockId.ToString() + ", "
                    + download + ", (SELECT max(byte_offset) FROM file_parts) + " + DownloadBlockSize + ", "+StatusProcess+")");

                if (result.RecordsAffected == 0)// Означает что такая задача на закачку уже есть
                {
                    // Пытаемся обновить если таск ошибочный, мы выставим его на работу
                    result = Db.Instance.Execute("UPDATE file_parts SET status = "+StatusProcess+" WHERE uid = " + tmp[0] + " AND fid=" + tmp[1] + " AND block_id=" + blockId.ToString()+" AND status="+StatusError);
                    if (result.RecordsAffected == 0) // Нет ошибок, кто-то уже работает над этим, подождем
                    {
                        if (between != null)
                        {
                            blockToDownload.Add(between);
                            between = null;
                        }
                        continue;
                    }
                }
                if (between == null)
                {
                    between = new int[2];
                    between[0] = blockId;
                    between[1] = blockId;
                }
                else
                    between[1] = blockId;
            }
            if (between != null)
                blockToDownload.Add(between);

            // Стартуем закачку
            foreach (int[] dmBlocksBetween in blockToDownload)
            {
                
                new ThreadExecutor().Execute(() => DownloadThread(dmBlocksBetween, finfo));
            }
            
        }

        static void DownloadThread(int[] dmBlockBetween, Download finfo)
        {
            Thread.CurrentThread.Name = "DownloadThread: " + finfo.FileName + " Block: " + dmBlockBetween[0] + "-"+dmBlockBetween[1];
            
            DownloadManager dm = DownloadManager.Instance;
            int blockIdStart = dmBlockBetween[0];
            int blockIdEnd = dmBlockBetween[1];

            // Организовываем закачку
            //Console.WriteLine(finfo.Url);
            
            int blockId = blockIdStart;

            int[] fileUid = finfo.GetUniqueId();
            SQLiteDataReader rows = Db.Instance.Execute(@"SELECT block_id, byte_offset FROM file_parts 
                        WHERE uid = " + fileUid[0] + " AND fid=" + fileUid[1] + " AND block_id>=" + blockIdStart + " AND block_id<=" + blockIdEnd);
            
            Dictionary<int, int> positionBlock = new Dictionary<int,int>();
            int maxPosition = 0;
            int byteOffset;
            while (rows.Read())
            {
                byteOffset = rows.GetInt32(1);
                positionBlock.Add(rows.GetInt32(0), byteOffset);
                if (maxPosition < byteOffset)
                    maxPosition = byteOffset;
            }
            lock (dm.FStreamLock)
            {
                Console.WriteLine("Файл занимает: " + dm.FStream.Length + ". Максимальная позиция: " + (maxPosition + dm.DownloadBlockSize));
                if (dm.FStream.Length < maxPosition + dm.DownloadBlockSize)
                {
                    SQLiteDataReader data = Db.Instance.Execute("SELECT max(byte_offset) FROM file_parts");
                    data.Read();
                    dm.FStream.SetLength(data.GetInt32(0) + dm.DownloadBlockSize);
                }
            }

            try
            {
                
                HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(finfo.Url);
                webReq.Timeout = Properties.Settings.Default.Timeout * 1000;
                webReq.AddRange(blockIdStart * dm.DownloadBlockSize, (blockIdEnd + 1) * dm.DownloadBlockSize);

                //Console.WriteLine("Start downdload: " + finfo.FileName+"\t" + blockIdStart + ".." + blockIdEnd + "\t\t" + finfo.Url);
            
                System.Net.WebResponse result = webReq.GetResponse();

                System.IO.Stream stream = result.GetResponseStream();
                
                byte[] bufferWritter = new byte[1024];

                int read = 1;
                int skipRead;
                int downloaded = 0;

                while (read > 0)
                {
                    read = stream.Read(bufferWritter, 0, bufferWritter.Length);
                    
                    lock (dm.FStreamLock)
                    {

                        dm.FStream.Position = positionBlock[blockId] + downloaded;
                        if (downloaded + read > dm.DownloadBlockSize)
                        {
                            if (dm.DownloadBlockSize - downloaded > 0)
                            {
                                dm.FStream.Write(bufferWritter, 0, dm.DownloadBlockSize - downloaded);
                            }
                            skipRead = dm.DownloadBlockSize - downloaded;

                            downloaded = 0;
                            
                            if (blockIdEnd == blockId) // Если блок конечный, range перестал работать, приходится выходить ручками
                            {
                                break;
                            }
                            Db.Instance.Execute("UPDATE file_parts SET status = " + StatusOk 
                                + " WHERE uid = " + fileUid[0] + " AND fid=" + fileUid[1] + " AND block_id = " + blockId);
                            
                            blockId++;
                            dm.FStream.Position = positionBlock[blockId];
                            
                            read -= skipRead;
                            dm.FStream.Write(bufferWritter, skipRead, read);

                        }
                        else
                        {
                            dm.FStream.Write(bufferWritter, 0, read);

                        }
                        
                    }

                    downloaded += read;
                }
                
                //Console.WriteLine("EndDowndloadBlock: " + blockIdStart + ".." + blockIdEnd + " End: " + blockId);
                Db.Instance.Execute("UPDATE file_parts SET status = " + StatusOk
                                + " WHERE uid = " + fileUid[0] + " AND fid=" + fileUid[1] + " AND block_id = " + blockId);

                stream.Close();
                result.Close();
                
            }
            catch (Exception e)
            {
                
                finfo.Update();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(finfo.Url + " " + e.Message);
                Console.ResetColor();
                
                // Все пропало, отменяем загрзку
                do
                {
                    Db.Instance.Execute("UPDATE file_parts SET status = " + StatusError
                                + " WHERE uid = " + fileUid[0] + " AND fid=" + fileUid[1]
                                + " AND block_id >= " + blockId + " AND block_id <= " + blockIdEnd);
                    blockId++;
                    
                } while (blockId <= blockIdEnd);
            }
            //Thread.CurrentThread.Abort();
        }

    }
}
