using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace GZipTest
{
    /// <summary>
    /// Блок байтов
    /// </summary>
    public class Block : IDisposable
    {
        #region Publics Properties
        /// <summary>
        /// Идентификатор
        /// </summary>
        public int ID { get; set; }
        /// <summary>
        /// Массив байт
        /// </summary>
        public byte[] Data { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="id">Идентификатор</param>
        /// <param name="buffer">Массив байт</param>
        public Block(int id, byte[] data)
        {
            ID = id;
            Data = data;
        }
        #endregion
        /// <summary>
        /// Инициирует освобождение ресурсов
        /// </summary>
        public void Dispose()
        {
            ID = 0;
            Data = null;
            //Запускаем сборку мусора вручную
            //GC.Collect();
        }
        //Деструктор
        ~Block() { Dispose(); }
    }
}
