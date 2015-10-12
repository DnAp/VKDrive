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
using Dokan;
using System.Data.SQLite;


namespace VKDrive
{

    class DownloadManager
    {
        protected int MaxBufferBlock;
        protected int DownloadBlockSize;
        System.IO.FileStream FStream;
        protected string CacheFileName;

        const int STATUS_PROCESS = 1;
        const int STATUS_OK = 2;
        const int STATUS_ERROR = 3;

        protected DownloadManager()
        {
            //new Dictionary<string, DMBlockInfo>();
            MaxBufferBlock = Properties.Settings.Default.CacheLength;
            DownloadBlockSize = Properties.Settings.Default.DownloadBlockSize;
            CacheFileName = System.IO.Path.GetTempFileName();
            Console.WriteLine(CacheFileName);
            FStream = System.IO.File.Create(CacheFileName);
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

        private int getBlockId(long offset)
        {
            decimal d = offset / DownloadBlockSize;
            return System.Convert.ToInt32(Math.Ceiling(d));
        }


        public int getBlock(Download finfo, byte[] buffer, ref uint readBytes, long offset)
        {
            Console.WriteLine("DM: c " + offset + " читать " + buffer.Length);
            #region Получение первичной информации
            try
            {
                lock (finfo)
                {
                    // кажись это нафик не нужный код
                    if (finfo.Length == 0)
                    {
                        HttpWebRequest WebReq = (HttpWebRequest)WebRequest.Create(finfo.Url);
                        WebReq.Timeout = Properties.Settings.Default.Timeout * 1000;
                        WebReq.Method = "HEAD";
                        System.Net.WebResponse result = WebReq.GetResponse();
                        finfo.Length = result.ContentLength;
                        result.Close();
                    }
                }
            }
            catch (Exception)
            {
                return -1;
            }
            #endregion
            
            if (offset >= finfo.Length)
            {
                readBytes = 0;
                return DokanNet.DOKAN_SUCCESS;
            }
            

            // Выбираем все блоки которые необходимы для загрузки

            long numByteToRead = offset + buffer.Length;
            
            // Попросили больше чем файл
            if (numByteToRead > finfo.Length)
            {
                numByteToRead = finfo.Length;
            }
            
            int blockIdStart = getBlockId(offset);
            int blockIdEnd = getBlockId(numByteToRead - 1);
            // Здесь -1, потомучто мы запрашиваем с 4 байта 4 байта.
            // Нам нужны 4,5,6,7. А если мы сложим 4+4 получим 8. Лишний байт может вызывать докачку блока.

            // С какого байта читать первый блок
            int strartBlockFromRead = Convert.ToInt32(offset - (blockIdStart * DownloadBlockSize));

            // До какого байта должен быть прочитан последний блок
            int endBlockToRead = Convert.ToInt32(numByteToRead - (blockIdEnd * DownloadBlockSize));
            // Console.WriteLine("End block" + endBlockToRead);
            try
            {
                Console.WriteLine("Readed: " + blockIdStart + ".." + blockIdEnd);
                startBlockDownload(finfo, blockIdStart, blockIdEnd);
            }
            catch (Exception)
            {
                return DokanNet.DOKAN_ERROR;
            }
            
            readBytes = 0;
            int counter;

            Dictionary<int, int[]> positionBlock = new Dictionary<int, int[]>();

            int[] fileUID = finfo.getUniqueId();

            counter = 0;
                
            // todo Раньше тут была интересная штука чтоб отдавать последний блок когда его еще не докачали 
            while (true)
            {
                SQLiteDataReader rows = DB.Instance.Execute(@"SELECT COUNT(*) FROM file_parts 
                    WHERE uid = " + fileUID[0] + " AND fid=" + fileUID[1] + " AND block_id>=" + blockIdStart 
                                    + " AND block_id<=" + blockIdEnd + " AND status = " + STATUS_PROCESS);
                if (rows.Read() && rows.GetInt32(0) == 0)
                {
                    // Все блоки переключили статус на зачачено
                    rows = DB.Instance.Execute("SELECT block_id, byte, byte_offset FROM file_parts "
                        + " WHERE uid = " + fileUID[0] + " AND fid=" + fileUID[1] + " AND block_id>=" + blockIdStart
                        + " AND block_id<=" + blockIdEnd + " AND status = " + STATUS_OK);
                    while (rows.Read())
                    {
                        positionBlock.Add(
                            rows.GetInt32(0), 
                            new int[] {rows.GetInt32(1), rows.GetInt32(2)}
                        );
                    }
                    if (positionBlock.Count < blockIdEnd - blockIdStart + 1)
                    { // Кто-то загрузился с ошибкой
                        return DokanNet.DOKAN_ERROR;
                    }

                    break;
                }

                counter++;
                if (counter == 20 * (blockIdEnd - blockIdStart + 1))
                {
                    Console.WriteLine("Длительное ожидание файла");
                }
                    
                Thread.Sleep(Properties.Settings.Default.DownloadSleepTimeout);
            }
            

            for (int blockId = blockIdStart; blockId <= blockIdEnd; blockId++)
            {
                lock (FStream)
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
                    Console.WriteLine("Переписываем из файла с " + FStream.Position + ", блок длинной " + count);
                    readBytes += (uint)FStream.Read(
                        buffer,
                        Convert.ToInt32(readBytes),
                        count
                    );
                }

            }
            Console.WriteLine("Итого отдали " + readBytes + " из " + buffer.Length + " запрашиваемых");

            return DokanNet.DOKAN_SUCCESS;
        }

        private void startBlockDownload(Download finfo, int blockIdStart, int blockIdEnd)
        {
            
            //Console.WriteLine(finfo.FileName + "\t" + blockIdStart + ".." + blockIdEnd+"\t\t"+finfo.Url);
            List<int[]> blockToDownload = new List<int[]>();
            int[] tmp = finfo.getUniqueId();
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
                SQLiteDataReader result = DB.Instance.Execute("INSERT OR IGNORE INTO file_parts "
                    + "( uid, fid, block_id, byte, byte_offset, status ) VALUES "
                    + "( " + tmp[0] + ", " + tmp[1] + ", " + blockId.ToString() + ", "
                    + download + ", (SELECT max(byte_offset) FROM file_parts) + " + DownloadBlockSize + ", "+STATUS_PROCESS+")");

                if (result.RecordsAffected == 0)// Означает что такая задача на закачку уже есть
                {
                    // Пытаемся обновить если таск ошибочный, мы выставим его на работу
                    result = DB.Instance.Execute("UPDATE file_parts SET status = "+STATUS_PROCESS+" WHERE uid = " + tmp[0] + " AND fid=" + tmp[1] + " AND block_id=" + blockId.ToString()+" AND status="+STATUS_ERROR);
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
            foreach (int[] DMBlocksBetween in blockToDownload)
            {
                
                new ThreadExecutor().Execute(() => DownloadThread(DMBlocksBetween, finfo));
            }
            
        }

        static void DownloadThread(int[] DMBlockBetween, Download finfo)
        {
            Thread.CurrentThread.Name = "DownloadThread: " + finfo.FileName + " Block: " + DMBlockBetween[0] + "-"+DMBlockBetween[1];
            
            DownloadManager DM = DownloadManager.Instance;
            int blockIdStart = DMBlockBetween[0];
            int blockIdEnd = DMBlockBetween[1];

            // Организовываем закачку
            //Console.WriteLine(finfo.Url);
            
            int blockId = blockIdStart;

            int[] fileUID = finfo.getUniqueId();
            SQLiteDataReader rows = DB.Instance.Execute(@"SELECT block_id, byte_offset FROM file_parts 
                        WHERE uid = " + fileUID[0] + " AND fid=" + fileUID[1] + " AND block_id>=" + blockIdStart + " AND block_id<=" + blockIdEnd);
            
            Dictionary<int, int> positionBlock = new Dictionary<int,int>();
            int maxPosition = 0;
            int byte_offset;
            while (rows.Read())
            {
                byte_offset = rows.GetInt32(1);
                positionBlock.Add(rows.GetInt32(0), byte_offset);
                if (maxPosition < byte_offset)
                    maxPosition = byte_offset;
            }
            lock (DM.FStream)
            {
                Console.WriteLine("Файл занимает: " + DM.FStream.Length + ". Максимальная позиция: " + (maxPosition + DM.DownloadBlockSize));
                if (DM.FStream.Length < maxPosition + DM.DownloadBlockSize)
                {
                    SQLiteDataReader data = DB.Instance.Execute("SELECT max(byte_offset) FROM file_parts");
                    data.Read();
                    DM.FStream.SetLength(data.GetInt32(0) + DM.DownloadBlockSize);
                }
            }

            try
            {
                
                HttpWebRequest WebReq = (HttpWebRequest)WebRequest.Create(finfo.Url);
                WebReq.Timeout = Properties.Settings.Default.Timeout * 1000;
                WebReq.AddRange(blockIdStart * DM.DownloadBlockSize, (blockIdEnd + 1) * DM.DownloadBlockSize);

                //Console.WriteLine("Start downdload: " + finfo.FileName+"\t" + blockIdStart + ".." + blockIdEnd + "\t\t" + finfo.Url);
            
                System.Net.WebResponse result = WebReq.GetResponse();

                System.IO.Stream stream = result.GetResponseStream();
                
                byte[] bufferWritter = new byte[1024];

                int read = 1;
                int skipRead;
                int downloaded = 0;

                while (read > 0)
                {
                    read = stream.Read(bufferWritter, 0, bufferWritter.Length);
                    
                    lock (DM.FStream)
                    {

                        DM.FStream.Position = positionBlock[blockId] + downloaded;
                        if (downloaded + read > DM.DownloadBlockSize)
                        {
                            if (DM.DownloadBlockSize - downloaded > 0)
                            {
                                DM.FStream.Write(bufferWritter, 0, DM.DownloadBlockSize - downloaded);
                            }
                            skipRead = DM.DownloadBlockSize - downloaded;

                            downloaded = 0;
                            
                            if (blockIdEnd == blockId) // Если блок конечный, range перестал работать, приходится выходить ручками
                            {
                                break;
                            }
                            DB.Instance.Execute("UPDATE file_parts SET status = " + STATUS_OK 
                                + " WHERE uid = " + fileUID[0] + " AND fid=" + fileUID[1] + " AND block_id = " + blockId);
                            
                            blockId++;
                            DM.FStream.Position = positionBlock[blockId];
                            
                            read -= skipRead;
                            DM.FStream.Write(bufferWritter, skipRead, read);

                        }
                        else
                        {
                            DM.FStream.Write(bufferWritter, 0, read);

                        }
                        
                    }

                    downloaded += read;
                }
                
                //Console.WriteLine("EndDowndloadBlock: " + blockIdStart + ".." + blockIdEnd + " End: " + blockId);
                DB.Instance.Execute("UPDATE file_parts SET status = " + STATUS_OK
                                + " WHERE uid = " + fileUID[0] + " AND fid=" + fileUID[1] + " AND block_id = " + blockId);

                stream.Close();
                result.Close();
                
            }
            catch (Exception e)
            {
                
                finfo.update();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(finfo.Url + " " + e.Message);
                Console.ResetColor();
                
                // Все пропало, отменяем загрзку
                do
                {
                    DB.Instance.Execute("UPDATE file_parts SET status = " + STATUS_ERROR
                                + " WHERE uid = " + fileUID[0] + " AND fid=" + fileUID[1]
                                + " AND block_id >= " + blockId + " AND block_id <= " + blockIdEnd);
                    blockId++;
                    
                } while (blockId <= blockIdEnd);
            }
            //Thread.CurrentThread.Abort();
        }

    }
}
