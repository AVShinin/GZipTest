using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace GZipTest
{
    /// <summary>
    /// Абстрактный класс компрессора
    /// </summary>
    public abstract class GZip
    {
        //Состояние отмены
        protected bool _cancelled = false;
        //Имя исходного/целевого файла
        protected string sourceFile, destinationFile;
        //Количество процессоров на машине
        protected static int _threads = Environment.ProcessorCount;

        //Размер блока
        const int DEF_BLOCK_SIZE = 1024 * 32; //8Kb
        protected int blockSize = DEF_BLOCK_SIZE;
        //Пул на чтение
        protected QueueManager _queueReader = new QueueManager(20);
        //Пул на запись
        protected QueueManager _queueWriter = new QueueManager(20);
        //Массив уведомлений равный кол-ву процессоров
        protected ManualResetEvent[] doneEvents = new ManualResetEvent[_threads];

        #region Constructors
        /// <summary>
        /// Конструктор
        /// </summary>
        public GZip()
        {
        }
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="input">Имя исходного файла</param>
        /// <param name="output">Имя целевого файла</param>
        /// <param name="size">Размер блока в Kb</param>
        public GZip(string input, string output)
        {
            this.sourceFile = input;
            this.destinationFile = output;
        }
        #endregion

        /// <summary>
        /// Возвращает результат выполнения
        /// </summary>
        /// <returns>0 если отменено или не доступно, иначе 1</returns>
        public int CallBackResult()
        {
            if (!_cancelled)
                return 0;
            return 1;
        }
        /// <summary>
        /// Отмена
        /// </summary>
        public void Cancel()
        {
            _cancelled = true;
        }
        //Запуск
        public void Launch()
        {
            Console.WriteLine("Start...");

            //Таймер выполнения
            //var timer = Stopwatch.StartNew();

            //Поток для чтения файла
            new Thread(new ThreadStart(Read)).Start();

            //Поток для записи файла
            new Thread(new ThreadStart(Write)).Start();

            //Цикл равный кол-ву ядер
            for (int i = 0; i < _threads; i++)
            {
                //Создаем сигнал-событие для i-того потока
                doneEvents[i] = new ManualResetEvent(false);
                //Поток для сжатия
                new Thread(new ParameterizedThreadStart(Run)).Start(i);
            }

            //Ожидаем завершения всех потоков
            WaitHandle.WaitAll(doneEvents);
            //Сообщаем ожидающим, что больше данных для записи не будет
            _queueWriter.Close();


            //Если не было отменено
            if (!_cancelled)
            {

                //Завершаем таймер
                //timer.Stop();
                //Console.WriteLine($"Time: {timer.Elapsed}");

                //Сообщаем о завершении
                Console.WriteLine("Wait for it to complete writing to the file...");
            }
        }

        /// <summary>
        /// Запись файла
        /// </summary>
        protected void Write()
        {
            try
            {
                //Создаем файл и открываем поток для записи
                using (var fs = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.Write))
                {
                    //Цикл пока не будет отмена или выход из цикла
                    while (_queueWriter.TryDequeue(out Block block) && !_cancelled)
                    {
                        //Иначе пишем блок в поток
                        fs.Write(block.Data, 0, block.Data.Length);
                        fs.Flush();

                        //Освобождение ресурсов
                        block.Dispose();
                    }
                }
                //Сообщаем пользователю о завершении
                Console.WriteLine("Write in file complete.");
            }
            //В случае ошибки
            catch (Exception ex)
            {
                //Сообщаем пользователю
                Console.WriteLine($"Error is occured!\n Method: {ex.TargetSite}\n Error description: {ex.Message}");
                //Установим флаг отмены
                _cancelled = true;
            }
        }

        protected abstract void Read();
        protected abstract void Run(object i);
    }
}
