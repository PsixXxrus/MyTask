public class InputeParam
		{
			/// <summary>
			/// Уникальное имя параметра. 
			/// По которому необходимо будет производиться сопоставление, когда параметры в рабочем процессе будут переданы в форму.
			/// </summary>
			public string Key { get; set; } = "None";
			/// <summary>
			/// Тип параметра. Определяет способ получения значения параметра при настройке в сценарии и тип значения. 
			/// </summary>
			public Types Type { get; set; } = Types._string;
			/// <summary>
			/// Название параметра, которое увидит разработчик сценанрия.
			/// </summary>
			public string Name { get; set; } = "Visible name param";
			/// <summary>
			/// Описание параметра для разработчика сценария (подробная инструкция для получения ожидаемого значения).
			/// </summary>
			public string Description { get; set; } = "Description param";
			/// <summary>
			/// Значение параметра, которое будет без изменений передано на вход в режиме выполнения. Может не быть задано.
			/// </summary>
			public string Extra { get; set; } = null;

			/// <summary>Перечень возможных типов параметра</summary>
			public sealed class Types
			{
				// Закрытые поля только для чтения для обеспечения неизменяемости
				private readonly string _name;
				private readonly int _value;

				/// <summary>Название типа</summary>
				public string Name
				{
					get { return _name; }
				}

				/// <summary>Тип в INT значении</summary>
				public int Value
				{
					get { return _value; }
				}

				// Конструктор для инициализации значений
				public Types(string name, int value)
				{
					_name = name;
					_value = value;
				}

				// Статические предопределенные переменные для быстрого использования
				public static readonly Types _string = new Types("string", 1);
				public static readonly Types _int = new Types("int", 2);
				public static readonly Types _datetime = new Types("datetime", 3);
				public static readonly Types _argument = new Types("argument", 4);
				public static readonly Types _table = new Types("table", 5);
				public static readonly Types _tablequery = new Types("tablequery", 6);
				public static readonly Types _fixedlist = new Types("fixedlist", 7);
			}
		}
