/*
* Соколов М.А.
* Класс для работы с битами.
*/

using System;
using System.Collections;

namespace CryptingTool
{
    /// <summary>
    /// Класс для работы с битами.
    /// Биты хранятся в массиве типа ArrayList, для облегчения работы с ними.
    /// </summary>
    class Bits
    {
        private ArrayList bits = new ArrayList();

        private int len = 0, num = 0; 

        /// <summary>
        /// Конструктор. Переводит целое значение в двоичный вид.
        /// </summary>
        public Bits(int value)
        {
            num = value; ToBits();
        }

        /// <summary>
        /// Конструктор. Код символа преобразует в двоичный вид.
        /// </summary>
        public Bits(char value)
        {
            num = (int)value; ToBits();
        }

        /// <summary>
        /// Конструктор. Код символа преобразует в двоичный вид.
        /// </summary>
        public Bits(byte value)
        {
            num = (int)value; ToBits();
        }

        /// <summary>
        /// Конструктор. Преобразует строку в "битовый массив"
        /// Все ненулевые элементы строки интерпритируются как '1'.
        /// </summary>
        public Bits(string value)
        {
            len = value.Length;

            for (int i = 0; i < len; i++)
                if (value[i] == '0') bits.Add(0);
                else bits.Add(1);
        }
        
        /// <summary>
        /// Свойство. Возвращает текущее количество бит.
        /// </summary>
        public int Length
        {
            get
            {
                return len;
            }
        }

        /// <summary>
        /// Свойство. Возвращает десятичное представление "битового массива".
        /// </summary>
        public int Number
        {
            get
            {
                ToInt(); return num;
            }
        }

        /// <summary>
        /// Свойство. Возвращает символ, который представлен текущим десятичным числом.
        /// </summary>
        public char Char
        {
            get
            {
                ToInt(); return (char)num;
            }
        }

        /// <summary>
        /// Операция индексации []. Возвращает i-ый элемент "битового массива".
        /// </summary>
        public int this[int i]
        {
            get
            {
                if (i >= 0 && i < len) return (int)bits[i];
                else return -1;
            }

            set
            {
                if (i >= 0 && i < len)
                    if (value > 0) bits[i] = 1;
                    else bits[i] = 0;
            }
        }

        /// <summary>
        /// Функция добавляет в конец новый элемент.
        /// Все ненулевые элементы интерпритируются как '1'.
        /// </summary>
        public void Add(object value)
        {
            if ((int)value == 0) bits.Add(0);
            else bits.Add(1);
            
            len++;
        }

        /// <summary>
        /// Функция вставляет в позицию index значение value
        /// Все ненулевые элементы интерпритируются как '1'.
        /// </summary>
        public void Insert(int index, object value)
        {
            if (index < 0 || index > len) return;

            if (value.Equals(0)) bits.Insert(index, 0);
            else bits.Insert(index, 1);

            len++;
        }

        /// <summary>
        /// Функция удаляет значение из позиции index.
        /// </summary>
        public void Erase(int index)
        {
            if (index < 0 || index > len) return;

            bits.RemoveAt(index); len--;
        }

        /// <summary>
        /// Функция инвертирует все биты. '0' = '1'; '1' = '0'.
        /// </summary>
        public void InvertBits()
        {
            for (int i = 0; i < len; i++)
                if (bits[i].Equals(0)) bits[i] = 1;
                else bits[i] = 0;
        }

        /// <summary>
        /// Функция разворачивает "битовый массив" задом наперед.
        /// </summary>
        public void Reverse()
        {
            bits.Reverse();
        }

        /// <summary>
        /// Функция преобразует текущий "битовый массив" в строку.
        /// </summary>
        public string ToString()
        {
            string str = "";

            for (int i = 0; i < len; i++)
                str += bits[i].ToString();

            return str;
        }

        /// <summary>
        /// Функция производит циклический сдвиг "битового массива" влево.
        /// </summary>
        public void ToLeft()
        {
            object tmp;

            for (int i = 1; i < len - 1; i++)
            {
                tmp = bits[i + 1]; bits[i + 1] = bits[i]; bits[i] = tmp;
            }

            tmp = bits[0]; bits[0] = bits[len - 1]; bits[len - 1] = tmp;
        }

        /// <summary>
        /// Функция производит циклический сдвиг "битового массива" вправо.
        /// </summary>
        public void ToRight()
        {
            object tmp;

            tmp = bits[0]; bits[0] = bits[len - 1]; bits[len - 1] = tmp;

            for (int i = len - 2; i != 0; i--)
            {
                tmp = bits[i]; bits[i] = bits[i + 1]; bits[i + 1] = tmp;
            }
        }

        /// <summary>
        /// Функция производит перевод десятичного числа в двоичное представление.
        /// </summary>
        private void ToBits()
        {
            int temp = num;

            while (temp != 0)
            {
                bits.Add(temp % 2); temp /= 2;
            }

            len = bits.Count; bits.Reverse();
        }

        /// <summary>
        /// Функция производит перевод из двоичного представления в десятичное.
        /// </summary>
        private void ToInt()
        {
            num = 0;

            for (int i = 0; i < len; i++)
                if (bits[len - i - 1].Equals(1))
                    num += (int)Math.Pow(2, i);
        }
    }
}