using System;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace Lab8WinForms
{

    public class XmlLogger
    {
        private string _logPath;

        public XmlLogger(string _logPath)
        {
            this._logPath = _logPath;
        }

        public void CreateLog(string logMessage)
        {
            if (!File.Exists(_logPath))
            {
                // Если файла нет, создаём его
                using (XmlWriter writer = XmlWriter.Create(_logPath))
                {
                    writer.WriteStartDocument();
                    writer.WriteAttributeString("date", DateTime.Now.ToString("yyyy-MM-dd"));
                    writer.WriteString(logMessage);
                    writer.WriteEndElement();
                    writer.Close();
                }
            }
            else 
            {
                // Загрузить XML-документ
                XDocument logDocument = XDocument.Load(_logPath);

                // Создать новый элемент
                XElement NewLogElement = new XElement("entry");
                NewLogElement.Add(new XAttribute("time", DateTime.Now.ToString("HH:mm:ss")));
                NewLogElement.Value = logMessage;

                // Добавить новый элемент к корневому элементу
                logDocument.Root.Add(NewLogElement);
                logDocument.Root.Add(new XElement("entry"));
                // Сохранить обновленный документ
                logDocument.Save(_logPath);
            }
        }

        public string GetLastString()
        {
            string last = File.ReadAllLines(_logPath).Last();
            return last;
        }
    }

    public class JsonLogger
    {
        private string _logPath;

        public JsonLogger(string _logPath)
        {
            this._logPath = _logPath;
        }

        public void CreateLog(string logMessage)
        {
            if (!File.Exists(_logPath))
            {
                var logEntry = new { timestamp = DateTime.Now, logMessage = "=== Начало лога ===" };
                var json = JsonConvert.SerializeObject(logEntry);
                json += "\n";
                File.AppendAllText(_logPath, json);
            }
            else
            {
                var logEntry = new { timestamp = DateTime.Now, logMessage = logMessage };
                var json = JsonConvert.SerializeObject(logEntry);
                json += "\n";
                File.AppendAllText(_logPath, json);
            }
        }
    }
}
