using System;
using System.Collections.Generic;
using System.Windows.Forms;
using OCTell.Plugins; // пространство имён Oktell SDK

namespace MyMinimalPlugin
{
    // Главный класс плагина — ОБЯЗАТЕЛЬНО должен реализовывать IOktellPluginForm
    public class PluginMain : IOktellPluginForm
    {
        public string PluginID => "MyMinimalPlugin"; // уникальный идентификатор
        public string PluginName => "Пример плагина Oktell";

        public PluginMain()
        {
            // Конструктор
        }

        // Возвращает входные параметры формы
        public Dictionary<string, string> GetInputParams(Guid idPlugin, Guid idForm)
        {
            var input = new Dictionary<string, string>
            {
                { "phone", "Номер телефона" }
            };
            return input;
        }

        // Возвращает выходные параметры формы
        public Dictionary<string, string> GetOutputParams(Guid idPlugin, Guid idForm)
        {
            var output = new Dictionary<string, string>
            {
                { "result", "Результат работы плагина" }
            };
            return output;
        }

        // Основной метод запуска
        public Dictionary<string, string> Run(Dictionary<string, string> inputParams, 
                                              out bool success, 
                                              out string message)
        {
            success = true;
            message = "Плагин успешно выполнен";

            var output = new Dictionary<string, string>
            {
                { "result", $"Обработан номер: {inputParams["phone"]}" }
            };

            return output;
        }

        // Метод возвращает WinForm-контрол (если форма требуется)
        public Control GetControl(Guid idPlugin, Guid idForm)
        {
            return new Label
            {
                Text = "Минимальный плагин Oktell работает!",
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
        }
    }
}