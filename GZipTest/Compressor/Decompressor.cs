using System;
using System.Linq;
using System.Threading;
using System.IO;
using System.IO.Compression;

namespace GZipTest
{

    class Decompressor : GZip
    {
        #region Constructor
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="input">Имя исходного файла</param>
        /// <param name="output">Имя целевого файла</param>
        /// <param name="size">Размер блока</param>
        public Decompressor(string input, string output) : base(input, output) { }
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
                    //Размер данных
                    int dataSize = 0;
                    //Буфер чтения
                    byte[] buffer;
                    //Хранит процент выполнения
                    double percent = 0.0;

                    //Циклично читаем данные до конца или состояния отмены
                    while (fs.Position < fs.Length && !_cancelled)
                    {
                        //Задаем буфер чтения
                        buffer = new byte[12];
                        //Прочитаем FEXTRA
                        fs.Read(buffer, 0, 12);

                        

                        //Прочитаем длинну блока
                        int blockLength = BitConverter.ToUInt16(buffer, 10);
                        //Изменим размер буфера
                        Array.Resize<byte>(ref buffer, buffer.Length + blockLength);
                        //Читаем данные
                        fs.Read(buffer, 12, blockLength);

                        //Проверяем, создан ли файл в нашей программе, иначе выдаем ошибку 
                        if (buffer[10] != 0x08 ||
                           buffer[11] != 0x00 ||
                           buffer[12] != 0x01 ||
                           buffer[13] != 0x01 ||
                           buffer[14] != 0x04 ||
                           buffer[15] != 0x00)
                        {
                            throw new Exception("File .gz is created not in GZipTest");
                        }

                        //Позиция
                        int pos = 12;
                        //В цикле пока не дойдем до конца
                        while (pos < blockLength + 12)
                        {
                            //Если последний и предпоследний байт равен 1
                            if (buffer[pos] == 1 && buffer[pos + 1] == 1)
                            {
                                //Получаем длинну данных
                                dataSize = BitConverter.ToInt32(buffer, pos + 4);
                                //Выходим
                                break;
                            }
                            //Иначе смещаемся и повторяем
                            pos = BitConverter.ToUInt16(buffer, pos + 2) + 4;
                        }
                        //Изменим размер буфера
                        Array.Resize<byte>(ref buffer, dataSize);
                        //Читаем данные
                        fs.Read(buffer, 12 + blockLength, dataSize - (12 + blockLength));

                        //Добавим в очередь на декомпрессию
                        _queueReader.EnqueueBytes(buffer);

                        buffer = null;

                        //Расчитаем и выведем процент выполнения
                        percent = (double)fs.Position / (double)fs.Length * 100.0;
                        Console.Title = $"{percent:0.00}%";
                    }
                    _queueReader.Close();
                }
                Console.WriteLine("Read file complete.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error is occured!\n Method: {ex.TargetSite}\n Error description: {ex.Message}");
                _cancelled = true;
            }
        }
        /// <summary>
        /// Разжатие
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

                //Размер порции
                int partSize = 4096;

                //Цикл пока не будет отмена или выход из цикла
                while (_queueReader.TryDequeue(out Block block) && !_cancelled)
                {
                    //Инициируем поток для данных
                    var ms = new MemoryStream(block.Data);
                    //Инициируем поток для расжатия
                    GZipStream gz = new GZipStream(ms, CompressionMode.Decompress);

                    //Установим размер буфера
                    buffer = new byte[partSize];
                    //Инициируем поток для чтения
                    using (MemoryStream msRead = new MemoryStream())
                    {
                        //Количество прочитанных байт
                        int readBytes = 0;
                        //Цикл
                        do
                        {
                            //Читаем данные
                            readBytes = gz.Read(buffer, 0, partSize);
                            //Если прочитано бульше 0
                            if (readBytes > 0)
                            {
                                //Пишем во временный поток
                                msRead.Write(buffer, 0, readBytes);
                            }
                        } while (readBytes > 0);
                        //Освобождаем поток
                        gz.Dispose();

                        //Получаем данные
                        buffer = msRead.ToArray();
                    }

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
 