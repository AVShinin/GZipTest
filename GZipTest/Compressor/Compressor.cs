using System;
using System.Threading;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Runtime;

namespace GZipTest
{
    class Compressor : GZip
    {
        #region Constructor
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="input">Имя исходного файла</param>
        /// <param name="output">Имя целевого файла</param>
        /// <param name="size">Размер блока</param>
        public Compressor(string input, string output) : base(input, output) {}
        #endregion

        /// <summary>
        /// Чтение файла
        /// </summary>
        protected override void Read()
        {
            try
            {
                //Открываем файл для чтения
                using (var fs = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    //Размер читаемых данных
                    long readBytes = 0;
                    //Буфер чтения
                    byte[] readBuffer;
                    //Хранит процент выполнения
                    double percent = 0.0;

                    //Циклично читаем данные до конца или состояния отмены
                    while (fs.Position < fs.Length && !_cancelled)
                    {
                        //Установим размер читаемых данных
                        readBytes = blockSize;

                        //Если размер не прочинанных данных меньше чем размер блока, читаем оставшиеся данные
                        if (fs.Length - fs.Position < blockSize)
                            readBytes = fs.Length - fs.Position;

                        //Задаем буфер
                        readBuffer = new byte[readBytes];
                        //Читаем порцию данных
                        fs.Read(readBuffer, 0, readBuffer.Length);

                        //Добавляем в очередь на сжатие
                        _queueReader.EnqueueBytes(readBuffer);
                        
                        //Расчитаем и выведем процент выполнения
                        percent = (double)fs.Position / (double)fs.Length * 100.0;
                        Console.Title = $"{percent:0.00}%";

                        //Освобождаем ссылку
                        readBuffer = null;
                    }
                    //Сообщаем ожидающим, что больше данных не будет
                    _queueReader.Close();
                }
                Console.WriteLine("Read file complete.");
            }
            //В случае ошибки
            catch (Exception ex)
            {
                //Сообщим пользователю
                Console.WriteLine($"Error is occured!\n Method: {ex.TargetSite}\n Error description: {ex.Message}");
                //Установим состояние отмены
                _cancelled = true;
            }
        }
        /// <summary>
        /// Сжатие
        /// </summary>
        /// <param name="i"></param>
        protected override void Run(object i)
        {
            try
            {
                //Получаем сигнал по номеру идентификатора
                ManualResetEvent doneEvent = doneEvents[(int)i];

                //Буфер данных
                byte[] buffer;

                //Цикл пока не будет отмена или выход из цикла
                while (_queueReader.TryDequeue(out Block block) && !_cancelled)
                {
                    //Инициируем поток для хранения временных данных
                    using (var ms = new MemoryStream())
                    {
                        //Инициируем GZip поток
                        using (var gzip = new GZipStream(ms, CompressionMode.Compress, false))
                        {
                            //Пишем в поток
                            gzip.Write(block.Data, 0, block.Data.Length);
                            //Фиксируем и освобождаем
                            gzip.Flush();
                        }
                        //Получаем из временного потока данные
                        buffer = ms.ToArray();
                    }

                    //FEXTRA
                    //10bytes
                    int totalLenght = buffer.Length + 10;
                    //Установим флаг FEXTRA
                    buffer[3] = 0x04;
                    //Запишем FEXTRA
                    Array.Resize<byte>(ref buffer, totalLenght);
                    Array.Copy(buffer, 10, buffer, 20, totalLenght - 20);
                    //Метка о том, что файл наш
                    buffer[10] = 0x08;
                    buffer[11] = 0x00;
                    buffer[12] = 0x01;
                    buffer[13] = 0x01;
                    buffer[14] = 0x04;
                    buffer[15] = 0x00;
                    //Пишем длинну блока
                    BitConverter.GetBytes(totalLenght).CopyTo(buffer, 16);

                    //Создадим новый блок
                    var newBlock = new Block(block.ID, buffer);

                    //Очистим ссылки
                    buffer = null;

                    //Добавляем блок к очереди на запись
                    _queueWriter.Enqueue(newBlock);

                    //Освобождение ресурсов
                    block.Dispose();
                }
                //Сообщаем ожидающему, что можно продолжать
                doneEvent.Set();
            }
            //В случае ошибки
            catch (Exception ex)
            {
                //Сообщим пользователю в каком потоке ошибка
                Console.WriteLine($"Error is occured!\n Error in thread number {i}. \n Method: {ex.TargetSite}\n Error description: {ex.Message}");
                //Установим состояние отмены
                _cancelled = true;
            }
        }
    }
}